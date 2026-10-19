# Спільні помічники чернеток зон (хмара, 2026-10-19). Кожен скрипт зони вставляє цей код у себе
# (exec), щоб лишатися окремим файлом поруч із .txt. Формат — gamedesign/LEVEL_TOOL.md.
H = 27  # row height in cells: 27 = 9 jumps of 3, so shaft doors one row apart line up with the ledges


class Room:
    def __init__(s, rid, name, x, y, w, h=H, flags="", keys=None):
        s.rid, s.name, s.x, s.y, s.w, s.h, s.flags = rid, name, x, y, w, h, flags
        s.keys = dict(keys or {})
        s.grid = [['#'] * w for _ in range(h)]
        for r in range(2, h - 2):
            for c in range(2, w - 2):
                s.set(c, r, '.')
        s.ladders = []
    # c from the left, r from the bottom, both local
    def set(s, c, r, ch):
        s.grid[s.h - 1 - r][c] = ch
    def get(s, c, r):
        return s.grid[s.h - 1 - r][c]
    def ledge(s, c0, c1, r, ch='='):
        for c in range(c0, c1 + 1):
            s.set(c, r, ch)
    def fill(s, c0, c1, r0, r1, ch):
        for r in range(r0, r1 + 1):
            for c in range(c0, c1 + 1):
                s.set(c, r, ch)
    def put(s, ch, c, r):
        assert ch in s.keys, f"{s.rid}: key {ch} not declared"
        s.set(c, r, ch)
    def text(s):
        used = {ch for row in s.grid for ch in row if ch.islower()}
        missing = set(s.keys) - used
        assert not missing, f"{s.rid}: keys never placed {missing}"
        out = [f"room {s.rid} «{s.name}» at {s.x} {s.y}"]
        if s.flags:
            out.append(f"flags {s.flags}")
        for ch, d in s.keys.items():
            out.append(f"key {ch} {d}")
        out.append("grid")
        out += [''.join(row) for row in s.grid]
        out.append("end")
        return '\n'.join(out)


rooms, links = [], []


def room(*a, **k):
    r = Room(*a, **k)
    rooms.append(r)
    return r


def door(a, b, stand=None, rows=8):
    """a is west of b and they touch. Opens both edges at world rows stand..stand+rows-1."""
    assert a.x + a.w == b.x, f"{a.rid} and {b.rid} do not touch"
    if stand is None:
        stand = max(a.y, b.y) + 2
    for wy in range(stand, stand + rows):
        ra, rb = wy - a.y, wy - b.y
        assert 2 <= ra < a.h - 2 and 2 <= rb < b.h - 2, f"door {a.rid}->{b.rid} row {wy} outside"
        a.set(a.w - 1, ra, '.'); a.set(a.w - 2, ra, '.')
        b.set(0, rb, '.'); b.set(1, rb, '.')


def hatch(low, up, c0, c1):
    """low is under up and they touch. Opens low's ceiling and up's floor at world columns c0..c1."""
    assert low.y + low.h == up.y, f"{low.rid} and {up.rid} do not touch"
    for wx in range(c0, c1 + 1):
        cl, cu = wx - low.x, wx - up.x
        assert 0 <= cl < low.w and 0 <= cu < up.w
        low.set(cl, low.h - 1, '.'); low.set(cl, low.h - 2, '.')
        up.set(cu, 0, '.'); up.set(cu, 1, '.')


def edge_open(r, side, a, b):
    """An opening to another zone: rows (W/E) or columns (N/S) a..b, local."""
    for v in range(a, b + 1):
        if side == 'W': r.set(0, v, '.'); r.set(1, v, '.')
        if side == 'E': r.set(r.w - 1, v, '.'); r.set(r.w - 2, v, '.')
        if side == 'N': r.set(v, r.h - 1, '.'); r.set(v, r.h - 2, '.')
        if side == 'S': r.set(v, 0, '.'); r.set(v, 1, '.')


def step_plan(a, side_a, b, side_b):
    """Stands strictly between a and b (and b itself) going up: steps of 2..3, sides alternate,
    never two 2-steps in a row (a ledge of the same side stays 5+ cells over the one below)."""
    d = b - a
    for n in range((d + 2) // 3, d // 2 + 1):
        if side_a != 'B' and side_b != 'B':
            if (n % 2 == 1) != (side_a != side_b):
                continue
        t = d - 2 * n  # number of 3-steps
        if t < 0 or t > n:
            continue
        twos = n - t
        if twos > (n + 1) // 2:
            continue
        steps = [3] * n
        # spread the 2-steps: every other slot from the top
        for i in range(twos):
            steps[n - 1 - 2 * i] = 2
        # sides: going down from b
        sides = []
        sb = side_b if side_b != 'B' else ('L' if side_a != 'L' else 'R')
        for i in range(n):
            sides.append(sb if (n - 1 - i) % 2 == 0 else ('R' if sb == 'L' else 'L'))
        if side_a != 'B' and n and sides[0] == side_a:
            continue
        out, s = [], a
        for st, sd in zip(steps, sides):
            s += st
            out.append((s, sd))
        return out
    raise ValueError(f"no ladder from {a}{side_a} to {b}{side_b}")


def ladder(r, points, left=2, right=None, width=4):
    """Ledges in a shaft. points: [(stand, side)] from the bottom; side L, R or B (a whole floor).
    The ledge of stand s is row s-1; L at columns left..left+width-1, R at right..right+width-1."""
    if right is None:
        right = r.w - 2 - width
    plan = []
    for (a, sa), (b, sb) in zip(points, points[1:]):
        plan += step_plan(a, sa, b, sb)
    for s, sd in plan:
        c0 = left if sd == 'L' else right
        r.ledge(c0, c0 + width - 1, s - 1)
    r.ladders.append((points[0], plan, left, right, width))
    return plan


def check(entry_rooms=()):
    problems = []
    for i, a in enumerate(rooms):
        for b in rooms[i + 1:]:
            if a.x < b.x + b.w and b.x < a.x + a.w and a.y < b.y + b.h and b.y < a.y + a.h:
                problems.append(f"overlap {a.rid} {b.rid}")

    def at(wx, wy):
        for r in rooms:
            if r.x <= wx < r.x + r.w and r.y <= wy < r.y + r.h:
                return r.get(wx - r.x, wy - r.y), r
        return None, None

    adj = {r.rid: set() for r in rooms}
    for r in rooms:
        exits = r.flags.split('exit=')[1].split()[0] if 'exit=' in r.flags else ''
        for c in range(r.w):
            for rr in range(r.h):
                if r.get(c, rr) == '#':
                    continue
                edges = []
                if c == 0: edges.append(('W', -1, 0))
                if c == r.w - 1: edges.append(('E', 1, 0))
                if rr == 0: edges.append(('S', 0, -1))
                if rr == r.h - 1: edges.append(('N', 0, 1))
                for side, dx, dy in edges:
                    ch, other = at(r.x + c + dx, r.y + rr + dy)
                    if other is None:
                        if side not in exits:
                            problems.append(f"{r.rid}: {side} edge at {c},{rr} opens to nothing")
                    elif ch == '#':
                        problems.append(f"{r.rid}: {side} edge at {c},{rr} faces a wall of {other.rid}")
                    else:
                        adj[r.rid].add(other.rid)
    # ladders: steps 2..3, alternate, same side 5+ apart
    for r in rooms:
        for (first, plan, left, right, width) in r.ladders:
            prev = {'L': None, 'R': None}
            last = first[0]
            for s, sd in plan:
                if not 2 <= s - last <= 3:
                    problems.append(f"{r.rid}: ladder step {last}->{s}")
                if prev[sd] is not None and s - prev[sd] < 5:
                    problems.append(f"{r.rid}: ledge {sd} at {s} under 5 over {prev[sd]}")
                prev[sd] = s
                last = s
    # every room is joined to the others by an opening or a link
    for l in links:
        p = l.split()
        if p[1] in adj and p[2] in adj:
            adj[p[1]].add(p[2]); adj[p[2]].add(p[1])
        else:
            problems.append(f"link to unknown room: {l}")
    seen, todo = set(), [rooms[0].rid]
    while todo:
        x = todo.pop()
        if x in seen:
            continue
        seen.add(x)
        todo += adj[x]
    for r in rooms:
        if r.rid not in seen:
            problems.append(f"{r.rid}: not joined to the rest")
    return sorted(set(problems))


def write(path, header):
    text = header + '\n' + '\n\n'.join(r.text() for r in rooms) + '\n\n' + '\n'.join(links) + '\n'
    open(path, "w", encoding="utf-8").write(text)
