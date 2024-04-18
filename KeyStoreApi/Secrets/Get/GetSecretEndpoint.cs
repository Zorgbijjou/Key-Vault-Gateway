using FastEndpoints;
using KeyStoreApi.Secrets.Service;

namespace KeyStoreApi.Secrets.Get;

public sealed class GetSecretEndpoint : Endpoint<KeyRequest, SecretResponse> {
    private readonly ErrorResponseFactory _error;
    private readonly SecretService _service;

    public GetSecretEndpoint(SecretService service, ErrorResponseFactory error) {
        _service = service;
        _error = error;
    }

    public override void Configure() {
        Get("/secrets/{Key}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(KeyRequest req, CancellationToken ct) {
        using var scope = Logger.BeginScope(req);
        Logger.LogInformation("Get Secret Requested");
        var result = await _service.GetSecret(req.Key, ct);

        switch (result.Result) {
            case SecretResult.Success:
                Logger.LogInformation("Secret get success");
                await SendAsync(new SecretResponse {
                    Secret = result.Value!
                }, (int)result.Result,
                CancellationToken.None);
                break;
            case SecretResult.Cancelled:
                Logger.LogInformation("Secret get cancelled");
                await SendResultAsync(_error.Build(result, 503));
                break;
            default:
                Logger.LogError(result.Exception, "Get secret errored");
                await SendResultAsync(_error.Build(result));
                break;
        }
        
        Logger.LogInformation("Get secret response sent");
    }
}