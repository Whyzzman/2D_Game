#!/usr/bin/env python3
"""Ledges LevelTidy took away as dead ends (2026-10-27) that were not dead ends (2026-11-05, user: «bugs everywhere,
bad parkour»): the ledge under the walled cell's door, a trial's perches, a shelf by a ring. A removed run of `=` is
given back when it served something — a door in the room's edge, a breakable wall, a find, a ring or a spot (a trial's
mark) within a few cells, or any ledge of an optional or secret room (a trial, a hiding place: the way to the prize is
the room) — and only where the cell is still air now.

  python tools/level-drafts/untidy.py <zone> <tidy-commit> [--fix]

Then LevelCheck; every zone must still pass (a restored ledge that opens a gate is taken out again by hand).
"""
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
NEAR = 4


def rooms(text):
    lines = text.split("\n")
    out, i = {}, 0
    while i < len(lines):
        m = re.match(r"room (\S+) ", lines[i])
        if not m:
            i += 1
            continue
        g = i + 1
        keys = {}
        while lines[g].strip() != "grid":
            km = re.match(r"key (\S) (\S+)", lines[g])
            if km:
                keys[km.group(1)] = km.group(2)
            g += 1
        g += 1
        e = g
        while lines[e].strip() != "end":
            e += 1
        flags = next((lines[k] for k in range(i + 1, g) if lines[k].startswith("flags ")), "")
        out[m.group(1)] = (g, e, keys, flags)
        i = e + 1
    return lines, out


def runs(row_old, row_new):
    out, x = [], 0
    while x < len(row_old):
        if row_old[x] == "=" and x < len(row_new) and row_new[x] == ".":
            s = x
            while x < len(row_old) and row_old[x] == "=" and row_new[x] == ".":
                x += 1
            out.append((s, x))
        else:
            x += 1
    return out


def main():
    zone, commit, fix = sys.argv[1], sys.argv[2], "--fix" in sys.argv
    path = ROOT / f"Assets/Game/Levels/{zone}.txt"
    before = subprocess.run(["git", "show", f"{commit}^:Assets/Game/Levels/{zone}.txt"], cwd=ROOT,
                            capture_output=True, text=True, encoding="utf-8").stdout
    after = subprocess.run(["git", "show", f"{commit}:Assets/Game/Levels/{zone}.txt"], cwd=ROOT,
                           capture_output=True, text=True, encoding="utf-8").stdout
    now = path.read_text(encoding="utf-8")
    _, old_rooms = rooms(before)
    old_lines = before.split("\n")
    _, tidy_rooms = rooms(after)
    tidy_lines = after.split("\n")
    lines, cur_rooms = rooms(now)
    restored = 0
    for rid, (og, oe, _, _) in old_rooms.items():
        if rid not in tidy_rooms or rid not in cur_rooms:
            continue
        tg, te, _, _ = tidy_rooms[rid]
        cg, ce, keys, flags = cur_rooms[rid]
        # a trial or a hiding place is all its ledges: the way to the prize is the room (the Thorn Path's perches)
        side = "optional" in flags or "secret" in flags
        old = old_lines[og:oe]
        tidy = tidy_lines[tg:te]
        cur = [list(r) for r in lines[cg:ce]]
        if len(old) != len(cur) or len(old) != len(tidy):
            continue
        h, w = len(cur), len(cur[0])
        marks = [(y, x) for y in range(h) for x in range(w) if cur[y][x] in keys]
        doors = [(y, x) for y in range(h) for x in range(w)
                 if (y in (0, h - 1) or x in (0, w - 1)) and cur[y][x] == "."]
        for y in range(h):
            for s, e in runs(old[y], tidy[y]):
                if not all(cur[y][x] == "." for x in range(s, e)):
                    continue
                near = lambda pts: any(abs(py - y) <= NEAR and s - NEAR <= px < e + NEAR for py, px in pts)
                why = [n for n, pts in (("door", doors), ("find", marks)) if near(pts)] + (["side room"] if side else [])
                if not why:
                    continue
                print(f"{rid} row {y} cols {s}-{e - 1} ({'+'.join(why)})")
                if fix:
                    for x in range(s, e):
                        cur[y][x] = "="
                    restored += 1
        lines[cg:ce] = ["".join(r) for r in cur]
    if fix:
        path.write_text("\n".join(lines), encoding="utf-8")
        print("restored", restored)


if __name__ == "__main__":
    main()
