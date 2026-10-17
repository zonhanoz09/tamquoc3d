using Godot;

namespace TamQuoc3D.Data.Resources;

/// <summary>
/// EnemyEntry - Dinh nghia mot tuong dich tren luoi 3x3 cua ai ai.
/// </summary>
[GlobalClass]
public partial class EnemyEntry : Resource
{
    [Export] public string HeroId { get; set; } = string.Empty;
    [Export] public int Row { get; set; } = 0; // 0: Front, 1: Mid, 2: Back
    [Export] public int Col { get; set; } = 0; // 0, 1, 2
    [Export] public int OverrideLevel { get; set; } = 0;
}
