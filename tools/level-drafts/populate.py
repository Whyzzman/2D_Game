#!/usr/bin/env python3
"""Things put into rooms that played empty (2026-11-06, user: «level logic and locations»; decided by Claude under
standing approval). Each entry names the room and what goes where (row and column of the room's grid, top-down):

  ("enemy", Prefab, row, col)        a foe standing on the floor (or in the air, for a raven)
  ("gold", "amount id", row, col)    a purse on the floor or a ledge
  ("ledge", row, col_from, col_to)   one-way ledge cells

Checks before writing: the cell is air; a foe that walks and a purse stand on something. Rooms with people, an
altar to rest at or the zone's arrival stay quiet. Then LevelCheck (all zones must pass) and Scene<Zone>.Apply.

  python tools/level-drafts/populate.py [--fix]
"""
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
FLYERS = {"AshRaven"}

PLAN = {
    "forest": {
        # the bridge before the Warden: an archer on the railing over the deck, a purse a jump above him
        "room_forest_warden_bridge": [("ledge", 11, 30, 38), ("enemy", "ForestArcher", 10, 34),
                                      ("ledge", 8, 42, 47), ("gold", "60 forest-warden-bridge-purse", 7, 45)],
    },
    "village": {
        # the road out to the marsh: a dog and a villager on the road, a cart to climb with a purse on it
        "room_village_marsh_road": [("enemy", "Watchdog", 19, 16), ("enemy", "PossessedVillager", 19, 30),
                                    ("ledge", 17, 7, 12), ("gold", "45 village-marsh-road-purse", 16, 9)],
        # eighty metres of street held one dog
        "room_village_upper_street": [("enemy", "PossessedVillager", 15, 95), ("enemy", "Scarecrow", 15, 140)],
        # a raven circling the bell shaft
        "room_village_bell_shaft": [("enemy", "AshRaven", 20, 18)],
    },
    "capital": {
        "room_capital_ash_road": [("enemy", "RoyalSpearman", 23, 18)],
        # the long climb: two ravens on the way up
        "room_capital_bell_tower": [("enemy", "AshRaven", 44, 8), ("enemy", "AshRaven", 92, 8)],
    },
    "marsh": {
        # the drain over the spikes guards the hold's cache
        "room_marsh_toxic_drain": [("enemy", "MarshLeech", 23, 36)],
    },
}


def main():
    fix = "--fix" in sys.argv
    bad = 0
    for zone, rooms in PLAN.items():
        path = ROOT / f"Assets/Game/Levels/{zone}.txt"
        lines = path.read_text(encoding="utf-8").split("\n")
        for rid, things in rooms.items():
            at = next(i for i, l in enumerate(lines) if l.startswith(f"room {rid} "))
            g = at + 1
            while lines[g].strip() != "grid":
                g += 1
            used = {l.split()[1] for l in lines[at + 1:g] if l.startswith("key ")}
            e = g + 1
            while lines[e].strip() != "end":
                e += 1
            grid = [list(r) for r in lines[g + 1:e]]
            used |= {ch for r in grid for ch in r}
            keys = []
            for t in things:
                if t[0] == "ledge":
                    _, row, c0, c1 = t
                    for c in range(c0, c1 + 1):
                        if grid[row][c] != ".":
                            print(f"{zone} {rid}: ledge cell {row},{c} is '{grid[row][c]}'")
                            bad += 1
                        grid[row][c] = "="
                    continue
                kind, arg, row, col = t
                if grid[row][col] != ".":
                    print(f"{zone} {rid}: {kind} cell {row},{col} is '{grid[row][col]}'")
                    bad += 1
                    continue
                stands = row + 1 < len(grid) and grid[row + 1][col] in "#%="
                if not stands and not (kind == "enemy" and arg in FLYERS):
                    print(f"{zone} {rid}: {kind} {arg} at {row},{col} stands on nothing")
                    bad += 1
                letter = next(c for c in "abcdefghijklmnopqrstuvwxyz" if c not in used)
                used.add(letter)
                grid[row][col] = letter
                keys.append(f"key {letter} {kind} {arg}")
                print(f"{zone} {rid}: {letter} = {kind} {arg} at {row},{col}")
            lines[g:e] = keys + ["grid"] + ["".join(r) for r in grid]
        if fix and not bad:
            path.write_text("\n".join(lines), encoding="utf-8")
    print("problems:", bad, "(written)" if fix and not bad else "")


if __name__ == "__main__":
    main()
