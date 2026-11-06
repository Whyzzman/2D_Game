#!/usr/bin/env python3
"""An animated GIF of the characters in motion over a painted room, from the rows tools/enemies/render.js and
tools/hero/render.js drew (kept in %TEMP%/frames-keep): to show what the frames look like moving.

  python tools/characters/showcase.py <out.gif> Actor:StateA,StateB[@scale] ...    (Hero: for the hero)
"""
import sys, os, tempfile
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
KEEP = Path(tempfile.gettempdir()) / "frames-keep"
SIZES = {"ZombieSoldier": (140, 176), "ShieldGuard": (168, 176), "BoneArcher": (150, 176), "GuardianOfTheTomb": (272, 300),
         "CorruptedHound": (200, 120), "ForestArcher": (150, 176), "RootShaman": (156, 178), "RoyalSpearman": (214, 178),
         "HollowWarden": (268, 260), "PossessedVillager": (214, 178), "Scarecrow": (214, 178), "Watchdog": (200, 120),
         "RoyalCrossbowman": (160, 176), "PalacePriest": (156, 178), "StoneGargoyle": (220, 186), "AshRaven": (140, 110),
         "Bellringer": (300, 300), "AshCommander": (320, 300), "OathBoundKing": (300, 312), "MarshCorpseEater": (200, 120),
         "PlagueDoctor": (156, 178), "MarshLeech": (140, 110), "PlagueStag": (300, 312), "Hero": (256, 224)}


def frames(actor, state):
    w, h = SIZES[actor]
    path = KEEP / (f"{state}.png" if actor == "Hero" else f"{actor}__{state}.png")
    row = Image.open(path).convert("RGBA")
    return [row.crop((i * w, 0, (i + 1) * w, h)) for i in range(row.width // w)]


def main():
    out = sys.argv[1]
    specs = []
    for arg in sys.argv[2:]:
        scale = 1.0
        if "@" in arg:
            arg, sc = arg.split("@"); scale = float(sc)
        actor, states = arg.split(":")
        seq = []
        for st in states.split(","):
            seq += frames(actor, st)
        specs.append((actor, [f.resize((round(f.width * scale), round(f.height * scale)), Image.LANCZOS) for f in seq]))
    W = sum(max(f.width for f in s) for _, s in specs) + 40
    H = max(max(f.height for f in s) for _, s in specs) + 70
    bg = Image.open(ROOT / "Assets/Game/Art/Levels/Tomb/Rooms/room_tomb_east_hall_mid.png").convert("RGB")
    s = max(W / bg.width, H / bg.height)
    bg = bg.resize((round(bg.width * s), round(bg.height * s))).crop((0, 0, W, H)).filter(ImageFilter.GaussianBlur(1.5))
    floor_y = H - 40
    d = ImageDraw.Draw(bg)
    d.rectangle((0, floor_y, W, H), fill=(14, 16, 20))
    d.line((0, floor_y, W, floor_y), fill=(120, 140, 160), width=2)
    n = max(len(s) for _, s in specs)
    out_frames = []
    for i in range(n * 2):
        fr = bg.copy().convert("RGBA")
        x = 20
        for actor, seq in specs:
            f = seq[i % len(seq)]
            cw = max(g.width for g in seq)
            fr.alpha_composite(f, (x + (cw - f.width) // 2, floor_y - f.height + 2))
            x += cw
        out_frames.append(fr.convert("RGB").quantize(colors=255, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.FLOYDSTEINBERG))
    out_frames[0].save(out, save_all=True, append_images=out_frames[1:], duration=42, loop=0, optimize=False)
    print("gif", out, len(out_frames), "frames", W, "x", H)


if __name__ == "__main__":
    main()
