#!/usr/bin/env python3
"""Turns Midjourney pictures of single objects on flat magenta into the game's static sprites.

  python tools/midjourney/sprites.py            -> every <name>.png of design/levels/midjourney/props/picks
  python tools/midjourney/sprites.py urn crate  -> only those

Names and target sprites: design/levels/midjourney/props/prompts.json (short name = file name in picks/).
The background is what is magenta or close to the colours of the picture's frame (see cut); soft edges
lose the pink they took from it, so no fringe is left. Needs Pillow and numpy. The
object is fitted into the old placeholder's box — same canvas, same pivot, same height in the world — so
nothing in the game moves: standing things keep their feet on the bottom of the box, hanging ones their top.
Writes Assets/Game/Art/<path>.png and Assets/Game/Art/Source/Final/<path>.png (drawn art: the generator
keeps it over its placeholder). Then in Unity: refresh; characters get normals (CharacterNormals.AttachAll).
"""
import json
import os
import re
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
MJ = ROOT / "design/levels/midjourney/props"
ART = ROOT / "Assets/Game/Art"
FINAL = ART / "Source/Final"
# the placeholders' own boxes, kept aside before the first run (else the file in place is read)
PLACEHOLDERS = Path(os.environ["SPRITE_PLACEHOLDERS"]) if os.environ.get("SPRITE_PLACEHOLDERS") else None
NEAR = 30.0      # this close to a frame colour looks like the background
PINK_BG = 0.3    # this magenta looks like the background whatever its brightness
PINK_ALWAYS = 0.33  # and this magenta is the background even shut in by the object (between bars, under a cart)
# and per picture, where a shut-in background is darker: inside the gate's arch, behind its bars
PINK_ALWAYS_BY = {"gate": 0.13}
BY_COLOUR = {"barrier"}         # violet ghost fire: pink itself, cut by its distance from the border colour

SHORT = {"env_break_urn_01": "urn", "env_break_crate_01": "crate", "env_break_skulls_01": "skulls",
         "env_forest_stump_01": "stump", "env_forest_arena_crest": "crest", "env_tomb_boss_gate": "gate",
         "env_tomb_boss_barrier": "barrier", "env_tomb_checkpoint_base": "shrine",
         "env_tomb_checkpoint_flame_lit": "flame_lit", "env_tomb_checkpoint_flame_cold": "flame_cold",
         "training_dummy": "dummy", "training_attacker": "attacker", "npc_severyn": "severyn",
         "npc_ostap": "ostap", "npc_myroslava": "myroslava", "npc_gnat": "gnat",
         "chest_locked": "chest_locked", "chest_open": "chest_open"}


def pinkness(px):
    """How magenta each pixel is, whatever its brightness: Midjourney's flat magenta, its gradient and the
    shadow on its floor are high; wood, stone, bone, cloth and fire are low."""
    r, g, b = px[..., 0], px[..., 1], px[..., 2]
    return ((r + b) / 2 - g) / np.maximum(24, px.max(-1))


def frame_colours(px, pink):
    """The background's colours as the frame shows them — only the frame's pinkish samples, so an object
    touching the edge does not teach the background its own colour."""
    h, w, _ = px.shape
    ys = np.r_[np.full(w // 8, 2), np.full(w // 8, h - 3), np.arange(0, h, 8), np.arange(0, h, 8)]
    xs = np.r_[np.arange(0, w, 8)[:w // 8], np.arange(0, w, 8)[:w // 8], np.full(len(range(0, h, 8)), 2),
               np.full(len(range(0, h, 8)), w - 3)]
    keep = pink[ys, xs] > 0.1
    return px[ys[keep], xs[keep]]


def connected_to_frame(mask):
    """The part of a boolean mask reachable from the picture's frame through the mask (4-neighbour fill)."""
    h, w = mask.shape
    pad = np.ones((h + 2, w + 2), np.uint8)
    pad[1:-1, 1:-1] = mask
    img = Image.fromarray(pad * 255).copy()  # an image made straight from an array ignores floodfill
    ImageDraw.floodfill(img, (0, 0), 128)
    return np.asarray(img)[1:-1, 1:-1] == 128


def cut(image, by_colour=False, always=None):
    """Midjourney's «flat magenta» is a pink-to-mauve gradient with a shadow on its floor. The background is
    what looks like it — magenta whatever its brightness, or close to one of the frame's pinkish colours
    (by_colour: only the latter, for violet things like a ghost fire) — and is joined to the frame; dark
    patches inside the object are surrounded by it and stay. The object's colours stay as they are; only
    its rim, where it was mixed with the pink, loses the tint."""
    px = np.asarray(image.convert("RGB"), dtype=np.float32)
    small = image.convert("RGB").resize((image.width // 2, image.height // 2), Image.BILINEAR)
    sp = np.asarray(small, dtype=np.float32)
    pink = pinkness(sp)
    edge = frame_colours(sp, pink)
    near = np.empty(pink.shape, np.float32)
    for y0 in range(0, sp.shape[0], 64):
        part = sp[y0:y0 + 64, :, None, :] - edge[None, None, :, :]
        near[y0:y0 + 64] = np.sqrt((part ** 2).sum(-1)).min(-1)
    like = near < NEAR if by_colour else (pink > PINK_BG) | (near < NEAR)
    bg = connected_to_frame(like)
    if not by_colour:
        bg |= pink > (always or PINK_ALWAYS)
    alpha = Image.fromarray(np.where(bg, 0, 255).astype(np.uint8)).resize(image.size, Image.BILINEAR)
    alpha = alpha.filter(ImageFilter.MedianFilter(5)).filter(ImageFilter.MinFilter(3)).filter(ImageFilter.GaussianBlur(1.0))
    a = np.asarray(alpha, dtype=np.float32) / 255
    r, g, b = px[..., 0], px[..., 1], px[..., 2]
    # the pink in a soft edge: red and blue pulled down to the green, as much as it was background
    t = np.where((a > 0) & (a < 0.98), 1 - a, 0)
    r2 = np.where(r > g, r - (r - g) * t * 0.8, r)
    b2 = np.where(b > g, b - (b - g) * t * 0.9, b)
    out = np.dstack([r2, g, b2, a * 255]).clip(0, 255).astype(np.uint8)
    return strip_floor(Image.fromarray(out, "RGBA"))


def strip_floor(img):
    """A painted ground line under the object: bottom rows that are solid nearly edge to edge go."""
    a = img.getchannel("A")
    box = a.point(lambda v: 255 if v > 64 else 0).getbbox()
    if not box:
        return img
    px = a.load()
    width = box[2] - box[0]
    for y in range(box[3] - 1, max(box[1], box[3] - img.height // 8), -1):
        solid = sum(1 for x in range(box[0], box[2]) if px[x, y] > 128)
        if solid < width * 0.8:
            break
        for x in range(img.width):
            px[x, y] = 0
    img.putalpha(a)
    return img


def pivot_y(meta):
    m = re.search(r"spritePivot: \{x: [\d.]+, y: ([\d.]+)\}", meta)
    return float(m.group(1)) if m else 0.0


def fit(obj, target):
    """Into the placeholder's canvas: as tall as its box (no wider than the canvas), feet/top/centre held."""
    old = Image.open(target)
    W, H = old.size
    box = old.getchannel("A").point(lambda v: 255 if v > 24 else 0).getbbox() or (0, 0, W, H)
    ob = obj.getchannel("A").point(lambda v: 255 if v > 24 else 0).getbbox()
    obj = obj.crop(ob)
    bw, bh = box[2] - box[0], box[3] - box[1]
    scale = min(bh / obj.height, W * 0.98 / obj.width, max(bw, W * 0.6) * 1.25 / obj.width)
    obj = obj.resize((max(1, round(obj.width * scale)), max(1, round(obj.height * scale))), Image.LANCZOS)
    canvas = Image.new("RGBA", (W, H))
    x = (W - obj.width) // 2
    py = pivot_y(Path(str(target) + ".meta").read_text(encoding="utf-8"))
    if py <= 0.01:
        y = box[3] - obj.height          # standing: feet on the box's bottom (the pivot)
    elif py >= 0.99:
        y = box[1]                       # hanging: from the box's top
    else:
        y = (box[1] + box[3] - obj.height) // 2
    canvas.alpha_composite(obj, (x, max(0, y)))
    return canvas


def main():
    spec = json.loads((MJ / "prompts.json").read_text(encoding="utf-8"))
    flips = set(spec.get("flip", []))
    only = set(sys.argv[1:])
    done = 0
    for path, *_ in spec["items"]:
        name = SHORT[Path(path).name]
        if only and name not in only:
            continue
        src = MJ / "picks" / f"{name}.png"
        if not src.exists():
            print("missing", src.name)
            continue
        obj = cut(Image.open(src), by_colour=name in BY_COLOUR, always=PINK_ALWAYS_BY.get(name))
        if name in flips:
            obj = obj.transpose(Image.FLIP_LEFT_RIGHT)
        # the box comes from the placeholder (a copy under PLACEHOLDERS, else the file in place)
        target = ART / f"{path}.png"
        kept = PLACEHOLDERS / f"{path}.png" if PLACEHOLDERS else None
        sprite = fit(obj, kept if kept and kept.exists() else target)
        for base in (ART, FINAL):
            out = base / f"{path}.png"
            out.parent.mkdir(parents=True, exist_ok=True)
            sprite.save(out)
        done += 1
    print(f"{done} sprites written")


if __name__ == "__main__":
    main()
