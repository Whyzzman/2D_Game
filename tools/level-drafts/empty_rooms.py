#!/usr/bin/env python3
"""Rooms that play empty (2026-11-06, user: «level logic and locations»; 10-25: «in the rooms it is empty»): for every
room its size, how many enemies, finds and ledges it has, and the share of its air with nothing to stand on near it.
A big room with no enemy, no find and few ledges is listed.

  python tools/level-drafts/empty_rooms.py tomb
"""
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
FINDS = {"gold", "shard", "item", "urn", "altar", "breakable", "ring", "chest"}


def main():
    zone = sys.argv[1]
    lines = (ROOT / f"Assets/Game/Levels/{zone}.txt").read_text(encoding="utf-8").split("\n")
    i, rows = 0, []
    while i < len(lines):
        m = re.match(r"room (\S+) «([^»]*)»", lines[i])
        if not m:
            i += 1
            continue
        rid, name = m.group(1), m.group(2)
        g, keys, flags = i + 1, {}, ""
        while lines[g].strip() != "grid":
            if lines[g].startswith("flags "):
                flags = lines[g]
            km = re.match(r"key (\S) (\S+)(?: (\S+))?", lines[g])
            if km:
                keys[km.group(1)] = (km.group(2), km.group(3) or "")
            g += 1
        g += 1
        e = g
        while lines[e].strip() != "end":
            e += 1
        grid = lines[g:e]
        h, w = len(grid), len(grid[0]) if grid else 0
        air = sum(r.count(".") for r in grid)
        ledges = sum(r.count("=") for r in grid)
        enemies = finds = 0
        for r in grid:
            for ch in r:
                if ch in keys:
                    kind = keys[ch][0]
                    if kind == "enemy":
                        enemies += 1
                    elif kind in FINDS:
                        finds += 1
        size = w * h
        rows.append((rid, name, w, h, size, air, ledges, enemies, finds, "boss" in flags, "secret" in flags))
        i = e + 1
    print(f"{'room':38} {'w':>3}x{'h':<3} {'ledge':>5} {'enem':>4} {'find':>4}")
    for rid, name, w, h, size, air, ledges, enemies, finds, boss, secret in sorted(rows, key=lambda r: -r[4]):
        empty = size >= 600 and enemies == 0 and finds == 0 and not boss and not secret
        sparse = size >= 1400 and enemies <= 1 and ledges < 12 and not boss
        mark = "EMPTY" if empty else "sparse" if sparse else ""
        print(f"{rid:38} {w:>3}x{h:<3} {ledges:>5} {enemies:>4} {finds:>4}  {mark}")


if __name__ == "__main__":
    main()
