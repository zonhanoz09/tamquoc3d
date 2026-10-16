#!/usr/bin/env python3
"""
Godot 4.3 SpriteFrames (.tres) Generator for TamQuoc3D.
Generates Godot SpriteFrames resources mapping AtlasTextures to named animation clips.
"""

import os
import sys
import json
import argparse


def generate_hero_spriteframes(res_texture_path, output_tres_path, frame_size=64):
    """
    Generates standard 24-frame HD-2D hero SpriteFrames (4 rows x 6 columns):
      - 'idle'   : Row 0, 6 frames, 8 FPS,  loop=true
      - 'attack' : Row 1, 6 frames, 12 FPS, loop=false
      - 'skill'  : Row 2, 6 frames, 12 FPS, loop=false
      - 'hurt'   : Row 3, 3 frames, 10 FPS, loop=false
      - 'death'  : Row 3, 3 frames, 6 FPS,  loop=false
    """
    tres = f"""[gd_resource type="SpriteFrames" load_steps=26 format=3]

[ext_resource type="Texture2D" path="{res_texture_path}" id="1_tex"]

"""
    # AtlasTextures for Idle
    for f in range(6):
        tres += f"""[sub_resource type="AtlasTexture" id="AtlasTexture_idle_{f}"]
atlas = ExtResource("1_tex")
region = Rect2({f * frame_size}, 0, {frame_size}, {frame_size})

"""
    # AtlasTextures for Attack
    for f in range(6):
        tres += f"""[sub_resource type="AtlasTexture" id="AtlasTexture_atk_{f}"]
atlas = ExtResource("1_tex")
region = Rect2({f * frame_size}, {1 * frame_size}, {frame_size}, {frame_size})

"""
    # AtlasTextures for Skill
    for f in range(6):
        tres += f"""[sub_resource type="AtlasTexture" id="AtlasTexture_skl_{f}"]
atlas = ExtResource("1_tex")
region = Rect2({f * frame_size}, {2 * frame_size}, {frame_size}, {frame_size})

"""
    # AtlasTextures for Hurt
    for f in range(3):
        tres += f"""[sub_resource type="AtlasTexture" id="AtlasTexture_hrt_{f}"]
atlas = ExtResource("1_tex")
region = Rect2({f * frame_size}, {3 * frame_size}, {frame_size}, {frame_size})

"""
    # AtlasTextures for Death
    for f in range(3):
        tres += f"""[sub_resource type="AtlasTexture" id="AtlasTexture_dth_{f}"]
atlas = ExtResource("1_tex")
region = Rect2({(3 + f) * frame_size}, {3 * frame_size}, {frame_size}, {frame_size})

"""

    idle_sub = ", ".join([f'SubResource("AtlasTexture_idle_{f}")' for f in range(6)])
    atk_sub = ", ".join([f'SubResource("AtlasTexture_atk_{f}")' for f in range(6)])
    skl_sub = ", ".join([f'SubResource("AtlasTexture_skl_{f}")' for f in range(6)])
    hrt_sub = ", ".join([f'SubResource("AtlasTexture_hrt_{f}")' for f in range(3)])
    dth_sub = ", ".join([f'SubResource("AtlasTexture_dth_{f}")' for f in range(3)])

    tres += f"""[resource]
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
    os.makedirs(os.path.dirname(os.path.abspath(output_tres_path)), exist_ok=True)
    with open(output_tres_path, "w") as fp:
        fp.write(tres)
    print(f"🎬 Generated Godot Hero SpriteFrames: {output_tres_path}")
    return output_tres_path


def generate_strip_spriteframes(res_texture_path, output_tres_path, frame_count=6, frame_w=96, frame_h=96, fps=16.0, anim_name="play", loop=False):
    """
    Generates SpriteFrames for a single horizontal animation strip.
    """
    load_steps = frame_count + 2
    tres = f"""[gd_resource type="SpriteFrames" load_steps={load_steps} format=3]

[ext_resource type="Texture2D" path="{res_texture_path}" id="1_tex"]

"""
    for f in range(frame_count):
        tres += f"""[sub_resource type="AtlasTexture" id="AtlasTexture_f_{f}"]
atlas = ExtResource("1_tex")
region = Rect2({f * frame_w}, 0, {frame_w}, {frame_h})

"""

    frames_sub = ", ".join([f'SubResource("AtlasTexture_f_{f}")' for f in range(frame_count)])
    loop_str = "true" if loop else "false"

    tres += f"""[resource]
animations = [{{
"frames": [{frames_sub}],
"loop": {loop_str},
"name": &"{anim_name}",
"speed": {fps:.1f}
}}]
"""
    os.makedirs(os.path.dirname(os.path.abspath(output_tres_path)), exist_ok=True)
    with open(output_tres_path, "w") as fp:
        fp.write(tres)
    print(f"🎬 Generated Godot Strip SpriteFrames: {output_tres_path}")
    return output_tres_path


def main():
    parser = argparse.ArgumentParser(description="Generate Godot 4.3 SpriteFrames (.tres) from spritesheet.")
    parser.add_argument("texture", help="Godot resource texture path (e.g., res://assets/heroes/trieu_van/spritesheet_hd.png)")
    parser.add_argument("-o", "--output", required=True, help="Output .tres file path")
    parser.add_argument("-p", "--preset", choices=["hero", "strip"], default="hero", help="Preset type")
    parser.add_argument("-s", "--size", type=int, default=64, help="Square frame size (default: 64)")
    parser.add_argument("-c", "--count", type=int, default=6, help="Frame count for strip preset")
    parser.add_argument("-f", "--fps", type=float, default=16.0, help="Frames per second for strip preset")
    parser.add_argument("-n", "--name", default="play", help="Animation name for strip preset")
    parser.add_argument("-l", "--loop", action="store_true", help="Loop animation for strip preset")

    args = parser.parse_args()

    if args.preset == "hero":
        generate_hero_spriteframes(args.texture, args.output, args.size)
    else:
        generate_strip_spriteframes(args.texture, args.output, args.count, args.size, args.size, args.fps, args.name, args.loop)


if __name__ == "__main__":
    main()
