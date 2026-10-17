using System.Collections.Generic;
using Godot;
using TamQuoc3D.Data.Database;
using TamQuoc3D.Data.Resources;

namespace TamQuoc3D.Core.Autoload;

/// <summary>
/// GameManager - Autoload toan cuc quan ly trang thai game, du lieu nguoi choi, chuyen scene.
/// </summary>
[GlobalClass]
public partial class GameManager : Node
{
    [Signal]
    public delegate void SceneChangedEventHandler(string sceneName);

    public enum GameState
    {
        MainMenu,
        Battle,
        Result,
        DeckBuilder,
        Campaign
    }

    public GameState CurrentState { get; set; } = GameState.MainMenu;
    public Godot.Collections.Array<HeroData> PlayerTeam { get; set; } = [];
    public int Gold { get; set; } = 10000;
    public int Gems { get; set; } = 500;
    public int Stamina { get; set; } = 100;
    public const int MaxStamina = 120;

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
        GD.Print("🎮 GameManager khởi động - Project Pixel Tactics (.NET 8)");
        InitializeDefaultData();
    }

    public void InitializeDefaultData()
    {
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

    public void AddGold(int amount) => Gold += amount;

    public bool SpendGold(int amount)
    {
        if (Gold < amount) return false;
        Gold -= amount;
        return true;
    }

    public void AddGems(int amount) => Gems += amount;

    public bool SpendGems(int amount)
    {
        if (Gems < amount) return false;
        Gems -= amount;
        return true;
    }
}
