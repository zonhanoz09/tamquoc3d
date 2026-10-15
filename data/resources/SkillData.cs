using Godot;
using TamQuoc3D.Core.Interfaces;

namespace TamQuoc3D.Data.Resources;

/// <summary>
/// SkillData - Dinh nghia thong tin ky nang duoi dang Custom Resource (.tres).
/// </summary>
[GlobalClass]
public partial class SkillData : Resource, ISkill
{
    public enum SkillType
    {
        Melee,      // Can chien
        Ranged,     // Tam xa (cung / phep)
        AoeAll,     // Toan the
        AoeRow,     // Theo hang
        AoeColumn,  // Theo cot
        Flank,      // Danh moc / danh tuong it mau nhat
        Buff,       // Ho tro / tang chi so
        Heal        // Hoi mau
    }

    public enum EffectType
    {
        None,
        Stun,
        Silence,
        Burn,
        Poison,
        Freeze,
        BuffAtk,
        BuffDef,
        BuffSpeed,
        DebuffAtk,
        DebuffDef,
        Shield
    }

    [Export] public string SkillId { get; set; } = string.Empty;
    [Export] public string SkillName { get; set; } = string.Empty;
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = string.Empty;
    [Export] public SkillType Type { get; set; } = SkillType.Melee;
    [Export] public int MoraleCost { get; set; } = 0; // 0 = danh thuong, 100 = No chieu
    [Export] public float DamageMultiplier { get; set; } = 1.0f;
    [Export] public bool IsMagic { get; set; } = false;
    [Export] public int TargetCount { get; set; } = 1;

    [Export] public Godot.Collections.Array<Godot.Collections.Dictionary> Effects { get; set; } = [];

    public bool IsRageSkill => MoraleCost >= 100;
}
