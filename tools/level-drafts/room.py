#!/usr/bin/env python3
"""Prints one room of a zone's text with column and row numbers, or replaces its grid.

  python tools/level-drafts/room.py tomb room_tomb_entrance            -> the grid, numbered
  python tools/level-drafts/room.py tomb room_tomb_entrance new.txt    -> grid replaced from new.txt

A replacement must keep the size and every cell on the room's edge (the openings are its links to
the neighbours); the script refuses it otherwise. Then run LevelCheck (gamedesign/LEVEL_TOOL.md).
"""
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def find(lines, room_id):
    start = next(i for i, l in enumerate(lines) if l.startswith(f"room {room_id} "))
    grid = next(i for i in range(start, len(lines)) if lines[i].strip() == "grid") + 1
    end = next(i for i in range(grid, len(lines)) if lines[i].strip() == "end")
    return grid, end


def edge(rows):
    h, w = len(rows), len(rows[0])
    return [(y, x, rows[y][x]) for y in range(h) for x in range(w) if y in (0, h - 1) or x in (0, w - 1)]


def main():
    zone, room = sys.argv[1], sys.argv[2]
    path = ROOT / "Assets/Game/Levels" / f"{zone}.txt"
    lines = path.read_text(encoding="utf-8").split("\n")
    grid, end = find(lines, room)
    rows = lines[grid:end]
    if len(sys.argv) < 4:
        w = len(rows[0])
        print("     " + "".join(str(x // 10) for x in range(w)))
        print("     " + "".join(str(x % 10) for x in range(w)))
        for y, r in enumerate(rows):
            print(f"{y:3}  {r}")
        return
    new = [r.rstrip("\r") for r in Path(sys.argv[3]).read_text(encoding="utf-8").split("\n") if r.strip()]
    if len(new) != len(rows) or any(len(r) != len(rows[0]) for r in new):
        sys.exit(f"size differs: {len(new)}x{len(new[0])} vs {len(rows)}x{len(rows[0])}")
    old_edge, new_edge = edge(rows), edge(new)
    bad = [(y, x, a, b[2]) for (y, x, a), b in zip(old_edge, new_edge) if a != b[2]]
    if bad:
        sys.exit("edge changed (links!): " + ", ".join(f"r{y}c{x} {a}->{b}" for y, x, a, b in bad[:10]))
    lines[grid:end] = new
    path.write_text("\n".join(lines), encoding="utf-8", newline="\n")
    print(f"{room}: grid replaced")


if __name__ == "__main__":
    main()
