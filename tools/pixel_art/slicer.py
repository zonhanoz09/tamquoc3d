#!/usr/bin/env python3
"""
Pixel Art Slicer Utility for TamQuoc3D.
Slices spritesheet images into individual frames without losing pixel quality.
"""

import os
import sys
import argparse
from PIL import Image


def slice_spritesheet(input_path, output_dir, frame_w=None, frame_h=None, cols=None, rows=None, prefix="frame"):
    """
    Slices an input spritesheet PNG into individual frames saved to output_dir.
    """
    if not os.path.exists(input_path):
        raise FileNotFoundError(f"Input image not found: {input_path}")

    sheet = Image.open(input_path)
    sheet_w, sheet_h = sheet.size

    if cols is not None and rows is not None:
        frame_w = sheet_w // cols
        frame_h = sheet_h // rows
    elif frame_w is not None and frame_h is not None:
        cols = sheet_w // frame_w
        rows = sheet_h // frame_h
    elif frame_w is not None:
        frame_h = frame_w
        cols = sheet_w // frame_w
        rows = sheet_h // frame_h
    else:
        # Default assume 64x64 square frames
        frame_w = 64
        frame_h = 64
        cols = sheet_w // frame_w
        rows = sheet_h // frame_h

    os.makedirs(output_dir, exist_ok=True)
    count = 0
    saved_files = []

    for r in range(rows):
        for c in range(cols):
            x0 = c * frame_w
            y0 = r * frame_h
            x1 = x0 + frame_w
            y1 = y0 + frame_h

            crop = sheet.crop((x0, y0, x1, y1))
            fname = f"{prefix}_{count:03d}.png"
            fpath = os.path.join(output_dir, fname)
            crop.save(fpath)
            saved_files.append(fpath)
            count += 1

    print(f"🔪 Sliced {count} frames ({frame_w}x{frame_h}) from {input_path} into {output_dir}")
    return saved_files


def main():
    parser = argparse.ArgumentParser(description="Slice a spritesheet into individual frames.")
    parser.add_argument("input", help="Path to input spritesheet image")
    parser.add_argument("-o", "--output-dir", default=None, help="Directory to save sliced frames")
    parser.add_argument("-w", "--width", type=int, default=None, help="Frame width in pixels")
    parser.add_argument("-H", "--height", type=int, default=None, help="Frame height in pixels")
    parser.add_argument("-c", "--cols", type=int, default=None, help="Number of columns")
    parser.add_argument("-r", "--rows", type=int, default=None, help="Number of rows")
    parser.add_argument("-p", "--prefix", default="frame", help="Prefix for output frames")

    args = parser.parse_args()
    out_dir = args.output_dir or os.path.splitext(args.input)[0] + "_frames"
    slice_spritesheet(args.input, out_dir, args.width, args.height, args.cols, args.rows, args.prefix)


if __name__ == "__main__":
    main()
