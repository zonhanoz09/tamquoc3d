-- ==============================================================================
-- PROJECT PIXEL TACTICS — TAM QUỐC 3D (SAMPLE SEED DATA)
-- Description: Dữ liệu khởi tạo mẫu cho máy chủ chạy Docker Compose
-- ==============================================================================

-- 1. Tài khoản mẫu
INSERT INTO users (id, status, created_at, updated_at)
VALUES
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11', 'ACTIVE', NOW(), NOW()),
    ('b1ffcd88-8b1c-3df7-aa5c-5aa8ac270b22', 'ACTIVE', NOW(), NOW())
ON CONFLICT (id) DO NOTHING;

-- 1.2 Auth providers
INSERT INTO user_auth_providers (user_id, provider, provider_user_id)
VALUES 
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11', 'GOOGLE', 'google_sub_109283719283'),
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11', 'GUEST', 'device_guest_8888'),
    ('b1ffcd88-8b1c-3df7-aa5c-5aa8ac270b22', 'STEAM', '76561198000000001')
ON CONFLICT (provider, provider_user_id) DO NOTHING;

-- 1.3 Hồ sơ hiển thị
INSERT INTO user_profiles (user_id, display_name, avatar_id, frame_id, level, exp, vip_level, combat_power, last_login_at, last_login_ip)
VALUES 
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11', 'Long Tướng Quân', 'lu_bo', 'frame_dragon_gold', 80, 850000, 11, 188880, NOW(), '127.0.0.1'),
    ('b1ffcd88-8b1c-3df7-aa5c-5aa8ac270b22', 'Ngụy Vương Tào Tháo', 'tao_nhan', 'frame_phoenix', 79, 780000, 10, 175200, NOW(), '127.0.0.1')
ON CONFLICT (user_id) DO NOTHING;

-- 1.4 Ví tiền tệ
INSERT INTO user_wallets (user_id, silver, gold, paid_gold, stamina, max_stamina, arena_tokens)
VALUES 
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11', 2145370, 150000, 12000, 100, 145, 450),
    ('b1ffcd88-8b1c-3df7-aa5c-5aa8ac270b22', 1200000, 80000, 5000, 120, 140, 280)
ON CONFLICT (user_id) DO NOTHING;

-- 2.1 Võ tướng sở hữu
INSERT INTO user_heroes (user_id, hero_id, star, level, exp, is_commander)
VALUES
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11', 'lu_bo', 6, 80, 500000, true),
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11', 'quan_vu', 5, 80, 480000, false),
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11', 'trieu_van', 5, 78, 410000, false),
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11', 'gia_cat_luong', 5, 80, 460000, false),
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11', 'chu_du', 4, 75, 320000, false),
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11', 'dieu_thuyen', 4, 75, 310000, false),
    ('b1ffcd88-8b1c-3df7-aa5c-5aa8ac270b22', 'dien_vi', 5, 78, 420000, false),
    ('b1ffcd88-8b1c-3df7-aa5c-5aa8ac270b22', 'tao_nhan', 5, 79, 440000, true)
ON CONFLICT (user_id, hero_id) DO NOTHING;

INSERT INTO user_hero_skills (user_hero_id, hero_id, skill_id, level)
SELECT uh.id, uh.hero_id, hs.skill_id,
       CASE hs.skill_slot
           WHEN 'NORMAL' THEN seed.normal_level
           ELSE seed.rage_level
       END
FROM (VALUES
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11'::uuid, 'lu_bo', 15, 15),
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11'::uuid, 'quan_vu', 14, 14),
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11'::uuid, 'trieu_van', 13, 13),
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11'::uuid, 'gia_cat_luong', 15, 15),
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11'::uuid, 'chu_du', 10, 10),
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11'::uuid, 'dieu_thuyen', 10, 10),
    ('b1ffcd88-8b1c-3df7-aa5c-5aa8ac270b22'::uuid, 'dien_vi', 13, 13),
    ('b1ffcd88-8b1c-3df7-aa5c-5aa8ac270b22'::uuid, 'tao_nhan', 14, 14)
) AS seed(user_id, hero_id, normal_level, rage_level)
JOIN user_heroes uh ON uh.user_id = seed.user_id AND uh.hero_id = seed.hero_id
JOIN hero_skills hs ON hs.hero_id = uh.hero_id
ON CONFLICT (user_hero_id, skill_id) DO NOTHING;

INSERT INTO user_hero_talents (user_hero_id, hero_id, talent_id)
SELECT uh.id, uh.hero_id, talent.talent_id
FROM (VALUES
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11'::uuid, 'lu_bo', 'vo_song'),
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11'::uuid, 'lu_bo', 'no_hoa'),
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11'::uuid, 'quan_vu', 'nghia_khi'),
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11'::uuid, 'quan_vu', 'yem_nguyet'),
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11'::uuid, 'trieu_van', 'long_dam'),
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11'::uuid, 'gia_cat_luong', 'dong_phong'),
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11'::uuid, 'gia_cat_luong', 'bat_tran'),
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11'::uuid, 'chu_du', 'hoa_thao'),
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11'::uuid, 'dieu_thuyen', 'be_nguyet'),
    ('b1ffcd88-8b1c-3df7-aa5c-5aa8ac270b22'::uuid, 'dien_vi', 'hung_dung'),
    ('b1ffcd88-8b1c-3df7-aa5c-5aa8ac270b22'::uuid, 'tao_nhan', 'thiet_bich')
) AS talent(user_id, hero_id, talent_id)
JOIN user_heroes uh ON uh.user_id = talent.user_id AND uh.hero_id = talent.hero_id
ON CONFLICT (user_hero_id, talent_id) DO NOTHING;

-- 2.2 Đội hình 3x3
INSERT INTO user_formations (user_id, formation_type, grid_layout, is_active)
VALUES 
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11', 'PVE_DEFAULT', '{
        "front": [null, "lu_bo", null],
        "mid":   ["quan_vu", null, "trieu_van"],
        "back":  ["gia_cat_luong", null, "chu_du"]
    }'::jsonb, true),
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11', 'ARENA_DEFENSE', '{
        "front": [null, "lu_bo", null],
        "mid":   ["quan_vu", "dieu_thuyen", "trieu_van"],
        "back":  [null, "gia_cat_luong", null]
    }'::jsonb, true),
    ('b1ffcd88-8b1c-3df7-aa5c-5aa8ac270b22', 'ARENA_DEFENSE', '{
        "front": [null, "dien_vi", null],
        "mid":   [null, "tao_nhan", null],
        "back":  [null, null, null]
    }'::jsonb, true)
ON CONFLICT (user_id, formation_type) DO NOTHING;

-- 3.1 Túi đồ
INSERT INTO user_inventories (user_id, item_id, quantity)
VALUES 
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11', 'dan_exp_cao', 45),
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11', 'da_cuong_hoa_bac', 120),
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11', 've_trieu_hoi_cao_cap', 15),
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11', 'manh_tuong_lu_bo', 20)
ON CONFLICT (user_id, item_id) DO NOTHING;

-- 3.2 Trang bị
INSERT INTO user_equipments (user_id, equipment_id, slot_type, enhance_level, main_stat, sub_stats, sockets)
VALUES 
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11', 'wpn_phuong_thien', 'WEAPON', 12, '{"type": "ATK", "val": 1580}'::jsonb, '[{"type": "CRIT_RATE", "val": 0.12}, {"type": "CRIT_DMG", "val": 0.35}]'::jsonb, '["gem_rubi_lvl3", null]'::jsonb),
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11', 'wpn_thanh_long', 'WEAPON', 10, '{"type": "ATK", "val": 1420}'::jsonb, '[{"type": "BLOCK", "val": 0.08}, {"type": "ATK_PERCENT", "val": 0.10}]'::jsonb, '["gem_sapphire_lvl2", null]'::jsonb)
ON CONFLICT DO NOTHING;

-- 4.1 Chiến dịch
INSERT INTO campaign_progress (user_id, stage_id, stars, first_clear_claimed, clear_count, best_record_turns)
VALUES 
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11', 'stage_1_1', 3, true, 8, 2),
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11', 'stage_1_2', 3, true, 5, 3),
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11', 'stage_1_3', 3, true, 4, 3),
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11', 'stage_1_4', 3, true, 2, 4)
ON CONFLICT (user_id, stage_id) DO NOTHING;

-- 4.2 Tháp vô tận
INSERT INTO tower_progress (user_id, highest_floor, swept_today)
VALUES 
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11', 68, false),
    ('b1ffcd88-8b1c-3df7-aa5c-5aa8ac270b22', 62, true)
ON CONFLICT (user_id) DO NOTHING;

-- 5.1 Gacha Pity
INSERT INTO user_gacha_pity (user_id, banner_id, pull_count_since_last_ssr, pull_count_since_last_ur, is_next_rate_up_guaranteed)
VALUES 
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11', 'banner_lu_bo_limited', 35, 12, true),
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11', 'banner_standard', 12, 5, false)
ON CONFLICT (user_id, banner_id) DO NOTHING;

-- 6.1 Đấu trường
INSERT INTO arena_profiles (user_id, rank_tier, mmr, win_count, loss_count, current_winning_streak, daily_tickets_used)
VALUES 
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11', 'PLATINUM', 1650, 48, 12, 6, 2),
    ('b1ffcd88-8b1c-3df7-aa5c-5aa8ac270b22', 'GOLD', 1420, 36, 18, 0, 4)
ON CONFLICT (user_id) DO NOTHING;

INSERT INTO arena_defense_snapshots (user_id, combat_power, lineup_payload)
VALUES 
    ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11', 188880, '{"heroes": ["lu_bo", "quan_vu", "dieu_thuyen", "trieu_van", "gia_cat_luong"]}'::jsonb),
    ('b1ffcd88-8b1c-3df7-aa5c-5aa8ac270b22', 175200, '{"heroes": ["dien_vi", "tao_nhan"]}'::jsonb)
ON CONFLICT (user_id) DO NOTHING;

-- 7.1 Hòm thư hệ thống & Thư tân thủ
INSERT INTO system_mails (id, title, content, attached_rewards, target_scope, expires_at)
VALUES 
    (1, 'Quà Mừng Khởi Công Đại Nghiệp', 'Chúc mừng Chúa Công đã bước chân vào chiến trường Tam Quốc! Tặng bạn quân lương và KNB ban đầu.', '[{"type": "SILVER", "val": 300000}, {"type": "GOLD", "val": 1000}, {"type": "TICKET", "val": 10}]'::jsonb, 'ALL', NOW() + INTERVAL '30 days')
ON CONFLICT (id) DO NOTHING;

INSERT INTO mail_recipients (mail_id, user_id, is_read, is_claimed)
VALUES 
    (1, 'a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11', false, false)
ON CONFLICT (mail_id, user_id) DO NOTHING;

-- 7.3 System configs
INSERT INTO system_configs (config_key, config_value, description)
VALUES 
    ('server_version', '"1.0.0"'::jsonb, 'Current production game server version'),
    ('maintenance_mode', 'false'::jsonb, 'Server maintenance switch'),
    ('gacha_rateup_hero', '"lu_bo"'::jsonb, 'Current rate-up banner legendary hero')
ON CONFLICT (config_key) DO NOTHING;
