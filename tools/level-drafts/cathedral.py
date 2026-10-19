# Чернетка Затопленого Собору: кімнати описані тут, сітки малює скрипт. Джерело істини — cathedral.txt.
# Хмарна чернетка (2026-10-19): LevelCheck не запускався. Запуск: python3 cathedral.py (пише cathedral.txt поруч).
import os

class Room:
    def __init__(s, rid, name, x, y, w, h, flags="", keys=None):
        s.rid, s.name, s.x, s.y, s.w, s.h, s.flags = rid, name, x, y, w, h, flags
        s.keys = keys or {}
        s.grid = [['#'] * w for _ in range(h)]
        for r in range(2, h - 2):
            for c in range(2, w - 2):
                s.set(c, r, '.')
    # local coordinates: c from the left, r from the bottom
    def set(s, c, r, ch):
        assert 0 <= c < s.w and 0 <= r < s.h, f"{s.rid}: cell {c},{r} outside the room"
        s.grid[s.h - 1 - r][c] = ch
    def get(s, c, r): return s.grid[s.h - 1 - r][c]
    def fill(s, c0, c1, r0, r1, ch):
        for c in range(c0, c1 + 1):
            for r in range(r0, r1 + 1): s.set(c, r, ch)
    def ledge(s, c0, c1, r, ch='='): s.fill(c0, c1, r, r, ch)
    def put(s, ch, c, r):
        assert ch in s.keys, f"{s.rid}: key {ch} not declared"
        assert s.get(c, r) == '.', f"{s.rid}: {ch} at {c},{r} is not air"
        s.set(c, r, ch)
    def contains(s, gx, gy): return s.x <= gx < s.x + s.w and s.y <= gy < s.y + s.h
    def text(s):
        out = [f"room {s.rid} «{s.name}» at {s.x} {s.y}"]
        if s.flags: out.append(f"flags {s.flags}")
        for ch, d in s.keys.items(): out.append(f"key {ch} {d}")
        out.append("grid")
        out += [''.join(row) for row in s.grid]
        out.append("end")
        return '\n'.join(out)

rooms = []
def room(*a, **k):
    r = Room(*a, **k); rooms.append(r); return r

def owner(gx, gy):
    for r in rooms:
        if r.contains(gx, gy): return r
    return None

def door(a, b, g0, g1):
    """Opens a shared wall between two touching rooms: global rows (E/W) or columns (N/S) g0..g1."""
    if a.x + a.w == b.x or b.x + b.w == a.x:
        west, east = (a, b) if a.x < b.x else (b, a)
        for gy in range(g0, g1 + 1):
            for c in (west.w - 1, west.w - 2): west.set(c, gy - west.y, '.')
            for c in (0, 1): east.set(c, gy - east.y, '.')
    elif a.y + a.h == b.y or b.y + b.h == a.y:
        low, high = (a, b) if a.y < b.y else (b, a)
        for gx in range(g0, g1 + 1):
            for r in (low.h - 1, low.h - 2): low.set(gx - low.x, r, '.')
            for r in (0, 1): high.set(gx - high.x, r, '.')
    else:
        raise ValueError(f"{a.rid} and {b.rid} do not touch")

def exit_side(r, side, l0, l1):
    for v in range(l0, l1 + 1):
        if side == 'W': r.set(0, v, '.'); r.set(1, v, '.')
        if side == 'E': r.set(r.w - 1, v, '.'); r.set(r.w - 2, v, '.')
        if side == 'S': r.set(v, 0, '.'); r.set(v, 1, '.')
        if side == 'N': r.set(v, r.h - 1, '.'); r.set(v, r.h - 2, '.')

boundary_rows = set()
def ladder(gx, stands, first='L'):
    """Shaft ladder in global coordinates: 4-cell ledges at gx..gx+3 (L) and gx+8..gx+11 (R), sides alternate.
    Returns {stand: side}. A ledge never sits on the row where two rooms meet (Tomb lesson, LEVEL_TOOL.md)."""
    sides = {}
    side = first
    for st in stands:
        row = st - 1
        c0 = gx if side == 'L' else gx + 8
        r = owner(c0, row)
        assert r is not None, f"ladder ledge {c0},{row} outside rooms"
        assert row - r.y not in (0, r.h - 1), f"ladder ledge on a room seam at {c0},{row} ({r.rid})"
        for c in range(c0, c0 + 4): r.set(c - r.x, row - r.y, '=')
        sides[st] = side
        side = 'R' if side == 'L' else 'L'
    return sides

def steps(a, b, step=3, skip=()):
    out, s = [], a
    while s <= b:
        out.append(s); s += step
    return [x for x in out if x not in skip]

# =========================== middle band (floor 53): narthex, galleries, lower nave ===========================
narthex = room("room_cathedral_narthex", "Притвор", 0, 51, 44, 27, flags="exit=W",
               keys={"s": "start", "m": "spot myroslava-narthex", "i": "spot inscription-narthex",
                     "c": "spot drowned-chorister-narthex", "r": "spot small-rose-window"})
exit_side(narthex, 'W', 2, 9)
gallery = room("room_cathedral_west_gallery", "Західна галерея", 44, 51, 56, 27,
               keys={"k": "spot candle-bearer-gallery", "g": "spot glass-shard-gallery", "n": "spot saint-niche-gallery",
                     "f": "spot waterfall-head", "b": "spot bell-lift-west"})
lower = room("room_cathedral_lower_nave", "Нижній неф", 100, 51, 72, 27,
             keys={"a": "altar cathedral-altar-01 «Вівтар нижнього нефу»", "c": "spot drowned-chorister-nave",
                   "x": "spot censer-acolyte-nave", "v": "spot stone-saint-nave", "l": "spot nave-gate-west"})
side = room("room_cathedral_side_chapel", "Бічна каплиця", 172, 51, 44, 26,
            keys={"a": "altar cathedral-altar-02 «Вівтар бічної каплиці»", "l": "spot nave-gate-lever",
                  "w": "spot bell-wraith-chapel"})
# =========================== towers, choir, nave (boss) ===========================
wtower = room("room_cathedral_west_tower", "Західна дзвіниця", 62, 78, 16, 52, flags="optional",
              keys={"w": "spot bell-wraith-west-tower"})
loft = room("room_cathedral_organ_loft", "Органні хори", 48, 130, 44, 26, flags="optional",
            keys={"r": "spot choir-regent", "h": "shard health cathedral-organ-loft", "o": "spot organ-pipes"})
nave = room("room_cathedral_rose_nave", "Неф із розеткою", 100, 78, 72, 52, flags="boss=drowned-saint",
            keys={"b": "spot drowned-saint", "d": "spot depths-breath-reward", "a": "spot nave-altar",
                  "m": "spot myroslava-last-fire", "r": "spot rose-window", "i": "spot inscription-rose"})
eshaft = room("room_cathedral_chain_shaft", "Ланцюгова шахта", 216, 0, 16, 48,
              keys={"b": "spot bell-lift-east", "c": "spot censer-acolyte-shaft"})
etower = room("room_cathedral_east_tower", "Східна дзвіниця", 216, 48, 16, 82,
              keys={"i": "spot inscription-bell-tower", "w": "spot bell-wraith-east-tower", "e": "spot lost-wisp-bells"})
choir = room("room_cathedral_choir", "Хори", 172, 87, 44, 26,
             keys={"i": "spot inscription-choir", "c": "spot drowned-chorister-choir", "v": "spot stone-saint-choir"})
singing = room("room_cathedral_singing_hall", "Співоча зала", 232, 60, 56, 26, flags="optional",
               keys={"t": "spot trial-choir-bell", "g": "gold 450 cathedral-trial-purse", "c": "spot choir-stalls"})
ecache = room("room_cathedral_bell_cache", "Схрон під дзвонами", 232, 102, 22, 26, flags="secret",
              keys={"w": "breakable explore-wall-cathedral_bell_cache 8", "g": "gold 300 cathedral-chest-bells",
                    "e": "spot lost-wisp-bell-cache"})
moon = room("room_cathedral_moon_vault", "Місячна ніша", 232, 0, 26, 26, flags="optional needs=moon-eye",
            keys={"g": "gold 280 cathedral-moon-vault", "i": "item ring_of_life cathedral-moon-ring", "m": "spot moon-door"})
# =========================== lower bands (floor 2 / 29) ===========================
mourner = room("room_cathedral_mourner_chapel", "Каплиця Плакальниці", 118, 0, 36, 51,
               keys={"p": "spot mourner-statue", "k": "spot candle-bearer-mourner", "g": "spot glass-shard-mourner",
                     "s": "spot stone-saint-mourner"})
cloister = room("room_cathedral_flooded_cloister", "Затоплена галерея", 154, 0, 62, 26,
                keys={"c": "spot drowned-chorister-cloister", "x": "spot censer-acolyte-cloister",
                      "o": "ring", "q": "spot deep-water-cloister"})
bellg = room("room_cathedral_pilgrim_gallery", "Галерея прочан", 154, 26, 62, 25, flags="optional needs=grapple",
             keys={"g": "gold 240 cathedral-chest-pilgrims", "k": "spot candle-bearer-pilgrims"})
falls = room("room_cathedral_waterfall_gallery", "Галерея водоспаду", 40, 27, 78, 24, flags="optional",
             keys={"g": "spot glass-shard-falls", "w": "spot bell-wraith-falls", "q": "spot deep-water-falls",
                   "f": "spot waterfall-foot"})
wchapel = room("room_cathedral_water_chapel", "Каплиця біля води", 0, 27, 40, 24, flags="optional",
               keys={"a": "altar cathedral-altar-03 «Вівтар біля води»", "g": "gold 220 cathedral-chest-water-chapel",
                     "k": "spot candle-bearer-water-chapel"})
necro = room("room_cathedral_drowned_necropolis", "Затоплений некрополь", 40, -30, 78, 57, flags="optional needs=depths-breath",
             keys={"c": "spot drowned-chorister-necropolis", "v": "spot stone-saint-necropolis", "t": "spot sunken-tree",
                   "e": "spot lost-wisp-necropolis"})
deep = room("room_cathedral_sunken_choir", "Потоплений хор", 118, -30, 114, 30, flags="optional needs=depths-breath exit=E",
            keys={"i": "spot inscription-sunken-choir", "h": "shard health cathedral-sunken-choir", "c": "spot chandelier",
                  "x": "spot drowned-chorister-deep", "z": "spot drowned-chorister-deep-2", "a": "spot aqueduct-gate"})
seabed = room("room_cathedral_seabed_cache", "Схрон на дні", 16, -30, 24, 26, flags="secret needs=depths-breath",
              keys={"w": "breakable explore-wall-cathedral_seabed_cache 8", "g": "gold 320 cathedral-chest-seabed",
                    "p": "item choir-psalter cathedral-psalter"})

# =========================== doors (W/E rows, N/S cols, global) ===========================
door(narthex, gallery, 53, 60)
door(gallery, lower, 53, 60)
door(lower, side, 53, 60)               # gate, opened by the lever from the chapel side (link shortcut)
door(gallery, wtower, 64, 75)           # the west tower climbs out of the gallery ceiling
door(wtower, loft, 64, 75)
door(gallery, falls, 84, 89)            # the waterfall: a one-way drop through the gallery floor
door(lower, mourner, 130, 141)          # the hole under the nave (the pin on the map)
door(mourner, falls, 29, 36)
door(falls, wchapel, 29, 36)
door(mourner, cloister, 2, 9)
door(cloister, eshaft, 2, 9)
door(eshaft, etower, 218, 229)
door(eshaft, moon, 2, 9)
door(side, etower, 53, 60)
door(choir, etower, 89, 96)
door(nave, choir, 89, 96)
door(etower, singing, 62, 69)
door(etower, ecache, 104, 111)
door(cloister, bellg, 176, 187)         # hole in the cloister ceiling: rings up to the pilgrims
door(falls, necro, 100, 105)            # deep water under the waterfall (needs depths-breath)
door(cloister, deep, 196, 201)          # deep water in the cloister floor
door(necro, deep, -28, -21)
door(necro, seabed, -28, -21)
exit_side(deep, 'E', 2, 9)

# =========================== ladders (global) ===========================
# West tower: from the gallery floor (53) to the organ loft (132); the tower cache opens on an L ledge.
w_st = ladder(64, steps(56, 77) + steps(80, 128) + [129])
cache_stand = next(st for st, sd in sorted(w_st.items()) if sd == 'L' and st >= 100)
tcache = room("room_cathedral_tower_cache", "Схрон у дзвіниці", 40, cache_stand - 2, 22, 26, flags="secret",
              keys={"w": "breakable explore-wall-cathedral_tower_cache 8", "g": "gold 260 cathedral-chest-tower",
                    "e": "spot lost-wisp-tower"})
door(wtower, tcache, cache_stand, cache_stand + 7)
tcache.put('w', tcache.w - 2, 2); tcache.put('g', 8, 2); tcache.put('e', 14, 2)
# Under the nave: the mourner's chapel climbs back up to the nave floor (53); the waterfall gallery joins an L ledge.
m_st = ladder(130, steps(5, 50))
assert m_st[29] == 'L'
mourner.ledge(2, 11, 28)                # bridge from the west door to the L ledge at 29
# East: chain shaft and east tower, one ladder from the floor (2) to the bell cache (104).
e_st = ladder(218, steps(5, 47) + steps(50, 107))
for st, want in [(53, 'L'), (89, 'L'), (62, 'R'), (104, 'R')]:
    assert e_st[st] == want, (st, e_st[st])

# =========================== room contents ===========================
narthex.put('s', 6, 2); narthex.put('m', 12, 2); narthex.put('i', 18, 2); narthex.put('r', 24, 12); narthex.put('c', 34, 2)
gallery.put('k', 12, 2); gallery.put('n', 30, 2); gallery.put('g', 48, 12); gallery.put('f', 40, 2); gallery.put('b', 26, 2)
# the nave below the boss: altar west of the hole, the east half reached by the gate
lower.put('a', 14, 2); lower.put('c', 24, 2); lower.put('v', 50, 2); lower.put('x', 60, 2); lower.put('l', 68, 2)
side.put('a', 18, 2); side.put('l', 4, 2); side.put('w', 30, 10)
wtower.put('w', 8, 30)
loft.put('r', 34, 2); loft.put('h', 6, 2); loft.put('o', 38, 12)
loft.ledge(2, 9, 8); loft.ledge(34, 41, 8)      # organ galleries to dodge the chord waves on
# The nave: floor in two steps, choir balconies on both sides, the saint floats over the centre.
nave.fill(2, 15, 2, 4, '#'); nave.fill(56, 69, 2, 4, '#')
nave.ledge(17, 20, 7); nave.ledge(51, 54, 7)
nave.ledge(2, 11, 10); nave.ledge(60, 69, 10)
nave.ledge(28, 43, 16)                           # the broken rood-loft under the rose
nave.put('b', 36, 2); nave.put('d', 30, 2); nave.put('a', 42, 2); nave.put('m', 46, 2)
nave.put('r', 36, 30); nave.put('i', 64, 11)
eshaft.put('b', 7, 2); eshaft.put('c', 12, 2)
etower.put('i', 11, 20); etower.put('w', 7, 40); etower.put('e', 3, 59)   # on ledges 68 (R) and 107 (L)
choir.put('i', 36, 2); choir.put('c', 20, 2); choir.put('v', 10, 2)
singing.put('t', 10, 2); singing.put('c', 30, 2); singing.put('g', 50, 2)
singing.ledge(18, 25, 7); singing.ledge(34, 41, 7)    # choir stalls: the trial waves pass under them
ecache.put('w', 0, 2); ecache.put('g', 10, 2); ecache.put('e', 16, 2)
moon.put('m', 4, 2); moon.put('g', 14, 2); moon.put('i', 20, 2)
mourner.put('p', 22, 2); mourner.put('k', 6, 2); mourner.put('g', 26, 40); mourner.put('s', 30, 2)
# cloister: shallow water is decoration; the deep pool over the hole is '^' until depths-breath (cell `w` locally)
for gx in range(196, 202): cloister.set(gx - cloister.x, 1, '^')
cloister.put('c', 12, 2); cloister.put('x', 52, 2); cloister.put('q', 44, 2)
# up to the pilgrims: two ledges, a gap only the ring closes (the ring hangs right over the third ledge),
# then ledges to the hole; the last step (24 -> 28) is a second jump, fine in an optional room
cloister.ledge(22, 25, 4); cloister.ledge(30, 33, 7)
cloister.ledge(22, 25, 17); cloister.put('o', 24, 22)
cloister.ledge(30, 33, 20); cloister.ledge(22, 25, 23)
bellg.put('g', 50, 2); bellg.put('k', 8, 2)
falls.put('g', 20, 12); falls.put('w', 56, 12); falls.put('f', 46, 2)
for gx in range(100, 106): falls.set(gx - falls.x, 1, '^')
falls.put('q', 58, 2)
wchapel.put('a', 18, 2); wchapel.put('g', 8, 2); wchapel.put('k', 30, 2)
# the necropolis: a deep water column from the pool down to the drowned floor
necro.put('c', 30, 2); necro.put('v', 50, 2); necro.put('t', 14, 2); necro.put('e', 66, 2)
for c0, r in [(6, 12), (20, 20), (36, 28), (52, 36), (66, 44)]:
    necro.ledge(c0, c0 + 6, r)                          # sunken tombs to rest on while diving
deep.put('i', 20, 2); deep.put('h', 58, 2); deep.put('c', 58, 22); deep.put('x', 36, 2); deep.put('z', 80, 2)
deep.put('a', 108, 2)
seabed.put('w', seabed.w - 2, 2); seabed.put('g', 8, 2); seabed.put('p', 14, 2)

links = [
    "link room_cathedral_west_gallery room_cathedral_waterfall_gallery oneway",
    "link room_cathedral_lower_nave room_cathedral_side_chapel shortcut=shortcut_cathedral_nave_gate from=room_cathedral_side_chapel",
    "link room_cathedral_choir room_cathedral_rose_nave door",
    "link room_cathedral_chain_shaft room_cathedral_moon_vault ability=moon-eye",
    "link room_cathedral_flooded_cloister room_cathedral_pilgrim_gallery ability=grapple",
    "link room_cathedral_waterfall_gallery room_cathedral_drowned_necropolis ability=depths-breath",
    "link room_cathedral_flooded_cloister room_cathedral_sunken_choir ability=depths-breath",
]

header = """# Затоплений Собор, по карті користувача (reference/maps/06_sunken_cathedral.webp). ЧЕРНЕТКА з хмари (2026-10-19):
# згенеровано скриптом cathedral.py і ще не перевірено LevelCheck — локальна сесія проганяє перевірку й правку.
# Формат — gamedesign/LEVEL_TOOL.md; зона — gamedesign/CATHEDRAL.md.
# Origin тимчасовий: шов зі Столицею (синя печать Крипт) локальна сесія ставить сама.
# Головний шлях: Притвор → Західна галерея → Нижній неф (вівтар) → люк у Каплицю Плакальниці → Затоплена галерея →
# Ланцюгова шахта → Бічна каплиця (вівтар, важіль ґрат нефу) → Східна дзвіниця → Хори → Неф із розеткою (Свята).
# Глибока вода (^ над люками) — до Дихання глибин не пускає; локально — клітка `w`.
zone zone_sunken_cathedral
cell 0.5
origin 300 -300
abilities dash double-jump grapple
"""
text = header + '\n' + '\n\n'.join(r.text() for r in rooms) + '\n\n' + '\n'.join(links) + '\n'
here = os.path.dirname(os.path.abspath(__file__))
open(os.path.join(here, "cathedral.txt"), "w", encoding="utf-8").write(text)

# --- sanity: rooms do not overlap; every open edge cell faces an open cell of a neighbour or a declared exit ---
problems = []
for i, a in enumerate(rooms):
    for b in rooms[i + 1:]:
        if a.x < b.x + b.w and b.x < a.x + a.w and a.y < b.y + b.h and b.y < a.y + a.h:
            problems.append(f"overlap {a.rid} {b.rid}")
def at(gx, gy):
    r = owner(gx, gy)
    return (r.get(gx - r.x, gy - r.y), r) if r else (None, None)
for r in rooms:
    exits = r.flags.split('exit=')[1].split()[0] if 'exit=' in r.flags else ''
    for c in range(r.w):
        for rr in range(r.h):
            if r.get(c, rr) == '#': continue
            edges = []
            if c == 0: edges.append(('W', -1, 0))
            if c == r.w - 1: edges.append(('E', 1, 0))
            if rr == 0: edges.append(('S', 0, -1))
            if rr == r.h - 1: edges.append(('N', 0, 1))
            for sd, dx, dy in edges:
                ch, other = at(r.x + c + dx, r.y + rr + dy)
                if other is None:
                    if sd not in exits: problems.append(f"{r.rid}: {sd} edge at {c},{rr} opens to nothing")
                elif ch == '#':
                    problems.append(f"{r.rid}: {sd} edge at {c},{rr} faces a wall of {other.rid}")
for line in links:
    for rid in line.split()[1:3]:
        if not any(r.rid == rid for r in rooms): problems.append(f"link to unknown room {rid}")
print('\n'.join(sorted(set(problems))) or 'edges ok')
print(len(rooms), 'rooms;', 'tower cache floor', cache_stand)
