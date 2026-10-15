using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TamQuoc3D.Hero;

namespace TamQuoc3D.Battle;

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
    public List<BattleHero> Team0 { get; } = new();
    public List<BattleHero> Team1 { get; } = new();

    public bool IsBattleActive { get; set; } = false;
    public float BattleSpeed { get; set; } = 1.0f;
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

        Log("⚔️ Trận chiến bắt đầu! (.NET 8 C#)");
    }

    public void StartBattle()
    {
        if (IsBattleActive) return;
        IsBattleActive = true;
        EmitSignal(SignalName.BattleStarted);
        _ = BattleLoop();
    }

    private async Task WaitTimer(double seconds)
    {
        double dur = Math.Max(0.01, seconds / BattleSpeed);
        await ToSignal(GetTree().CreateTimer(dur), SceneTreeTimer.SignalName.Timeout);
    }

    private async Task BattleLoop()
    {
        while (IsBattleActive)
        {
            while (IsPaused)
            {
                await WaitTimer(0.1);
                if (!IsBattleActive) return;
            }

            int roundNum = TurnManager.RoundNumber + 1;
            TurnManager.StartNextRound();
            EmitSignal(SignalName.RoundStarted, roundNum);
            Log($"--- Round {roundNum} ---");

            while (!TurnManager.IsRoundOver() && IsBattleActive)
            {
                while (IsPaused)
                {
                    await WaitTimer(0.1);
                    if (!IsBattleActive) return;
                }

                var hero = TurnManager.GetNextHero();
                if (hero == null || !hero.IsAlive || !hero.Stats.IsAlive())
                    continue;

                hero.TickStatusEffects();

                if (hero.IsStunned())
                {
                    Log($"  🌀 {hero.GetDisplayName()} bị CHOÁNG, bỏ lượt!");
                    if (hero.Node != null && hero.Node.HasMethod("show_floating_text"))
                        hero.Node.Call("show_floating_text", "CHOÁNG!", new Color(1.0f, 0.8f, 0.2f), false);
                    await WaitTimer(0.6);
                    continue;
                }

                EmitSignal(SignalName.HeroTurnStarted, hero);

                await ExecuteHeroTurn(hero);

                EmitSignal(SignalName.HeroTurnEnded, hero);

                int winner = CheckWinner();
                if (winner >= 0)
                {
                    IsBattleActive = false;
                    EmitSignal(SignalName.BattleEnded, winner);
                    Log($"🏆 Đội {(winner == 0 ? "Player" : "Enemy")} CHIẾN THẮNG!");
                    return;
                }
            }

            await WaitTimer(0.55);
        }
    }

    private async Task ExecuteHeroTurn(BattleHero hero)
    {
        SkillData? skillToUse = null;
        bool isRage = false;

        if (!hero.IsSilenced() && hero.Stats.CanRage() && hero.Data.Skills.Count > 0)
        {
            foreach (var sk in hero.Data.Skills)
            {
                if (sk.MoraleCost == 100)
                {
                    skillToUse = sk;
                    isRage = true;
                    break;
                }
            }
        }

        skillToUse ??= GetNormalAttack(hero);

        if (skillToUse == null)
        {
            Log($"  {hero.GetDisplayName()} không có hành động!");
            return;
        }

        var targets = ResolveTargets(hero, skillToUse);
        if (targets.Count == 0)
        {
            Log($"  {hero.GetDisplayName()} không có mục tiêu!");
            return;
        }

        if (isRage)
        {
            hero.Stats.ConsumeMorale();
            Log($"  💥 {hero.GetDisplayName()} thi triển NỘ CHIÊU: {skillToUse.SkillName}!");
        }
        else
        {
            Log($"  ⚔️ {hero.GetDisplayName()} tấn công!");
        }

        var results = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var target in targets)
        {
            var res = CalculateSkillResult(hero, target, skillToUse);
            results.Add(res);
        }

        var targetsArray = new Godot.Collections.Array<BattleHero>();
        foreach (var t in targets) targetsArray.Add(t);

        var actionData = new Godot.Collections.Dictionary
        {
            { "attacker", hero },
            { "skill", skillToUse },
            { "is_rage", isRage },
            { "targets", targetsArray },
            { "results", results }
        };

        EmitSignal(SignalName.HeroActed, hero, actionData);

        double animDur = isRage ? 1.95 : 1.05;
        await WaitTimer(animDur);
    }

    private static SkillData GetNormalAttack(BattleHero hero)
    {
        foreach (var sk in hero.Data.Skills)
        {
            if (sk.MoraleCost == -1)
                return sk;
        }
        return new SkillData
        {
            SkillName = "Đòn Đánh Thường",
            Type = SkillData.SkillType.Melee,
            MoraleCost = -1,
            DamageMultiplier = 1.0f
        };
    }

    private List<BattleHero> ResolveTargets(BattleHero attacker, SkillData skill)
    {
        var enemyTeam = attacker.Team == 0 ? Team1 : Team0;
        var allyTeam = attacker.Team == 0 ? Team0 : Team1;

        if (skill.Type == SkillData.SkillType.Support)
        {
            var aliveAllies = allyTeam.Where(h => h.IsAlive && h.Stats.IsAlive()).ToList();
            if (aliveAllies.Count == 0) return new List<BattleHero>();
            aliveAllies.Sort((a, b) => ((float)a.Stats.HpCurrent / a.Stats.HpMax).CompareTo((float)b.Stats.HpCurrent / b.Stats.HpMax));
            return new List<BattleHero> { aliveAllies[0] };
        }

        var aliveEnemies = enemyTeam.Where(h => h.IsAlive && h.Stats.IsAlive()).ToList();
        if (aliveEnemies.Count == 0) return new List<BattleHero>();

        return skill.Type switch
        {
            SkillData.SkillType.AoeFull => aliveEnemies,
            SkillData.SkillType.AoeRow => GetTargetRow(aliveEnemies),
            SkillData.SkillType.Pierce => GetTargetColumn(aliveEnemies),
            SkillData.SkillType.Flank => GetBackTarget(aliveEnemies),
            _ => new List<BattleHero> { GetFrontTarget(aliveEnemies) }
        };
    }

    private static BattleHero GetFrontTarget(List<BattleHero> enemies)
    {
        return enemies.OrderBy(h => h.SlotRow).ThenBy(h => h.SlotCol).First();
    }

    private static List<BattleHero> GetBackTarget(List<BattleHero> enemies)
    {
        return new List<BattleHero> { enemies.OrderByDescending(h => h.SlotRow).ThenBy(h => h.SlotCol).First() };
    }

    private static List<BattleHero> GetTargetRow(List<BattleHero> enemies)
    {
        for (int r = 0; r <= 2; r++)
        {
            var rowHeroes = enemies.Where(h => h.SlotRow == r).ToList();
            if (rowHeroes.Count > 0) return rowHeroes;
        }
        return enemies;
    }

    private static List<BattleHero> GetTargetColumn(List<BattleHero> enemies)
    {
        for (int c = 0; c <= 2; c++)
        {
            var colHeroes = enemies.Where(h => h.SlotCol == c).ToList();
            if (colHeroes.Count > 0) return colHeroes;
        }
        return enemies;
    }

    private Godot.Collections.Dictionary CalculateSkillResult(BattleHero attacker, BattleHero target, SkillData skill)
    {
        var res = new Godot.Collections.Dictionary
        {
            { "target", target },
            { "is_support", skill.Type == SkillData.SkillType.Support },
            { "heal_amount", 0 },
            { "damage", 0 },
            { "is_crit", false },
            { "is_dodge", false },
            { "is_block", false },
            { "is_counter", false },
            { "is_dead", false }
        };

        if (skill.Type == SkillData.SkillType.Support)
        {
            int healAmt = CombatFormulas.CalcHeal(attacker.Stats, skill.DamageMultiplier);
            target.Stats.Heal(healAmt);
            res["heal_amount"] = healAmt;
            Log($"    💚 {attacker.GetDisplayName()} hồi {healAmt} HP cho {target.GetDisplayName()}");
            return res;
        }

        CombatResult combatRes = skill.IsMagicDamage
            ? CombatFormulas.CalcMagicDamage(attacker.Stats, target.Stats, attacker.GetElement(), target.GetElement(), skill.DamageMultiplier)
            : CombatFormulas.CalcPhysicalDamage(attacker.Stats, target.Stats, attacker.GetElement(), target.GetElement(), skill.DamageMultiplier);

        res["is_dodge"] = combatRes.IsDodge;
        res["is_crit"] = combatRes.IsCrit;
        res["is_block"] = combatRes.IsBlock;
        res["is_counter"] = combatRes.IsCounter;

        if (combatRes.IsDodge)
        {
            Log($"    💨 {target.GetDisplayName()} NÉ TRÁNH thành công!");
            EmitSignal(SignalName.DamageDealt, target, 0, false);
            return res;
        }

        int dmg = combatRes.Damage;
        res["damage"] = dmg;
        target.Stats.TakeDamage(dmg);
        EmitSignal(SignalName.DamageDealt, target, dmg, combatRes.IsCrit);

        string logTxt = $"    💥 {attacker.GetDisplayName()} gây {dmg} sát thương lên {target.GetDisplayName()}";
        if (combatRes.IsCrit) logTxt += " [BẠO KÍCH!]";
        if (combatRes.IsBlock) logTxt += " [ĐỠ ĐÒN]";
        Log(logTxt);

        attacker.Stats.GainMorale(combatRes.MoraleGainAttacker);
        target.Stats.GainMorale(combatRes.MoraleGainDefender);
        EmitSignal(SignalName.MoraleChanged, attacker, attacker.Stats.MoraleCurrent);
        EmitSignal(SignalName.MoraleChanged, target, target.Stats.MoraleCurrent);

        if (combatRes.IsCounter && target.Stats.IsAlive())
        {
            Log($"    🔄 {target.GetDisplayName()} PHẢN CÔNG!");
            var counterRes = CombatFormulas.CalcPhysicalDamage(target.Stats, attacker.Stats, target.GetElement(), attacker.GetElement(), 1.0f);
            if (!counterRes.IsDodge)
            {
                attacker.Stats.TakeDamage(counterRes.Damage);
                EmitSignal(SignalName.DamageDealt, attacker, counterRes.Damage, counterRes.IsCrit);
                if (!attacker.Stats.IsAlive())
                {
                    HandleDeath(attacker);
                }
            }
        }

        var rnd = new Random();
        foreach (var eff in skill.Effects)
        {
            float chance = eff.ContainsKey("chance") ? (float)eff["chance"] : 1.0f;
            if (rnd.NextSingle() < chance)
            {
                var type = (SkillData.EffectType)(int)eff["type"];
                int dur = eff.ContainsKey("duration") ? (int)eff["duration"] : 1;
                int val = eff.ContainsKey("value") ? (int)eff["value"] : 0;
                target.ApplyStatus(type, dur, val);
            }
        }

        if (!target.Stats.IsAlive())
        {
            HandleDeath(target);
            res["is_dead"] = true;
        }

        return res;
    }

    private void HandleDeath(BattleHero hero)
    {
        hero.IsAlive = false;
        hero.EmitSignal(BattleHero.SignalName.Died, hero);
        Log($"    💀 {hero.GetDisplayName()} đã ngã xuống!");
    }

    private void OnHeroDied(BattleHero hero)
    {
        EmitSignal(SignalName.HeroDied, hero);
    }

    private int CheckWinner()
    {
        bool t0Alive = Team0.Any(h => h.IsAlive && h.Stats.IsAlive());
        bool t1Alive = Team1.Any(h => h.IsAlive && h.Stats.IsAlive());

        if (!t1Alive) return 0; // Player win
        if (!t0Alive) return 1; // Enemy win
        return -1;
    }

    public void Log(string msg)
    {
        GD.Print(msg);
        EmitSignal(SignalName.LogMessage, msg);
    }
}
