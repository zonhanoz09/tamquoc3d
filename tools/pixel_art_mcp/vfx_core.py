#!/usr/bin/env python3
"""
Pixel Art & Animation VFX Engine for TamQuoc3D.
Generates multi-frame HD-2D skill VFX sprite strips (576x96, 6 frames of 96x96)
and corresponding Godot 4.3 SpriteFrames .tres resources.
"""

import os
import math
from PIL import Image, ImageDraw

VFX_HERO_MAP = {
    "vfx_dragon_white": "trieu_van",
    "vfx_dragon_blade_ultimate": "quan_vu",
    "vfx_demonic_meteor": "lu_bo",
    "vfx_divine_storm": "gia_cat_luong",
    "vfx_inferno_storm": "chu_du",
    "vfx_lotus_healing": "tieu_kieu",
    "vfx_sonic_roar": "truong_phi",
    "vfx_magma_eruption": "dien_vi",
    "vfx_fortress_shield": "tao_nhan",
    "vfx_fire_ship": "hoang_cai",
    "vfx_sniper_beam": "hoang_trung",
    "vfx_cavalry_charge": "ma_sieu",
    "vfx_crescent_moon": "dieu_thuyen",
    "vfx_phoenix_chains": "bang_thong",
    "vfx_celestial_zither": "sai_van_co"
}


def draw_vfx_frame(vfx_name, frame_idx, total_frames=6, size=96):
    """Draws a single 96x96 frame for a specific ultimate skill VFX."""
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    t = frame_idx / float(total_frames - 1) # 0.0 -> 1.0
    cx, cy = size // 2, size // 2

    if vfx_name == "vfx_dragon_white": # Trieu Van - White Dragon Rush
        head_x = int(18 + t * 58)
        head_y = cy - int(math.sin(t * math.pi * 1.5) * 10)
        body_len = 16 + int(t * 28)
        d.ellipse([head_x - 14, head_y - 14, head_x + 14, head_y + 14], fill=(56, 189, 248, 80))
        for b in range(body_len):
            bx = head_x - b * 2
            by = head_y + int(math.sin((b / 6.0) + t * 4) * 8)
            alpha = int((1.0 - (b / float(body_len))) * 240)
            d.ellipse([bx - 6, by - 6, bx + 6, by + 6], fill=(240, 249, 255, alpha))
            d.ellipse([bx - 3, by - 3, bx + 3, by + 3], fill=(186, 230, 253, alpha))
        d.polygon([(head_x, head_y - 8), (head_x + 10, head_y), (head_x, head_y + 8)], fill=(255, 255, 255, 255))
        d.point([head_x + 4, head_y - 3], fill=(56, 189, 248))
        d.point([head_x + 4, head_y + 3], fill=(56, 189, 248))

    elif vfx_name == "vfx_dragon_blade_ultimate": # Quan Vu - Crescent Dragon Cleave
        arc_radius = 20 + int(t * 22)
        rot_angle = int(t * 160)
        d.arc([cx - arc_radius, cy - arc_radius, cx + arc_radius, cy + arc_radius],
              start=200 - rot_angle, end=380 - rot_angle, fill=(34, 197, 94, 220), width=6)
        d.arc([cx - arc_radius + 4, cy - arc_radius + 4, cx + arc_radius - 4, cy + arc_radius - 4],
              start=220 - rot_angle, end=360 - rot_angle, fill=(240, 253, 244, 255), width=3)
        if frame_idx >= 2:
            d.ellipse([cx - 24, cy + 18, cx + 24, cy + 28], outline=(74, 222, 128, 200), width=2)
            d.line([cx - 30, cy + 23, cx + 30, cy + 23], fill=(255, 255, 255, 220), width=2)

    elif vfx_name == "vfx_demonic_meteor": # Lu Bo - Demonic Meteor Strike
        if frame_idx <= 2:
            meteor_x = int(24 + t * 28)
            meteor_y = int(12 + t * 50)
            tail_len = int(16 + t * 24)
            d.line([meteor_x - tail_len, meteor_y - tail_len, meteor_x, meteor_y], fill=(225, 29, 72, 220), width=5)
            d.line([meteor_x - tail_len + 4, meteor_y - tail_len + 4, meteor_x, meteor_y], fill=(254, 205, 211, 255), width=2)
            d.ellipse([meteor_x - 10, meteor_y - 10, meteor_x + 10, meteor_y + 10], fill=(244, 63, 94, 255))
            d.ellipse([meteor_x - 5, meteor_y - 5, meteor_x + 5, meteor_y + 5], fill=(255, 255, 255, 255))
        else:
            exp_r = 14 + int((t - 0.4) * 44)
            alpha = int((1.0 - t) * 255)
            d.ellipse([cx - exp_r, cy - exp_r, cx + exp_r, cy + exp_r], outline=(225, 29, 72, alpha), width=4)
            d.ellipse([cx - int(exp_r * 0.7), cy - int(exp_r * 0.7), cx + int(exp_r * 0.7), cy + int(exp_r * 0.7)], fill=(244, 63, 94, int(alpha * 0.8)))
            d.ellipse([cx - 8, cy - 8, cx + 8, cy + 8], fill=(255, 255, 255, alpha))

    elif vfx_name == "vfx_divine_storm": # Gia Cat Luong - Bagua Gale Tornado
        rot = t * math.pi * 2
        for ring in (12, 22, 32):
            d.ellipse([cx - ring, cy - (ring // 2), cx + ring, cy + (ring // 2)], outline=(45, 212, 191, 160), width=2)
        h_top = int(20 + (1.0 - t) * 30)
        funnel_w = 12 + int(t * 18)
        d.polygon([(cx - funnel_w, h_top), (cx + funnel_w, h_top), (cx + 4, cy + 24), (cx - 4, cy + 24)],
                  fill=(20, 184, 166, 140))
        for i in range(4):
            ang = rot + i * (math.pi / 2)
            wx = cx + int(math.cos(ang) * (18 + t * 12))
            wy = cy - 10 + int(math.sin(ang) * 8)
            d.line([wx - 4, wy - 4, wx + 6, wy + 6], fill=(255, 255, 255, 220), width=2)

    elif vfx_name == "vfx_inferno_storm": # Chu Du - Red Cliffs Inferno
        flame_r = 14 + int(t * 28)
        for i in range(6):
            ang = (i / 6.0) * math.pi * 2 + (t * 2)
            fx = cx + int(math.cos(ang) * (flame_r * 0.6))
            fy = cy + 10 - int(math.sin(t * math.pi) * 16) + int(math.sin(ang) * 6)
            d.ellipse([fx - 10, fy - 14, fx + 10, fy + 14], fill=(239, 68, 68, 160))
            d.ellipse([fx - 6, fy - 10, fx + 6, fy + 10], fill=(251, 146, 60, 200))
            d.ellipse([fx - 3, fy - 5, fx + 3, fy + 5], fill=(254, 240, 138, 240))

    elif vfx_name == "vfx_lotus_healing": # Tieu Kieu - Sacred Lotus Bloom
        rip_r = 12 + int(t * 30)
        d.ellipse([cx - rip_r, cy + 18 - (rip_r // 3), cx + rip_r, cy + 18 + (rip_r // 3)],
                  outline=(56, 189, 248, 160), width=2)
        petals = 8
        petal_reach = 8 + int(t * 22)
        for p in range(petals):
            ang = (p / float(petals)) * math.pi * 2
            px = cx + int(math.cos(ang) * petal_reach)
            py = cy + 4 + int(math.sin(ang) * (petal_reach * 0.7))
            d.ellipse([px - 6, py - 8, px + 6, py + 8], fill=(244, 114, 182, 200))
            d.ellipse([px - 3, py - 4, px + 3, py + 4], fill=(253, 242, 248, 230))
        d.ellipse([cx - 5, cy + 2, cx + 5, cy + 8], fill=(250, 204, 21, 255))

    elif vfx_name == "vfx_sonic_roar": # Truong Phi - Sonic War Cry
        ring_r = 12 + int(t * 34)
        alpha = int((1.0 - t * 0.7) * 255)
        d.ellipse([cx - ring_r, cy - ring_r, cx + ring_r, cy + ring_r], outline=(249, 115, 22, alpha), width=3)
        if ring_r > 6:
            d.ellipse([cx - (ring_r - 6), cy - (ring_r - 6), cx + (ring_r - 6), cy + (ring_r - 6)], outline=(254, 240, 138, alpha), width=2)
        d.line([cx - ring_r, cy + 26, cx + ring_r, cy + 26], fill=(120, 113, 108, alpha), width=3)

    elif vfx_name == "vfx_magma_eruption": # Dien Vi - Magma Eruption
        pillar_h = max(2, int(t * 54))
        bottom_y = cy + 30
        top_y = max(4, bottom_y - pillar_h)
        d.rectangle([cx - 14, top_y, cx + 14, bottom_y], fill=(220, 38, 38, 220))
        if bottom_y - top_y > 4:
            d.rectangle([cx - 8, top_y + 4, cx + 8, bottom_y], fill=(249, 115, 22, 240))
        if bottom_y - top_y > 8:
            d.rectangle([cx - 3, top_y + 8, cx + 3, bottom_y], fill=(254, 240, 138, 255))
        for e in range(-2, 3):
            ex = cx + e * 10
            ey = top_y - int(math.sin(t * math.pi) * 16)
            d.ellipse([ex - 3, ey - 3, ex + 3, ey + 3], fill=(255, 255, 255, 240))

    elif vfx_name == "vfx_fortress_shield": # Tao Nhan - Golden Aegis Dome
        dome_r = 16 + int(t * 22)
        d.arc([cx - dome_r, cy - dome_r, cx + dome_r, cy + dome_r], start=180, end=360, fill=(250, 204, 21, 240), width=4)
        d.line([cx - dome_r, cy, cx + dome_r, cy], fill=(254, 240, 138, 220), width=2)
        d.polygon([(cx, cy - dome_r + 6), (cx - 10, cy - dome_r + 14), (cx + 10, cy - dome_r + 14)], fill=(254, 240, 138, 180))

    elif vfx_name == "vfx_fire_ship": # Hoang Cai - Burning Ram Ship
        ship_x = int(14 + t * 56)
        d.polygon([(ship_x, cy + 12), (ship_x - 30, cy - 8), (ship_x - 34, cy + 22)], fill=(30, 58, 138, 220))
        d.polygon([(ship_x + 4, cy + 6), (ship_x - 18, cy - 14), (ship_x - 22, cy + 14)], fill=(239, 68, 68, 240))
        d.ellipse([ship_x - 12, cy - 16, ship_x + 12, cy + 8], fill=(251, 146, 60, 200))

    elif vfx_name == "vfx_sniper_beam": # Hoang Trung - Solar Piercing Beam
        beam_w = max(4, int(math.sin(t * math.pi) * 22))
        d.rectangle([4, cy - (beam_w // 2), size - 4, cy + (beam_w // 2)], fill=(250, 204, 21, 220))
        if beam_w > 4:
            d.rectangle([4, cy - (beam_w // 4), size - 4, cy + (beam_w // 4)], fill=(255, 255, 255, 255))
        d.ellipse([cx - 20, cy - 20, cx + 20, cy + 20], outline=(253, 224, 71, 200), width=2)

    elif vfx_name == "vfx_cavalry_charge": # Ma Sieu - Spirit Cavalry Phalanx
        hx = int(16 + t * 56)
        d.ellipse([hx - 16, cy - 8, hx + 16, cy + 14], fill=(226, 232, 240, 230))
        d.polygon([(hx + 8, cy - 12), (hx + 22, cy - 4), (hx + 12, cy + 6)], fill=(248, 250, 252, 255))
        d.arc([hx - 28, cy - 14, hx + 12, cy + 20], start=160, end=340, fill=(56, 189, 248, 220), width=3)

    elif vfx_name == "vfx_crescent_moon": # Dieu Thuyen - Crescent Moonfall
        moon_y = int(18 + t * 36)
        d.ellipse([cx - 16, moon_y - 16, cx + 16, moon_y + 16], fill=(232, 121, 249, 200))
        d.ellipse([cx - 10, moon_y - 20, cx + 18, moon_y + 12], fill=(0, 0, 0, 0))
        for p in range(5):
            px = cx + int(math.cos(t * math.pi * 2 + p) * (18 + p * 4))
            py = moon_y + int(math.sin(t * math.pi * 2 + p) * 12)
            d.ellipse([px - 4, py - 4, px + 4, py + 4], fill=(244, 114, 182, 220))

    elif vfx_name == "vfx_phoenix_chains": # Bang Thong - Phoenix Flame Chains
        chain_span = int(16 + t * 30)
        d.line([cx - chain_span, cy - chain_span, cx + chain_span, cy + chain_span], fill=(239, 68, 68, 240), width=3)
        d.line([cx - chain_span, cy + chain_span, cx + chain_span, cy - chain_span], fill=(249, 115, 22, 240), width=3)
        d.arc([cx - 28, cy - 24, cx + 28, cy + 24], start=0, end=180, fill=(254, 240, 138, 220), width=3)

    elif vfx_name == "vfx_celestial_zither": # Sai Van Co - Celestial Zither Rings
        for r in range(1, 4):
            rr = int((r * 10) + t * 20)
            alpha = int((1.0 - t * 0.7) * 220)
            d.ellipse([cx - rr, cy - (rr // 2), cx + rr, cy + (rr // 2)], outline=(250, 204, 21, alpha), width=2)
        d.ellipse([cx - 12, cy - 14, cx - 6, cy - 8], fill=(216, 180, 254, 255))
        d.line([cx - 6, cy - 14, cx - 6, cy - 24], fill=(216, 180, 254, 255), width=2)
        d.ellipse([cx + 6, cy - 18, cx + 12, cy - 12], fill=(216, 180, 254, 255))
        d.line([cx + 12, cy - 18, cx + 12, cy - 28], fill=(216, 180, 254, 255), width=2)

    return img


def generate_vfx_strip(vfx_name, hero_id, project_root, frame_size=96, total_frames=6):
    """
    Generates a horizontal multi-frame sprite strip (e.g. 576x96 for 6 frames)
    and saves it to assets/heroes/{hero_id}/vfx/{vfx_name}_strip.png
    together with its Godot 4.3 SpriteFrames {vfx_name}_frames.tres
    """
    strip = Image.new("RGBA", (total_frames * frame_size, frame_size), (0, 0, 0, 0))

    for f in range(total_frames):
        fr = draw_vfx_frame(vfx_name, f, total_frames, frame_size)
        strip.paste(fr, (f * frame_size, 0))

    vfx_dir = os.path.join(project_root, f"assets/heroes/{hero_id}/vfx")
    os.makedirs(vfx_dir, exist_ok=True)

    img_path = os.path.join(vfx_dir, f"{vfx_name}_strip.png")
    strip.save(img_path)

    # Generate SpriteFrames .tres
    res_tex = f"res://assets/heroes/{hero_id}/vfx/{vfx_name}_strip.png"
    tres_path = os.path.join(vfx_dir, f"{vfx_name}_frames.tres")

    tres_content = f"""[gd_resource type="SpriteFrames" load_steps=8 format=3]

[ext_resource type="Texture2D" path="{res_tex}" id="1_vfx"]

"""
    for f in range(total_frames):
        tres_content += f"""[sub_resource type="AtlasTexture" id="AtlasTexture_vfx_{f}"]
atlas = ExtResource("1_vfx")
region = Rect2({f * frame_size}, 0, {frame_size}, {frame_size})

"""

    frames_sub = ", ".join([f'SubResource("AtlasTexture_vfx_{f}")' for f in range(total_frames)])
    tres_content += f"""[resource]
animations = [{{
"frames": [{frames_sub}],
"loop": false,
"name": &"play",
"speed": 16.0
}}]
"""
    with open(tres_path, "w") as fp:
        fp.write(tres_content)

    print(f"✅ Generated Animated VFX Strip: {img_path} ({strip.size[0]}x{strip.size[1]}) + {tres_path}")


def generate_all_skill_vfx(project_root):
    """Generates all 15 hero animated ultimate skill VFX strips."""
    for vfx_name, hero_id in VFX_HERO_MAP.items():
        generate_vfx_strip(vfx_name, hero_id, project_root)
    print(f"✨ Successfully generated all {len(VFX_HERO_MAP)} multi-frame skill VFX strips!")


if __name__ == "__main__":
    import sys
    proj = sys.argv[1] if len(sys.argv) > 1 else os.path.abspath(os.path.join(os.path.dirname(__file__), "../.."))
    generate_all_skill_vfx(proj)
