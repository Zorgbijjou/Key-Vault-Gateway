using System.Net.Mime;
using KeyStoreApi.Shared;
using Microsoft.AspNetCore.Http.HttpResults;

namespace KeyStoreApi.Secrets.Service;

public sealed class ErrorResponseFactory {
    private readonly string _vaultUri;

    public ErrorResponseFactory(string vaultUri) {
        _vaultUri = vaultUri;
    }

    public JsonHttpResult<ErrorResponse> Build<TPayload>(Response<TPayload> response, int? statusCode = null) {
        return TypedResults.Json(new ErrorResponse {
            Title = response.Problem ?? string.Empty,
            Backend = _vaultUri,
            Details = response.ProblemDetails ?? string.Empty,
            Status = (int)response.Result
        },
        contentType: MediaTypeNames.Application.ProblemJson,
        statusCode: statusCode ?? (int)response.Result);
    }
}