import os
from PIL import Image, ImageDraw

OUT_DIR = "assets/ui/formation"
os.makedirs(OUT_DIR, exist_ok=True)

def draw_beveled_box(draw, bounds, bg_color, border_outer, border_inner, corner_color=None):
    x0, y0, x1, y1 = bounds
    # Background
    draw.rectangle([x0+2, y0+2, x1-2, y1-2], fill=bg_color)
    # Outer border
    draw.rectangle([x0, y0, x1, y1], outline=border_outer, width=1)
    # Inner border
    draw.rectangle([x0+1, y0+1, x1-1, y1-1], outline=border_inner, width=1)
    # Corner dots
    if corner_color:
        for cx, cy in [(x0+1, y0+1), (x1-1, y0+1), (x0+1, y1-1), (x1-1, y1-1)]:
            draw.point((cx, cy), fill=corner_color)

# 1. formation_grid_bg.png (640x480)
def gen_formation_grid_bg():
    w, h = 640, 480
    im = Image.new("RGBA", (w, h), (18, 16, 24, 255))
    draw = ImageDraw.Draw(im)
    
    # Outer antique frame
    draw.rectangle([0, 0, w-1, h-1], outline=(160, 125, 45, 255), width=3)
    draw.rectangle([4, 4, w-5, h-5], outline=(60, 20, 25, 255), width=4)
    draw.rectangle([9, 9, w-10, h-10], outline=(120, 95, 35, 255), width=1)
    
    # Subtle inner board pattern
    for y in range(12, h-12, 16):
        draw.line([(12, y), (w-13, y)], fill=(24, 21, 32, 255), width=1)
    for x in range(12, w-12, 16):
        draw.line([(x, 12), (x, h-13)], fill=(24, 21, 32, 255), width=1)
        
    # Yin-yang / Bagua circle hint in the middle
    cx, cy = w // 2, h // 2
    for r in [180, 120, 60]:
        draw.ellipse([cx-r, cy-r, cx+r, cy+r], outline=(45, 38, 55, 180), width=1)
        
    # Corner brackets
    for ox, oy, dx, dy in [(12, 12, 1, 1), (w-13, 12, -1, 1), (12, h-13, 1, -1), (w-13, h-13, -1, -1)]:
        draw.line([(ox, oy), (ox + dx*32, oy)], fill=(215, 180, 75, 255), width=2)
        draw.line([(ox, oy), (ox, oy + dy*32)], fill=(215, 180, 75, 255), width=2)
        rx0, rx1 = min(ox, ox+dx*6), max(ox, ox+dx*6)
        ry0, ry1 = min(oy, oy+dy*6), max(oy, oy+dy*6)
        draw.rectangle([rx0, ry0, rx1, ry1], fill=(235, 205, 95, 255))

    im.save(os.path.join(OUT_DIR, "formation_grid_bg.png"))
    print("Created formation_grid_bg.png")

# 2. slot_empty.png (96x96)
def gen_slot_empty():
    w, h = 96, 96
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(im)
    
    # Base background
    draw.rectangle([2, 2, w-3, h-3], fill=(26, 24, 35, 220))
    # Border
    draw.rectangle([0, 0, w-1, h-1], outline=(140, 115, 55, 240), width=2)
    draw.rectangle([3, 3, w-4, h-4], outline=(60, 50, 75, 180), width=1)
    
    # Corner accents
    for x, y in [(3, 3), (w-4, 3), (3, h-4), (w-4, h-4)]:
        draw.rectangle([x-2, y-2, x+2, y+2], fill=(200, 170, 70, 255))
        
    # Centered Plus sign
    cx, cy = w//2, h//2
    draw.line([(cx-10, cy), (cx+10, cy)], fill=(150, 130, 85, 200), width=2)
    draw.line([(cx, cy-10), (cx, cy+10)], fill=(150, 130, 85, 200), width=2)
    
    im.save(os.path.join(OUT_DIR, "slot_empty.png"))
    print("Created slot_empty.png")

# 3. slot_filled.png (96x96)
def gen_slot_filled():
    w, h = 96, 96
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(im)
    
    draw.rectangle([2, 2, w-3, h-3], fill=(35, 30, 48, 240))
    # Golden outer & inner border
    draw.rectangle([0, 0, w-1, h-1], outline=(255, 215, 60, 255), width=2)
    draw.rectangle([3, 3, w-4, h-4], outline=(200, 150, 30, 220), width=1)
    
    # Golden corner highlights
    for ox, oy, dx, dy in [(2, 2, 1, 1), (w-3, 2, -1, 1), (2, h-3, 1, -1), (w-3, h-3, -1, -1)]:
        draw.line([(ox, oy), (ox+dx*12, oy)], fill=(255, 240, 140, 255), width=2)
        draw.line([(ox, oy), (ox, oy+dy*12)], fill=(255, 240, 140, 255), width=2)
        
    im.save(os.path.join(OUT_DIR, "slot_filled.png"))
    print("Created slot_filled.png")

# 4. slot_inactive.png (96x96)
def gen_slot_inactive():
    w, h = 96, 96
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(im)
    
    draw.rectangle([2, 2, w-3, h-3], fill=(16, 14, 20, 160))
    draw.rectangle([0, 0, w-1, h-1], outline=(50, 45, 60, 180), width=1)
    
    # Subtle crossed hatch
    for i in range(16, w-16, 16):
        draw.line([(i, 16), (i+16, 32)], fill=(35, 30, 42, 140), width=1)
        
    im.save(os.path.join(OUT_DIR, "slot_inactive.png"))
    print("Created slot_inactive.png")

# 5. hero_card_bg_*.png (80x120)
def gen_card_bgs():
    themes = {
        "default": ((38, 38, 48), (24, 24, 32), (110, 110, 130), (70, 70, 85)),
        "fire":    ((75, 25, 22), (40, 15, 15), (220, 80, 50), (140, 40, 30)),
        "water":   ((22, 45, 75), (15, 26, 48), (55, 150, 225), (30, 80, 140)),
        "wind":    ((22, 60, 38), (15, 36, 25), (65, 195, 115), (35, 110, 65)),
        "light":   ((78, 62, 25), (46, 36, 16), (245, 200, 65), (160, 125, 35)),
        "dark":    ((55, 25, 72), (32, 16, 44), (185, 75, 225), (110, 40, 140)),
    }
    
    w, h = 80, 120
    for name, (c_top, c_bot, b_outer, b_inner) in themes.items():
        im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        draw = ImageDraw.Draw(im)
        
        # Gradient background
        for y in range(2, h-2):
            factor = (y - 2) / float(h - 4)
            r = int(c_top[0] * (1 - factor) + c_bot[0] * factor)
            g = int(c_top[1] * (1 - factor) + c_bot[1] * factor)
            b = int(c_top[2] * (1 - factor) + c_bot[2] * factor)
            draw.line([(2, y), (w-3, y)], fill=(r, g, b, 245))
            
        # Borders
        draw.rectangle([0, 0, w-1, h-1], outline=b_outer, width=1)
        draw.rectangle([1, 1, w-2, h-2], outline=b_inner, width=1)
        
        # Header banner divider line for hero name/stars
        draw.line([(3, 24), (w-4, 24)], fill=b_inner, width=1)
        # Footer divider for power
        draw.line([(3, 98), (w-4, 98)], fill=b_inner, width=1)
        
        # Subtle corner dots
        for cx, cy in [(1, 1), (w-2, 1), (1, h-2), (w-2, h-2)]:
            draw.point((cx, cy), fill=b_outer)

        im.save(os.path.join(OUT_DIR, f"hero_card_bg_{name}.png"))
        print(f"Created hero_card_bg_{name}.png")

# 6. hero_card_frame_*.png (80x120)
def gen_card_frames():
    w, h = 80, 120
    
    # Selected frame (glowing gold)
    im_sel = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d_sel = ImageDraw.Draw(im_sel)
    d_sel.rectangle([0, 0, w-1, h-1], outline=(255, 230, 80, 255), width=2)
    d_sel.rectangle([2, 2, w-3, h-3], outline=(255, 255, 180, 200), width=1)
    for ox, oy, dx, dy in [(1, 1, 1, 1), (w-2, 1, -1, 1), (1, h-2, 1, -1), (w-2, h-2, -1, -1)]:
        d_sel.line([(ox, oy), (ox+dx*8, oy)], fill=(255, 255, 220, 255), width=2)
        d_sel.line([(ox, oy), (ox, oy+dy*8)], fill=(255, 255, 220, 255), width=2)
    im_sel.save(os.path.join(OUT_DIR, "hero_card_frame_selected.png"))
    print("Created hero_card_frame_selected.png")
    
    # In formation frame (subtle green/cyan)
    im_inf = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d_inf = ImageDraw.Draw(im_inf)
    d_inf.rectangle([0, 0, w-1, h-1], outline=(60, 210, 150, 240), width=2)
    d_inf.rectangle([2, 2, w-3, h-3], outline=(120, 255, 200, 160), width=1)
    im_inf.save(os.path.join(OUT_DIR, "hero_card_frame_in_formation.png"))
    print("Created hero_card_frame_in_formation.png")

# 7. btn_auto_deploy.png & btn_clear_formation.png (160x40)
def gen_buttons():
    w, h = 160, 40
    
    # Auto deploy button (tactical gold/amber)
    im_auto = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d_auto = ImageDraw.Draw(im_auto)
    draw_beveled_box(d_auto, (0, 0, w-1, h-1), (30, 40, 55, 255), (210, 175, 60, 255), (140, 110, 35, 255), (255, 235, 120, 255))
    im_auto.save(os.path.join(OUT_DIR, "btn_auto_deploy.png"))
    print("Created btn_auto_deploy.png")

    # Clear formation button (crimson/ruby)
    im_clr = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d_clr = ImageDraw.Draw(im_clr)
    draw_beveled_box(d_clr, (0, 0, w-1, h-1), (50, 22, 26, 255), (200, 65, 65, 255), (130, 35, 35, 255), (255, 140, 140, 255))
    im_clr.save(os.path.join(OUT_DIR, "btn_clear_formation.png"))
    print("Created btn_clear_formation.png")

# 8. icon_star_filled.png & icon_star_empty.png (16x16)
def gen_stars():
    w, h = 16, 16
    
    # Filled star
    im_f = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d_f = ImageDraw.Draw(im_f)
    pts = [(8, 1), (10, 6), (15, 6), (11, 10), (13, 15), (8, 12), (3, 15), (5, 10), (1, 6), (6, 6)]
    d_f.polygon(pts, fill=(255, 215, 20, 255), outline=(160, 110, 10, 255))
    d_f.point((8, 4), fill=(255, 255, 180, 255))
    d_f.point((8, 5), fill=(255, 255, 180, 255))
    im_f.save(os.path.join(OUT_DIR, "icon_star_filled.png"))
    print("Created icon_star_filled.png")
    
    # Empty star
    im_e = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d_e = ImageDraw.Draw(im_e)
    d_e.polygon(pts, fill=(35, 36, 48, 200), outline=(90, 95, 120, 255))
    im_e.save(os.path.join(OUT_DIR, "icon_star_empty.png"))
    print("Created icon_star_empty.png")

if __name__ == "__main__":
    gen_formation_grid_bg()
    gen_slot_empty()
    gen_slot_filled()
    gen_slot_inactive()
    gen_card_bgs()
    gen_card_frames()
    gen_buttons()
    gen_stars()
    print("All formation UI assets generated successfully!")
