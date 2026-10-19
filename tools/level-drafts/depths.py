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

# ======================= Попелясті Глибини (зона 9) =======================
# Ряди по 27 клітин: y=54 (верхні кузні, горно Присяжного), y=27 (зала міхів), y=0 (галерея невільників),
# y=-27 (ковальня, велике колесо, плавильня), y=-54 (лавова ріка). Шахти: Кліть на ланцюгах (x 50),
# Лавопад (x 146), Східна шахта (x 222), Втеча від лави (x 298). Лава в чернетці — «^».

# ---------------- y = 54 ----------------
ccache = room("room_depths_chain_cache", "Схрон над кліттю", 26, 54, 24, flags="secret",
              keys={"w": "breakable explore-wall-depths_chain_cache 8", "g": "gold 340 depths-chain-purse",
                    "h": "shard health depths-chain-cache"})
lift = room("room_depths_cage_lift", "Кліть на ланцюгах", 50, 0, 16, h=81,
            keys={"l": "spot chain-lift-west", "i": "spot fire-imp-lift"})
slag = room("room_depths_slag_walk", "Шлаковий хідник", 66, 54, 60, flags="optional",
            keys={"b": "spot bellows-keeper-slag", "c": "spot coal-slime-slag", "i": "spot fire-imp-slag"})
forges = room("room_depths_upper_forges", "Верхні кузні", 126, 54, 96, flags="optional",
              keys={"s": "spot forge-slave-upper", "t": "spot forge-slave-upper-2", "g": "spot ash-golem-upper",
                    "k": "spot crane-hook", "n": "spot inscription-forges", "o": "ring"})
east = room("room_depths_east_shaft", "Східна шахта", 222, -27, 16, h=108,
            keys={"i": "spot fire-imp-east", "j": "spot fire-imp-east-2", "l": "spot chain-lift-east"})
stair = room("room_depths_oath_stair", "Сходи Присяжного", 238, 54, 36,
             keys={"a": "altar depths-altar-03 «Вівтар перед горном»", "n": "spot inscription-oath"})
throne = room("room_depths_fire_throne", "Горно Присяжного", 274, 54, 84, h=36, flags="boss=fire-oathsworn",
              keys={"b": "spot fire-oathsworn", "m": "spot fire-seal-reward", "p": "spot forge-pendulum-west",
                    "q": "spot forge-pendulum-east"})

# ---------------- y = 27 ----------------
bellows = room("room_depths_bellows_hall", "Зала міхів", 66, 27, 80,
               keys={"a": "altar depths-altar-01 «Вівтар міхів»", "b": "spot bellows-keeper-hall",
                     "c": "spot coal-slime-hall", "n": "spot inscription-bellows", "k": "spot hanging-crate"})
lavafall = room("room_depths_lavafall", "Лавопад", 146, -54, 16, h=108,
                keys={"f": "spot lavafall-pulse", "i": "spot fire-imp-lavafall"})
lantern = room("room_depths_lantern_hall", "Ліхтарня", 162, 27, 60, flags="optional",
               keys={"s": "spot forge-slave-lantern", "i": "spot fire-imp-lantern", "x": "spot sava"})
post = room("room_depths_watch_post", "Пост наглядачів", 238, 27, 50, flags="optional",
            keys={"q": "spot crystal-gap-lava", "g": "spot ash-golem-post"})
ecache = room("room_depths_east_cache", "Схрон за лавою", 288, 27, 24, flags="secret needs=crystal-dash",
              keys={"w": "breakable explore-wall-depths_east_cache 8", "r": "item ember_heart depths-ember-heart",
                    "g": "gold 380 depths-east-purse"})

# ---------------- y = 0 ----------------
threshold = room("room_depths_threshold", "Попелястий поріг", 0, 0, 50, flags="exit=W",
                 keys={"s": "start", "n": "spot inscription-threshold", "c": "spot coal-slime-threshold",
                       "m": "spot magma-leaper-threshold"})
gallery = room("room_depths_slave_gallery", "Галерея невільників", 66, 0, 80, flags="optional",
               keys={"s": "spot forge-slave-gallery", "t": "spot forge-slave-gallery-2", "b": "spot bellows-keeper-gallery",
                     "n": "spot inscription-slaves"})
furnace = room("room_depths_furnace", "Піч", 162, 0, 60, flags="optional",
               keys={"g": "spot ash-golem-furnace", "c": "spot coal-slime-furnace", "i": "item iron_scrap depths-furnace-scrap"})
landing = room("room_depths_ember_landing", "Жаровий поміст", 238, 0, 60, flags="optional",
               keys={"b": "spot bellows-keeper-landing", "g": "gold 220 depths-landing-purse"})
trial = room("room_depths_lava_escape", "Втеча від лави", 298, -54, 16, h=81, flags="optional",
             keys={"t": "spot trial-lava-escape", "k": "shard mana depths-lava-escape", "l": "spot trial-lava-line"})

# ---------------- y = -27 ----------------
smithy = room("room_depths_altar_forge", "Ковальня", 66, -27, 80,
              keys={"a": "altar depths-altar-02 «Вівтар ковальні»", "s": "spot forge-slave-smithy",
                    "c": "spot coal-slime-smithy", "n": "spot inscription-smithy", "v": "spot anvil"})
wheel = room("room_depths_great_wheel", "Велике колесо", 162, -27, 60,
             keys={"w": "spot great-wheel", "g": "spot ash-golem-wheel", "i": "spot fire-imp-wheel"})
smelter = room("room_depths_smelter", "Плавильня", 238, -27, 60, flags="optional",
               keys={"b": "spot cauldron-bearer", "r": "spot cauldron-reward"})
inlet = room("room_depths_drain_inlet", "Нижній стік", 314, -27, 30, flags="optional exit=N",
             keys={"d": "spot aqueduct-drop", "c": "spot coal-slime-inlet"})

# ---------------- y = -54 ----------------
dcache = room("room_depths_deep_cache", "Схрон під ковальнею", 122, -54, 24, flags="secret",
              keys={"w": "breakable explore-wall-depths_deep_cache 8", "g": "gold 300 depths-deep-purse",
                    "i": "item iron_scrap depths-deep-scrap"})
river = room("room_depths_lava_river", "Лавова ріка", 162, -54, 136, flags="optional",
             keys={"m": "spot magma-leaper-river", "n": "spot magma-leaper-river-2", "o": "spot magma-leaper-river-3",
                   "x": "spot river-bridge"})

# ---------------- doors ----------------
door(ccache, lift, 56)
door(lift, slag, 59)
door(slag, forges)
door(forges, east, 56)
door(east, stair, 59)
door(stair, throne)
door(lift, bellows, 29)
door(bellows, lavafall, 29)
door(lavafall, lantern, 32)
door(lantern, east, 29)
door(east, post, 32)
door(post, ecache, 29)
door(threshold, lift, 2)
door(lift, gallery, 2)
door(gallery, lavafall, 2)
door(lavafall, furnace, 5)
door(furnace, east, 2)
door(east, landing, 5)
door(landing, trial, 2)
door(smithy, lavafall, -25)
door(lavafall, wheel, -22)
door(wheel, east, -25)
door(east, smelter, -25)
door(smelter, trial, -25)
door(trial, inlet, -22)
door(dcache, lavafall, -52)
door(lavafall, river, -52)
door(river, trial, -52)
edge_open(threshold, 'W', 2, 9)
edge_open(inlet, 'N', 10, 19)

# raised sills where a shaft door is one row over the floor
slag.fill(2, 7, 2, 4, '#'); stair.fill(2, 7, 2, 4, '#')
lantern.fill(2, 7, 2, 4, '#'); post.fill(2, 7, 2, 4, '#')
furnace.fill(2, 7, 2, 4, '#'); landing.fill(2, 7, 2, 4, '#'); wheel.fill(2, 7, 2, 4, '#')
inlet.fill(2, 7, 2, 4, '#')

# ---------------- shafts ----------------
ladder(lift, [(2, 'B'), (29, 'R'), (56, 'L'), (59, 'R')])
lift.put('l', 8, 2); lift.put('i', 8, 40)
ladder(lavafall, [(2, 'B'), (29, 'L'), (32, 'R'), (56, 'L'), (59, 'R'), (83, 'L'), (86, 'R')])
lavafall.put('f', 7, 20); lavafall.put('i', 8, 70)
ladder(east, [(2, 'B'), (29, 'L'), (32, 'R'), (56, 'L'), (59, 'R'), (83, 'L'), (86, 'R')])
east.put('i', 8, 30); east.put('j', 8, 66); east.put('l', 8, 2)
ladder(trial, [(2, 'B'), (29, 'L'), (32, 'R'), (56, 'L')])
trial.put('t', 8, 2); trial.put('k', 4, 56); trial.put('l', 8, 6)

# ---------------- rooms: things and shapes ----------------
ccache.put('w', 22, 2); ccache.put('g', 8, 2); ccache.put('h', 14, 2)
slag.put('b', 30, 5); slag.put('c', 46, 2); slag.put('i', 20, 10)
slag.ledge(14, 41, 1, '^'); slag.ledge(12, 43, 4)          # a slag bridge over a lava channel
forges.put('s', 24, 2); forges.put('t', 60, 2); forges.put('g', 76, 2); forges.put('k', 44, 13)
forges.put('n', 16, 2); forges.put('o', 44, 17)
forges.ledge(36, 52, 12)                                     # the crane arm: a hook over the floor
stair.put('a', 18, 2); stair.put('n', 10, 2)
throne.put('b', 56, 2); throne.put('m', 66, 2); throne.put('p', 28, 26); throne.put('q', 60, 26)
throne.ledge(8, 15, 1, '^'); throne.ledge(70, 81, 1, '^')     # lava gutters at the edges of the forge
for c0, r in [(16, 7), (36, 11), (54, 7)]:
    throne.ledge(c0, c0 + 8, r)                               # anvils and the throne steps

bellows.put('a', 30, 2); bellows.put('b', 52, 2); bellows.put('c', 64, 2); bellows.put('n', 20, 2)
bellows.put('k', 42, 14); bellows.ledge(38, 46, 13)          # the hanging crate on its chain
lantern.put('s', 30, 2); lantern.put('i', 46, 10); lantern.put('x', 50, 2)
post.ledge(8, 37, 1, '^')                                    # the lava pit: the crystal dash crosses it
post.put('q', 6, 5); post.put('g', 42, 2)
ecache.put('w', 0, 2); ecache.put('r', 12, 2); ecache.put('g', 18, 2)

threshold.ledge(20, 25, 1, '^')                              # the first lava pool, jumped
threshold.put('s', 6, 2); threshold.put('n', 12, 2); threshold.put('c', 36, 2); threshold.put('m', 22, 2)
gallery.put('s', 22, 2); gallery.put('t', 48, 2); gallery.put('b', 64, 2); gallery.put('n', 10, 2)
gallery.ledge(30, 37, 6); gallery.ledge(54, 59, 6)           # slave cages hung low: jump-overs
furnace.put('g', 30, 2); furnace.put('c', 44, 2); furnace.put('i', 52, 2)
furnace.ledge(20, 25, 1, '^'); furnace.ledge(36, 41, 1, '^')  # the furnace mouths
landing.put('b', 30, 2); landing.put('g', 50, 2)

smithy.put('a', 40, 2); smithy.put('s', 24, 2); smithy.put('c', 60, 2); smithy.put('n', 12, 2); smithy.put('v', 48, 2)
wheel.put('w', 30, 2); wheel.put('g', 46, 2); wheel.put('i', 20, 12)
for c0, r in [(16, 8), (26, 13), (36, 8)]:
    wheel.ledge(c0, c0 + 5, r)                                # the wheel's spokes, at rest
smelter.put('b', 40, 2); smelter.put('r', 50, 2)
smelter.ledge(18, 25, 1, '^'); smelter.ledge(12, 19, 7); smelter.ledge(36, 43, 9)
inlet.put('d', 14, 2); inlet.put('c', 22, 2)

dcache.put('w', 22, 2); dcache.put('g', 8, 2); dcache.put('i', 14, 2)
river.ledge(8, 127, 1, '^')
for c0, c1 in [(8, 24), (30, 46), (52, 68), (74, 90), (96, 112), (118, 127)]:
    river.ledge(c0, c1, 4)                                    # the broken bridge over the lava river
river.put('m', 27, 2); river.put('n', 71, 2); river.put('o', 115, 2); river.put('x', 60, 5)

links += [
    "link room_depths_oath_stair room_depths_fire_throne door",
    "link room_depths_watch_post room_depths_east_cache ability=crystal-dash",
]

header = """# Попелясті Глибини, по карті користувача (reference/maps/09_ashen_depths.webp). ЧЕРНЕТКА з хмари (2026-10-19):
# згенеровано скриптом tools/level-drafts/depths.py і ще НЕ перевірено LevelCheck — локальна сесія проганяє
# перевірку й правку. Формат — gamedesign/LEVEL_TOOL.md; зона — gamedesign/DEPTHS.md.
# На схід від Боліт: Отруйний стік (marsh.txt, потрібна Чумна маска) відчиняється в Попелястий поріг.
# origin тимчасовий — шов із Болотами ставить локальна сесія. Лава — «^» (як шипи: удар і повернення на край).
# Головний шлях: Попелястий поріг → Кліть на ланцюгах угору → Зала міхів (вівтар) → Лавопад униз
# (з нього — Ковальня, вівтар) → Велике колесо → Східна шахта вгору → Сходи Присяжного (вівтар) → Горно.
# Друга дорога: Галерея невільників → Піч; верхня — Шлаковий хідник → Верхні кузні.
# Нижній стік — прихід згори з Акведуків (обвальний удар на їхньому боці), назад угору звідси не вийти.
zone zone_ashen_depths
cell 0.5
origin 1100 -300
abilities dash double-jump grapple plague-mask
"""

if __name__ == "__main__":
    import os, sys
    out = os.path.join(os.path.dirname(os.path.abspath(__file__)), "depths.txt")
    probs = check()
    write(out, header)
    print('\n'.join(probs) or 'edges ok')
    print(len(rooms), 'rooms')
    sys.exit(1 if probs else 0)
