using FastEndpoints;
using KeyStoreApi.Secrets.Service;

namespace KeyStoreApi.Secrets.List;

public sealed class ListKeysEndpoint : EndpointWithoutRequest<IEnumerable<string>> {
    private readonly ErrorResponseFactory _error;
    private readonly SecretService _service;

    public ListKeysEndpoint(SecretService service, ErrorResponseFactory error) {
        _service = service;
        _error = error;
    }

    public override void Configure() {
        Get("/secrets");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct) {
        Logger.LogInformation("List secrets requested");
        var result = await _service.ListSecrets(ct);

        switch (result.Result) {
            case SecretResult.Success:
                Logger.LogInformation("List secrets success");
                await SendAsync(result.Value!, (int)result.Result, CancellationToken.None);
                break;
            case SecretResult.Cancelled:
                Logger.LogInformation("List secrets cancelled");
                await SendResultAsync(_error.Build(result, 503));
                break;
            default:
                Logger.LogError(result.Exception, "List secrets errored");
                await SendResultAsync(_error.Build(result));
                break;
        }
        
        Logger.LogInformation("List keys response sent");
    }
}