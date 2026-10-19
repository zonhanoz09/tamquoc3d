-- Static hero catalogue, combat stats, skill definitions, and talent definitions.
CREATE TABLE IF NOT EXISTS hero_definitions (
    hero_id VARCHAR(50) PRIMARY KEY,
    hero_name VARCHAR(100) NOT NULL,
    title VARCHAR(150) NOT NULL DEFAULT '',
    element VARCHAR(10) NOT NULL CHECK (element IN ('NONE', 'FIRE', 'WATER', 'WIND', 'LIGHT', 'DARK')),
    position VARCHAR(10) NOT NULL CHECK (position IN ('FRONT', 'MID', 'BACK')),
    rarity VARCHAR(10) NOT NULL CHECK (rarity IN ('R', 'SR', 'SSR', 'UR')),
    base_stats JSONB NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE INDEX IF NOT EXISTS idx_hero_definitions_rarity ON hero_definitions(rarity, element);

CREATE TABLE IF NOT EXISTS hero_skills (
    skill_id VARCHAR(50) PRIMARY KEY,
    hero_id VARCHAR(50) NOT NULL REFERENCES hero_definitions(hero_id) ON DELETE CASCADE,
    skill_slot VARCHAR(10) NOT NULL CHECK (skill_slot IN ('NORMAL', 'RAGE')),
    skill_name VARCHAR(100) NOT NULL,
    description TEXT NOT NULL DEFAULT '',
    targeting_type SMALLINT NOT NULL CHECK (targeting_type BETWEEN 0 AND 7),
    morale_cost SMALLINT NOT NULL CHECK (morale_cost BETWEEN 0 AND 100),
    damage_multiplier NUMERIC(6, 3) NOT NULL DEFAULT 1,
    is_magic BOOLEAN NOT NULL DEFAULT FALSE,
    target_count SMALLINT NOT NULL DEFAULT 1 CHECK (target_count > 0),
    effects JSONB NOT NULL DEFAULT '[]'::jsonb,
    CONSTRAINT uq_hero_skill_slot UNIQUE (hero_id, skill_slot),
    CONSTRAINT uq_hero_skill_identity UNIQUE (hero_id, skill_id)
);

CREATE INDEX IF NOT EXISTS idx_hero_skills_hero ON hero_skills(hero_id);

CREATE TABLE IF NOT EXISTS hero_talents (
    hero_id VARCHAR(50) NOT NULL REFERENCES hero_definitions(hero_id) ON DELETE CASCADE,
    talent_id VARCHAR(50) NOT NULL,
    talent_name VARCHAR(100) NOT NULL,
    description TEXT NOT NULL DEFAULT '',
    effects JSONB NOT NULL DEFAULT '{}'::jsonb,
    PRIMARY KEY (hero_id, talent_id)
);

INSERT INTO hero_definitions (hero_id, hero_name, title, element, position, rarity, base_stats)
VALUES
    ('default_hero', 'Tân Binh', 'Tướng Mặc Định', 'NONE', 'MID', 'R', '{"hp_max":1000,"atk_phys":100,"atk_mag":0,"def_phys":50,"def_mag":30,"speed":100,"crit_rate":0.05,"crit_dmg":1.5,"dodge_rate":0.05,"block_rate":0.05,"counter_rate":0.0,"morale_max":100}'::jsonb),
    ('bang_thong', 'Bàng Thống', 'Phượng Sồ Tiên Sinh', 'FIRE', 'BACK', 'SSR', '{"hp_max":1580,"atk_phys":45,"atk_mag":235,"def_phys":75,"def_mag":130,"speed":108,"crit_rate":0.05,"crit_dmg":1.5,"dodge_rate":0.05,"block_rate":0.05,"counter_rate":0.0,"morale_max":100}'::jsonb),
    ('chu_du', 'Chu Du', 'Mỹ Chu Lang', 'FIRE', 'MID', 'SSR', '{"hp_max":1600,"atk_phys":60,"atk_mag":225,"def_phys":75,"def_mag":125,"speed":110,"crit_rate":0.16,"crit_dmg":1.5,"dodge_rate":0.05,"block_rate":0.05,"counter_rate":0.0,"morale_max":100}'::jsonb),
    ('dien_vi', 'Điển Vi', 'Cổ Chi Ác Lai', 'FIRE', 'FRONT', 'SSR', '{"hp_max":2600,"atk_phys":175,"atk_mag":0,"def_phys":145,"def_mag":75,"speed":92,"crit_rate":0.05,"crit_dmg":1.5,"dodge_rate":0.05,"block_rate":0.15,"counter_rate":0.25,"morale_max":100}'::jsonb),
    ('dieu_thuyen', 'Điêu Thuyền', 'Bế Nguyệt Mỹ Nhân', 'DARK', 'MID', 'SSR', '{"hp_max":1550,"atk_phys":50,"atk_mag":200,"def_phys":70,"def_mag":115,"speed":116,"crit_rate":0.05,"crit_dmg":1.5,"dodge_rate":0.16,"block_rate":0.05,"counter_rate":0.0,"morale_max":100}'::jsonb),
    ('gia_cat_luong', 'Gia Cát Lượng', 'Ngọa Long Tiên Sinh', 'WIND', 'BACK', 'UR', '{"hp_max":1650,"atk_phys":50,"atk_mag":250,"def_phys":80,"def_mag":140,"speed":112,"crit_rate":0.15,"crit_dmg":1.5,"dodge_rate":0.05,"block_rate":0.05,"counter_rate":0.0,"morale_max":100}'::jsonb),
    ('hoang_cai', 'Hoàng Cái', 'Khổ Nhục Tiên Phong', 'WATER', 'FRONT', 'SR', '{"hp_max":2300,"atk_phys":160,"atk_mag":0,"def_phys":125,"def_mag":85,"speed":94,"crit_rate":0.05,"crit_dmg":1.5,"dodge_rate":0.05,"block_rate":0.15,"counter_rate":0.0,"morale_max":100}'::jsonb),
    ('hoang_trung', 'Hoàng Trung', 'Lão Tướng Thần Xạ', 'LIGHT', 'MID', 'SSR', '{"hp_max":1650,"atk_phys":230,"atk_mag":0,"def_phys":80,"def_mag":80,"speed":106,"crit_rate":0.28,"crit_dmg":1.9,"dodge_rate":0.05,"block_rate":0.05,"counter_rate":0.0,"morale_max":100}'::jsonb),
    ('lu_bo', 'Lữ Bố', 'Chiến Thần Vô Song', 'DARK', 'MID', 'UR', '{"hp_max":2100,"atk_phys":265,"atk_mag":0,"def_phys":115,"def_mag":70,"speed":114,"crit_rate":0.25,"crit_dmg":1.85,"dodge_rate":0.05,"block_rate":0.05,"counter_rate":0.0,"morale_max":100}'::jsonb),
    ('ma_sieu', 'Mã Siêu', 'Cẩm Mã Siêu', 'WIND', 'MID', 'SSR', '{"hp_max":1850,"atk_phys":215,"atk_mag":0,"def_phys":95,"def_mag":80,"speed":120,"crit_rate":0.18,"crit_dmg":1.5,"dodge_rate":0.05,"block_rate":0.05,"counter_rate":0.0,"morale_max":100}'::jsonb),
    ('quan_vu', 'Quan Vũ', 'Mỹ Nhiễm Công', 'WIND', 'MID', 'UR', '{"hp_max":1950,"atk_phys":240,"atk_mag":0,"def_phys":110,"def_mag":85,"speed":108,"crit_rate":0.22,"crit_dmg":1.75,"dodge_rate":0.05,"block_rate":0.05,"counter_rate":0.0,"morale_max":100}'::jsonb),
    ('sai_van_co', 'Sái Văn Cơ', 'Tài Nữ Thi Ca', 'LIGHT', 'BACK', 'SR', '{"hp_max":1450,"atk_phys":40,"atk_mag":175,"def_phys":65,"def_mag":115,"speed":104,"crit_rate":0.05,"crit_dmg":1.5,"dodge_rate":0.05,"block_rate":0.05,"counter_rate":0.0,"morale_max":100}'::jsonb),
    ('tao_nhan', 'Tào Nhân', 'Thiên Nhân Chi Địch', 'LIGHT', 'FRONT', 'SSR', '{"hp_max":2800,"atk_phys":140,"atk_mag":0,"def_phys":160,"def_mag":110,"speed":88,"crit_rate":0.05,"crit_dmg":1.5,"dodge_rate":0.05,"block_rate":0.28,"counter_rate":0.0,"morale_max":100}'::jsonb),
    ('tieu_kieu', 'Tiểu Kiều', 'Giang Nam Đệ Nhất Kiều', 'WATER', 'BACK', 'SSR', '{"hp_max":1500,"atk_phys":40,"atk_mag":180,"def_phys":70,"def_mag":120,"speed":105,"crit_rate":0.05,"crit_dmg":1.5,"dodge_rate":0.05,"block_rate":0.05,"counter_rate":0.0,"morale_max":100}'::jsonb),
    ('trieu_van', 'Triệu Vân', 'Thường Sơn Triệu Tử Long', 'WATER', 'FRONT', 'UR', '{"hp_max":2200,"atk_phys":195,"atk_mag":0,"def_phys":120,"def_mag":90,"speed":118,"crit_rate":0.15,"crit_dmg":1.5,"dodge_rate":0.18,"block_rate":0.05,"counter_rate":0.0,"morale_max":100}'::jsonb),
    ('truong_phi', 'Trương Phi', 'Vạn Nhân Địch', 'FIRE', 'FRONT', 'SSR', '{"hp_max":2400,"atk_phys":185,"atk_mag":0,"def_phys":115,"def_mag":80,"speed":98,"crit_rate":0.12,"crit_dmg":1.5,"dodge_rate":0.05,"block_rate":0.05,"counter_rate":0.0,"morale_max":100}'::jsonb)
ON CONFLICT (hero_id) DO UPDATE SET
    hero_name = EXCLUDED.hero_name,
    title = EXCLUDED.title,
    element = EXCLUDED.element,
    position = EXCLUDED.position,
    rarity = EXCLUDED.rarity,
    base_stats = EXCLUDED.base_stats;

INSERT INTO hero_skills
    (hero_id, skill_id, skill_slot, skill_name, description, targeting_type,
     morale_cost, damage_multiplier, is_magic, target_count, effects)
VALUES
    ('bang_thong','bt_atk','NORMAL','Hỏa Long Chú','',1,0,1.0,true,1,'[]'::jsonb),
    ('bang_thong','bt_rage','RAGE','Liên Hoàn Hỏa Kế','',2,100,1.6,true,1,'[{"duration":3,"type":3,"val":85}]'::jsonb),
    ('chu_du','cd_atk','NORMAL','Hỏa Kiếm Vũ','',1,0,1.0,true,1,'[]'::jsonb),
    ('chu_du','cd_rage','RAGE','Đại Hỏa Xích Bích','',2,100,1.55,true,1,'[{"duration":3,"type":3,"val":90}]'::jsonb),
    ('dien_vi','dv_atk','NORMAL','Song Kích Trọng Kích','',0,0,1.0,false,1,'[]'::jsonb),
    ('dien_vi','dv_rage','RAGE','Hộ Chủ Quyết Tử','',0,100,2.2,false,1,'[{"duration":2,"type":7,"val":40}]'::jsonb),
    ('dieu_thuyen','dt_atk','NORMAL','Lụa Hồng Mê Hoặc','',1,0,1.0,true,1,'[]'::jsonb),
    ('dieu_thuyen','dt_rage','RAGE','Bế Nguyệt Tuyệt Sắc','',3,100,1.45,true,1,'[{"chance":0.6,"duration":2,"type":2}]'::jsonb),
    ('gia_cat_luong','gcl_atk','NORMAL','Bát Trận Đồ','',1,0,1.0,true,1,'[]'::jsonb),
    ('gia_cat_luong','gcl_rage','RAGE','Đông Phong Bát Trận','',2,100,1.65,true,1,'[{"chance":0.4,"duration":1,"type":1}]'::jsonb),
    ('hoang_cai','hc_atk','NORMAL','Thiết Tiết Kích','',0,0,1.0,false,1,'[]'::jsonb),
    ('hoang_cai','hc_rage','RAGE','Khổ Nhục Hỏa Thuyền','',3,100,1.7,false,1,'[{"duration":2,"type":3,"val":80}]'::jsonb),
    ('hoang_trung','ht_atk','NORMAL','Tiễn Tiệt Không','',1,0,1.05,false,1,'[]'::jsonb),
    ('hoang_trung','ht_rage','RAGE','Bách Bộ Xuyên Dương','',5,100,2.6,false,1,'[{"duration":2,"type":10,"val":30}]'::jsonb),
    ('lu_bo','lb_atk','NORMAL','Phương Thiên Họa Kích','',0,0,1.15,false,1,'[]'::jsonb),
    ('lu_bo','lb_rage','RAGE','Vô Song Chiến Thần','',2,100,1.85,false,1,'[{"chance":0.35,"duration":1,"type":1}]'::jsonb),
    ('ma_sieu','ms_atk','NORMAL','Tây Lương Mã Đao','',0,0,1.05,false,1,'[]'::jsonb),
    ('ma_sieu','ms_rage','RAGE','Thần Uy Đột Kích','',5,100,2.3,false,1,'[{"duration":2,"type":9,"val":25}]'::jsonb),
    ('quan_vu','qv_atk','NORMAL','Thanh Long Yển Nguyệt','',0,0,1.1,false,1,'[]'::jsonb),
    ('quan_vu','qv_rage','RAGE','Qua Ngũ Quan Trảm Tướng','',0,100,2.8,false,1,'[{"duration":2,"type":10,"val":35}]'::jsonb),
    ('sai_van_co','svc_atk','NORMAL','Hồ Gia Khúc','',7,0,0.95,true,1,'[]'::jsonb),
    ('sai_van_co','svc_rage','RAGE','Thập Bát Phách Linh','',7,100,1.5,true,5,'[{"duration":2,"type":7,"val":25}]'::jsonb),
    ('tao_nhan','tn_atk','NORMAL','Kim Cương Kích','',0,0,0.9,false,1,'[]'::jsonb),
    ('tao_nhan','tn_rage','RAGE','Trầm Lực Thiết Bích','',6,100,0.0,false,1,'[{"duration":2,"type":11,"val":500}]'::jsonb),
    ('tieu_kieu','tk_atk','NORMAL','Khúc Nhạc Hồi Sinh','',7,0,1.0,true,1,'[]'::jsonb),
    ('tieu_kieu','tk_rage','RAGE','Điệu Vũ Thiên Kiều','',7,100,1.6,true,5,'[{"duration":2,"type":6,"val":25}]'::jsonb),
    ('trieu_van','tv_atk','NORMAL','Bạch Long Thương','',0,0,1.0,false,1,'[]'::jsonb),
    ('trieu_van','tv_rage','RAGE','Long Đảm Thần Uy','',3,100,1.6,false,1,'[{"duration":2,"type":8,"val":25}]'::jsonb),
    ('truong_phi','tp_atk','NORMAL','Bát Xà Mâu Trảm','',0,0,1.0,false,1,'[]'::jsonb),
    ('truong_phi','tp_rage','RAGE','Nộ Hống Bản Cầu','',3,100,1.4,false,1,'[{"chance":0.45,"duration":1,"type":1}]'::jsonb)
ON CONFLICT (skill_id) DO UPDATE SET
    hero_id = EXCLUDED.hero_id,
    skill_slot = EXCLUDED.skill_slot,
    skill_name = EXCLUDED.skill_name,
    description = EXCLUDED.description,
    targeting_type = EXCLUDED.targeting_type,
    morale_cost = EXCLUDED.morale_cost,
    damage_multiplier = EXCLUDED.damage_multiplier,
    is_magic = EXCLUDED.is_magic,
    target_count = EXCLUDED.target_count,
    effects = EXCLUDED.effects;

INSERT INTO hero_talents (hero_id, talent_id, talent_name)
VALUES
    ('lu_bo','vo_song','Võ Song'),
    ('lu_bo','no_hoa','Nộ Hỏa'),
    ('quan_vu','nghia_khi','Nghĩa Khí'),
    ('quan_vu','yem_nguyet','Yểm Nguyệt'),
    ('trieu_van','long_dam','Long Đảm'),
    ('gia_cat_luong','dong_phong','Đông Phong'),
    ('gia_cat_luong','bat_tran','Bát Trận'),
    ('chu_du','hoa_thao','Hỏa Thao'),
    ('dieu_thuyen','be_nguyet','Bế Nguyệt'),
    ('dien_vi','hung_dung','Hùng Dũng'),
    ('tao_nhan','thiet_bich','Thiết Bích')
ON CONFLICT (hero_id, talent_id) DO UPDATE SET talent_name = EXCLUDED.talent_name;
