using Godot;

namespace TamQuoc3D.Hero;

[GlobalClass]
public partial class SkillData : Resource
{
    public enum SkillType
    {
        Melee,
        Ranged,
        Magic,
        Flank,
        Pierce,
        AoeRow,
        AoeFull,
        Support,
    }

    public enum EffectType
    {
        None,
        Stun,
        Silence,
        DrainMorale,
        GrantMorale,
        Heal,
        BuffAtk,
        BuffDef,
        BuffSpeed,
        DebuffAtk,
        DebuffDef,
    }

    [Export] public string SkillName { get; set; } = "Unnamed Skill";
    [Export] public string Description { get; set; } = "";
    [Export] public SkillType Type { get; set; } = SkillType.Melee;
    [Export] public int MoraleCost { get; set; } = 100;
    [Export] public float DamageMultiplier { get; set; } = 1.0f;
    [Export] public bool IsMagicDamage { get; set; } = false;
    [Export] public string AnimationKey { get; set; } = "";

    public Godot.Collections.Array<Godot.Collections.Dictionary> Effects { get; set; } = new();
}
