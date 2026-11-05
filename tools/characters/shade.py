#!/usr/bin/env python3
"""Paints light onto the code-drawn character frames, so they stand beside the painted backdrops
(2026-11-04, user: «make the game look very expensive»; decided by Claude under standing consent).

  python tools/characters/shade.py <frame.png> ...          -> shaded in place
  python tools/characters/shade.py --dir <folder> ...       -> every frame under the folders

Only the silhouette is known, so the light is read off its shape: a cool rim where the outline faces the
upper left (the light the rooms are painted with), a soft shade where it faces away, the body a little darker
toward the feet, and a faint inner glow off the lit side. The flat fills of the drawing turn into volume; the
ink outline and the eyes stay as they were. Run after tools/hero/split.py or tools/enemies/split.py, on both
the frames and their Source/Final copies, then CharacterNormals.AttachAll for the normals.
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter

LIGHT = np.array([-0.62, -0.78])          # towards the light, image axes (y down): upper left
RIM = np.array([214, 226, 240], np.float32)  # cool moonlit rim
RIM_STRENGTH, SHADE_STRENGTH, FEET_DARK = 0.7, 0.4, 0.22


def blur(a, sigma):
    img = Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8))
    return np.asarray(img.filter(ImageFilter.GaussianBlur(sigma)), np.float32) / 255


def shade(img):
    px = np.asarray(img.convert("RGBA"), np.float32)
    rgb, a = px[..., :3], px[..., 3] / 255
    if a.max() < 0.05:
        return img
    soft = blur(a, 3.0)
    gy, gx = np.gradient(soft)
    length = np.sqrt(gx * gx + gy * gy) + 1e-6
    nx, ny = -gx / length, -gy / length          # out of the body
    facing = nx * LIGHT[0] + ny * LIGHT[1]
    edge = np.clip((1 - blur(a, 5.0)) * 2.0, 0, 1) * a     # a band a few pixels in from the outline, past the ink
    deep = np.clip((1 - blur(a, 10.0)) * 1.5, 0, 1) * a    # a wider one, for the shade and the glow
    rim = np.clip(facing, 0, 1) ** 1.5 * edge
    away = np.clip(-facing, 0, 1) * deep
    # the ink stays ink: dark pixels take less of the rim
    lum = rgb.mean(-1) / 255
    ink = np.clip((lum - 0.06) / 0.12, 0, 1)
    out = rgb + (RIM - rgb) * (rim * RIM_STRENGTH * ink)[..., None]
    out *= (1 - away * SHADE_STRENGTH)[..., None]
    ys = np.nonzero(a.max(1) > 0.05)[0]
    top, bottom = ys.min(), ys.max()
    t = np.clip((np.arange(a.shape[0]) - top) / max(1, bottom - top), 0, 1)
    out *= (1 - FEET_DARK * t[:, None] ** 2)[..., None]
    glow = np.clip(facing, 0, 1) * deep * (1 - edge)
    out += (RIM * 0.22 * glow[..., None] * ink[..., None])
    # the eyes stay as drawn: bright, coloured pixels are light sources, not surfaces
    mx, mn = rgb.max(-1), rgb.min(-1)
    lit = ((mx >= 200) & (mx - mn >= 60)) | (rgb.mean(-1) >= 235)
    out[lit] = rgb[lit]
    res = np.dstack([np.clip(out, 0, 255), px[..., 3]]).astype(np.uint8)
    return Image.fromarray(res, "RGBA")


def hero_rim(img, strength=0.6):
    """The hero only: the outermost opaque pixels facing the light take the cool rim, so his edge reads the
    brightest and coldest on screen (VISUAL_BIBLE: the hero is the one cold light among warm enemies)."""
    px = np.asarray(img.convert("RGBA"), np.float32).copy()
    solid = px[..., 3] >= 255
    left = np.zeros_like(solid); left[:, 1:] = solid[:, :-1]
    up = np.zeros_like(solid); up[1:, :] = solid[:-1, :]
    upleft = np.zeros_like(solid); upleft[1:, 1:] = solid[:-1, :-1]
    edge = solid & ~(left & up & upleft)
    inner = np.zeros_like(edge)  # and one pixel further in, so the line has body
    inner[:, 1:] |= edge[:, :-1]; inner[1:, :] |= edge[:-1, :]
    band = (edge | (inner & solid))
    k = np.where(edge, strength, strength * 0.5)[band][:, None]
    px[..., :3][band] = px[..., :3][band] * (1 - k) + RIM * k
    return Image.fromarray(px.astype(np.uint8), "RGBA")


def main():
    args = sys.argv[1:]
    files = []
    if args and args[0] == "--dir":
        for d in args[1:]:
            files += [p for p in Path(d).rglob("*.png")]
    else:
        files = [Path(a) for a in args]
    for f in files:
        shade(Image.open(f)).save(f)
    print(f"shaded {len(files)} frames")


if __name__ == "__main__":
    main()
