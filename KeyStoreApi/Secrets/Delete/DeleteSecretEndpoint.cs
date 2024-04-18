using FastEndpoints;
using KeyStoreApi.Secrets.Service;

namespace KeyStoreApi.Secrets.Delete;

public sealed class DeleteSecretEndpoint : Endpoint<KeyRequest> {
    private readonly ErrorResponseFactory _error;
    private readonly SecretService _service;

    public DeleteSecretEndpoint(SecretService service, ErrorResponseFactory error) {
        _service = service;
        _error = error;
    }

    public override void Configure() {
        Delete("/secrets/{Key}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(KeyRequest req, CancellationToken ct) {
        using var scope = Logger.BeginScope(req);
        Logger.LogInformation("Remove secret requested");
        var result = await _service.RemoveSecret(req.Key, ct);
        
        switch (result.Result) {
            case SecretResult.Success:
                Logger.LogInformation("Remove secret success");
                await SendAsync(null, 204, CancellationToken.None);
                break;
            case SecretResult.Cancelled:
                Logger.LogInformation("Remove secret cancelled");
                await SendResultAsync(_error.Build(result, 503));
                break;
            default:
                Logger.LogError(result.Exception, "Remove secret errored");
                await SendResultAsync(_error.Build(result));
                break;
        }
        
        Logger.LogInformation("Delete secret response sent");
    }
}