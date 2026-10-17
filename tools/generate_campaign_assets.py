#!/usr/bin/env python3
"""
tools/generate_campaign_assets.py
Generates pixel art assets for Phase 3 Campaign Map (Three Kingdoms PvE Map).
Follows pixel-art-pipeline standards (16-bit JRPG aesthetic, crisp palettes).
"""

import os
from PIL import Image, ImageDraw, ImageFont

OUTPUT_DIR = "assets/ui/activities/campaign"
os.makedirs(OUTPUT_DIR, exist_ok=True)

def create_circle_medallion(size, rim_color, fill_color, icon_func, is_locked=False):
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    pad = 4

    # Shadow
    draw.ellipse([pad + 2, pad + 4, size - pad + 2, size - pad + 4], fill=(10, 12, 16, 160))

    # Outer Rim
    draw.ellipse([pad, pad, size - pad, size - pad], fill=rim_color)
    # Inner border
    draw.ellipse([pad + 3, pad + 3, size - pad - 3, size - pad - 3], fill=(20, 24, 32, 255))
    # Fill
    draw.ellipse([pad + 6, pad + 6, size - pad - 6, size - pad - 6], fill=fill_color)

    # Specular shine on upper rim
    draw.arc([pad + 2, pad + 2, size - pad - 2, size - pad - 2], start=190, end=350, fill=(255, 255, 255, 140), width=2)

    # Draw specific icon
    icon_func(draw, size, size)

    if is_locked:
        # Dark overlay
        overlay = Image.new("RGBA", (size, size), (0, 0, 0, 110))
        img = Image.alpha_composite(img, overlay)
    return img

def draw_flag_icon(draw, w, h):
    cx, cy = w // 2, h // 2
    # Flagpole
    draw.rectangle([cx - 8, cy - 14, cx - 6, cy + 14], fill=(220, 190, 120))
    draw.ellipse([cx - 10, cy - 17, cx - 4, cy - 11], fill=(255, 215, 0))
    # Flag fabric
    draw.polygon([(cx - 6, cy - 14), (cx + 12, cy - 8), (cx - 6, cy - 2)], fill=(217, 83, 79))
    draw.polygon([(cx - 5, cy - 12), (cx + 8, cy - 8), (cx - 5, cy - 4)], fill=(255, 195, 0))

def draw_skull_icon(draw, w, h):
    cx, cy = w // 2, h // 2
    # Skull crown
    draw.polygon([(cx - 12, cy - 10), (cx - 8, cy - 18), (cx - 3, cy - 12), (cx, cy - 18), (cx + 3, cy - 12), (cx + 8, cy - 18), (cx + 12, cy - 10)], fill=(255, 215, 0))
    # Skull head
    draw.ellipse([cx - 11, cy - 8, cx + 11, cy + 6], fill=(245, 240, 235))
    # Jaw
    draw.rectangle([cx - 7, cy + 3, cx + 7, cy + 12], fill=(230, 225, 220))
    # Eye sockets
    draw.ellipse([cx - 8, cy - 3, cx - 2, cy + 3], fill=(160, 20, 20))
    draw.ellipse([cx + 2, cy - 3, cx + 8, cy + 3], fill=(160, 20, 20))
    # Teeth
    draw.line([cx - 4, cy + 7, cx - 4, cy + 11], fill=(80, 80, 80), width=1)
    draw.line([cx, cy + 7, cx, cy + 11], fill=(80, 80, 80), width=1)
    draw.line([cx + 4, cy + 7, cx + 4, cy + 11], fill=(80, 80, 80), width=1)

def draw_chest_icon(draw, w, h):
    cx, cy = w // 2, h // 2
    # Treasure chest
    draw.rectangle([cx - 13, cy - 6, cx + 13, cy + 10], fill=(180, 110, 40))
    draw.rectangle([cx - 14, cy - 12, cx + 14, cy - 4], fill=(210, 140, 50))
    # Gold trims
    draw.rectangle([cx - 14, cy - 12, cx - 11, cy + 10], fill=(255, 215, 0))
    draw.rectangle([cx + 11, cy - 12, cx + 14, cy + 10], fill=(255, 215, 0))
    # Lock
    draw.rectangle([cx - 3, cy - 5, cx + 3, cy + 2], fill=(255, 215, 0))
    draw.rectangle([cx - 1, cy - 2, cx + 1, cy + 1], fill=(30, 30, 30))

def draw_lock_icon(draw, w, h):
    cx, cy = w // 2, h // 2
    # Shackle
    draw.arc([cx - 8, cy - 15, cx + 8, cy + 1], start=180, end=360, fill=(180, 185, 195), width=3)
    # Body
    draw.rectangle([cx - 11, cy - 3, cx + 11, cy + 12], fill=(120, 125, 135))
    draw.rectangle([cx - 9, cy - 1, cx + 9, cy + 10], fill=(85, 90, 100))
    # Keyhole
    draw.ellipse([cx - 3, cy + 1, cx + 3, cy + 6], fill=(25, 25, 30))
    draw.polygon([(cx - 2, cy + 4), (cx + 2, cy + 4), (cx + 1, cy + 8), (cx - 1, cy + 8)], fill=(25, 25, 30))

def generate_nodes():
    # 1. Normal Node
    norm = create_circle_medallion(80, (230, 185, 60), (35, 75, 125), draw_flag_icon)
    norm.save(os.path.join(OUTPUT_DIR, "stage_node_normal.png"))

    # 2. Boss Node
    boss = create_circle_medallion(80, (235, 60, 60), (100, 20, 25), draw_skull_icon)
    boss.save(os.path.join(OUTPUT_DIR, "stage_node_boss.png"))

    # 3. Resource Node
    res = create_circle_medallion(80, (245, 200, 40), (40, 110, 60), draw_chest_icon)
    res.save(os.path.join(OUTPUT_DIR, "stage_node_resource.png"))

    # 4. Locked Node
    locked = create_circle_medallion(80, (110, 115, 125), (45, 50, 60), draw_lock_icon, is_locked=True)
    locked.save(os.path.join(OUTPUT_DIR, "stage_node_locked.png"))

def generate_stars():
    # Filled star 24x24
    img_filled = Image.new("RGBA", (24, 24), (0, 0, 0, 0))
    draw_f = ImageDraw.Draw(img_filled)
    pts_f = [(12, 1), (15, 8), (23, 9), (17, 14), (19, 22), (12, 17), (5, 22), (7, 14), (1, 9), (9, 8)]
    # Shadow
    draw_f.polygon([(x, y + 1) for x, y in pts_f], fill=(120, 70, 0, 180))
    # Gold body
    draw_f.polygon(pts_f, fill=(255, 215, 0))
    # Highlight
    draw_f.polygon([(12, 2), (14, 8), (12, 15), (10, 8)], fill=(255, 250, 180))
    img_filled.save(os.path.join(OUTPUT_DIR, "star_filled.png"))

    # Empty star 24x24
    img_empty = Image.new("RGBA", (24, 24), (0, 0, 0, 0))
    draw_e = ImageDraw.Draw(img_empty)
    pts_e = [(12, 1), (15, 8), (23, 9), (17, 14), (19, 22), (12, 17), (5, 22), (7, 14), (1, 9), (9, 8)]
    draw_e.polygon([(x, y + 1) for x, y in pts_e], fill=(15, 18, 24, 150))
    draw_e.polygon(pts_e, fill=(60, 65, 75))
    # Inner cutout for hollow star look
    inner_pts = [(12, 5), (14, 9), (19, 10), (15, 13), (16, 18), (12, 15), (8, 18), (9, 13), (5, 10), (10, 9)]
    draw_e.polygon(inner_pts, fill=(35, 38, 45))
    img_empty.save(os.path.join(OUTPUT_DIR, "star_empty.png"))

def generate_path_line():
    img = Image.new("RGBA", (32, 8), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    # Dotted golden line
    draw.rounded_rectangle([2, 1, 14, 7], radius=2, fill=(255, 215, 0))
    draw.rounded_rectangle([18, 1, 30, 7], radius=2, fill=(255, 215, 0))
    img.save(os.path.join(OUTPUT_DIR, "stage_path_line.png"))

def generate_buttons():
    # Sweep Button (140x44)
    sweep = Image.new("RGBA", (140, 44), (0, 0, 0, 0))
    draw_s = ImageDraw.Draw(sweep)
    # Shadow
    draw_s.rounded_rectangle([2, 4, 138, 44], radius=6, fill=(10, 14, 20, 180))
    # Border & Fill (Royal Blue / Cyan)
    draw_s.rounded_rectangle([0, 0, 136, 40], radius=6, fill=(20, 75, 140))
    draw_s.rounded_rectangle([2, 2, 134, 38], radius=5, fill=(30, 110, 190))
    draw_s.line([4, 3, 132, 3], fill=(100, 180, 255), width=2)
    sweep.save(os.path.join(OUTPUT_DIR, "sweep_button.png"))

    # Challenge Button (160x44)
    chal = Image.new("RGBA", (160, 44), (0, 0, 0, 0))
    draw_c = ImageDraw.Draw(chal)
    # Shadow
    draw_c.rounded_rectangle([2, 4, 158, 44], radius=6, fill=(15, 5, 5, 200))
    # Border & Fill (Crimson & Gold)
    draw_c.rounded_rectangle([0, 0, 156, 40], radius=6, fill=(255, 215, 0))
    draw_c.rounded_rectangle([2, 2, 154, 38], radius=5, fill=(185, 28, 28))
    draw_c.line([4, 3, 152, 3], fill=(255, 120, 120), width=2)
    chal.save(os.path.join(OUTPUT_DIR, "challenge_button.png"))

def generate_popup_frame():
    img = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    # Outer dark border
    draw.rounded_rectangle([0, 0, 127, 127], radius=8, fill=(15, 18, 24))
    # Golden border
    draw.rounded_rectangle([2, 2, 125, 125], radius=7, outline=(220, 180, 50), width=2)
    # Inner background
    draw.rounded_rectangle([6, 6, 121, 121], radius=5, fill=(25, 30, 40))
    # Corner rivets
    for x, y in [(8, 8), (119, 8), (8, 119), (119, 119)]:
        draw.ellipse([x - 2, y - 2, x + 2, y + 2], fill=(255, 215, 0))
    img.save(os.path.join(OUTPUT_DIR, "stage_popup_frame.png"))

def generate_chapter_backgrounds():
    # Chapter 1: Ancient Plains & Fortress (1280x720)
    bg1 = Image.new("RGBA", (1280, 720), (22, 26, 35))
    d1 = ImageDraw.Draw(bg1)

    # Gradient sky / atmosphere
    for y in range(0, 320):
        t = y / 320.0
        r = int(28 + t * 25)
        g = int(35 + t * 40)
        b = int(48 + t * 40)
        d1.line([(0, y), (1280, y)], fill=(r, g, b))

    # Mountain ranges in silhouette
    mountains = [
        [(0, 360), (140, 260), (320, 340), (510, 230), (740, 350), (980, 220), (1140, 290), (1280, 250), (1280, 480), (0, 480)],
        [(0, 420), (200, 330), (450, 410), (680, 310), (890, 400), (1100, 330), (1280, 380), (1280, 560), (0, 560)]
    ]
    d1.polygon(mountains[0], fill=(38, 55, 65))
    d1.polygon(mountains[1], fill=(28, 42, 52))

    # Tactical terrain ground
    for y in range(460, 720):
        t = (y - 460) / 260.0
        r = int(35 + t * 15)
        g = int(45 + t * 20)
        b = int(42 + t * 15)
        d1.line([(0, y), (1280, y)], fill=(r, g, b))

    # Rivers & Tactical contour lines
    d1.arc([100, 480, 1180, 780], start=10, end=170, fill=(30, 85, 120, 160), width=18)
    d1.arc([150, 520, 1100, 820], start=20, end=160, fill=(45, 120, 165, 180), width=10)

    # Tactical grid watermark
    for gx in range(0, 1280, 80):
        d1.line([(gx, 440), (gx, 720)], fill=(255, 255, 255, 12), width=1)
    for gy in range(440, 720, 60):
        d1.line([(0, gy), (1280, gy)], fill=(255, 255, 255, 12), width=1)

    # Vignette border
    bg1.save(os.path.join(OUTPUT_DIR, "chapter_bg_ch01.png"))

    # Chapter 2: Red Cliffs / Yangtze River (1280x720)
    bg2 = Image.new("RGBA", (1280, 720), (18, 22, 32))
    d2 = ImageDraw.Draw(bg2)
    # Sunset fiery sky
    for y in range(0, 340):
        t = y / 340.0
        r = int(55 + t * 65)
        g = int(25 + t * 35)
        b = int(35 + t * 20)
        d2.line([(0, y), (1280, y)], fill=(r, g, b))

    # Dark cliffs
    cliffs = [(0, 320), (250, 240), (450, 340), (700, 260), (950, 360), (1280, 270), (1280, 480), (0, 480)]
    d2.polygon(cliffs, fill=(40, 25, 30))

    # Water with fiery reflection
    for y in range(400, 720):
        t = (y - 400) / 320.0
        r = int(45 + t * 25)
        g = int(35 + t * 20)
        b = int(55 + t * 25)
        d2.line([(0, y), (1280, y)], fill=(r, g, b))

    # Fiery highlights on waves
    d2.ellipse([400, 520, 880, 580], fill=(180, 60, 30, 80))
    d2.ellipse([450, 540, 830, 570], fill=(240, 120, 40, 110))

    bg2.save(os.path.join(OUTPUT_DIR, "chapter_bg_ch02.png"))

def main():
    print("🎨 Generating Campaign Map Pixel Art Assets...")
    generate_nodes()
    generate_stars()
    generate_path_line()
    generate_buttons()
    generate_popup_frame()
    generate_chapter_backgrounds()
    print("✅ All Campaign Map assets generated successfully!")

if __name__ == "__main__":
    main()
