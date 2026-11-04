#!/usr/bin/env python3
"""Trims one-way ledges the hero does not fit on: under 4 rows (2 units) of air below the stone over them
(the hero is 1.6 tall). Things standing there move along the ledge to the nearest cell he fits on.

  python tools/level-drafts/headroom.py tomb       -> rewrites Levels/tomb.txt, prints what changed

Only `=` cells change, never the room's edge. A top opening counts as air (the ledge under an exit is fine).
Then run LevelCheck.
"""
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SOLID = "#%"
NEED = 4


def main():
    zone = sys.argv[1]
    path = ROOT / "Assets/Game/Levels" / f"{zone}.txt"
    lines = path.read_text(encoding="utf-8").split("\n")
    i = 0
    while i < len(lines):
        m = re.match(r"room (\S+) ", lines[i])
        if not m:
            i += 1
            continue
        rid = m.group(1)
        keys = {}
        g = i + 1
        while lines[g].strip() != "grid":
            k = re.match(r"key (\S) ", lines[g])
            if k:
                keys[k.group(1)] = True
            g += 1
        g += 1
        e = next(j for j in range(g, len(lines)) if lines[j].strip() == "end")
        rows = [list(r) for r in lines[g:e]]
        h, w = len(rows), len(rows[0])

        def headroom(y, x):
            k = 1
            while y - k >= 0 and rows[y - k][x] not in SOLID:
                k += 1
            return NEED if y - k < 0 and rows[0][x] not in SOLID else k - 1

        trimmed = []
        for y in range(1, h - 1):
            for x in range(1, w - 1):
                if rows[y][x] == "=" and headroom(y, x) < NEED:
                    rows[y][x] = "."
                    trimmed.append((y, x))
        moved = []
        for y, x in trimmed:
            above = rows[y - 1][x]
            if above in keys:
                spots = [xx for xx in range(w) if rows[y][xx] == "=" and rows[y - 1][xx] == "."]
                if spots:
                    to = min(spots, key=lambda xx: abs(xx - x))
                    rows[y - 1][x], rows[y - 1][to] = ".", above
                    moved.append(f"{above} {y - 1}:{x}->{to}")
        if trimmed:
            lines[g:e] = ["".join(r) for r in rows]
            print(rid, "trimmed", " ".join(f"{y}:{x}" for y, x in trimmed), "| moved", " ".join(moved))
        i = e
    path.write_text("\n".join(lines), encoding="utf-8")


if __name__ == "__main__":
    main()
