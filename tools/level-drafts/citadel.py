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

# ======================= Залізна Цитадель (зона 8) =======================
# Ряди по 27 клітин: T y=54 (двір Тюремника), B y=27 (брама, зала присяги, казарми),
# C y=0 (камери, катівня), D y=-27 (камерний блок, зала стягів), E y=-54 (плац страт, клітка Богдана).
# Дві шахти на всю висоту: Західна якірна (x 166) і Головна (x 248).

# ---------------- T (y = 54) ----------------
hanged = room("room_citadel_hanged_cache", "Схрон повішеника", 100, 54, 24, flags="secret",
              keys={"w": "breakable explore-wall-citadel_hanged_cache 8", "g": "gold 320 citadel-hanged-purse",
                    "i": "item iron_scrap citadel-hanged-scrap"})
rampart = room("room_citadel_rampart", "Верхній мур", 124, 54, 42, flags="optional",
               keys={"c": "spot crossbowman-rampart", "x": "spot hanged-statue"})
anchor = room("room_citadel_anchor_shaft", "Якірна шахта", 166, -27, 16, h=108,
              keys={"o": "ring", "p": "ring", "h": "spot hound-anchor"})
gallery = room("room_citadel_portcullis", "Галерея ґрат", 182, 54, 44,
               keys={"j": "spot chain-jailer-gallery", "c": "spot crossbowman-gallery"})
approach = room("room_citadel_yard_approach", "Сторожа двору", 226, 54, 52,
                keys={"l": "spot yard-grate-lever", "m": "spot iron-maiden-approach", "n": "spot inscription-yard"})
yard = room("room_citadel_jailer_yard", "Двір Тюремника", 278, 54, 88, h=32, flags="boss=iron-jailer",
            keys={"b": "spot iron-jailer", "k": "spot bastion-key-reward", "o": "ring", "p": "ring"})
north = room("room_citadel_north_gate", "Північна брама", 366, 54, 60, h=54, flags="optional exit=N",
             keys={"q": "spot crystal-gap-north"})
ycache = room("room_citadel_yard_cache", "Схрон за двором", 426, 54, 24, flags="secret needs=crystal-dash",
              keys={"w": "breakable explore-wall-citadel_yard_cache 8", "r": "item warden_oath citadel-warden-oath",
                    "g": "gold 360 citadel-yard-purse"})

# ---------------- B (y = 27) ----------------
bcache = room("room_citadel_barracks_cache", "Схрон за казармами", 66, 27, 24, flags="secret",
              keys={"w": "breakable explore-wall-citadel_barracks_cache 8", "g": "gold 280 citadel-barracks-purse",
                    "i": "item iron_scrap citadel-barracks-scrap"})
barracks = room("room_citadel_barracks", "Казарми", 90, 27, 76, flags="optional",
                keys={"k": "spot chain-key", "j": "spot chain-jailer-barracks", "h": "spot hound-barracks",
                      "d": "spot hound-barracks-2", "n": "spot inscription-barracks", "c": "spot hanging-cage-barracks"})
oath = room("room_citadel_oath_hall", "Зала другої присяги", 182, 27, 66,
            keys={"a": "altar citadel-altar-02 «Вівтар присяги»", "c": "spot crossbowman-oath",
                  "m": "spot iron-maiden-oath"})
main = room("room_citadel_main_shaft", "Головна шахта", 248, -54, 16, h=108,
            keys={"s": "spot prisoner-main-shaft"})
gate = room("room_citadel_gate", "Брама Цитаделі", 264, 27, 66, flags="exit=E",
            keys={"s": "start", "a": "altar citadel-altar-01 «Вівтар брами»", "n": "spot inscription-gate",
                  "x": "spot gate-knight-statue", "h": "spot hound-gate"})

# ---------------- C (y = 0) ----------------
torture = room("room_citadel_torture", "Катівня", 110, 0, 56, flags="optional",
               keys={"e": "spot executioner", "k": "spot cage-key", "n": "spot inscription-torture",
                     "f": "spot spike-fence"})
cells = room("room_citadel_iron_cells", "Залізні камери", 182, 0, 36,
             keys={"m": "spot iron-maiden-cells", "p": "spot shackled-prisoner-cells", "v": "spot cell-shade-cells"})
post = room("room_citadel_warden_post", "Пост наглядача", 218, 0, 30,
            keys={"j": "spot chain-jailer-post", "c": "spot crossbowman-post"})

# ---------------- D (y = -27) ----------------
block = room("room_citadel_cell_block", "Камерний блок", 60, -27, 60, flags="optional",
             keys={"k": "spot cell-key", "n": "spot inscription-cells", "v": "spot cell-shade-block",
                   "u": "spot cell-shade-block-2", "p": "spot shackled-prisoner-block"})
lower = room("room_citadel_lower_hall", "Нижня зала", 120, -27, 46, flags="optional",
             keys={"m": "spot iron-maiden-lower", "h": "spot hound-lower"})
banners = room("room_citadel_banner_hall", "Зала стягів", 182, -27, 66,
               keys={"j": "spot chain-jailer-banners", "p": "spot shackled-prisoner-banners",
                     "c": "spot crossbowman-banners"})

# ---------------- E (y = -54) ----------------
cdrop = room("room_citadel_caverns_drop", "Стік до Печер", 60, -54, 30, flags="optional needs=crystal-dash exit=S",
             keys={"q": "spot crystal-gap-caverns"})
chasm = room("room_citadel_chasm", "Провалля", 90, -54, 60, flags="optional",
             keys={"v": "spot cell-shade-chasm"})
acache = room("room_citadel_ash_cache", "Схрон під шахтою", 224, -54, 24, flags="secret",
              keys={"w": "breakable explore-wall-citadel_ash_cache 8", "g": "gold 300 citadel-ash-purse",
                    "i": "item iron_scrap citadel-ash-scrap"})
arena = room("room_citadel_execution", "Плац страт", 264, -54, 80, flags="optional",
             keys={"t": "spot trial-citadel-arena", "g": "spot trial-gong", "l": "spot trial-wall-west",
                   "r": "spot trial-wall-east", "x": "spot gallows"})
cage = room("room_citadel_cage", "Клітка Богдана", 344, -54, 50, flags="optional",
            keys={"y": "spot bohdan", "a": "altar citadel-altar-03 «Вівтар біля клітки»",
                  "h": "spot cage-shard", "j": "spot chain-jailer-cage"})

# ---------------- doors ----------------
# shafts: floor stand is 2 above the room's y; a door's stand is the world row the hero walks on.
door(hanged, rampart)
door(rampart, anchor, 56)
door(anchor, gallery, 59)            # one row over the rampart door: the ledges alternate sides
door(gallery, approach)
door(approach, yard)
door(yard, north)
door(north, ycache)
door(bcache, barracks)
door(barracks, anchor, 29)
door(anchor, oath, 32)
door(main, gate, 29)
door(torture, anchor, 2)
door(anchor, cells, 5)
door(cells, post)
door(post, main, 2)
door(block, lower)
door(lower, anchor, -25)
door(anchor, banners, -25)
door(banners, main, -25)
door(cdrop, chasm)
door(acache, main, -52)
door(main, arena, -52)
door(arena, cage)
# the shortcut: a grate in the approach floor over the main shaft (lever on the approach side)
hatch(main, approach, 250, 255)
# the drop from the lower hall into the chasm (and back up by ledges)
hatch(chasm, lower, 140, 145)

# raised sills inside rooms where a shaft door sits one row higher than the floor
for r_ in (gallery, oath, cells):
    r_.fill(2, 7, 2, 4, '#')

# ---------------- shafts ----------------
# Якірна шахта: floor -25 (local 2); doors D both sides at 2, C W 29 / E 32, B W 56 / E 59, T W 83 / E 86.
ladder(anchor, [(2, 'B'), (29, 'L'), (32, 'R'), (56, 'L'), (59, 'R'), (83, 'L'), (86, 'R')])
anchor.put('o', 8, 70); anchor.put('p', 8, 44); anchor.put('h', 8, 2)
# Головна шахта: floor -52 (local 2); E row doors both sides at 2, D W 29, C W 56, B E 83, grate top-left 104.
ladder(main, [(2, 'B'), (29, 'L'), (56, 'L'), (83, 'R'), (104, 'L')])
main.put('s', 8, 2)

# ---------------- rooms: things and shapes ----------------
hanged.put('w', 22, 2); hanged.put('g', 8, 2); hanged.put('i', 14, 2)
rampart.put('c', 30, 2); rampart.put('x', 14, 2)
rampart.ledge(18, 25, 6)                                     # a broken merlon to fight around
gallery.put('j', 26, 2); gallery.put('c', 38, 2)
gallery.ledge(14, 19, 6); gallery.ledge(30, 35, 6)          # portcullis cross-bars, jump-overs
approach.put('l', 32, 2); approach.put('m', 14, 2); approach.put('n', 40, 2)
yard.put('b', 60, 2); yard.put('k', 70, 2)
for c0, r in [(14, 7), (34, 10), (52, 10), (72, 7)]:
    yard.ledge(c0, c0 + 6, r)                                 # two rows of gallows beams
yard.put('o', 37, 17); yard.put('p', 55, 17)
# Північна брама: a spike pit only the crystal dash crosses, then ledges up to the road north
north.ledge(8, 37, 1, '^')
ladder(north, [(2, 'B'), (47, 'L')], left=38, right=46)
edge_open(north, 'N', 38, 47)
north.put('q', 4, 2)
ycache.put('w', 0, 2); ycache.put('r', 10, 2); ycache.put('g', 16, 2)

bcache.put('w', 22, 2); bcache.put('g', 8, 2); bcache.put('i', 14, 2)
barracks.put('k', 8, 2); barracks.put('j', 40, 2); barracks.put('h', 24, 2); barracks.put('d', 30, 2)
barracks.put('n', 56, 2); barracks.put('c', 48, 12)
barracks.ledge(44, 52, 11)                                   # bunks stacked as a mezzanine
oath.put('a', 30, 2); oath.put('c', 50, 8); oath.put('m', 40, 2)
oath.ledge(46, 55, 7)                                        # the choir loft for the crossbowman
edge_open(gate, 'E', 2, 9)
gate.put('s', 56, 2); gate.put('a', 40, 2); gate.put('n', 48, 2); gate.put('x', 24, 2); gate.put('h', 12, 2)

torture.put('e', 26, 2); torture.put('k', 6, 2); torture.put('n', 40, 2); torture.put('f', 16, 2)
torture.ledge(10, 15, 6); torture.ledge(36, 41, 6)            # rack and wheel — places to jump the axe sweep
cells.put('m', 14, 2); cells.put('p', 22, 2); cells.put('v', 8, 2)
post.put('j', 14, 2); post.put('c', 22, 8); post.ledge(19, 26, 7)

block.put('k', 6, 2); block.put('n', 20, 2); block.put('v', 30, 2); block.put('u', 44, 2); block.put('p', 52, 2)
lower.put('m', 26, 2); lower.put('h', 10, 2)
banners.put('j', 30, 2); banners.put('p', 14, 2); banners.put('c', 50, 8); banners.ledge(46, 55, 7)

# Провалля: land under the hatch (east), climb back up by ledges; a spike pit west to the crystal gap.
chasm.ledge(4, 33, 1, '^')
ladder(chasm, [(2, 'B'), (23, 'R')], left=42, right=50)
chasm.put('v', 40, 2)
edge_open(cdrop, 'S', 8, 15)
cdrop.put('q', 22, 2)

acache.put('w', 22, 2); acache.put('g', 8, 2); acache.put('i', 14, 2)
arena.put('t', 40, 2); arena.put('g', 36, 2); arena.put('l', 4, 2); arena.put('r', 75, 2); arena.put('x', 56, 2)
for c0, r in [(10, 6), (26, 10), (46, 10), (62, 6)]:
    arena.ledge(c0, c0 + 7, r)                                # scaffolds of the old executions
cage.put('y', 30, 2); cage.put('a', 16, 2); cage.put('h', 36, 2); cage.put('j', 44, 2)

links += [
    "link room_citadel_yard_approach room_citadel_jailer_yard door",
    "link room_citadel_main_shaft room_citadel_yard_approach shortcut=shortcut_citadel_yard_grate from=room_citadel_yard_approach",
    "link room_citadel_jailer_yard room_citadel_north_gate boss=iron-jailer",
    "link room_citadel_north_gate room_citadel_yard_cache ability=crystal-dash",
    "link room_citadel_chasm room_citadel_caverns_drop ability=crystal-dash",
    "link room_citadel_lower_hall room_citadel_chasm oneway",
]

header = """# Залізна Цитадель, по карті користувача (reference/maps/10_iron_citadel.webp). ЧЕРНЕТКА з хмари (2026-10-19):
# згенеровано скриптом tools/level-drafts/citadel.py і ще НЕ перевірено LevelCheck — локальна сесія проганяє
# перевірку й правку. Формат — gamedesign/LEVEL_TOOL.md; зона — gamedesign/CITADEL.md.
# На захід від Столиці: Брама Цитаделі відчиняється на схід, у Затоплену цистерну (печать Цитаделі).
# origin тимчасовий — шов зі Столицею ставить локальна сесія.
# Головний шлях: Брама (вівтар) → Головна шахта вниз → Пост наглядача → Залізні камери → Якірна шахта вгору
# (з неї — Зала другої присяги, вівтар) → Галерея ґрат → Сторожа двору (ґрати-шорткат у шахту) → Двір Тюремника.
# Ключі Богдана: Казарми (ланцюг), Катівня (клітка, Кат), Камерний блок (камера). Плац страт — Арена Цитаделі.
# Кришталевий ривок: Північна брама (на Бастіон), Схрон за двором, Стік до Печер.
zone zone_iron_citadel
cell 0.5
origin 700 -300
abilities dash double-jump grapple ground-slam
"""

if __name__ == "__main__":
    import os, sys
    out = os.path.join(os.path.dirname(os.path.abspath(__file__)), "citadel.txt")
    probs = check()
    write(out, header)
    print('\n'.join(probs) or 'edges ok')
    print(len(rooms), 'rooms')
    sys.exit(1 if probs else 0)
