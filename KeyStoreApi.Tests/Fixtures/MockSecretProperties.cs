using Azure.Security.KeyVault.Secrets;
using KeyStoreApi.Secrets.Service;

namespace KeyStoreApi.Tests.Fixtures;

public sealed class MockSecretProperties : SecretProperties {
    public MockSecretProperties(string key, string value, bool enabled) : base(key) {
        Name = SecretService.Encode(key);
        Id = new Uri($"https://mock.key.vault/secrets/{Name}");
        Version = "1";
        ContentType = "text/plain";
        Enabled = enabled;
        Tags.Add("Key", key);
    }

    public new Uri Id { get; }
    public new string Name { get; }
    public new string Version { get; }
}