using System.Collections.Generic;
using System.Linq;
using Godot;

namespace TamQuoc3D.Battle;

[GlobalClass]
public partial class TurnManager : RefCounted
{
    [Signal]
    public delegate void RoundStartedEventHandler(int roundNumber);

    public int RoundNumber { get; set; } = 0;
    public List<BattleHero> TurnQueue { get; } = new();
    public BattleHero? CurrentHero { get; private set; }
    public List<BattleHero> AllHeroes { get; } = new();

    public void Setup(IEnumerable<BattleHero> team0, IEnumerable<BattleHero> team1)
    {
        AllHeroes.Clear();
        AllHeroes.AddRange(team0);
        AllHeroes.AddRange(team1);
        RoundNumber = 0;
        RebuildQueue();
    }

    private void RebuildQueue()
    {
        var alive = AllHeroes.Where(h => h.IsAlive && h.Stats.IsAlive()).ToList();

        int speed0 = alive.Where(h => h.Team == 0).Sum(h => h.Stats.Speed);
        int speed1 = alive.Where(h => h.Team == 1).Sum(h => h.Stats.Speed);

        int priorityTeam = speed0 >= speed1 ? 0 : 1;

        var teamPriority = alive.Where(h => h.Team == priorityTeam).OrderByDescending(h => h.Stats.Speed).ToList();
        var teamOther = alive.Where(h => h.Team != priorityTeam).OrderByDescending(h => h.Stats.Speed).ToList();

        TurnQueue.Clear();
        int i = 0, j = 0;
        while (i < teamPriority.Count || j < teamOther.Count)
        {
            if (i < teamPriority.Count)
            {
                TurnQueue.Add(teamPriority[i]);
                i++;
            }
            if (j < teamOther.Count)
            {
                TurnQueue.Add(teamOther[j]);
                j++;
            }
        }
    }

    public void StartNextRound()
    {
        RoundNumber++;
        RebuildQueue();
        EmitSignal(SignalName.RoundStarted, RoundNumber);
    }

    public BattleHero? GetNextHero()
    {
        while (TurnQueue.Count > 0)
        {
            var hero = TurnQueue[0];
            TurnQueue.RemoveAt(0);
            if (hero.IsAlive && hero.Stats.IsAlive())
            {
                CurrentHero = hero;
                return hero;
            }
        }
        return null;
    }

    public bool IsRoundOver() => TurnQueue.Count == 0;

    public List<BattleHero> PeekNextN(int n)
    {
        var result = new List<BattleHero>();
        int checkedCount = 0;
        foreach (var hero in TurnQueue)
        {
            if (checkedCount >= n) break;
            if (hero.IsAlive && hero.Stats.IsAlive())
            {
                result.Add(hero);
                checkedCount++;
            }
        }
        return result;
    }
}
