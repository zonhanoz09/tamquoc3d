using Godot;
using TamQuoc3D.Data.Resources;
using TamQuoc3D.Features.Battle.Entities;

namespace TamQuoc3D.Features.Battle.Controllers;

/// <summary>
/// CombatFormulas - Cong thuc tinh toan sat thuong, khac che nguyen to, bao kich, ne tranh.
/// </summary>
public static class CombatFormulas
{
    public static float GetElementMultiplier(HeroData.ElementType attacker, HeroData.ElementType defender)
    {
        if (attacker == HeroData.ElementType.None || defender == HeroData.ElementType.None)
            return 1.0f;

        // Khac che tam giac tu nhien: Hoa > Phong > Thuy > Hoa
        if (attacker == HeroData.ElementType.Fire && defender == HeroData.ElementType.Wind) return 1.30f;
        if (attacker == HeroData.ElementType.Wind && defender == HeroData.ElementType.Water) return 1.30f;
        if (attacker == HeroData.ElementType.Water && defender == HeroData.ElementType.Fire) return 1.30f;

        // Bi khac nguoc
        if (attacker == HeroData.ElementType.Wind && defender == HeroData.ElementType.Fire) return 0.70f;
        if (attacker == HeroData.ElementType.Water && defender == HeroData.ElementType.Wind) return 0.70f;
        if (attacker == HeroData.ElementType.Fire && defender == HeroData.ElementType.Water) return 0.70f;

        // Khac che luong cuc: Quang <-> Am
        if ((attacker == HeroData.ElementType.Light && defender == HeroData.ElementType.Dark) ||
            (attacker == HeroData.ElementType.Dark && defender == HeroData.ElementType.Light))
        {
            return 1.25f;
        }

        return 1.0f;
    }

    public static (int damage, bool isCrit, bool isDodge, bool isBlock) CalculateDamage(BattleHero attacker, BattleHero defender, SkillData skill)
    {
        // 1. Tinh Ne Tranh (Dodge)
        float dodgeChance = defender.Stats.DodgeRate;
        if (GD.Randf() < dodgeChance)
        {
            return (0, false, true, false);
        }

        // 2. Tinh Sat Thuong Co Ban (Vat ly / Phep thuat)
        float rawAtk = skill.IsMagic ? attacker.Stats.AtkMag : attacker.Stats.AtkPhys;
        float rawDef = skill.IsMagic ? defender.Stats.DefMag : defender.Stats.DefPhys;

        float defFactor = 1000.0f / (1000.0f + rawDef * 3.5f);
        float baseDmg = rawAtk * skill.DamageMultiplier * defFactor;

        // 3. Khac che he nguyen to
        float elemMult = GetElementMultiplier(attacker.Data.Element, defender.Data.Element);
        baseDmg *= elemMult;

        // 4. Do bien thien ngau nhien (Variance 95% - 105%)
        float variance = (float)GD.RandRange(0.95, 1.05);
        baseDmg *= variance;

        // 5. Tinh Bao Kich (Crit)
        bool isCrit = GD.Randf() < attacker.Stats.CritRate;
        if (isCrit)
        {
            baseDmg *= attacker.Stats.CritDmg;
        }

        // 6. Tinh Do Don (Block)
        bool isBlock = false;
        if (GD.Randf() < defender.Stats.BlockRate)
        {
            isBlock = true;
            baseDmg *= 0.5f; // Giam 50% sat thuong
        }

        int finalDmg = Mathf.Max(1, Mathf.RoundToInt(baseDmg));
        return (finalDmg, isCrit, false, isBlock);
    }

    public static int CalculateHeal(BattleHero healer, BattleHero target, SkillData skill)
    {
        float atk = healer.Stats.AtkMag > 0 ? healer.Stats.AtkMag : healer.Stats.AtkPhys;
        float baseHeal = atk * skill.DamageMultiplier * 1.5f;
        float variance = (float)GD.RandRange(0.95, 1.05);
        return Mathf.Max(1, Mathf.RoundToInt(baseHeal * variance));
    }
}
