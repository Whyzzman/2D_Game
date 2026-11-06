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
from PIL import Image, ImageDraw, ImageFilter

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
    # the fallen log lies against his skirt from 0.555 on: the box stops short of it (2026-11-06)
    "HollowWarden": {"body": (0.12, 0.0, 0.557, 0.98)},
    # roots on the ground joined the shaman to his staff, standing apart on the sheet: one piece, the staff floating
    # by his side in every frame (2026-11-06); boxed apart, the staff is his weapon again
    "RootShaman": {"body": (0.0, 0.0, 0.52, 0.97), "weapon": (0.62, 0.1, 0.76, 0.935)},
    "AshCommander": {"body": (0.0, 0.0, 0.78, 0.97)},
    "OathBoundKing": {"body": (0.0, 0.0, 0.62, 0.97)},
    "PlagueStag": {"body": (0.0, 0.0, 0.62, 1.0), "weapon": (0.62, 0.0, 1.0, 0.97)},
    "PlagueDoctor": {"body": (0.0, 0.0, 0.5, 1.0)},
    "Bellringer": {"body": (0.0, 0.0, 0.62, 1.0)},
    # the Capital's weeping angel without its plinth: the statue wakes
    "StoneGargoyle": {"body": (0.0, 0.0, 1.0, 0.7)},
}
# a background that glows behind the figure (lighter than the frame's colours, so the cut kept it as a cloud by
# his side, 2026-11-06): the cut-out grows into what continues it smoothly — warm, not dark — and stops at the
# figure's sharp dark edge
GLOW_BG = {"HollowWarden"}
# figures Midjourney drew facing left
FLIP_BODY = {"Watchdog", "BoneArcher", "GuardianOfTheTomb", "RoyalSpearman", "ZombieSoldier", "AshCommander",
             "OathBoundKing", "Bellringer"}
# manual eyes already measured on the turned figure (the rest are read off the picture as Midjourney drew it)
EYES_ON_FLIPPED = {"Watchdog", "AshCommander", "OathBoundKing", "Bellringer", "GuardianOfTheTomb"}
# the body and the weapon cut differently: the weapon's red is pink enough to go with the background, the body's
# feet stand on a pink shadow that only the plain cut removes
SPLIT_CUT = {"Scarecrow"}
# characters whose own Midjourney sheet is still to come (fast hours ran out, 2026-11-05): an unused variant of a
# kindred sheet, recoloured, with a weapon borrowed from another where its own was not drawn apart. Each stays
# its own silhouette (a different variant), as Hollow Knight's palette-swapped husks do.
TINT = {
    "AshCommander": {"hue": 14, "sat": 0.9, "val": 0.78},
    "OathBoundKing": {"hue": -72, "sat": 0.95, "val": 0.95},
    "PlagueStag": {"hue": 18, "sat": 1.25, "val": 0.95},
    "MarshCorpseEater": {"hue": 25, "sat": 0.75, "val": 0.95},
    "PlagueDoctor": {"hue": 0, "sat": 0.18, "val": 0.62},
    "Bellringer": {"hue": -125, "sat": 1.1, "val": 0.9},
    # its own sheet, but Midjourney drew the robe pale and the magenta ground tinged it pink: the one light figure
    # among dark ones (mean 178 against 20–60). Ash-violet dusk robe; the gold staff and the eyes keep their light
    # (2026-11-06, decided by Claude under standing approval)
    "PalacePriest": {"hue": -104, "sat": 1.0, "val": 0.36},
}
WEAPON_FROM = {"AshCommander": "GuardianOfTheTomb", "OathBoundKing": "GuardianOfTheTomb", "Bellringer": "ShieldGuard"}
# a source that is already cut out (transparent), not on magenta: the Capital's weeping-angel statue wakes as the gargoyle
PREMADE = set()
# beasts hold nothing: a second piece of their picture is a shadow or a stray tuft
NO_WEAPON = {"CorruptedHound", "Watchdog", "MarshCorpseEater", "StoneGargoyle", "HollowWarden"}
# eyes read off the pictures by hand (body pixels: x, y, radius, colour), where the brightest spots are not the eyes
EYES = {
    "BoneArcher": [[42, 95, 6, "#9ae8ff"], [64, 93, 5, "#9ae8ff"]],
    "CorruptedHound": [[905, 205, 7, "#ff9a40"]],
    "ForestArcher": [[219, 95, 7, "#e0ffb0"], [259, 95, 7, "#e0ffb0"]],
    "GuardianOfTheTomb": [[404, 80, 6, "#ff9040"], [426, 80, 6, "#ff9040"]],
    "HollowWarden": [[253, 110, 8, "#ffb040"], [271, 110, 8, "#ffb040"]],
    "PalacePriest": [[306, 159, 8, "#ffd040"], [333, 159, 8, "#ffd040"]],
    "PossessedVillager": [[208, 122, 14, "#ff7a30"], [272, 122, 13, "#ff7a30"]],
    "RootShaman": [[140, 178, 6, "#a0ff80"], [156, 180, 6, "#a0ff80"]],
    "RoyalCrossbowman": [[132, 80, 6, "#ffc040"], [173, 80, 6, "#ffc040"]],
    "RoyalSpearman": [[109, 171, 6, "#ffe040"], [127, 171, 6, "#ffe040"]],
    "Scarecrow": [[138, 131, 9, "#ffd070"], [186, 134, 9, "#ffd070"]],
    "ShieldGuard": [[318, 40, 7, "#c080ff"]],
    "MarshCorpseEater": [[825, 188, 7, "#b6ff5a"]],
    "ZombieSoldier": [[62, 67, 6, "#b0a0ff"], [76, 66, 5, "#b0a0ff"]],
    # read off the turned figures
    "AshCommander": [[365, 104, 8, "#ff8a3a"]],
    "OathBoundKing": [[216, 99, 7, "#ffd27a"]],
    "StoneGargoyle": [[205, 108, 5, "#9ad0ff"], [228, 106, 5, "#9ad0ff"]],
    "Bellringer": [[540, 95, 6, "#ffb060"], [562, 93, 6, "#ffb060"]],
    "Watchdog": [[1080, 165, 13, "#ff5030"]],
}
BY_COLOUR = {"GuardianOfTheTomb", "RoyalCrossbowman", "Scarecrow", "HollowWarden", "AshCommander", "OathBoundKing",
             "PlagueDoctor"}


def save_retry(image, path):
    """Windows keeps a PNG locked for a moment while something reads it: try again."""
    import time
    for attempt in range(40):
        try:
            image.save(path)
            return
        except OSError:
            if attempt == 39:
                raise
            time.sleep(0.5)


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


def rgb_to_hsv(a):
    r, g, b = a[..., 0] / 255, a[..., 1] / 255, a[..., 2] / 255
    mx, mn = np.maximum(np.maximum(r, g), b), np.minimum(np.minimum(r, g), b)
    d = mx - mn
    m = d > 1e-6
    safe = np.where(m, d, 1)
    rc, gc, bc = (mx - r) / safe, (mx - g) / safe, (mx - b) / safe
    h = np.where(r == mx, bc - gc, np.where(g == mx, 2 + rc - bc, 4 + gc - rc))
    h = np.where(m, (h / 6) % 1, 0)
    sat = np.where(mx > 1e-6, d / np.where(mx > 1e-6, mx, 1), 0)
    return h, sat, mx


def hsv_to_rgb(h, s, v):
    i = np.floor(h * 6).astype(int) % 6
    f = h * 6 - np.floor(h * 6)
    p, q, t = v * (1 - s), v * (1 - s * f), v * (1 - s * (1 - f))
    r = np.choose(i, [v, q, p, p, t, v])
    g = np.choose(i, [t, v, v, q, p, p])
    b = np.choose(i, [p, p, t, v, v, q])
    return np.stack([r, g, b], -1) * 255


def tint(piece, spec):
    """Recolours a borrowed variant: hue turned, saturation and value scaled; its lit spots keep their light."""
    a = np.asarray(piece, np.float32).copy()
    h, sat, v = rgb_to_hsv(a)
    h = (h + spec.get("hue", 0) / 360) % 1
    sat = np.clip(sat * spec.get("sat", 1), 0, 1)
    v = np.clip(v * spec.get("val", 1), 0, 1)
    mx, mn = a[..., :3].max(-1), a[..., :3].min(-1)
    lit = (mx >= 200) & ((mx - mn) >= 60)
    rgb = hsv_to_rgb(h, sat, v)
    a[..., :3] = np.where(lit[..., None], a[..., :3], rgb)
    return Image.fromarray(np.clip(a, 0, 255).astype(np.uint8), "RGBA")


def eat_glow(piece, rounds=80, step=26.0):
    """The cut-out grows, a pixel ring at a time, into neighbours that continue the background: within `step` of
    the colour of the background beside them, warm (red over green and blue) and not dark."""
    px = np.asarray(piece, np.float32).copy()
    rgb, a = px[..., :3], px[..., 3]
    bg = a < 24
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    warm = (r > g * 1.25) & (r > b * 1.25) & (rgb.mean(-1) > 70)
    for _ in range(rounds):
        grown = False
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nb = np.roll(bg, (dy, dx), (0, 1))
            nb_rgb = np.roll(rgb, (dy, dx), (0, 1))
            close = np.sqrt(((rgb - nb_rgb) ** 2).sum(-1)) < step
            take = nb & ~bg & warm & close
            if take.any():
                bg |= take
                grown = True
        if not grown:
            break
    alpha = Image.fromarray(np.where(bg, 0, a).astype(np.uint8)).filter(ImageFilter.MinFilter(3))
    soft = np.asarray(alpha.filter(ImageFilter.GaussianBlur(0.8)), np.float32)
    px[..., 3] = np.minimum(a, soft)
    return Image.fromarray(px.astype(np.uint8), "RGBA")


def strip_ground(piece):
    """A ground line or a shadow Midjourney drew along the feet: columns whose only paint is the bottom few rows go,
    and so do thin runs at either side (a line on the ground, a spear's shadow); a pink floor shadow under the feet
    goes too."""
    a = np.asarray(piece.getchannel("A")).copy()
    h = a.shape[0]
    band = max(3, int(h * 0.03))
    upper = (a[: h - band] > 40).any(0)
    a[:, ~upper] = 0
    counts = (a > 40).sum(0)
    thin = counts < max(4, h * 0.025)
    for order in (range(a.shape[1]), range(a.shape[1] - 1, -1, -1)):
        for x in order:
            if not thin[x] and counts[x] > 0:
                break
            a[:, x] = 0
    rgb = np.asarray(piece.convert("RGB"), np.float32)
    pink = ((rgb[..., 0] + rgb[..., 2]) / 2 - rgb[..., 1]) / np.maximum(24, rgb.max(-1))
    floor = np.zeros_like(a, bool)
    floor[int(h * 0.88):] = True
    a[floor & (pink > 0.2)] = 0
    # a weapon or a log lying at the feet: in the bottom fifth, a vertical run of paint thinner than 3.5% of the
    # height that does not reach up into the figure is something on the ground, not the figure
    top = int(h * 0.8)
    solid = a > 40
    for x in range(a.shape[1]):
        col = solid[:, x]
        y = top
        while y < h:
            if not col[y]:
                y += 1
                continue
            start = y
            while y < h and col[y]:
                y += 1
            if start > top and (y - start) < h * 0.035:
                a[start:y, x] = 0
    # what the cleaning cut loose (a hilt left of a lying sword, a splinter): islands under 3% of the figure go
    img = Image.fromarray(((a > 40) * 255).astype(np.uint8))
    parts = components(img, keep=8)
    if parts:
        keep = np.zeros_like(a, bool)
        for area, mask in parts:
            box = mask.getbbox()
            tall = box is not None and (box[3] - box[1]) >= h * 0.25   # a staff beside the figure stays
            if area >= parts[0][0] * 0.15 or (area >= parts[0][0] * 0.03 and tall):
                keep |= np.asarray(mask) > 0
        a[~keep] = 0
    piece = piece.copy()
    piece.putalpha(Image.fromarray(a))
    box = piece.getchannel("A").point(lambda v: 255 if v > 24 else 0).getbbox()
    return piece.crop(box)


def build(actor):
    src = PARTS / actor / "source.png"
    if not src.exists():
        print("no source:", actor)
        return
    raw = Image.open(src).convert("RGBA")
    if actor in PREMADE:
        clean = weapon_clean = raw
    else:
        clean = cut(raw, by_colour=actor in BY_COLOUR)
        if actor in GLOW_BG:
            clean = eat_glow(clean)
        weapon_clean = cut(raw, by_colour=True) if actor in SPLIT_CUT else clean
    alpha = clean.getchannel("A")
    if actor in BOXES:
        W, H = alpha.size
        chosen = []
        for name, (x0, y0, x1, y1) in BOXES[actor].items():
            source_alpha = (weapon_clean if name == "weapon" else clean).getchannel("A")
            boxed = Image.new("L", alpha.size, 0)
            region = (int(x0 * W), int(y0 * H), int(x1 * W), int(y1 * H))
            boxed.paste(source_alpha.crop(region), region[:2])
            area, mask = components(boxed, keep=1)[0]
            chosen.append((name, area, mask))
    else:
        pieces = components(alpha)
        chosen = [("body", *pieces[0])]
        if actor in SPLIT_CUT:
            body_box = pieces[0][1].getbbox()
            for area, mask in components(weapon_clean.getchannel("A")):
                mb = mask.getbbox()
                if mb and (mb[0] >= body_box[2] - 4 or mb[2] <= body_box[0] + 4):
                    chosen.append(("weapon", area, mask))
                    break
        elif len(pieces) > 1 and pieces[1][0] >= pieces[0][0] * 0.004 and actor not in NO_WEAPON:
            chosen.append(("weapon", *pieces[1]))
    meta = {}
    (fw, fh), box = old_size(actor)
    for name, area, mask in chosen:
        base = weapon_clean if name == "weapon" else clean
        piece = base.copy()
        piece.putalpha(Image.composite(base.getchannel("A"), Image.new("L", base.size, 0), mask))
        piece = piece.crop(piece.getchannel("A").point(lambda v: 255 if v > 24 else 0).getbbox())
        if name == "weapon":
            if piece.height > piece.width:            # stood up: lay it down, handle to the left
                piece = piece.rotate(-90, expand=True)
            if actor in FLIP_WEAPON:
                piece = piece.transpose(Image.FLIP_LEFT_RIGHT)
        else:
            piece = strip_ground(piece)
            if actor in FLIP_BODY:
                piece = piece.transpose(Image.FLIP_LEFT_RIGHT)
        if actor in TINT:
            piece = tint(piece, TINT[actor])
        save_retry(piece, PARTS / actor / f"{name}.png")
        meta[name] = {"w": piece.width, "h": piece.height}
        if name == "body":
            save_retry(glow_of(piece), PARTS / actor / "glow.png")
            eyes = EYES.get(actor)
            if eyes is not None and actor in FLIP_BODY and actor not in EYES_ON_FLIPPED:
                eyes = [[piece.width - e[0]] + list(e[1:]) for e in eyes]
            meta["eyes"] = eyes if eyes is not None else glow_of.eyes
            px = np.asarray(piece, np.float32)
            solid = px[px[..., 3] > 200][:, :3]
            lum = solid.mean(-1)
            meta["dark"] = [int(v) for v in np.median(solid[lum <= np.percentile(lum, 30)], 0)]
    if actor in WEAPON_FROM and "weapon" not in meta:
        w = Image.open(PARTS / WEAPON_FROM[actor] / "weapon.png").convert("RGBA")
        if actor in TINT:
            w = tint(w, TINT[actor])
        save_retry(w, PARTS / actor / "weapon.png")
        meta["weapon"] = {"w": w.width, "h": w.height}
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
