#!/usr/bin/env python3
"""A plain map of a zone's text, for reading its logic without Unity (2026-11-06): stone, ledges, spikes, every
room's frame and name, and its things — enemies red, finds gold, shards cyan, rings white, altars green, spots
violet, the start lime.

  python tools/level-drafts/draw_zone.py <zone> [out.png] [px per cell]
"""
import re
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
CELL = {"#": (70, 72, 82), "%": (88, 100, 120), "=": (150, 110, 60), "^": (200, 50, 50), ".": (22, 24, 32)}
KIND = {"enemy": (255, 70, 70), "gold": (255, 210, 60), "item": (255, 160, 40), "shard": (80, 230, 255),
        "ring": (255, 255, 255), "altar": (80, 255, 120), "spot": (190, 120, 255), "start": (180, 255, 60),
        "urn": (200, 170, 110), "breakable": (255, 120, 200), "reward": (255, 255, 120)}


def rooms(text):
    lines = text.split("\n")
    out, i = [], 0
    while i < len(lines):
        m = re.match(r"room (\S+) «([^»]*)» at (-?\d+) (-?\d+)", lines[i])
        if not m:
            i += 1
            continue
        g, keys, flags = i + 1, {}, ""
        while lines[g].strip() != "grid":
            if lines[g].startswith("flags "):
                flags = lines[g][6:]
            km = re.match(r"key (\S) (\S+)(?: (.*))?", lines[g])
            if km:
                keys[km.group(1)] = (km.group(2), km.group(3) or "")
            g += 1
        g += 1
        e = g
        while lines[e].strip() != "end":
            e += 1
        out.append(dict(id=m.group(1), name=m.group(2), x=int(m.group(3)), y=int(m.group(4)), grid=lines[g:e],
                        keys=keys, flags=flags))
        i = e + 1
    return out


def main():
    zone = sys.argv[1]
    out = Path(sys.argv[2]) if len(sys.argv) > 2 else ROOT / f"Artifacts/LevelPreviews/{zone}_plan.png"
    s = int(sys.argv[3]) if len(sys.argv) > 3 else 3
    rs = rooms((ROOT / f"Assets/Game/Levels/{zone}.txt").read_text(encoding="utf-8"))
    x0 = min(r["x"] for r in rs)
    y0 = min(r["y"] for r in rs)
    x1 = max(r["x"] + len(r["grid"][0]) for r in rs)
    y1 = max(r["y"] + len(r["grid"]) for r in rs)
    W, H = (x1 - x0) * s, (y1 - y0) * s
    im = Image.new("RGB", (W, H), (8, 8, 12))
    d = ImageDraw.Draw(im)
    font = ImageFont.load_default()
    for r in rs:
        h = len(r["grid"])
        for row, line in enumerate(r["grid"]):
            for col, ch in enumerate(line):
                cx = (r["x"] + col - x0) * s
                cy = (y1 - (r["y"] + h - 1 - row) - 1) * s
                color = CELL.get(ch)
                if color is None:
                    color = CELL["."]
                d.rectangle([cx, cy, cx + s - 1, cy + s - 1], fill=color)
        left, top = (r["x"] - x0) * s, (y1 - r["y"] - h) * s
        right, bottom = left + len(r["grid"][0]) * s - 1, top + h * s - 1
        tag = (120, 200, 255) if "secret" in r["flags"] else (255, 200, 120) if "optional" in r["flags"] else (90, 90, 110)
        d.rectangle([left, top, right, bottom], outline=tag)
        for row, line in enumerate(r["grid"]):
            for col, ch in enumerate(line):
                if ch in r["keys"]:
                    kind = r["keys"][ch][0]
                    cx = (r["x"] + col - x0) * s + s // 2
                    cy = (y1 - (r["y"] + h - 1 - row) - 1) * s + s // 2
                    rad = max(2, s)
                    d.ellipse([cx - rad, cy - rad, cx + rad, cy + rad], fill=KIND.get(kind, (255, 0, 255)))
        label = r["id"].split("_", 2)[-1]
        d.text((left + 3, top + 2), label, fill=(230, 230, 240), font=font)
    out.parent.mkdir(parents=True, exist_ok=True)
    im.save(out)
    print(out, im.size)


if __name__ == "__main__":
    main()
