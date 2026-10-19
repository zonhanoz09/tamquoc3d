-- Run after 00_hero_catalog.sql to normalize player-owned hero progress.
BEGIN;

ALTER TABLE user_heroes
    ADD COLUMN IF NOT EXISTS is_commander BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE user_heroes
    ADD COLUMN IF NOT EXISTS updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW();

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conrelid = 'user_heroes'::regclass
          AND conname = 'uq_user_hero_identity'
    ) THEN
        ALTER TABLE user_heroes
            ADD CONSTRAINT uq_user_hero_identity UNIQUE (id, hero_id);
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conrelid = 'user_heroes'::regclass
          AND contype = 'f'
          AND confrelid = 'hero_definitions'::regclass
    ) THEN
        ALTER TABLE user_heroes
            ADD CONSTRAINT fk_user_heroes_definition
            FOREIGN KEY (hero_id) REFERENCES hero_definitions(hero_id);
    END IF;
END;
$$;

CREATE TABLE IF NOT EXISTS user_hero_skills (
    user_hero_id BIGINT NOT NULL,
    hero_id VARCHAR(50) NOT NULL,
    skill_id VARCHAR(50) NOT NULL,
    level INT NOT NULL DEFAULT 1 CHECK (level >= 1),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (user_hero_id, skill_id),
    CONSTRAINT fk_user_hero_skills_owner
        FOREIGN KEY (user_hero_id, hero_id)
        REFERENCES user_heroes(id, hero_id) ON DELETE CASCADE,
    CONSTRAINT fk_user_hero_skills_definition
        FOREIGN KEY (hero_id, skill_id)
        REFERENCES hero_skills(hero_id, skill_id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_user_hero_skills_skill ON user_hero_skills(hero_id, skill_id);

CREATE TABLE IF NOT EXISTS user_hero_talents (
    user_hero_id BIGINT NOT NULL,
    hero_id VARCHAR(50) NOT NULL,
    talent_id VARCHAR(50) NOT NULL,
    unlocked_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (user_hero_id, talent_id),
    CONSTRAINT fk_user_hero_talents_owner
        FOREIGN KEY (user_hero_id, hero_id)
        REFERENCES user_heroes(id, hero_id) ON DELETE CASCADE,
    CONSTRAINT fk_user_hero_talents_definition
        FOREIGN KEY (hero_id, talent_id)
        REFERENCES hero_talents(hero_id, talent_id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_user_hero_talents_definition ON user_hero_talents(hero_id, talent_id);

DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'user_heroes' AND column_name = 'skill_levels'
    ) THEN
        EXECUTE $migration$
            INSERT INTO user_hero_skills (user_hero_id, hero_id, skill_id, level)
            SELECT uh.id, uh.hero_id, hs.skill_id,
                   CASE hs.skill_slot
                       WHEN 'NORMAL' THEN COALESCE((uh.skill_levels ->> 'normal')::int, 1)
                       ELSE COALESCE(
                           (uh.skill_levels ->> 'skill')::int,
                           (uh.skill_levels ->> 'rage')::int,
                           (uh.skill_levels ->> 'passive')::int,
                           1)
                   END
            FROM user_heroes uh
            JOIN hero_skills hs ON hs.hero_id = uh.hero_id
            ON CONFLICT (user_hero_id, skill_id) DO NOTHING;
        $migration$;
    END IF;

    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'user_heroes' AND column_name = 'unlocked_talents'
    ) THEN
        EXECUTE $migration$
            INSERT INTO hero_talents (hero_id, talent_id, talent_name)
            SELECT DISTINCT uh.hero_id, talent.talent_id,
                   INITCAP(REPLACE(talent.talent_id, '_', ' '))
            FROM user_heroes uh
            CROSS JOIN LATERAL jsonb_array_elements_text(
                CASE
                    WHEN jsonb_typeof(uh.unlocked_talents) = 'array' THEN uh.unlocked_talents
                    ELSE '[]'::jsonb
                END
            ) AS talent(talent_id)
            ON CONFLICT (hero_id, talent_id) DO NOTHING;

            INSERT INTO user_hero_talents (user_hero_id, hero_id, talent_id)
            SELECT uh.id, uh.hero_id, talent.talent_id
            FROM user_heroes uh
            CROSS JOIN LATERAL jsonb_array_elements_text(
                CASE
                    WHEN jsonb_typeof(uh.unlocked_talents) = 'array' THEN uh.unlocked_talents
                    ELSE '[]'::jsonb
                END
            ) AS talent(talent_id)
            ON CONFLICT (user_hero_id, talent_id) DO NOTHING;
        $migration$;
    END IF;
END;
$$;

UPDATE user_heroes SET is_commander = FALSE WHERE is_commander;
WITH ranked_heroes AS (
    SELECT uh.id,
           ROW_NUMBER() OVER (
               PARTITION BY uh.user_id
               ORDER BY COALESCE(uh.hero_id = profile.avatar_id, FALSE) DESC, uh.created_at, uh.id
           ) AS hero_rank
    FROM user_heroes uh
    LEFT JOIN user_profiles profile ON profile.user_id = uh.user_id
)
UPDATE user_heroes uh
SET is_commander = TRUE
FROM ranked_heroes ranked
WHERE ranked.id = uh.id AND ranked.hero_rank = 1;

CREATE UNIQUE INDEX IF NOT EXISTS uq_user_heroes_commander
    ON user_heroes(user_id) WHERE is_commander;

DROP TRIGGER IF EXISTS trg_user_heroes_updated_at ON user_heroes;
CREATE TRIGGER trg_user_heroes_updated_at
BEFORE UPDATE ON user_heroes
FOR EACH ROW EXECUTE FUNCTION update_modified_column();

DROP TRIGGER IF EXISTS trg_user_hero_skills_updated_at ON user_hero_skills;
CREATE TRIGGER trg_user_hero_skills_updated_at
BEFORE UPDATE ON user_hero_skills
FOR EACH ROW EXECUTE FUNCTION update_modified_column();

ALTER TABLE user_heroes DROP COLUMN IF EXISTS skill_levels;
ALTER TABLE user_heroes DROP COLUMN IF EXISTS unlocked_talents;

COMMIT;
