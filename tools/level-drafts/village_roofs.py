#!/usr/bin/env python3
"""Хати на вулицях Села: дахи-уступи, під якими painter малює фасад (terrain.js housesOf; 2026-11-13, решено Claude
за постійною згодою, gamedesign/LEVEL_LOOK_2.md). Ряди — згори. Потім — LevelCheck і BypassReport.
  .venv/bin/python tools/level-drafts/village_roofs.py
"""
from pathlib import Path
P = Path(__file__).resolve().parents[2] / "Assets/Game/Levels/village.txt"
ROOFS = {
    "room_village_lower_street": [(14, 24, 35)],  # другий дах (13, 44-55) давав дострибнути до пера без гака
    "room_village_upper_street": [(10, 14, 25), (11, 84, 93), (10, 106, 117)],
    "room_village_market": [(14, 26, 37), (13, 42, 51)],
    "room_village_hill": [(34, 14, 25)],
    "room_village_marsh_road": [(14, 22, 33)],
}
t = P.read_text(encoding="utf-8")
for room, roofs in ROOFS.items():
    a = t.index(f"room {room} ")
    g = t.index("grid\n", a) + 5
    e = t.index("\nend", g)
    rows = [list(r) for r in t[g:e].split("\n")]
    for y, x0, x1 in roofs:
        for x in range(x0, x1 + 1):
            assert rows[y][x] == ".", (room, y, x, rows[y][x])
            rows[y][x] = "="
    t = t[:g] + "\n".join("".join(r) for r in rows) + t[e:]
    print(room, roofs)
P.write_text(t, encoding="utf-8")
