using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TamQuoc3D.Core.Autoload;
using TamQuoc3D.Core.Navigation;
using TamQuoc3D.Data.Database;
using TamQuoc3D.Data.Resources;
using TamQuoc3D.Features.Battle.Controllers;

namespace TamQuoc3D.Tests;

/// <summary>
/// TestFormationToBattle - End-to-end integration test verifying DeckBuilder -> BattleScene transition with custom formation.
/// </summary>
public partial class TestFormationToBattle : Node
{
    public override void _Ready()
    {
        RunTest();
    }

    private async void RunTest()
    {
        GD.Print("\n=== ⚔️ STARTING END-TO-END FORMATION -> BATTLE TEST ===");

        // 1. Prepare custom player formation
        // Setup: Front = Lu Bo (Dark), Mid Top = Guan Yu, Mid Bot = Ma Sieu, Back Top = Chu Du, Back Bot = Diao Chan
        var customFormation = new List<FormationEntry>
        {
            new("lu_bo", 0, 1),
            new("quan_vu", 1, 0),
            new("ma_sieu", 1, 2),
            new("chu_du", 2, 0),
            new("dieu_thuyen", 2, 2)
        };

        // 2. Set into GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CurrentFormation.Clear();
            foreach (var e in customFormation)
            {
                GameManager.Instance.CurrentFormation.Add(e);
            }
        }

        // 3. Instantiate BattleScene
        var battleScene = new BattleScene();
        AddChild(battleScene);

        // Wait a few frames for battle setup & start
        for (int i = 0; i < 5; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        var battleMgr = battleScene.GetChildren().OfType<BattleManager>().FirstOrDefault()
                     ?? battleScene.GetNodeOrNull<BattleManager>("BattleManager");
        Assert(battleMgr != null, "BattleManager should be initialized in BattleScene");
        Assert(battleMgr.Team0.Count == 5, $"Team0 should have 5 heroes, but got {battleMgr.Team0.Count}");

        // Verify that custom formation heroes were spawned in Team0
        Assert(battleMgr.Team0[0].Data.HeroId == "lu_bo", "Team0[0] should be Lu Bo");
        Assert(battleMgr.Team0[0].SlotRow == 0 && battleMgr.Team0[0].SlotCol == 1, "Lu Bo should be at (0, 1)");

        Assert(battleMgr.Team0[1].Data.HeroId == "quan_vu", "Team0[1] should be Quan Vu");
        Assert(battleMgr.Team0[1].SlotRow == 1 && battleMgr.Team0[1].SlotCol == 0, "Quan Vu should be at (1, 0)");

        Assert(battleMgr.Team0[2].Data.HeroId == "ma_sieu", "Team0[2] should be Ma Sieu");
        Assert(battleMgr.Team0[2].SlotRow == 1 && battleMgr.Team0[2].SlotCol == 2, "Ma Sieu should be at (1, 2)");

        Assert(battleMgr.Team0[3].Data.HeroId == "chu_du", "Team0[3] should be Chu Du");
        Assert(battleMgr.Team0[3].SlotRow == 2 && battleMgr.Team0[3].SlotCol == 0, "Chu Du should be at (2, 0)");

        Assert(battleMgr.Team0[4].Data.HeroId == "dieu_thuyen", "Team0[4] should be Dieu Thuyen");
        Assert(battleMgr.Team0[4].SlotRow == 2 && battleMgr.Team0[4].SlotCol == 2, "Dieu Thuyen should be at (2, 2)");

        GD.Print("✅ All 5 heroes spawned with exact custom formation data!");

        // Wait for 0.5s of battle execution
        await ToSignal(GetTree().CreateTimer(0.5f), SceneTreeTimer.SignalName.Timeout);

        Assert(battleMgr.IsBattleActive, "Battle should be actively running without errors");
        GD.Print("🎉 END-TO-END FORMATION -> BATTLE TEST COMPLETED SUCCESSFULLY!\n");

        battleMgr.IsBattleActive = false;
        GetTree().Quit(0);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            GD.PrintErr($"❌ ASSERTION FAILED: {message}");
            throw new Exception($"Assertion failed: {message}");
        }
    }
}
