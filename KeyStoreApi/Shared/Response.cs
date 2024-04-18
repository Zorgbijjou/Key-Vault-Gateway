using System.Text;
using KeyStoreApi.Secrets.Service;

namespace KeyStoreApi.Shared;

public struct Response<TValue> {
    public TValue? Value { get; private set; }
    public SecretResult Result { get; private set; }
    public Exception? Exception { get; private set; }

    public string? Problem { get; private set; }
    public string? ProblemDetails { get; private set; }

    private Response(TValue? value, SecretResult result, Exception? exception) {
        Value = value;
        Result = result;
        Exception = exception;
    }

    // Success
    public static Response<TValue> Success(TValue value) {
        return new Response<TValue> {
            Value = value,
            Result = SecretResult.Success
        };
    }

    // Failures
    private static Response<TValue> Failure(
            SecretResult result,
            Exception? exception = null,
            string? problem = null,
            string? problemDetails = null
        ) {
        return new Response<TValue> {
            Result = result,
            Exception = exception,
            Problem = problem,
            ProblemDetails = problemDetails
        };
    }

    public static Response<TValue> Error(
            Exception exception,
            string operation
        ) {
        return Failure(SecretResult.Error,
        exception,
        $"{operation} failed unexpectedly",
        $"{exception.Message}\n\n{exception.StackTrace ?? string.Empty}");
    }

    public static Response<TValue> Conflict(
            string? problem = null,
            string? problemDetails = null
        ) {
        return Failure(SecretResult.Conflict, null, problem, problemDetails ?? problem);
    }

    public static Response<TValue> NotFound(
            string? problem = null,
            string? problemDetails = null
        ) {
        return Failure(SecretResult.NotFound, null, problem, problemDetails ?? problem);
    }

    private const string BadRequestProblem = "Bad request";
    private const string BadRequestDescription = "Problem with following arguments:";

    public static Response<TValue> BadRequest(
            List<string> arguments
        ) {
        var stringBuilder = new StringBuilder(BadRequestDescription);
        foreach (var argument in arguments) stringBuilder.AppendLine(argument);
        return Failure(SecretResult.BadRequest, null, BadRequestProblem, stringBuilder.ToString());
    }

    public static Response<TValue> BadRequest(
            string argument
        ) {
        return Failure(
        SecretResult.BadRequest,
        problem: BadRequestProblem,
        problemDetails: $"{BadRequestDescription}\n{argument}");
    }

    public static Response<TValue> Cancel() {
        return Failure(SecretResult.Cancelled);
    }
}