-- ==============================================================================
-- PROJECT PIXEL TACTICS — TAM QUỐC 3D (POSTGRESQL DATABASE SCHEMA)
-- Description: Hệ thống Cơ sở dữ liệu Game Authoritative Server, hỗ trợ Cross-platform,
--              Anti-cheat, Deterministic Combat Replay, Audit Log & LiveOps.
-- Target RDBMS: PostgreSQL 14+ / 15+ / 16+
-- ==============================================================================

-- 0. Khởi tạo Extensions & Hàm Bổ Trợ Toàn Cục
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Hàm trigger tự động cập nhật updated_at
CREATE OR REPLACE FUNCTION update_modified_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- ==============================================================================
-- MODULE 1: ĐỊNH DANH, TÀI KHOẢN & TIỀN TỆ (AUTH & WALLETS)
-- ==============================================================================

-- 1.1 Bảng tài khoản gốc
CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    status VARCHAR(20) NOT NULL DEFAULT 'ACTIVE', -- ACTIVE, BANNED, SUSPENDED
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

DROP TRIGGER IF EXISTS trg_users_updated_at ON users;
CREATE TRIGGER trg_users_updated_at
BEFORE UPDATE ON users
FOR EACH ROW EXECUTE FUNCTION update_modified_column();

-- 1.2 Liên kết kênh đăng nhập đa nền tảng
CREATE TABLE IF NOT EXISTS user_auth_providers (
    id BIGSERIAL PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    provider VARCHAR(30) NOT NULL, -- GUEST, GOOGLE, APPLE, STEAM
    provider_user_id VARCHAR(255) NOT NULL,
    linked_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_provider_account UNIQUE(provider, provider_user_id)
);

CREATE INDEX IF NOT EXISTS idx_auth_user ON user_auth_providers(user_id);

-- 1.3 Hồ sơ hiển thị người chơi
CREATE TABLE IF NOT EXISTS user_profiles (
    user_id UUID PRIMARY KEY REFERENCES users(id) ON DELETE CASCADE,
    display_name VARCHAR(50) NOT NULL,
    avatar_id VARCHAR(50) DEFAULT 'avatar_default',
    frame_id VARCHAR(50) DEFAULT 'frame_default',
    level INT NOT NULL DEFAULT 1 CHECK (level >= 1),
    exp BIGINT NOT NULL DEFAULT 0 CHECK (exp >= 0),
    vip_level INT NOT NULL DEFAULT 0 CHECK (vip_level >= 0),
    combat_power BIGINT NOT NULL DEFAULT 0 CHECK (combat_power >= 0),
    last_login_at TIMESTAMPTZ,
    last_login_ip VARCHAR(45)
);

CREATE INDEX IF NOT EXISTS idx_profiles_combat_power ON user_profiles(combat_power DESC);
CREATE INDEX IF NOT EXISTS idx_profiles_display_name ON user_profiles(display_name);

-- 1.4 Ví tiền tệ và cơ chế thể lực
CREATE TABLE IF NOT EXISTS user_wallets (
    user_id UUID PRIMARY KEY REFERENCES users(id) ON DELETE CASCADE,
    silver BIGINT NOT NULL DEFAULT 0 CHECK (silver >= 0),
    gold BIGINT NOT NULL DEFAULT 0 CHECK (gold >= 0),
    paid_gold BIGINT NOT NULL DEFAULT 0 CHECK (paid_gold >= 0),
    stamina INT NOT NULL DEFAULT 120 CHECK (stamina >= 0),
    max_stamina INT NOT NULL DEFAULT 120 CHECK (max_stamina >= 0),
    last_stamina_recovery_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    arena_tokens INT NOT NULL DEFAULT 0 CHECK (arena_tokens >= 0),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

DROP TRIGGER IF EXISTS trg_user_wallets_updated_at ON user_wallets;
CREATE TRIGGER trg_user_wallets_updated_at
BEFORE UPDATE ON user_wallets
FOR EACH ROW EXECUTE FUNCTION update_modified_column();

-- ==============================================================================
-- MODULE 2: TƯỚNG, KỸ NĂNG & TRẬN HÌNH (HEROES & FORMATIONS)
-- ==============================================================================

-- 2.1 Danh sách tướng người chơi sở hữu
CREATE TABLE IF NOT EXISTS user_heroes (
    id BIGSERIAL PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    hero_id VARCHAR(50) NOT NULL,
    star INT NOT NULL DEFAULT 1 CHECK (star BETWEEN 1 AND 6),
    level INT NOT NULL DEFAULT 1 CHECK (level >= 1),
    exp BIGINT NOT NULL DEFAULT 0 CHECK (exp >= 0),
    skill_levels JSONB NOT NULL DEFAULT '{
        "normal": 1,
        "skill": 1,
        "passive": 1
    }'::jsonb,
    unlocked_talents JSONB NOT NULL DEFAULT '[]'::jsonb,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_user_hero UNIQUE(user_id, hero_id)
);

CREATE INDEX IF NOT EXISTS idx_user_heroes_user ON user_heroes(user_id);
CREATE INDEX IF NOT EXISTS idx_user_heroes_hero ON user_heroes(hero_id);

-- 2.2 Bố cục trận hình 3 hàng (Front, Mid, Back)
CREATE TABLE IF NOT EXISTS user_formations (
    id BIGSERIAL PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    formation_type VARCHAR(30) NOT NULL, -- PVE_DEFAULT, ARENA_DEFENSE, GUILD_BOSS
    grid_layout JSONB NOT NULL DEFAULT '{
        "front": [null, null, null],
        "mid":   [null, null, null],
        "back":  [null, null, null]
    }'::jsonb,
    is_active BOOLEAN NOT NULL DEFAULT true,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_user_formation_type UNIQUE(user_id, formation_type)
);

DROP TRIGGER IF EXISTS trg_user_formations_updated_at ON user_formations;
CREATE TRIGGER trg_user_formations_updated_at
BEFORE UPDATE ON user_formations
FOR EACH ROW EXECUTE FUNCTION update_modified_column();

-- ==============================================================================
-- MODULE 3: KHO ĐỒ & TRANG BỊ (INVENTORIES & EQUIPMENTS)
-- ==============================================================================

-- 3.1 Túi đồ chứa vật phẩm xếp chồng (Stackable Items)
CREATE TABLE IF NOT EXISTS user_inventories (
    id BIGSERIAL PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    item_id VARCHAR(50) NOT NULL,
    quantity BIGINT NOT NULL DEFAULT 0 CHECK (quantity >= 0),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_user_item UNIQUE(user_id, item_id)
);

CREATE INDEX IF NOT EXISTS idx_user_inventory ON user_inventories(user_id);

DROP TRIGGER IF EXISTS trg_user_inventories_updated_at ON user_inventories;
CREATE TRIGGER trg_user_inventories_updated_at
BEFORE UPDATE ON user_inventories
FOR EACH ROW EXECUTE FUNCTION update_modified_column();

-- 3.2 Trang bị cá thể hóa (Non-stackable, có Main/Sub-stats và Socket)
CREATE TABLE IF NOT EXISTS user_equipments (
    id BIGSERIAL PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    equipment_id VARCHAR(50) NOT NULL,
    slot_type VARCHAR(20) NOT NULL, -- WEAPON, ARMOR, HELMET, BOOTS, ACCESSORY
    enhance_level INT NOT NULL DEFAULT 0 CHECK (enhance_level BETWEEN 0 AND 15),
    equipped_hero_id BIGINT REFERENCES user_heroes(id) ON DELETE SET NULL,
    main_stat JSONB NOT NULL,
    sub_stats JSONB NOT NULL DEFAULT '[]'::jsonb,
    sockets JSONB NOT NULL DEFAULT '[null, null]'::jsonb,
    is_locked BOOLEAN NOT NULL DEFAULT false,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_equip_user ON user_equipments(user_id);
CREATE INDEX IF NOT EXISTS idx_equip_hero ON user_equipments(equipped_hero_id);
CREATE INDEX IF NOT EXISTS idx_equip_sub_stats ON user_equipments USING gin (sub_stats);
CREATE INDEX IF NOT EXISTS idx_equip_sockets ON user_equipments USING gin (sockets);

-- ==============================================================================
-- MODULE 4: TIẾN TRÌNH VƯỢT ẢI (PROGRESSION)
-- ==============================================================================

-- 4.1 Tiến trình vượt ải Cốt truyện chính
CREATE TABLE IF NOT EXISTS campaign_progress (
    id BIGSERIAL PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    stage_id VARCHAR(50) NOT NULL,
    stars INT NOT NULL DEFAULT 0 CHECK (stars BETWEEN 0 AND 3),
    first_clear_claimed BOOLEAN NOT NULL DEFAULT false,
    clear_count INT NOT NULL DEFAULT 1 CHECK (clear_count >= 1),
    best_record_turns INT,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_user_stage UNIQUE(user_id, stage_id)
);

CREATE INDEX IF NOT EXISTS idx_campaign_user ON campaign_progress(user_id);

DROP TRIGGER IF EXISTS trg_campaign_progress_updated_at ON campaign_progress;
CREATE TRIGGER trg_campaign_progress_updated_at
BEFORE UPDATE ON campaign_progress
FOR EACH ROW EXECUTE FUNCTION update_modified_column();

-- 4.2 Tháp vô tận (Endless Tower)
CREATE TABLE IF NOT EXISTS tower_progress (
    user_id UUID PRIMARY KEY REFERENCES users(id) ON DELETE CASCADE,
    highest_floor INT NOT NULL DEFAULT 0 CHECK (highest_floor >= 0),
    swept_today BOOLEAN NOT NULL DEFAULT false,
    last_sweep_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

DROP TRIGGER IF EXISTS trg_tower_progress_updated_at ON tower_progress;
CREATE TRIGGER trg_tower_progress_updated_at
BEFORE UPDATE ON tower_progress
FOR EACH ROW EXECUTE FUNCTION update_modified_column();

-- 4.3 Giới hạn vượt phó bản hàng ngày
CREATE TABLE IF NOT EXISTS daily_dungeon_records (
    id BIGSERIAL PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    dungeon_type VARCHAR(30) NOT NULL, -- GOLD_DUNGEON, EXP_DUNGEON
    date_key DATE NOT NULL,
    attempts_used INT NOT NULL DEFAULT 0 CHECK (attempts_used >= 0),
    CONSTRAINT uq_user_daily_dungeon UNIQUE(user_id, dungeon_type, date_key)
);

CREATE INDEX IF NOT EXISTS idx_daily_dungeon_user ON daily_dungeon_records(user_id, date_key);

-- ==============================================================================
-- MODULE 5: GACHA, DOANH THU & SỔ CÁI GIAO DỊCH (MONETIZATION & AUDIT)
-- ==============================================================================

-- 5.1 Bộ đếm Pity từng Banner
CREATE TABLE IF NOT EXISTS user_gacha_pity (
    id BIGSERIAL PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    banner_id VARCHAR(50) NOT NULL,
    pull_count_since_last_ssr INT NOT NULL DEFAULT 0 CHECK (pull_count_since_last_ssr >= 0),
    pull_count_since_last_ur INT NOT NULL DEFAULT 0 CHECK (pull_count_since_last_ur >= 0),
    is_next_rate_up_guaranteed BOOLEAN NOT NULL DEFAULT false,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_user_banner_pity UNIQUE(user_id, banner_id)
);

DROP TRIGGER IF EXISTS trg_user_gacha_pity_updated_at ON user_gacha_pity;
CREATE TRIGGER trg_user_gacha_pity_updated_at
BEFORE UPDATE ON user_gacha_pity
FOR EACH ROW EXECUTE FUNCTION update_modified_column();

-- 5.2 Lịch sử quay Gacha (Phân vùng theo tháng)
CREATE TABLE IF NOT EXISTS gacha_audit_logs (
    id BIGSERIAL,
    user_id UUID NOT NULL,
    banner_id VARCHAR(50) NOT NULL,
    result_type VARCHAR(20) NOT NULL, -- HERO, ITEM
    result_id VARCHAR(50) NOT NULL,
    rarity VARCHAR(10) NOT NULL,      -- R, SR, SSR, UR
    pull_pity_count INT NOT NULL,
    currency_spent VARCHAR(20) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (id, created_at)
) PARTITION BY RANGE (created_at);

-- Tạo sẵn phân vùng khởi đầu mẫu
CREATE TABLE IF NOT EXISTS gacha_audit_logs_y2026m10 PARTITION OF gacha_audit_logs
    FOR VALUES FROM ('2026-10-01 00:00:00+00') TO ('2026-11-01 00:00:00+00');
CREATE TABLE IF NOT EXISTS gacha_audit_logs_y2026m11 PARTITION OF gacha_audit_logs
    FOR VALUES FROM ('2026-11-01 00:00:00+00') TO ('2026-12-01 00:00:00+00');
CREATE TABLE IF NOT EXISTS gacha_audit_logs_y2026m12 PARTITION OF gacha_audit_logs
    FOR VALUES FROM ('2026-12-01 00:00:00+00') TO ('2027-01-01 00:00:00+00');

CREATE INDEX IF NOT EXISTS idx_gacha_audit_user ON gacha_audit_logs(user_id, created_at DESC);

-- 5.3 Lịch sử Giao dịch Nạp IAP
CREATE TABLE IF NOT EXISTS iap_transactions (
    id BIGSERIAL PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id),
    store VARCHAR(20) NOT NULL,            -- APPLE_APPSTORE, GOOGLE_PLAY
    transaction_id VARCHAR(255) NOT NULL,
    product_id VARCHAR(100) NOT NULL,
    amount_usd NUMERIC(10, 2) NOT NULL CHECK (amount_usd >= 0),
    status VARCHAR(30) NOT NULL,           -- PENDING, COMPLETED, FAILED, REFUNDED
    receipt_data TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_store_transaction UNIQUE(store, transaction_id)
);

CREATE INDEX IF NOT EXISTS idx_iap_user ON iap_transactions(user_id);
CREATE INDEX IF NOT EXISTS idx_iap_status ON iap_transactions(status);

-- ==============================================================================
-- MODULE 6: ĐẤU TRƯỜNG & CHIẾN BÁO REPLAY (ARENA & BATTLES)
-- ==============================================================================

-- 6.1 Hồ sơ Xếp hạng Đấu trường
CREATE TABLE IF NOT EXISTS arena_profiles (
    user_id UUID PRIMARY KEY REFERENCES users(id) ON DELETE CASCADE,
    rank_tier VARCHAR(20) NOT NULL DEFAULT 'BRONZE',
    mmr INT NOT NULL DEFAULT 1000 CHECK (mmr >= 0),
    win_count INT NOT NULL DEFAULT 0 CHECK (win_count >= 0),
    loss_count INT NOT NULL DEFAULT 0 CHECK (loss_count >= 0),
    current_winning_streak INT NOT NULL DEFAULT 0 CHECK (current_winning_streak >= 0),
    daily_tickets_used INT NOT NULL DEFAULT 0 CHECK (daily_tickets_used >= 0),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_arena_mmr ON arena_profiles(mmr DESC);

DROP TRIGGER IF EXISTS trg_arena_profiles_updated_at ON arena_profiles;
CREATE TRIGGER trg_arena_profiles_updated_at
BEFORE UPDATE ON arena_profiles
FOR EACH ROW EXECUTE FUNCTION update_modified_column();

-- 6.2 Snapshot Đội hình Phòng thủ
CREATE TABLE IF NOT EXISTS arena_defense_snapshots (
    user_id UUID PRIMARY KEY REFERENCES users(id) ON DELETE CASCADE,
    combat_power BIGINT NOT NULL CHECK (combat_power >= 0),
    lineup_payload JSONB NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

DROP TRIGGER IF EXISTS trg_arena_defense_snapshots_updated_at ON arena_defense_snapshots;
CREATE TRIGGER trg_arena_defense_snapshots_updated_at
BEFORE UPDATE ON arena_defense_snapshots
FOR EACH ROW EXECUTE FUNCTION update_modified_column();

-- 6.3 Nhật ký Chiến báo & Dữ liệu Replay (Phân vùng theo tháng)
CREATE TABLE IF NOT EXISTS battle_replays (
    id UUID DEFAULT gen_random_uuid(),
    battle_type VARCHAR(30) NOT NULL, -- ASYNC_ARENA, RTA, PVE_BOSS
    attacker_id UUID NOT NULL,
    defender_id UUID,
    is_attacker_win BOOLEAN NOT NULL,
    seed BIGINT NOT NULL,
    input_actions JSONB NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (id, created_at)
) PARTITION BY RANGE (created_at);

-- Phân vùng khởi tạo
CREATE TABLE IF NOT EXISTS battle_replays_y2026m10 PARTITION OF battle_replays
    FOR VALUES FROM ('2026-10-01 00:00:00+00') TO ('2026-11-01 00:00:00+00');
CREATE TABLE IF NOT EXISTS battle_replays_y2026m11 PARTITION OF battle_replays
    FOR VALUES FROM ('2026-11-01 00:00:00+00') TO ('2026-12-01 00:00:00+00');
CREATE TABLE IF NOT EXISTS battle_replays_y2026m12 PARTITION OF battle_replays
    FOR VALUES FROM ('2026-12-01 00:00:00+00') TO ('2027-01-01 00:00:00+00');

CREATE INDEX IF NOT EXISTS idx_replays_attacker ON battle_replays(attacker_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_replays_defender ON battle_replays(defender_id, created_at DESC);

-- ==============================================================================
-- MODULE 7: HÒM THƯ & CẤU HÌNH LIVEOPS (MAIL & SYSTEM CONFIGS)
-- ==============================================================================

-- 7.1 Hòm thư hệ thống chung
CREATE TABLE IF NOT EXISTS system_mails (
    id BIGSERIAL PRIMARY KEY,
    title VARCHAR(100) NOT NULL,
    content TEXT NOT NULL,
    attached_rewards JSONB NOT NULL DEFAULT '[]'::jsonb,
    target_scope VARCHAR(20) NOT NULL DEFAULT 'ALL', -- ALL, FILTER, SPECIFIC
    filter_condition JSONB,
    expires_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_system_mails_expires ON system_mails(expires_at);

-- 7.2 Trạng thái nhận thư theo từng User
CREATE TABLE IF NOT EXISTS mail_recipients (
    id BIGSERIAL PRIMARY KEY,
    mail_id BIGINT NOT NULL REFERENCES system_mails(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    is_read BOOLEAN NOT NULL DEFAULT false,
    is_claimed BOOLEAN NOT NULL DEFAULT false,
    claimed_at TIMESTAMPTZ,
    CONSTRAINT uq_mail_user UNIQUE(mail_id, user_id)
);

CREATE INDEX IF NOT EXISTS idx_mail_user ON mail_recipients(user_id, is_read, is_claimed);

-- 7.3 Cấu hình Cờ Tính năng & Tham số nóng (Feature Flags)
CREATE TABLE IF NOT EXISTS system_configs (
    config_key VARCHAR(100) PRIMARY KEY,
    config_value JSONB NOT NULL,
    description TEXT,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

DROP TRIGGER IF EXISTS trg_system_configs_updated_at ON system_configs;
CREATE TRIGGER trg_system_configs_updated_at
BEFORE UPDATE ON system_configs
FOR EACH ROW EXECUTE FUNCTION update_modified_column();
