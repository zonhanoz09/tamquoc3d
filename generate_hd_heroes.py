#!/usr/bin/env python3
"""
HD-2D High Detail 64x64 Pixel Art Character Generator for Project Pixel Tactics
Generates beautiful, detailed pixel art sprites for 15 Three Kingdoms heroes.
Frame size: 64x64 px (4 frames per spritesheet = 256x64 px)
"""

import os
import math
from PIL import Image, ImageDraw

HERO_DIR = "/home/annhan/Documents/src/tamquoc3d/assets/sprites/heroes"
PORTRAIT_DIR = "/home/annhan/Documents/src/tamquoc3d/assets/sprites/portraits"
os.makedirs(HERO_DIR, exist_ok=True)
os.makedirs(PORTRAIT_DIR, exist_ok=True)

FW = 64
FH = 64

HERO_STYLES = {
    "trieu_van": {
        "name": "Triệu Vân",
        "skin": (255, 224, 196),
        "skin_shadow": (214, 175, 148),
        "hair": (32, 28, 42),
        "armor_light": (235, 245, 255),
        "armor_mid": (175, 195, 220),
        "armor_dark": (115, 135, 165),
        "trim": (240, 205, 90),
        "cloak": (35, 110, 225),
        "cloak_dark": (20, 65, 150),
        "weapon_type": "spear",
        "weapon_blade": (245, 250, 255),
        "weapon_hilt": (110, 70, 45),
        "tassel": (230, 45, 45),
        "eyes": (40, 140, 240),
        "helm_ornament": "dragon_crest",
    },
    "quan_vu": {
        "name": "Quan Vũ",
        "skin": (195, 75, 65),          # Red face of Guan Yu
        "skin_shadow": (150, 50, 45),
        "hair": (15, 15, 20),           # Long flowing beard
        "armor_light": (60, 170, 95),   # Emerald Green robe
        "armor_mid": (35, 125, 65),
        "armor_dark": (20, 80, 40),
        "trim": (245, 210, 75),         # Gold accents
        "cloak": (25, 90, 50),
        "cloak_dark": (15, 60, 35),
        "weapon_type": "glaive",
        "weapon_blade": (230, 240, 250),
        "weapon_hilt": (95, 60, 35),
        "tassel": (220, 40, 40),
        "eyes": (255, 230, 160),
        "helm_ornament": "green_turban",
    },
    "lu_bo": {
        "name": "Lữ Bố",
        "skin": (248, 210, 180),
        "skin_shadow": (205, 160, 135),
        "hair": (25, 20, 25),
        "armor_light": (95, 80, 75),    # Black-gold iron armor
        "armor_mid": (60, 50, 48),
        "armor_dark": (35, 28, 28),
        "trim": (245, 195, 60),         # Ornate Gold trim
        "cloak": (220, 35, 45),         # Imperial Red cape
        "cloak_dark": (145, 20, 28),
        "weapon_type": "halberd",
        "weapon_blade": (240, 240, 245),
        "weapon_hilt": (50, 45, 50),
        "tassel": (230, 180, 50),
        "eyes": (255, 50, 50),
        "helm_ornament": "pheasant_feathers",
    },
    "dien_vi": {
        "name": "Điển Vi",
        "skin": (225, 175, 145),
        "skin_shadow": (175, 130, 105),
        "hair": (20, 18, 18),
        "armor_light": (120, 85, 75),
        "armor_mid": (80, 55, 48),
        "armor_dark": (50, 35, 30),
        "trim": (200, 145, 55),
        "cloak": (170, 40, 35),
        "cloak_dark": (110, 25, 20),
        "weapon_type": "dual_axe",
        "weapon_blade": (200, 205, 215),
        "weapon_hilt": (85, 55, 35),
        "tassel": (180, 40, 35),
        "eyes": (255, 120, 50),
        "helm_ornament": "spiked_brow",
    },
    "gia_cat_luong": {
        "name": "Gia Cát Lượng",
        "skin": (255, 228, 202),
        "skin_shadow": (215, 180, 155),
        "hair": (30, 25, 35),
        "armor_light": (240, 245, 255), # Moonlight white & sky blue
        "armor_mid": (65, 110, 185),
        "armor_dark": (35, 65, 125),
        "trim": (230, 200, 80),
        "cloak": (240, 245, 255),
        "cloak_dark": (180, 195, 220),
        "weapon_type": "crane_fan",
        "weapon_blade": (250, 245, 230),
        "weapon_hilt": (120, 80, 50),
        "tassel": (60, 140, 240),
        "eyes": (80, 180, 255),
        "helm_ornament": "luan_jin",
    },
    "tieu_kieu": {
        "name": "Tiểu Kiều",
        "skin": (255, 232, 215),
        "skin_shadow": (225, 188, 170),
        "hair": (45, 35, 40),
        "armor_light": (145, 230, 210), # Jade green & blossom silk
        "armor_mid": (80, 185, 165),
        "armor_dark": (45, 130, 115),
        "trim": (255, 175, 200),
        "cloak": (255, 210, 225),
        "cloak_dark": (215, 150, 175),
        "weapon_type": "flower_fans",
        "weapon_blade": (255, 190, 205),
        "weapon_hilt": (220, 180, 90),
        "tassel": (100, 230, 210),
        "eyes": (90, 220, 200),
        "helm_ornament": "flower_pins",
    },
    "truong_phi": {
        "name": "Trương Phi",
        "skin": (185, 145, 120),
        "skin_shadow": (145, 110, 90),
        "hair": (15, 15, 18),
        "armor_light": (90, 95, 105),
        "armor_mid": (55, 60, 70),
        "armor_dark": (35, 38, 45),
        "trim": (230, 175, 60),
        "cloak": (175, 45, 40),
        "cloak_dark": (115, 25, 22),
        "weapon_type": "serpent_spear",
        "weapon_blade": (225, 230, 240),
        "weapon_hilt": (85, 55, 35),
        "tassel": (220, 45, 45),
        "eyes": (255, 160, 40),
        "helm_ornament": "iron_brow",
    },
    "tao_nhan": {
        "name": "Tào Nhân",
        "skin": (240, 205, 178),
        "skin_shadow": (195, 160, 135),
        "hair": (35, 30, 35),
        "armor_light": (175, 185, 200),
        "armor_mid": (125, 135, 150),
        "armor_dark": (80, 90, 105),
        "trim": (220, 190, 80),
        "cloak": (140, 120, 85),
        "cloak_dark": (95, 80, 55),
        "weapon_type": "tower_shield",
        "weapon_blade": (205, 215, 230),
        "weapon_hilt": (100, 70, 45),
        "tassel": (210, 180, 70),
        "eyes": (220, 220, 240),
        "helm_ornament": "steel_crest",
    },
    "hoang_cai": {
        "name": "Hoàng Cái",
        "skin": (215, 175, 145),
        "skin_shadow": (170, 130, 105),
        "hair": (220, 225, 235),       # White hair/beard
        "armor_light": (105, 135, 150),
        "armor_mid": (65, 95, 110),
        "armor_dark": (40, 65, 80),
        "trim": (230, 150, 60),
        "cloak": (210, 95, 40),
        "cloak_dark": (145, 60, 25),
        "weapon_type": "iron_whip",
        "weapon_blade": (180, 185, 195),
        "weapon_hilt": (90, 60, 40),
        "tassel": (230, 110, 40),
        "eyes": (240, 180, 90),
        "helm_ornament": "naval_crest",
    },
    "chu_du": {
        "name": "Chu Du",
        "skin": (255, 228, 205),
        "skin_shadow": (220, 185, 160),
        "hair": (35, 30, 40),
        "armor_light": (245, 245, 255),
        "armor_mid": (205, 210, 225),
        "armor_dark": (145, 150, 170),
        "trim": (245, 200, 70),
        "cloak": (220, 45, 45),
        "cloak_dark": (150, 25, 25),
        "weapon_type": "ruby_sword",
        "weapon_blade": (255, 235, 240),
        "weapon_hilt": (230, 180, 60),
        "tassel": (235, 50, 50),
        "eyes": (255, 140, 50),
        "helm_ornament": "silver_coronet",
    },
    "hoang_trung": {
        "name": "Hoàng Trung",
        "skin": (225, 185, 155),
        "skin_shadow": (180, 140, 115),
        "hair": (225, 230, 240),       # White beard
        "armor_light": (205, 175, 85),
        "armor_mid": (155, 125, 55),
        "armor_dark": (105, 80, 35),
        "trim": (245, 215, 90),
        "cloak": (65, 125, 85),
        "cloak_dark": (40, 85, 55),
        "weapon_type": "great_bow",
        "weapon_blade": (160, 105, 55),
        "weapon_hilt": (95, 60, 30),
        "tassel": (230, 190, 60),
        "eyes": (255, 215, 80),
        "helm_ornament": "golden_helm",
    },
    "ma_sieu": {
        "name": "Mã Siêu",
        "skin": (250, 218, 190),
        "skin_shadow": (210, 175, 145),
        "hair": (38, 32, 42),
        "armor_light": (240, 245, 255), # Silver lion armor
        "armor_mid": (185, 200, 220),
        "armor_dark": (120, 140, 165),
        "trim": (240, 210, 85),
        "cloak": (245, 245, 250),
        "cloak_dark": (185, 190, 205),
        "weapon_type": "spear",
        "weapon_blade": (245, 250, 255),
        "weapon_hilt": (115, 75, 45),
        "tassel": (50, 140, 240),
        "eyes": (100, 200, 255),
        "helm_ornament": "lion_crest",
    },
    "dieu_thuyen": {
        "name": "Điêu Thuyền",
        "skin": (255, 235, 220),
        "skin_shadow": (228, 195, 178),
        "hair": (30, 25, 35),
        "armor_light": (255, 185, 210),
        "armor_mid": (230, 135, 170),
        "armor_dark": (175, 85, 125),
        "trim": (255, 215, 130),
        "cloak": (245, 150, 190),
        "cloak_dark": (195, 100, 145),
        "weapon_type": "dancing_ribbons",
        "weapon_blade": (255, 210, 230),
        "weapon_hilt": (240, 190, 90),
        "tassel": (255, 140, 185),
        "eyes": (220, 100, 180),
        "helm_ornament": "phoenix_hairpin",
    },
    "bang_thong": {
        "name": "Bàng Thống",
        "skin": (215, 175, 142),
        "skin_shadow": (175, 135, 108),
        "hair": (30, 25, 25),
        "armor_light": (155, 65, 50),
        "armor_mid": (105, 40, 32),
        "armor_dark": (65, 25, 20),
        "trim": (240, 160, 60),
        "cloak": (205, 105, 45),
        "cloak_dark": (145, 65, 25),
        "weapon_type": "fire_staff",
        "weapon_blade": (255, 130, 40),
        "weapon_hilt": (110, 65, 35),
        "tassel": (255, 80, 30),
        "eyes": (255, 140, 40),
        "helm_ornament": "wanderer_hat",
    },
    "sai_van_co": {
        "name": "Sái Văn Cơ",
        "skin": (255, 230, 212),
        "skin_shadow": (225, 190, 170),
        "hair": (32, 28, 38),
        "armor_light": (115, 170, 215),
        "armor_mid": (65, 120, 170),
        "armor_dark": (38, 78, 120),
        "trim": (245, 220, 110),
        "cloak": (235, 240, 250),
        "cloak_dark": (185, 195, 215),
        "weapon_type": "guqin",
        "weapon_blade": (160, 105, 65),
        "weapon_hilt": (220, 180, 80),
        "tassel": (100, 180, 240),
        "eyes": (120, 190, 250),
        "helm_ornament": "scholar_ribbon",
    },
}

def draw_hd_hero_frame(draw, ox, oy, h_info, state="idle", tick=0):
    """
    Renders a high-detail 64x64 pixel art character frame.
    Layers:
      1. Drop Shadow & Ground Aura
      2. Flowing Cape / Back cloth
      3. Legs, Greaves & Boots
      4. Torso Armor, Breastplate, Belt & Buckle
      5. Shoulder Pauldrons & Arms
      6. Head, Face, Expressive Eyes & Facial Hair
      7. Ornate Helmet & Plumes
      8. Weapons with reflections & tassels
    """
    s_l = h_info["armor_light"]
    s_m = h_info["armor_mid"]
    s_d = h_info["armor_dark"]
    trim = h_info["trim"]
    cloak = h_info["cloak"]
    cloak_d = h_info["cloak_dark"]
    skin = h_info["skin"]
    skin_d = h_info["skin_shadow"]
    hair = h_info["hair"]
    w_type = h_info["weapon_type"]
    w_blade = h_info["weapon_blade"]
    w_hilt = h_info["weapon_hilt"]
    tassel = h_info["tassel"]
    eyes = h_info["eyes"]
    helm_orn = h_info["helm_ornament"]

    bx = ox + 32
    by = oy + 56

    body_x = 0
    body_y = 0
    arm_x = 0
    arm_y = 0
    weapon_rot = 0
    is_flash_white = False

    if state == "idle":
        if tick == 1:
            body_y = 1
    elif state == "attack":
        body_x = 6
        body_y = 0
        arm_x = 8
        arm_y = -3
        weapon_rot = 1
    elif state == "hurt":
        body_x = -4
        body_y = -1
        is_flash_white = True

    # Palette overrides for Hurt flash
    if is_flash_white:
        s_l = (255, 255, 255)
        s_m = (235, 235, 240)
        s_d = (200, 200, 210)
        cloak = (240, 240, 245)
        cloak_d = (190, 190, 200)
        skin = (255, 255, 255)
        skin_d = (220, 220, 220)

    # 1. Ground Shadow
    draw.ellipse([bx - 15 + body_x, by - 3, bx + 15 + body_x, by + 5], fill=(0, 0, 0, 90))

    # 2. Flowing Cape (Back)
    cape_poly = [
        (bx - 12 + body_x, by - 32 + body_y),
        (bx + 12 + body_x, by - 32 + body_y),
        (bx + 16 + body_x + (2 if tick==1 else 0), by - 5 + body_y),
        (bx - 16 + body_x - (2 if tick==1 else 0), by - 5 + body_y),
    ]
    draw.polygon(cape_poly, fill=cloak)
    # Cape folds / shading
    draw.line([(bx - 6 + body_x, by - 30 + body_y), (bx - 8 + body_x, by - 5 + body_y)], fill=cloak_d, width=2)
    draw.line([(bx + 6 + body_x, by - 30 + body_y), (bx + 8 + body_x, by - 5 + body_y)], fill=cloak_d, width=2)

    # 3. Legs, Greaves & Boots
    # Left leg
    draw.rectangle([bx - 9 + body_x, by - 14 + body_y, bx - 3 + body_x, by], fill=s_d)
    draw.rectangle([bx - 8 + body_x, by - 12 + body_y, bx - 4 + body_x, by - 3], fill=s_m)
    draw.line([(bx - 9 + body_x, by), (bx - 3 + body_x, by)], fill=trim, width=1)
    # Right leg
    draw.rectangle([bx + 3 + body_x, by - 14 + body_y, bx + 9 + body_x, by], fill=s_d)
    draw.rectangle([bx + 4 + body_x, by - 12 + body_y, bx + 8 + body_x, by - 3], fill=s_m)
    draw.line([(bx + 3 + body_x, by), (bx + 9 + body_x, by)], fill=trim, width=1)

    # 4. Torso Armor / Robes
    # Base armor
    draw.rectangle([bx - 10 + body_x, by - 34 + body_y, bx + 10 + body_x, by - 14 + body_y], fill=s_m)
    # Chestplate highlight & detail
    draw.rectangle([bx - 7 + body_x, by - 32 + body_y, bx + 7 + body_x, by - 18 + body_y], fill=s_l)
    draw.polygon([(bx - 5 + body_x, by - 30 + body_y), (bx, by - 22 + body_y), (bx + 5 + body_x, by - 30 + body_y)], fill=s_d)
    # Ornate Gold Belt & Buckle
    draw.rectangle([bx - 10 + body_x, by - 16 + body_y, bx + 10 + body_x, by - 12 + body_y], fill=(60, 45, 30))
    draw.rectangle([bx - 4 + body_x, by - 17 + body_y, bx + 4 + body_x, by - 11 + body_y], fill=trim)
    draw.point([bx + body_x, by - 14 + body_y], fill=(220, 40, 40)) # Center gem

    # Fauld / Hip Armor skirts
    draw.polygon([(bx - 10 + body_x, by - 12 + body_y), (bx - 12 + body_x, by - 4 + body_y), (bx - 2 + body_x, by - 4 + body_y), (bx - 2 + body_x, by - 12 + body_y)], fill=s_m)
    draw.polygon([(bx + 2 + body_x, by - 12 + body_y), (bx + 2 + body_x, by - 4 + body_y), (bx + 12 + body_x, by - 4 + body_y), (bx + 10 + body_x, by - 12 + body_y)], fill=s_m)

    # 5. Shoulder Pauldrons (Spikes / Dragon Heads)
    draw.rectangle([bx - 15 + body_x, by - 35 + body_y, bx - 8 + body_x, by - 27 + body_y], fill=s_m)
    draw.rectangle([bx - 14 + body_x, by - 34 + body_y, bx - 9 + body_x, by - 28 + body_y], fill=s_l)
    draw.line([(bx - 15 + body_x, by - 35 + body_y), (bx - 8 + body_x, by - 35 + body_y)], fill=trim, width=1)

    draw.rectangle([bx + 8 + body_x, by - 35 + body_y, bx + 15 + body_x, by - 27 + body_y], fill=s_m)
    draw.rectangle([bx + 9 + body_x, by - 34 + body_y, bx + 14 + body_x, by - 28 + body_y], fill=s_l)
    draw.line([(bx + 8 + body_x, by - 35 + body_y), (bx + 15 + body_x, by - 35 + body_y)], fill=trim, width=1)

    # 6. Head, Face & Expression
    # Neck
    draw.rectangle([bx - 3 + body_x, by - 37 + body_y, bx + 3 + body_x, by - 34 + body_y], fill=skin_d)
    # Face
    draw.rectangle([bx - 7 + body_x, by - 48 + body_y, bx + 7 + body_x, by - 37 + body_y], fill=skin)
    draw.rectangle([bx - 6 + body_x, by - 39 + body_y, bx + 6 + body_x, by - 37 + body_y], fill=skin_d) # Chin shadow

    # Expressive Eyes
    if not is_flash_white:
        draw.point([bx + 1 + body_x, by - 43 + body_y], fill=eyes)
        draw.point([bx + 5 + body_x, by - 43 + body_y], fill=eyes)
        draw.line([(bx + body_x, by - 45 + body_y), (bx + 3 + body_x, by - 45 + body_y)], fill=(30, 20, 20), width=1) # Brow
        draw.line([(bx + 4 + body_x, by - 45 + body_y), (bx + 7 + body_x, by - 45 + body_y)], fill=(30, 20, 20), width=1)

    # Quan Vũ Special: Magnificent 5-strand flowing beard
    if "Quan Vũ" in h_info["name"]:
        beard_poly = [
            (bx - 4 + body_x, by - 39 + body_y),
            (bx + 4 + body_x, by - 39 + body_y),
            (bx + 3 + body_x, by - 18 + body_y),
            (bx - 3 + body_x, by - 18 + body_y),
        ]
        draw.polygon(beard_poly, fill=hair)
        draw.line([(bx - 1 + body_x, by - 38 + body_y), (bx - 1 + body_x, by - 16 + body_y)], fill=(40, 40, 50), width=1)

    # Trương Phi Special: Bristling leopard beard
    elif "Trương Phi" in h_info["name"]:
        draw.rectangle([bx - 7 + body_x, by - 40 + body_y, bx + 7 + body_x, by - 32 + body_y], fill=hair)

    # 7. Helmet & Ornaments
    draw.rectangle([bx - 8 + body_x, by - 53 + body_y, bx + 8 + body_x, by - 46 + body_y], fill=s_m)
    draw.rectangle([bx - 7 + body_x, by - 52 + body_y, bx + 7 + body_x, by - 48 + body_y], fill=s_l)
    draw.line([(bx - 8 + body_x, by - 47 + body_y), (bx + 8 + body_x, by - 47 + body_y)], fill=trim, width=1)

    # Head Ornaments
    if helm_orn == "pheasant_feathers":
        # Lữ Bố: Two tall curved pheasant feathers
        f_color = (230, 40, 40)
        draw.line([(bx - 3 + body_x, by - 53 + body_y), (bx - 12 + body_x, by - 63 + body_y)], fill=f_color, width=2)
        draw.line([(bx + 3 + body_x, by - 53 + body_y), (bx + 12 + body_x, by - 63 + body_y)], fill=f_color, width=2)
        draw.point([bx - 13 + body_x, by - 64 + body_y], fill=(255, 220, 80))
        draw.point([bx + 13 + body_x, by - 64 + body_y], fill=(255, 220, 80))
    elif helm_orn == "dragon_crest":
        # Triệu Vân: Silver dragon spike + red tassel
        draw.polygon([(bx + body_x, by - 59 + body_y), (bx - 3 + body_x, by - 53 + body_y), (bx + 3 + body_x, by - 53 + body_y)], fill=s_l)
        draw.line([(bx + body_x, by - 53 + body_y), (bx - 5 + body_x, by - 47 + body_y)], fill=tassel, width=2)
    elif helm_orn == "green_turban":
        # Quan Vũ: Green silk turban
        draw.rectangle([bx - 8 + body_x, by - 55 + body_y, bx + 8 + body_x, by - 47 + body_y], fill=(40, 130, 70))
        draw.rectangle([bx - 3 + body_x, by - 57 + body_y, bx + 3 + body_x, by - 53 + body_y], fill=trim)
    elif helm_orn == "luan_jin":
        # Gia Cát Lượng: Taoist Dao Guan hat
        draw.rectangle([bx - 6 + body_x, by - 57 + body_y, bx + 6 + body_x, by - 48 + body_y], fill=(50, 80, 150))
        draw.rectangle([bx - 2 + body_x, by - 60 + body_y, bx + 2 + body_x, by - 56 + body_y], fill=trim)
    elif helm_orn == "flower_pins":
        # Tiểu Kiều: Twin blossom hairpins
        draw.ellipse([bx - 8 + body_x, by - 52 + body_y, bx - 4 + body_x, by - 48 + body_y], fill=(255, 150, 180))
        draw.ellipse([bx + 4 + body_x, by - 52 + body_y, bx + 8 + body_x, by - 48 + body_y], fill=(255, 150, 180))

    # 8. Arms & High Detail Weapons
    wx = bx + 12 + body_x + arm_x
    wy = by - 26 + body_y + arm_y

    # Hand holding weapon
    draw.rectangle([wx - 2, wy - 2, wx + 3, wy + 3], fill=skin)

    # WEAPONS:
    if w_type == "spear":
        # Bạch Long Thương / Hổ Đầu Thương
        draw.line([(wx - 8, wy + 24), (wx + 8, wy - 28)], fill=w_hilt, width=2)
        # Ornate Spearhead
        draw.polygon([(wx + 8, wy - 36), (wx + 13, wy - 26), (wx + 4, wy - 26)], fill=w_blade)
        draw.line([(wx + 8, wy - 36), (wx + 8, wy - 24)], fill=(120, 200, 255), width=1) # Cyan glint
        draw.polygon([(wx + 5, wy - 24), (wx + 11, wy - 24), (wx + 8, wy - 20)], fill=tassel) # Red tassel
    elif w_type == "glaive":
        # Thanh Long Yển Nguyệt Đao
        draw.line([(wx - 10, wy + 26), (wx + 6, wy - 26)], fill=w_hilt, width=3)
        # Giant Crescent Blade
        blade_poly = [
            (wx + 6, wy - 26),
            (wx + 6, wy - 42),
            (wx + 18, wy - 32),
            (wx + 14, wy - 18),
            (wx + 4, wy - 18),
        ]
        draw.polygon(blade_poly, fill=w_blade)
        draw.polygon([(wx + 7, wy - 34), (wx + 13, wy - 28), (wx + 6, wy - 22)], fill=(40, 170, 95)) # Green dragon etching
        draw.line([(wx + 6, wy - 42), (wx + 18, wy - 32)], fill=(255, 255, 255), width=2) # Edge shine
    elif w_type == "halberd":
        # Phương Thiên Họa Kích
        draw.line([(wx - 8, wy + 26), (wx + 8, wy - 30)], fill=(70, 70, 75), width=3)
        # Spear point
        draw.polygon([(wx + 8, wy - 42), (wx + 12, wy - 30), (wx + 4, wy - 30)], fill=w_blade)
        # Crescent side blade (Họa kích)
        draw.arc([wx - 2, wy - 34, wx + 20, wy - 18], 270, 90, fill=w_blade, width=3)
        draw.line([(wx + 3, wy - 26), (wx + 15, wy - 26)], fill=trim, width=2)
        draw.point([wx + 8, wy - 28], fill=tassel)
    elif w_type == "dual_axe":
        # Điển Vi: Song Kích / Heavy Battleaxes
        draw.line([(wx - 2, wy + 16), (wx + 4, wy - 18)], fill=w_hilt, width=3)
        draw.polygon([(wx - 8, wy - 18), (wx + 14, wy - 22), (wx + 6, wy - 8)], fill=w_blade)
        draw.point([wx + 3, wy - 15], fill=(220, 50, 40))
    elif w_type == "crane_fan":
        # Gia Cát Lượng: Bát Quái Hạc Vũ Phiến
        draw.polygon([(wx, wy), (wx + 14, wy - 18), (wx + 20, wy - 6)], fill=w_blade)
        draw.line([(wx, wy), (wx + 16, wy - 12)], fill=trim, width=2)
        # Mystic wind glow dot
        draw.point([wx + 12, wy - 10], fill=(80, 200, 255))
    elif w_type == "flower_fans":
        # Tiểu Kiều: Song Phiến Hoa Đào
        draw.polygon([(wx - 2, wy), (wx + 14, wy - 14), (wx + 18, wy - 2)], fill=w_blade)
        draw.point([wx + 8, wy - 8], fill=(255, 80, 140))
        # Flowing water silk ribbon
        draw.arc([wx - 4, wy - 12, wx + 18, wy + 12], 0, 180, fill=(100, 230, 220), width=2)
    elif w_type == "tower_shield":
        # Tào Nhân: Đại Khiên Thiết Bích
        draw.polygon([(wx - 10, wy - 22), (wx + 8, wy - 22), (wx + 5, wy + 12), (wx - 8, wy + 12)], fill=w_blade)
        draw.rectangle([wx - 6, wy - 14, wx + 2, wy + 4], fill=s_d)
        draw.point([wx - 2, wy - 5], fill=trim)
    elif w_type == "serpent_spear":
        # Trương Phi: Bát Xà Mâu
        draw.line([(wx - 8, wy + 26), (wx + 8, wy - 26)], fill=w_hilt, width=2)
        # Wavy snake blade
        draw.line([(wx + 8, wy - 26), (wx + 12, wy - 31)], fill=w_blade, width=2)
        draw.line([(wx + 12, wy - 31), (wx + 6, wy - 36)], fill=w_blade, width=2)
        draw.line([(wx + 6, wy - 36), (wx + 10, wy - 42)], fill=w_blade, width=2)
    else:
        # Default sword / staff / bow
        draw.line([(wx - 4, wy + 18), (wx + 10, wy - 22)], fill=w_blade, width=2)
        draw.line([(wx - 3, wy + 8), (wx + 4, wy + 12)], fill=trim, width=2)

def generate_all_hd_heroes():
    for hid, info in HERO_STYLES.items():
        img = Image.new("RGBA", (FW * 4, FH), (0, 0, 0, 0))
        draw = ImageDraw.Draw(img)

        # Frame 0: Idle 1
        draw_hd_hero_frame(draw, 0, 0, info, state="idle", tick=0)
        # Frame 1: Idle 2 (subtle breathing & cape sway)
        draw_hd_hero_frame(draw, FW, 0, info, state="idle", tick=1)
        # Frame 2: Attack (forward thrust & weapon strike)
        draw_hd_hero_frame(draw, FW * 2, 0, info, state="attack")
        # Frame 3: Hurt (knockback & white impact flash)
        draw_hd_hero_frame(draw, FW * 3, 0, info, state="hurt")

        out_path = os.path.join(HERO_DIR, f"{hid}.png")
        img.save(out_path)

        # Generate HD Portrait (64x64)
        p_img = Image.new("RGBA", (64, 64), (20, 16, 28, 255))
        p_draw = ImageDraw.Draw(p_img)

        # Ornate Three Kingdoms Gold Frame
        p_draw.rectangle([0, 0, 63, 63], outline=(235, 195, 75), width=2)
        p_draw.rectangle([2, 2, 61, 61], outline=(75, 55, 95), width=1)
        # Corner brackets
        for cx, cy in [(2, 2), (58, 2), (2, 58), (58, 58)]:
            p_draw.rectangle([cx, cy, cx + 3, cy + 3], fill=(255, 230, 120))

        # Close-up head and chest
        draw_hd_hero_frame(p_draw, 0, 16, info, state="idle", tick=0)

        p_out = os.path.join(PORTRAIT_DIR, f"{hid}.png")
        p_img.save(p_out)

    print(f"Generated {len(HERO_STYLES)} HD-2D hero spritesheets and portraits.")

if __name__ == "__main__":
    generate_all_hd_heroes()
