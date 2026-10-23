-- ============================================================
-- 08_chapter_rewards.sql - Stage rewards for Chapter 2 + 3 (Phase 5)
-- Values mirror data/database/stages/ch02 + ch03/*.tres exactly.
-- ============================================================

INSERT INTO stage_rewards (stage_id, stamina_cost, first_clear_silver, first_clear_gold, normal_silver, first_clear_exp, normal_exp)
VALUES
    ('2-1', 5, 3500, 350, 900, 1400, 600),
    ('2-2', 5, 4200, 420, 1100, 1700, 720),
    ('2-3', 6, 5000, 500, 1300, 2000, 850),
    ('2-4', 6, 5800, 580, 1500, 2300, 1000),
    ('2-5', 7, 6800, 680, 1750, 2600, 1100),
    ('2-6', 8, 8000, 800, 2000, 3000, 1200),
    ('3-1', 7, 9000, 900, 2200, 3200, 1400),
    ('3-2', 7, 10500, 1050, 2600, 3700, 1600),
    ('3-3', 8, 12000, 1200, 3000, 4200, 1800),
    ('3-4', 8, 14000, 1400, 3500, 4800, 2100),
    ('3-5', 9, 16000, 1600, 4000, 5400, 2400),
    ('3-6', 10, 18000, 1800, 4500, 6000, 2600)
ON CONFLICT (stage_id) DO UPDATE SET
    stamina_cost = EXCLUDED.stamina_cost,
    first_clear_silver = EXCLUDED.first_clear_silver,
    first_clear_gold = EXCLUDED.first_clear_gold,
    normal_silver = EXCLUDED.normal_silver,
    first_clear_exp = EXCLUDED.first_clear_exp,
    normal_exp = EXCLUDED.normal_exp;
