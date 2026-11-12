#!/usr/bin/env python3
"""Східна зала: дві висячі плити ліворуч стають кам'яними сходинками на підлозі (2026-11-12, решено Claude за
постійною згодою, gamedesign/LEVEL_LOOK_2.md). Потім — LevelCheck.
  .venv/bin/python tools/level-drafts/east_hall_steps.py
"""
from pathlib import Path
P = Path(__file__).resolve().parents[2] / "Assets/Game/Levels/tomb.txt"
t = P.read_text(encoding="utf-8")
a = t.index("room room_tomb_east_hall ")
g = t.index("grid\n", a) + 5
e = t.index("\nend", g)
rows = [list(r) for r in t[g:e].split("\n")]
assert "".join(rows[21][6:10]) == "====" and "".join(rows[18][11:15]) == "===="
for y in range(21, 24):
    for x in range(6, 10): rows[y][x] = "#"       # нижня сходинка (стоять на ряду 20)
for y in range(18, 24):
    for x in range(11, 15): rows[y][x] = "#"      # верхня (стоять на ряду 17)
for y in range(21, 24): rows[y][10] = "#"         # між ними — без щілини
P.write_text(t[:g] + "\n".join("".join(r) for r in rows) + t[e:], encoding="utf-8")
print("\n".join("".join(r) for r in rows[15:25]))
