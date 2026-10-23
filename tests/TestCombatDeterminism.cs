using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using TamQuoc3D.Data.Database;
using TamQuoc3D.Features.Battle.Controllers;

namespace TamQuoc3D.Tests;

/// <summary>
/// TestCombatDeterminism (Phase 5) - Cung seed + cung doi hinh thi hai tran dau
/// phai cho chuoi sat thuong giong het nhau; seed khac thi ket qua phai khac.
/// Day la nen tang cho replay + mo phong tran phia server sau nay.
/// </summary>
public partial class TestCombatDeterminism : Node
{
    public override void _Ready()
    {
        RunTest();
    }

    private async void RunTest()
    {
        GD.Print("\n=== 🎲 STARTING COMBAT DETERMINISM TEST ===");
        try
        {
            var (winnerA, logA) = await RunBattle(12345);
            var (winnerB, logB) = await RunBattle(12345);
            var (winnerC, logC) = await RunBattle(98765);

            Assert(logA.Count >= 4, $"Need enough combat events to compare, got {logA.Count}");
            Assert(winnerA == winnerB, $"Same seed must give same winner ({winnerA} vs {winnerB})");
            Assert(SequencesEqual(logA, logB), "Same seed must give identical damage sequences");
            Assert(!SequencesEqual(logA, logC) || winnerA != winnerC,
                "Different seed should produce a different battle");

            GD.Print($"🎉 COMBAT DETERMINISM TEST PASSED (events: {logA.Count}, winner: team {winnerA})");
            GetTree().Quit(0);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"❌ COMBAT DETERMINISM TEST FAILED: {ex.Message}");
            GetTree().Quit(1);
        }
    }

    private async Task<(int Winner, List<string> DamageLog)> RunBattle(int seed)
    {
        var db = new HeroDatabase();
        db.GenerateDefaultHeroDatabase();

        var manager = new BattleManager();
        AddChild(manager);
        manager.SeedOverride = seed;
        manager.SetupBattle(
            [db.GetHero("trieu_van"), db.GetHero("quan_vu")],
            [db.GetHero("dien_vi"), db.GetHero("tao_nhan")]);

        foreach (var team in new[] { manager.Team0, manager.Team1 })
        {
            foreach (var h in team)
            {
                h.Stats.HpMax = 250;
                h.Stats.HpCurrent = 250;
                h.Stats.AtkPhys = 220;
                h.Stats.Speed = 110;
            }
        }

        var damageLog = new List<string>();
        manager.LogMessage += msg =>
        {
            if (msg.Contains("gây")) damageLog.Add(msg);
        };
        int winner = -1;
        manager.BattleEnded += w => winner = w;

        manager.StartBattle();
        int waited = 0;
        while (winner < 0 && waited < 60_000)
        {
            await ToSignal(GetTree().CreateTimer(0.25), SceneTreeTimer.SignalName.Timeout);
            waited += 250;
        }

        Assert(winner >= 0, "Battle must finish");
        RemoveChild(manager);
        manager.QueueFree();
        return (winner, damageLog);
    }

    private static bool SequencesEqual(List<string> a, List<string> b)
    {
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
        {
            if (a[i] != b[i]) return false;
        }
        return true;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("ASSERT: " + message);
    }
}
