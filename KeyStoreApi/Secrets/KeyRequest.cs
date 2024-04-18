namespace KeyStoreApi.Secrets;

public record KeyRequest {
    public required string Key { get; set; }
}