namespace KeyStoreApi.Health;

public sealed record HealthCheckScope {
    public string Now { get; init; } = DateTime.UtcNow.ToString("O");
}
