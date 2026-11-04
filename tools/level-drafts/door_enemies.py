#!/usr/bin/env python3
"""Moves enemies standing within 7 cells of an opening further into their room, along their own floor
(audit.py: enemy-at-door — the player is hit before the camera has settled into the room).

  python tools/level-drafts/door_enemies.py tomb [room_id ...]   -> every room, or only those

An enemy goes the other way from the nearest opening, cell by cell while there is floor under it, to the first
cell more than 7 from every opening. Then run LevelCheck.
"""
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
STAND = "#%="


def main():
    zone, only = sys.argv[1], set(sys.argv[2:])
    path = ROOT / "Assets/Game/Levels" / f"{zone}.txt"
    lines = path.read_text(encoding="utf-8").split("\n")
    i = 0
    while i < len(lines):
        m = re.match(r"room (\S+) ", lines[i])
        if not m:
            i += 1
            continue
        rid, keys, g = m.group(1), {}, i + 1
        while lines[g].strip() != "grid":
            k = re.match(r"key (\S) (\S+)", lines[g])
            if k:
                keys[k.group(1)] = k.group(2)
            g += 1
        g += 1
        e = next(j for j in range(g, len(lines)) if lines[j].strip() == "end")
        if only and rid not in only:
            i = e
            continue
        rows = [list(r) for r in lines[g:e]]
        h, w = len(rows), len(rows[0])
        air = lambda c: c not in STAND
        doors = [(y, x) for y in range(h) for x in range(w) if (y in (0, h - 1) or x in (0, w - 1)) and air(rows[y][x])]
        clear = lambda y, x: all(abs(o[0] - y) > 7 or abs(o[1] - x) > 7 for o in doors)
        for y in range(h):
            for x in range(w):
                c = rows[y][x]
                if keys.get(c) != "enemy" or clear(y, x):
                    continue
                near = min(doors, key=lambda o: abs(o[0] - y) + abs(o[1] - x))
                for step in ((1, -1) if near[1] <= x else (-1, 1)):
                    to = None
                    for d in range(1, w):
                        nx = x + step * d
                        if not (1 <= nx < w - 1) or not air(rows[y][nx]) or y + 1 >= h or rows[y + 1][nx] not in STAND:
                            break
                        if rows[y][nx] == "." and clear(y, nx):
                            to = nx
                            break
                    if to is not None:
                        rows[y][x], rows[y][to] = ".", c
                        print(rid, "moved", c, f"{y}:{x}->{to}")
                        break
                else:
                    print(rid, "could not move", c, f"{y}:{x}")
        lines[g:e] = ["".join(r) for r in rows]
        i = e
    path.write_text("\n".join(lines), encoding="utf-8")


if __name__ == "__main__":
    main()
