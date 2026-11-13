#!/usr/bin/env python3
"""Puts Midjourney room backdrops in place of the painter's far layer (gamedesign/MIDJOURNEY.md).

Usage: python tools/midjourney/place.py <zone> [room_id ...]   e.g. python tools/midjourney/place.py tomb
Needs Pillow (pip install pillow). Reads design/levels/midjourney/<zone>/<room_id>.(png|jpg|jpeg|webp),
crops it to the room's proportions from the centre, scales it to the size of
Assets/Game/Art/Levels/<Zone>/Rooms/<room_id>_mid.png and grades it to a background plan (VISUAL_BIBLE.md §10:
darker and duller than the playfield). The painter's near wall of that room (<room_id>_near.png) becomes a soft dark frame:
the painting already holds the room's architecture, and the flat wall hid most of it.
Then in Unity: ContentBuilder.RefreshLevelArt.
"""
import math
import time
import sys
from pathlib import Path

from PIL import Image, ImageChops, ImageEnhance, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
EXTENSIONS = (".png", ".jpg", ".jpeg", ".webp")

# Midjourney paints the far plan too bright: compress the lights into a soft shoulder and take out a
# little colour, so the playfield stays the brightest thing on screen. Blacks stay deep (a lifted black
# turned the paintings milky grey); each room keeps its own hue (rooms differ in colour by design).
# 2026-11-13, user (gallery 6, a Hollow Knight board as the reference): «rooms are ugly» — the far plan had become one
# flat brown smear: its lights were pressed to L 55, a third to a half of it was haze, the colour was taken out.
# Hollow Knight's far plan glows: the lights and the colour stay, only the darks are lifted into the room's mist
# (DARK_LIFT below), so the black playfield still stands in front.
BLACK_L, WHITE_L, SHOULDER = 2.0, 80.0, 62.0
SATURATION = 1.05


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

# Two rooms' paintings met in a hard line of two hues (user, 2026-10-27: «looks raw»). Each painting
# sinks into one shared dark at its edges, so neighbours meet in the same shadow. The layer is wider
# than the room by MARGIN on each side (ContentBuilderLevelArt) and slides up to MARGIN with the camera,
# so the room's edge may show anything from 0 to 2 × MARGIN in from the painting's edge: the fade is long
# and steep there (a neighbour seen from the next room had slid its lit middle up to the seam).
PPU = 32
NEAR_PPU = 48  # the near layer (tools/level-art/jobs.js)
MARGIN = 3.0
FADE = 5.5
EDGE_DARK = (8, 10, 14)


def save(image, path):
    # An open Unity Editor locks a PNG for a moment while it imports the one written just before (Windows).
    for attempt in range(20):
        try:
            image.save(path)
            return
        except OSError:
            if attempt == 19:
                raise
            time.sleep(0.5)


def smooth(t):
    t = max(0.0, min(1.0, t))
    return t * t * (3 - 2 * t)


# Soft seams (2026-11-13, the user's screenshot of the Tomb: «the passage between rooms is very sharp in colour»):
# in these zones a room's far plan is not cut by a mask at the room's edge and does not sink into a dark frame —
# it fades out in alpha over its margin and lies over its neighbours, so two paintings cross-fade in the air
# between two rooms. ContentBuilderLevelArt.SoftSeamZones is the same list; the layer slides 1 unit at most there.
SOFT_ZONES = {"tomb", "forest", "village", "capital"}  # the Marsh has no paintings yet: its far plan is the painter's
SOFT_CLEAR, SOFT_FULL = 0.5, 4.0  # units from the picture's edge: see-through up to the first, solid from the second


def feather(image, soft=False):
    w, h = image.size
    if soft:
        def ramp(n):
            return [smooth((min(i, n - 1 - i) / PPU - SOFT_CLEAR) / (SOFT_FULL - SOFT_CLEAR)) for i in range(n)]
        # a narrow room: the solid part never shrinks below a third of the side
        def fit(values, n):
            limit = n / 3
            return [smooth((min(i, n - 1 - i) - SOFT_CLEAR * PPU) / max(1.0, limit - SOFT_CLEAR * PPU))
                    for i in range(n)] if n < 2 * SOFT_FULL * PPU * 1.5 else values
        xs, ys = fit(ramp(w), w), fit(ramp(h), h)
        row = Image.new("L", (w, 1))
        row.putdata([round(v * 255) for v in xs])
        col = Image.new("L", (1, h))
        col.putdata([round(v * 255) for v in ys])
        out = image.convert("RGBA")
        out.putalpha(ImageChops.multiply(row.resize((w, h)), col.resize((w, h))))
        return out
    # a small room keeps its middle lit: the fade never takes more than a third of a side
    reach_x = min(FADE * PPU, w * 0.35)
    reach_y = min(FADE * PPU, h * 0.35)
    xs = [smooth(min(x, w - 1 - x) / reach_x) for x in range(w)]
    ys = [smooth(min(y, h - 1 - y) / reach_y) for y in range(h)]
    row = Image.new("L", (w, 1))
    row.putdata([round(v * 255) for v in xs])
    col = Image.new("L", (1, h))
    col.putdata([round(v * 255) for v in ys])
    mask = ImageChops.multiply(row.resize((w, h)), col.resize((w, h)))
    return Image.composite(image, Image.new("RGB", (w, h), EDGE_DARK), mask)


# The far layer slides, so its own fade cannot hold the seam still; the near layer is exactly the room
# and does not move. It becomes a soft dark frame, clear in the middle: wherever the camera is, two
# rooms meet in the same shadow.
# 2026-11-13: with the far plan glowing again, two rooms of different hue met in a hard line (the Tomb's chain
# shaft, orange against teal): the frame is wider and deeper, so neighbours meet in the same shadow.
BORDER = 1.9
BORDER_ALPHA = 0.85


def border(near):
    w, h = Image.open(near).size
    if near.parent.parent.name.lower() in SOFT_ZONES:  # soft seams: no dark frame, the layer only carries the props
        save(Image.new("RGBA", (w, h), (0, 0, 0, 0)), near)
        return
    reach = min(BORDER * NEAR_PPU, min(w, h) * 0.3)
    xs = [smooth(min(x, w - 1 - x) / reach) for x in range(w)]
    ys = [smooth(min(y, h - 1 - y) / reach) for y in range(h)]
    row = Image.new("L", (w, 1))
    row.putdata([round(v * 255) for v in xs])
    col = Image.new("L", (1, h))
    col.putdata([round(v * 255) for v in ys])
    clear = ImageChops.multiply(row.resize((w, h)), col.resize((w, h)))
    alpha = clear.point(lambda v: round((255 - v) * BORDER_ALPHA))
    frame = Image.new("RGBA", (w, h), EDGE_DARK + (255,))
    frame.putalpha(alpha)
    save(frame, near)


# Air between the painting and the playfield (2026-11-04, user: «looks untalented — take it from Hollow Knight»):
# the far plan is distance, not a second picture. A touch of blur takes its brush detail away, and the room's own
# tinted haze — the painting's average hue, lifted and kept coloured (a grey one turned them milky) — eats a part
# of its contrast, more toward the floor, where the mist lies. The dark playfield then stands in front of it.
HAZE_TOP, HAZE_BOTTOM, HAZE_L, BLUR = 0.06, 0.2, 36.0, 1.3
# 2026-11-05, user: «jumping onto the side of a block works badly»: a dark backdrop column read as a block of the
# playfield. The darks of the far plan are lifted toward the haze, so nothing behind is ever as dark as the stone
# the hero stands on (Hollow Knight: the background is always lighter and softer than the ground).
DARK_LIFT = 0.5


def atmosphere(image):
    w, h = image.size
    small = image.resize((1, 1), Image.BOX).getpixel((0, 0))
    l = srgb_to_lstar(sum(small) / 3)
    lift = (HAZE_L - l) * 1.6
    haze = tuple(max(0, min(255, round(c + lift))) for c in small)
    # keep the hue: push the haze a little further from grey than the average was
    grey = sum(haze) / 3
    haze = tuple(max(0, min(255, round(grey + (c - grey) * 1.4))) for c in haze)
    image = image.filter(ImageFilter.GaussianBlur(BLUR))
    ramp = Image.linear_gradient("L").resize((w, h))  # 0 at the top, 255 at the bottom
    mask = ramp.point(lambda v: round(255 * (HAZE_TOP + (HAZE_BOTTOM - HAZE_TOP) * v / 255)))
    image = Image.composite(Image.new("RGB", (w, h), haze), image, mask)
    import numpy as np
    a = np.asarray(image, np.float32)
    hz = np.array(haze, np.float32)
    lum, hl = a.mean(-1), max(1.0, hz.mean())
    k = np.clip((hl - lum) / hl, 0, 1)[..., None] * DARK_LIFT
    return Image.fromarray(np.clip(a + (hz - a) * k, 0, 255).astype(np.uint8), "RGB")


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
    image = atmosphere(image)
    image = feather(image, target.parent.parent.name.lower() in SOFT_ZONES)
    save(image, target)
    near = target.with_name(target.name.replace("_mid.png", "_near.png"))
    if near.exists():
        border(near)
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
