using System.Collections.Generic;
using System.Linq;
using Godot;
using TamQuoc3D.Features.Battle.Entities;

namespace TamQuoc3D.Features.Battle.Controllers;

/// <summary>
/// TurnManager - Quan ly toc do, luot di va hang doi danh xen ke giua 2 phe.
/// </summary>
public class TurnManager
{

    /// <summary>RNG co seed do BattleManager tiem vao (Phase 5 - deterministic combat).</summary>
    public System.Random Rng { get; set; }

    public List<BattleHero> Team0 { get; private set; } = [];
    public List<BattleHero> Team1 { get; private set; } = [];

    public int CurrentRound { get; private set; } = 0;
    public List<BattleHero> TurnQueue { get; private set; } = [];
    public int CurrentTurnIndex { get; private set; } = 0;

    public void Setup(List<BattleHero> t0, List<BattleHero> t1)
    {
        Team0 = t0;
        Team1 = t1;
        CurrentRound = 0;
        CurrentTurnIndex = 0;
        TurnQueue.Clear();
    }

    public int GetTeamTotalSpeed(List<BattleHero> team)
    {
        return team.Where(h => h.IsAlive).Sum(h => h.Stats.Speed);
    }

    public int GetPriorityTeam()
    {
        int spd0 = GetTeamTotalSpeed(Team0);
        int spd1 = GetTeamTotalSpeed(Team1);
        if (spd0 > spd1) return 0;
        if (spd1 > spd0) return 1;
        return (Rng != null ? (float)Rng.NextDouble() : GD.Randf()) < 0.5f ? 0 : 1;
    }

    public void BuildRoundQueue()
    {
        CurrentRound++;
        CurrentTurnIndex = 0;
        TurnQueue.Clear();

        int firstTeam = GetPriorityTeam();
        var teamA = (firstTeam == 0 ? Team0 : Team1).Where(h => h.IsAlive).OrderByDescending(h => h.Stats.Speed).ToList();
        var teamB = (firstTeam == 0 ? Team1 : Team0).Where(h => h.IsAlive).OrderByDescending(h => h.Stats.Speed).ToList();

        int maxLen = Mathf.Max(teamA.Count, teamB.Count);
        for (int i = 0; i < maxLen; i++)
        {
            if (i < teamA.Count) TurnQueue.Add(teamA[i]);
            if (i < teamB.Count) TurnQueue.Add(teamB[i]);
        }
    }

    public BattleHero GetNextHero()
    {
        while (CurrentTurnIndex < TurnQueue.Count)
        {
            var hero = TurnQueue[CurrentTurnIndex];
            CurrentTurnIndex++;
            if (hero.IsAlive)
            {
                return hero;
            }
        }
        return null;
    }

    public bool IsRoundFinished()
    {
        return CurrentTurnIndex >= TurnQueue.Count;
    }
}
