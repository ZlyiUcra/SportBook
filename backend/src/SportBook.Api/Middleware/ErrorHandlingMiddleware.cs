using System.Text.Json;
using SportBook.Application.Exceptions;

namespace SportBook.Api.Middleware;

/// <summary>
/// Translates <see cref="ApiException"/> into the `{ error: { code, message } }` contract shape
/// (contracts/api.md); any other unhandled exception becomes a generic 500 so internals never leak.
/// </summary>
public class ErrorHandlingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ApiException ex)
        {
            context.Response.StatusCode = ex.StatusCode;
            await WriteError(context, ex.Code, ex.Message);
        }
        catch (Exception)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await WriteError(context, "INTERNAL_ERROR", "An unexpected error occurred.");
        }
    }

    private static Task WriteError(HttpContext context, string code, string message)
    {
        context.Response.ContentType = "application/json";
        var payload = new { error = new { code, message } };
        return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
