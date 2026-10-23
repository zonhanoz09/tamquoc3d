using TamQuoc3D.Data.Database;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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

    /// <summary>
    /// Delegate bat dong bo de cho hoat anh chien dau (Attack / Rage / VFX) hoan tat truoc khi chuyen luot.
    /// </summary>
    public System.Func<BattleHero, Godot.Collections.Dictionary, Task> ActionAwaiter { get; set; }

    public void SetupBattle(IList<HeroData> party0, IList<HeroData> party1)
    {
        Team0.Clear();
        Team1.Clear();

        SetupTeam(party0, 0, Team0);
        SetupTeam(party1, 1, Team1);

        TurnManager = new TurnManager();
        TurnManager.Setup(Team0, Team1);
    }

    public void SetupBattleWithFormation(IList<FormationEntry> playerFormation, IList<HeroData> party1)
    {
        Team0.Clear();
        Team1.Clear();

        BuildPlayerTeam(playerFormation);
        SetupTeam(party1, 1, Team1);

        TurnManager = new TurnManager();
        TurnManager.Setup(Team0, Team1);
    }

    /// <summary>
    /// Dung tran PvE theo ai chien dich: doi nguoi choi theo FormationEntry da bo tri,
    /// doi dich theo EnemyEntry cua StageData (dung vi tri Row/Col khai bao trong .tres).
    /// </summary>
    public void SetupBattleWithFormations(IList<FormationEntry> playerFormation, IList<EnemyEntry> enemyFormation)
    {
        Team0.Clear();
        Team1.Clear();

        BuildPlayerTeam(playerFormation);

        foreach (var entry in enemyFormation)
        {
            var heroData = ResolveHero(entry.HeroId);
            if (heroData == null) continue;

            var bh = new BattleHero(heroData, 1,
                Mathf.Clamp(entry.Row, 0, GameConstants.GridRows - 1),
                Mathf.Clamp(entry.Col, 0, GameConstants.GridCols - 1));
            bh.Died += OnHeroDied;
            Team1.Add(bh);
        }

        TurnManager = new TurnManager();
        TurnManager.Setup(Team0, Team1);
    }

    private void BuildPlayerTeam(IList<FormationEntry> playerFormation)
    {
        foreach (var entry in playerFormation)
        {
            var heroData = ResolveHero(entry.HeroId);
            if (heroData == null) continue;

            var bh = new BattleHero(heroData, 0, entry.Row, entry.Col);
            bh.Died += OnHeroDied;
            Team0.Add(bh);
        }

        if (Team0.Count == 0)
        {
            var defaultParty = new List<HeroData>
            {
                ResolveHero("trieu_van"),
                ResolveHero("quan_vu"),
                ResolveHero("ma_sieu"),
                ResolveHero("gia_cat_luong"),
                ResolveHero("tieu_kieu")
            };
            SetupTeam(defaultParty, 0, Team0);
        }
    }

    private static HeroData ResolveHero(string heroId)
    {
        var heroData = HeroDatabase.Instance?.GetHero(heroId);
        if (heroData != null) return heroData;

        var dbNode = ((SceneTree)Engine.GetMainLoop())?.Root.GetNodeOrNull<HeroDatabase>("HeroDatabase");
        return dbNode?.GetHero(heroId);
    }

    private void SetupTeam(IList<HeroData> party, int teamId, List<BattleHero> teamList)
    {
        int midCount = 0;
        int backCount = 0;

        foreach (var data in party)
        {
            int row = (int)data.Position;
            int col;

            if (data.Position == BattlePosition.Front)
            {
                // Luật bất biến: Tiền quân chỉ có duy nhất 1 vị trí ở chính giữa (Lane Center)
                col = (int)BattleLane.Center;
            }
            else if (data.Position == BattlePosition.Mid)
            {
                col = midCount switch
                {
                    0 => (int)BattleLane.Top,
                    1 => (int)BattleLane.Bottom,
                    _ => (int)BattleLane.Center
                };
                midCount++;
            }
            else
            {
                col = backCount switch
                {
                    0 => (int)BattleLane.Top,
                    1 => (int)BattleLane.Bottom,
                    _ => (int)BattleLane.Center
                };
                backCount++;
            }

            var bh = new BattleHero(data, teamId, row, col);
            bh.Died += OnHeroDied;
            teamList.Add(bh);
        }
    }

    public async void StartBattle()
    {
        IsBattleActive = true;
        EmitSignal(SignalName.BattleStarted);
        EventBus.Instance?.EmitSignal(EventBus.SignalName.BattleStarted);
        Log("⚔️ [color=#ffd700][b]TRẬN CHIẾN BẮT ĐẦU![/b][/color] (Đấu Trường Đấu Hạng)");

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
            Log($"\n[color=#ffd700]═══════【 HIỆP {TurnManager.CurrentRound} 】═══════[/color]");

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

                if (IsBattleOver())
                {
                    EndBattle();
                    return;
                }

                // Khoang nghi giua cac luot de nguoi choi de dang quan sat dien bien
                await ToSignal(GetTree().CreateTimer(GameConstants.InterTurnDelay / BattleSpeed), SceneTreeTimer.SignalName.Timeout);
            }

            if (!IsInsideTree() || GetTree() == null) return;
            // Khoang nghi giua cac hiep dau
            await ToSignal(GetTree().CreateTimer(GameConstants.InterRoundDelay / BattleSpeed), SceneTreeTimer.SignalName.Timeout);
        }
    }

    private async Task ExecuteTurn(BattleHero hero)
    {
        if (!hero.IsAlive || IsBattleOver())
        {
            EmitSignal(SignalName.HeroTurnEnded, hero);
            EventBus.Instance?.EmitSignal(EventBus.SignalName.HeroTurnEnded, hero);
            return;
        }

        EmitSignal(SignalName.HeroTurnStarted, hero);
        EventBus.Instance?.EmitSignal(EventBus.SignalName.HeroTurnStarted, hero);

        // Check Stun
        if (HasStatus(hero, SkillData.EffectType.Stun))
        {
            Log($"💫 {FormatHero(hero)} bị [color=#fbbf24]Choáng[/color], mất lượt!");
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
            skill = new SkillData
            {
                SkillId = "basic_attack",
                SkillName = "Tấn công cơ bản",
                Type = SkillData.SkillType.Melee,
                DamageMultiplier = 1.0f,
                TargetCount = 1
            };
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
            Log($"⚡ {FormatHero(hero)} thi triển Tuyệt Kỹ: [color=#fef08a]«{skill.SkillName}»[/color]!");
        }
        else
        {
            Log($"⚔️ {FormatHero(hero)} xuất chiêu tấn công!");
        }

        var results = new Godot.Collections.Array();
        bool isSupport = skill.Type == SkillData.SkillType.Heal || skill.Type == SkillData.SkillType.Buff;

        foreach (var target in targets)
        {
            if (isSupport)
            {
                int heal = CombatFormulas.CalculateHeal(hero, target, skill);
                target.Stats.Heal(heal);
                Log($"  💚 {FormatHero(hero)} hồi phục [color=#4ade80][b]+{heal} HP[/b][/color] cho {FormatHero(target)}");

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

                    string critStr = isCrit ? " [color=#ff2e63][b]★BẠO KÍCH★[/b][/color]" : "";
                    string blockStr = isBlock ? " [color=#38bdf8][b]⛨ĐỠ ĐÒN[/b][/color]" : "";
                    Log($"  💥 {FormatHero(hero)} gây [color=#ff6b6b][b]{dmg}[/b][/color] ST lên {FormatHero(target)}{critStr}{blockStr}");

                    target.CheckDeath();
                }
                else
                {
                    Log($"  💨 {FormatHero(target)} [color=#4ade80][b]NÉ ĐÒN[/b][/color] từ {FormatHero(hero)}!");
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

        // Chờ toàn bộ hoạt ảnh đánh, VFX, floating text hoàn tất
        if (ActionAwaiter != null)
        {
            await ActionAwaiter(hero, actionData);
        }
        else
        {
            float fallbackDelay = (isRage ? 2.0f : 1.2f) / BattleSpeed;
            await ToSignal(GetTree().CreateTimer(fallbackDelay), SceneTreeTimer.SignalName.Timeout);
        }

        DecrementStatus(hero);
        EmitSignal(SignalName.HeroTurnEnded, hero);
        EventBus.Instance?.EmitSignal(EventBus.SignalName.HeroTurnEnded, hero);
    }

    private List<BattleHero> SelectTargets(BattleHero attacker, SkillData skill)
    {
        var enemyTeam = attacker.Team == 0 ? Team1 : Team0;
        var aliveEnemies = enemyTeam.Where(h => h.IsAlive).ToList();
        if (aliveEnemies.Count == 0) return [];

        var allyTeam = attacker.Team == 0 ? Team0 : Team1;
        var aliveAllies = allyTeam.Where(h => h.IsAlive).ToList();
        if (aliveAllies.Count == 0) return [];

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

        // 3. AoE Row (Hang ngang theo vi tri)
        if (skill.Type == SkillData.SkillType.AoeRow)
        {
            var frontRow = aliveEnemies.Where(h => h.PositionType == BattlePosition.Front).ToList();
            if (frontRow.Count > 0) return frontRow;
            var midRow = aliveEnemies.Where(h => h.PositionType == BattlePosition.Mid).ToList();
            if (midRow.Count > 0) return midRow;
            return aliveEnemies.Where(h => h.PositionType == BattlePosition.Back).ToList();
        }

        // 4. Flank (Muc tieu it mau nhat)
        if (skill.Type == SkillData.SkillType.Flank)
        {
            return [aliveEnemies.OrderBy(h => h.Stats.HpCurrent).First()];
        }

        // 5. Normal / Melee / Ranged: uu tien danh Tien quan truoc, roi toi Trung quan, Hau quan
        var targetFront = aliveEnemies.Where(h => h.PositionType == BattlePosition.Front).ToList();
        if (targetFront.Count > 0)
        {
            return [targetFront[GD.RandRange(0, targetFront.Count - 1)]];
        }

        var targetMid = aliveEnemies.Where(h => h.PositionType == BattlePosition.Mid).ToList();
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
        Log($"💀 {FormatHero(hero)} [color=#f43f5e]trọng thương ngã xuống![/color]");
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
        string wName = winner == 0 ? "Phe Ta (Player)" : "Phe Địch (Enemy)";
        string wCol = winner == 0 ? "#38bdf8" : "#f87171";
        Log($"\n[color=#ffd700]🏆 ════ KẾT THÚC CHIẾN ĐẤU ════[/color]\n[color={wCol}][b]{wName}[/b][/color] [color=#ffd700]giành đại thắng oanh liệt![/color]");
        EmitSignal(SignalName.BattleEnded, winner);
        EventBus.Instance?.EmitSignal(EventBus.SignalName.BattleEnded, winner);
    }

    private static string FormatHero(BattleHero hero)
    {
        string col = hero.Team == 0 ? "#38bdf8" : "#f87171";
        return $"[color={col}][b]{hero.Data.HeroName}[/b][/color]";
    }

    private void Log(string text)
    {
        string plain = Regex.Replace(text, @"\[/?[a-zA-Z0-9_]+(=[^\]]*)?\]", "");
        GD.Print(plain);
        EmitSignal(SignalName.LogMessage, text);
        EventBus.Instance?.EmitSignal(EventBus.SignalName.LogMessage, text);
    }
}
