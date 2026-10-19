using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using TamQuoc3D.Server.Services;

namespace TamQuoc3D.Server.Middleware;

internal sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            AccountConflictException => (StatusCodes.Status409Conflict, "Conflict", exception.Message),
            InvalidCredentialsException => (StatusCodes.Status401Unauthorized, "Unauthorized", exception.Message),
            InvalidGameRequestException => (StatusCodes.Status400BadRequest, "Bad Request", exception.Message),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Not Found", exception.Message),
            PostgresException postgres when postgres.SqlState == PostgresErrorCodes.UniqueViolation =>
                (StatusCodes.Status409Conflict, "Conflict", "The requested resource already exists."),
            _ => (0, string.Empty, string.Empty)
        };

        if (status == 0)
        {
            logger.LogError(exception, "Unhandled game server request failure.");
            return false;
        }

        logger.LogWarning(exception, "Handled game server request failure: {Title}", title);
        httpContext.Response.StatusCode = status;
        if (status == StatusCodes.Status401Unauthorized)
        {
            httpContext.Response.Headers.WWWAuthenticate = "Bearer";
        }
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        }, cancellationToken);
        return true;
    }
}
