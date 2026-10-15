using Godot;
using TamQuoc3D.Hero;

namespace TamQuoc3D.Autoload;

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
    }

    public GameState CurrentState { get; set; } = GameState.MainMenu;
    public Godot.Collections.Array<HeroData> PlayerTeam { get; set; } = [];
    public int Gold { get; set; } = 0;
    public int Gems { get; set; } = 0;
    public int Stamina { get; set; } = 100;
    public const int MaxStamina = 120;

    public Godot.Collections.Array<HeroData> OwnedHeroes { get; set; } = [];

    public static GameManager Instance { get; private set; }

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        GD.Print("🎮 GameManager khởi động - Project Pixel Tactics (.NET 8)");
    }

    public void ChangeScene(string path)
    {
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
