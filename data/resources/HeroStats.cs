using Godot;

namespace TamQuoc3D.Data.Resources;

/// <summary>
/// HeroStats - Quan ly tat ca chi so co ban va chien dau cua tuong.
/// Ke thua Resource de ho tro luu file .tres (Data-Driven).
/// </summary>
[GlobalClass]
public partial class HeroStats : Resource
{
    // Chi so sinh ton
    [Export] public int HpMax { get; set; } = 1000;
    [Export] public int HpCurrent { get; set; } = 1000;

    // Chi so sat thuong & phong thu
    [Export] public int AtkPhys { get; set; } = 100;
    [Export] public int AtkMag { get; set; } = 0;
    [Export] public int DefPhys { get; set; } = 50;
    [Export] public int DefMag { get; set; } = 30;

    // Chi so chien thuat
    [Export] public int Speed { get; set; } = 100;
    [Export] public float CritRate { get; set; } = 0.05f;   // 5%
    [Export] public float CritDmg { get; set; } = 1.50f;    // 150%
    [Export] public float DodgeRate { get; set; } = 0.05f;  // 5%
    [Export] public float BlockRate { get; set; } = 0.05f;  // 5%
    [Export] public float CounterRate { get; set; } = 0.0f; // 0%

    // Si khi / No khi (0 - 100)
    [Export] public int MoraleCurrent { get; set; } = 50;
    [Export] public int MoraleMax { get; set; } = 100;

    public void GainMorale(int amount)
    {
        MoraleCurrent = Mathf.Clamp(MoraleCurrent + amount, 0, MoraleMax);
    }

    public bool CanCastRage() => MoraleCurrent >= MoraleMax;

    public void ConsumeMorale(int amount)
    {
        MoraleCurrent = Mathf.Clamp(MoraleCurrent - amount, 0, MoraleMax);
    }

    public void TakeDamage(int dmg)
    {
        HpCurrent = Mathf.Clamp(HpCurrent - dmg, 0, HpMax);
    }

    public void Heal(int amount)
    {
        HpCurrent = Mathf.Clamp(HpCurrent + amount, 0, HpMax);
    }

    public bool IsAlive() => HpCurrent > 0;

    public HeroStats Clone()
    {
        return new HeroStats
        {
            HpMax = HpMax,
            HpCurrent = HpCurrent,
            AtkPhys = AtkPhys,
            AtkMag = AtkMag,
            DefPhys = DefPhys,
            DefMag = DefMag,
            Speed = Speed,
            CritRate = CritRate,
            CritDmg = CritDmg,
            DodgeRate = DodgeRate,
            BlockRate = BlockRate,
            CounterRate = CounterRate,
            MoraleCurrent = MoraleCurrent,
            MoraleMax = MoraleMax
        };
    }
}
