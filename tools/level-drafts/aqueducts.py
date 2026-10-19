# Чернетка Королівських Акведуків: кімнати описані тут, сітки малює скрипт. Джерело істини — aqueducts.txt.
# Хмарна чернетка (2026-10-19): LevelCheck не запускався; перевіряє лише перекриття кімнат і проёми.
import os
H = 26  # row height in cells

class Room:
    def __init__(s, rid, name, x, y, w, h=H, flags="", keys=None):
        s.rid, s.name, s.x, s.y, s.w, s.h, s.flags = rid, name, x, y, w, h, flags
        s.keys = keys or {}       # char -> definition
        s.grid = [['#'] * w for _ in range(h)]
        for r in range(2, h - 2):
            for c in range(2, w - 2):
                s.set(c, r, '.')
    # coordinates: c from left, r from bottom
    def set(s, c, r, ch):
        s.grid[s.h - 1 - r][c] = ch
    def get(s, c, r):
        return s.grid[s.h - 1 - r][c]
    def open_side(s, side, a, b):
        for v in range(a, b + 1):
            if side == 'W': s.set(0, v, '.'); s.set(1, v, '.')
            if side == 'E': s.set(s.w - 1, v, '.'); s.set(s.w - 2, v, '.')
            if side == 'N': s.set(v, s.h - 1, '.'); s.set(v, s.h - 2, '.')
            if side == 'S': s.set(v, 0, '.'); s.set(v, 1, '.')
    def ledge(s, c0, c1, r, ch='='):
        for c in range(c0, c1 + 1): s.set(c, r, ch)
    def stand(s, c0, c1, height, ch='='):
        """A ledge whose top a hero stands on at `height` (ledge row = height - 1)."""
        s.ledge(c0, c1, height - 1, ch)
    def put(s, ch, c, r):
        s.set(c, r, ch)
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

def climb(r, steps):
    """Shaft ledges: list of (stand height, first col, last col). Rules from LEVEL_TOOL.md:
    the first ledge 5 over the floor (walk under it), then +4..+5, nothing within 6 over a ledge one jumps from."""
    for h, c0, c1 in steps:
        r.stand(c0, c1, h)

def bridge(r, height, hole0, hole1):
    """A floor across a shaft at `height` with a hole to climb through."""
    for c in range(2, r.w - 2):
        if not hole0 <= c <= hole1:
            r.set(c, height - 1, '=')

# ---------------- middle row (y = 0): the water road from the Cathedral ----------------
inlet = room("room_aqueducts_inlet", "Водозабір", 0, 0, 40, flags="exit=W",
             keys={"s": "start", "d": "spot drowned-inlet", "n": "spot inscription-inlet", "j": "spot spout-inlet"})
inlet.open_side('W', 2, 9); inlet.open_side('E', 2, 9)
inlet.put('s', 6, 2); inlet.put('n', 12, 2); inlet.put('d', 26, 2); inlet.put('j', 35, 7)
inlet.ledge(16, 21, 1, '^')            # a first water trough (deep water placeholder), jumped over

west = room("room_aqueducts_west_channel", "Західний канал", 40, 0, 60,
            keys={"a": "altar aqueducts-altar-01 «Вівтар водозабору»", "e": "spot eel-west",
                  "k": "spot mokiy", "v": "spot sluice-west"})
west.open_side('W', 2, 9); west.open_side('E', 2, 9)
west.open_side('N', 4, 13)                       # up into the west wing (a dead end until its grate is open)
west.open_side('S', 12, 21); west.ledge(12, 16, 1)   # drop to the cistern; boards over the west half
climb(west, [(7, 6, 9), (11, 14, 17), (15, 6, 9), (19, 14, 17), (23, 6, 9)])
west.put('a', 34, 2); west.put('k', 30, 2); west.put('v', 52, 2)
west.ledge(40, 47, 1, '^'); west.put('e', 43, 2)    # the channel itself: an eel lives in it

wheel = room("room_aqueducts_wheel_channel", "Канал коліс", 100, 0, 70,
             keys={"w": "spot sluice-warden-wheel", "l": "spot living-water-wheel", "e": "spot eel-wheel",
                   "v": "spot sluice-wheel", "q": "spot waterwheels"})
wheel.open_side('W', 2, 9); wheel.open_side('E', 2, 9)
wheel.open_side('S', 20, 29); wheel.ledge(20, 24, 1)  # down to the wheel hall; boards keep the road
wheel.put('q', 26, 3); wheel.put('l', 12, 2); wheel.put('w', 50, 2); wheel.put('v', 62, 2)
wheel.ledge(38, 43, 1, '^'); wheel.put('e', 40, 2)
wheel.stand(46, 53, 5)                                 # the sluice house: the warden stands on its step

lock = room("room_aqueducts_lock_gate", "Шлюзова брама", 170, 0, 24,
            keys={"g": "spot lock-gate", "v": "spot sluice-lock", "d": "spot drowned-lock"})
lock.open_side('W', 2, 9); lock.open_side('E', 2, 9)
lock.put('v', 5, 2); lock.put('g', 12, 2); lock.put('d', 18, 2)

fall = room("room_aqueducts_great_fall", "Великий водоспад", 194, 0, 30, h=52,
            keys={"f": "spot waterfall-great", "c": "spot current-great", "j": "spot spout-great"})
fall.open_side('W', 2, 9); fall.open_side('E', 2, 9)
fall.open_side('S', 16, 25); fall.ledge(16, 20, 1)    # the plunge into the basin; boards keep the floor
fall.open_side('W', 28, 35); fall.open_side('E', 28, 35)
climb(fall, [(7, 12, 17), (11, 4, 7), (15, 12, 17), (19, 22, 25), (23, 12, 17)])
bridge(fall, 28, 12, 17)
fall.put('f', 14, 40); fall.put('c', 14, 30); fall.put('j', 26, 16)

pump = room("room_aqueducts_east_pump", "Східна помпа", 224, 0, 30, flags="optional",
            keys={"g": "gold 320 aqueducts-pump-chest", "e": "spot eel-pump", "v": "spot sluice-pump",
                  "o": "spot lost-wisp-pump"})
pump.open_side('W', 2, 9)
pump.ledge(8, 15, 1, '^'); pump.put('e', 11, 2)
pump.put('v', 18, 2); pump.put('g', 24, 2); pump.stand(22, 26, 9); pump.put('o', 24, 9)

# ---------------- upper-middle row (y = 26): the gallery, the wing, the trial ----------------
wing = room("room_aqueducts_west_wing", "Західне крило", 0, 26, 60, flags="optional",
            keys={"d": "spot drowned-wing", "j": "spot spout-wing", "o": "spot lost-wisp-wing",
                  "g": "gold 180 aqueducts-wing-purse"})
wing.open_side('S', 44, 53); wing.ledge(44, 48, 1)
wing.open_side('E', 2, 9)
wing.put('d', 22, 2); wing.put('j', 3, 8); wing.stand(6, 13, 9); wing.put('o', 9, 9); wing.put('g', 30, 2)

shaft = room("room_aqueducts_moon_shaft", "Місячний колодязь", 60, 26, 24, h=78,
             keys={"m": "spot moon-grate-lever", "l": "spot living-water-shaft", "f": "spot waterfall-moon",
                   "o": "spot moon-door", "j": "spot spout-shaft"})
shaft.open_side('E', 2, 9); shaft.open_side('W', 2, 9)        # gallery; west wing grate (shortcut)
shaft.open_side('E', 28, 35)                                  # the upper conduit
shaft.open_side('W', 54, 61)                                  # tower cache (its wall is a breakable)
climb(shaft, [(7, 9, 14), (11, 2, 5), (15, 9, 14), (19, 18, 21), (23, 9, 14)])
bridge(shaft, 28, 9, 14)
climb(shaft, [(34, 2, 5), (39, 9, 14), (44, 18, 21), (49, 9, 14)])
shaft.stand(2, 8, 54)
shaft.put('m', 3, 2); shaft.put('o', 4, 28); shaft.put('l', 18, 2); shaft.put('f', 12, 60); shaft.put('j', 20, 40)

gallery = room("room_aqueducts_waterfall_gallery", "Галерея водоспаду", 84, 26, 110,
               keys={"a": "altar aqueducts-altar-02 «Вівтар галереї»", "x": "spot gallery-waterfall",
                     "h": "spot gargoyle-gallery", "i": "spot gargoyle-gallery-2", "e": "spot eel-gallery",
                     "d": "spot drowned-gallery", "n": "spot inscription-gallery", "p": "spot pool-gallery"})
gallery.open_side('W', 2, 9); gallery.open_side('E', 2, 9)
gallery.ledge(40, 69, 1, '^')               # the great pool under the arcade
gallery.stand(36, 73, 5)                    # the arch bridge over it
gallery.put('p', 54, 2); gallery.put('e', 48, 2)
gallery.put('a', 18, 2); gallery.put('n', 24, 2); gallery.put('d', 86, 2); gallery.put('x', 100, 12)
gallery.stand(8, 15, 13); gallery.put('h', 11, 13)          # gargoyle perches on the arcade
gallery.stand(80, 87, 13); gallery.put('i', 83, 13)

trial = room("room_aqueducts_sluice_trial", "Шлюзи", 224, 26, 50, flags="optional",
             keys={"t": "spot trial-sluices", "u": "spot sluice-trial-a", "v": "spot sluice-trial-b",
                   "x": "spot sluice-trial-c", "r": "item current-ring aqueducts-trial-relic"})
trial.open_side('W', 2, 9); trial.open_side('E', 2, 9)
# three lock chambers: lintels hang from the ceiling, low sills on the floor; the water falls chamber by chamber
for c in (14, 26, 38):
    for rr in range(9, 24): trial.set(c, rr, '#'); trial.set(c + 1, rr, '#')
    trial.ledge(c - 1, c + 2, 2)
trial.put('t', 4, 2); trial.put('u', 12, 2); trial.put('v', 24, 2); trial.put('x', 36, 2); trial.put('r', 44, 2)

cache_e = room("room_aqueducts_east_cache", "Схрон за помпою", 274, 26, 24, flags="secret",
               keys={"w": "breakable explore-wall-aqueducts_east_cache 8", "g": "gold 300 aqueducts-chest-east",
                     "k": "item lockpick aqueducts-east-lockpick"})
cache_e.open_side('W', 2, 9); cache_e.put('w', 0, 2); cache_e.put('g', 10, 2); cache_e.put('k', 16, 2)

# ---------------- upper row (y = 52): the conduit to the throne, the moon door ----------------
moon = room("room_aqueducts_moon_door", "Місячні двері", 36, 52, 24, flags="optional needs=moon-eye exit=W",
            keys={"e": "spot moon-eye-portal"})
moon.open_side('W', 2, 9); moon.put('e', 10, 2)

conduit = room("room_aqueducts_upper_conduit", "Верхній акведук", 84, 52, 86,
               keys={"w": "spot sluice-warden-conduit", "x": "spot sluice-warden-conduit-2", "j": "spot spout-conduit",
                     "l": "spot living-water-conduit", "b": "spot aqueduct-arches", "v": "spot sluice-conduit"})
conduit.open_side('W', 2, 9); conduit.open_side('E', 2, 9)
conduit.ledge(40, 45, 1, '^')               # the open channel along the top of the aqueduct
conduit.put('l', 20, 2); conduit.put('w', 32, 2); conduit.put('b', 50, 2); conduit.put('x', 66, 2)
conduit.put('j', 83, 8); conduit.put('v', 74, 2)

stair = room("room_aqueducts_throne_stair", "Сходи до трону", 170, 52, 24, h=52,
             keys={"a": "altar aqueducts-altar-03 «Вівтар біля трону»", "n": "spot inscription-throne-door",
                   "d": "spot drowned-stair"})
stair.open_side('W', 2, 9); stair.open_side('E', 28, 35)
climb(stair, [(7, 9, 14), (11, 2, 5), (15, 9, 14), (19, 18, 21), (23, 9, 14)])
stair.stand(15, 21, 28)
stair.put('a', 6, 2); stair.put('d', 18, 2); stair.put('n', 19, 28)

cache_t = room("room_aqueducts_tower_cache", "Схрон у вежі", 36, 78, 24, flags="secret",
               keys={"w": "breakable explore-wall-aqueducts_tower_cache 8", "g": "gold 350 aqueducts-chest-tower",
                     "k": "item tide-signet aqueducts-tower-relic"})
cache_t.open_side('E', 2, 9); cache_t.put('w', 23, 2); cache_t.put('g', 8, 2); cache_t.put('k', 14, 2)

throne = room("room_aqueducts_throne", "Тронна зала Владаря", 194, 78, 80, h=32, flags="boss=sluice-lord",
              keys={"b": "spot sluice-lord", "t": "spot throne", "u": "spot valve-1", "v": "spot valve-2",
                    "x": "spot valve-3", "m": "spot water-seal-reward"})
throne.open_side('W', 2, 9)
throne.ledge(34, 45, 2); throne.ledge(34, 45, 3)      # the dais under the throne
throne.stand(6, 17, 9); throne.stand(62, 73, 9)       # side galleries — dry at mid water
throne.stand(22, 31, 16); throne.stand(48, 57, 16)    # high ledges — dry at high water
throne.put('t', 40, 4); throne.put('b', 52, 2)
throne.put('u', 10, 9); throne.put('v', 68, 9); throne.put('x', 52, 16); throne.put('m', 36, 4)

# ---------------- lower row (y = -26): the cistern, the great wheel, the basin ----------------
cistern = room("room_aqueducts_cistern", "Нижня цистерна", 20, -26, 70, flags="optional",
               keys={"m": "spot eel-mother", "r": "spot eel-mother-reward", "c": "spot cistern-arches"})
cistern.open_side('N', 32, 41); cistern.open_side('E', 2, 9)
cistern.ledge(4, 27, 1, '^'); cistern.ledge(46, 65, 1, '^')     # two dark pools
cistern.stand(4, 25, 5); cistern.stand(48, 65, 5)               # arch bridges over them
climb(cistern, [(7, 30, 33), (11, 38, 41), (15, 30, 33), (19, 38, 41), (23, 34, 37)])
cistern.put('m', 14, 2); cistern.put('r', 56, 5); cistern.put('c', 20, 5)

hall = room("room_aqueducts_wheel_hall", "Зала колеса", 90, -26, 64,
            keys={"q": "spot great-wheel", "l": "spot living-water-hall", "d": "spot drowned-hall"},
            flags="optional")
hall.open_side('W', 2, 9); hall.open_side('E', 2, 9)
hall.open_side('N', 30, 39)
hall.open_side('S', 16, 25); hall.ledge(16, 20, 1)
climb(hall, [(7, 28, 31), (11, 38, 41), (15, 28, 31), (19, 38, 41), (23, 32, 35)])
hall.put('q', 50, 2); hall.put('l', 8, 2); hall.put('d', 44, 2)

under = room("room_aqueducts_undercroft", "Підмурок", 154, -26, 40, flags="optional",
             keys={"d": "spot drowned-undercroft", "j": "spot spout-undercroft", "w": "spot sluice-warden-undercroft"})
under.open_side('W', 2, 9); under.open_side('E', 2, 9)
under.put('d', 10, 2); under.put('w', 26, 2); under.put('j', 37, 7)

basin = room("room_aqueducts_basin", "Чаша водоспаду", 194, -26, 40, flags="optional",
             keys={"c": "spot cracked-floor-basin", "e": "spot eel-basin", "f": "spot waterfall-basin-foot"})
basin.open_side('W', 2, 9); basin.open_side('N', 16, 25)
basin.ledge(30, 37, 1, '^'); basin.put('e', 33, 2)
climb(basin, [(7, 12, 15), (11, 22, 25), (15, 12, 15), (19, 22, 25), (23, 16, 19)])
basin.put('c', 6, 2); basin.put('f', 20, 2)

# ---------------- bottom row (y = -52): the deep run, the drain to the Depths ----------------
deep = room("room_aqueducts_deep_run", "Глибокий жолоб", 104, -52, 24, flags="optional",
            keys={"c": "spot deep-current", "o": "spot lost-wisp-deep"})
deep.open_side('N', 2, 11); deep.open_side('W', 2, 9)
climb(deep, [(7, 14, 17), (11, 4, 7), (15, 14, 17), (19, 4, 7), (23, 8, 11)])
deep.put('c', 16, 2); deep.put('o', 18, 2)

cache_d = room("room_aqueducts_deep_cache", "Схрон на дні", 80, -52, 24, flags="secret",
               keys={"w": "breakable explore-wall-aqueducts_deep_cache 8", "g": "gold 280 aqueducts-chest-deep",
                     "k": "item sluice-journal aqueducts-deep-journal"})
cache_d.open_side('E', 2, 9); cache_d.put('w', 23, 2); cache_d.put('g', 8, 2); cache_d.put('k', 14, 2)

drain = room("room_aqueducts_drain", "Нижній стік", 194, -52, 30, flags="optional needs=ground-slam exit=S",
             keys={"m": "spot drain-to-depths"})
drain.open_side('S', 10, 19); drain.put('m', 4, 2)

links = [
    "link room_aqueducts_west_wing room_aqueducts_moon_shaft shortcut=shortcut_aqueducts_wing_grate from=room_aqueducts_moon_shaft",
    "link room_aqueducts_moon_shaft room_aqueducts_moon_door ability=moon-eye",
    "link room_aqueducts_throne_stair room_aqueducts_throne door",
    "link room_aqueducts_basin room_aqueducts_drain ability=ground-slam",
]

header = """# Королівські Акведуки, по карті користувача (reference/maps/08_royal_aqueducts.webp). ЧЕРНЕТКА з хмари
# (2026-10-19): згенеровано скриптом aqueducts.py і ще не перевірено LevelCheck — локальна сесія проганяє перевірку
# й правку. Формат — gamedesign/LEVEL_TOOL.md; зона — gamedesign/AQUEDUCTS.md.
# Вхід — із затопленого низу Собору (пливти, Дихання глибин) на захід у Водозабір; origin тимчасовий, шов — локально.
# Головний шлях: Водозабір → Західний канал (вівтар) → Канал коліс → Шлюзова брама → Великий водоспад →
# Галерея водоспаду (вівтар) → Місячний колодязь → Верхній акведук → Сходи до трону (вівтар) → Тронна зала Владаря.
# Друга дорога: канал → Нижня цистерна → Зала колеса → Підмурок → Чаша водоспаду → назад угору водоспадом.
# Глибока вода (^) — заглушка: локально клітка ':' (пливти з Диханням глибин). Рівень води — шлюзи (спот sluice-*).
zone zone_royal_aqueducts
cell 0.5
origin 300 -600
abilities dash double-jump grapple depths-breath
"""
here = os.path.dirname(os.path.abspath(__file__))
text = header + '\n' + '\n\n'.join(r.text() for r in rooms) + '\n\n' + '\n'.join(links) + '\n'
open(os.path.join(here, "aqueducts.txt"), "w", encoding="utf-8").write(text)

# --- sanity: rooms do not overlap; every open edge cell faces an open cell of a neighbour or an exit;
#     every key letter is placed exactly once; links name real rooms ---
def cells(r):
    return (r.x, r.y, r.x + r.w, r.y + r.h)
problems = []
for i, a in enumerate(rooms):
    for b in rooms[i + 1:]:
        ax0, ay0, ax1, ay1 = cells(a); bx0, by0, bx1, by1 = cells(b)
        if ax0 < bx1 and bx0 < ax1 and ay0 < by1 and by0 < ay1:
            problems.append(f"overlap {a.rid} {b.rid}")
def at(wx, wy):
    for r in rooms:
        if r.x <= wx < r.x + r.w and r.y <= wy < r.y + r.h:
            return r.get(wx - r.x, wy - r.y), r
    return None, None
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
            for side, dx, dy in edges:
                ch, other = at(r.x + c + dx, r.y + rr + dy)
                if other is None:
                    if side not in exits: problems.append(f"{r.rid}: {side} edge at {c},{rr} opens to nothing")
                elif ch == '#':
                    problems.append(f"{r.rid}: {side} edge at {c},{rr} faces a wall of {other.rid}")
    for ch in r.keys:
        n = sum(row.count(ch) for row in r.grid)
        if n != 1: problems.append(f"{r.rid}: key {ch} placed {n} times")
ids = {r.rid for r in rooms}
for l in links:
    for w in l.split()[1:3]:
        if w not in ids: problems.append(f"link names unknown room {w}")
print('\n'.join(sorted(set(problems))) or 'edges ok')
print(len(rooms), 'rooms')
