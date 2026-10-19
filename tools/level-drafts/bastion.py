# Чернетка Крижаного Бастіону: кімнати описані тут, сітки малює скрипт. Джерело істини — bastion.txt.
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

# ---------------- row 3 (y = 26): the gate, the banner hall, the knight, the rift, the keep ----------------
gate = room("room_bastion_gate", "Брама Бастіону", 0, 26, 24, flags="exit=W",
            keys={"s": "start", "i": "spot ice-floor-gate", "g": "spot bastion-gate"})
gate.open_side('W', 2, 9); gate.open_side('E', 2, 9)
gate.put('s', 4, 2); gate.put('g', 10, 2); gate.put('i', 16, 2)

banner = room("room_bastion_banner_hall", "Зала стягів", 24, 26, 100,
              keys={"a": "altar bastion-altar-01 «Вівтар брами»", "n": "spot inscription-banners",
                    "o": "spot oath-sentinel-banner", "p": "spot oath-sentinel-banner-2", "w": "spot snow-wolf-banner",
                    "b": "spot banners", "i": "spot ice-floor-banner"})
banner.open_side('W', 2, 9); banner.open_side('E', 2, 9)
banner.put('a', 12, 2); banner.put('n', 18, 2); banner.put('o', 34, 2); banner.put('b', 46, 12)
banner.ledge(60, 79, 2); banner.ledge(64, 75, 3)            # the frozen stair-dais in the middle of the hall
banner.put('i', 50, 2); banner.put('p', 70, 4); banner.put('w', 88, 2)

tower = room("room_bastion_central_tower", "Центральна вежа", 124, 0, 24, h=78,
             keys={"l": "spot rime-crawler-tower", "k": "spot tower-ladders", "i": "spot ice-floor-tower"})
for lo in (2, 28, 54):
    tower.open_side('W', lo, lo + 7); tower.open_side('E', lo, lo + 7)
climb(tower, [(7, 9, 14), (11, 2, 5), (15, 9, 14), (19, 18, 21), (23, 9, 14)])
bridge(tower, 28, 9, 14)
climb(tower, [(34, 2, 5), (39, 9, 14), (44, 18, 21), (49, 9, 14)])
bridge(tower, 54, 9, 14)
tower.put('k', 4, 2); tower.put('l', 11, 70); tower.put('i', 18, 28)

knight = room("room_bastion_knight_hall", "Зала Крижаного лицаря", 148, 26, 60,
              keys={"a": "altar bastion-altar-02 «Вівтар лицаря»", "k": "spot ice-knight-hall",
                    "x": "spot knight-statue", "c": "spot icicles-knight", "o": "spot oath-sentinel-knight",
                    "i": "spot ice-floor-knight"})
knight.open_side('W', 2, 9); knight.open_side('E', 2, 9)
knight.ledge(24, 35, 2); knight.put('x', 30, 3)             # the statue's plinth
knight.put('a', 8, 2); knight.put('k', 44, 2); knight.put('c', 40, 22); knight.put('o', 52, 2); knight.put('i', 16, 2)

rift = room("room_bastion_rift", "Розлам", 208, 26, 44,
            keys={"r": "spot crystal-dash-rift", "w": "spot snow-wolf-rift", "c": "spot rime-crawler-rift",
                  "i": "spot ice-floor-rift"})
rift.open_side('W', 2, 9); rift.open_side('E', 2, 9)
rift.open_side('S', 16, 33)            # the chasm: 18 cells (9 units) — only the crystal dash crosses it
rift.put('r', 13, 2); rift.put('w', 6, 2); rift.put('c', 25, 23); rift.put('i', 38, 2)

keep = room("room_bastion_east_keep", "Східна башта", 252, 26, 30, h=52,
            keys={"a": "altar bastion-altar-03 «Вівтар башти»", "n": "spot inscription-keep-door",
                  "l": "spot keep-ladder", "f": "spot rampart-archer-keep"})
keep.open_side('W', 2, 9); keep.open_side('E', 2, 9); keep.open_side('E', 28, 35)
keep.open_side('S', 6, 13); keep.ledge(6, 8, 1)              # the ladder well down to the chapel (shortcut)
climb(keep, [(7, 12, 17), (11, 4, 7), (15, 12, 17), (19, 22, 25), (23, 12, 17)])
keep.stand(20, 27, 28)
keep.put('a', 20, 2); keep.put('l', 11, 2); keep.put('n', 23, 28); keep.put('f', 5, 11)

throne = room("room_bastion_oath_throne", "Тронна зала Присяжної", 282, 52, 70, h=78, flags="boss=ice-oathsworn",
              keys={"b": "spot ice-oathsworn", "t": "spot oath-throne", "c": "spot chandeliers",
                    "m": "spot ice-seal-reward", "y": "spot sky-gate"})
throne.open_side('W', 2, 9); throne.open_side('W', 54, 61)
throne.stand(2, 13, 54); throne.put('y', 4, 54)            # the balcony from the sky bridge
throne.stand(8, 17, 9); throne.stand(52, 61, 9)
throne.ledge(56, 66, 2); throne.put('t', 62, 3)
throne.put('b', 44, 2); throne.put('m', 58, 3); throne.put('c', 34, 30)

cache_k = room("room_bastion_keep_eye", "Око башти", 282, 26, 24, flags="secret",
               keys={"w": "breakable explore-wall-bastion_keep_eye 8", "g": "gold 340 bastion-chest-keep",
                     "e": "spot eye-keep"})
cache_k.open_side('W', 2, 9); cache_k.put('w', 0, 2); cache_k.put('g', 10, 2); cache_k.put('e', 16, 2)

# ---------------- row 2 (y = 52): the casemate under the wall, the feather loft ----------------
casemate = room("room_bastion_west_casemate", "Західний каземат", 0, 52, 124,
                keys={"o": "spot oath-sentinel-casemate", "f": "spot frost-wraith-casemate",
                      "w": "spot snow-wolf-casemate", "g": "gold 160 bastion-casemate-purse"})
casemate.open_side('E', 2, 9)
casemate.open_side('N', 28, 37)          # up into the west tower
climb(casemate, [(7, 32, 35), (11, 24, 27), (15, 32, 35), (19, 40, 43), (23, 33, 36)])
casemate.put('g', 6, 2); casemate.put('o', 16, 2); casemate.put('f', 60, 6); casemate.put('w', 90, 2)

loft = room("room_bastion_feather_loft", "Горище з пером", 148, 52, 80, flags="optional",
            keys={"r": "spot crystal-dash-loft", "g": "gold 360 bastion-chest-loft", "o": "spot lost-wisp-loft",
                  "c": "spot rime-crawler-loft", "i": "spot ice-floor-loft"})
loft.open_side('W', 2, 9)
loft.ledge(30, 47, 1, '^')              # a crevasse of ice spikes — the crystal dash crosses it
loft.put('r', 26, 2); loft.put('c', 38, 23); loft.put('g', 66, 2); loft.put('o', 72, 2); loft.put('i', 12, 2)

# ---------------- row 1 (y = 78): the west tower, the wall, the spire, the sky bridge ----------------
cache_w = room("room_bastion_tower_eye", "Око західної вежі", 0, 78, 24, h=28, flags="secret",
               keys={"w": "breakable explore-wall-bastion_tower_eye 8", "g": "gold 380 bastion-chest-tower",
                     "k": "item frost-lens bastion-tower-relic", "e": "spot eye-west"})
cache_w.open_side('E', 2, 9); cache_w.put('w', 23, 2); cache_w.put('g', 6, 2); cache_w.put('k', 10, 2); cache_w.put('e', 15, 2)

wtower = room("room_bastion_west_tower", "Західна вежа", 24, 78, 24, h=28,
              keys={"f": "spot rampart-archer-tower", "i": "spot ice-floor-wtower"})
wtower.open_side('S', 4, 13); wtower.ledge(4, 8, 1)
wtower.open_side('W', 2, 9); wtower.open_side('E', 2, 9)
wtower.stand(14, 21, 9); wtower.put('f', 18, 9); wtower.put('i', 16, 2)

rampart = room("room_bastion_rampart", "Мури", 48, 78, 148, h=28,
               keys={"n": "spot inscription-rampart", "h": "spot trumpeter", "f": "spot rampart-archer-1",
                     "g": "spot rampart-archer-2", "o": "spot oath-sentinel-rampart", "p": "spot oath-sentinel-rampart-2",
                     "w": "spot wind-rampart", "b": "spot banners-rampart", "x": "spot frost-wraith-rampart",
                     "i": "spot ice-floor-rampart"})
rampart.open_side('W', 2, 9); rampart.open_side('E', 2, 9)
for c0 in (30, 74, 118):               # guard platforms behind the merlons
    rampart.stand(c0, c0 + 9, 7)
rampart.put('n', 8, 2); rampart.put('h', 14, 2); rampart.put('f', 34, 7); rampart.put('g', 122, 7)
rampart.put('o', 56, 2); rampart.put('p', 100, 2); rampart.put('w', 64, 12); rampart.put('b', 88, 14)
rampart.put('x', 110, 10); rampart.put('i', 46, 2)

spire = room("room_bastion_spire", "Шпиль вартових", 196, 78, 24, h=52, flags="exit=N",
             keys={"r": "ring", "x": "spot to-observatory", "c": "spot rime-crawler-spire", "i": "spot ice-floor-spire"})
spire.open_side('W', 2, 9); spire.open_side('E', 28, 35); spire.open_side('N', 9, 14)
climb(spire, [(7, 9, 14), (11, 2, 5), (15, 9, 14), (19, 18, 21), (23, 9, 14)])
spire.stand(15, 21, 28)
climb(spire, [(34, 9, 14), (39, 2, 5), (44, 9, 14)])
spire.put('r', 11, 48); spire.put('x', 12, 45); spire.put('c', 4, 30); spire.put('i', 6, 2)

sky = room("room_bastion_sky_bridge", "Небесний міст", 220, 104, 62,
           keys={"w": "spot wind-sky-bridge", "f": "spot frost-wraith-bridge", "i": "spot ice-floor-sky"})
sky.open_side('W', 2, 9); sky.open_side('E', 2, 9)
sky.put('w', 20, 8); sky.put('f', 40, 6); sky.put('i', 10, 2)

# ---------------- row 4 (y = 0) and below: the abyss bridge, the stair hall, the well, the cage, the chapel ----------------
abyss = room("room_bastion_abyss_bridge", "Міст над прірвою", 34, 0, 90, flags="optional",
             keys={"g": "gold 300 bastion-chest-bridge", "w": "spot snow-wolf-bridge", "v": "spot snow-wolf-bridge-2",
                   "f": "spot rampart-archer-bridge", "i": "spot ice-floor-bridge"})
abyss.open_side('E', 2, 9)
abyss.ledge(20, 25, 1, '^'); abyss.ledge(48, 53, 1, '^')      # broken spans: ice teeth below
abyss.put('g', 6, 2); abyss.put('w', 34, 2); abyss.put('v', 62, 2); abyss.put('i', 72, 2)
abyss.stand(10, 17, 8); abyss.put('f', 13, 8)

stairs = room("room_bastion_stair_hall", "Сходова зала", 148, 0, 50,
              keys={"c": "spot cracked-ice-stairs", "o": "spot oath-sentinel-stairs", "x": "spot frost-wraith-stairs"})
stairs.open_side('W', 2, 9); stairs.open_side('E', 2, 9)
stairs.ledge(30, 37, 2); stairs.ledge(32, 37, 3)             # the broken stair
stairs.put('c', 20, 2); stairs.put('o', 12, 2); stairs.put('x', 42, 8)

pit = room("room_bastion_wolf_pit", "Вовча яма", 158, -26, 40, flags="optional needs=ground-slam",
           keys={"m": "spot white-she-wolf", "r": "spot she-wolf-reward", "c": "spot pit-chains"})
pit.open_side('E', 2, 9)
pit.stand(6, 13, 9); pit.stand(24, 31, 9)
pit.put('m', 20, 2); pit.put('r', 9, 9); pit.put('c', 18, 18)

well = room("room_bastion_ice_well", "Крижаний колодязь", 198, -26, 24, h=52, flags="optional",
            keys={"t": "spot trial-ice-wall", "h": "shard mana bastion-ice-wall", "i": "spot ice-floor-well"})
well.open_side('W', 28, 35); well.open_side('E', 28, 35)
well.open_side('W', 2, 9); well.open_side('E', 2, 9)          # the pit grate (shortcut); the well eye
climb(well, [(7, 9, 14), (11, 2, 5), (15, 9, 14), (19, 18, 21), (23, 9, 14)])
bridge(well, 28, 9, 14)
climb(well, [(34, 2, 5), (39, 9, 14), (44, 18, 21)])
well.put('t', 12, 2); well.put('h', 20, 44); well.put('i', 4, 28)

cache_l = room("room_bastion_well_eye", "Око колодязя", 222, -26, 24, flags="secret",
               keys={"w": "breakable explore-wall-bastion_well_eye 8", "g": "gold 320 bastion-chest-well",
                     "e": "spot eye-well"})
cache_l.open_side('W', 2, 9); cache_l.put('w', 0, 2); cache_l.put('g', 10, 2); cache_l.put('e', 16, 2)

cage = room("room_bastion_cage_hall", "Зала клітки", 222, 0, 34,
            keys={"c": "spot hanging-cage-bastion", "o": "spot oath-sentinel-cage", "l": "spot rime-crawler-cage"},
            flags="optional")
cage.open_side('W', 2, 9); cage.open_side('E', 2, 9); cage.open_side('N', 2, 19)
cage.put('c', 24, 12); cage.put('o', 26, 2); cage.put('l', 28, 22)

chapel = room("room_bastion_east_chapel", "Північна каплиця", 256, 0, 50, flags="optional",
              keys={"o": "spot lost-wisp-chapel", "b": "spot banners-chapel", "f": "spot frost-wraith-chapel",
                    "k": "spot ice-knight-chapel"})
chapel.open_side('W', 2, 9); chapel.open_side('N', 2, 9)
chapel.put('b', 30, 12); chapel.put('k', 24, 2); chapel.put('f', 38, 8); chapel.put('o', 46, 2)

links = [
    "link room_bastion_rift room_bastion_cage_hall oneway",
    "link room_bastion_east_chapel room_bastion_east_keep shortcut=shortcut_bastion_keep_ladder from=room_bastion_east_keep",
    "link room_bastion_east_keep room_bastion_oath_throne door",
    "link room_bastion_sky_bridge room_bastion_oath_throne shortcut=shortcut_bastion_sky_gate from=room_bastion_oath_throne",
    "link room_bastion_stair_hall room_bastion_wolf_pit ability=ground-slam",
    "link room_bastion_wolf_pit room_bastion_ice_well shortcut=shortcut_bastion_pit_grate from=room_bastion_wolf_pit",
]

header = """# Крижаний Бастіон, по карті користувача (reference/maps/11_frozen_bastion.webp). ЧЕРНЕТКА з хмари (2026-10-19):
# згенеровано скриптом bastion.py і ще не перевірено LevelCheck — локальна сесія проганяє перевірку й правку.
# Формат — gamedesign/LEVEL_TOOL.md; зона — gamedesign/BASTION.md.
# Вхід — гірська дорога з Цитаделі (ключ Тюремника) на захід у Браму; origin тимчасовий, шов — локально.
# Головний шлях: Брама → Зала стягів (вівтар) → Центральна вежа → Зала Крижаного лицаря (вівтар) → Розлам
# (кришталевий ривок) → Східна башта (вівтар) → Тронна зала Присяжної.
# Дорога далі: вежа → Західний каземат → Західна вежа → Мури → Шпиль вартових (вихід на північ, Обсерваторія).
# Лід — спот ice-floor-* у кімнаті: усі підлоги цієї кімнати слизькі (локально — клітка або прапор кімнати).
zone zone_frozen_bastion
cell 0.5
origin 700 -600
abilities dash double-jump grapple crystal-dash
"""
here = os.path.dirname(os.path.abspath(__file__))
text = header + '\n' + '\n\n'.join(r.text() for r in rooms) + '\n\n' + '\n'.join(links) + '\n'
open(os.path.join(here, "bastion.txt"), "w", encoding="utf-8").write(text)

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
