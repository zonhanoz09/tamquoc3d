using Godot;
using TamQuoc3D.Data.Database;
using TamQuoc3D.Data.Resources;

namespace TamQuoc3D.Core.Autoload;

/// <summary>
/// GameManager - Autoload toàn cục quản lý trạng thái game, dữ liệu người chơi, chuyển scene.
/// </summary>
[GlobalClass]
public partial class GameManager : Node
{
    [Signal]
    public delegate void SceneChangedEventHandler(string sceneName);

    public enum GameState
    {
        MainMenu,
        Hub,
        Battle,
        Result,
        DeckBuilder,
        Campaign
    }

    public GameState CurrentState { get; set; } = GameState.Hub;
    public Godot.Collections.Array<HeroData> PlayerTeam { get; set; } = [];
    public PlayerProfile Profile { get; set; }

    public long Gold
    {
        get => Profile?.Gold ?? _legacyGold;
        set
        {
            if (Profile != null) Profile.Gold = value;
            _legacyGold = value;
        }
    }

    public int Gems
    {
        get => Profile?.Diamond ?? _legacyGems;
        set
        {
            if (Profile != null) Profile.Diamond = value;
            _legacyGems = value;
        }
    }

    public int Stamina
    {
        get => Profile?.Stamina ?? _legacyStamina;
        set
        {
            if (Profile != null) Profile.Stamina = value;
            _legacyStamina = value;
        }
    }

    public int MaxStamina => Profile?.MaxStamina ?? 145;

    private long _legacyGold = 2145370;
    private int _legacyGems = 150000;
    private int _legacyStamina = 100;

    public Godot.Collections.Array<HeroData> OwnedHeroes { get; set; } = [];

    /// <summary>Tien trinh nang cap cua mot tuong (dong bo tu server user_heroes).</summary>
    public sealed class HeroProgress
    {
        public int Level { get; set; } = 1;
        public int Star { get; set; } = 1;
        public long Exp { get; set; } = 0;
    }

    /// <summary>Level/sao/EXP cua tung tuong theo hero_id (nguon: server hoac che do offline).</summary>
    public System.Collections.Generic.Dictionary<string, HeroProgress> HeroProgressById { get; } = new();

    public HeroProgress GetHeroProgress(string heroId)
    {
        if (!string.IsNullOrEmpty(heroId) && HeroProgressById.TryGetValue(heroId, out var progress))
        {
            return progress;
        }
        return new HeroProgress();
    }

    /// <summary>EXP moi level: 300 EXP = 1 level (khop server LevelFromExp).</summary>
    public static int ExpPerLevel => 300;

    /// <summary>He so scale chi so theo level + sao: +5%/level, +10%/sao.</summary>
    public static float HeroStatScale(HeroProgress progress)
    {
        if (progress == null) return 1.0f;
        return (1.0f + 0.05f * (progress.Level - 1)) * (1.0f + 0.10f * (progress.Star - 1));
    }
    public Godot.Collections.Array<FormationEntry> CurrentFormation { get; set; } = [];
    public Godot.Collections.Array<FormationPreset> SavedPresets { get; set; } = [];
    public string BattleContext { get; set; } = "campaign";

    /// <summary>Tang thap dang thach dau (BattleContext == "tower"), set boi TowerScene.</summary>
    public int CurrentTowerFloor { get; set; } = 0;

    /// <summary>Tang thap cao nhat da qua (che do offline; online lay tu server).</summary>
    public int TowerHighestFloor { get; set; } = 0;

    /// <summary>Tui do local cho che do offline (online dung user_inventories tren server).</summary>
    public System.Collections.Generic.Dictionary<string, int> LocalInventory { get; } = new();

    public string PreviousScenePath { get; set; } = string.Empty;

    /// <summary>
    /// Ai PvE dang duoc thach dau tu man hinh Campaign (set boi CampaignController).
    /// BattleScene doc CurrentStage de dung doi hinh dich + tinh sao/thuong khi thang.
    /// Null khi khong phai tran chien dich (dau thu / arena).
    /// </summary>
    public StageData CurrentStage { get; set; }

    /// <summary>
    /// So sao tung ai da vuot trong phien choi (key = StageData.StageId dang so).
    /// CampaignController doc ghi de de tien trinh khong mat khi chuyen scene;
    /// khi online, so lieu server (campaign_progress) van la nguon chuan.
    /// </summary>
    public Godot.Collections.Dictionary<int, int> StageStars { get; set; } = [];

    /// <summary>
    /// Ghi nhan ket qua vuot ai (chi giu so sao cao nhat). Tra ve true neu day la
    /// lan dau vuot ai nay trong phien (dung de tinh thuong First Clear khi offline).
    /// </summary>
    public bool RecordStageResult(int stageId, int stars)
    {
        bool firstClear = !StageStars.TryGetValue(stageId, out int previous);
        if (firstClear || stars > previous)
        {
            StageStars[stageId] = stars;
        }
        return firstClear;
    }

    public static GameManager Instance { get; private set; }

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        GD.Print("GameManager khởi động...");
        InitializeDefaultData();
    }

    /// <summary>
    /// True khi dang dung du lieu demo offline (chua co phien dang nhap luu tren may).
    /// Khi da co session, danh sach tuong se do server dong bo - khong seed toan bo
    /// tuong vao OwnedHeroes nua de tranh hien thi sai truoc khi SyncPlayerData chay.
    /// </summary>
    public bool UsesDemoData { get; private set; }

    public void InitializeDefaultData()
    {
        Profile = PlayerProfile.Load();

        UsesDemoData = !FileAccess.FileExists("user://auth_session.json");

        // Load all heroes into OwnedHeroes if empty (chi che do demo offline)
        if (UsesDemoData && OwnedHeroes.Count == 0 && HeroDatabase.Instance != null)
        {
            foreach (var h in HeroDatabase.Instance.GetAllHeroes())
            {
                OwnedHeroes.Add(h);
            }
        }

        // Initialize 3 default preset slots
        if (SavedPresets.Count == 0)
        {
            for (int i = 1; i <= 3; i++)
            {
                SavedPresets.Add(new FormationPreset
                {
                    PresetName = $"Đội hình {i}",
                    Entries = []
                });
            }
        }
    }

    public void ChangeScene(string path)
    {
        PreviousScenePath = GetTree().CurrentScene?.SceneFilePath ?? string.Empty;
        EmitSignal(SignalName.SceneChanged, path);
        GetTree().ChangeSceneToFile(path);
    }

    public void StartBattle(Godot.Collections.Array<HeroData> playerHeroes, Godot.Collections.Array<HeroData> enemyHeroes = null)
    {
        CurrentState = GameState.Battle;
        PlayerTeam = playerHeroes;
    }

    public void AddGold(int amount)
    {
        Gold += amount;
        EventBus.Instance?.EmitSignal(EventBus.SignalName.GoldChanged, amount, (int)Gold);
    }

    public bool SpendGold(int amount)
    {
        if (Gold < amount) return false;
        Gold -= amount;
        EventBus.Instance?.EmitSignal(EventBus.SignalName.GoldChanged, -amount, (int)Gold);
        return true;
    }

    public void AddGems(int amount)
    {
        Gems += amount;
        EventBus.Instance?.EmitSignal(EventBus.SignalName.DiamondChanged, amount, Gems);
    }

    public bool SpendGems(int amount)
    {
        if (Gems < amount) return false;
        Gems -= amount;
        EventBus.Instance?.EmitSignal(EventBus.SignalName.DiamondChanged, -amount, Gems);
        return true;
    }

    public bool ConsumeStamina(int amount)
    {
        if (Stamina < amount) return false;
        Stamina -= amount;
        EventBus.Instance?.EmitSignal(EventBus.SignalName.StaminaChanged, Stamina, MaxStamina);
        return true;
    }

    public void AddStamina(int amount)
    {
        Stamina = Mathf.Min(Stamina + amount, MaxStamina);
        EventBus.Instance?.EmitSignal(EventBus.SignalName.StaminaChanged, Stamina, MaxStamina);
    }
}
