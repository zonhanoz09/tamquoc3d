using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Npgsql;
using TamQuoc3D.Server.Contracts;

namespace TamQuoc3D.Server.Services;

public sealed class AuthService(NpgsqlDataSource dataSource) : IAuthService
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(30);
    private static readonly Regex UsernamePattern = new("^[A-Za-z0-9_.-]{3,32}$", RegexOptions.Compiled);
    private const int PasswordHashIterations = 310_000;

    public async Task<AuthenticationResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        ValidateCredentials(request.Username, request.Password);
        var displayName = request.DisplayName?.Trim() ?? string.Empty;
        if (displayName.Length is < 1 or > 50)
        {
            throw new InvalidGameRequestException("Display name must contain between 1 and 50 characters.");
        }

        var username = request.Username.Trim().ToLowerInvariant();
        var salt = RandomNumberGenerator.GetBytes(16);
        var passwordHash = HashPassword(request.Password, salt);

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            Guid userId;
            await using (var createUser = new NpgsqlCommand(
                "INSERT INTO users (status) VALUES ('ACTIVE') RETURNING id;",
                connection, transaction))
            {
                userId = (Guid)(await createUser.ExecuteScalarAsync(cancellationToken)
                    ?? throw new InvalidOperationException("The database did not return the new user ID."));
            }

            await using (var createProvider = new NpgsqlCommand("""
                INSERT INTO user_auth_providers (user_id, provider, provider_user_id)
                VALUES (@user_id, 'PASSWORD', @username);
                """, connection, transaction))
            {
                createProvider.Parameters.AddWithValue("user_id", userId);
                createProvider.Parameters.AddWithValue("username", username);
                await createProvider.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var createCredentials = new NpgsqlCommand("""
                INSERT INTO user_password_credentials (user_id, username, password_salt, password_hash)
                VALUES (@user_id, @username, @salt, @password_hash);
                """, connection, transaction))
            {
                createCredentials.Parameters.AddWithValue("user_id", userId);
                createCredentials.Parameters.AddWithValue("username", username);
                createCredentials.Parameters.AddWithValue("salt", salt);
                createCredentials.Parameters.AddWithValue("password_hash", passwordHash);
                await createCredentials.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var createProfile = new NpgsqlCommand("""
                INSERT INTO user_profiles (user_id, display_name, avatar_id, level, vip_level)
                VALUES (@user_id, @display_name, 'default_hero', 1, 0);
                """, connection, transaction))
            {
                createProfile.Parameters.AddWithValue("user_id", userId);
                createProfile.Parameters.AddWithValue("display_name", displayName);
                await createProfile.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var createWallet = new NpgsqlCommand("""
                INSERT INTO user_wallets (user_id, silver, gold, stamina, max_stamina)
                VALUES (@user_id, 5000, 1000, 100, 120);
                """, connection, transaction))
            {
                createWallet.Parameters.AddWithValue("user_id", userId);
                await createWallet.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var createHeroes = new NpgsqlCommand("""
                INSERT INTO user_heroes (user_id, hero_id, custom_name, star, level, is_commander)
                VALUES (@user_id, 'default_hero', @display_name, 1, 1, TRUE);
                """, connection, transaction))
            {
                createHeroes.Parameters.AddWithValue("user_id", userId);
                createHeroes.Parameters.AddWithValue("display_name", displayName);
                await createHeroes.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var createFormation = new NpgsqlCommand("""
                INSERT INTO user_formations (user_id, formation_type, grid_layout, is_active)
                VALUES (@user_id, 'PVE_DEFAULT',
                    '{"front":[null,null,null],"mid":[null,"default_hero",null],"back":[null,null,null]}'::jsonb,
                    TRUE);
                """, connection, transaction))
            {
                createFormation.Parameters.AddWithValue("user_id", userId);
                await createFormation.ExecuteNonQueryAsync(cancellationToken);
            }

            var token = await IssueSessionAsync(connection, transaction, userId, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new AuthenticationResponse(
                userId.ToString(), username, displayName, token, "bearer", (long)SessionLifetime.TotalSeconds);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new AccountConflictException("Username is already registered.");
        }
    }

    public async Task<AuthenticationResponse> LoginAsync(
        AuthenticationRequest request,
        CancellationToken cancellationToken)
    {
        ValidateCredentials(request.Username, request.Password);
        var username = request.Username.Trim().ToLowerInvariant();

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        byte[]? salt = null;
        byte[]? storedHash = null;
        Guid userId = Guid.Empty;
        string? status = null;
        string? displayName = null;

        await using (var findAccount = new NpgsqlCommand("""
            SELECT u.id, u.status, c.password_salt, c.password_hash, p.display_name
            FROM user_password_credentials c
            JOIN users u ON u.id = c.user_id
            JOIN user_profiles p ON p.user_id = u.id
            WHERE c.username = @username;
            """, connection, transaction))
        {
            findAccount.Parameters.AddWithValue("username", username);
            await using var reader = await findAccount.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                userId = reader.GetGuid(0);
                status = reader.GetString(1);
                salt = (byte[])reader[2];
                storedHash = (byte[])reader[3];
                displayName = reader.GetString(4);
            }
        }

        var submittedHash = HashPassword(request.Password, salt ?? new byte[16]);
        if (salt is null || storedHash is null || status != "ACTIVE" ||
            !CryptographicOperations.FixedTimeEquals(submittedHash, storedHash))
        {
            throw new InvalidCredentialsException();
        }

        await using (var updateLogin = new NpgsqlCommand(
            "UPDATE user_profiles SET last_login_at = NOW() WHERE user_id = @user_id;",
            connection, transaction))
        {
            updateLogin.Parameters.AddWithValue("user_id", userId);
            await updateLogin.ExecuteNonQueryAsync(cancellationToken);
        }

        var token = await IssueSessionAsync(connection, transaction, userId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new AuthenticationResponse(
            userId.ToString(), username, displayName!, token, "bearer", (long)SessionLifetime.TotalSeconds);
    }

    public async Task<SessionResponse?> GetSessionAsync(Guid userId, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("""
            SELECT c.username, p.display_name
            FROM user_password_credentials c
            JOIN user_profiles p ON p.user_id = c.user_id
            WHERE c.user_id = @user_id;
            """);
        command.Parameters.AddWithValue("user_id", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new SessionResponse(userId.ToString(), reader.GetString(0), reader.GetString(1))
            : null;
    }

    public async Task LogoutAsync(Guid userId, string token, CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(token);
        await using var command = dataSource.CreateCommand("""
            DELETE FROM user_sessions WHERE token_hash = @token_hash AND user_id = @user_id;
            """);
        command.Parameters.AddWithValue("token_hash", tokenHash);
        command.Parameters.AddWithValue("user_id", userId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    internal static byte[] HashPassword(string password, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            PasswordHashIterations,
            HashAlgorithmName.SHA256,
            32);

    internal static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    private static void ValidateCredentials(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || !UsernamePattern.IsMatch(username.Trim()))
        {
            throw new InvalidGameRequestException("Username must be 3-32 letters, numbers, dots, underscores, or hyphens.");
        }
        if (password is null)
        {
            throw new InvalidGameRequestException("Password must contain between 10 and 128 characters.");
        }
    }

    private static async Task<string> IssueSessionAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        await using var command = new NpgsqlCommand("""
            DELETE FROM user_sessions WHERE expires_at <= NOW();
            INSERT INTO user_sessions (token_hash, user_id, expires_at)
            VALUES (@token_hash, @user_id, NOW() + INTERVAL '30 days');
            """, connection, transaction);
        command.Parameters.AddWithValue("token_hash", HashToken(token));
        command.Parameters.AddWithValue("user_id", userId);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return token;
    }
}

public sealed class InvalidCredentialsException() : Exception("Username or password is incorrect.");
public sealed class InvalidGameRequestException(string message) : Exception(message);
