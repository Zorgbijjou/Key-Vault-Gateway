namespace KeyStoreApi.Secrets.Service;

public enum SecretResult {
    Success = 200,
    BadRequest = 400,
    NotFound = 404,
    Conflict = 409,
    Cancelled = 499,
    Error = 500
}