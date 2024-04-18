namespace KeyStoreApi.Shared;

public sealed record ErrorResponse {
    public required string Title { get; set; }
    public required int Status { get; set; }
    public required string Backend { get; set; }
    public required string Details { get; set; }
}