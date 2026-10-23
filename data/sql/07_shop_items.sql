-- ============================================================
-- 07_shop_items.sql - Shop catalog (Phase 4)
-- Server is the only source of truth for prices and grants.
-- grant_type: STAMINA (instant stamina), SILVER (instant silver),
--             ITEM (adds grant_item_id to user_inventories)
-- ============================================================

CREATE TABLE IF NOT EXISTS shop_items (
    item_id VARCHAR(50) PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    description VARCHAR(255) NOT NULL DEFAULT '',
    currency VARCHAR(10) NOT NULL CHECK (currency IN ('silver', 'gold')),
    price INT NOT NULL CHECK (price > 0),
    grant_type VARCHAR(20) NOT NULL CHECK (grant_type IN ('STAMINA', 'SILVER', 'ITEM')),
    grant_amount INT NOT NULL DEFAULT 0,
    grant_item_id VARCHAR(50),
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

INSERT INTO shop_items (item_id, name, description, currency, price, grant_type, grant_amount, grant_item_id)
VALUES
    ('the_luc_dan',  'Thể Lực Đan',      'Hồi ngay 30 Thể Lực',                    'gold',   20, 'STAMINA', 30,   NULL),
    ('the_luc_dai',  'Đại Thể Lực Đan',  'Hồi ngay 120 Thể Lực',                   'gold',   70, 'STAMINA', 120,  NULL),
    ('tui_bac_lon',  'Túi Bạc Lớn',      'Nhận ngay 5.000 Bạc',                    'gold',   45, 'SILVER',  5000, NULL),
    ('sach_exp',     'Sách Kinh Nghiệm', 'Vào Túi Đồ: dùng cho 1 tướng +300 EXP',  'silver', 400, 'ITEM',    1,    'sach_exp')
ON CONFLICT (item_id) DO UPDATE SET
    name = EXCLUDED.name,
    description = EXCLUDED.description,
    currency = EXCLUDED.currency,
    price = EXCLUDED.price,
    grant_type = EXCLUDED.grant_type,
    grant_amount = EXCLUDED.grant_amount,
    grant_item_id = EXCLUDED.grant_item_id,
    is_active = TRUE;
