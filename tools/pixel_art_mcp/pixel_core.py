#!/usr/bin/env python3
"""
Pixel Art & Animation Core Engine for TamQuoc3D.
Generates multi-frame pixel art sprite sheets and Godot 4.3 SpriteFrames resources.
"""

import os
import math
from PIL import Image, ImageDraw

# Color Palettes (Neo-Geo / Capcom CPS2 16-bit Arcade Style)
PALETTES = {
    "trieu_van": {
        "skin": [(255, 224, 189), (232, 186, 148), (196, 142, 104)],
        "armor": [(235, 245, 255), (195, 215, 235), (145, 170, 200), (90, 115, 145)],
        "cape": [(56, 189, 248), (14, 165, 233), (2, 132, 199), (3, 105, 161)],
        "gold": [(254, 240, 138), (250, 204, 21), (202, 138, 4), (161, 98, 7)],
        "hair": [(30, 41, 59), (15, 23, 42)],
        "spear": [(240, 249, 255), (186, 230, 253), (56, 189, 248), (120, 113, 108)],
        "boots": [(100, 116, 139), (71, 85, 105), (51, 65, 85)],
        "vfx": [(186, 230, 253), (56, 189, 248), (14, 165, 233), (255, 255, 255)],
    },
    "lu_bo": {
        "skin": [(245, 210, 180), (215, 170, 135), (175, 125, 90)],
        "armor": [(45, 40, 55), (30, 25, 40), (20, 15, 25), (15, 10, 20)],
        "cape": [(225, 29, 72), (190, 18, 60), (159, 18, 57), (136, 19, 55)],
        "gold": [(253, 224, 71), (234, 179, 8), (180, 83, 9), (120, 53, 15)],
        "hair": [(24, 24, 27), (9, 9, 11)],
        "halberd": [(244, 63, 94), (225, 29, 72), (214, 211, 209), (120, 113, 108)],
        "boots": [(39, 39, 42), (24, 24, 27), (9, 9, 11)],
        "vfx": [(254, 205, 211), (244, 63, 94), (225, 29, 72), (159, 18, 57)],
    }
}

def draw_trieu_van_frame(anim_type, frame_idx, total_frames, size=64):
    """Draw a detailed 64x64 frame for Trieu Van."""
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    pal = PALETTES["trieu_van"]
    
    # Center coords: base foot at x=32, y=56
    cx = 32
    cy = 56
    
    # Dynamic breathing / movement offsets
    if anim_type == "idle":
        # 6-frame smooth breathing sine wave
        t = (frame_idx / total_frames) * 2 * math.pi
        bob_y = int(math.sin(t) * 1.5)
        cape_sway = int(math.cos(t) * 2)
        head_y = 16 + bob_y
        body_y = 28 + bob_y
        
        # Shadow
        d.ellipse([cx - 14, cy - 3, cx + 14, cy + 3], fill=(0, 0, 0, 90))
        
        # Cape in back
        for i in range(12):
            d.line([cx - 8 - i + cape_sway, body_y + i, cx - 4 + cape_sway, body_y + i + 14], fill=pal["cape"][min(i//3, 3)])
        
        # Legs / Boots
        d.rectangle([cx - 7, cy - 14, cx - 2, cy - 2], fill=pal["boots"][1])
        d.rectangle([cx + 2, cy - 14, cx + 7, cy - 2], fill=pal["boots"][0])
        # Gold knee guards
        d.rectangle([cx - 8, cy - 12, cx - 1, cy - 9], fill=pal["gold"][1])
        d.rectangle([cx + 1, cy - 12, cx + 8, cy - 9], fill=pal["gold"][0])
        
        # Torso / Silver Armor
        d.rectangle([cx - 9, body_y, cx + 9, cy - 14], fill=pal["armor"][1])
        d.rectangle([cx - 7, body_y + 2, cx + 7, cy - 16], fill=pal["armor"][0])
        d.rectangle([cx - 4, body_y + 4, cx + 4, body_y + 12], fill=pal["armor"][0])
        # Gold lion emblem
        d.rectangle([cx - 3, body_y + 6, cx + 3, body_y + 10], fill=pal["gold"][1])
        d.point([cx, body_y + 8], fill=pal["gold"][0])
        # Blue sash
        d.line([cx - 8, cy - 16, cx + 8, cy - 16], fill=pal["cape"][1], width=2)
        
        # Shoulder Pauldrons (Silver & Gold Dragon)
        d.rectangle([cx - 14, body_y - 2, cx - 9, body_y + 6], fill=pal["gold"][1])
        d.rectangle([cx + 9, body_y - 2, cx + 14, body_y + 6], fill=pal["gold"][0])
        d.rectangle([cx - 13, body_y, cx - 10, body_y + 4], fill=pal["armor"][0])
        d.rectangle([cx + 10, body_y, cx + 13, body_y + 4], fill=pal["armor"][0])
        
        # Head / Face
        d.rectangle([cx - 5, head_y, cx + 5, body_y - 1], fill=pal["skin"][0])
        d.rectangle([cx - 4, head_y + 2, cx + 4, head_y + 7], fill=pal["skin"][0])
        # Eyes
        d.point([cx + 1, head_y + 5], fill=(30, 41, 59))
        d.point([cx + 3, head_y + 5], fill=(30, 41, 59))
        
        # Silver Circlet Helmet with white plumage
        d.rectangle([cx - 6, head_y - 3, cx + 6, head_y + 1], fill=pal["armor"][0])
        d.line([cx - 6, head_y + 1, cx + 6, head_y + 1], fill=pal["gold"][0])
        # White plumage
        d.line([cx - 2, head_y - 6, cx + 4 + cape_sway//2, head_y - 2], fill=(255, 255, 255), width=3)
        # Hair tail
        d.line([cx - 6, head_y + 2, cx - 9 + cape_sway, head_y + 12], fill=pal["hair"][0], width=2)
        
        # Spear (White Dragon Spear) held resting diagonally
        spear_tip_x = cx + 16
        spear_tip_y = head_y - 8
        d.line([cx - 12, cy - 2, spear_tip_x, spear_tip_y], fill=pal["spear"][3], width=2)
        # Silver Spearhead
        d.polygon([
            (spear_tip_x, spear_tip_y),
            (spear_tip_x + 5, spear_tip_y - 5),
            (spear_tip_x + 8, spear_tip_y - 4),
            (spear_tip_x + 4, spear_tip_y + 2)
        ], fill=pal["spear"][0])
        # Blue tassle
        d.line([spear_tip_x - 1, spear_tip_y + 2, spear_tip_x + 2, spear_tip_y + 6], fill=pal["cape"][0], width=2)
        
    elif anim_type == "attack":
        # 6-frame dynamic attack cycle (Lunging forward slash & thrust)
        progress = frame_idx / (total_frames - 1)
        lunge_x = int(math.sin(progress * math.pi) * 14)
        
        shadow_w = 14 + int(progress * 6)
        d.ellipse([cx - shadow_w + lunge_x, cy - 3, cx + shadow_w + lunge_x, cy + 3], fill=(0, 0, 0, 90))
        
        # Dynamic pose per frame
        if frame_idx in (0, 1): # Stance windup
            head_y = 18
            body_y = 30
            # Body pulling back slightly
            cx_dyn = cx - 2
            d.rectangle([cx_dyn - 8, body_y, cx_dyn + 8, cy - 12], fill=pal["armor"][1])
            d.rectangle([cx_dyn - 5, head_y, cx_dyn + 5, body_y], fill=pal["skin"][0])
            d.rectangle([cx_dyn - 6, head_y - 2, cx_dyn + 6, head_y + 1], fill=pal["armor"][0])
            # Spear drawn back
            d.line([cx_dyn - 18, body_y + 8, cx_dyn + 10, head_y - 2], fill=pal["spear"][3], width=2)
            
        elif frame_idx in (2, 3): # Full thrust & slash forward!
            head_y = 20
            body_y = 30
            cx_dyn = cx + lunge_x
            # Lunging legs
            d.line([cx_dyn - 10, cy - 2, cx_dyn - 2, body_y + 10], fill=pal["boots"][1], width=4)
            d.line([cx_dyn + 10, cy - 2, cx_dyn + 2, body_y + 10], fill=pal["boots"][0], width=4)
            # Body leaning into strike
            d.rectangle([cx_dyn - 6, body_y, cx_dyn + 12, cy - 14], fill=pal["armor"][0])
            d.rectangle([cx_dyn - 2, head_y, cx_dyn + 8, body_y], fill=pal["skin"][0])
            # Glowing blue spear thrusting forward across screen!
            d.line([cx_dyn - 4, body_y + 4, cx_dyn + 26, body_y - 2], fill=pal["spear"][1], width=3)
            d.polygon([
                (cx_dyn + 24, body_y - 5),
                (cx_dyn + 34, body_y - 2),
                (cx_dyn + 24, body_y + 1)
            ], fill=pal["spear"][0])
            # Slash Energy Smear Arc (VFX)
            d.arc([cx_dyn + 10, body_y - 18, cx_dyn + 34, body_y + 14], start=270, end=90, fill=pal["vfx"][1], width=3)
            d.arc([cx_dyn + 14, body_y - 14, cx_dyn + 32, body_y + 10], start=290, end=70, fill=pal["vfx"][0], width=2)
            
        else: # Recovery
            head_y = 17
            body_y = 29
            cx_dyn = cx + int(lunge_x * 0.5)
            d.rectangle([cx_dyn - 7, body_y, cx_dyn + 7, cy - 13], fill=pal["armor"][1])
            d.rectangle([cx_dyn - 4, head_y, cx_dyn + 4, body_y], fill=pal["skin"][0])
            d.line([cx_dyn - 8, cy - 2, cx_dyn + 18, head_y + 4], fill=pal["spear"][3], width=2)

    elif anim_type == "skill":
        # 6-frame Ultimate Skill: Dragon Ascension Pierce
        t = frame_idx / (total_frames - 1)
        cx_dyn = cx + int(math.sin(t * math.pi) * 8)
        lift_y = int(math.sin(t * math.pi) * 6)
        
        # Ground energy ring
        d.ellipse([cx - 20, cy - 4, cx + 20, cy + 4], fill=(56, 189, 248, 80))
        d.ellipse([cx - 15, cy - 2, cx + 15, cy + 2], fill=(255, 255, 255, 120))
        
        # Floating hero pose
        head_y = 14 - lift_y
        body_y = 25 - lift_y
        
        # Glowing Aura behind hero
        d.ellipse([cx_dyn - 16, head_y - 4, cx_dyn + 16, cy - lift_y], outline=pal["vfx"][1], width=2)
        
        # Hero body in mid-air spear plunge
        d.rectangle([cx_dyn - 8, body_y, cx_dyn + 8, cy - 14 - lift_y], fill=pal["armor"][0])
        d.rectangle([cx_dyn - 5, head_y, cx_dyn + 5, body_y], fill=pal["skin"][0])
        # White hair plume whipping
        d.line([cx_dyn - 2, head_y - 8, cx_dyn - 10, head_y - 2], fill=(255, 255, 255), width=3)
        
        # Radiant Dragon Spear
        d.line([cx_dyn - 6, cy - lift_y, cx_dyn + 24, head_y - 6], fill=pal["spear"][1], width=4)
        d.line([cx_dyn - 4, cy - lift_y, cx_dyn + 22, head_y - 6], fill=(255, 255, 255), width=2)
        
        # Dragon energy swirl
        dragon_radius = 12 + int(t * 8)
        d.arc([cx_dyn + 8, head_y - 12, cx_dyn + 8 + dragon_radius, head_y - 12 + dragon_radius], start=0, end=360, fill=pal["vfx"][0], width=2)

    elif anim_type == "hurt":
        # 3-frame knockback recoil
        recoil = int(frame_idx * 4)
        head_y = 18
        body_y = 30
        cx_dyn = cx - recoil
        
        d.ellipse([cx_dyn - 12, cy - 3, cx_dyn + 12, cy + 3], fill=(0, 0, 0, 80))
        # White hit flash on frame 0
        fill_col = (255, 255, 255) if frame_idx == 0 else pal["armor"][2]
        d.rectangle([cx_dyn - 8, body_y, cx_dyn + 8, cy - 14], fill=fill_col)
        d.rectangle([cx_dyn - 5, head_y, cx_dyn + 5, body_y], fill=pal["skin"][1])
        d.line([cx_dyn - 14, body_y + 12, cx_dyn + 12, body_y - 6], fill=pal["spear"][3], width=2)

    elif anim_type == "death":
        # 4-frame collapsing to knee and fading
        t = frame_idx / (total_frames - 1)
        drop_y = int(t * 12)
        alpha = int((1.0 - t * 0.7) * 255)
        
        head_y = 20 + drop_y
        body_y = 32 + drop_y
        
        d.ellipse([cx - 10, cy - 3, cx + 10, cy + 3], fill=(0, 0, 0, int(alpha * 0.4)))
        d.rectangle([cx - 7, body_y, cx + 7, cy - 4], fill=pal["armor"][2])
        d.rectangle([cx - 4, head_y, cx + 4, body_y], fill=pal["skin"][2])
        # Dropped spear on floor
        d.line([cx - 18, cy - 2, cx + 12, cy - 2], fill=pal["spear"][3], width=2)

    return img


def draw_lu_bo_frame(anim_type, frame_idx, total_frames, size=64):
    """Draw a detailed 64x64 frame for Lu Bo (War God Lu Bu)."""
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    pal = PALETTES["lu_bo"]
    
    cx = 32
    cy = 56
    
    if anim_type == "idle":
        t = (frame_idx / total_frames) * 2 * math.pi
        bob_y = int(math.sin(t) * 1.5)
        feather_sway = int(math.cos(t) * 2)
        head_y = 15 + bob_y
        body_y = 27 + bob_y
        
        # Heavy shadow
        d.ellipse([cx - 16, cy - 3, cx + 16, cy + 3], fill=(0, 0, 0, 110))
        
        # Crimson Cape in back
        for i in range(14):
            d.line([cx - 10 - i + feather_sway, body_y + i, cx - 4 + feather_sway, body_y + i + 16], fill=pal["cape"][min(i//4, 3)])
        
        # Legs / Dark Iron Greaves
        d.rectangle([cx - 8, cy - 14, cx - 2, cy - 2], fill=pal["boots"][1])
        d.rectangle([cx + 2, cy - 14, cx + 8, cy - 2], fill=pal["boots"][0])
        # Gold Demon Knee Guards
        d.rectangle([cx - 9, cy - 13, cx - 1, cy - 9], fill=pal["gold"][1])
        d.rectangle([cx + 1, cy - 13, cx + 9, cy - 9], fill=pal["gold"][0])
        
        # Torso / Dark Gold Black Iron Dragon Armor
        d.rectangle([cx - 11, body_y, cx + 11, cy - 14], fill=pal["armor"][1])
        d.rectangle([cx - 9, body_y + 2, cx + 9, cy - 15], fill=pal["armor"][0])
        # Golden Dragon Face Chestplate
        d.rectangle([cx - 5, body_y + 4, cx + 5, body_y + 12], fill=pal["gold"][1])
        d.point([cx - 2, body_y + 6], fill=(225, 29, 72)) # Demon eye ruby
        d.point([cx + 2, body_y + 6], fill=(225, 29, 72))
        
        # Shoulder Armor (Massive Spiked Gold Pauldrons)
        d.polygon([
            (cx - 17, body_y - 4), (cx - 9, body_y), (cx - 10, body_y + 8), (cx - 16, body_y + 4)
        ], fill=pal["gold"][1])
        d.polygon([
            (cx + 9, body_y), (cx + 17, body_y - 4), (cx + 16, body_y + 4), (cx + 10, body_y + 8)
        ], fill=pal["gold"][0])
        
        # Head / Fierce Face
        d.rectangle([cx - 5, head_y, cx + 5, body_y - 1], fill=pal["skin"][0])
        # Dark eyes & scowl
        d.line([cx - 3, head_y + 5, cx - 1, head_y + 6], fill=(15, 10, 20), width=1)
        d.line([cx + 1, head_y + 6, cx + 3, head_y + 5], fill=(15, 10, 20), width=1)
        
        # Golden Demon Crown & Two Long Pheasant Feathers
        d.rectangle([cx - 7, head_y - 3, cx + 7, head_y + 1], fill=pal["gold"][0])
        # Long Feathers curving high into the air!
        d.line([cx - 3, head_y - 3, cx - 8 + feather_sway, head_y - 14], fill=pal["cape"][0], width=2)
        d.line([cx + 3, head_y - 3, cx + 10 + feather_sway, head_y - 15], fill=pal["cape"][1], width=2)
        
        # Sky Piercer Halberd (Phuong Thien Hoa Kich)
        hx = cx + 18
        hy = head_y - 6
        d.line([cx - 10, cy - 2, hx, hy], fill=(120, 113, 108), width=3)
        # Crescent Blade on Halberd
        d.polygon([(hx, hy), (hx + 6, hy - 8), (hx + 9, hy - 4), (hx + 4, hy + 2)], fill=pal["halberd"][2])
        # Crimson crescent wing
        d.arc([hx - 2, hy - 10, hx + 10, hy + 2], start=200, end=340, fill=pal["halberd"][0], width=3)

    elif anim_type == "attack":
        progress = frame_idx / (total_frames - 1)
        slash_x = int(math.sin(progress * math.pi) * 16)
        
        d.ellipse([cx - 16 + slash_x, cy - 3, cx + 16 + slash_x, cy + 3], fill=(0, 0, 0, 100))
        
        if frame_idx in (0, 1): # Power windup overhead
            head_y = 16
            body_y = 28
            cx_dyn = cx - 3
            d.rectangle([cx_dyn - 10, body_y, cx_dyn + 10, cy - 12], fill=pal["armor"][1])
            d.rectangle([cx_dyn - 5, head_y, cx_dyn + 5, body_y], fill=pal["skin"][0])
            # Halberd raised high behind head
            d.line([cx_dyn - 14, cy - 8, cx_dyn - 2, head_y - 14], fill=(120, 113, 108), width=3)
            d.arc([cx_dyn - 10, head_y - 18, cx_dyn + 4, head_y - 6], start=180, end=360, fill=pal["halberd"][0], width=3)
            
        elif frame_idx in (2, 3): # Devastating cleave!
            head_y = 19
            body_y = 29
            cx_dyn = cx + slash_x
            d.rectangle([cx_dyn - 8, body_y, cx_dyn + 12, cy - 13], fill=pal["armor"][0])
            d.rectangle([cx_dyn - 3, head_y, cx_dyn + 7, body_y], fill=pal["skin"][0])
            # Massive Crimson Slash Wave!
            d.line([cx_dyn - 4, body_y - 12, cx_dyn + 26, cy - 4], fill=(120, 113, 108), width=3)
            d.arc([cx_dyn + 4, cy - 28, cx_dyn + 34, cy + 2], start=250, end=90, fill=pal["vfx"][1], width=4)
            d.arc([cx_dyn + 8, cy - 24, cx_dyn + 32, cy - 2], start=270, end=70, fill=(255, 255, 255), width=2)
            
        else: # Impact followthrough
            head_y = 18
            body_y = 28
            cx_dyn = cx + int(slash_x * 0.6)
            d.rectangle([cx_dyn - 9, body_y, cx_dyn + 9, cy - 13], fill=pal["armor"][1])
            d.rectangle([cx_dyn - 4, head_y, cx_dyn + 4, body_y], fill=pal["skin"][0])
            d.line([cx_dyn - 6, cy - 4, cx_dyn + 20, cy - 2], fill=(120, 113, 108), width=3)

    elif anim_type == "skill":
        # 6-frame Demonic Meteor Strike
        t = frame_idx / (total_frames - 1)
        jump_y = int(math.sin(t * math.pi) * 10)
        cx_dyn = cx + int(math.sin(t * math.pi) * 6)
        
        # Ground crater glow
        d.ellipse([cx - 22, cy - 4, cx + 22, cy + 4], fill=(225, 29, 72, 90))
        d.ellipse([cx - 14, cy - 2, cx + 14, cy + 2], fill=(254, 205, 211, 140))
        
        head_y = 14 - jump_y
        body_y = 26 - jump_y
        
        # Demonic Fire Aura around Lu Bu
        d.ellipse([cx_dyn - 18, head_y - 6, cx_dyn + 18, cy - jump_y], outline=pal["vfx"][1], width=2)
        d.ellipse([cx_dyn - 14, head_y - 2, cx_dyn + 14, cy - 4 - jump_y], outline=(253, 224, 71), width=1)
        
        # Floating cleave pose
        d.rectangle([cx_dyn - 10, body_y, cx_dyn + 10, cy - 14 - jump_y], fill=pal["armor"][0])
        d.rectangle([cx_dyn - 5, head_y, cx_dyn + 5, body_y], fill=pal["skin"][0])
        # Crimson Feathers blazing
        d.line([cx_dyn - 3, head_y - 3, cx_dyn - 14, head_y - 18], fill=pal["cape"][0], width=3)
        d.line([cx_dyn + 3, head_y - 3, cx_dyn + 14, head_y - 18], fill=pal["cape"][0], width=3)
        
        # Fiery Halberd plunging downward
        d.line([cx_dyn - 2, head_y - 12, cx_dyn + 24, cy - jump_y + 4], fill=pal["halberd"][0], width=4)
        d.line([cx_dyn, head_y - 10, cx_dyn + 22, cy - jump_y + 2], fill=(255, 255, 255), width=2)

    elif anim_type == "hurt":
        recoil = int(frame_idx * 4)
        head_y = 17
        body_y = 29
        cx_dyn = cx - recoil
        d.ellipse([cx_dyn - 14, cy - 3, cx_dyn + 14, cy + 3], fill=(0, 0, 0, 80))
        fill_col = (255, 255, 255) if frame_idx == 0 else pal["armor"][2]
        d.rectangle([cx_dyn - 10, body_y, cx_dyn + 10, cy - 14], fill=fill_col)
        d.rectangle([cx_dyn - 5, head_y, cx_dyn + 5, body_y], fill=pal["skin"][1])
        d.line([cx_dyn - 16, cy - 6, cx_dyn + 10, body_y - 4], fill=(120, 113, 108), width=3)

    elif anim_type == "death":
        t = frame_idx / (total_frames - 1)
        drop_y = int(t * 12)
        alpha = int((1.0 - t * 0.7) * 255)
        head_y = 19 + drop_y
        body_y = 31 + drop_y
        d.ellipse([cx - 12, cy - 3, cx + 12, cy + 3], fill=(0, 0, 0, int(alpha * 0.4)))
        d.rectangle([cx - 9, body_y, cx + 9, cy - 4], fill=pal["armor"][2])
        d.rectangle([cx - 5, head_y, cx + 5, body_y], fill=pal["skin"][2])
        d.line([cx - 20, cy - 2, cx + 14, cy - 2], fill=(120, 113, 108), width=3)

    return img


def generate_hero_spritesheet(hero_id, output_path, frame_size=64):
    """
    Generates a full 24-frame (4 rows x 6 columns) HD-2D spritesheet:
    Row 0: Idle (6 frames)
    Row 1: Attack (6 frames)
    Row 2: Skill (6 frames)
    Row 3: Hurt (3 frames) + Death (3 frames)
    """
    draw_fn = draw_trieu_van_frame if hero_id == "trieu_van" else draw_lu_bo_frame
    
    cols = 6
    rows = 4
    sheet = Image.new("RGBA", (cols * frame_size, rows * frame_size), (0, 0, 0, 0))
    
    # 1. Idle (6 frames)
    for f in range(6):
        fr = draw_fn("idle", f, 6, frame_size)
        sheet.paste(fr, (f * frame_size, 0 * frame_size))
        
    # 2. Attack (6 frames)
    for f in range(6):
        fr = draw_fn("attack", f, 6, frame_size)
        sheet.paste(fr, (f * frame_size, 1 * frame_size))
        
    # 3. Skill (6 frames)
    for f in range(6):
        fr = draw_fn("skill", f, 6, frame_size)
        sheet.paste(fr, (f * frame_size, 2 * frame_size))
        
    # 4. Hurt (3 frames) + Death (3 frames)
    for f in range(3):
        fr = draw_fn("hurt", f, 3, frame_size)
        sheet.paste(fr, (f * frame_size, 3 * frame_size))
        
    for f in range(3):
        fr = draw_fn("death", f, 3, frame_size)
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


if __name__ == "__main__":
    import sys
    hero = sys.argv[1] if len(sys.argv) > 1 else "trieu_van"
    out_img = sys.argv[2] if len(sys.argv) > 2 else f"assets/heroes/{hero}/spritesheet_hd.png"
    out_tres = sys.argv[3] if len(sys.argv) > 3 else f"assets/heroes/{hero}/sprite_frames.tres"
    res_tex = f"res://assets/heroes/{hero}/spritesheet_hd.png"
    
    generate_hero_spritesheet(hero, out_img, 64)
    generate_godot_spriteframes(hero, res_tex, out_tres, 64)
