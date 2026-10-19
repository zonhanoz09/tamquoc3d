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
    public Godot.Collections.Array<FormationEntry> CurrentFormation { get; set; } = [];
    public Godot.Collections.Array<FormationPreset> SavedPresets { get; set; } = [];
    public string BattleContext { get; set; } = "campaign";
    public string PreviousScenePath { get; set; } = string.Empty;

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

    public void InitializeDefaultData()
    {
        Profile = PlayerProfile.Load();

        // Load all heroes into OwnedHeroes if empty
        if (OwnedHeroes.Count == 0 && HeroDatabase.Instance != null)
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
