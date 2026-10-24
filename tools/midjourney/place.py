#!/usr/bin/env python3
"""Puts Midjourney room backdrops in place of the painter's far layer (gamedesign/MIDJOURNEY.md).

Usage: python tools/midjourney/place.py <zone> [room_id ...]   e.g. python tools/midjourney/place.py tomb
Needs Pillow (pip install pillow). Reads design/levels/midjourney/<zone>/<room_id>.(png|jpg|jpeg|webp),
crops it to the room's proportions from the centre, scales it to the size of
Assets/Game/Art/Levels/<Zone>/Rooms/<room_id>_mid.png and grades it to a background plan (VISUAL_BIBLE.md §10:
darker and duller than the playfield). The painter's near wall of that room (<room_id>_near.png) is cleared:
the painting already holds the room's architecture, and the flat wall hid most of it.
Then in Unity: ContentBuilder.RefreshLevelArt.
"""
import math
import sys
from pathlib import Path

from PIL import Image, ImageEnhance

ROOT = Path(__file__).resolve().parents[2]
EXTENSIONS = (".png", ".jpg", ".jpeg", ".webp")

# Midjourney paints the far plan too bright: compress the lights into a soft shoulder and take out a
# little colour, so the playfield stays the brightest thing on screen. Blacks stay deep (a lifted black
# turned the paintings milky grey); each room keeps its own hue (rooms differ in colour by design).
BLACK_L, WHITE_L, SHOULDER = 2.0, 55.0, 40.0
SATURATION = 0.8


def srgb_to_lstar(v):
    c = v / 255.0
    lin = c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4
    return 116.0 * lin ** (1.0 / 3.0) - 16.0 if lin > 0.008856 else 903.3 * lin


def lstar_to_srgb(l):
    lin = ((l + 16.0) / 116.0) ** 3 if l > 8.0 else l / 903.3
    c = lin * 12.92 if lin <= 0.0031308 else 1.055 * lin ** (1.0 / 2.4) - 0.055
    return max(0, min(255, round(c * 255.0)))


def tone(v):
    l = srgb_to_lstar(v)
    shaped = (1.0 - math.exp(-l / SHOULDER)) / (1.0 - math.exp(-100.0 / SHOULDER))
    return lstar_to_srgb(BLACK_L + (WHITE_L - BLACK_L) * shaped)


LUT = [tone(v) for v in range(256)] * 3


def place(source, target):
    tw, th = Image.open(target).size
    image = Image.open(source).convert("RGB")
    sw, sh = image.size
    scale = max(tw / sw, th / sh)
    w, h = math.ceil(sw * scale), math.ceil(sh * scale)
    image = image.resize((w, h), Image.LANCZOS)
    left, top = (w - tw) // 2, (h - th) // 2
    image = image.crop((left, top, left + tw, top + th))
    image = ImageEnhance.Color(image).enhance(SATURATION).point(LUT)
    image.save(target)
    near = target.with_name(target.name.replace("_mid.png", "_near.png"))
    if near.exists():
        Image.new("RGBA", Image.open(near).size, (0, 0, 0, 0)).save(near)
    return sw, sh, tw, th


def main():
    if len(sys.argv) < 2:
        sys.exit("usage: place.py <zone> [room_id ...]")
    zone, only = sys.argv[1], set(sys.argv[2:])
    source_dir = ROOT / "design/levels/midjourney" / zone
    target_dir = ROOT / "Assets/Game/Art/Levels" / zone.capitalize() / "Rooms"
    placed = 0
    for source in sorted(source_dir.iterdir()):
        room = source.stem
        if source.suffix.lower() not in EXTENSIONS or room.startswith("_") or (only and room not in only):
            continue
        target = target_dir / f"{room}_mid.png"
        if not target.exists():
            print(f"skip {room}: no {target.relative_to(ROOT)} (wrong room id?)")
            continue
        sw, sh, tw, th = place(source, target)
        print(f"placed {room} ({sw}x{sh} -> {tw}x{th})")
        placed += 1
    print(f"{placed} backdrop(s) placed in {target_dir.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
