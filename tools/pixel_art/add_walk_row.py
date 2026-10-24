#!/usr/bin/env python3
"""
add_walk_row.py — TamQuoc3D pixel-art pipeline helper.

Appends a walk-cycle row to a hero's HD spritesheet and regenerates
sprite_frames.tres with a "walk" animation. HubCharacter (features/hub/ui/HubCharacter.cs)
auto-plays "walk" when the animation exists, otherwise falls back to idle + bounce.

Usage:
    python3 add_walk_row.py --hero quan_vu --strip ./walk_quan_vu.png

Input strip: PNG 384x64 = 6 frames of 64x64, same pixel style as the hero's
spritesheet_hd.png (row 4 of the sheet).

What it does:
  1. Validates assets/heroes/<hero>/spritesheet_hd.png is 384x256 (4 rows x 6 cols).
  2. Appends the strip as row 4 -> sheet becomes 384x320.
  3. Regenerates sprite_frames.tres keeping the existing animations
     (idle 8fps loop, attack 12, skill 12, hurt 10, death 6) and adding
     "walk": row 4, 6 frames, loop=true, speed=<fps> (default 8.0).

Safety:
  - Refuses if the sheet is not 384x256 or the .tres already has a "walk" animation.
  - Reversible via: git checkout -- assets/heroes/<hero>/spritesheet_hd.png assets/heroes/<hero>/sprite_frames.tres
"""

import argparse
import os
import sys

try:
    from PIL import Image
except ImportError:
    sys.exit("Pillow is required: pip install pillow")

FRAME = 64
COLS = 6
BASE_ROWS = 4
WALK_ROW = 4

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
DEFAULT_ROOT = os.path.dirname(os.path.dirname(SCRIPT_DIR))  # repo root (contains tools/)


def hero_paths(root, hero):
    hero_dir = os.path.join(root, "assets", "heroes", hero)
    sheet = os.path.join(hero_dir, "spritesheet_hd.png")
    tres = os.path.join(hero_dir, "sprite_frames.tres")
    res_tex = f"res://assets/heroes/{hero}/spritesheet_hd.png"
    return hero_dir, sheet, tres, res_tex


def generate_spriteframes_with_walk(res_texture_path, walk_fps):
    """30-frame HD-2D hero SpriteFrames: standard 24 frames + walk row."""
    tres = f'[gd_resource type="SpriteFrames" load_steps=32 format=3]\n\n'
    tres += f'[ext_resource type="Texture2D" path="{res_texture_path}" id="1_tex"]\n\n'

    def row_frames(prefix, row, cols, x0=0):
        out = ""
        for f in range(cols):
            out += (f'[sub_resource type="AtlasTexture" id="AtlasTexture_{prefix}_{f}"]\n'
                    f'atlas = ExtResource("1_tex")\n'
                    f'region = Rect2({(x0 + f) * FRAME}, {row * FRAME}, {FRAME}, {FRAME})\n\n')
        return out

    tres += row_frames("idle", 0, 6)
    tres += row_frames("atk", 1, 6)
    tres += row_frames("skl", 2, 6)
    tres += row_frames("hrt", 3, 3)
    tres += row_frames("dth", 3, 3, x0=3)   # death: frames 3..5 of row 3
    tres += row_frames("walk", WALK_ROW, 6)

    def sub(prefix, n):
        return ", ".join(f'SubResource("AtlasTexture_{prefix}_{f}")' for f in range(n))

    tres += ("[resource]\n"
             "animations = [{\n"
             f"\"frames\": [{sub('idle', 6)}],\n\"loop\": true,\n\"name\": &\"idle\",\n\"speed\": 8.0\n"
             "}, {\n"
             f"\"frames\": [{sub('atk', 6)}],\n\"loop\": false,\n\"name\": &\"attack\",\n\"speed\": 12.0\n"
             "}, {\n"
             f"\"frames\": [{sub('skl', 6)}],\n\"loop\": false,\n\"name\": &\"skill\",\n\"speed\": 12.0\n"
             "}, {\n"
             f"\"frames\": [{sub('hrt', 3)}],\n\"loop\": false,\n\"name\": &\"hurt\",\n\"speed\": 10.0\n"
             "}, {\n"
             f"\"frames\": [{sub('dth', 3)}],\n\"loop\": false,\n\"name\": &\"death\",\n\"speed\": 6.0\n"
             "}, {\n"
             f"\"frames\": [{sub('walk', 6)}],\n\"loop\": true,\n\"name\": &\"walk\",\n\"speed\": {walk_fps:.1f}\n"
             "}]\n")
    return tres


def main():
    ap = argparse.ArgumentParser(description="Append a walk-cycle row to a hero's HD spritesheet + .tres.")
    ap.add_argument("--hero", required=True, help="Hero id, e.g. quan_vu")
    ap.add_argument("--strip", required=True, help="Walk strip PNG, 384x64 (6 frames x 64x64)")
    ap.add_argument("--fps", type=float, default=8.0, help="Walk animation FPS (default 8.0)")
    ap.add_argument("--root", default=DEFAULT_ROOT, help="Repo root override (for testing)")
    args = ap.parse_args()

    hero_dir, sheet, tres, res_tex = hero_paths(args.root, args.hero)
    if not os.path.isfile(sheet):
        sys.exit(f"Not a standard HD hero (missing spritesheet_hd.png): {hero_dir}")
    if not os.path.isfile(tres):
        sys.exit(f"Missing sprite_frames.tres: {hero_dir}")

    with open(tres) as fp:
        if '"walk"' in fp.read():
            sys.exit(f"{args.hero}: sprite_frames.tres already has a walk animation — nothing to do.")

    im = Image.open(sheet)
    if im.size != (COLS * FRAME, BASE_ROWS * FRAME):
        sys.exit(f"{args.hero}: expected sheet 384x256, found {im.size} — refusing.")

    strip = Image.open(args.strip).convert("RGBA")
    if strip.size != (COLS * FRAME, FRAME):
        sys.exit(f"Walk strip must be 384x64, got {strip.size}: {args.strip}")

    out = Image.new("RGBA", (COLS * FRAME, (BASE_ROWS + 1) * FRAME), (0, 0, 0, 0))
    out.paste(im.convert("RGBA"), (0, 0))
    out.paste(strip, (0, BASE_ROWS * FRAME))
    out.save(sheet)

    with open(tres, "w") as fp:
        fp.write(generate_spriteframes_with_walk(res_tex, args.fps))

    rel = f"assets/heroes/{args.hero}"
    print(f"OK: {rel}/spritesheet_hd.png is now 384x320 with walk row; {rel}/sprite_frames.tres has 'walk' ({args.fps:.1f} fps).")
    print(f"Revert: git checkout -- {rel}/spritesheet_hd.png {rel}/sprite_frames.tres")


if __name__ == "__main__":
    main()
