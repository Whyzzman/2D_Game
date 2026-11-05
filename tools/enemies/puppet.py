#!/usr/bin/env python3
"""Cuts each painted enemy (a Midjourney sheet: the figure, and its weapon apart) into puppet pieces for the
enemy rig (design/characters/enemies/enemies.js, Enemies.loadPuppet):

  python tools/enemies/puppet.py [Actor ...]

design/characters/enemies/parts/<Actor>/source.png  ->  body.png, weapon.png (if any), parts.json
The figure is the largest piece of the picture, the weapon the next. The body keeps the size the old frames
had (the height of the first Idle frame, so hitboxes and the look in the room stay where they were); eyes are
found as the brightest coloured spots. 2026-11-05, user: «every model at the level of the Midjourney hero,
Hollow Knight-like»; decided by Claude under standing approval.
"""
import json, sys
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/midjourney"))
from sprites import cut  # noqa: E402
PARTS = ROOT / "design/characters/enemies/parts"
ART = ROOT / "Assets/Game/Art/Characters"
FOLDERS = {
    "ZombieSoldier": "Enemies/ZombieSoldier", "ShieldGuard": "Enemies/ShieldGuard", "BoneArcher": "Enemies/BoneArcher",
    "GuardianOfTheTomb": "Bosses/GuardianOfTheTomb", "CorruptedHound": "Enemies/CorruptedHound", "ForestArcher": "Enemies/ForestArcher",
    "RootShaman": "Enemies/RootShaman", "RoyalSpearman": "Enemies/RoyalSpearman", "HollowWarden": "Bosses/HollowWarden",
    "PossessedVillager": "Enemies/PossessedVillager", "Scarecrow": "Enemies/Scarecrow", "Watchdog": "Enemies/Watchdog",
    "RoyalCrossbowman": "Enemies/RoyalCrossbowman", "PalacePriest": "Enemies/PalacePriest", "StoneGargoyle": "Enemies/StoneGargoyle",
    "AshRaven": "Enemies/AshRaven", "Bellringer": "Bosses/Bellringer", "AshCommander": "Bosses/AshCommander",
    "OathBoundKing": "Bosses/OathBoundKing", "MarshCorpseEater": "Enemies/MarshCorpseEater", "PlagueDoctor": "Enemies/PlagueDoctor",
    "MarshLeech": "Enemies/MarshLeech", "PlagueStag": "Bosses/PlagueStag",
}
# a weapon Midjourney drew pointing the other way (handle on the right): mirrored before it is used
FLIP_WEAPON = {"ZombieSoldier", "ShieldGuard"}
# red and crimson cloth is pink enough to be taken for the background: cut these by the frame's colour only
# where the figure and the weapon touch in the picture (a ground line, a log against the leg): their boxes by hand,
# as fractions of the source (x0, y0, x1, y1); inside each box the largest piece is taken
BOXES = {
    "GuardianOfTheTomb": {"body": (0.0, 0.0, 0.72, 0.97), "weapon": (0.74, 0.0, 1.0, 0.97)},
    "HollowWarden": {"body": (0.12, 0.0, 0.6, 0.98)},
}
# figures Midjourney drew facing left
FLIP_BODY = {"Watchdog"}
# eyes read off the pictures by hand (body pixels: x, y, radius, colour), where the brightest spots are not the eyes
EYES = {
    "BoneArcher": [[42, 95, 6, "#9ae8ff"], [64, 93, 5, "#9ae8ff"]],
    "CorruptedHound": [[905, 205, 7, "#ff9a40"]],
    "ForestArcher": [[252, 92, 7, "#e0ffb0"], [290, 93, 7, "#e0ffb0"]],
    "GuardianOfTheTomb": [[198, 118, 7, "#ff9040"]],
    "HollowWarden": [[253, 110, 8, "#ffb040"], [271, 110, 8, "#ffb040"]],
    "PalacePriest": [[306, 159, 8, "#ffd040"], [333, 159, 8, "#ffd040"]],
    "PossessedVillager": [[208, 122, 14, "#ff7a30"], [272, 122, 13, "#ff7a30"]],
    "RootShaman": [[228, 180, 8, "#a0ff80"]],
    "RoyalCrossbowman": [[132, 80, 6, "#ffc040"], [173, 80, 6, "#ffc040"]],
    "RoyalSpearman": [[109, 171, 6, "#ffe040"], [127, 171, 6, "#ffe040"]],
    "Scarecrow": [[138, 131, 9, "#ffd070"], [186, 134, 9, "#ffd070"]],
    "ShieldGuard": [[490, 90, 7, "#c080ff"]],
    "ZombieSoldier": [[62, 67, 6, "#b0a0ff"], [76, 66, 5, "#b0a0ff"]],
    "Watchdog": [[1080, 165, 13, "#ff5030"]],
}
BY_COLOUR = {"GuardianOfTheTomb", "RoyalCrossbowman", "Scarecrow", "HollowWarden"}


def components(alpha, keep=6):
    """Pieces of the picture, largest first, as boolean masks (4-neighbour, on a quarter-size copy)."""
    small = (np.asarray(alpha.resize((alpha.width // 4, alpha.height // 4))) > 64).astype(np.uint8) * 255
    img = Image.fromarray(small).copy()
    found = []
    for label in range(1, 200):
        arr = np.asarray(img)
        ys, xs = np.nonzero(arr == 255)
        if len(ys) == 0:
            break
        ImageDraw.floodfill(img, (int(xs[0]), int(ys[0])), label)
        found.append(label)
    arr = np.asarray(img)
    masks = sorted(((arr == l).sum(), l) for l in found)[::-1][:keep]
    big = [Image.fromarray(((arr == l) * 255).astype(np.uint8)).resize(alpha.size).point(lambda v: 255 if v > 0 else 0) for _, l in masks]
    return [(a, m) for (a, _), m in zip(masks, big)]


def old_size(actor):
    folder = ART / FOLDERS[actor]
    idle = sorted([p for p in folder.rglob("*.png") if "/idle" in p.as_posix().lower()])
    frame = Image.open(idle[0]).convert("RGBA")
    box = frame.getchannel("A").point(lambda v: 255 if v > 40 else 0).getbbox()
    return frame.size, box


def eyes_of(body):
    px = np.asarray(body, np.float32)
    rgb, a = px[..., :3], px[..., 3]
    mx, mn = rgb.max(-1), rgb.min(-1)
    h = body.height
    yy = np.arange(body.height)[:, None].repeat(body.width, 1)
    lit = (a > 200) & (mx >= 185) & ((mx - mn) >= 60) & (yy < h * 0.32)
    ys, xs = np.nonzero(lit)
    if len(xs) < 3:
        return []
    # up to two clusters along x
    order = np.argsort(xs)
    xs, ys = xs[order], ys[order]
    gaps = np.diff(xs)
    split = int(np.argmax(gaps)) + 1 if len(gaps) and gaps.max() > 4 else len(xs)
    out = []
    for gx, gy in ((xs[:split], ys[:split]), (xs[split:], ys[split:])):
        if len(gx) >= 2:
            out.append([float(gx.mean()), float(gy.mean())])
    return out[:2]


def glow_of(body):
    """The light of the figure: the brightest coloured spots of its head (eyes, a mask, a lantern), as a layer the
    rig adds over the body — always a little, and in a flare just before a blow."""
    from PIL import ImageFilter
    px = np.asarray(body, np.float32)
    rgb, a = px[..., :3], px[..., 3]
    mx, mn = rgb.max(-1), rgb.min(-1)
    score = mx * (mx - mn + 40) / 255
    head = np.zeros(a.shape, bool)
    head[: int(body.height * 0.42)] = True
    cand = (a > 200) & head
    if cand.sum() == 0:
        return Image.new("RGBA", body.size, (0, 0, 0, 0))
    thr = max(np.percentile(score[cand], 99.6), 70)
    mask = cand & (score >= thr)
    out = np.zeros_like(px)
    # the colour of the light at full strength: its own hue, brightest channel to 255, a little toward white
    norm = rgb / np.maximum(1, mx)[..., None] * 255
    out[..., :3] = np.clip(norm * 0.7 + 255 * 0.3, 0, 255)
    out[..., 3] = np.where(mask, 255, 0)
    img = Image.fromarray(out.astype(np.uint8), "RGBA")
    halo = img.filter(ImageFilter.GaussianBlur(3))
    halo.alpha_composite(img)
    # the two brightest spots as eyes, for the rig to draw at a size that reads in the frame
    ys, xs = np.nonzero(mask)
    eyes = []
    if len(xs):
        order = np.argsort(xs); xs, ys = xs[order], ys[order]
        gaps = np.diff(xs)
        cut_at = int(np.argmax(gaps)) + 1 if len(gaps) and gaps.max() > body.width * 0.02 else len(xs)
        for gx, gy in ((xs[:cut_at], ys[:cut_at]), (xs[cut_at:], ys[cut_at:])):
            if len(gx):
                c = norm[gy, gx].mean(0)
                eyes.append([float(gx.mean()), float(gy.mean()), float(max(np.ptp(gx), np.ptp(gy)) / 2 + 1),
                             "#%02x%02x%02x" % tuple(int(v) for v in np.clip(c, 0, 255))])
    glow_of.eyes = eyes[:2]
    return halo


def build(actor):
    src = PARTS / actor / "source.png"
    if not src.exists():
        print("no source:", actor)
        return
    clean = cut(Image.open(src).convert("RGBA"), by_colour=actor in BY_COLOUR)
    alpha = clean.getchannel("A")
    if actor in BOXES:
        W, H = alpha.size
        chosen = []
        for name, (x0, y0, x1, y1) in BOXES[actor].items():
            boxed = Image.new("L", alpha.size, 0)
            region = (int(x0 * W), int(y0 * H), int(x1 * W), int(y1 * H))
            boxed.paste(alpha.crop(region), region[:2])
            area, mask = components(boxed, keep=1)[0]
            chosen.append((name, area, mask))
    else:
        pieces = components(alpha)
        chosen = [("body", *pieces[0])]
        if len(pieces) > 1 and pieces[1][0] >= pieces[0][0] * 0.004:
            chosen.append(("weapon", *pieces[1]))
    meta = {}
    (fw, fh), box = old_size(actor)
    for name, area, mask in chosen:
        piece = clean.copy()
        piece.putalpha(Image.composite(alpha, Image.new("L", alpha.size, 0), mask))
        piece = piece.crop(piece.getchannel("A").point(lambda v: 255 if v > 24 else 0).getbbox())
        if name == "weapon":
            if piece.height > piece.width:            # stood up: lay it down, handle to the left
                piece = piece.rotate(-90, expand=True)
            if actor in FLIP_WEAPON:
                piece = piece.transpose(Image.FLIP_LEFT_RIGHT)
        piece.save(PARTS / actor / f"{name}.png")
        meta[name] = {"w": piece.width, "h": piece.height}
        if name == "body":
            if actor in FLIP_BODY:
                piece = piece.transpose(Image.FLIP_LEFT_RIGHT)
                piece.save(PARTS / actor / f"{name}.png")
            glow_of(piece).save(PARTS / actor / "glow.png")
            meta["eyes"] = EYES.get(actor, glow_of.eyes)
            px = np.asarray(piece, np.float32)
            solid = px[px[..., 3] > 200][:, :3]
            lum = solid.mean(-1)
            meta["dark"] = [int(v) for v in np.median(solid[lum <= np.percentile(lum, 30)], 0)]
    # the old figure's height and where it stood in the frame
    meta["frame"] = [fw, fh]
    meta["height"] = box[3] - box[1]
    meta["width"] = box[2] - box[0]
    meta["centre"] = (box[0] + box[2]) / 2
    (PARTS / actor / "parts.json").write_text(json.dumps(meta, indent=1), encoding="utf-8")
    print(actor, "pieces:", ", ".join(k for k in ("body", "weapon") if k in meta), "eyes:", len(meta.get("eyes", [])))


def main():
    actors = sys.argv[1:] or [d.name for d in PARTS.iterdir() if d.is_dir()]
    for a in actors:
        build(a)


if __name__ == "__main__":
    main()
