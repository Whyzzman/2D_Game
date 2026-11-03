#!/usr/bin/env python3
"""Lists the one-way ledges of a zone that touch nothing: no stone at either end, none right above.

  python tools/level-drafts/floating.py forest

Prints per room: ledges, how many float, and each floating one as row:x0-x1 (rows top-down, as room.py numbers them).
"""
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SOLID = "#%"


def rooms(lines):
    i = 0
    while i < len(lines):
        m = re.match(r"room (\S+) ", lines[i])
        if not m:
            i += 1
            continue
        g = next(j for j in range(i, len(lines)) if lines[j].strip() == "grid") + 1
        e = next(j for j in range(g, len(lines)) if lines[j].strip() == "end")
        yield m.group(1), lines[g:e]
        i = e


def segments(rows):
    for y, r in enumerate(rows):
        for m in re.finditer(r"=+", r):
            yield y, m.start(), m.end() - 1


def floats(rows, y, x0, x1):
    w = len(rows[0])
    at = lambda yy, xx: 0 <= yy < len(rows) and 0 <= xx < w and rows[yy][xx] in SOLID
    if at(y, x0 - 1) or at(y, x1 + 1):
        return False
    return not any(at(y - 1, x) for x in range(x0, x1 + 1))


def main():
    zone = sys.argv[1]
    lines = (ROOT / "Assets/Game/Levels" / f"{zone}.txt").read_text(encoding="utf-8").split("\n")
    total = 0
    for rid, rows in rooms(lines):
        segs = list(segments(rows))
        fl = [s for s in segs if floats(rows, *s)]
        total += len(fl)
        if fl:
            print(f"{rid}: {len(segs)} ledges, {len(fl)} float: " + " ".join(f"{y}:{a}-{b}" for y, a, b in fl))
    print("floating total", total)


if __name__ == "__main__":
    main()
