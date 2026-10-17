---
name: battle-combat-engine
description: Rules, deterministic simulation, and mechanics for the 3x3 turn-based tactical combat engine in Tam Quoc 3D
triggers:
  - battle
  - combat
  - formation
  - simulation
  - rage
  - deterministic
  - skill
---

# 3x3 Turn-Based Tactical Combat Engine Skill

This skill governs the turn-based combat mechanics, 3x3 formation rules, skill execution, and deterministic simulation in **Tam Quoc 3D**.

## 1. 3x3 Grid Formation Rules
Each team has a 3x3 grid consisting of 9 slots:
- **Front Row (`front[0..2]`)**: Tanks, Warriors. Takes damage first; blocks melee direct attacks targeting back rows.
- **Mid Row (`mid[0..2]`)**: Brawlers, Supports, Hybrid Fighters.
- **Back Row (`back[0..2]`)**: Mages, Archers, Strategists, Healers. Protected by front line.

```
       [Enemies]                     [Allies]
Back    Mid    Front           Front    Mid    Back
[0][1][2] [0][1][2] [0][1][2]   vs   [0][1][2] [0][1][2] [0][1][2]
```

## 2. Speed & Initiative Turn Order
- At the start of each round, all living units on both teams are sorted by `Speed` (descending).
- Ties are broken deterministically by `Team (Attacker first)` then `Grid Slot Index`.
- Each unit executes one action turn per round.

## 3. Morale / Rage System
- Maximum Rage: **100**.
- Starting Rage: Usually 50 (can be modified by talents or formations).
- Rage Gain:
  - Normal Attack: +25 Rage.
  - Taking Damage: +10 Rage.
  - Kills: +20 Rage.
- **Ultimate Skill Execution**:
  - When Rage reaches 100, unit immediately casts Ultimate Skill instead of Normal Attack on its turn, consuming 100 Rage.
  - Triggers fullscreen or portrait `SkillCutIn` animation.

## 4. Targeting Logic Archetypes
Targeting queries follow strict priority orders:
1. **Front-Row Priority (Default Melee)**: Targets the opposing column's front row; if empty, searches adjacent columns front row, then mid row, then back row.
2. **Back-Row Snipers (Archers / Assassins)**: Directly targets back row units (lowest HP or highest Attack).
3. **Column Piercing**: Hits all enemies in the targeted column (Front -> Mid -> Back).
4. **Row Sweep**: Hits all enemies in a horizontal row.
5. **Full AoE (All Targets)**: Hits all living units on the opposing side.
6. **Lowest HP Ally**: For healers and buffers.

## 5. Deterministic Combat Simulation & Replay
To support Server-Authoritative PvP and anti-cheat validation:
1. **PRNG Seed**: Battles must be initialized with an integer seed (e.g. `battle_seed = 123456`).
2. **All random rolls** (Crit, Dodge, Status Proc, Variance `0.95 - 1.05`) must consume the seeded PRNG sequentially.
3. **Input Actions**: Any player interactions (manual skill trigger, auto-battle toggle) are recorded with `[turn, unit_id, action_type]`.
4. The server can replay the exact battle using `POST /api/battle/simulate` with identical inputs, yielding identical winners and damage statistics.
