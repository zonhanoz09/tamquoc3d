using Godot;

namespace TamQuoc3D.Core.Autoload;

/// <summary>
/// EventBus - Kenh su kien trung gian toan cuc giup giao tiep giua logic, UI va hieu ung khong bi phu thuoc chat.
/// </summary>
[GlobalClass]
public partial class EventBus : Node
{
    public static EventBus Instance { get; private set; }

    [Signal] public delegate void BattleStartedEventHandler();
    [Signal] public delegate void BattleEndedEventHandler(int winnerTeam);
    [Signal] public delegate void RoundStartedEventHandler(int roundNumber);
    [Signal] public delegate void HeroTurnStartedEventHandler(GodotObject hero);
    [Signal] public delegate void HeroTurnEndedEventHandler(GodotObject hero);
    [Signal] public delegate void HeroActedEventHandler(GodotObject hero, Godot.Collections.Dictionary actionData);
    [Signal] public delegate void HeroDiedEventHandler(GodotObject hero);
    [Signal] public delegate void DamageDealtEventHandler(GodotObject target, int amount, bool isCrit);
    [Signal] public delegate void MoraleChangedEventHandler(GodotObject hero, int newMorale);
    [Signal] public delegate void LogMessageEventHandler(string message);

    // Formation Signals
    [Signal] public delegate void HeroAssignedToSlotEventHandler(string heroId, int row, int col);
    [Signal] public delegate void HeroRemovedFromSlotEventHandler(int row, int col);
    [Signal] public delegate void FormationConfirmedEventHandler(Godot.Collections.Array formation);
    [Signal] public delegate void AutoDeployRequestedEventHandler();
    [Signal] public delegate void FormationPresetSavedEventHandler(int presetIndex, string presetName);
    [Signal] public delegate void FormationPresetLoadedEventHandler(int presetIndex);

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        GD.Print("📡 EventBus khởi động thành công.");
    }
}
