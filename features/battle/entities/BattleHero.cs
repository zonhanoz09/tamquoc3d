using System.Collections.Generic;
using Godot;
using TamQuoc3D.Core.Interfaces;
using TamQuoc3D.Data.Resources;

namespace TamQuoc3D.Features.Battle.Entities;

/// <summary>
/// BattleHero - Thuc the dai dien cho tuong trong tran danh.
/// </summary>
[GlobalClass]
public partial class BattleHero : RefCounted, IEntity, ITurnReceiver
{
    [Signal]
    public delegate void DiedEventHandler(BattleHero hero);

    public HeroData Data { get; set; }
    public HeroStats Stats { get; set; }
    public int Team { get; set; } // 0 = Player, 1 = Enemy
    public int SlotRow { get; set; } // 0 = Front, 1 = Mid, 2 = Back
    public int SlotCol { get; set; }
    public bool IsAlive { get; set; } = true;
    public List<Godot.Collections.Dictionary> StatusEffects { get; set; } = [];

    public Node2D Node { get; set; }

    public string Id => Data?.HeroId ?? string.Empty;
    public string Name => Data?.HeroName ?? string.Empty;
    public Vector2 Position
    {
        get => Node?.Position ?? Vector2.Zero;
        set { if (Node != null) Node.Position = value; }
    }

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

    public int GetSpeed() => Stats?.Speed ?? 0;

    public void OnTurnStart()
    {
        // Luot bat dau
    }

    public void OnTurnEnd()
    {
        // Luot ket thuc
    }

    public void CheckDeath()
    {
        if (Stats.HpCurrent <= 0 && IsAlive)
        {
            IsAlive = false;
            EmitSignal(SignalName.Died, this);
        }
    }
}
