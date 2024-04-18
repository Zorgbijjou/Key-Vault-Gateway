using FastEndpoints;
using KeyStoreApi.Secrets.Service;

namespace KeyStoreApi.Secrets.Post;

public sealed class UpdateSecretEndpoint : Endpoint<SecretKeyValuePairRequest, SecretResponse> {
    private readonly static SecretRequestBody AnonymizedBody = new() {
        Secret = "***"
    };
    
    private readonly ErrorResponseFactory _error;
    private readonly SecretService _service;

    public UpdateSecretEndpoint(SecretService service, ErrorResponseFactory error) {
        _service = service;
        _error = error;
    }

    public override void Configure() {
        Post("/secrets/{Key}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(SecretKeyValuePairRequest req, CancellationToken ct) {
        Logger.BeginScope(req with {
            RequestBody = AnonymizedBody
        });
        Logger.LogInformation("Update secret requested");
        var result = await _service.SetSecret(req.Key, req.RequestBody.Secret, ct);

        switch (result.Result) {
            case SecretResult.Success:
                Logger.LogInformation("Update secret success");
                await SendAsync(new SecretResponse {
                    Secret = req.RequestBody.Secret
                }, (int)result.Result,
                CancellationToken.None);
                break;
            case SecretResult.Cancelled:
                Logger.LogInformation("Update secret cancelled");
                await SendResultAsync(_error.Build(result, 503));
                break;
            default:
                Logger.LogError(result.Exception, "Update secret errored");
                await SendResultAsync(_error.Build(result));
                break;
        }
        
        Logger.LogInformation("Update secret requested sent");
    }
}