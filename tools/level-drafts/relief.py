#!/usr/bin/env python3
"""Gives flat box rooms a floor with relief: mounds of stone up to three cells high (steps of one or two
cells, the hero walks them without jumping), and here and there a puddle (`^`) three to five cells wide
to jump over. Every room gets its own seed, so no two floors are alike.

  python tools/level-drafts/relief.py marsh room_a room_b ...   -> rewrites Levels/marsh.txt

The floor stays flat by the side walls (doors) and by holes in the floor; a mound never comes closer than
NEED cells of air under a ledge or the ceiling. Things standing on the floor move up onto the mound.
Then run LevelCheck.
"""
import random
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
NEED = 5          # cells of air kept over the raised floor
MARGIN = 7        # flat cells by each side wall
MAX_H = 3


def heights(rid, grid, floor):
    w = len(grid[0])
    rnd = random.Random(rid)
    h = [0.0] * w
    span = w - 2 * MARGIN
    hills = max(1, min(3, span // 22))
    for n in range(hills):
        centre = MARGIN + span * (n + rnd.uniform(0.3, 0.7)) / hills
        top = rnd.randint(1, MAX_H)
        flat = rnd.uniform(2, 6)               # half-width of the crest
        for k in range(MARGIN, w - MARGIN):
            d = abs(k - centre) - flat
            h[k] = max(h[k], top - max(0.0, d) / 3.6)   # one cell down every three or four columns: a rolling bank, not a stair
    h = [max(0, int(round(v))) for v in h]
    # flat beside holes in the floor and where the floor row is not stone
    for k in range(w):
        if any(grid[floor][j] != '#' for j in range(max(0, k - 3), min(w, k + 4))):
            h[k] = 0
    # keep air over the mound
    for k in range(w):
        while h[k] > 0:
            top = floor - h[k]
            air = 0
            r = top
            while r >= 0 and grid[r][k] not in "#=%":
                air += 1
                r -= 1
            if air >= NEED: break
            h[k] -= 1
    # no cliff taller than two cells between neighbours
    for _ in range(3):
        for k in range(1, w):
            if h[k] - h[k - 1] > 2: h[k] = h[k - 1] + 2
            if h[k - 1] - h[k] > 2: h[k - 1] = h[k] + 2
    # no stubs: a step narrower than four columns is knocked down to the one below it
    for level in range(MAX_H, 0, -1):
        k = 0
        while k < w:
            if h[k] >= level:
                e = k
                while e < w and h[e] >= level: e += 1
                if e - k < 4:
                    for j in range(k, e): h[j] = level - 1
                k = e
            else:
                k += 1
    return h, rnd


def puddle(grid, floor, h, rnd, keys):
    w = len(grid[0])
    flat = [k for k in range(MARGIN + 6, w - MARGIN - 8)
            if all(h[j] == 0 and grid[floor][j] == '#' and grid[floor - 1][j] == '.' for j in range(k - 2, k + 7))]
    if not flat or rnd.random() < 0.35: return
    k = rnd.choice(flat)
    for j in range(k, k + rnd.randint(3, 5)):
        grid[floor][j] = '^'


def relieve(rid, grid, keys):
    floor = len(grid) - 2
    h, rnd = heights(rid, grid, floor)
    for k, hk in enumerate(h):
        if hk == 0: continue
        stand = grid[floor - 1][k]
        for r in range(floor - hk, floor):
            grid[r][k] = '#'
        if stand != '.':
            grid[floor - 1 - hk][k] = stand
    puddle(grid, floor, h, rnd, keys)
    return h


def main():
    zone, wanted = sys.argv[1], set(sys.argv[2:])
    path = ROOT / "Assets/Game/Levels" / f"{zone}.txt"
    lines = path.read_text(encoding="utf-8").split("\n")
    out, i = [], 0
    while i < len(lines):
        m = re.match(r"room (\S+) ", lines[i])
        if not m or m.group(1) not in wanted:
            out.append(lines[i]); i += 1; continue
        rid = m.group(1)
        while lines[i] != "grid":
            out.append(lines[i]); i += 1
        out.append(lines[i]); i += 1
        start = i
        while lines[i] != "end": i += 1
        grid = [list(row) for row in lines[start:i]]
        h = relieve(rid, grid, None)
        out += [''.join(row) for row in grid]
        print(f"{rid}: highest {max(h)}, raised {sum(1 for v in h if v)} columns")
    path.write_text("\n".join(out), encoding="utf-8", newline="\n")


if __name__ == "__main__":
    main()
