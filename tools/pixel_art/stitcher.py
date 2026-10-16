#!/usr/bin/env python3
"""
Pixel Art Stitcher Utility for TamQuoc3D.
Stitches multiple frame images into a single spritesheet (horizontal strip or grid).
"""

import os
import sys
import argparse
from PIL import Image


def stitch_frames(frame_paths, output_path, cols=None, padding=0):
    """
    Stitches a list of frame images into a spritesheet.
    If cols is None, stitches all frames horizontally into a single row.
    """
    if not frame_paths:
        raise ValueError("No frame paths provided for stitching.")

    images = [Image.open(p).convert("RGBA") for p in frame_paths]
    max_w = max(img.size[0] for img in images)
    max_h = max(img.size[1] for img in images)

    num_frames = len(images)
    if cols is None or cols <= 0 or cols >= num_frames:
        cols = num_frames
        rows = 1
    else:
        rows = (num_frames + cols - 1) // cols

    sheet_w = cols * max_w + (cols - 1) * padding
    sheet_h = rows * max_h + (rows - 1) * padding

    sheet = Image.new("RGBA", (sheet_w, sheet_h), (0, 0, 0, 0))

    for idx, img in enumerate(images):
        c = idx % cols
        r = idx // cols
        x = c * (max_w + padding)
        y = r * (max_h + padding)

        # Center frame within cell if dimensions differ slightly
        offset_x = (max_w - img.size[0]) // 2
        offset_y = (max_h - img.size[1]) // 2
        sheet.paste(img, (x + offset_x, y + offset_y), img)

    os.makedirs(os.path.dirname(os.path.abspath(output_path)), exist_ok=True)
    sheet.save(output_path)
    print(f"🧵 Stitched {num_frames} frames into {output_path} ({sheet_w}x{sheet_h}, {cols}x{rows} grid)")
    return sheet.size


def stitch_directory(input_dir, output_path, cols=None, padding=0, extensions=(".png", ".jpg", ".webp")):
    """Stitches all images in a directory sorted alphabetically."""
    files = sorted([
        os.path.join(input_dir, f) for f in os.listdir(input_dir)
        if any(f.lower().endswith(ext) for ext in extensions)
    ])
    if not files:
        raise FileNotFoundError(f"No image files found in {input_dir}")
    return stitch_frames(files, output_path, cols, padding)


def main():
    parser = argparse.ArgumentParser(description="Stitch multiple frames into a spritesheet.")
    parser.add_argument("input", help="Directory containing frames OR comma-separated file paths")
    parser.add_argument("-o", "--output", required=True, help="Output spritesheet path")
    parser.add_argument("-c", "--cols", type=int, default=None, help="Columns in spritesheet (default: horizontal row)")
    parser.add_argument("-p", "--padding", type=int, default=0, help="Pixel padding between frames")

    args = parser.parse_args()

    if os.path.isdir(args.input):
        stitch_directory(args.input, args.output, args.cols, args.padding)
    else:
        paths = [p.strip() for p in args.input.split(",") if p.strip()]
        stitch_frames(paths, args.output, args.cols, args.padding)


if __name__ == "__main__":
    main()
