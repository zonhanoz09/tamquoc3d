using System;
using Godot;

namespace TamQuoc3D.Hero;

[GlobalClass]
public partial class HeroStats : Resource
{
    // Thuộc tính gốc
    [Export] public int HpMax { get; set; } = 1000;
    [Export] public int Strength { get; set; } = 50;
    [Export] public int Agi { get; set; } = 50;
    [Export] public int Intel { get; set; } = 50;
    [Export] public int Speed { get; set; } = 100;

    // Thuộc tính chiến đấu
    [Export] public int PhysAtk { get; set; } = 100;
    [Export] public int PhysDef { get; set; } = 50;
    [Export] public int MagAtk { get; set; } = 80;
    [Export] public int MagDef { get; set; } = 50;

    // Thuộc tính đặc biệt %
    [Export(PropertyHint.Range, "0.0,1.0")] public float CritRate { get; set; } = 0.05f;
    [Export(PropertyHint.Range, "0.0,1.0")] public float AntiCrit { get; set; } = 0.0f;
    [Export(PropertyHint.Range, "0.0,1.0")] public float HitRate { get; set; } = 0.9f;
    [Export(PropertyHint.Range, "0.0,1.0")] public float DodgeRate { get; set; } = 0.05f;
    [Export(PropertyHint.Range, "0.0,1.0")] public float BlockRate { get; set; } = 0.0f;
    [Export(PropertyHint.Range, "0.0,1.0")] public float PierceRate { get; set; } = 0.0f;

    // Sĩ khí
    [Export] public int MoraleInit { get; set; } = 50;

    // Runtime state
    public int HpCurrent { get; set; } = 0;
    public int MoraleCurrent { get; set; } = 0;

    public HeroStats()
    {
        HpCurrent = HpMax;
        MoraleCurrent = MoraleInit;
    }

    public void Reset()
    {
        HpCurrent = HpMax;
        MoraleCurrent = MoraleInit;
    }

    public bool IsAlive() => HpCurrent > 0;

    public int TakeDamage(int amount)
    {
        int actual = Math.Clamp(amount, 0, HpCurrent);
        HpCurrent -= actual;
        return actual;
    }

    public int Heal(int amount)
    {
        int actual = Math.Clamp(amount, 0, HpMax - HpCurrent);
        HpCurrent += actual;
        return actual;
    }

    public void GainMorale(int amount)
    {
        MoraleCurrent = Math.Clamp(MoraleCurrent + amount, 0, 200);
    }

    public bool ConsumeMorale()
    {
        if (MoraleCurrent >= 100)
        {
            MoraleCurrent -= 100;
            return true;
        }
        return false;
    }

    public bool CanRage() => MoraleCurrent >= 100;

    public HeroStats Clone()
    {
        var clone = (HeroStats)Duplicate(true);
        clone.Reset();
        return clone;
    }
}
