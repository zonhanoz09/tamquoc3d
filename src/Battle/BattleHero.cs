using System.Collections.Generic;
using Godot;
using TamQuoc3D.Hero;

namespace TamQuoc3D.Battle;

[GlobalClass]
public partial class BattleHero : RefCounted
{
    [Signal]
    public delegate void DiedEventHandler(BattleHero hero);

    public HeroData Data { get; set; }
    public HeroStats Stats { get; set; }
    public int Team { get; set; } // 0 = Player, 1 = Enemy
    public int SlotRow { get; set; } // 0 = Front, 1 = Mid, 2 = Back
    public int SlotCol { get; set; }
    public bool IsAlive { get; set; } = true;
    public List<Godot.Collections.Dictionary> StatusEffects { get; set; } = new();

    public Node2D? Node { get; set; }

    public BattleHero(HeroData heroData, int teamId, int row, int col)
    {
        Data = heroData;
        Team = teamId;
        SlotRow = row;
        SlotCol = col;
        Stats = heroData.BaseStats?.Clone() ?? new HeroStats();
    }

    public HeroData.PositionType GetPosition()
    {
        return SlotRow switch
        {
            0 => HeroData.PositionType.Front,
            1 => HeroData.PositionType.Mid,
            _ => HeroData.PositionType.Back
        };
    }

    public bool HasStatus(SkillData.EffectType effectType)
    {
        foreach (var e in StatusEffects)
        {
            if (e.TryGetValue("type", out var typeVal) && (int)typeVal == (int)effectType)
                return true;
        }
        return false;
    }

    public bool IsStunned() => HasStatus(SkillData.EffectType.Stun);
    public bool IsSilenced() => HasStatus(SkillData.EffectType.Silence);

    public void TickStatusEffects()
    {
        var remaining = new List<Godot.Collections.Dictionary>();
        foreach (var e in StatusEffects)
        {
            int dur = (int)e["duration"] - 1;
            e["duration"] = dur;
            if (dur > 0)
                remaining.Add(e);
        }
        StatusEffects = remaining;
    }

    public void ApplyStatus(SkillData.EffectType effectType, int duration, int value = 0)
    {
        foreach (var e in StatusEffects)
        {
            if (e.TryGetValue("type", out var typeVal) && (int)typeVal == (int)effectType)
            {
                e["duration"] = Mathf.Max((int)e["duration"], duration);
                e["value"] = value;
                return;
            }
        }
        StatusEffects.Add(new Godot.Collections.Dictionary
        {
            { "type", (int)effectType },
            { "duration", duration },
            { "value", value }
        });
    }

    public string GetDisplayName() => Data.HeroName;
    public HeroData.ElementType GetElement() => Data.Element;
}
