using FastEndpoints;

namespace KeyStoreApi.Secrets.Post;

public sealed record SecretKeyValuePairRequest {
    public required string Key { get; set; }

    [FromBody]
    public required SecretRequestBody RequestBody { get; set; }
}