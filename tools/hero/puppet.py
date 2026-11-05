#!/usr/bin/env python3
"""Cuts the painted hero (Midjourney parts sheet, design/characters/hero/parts/source.png) into puppet pieces
for hero.js: body (hood, cloak, wrapped scarf), the two legs split at the knee, the glove and the sword.
Each piece is cut off the magenta, cropped, and saved with its pivot (pixels in the piece) in parts.json.
2026-11-05, user: «the hero must look like the Midjourney picture, and every model at that level»;
decided by Claude under standing approval.
  python tools/hero/puppet.py
"""
import json, sys
from pathlib import Path
from PIL import Image, ImageDraw
ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/midjourney"))
from sprites import cut  # noqa: E402
OUT = ROOT / "design/characters/hero/parts"

# (name, crop box in the source, pivot in source pixels): the right-hand figure of the sheet, 3/4 view facing right
PIECES = [
    ("body", (858, 50, 1235, 702), (1050, 640)),       # pivot: the hips under the cloak
    ("leg_back_thigh", (1045, 680, 1120, 770), (1080, 690)),
    ("leg_back_shin", (1040, 760, 1145, 858), (1080, 768)),
    ("leg_front_thigh", (965, 680, 1040, 770), (1000, 690)),
    ("leg_front_shin", (955, 760, 1045, 858), (995, 768)),
    ("glove", (722, 375, 822, 492), (760, 440)),        # pivot: the grip in the fist
    ("sword", (318, 543, 630, 822), (566, 592)),        # pivot: the grip under the guard
]


def main():
    src = Image.open(OUT / "source.png").convert("RGBA")
    clean = cut(src)
    meta = {}
    for name, box, pivot in PIECES:
        piece = clean.crop(box)
        # only the piece itself: what joins the pivot, not a neighbour's edge that fell into the box
        mask = piece.getchannel("A").point(lambda v: 255 if v > 24 else 0).copy()
        seed = (pivot[0] - box[0], pivot[1] - box[1])
        if mask.getpixel(seed) == 255:
            ImageDraw.floodfill(mask, seed, 128)
            keep = mask.point(lambda v: 255 if v == 128 else 0)
            alpha = Image.composite(piece.getchannel("A"), Image.new("L", piece.size, 0), keep)
            piece.putalpha(alpha)
        else:
            print("pivot outside", name)
        bb = piece.getchannel("A").point(lambda v: 255 if v > 24 else 0).getbbox()
        piece = piece.crop(bb)
        if name == "body":
            # Midjourney's eyes are warm; the hero's are the one cold light (VISUAL_BIBLE): pale blue-white
            import numpy as np
            a = np.asarray(piece, np.float32).copy()
            r, g, b = a[..., 0], a[..., 1], a[..., 2]
            eye = (r > 140) & (r > b + 25) & (a[..., 3] > 0)
            a[eye, 0:3] = [228, 238, 255]
            piece = Image.fromarray(a.astype(np.uint8), "RGBA")
        meta[name] = {"w": piece.width, "h": piece.height,
                      "px": pivot[0] - box[0] - bb[0], "py": pivot[1] - box[1] - bb[1]}
        piece.save(OUT / f"{name}.png")
    (OUT / "parts.json").write_text(json.dumps(meta, indent=1), encoding="utf-8")
    print("puppet:", ", ".join(meta))


if __name__ == "__main__":
    main()
