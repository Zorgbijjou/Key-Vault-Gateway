using FastEndpoints;

namespace KeyStoreApi.Health;

public sealed class HealthEndpoint : EndpointWithoutRequest<ServiceStatusResponse> {
    private const string Ok = "ok";

    public override void Configure() {
        Get("/health");
        AllowAnonymous();
    }

    // We kinda don't start if we are not healthy, so-- let's just return ok status, k8s will
    // respond 503 if we are not up.
    public override async Task HandleAsync(CancellationToken ct) {
        Logger.BeginScope(new HealthCheckScope());
        Logger.LogInformation("Liveness requested");
        
        await SendAsync(new ServiceStatusResponse {
            Status = ServiceHealth.pass,
            Description = Ok
        }, cancellation: ct);
        
        Logger.LogInformation("Liveness response sent");
    }
}