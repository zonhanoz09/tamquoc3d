#!/usr/bin/env python3
"""
Pixel Art Asset Generator for Project Pixel Tactics (Tam Quoc 3D)
Generates retro 16-bit / HD-2D style sprites for 15 heroes, battleground, VFX and UI.
"""

import os
import math
from PIL import Image, ImageDraw

BASE_DIR = "/home/annhan/Documents/src/tamquoc3d/assets/sprites"
HERO_DIR = os.path.join(BASE_DIR, "heroes")
PORTRAIT_DIR = os.path.join(BASE_DIR, "portraits")
VFX_DIR = os.path.join(BASE_DIR, "vfx")
UI_DIR = os.path.join(BASE_DIR, "ui")
BG_DIR = os.path.join(BASE_DIR, "backgrounds")

for d in [HERO_DIR, PORTRAIT_DIR, VFX_DIR, UI_DIR, BG_DIR]:
    os.makedirs(d, exist_ok=True)

# ─────────────────────────────────────────────────────────────────────────────
# 1. HERO COLOR SCHEMES & CHARACTERISTICS (15 Heroes)
# ─────────────────────────────────────────────────────────────────────────────
HEROES = {
    # TIỀN QUÂN
    "trieu_van": {
        "name": "Triệu Vân",
        "armor": (210, 225, 245),      # Bạch ngân giáp
        "cloak": (50, 120, 220),       # Áo choàng xanh biển
        "skin": (255, 220, 190),
        "hair": (35, 30, 45),          # Tóc đen buộc cao
        "weapon": (240, 245, 255),     # Bạch Long thương
        "weapon_type": "spear",
        "helm": (180, 200, 230),
        "aura": (100, 200, 255),
    },
    "dien_vi": {
        "name": "Điển Vi",
        "armor": (80, 50, 45),         # Thiết giáp đen nặng
        "cloak": (190, 40, 30),        # Vải đỏ hung
        "skin": (220, 175, 145),       # Da sạm cơ bắp
        "hair": (25, 20, 20),          # Tóc ngắn râu quai nón
        "weapon": (160, 160, 170),     # Song thiết kích
        "weapon_type": "dual_axe",
        "helm": (110, 90, 85),
        "aura": (255, 100, 50),
    },
    "truong_phi": {
        "name": "Trương Phi",
        "armor": (60, 70, 65),         # Giáp hắc thiết
        "cloak": (170, 40, 40),        # Khăn choàng huyết xích
        "skin": (195, 150, 120),       # Mặt đen râu hùm
        "hair": (15, 15, 20),          # Râu hùm xòe rộng
        "weapon": (180, 185, 200),     # Bát Xà Mâu uốn lượn
        "weapon_type": "snake_spear",
        "helm": (90, 70, 50),
        "aura": (255, 140, 30),
    },
    "tao_nhan": {
        "name": "Tào Nhân",
        "armor": (140, 150, 165),      # Giáp thành đồng
        "cloak": (120, 110, 80),       # Áo nâu đất
        "skin": (240, 200, 170),
        "hair": (40, 35, 35),
        "weapon": (190, 195, 210),     # Đại khiên kim loại + chùy
        "weapon_type": "shield_mace",
        "helm": (160, 170, 185),
        "aura": (220, 200, 150),
    },
    "hoang_cai": {
        "name": "Hoàng Cái",
        "armor": (85, 115, 130),       # Giáp thủy binh xanh sẫm
        "cloak": (210, 120, 50),       # Áo choàng màu lửa
        "skin": (215, 170, 140),
        "hair": (210, 210, 220),       # Râu tóc bạc phơ
        "weapon": (150, 155, 160),     # Thiết tiết / đao ngắn
        "weapon_type": "iron_whip",
        "helm": (100, 120, 135),
        "aura": (70, 170, 220),
    },

    # TRUNG QUÂN
    "quan_vu": {
        "name": "Quan Vũ",
        "armor": (40, 130, 75),        # Xanh lục bảo (Lục bào)
        "cloak": (30, 95, 55),
        "skin": (205, 80, 70),         # Mặt đỏ như gấc
        "hair": (20, 20, 20),          # Râu dài chấm ngực
        "weapon": (220, 230, 240),     # Thanh Long Yển Nguyệt Đao
        "weapon_type": "glaive",
        "helm": (50, 150, 90),         # Khăn lục
        "aura": (80, 230, 120),
    },
    "lu_bo": {
        "name": "Lữ Bố",
        "armor": (60, 50, 50),         # Hắc kim giáp viền vàng
        "cloak": (200, 30, 40),        # Hồng bào rực cháy
        "skin": (250, 210, 180),
        "hair": (30, 25, 25),
        "weapon": (230, 210, 140),     # Phương Thiên Họa Kích
        "weapon_type": "halberd",
        "helm": (210, 60, 60),         # Mũ kim khôi 2 dải lông trĩ đỏ dài
        "aura": (255, 50, 70),
    },
    "chu_du": {
        "name": "Chu Du",
        "armor": (230, 230, 240),      # Bạch y tướng quân
        "cloak": (215, 45, 50),        # Áo choàng đỏ rực
        "skin": (255, 225, 200),       # Tuấn tú hào hoa
        "hair": (35, 30, 35),
        "weapon": (240, 180, 70),      # Bảo kiếm nạm hồng ngọc
        "weapon_type": "sword",
        "helm": (200, 200, 220),       # Khăn bạch ngân
        "aura": (255, 120, 40),
    },
    "hoang_trung": {
        "name": "Hoàng Trung",
        "armor": (170, 140, 60),       # Kim giáp lão tướng
        "cloak": (60, 110, 80),        # Áo xanh lục
        "skin": (225, 185, 155),
        "hair": (225, 225, 230),       # Tóc bạc, râu bạc dài
        "weapon": (150, 100, 50),      # Đại cung trường xạ
        "weapon_type": "bow",
        "helm": (190, 160, 70),
        "aura": (230, 200, 80),
    },
    "ma_sieu": {
        "name": "Mã Siêu",
        "armor": (220, 225, 235),      # Sư tử ngân giáp
        "cloak": (240, 240, 245),      # Bạch bào Tây Lương
        "skin": (250, 215, 185),       # Mặt ngọc môi son
        "hair": (40, 35, 45),
        "weapon": (210, 220, 240),     # Hổ đầu trạm kim thương
        "weapon_type": "spear",
        "helm": (230, 235, 245),       # Mũ sư tử bạc
        "aura": (180, 220, 255),
    },
    "dieu_thuyen": {
        "name": "Điêu Thuyền",
        "armor": (245, 160, 185),      # Váy lụa hồng đào
        "cloak": (230, 120, 160),      # Dải lụa múa phiêu dật
        "skin": (255, 230, 215),       # Da trắng tuyệt sắc
        "hair": (30, 25, 35),          # Tóc búi bế nguyệt kèm trâm hoa
        "weapon": (255, 200, 220),     # Dải lụa tiên vũ
        "weapon_type": "ribbon",
        "helm": (255, 140, 170),
        "aura": (255, 130, 200),
    },

    # HẬU QUÂN
    "gia_cat_luong": {
        "name": "Gia Cát Lượng",
        "armor": (45, 70, 130),        # Bát quái đạo bào lam
        "cloak": (235, 240, 245),      # Áo choàng nguyệt bạch
        "skin": (255, 225, 195),
        "hair": (35, 30, 40),          # Khăn Luân Cân
        "weapon": (235, 225, 190),     # Quạt lông vũ Hạc Vũ Phiến
        "weapon_type": "fan",
        "helm": (60, 80, 140),         # Mũ quan đạo gia
        "aura": (120, 180, 255),
    },
    "tieu_kieu": {
        "name": "Tiểu Kiều",
        "armor": (130, 210, 200),      # Váy xanh ngọc bích
        "cloak": (245, 210, 225),      # Áo khoác hồng phấn nhẹ
        "skin": (255, 230, 215),       # Nhí nhảnh dễ thương
        "hair": (45, 35, 30),          # Tóc hai búi hoa mai
        "weapon": (250, 180, 190),     # Quạt hoa đào
        "weapon_type": "flower_fan",
        "helm": (255, 190, 210),
        "aura": (100, 235, 210),
    },
    "bang_thong": {
        "name": "Bàng Thống",
        "armor": (120, 50, 40),        # Đạo bào đỏ thẫm
        "cloak": (180, 90, 40),        # Khăn choàng Phượng sồ
        "skin": (210, 170, 135),
        "hair": (30, 25, 25),          # Nón rộng vành lãng tử
        "weapon": (160, 90, 50),       # Trượng gỗ phong ấn lửa
        "weapon_type": "staff",
        "helm": (140, 70, 40),
        "aura": (255, 130, 50),
    },
    "sai_van_co": {
        "name": "Sái Văn Cơ",
        "armor": (90, 140, 180),       # Áo dài xanh lam nho nhã
        "cloak": (215, 225, 240),      # Khăn choàng tuyết trắng
        "skin": (255, 228, 210),       # Dịu dàng đoan trang
        "hair": (35, 30, 40),          # Tóc buông cài trâm
        "weapon": (150, 100, 65),      # Cổ Cầm 7 dây
        "weapon_type": "zither",
        "helm": (130, 170, 210),
        "aura": (140, 210, 255),
    },
}

# ─────────────────────────────────────────────────────────────────────────────
# 2. GENERATE HERO SPRITESHEET (48x48 per frame, 4 frames: Idle1, Idle2, Atk, Hurt)
# Total image size: 192 x 48 px
# ─────────────────────────────────────────────────────────────────────────────
FRAME_W = 48
FRAME_H = 48

def draw_hero_frame(draw, ox, oy, h_info, state="idle", tick=0):
    """
    Draw a single 48x48 hero frame at offset (ox, oy).
    States: 'idle' (tick 0 or 1), 'attack', 'hurt'
    """
    skin = h_info["skin"]
    armor = h_info["armor"]
    cloak = h_info["cloak"]
    hair = h_info["hair"]
    weap = h_info["weapon"]
    helm = h_info["helm"]
    wtype = h_info["weapon_type"]

    # Base coords
    bx = ox + 24
    by = oy + 42

    # Animation offsets
    head_y_off = 0
    body_x_off = 0
    body_y_off = 0
    arm_x_off = 0
    arm_y_off = 0
    flash_white = False

    if state == "idle":
        if tick == 1:
            body_y_off = 1
            head_y_off = 1
    elif state == "attack":
        body_x_off = 4
        arm_x_off = 6
        arm_y_off = -2
    elif state == "hurt":
        body_x_off = -3
        head_y_off = -1
        flash_white = True

    # Shadow
    shadow_col = (0, 0, 0, 80)
    draw.ellipse([bx - 10 + body_x_off, by - 2, bx + 10 + body_x_off, by + 4], fill=shadow_col)

    # Cloak (Back)
    c_col = (255, 255, 255) if flash_white else cloak
    draw.rectangle([bx - 8 + body_x_off, by - 24 + body_y_off, bx + 8 + body_x_off, by - 4], fill=c_col)
    # Cloak folds
    c_dark = (max(0, c_col[0] - 40), max(0, c_col[1] - 40), max(0, c_col[2] - 40))
    draw.line([bx - 4 + body_x_off, by - 22 + body_y_off, bx - 4 + body_x_off, by - 4], fill=c_dark)
    draw.line([bx + 4 + body_x_off, by - 22 + body_y_off, bx + 4 + body_x_off, by - 4], fill=c_dark)

    # Legs & Boots
    boot_col = (50, 45, 55) if not flash_white else (255, 255, 255)
    draw.rectangle([bx - 6 + body_x_off, by - 10 + body_y_off, bx - 2 + body_x_off, by], fill=boot_col)
    draw.rectangle([bx + 2 + body_x_off, by - 10 + body_y_off, bx + 6 + body_x_off, by], fill=boot_col)

    # Torso Armor
    a_col = (255, 255, 255) if flash_white else armor
    a_dark = (max(0, a_col[0] - 40), max(0, a_col[1] - 40), max(0, a_col[2] - 40))
    draw.rectangle([bx - 7 + body_x_off, by - 24 + body_y_off, bx + 7 + body_x_off, by - 10 + body_y_off], fill=a_col)
    draw.rectangle([bx - 5 + body_x_off, by - 22 + body_y_off, bx + 5 + body_x_off, by - 12 + body_y_off], fill=a_dark)
    # Belt / Waist
    draw.rectangle([bx - 7 + body_x_off, by - 12 + body_y_off, bx + 7 + body_x_off, by - 9 + body_y_off], fill=(220, 180, 70))

    # Shoulder pads
    draw.rectangle([bx - 10 + body_x_off, by - 25 + body_y_off, bx - 6 + body_x_off, by - 20 + body_y_off], fill=a_col)
    draw.rectangle([bx + 6 + body_x_off, by - 25 + body_y_off, bx + 10 + body_x_off, by - 20 + body_y_off], fill=a_col)

    # Head & Neck
    s_col = (255, 255, 255) if flash_white else skin
    draw.rectangle([bx - 2 + body_x_off, by - 27 + head_y_off, bx + 2 + body_x_off, by - 24 + head_y_off], fill=s_col)
    draw.rectangle([bx - 6 + body_x_off, by - 37 + head_y_off, bx + 6 + body_x_off, by - 27 + head_y_off], fill=s_col)

    # Hair / Helmet
    h_col = (255, 255, 255) if flash_white else helm
    hr_col = (255, 255, 255) if flash_white else hair
    # Top helmet
    draw.rectangle([bx - 7 + body_x_off, by - 41 + head_y_off, bx + 7 + body_x_off, by - 35 + head_y_off], fill=h_col)
    draw.rectangle([bx - 2 + body_x_off, by - 44 + head_y_off, bx + 2 + body_x_off, by - 41 + head_y_off], fill=(220, 180, 70))
    # Hair sides
    draw.rectangle([bx - 7 + body_x_off, by - 36 + head_y_off, bx - 5 + body_x_off, by - 28 + head_y_off], fill=hr_col)
    draw.rectangle([bx + 5 + body_x_off, by - 36 + head_y_off, bx + 7 + body_x_off, by - 28 + head_y_off], fill=hr_col)

    # Lữ Bố special: Two tall red pheasant feathers
    if "lu_bo" in h_info.get("name_id", "") or "Lữ Bố" in h_info["name"]:
        draw.line([bx - 2 + body_x_off, by - 42 + head_y_off, bx - 8 + body_x_off, by - 52 + head_y_off], fill=(220, 40, 40), width=2)
        draw.line([bx + 2 + body_x_off, by - 42 + head_y_off, bx + 8 + body_x_off, by - 52 + head_y_off], fill=(220, 40, 40), width=2)

    # Quan Vũ special: Long Beard
    if "Quan Vũ" in h_info["name"]:
        draw.rectangle([bx - 3 + body_x_off, by - 29 + head_y_off, bx + 3 + body_x_off, by - 16 + head_y_off], fill=(20, 20, 20))

    # Eyes
    eye_col = (30, 20, 40) if not flash_white else (255, 255, 255)
    draw.point([bx + 1 + body_x_off, by - 32 + head_y_off], fill=eye_col)
    draw.point([bx + 4 + body_x_off, by - 32 + head_y_off], fill=eye_col)

    # Arms & Weapon
    w_col = (255, 255, 255) if flash_white else weap
    wx = bx + 9 + body_x_off + arm_x_off
    wy = by - 19 + body_y_off + arm_y_off

    # Right hand holding weapon
    draw.rectangle([wx - 2, wy - 2, wx + 2, wy + 2], fill=s_col)

    # Draw Weapon types
    if wtype in ["spear", "snake_spear"]:
        # Pole
        draw.line([wx - 4, wy + 16, wx + 6, wy - 22], fill=(120, 80, 50), width=2)
        # Spearhead
        draw.polygon([(wx + 6, wy - 26), (wx + 9, wy - 20), (wx + 3, wy - 20)], fill=w_col)
        draw.point([wx + 6, wy - 19], fill=(220, 40, 40)) # Tassel
    elif wtype == "glaive":
        # Green Dragon Crescent Blade
        draw.line([wx - 6, wy + 18, wx + 4, wy - 20], fill=(100, 70, 40), width=2)
        draw.polygon([(wx + 4, wy - 28), (wx + 12, wy - 20), (wx + 2, wy - 14)], fill=w_col)
        draw.polygon([(wx + 8, wy - 24), (wx + 10, wy - 16), (wx + 4, wy - 18)], fill=(120, 210, 160))
    elif wtype == "halberd":
        # Lu Bo Halberd
        draw.line([wx - 4, wy + 18, wx + 6, wy - 24], fill=(70, 70, 70), width=2)
        draw.polygon([(wx + 6, wy - 30), (wx + 10, wy - 22), (wx + 2, wy - 22)], fill=w_col)
        draw.line([wx - 1, wy - 20, wx + 13, wy - 20], fill=(220, 180, 70), width=2)
        draw.polygon([(wx + 13, wy - 24), (wx + 16, wy - 20), (wx + 13, wy - 16)], fill=w_col)
    elif wtype == "dual_axe":
        draw.line([wx, wy + 10, wx + 2, wy - 12], fill=(90, 60, 40), width=2)
        draw.polygon([(wx - 5, wy - 12), (wx + 7, wy - 14), (wx + 2, wy - 6)], fill=w_col)
    elif wtype == "sword":
        draw.line([wx, wy + 6, wx + 12, wy - 16], fill=w_col, width=2)
        draw.line([wx - 2, wy + 3, wx + 3, wy + 8], fill=(220, 180, 60), width=2)
    elif wtype == "bow":
        draw.arc([wx - 2, wy - 20, wx + 14, wy + 16], 270, 90, fill=(130, 80, 40), width=2)
        draw.line([wx + 6, wy - 20, wx + 6, wy + 16], fill=(220, 220, 220))
    elif wtype in ["fan", "flower_fan"]:
        # Fan
        draw.polygon([(wx, wy), (wx + 10, wy - 12), (wx + 14, wy - 4)], fill=w_col)
        draw.line([wx, wy, wx + 12, wy - 8], fill=(220, 190, 100))
    elif wtype == "zither":
        # Guqin
        draw.rectangle([wx - 4, wy - 12, wx + 10, wy + 4], fill=(130, 75, 45))
        draw.line([wx - 2, wy - 10, wx + 8, wy - 10], fill=(230, 230, 240))
        draw.line([wx - 2, wy - 6, wx + 8, wy - 6], fill=(230, 230, 240))
        draw.line([wx - 2, wy - 2, wx + 8, wy - 2], fill=(230, 230, 240))
    elif wtype == "shield_mace":
        # Shield
        draw.polygon([(wx - 8, wy - 14), (wx + 4, wy - 14), (wx + 2, wy + 6), (wx - 6, wy + 6)], fill=(160, 175, 195))
        draw.rectangle([wx - 4, wy - 8, wx, wy], fill=(220, 190, 70))
    else:
        # Default ribbon/magic
        draw.arc([wx - 4, wy - 14, wx + 12, wy + 10], 0, 180, fill=w_col, width=2)

def generate_hero_sprites():
    for hid, info in HEROES.items():
        info["name_id"] = hid
        img = Image.new("RGBA", (FRAME_W * 4, FRAME_H), (0, 0, 0, 0))
        draw = ImageDraw.Draw(img)

        # Frame 0: Idle 1
        draw_hero_frame(draw, 0, 0, info, state="idle", tick=0)
        # Frame 1: Idle 2 (breathing bob)
        draw_hero_frame(draw, FRAME_W, 0, info, state="idle", tick=1)
        # Frame 2: Attack (forward thrust)
        draw_hero_frame(draw, FRAME_W * 2, 0, info, state="attack")
        # Frame 3: Hurt (knockback flash)
        draw_hero_frame(draw, FRAME_W * 3, 0, info, state="hurt")

        out_path = os.path.join(HERO_DIR, f"{hid}.png")
        img.save(out_path)

        # Generate Portrait (48x48)
        p_img = Image.new("RGBA", (48, 48), (25, 20, 35, 255))
        p_draw = ImageDraw.Draw(p_img)

        # Gold frame border
        p_draw.rectangle([0, 0, 47, 47], outline=(220, 180, 70), width=2)
        p_draw.rectangle([2, 2, 45, 45], outline=(60, 45, 80), width=1)

        # Draw close up head/chest
        draw_hero_frame(p_draw, 0, 10, info, state="idle", tick=0)

        p_out = os.path.join(PORTRAIT_DIR, f"{hid}.png")
        p_img.save(p_out)

    print(f"Generated {len(HEROES)} hero spritesheets and portraits.")

# ─────────────────────────────────────────────────────────────────────────────
# 3. GENERATE BATTLEFIELD BACKGROUND (1280 x 720)
# ─────────────────────────────────────────────────────────────────────────────
def generate_background():
    w, h = 1280, 720
    bg = Image.new("RGBA", (w, h))
    draw = ImageDraw.Draw(bg)

    # 1. Sky Gradient (Dusk / Twilight Tam Quoc battlefield)
    sky_top = (35, 25, 60)
    sky_mid = (160, 65, 55)
    sky_bottom = (240, 140, 60)

    for y in range(360):
        t = y / 360.0
        if t < 0.5:
            k = t * 2.0
            r = int(sky_top[0] * (1 - k) + sky_mid[0] * k)
            g = int(sky_top[1] * (1 - k) + sky_mid[1] * k)
            b = int(sky_top[2] * (1 - k) + sky_mid[2] * k)
        else:
            k = (t - 0.5) * 2.0
            r = int(sky_mid[0] * (1 - k) + sky_bottom[0] * k)
            g = int(sky_mid[1] * (1 - k) + sky_bottom[1] * k)
            b = int(sky_mid[2] * (1 - k) + sky_bottom[2] * k)
        draw.line([(0, y), (w, y)], fill=(r, g, b))

    # Sun / Moon (Blood sun)
    draw.ellipse([640 - 50, 200 - 50, 640 + 50, 200 + 50], fill=(255, 200, 120, 220))
    draw.ellipse([640 - 44, 200 - 44, 640 + 44, 200 + 44], fill=(255, 235, 170, 240))

    # 2. Distant Mountains (2 layers)
    # Far Mountains
    far_pts = [(0, 360)]
    for x in range(0, w + 40, 40):
        my = 260 + int(math.sin(x * 0.008) * 35 + math.cos(x * 0.02) * 20)
        far_pts.append((x, my))
    far_pts.append((w, 360))
    draw.polygon(far_pts, fill=(90, 45, 65))

    # Mid Mountains & Ancient Watchtowers
    mid_pts = [(0, 360)]
    for x in range(0, w + 30, 30):
        my = 290 + int(math.sin(x * 0.015) * 25 + math.cos(x * 0.035) * 15)
        mid_pts.append((x, my))
    mid_pts.append((w, 360))
    draw.polygon(mid_pts, fill=(60, 35, 55))

    # Distant Fortress Walls & Watchtower roofs (Hổ Lao Quan silhouette)
    for bx in [200, 600, 1000]:
        draw.rectangle([bx, 280, bx + 80, 360], fill=(45, 25, 40))
        # Pagoda roof
        draw.polygon([(bx - 10, 285), (bx + 40, 260), (bx + 90, 285)], fill=(35, 20, 30))
        # Flagpole
        draw.line([(bx + 40, 260), (bx + 40, 240)], fill=(30, 20, 30), width=2)
        draw.polygon([(bx + 40, 240), (bx + 60, 248), (bx + 40, 256)], fill=(180, 40, 40))

    # 3. Ground / Battlefield Terrain (y = 360 to 720)
    ground_top = (110, 85, 60)
    ground_bot = (45, 30, 25)
    for y in range(360, h):
        t = (y - 360) / 360.0
        r = int(ground_top[0] * (1 - t) + ground_bot[0] * t)
        g = int(ground_top[1] * (1 - t) + ground_bot[1] * t)
        b = int(ground_top[2] * (1 - t) + ground_bot[2] * t)
        draw.line([(0, y), (w, y)], fill=(r, g, b))

    # Stone pavers / Battlefield grid tiles perspective
    tile_line_col = (90, 65, 45, 120)
    # Horizontal lines with increasing spacing
    y_lines = [380, 410, 450, 500, 560, 630, 710]
    for yl in y_lines:
        draw.line([(0, yl), (w, yl)], fill=tile_line_col, width=2)

    # Perspective vertical converging lines
    for xp in range(0, w + 120, 120):
        # Vanishing point near center (640, 350)
        draw.line([(640 + (xp - 640) * 0.25, 360), (xp, 720)], fill=tile_line_col, width=2)

    # 4. Tactical Formation Markings (Subtle gold / bronze runes for 3 rows)
    # Player side (Left: Back, Mid, Front)
    # Enemy side (Right: Front, Mid, Back)
    rows_info = [
        ("HẬU QUÂN", 220, 470, (80, 140, 220, 70)),
        ("TRUNG QUÂN", 380, 520, (80, 200, 120, 70)),
        ("TIỀN QUÂN", 530, 570, (230, 180, 50, 70)),
        ("TIỀN QUÂN", 750, 570, (230, 180, 50, 70)),
        ("TRUNG QUÂN", 900, 520, (220, 100, 80, 70)),
        ("HẬU QUÂN", 1060, 470, (180, 80, 180, 70)),
    ]
    for label, rx, ry, col in rows_info:
        # Subtle tactical circle pads where heroes stand
        draw.ellipse([rx - 55, ry - 18, rx + 55, ry + 18], fill=col)
        draw.ellipse([rx - 55, ry - 18, rx + 55, ry + 18], outline=(col[0], col[1], col[2], 140), width=2)

    # 5. War Banners & Battlefield Torches
    # Left Army Banner (Thục / Lưu)
    draw.line([(90, 340), (90, 550)], fill=(70, 45, 30), width=4)
    draw.polygon([(90, 360), (160, 390), (90, 430)], fill=(200, 40, 40))
    draw.polygon([(100, 375), (145, 395), (100, 415)], fill=(230, 190, 60))

    # Right Army Banner (Ngụy / Tào)
    draw.line([(1190, 340), (1190, 550)], fill=(70, 45, 30), width=4)
    draw.polygon([(1190, 360), (1120, 390), (1190, 430)], fill=(40, 70, 160))
    draw.polygon([(1180, 375), (1135, 395), (1180, 415)], fill=(220, 190, 70))

    # Torches with fire
    for tx, ty in [(60, 480), (1220, 480)]:
        draw.line([(tx, ty), (tx, ty + 60)], fill=(80, 50, 30), width=3)
        draw.ellipse([tx - 8, ty - 14, tx + 8, ty + 2], fill=(255, 120, 30))
        draw.ellipse([tx - 5, ty - 10, tx + 5, ty], fill=(255, 230, 80))

    out_file = os.path.join(BG_DIR, "battlefield.png")
    bg.save(out_file)
    print("Generated battlefield.png (1280x720).")

# ─────────────────────────────────────────────────────────────────────────────
# 4. GENERATE COMBAT VFX SPRITES
# ─────────────────────────────────────────────────────────────────────────────
def generate_vfx():
    # 1. Slash arc (64x64)
    slash = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    s_draw = ImageDraw.Draw(slash)
    s_draw.arc([8, 8, 56, 56], 190, 340, fill=(255, 255, 255), width=5)
    s_draw.arc([10, 10, 54, 54], 195, 335, fill=(120, 220, 255), width=3)
    s_draw.polygon([(52, 22), (60, 14), (54, 30)], fill=(255, 255, 255))
    slash.save(os.path.join(VFX_DIR, "slash.png"))

    # 2. Fire Explosion (64x64)
    fire = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    f_draw = ImageDraw.Draw(fire)
    f_draw.ellipse([12, 12, 52, 52], fill=(220, 50, 20, 200))
    f_draw.ellipse([18, 18, 46, 46], fill=(255, 140, 30, 230))
    f_draw.ellipse([24, 24, 40, 40], fill=(255, 240, 100, 255))
    # Sparks radiating
    for deg in range(0, 360, 45):
        rad = math.radians(deg)
        x1 = 32 + int(math.cos(rad) * 22)
        y1 = 32 + int(math.sin(rad) * 22)
        x2 = 32 + int(math.cos(rad) * 30)
        y2 = 32 + int(math.sin(rad) * 30)
        f_draw.line([(x1, y1), (x2, y2)], fill=(255, 200, 50), width=2)
    fire.save(os.path.join(VFX_DIR, "fire.png"))

    # 3. Wind Whirlwind (64x64)
    wind = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    w_draw = ImageDraw.Draw(wind)
    w_draw.arc([8, 14, 56, 38], 0, 180, fill=(180, 255, 200), width=3)
    w_draw.arc([14, 24, 50, 44], 180, 360, fill=(120, 240, 180), width=3)
    w_draw.arc([20, 34, 44, 50], 0, 180, fill=(220, 255, 230), width=2)
    wind.save(os.path.join(VFX_DIR, "wind.png"))

    # 4. Water / Heal Sparkle (64x64)
    water = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    wat_draw = ImageDraw.Draw(water)
    # Cross star / sparkles
    wat_draw.polygon([(32, 8), (36, 26), (54, 32), (36, 38), (32, 56), (28, 38), (10, 32), (28, 26)], fill=(100, 240, 180))
    wat_draw.polygon([(32, 16), (34, 28), (46, 32), (34, 36), (32, 48), (30, 36), (18, 32), (30, 28)], fill=(230, 255, 245))
    water.save(os.path.join(VFX_DIR, "water.png"))

    # 5. Crit Impact Star (64x64)
    crit = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    c_draw = ImageDraw.Draw(crit)
    # 8-point golden/red star
    for i in range(8):
        a1 = i * (math.pi / 4)
        a2 = a1 + math.pi / 8
        r1 = 28 if i % 2 == 0 else 18
        r2 = 8
        p1 = (32 + math.cos(a1) * r1, 32 + math.sin(a1) * r1)
        p2 = (32 + math.cos(a2) * r2, 32 + math.sin(a2) * r2)
        c_draw.polygon([(32, 32), p1, p2], fill=(255, 220, 50))
    c_draw.ellipse([26, 26, 38, 38], fill=(255, 50, 50))
    crit.save(os.path.join(VFX_DIR, "impact.png"))

    # 6. Rage Aura Ring (96x96)
    aura = Image.new("RGBA", (96, 96), (0, 0, 0, 0))
    a_draw = ImageDraw.Draw(aura)
    a_draw.ellipse([8, 24, 88, 72], outline=(255, 180, 40, 180), width=4)
    a_draw.ellipse([14, 28, 82, 68], outline=(255, 80, 20, 230), width=3)
    aura.save(os.path.join(VFX_DIR, "rage_aura.png"))

    print("Generated combat VFX textures.")

# ─────────────────────────────────────────────────────────────────────────────
# 5. GENERATE UI ICONS (Element badges, status icons)
# ─────────────────────────────────────────────────────────────────────────────
def generate_ui():
    # Element Badges (24x24)
    elements = {
        "fire": ((240, 60, 40), "🔥"),
        "water": ((40, 140, 240), "💧"),
        "wind": ((40, 200, 100), "🍃"),
        "light": ((245, 215, 60), "☀️"),
        "dark": ((160, 60, 220), "🌙"),
        "neutral": ((160, 160, 160), "⚔️"),
    }
    for el_name, (color, _) in elements.items():
        img = Image.new("RGBA", (24, 24), (0, 0, 0, 0))
        draw = ImageDraw.Draw(img)
        # Diamond shape
        draw.polygon([(12, 1), (22, 12), (12, 22), (1, 12)], fill=color, outline=(255, 255, 255), width=1)
        draw.polygon([(12, 4), (19, 12), (12, 19), (4, 12)], fill=(min(255, color[0] + 40), min(255, color[1] + 40), min(255, color[2] + 40)))
        img.save(os.path.join(UI_DIR, f"elem_{el_name}.png"))

    # Status Icons (16x16)
    status_defs = {
        "stun": (240, 200, 40),     # Choáng (Vàng)
        "silence": (180, 80, 220),  # Khóa chiêu (Tím)
        "buff_atk": (240, 60, 60),  # Tăng công (Đỏ)
        "buff_def": (60, 140, 240), # Tăng thủ (Lam)
        "buff_spd": (60, 220, 120), # Tăng tốc (Lục)
        "debuff_atk": (140, 40, 40),
        "debuff_def": (40, 80, 140),
    }
    for s_name, col in status_defs.items():
        img = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
        draw = ImageDraw.Draw(img)
        draw.rectangle([1, 1, 14, 14], fill=col, outline=(255, 255, 255))
        # Inner symbol
        if "atk" in s_name:
            draw.line([(4, 12), (11, 4)], fill=(255, 255, 255), width=2)
        elif "def" in s_name:
            draw.polygon([(8, 3), (12, 6), (10, 12), (8, 13), (6, 12), (4, 6)], fill=(255, 255, 255))
        elif "stun" in s_name:
            draw.polygon([(8, 2), (10, 7), (6, 9), (9, 14)], fill=(255, 255, 255))
        elif "silence" in s_name:
            draw.rectangle([5, 5, 11, 11], fill=(255, 255, 255))
            draw.line([(4, 4), (12, 12)], fill=(200, 30, 30), width=2)
        img.save(os.path.join(UI_DIR, f"status_{s_name}.png"))

    print("Generated UI icons.")

if __name__ == "__main__":
    generate_hero_sprites()
    generate_background()
    generate_vfx()
    generate_ui()
    print("All Pixel Art assets generated successfully!")
