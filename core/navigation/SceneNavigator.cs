using System.Collections.Generic;
using Godot;
using TamQuoc3D.Core.Autoload;
using TamQuoc3D.Data.Resources;

namespace TamQuoc3D.Core.Navigation;

/// <summary>
/// SceneNavigator - Quan ly dieu huong chuyen doi man hinh giua DeckBuilder, BattleScene, Hub, Campaign.
/// </summary>
public static class SceneNavigator
{
    public const string BattleScenePath = "res://features/battle/BattleScene.tscn";
    public const string DeckBuilderScenePath = "res://features/deck_builder/DeckBuilderScene.tscn";

    public static void GoToBattle(IEnumerable<FormationEntry> formation = null, object config = null)
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        if (tree == null) return;

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

        tree.ChangeSceneToFile(BattleScenePath);
    }

    public static void GoToFormationSelector(string context = "campaign")
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        if (tree == null) return;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.PreviousScenePath = tree.CurrentScene?.SceneFilePath ?? BattleScenePath;
            GameManager.Instance.BattleContext = context;
            GameManager.Instance.CurrentState = GameManager.GameState.DeckBuilder;
        }

        tree.ChangeSceneToFile(DeckBuilderScenePath);
    }

    public static void GoBack()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        if (tree == null) return;

        string target = GameManager.Instance?.PreviousScenePath;
        if (string.IsNullOrEmpty(target) || target == tree.CurrentScene?.SceneFilePath)
        {
            target = BattleScenePath;
        }

        tree.ChangeSceneToFile(target);
    }
}
