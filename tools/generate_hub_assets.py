#!/usr/bin/env python3
import os
import math
from PIL import Image, ImageDraw, ImageFont

OUT_DIR = "assets/ui/hub"
os.makedirs(OUT_DIR, exist_ok=True)

def draw_beveled_box(draw, bounds, bg_color, border_outer, border_inner, corner_color=None):
    x0, y0, x1, y1 = bounds
    draw.rectangle([x0+2, y0+2, x1-2, y1-2], fill=bg_color)
    draw.rectangle([x0, y0, x1, y1], outline=border_outer, width=1)
    draw.rectangle([x0+1, y0+1, x1-1, y1-1], outline=border_inner, width=1)
    if corner_color:
        for cx, cy in [(x0+1, y0+1), (x1-1, y0+1), (x0+1, y1-1), (x1-1, y1-1)]:\
            draw.point((cx, cy), fill=corner_color)

# 1. topbar_bg.png (1280x64)
def gen_topbar_bg():
    w, h = 1280, 64
    im = Image.new("RGBA", (w, h), (18, 14, 22, 245))
    draw = ImageDraw.Draw(im)
    # Subtle gradient bands
    for y in range(h):
        alpha = int(240 + (y / h) * 15)
        r = int(22 - (y / h) * 8)
        g = int(16 - (y / h) * 6)
        b = int(26 - (y / h) * 10)
        draw.line([(0, y), (w, y)], fill=(r, g, b, alpha))
    # Top & bottom gold borders
    draw.line([(0, 0), (w, 0)], fill=(212, 175, 55, 255), width=2)
    draw.line([(0, 2), (w, 2)], fill=(120, 95, 35, 255), width=1)
    draw.line([(0, h-2), (w, h-2)], fill=(120, 95, 35, 255), width=1)
    draw.line([(0, h-1), (w, h-1)], fill=(212, 175, 55, 255), width=2)
    # Cloud filigree accents every 64px
    for x in range(32, w - 32, 64):
        draw.line([(x-8, 3), (x+8, 3)], fill=(240, 215, 120, 160), width=1)
        draw.line([(x-8, h-4), (x+8, h-4)], fill=(240, 215, 120, 160), width=1)
    im.save(os.path.join(OUT_DIR, "topbar_bg.png"))

# 2. topbar_avatar_frame.png (72x72)
def gen_avatar_frame():
    w, h = 72, 72
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(im)
    cx, cy = w // 2, h // 2
    # Outer ornate rim
    draw.ellipse([2, 2, w-3, h-3], outline=(225, 185, 75, 255), width=3)
    draw.ellipse([5, 5, w-6, h-6], outline=(90, 25, 30, 255), width=2)
    draw.ellipse([7, 7, w-8, h-8], outline=(180, 145, 50, 255), width=1)
    # 4 cardinal dragon claws/gems
    gem_color = (220, 40, 40, 255)
    for px, py in [(cx, 4), (cx, h-5), (4, cy), (w-5, cy)]:
        draw.ellipse([px-3, py-3, px+3, py+3], fill=gem_color, outline=(255, 220, 100, 255))
    im.save(os.path.join(OUT_DIR, "topbar_avatar_frame.png"))

# 3. vip_badge_1 .. vip_badge_15 (48x24)
def gen_vip_badges():
    for v in range(1, 16):
        w, h = 48, 24
        im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        draw = ImageDraw.Draw(im)
        # Gradient background
        is_high = v >= 10
        bg_col = (130, 20, 30, 255) if not is_high else (110, 25, 130, 255)
        border_col = (245, 215, 80, 255)
        draw.rounded_rectangle([1, 1, w-2, h-2], radius=4, fill=bg_col, outline=border_col, width=1)
        # VIP text simulation
        # Draw "V"
        draw.line([(6, 6), (11, 17)], fill=(255, 240, 150, 255), width=2)
        draw.line([(11, 17), (16, 6)], fill=(255, 240, 150, 255), width=2)
        # Draw "I"
        draw.line([(19, 6), (19, 17)], fill=(255, 240, 150, 255), width=2)
        # Draw "P"
        draw.line([(23, 6), (23, 17)], fill=(255, 240, 150, 255), width=2)
        draw.arc([22, 6, 29, 12], -90, 90, fill=(255, 240, 150, 255), width=2)
        # Draw number
        num_str = str(v)
        offset_x = 32
        for char in num_str:
            if char == '1':
                draw.line([(offset_x+2, 6), (offset_x+2, 17)], fill=(255, 255, 255, 255), width=2)
                offset_x += 5
            elif char == '0':
                draw.rectangle([offset_x, 6, offset_x+5, 17], outline=(255, 255, 255, 255), width=1)
                offset_x += 7
            else:
                draw.rectangle([offset_x, 6, offset_x+5, 17], outline=(255, 255, 255, 255), width=1)
                offset_x += 7
        im.save(os.path.join(OUT_DIR, f"vip_badge_{v}.png"))

# 4. Currency icons (28x28)
def gen_currency_icons():
    # Gold (KNB / Ingot)
    im = Image.new("RGBA", (28, 28), (0, 0, 0, 0))
    draw = ImageDraw.Draw(im)
    draw.polygon([(4, 18), (24, 18), (21, 24), (7, 24)], fill=(185, 140, 20, 255), outline=(240, 200, 50, 255))
    draw.ellipse([3, 10, 25, 20], fill=(255, 215, 60, 255), outline=(255, 245, 180, 255))
    draw.ellipse([8, 12, 20, 18], fill=(230, 180, 40, 255))
    draw.point((14, 14), fill=(255, 255, 255, 255))
    im.save(os.path.join(OUT_DIR, "currency_icon_gold.png"))

    # Diamond (Gemstone)
    im2 = Image.new("RGBA", (28, 28), (0, 0, 0, 0))
    draw2 = ImageDraw.Draw(im2)
    draw2.polygon([(8, 6), (20, 6), (25, 13), (14, 25), (3, 13)], fill=(40, 180, 240, 255), outline=(180, 240, 255, 255))
    draw2.polygon([(8, 6), (20, 6), (17, 13), (11, 13)], fill=(100, 210, 255, 255))
    draw2.polygon([(11, 13), (17, 13), (14, 25)], fill=(25, 140, 200, 255))
    draw2.line([(14, 25), (3, 13), (11, 13)], fill=(180, 240, 255, 255))
    draw2.line([(14, 25), (25, 13), (17, 13)], fill=(180, 240, 255, 255))
    draw2.point((12, 9), fill=(255, 255, 255, 255))
    im2.save(os.path.join(OUT_DIR, "currency_icon_diamond.png"))

    # Stamina (Tiger tally / Token)
    im3 = Image.new("RGBA", (28, 28), (0, 0, 0, 0))
    draw3 = ImageDraw.Draw(im3)
    draw3.rounded_rectangle([4, 4, 24, 24], radius=4, fill=(160, 40, 45, 255), outline=(245, 210, 75, 255), width=2)
    # Winged flame / tiger crest
    draw3.line([(14, 7), (14, 21)], fill=(255, 230, 120, 255), width=2)
    draw3.line([(9, 13), (19, 13)], fill=(255, 230, 120, 255), width=2)
    draw3.polygon([(14, 7), (11, 11), (17, 11)], fill=(255, 245, 180, 255))
    im3.save(os.path.join(OUT_DIR, "currency_icon_stamina.png"))

# 5. shortcut_circle_frame.png (64x64) and dot
def gen_shortcut_frame():
    w, h = 64, 64
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(im)
    draw.ellipse([2, 2, w-3, h-3], fill=(28, 22, 35, 230), outline=(215, 175, 60, 255), width=2)
    draw.ellipse([5, 5, w-6, h-6], outline=(100, 75, 30, 255), width=1)
    # 4 little studs
    for x, y in [(w//2, 4), (w//2, h-5), (4, h//2), (w-5, h//2)]:
        draw.ellipse([x-2, y-2, x+2, y+2], fill=(255, 225, 95, 255))
    im.save(os.path.join(OUT_DIR, "shortcut_circle_frame.png"))

    # Notification dot (16x16)
    dot = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    draw_dot = ImageDraw.Draw(dot)
    draw_dot.ellipse([1, 1, 14, 14], fill=(230, 35, 35, 255), outline=(255, 220, 120, 255), width=1)
    draw_dot.point((5, 5), fill=(255, 150, 150, 255))
    dot.save(os.path.join(OUT_DIR, "shortcut_notification_dot.png"))

# 6. 10 Shortcut Icons (64x64)
def gen_shortcut_icons():
    shortcuts = [
        ("shortcut_hoat_dong.png", (210, 80, 50), "ACT"),
        ("shortcut_su_kien.png", (230, 140, 30), "EVT"),
        ("shortcut_khanh_dien.png", (220, 50, 120), "CEL"),
        ("shortcut_dau_gia.png", (210, 180, 40), "AUC"),
        ("shortcut_quoc_gia.png", (50, 130, 210), "NAT"),
        ("shortcut_tuyet_lo.png", (180, 40, 40), "DES"),
        ("shortcut_long_dien.png", (160, 60, 220), "PAL"),
        ("shortcut_di_canh.png", (50, 170, 140), "REL"),
        ("shortcut_tu_luyen.png", (60, 160, 70), "CUL"),
        ("shortcut_donate.png", (230, 190, 50), "GFT"),
    ]
    for filename, tint, label in shortcuts:
        im = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
        draw = ImageDraw.Draw(im)
        # Background circle
        draw.ellipse([4, 4, 59, 59], fill=(tint[0]//4, tint[1]//4, tint[2]//4, 240), outline=(215, 175, 60, 255), width=2)
        draw.ellipse([8, 8, 55, 55], outline=(tint[0]//2, tint[1]//2, tint[2]//2, 180), width=1)
        # Inner emblem
        cx, cy = 32, 32
        if "hoat_dong" in filename:
            # Scroll/drum
            draw.rounded_rectangle([20, 18, 44, 46], radius=4, fill=tint, outline=(255, 230, 140, 255))
            draw.line([(24, 26), (40, 26)], fill=(255, 255, 255, 255), width=2)
            draw.line([(24, 32), (40, 32)], fill=(255, 255, 255, 255), width=2)
            draw.line([(24, 38), (34, 38)], fill=(255, 255, 255, 255), width=2)
        elif "su_kien" in filename:
            # Flame / banner
            draw.polygon([(cx, 16), (46, 46), (cx, 40), (18, 46)], fill=tint, outline=(255, 235, 150, 255))
            draw.polygon([(cx, 24), (38, 42), (cx, 38), (26, 42)], fill=(255, 240, 100, 255))
        elif "khanh_dien" in filename:
            # Crown
            draw.polygon([(18, 44), (46, 44), (44, 24), (38, 32), (cx, 18), (26, 32), (20, 24)], fill=tint, outline=(255, 235, 150, 255))
            draw.ellipse([cx-3, 16, cx+3, 22], fill=(255, 255, 100, 255))
        elif "dau_gia" in filename:
            # Gavel
            draw.rectangle([18, 22, 34, 30], fill=tint, outline=(255, 235, 150, 255))
            draw.line([(26, 30), (44, 46)], fill=(215, 175, 60, 255), width=4)
        elif "quoc_gia" in filename:
            # Castle flag
            draw.line([(20, 16), (20, 48)], fill=(215, 175, 60, 255), width=3)
            draw.polygon([(22, 18), (46, 26), (22, 34)], fill=tint, outline=(255, 240, 150, 255))
        elif "tuyet_lo" in filename:
            # Crossed swords
            draw.line([(18, 18), (46, 46)], fill=(240, 240, 255, 255), width=3)
            draw.line([(46, 18), (18, 46)], fill=(240, 240, 255, 255), width=3)
            draw.ellipse([cx-5, cy-5, cx+5, cy+5], fill=tint)
        elif "long_dien" in filename:
            # Dragon / pagoda
            draw.polygon([(cx, 16), (48, 28), (16, 28)], fill=tint, outline=(255, 235, 150, 255))
            draw.rectangle([22, 28, 42, 46], fill=(50, 30, 70, 255), outline=(215, 175, 60, 255))
        elif "di_canh" in filename:
            # Compass
            draw.ellipse([18, 18, 46, 46], outline=tint, width=2)
            draw.polygon([(cx, 18), (36, cy), (cx, 26), (28, cy)], fill=(255, 70, 70, 255))
            draw.polygon([(cx, 46), (36, cy), (cx, 38), (28, cy)], fill=(120, 180, 255, 255))
        elif "tu_luyen" in filename:
            # Yin yang / lotus
            draw.ellipse([18, 18, 46, 46], fill=(240, 240, 240, 255), outline=(215, 175, 60, 255), width=2)
            draw.pieslice([18, 18, 46, 46], -90, 90, fill=(40, 40, 50, 255))
        else: # donate
            # Treasure chest / gift
            draw.rectangle([18, 26, 46, 46], fill=tint, outline=(255, 235, 150, 255), width=2)
            draw.arc([18, 18, 46, 34], 180, 360, fill=(255, 235, 150, 255), width=2)
            draw.line([(cx, 18), (cx, 46)], fill=(255, 235, 150, 255), width=2)

        im.save(os.path.join(OUT_DIR, filename))

# 7. bottom_nav_bg.png (1280x70) & active tab indicator
def gen_bottom_nav():
    w, h = 1280, 70
    im = Image.new("RGBA", (w, h), (18, 14, 22, 250))
    draw = ImageDraw.Draw(im)
    for y in range(h):
        alpha = int(245 + (y / h) * 10)
        draw.line([(0, y), (w, y)], fill=(22 - (y//8), 16 - (y//10), 28 - (y//6), alpha))
    draw.line([(0, 0), (w, 0)], fill=(212, 175, 55, 255), width=2)
    draw.line([(0, 2), (w, 2)], fill=(120, 95, 35, 255), width=1)
    im.save(os.path.join(OUT_DIR, "bottom_nav_bg.png"))

    # Active tab indicator (80x4)
    ind = Image.new("RGBA", (80, 4), (255, 215, 65, 255))
    draw_ind = ImageDraw.Draw(ind)
    draw_ind.line([(0, 0), (79, 0)], fill=(255, 245, 180, 255), width=1)
    ind.save(os.path.join(OUT_DIR, "bottom_nav_tab_active.png"))

# 8. 11 Bottom Nav Icons (36x36)
def gen_bottom_nav_icons():
    nav_icons = [
        ("icon_hero.png", "Võ Tướng", (220, 60, 60)),
        ("icon_item.png", "Bảo Vật", (220, 180, 50)),
        ("icon_arena.png", "Thí Đấu", (230, 90, 40)),
        ("icon_map.png", "Địa Chỉ", (80, 180, 120)),
        ("icon_bag.png", "Túi", (180, 120, 60)),
        ("icon_guild.png", "Bang Hội", (60, 140, 220)),
        ("icon_pvp.png", "Võ Thành", (200, 60, 180)),
        ("icon_pet.png", "Linh Thú", (60, 200, 180)),
        ("icon_upgrade.png", "Cường Hóa", (220, 140, 40)),
        ("icon_formation.png", "Quân Đoàn", (230, 190, 60)),
        ("icon_wedding.png", "Thú Cưỡi", (210, 80, 130)),
    ]
    for filename, name, col in nav_icons:
        im = Image.new("RGBA", (36, 36), (0, 0, 0, 0))
        draw = ImageDraw.Draw(im)
        cx, cy = 18, 18
        if "hero" in filename:
            # Warrior helmet
            draw.arc([8, 8, 28, 28], 180, 360, fill=(240, 200, 80, 255), width=3)
            draw.rectangle([11, 18, 25, 26], fill=col)
            draw.polygon([(cx, 6), (15, 12), (21, 12)], fill=(220, 40, 40, 255))
        elif "item" in filename:
            # Mirror/relic
            draw.ellipse([8, 8, 28, 28], outline=(240, 200, 80, 255), width=2)
            draw.ellipse([12, 12, 24, 24], fill=col)
            draw.point((15, 15), fill=(255, 255, 255, 255))
        elif "arena" in filename:
            # Battle horn
            draw.polygon([(8, 24), (26, 10), (28, 18), (12, 28)], fill=col, outline=(240, 200, 80, 255))
        elif "map" in filename:
            # Scroll/map
            draw.rounded_rectangle([7, 9, 29, 27], radius=3, fill=(245, 230, 190, 255), outline=(140, 90, 40, 255))
            draw.line([(11, 14), (25, 14)], fill=col, width=2)
            draw.line([(11, 18), (25, 18)], fill=col, width=2)
            draw.line([(11, 22), (21, 22)], fill=col, width=2)
        elif "bag" in filename:
            # Bag / sack
            draw.ellipse([10, 12, 26, 28], fill=col, outline=(240, 200, 80, 255))
            draw.line([(14, 12), (22, 12)], fill=(240, 200, 80, 255), width=2)
        elif "guild" in filename:
            # Shield
            draw.polygon([(8, 8), (28, 8), (28, 20), (cx, 28), (8, 20)], fill=col, outline=(240, 200, 80, 255), width=2)
        elif "pvp" in filename:
            # Crossed axes/swords
            draw.line([(8, 8), (28, 28)], fill=(240, 240, 255, 255), width=2)
            draw.line([(28, 8), (8, 28)], fill=(240, 240, 255, 255), width=2)
        elif "pet" in filename:
            # Mythic beast paw / egg
            draw.ellipse([10, 10, 26, 28], fill=col, outline=(240, 200, 80, 255))
            draw.ellipse([14, 14, 22, 22], fill=(255, 245, 200, 255))
        elif "upgrade" in filename:
            # Hammer / anvil
            draw.rectangle([8, 10, 28, 18], fill=(160, 160, 170, 255), outline=(240, 200, 80, 255))
            draw.rectangle([16, 18, 20, 28], fill=(140, 90, 40, 255))
        elif "formation" in filename:
            # 3x3 tactical grid
            for rx in range(3):
                for ry in range(3):
                    px = 9 + rx * 7
                    py = 9 + ry * 7
                    draw.rectangle([px, py, px+4, py+4], fill=(240, 200, 80, 255) if (rx+ry)%2==0 else col)
        else: # wedding / mount
            # Horse silhouette / horseshoe
            draw.arc([10, 8, 26, 26], 30, 330, fill=(240, 200, 80, 255), width=3)
            draw.point((cx, 20), fill=col)

        im.save(os.path.join(OUT_DIR, filename))

# 9. Quest panel, Chatbox, Event banner, Hub Courtyard background
def gen_panels_and_background():
    # Notification badge bg (24x24)
    badge = Image.new("RGBA", (24, 24), (0, 0, 0, 0))
    draw_badge = ImageDraw.Draw(badge)
    draw_badge.ellipse([1, 1, 22, 22], fill=(220, 30, 40, 255), outline=(255, 225, 95, 255), width=2)
    badge.save(os.path.join(OUT_DIR, "notification_badge_bg.png"))

    # Quest panel bg (260x380)
    w, h = 260, 380
    im_q = Image.new("RGBA", (w, h), (18, 14, 24, 240))
    draw_q = ImageDraw.Draw(im_q)
    draw_beveled_box(draw_q, (0, 0, w-1, h-1), (22, 18, 28, 240), (212, 175, 55, 255), (100, 75, 30, 255), (255, 215, 65, 255))
    im_q.save(os.path.join(OUT_DIR, "quest_panel_bg.png"))

    # Quest item bg (240x80)
    w, h = 240, 80
    im_qi = Image.new("RGBA", (w, h), (26, 20, 34, 230))
    draw_qi = ImageDraw.Draw(im_qi)
    draw_beveled_box(draw_qi, (0, 0, w-1, h-1), (30, 24, 38, 230), (140, 110, 45, 255), (60, 45, 20, 255), (210, 175, 60, 255))
    im_qi.save(os.path.join(OUT_DIR, "quest_item_bg.png"))

    # Chatbox bg (360x130)
    w, h = 360, 130
    im_c = Image.new("RGBA", (w, h), (16, 12, 22, 220))
    draw_c = ImageDraw.Draw(im_c)
    draw_beveled_box(draw_c, (0, 0, w-1, h-1), (18, 14, 26, 220), (160, 125, 45, 255), (80, 60, 25, 255), (215, 180, 70, 255))
    im_c.save(os.path.join(OUT_DIR, "chat_box_bg.png"))

    # Event banner frame (420x180)
    w, h = 420, 180
    im_e = Image.new("RGBA", (w, h), (35, 15, 25, 245))
    draw_e = ImageDraw.Draw(im_e)
    draw_beveled_box(draw_e, (0, 0, w-1, h-1), (45, 20, 30, 245), (235, 195, 65, 255), (130, 35, 45, 255), (255, 230, 120, 255))
    # Dragon decorative borders
    draw_e.line([(10, 10), (w-11, 10)], fill=(255, 215, 80, 180), width=2)
    draw_e.line([(10, h-11), (w-11, h-11)], fill=(255, 215, 80, 180), width=2)
    im_e.save(os.path.join(OUT_DIR, "event_banner_frame.png"))

    # Hub Courtyard background (1280x720)
    # Magnificent Three Kingdoms Grand Garrison / Palace Plaza
    w, h = 1280, 720
    im_bg = Image.new("RGBA", (w, h), (15, 12, 20, 255))
    draw_bg = ImageDraw.Draw(im_bg)

    # 1. Sky gradient (Twilight sunset / dusk battle sky)
    for y in range(320):
        t = y / 320.0
        r = int(55 * (1 - t) + 120 * t)
        g = int(35 * (1 - t) + 55 * t)
        b = int(75 * (1 - t) + 40 * t)
        draw_bg.line([(0, y), (w, y)], fill=(r, g, b, 255))

    # Distant mountain silhouettes
    for i in range(w):
        m1 = int(220 + 45 * math.sin(i * 0.007) + 20 * math.sin(i * 0.02))
        draw_bg.line([(i, m1), (i, 320)], fill=(45, 28, 48, 255))
        m2 = int(260 + 35 * math.sin(i * 0.012 + 1.2) + 15 * math.cos(i * 0.03))
        draw_bg.line([(i, m2), (i, 320)], fill=(32, 20, 36, 255))

    # Grand Palace Roof & Pagodas in background (silhouette with red/gold highlights)
    # Central Palace Pavilion
    cx = w // 2
    # Roof eaves
    draw_bg.polygon([(cx - 240, 270), (cx + 240, 270), (cx + 180, 220), (cx - 180, 220)], fill=(60, 25, 32, 255), outline=(195, 145, 55, 255))
    draw_bg.polygon([(cx - 160, 220), (cx + 160, 220), (cx + 110, 175), (cx - 110, 175)], fill=(75, 30, 40, 255), outline=(215, 170, 65, 255))
    # Eave horn curves
    draw_bg.line([(cx - 240, 270), (cx - 255, 255)], fill=(235, 190, 70, 255), width=3)
    draw_bg.line([(cx + 240, 270), (cx + 255, 255)], fill=(235, 190, 70, 255), width=3)
    draw_bg.line([(cx - 160, 220), (cx - 175, 205)], fill=(235, 190, 70, 255), width=3)
    draw_bg.line([(cx + 160, 220), (cx + 175, 205)], fill=(235, 190, 70, 255), width=3)
    # Pavilion columns
    for col_x in [cx - 140, cx - 70, cx, cx + 70, cx + 140]:
        draw_bg.rectangle([col_x - 6, 270, col_x + 6, 330], fill=(120, 35, 35, 255), outline=(80, 20, 25, 255))

    # Left & Right Watchtowers
    for tw_x in [140, w - 140]:
        draw_bg.polygon([(tw_x - 70, 280), (tw_x + 70, 280), (tw_x + 50, 240), (tw_x - 50, 240)], fill=(55, 25, 35, 255), outline=(180, 130, 45, 255))
        draw_bg.rectangle([tw_x - 35, 280, tw_x + 35, 340], fill=(70, 30, 40, 255), outline=(100, 40, 50, 255))
        # Hanging lanterns
        draw_bg.ellipse([tw_x - 42, 285, tw_x - 30, 305], fill=(240, 60, 40, 255), outline=(255, 220, 80, 255))
        draw_bg.ellipse([tw_x + 30, 285, tw_x + 42, 305], fill=(240, 60, 40, 255), outline=(255, 220, 80, 255))

    # 2. Courtyard Stone Plaza (from y=330 to y=720)
    for y in range(330, h):
        t = (y - 330) / (h - 330)
        # Perspective pavement
        r = int(28 + 25 * t)
        g = int(24 + 20 * t)
        b = int(32 + 25 * t)
        draw_bg.line([(0, y), (w, y)], fill=(r, g, b, 255))

    # Pavement tile grid lines (Perspective)
    for y in range(340, h, 28):
        draw_bg.line([(0, y), (w, y)], fill=(45, 38, 52, 160), width=1)
    for x in range(-200, w + 200, 90):
        # vanishing point around cx, 320
        draw_bg.line([(cx + (x - cx) * 0.2, 330), (x, h)], fill=(45, 38, 52, 140), width=1)

    # Crimson Imperial Red Carpet down the center
    carpet_top_w = 120
    carpet_bot_w = 420
    draw_bg.polygon([
        (cx - carpet_top_w//2, 330),
        (cx + carpet_top_w//2, 330),
        (cx + carpet_bot_w//2, h),
        (cx - carpet_bot_w//2, h)
    ], fill=(135, 25, 35, 255))
    # Golden embroidery on carpet edges
    draw_bg.line([(cx - carpet_top_w//2, 330), (cx - carpet_bot_w//2, h)], fill=(225, 185, 65, 255), width=3)
    draw_bg.line([(cx + carpet_top_w//2, 330), (cx + carpet_bot_w//2, h)], fill=(225, 185, 65, 255), width=3)

    # Braziers with burning embers on courtyard flanks
    for b_x in [cx - 280, cx + 280]:
        draw_bg.rectangle([b_x - 12, 420, b_x + 12, 470], fill=(85, 75, 80, 255), outline=(140, 120, 90, 255), width=2)
        # Fire
        draw_bg.ellipse([b_x - 18, 395, b_x + 18, 425], fill=(255, 120, 20, 230))
        draw_bg.ellipse([b_x - 10, 398, b_x + 10, 420], fill=(255, 230, 60, 240))

    im_bg.save(os.path.join(OUT_DIR, "hub_courtyard_bg.png"))

def main():
    print("Generating Hub UI assets...")
    gen_topbar_bg()
    gen_avatar_frame()
    gen_vip_badges()
    gen_currency_icons()
    gen_shortcut_frame()
    gen_shortcut_icons()
    gen_bottom_nav()
    gen_bottom_nav_icons()
    gen_panels_and_background()
    print("Hub UI assets generated successfully!")

if __name__ == "__main__":
    main()
