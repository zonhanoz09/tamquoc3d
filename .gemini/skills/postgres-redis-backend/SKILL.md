---
name: postgres-redis-backend
description: Architecture, database schema design, and backend API integration for PostgreSQL 16 & Redis 7 in Tam Quoc 3D
triggers:
  - database
  - postgres
  - redis
  - docker
  - sql
  - fastapi
  - backend
---

# PostgreSQL 16 & Redis 7 Backend Skill

This skill governs database design, schema migrations, Docker operations, and FastAPI server implementations for **Tam Quoc 3D**.

## 1. System Topology & Infrastructure
The backend is packaged and containerized with Docker Compose:
- **`tamquoc-postgres`** (PostgreSQL 16-alpine on port `5432`):
  - Database: `tamquoc_db`, User: `tamquoc`, Pass: `tamquoc_secret_2026`.
- **`tamquoc-redis`** (Redis 7-alpine on port `6379`):
  - In-memory caching, rate limiting, and session management.
- **`tamquoc-game-server`** (FastAPI / Python 3.11 on port `8000`):
  - REST API, deterministic combat validator, Web Game Portal.

## 2. PostgreSQL 7 Core Game Modules Schema Reference
All tables reside in [`data/sql/01_schema.sql`](file:///home/annhan/Documents/src/tamquoc3d/data/sql/01_schema.sql):

1. **Module 1: Auth & Wallets**:
   - `users`: Core UUID identity, status (`ACTIVE`, `BANNED`, `SUSPENDED`).
   - `user_auth_providers`: Multi-platform linking (`GUEST`, `GOOGLE`, `APPLE`, `STEAM`).
   - `user_profiles`: `display_name`, `avatar_id`, `level`, `exp`, `vip_level`, `combat_power`.
   - `user_wallets`: `silver`, `gold`, `paid_gold`, `stamina`, `max_stamina`, `arena_tokens`.
2. **Module 2: Heroes & Formations**:
   - `user_heroes`: Owned heroes, `star` (1-6), `level`, `skill_levels` (JSONB), `unlocked_talents` (JSONB).
   - `user_formations`: 3x3 grid layout (Front, Mid, Back) for `PVE_DEFAULT`, `ARENA_DEFENSE`, `GUILD_BOSS`.
3. **Module 3: Inventories & Equipments**:
   - `user_inventories`: Stackable items (`item_id`, `quantity`).
   - `user_equipments`: Unique gears with `slot_type`, `enhance_level`, `main_stat` (JSONB), `sub_stats` (JSONB GIN index), `sockets` (JSONB GIN index).
4. **Module 4: Progression**:
   - `campaign_progress`: Stages cleared, stars (0-3), first clear status, best turns.
   - `tower_progress`: Endless tower floor and sweep status.
   - `daily_dungeon_records`: Daily entry limits by date (`date_key`).
5. **Module 5: Gacha, Monetization & Audit**:
   - `user_gacha_pity`: Pity counters per banner (SSR pity, UR pity, rate-up guarantee).
   - `gacha_audit_logs`: Range-partitioned by month on `created_at`.
   - `iap_transactions`: In-app purchases with receipts and validation status.
6. **Module 6: Arena & Battles**:
   - `arena_profiles`: Elo rating, seasonal rank, defense formation reference.
   - `arena_match_history`: PvP match records and rating changes.
   - `battle_replays`: Range-partitioned by month on `created_at`, stores deterministic `seed` and `input_actions`.
7. **Module 7: System Mail & Configs**:
   - `system_mail_templates`: Global announcements and system rewards.
   - `user_mails`: Player inbox, attachment claims (`is_claimed`).
   - `game_configs`: Hot-reloadable balance constants, drop rates, level caps.

## 3. Strict SQL Rules & Conventions
1. **Always Use `TIMESTAMPTZ`**: Never use timestamp without timezone.
2. **Updated_At Trigger**: Every mutable table must attach `trg_*_updated_at`:
   ```sql
   CREATE TRIGGER trg_table_updated_at
   BEFORE UPDATE ON table_name
   FOR EACH ROW EXECUTE FUNCTION update_modified_column();
   ```
3. **Partitioning Rule**: For tables partitioned by `RANGE (created_at)` (like `gacha_audit_logs` or `battle_replays`), the primary key **must include the partition key**:
   ```sql
   PRIMARY KEY (id, created_at)
   ```
4. **JSONB Indexing**: Always add GIN indexes for queried JSONB columns:
   ```sql
   CREATE INDEX idx_equip_sub_stats ON user_equipments USING gin (sub_stats);
   ```

## 4. Docker Compose Operations
```bash
# Start all services in background
docker compose up -d

# Check status of containers
docker compose ps

# View logs of game server or postgres
docker compose logs -f game-server
docker compose logs -f postgres

# Execute psql inside postgres container
docker compose exec postgres psql -U tamquoc -d tamquoc_db
```
