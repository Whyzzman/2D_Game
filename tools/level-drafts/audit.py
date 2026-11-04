#!/usr/bin/env python3
"""Finds the places of a zone that play badly, the way a player meets them (2026-11-04, user: «on the first
level there are still many bad places — I walk from one room into another and spikes meet me at once»).

  python tools/level-drafts/audit.py tomb

Per room, rows top-down as room.py numbers them:
  spikes-at-door  spikes within 6 cells (3 units) of an opening — the first thing a player meets
  enemy-at-door   an enemy within 6 cells of an opening — hit before the camera has settled
  spikes-by-enemy spikes within 6 cells of an enemy — a shove or a chase into them
  drop-at-door    walking in through a side opening, no floor under it for more than 8 cells (4 units)
  low-ceiling     a floor run of 4+ cells with stone less than 5 cells over it — the head bumps every jump
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
        keys = {}
        g = i + 1
        while lines[g].strip() != "grid":
            k = re.match(r"key (\S) (\S+)", lines[g])
            if k:
                keys[k.group(1)] = k.group(2)
            g += 1
        e = next(j for j in range(g + 1, len(lines)) if lines[j].strip() == "end")
        yield m.group(1), lines[g + 1:e], keys
        i = e


def audit(rid, rows, keys):
    h, w = len(rows), len(rows[0])
    cell = lambda y, x: rows[y][x] if 0 <= y < h and 0 <= x < w else "#"
    air = lambda c: c not in SOLID and c != "="
    openings = [(y, x) for y in range(h) for x in range(w) if (y in (0, h - 1) or x in (0, w - 1)) and air(cell(y, x))]
    spikes = [(y, x) for y in range(h) for x in range(w) if cell(y, x) == "^"]
    enemies = [(y, x) for y in range(h) for x in range(w) if keys.get(cell(y, x)) in ("enemy", "boss")]
    near = lambda a, b, d: abs(a[0] - b[0]) <= d and abs(a[1] - b[1]) <= d
    out = []
    for s in spikes:
        if any(near(s, o, 6) for o in openings):
            out.append(f"spikes-at-door {s[0]}:{s[1]}")
            break
    for e in enemies:
        if any(near(e, o, 6) for o in openings):
            out.append(f"enemy-at-door {e[0]}:{e[1]}")
    for s in spikes:
        hit = [e for e in enemies if near(s, e, 6)]
        if hit:
            out.append(f"spikes-by-enemy {s[0]}:{s[1]}")
            break
    for y, x in openings:
        # the bottom cell of a side opening: walking in, how far is it down to something to stand on?
        if x not in (0, w - 1) or (y + 1 < h and air(cell(y + 1, x))):
            continue
        inward = 1 if x == 0 else w - 2
        depth = 0
        while y + depth + 1 < h and air(cell(y + depth + 1, inward)):
            depth += 1
        if depth > 8:
            out.append(f"drop-at-door {y}:{x} falls {depth}")
    for y in range(1, h):
        run = 0
        for x in range(w + 1):
            stand = x < w and cell(y, x) in SOLID + "=" and air(cell(y - 1, x))
            if stand:
                run += 1
                continue
            if run >= 4 and y > 2:  # a ledge in the top exit itself is under open air
                xs = range(x - run, x)
                # a one-way ledge over the head is no ceiling: the jump goes through it
                clear = min(next((k for k in range(1, 12) if cell(y - k, xx) in SOLID), 12) for xx in xs)
                if clear < 5:
                    out.append(f"low-ceiling {y}:{x - run}-{x - 1} clear {clear - 1}")
            run = 0
    return out


def main():
    zone = sys.argv[1]
    lines = (ROOT / "Assets/Game/Levels" / f"{zone}.txt").read_text(encoding="utf-8").split("\n")
    total = 0
    for rid, rows, keys in rooms(lines):
        found = audit(rid, rows, keys)
        if found:
            total += len(found)
            print(rid, "|", "; ".join(found))
    print("findings", total)


if __name__ == "__main__":
    main()
