using System.IO.Hashing;
using System.Text;
using Azure;
using Azure.Security.KeyVault.Secrets;

namespace KeyStoreApi.Secrets.Service;

public sealed class SecretService {
    private const string KeyTag = "Key";
    private const string NoSuchSecret = "No such secret '{0}'";
    private readonly SecretClient _client;

    public SecretService(SecretClient client) {
        _client = client;
    }

    /// <summary>
    ///     Fetches all secret keys present in the vault
    /// </summary>
    /// <param name="ctx">Cancellation token</param>
    /// <returns>Success or failure results. Payload is a IEnumerable of key strings</returns>
    public async Task<Shared.Response<IEnumerable<string>>> ListSecrets(CancellationToken ctx) {
        try {
            var secretProperties = await GetSecrets(ctx);
            return Shared.Response<IEnumerable<string>>.Success(secretProperties
                .Select(x => x.Tags.TryGetValue(KeyTag, out var val) ? val : string.Empty)
                .Where(x => !string.IsNullOrWhiteSpace(x)));
        }
        catch (OperationCanceledException) {
            return Shared.Response<IEnumerable<string>>.Cancel();
        }
        catch (Exception e) {
            // Something unexpected happened
            return Shared.Response<IEnumerable<string>>.Error(e, nameof(ListSecrets)); // 500
        }
    }

    /// <summary>
    ///     Fetch properties of all secrets in the vault
    /// </summary>
    /// <param name="ctx">Cancellation token</param>
    /// <returns>List of all secret properties</returns>
    private async Task<List<SecretProperties>> GetSecrets(CancellationToken ctx) {
        var secrets = new List<SecretProperties>();
        var secretProperties = _client.GetPropertiesOfSecretsAsync(ctx);
        await foreach (var secret in secretProperties)
            if (secret.Enabled ?? false) {
                secrets.Add(secret);
            }

        return secrets;
    }

    /// <summary>
    ///     Fetches a secret by key.
    /// </summary>
    /// <param name="key">Key to fetch secret with</param>
    /// <param name="ctx">Cancellation token</param>
    /// <returns>Success or failure result. Payload is the secret value.</returns>
    public async Task<Shared.Response<string>> GetSecret(string key, CancellationToken ctx) {
        try {
            if (string.IsNullOrWhiteSpace(key)) {
                return Shared.Response<string>.NotFound(string.Format(NoSuchSecret, key));
            }
            // 404

            var secret = await _client.GetSecretAsync(Encode(key), cancellationToken: ctx);
            if (!secret.HasValue) {
                return Shared.Response<string>.NotFound(string.Format(NoSuchSecret, key));
            }

            return Shared.Response<string>.Success(secret.Value.Value); // 200
        }
        catch (OperationCanceledException) {
            return Shared.Response<string>.Cancel();
        }
        catch (RequestFailedException e) when (e.ErrorCode == "SecretNotFound") {
            return Shared.Response<string>.NotFound(string.Format(NoSuchSecret, key)); //404
        }
        catch (Exception e) {
            Console.WriteLine(e.GetType());
            return Shared.Response<string>.Error(e, nameof(GetSecret)); // 500
        }
    }

    /// <summary>
    ///     Sets secret by key and value.
    /// </summary>
    /// <param name="key">Secret key to set</param>
    /// <param name="value">Value to set</param>
    /// <param name="ctx">Cancellation token</param>
    /// <returns>Success or failure response. 'true' payload indicates secret has been committed to the vault.</returns>
    public async Task<Shared.Response<bool>> SetSecret(string key, string value, CancellationToken ctx) {
        try {
            var problems = new List<string>();

            if (string.IsNullOrWhiteSpace(key)) {
                problems.Add(nameof(key));
            }

            if (string.IsNullOrWhiteSpace(value)) {
                problems.Add(nameof(key));
            }

            if (problems.Count != 0) {
                return Shared.Response<bool>.BadRequest(problems);
            }
            // 400

            // Key vault just creates a new version, if we are trying to update an existing secret. Spec demands 409
            // on this case. We must check existence first.
            var encodedKey = Encode(key);
            var secrets = await GetSecrets(ctx);
            if (secrets.Exists(x => {
                    var hasKey = x.Tags.TryGetValue(KeyTag, out var keyTag);
                    if (!hasKey) {
                        return false;
                    }
                    return keyTag?.Equals(keyTag, StringComparison.InvariantCulture) ?? false;
                })) {
                return Shared.Response<bool>.Conflict(
                "Secret already exists",
                $"Secret with key '{key}' already present. This is not a limitation of Azure Key Vault, but arbitrary limit imposed by the spec: https://github.com/nuts-foundation/secret-store-api/blob/main/nuts-storage-api-v1.yaml"
                );
            }

            var secret = new KeyVaultSecret(encodedKey, value) {
                Properties = {
                    Tags = {
                        {
                            KeyTag, key
                        }
                    }
                }
            };

            _ = await _client.SetSecretAsync(secret, ctx);
            return Shared.Response<bool>.Success(true); // 200
        }
        catch (OperationCanceledException) {
            return Shared.Response<bool>.Cancel();
        }
        catch (Exception e) {
            return Shared.Response<bool>.Error(e, nameof(SetSecret)); // 500
        }
    }

    /// <summary>
    ///     Removes a secret. Initiates the delete, waits for it to finish and then purges the secret.
    /// </summary>
    /// <param name="key">Key to delete.</param>
    /// <param name="ctx">Cancellation token. In case of cancellation, attempts to revert the action.</param>
    /// <returns>Success or failure result. 'true' payload indicates secret has been deleted.</returns>
    public async Task<Shared.Response<bool>> RemoveSecret(string key, CancellationToken ctx) {
        try {
            if (string.IsNullOrWhiteSpace(key)) {
                return Shared.Response<bool>.BadRequest(nameof(key));
            }
            // 400

            var encodedKey = Encode(key);

            // try/catch is such BS pattern-- can't we just return failures :(
            try {
                // Actually just soft deletes secret and does not wait for the operation to complete. 
                try {
                    var op = await _client.StartDeleteSecretAsync(encodedKey, ctx);
                    await op.WaitForCompletionAsync(ctx);
                    await _client.PurgeDeletedSecretAsync(encodedKey, ctx);
                }
                catch (OperationCanceledException) {
                    // This should be done, even if the cancellation is requested, to roll back the delete.
                    await _client.StartRecoverDeletedSecretAsync(encodedKey, CancellationToken.None);
                    throw;
                }
            }
            // This can only be thrown from the StartDeleteSecretAsync()
            // We making sure no lingering soft-deleted secret exists, since they "reserve" a name, and
            // a new secret with the same name cannot be created until the soft-deleted secret is gone.
            catch (RequestFailedException e) when (e.ErrorCode == "SecretNotFound") {
                // If this also throws, we know for sure the secret was already gone, or never existed.
                await _client.PurgeDeletedSecretAsync(encodedKey, ctx);
            }

            return Shared.Response<bool>.Success(true); // 200
        }
        catch (OperationCanceledException) {
            return Shared.Response<bool>.Cancel(); // Cancellation
        }
        catch (RequestFailedException e) when (e.ErrorCode == "SecretNotFound") {
            return Shared.Response<bool>.NotFound(string.Format(NoSuchSecret, key)); //404
        }
        catch (Exception e) {
            return Shared.Response<bool>.Error(e, nameof(RemoveSecret));
        }
    }

    /// <summary>
    ///     Encodes secret names to be compatible with key vault secret naming restrictions.
    /// </summary>
    /// <param name="key">Clear text secret name</param>
    /// <returns>Encoded secret name</returns>
    public static string Encode(string key) {
        if (string.IsNullOrWhiteSpace(key)) {
            throw new ArgumentNullException(nameof(key));
        }

        var bytes = XxHash128.Hash(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(bytes);
    }
}