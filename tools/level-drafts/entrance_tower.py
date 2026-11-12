#!/usr/bin/env python3
"""Вхід до Гробниці: правий підйом — стовп із кладки на правому блоці й консоль від нього, замість двох висячих
плит (2026-11-12, решено Claude за постійною згодою, gamedesign/LEVEL_LOOK_2.md). Потім — LevelCheck.
  .venv/bin/python tools/level-drafts/entrance_tower.py
"""
from pathlib import Path
P = Path(__file__).resolve().parents[2] / "Assets/Game/Levels/tomb.txt"
t = P.read_text(encoding="utf-8")
a = t.index("room room_tomb_entrance ")
g = t.index("grid\n", a) + 5
e = t.index("\nend", g)
rows = [list(r) for r in t[g:e].split("\n")]
assert "".join(rows[15][54:58]) == "====" and "".join(rows[12][49:53]) == "===="
for x in range(54, 58): rows[15][x] = "."
for x in range(49, 53): rows[12][x] = "."
def fill(x0, x1, y0, y1):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            rows[y][x] = "#"
fill(55, 57, 15, 17)    # стовп на правому блоці (стоять на ряду 14)
fill(55, 55, 12, 14)    # він же вгору
fill(50, 55, 12, 12)    # консоль від стовпа ліворуч (стоять на ряду 11)
P.write_text(t[:g] + "\n".join("".join(r) for r in rows) + t[e:], encoding="utf-8")
print("\n".join("".join(r) for r in rows[7:21]))
