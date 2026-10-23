-- ============================================================
-- 06_stage_rewards.sql
-- Server-side source of truth for campaign stage rewards.
-- The client must NEVER dictate rewards: POST /api/campaign/clear
-- only sends stage_id + stars + turns, the server looks up the
-- stamina cost and rewards here (values mirror data/database/stages/ch01/*.tres).
-- ============================================================

CREATE TABLE IF NOT EXISTS stage_rewards (
    stage_id            TEXT PRIMARY KEY,
    stamina_cost        INT NOT NULL DEFAULT 0,
    first_clear_silver  INT NOT NULL DEFAULT 0,
    first_clear_gold    INT NOT NULL DEFAULT 0,
    first_clear_exp     INT NOT NULL DEFAULT 0,
    normal_silver       INT NOT NULL DEFAULT 0,
    normal_exp          INT NOT NULL DEFAULT 0
);

INSERT INTO stage_rewards
    (stage_id, stamina_cost, first_clear_silver, first_clear_gold, first_clear_exp, normal_silver, normal_exp)
VALUES
    ('1-1', 3,  500,  50,  200, 200, 100),
    ('1-2', 3,  600,  60,  250, 250, 120),
    ('1-3', 4,  800,  80,  350, 300, 150),
    ('1-4', 4, 1000, 100,  450, 350, 180),
    ('1-5', 5, 1500, 150,  600, 400, 200),
    ('1-6', 6, 3000, 300, 1200, 800, 500)
ON CONFLICT (stage_id) DO UPDATE
SET stamina_cost       = EXCLUDED.stamina_cost,
    first_clear_silver = EXCLUDED.first_clear_silver,
    first_clear_gold   = EXCLUDED.first_clear_gold,
    first_clear_exp    = EXCLUDED.first_clear_exp,
    normal_silver      = EXCLUDED.normal_silver,
    normal_exp         = EXCLUDED.normal_exp;
