#!/usr/bin/env python3
"""Turns the Forest's floating zigzag ladders into trees: the two columns of ledges grow from one trunk.

  python tools/level-drafts/trees.py forest          -> rewrites Levels/forest.txt, prints what changed

A cluster is three or more floating ledges (floating.py) in two columns that alternate up a room. The trunk takes
the two middle cells of the gap between the columns (the painter draws it there, behind the hero — it has no
collider); every ledge grows inward to touch the trunk and its outer end moves by -1…+1 so no two branches match,
and every fourth one is a longer fork to rest on. Only air cells change. Then run LevelCheck.
"""
import random
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
from floating import ROOT, floats, rooms, segments  # noqa: E402


def clusters(rows):
    fl = [s for s in segments(rows) if floats(rows, *s)]
    seen, out = set(), []
    for i in range(len(fl)):
        if i in seen:
            continue
        group, todo = [], [i]
        seen.add(i)
        while todo:
            a = todo.pop()
            group.append(fl[a])
            for b in range(len(fl)):
                if b in seen:
                    continue
                ya, a0, a1 = fl[a]
                yb, b0, b1 = fl[b]
                gap = max(a0, b0) - min(a1, b1) - 1
                if abs(ya - yb) <= 7 and gap <= 6:
                    seen.add(b)
                    todo.append(b)
        if len(group) >= 3:
            out.append(sorted(group))
    return out


def grow(rows, rid):
    rows = [list(r) for r in rows]
    w = len(rows[0])
    rng = random.Random(rid)
    changed = []
    for group in clusters(["".join(r) for r in rows]):
        starts = sorted({a for _, a, _ in group})
        if len(starts) < 2:
            continue
        mid = (starts[0] + starts[-1]) / 2
        left = [s for s in group if s[1] < mid + 0.5]
        right = [s for s in group if s[1] >= mid + 0.5]
        spread = lambda side: max(a for _, a, _ in side) - min(a for _, a, _ in side)
        if len(left) < 2 or len(right) < 2 or spread(left) > 2 or spread(right) > 2:  # a stair, not a zigzag
            continue
        gap0 = max(b for _, _, b in left) + 1
        gap1 = min(a for _, a, _ in right) - 1
        if gap1 - gap0 + 1 < 4:
            continue
        t0 = (gap0 + gap1) // 2  # trunk: t0, t0+1
        for k, (y, a, b) in enumerate(sorted(group, key=lambda s: -s[0])):
            is_left = (y, a, b) in left
            fork = k % 4 == 3
            if is_left:
                na, nb = a + rng.randint(-1, 1) - (2 if fork else 0), t0 - 1
            else:
                na, nb = t0 + 2, b + rng.randint(-1, 1) + (2 if fork else 0)
            na, nb = max(1, na), min(w - 2, nb)
            if nb - na + 1 < 4:
                continue
            row = rows[y]
            if any(row[x] not in ".=" for x in range(na, nb + 1)):
                continue
            for x in range(a, b + 1):
                row[x] = "."
            for x in range(na, nb + 1):
                row[x] = "="
            changed.append(f"y{y} {a}-{b} -> {na}-{nb}")
        changed.append(f"trunk x{t0}-{t0 + 1}")
    return ["".join(r) for r in rows], changed


def main():
    zone = sys.argv[1]
    path = ROOT / "Assets/Game/Levels" / f"{zone}.txt"
    lines = path.read_text(encoding="utf-8").split("\n")
    for rid, rows in list(rooms(lines)):
        start = next(i for i, l in enumerate(lines) if l.startswith(f"room {rid} "))
        g = next(j for j in range(start, len(lines)) if lines[j].strip() == "grid") + 1
        new, changed = grow(lines[g:g + len(rows)], rid)
        if changed:
            lines[g:g + len(rows)] = new
            print(rid, "; ".join(changed))
    path.write_text("\n".join(lines), encoding="utf-8")


if __name__ == "__main__":
    main()
