namespace SportBook.Application.Exceptions;

/// <summary>
/// A domain-level failure that should reach the client as a specific HTTP status and
/// `{ error: { code, message } }` body (contracts/api.md error shape), rather than a generic 500.
/// </summary>
public class ApiException(int statusCode, string code, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;

    public string Code { get; } = code;
}
