# Чернетка Трону Забуття (зона 15): кімнати описані тут, сітки малює скрипт. Джерело істини — throne.txt.
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


# ======================= Row M (y = 26): the hall of the sworn, the last altar, the veil =======================
hall = room("room_throne_sworn_hall", "Зала присяжних", 0, 26, 70, flags="exit=W",
            keys={"s": "start", "a": "altar throne-altar-01 «Вівтар присяжних»", "i": "spot inscription-sworn-hall",
                  "k": "spot kneeling-sworn", "q": "spot sworn-shieldbearer-hall", "h": "spot hnat-last"})
tower = room("room_throne_silent_tower", "Вежа Мовчазного", 70, 0, 28, h=104,
             keys={"v": "spot silent-statue", "c": "spot chain-hanged-tower", "l": "spot lock-treasury"})
corridor = room("room_throne_lower_corridor", "Нижній коридор", 98, 26, 22,
                keys={"r": "spot eclipse-raven-corridor"})
altar = room("room_throne_last_altar", "Вогонь забуття", 120, 26, 80,
             keys={"a": "altar throne-altar-02 «Вогонь забуття»", "i": "spot inscription-last-altar",
                   "p": "spot sworn-priest-altar", "q": "spot sworn-shieldbearer-altar", "y": "spot bohdan-fire",
                   "m": "spot miroslava-lantern"})
veil = room("room_throne_shadow_veil", "Завіса Тіні", 200, 26, 14,
            keys={"v": "spot shadow-veil"})
warden = room("room_throne_warden_hall", "Чатівня", 214, 0, 30, h=78,
              keys={"k": "spot shadow-knight-warden", "c": "spot chain-hanged-warden", "l": "spot lock-east"})
elock = room("room_throne_east_lock", "Замкнений прохід", 244, 26, 24, flags="optional",
             keys={"r": "spot eclipse-raven-lock"})

# ======================= Row L (y = 0) and below: cages, abyss, well, seal circle =======================
cages = room("room_throne_cage_hall", "Зала кліток", 130, 0, 84, flags="optional",
             keys={"t": "spot trial-court-of-the-forgotten", "g": "gold 520 throne-chest-cages",
                   "c": "spot hanging-cages", "s": "spot sworn-spearman-cages", "l": "spot lost-light-cages"})
abyss = room("room_throne_abyss_of_names", "Безодня імен", 10, -26, 120, flags="optional",
             keys={"b": "spot miniboss-oath-scribe", "g": "gold 480 throne-chest-abyss",
                   "r": "item relic-book-of-names throne-scribe-relic", "a": "spot sworn-archer-abyss"})
well = room("room_throne_eclipse_well", "Колодязь затемнення", 130, -52, 28, h=52, flags="optional",
            keys={"r": "spot eclipse-raven-well", "k": "spot shadow-knight-well"})
circle = room("room_throne_seal_circle", "Коло печаток", 158, -52, 60, flags="optional",
              keys={"o": "spot shadow-portal", "w": "spot seal-wheel", "s": "spot sworn-spearman-circle",
                    "l": "spot lock-crypt", "x": "spot lost-light-circle"})
crypt = room("room_throne_sealed_crypt", "Замкнена крипта", 218, -52, 30, flags="secret",
             keys={"g": "gold 600 throne-chest-crypt", "r": "item relic-first-oath throne-crypt-relic"})

# ======================= Row U (y = 52): treasury, candle gallery, watch niche, east vault =======================
treasury = room("room_throne_treasury", "Скарбниця вірних", 40, 52, 30, flags="secret",
                keys={"g": "gold 560 throne-chest-treasury", "r": "item relic-faithful-ring throne-treasury-relic"})
gallery = room("room_throne_candle_gallery", "Галерея свічок", 98, 52, 50, flags="optional",
               keys={"a": "spot sworn-archer-gallery", "c": "spot candle-rows"})
niche = room("room_throne_watch_niche", "Вартова ніша", 148, 52, 66, flags="optional",
             keys={"k": "spot shadow-knight-niche", "r": "spot eclipse-raven-niche", "i": "spot inscription-watch"})
vault = room("room_throne_east_vault", "Східна скарбниця", 244, 52, 40, flags="secret",
             keys={"g": "gold 540 throne-chest-east", "r": "item relic-kings-gauntlet throne-east-relic"})

# ======================= Row T (y = 78): vow cell, chain bridge, antechamber, the eclipse throne room =======================
vow = room("room_throne_vow_cell", "Келія присяги", 46, 78, 24, flags="secret",
           keys={"w": "breakable explore-wall-throne_vow_cell 8", "g": "gold 400 throne-chest-vow",
                 "v": "spot vow-statue"})
bridge = room("room_throne_chain_bridge", "Ланцюговий міст", 98, 78, 70, flags="optional",
              keys={"o": "ring", "c": "spot chain-hanged-bridge", "a": "spot sworn-archer-bridge"})
ante = room("room_throne_antechamber", "Передпокій затемнення", 168, 78, 22, flags="optional",
            keys={"i": "spot inscription-antechamber"})
eclipse = room("room_throne_eclipse_hall", "Тронна зала Затемнення", 190, 78, 110, h=52, flags="boss=ashen-king",
               keys={"b": "spot ashen-king", "t": "spot oblivion-throne", "e": "spot eclipse-sun",
                     "c": "spot ending-choice", "y": "spot bohdan-ending"})

# ----------------------------- doors -----------------------------
hdoor(hall, tower, 28, 35); hdoor(tower, corridor, 28, 35); hdoor(corridor, altar, 28, 35)
hdoor(altar, veil, 28, 35); hdoor(veil, warden, 28, 35); hdoor(warden, elock, 28, 35)
hdoor(treasury, tower, 54, 61); hdoor(tower, gallery, 54, 61); hdoor(gallery, niche, 54, 61); hdoor(niche, warden, 54, 61)
hdoor(vow, tower, 80, 87); hdoor(tower, bridge, 80, 87); hdoor(bridge, ante, 80, 87); hdoor(ante, eclipse, 80, 87)
hdoor(cages, warden, 2, 9)
hdoor(abyss, well, -24, -17); hdoor(well, circle, -50, -43); hdoor(circle, crypt, -50, -43)
exit_side(hall, 'W', 2, 9)                       # the crown gate from the Palace (seam: local session)

# ----------------------------- vertical links -----------------------------
vhole(abyss, tower, 76, 89)
ladder(76, [-18, -14, -10, -6, -2])               # out of the abyss into the tower
# the tower: west doors leave from a left landing, east doors from a right one 3 cells higher (Capital's tower);
# ledges L 74-77 / R 84-87, never one right under another closer than 6 cells
def ledge_lr(lw, rw, st, side):
    a, b = lw if side == 'L' else rw
    wledge(a, b, st - 1)
for st, side in [(8, 'L'), (12, 'R'), (16, 'L'), (22, 'R'), (36, 'L'), (40, 'R'), (44, 'L'), (48, 'R'),
                 (62, 'L'), (66, 'R'), (70, 'L'), (74, 'R')]:
    ledge_lr((74, 77), (84, 87), st, side)
for st in (28, 54, 80):
    wledge(72, 77, st - 1)                         # L landing: west door (floor st)
    wledge(84, 95, st + 2)                         # R landing 3 higher: east door, drop to the floor st

vhole(altar, gallery, 124, 137)                   # fall from the candle gallery to the last altar (one-way)
wledge(130, 137, 53)                              # boards over the east half: the gallery floor stays walkable
vhole(gallery, bridge, 128, 139)                  # the broken span of the chain bridge
vhole(cages, altar, 150, 163)                     # drop from the altar hall into the cages
wledge(150, 157, 27)                              # boards over the west half of that hole
vhole(well, cages, 136, 149)
for st, side in [(-44, 'L'), (-40, 'R'), (-36, 'L'), (-30, 'R'), (-18, 'R'), (-14, 'L'), (-10, 'R'),
                 (-6, 'L'), (-2, 'R')]:
    ledge_lr((136, 139), (146, 149), st, side)
wledge(132, 139, -25)                             # landing at -24 (L) to the abyss door
# the warden hall: W doors from L landings (28, 54), the E door from an R landing at 31; ledges L 220-223 / R 230-233
vhole(warden, eclipse, 220, 233)
for st, side in [(8, 'L'), (12, 'R'), (16, 'L'), (22, 'R'), (36, 'L'), (40, 'R'), (44, 'L'), (48, 'R'),
                 (58, 'R'), (62, 'L'), (66, 'R'), (70, 'L'), (74, 'R')]:
    ledge_lr((220, 223), (230, 233), st, side)
wledge(216, 223, 27); wledge(230, 241, 30); wledge(216, 223, 53)
vhole(elock, vault, 250, 263)
ladder(250, [34, 38, 42, 46, 50])

# ----------------------------- insides -----------------------------
hall.put('s', 6, 2); hall.put('a', 16, 2); hall.put('i', 24, 2); hall.put('k', 34, 2); hall.put('q', 50, 2)
hall.put('h', 10, 2)
for c0, r in [(28, 8), (44, 10)]:
    hall.ledge(c0, c0 + 6, r)                    # plinths of the kneeling sworn
tower.put('v', 22, 2); tower.put('c', 10, 44); tower.put('l', 4, 54)
corridor.put('r', 10, 2)
altar.ledge(54, 68, 4); altar.ledge(57, 65, 7)   # the stepped dais of the fire: two steps of 3 cells
altar.put('a', 61, 8); altar.put('i', 50, 2); altar.put('p', 72, 2); altar.put('q', 22, 2); altar.put('y', 67, 5)
altar.put('m', 55, 5)
veil.put('v', 6, 2)
warden.put('k', 24, 2); warden.put('c', 4, 12); warden.put('l', 26, 28)
elock.put('r', 3, 2)
cages.put('t', 40, 2); cages.put('g', 70, 2); cages.put('c', 50, 12); cages.put('s', 60, 2); cages.put('l', 4, 2)
for c0, r in [(38, 8), (54, 10), (66, 8)]:
    cages.ledge(c0, c0 + 5, r)                   # hanging cages as platforms
abyss.put('b', 40, 2); abyss.put('g', 8, 2); abyss.put('r', 12, 2); abyss.put('a', 96, 2)
for c0, r in [(20, 8), (34, 12), (50, 9), (100, 8)]:
    abyss.ledge(c0, c0 + 7, r)                   # spines of the cavern to dodge the ink
well.put('r', 22, 40); well.put('k', 24, 2)
circle.put('o', 30, 2); circle.put('w', 36, 2); circle.put('s', 46, 2); circle.put('l', 56, 2); circle.put('x', 24, 2)
crypt.put('g', 10, 2); crypt.put('r', 18, 2)
treasury.put('g', 10, 2); treasury.put('r', 18, 2)
gallery.put('a', 14, 2); gallery.put('c', 44, 2)
niche.put('k', 30, 2); niche.put('r', 50, 12); niche.put('i', 10, 2)
vault.put('g', 26, 2); vault.put('r', 32, 2)
vow.put('w', 23, 2); vow.put('g', 8, 2); vow.put('v', 14, 2)
bridge.put('o', 36, 14); bridge.put('c', 50, 2); bridge.put('a', 60, 2)
ante.put('i', 10, 2)
eclipse.put('b', 70, 2); eclipse.put('t', 92, 6); eclipse.put('e', 60, 40); eclipse.put('c', 96, 6); eclipse.put('y', 84, 2)
eclipse.ledge(86, 101, 5)                         # the throne dais
for c0, r in [(10, 9), (58, 11), (70, 16)]:
    eclipse.ledge(c0, c0 + 8, r)                  # broken balconies (phase 3 refuge from the oath wall)

links = [
    "link room_throne_last_altar room_throne_shadow_veil ability=shadow-cloak",
    "link room_throne_antechamber room_throne_eclipse_hall door",
    "link room_throne_warden_hall room_throne_eclipse_hall door",
    "link room_throne_candle_gallery room_throne_last_altar oneway",
    "link room_throne_chain_bridge room_throne_candle_gallery oneway",
    "link room_throne_last_altar room_throne_cage_hall oneway",
    "# замки (відмичка): новий вид зв'язку lock=lockpick — у локальній сесії; поки — двері",
    "link room_throne_treasury room_throne_silent_tower door",
    "link room_throne_warden_hall room_throne_east_lock door",
    "link room_throne_seal_circle room_throne_sealed_crypt door",
]

header = """# Трон Забуття, по карті користувача (reference/maps/15_throne_of_oblivion.webp). ЧЕРНЕТКА з хмари (2026-10-19):
# згенеровано скриптом throne.py і ще НЕ перевірено LevelCheck — локальна сесія проганяє перевірку й правку.
# Формат — gamedesign/LEVEL_TOOL.md; зона — gamedesign/THRONE.md. origin тимчасовий: шов із Палацом (коронні
# ворота, вихід W Зали присяжних) і портал із Царства Тіні (Коло печаток) — у локальній сесії.
# Головний шлях: Зала присяжних (вівтар) → Вежа Мовчазного → Нижній коридор → Вогонь забуття (останній вівтар) →
# Завіса Тіні (тіньовий плащ) → Чатівня (шахта на ланцюгах угору) → Тронна зала Затемнення (Король-Заклятий).
# Друга дорога (верхня): Вежа → Ланцюговий міст (кільце над проломом) → Передпокій затемнення → тронна зала.
# Замки (відмичка): Скарбниця вірних, Замкнений прохід → Східна скарбниця, Замкнена крипта.
zone zone_throne_of_oblivion
cell 0.5
origin 1100 -900
abilities dash double-jump grapple ground-slam plague-mask depths-breath crystal-dash moon-eye shadow-cloak
"""

text = header + '\n' + '\n\n'.join(r.text() for r in rooms) + '\n\n' + '\n'.join(links) + '\n'
out = os.path.join(os.path.dirname(os.path.abspath(__file__)), "throne.txt")
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
closed = set()  # no shortcut gates on the way: the locks (lockpick) lead only to optional rooms


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
