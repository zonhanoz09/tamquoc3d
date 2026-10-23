using System;
using System.Threading.Tasks;
using Godot;
using TamQuoc3D.Data.Database;
using TamQuoc3D.Features.Battle.Controllers;
using TamQuoc3D.Features.Battle.Entities;

namespace TamQuoc3D.Tests;

/// <summary>
/// TestManualBattle - Kiem chung che do Manual (Phase 2): BattleManager phai goi
/// PlayerActionProvider khi toi luot phe nguoi choi, dung dung skill + muc tieu
/// duoc chon, va tran dau van ket thuc binh thuong.
/// </summary>
public partial class TestManualBattle : Node
{
    private int _providerCalls = 0;
    private int _winner = -1;

    public override void _Ready()
    {
        RunTest();
    }

    private async void RunTest()
    {
        GD.Print("\n=== 🎯 STARTING MANUAL BATTLE TEST ===");
        try
        {
            var db = new HeroDatabase();
            db.GenerateDefaultHeroDatabase();

            var manager = new BattleManager();
            AddChild(manager);

            manager.SetupBattle(
                [db.GetHero("trieu_van")],
                [db.GetHero("tao_nhan")]);

            // Rut gon tran dau cho test: mau thap, cong cao
            foreach (var team in new[] { manager.Team0, manager.Team1 })
            {
                foreach (var h in team)
                {
                    h.Stats.HpMax = 300;
                    h.Stats.HpCurrent = 300;
                    h.Stats.AtkPhys = 220;
                    h.Stats.Speed = 120;
                }
            }

            manager.ManualMode = true;
            manager.PlayerActionProvider = hero =>
            {
                _providerCalls++;
                GD.Print($"[Test] Provider called for {hero.Data.HeroId} (call #{_providerCalls})");
                return Task.FromResult(new BattleManager.PlayerAction
                {
                    Skill = hero.Data.GetNormalAttack(),
                    PrimaryTarget = manager.Team1[0]
                });
            };
            manager.BattleEnded += w => _winner = w;

            manager.StartBattle();

            // Cho tran ket thuc (fallback delay ~1.2-2s/luot, tran rut gon < 30s)
            int waited = 0;
            while (_winner < 0 && waited < 60_000)
            {
                await ToSignal(GetTree().CreateTimer(0.25), SceneTreeTimer.SignalName.Timeout);
                waited += 250;
            }

            Assert(_winner >= 0, "Battle must finish in Manual mode");
            Assert(_providerCalls > 0, "PlayerActionProvider must be called on player turns");
            Assert(manager.Team1[0].Stats.HpCurrent < 300 || !manager.Team1[0].IsAlive,
                "Chosen target must have taken damage from the manually selected attack");

            GD.Print($"🎉 MANUAL BATTLE TEST PASSED (provider calls: {_providerCalls}, winner: team {_winner})");
            GetTree().Quit(0);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"❌ MANUAL BATTLE TEST FAILED: {ex.Message}");
            GetTree().Quit(1);
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("ASSERT: " + message);
    }
}
