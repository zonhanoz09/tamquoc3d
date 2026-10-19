using System;
using System.Collections.Generic;
using Godot;
using TamQuoc3D.Core.Autoload;
using TamQuoc3D.Data.Resources;

namespace TamQuoc3D.Core.Navigation;

/// <summary>
/// SceneNavigator - Quản lý điều hướng chuyển đổi màn hình toàn cục: Hub, Formation, Battle, Campaign, Arena, HeroCollection,...
/// Hỗ trợ Navigation Stack (Stack history) và hiệu ứng chuyển cảnh mượt mà (Fade transition).
/// </summary>
public static class SceneNavigator
{
    public const string LoginScenePath = "res://features/auth/scenes/LoginScene.tscn";
    public const string HubScenePath = "res://features/hub/scenes/HubScene.tscn";
    public const string DeckBuilderScenePath = "res://features/deck_builder/scenes/DeckBuilderScene.tscn";
    public const string BattleScenePath = "res://features/battle/scenes/BattleScene.tscn";
    public const string CampaignScenePath = "res://features/campaign/scenes/CampaignScene.tscn";
    public const string ArenaScenePath = "res://features/arena/scenes/ArenaScene.tscn";
    public const string HeroCollectionScenePath = "res://features/hero_collection/scenes/HeroCollectionScene.tscn";
    public const string GuildScenePath = "res://features/guild/scenes/GuildScene.tscn";
    public const string TowerScenePath = "res://features/tower/TowerScene.tscn";
    public const string LabyrinthScenePath = "res://features/labyrinth/LabyrinthScene.tscn";
    public const string ShopScenePath = "res://features/shop/ShopScene.tscn";
    public const string GachaScenePath = "res://features/gacha/GachaScene.tscn";

    private static readonly Stack<string> _sceneHistory = new();
    private static object _currentBattleConfig = null;
    private static bool _isTransitioning = false;

    public static string CurrentScenePath => ((SceneTree)Engine.GetMainLoop())?.CurrentScene?.SceneFilePath ?? string.Empty;

    public static void GoToHub()
    {
        NavigateTo(HubScenePath, addToHistory: true, state: GameManager.GameState.Hub);
    }

    public static void GoToFormationSelector(string context = "hub")
    {
        SetBattleContext(context);
        NavigateTo(DeckBuilderScenePath, addToHistory: true, state: GameManager.GameState.DeckBuilder);
    }

    public static void GoToBattle(IEnumerable<FormationEntry> formation = null, object config = null)
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        if (tree == null) return;

        _currentBattleConfig = config;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.PreviousScenePath = tree.CurrentScene?.SceneFilePath ?? DeckBuilderScenePath;
            GameManager.Instance.CurrentState = GameManager.GameState.Battle;

            if (formation != null)
            {
                GameManager.Instance.CurrentFormation.Clear();
                foreach (var entry in formation)
                {
                    GameManager.Instance.CurrentFormation.Add(entry);
                }
            }
        }

        NavigateTo(BattleScenePath, addToHistory: true, state: GameManager.GameState.Battle);
    }

    public static void GoToCampaign()
    {
        NavigateTo(CampaignScenePath, addToHistory: true, state: GameManager.GameState.Campaign);
    }

    public static void GoToArena()
    {
        NavigateTo(ArenaScenePath, addToHistory: true);
    }

    public static void GoToHeroCollection()
    {
        NavigateTo(HeroCollectionScenePath, addToHistory: true);
    }

    public static void GoToGuild()
    {
        NavigateTo(GuildScenePath, addToHistory: true);
    }

    public static void GoToTower()
    {
        NavigateTo(TowerScenePath, addToHistory: true);
    }

    public static void GoToLabyrinth()
    {
        NavigateTo(LabyrinthScenePath, addToHistory: true);
    }

    public static void GoToShop()
    {
        NavigateTo(ShopScenePath, addToHistory: true);
    }

    public static void GoToGacha()
    {
        NavigateTo(GachaScenePath, addToHistory: true);
    }

    public static void GoBack()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        if (tree == null) return;

        string target = null;
        while (_sceneHistory.Count > 0)
        {
            string candidate = _sceneHistory.Pop();
            if (!string.IsNullOrEmpty(candidate) && candidate != tree.CurrentScene?.SceneFilePath)
            {
                target = candidate;
                break;
            }
        }

        if (string.IsNullOrEmpty(target))
        {
            target = GameManager.Instance?.PreviousScenePath;
        }

        if (string.IsNullOrEmpty(target) || target == tree.CurrentScene?.SceneFilePath)
        {
            target = HubScenePath;
        }

        EventBus.Instance?.EmitSignal(EventBus.SignalName.BackRequested);
        NavigateTo(target, addToHistory: false);
    }

    public static void SetBattleContext(string context)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.BattleContext = context;
        }
    }

    public static object GetCurrentBattleConfig() => _currentBattleConfig;

    public static void ClearHistory()
    {
        _sceneHistory.Clear();
    }

    private static void NavigateTo(string scenePath, bool addToHistory = true, GameManager.GameState? state = null)
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        if (tree == null) return;

        if (_isTransitioning) return;

        string current = tree.CurrentScene?.SceneFilePath;
        if (addToHistory && !string.IsNullOrEmpty(current) && current != scenePath)
        {
            _sceneHistory.Push(current);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PreviousScenePath = current;
            }
        }

        if (state.HasValue && GameManager.Instance != null)
        {
            GameManager.Instance.CurrentState = state.Value;
        }

        EventBus.Instance?.EmitSignal(EventBus.SignalName.SceneChangeRequested, scenePath);

        // Transition fade
        PerformFadeTransition(tree, scenePath);
    }

    private static void PerformFadeTransition(SceneTree tree, string scenePath)
    {
        var root = tree.Root;
        if (root == null)
        {
            tree.ChangeSceneToFile(scenePath);
            return;
        }

        // Overlay canvas layer for smooth black fade
        var canvas = new CanvasLayer { Layer = 100 };
        var colorRect = new ColorRect
        {
            Color = new Color(0, 0, 0, 0),
            CustomMinimumSize = new Vector2(1920, 1080),
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        canvas.AddChild(colorRect);
        root.AddChild(canvas);

        _isTransitioning = true;
        var tween = tree.CreateTween();
        if (tween == null)
        {
            canvas.QueueFree();
            _isTransitioning = false;
            tree.ChangeSceneToFile(scenePath);
            return;
        }

        tween.TweenProperty(colorRect, "color:a", 1.0f, 0.12f);
        tween.TweenCallback(Callable.From(() =>
        {
            tree.ChangeSceneToFile(scenePath);
        }));
        tween.TweenInterval(0.04f);
        tween.TweenProperty(colorRect, "color:a", 0.0f, 0.12f);
        tween.TweenCallback(Callable.From(() =>
        {
            canvas.QueueFree();
            _isTransitioning = false;
        }));
    }
}
