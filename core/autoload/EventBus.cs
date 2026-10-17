using Godot;

namespace TamQuoc3D.Core.Autoload;

/// <summary>
/// EventBus - Kênh sự kiện trung gian toàn cục giúp giao tiếp giữa logic, UI và hiệu ứng không bị phụ thuộc chặt.
/// </summary>
[GlobalClass]
public partial class EventBus : Node
{
    public static EventBus Instance { get; private set; }

    // Combat Signals
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

    // Player & Profile Signals
    [Signal] public delegate void ProfileLoadedEventHandler();
    [Signal] public delegate void LevelUpEventHandler(int newLevel);
    [Signal] public delegate void VipLevelChangedEventHandler(int vipLevel);
    [Signal] public delegate void TitleChangedEventHandler(string newTitle);

    // Currency Signals
    [Signal] public delegate void GoldChangedEventHandler(int delta, int total);
    [Signal] public delegate void DiamondChangedEventHandler(int delta, int total);
    [Signal] public delegate void StaminaChangedEventHandler(int current, int max);
    [Signal] public delegate void StaminaTimerTickEventHandler(int secondsRemaining);

    // Navigation Signals
    [Signal] public delegate void SceneChangeRequestedEventHandler(string scenePath);
    [Signal] public delegate void BackRequestedEventHandler();

    // Quest Signals
    [Signal] public delegate void QuestProgressUpdatedEventHandler(string questId, int current, int target);
    [Signal] public delegate void QuestCompletedEventHandler(string questId);
    [Signal] public delegate void DailyQuestResetEventHandler();

    // Campaign Signals
    [Signal] public delegate void StageSelectedEventHandler(int chapterId, int stageId);
    [Signal] public delegate void StageStartedEventHandler(int chapterId, int stageId);
    [Signal] public delegate void StageCompletedEventHandler(int chapterId, int stageId, int stars);
    [Signal] public delegate void ChapterUnlockedEventHandler(int chapterId);
    [Signal] public delegate void FirstClearRewardEventHandler(int stageId, int gold, int diamond);

    // Notification & Chat Signals
    [Signal] public delegate void SystemNotificationEventHandler(string title, string body, string type);
    [Signal] public delegate void ChatMessageReceivedEventHandler(string sender, string message, string channel);
    [Signal] public delegate void EventBannerShowEventHandler(string eventId);

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        GD.Print("📡 EventBus khởi động thành công.");
    }
}
