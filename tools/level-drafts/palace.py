# Чернетка Королівського Палацу (зона 14): кімнати описані тут, сітки малює скрипт. Джерело істини — palace.txt.
# ХМАРНА ЧЕРНЕТКА (2026-10-19): через LevelCheck не проганялася. Скрипт перевіряє лише, що кімнати не
# перекриваються, що кожен відкритий край дивиться у відкриту клітку сусіда чи в оголошений вихід, і грубо —
# що головні кімнати досяжні (угору до 6 кліток і 7 убік, униз і вбік до 9; кільця грубо; LevelCheck точніший).
import os, sys
from collections import deque

H = 26  # row height in cells


class Room:
    def __init__(s, rid, name, x, y, w, h=H, flags="", keys=None):
        s.rid, s.name, s.x, s.y, s.w, s.h, s.flags = rid, name, x, y, w, h, flags
        s.keys = keys or {}
        s.grid = [['#'] * w for _ in range(h)]
        for r in range(2, h - 2):
            for c in range(2, w - 2):
                s.set(c, r, '.')

    def set(s, c, r, ch):
        s.grid[s.h - 1 - r][c] = ch

    def get(s, c, r):
        return s.grid[s.h - 1 - r][c]

    def put(s, ch, c, r):
        assert s.get(c, r) == '.', f"{s.rid}: key {ch} at {c},{r} is not air"
        s.set(c, r, ch)

    def ledge(s, c0, c1, r, ch='='):
        for c in range(c0, c1 + 1):
            s.set(c, r, ch)

    def fill(s, c0, c1, r0, r1, ch='#'):
        for r in range(r0, r1 + 1):
            for c in range(c0, c1 + 1):
                s.set(c, r, ch)

    def text(s):
        out = [f"room {s.rid} «{s.name}» at {s.x} {s.y}"]
        if s.flags:
            out.append(f"flags {s.flags}")
        for ch, d in s.keys.items():
            out.append(f"key {ch} {d}")
        out.append("grid")
        out += [''.join(row) for row in s.grid]
        out.append("end")
        return '\n'.join(out)


rooms = []


def room(*a, **k):
    r = Room(*a, **k)
    rooms.append(r)
    return r


def find(wx, wy):
    for r in rooms:
        if r.x <= wx < r.x + r.w and r.y <= wy < r.y + r.h:
            return r
    return None


def wset(wx, wy, ch):
    r = find(wx, wy)
    assert r is not None, f"no room at {wx},{wy}"
    r.set(wx - r.x, wy - r.y, ch)


def hdoor(a, b, wy0, wy1):
    """Doorway between a (west) and b (east), world rows wy0..wy1."""
    assert a.x + a.w == b.x, f"{a.rid} and {b.rid} are not neighbours"
    for wy in range(wy0, wy1 + 1):
        a.set(a.w - 1, wy - a.y, '.'); a.set(a.w - 2, wy - a.y, '.')
        b.set(0, wy - b.y, '.'); b.set(1, wy - b.y, '.')


def vhole(low, high, wx0, wx1):
    """Hole between low (below) and high (above), world columns wx0..wx1."""
    assert low.y + low.h == high.y, f"{low.rid} and {high.rid} are not stacked"
    for wx in range(wx0, wx1 + 1):
        low.set(wx - low.x, low.h - 1, '.'); low.set(wx - low.x, low.h - 2, '.')
        high.set(wx - high.x, 0, '.'); high.set(wx - high.x, 1, '.')


def exit_side(r, side, a, b):
    for v in range(a, b + 1):
        if side == 'W': r.set(0, v, '.'); r.set(1, v, '.')
        if side == 'E': r.set(r.w - 1, v, '.'); r.set(r.w - 2, v, '.')
        if side == 'N': r.set(v, r.h - 1, '.'); r.set(v, r.h - 2, '.')
        if side == 'S': r.set(v, 0, '.'); r.set(v, 1, '.')


def ladder(wx, stands, first='L'):
    """Shaft ledges (4 cells) alternating at wx..wx+3 (L) and wx+10..wx+13 (R); stand = row the hero stands on."""
    side = first
    for st in stands:
        if side == 'B':
            side = 'L'
        c = wx if side == 'L' else wx + 10
        for x in range(c, c + 4):
            wset(x, st - 1, '=')
        side = 'R' if side == 'L' else 'L'


def wledge(wx0, wx1, wy, ch='='):
    for x in range(wx0, wx1 + 1):
        wset(x, wy, ch)


# ======================= Row D (y = 0): the gate, the bath, the ballroom, the cellar =======================
ccache = room("room_palace_cellar_cache", "Схрон у льоху", 0, 0, 24, flags="secret",
              keys={"w": "breakable explore-wall-palace_cellar_cache 8", "g": "gold 420 palace-chest-cellar",
                    "r": "item relic-vintners-seal palace-cellar-relic"})
cellar = room("room_palace_cellar", "Винний льох", 24, 0, 44, flags="optional",
              keys={"m": "spot candle-moth-cellar", "b": "spot wine-barrels"})
gate = room("room_palace_seal_gate", "Брама трьох печаток", 68, 0, 40, flags="exit=S",
            keys={"s": "start", "i": "spot inscription-seal-gate", "x": "spot three-seals-door",
                  "q": "spot royal-guardsman-gate"})
pool = room("room_palace_bath", "Купальня зі статуєю", 108, -4, 76, h=30,
            keys={"v": "spot queen-statue", "u": "spot bath-water", "p": "spot court-phantom-bath",
                  "m": "spot candle-moth-bath", "l": "spot lost-light-bath"})
ball = room("room_palace_ballroom", "Бальна зала", 184, 0, 80,
            keys={"a": "altar palace-altar-04 «Вівтар бальної зали»", "p": "spot court-phantom-ball-1",
                  "q": "spot court-phantom-ball-2", "f": "spot lady-in-waiting-ball", "k": "spot clockwork-knight-ball",
                  "i": "spot inscription-ballroom", "c": "spot ball-chandelier"})

# ======================= Row C (y = 26): garden, gallery, lift, water shaft, servants' stair, rotunda =======================
garden = room("room_palace_garden", "Королівський сад", 0, 26, 64, flags="optional",
              keys={"a": "altar palace-altar-03 «Вівтар саду»", "t": "spot trial-hedge-maze",
                    "f": "spot garden-fountain", "g": "gold 180 palace-garden-purse"})
gallery = room("room_palace_banner_gallery", "Галерея стягів", 64, 26, 40, flags="optional",
               keys={"q": "spot royal-guardsman-gallery", "o": "spot palace-priest-gallery"})
lift = room("room_palace_lift", "Ліфт челяді", 104, 26, 20, h=78, flags="optional",
            keys={"c": "spot lift-cage"})
shaft = room("room_palace_water_shaft", "Водяна шахта", 160, 26, 24,
             keys={"m": "spot candle-moth-shaft"})
servants = room("room_palace_servants_stair", "Сходи челяді", 184, 26, 24, flags="optional",
                keys={"k": "spot clockwork-knight-servants"})
rotunda = room("room_palace_moon_rotunda", "Місячна ротонда", 208, 26, 88, flags="optional",
               keys={"r": "spot moon-rotunda", "p": "spot court-phantom-rotunda", "f": "spot lady-in-waiting-rotunda",
                     "g": "gold 240 palace-rotunda-purse"})
moongate = room("room_palace_moon_door", "Місячні двері", 296, 26, 16, flags="optional needs=moon-eye exit=E",
                keys={"d": "spot moon-door-observatory"})

# ======================= Row B (y = 52): mirror hall, grand stair, chandeliers, audience, dining =======================
mirror = room("room_palace_mirror_hall", "Дзеркальна зала", 44, 52, 60, flags="optional",
              keys={"b": "spot miniboss-mirror-cavalier", "1": "spot mirror-1", "2": "spot mirror-2",
                    "3": "spot mirror-3", "r": "item relic-silver-hand-mirror palace-mirror-relic"})
grand = room("room_palace_grand_stair", "Парадні сходи", 124, 52, 60,
             keys={"a": "altar palace-altar-02 «Вівтар парадних сходів»", "q": "spot royal-guardsman-stair",
                   "o": "spot palace-priest-stair", "l": "spot lost-light-chandelier", "c": "spot grand-chandelier"})
chand = room("room_palace_chandelier_hall", "Зала люстр", 184, 52, 44,
             keys={"o": "ring", "p": "ring", "m": "spot candle-moth-chandelier", "c": "spot swinging-chandelier",
                   "d": "spot drawbridge"})
audience = room("room_palace_audience", "Приймальня", 228, 52, 32,
                keys={"k": "spot clockwork-knight-audience", "d": "spot red-door-lever", "t": "spot curtain-climb"})
dining = room("room_palace_dining", "Королівська їдальня", 260, 52, 56,
              keys={"y": "spot luka", "f": "spot lady-in-waiting-dining", "q": "spot royal-guardsman-dining",
                    "t": "spot long-table", "g": "gold 300 palace-dining-chest"})

# ======================= Row A (y = 78): terrace, throne, chapel, bedchamber, bell =======================
tcache = room("room_palace_terrace_cache", "Схрон на терасі", 0, 78, 24, flags="secret",
              keys={"w": "breakable explore-wall-palace_terrace_cache 8", "g": "gold 460 palace-chest-terrace",
                    "r": "item relic-moon-brooch palace-terrace-relic"})
terrace = room("room_palace_moon_terrace", "Тераса під місяцем", 24, 78, 80, flags="optional",
               keys={"a": "altar palace-altar-01 «Вівтар тераси»", "z": "spot gazebo", "p": "spot court-phantom-terrace",
                     "m": "spot candle-moth-terrace"})
throne = room("room_palace_throne_hall", "Тронна зала Королеви", 124, 78, 84, flags="boss=phantom-queen",
              keys={"b": "spot phantom-queen", "c": "spot crown-reward", "h": "spot bohdan-arena",
                    "1": "spot throne-mirror-west", "2": "spot throne-mirror-east", "x": "spot red-stair-gate"})
chapel = room("room_palace_chapel", "Палацова каплиця", 208, 78, 44,
              keys={"y": "spot bohdan", "i": "spot inscription-chapel", "v": "spot chapel-window"})
bedroom = room("room_palace_bedchamber", "Опочивальня Королеви", 252, 78, 32,
               keys={"p": "spot court-phantom-bedchamber", "o": "spot palace-priest-bedchamber"})
bell = room("room_palace_bell_tower", "Палацова дзвіниця", 284, 78, 40, h=52,
            keys={"k": "spot clockwork-knight-bell", "b": "spot palace-bell", "l": "spot lost-light-bell"})
bcache = room("room_palace_bell_cache", "Схрон за дзвіницею", 324, 78, 24, flags="secret",
              keys={"w": "breakable explore-wall-palace_bell_cache 8", "g": "gold 380 palace-chest-bell",
                    "r": "item relic-bellringers-glove palace-bell-relic"})

# ----------------------------- doors (world rows of the floor + 7) -----------------------------
hdoor(ccache, cellar, 2, 9); hdoor(cellar, gate, 2, 9); hdoor(gate, pool, 2, 9); hdoor(pool, ball, 2, 9)
hdoor(garden, gallery, 28, 35); hdoor(gallery, lift, 28, 35)
hdoor(shaft, servants, 28, 35); hdoor(servants, rotunda, 28, 35); hdoor(rotunda, moongate, 28, 35)
hdoor(mirror, lift, 54, 61); hdoor(lift, grand, 54, 61)  # lift side: the R landing at 57; hdoor(grand, chand, 54, 61)
hdoor(chand, audience, 54, 61); hdoor(audience, dining, 54, 61)
hdoor(tcache, terrace, 80, 87); hdoor(terrace, lift, 80, 87)
hdoor(throne, chapel, 80, 87); hdoor(chapel, bedroom, 80, 87); hdoor(bedroom, bell, 80, 87); hdoor(bell, bcache, 80, 87)
exit_side(gate, 'S', 14, 25)                 # stair up from the Capital's throne room (seam: local session)
gate.ledge(14, 19, 1)                        # boards over the west half of the stairwell: the floor stays walkable
exit_side(moongate, 'E', 2, 9)               # the moon door to the Observatory

# ----------------------------- the bath: raised walks, a sunk basin, a bridge -----------------------------
pool.fill(2, 17, 2, 5); pool.fill(52, 73, 2, 5)  # walks at stand 6 (world 2) on both sides
pool.ledge(22, 47, 5)                             # the balcony bridge over the water
pool.put('v', 34, 2); pool.put('u', 26, 2); pool.put('p', 30, 6); pool.put('m', 12, 6); pool.put('l', 20, 2)

# ----------------------------- vertical links -----------------------------
vhole(cellar, garden, 30, 43)
ladder(30, [8, 12, 16, 20, 24])                   # wine-cellar ladder up into the garden
vhole(pool, shaft, 166, 179)
vhole(shaft, grand, 166, 179)
ladder(166, [8, 12, 16, 20, 24, 28, 32, 36, 40, 44, 48])
wledge(172, 179, 53)                              # boards over the east half of the hole: the stair floor stays walkable
vhole(ball, servants, 190, 203)
ladder(190, [8, 12, 16, 20, 24])
vhole(servants, chand, 192, 205)                  # fall from the chandelier pit (one-way)
vhole(rotunda, dining, 268, 281)
ladder(268, [34, 38, 42, 46, 50], first='R')
wledge(268, 275, 53)                              # boards over the west half of the hole in the dining floor
vhole(dining, bell, 290, 303)
ladder(290, [60, 64, 68, 72, 76])
wledge(296, 303, 79)                              # boards over the east half of the hole in the bell-tower floor
vhole(grand, throne, 148, 161)                    # the red stair: shortcut from the throne side
ladder(148, [60, 64, 68, 72, 76])

# lift: landings at 54 (both doors) and 80 (west door), alternating ledges between
L, R = (106, 109), (116, 119)
def lift_ledge(side, st):
    a, b = L if side == 'L' else R
    wledge(a, b, st - 1)
# west doors leave from a left landing, the east door from a right one 3 cells higher (as the Capital's tower)
for st, side in [(34, 'L'), (38, 'R'), (42, 'L'), (48, 'R'), (62, 'L'), (66, 'R'), (70, 'L'), (74, 'R')]:
    lift_ledge(side, st)
wledge(106, 109, 53)                              # landing 54 (L): to the mirror hall
wledge(116, 121, 56)                              # landing 57 (R): down into the grand stair (floor 54)
wledge(106, 109, 79)                              # landing 80 (L): to the terrace
lift.put('c', 9, 2)

# bell climb to the bell (optional)
ladder(306, [86, 90, 94, 98, 102, 106, 110, 114, 118])

# ----------------------------- rooms' insides -----------------------------
ccache.put('w', 23, 2); ccache.put('g', 8, 2); ccache.put('r', 14, 2)
cellar.put('m', 28, 2); cellar.put('b', 36, 2)
gate.put('s', 8, 2); gate.put('i', 30, 2); gate.put('x', 34, 2); gate.put('q', 26, 2)
ball.put('i', 12, 2); ball.put('p', 30, 2); ball.put('q', 46, 2); ball.put('f', 58, 2); ball.put('k', 38, 2)
ball.put('a', 72, 2); ball.put('c', 44, 14)
for c0, r in [(28, 8), (46, 9), (60, 8)]:          # galleries of the ballroom (musicians' balconies)
    ball.ledge(c0, c0 + 6, r)

# garden: the hedge maze — clipped hedges are stone blocks (3 high, jumpable), walks on hedge tops
for c0 in (16, 24, 58):
    garden.fill(c0, c0 + 1, 2, 4)
for c0, c1, r in [(4, 12, 8), (16, 26, 11), (6, 14, 14), (20, 28, 17), (44, 54, 8), (48, 58, 13), (36, 44, 15)]:
    garden.ledge(c0, c1, r)
garden.put('a', 4, 2); garden.put('t', 12, 2); garden.put('f', 26, 2); garden.put('g', 52, 14)
gallery.put('q', 14, 2); gallery.put('o', 28, 2)
shaft.put('m', 3, 2)
servants.put('k', 3, 2)
rotunda.put('r', 30, 2); rotunda.put('p', 18, 2); rotunda.put('f', 44, 2); rotunda.put('g', 80, 2)
moongate.put('d', 8, 2)
mirror.put('b', 30, 2); mirror.put('1', 10, 2); mirror.put('2', 30, 6); mirror.put('3', 50, 2); mirror.put('r', 6, 2)
mirror.ledge(26, 34, 5)                            # dais of the middle mirror
grand.put('a', 10, 2); grand.put('q', 32, 2); grand.put('o', 18, 2); grand.put('l', 30, 23); grand.put('c', 26, 21)
chand.put('o', 11, 13); chand.put('p', 18, 13); chand.ledge(14, 15, 8)  # rings on two chandeliers, a candle-ring ledge
chand.put('m', 30, 2); chand.put('c', 14, 9); chand.put('d', 4, 2)
audience.put('k', 18, 2); audience.put('d', 4, 2); audience.put('t', 26, 2)
dining.put('y', 46, 2); dining.put('f', 26, 2); dining.put('q', 36, 2); dining.put('t', 40, 2); dining.put('g', 52, 2)
tcache.put('w', 23, 2); tcache.put('g', 8, 2); tcache.put('r', 14, 2)
terrace.put('a', 30, 2); terrace.put('z', 62, 2); terrace.put('p', 46, 2); terrace.put('m', 16, 2)
for c0, r in [(54, 8), (66, 12)]:                  # the gazebo roof and a balustrade step
    terrace.ledge(c0, c0 + 8, r)
throne.put('b', 62, 2); throne.put('c', 66, 2); throne.put('h', 76, 2); throne.put('1', 8, 2); throne.put('2', 78, 6)
throne.put('x', 30, 2)
for c0, r in [(10, 8), (44, 10), (68, 8)]:          # chandeliers and the throne dais
    throne.ledge(c0, c0 + 7, r)
chapel.put('y', 16, 2); chapel.put('i', 26, 2); chapel.put('v', 22, 10)
bedroom.put('p', 14, 2); bedroom.put('o', 22, 2)
bell.put('k', 28, 2); bell.put('b', 30, 46); bell.put('l', 24, 46)
bcache.put('w', 0, 2); bcache.put('g', 10, 2); bcache.put('r', 16, 2)

links = [
    "link room_palace_chapel room_palace_throne_hall door",
    "link room_palace_grand_stair room_palace_throne_hall shortcut=palace_red_stair from=room_palace_throne_hall",
    "link room_palace_chandelier_hall room_palace_audience shortcut=palace_drawbridge from=room_palace_audience",
    "link room_palace_chandelier_hall room_palace_servants_stair oneway",
    "link room_palace_moon_rotunda room_palace_moon_door ability=moon-eye",
]

header = """# Королівський Палац, по карті користувача (reference/maps/14_royal_palace.webp). ЧЕРНЕТКА з хмари (2026-10-19):
# згенеровано скриптом palace.py і ще НЕ перевірено LevelCheck — локальна сесія проганяє перевірку й правку.
# Формат — gamedesign/LEVEL_TOOL.md; зона — gamedesign/PALACE.md. origin тимчасовий: шов зі Столицею
# (сходи з «Трону Короля», синя печатка Палацу) і з Обсерваторією (місячні двері) — у локальній сесії.
# Головний шлях: Брама трьох печаток → Купальня зі статуєю → Водяна шахта → Парадні сходи (вівтар) →
# Зала люстр (кільця на люстрах над проваллям) → Приймальня → Королівська їдальня (Лука) → Палацова дзвіниця →
# Опочивальня Королеви → Палацова каплиця (Богдан) → Тронна зала Королеви (бос).
# Друга дорога: Купальня → Бальна зала (вівтар) → Сходи челяді → Місячна ротонда → угору в їдальню.
# Західне крило (необов'язкове): Ліфт челяді → Галерея стягів → Королівський сад (випробування, вівтар) →
# Винний льох; Дзеркальна зала (мінібос); Тераса під місяцем (вівтар).
zone zone_royal_palace
cell 0.5
origin 700 -900
abilities dash double-jump grapple ground-slam plague-mask depths-breath crystal-dash
"""

text = header + '\n' + '\n\n'.join(r.text() for r in rooms) + '\n\n' + '\n'.join(links) + '\n'
out = os.path.join(os.path.dirname(os.path.abspath(__file__)), "palace.txt")
open(out, "w", encoding="utf-8").write(text)

# ============================== checks ==============================
problems = []
for i, a in enumerate(rooms):
    for b in rooms[i + 1:]:
        if a.x < b.x + b.w and b.x < a.x + a.w and a.y < b.y + b.h and b.y < a.y + a.h:
            problems.append(f"overlap {a.rid} {b.rid}")


def cell(wx, wy):
    r = find(wx, wy)
    return (None, None) if r is None else (r.get(wx - r.x, wy - r.y), r)


for r in rooms:
    exits = r.flags.split('exit=')[1].split()[0] if 'exit=' in r.flags else ''
    for c in range(r.w):
        for rr in range(r.h):
            if r.get(c, rr) == '#':
                continue
            for side, dx, dy, on in (('W', -1, 0, c == 0), ('E', 1, 0, c == r.w - 1),
                                     ('S', 0, -1, rr == 0), ('N', 0, 1, rr == r.h - 1)):
                if not on:
                    continue
                ch, other = cell(r.x + c + dx, r.y + rr + dy)
                if other is None:
                    if side not in exits:
                        problems.append(f"{r.rid}: {side} edge at {c},{rr} opens to nothing")
                elif ch == '#':
                    problems.append(f"{r.rid}: {side} edge at {c},{rr} faces a wall of {other.rid}")

# keys used once, every key placed
for r in rooms:
    placed = {ch for row in r.grid for ch in row if ch not in '#.=^%'}
    for ch in r.keys:
        if ch not in placed:
            problems.append(f"{r.rid}: key {ch} never placed")
    for ch in placed - set(r.keys):
        problems.append(f"{r.rid}: char {ch} has no key")


# coarse reach: stand cells (air with 4 air above, solid below), moves: walk, fall, jump up to 6 / across 5,
# along an L-shaped body path through air. Rings lift to stands within 8 rows under/over them.
# closed shortcut gates count as stone: the main path must not need them
closed = {(x, y) for x in range(148, 162) for y in (76, 77, 78, 79)}  # the red stair (opened from the throne)


def solid(wx, wy):
    ch, r = cell(wx, wy)
    return r is not None and (ch in '#=%' or (wx, wy) in closed)


def air(wx, wy):
    ch, r = cell(wx, wy)
    return r is not None and ch not in '#=%^' and (wx, wy) not in closed


def body(wx, wy):
    return all(air(wx, wy + k) for k in range(4))


def standable(wx, wy):
    return body(wx, wy) and solid(wx, wy - 1)


xs = [r.x for r in rooms] + [r.x + r.w for r in rooms]
ys = [r.y for r in rooms] + [r.y + r.h for r in rooms]
stands = {(x, y) for x in range(min(xs), max(xs)) for y in range(min(ys), max(ys)) if standable(x, y)}
rings = [(r.x + c, r.y + rr) for r in rooms for c in range(r.w) for rr in range(r.h)
         if r.keys.get(r.get(c, rr), '') == 'ring']
blocked_rooms = {r.rid for r in rooms if 'needs=' in r.flags}


def path_clear(a, b):
    (x0, y0), (x1, y1) = a, b
    n = max(abs(x1 - x0), abs(y1 - y0), 1)
    if all(body(round(x0 + (x1 - x0) * k / n), round(y0 + (y1 - y0) * k / n) + (1 if 0 < k < n and y1 >= y0 else 0))
           for k in range(n + 1)):
        return True
    top = max(y0, y1)
    for y in range(y0, top + 1):
        if not body(x0, y):
            return False
    step = 1 if x1 >= x0 else -1
    for x in range(x0, x1 + step, step):
        if not body(x, top):
            return False
    for y in range(top, y1 - 1, -1):
        if not body(x1, y):
            return False
    return True


def neighbours(p):
    x, y = p
    for dx in range(-9, 10):
        for dy in range(-40, 7):
            q = (x + dx, y + dy)
            if q in stands and q != p and (dy <= 0 or abs(dx) <= 7) and path_clear(p, q):
                yield q
    for rx, ry in rings:
        if abs(rx - x) <= 14 and 0 < ry - y <= 15:
            for q in stands:
                if abs(q[0] - rx) <= 8 and -14 <= q[1] - ry <= 2:
                    yield q


start = next((r.x + c, r.y + rr) for r in rooms for c in range(r.w) for rr in range(r.h) if r.get(c, rr) == 's')
seen = {start}
dq = deque([start])
while dq:
    p = dq.popleft()
    for q in neighbours(p):
        if q not in seen and find(*q).rid not in blocked_rooms:
            seen.add(q); dq.append(q)
reached = {find(*p).rid for p in seen}
for r in rooms:
    if r.rid not in reached and r.rid not in blocked_rooms:
        problems.append(f"reach: {r.rid} not reached from start (coarse model)")

print('\n'.join(sorted(set(problems))) or 'edges ok, keys ok, all rooms reached (coarse)')
print(len(rooms), 'rooms')
sys.exit(1 if problems else 0)
