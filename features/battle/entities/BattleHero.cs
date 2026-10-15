using System.Collections.Generic;
using Godot;
using TamQuoc3D.Core.Constants;
using TamQuoc3D.Core.Interfaces;
using TamQuoc3D.Data.Resources;

namespace TamQuoc3D.Features.Battle.Entities;

/// <summary>
/// BattleHero - Thuc the chien dau logic, chua chi so, status effect va trang thai luot.
/// </summary>
[GlobalClass]
public partial class BattleHero : RefCounted, IEntity, ITurnReceiver
{
    [Signal]
    public delegate void DiedEventHandler(BattleHero hero);

    public HeroData Data { get; set; }
    public HeroStats Stats { get; set; }
    public int Team { get; set; }     // 0 = Player, 1 = Enemy
    public int SlotRow { get; set; }  // 0 = Front, 1 = Mid, 2 = Back
    public int SlotCol { get; set; }  // 0 = Top, 1 = Center, 2 = Bottom
    public bool IsAlive { get; set; } = true;

    public Node2D Node { get; set; }  // Reference toi HeroBattleNode
    public List<Godot.Collections.Dictionary> StatusEffects { get; set; } = [];

    // IEntity implementation
    public string Id => Data?.HeroId ?? string.Empty;
    public string Name => Data?.HeroName ?? string.Empty;
    public Vector2 Position
    {
        get => Node?.Position ?? Vector2.Zero;
        set { if (Node != null) Node.Position = value; }
    }

    public BattleHero() { }

    public BattleHero(HeroData heroData, int team, int row, int col)
    {
        Data = heroData;
        Team = team;
        SlotRow = row;
        SlotCol = col;
        Stats = heroData.BaseStats?.Clone() ?? new HeroStats();
    }

    public BattlePosition GetPosition()
    {
        return (BattlePosition)SlotRow;
    }

    public BattlePosition PositionType
    {
        get => (BattlePosition)SlotRow;
        set => SlotRow = (int)value;
    }

    public BattleLane Lane
    {
        get => (BattleLane)SlotCol;
        set => SlotCol = (int)value;
    }

    public int GetSpeed() => Stats?.Speed ?? 0;

    public void OnTurnStart()
    {
    }

    public void OnTurnEnd()
    {
    }

    public void CheckDeath()
    {
        if (Stats.HpCurrent <= 0 && IsAlive)
        {
            IsAlive = false;
            EmitSignal(SignalName.Died, this);
            if (Node is HeroBattleNode hNode)
            {
                hNode.AnimateDeath();
            }
        }
    }
}
