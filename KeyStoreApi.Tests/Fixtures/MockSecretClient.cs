using Azure;
using Azure.Security.KeyVault.Secrets;
using NSubstitute;

namespace KeyStoreApi.Tests.Fixtures;

public sealed class MockSecretClient : SecretClient {
    public Exception? ToThrow = null;
    public List<(string key, string value, bool enabled)> VaultSecrets { get; } = [];

    public Response<KeyVaultSecret> PopSecret() {
        var toReturn = VaultSecrets.First();
        VaultSecrets.Remove(toReturn);

        var secret = new KeyVaultSecret(toReturn.key, toReturn.value) {
            Properties = {
                Enabled = toReturn.enabled
            }
        };
        return Response.FromValue(secret, null!);
    }

    public override AsyncPageable<SecretProperties> GetPropertiesOfSecretsAsync(CancellationToken ct) {
        ct.ThrowIfCancellationRequested();
        if (ToThrow is not null) {
            throw ToThrow;
        }

        var asMocks = VaultSecrets.Select(x => new MockSecretProperties(x.key, x.value, x.enabled));
        var asPages = new[] {
            Page<SecretProperties>.FromValues(asMocks.ToList(), null, Substitute.For<Response>())
        };
        return AsyncPageable<SecretProperties>.FromPages(asPages);
    }

    public override Task<Response<KeyVaultSecret>> GetSecretAsync(
            string name,
            string? version = null,
            CancellationToken ct = new()
        ) {
        ct.ThrowIfCancellationRequested();
        if (ToThrow is not null) {
            throw ToThrow;
        }

        return Task.FromResult(PopSecret());
    }

    public override Task<DeleteSecretOperation> StartDeleteSecretAsync(string key, CancellationToken ct) {
        ct.ThrowIfCancellationRequested();
        if (ToThrow is not null) {
            throw ToThrow;
        }

        var op = Substitute.For<DeleteSecretOperation>();
        return Task.FromResult(op);
    }

    public override Task<Response> PurgeDeletedSecretAsync(string key, CancellationToken ct) {
        ct.ThrowIfCancellationRequested();
        if (ToThrow is not null) {
            throw ToThrow;
        }

        return Task.FromResult<Response>(null!);
    }

    public override Task<Response<KeyVaultSecret>> SetSecretAsync(KeyVaultSecret secret, CancellationToken ct) {
        ct.ThrowIfCancellationRequested();
        if (ToThrow is not null) {
            throw ToThrow;
        }

        var size = secret.Name.Length;
        if (size is < 1 or > 63) {
            throw new ArgumentException($"Secret name wrong length {size} [1, 63]");
        }

        VaultSecrets.Add(new ValueTuple<string, string, bool>(
        secret.Name,
        secret.Value,
        secret.Properties.Enabled ?? true));
        return Task.FromResult<Response<KeyVaultSecret>>(null!);
    }
}