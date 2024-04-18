namespace KeyStoreApi.Secrets.Post;

public sealed record SecretRequestBody {
    public required string Secret { get; set; }
}