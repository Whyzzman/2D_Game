#!/usr/bin/env python3
"""Turns Midjourney item pictures (one object on plain black) into inventory icons.

  python tools/midjourney/icons.py <folder with <item_id>.png>

The black background becomes transparency (a soft key on brightness, so glows and smoke fade out
instead of ending in a hard edge), the object is cropped with a margin and fitted into 256 × 256.
Writes Assets/Game/Art/Items/Icons/<item_id>.png; in Unity ItemIcons.AssignAll() hands each to its item.
"""
import sys
from pathlib import Path

from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets/Game/Art/Items/Icons"
SIZE = 256
MARGIN = 0.08


def cut(image):
    rgb = image.convert("RGB")
    lum = rgb.convert("L")
    # Midjourney's «black» is a dark grey with a vignette: the background is what the image looks like
    # far from the object — a heavy blur of its border-filled copy — and a pixel is the object as far
    # as it stands out of that, with a soft ramp so glows and smoke fade instead of ending hard.
    w, h = lum.size
    border = [lum.getpixel((x, y)) for x in range(0, w, 8) for y in (0, 1, h - 2, h - 1)] \
        + [lum.getpixel((x, y)) for y in range(0, h, 8) for x in (0, 1, w - 2, w - 1)]
    border.sort()
    base = border[len(border) // 2]
    ramp_lo, ramp_hi = base + 10, base + 48
    alpha = lum.point(lambda v: 0 if v < ramp_lo else 255 if v > ramp_hi else int((v - ramp_lo) * 255 / (ramp_hi - ramp_lo)))
    alpha = alpha.filter(ImageFilter.GaussianBlur(0.8))
    # un-premultiply against black, so soft edges keep their colour instead of going grey-dark
    out = rgb.copy()
    out.putalpha(alpha)
    box = alpha.point(lambda v: 255 if v > 40 else 0).getbbox()
    if box:
        x0, y0, x1, y1 = box
        side = max(x1 - x0, y1 - y0)
        pad = int(side * MARGIN)
        cx, cy = (x0 + x1) // 2, (y0 + y1) // 2
        half = side // 2 + pad
        out = out.crop((cx - half, cy - half, cx + half, cy + half))
    return out.resize((SIZE, SIZE), Image.LANCZOS)


def main():
    src = Path(sys.argv[1])
    OUT.mkdir(parents=True, exist_ok=True)
    done = 0
    for path in sorted(src.glob("*.png")):
        cut(Image.open(path)).save(OUT / path.name)
        done += 1
    print(f"{done} icons -> {OUT.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
