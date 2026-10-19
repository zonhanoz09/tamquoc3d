-- Run after 00_hero_catalog.sql. Adds per-player hero display names.
BEGIN;

ALTER TABLE user_heroes
    ADD COLUMN IF NOT EXISTS custom_name VARCHAR(100) NOT NULL DEFAULT '';

UPDATE user_heroes owned
SET custom_name = profile.display_name
FROM user_profiles profile
WHERE profile.user_id = owned.user_id
  AND owned.hero_id = 'default_hero'
  AND owned.custom_name = '';

COMMIT;
