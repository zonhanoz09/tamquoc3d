using TamQuoc3D.Server.Contracts;

namespace TamQuoc3D.Server.Services;

public interface IAuthService
{
    Task<AuthenticationResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<AuthenticationResponse> LoginAsync(AuthenticationRequest request, CancellationToken cancellationToken);
    Task<SessionResponse?> GetSessionAsync(Guid userId, CancellationToken cancellationToken);
    Task LogoutAsync(Guid userId, string token, CancellationToken cancellationToken);
}

public sealed class AccountConflictException(string message) : Exception(message);
