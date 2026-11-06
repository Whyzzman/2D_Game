#!/usr/bin/env python3
"""Things standing on nothing (2026-11-05, user: «bugs everywhere»): a shard, gold, an urn, an altar, an item, a
breakable or a walking enemy whose cell has no stone or ledge right under it — left in the air when a ledge it
stood on was tidied away. Rings, spots and flying enemies are meant to hang.

  python tools/level-drafts/floating_things.py tomb [--fix]

--fix gives each find back the shelf it stood on: a ledge of five cells under it (only over air, never on the
room's edge); a walking enemy is dropped straight down onto the floor under it instead.
"""
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
FLOOR = "#%="
FLIERS = {"AshRaven", "MarshLeech"}
HANGS = {"ring", "spot", "start"}


def main():
    zone, fix = sys.argv[1], "--fix" in sys.argv
    path = ROOT / f"Assets/Game/Levels/{zone}.txt"
    lines = path.read_text(encoding="utf-8").split("\n")
    i, found = 0, []
    while i < len(lines):
        m = re.match(r"room (\S+) ", lines[i])
        if not m:
            i += 1
            continue
        room, keys = m.group(1), {}
        g = i + 1
        while lines[g].strip() != "grid":
            km = re.match(r"key (\S) (\S+)(?: (\S+))?", lines[g])
            if km:
                keys[km.group(1)] = (km.group(2), km.group(3) or "")
            g += 1
        g += 1
        e = g
        while lines[e].strip() != "end":
            e += 1
        grid = [list(r) for r in lines[g:e]]
        for y, row in enumerate(grid):
            for x, ch in enumerate(row):
                if ch not in keys:
                    continue
                kind, arg = keys[ch]
                if kind in HANGS or (kind == "enemy" and arg in FLIERS):
                    continue
                below = grid[y + 1][x] if y + 1 < len(grid) else "#"
                if below in FLOOR:
                    continue
                drop = next((yy for yy in range(y + 1, len(grid)) if grid[yy][x] in FLOOR + "^"), None)
                found.append((room, ch, kind, arg, y, x, drop))
                if not fix:
                    continue
                if kind == "enemy":
                    if drop is not None and grid[drop][x] in FLOOR:
                        grid[y][x] = "."
                        grid[drop - 1][x] = ch
                elif y + 1 < len(grid) - 1:
                    for xx in range(max(1, x - 2), min(len(row) - 1, x + 3)):
                        if grid[y + 1][xx] == ".":
                            grid[y + 1][xx] = "="
        lines[g:e] = ["".join(r) for r in grid]
        i = e + 1
    for room, ch, kind, arg, y, x, drop in found:
        print(f"{room} | {ch} {kind} {arg} at row {y} col {x} | floor at row {drop}")
    print("floating", len(found))
    if fix:
        path.write_text("\n".join(lines), encoding="utf-8")


if __name__ == "__main__":
    main()
