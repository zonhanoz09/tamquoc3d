using System.ComponentModel.DataAnnotations;

namespace TamQuoc3D.Server.Contracts;

/// <summary>Credentials supplied when logging in or creating an account.</summary>
public sealed record AuthenticationRequest
{
    [Required, StringLength(32, MinimumLength = 3), RegularExpression("^[A-Za-z0-9_.-]+$")]
    public required string Username { get; init; }

    [Required, StringLength(128, MinimumLength = 10)]
    public required string Password { get; init; }
}

/// <summary>Credentials and display name supplied when creating an account.</summary>
public sealed record RegisterRequest
{
    [Required, StringLength(32, MinimumLength = 3), RegularExpression("^[A-Za-z0-9_.-]+$")]
    public required string Username { get; init; }

    [Required, StringLength(128, MinimumLength = 10)]
    public required string Password { get; init; }

    [Required, StringLength(50, MinimumLength = 1)]
    public required string DisplayName { get; init; }
}

/// <summary>Authentication result containing an opaque revocable bearer token.</summary>
public sealed record AuthenticationResponse(
    string UserId,
    string Username,
    string DisplayName,
    string AccessToken,
    string TokenType,
    long ExpiresIn);

/// <summary>Authenticated account identity returned during saved-session validation.</summary>
public sealed record SessionResponse(string UserId, string Username, string DisplayName);

/// <summary>Formation layout saved for a player.</summary>
public sealed record FormationUpdateRequest
{
    public string FormationType { get; init; } = "PVE_DEFAULT";
    public required Dictionary<string, List<string?>> GridLayout { get; init; }
}

/// <summary>Requested gacha banner and number of pulls.</summary>
public sealed record GachaPullRequest
{
    public string BannerId { get; init; } = "banner_lu_bo_limited";
    [Range(1, 10)]
    public int PullCount { get; init; } = 1;
}

/// <summary>Battle simulation mode and optional defender account.</summary>
public sealed record BattleSimulationRequest
{
    public string BattleType { get; init; } = "ASYNC_ARENA";
    public string? DefenderId { get; init; } = "b1ffcd88-8b1c-3df7-aa5c-5aa8ac270b22";
}

/// <summary>
/// Campaign completion submitted by the game client. Rewards and stamina cost are
/// deliberately NOT part of this contract: the server derives them from the
/// stage_rewards table so a tampered client cannot mint currency.
/// (Stars/turns are still client-reported until deterministic server-side
/// battle simulation lands - see Phase 5 of the project plan.)
/// </summary>
public sealed record CampaignClearRequest
{
    public string StageId { get; init; } = "1-1";
    [Range(0, 3)]
    public int Stars { get; init; } = 3;
    [Range(1, int.MaxValue)]
    public int Turns { get; init; } = 4;
}

/// <summary>Database and Redis connectivity returned by the health endpoint.</summary>
public sealed record HealthResponse(
    string Status,
    bool DatabaseConnected,
    bool RedisConnected,
    string GameVersion,
    double Timestamp);
