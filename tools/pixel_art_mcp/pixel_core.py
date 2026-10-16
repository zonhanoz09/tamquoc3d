#!/usr/bin/env python3
"""
Pixel Art & Animation Core Engine for TamQuoc3D.
Generates HD-2D multi-frame pixel art sprite sheets and Godot 4.3 SpriteFrames resources
for all 15 Three Kingdoms heroes.
"""

import os
import math
from PIL import Image, ImageDraw

HERO_CONFIGS = {
    # ── TIỀN QUÂN ──
    "trieu_van": {
        "name": "Triệu Vân",
        "archetype": "warrior_spear",
        "skin": [(255, 224, 189), (232, 186, 148), (196, 142, 104)],
        "armor": [(235, 245, 255), (195, 215, 235), (145, 170, 200), (90, 115, 145)],
        "cape": [(56, 189, 248), (14, 165, 233), (2, 132, 199), (3, 105, 161)],
        "gold": [(254, 240, 138), (250, 204, 21), (202, 138, 4), (161, 98, 7)],
        "hair": [(30, 41, 59), (15, 23, 42)],
        "weapon": [(240, 249, 255), (186, 230, 253), (56, 189, 248), (120, 113, 108)],
        "boots": [(100, 116, 139), (71, 85, 105), (51, 65, 85)],
        "vfx": [(186, 230, 253), (56, 189, 248), (14, 165, 233), (255, 255, 255)],
        "features": ["helmet_circlet", "white_plume", "dragon_spear", "blue_sash"]
    },
    "dien_vi": {
        "name": "Điển Vi",
        "archetype": "warrior_heavy",
        "skin": [(215, 175, 145), (185, 140, 110), (150, 105, 80)],
        "armor": [(75, 85, 99), (55, 65, 81), (31, 41, 55), (17, 24, 39)],
        "cape": [(185, 28, 28), (127, 29, 29), (69, 10, 10)],
        "gold": [(202, 138, 4), (161, 98, 7)],
        "hair": [(20, 20, 20), (10, 10, 10)],
        "weapon": [(209, 213, 219), (156, 163, 175), (107, 114, 128), (75, 85, 99)],
        "boots": [(55, 65, 81), (31, 41, 55)],
        "vfx": [(239, 68, 68), (220, 38, 38), (185, 28, 28), (254, 202, 202)],
        "features": ["dual_halberds", "spiked_pauldrons", "scarred_chest", "horned_band"]
    },
    "truong_phi": {
        "name": "Trương Phi",
        "archetype": "warrior_spear",
        "skin": [(210, 170, 140), (180, 140, 110), (140, 100, 75)],
        "armor": [(60, 60, 65), (42, 42, 47), (28, 28, 32), (18, 18, 22)],
        "cape": [(194, 65, 12), (154, 52, 10), (124, 45, 18)],
        "gold": [(217, 119, 6), (180, 83, 9)],
        "hair": [(20, 20, 20), (10, 10, 10)],
        "weapon": [(226, 232, 240), (148, 163, 184), (100, 116, 139), (71, 85, 105)],
        "boots": [(42, 42, 47), (28, 28, 32)],
        "vfx": [(249, 115, 22), (234, 88, 12), (194, 65, 12), (255, 237, 213)],
        "features": ["serpent_spear", "tiger_pauldron", "wild_beard", "red_headband"]
    },
    "tao_nhan": {
        "name": "Tào Nhân",
        "archetype": "warrior_heavy",
        "skin": [(245, 215, 185), (215, 175, 145), (175, 135, 105)],
        "armor": [(203, 213, 225), (148, 163, 184), (100, 116, 139), (71, 85, 105)],
        "cape": [(30, 64, 175), (29, 78, 216), (37, 99, 235)],
        "gold": [(250, 204, 21), (202, 138, 4), (161, 98, 7)],
        "hair": [(30, 41, 59), (15, 23, 42)],
        "weapon": [(148, 163, 184), (100, 116, 139), (71, 85, 105)],
        "boots": [(71, 85, 105), (51, 65, 85)],
        "vfx": [(253, 224, 71), (250, 204, 21), (234, 179, 8), (255, 255, 255)],
        "features": ["tower_shield", "spiked_mace", "knight_visor", "gold_trim"]
    },
    "hoang_cai": {
        "name": "Hoàng Cái",
        "archetype": "warrior_heavy",
        "skin": [(215, 170, 135), (180, 135, 100), (145, 100, 70)],
        "armor": [(30, 58, 138), (30, 64, 175), (29, 78, 216), (23, 37, 84)],
        "cape": [(220, 38, 38), (185, 28, 28), (153, 27, 27)],
        "gold": [(217, 119, 6), (180, 83, 9)],
        "hair": [(229, 231, 235), (156, 163, 175), (107, 114, 128)],
        "weapon": [(107, 114, 128), (75, 85, 99), (55, 65, 81)],
        "boots": [(30, 41, 59), (15, 23, 42)],
        "vfx": [(249, 115, 22), (239, 68, 68), (56, 189, 248), (255, 255, 255)],
        "features": ["naval_club", "gray_beard", "veteran_scars", "fiery_band"]
    },

    # ── TRUNG QUÂN ──
    "quan_vu": {
        "name": "Quan Vũ",
        "archetype": "warrior_spear",
        "skin": [(225, 105, 80), (195, 80, 60), (155, 55, 40)], # Gương mặt đỏ như gấc
        "armor": [(34, 197, 94), (22, 163, 74), (21, 128, 61), (15, 80, 40)], # Áo xanh lục bảo
        "cape": [(21, 128, 61), (15, 80, 40), (10, 50, 25)],
        "gold": [(254, 240, 138), (250, 204, 21), (202, 138, 4), (161, 98, 7)],
        "hair": [(20, 20, 25), (10, 10, 15)],
        "weapon": [(187, 247, 208), (134, 239, 172), (74, 222, 128), (34, 197, 94)],
        "boots": [(21, 128, 61), (15, 80, 40)],
        "vfx": [(134, 239, 172), (74, 222, 128), (22, 163, 74), (255, 255, 255)],
        "features": ["magnificent_beard", "crescent_blade", "green_headband", "dragon_armor"]
    },
    "lu_bo": {
        "name": "Lữ Bố",
        "archetype": "warrior_spear",
        "skin": [(245, 210, 180), (215, 170, 135), (175, 125, 90)],
        "armor": [(45, 40, 55), (30, 25, 40), (20, 15, 25), (15, 10, 20)],
        "cape": [(225, 29, 72), (190, 18, 60), (159, 18, 57), (136, 19, 55)],
        "gold": [(253, 224, 71), (234, 179, 8), (180, 83, 9), (120, 53, 15)],
        "hair": [(24, 24, 27), (9, 9, 11)],
        "weapon": [(244, 63, 94), (225, 29, 72), (214, 211, 209), (120, 113, 108)],
        "boots": [(39, 39, 42), (24, 24, 27), (9, 9, 11)],
        "vfx": [(254, 205, 211), (244, 63, 94), (225, 29, 72), (159, 18, 57)],
        "features": ["pheasant_feathers", "halberd_fangtian", "obsidian_armor", "gold_trim"]
    },
    "chu_du": {
        "name": "Chu Du",
        "archetype": "mage_scholar",
        "skin": [(255, 228, 196), (235, 190, 155), (200, 150, 120)],
        "armor": [(255, 255, 255), (241, 245, 249), (226, 232, 240), (203, 213, 225)],
        "cape": [(225, 29, 72), (190, 18, 60), (159, 18, 57)],
        "gold": [(250, 204, 21), (202, 138, 4)],
        "hair": [(30, 41, 59), (15, 23, 42)],
        "weapon": [(244, 63, 94), (225, 29, 72), (226, 232, 240)],
        "boots": [(190, 18, 60), (159, 18, 57)],
        "vfx": [(251, 146, 60), (239, 68, 68), (220, 38, 38), (255, 255, 255)],
        "features": ["flame_sword", "scholar_ribbons", "white_robe", "crimson_sash"]
    },
    "hoang_trung": {
        "name": "Hoàng Trung",
        "archetype": "archer",
        "skin": [(240, 205, 175), (205, 165, 135), (170, 130, 100)],
        "armor": [(180, 83, 9), (146, 64, 14), (120, 53, 15), (69, 26, 3)],
        "cape": [(22, 101, 52), (20, 83, 45), (15, 60, 30)],
        "gold": [(253, 224, 71), (250, 204, 21), (202, 138, 4)],
        "hair": [(255, 255, 255), (209, 213, 219), (156, 163, 175)],
        "weapon": [(217, 119, 6), (180, 83, 9), (253, 224, 71)],
        "boots": [(120, 53, 15), (69, 26, 3)],
        "vfx": [(253, 224, 71), (250, 204, 21), (234, 179, 8), (255, 255, 255)],
        "features": ["golden_bow", "quiver", "white_beard", "archer_cap"]
    },
    "ma_sieu": {
        "name": "Mã Siêu",
        "archetype": "warrior_spear",
        "skin": [(255, 224, 189), (232, 186, 148), (196, 142, 104)],
        "armor": [(248, 250, 252), (226, 232, 240), (148, 163, 184), (71, 85, 105)],
        "cape": [(255, 255, 255), (226, 232, 240), (203, 213, 225)],
        "gold": [(250, 204, 21), (202, 138, 4)],
        "hair": [(30, 41, 59), (15, 23, 42)],
        "weapon": [(203, 213, 225), (148, 163, 184), (56, 189, 248)],
        "boots": [(148, 163, 184), (100, 116, 139)],
        "vfx": [(186, 230, 253), (125, 211, 252), (56, 189, 248), (255, 255, 255)],
        "features": ["lion_helmet", "white_fur_mantle", "silver_spear", "wind_crest"]
    },
    "dieu_thuyen": {
        "name": "Điêu Thuyền",
        "archetype": "maiden_support",
        "skin": [(255, 230, 210), (240, 195, 175), (210, 160, 140)],
        "armor": [(232, 121, 249), (192, 38, 211), (147, 51, 234), (107, 33, 168)], # Váy sen tím
        "cape": [(244, 114, 182), (236, 72, 153), (219, 39, 119)],
        "gold": [(254, 240, 138), (250, 204, 21)],
        "hair": [(30, 27, 75), (15, 10, 40)],
        "weapon": [(244, 114, 182), (236, 72, 153), (255, 255, 255)], # Dải lụa múa
        "boots": [(192, 38, 211), (147, 51, 234)],
        "vfx": [(244, 114, 182), (232, 121, 249), (192, 132, 252), (255, 255, 255)],
        "features": ["moon_ribbons", "lotus_hairpin", "flowing_silks", "crescent_aura"]
    },

    # ── HẬU QUÂN ──
    "gia_cat_luong": {
        "name": "Gia Cát Lượng",
        "archetype": "mage_scholar",
        "skin": [(255, 228, 196), (235, 190, 155), (200, 150, 120)],
        "armor": [(248, 250, 252), (226, 232, 240), (13, 148, 136), (15, 118, 110)],
        "cape": [(13, 148, 136), (15, 118, 110), (17, 94, 89)],
        "gold": [(250, 204, 21), (202, 138, 4)],
        "hair": [(30, 41, 59), (15, 23, 42)],
        "weapon": [(255, 255, 255), (203, 213, 225), (148, 163, 184)], # Bạch Vũ Phiến
        "boots": [(13, 148, 136), (15, 118, 110)],
        "vfx": [(45, 212, 191), (20, 184, 166), (13, 148, 136), (255, 255, 255)],
        "features": ["crane_fan", "scholar_cap", "bagua_robe", "mystic_breeze"]
    },
    "bang_thong": {
        "name": "Bàng Thống",
        "archetype": "mage_scholar",
        "skin": [(235, 195, 160), (200, 155, 120), (165, 120, 85)],
        "armor": [(185, 28, 28), (153, 27, 27), (88, 28, 135), (59, 7, 100)],
        "cape": [(185, 28, 28), (153, 27, 27)],
        "gold": [(245, 158, 11), (217, 119, 6)],
        "hair": [(35, 30, 45), (20, 15, 30)],
        "weapon": [(120, 53, 15), (249, 115, 22), (239, 68, 68)], # Gậy Phượng Sồ
        "boots": [(88, 28, 135), (59, 7, 100)],
        "vfx": [(249, 115, 22), (234, 88, 12), (185, 28, 28), (255, 255, 255)],
        "features": ["phoenix_staff", "mystic_hat", "flame_chains", "feather_mantle"]
    },
    "tieu_kieu": {
        "name": "Tiểu Kiều",
        "archetype": "maiden_support",
        "skin": [(255, 230, 210), (240, 195, 175), (210, 160, 140)],
        "armor": [(56, 189, 248), (14, 165, 233), (244, 114, 182), (236, 72, 153)],
        "cape": [(244, 114, 182), (236, 72, 153)],
        "gold": [(254, 240, 138), (250, 204, 21)],
        "hair": [(39, 39, 42), (24, 24, 27)],
        "weapon": [(244, 114, 182), (251, 207, 232), (56, 189, 248)], # Song Phiến
        "boots": [(14, 165, 233), (2, 132, 199)],
        "vfx": [(56, 189, 248), (244, 114, 182), (186, 230, 253), (255, 255, 255)],
        "features": ["dual_fans", "twin_buns", "cherry_dress", "water_dew"]
    },
    "sai_van_co": {
        "name": "Sái Văn Cơ",
        "archetype": "maiden_support",
        "skin": [(255, 230, 210), (240, 195, 175), (210, 160, 140)],
        "armor": [(192, 132, 252), (168, 85, 247), (254, 240, 138), (250, 204, 21)],
        "cape": [(192, 132, 252), (147, 51, 234)],
        "gold": [(250, 204, 21), (202, 138, 4)],
        "hair": [(24, 24, 27), (15, 15, 20)],
        "weapon": [(146, 64, 14), (120, 53, 15), (254, 240, 138)], # Đàn Tranh Guqin
        "boots": [(168, 85, 247), (147, 51, 234)],
        "vfx": [(216, 180, 254), (192, 132, 252), (250, 204, 21), (255, 255, 255)],
        "features": ["guqin_zither", "braided_hair", "celestial_dress", "music_notes"]
    }
}


def draw_hero_frame(hero_id, anim_type, frame_idx, total_frames, size=64):
    """
    Renders a stylized HD-2D pixel art frame (64x64) tailored to the hero's identity,
    archetype, equipment, and dynamic motion cycle.
    """
    cfg = HERO_CONFIGS.get(hero_id, HERO_CONFIGS["trieu_van"])
    arch = cfg["archetype"]
    features = cfg["features"]

    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    cx = 32
    cy = 56

    # ─────────────────────────────────────────────────────────────
    # 1. IDLE ANIMATION (6 Frames Smooth Breathing Cycle)
    # ─────────────────────────────────────────────────────────────
    if anim_type == "idle":
        t = (frame_idx / total_frames) * 2 * math.pi
        bob_y = int(math.sin(t) * 1.5)
        sway = int(math.cos(t) * 2)
        head_y = 16 + bob_y
        body_y = 28 + bob_y

        # Shadow
        sh_w = 15 if arch == "warrior_heavy" else 13
        d.ellipse([cx - sh_w, cy - 3, cx + sh_w, cy + 3], fill=(0, 0, 0, 95))

        # Cape / Robe Back
        if "cape" in cfg and len(cfg["cape"]) > 0:
            cape_cols = cfg["cape"]
            cape_w = 14 if arch != "maiden_support" else 10
            for i in range(cape_w):
                c_idx = min(i // 4, len(cape_cols) - 1)
                d.line([cx - 8 - i + sway, body_y + i, cx - 4 + sway, body_y + i + 14], fill=cape_cols[c_idx])

        # Legs / Lower body
        if arch in ("warrior_spear", "warrior_heavy", "archer"):
            d.rectangle([cx - 7, cy - 14, cx - 2, cy - 2], fill=cfg["boots"][1])
            d.rectangle([cx + 2, cy - 14, cx + 7, cy - 2], fill=cfg["boots"][0])
            if "gold" in cfg:
                d.rectangle([cx - 8, cy - 12, cx - 1, cy - 9], fill=cfg["gold"][0])
                d.rectangle([cx + 1, cy - 12, cx + 8, cy - 9], fill=cfg["gold"][0])
        else: # Flowing robe / maiden skirt
            d.polygon([
                (cx - 8, body_y + 8),
                (cx + 8, body_y + 8),
                (cx + 11 + sway // 2, cy - 2),
                (cx - 11 + sway // 2, cy - 2)
            ], fill=cfg["armor"][1])
            d.polygon([
                (cx - 6, body_y + 10),
                (cx + 6, body_y + 10),
                (cx + 8 + sway // 2, cy - 2),
                (cx - 8 + sway // 2, cy - 2)
            ], fill=cfg["armor"][0])

        # Torso / Armor / Robes
        armor_w = 11 if arch == "warrior_heavy" else 9
        d.rectangle([cx - armor_w, body_y, cx + armor_w, cy - 14], fill=cfg["armor"][1])
        d.rectangle([cx - (armor_w - 2), body_y + 2, cx + (armor_w - 2), cy - 16], fill=cfg["armor"][0])

        # Gold Emblem / Ornaments
        if "gold" in cfg:
            d.rectangle([cx - 3, body_y + 6, cx + 3, body_y + 10], fill=cfg["gold"][0])

        # Shoulder Pauldrons / Sleeves
        d.rectangle([cx - (armor_w + 5), body_y - 2, cx - armor_w, body_y + 6], fill=cfg["armor"][1])
        d.rectangle([cx + armor_w, body_y - 2, cx + (armor_w + 5), body_y + 6], fill=cfg["armor"][0])

        # Head / Face
        face_w = 5
        d.rectangle([cx - face_w, head_y, cx + face_w, body_y - 1], fill=cfg["skin"][0])
        d.point([cx + 1, head_y + 5], fill=cfg["hair"][0])
        d.point([cx + 3, head_y + 5], fill=cfg["hair"][0])

        # Headgear / Hair / Beards
        _draw_hero_head_accessories(d, hero_id, cfg, cx, head_y, sway)

        # Held Weapon / Instruments in Idle Stance
        _draw_hero_weapon_idle(d, hero_id, cfg, cx, cy, head_y, body_y, sway)

    # ─────────────────────────────────────────────────────────────
    # 2. ATTACK ANIMATION (6 Frames Dynamic Lunge, Cleave, Cast)
    # ─────────────────────────────────────────────────────────────
    elif anim_type == "attack":
        prog = frame_idx / (total_frames - 1)
        lunge_x = int(math.sin(prog * math.pi) * (14 if arch != "archer" else 6))
        cx_dyn = cx + lunge_x

        sh_w = 14 + int(prog * 6)
        d.ellipse([cx_dyn - sh_w, cy - 3, cx_dyn + sh_w, cy + 3], fill=(0, 0, 0, 95))

        if frame_idx in (0, 1): # Stance windup
            head_y = 18
            body_y = 30
            cx_w = cx - 2
            d.rectangle([cx_w - 8, body_y, cx_w + 8, cy - 12], fill=cfg["armor"][1])
            d.rectangle([cx_w - 5, head_y, cx_w + 5, body_y], fill=cfg["skin"][0])
            _draw_hero_head_accessories(d, hero_id, cfg, cx_w, head_y, 0)
            _draw_hero_attack_windup(d, hero_id, cfg, cx_w, cy, head_y, body_y)

        elif frame_idx in (2, 3): # Impact Strike / Full Extension
            head_y = 20
            body_y = 30
            # Striding legs
            d.line([cx_dyn - 10, cy - 2, cx_dyn - 2, body_y + 10], fill=cfg["boots"][1], width=4)
            d.line([cx_dyn + 10, cy - 2, cx_dyn + 2, body_y + 10], fill=cfg["boots"][0], width=4)
            # Torso leaning
            d.rectangle([cx_dyn - 6, body_y, cx_dyn + 12, cy - 14], fill=cfg["armor"][0])
            d.rectangle([cx_dyn - 2, head_y, cx_dyn + 8, body_y], fill=cfg["skin"][0])
            _draw_hero_head_accessories(d, hero_id, cfg, cx_dyn + 2, head_y, 2)
            _draw_hero_attack_impact(d, hero_id, cfg, cx_dyn, cy, head_y, body_y)

        else: # Follow-through & Recovery
            head_y = 17
            body_y = 29
            d.rectangle([cx_dyn - 7, body_y, cx_dyn + 7, cy - 13], fill=cfg["armor"][1])
            d.rectangle([cx_dyn - 4, head_y, cx_dyn + 4, body_y], fill=cfg["skin"][0])
            _draw_hero_head_accessories(d, hero_id, cfg, cx_dyn, head_y, 0)
            _draw_hero_attack_recovery(d, hero_id, cfg, cx_dyn, cy, head_y, body_y)

    # ─────────────────────────────────────────────────────────────
    # 3. SKILL ANIMATION (6 Frames Ultimate Rage Climax)
    # ─────────────────────────────────────────────────────────────
    elif anim_type == "skill":
        t = frame_idx / (total_frames - 1)
        lift_y = int(math.sin(t * math.pi) * 7)
        cx_dyn = cx + int(math.sin(t * math.pi) * 8)

        # Ground Energy Ring
        ring_r = 16 + int(t * 8)
        vfx_col = cfg["vfx"][1]
        d.ellipse([cx - ring_r, cy - 4, cx + ring_r, cy + 4], outline=vfx_col, width=2)
        d.ellipse([cx - (ring_r // 2), cy - 2, cx + (ring_r // 2), cy + 2], fill=(255, 255, 255, 120))

        head_y = 14 - lift_y
        body_y = 25 - lift_y

        # Aura surrounding hero
        aura_col = cfg["vfx"][0]
        d.ellipse([cx_dyn - 18, head_y - 6, cx_dyn + 18, cy - lift_y + 2], outline=aura_col, width=2)

        # Floating Hero Body
        d.rectangle([cx_dyn - 8, body_y, cx_dyn + 8, cy - 14 - lift_y], fill=cfg["armor"][0])
        d.rectangle([cx_dyn - 5, head_y, cx_dyn + 5, body_y], fill=cfg["skin"][0])
        _draw_hero_head_accessories(d, hero_id, cfg, cx_dyn, head_y, 3)

        # Climax Skill Weapon & Visual Effects
        _draw_hero_skill_climax(d, hero_id, cfg, cx_dyn, cy, head_y, body_y, lift_y, t)

    # ─────────────────────────────────────────────────────────────
    # 4. HURT ANIMATION (3 Frames Knockback Recoil)
    # ─────────────────────────────────────────────────────────────
    elif anim_type == "hurt":
        recoil = int(frame_idx * 4)
        head_y = 18
        body_y = 30
        cx_dyn = cx - recoil

        d.ellipse([cx_dyn - 12, cy - 3, cx_dyn + 12, cy + 3], fill=(0, 0, 0, 80))
        fill_col = (255, 255, 255) if frame_idx == 0 else cfg["armor"][min(2, len(cfg["armor"]) - 1)]
        d.rectangle([cx_dyn - 8, body_y, cx_dyn + 8, cy - 14], fill=fill_col)
        d.rectangle([cx_dyn - 5, head_y, cx_dyn + 5, body_y], fill=cfg["skin"][1])
        _draw_hero_head_accessories(d, hero_id, cfg, cx_dyn, head_y, -2)

    # ─────────────────────────────────────────────────────────────
    # 5. DEATH ANIMATION (3 Frames Collapse to Ground)
    # ─────────────────────────────────────────────────────────────
    elif anim_type == "death":
        t = frame_idx / (total_frames - 1)
        drop_y = int(t * 12)
        alpha = int((1.0 - t * 0.7) * 255)

        head_y = 20 + drop_y
        body_y = 32 + drop_y

        d.ellipse([cx - 10, cy - 3, cx + 10, cy + 3], fill=(0, 0, 0, int(alpha * 0.4)))
        d.rectangle([cx - 7, body_y, cx + 7, cy - 4], fill=cfg["armor"][min(2, len(cfg["armor"]) - 1)])
        d.rectangle([cx - 4, head_y, cx + 4, body_y], fill=cfg["skin"][min(2, len(cfg["skin"]) - 1)])

    return img


# ─────────────────────────────────────────────────────────────────
# HELPER RENDERING PROCEDURES PER HERO FEATURE
# ─────────────────────────────────────────────────────────────────
def _draw_hero_head_accessories(d, hero_id, cfg, cx, head_y, sway):
    """Draws hero headgear, beards, feathers, circlets, or buns."""
    hair_col = cfg["hair"][0]

    if hero_id == "quan_vu":
        # Green headband & Long magnificent black beard
        d.rectangle([cx - 6, head_y - 3, cx + 6, head_y + 1], fill=cfg["armor"][1])
        d.line([cx - 6, head_y + 1, cx + 6, head_y + 1], fill=cfg["gold"][0])
        # Legendary 5-inch long beard flowing down torso
        for b in range(16):
            b_sway = int(sway * 0.5)
            d.line([cx - 3 + b_sway, head_y + 7 + b, cx + 3 + b_sway, head_y + 7 + b], fill=hair_col)

    elif hero_id == "truong_phi":
        # Wild black spiky hair & bushy beard
        d.rectangle([cx - 7, head_y - 4, cx + 7, head_y], fill=cfg["cape"][0]) # Red band
        for sp in range(-8, 9, 3):
            d.line([cx + sp, head_y - 2, cx + sp + 1, head_y - 7], fill=hair_col, width=2)
        # Bushy beard
        d.rectangle([cx - 6, head_y + 6, cx + 6, head_y + 14], fill=hair_col)

    elif hero_id == "lu_bo":
        # Golden Crown & Dual Pheasant Feathers
        d.rectangle([cx - 6, head_y - 3, cx + 6, head_y + 1], fill=cfg["gold"][0])
        d.line([cx - 3, head_y - 3, cx - 14 + sway, head_y - 18], fill=cfg["cape"][0], width=2)
        d.line([cx + 3, head_y - 3, cx + 14 + sway, head_y - 18], fill=cfg["cape"][0], width=2)

    elif hero_id == "trieu_van":
        # Silver circlet with white plume
        d.rectangle([cx - 6, head_y - 3, cx + 6, head_y + 1], fill=cfg["armor"][0])
        d.line([cx - 2, head_y - 6, cx + 4 + sway // 2, head_y - 2], fill=(255, 255, 255), width=3)

    elif hero_id == "ma_sieu":
        # Silver lion helmet & flowing white fur plume
        d.rectangle([cx - 7, head_y - 5, cx + 7, head_y + 1], fill=cfg["armor"][0])
        d.line([cx - 1, head_y - 4, cx + 1, head_y - 4], fill=cfg["gold"][0])
        d.line([cx - 4, head_y - 8, cx - 12 + sway, head_y + 4], fill=(255, 255, 255), width=4)

    elif hero_id == "hoang_trung":
        # Veteran golden cap & long noble white beard
        d.rectangle([cx - 6, head_y - 4, cx + 6, head_y], fill=cfg["armor"][0])
        for b in range(14):
            d.line([cx - 2, head_y + 7 + b, cx + 3, head_y + 7 + b], fill=(245, 245, 245))

    elif hero_id == "gia_cat_luong":
        # Scholar Lun Jin cap with twin ribbons
        d.rectangle([cx - 5, head_y - 8, cx + 5, head_y], fill=(30, 41, 59))
        d.line([cx - 3, head_y - 2, cx - 10 + sway, head_y + 10], fill=(15, 23, 42), width=2)

    elif hero_id == "dieu_thuyen":
        # Royal high hair bun with lotus hairpin
        d.ellipse([cx - 5, head_y - 8, cx + 5, head_y - 1], fill=hair_col)
        d.point([cx + 3, head_y - 4], fill=cfg["gold"][0])

    elif hero_id == "tieu_kieu":
        # Twin cute hair buns
        d.ellipse([cx - 9, head_y - 5, cx - 3, head_y + 1], fill=hair_col)
        d.ellipse([cx + 3, head_y - 5, cx + 9, head_y + 1], fill=hair_col)
        d.point([cx - 6, head_y - 2], fill=cfg["cape"][0])
        d.point([cx + 6, head_y - 2], fill=cfg["cape"][0])

    elif hero_id == "sai_van_co":
        # Braided hair with jade hairpin
        d.ellipse([cx - 6, head_y - 6, cx + 6, head_y], fill=hair_col)
        d.line([cx - 4, head_y + 2, cx - 8 + sway, head_y + 12], fill=hair_col, width=2)
        d.point([cx + 2, head_y - 3], fill=(134, 239, 172))

    elif hero_id == "chu_du":
        # Noble commander cap with twin red ribbons
        d.rectangle([cx - 5, head_y - 5, cx + 5, head_y], fill=cfg["cape"][0])
        d.line([cx - 4, head_y - 1, cx - 11 + sway, head_y + 8], fill=cfg["cape"][1], width=2)

    elif hero_id == "bang_thong":
        # Pointed mystic scholar hat
        d.polygon([(cx, head_y - 10), (cx - 6, head_y), (cx + 6, head_y)], fill=(76, 29, 149))

    elif hero_id == "tao_nhan":
        # Heavy steel knight helmet visor
        d.rectangle([cx - 7, head_y - 5, cx + 7, head_y + 2], fill=cfg["armor"][0])
        d.line([cx - 5, head_y, cx + 5, head_y], fill=(30, 41, 59), width=2)

    elif hero_id == "hoang_cai":
        # Gray hair, tough bandana
        d.rectangle([cx - 6, head_y - 3, cx + 6, head_y + 1], fill=cfg["cape"][0])
        d.rectangle([cx - 4, head_y + 7, cx + 4, head_y + 11], fill=(209, 213, 219))


def _draw_hero_weapon_idle(d, hero_id, cfg, cx, cy, head_y, body_y, sway):
    """Draws the hero's characteristic weapon in idle stance."""
    wep = cfg["weapon"]

    if hero_id in ("trieu_van", "lu_bo", "ma_sieu"):
        # Long spear / Halberd held upright-diagonal
        tip_x = cx + 16
        tip_y = head_y - 8
        d.line([cx - 12, cy - 2, tip_x, tip_y], fill=wep[min(2, len(wep) - 1)], width=2)
        d.polygon([(tip_x, tip_y), (tip_x + 5, tip_y - 5), (tip_x + 8, tip_y - 4), (tip_x + 4, tip_y + 2)], fill=wep[0])

    elif hero_id == "quan_vu":
        # Thanh Long Yển Nguyệt Đao (Huge Green Dragon Blade)
        tip_x = cx + 18
        tip_y = head_y - 12
        d.line([cx - 14, cy - 2, tip_x, tip_y], fill=(120, 113, 108), width=3)
        # Giant crescent blade head
        d.polygon([
            (tip_x - 2, tip_y + 4),
            (tip_x + 8, tip_y - 6),
            (tip_x + 12, tip_y - 2),
            (tip_x + 4, tip_y + 8)
        ], fill=wep[0])
        # Red tassel
        d.line([tip_x, tip_y + 4, tip_x - 3, tip_y + 10], fill=(225, 29, 72), width=2)

    elif hero_id == "truong_phi":
        # Bát Xà Mâu (Wavy Serpent Spear)
        tip_x = cx + 16
        tip_y = head_y - 8
        d.line([cx - 12, cy - 2, tip_x, tip_y], fill=(120, 113, 108), width=2)
        # Wavy blade
        for s in range(4):
            dx = 2 if s % 2 == 0 else -2
            d.line([tip_x + s * 2, tip_y - s * 3, tip_x + s * 2 + dx, tip_y - s * 3 - 3], fill=wep[0], width=2)

    elif hero_id == "dien_vi":
        # Dual Halberds (Song Kích) held in both hands
        d.line([cx - 14, cy - 8, cx - 18, head_y - 2], fill=wep[1], width=3)
        d.polygon([(cx - 20, head_y - 6), (cx - 16, head_y - 6), (cx - 18, head_y - 12)], fill=wep[0])
        d.line([cx + 12, cy - 8, cx + 16, head_y - 2], fill=wep[1], width=3)
        d.polygon([(cx + 14, head_y - 6), (cx + 18, head_y - 6), (cx + 16, head_y - 12)], fill=wep[0])

    elif hero_id == "tao_nhan":
        # Massive Tower Shield on left, Spiked Mace on right
        d.rectangle([cx - 20, body_y - 2, cx - 8, cy - 2], fill=cfg["armor"][0])
        d.rectangle([cx - 18, body_y, cx - 10, cy - 4], fill=cfg["armor"][1])
        d.point([cx - 14, body_y + 8], fill=cfg["gold"][0]) # Shield boss
        d.line([cx + 10, cy - 8, cx + 16, body_y + 4], fill=wep[1], width=3)

    elif hero_id == "hoang_cai":
        # Heavy Iron Naval Club
        d.line([cx + 10, cy - 4, cx + 16, head_y + 4], fill=wep[0], width=4)

    elif hero_id == "hoang_trung":
        # Great Golden Bow on shoulder
        d.arc([cx + 6, head_y - 6, cx + 22, cy - 6], start=270, end=90, fill=wep[0], width=3)
        d.line([cx + 14, head_y - 2, cx + 14, cy - 8], fill=(255, 255, 255), width=1)

    elif hero_id == "gia_cat_luong":
        # White Crane Feather Fan
        d.polygon([(cx + 8, body_y + 4), (cx + 16, body_y - 4), (cx + 18, body_y + 6)], fill=(255, 255, 255))
        d.line([cx + 6, body_y + 6, cx + 16, body_y - 2], fill=(148, 163, 184), width=1)

    elif hero_id == "chu_du":
        # Flame Sword at hip
        d.line([cx + 6, body_y + 6, cx + 18, head_y + 4], fill=wep[0], width=2)
        d.rectangle([cx + 6, body_y + 4, cx + 10, body_y + 8], fill=cfg["gold"][0])

    elif hero_id == "bang_thong":
        # Phoenix Wood Staff with glowing fiery orb
        d.line([cx + 12, cy - 2, cx + 16, head_y - 6], fill=wep[0], width=2)
        d.ellipse([cx + 14, head_y - 10, cx + 20, head_y - 4], fill=wep[1])

    elif hero_id == "dieu_thuyen":
        # Moon Dancer Ribbons floating
        d.arc([cx - 18 + sway, body_y, cx - 4 + sway, cy - 4], start=180, end=360, fill=wep[0], width=2)
        d.arc([cx + 4 + sway, body_y, cx + 18 + sway, cy - 4], start=0, end=180, fill=wep[0], width=2)

    elif hero_id == "tieu_kieu":
        # Dual Lotus Fans held at chest
        d.polygon([(cx - 12, body_y + 4), (cx - 4, body_y - 2), (cx - 4, body_y + 8)], fill=wep[0])
        d.polygon([(cx + 4, body_y - 2), (cx + 12, body_y + 4), (cx + 4, body_y + 8)], fill=wep[0])

    elif hero_id == "sai_van_co":
        # Ancient Guqin Zither floating gracefully in front
        d.rectangle([cx - 12, body_y + 10, cx + 12, body_y + 16], fill=wep[0])
        for s in range(-8, 9, 4):
            d.line([cx + s, body_y + 11, cx + s, body_y + 15], fill=(255, 255, 255))


def _draw_hero_attack_windup(d, hero_id, cfg, cx_w, cy, head_y, body_y):
    wep = cfg["weapon"]
    d.line([cx_w - 18, body_y + 8, cx_w + 10, head_y - 2], fill=wep[min(1, len(wep) - 1)], width=2)


def _draw_hero_attack_impact(d, hero_id, cfg, cx_dyn, cy, head_y, body_y):
    wep = cfg["weapon"]
    vfx = cfg["vfx"]
    # Weapon thrust / swing reaching across
    d.line([cx_dyn - 4, body_y + 4, cx_dyn + 26, body_y - 2], fill=wep[0], width=3)
    # Dynamic Slash Smear Arc
    d.arc([cx_dyn + 10, body_y - 18, cx_dyn + 34, body_y + 14], start=270, end=90, fill=vfx[1], width=3)
    d.arc([cx_dyn + 14, body_y - 14, cx_dyn + 32, body_y + 10], start=290, end=70, fill=vfx[0], width=2)


def _draw_hero_attack_recovery(d, hero_id, cfg, cx_dyn, cy, head_y, body_y):
    wep = cfg["weapon"]
    d.line([cx_dyn - 8, cy - 2, cx_dyn + 18, head_y + 4], fill=wep[min(2, len(wep) - 1)], width=2)


def _draw_hero_skill_climax(d, hero_id, cfg, cx_dyn, cy, head_y, body_y, lift_y, t):
    wep = cfg["weapon"]
    vfx = cfg["vfx"]

    # Signature weapon thrust / strike in air
    d.line([cx_dyn - 4, cy - lift_y, cx_dyn + 24, head_y - 8], fill=wep[0], width=4)
    d.line([cx_dyn - 2, cy - lift_y, cx_dyn + 22, head_y - 8], fill=(255, 255, 255), width=2)

    # Elemental Magic Burst
    burst_r = 12 + int(t * 10)
    d.arc([cx_dyn + 8, head_y - 14, cx_dyn + 8 + burst_r, head_y - 14 + burst_r], start=0, end=360, fill=vfx[0], width=3)
    d.arc([cx_dyn + 6, head_y - 16, cx_dyn + 10 + burst_r, head_y - 12 + burst_r], start=45, end=315, fill=vfx[1], width=2)


# ─────────────────────────────────────────────────────────────────
# EXPORT SPRITESHEET & GODOT SPRITEFRAMES (.TRES)
# ─────────────────────────────────────────────────────────────────
def generate_hero_spritesheet(hero_id, output_path, frame_size=64):
    """
    Generates a full 24-frame (4 rows x 6 columns) HD-2D spritesheet:
    Row 0: Idle (6 frames)
    Row 1: Attack (6 frames)
    Row 2: Skill (6 frames)
    Row 3: Hurt (3 frames) + Death (3 frames)
    """
    cols = 6
    rows = 4
    sheet = Image.new("RGBA", (cols * frame_size, rows * frame_size), (0, 0, 0, 0))

    # 1. Idle (6 frames)
    for f in range(6):
        fr = draw_hero_frame(hero_id, "idle", f, 6, frame_size)
        sheet.paste(fr, (f * frame_size, 0 * frame_size))

    # 2. Attack (6 frames)
    for f in range(6):
        fr = draw_hero_frame(hero_id, "attack", f, 6, frame_size)
        sheet.paste(fr, (f * frame_size, 1 * frame_size))

    # 3. Skill (6 frames)
    for f in range(6):
        fr = draw_hero_frame(hero_id, "skill", f, 6, frame_size)
        sheet.paste(fr, (f * frame_size, 2 * frame_size))

    # 4. Hurt (3 frames) + Death (3 frames)
    for f in range(3):
        fr = draw_hero_frame(hero_id, "hurt", f, 3, frame_size)
        sheet.paste(fr, (f * frame_size, 3 * frame_size))

    for f in range(3):
        fr = draw_hero_frame(hero_id, "death", f, 3, frame_size)
        sheet.paste(fr, ((3 + f) * frame_size, 3 * frame_size))

    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    sheet.save(output_path)
    print(f"Generated {hero_id} spritesheet: {output_path} ({sheet.size[0]}x{sheet.size[1]})")
    return sheet.size


def generate_godot_spriteframes(hero_id, res_texture_path, output_tres_path, frame_size=64):
    """
    Generates a Godot 4.3 SpriteFrames .tres resource with configured animation clips:
    'idle' (8 FPS, loop=true)
    'attack' (12 FPS, loop=false)
    'skill' (12 FPS, loop=false)
    'hurt' (10 FPS, loop=false)
    'death' (6 FPS, loop=false)
    """
    tres_content = f"""[gd_resource type="SpriteFrames" load_steps=26 format=3]

[ext_resource type="Texture2D" path="{res_texture_path}" id="1_tex"]

"""
    # Create AtlasTextures for each frame
    # Row 0: Idle (frames 0..5)
    for f in range(6):
        tres_content += f"""[sub_resource type="AtlasTexture" id="AtlasTexture_idle_{f}"]
atlas = ExtResource("1_tex")
region = Rect2({f * frame_size}, 0, {frame_size}, {frame_size})

"""
    # Row 1: Attack (frames 0..5)
    for f in range(6):
        tres_content += f"""[sub_resource type="AtlasTexture" id="AtlasTexture_atk_{f}"]
atlas = ExtResource("1_tex")
region = Rect2({f * frame_size}, {1 * frame_size}, {frame_size}, {frame_size})

"""
    # Row 2: Skill (frames 0..5)
    for f in range(6):
        tres_content += f"""[sub_resource type="AtlasTexture" id="AtlasTexture_skl_{f}"]
atlas = ExtResource("1_tex")
region = Rect2({f * frame_size}, {2 * frame_size}, {frame_size}, {frame_size})

"""
    # Row 3: Hurt (frames 0..2)
    for f in range(3):
        tres_content += f"""[sub_resource type="AtlasTexture" id="AtlasTexture_hrt_{f}"]
atlas = ExtResource("1_tex")
region = Rect2({f * frame_size}, {3 * frame_size}, {frame_size}, {frame_size})

"""
    # Row 3: Death (frames 0..2)
    for f in range(3):
        tres_content += f"""[sub_resource type="AtlasTexture" id="AtlasTexture_dth_{f}"]
atlas = ExtResource("1_tex")
region = Rect2({(3 + f) * frame_size}, {3 * frame_size}, {frame_size}, {frame_size})

"""

    # Assemble animations array
    idle_sub = ", ".join([f'SubResource("AtlasTexture_idle_{f}")' for f in range(6)])
    atk_sub = ", ".join([f'SubResource("AtlasTexture_atk_{f}")' for f in range(6)])
    skl_sub = ", ".join([f'SubResource("AtlasTexture_skl_{f}")' for f in range(6)])
    hrt_sub = ", ".join([f'SubResource("AtlasTexture_hrt_{f}")' for f in range(3)])
    dth_sub = ", ".join([f'SubResource("AtlasTexture_dth_{f}")' for f in range(3)])

    tres_content += f"""[resource]
animations = [{{
"frames": [{idle_sub}],
"loop": true,
"name": &"idle",
"speed": 8.0
}}, {{
"frames": [{atk_sub}],
"loop": false,
"name": &"attack",
"speed": 12.0
}}, {{
"frames": [{skl_sub}],
"loop": false,
"name": &"skill",
"speed": 12.0
}}, {{
"frames": [{hrt_sub}],
"loop": false,
"name": &"hurt",
"speed": 10.0
}}, {{
"frames": [{dth_sub}],
"loop": false,
"name": &"death",
"speed": 6.0
}}]
"""
    os.makedirs(os.path.dirname(output_tres_path), exist_ok=True)
    with open(output_tres_path, "w") as f:
        f.write(tres_content)
    print(f"Generated Godot SpriteFrames .tres: {output_tres_path}")


def generate_all_heroes(project_root):
    """Generates HD spritesheets and SpriteFrames for all 15 heroes."""
    for hero_id in HERO_CONFIGS:
        out_img = os.path.join(project_root, f"assets/heroes/{hero_id}/spritesheet_hd.png")
        out_tres = os.path.join(project_root, f"assets/heroes/{hero_id}/sprite_frames.tres")
        res_tex = f"res://assets/heroes/{hero_id}/spritesheet_hd.png"

        generate_hero_spritesheet(hero_id, out_img, 64)
        generate_godot_spriteframes(hero_id, res_tex, out_tres, 64)
    print(f"🎉 Successfully generated HD-2D assets for all {len(HERO_CONFIGS)} heroes!")


if __name__ == "__main__":
    import sys
    arg = sys.argv[1] if len(sys.argv) > 1 else "all"

    if arg == "all":
        proj_root = sys.argv[2] if len(sys.argv) > 2 else os.path.abspath(os.path.join(os.path.dirname(__file__), "../.."))
        generate_all_heroes(proj_root)
    else:
        hero = arg
        out_img = sys.argv[2] if len(sys.argv) > 2 else f"assets/heroes/{hero}/spritesheet_hd.png"
        out_tres = sys.argv[3] if len(sys.argv) > 3 else f"assets/heroes/{hero}/sprite_frames.tres"
        res_tex = f"res://assets/heroes/{hero}/spritesheet_hd.png"

        generate_hero_spritesheet(hero, out_img, 64)
        generate_godot_spriteframes(hero, res_tex, out_tres, 64)
