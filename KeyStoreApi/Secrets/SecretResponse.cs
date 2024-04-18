namespace KeyStoreApi.Secrets;

public sealed record SecretResponse {
    public required string Secret { get; set; }
}