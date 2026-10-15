using Godot;

namespace TamQuoc3D.Core.Constants;

/// <summary>
/// Cac hang so tro choi toan cuc.
/// </summary>
public static class GameConstants
{
    // Screen resolution
    public const int ViewportWidth = 1280;
    public const int ViewportHeight = 720;

    // Formation & Grid 3x3
    public const int GridRows = 3; // 0 = Tien quan, 1 = Trung quan, 2 = Hau quan
    public const int GridCols = 3; // 0 = Tren, 1 = Giua, 2 = Duoi
    public const int MaxTeamSize = 5;

    // Morale & Rage
    public const int MaxMorale = 100;
    public const int DefaultStartingMorale = 50;
    public const int MoraleGainOnAttack = 25;
    public const int MoraleGainOnHurt = 15;

    // Visual Standards
    public const float HealthBarWidth = 90.0f;
    public const float HealthBarHeight = 4.0f;
    public const int HealthBarCornerRadius = 2;

    // Pacing & Animation Timings
    public const float DefaultBattleSpeed = 1.0f;
    public const float AttackDashDuration = 0.28f;
    public const float AttackHoldDuration = 0.24f;
    public const float AttackBackDuration = 0.28f;
    public const float SkillBannerInDuration = 0.25f;
    public const float SkillBannerStayDuration = 0.70f;
    public const float SkillBannerOutDuration = 0.25f;
}
