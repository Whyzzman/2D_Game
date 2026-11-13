#!/usr/bin/env python3
"""Майдан Села як майдан: хати довкола, дахами яких ходять (2026-11-13, решено Claude за постійною згодою,
gamedesign/LEVEL_LOOK_2.md). Кам'яні брили, що висіли в небі, стають дахами високих хат (уступ «=» — під ним painter
малює фасад до землі, terrain.js housesOf); між ними — два нижчі дахи. Потім — LevelCheck і BypassReport.
  .venv/bin/python tools/level-drafts/village_square.py
"""
from pathlib import Path
P = Path(__file__).resolve().parents[2] / "Assets/Game/Levels/village.txt"
t = P.read_text(encoding="utf-8")
a = t.index("room room_village_square ")
g = t.index("grid\n", a) + 5
e = t.index("\nend", g)
rows = [list(r) for r in t[g:e].split("\n")]
assert "".join(rows[20][53:58]) == "#####" and "".join(rows[20][84:90]) == "######"
# брили в небі геть
for y in (20, 21, 22):
    for x in list(range(53, 58)) + list(range(84, 90)) + list(range(98, 102)):
        if rows[y][x] == "#": rows[y][x] = "."
def roof(y, x0, x1):
    for x in range(x0, x1 + 1):
        assert rows[y][x] == ".", (y, x, rows[y][x])
        rows[y][x] = "="
roof(20, 51, 60)     # висока хата біля колодязя
roof(20, 82, 92)     # висока хата на сході
roof(31, 20, 33)     # низька хата біля драбини (3 над землею)
roof(32, 64, 76)     # низька хата за колодязем (2,5 над землею)
roof(26, 94, 105)    # середня хата на сході (5,5 над землею): сходинка до високої
P.write_text(t[:g] + "\n".join("".join(r) for r in rows) + t[e:], encoding="utf-8")
print("\n".join("".join(r) for r in rows[18:38]))
