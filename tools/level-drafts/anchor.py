#!/usr/bin/env python3
"""Прив'язує висячий уступ «=» до будови (2026-11-12, користувач: «елементи платформеру прості й не красиві»;
решено Claude за постійною згодою, gamedesign/LEVEL_LOOK_2.md). Ряди — згори, як у room.py / floating.py.
  .venv/bin/python tools/level-drafts/anchor.py <зона> <кімната> <ряд> <x0> <x1> floor|wall
floor — уступ стає кам'яною сходинкою до підлоги під ним; wall — уступ доростає до найближчої стіни свого ряду
(не далі 6 клітинок, не крізь речі). Потім — LevelCheck (ComfortReport), інакше відкотити.
"""
import sys
from pathlib import Path
zone, room, row, x0, x1, mode = sys.argv[1], sys.argv[2], *map(int, sys.argv[3:6]), sys.argv[6]
P = Path(__file__).resolve().parents[2] / f"Assets/Game/Levels/{zone}.txt"
t = P.read_text(encoding="utf-8")
a = t.index(f"room {room} ")
g = t.index("grid\n", a) + 5
e = t.index("\nend", g)
rows = [list(r) for r in t[g:e].split("\n")]
H, W = len(rows), len(rows[0])
assert all(rows[row][x] == "=" for x in range(x0, x1 + 1)), "".join(rows[row])
if mode == "floor":
    for x in range(x0, x1 + 1):                     # кожна колонка — до свого каменю
        y = row
        while y < H and rows[y][x] in "=.":
            rows[y][x] = "#"; y += 1
        assert y < H and rows[y][x] == "#", f"column {x}: no stone under, hits {rows[y][x] if y < H else 'edge'}"
elif mode == "wall":
    left = next((d for d in range(1, 7) if x0 - d >= 0 and rows[row][x0 - d] == "#"), None)
    right = next((d for d in range(1, 7) if x1 + d < W and rows[row][x1 + d] == "#"), None)
    if left is None and right is None: sys.exit("no wall within 6")
    span = range(x0 - left + 1, x0) if right is None or (left is not None and left <= right) else range(x1 + 1, x1 + right)
    assert all(rows[row][x] == "." for x in span), "something in the way"
    for x in span: rows[row][x] = "="
else:
    sys.exit("floor|wall")
P.write_text(t[:g] + "\n".join("".join(r) for r in rows) + t[e:], encoding="utf-8")
print(f"{room} {row}:{x0}-{x1} {mode}")
