using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Npgsql;

namespace TamQuoc3D.Server.Authentication;

public sealed class SessionAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    NpgsqlDataSource dataSource)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var token = authorization["Bearer ".Length..].Trim();
        if (token.Length == 0)
        {
            return AuthenticateResult.Fail("Bearer token is missing.");
        }

        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
        await using var command = dataSource.CreateCommand("""
            SELECT s.user_id
            FROM user_sessions s
            JOIN users u ON u.id = s.user_id
            WHERE s.token_hash = @token_hash
              AND s.expires_at > NOW()
              AND u.status = 'ACTIVE';
            """);
        command.Parameters.AddWithValue("token_hash", tokenHash);

        var userId = await command.ExecuteScalarAsync(Context.RequestAborted);
        if (userId is not Guid id)
        {
            return AuthenticateResult.Fail("Session is invalid or expired.");
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, id.ToString())],
            Scheme.Name);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate = "Bearer";
        return base.HandleChallengeAsync(properties);
    }
}
