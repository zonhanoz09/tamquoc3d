using System;
using System.Collections.Generic;
using Godot;
using TamQuoc3D.Hero;

namespace TamQuoc3D.Battle;

public static class CombatFormulas
{
    private static readonly Random _rnd = new();

    public static float ElementMultiplier(HeroData.ElementType attackerElement, HeroData.ElementType defenderElement)
    {
        var beats = new Dictionary<HeroData.ElementType, HeroData.ElementType>
        {
            { HeroData.ElementType.Fire,  HeroData.ElementType.Wind },
            { HeroData.ElementType.Wind,  HeroData.ElementType.Water },
            { HeroData.ElementType.Water, HeroData.ElementType.Fire },
            { HeroData.ElementType.Light, HeroData.ElementType.Dark },
            { HeroData.ElementType.Dark,  HeroData.ElementType.Light },
        };

        if (beats.TryGetValue(attackerElement, out var beaten) && beaten == defenderElement)
            return 1.30f;

        if (beats.TryGetValue(defenderElement, out var counter) && counter == attackerElement)
            return 0.80f;

        return 1.0f;
    }

    public static float ElementCritBonus(HeroData.ElementType attackerElement, HeroData.ElementType defenderElement)
    {
        var beats = new Dictionary<HeroData.ElementType, HeroData.ElementType>
        {
            { HeroData.ElementType.Fire,  HeroData.ElementType.Wind },
            { HeroData.ElementType.Wind,  HeroData.ElementType.Water },
            { HeroData.ElementType.Water, HeroData.ElementType.Fire },
            { HeroData.ElementType.Light, HeroData.ElementType.Dark },
            { HeroData.ElementType.Dark,  HeroData.ElementType.Light },
        };

        if (beats.TryGetValue(attackerElement, out var beaten) && beaten == defenderElement)
            return 0.15f;

        return 0.0f;
    }

    public static CombatResult CalcPhysicalDamage(
        HeroStats attacker,
        HeroStats defender,
        HeroData.ElementType attackerElement,
        HeroData.ElementType defenderElement,
        float skillMultiplier = 1.0f)
    {
        var result = new CombatResult();

        // 1. Né tránh
        float effectiveDodge = MathF.Max(0.0f, defender.DodgeRate - attacker.HitRate + 0.9f);
        if (_rnd.NextSingle() < effectiveDodge)
        {
            result.IsDodge = true;
            result.MoraleGainAttacker = 0;
            result.MoraleGainDefender = 0;
            return result;
        }

        // 2. Đỡ đòn (Block)
        float effectiveBlock = MathF.Max(0.0f, defender.BlockRate - attacker.PierceRate);
        if (_rnd.NextSingle() < effectiveBlock)
        {
            result.IsBlock = true;
            result.IsCounter = true;
        }

        // 3. Bạo kích (Crit)
        float effCrit = attacker.CritRate + ElementCritBonus(attackerElement, defenderElement);
        effCrit = MathF.Max(0.0f, effCrit - defender.AntiCrit);
        float critMultiplier = 1.0f;
        if (_rnd.NextSingle() < effCrit)
        {
            result.IsCrit = true;
            critMultiplier = (float)(1.5 + _rnd.NextDouble() * 0.5);
        }

        // 4. Tính sát thương
        float elemMult = ElementMultiplier(attackerElement, defenderElement);
        float baseDmg = attacker.PhysAtk * skillMultiplier * elemMult * critMultiplier;
        float reduced = baseDmg - defender.PhysDef;
        if (result.IsBlock)
            reduced *= 0.5f;

        result.Damage = Math.Max(1, (int)reduced);
        result.MoraleGainAttacker = 25;
        result.MoraleGainDefender = 25;

        return result;
    }

    public static CombatResult CalcMagicDamage(
        HeroStats attacker,
        HeroStats defender,
        HeroData.ElementType attackerElement,
        HeroData.ElementType defenderElement,
        float skillMultiplier = 1.0f)
    {
        var result = new CombatResult
        {
            MoraleGainAttacker = 25,
            MoraleGainDefender = 25
        };

        float effCrit = attacker.CritRate + ElementCritBonus(attackerElement, defenderElement);
        effCrit = MathF.Max(0.0f, effCrit - defender.AntiCrit);
        float critMultiplier = 1.0f;
        if (_rnd.NextSingle() < effCrit)
        {
            result.IsCrit = true;
            critMultiplier = (float)(1.5 + _rnd.NextDouble() * 0.5);
        }

        float elemMult = ElementMultiplier(attackerElement, defenderElement);
        float baseDmg = attacker.MagAtk * skillMultiplier * elemMult * critMultiplier;
        float reduced = baseDmg - defender.MagDef;

        result.Damage = Math.Max(1, (int)reduced);
        return result;
    }

    public static int CalcHeal(HeroStats caster, float skillMultiplier = 1.0f)
    {
        return (int)(caster.MagAtk * skillMultiplier);
    }
}

public class CombatResult
{
    public int Damage { get; set; }
    public bool IsCrit { get; set; }
    public bool IsDodge { get; set; }
    public bool IsBlock { get; set; }
    public bool IsCounter { get; set; }
    public int MoraleGainAttacker { get; set; }
    public int MoraleGainDefender { get; set; }
}
