#!/usr/bin/env python3
"""
Pixel Art Resizer & Normalizer for TamQuoc3D.
Standardizes frame dimensions (e.g. 64x64 for heroes, 96x96 for VFX, 128x128 for bosses)
using Nearest-Neighbor interpolation to preserve 100% sharp pixel art edges without blur.
"""

import os
import sys
import argparse
from PIL import Image

STANDARD_SIZES = {
    "hero": (64, 64),
    "vfx": (96, 96),
    "boss": (128, 128),
    "icon": (32, 32)
}


def normalize_image(input_path, output_path, target_size=(64, 64), mode="scale"):
    """
    Normalizes an image to the exact target size.
    Modes:
      - 'scale': Scales using Nearest-Neighbor to preserve pixel art.
      - 'pad': Centers the image on a transparent canvas of target_size.
      - 'fit': Scales preserving aspect ratio, then centers on canvas.
    """
    if not os.path.exists(input_path):
        raise FileNotFoundError(f"Input image not found: {input_path}")

    img = Image.open(input_path).convert("RGBA")
    tw, th = target_size

    if mode == "scale":
        res = img.resize((tw, th), Image.Resampling.NEAREST)
    elif mode == "pad":
        res = Image.new("RGBA", (tw, th), (0, 0, 0, 0))
        ox = (tw - img.size[0]) // 2
        oy = (th - img.size[1]) // 2
        res.paste(img, (ox, oy), img)
    elif mode == "fit":
        # Keep aspect ratio with nearest neighbor, then pad
        scale = min(tw / img.size[0], th / img.size[1])
        scaled_w = int(img.size[0] * scale)
        scaled_h = int(img.size[1] * scale)
        scaled = img.resize((scaled_w, scaled_h), Image.Resampling.NEAREST)

        res = Image.new("RGBA", (tw, th), (0, 0, 0, 0))
        ox = (tw - scaled_w) // 2
        oy = (th - scaled_h) // 2
        res.paste(scaled, (ox, oy), scaled)
    else:
        raise ValueError(f"Unknown mode: {mode}. Choose from 'scale', 'pad', 'fit'.")

    os.makedirs(os.path.dirname(os.path.abspath(output_path)), exist_ok=True)
    res.save(output_path)
    print(f"📐 Normalized {input_path} [{img.size[0]}x{img.size[1]}] -> {output_path} [{tw}x{th}] (mode: {mode})")
    return res.size


def main():
    parser = argparse.ArgumentParser(description="Normalize or resize pixel art images without blurring.")
    parser.add_argument("input", help="Input image path")
    parser.add_argument("-o", "--output", required=True, help="Output image path")
    parser.add_argument("-s", "--size", type=int, default=64, help="Target square size (e.g., 64, 96, 128)")
    parser.add_argument("-W", "--width", type=int, default=None, help="Target width")
    parser.add_argument("-H", "--height", type=int, default=None, help="Target height")
    parser.add_argument("-m", "--mode", choices=["scale", "pad", "fit"], default="scale", help="Resize mode")
    parser.add_argument("-p", "--preset", choices=list(STANDARD_SIZES.keys()), default=None, help="Standard size preset")

    args = parser.parse_args()

    if args.preset:
        target_size = STANDARD_SIZES[args.preset]
    elif args.width is not None and args.height is not None:
        target_size = (args.width, args.height)
    else:
        target_size = (args.size, args.size)

    normalize_image(args.input, args.output, target_size, args.mode)


if __name__ == "__main__":
    main()
