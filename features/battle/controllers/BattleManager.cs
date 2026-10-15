using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TamQuoc3D.Core.Autoload;
using TamQuoc3D.Core.Constants;
using TamQuoc3D.Data.Resources;
using TamQuoc3D.Features.Battle.Entities;

namespace TamQuoc3D.Features.Battle.Controllers;

/// <summary>
/// BattleManager - Dieu phoi vong lap tran danh, chon muc tieu, tinh toan sat thuong va phat su kien.
/// </summary>
[GlobalClass]
public partial class BattleManager : Node
{
    [Signal] public delegate void BattleStartedEventHandler();
    [Signal] public delegate void BattleEndedEventHandler(int winnerTeam);
    [Signal] public delegate void RoundStartedEventHandler(int roundNumber);
    [Signal] public delegate void HeroTurnStartedEventHandler(BattleHero hero);
    [Signal] public delegate void HeroTurnEndedEventHandler(BattleHero hero);
    [Signal] public delegate void HeroActedEventHandler(BattleHero hero, Godot.Collections.Dictionary actionData);
    [Signal] public delegate void HeroDiedEventHandler(BattleHero hero);
    [Signal] public delegate void DamageDealtEventHandler(BattleHero target, int amount, bool isCrit);
    [Signal] public delegate void MoraleChangedEventHandler(BattleHero hero, int newValue);
    [Signal] public delegate void LogMessageEventHandler(string text);

    public TurnManager TurnManager { get; private set; } = new();
    public List<BattleHero> Team0 { get; } = [];
    public List<BattleHero> Team1 { get; } = [];

    public bool IsBattleActive { get; set; } = false;
    public float BattleSpeed { get; set; } = GameConstants.DefaultBattleSpeed;
    public bool IsPaused { get; set; } = false;

    public void SetupBattle(IList<HeroData> party0, IList<HeroData> party1)
    {
        Team0.Clear();
        Team1.Clear();

        // Team 0: Player
        for (int i = 0; i < party0.Count; i++)
        {
            var data = party0[i];
            int row = (int)data.Position;
            int col = i % 3;
            var bh = new BattleHero(data, 0, row, col);
            bh.Died += OnHeroDied;
            Team0.Add(bh);
        }

        // Team 1: Enemy
        for (int i = 0; i < party1.Count; i++)
        {
            var data = party1[i];
            int row = (int)data.Position;
            int col = i % 3;
            var bh = new BattleHero(data, 1, row, col);
            bh.Died += OnHeroDied;
            Team1.Add(bh);
        }

        TurnManager = new TurnManager();
        TurnManager.Setup(Team0, Team1);
    }

    public async void StartBattle()
    {
        IsBattleActive = true;
        EmitSignal(SignalName.BattleStarted);
        EventBus.Instance?.EmitSignal(EventBus.SignalName.BattleStarted);
        Log("⚔️ Trận chiến bắt đầu! (Architecture: core/ data/ features/)");

        while (IsBattleActive && IsInsideTree())
        {
            if (IsBattleOver())
            {
                EndBattle();
                return;
            }

            TurnManager.BuildRoundQueue();
            EmitSignal(SignalName.RoundStarted, TurnManager.CurrentRound);
            EventBus.Instance?.EmitSignal(EventBus.SignalName.RoundStarted, TurnManager.CurrentRound);
            Log($"--- Round {TurnManager.CurrentRound} ---");

            while (!TurnManager.IsRoundFinished())
            {
                while (IsPaused)
                {
                    if (!IsInsideTree() || GetTree() == null) return;
                    await ToSignal(GetTree().CreateTimer(0.1f), SceneTreeTimer.SignalName.Timeout);
                }

                if (!IsBattleActive || IsBattleOver() || !IsInsideTree() || GetTree() == null)
                {
                    EndBattle();
                    return;
                }

                var currentHero = TurnManager.GetNextHero();
                if (currentHero == null || !currentHero.IsAlive)
                {
                    continue;
                }

                await ExecuteTurn(currentHero);
                if (!IsInsideTree() || GetTree() == null) return;
                await ToSignal(GetTree().CreateTimer(0.4f / BattleSpeed), SceneTreeTimer.SignalName.Timeout);
            }

            if (!IsInsideTree() || GetTree() == null) return;
            await ToSignal(GetTree().CreateTimer(0.6f / BattleSpeed), SceneTreeTimer.SignalName.Timeout);
        }
    }

    private async Task ExecuteTurn(BattleHero hero)
    {
        EmitSignal(SignalName.HeroTurnStarted, hero);
        EventBus.Instance?.EmitSignal(EventBus.SignalName.HeroTurnStarted, hero);

        // Check Stun
        if (HasStatus(hero, SkillData.EffectType.Stun))
        {
            Log($"💫 {hero.Data.HeroName} bị Choáng, mất lượt!");
            DecrementStatus(hero);
            EmitSignal(SignalName.HeroTurnEnded, hero);
            EventBus.Instance?.EmitSignal(EventBus.SignalName.HeroTurnEnded, hero);
            return;
        }

        // Chọn skill
        bool isRage = hero.Stats.CanCastRage();
        SkillData skill = isRage ? hero.Data.GetRageSkill() : hero.Data.GetNormalAttack();
        if (skill == null)
        {
            skill = hero.Data.GetNormalAttack();
            isRage = false;
        }

        var targets = SelectTargets(hero, skill);
        if (targets.Count == 0)
        {
            EmitSignal(SignalName.HeroTurnEnded, hero);
            EventBus.Instance?.EmitSignal(EventBus.SignalName.HeroTurnEnded, hero);
            return;
        }

        if (isRage)
        {
            hero.Stats.ConsumeMorale(100);
            Log($"⚡ {hero.Data.HeroName} thi triển Tuyệt Kỹ: [{skill.SkillName}]!");
        }
        else
        {
            Log($"⚔️ {hero.Data.HeroName} tấn công!");
        }

        var results = new Godot.Collections.Array();
        bool isSupport = skill.Type == SkillData.SkillType.Heal || skill.Type == SkillData.SkillType.Buff;

        foreach (var target in targets)
        {
            if (isSupport)
            {
                int heal = CombatFormulas.CalculateHeal(hero, target, skill);
                target.Stats.Heal(heal);
                Log($"    💚 {hero.Data.HeroName} hồi {heal} HP cho {target.Data.HeroName}");

                var res = new Godot.Collections.Dictionary
                {
                    { "target", target },
                    { "heal_amount", heal },
                    { "is_support", true }
                };
                results.Add(res);
            }
            else
            {
                var (dmg, isCrit, isDodge, isBlock) = CombatFormulas.CalculateDamage(hero, target, skill);

                if (!isDodge)
                {
                    target.Stats.TakeDamage(dmg);
                    target.Stats.GainMorale(GameConstants.MoraleGainOnHurt);
                    hero.Stats.GainMorale(GameConstants.MoraleGainOnAttack);

                    string critStr = isCrit ? " [BẠO KÍCH!]" : "";
                    string blockStr = isBlock ? " [ĐỠ ĐÒN!]" : "";
                    Log($"    💥 {hero.Data.HeroName} gây {dmg} sát thương lên {target.Data.HeroName}{critStr}{blockStr}");

                    target.CheckDeath();
                }
                else
                {
                    Log($"    💨 {target.Data.HeroName} né đòn tấn công của {hero.Data.HeroName}!");
                }

                var res = new Godot.Collections.Dictionary
                {
                    { "target", target },
                    { "damage", dmg },
                    { "is_crit", isCrit },
                    { "is_dodge", isDodge },
                    { "is_block", isBlock },
                    { "is_support", false }
                };
                results.Add(res);
            }
        }

        var targetArray = new Godot.Collections.Array();
        foreach (var t in targets) targetArray.Add(t);

        var actionData = new Godot.Collections.Dictionary
        {
            { "hero", hero },
            { "skill", skill },
            { "is_rage", isRage },
            { "targets", targetArray },
            { "results", results }
        };

        EmitSignal(SignalName.HeroActed, hero, actionData);
        EventBus.Instance?.EmitSignal(EventBus.SignalName.HeroActed, hero, actionData);

        DecrementStatus(hero);
        EmitSignal(SignalName.HeroTurnEnded, hero);
        EventBus.Instance?.EmitSignal(EventBus.SignalName.HeroTurnEnded, hero);

        await Task.CompletedTask;
    }

    private List<BattleHero> SelectTargets(BattleHero attacker, SkillData skill)
    {
        var enemyTeam = attacker.Team == 0 ? Team1 : Team0;
        var aliveEnemies = enemyTeam.Where(h => h.IsAlive).ToList();
        if (aliveEnemies.Count == 0) return [];

        var allyTeam = attacker.Team == 0 ? Team0 : Team1;
        var aliveAllies = allyTeam.Where(h => h.IsAlive).ToList();

        // 1. Heal / Buff Allies
        if (skill.Type is SkillData.SkillType.Heal or SkillData.SkillType.Buff)
        {
            if (skill.TargetCount > 1)
            {
                return aliveAllies;
            }
            return [aliveAllies.OrderBy(h => (float)h.Stats.HpCurrent / h.Stats.HpMax).First()];
        }

        // 2. AoE All Enemies
        if (skill.Type == SkillData.SkillType.AoeAll)
        {
            return aliveEnemies;
        }

        // 3. AoE Row (Hang ngang)
        if (skill.Type == SkillData.SkillType.AoeRow)
        {
            var frontRow = aliveEnemies.Where(h => h.SlotRow == 0).ToList();
            if (frontRow.Count > 0) return frontRow;
            var midRow = aliveEnemies.Where(h => h.SlotRow == 1).ToList();
            if (midRow.Count > 0) return midRow;
            return aliveEnemies.Where(h => h.SlotRow == 2).ToList();
        }

        // 4. Flank (Muc tieu it mau nhat)
        if (skill.Type == SkillData.SkillType.Flank)
        {
            return [aliveEnemies.OrderBy(h => h.Stats.HpCurrent).First()];
        }

        // 5. Normal / Melee / Ranged: danh tien quan truoc
        var targetFront = aliveEnemies.Where(h => h.SlotRow == 0).ToList();
        if (targetFront.Count > 0)
        {
            return [targetFront[GD.RandRange(0, targetFront.Count - 1)]];
        }

        var targetMid = aliveEnemies.Where(h => h.SlotRow == 1).ToList();
        if (targetMid.Count > 0)
        {
            return [targetMid[GD.RandRange(0, targetMid.Count - 1)]];
        }

        return [aliveEnemies[GD.RandRange(0, aliveEnemies.Count - 1)]];
    }

    private static bool HasStatus(BattleHero hero, SkillData.EffectType effType)
    {
        return hero.StatusEffects.Any(e => (int)e["type"] == (int)effType);
    }

    private static void DecrementStatus(BattleHero hero)
    {
        for (int i = hero.StatusEffects.Count - 1; i >= 0; i--)
        {
            int dur = (int)hero.StatusEffects[i]["duration"] - 1;
            if (dur <= 0)
            {
                hero.StatusEffects.RemoveAt(i);
            }
            else
            {
                hero.StatusEffects[i]["duration"] = dur;
            }
        }
    }

    private void OnHeroDied(BattleHero hero)
    {
        Log($"💀 {hero.Data.HeroName} đã ngã xuống!");
        EmitSignal(SignalName.HeroDied, hero);
        EventBus.Instance?.EmitSignal(EventBus.SignalName.HeroDied, hero);
    }

    private bool IsBattleOver()
    {
        bool team0Alive = Team0.Any(h => h.IsAlive);
        bool team1Alive = Team1.Any(h => h.IsAlive);
        return !team0Alive || !team1Alive;
    }

    private void EndBattle()
    {
        IsBattleActive = false;
        int winner = Team0.Any(h => h.IsAlive) ? 0 : 1;
        Log($"\n🏆 TRẬN ĐẤU KẾT THÚC! Phe {(winner == 0 ? "Player" : "Enemy")} chiến thắng!");
        EmitSignal(SignalName.BattleEnded, winner);
        EventBus.Instance?.EmitSignal(EventBus.SignalName.BattleEnded, winner);
    }

    private void Log(string text)
    {
        GD.Print(text);
        EmitSignal(SignalName.LogMessage, text);
        EventBus.Instance?.EmitSignal(EventBus.SignalName.LogMessage, text);
    }
}
