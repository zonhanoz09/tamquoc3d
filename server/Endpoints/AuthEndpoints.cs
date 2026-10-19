using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using TamQuoc3D.Server.Contracts;
using TamQuoc3D.Server.Services;

namespace TamQuoc3D.Server.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var auth = endpoints.MapGroup("/api/auth").WithTags("Authentication");
        auth.MapPost("/register", async (
                RegisterRequest request,
                IAuthService service,
                CancellationToken cancellationToken) =>
            {
                var response = await service.RegisterAsync(request, cancellationToken);
                return TypedResults.Created("/api/auth/session", response);
            })
            .WithName("Register")
            .WithSummary("Register a new account and start a session.")
            .Produces<AuthenticationResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        auth.MapPost("/login", async (
                AuthenticationRequest request,
                IAuthService service,
                CancellationToken cancellationToken) =>
                TypedResults.Ok(await service.LoginAsync(request, cancellationToken)))
            .WithName("Login")
            .WithSummary("Authenticate using username and password.")
            .Produces<AuthenticationResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        auth.MapGet("/session", async Task<Results<Ok<SessionResponse>, NotFound>> (
                ClaimsPrincipal principal,
                IAuthService service,
                CancellationToken cancellationToken) =>
            {
                var session = await service.GetSessionAsync(GetUserId(principal), cancellationToken);
                return session is null
                    ? TypedResults.NotFound()
                    : TypedResults.Ok(session);
            })
            .RequireAuthorization()
            .WithName("GetAuthSession")
            .WithSummary("Validate the current bearer session.")
            .Produces<SessionResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        auth.MapPost("/logout", async (
                HttpContext context,
                ClaimsPrincipal principal,
                IAuthService service,
                CancellationToken cancellationToken) =>
            {
                var authorization = context.Request.Headers.Authorization.ToString();
                var token = authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                    ? authorization["Bearer ".Length..].Trim()
                    : string.Empty;
                await service.LogoutAsync(GetUserId(principal), token, cancellationToken);
                return TypedResults.Ok(new { status = "logged_out" });
            })
            .RequireAuthorization()
            .WithName("Logout")
            .WithSummary("Revoke the current bearer session.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return endpoints;
    }

    internal static Guid GetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new InvalidOperationException("Authenticated request has no valid user ID claim.");
    }
}
