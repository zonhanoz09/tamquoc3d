import os
import math
from PIL import Image, ImageDraw, ImageFilter

BG_PATH = '/home/annhan/Documents/src/tamquoc3d/assets/sprites/backgrounds/battlefield.png'
W, H = 1280, 720
CX, CY = 640, 520

bg = Image.new('RGBA', (W, H))
draw = ImageDraw.Draw(bg)

# ─────────────────────────────────────────────────────────────────────────────
# 1. SKY & DISTANT FORTRESS (y: 0 to 340)
# ─────────────────────────────────────────────────────────────────────────────
sky_top = (32, 22, 45)      # Deep twilight purple
sky_mid = (130, 45, 50)     # Imperial crimson
sky_bot = (220, 130, 55)    # Warm sunset amber

for y in range(340):
    t = y / 340.0
    if t < 0.45:
        k = t / 0.45
        r = int(sky_top[0] * (1 - k) + sky_mid[0] * k)
        g = int(sky_top[1] * (1 - k) + sky_mid[1] * k)
        b = int(sky_top[2] * (1 - k) + sky_mid[2] * k)
    else:
        k = (t - 0.45) / 0.55
        r = int(sky_mid[0] * (1 - k) + sky_bot[0] * k)
        g = int(sky_mid[1] * (1 - k) + sky_bot[1] * k)
        b = int(sky_mid[2] * (1 - k) + sky_bot[2] * k)
    draw.line([(0, y), (W, y)], fill=(r, g, b))

# Dramatic sunset sun in the center background
draw.ellipse([CX - 60, 200 - 60, CX + 60, 200 + 60], fill=(255, 180, 80, 180))
draw.ellipse([CX - 45, 200 - 45, CX + 45, 200 + 45], fill=(255, 225, 140, 230))

# Distant Mountain Ridges (Symmetrical silhouette)
far_mountains = [(0, 340)]
for x in range(0, W + 20, 20):
    dx = abs(x - CX)
    my = 230 + int(math.sin(dx * 0.012) * 35 + math.cos(dx * 0.025) * 18)
    far_mountains.append((x, my))
far_mountains.append((W, 340))
draw.polygon(far_mountains, fill=(75, 38, 55))

# Mid Mountains & Ancient Walls
mid_mountains = [(0, 340)]
for x in range(0, W + 15, 15):
    dx = abs(x - CX)
    my = 265 + int(math.sin(dx * 0.02) * 22 + math.cos(dx * 0.04) * 12)
    mid_mountains.append((x, my))
mid_mountains.append((W, 340))
draw.polygon(mid_mountains, fill=(50, 28, 42))

# Fortress Walls & Watchtowers (Symmetrical Hổ Lao Quan Gate)
# Central fortress gate
draw.rectangle([CX - 70, 270, CX + 70, 340], fill=(42, 24, 32))
# Central gate arch
draw.arc([CX - 32, 290, CX + 32, 344], 180, 360, fill=(25, 14, 20), width=4)
# Gate roof (Ancient Chinese curved pagoda roof)
draw.polygon([(CX - 85, 275), (CX, 245), (CX + 85, 275)], fill=(30, 18, 25))
draw.polygon([(CX - 65, 255), (CX, 235), (CX + 65, 255)], fill=(38, 22, 30))
# Central watchtower roof peak
draw.line([(CX, 235), (CX, 218)], fill=(180, 140, 60), width=2)
draw.point([CX, 217], fill=(255, 220, 80))

# Symmetrical Left & Right Watchtowers
for tw_x in [240, 1040]:
    draw.rectangle([tw_x - 35, 275, tw_x + 35, 340], fill=(42, 24, 32))
    draw.polygon([(tw_x - 45, 280), (tw_x, 258), (tw_x + 45, 280)], fill=(32, 18, 25))
    draw.line([(tw_x, 258), (tw_x, 242)], fill=(120, 90, 50), width=2)

# Symmetrical Barricades / Log Chevaux-de-frise (Chông gỗ)
# Centered precisely around CX = 640
for bx in range(CX - 75, CX + 75, 25):
    draw.line([(bx, 342), (bx + 12, 308)], fill=(75, 48, 30), width=4)
    draw.line([(bx + 18, 342), (bx - 6, 312)], fill=(60, 38, 24), width=3)

# Symmetrical War Banners (Thục đỏ bên trái, Ngụy xanh bên phải)
# Left Shu Banners
for fx in [90, 190]:
    draw.line([(fx, 340), (fx, 255)], fill=(70, 48, 32), width=3)
    draw.polygon([(fx, 260), (fx + 55, 280), (fx, 300)], fill=(195, 35, 35))
    draw.polygon([(fx + 8, 270), (fx + 42, 280), (fx + 8, 290)], fill=(235, 185, 55))

# Right Wei Banners
for fx in [1190, 1090]:
    draw.line([(fx, 340), (fx, 255)], fill=(70, 48, 32), width=3)
    draw.polygon([(fx, 260), (fx - 55, 280), (fx, 300)], fill=(35, 65, 165))
    draw.polygon([(fx - 8, 270), (fx - 42, 280), (fx - 8, 290)], fill=(220, 185, 65))

# Symmetrical Braziers / Torches with Roaring Fire
for tx in [70, 440, 840, 1210]:
    ty = 338
    # Iron brazier stand
    draw.line([(tx, ty), (tx, ty - 22)], fill=(50, 45, 50), width=3)
    draw.polygon([(tx - 10, ty - 22), (tx + 10, ty - 22), (tx + 6, ty - 16), (tx - 6, ty - 16)], fill=(70, 65, 70))
    # Fire flames
    draw.ellipse([tx - 8, ty - 36, tx + 8, ty - 20], fill=(240, 70, 20))
    draw.ellipse([tx - 5, ty - 33, tx + 5, ty - 23], fill=(255, 180, 40))
    draw.ellipse([tx - 2, ty - 30, tx + 2, ty - 25], fill=(255, 245, 160))

# ─────────────────────────────────────────────────────────────────────────────
# 2. GRAND ARENA PLATFORM ("SÀN ĐẤU") (y: 340 to 720)
# ─────────────────────────────────────────────────────────────────────────────
# Stone arena base shading
floor_top = (155, 120, 80)
floor_bot = (75, 55, 40)
for y in range(340, H):
    t = (y - 340) / 380.0
    r = int(floor_top[0] * (1 - t) + floor_bot[0] * t)
    g = int(floor_top[1] * (1 - t) + floor_bot[1] * t)
    b = int(floor_top[2] * (1 - t) + floor_bot[2] * t)
    draw.line([(0, y), (W, y)], fill=(r, g, b))

# Grand Arena Octagonal Outer Rim (Đường viền võ đài bát giác lớn)
rim_col_outer = (55, 40, 28, 210)
rim_col_inner = (210, 165, 65, 160)
arena_w = 580
arena_h = 240
# Outer stone rim
draw.ellipse([CX - arena_w - 6, CY - arena_h - 4, CX + arena_w + 6, CY + arena_h + 4], outline=rim_col_outer, width=5)
draw.ellipse([CX - arena_w, CY - arena_h, CX + arena_w, CY + arena_h], outline=rim_col_inner, width=3)

# Inner tactical division circle
draw.ellipse([CX - 360, CY - 165, CX + 360, CY + 165], outline=(180, 140, 60, 110), width=2)

# Perspective Grid Lines (Tia phân kỳ chuẩn tâm x=640, y=340)
grid_col = (90, 68, 48, 90)
for xi in range(-160, W + 200, 80):
    top_x = CX + (xi - CX) * 0.28
    draw.line([(top_x, 340), (xi, H)], fill=grid_col, width=1)

# Horizontal Stone Joint Lines (Đường gạch đá ngang đối xứng theo luật phối cảnh)
h_joints = [360, 385, 415, 455, 505, 565, 635, 710]
for yj in h_joints:
    draw.line([(0, yj), (W, yj)], fill=(50, 36, 25, 95), width=2)
    draw.line([(0, yj - 1), (W, yj - 1)], fill=(210, 180, 130, 45), width=1)

# Center Division Line (Đường ranh giới trung tâm)
draw.line([(CX, 340), (CX, H)], fill=(215, 175, 70, 140), width=2)

# Center Martial Medallion (Vòng ngọc Bát Quái / Thái Cực trung tâm tại x=640, y=520)
med_rx, med_ry = 68, 28
# Outer bronze ring
draw.ellipse([CX - med_rx - 4, CY - med_ry - 2, CX + med_rx + 4, CY + med_ry + 2], fill=(45, 32, 22, 220))
draw.ellipse([CX - med_rx, CY - med_ry, CX + med_rx, CY + med_ry], outline=(230, 185, 75, 230), width=3)
# Inner bronze fill
draw.ellipse([CX - med_rx + 4, CY - med_ry + 2, CX + med_rx - 4, CY + med_ry - 2], fill=(110, 80, 45, 180))
# Yin-Yang / Dual Dragon curve hint
draw.arc([CX - 28, CY - 14, CX + 28, CY + 14], 0, 180, fill=(235, 195, 80), width=2)
draw.arc([CX - 28, CY - 14, CX + 28, CY + 14], 180, 360, fill=(80, 45, 35), width=2)

# ─────────────────────────────────────────────────────────────────────────────
# 3. TACTICAL FORMATION PADS ("BỆ ĐỨNG CHIẾN THUẬT DƯỚI NỀN ĐÁ")
# Khớp 100% với get_slot_position() trong BattleScene.gd:
# Tiền quân: (520, 520) / (760, 520)
# Trung quân: (350, 410/520/630) / (930, 410/520/630)
# Hậu quân: (180, 410/520/630) / (1100, 410/520/630)
# ─────────────────────────────────────────────────────────────────────────────
slots_player = [
    (520, 520),                 # Tiền quân
    (350, 410), (350, 520), (350, 630), # Trung quân
    (180, 410), (180, 520), (180, 630), # Hậu quân
]

slots_enemy = [
    (760, 520),                 # Tiền quân
    (930, 410), (930, 520), (930, 630), # Trung quân
    (1100, 410), (1100, 520), (1100, 630), # Hậu quân
]

def draw_tactical_pad(draw, sx, sy, is_p=True):
    # Stone engraved tactical rune disc on floor
    rx, ry = 46, 17
    # Dark recessed stone groove
    draw.ellipse([sx - rx - 3, sy - ry - 2, sx + rx + 3, sy + ry + 2], fill=(30, 22, 18, 110))
    # Outer rune rim
    gold = (215, 175, 65, 150)
    draw.ellipse([sx - rx, sy - ry, sx + rx, sy + ry], outline=gold, width=2)
    # Inner faction aura glow
    glow = (45, 130, 210, 65) if is_p else (210, 55, 40, 65)
    inner_rim = (80, 185, 255, 130) if is_p else (255, 110, 80, 130)
    draw.ellipse([sx - rx + 4, sy - ry + 2, sx + rx - 4, sy + ry - 2], fill=glow, outline=inner_rim, width=1)
    # Four corner tactical cross ticks
    draw.line([(sx - rx - 4, sy), (sx - rx + 2, sy)], fill=gold, width=2)
    draw.line([(sx + rx - 2, sy), (sx + rx + 4, sy)], fill=gold, width=2)
    draw.line([(sx, sy - ry - 2), (sx, sy - ry + 2)], fill=gold, width=2)
    draw.line([(sx, sy + ry - 2), (sx, sy + ry + 2)], fill=gold, width=2)

for sx, sy in slots_player:
    draw_tactical_pad(draw, sx, sy, is_p=True)

for sx, sy in slots_enemy:
    draw_tactical_pad(draw, sx, sy, is_p=False)

# Atmospheric warm torch lighting vignette
torch_vignette = Image.new('RGBA', (W, H), (0, 0, 0, 0))
tv_draw = ImageDraw.Draw(torch_vignette)
for tx in [70, 440, 840, 1210]:
    tv_draw.ellipse([tx - 110, 338 - 90, tx + 110, 338 + 90], fill=(255, 170, 50, 32))

bg = Image.alpha_composite(bg, torch_vignette)
bg.save(BG_PATH)
print("Magnificent Three Kingdoms Symmetrical Battle Arena saved to", BG_PATH)
