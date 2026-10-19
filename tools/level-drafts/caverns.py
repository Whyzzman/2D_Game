# Чернетка Кришталевих Печер: кімнати описані тут, сітки малює скрипт. Джерело істини — caverns.txt.
# Хмарна чернетка (2026-10-19): LevelCheck не запускався. Запуск: python3 caverns.py (пише caverns.txt поруч).
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

# =========================== west: the mine mouth, drifts, the old face, the shrine (floors 2 / 26 / 50 / 74) ===========================
mouth = room("room_caverns_mouth", "Гирло шахти", 0, 0, 44, 24, flags="exit=S",
             keys={"s": "start", "o": "ring", "i": "spot inscription-mouth", "b": "spot cave-bat-mouth"})
drift = room("room_caverns_lower_drift", "Нижній штрек", 44, 0, 52, 24,
             keys={"m": "spot dead-miner-drift", "k": "spot crystal-beetle-drift", "r": "spot mine-rails-drift"})
vein = room("room_caverns_amethyst_vein", "Аметистова жила", 96, 0, 24, 24,
            keys={"i": "spot inscription-walled-drift", "l": "spot walled-drift-lever"})
face = room("room_caverns_old_face", "Старий забій", 20, 24, 60, 24,
            keys={"m": "spot dead-miner-face", "n": "spot dead-miner-face-2", "t": "spot shift-tally-board",
                  "g": "item shift-tally caverns-shift-tally", "b": "spot cave-bat-face"})
timber = room("room_caverns_timber_hall", "Зала кріплення", 80, 24, 40, 24, flags="optional",
              keys={"s": "spot geode-slug-timber", "g": "gold 180 caverns-timber-purse", "l": "spot stalactite-lurker-timber"})
shrine = room("room_caverns_crystal_shrine", "Кришталевий вівтар", 0, 48, 56, 24,
              keys={"a": "altar caverns-altar-01 «Кришталевий вівтар»", "l": "spot shrine-lock-lever",
                    "c": "spot crystal-pedestal"})
drip = room("room_caverns_dripping_hall", "Крапельня", 56, 48, 64, 24,
            keys={"k": "spot crystal-beetle-drip", "p": "spot prism-eye-drip", "s": "spot geode-slug-drip",
                  "l": "spot stalactite-lurker-drip"})
scaffold = room("room_caverns_upper_scaffold", "Верхні риштування", 60, 72, 60, 24, flags="optional",
                keys={"h": "shard health caverns-falls-top", "b": "spot cave-bat-scaffold", "c": "spot cave-bat-scaffold-2",
                      "e": "spot lost-wisp-scaffold"})
geode = room("room_caverns_geode_cache", "Схрон у жеоді", 36, 72, 24, 24, flags="secret",
             keys={"w": "breakable explore-wall-caverns_geode_cache 8", "g": "gold 260 caverns-chest-geode",
                   "l": "item miner-lamp caverns-miner-lamp"})
trial = room("room_caverns_crystal_path", "Кришталевий шлях", -64, 0, 64, 24, flags="optional needs=crystal-dash",
             keys={"t": "spot trial-crystal-path", "k": "shard mana caverns-crystal-path", "c": "spot trial-crystals"})
# =========================== the waterfall trunk and the lake under it ===========================
shaft = room("room_caverns_waterfall_shaft", "Водоспад-стовбур", 120, 0, 16, 112,
             keys={"i": "spot inscription-scaffold", "p": "spot prism-eye-shaft", "b": "spot cave-bat-shaft",
                   "w": "spot waterfall-trunk"})
lake = room("room_caverns_lake", "Підземне озеро", 40, -27, 160, 27, flags="optional",
            keys={"i": "spot inscription-lake", "m": "spot dead-miner-lake", "s": "spot geode-slug-lake",
                  "g": "gold 220 caverns-miners-pay", "q": "spot deep-water-lake"})
lshrine = room("room_caverns_lower_shrine", "Нижній вівтар", 200, -27, 44, 27, flags="optional",
               keys={"a": "altar caverns-altar-02 «Вівтар над озером»", "l": "spot lake-lock-lever"})
foreman = room("room_caverns_flooded_face", "Затоплений забій", 244, -27, 56, 27, flags="optional",
               keys={"f": "spot mine-foreman", "r": "spot foreman-reward", "c": "spot ore-cart"})
# =========================== east: the still lake, the cascade, the far drift, the rift to the Citadel ===========================
still = room("room_caverns_still_lake", "Тихе озеро", 136, 0, 80, 51, flags="optional",
             keys={"k": "spot crystal-beetle-still", "l": "spot stalactite-lurker-still", "q": "spot still-water"})
dcache = room("room_caverns_vein_cache", "Схрон у жилі", 216, 0, 24, 24, flags="secret",
              keys={"w": "breakable explore-wall-caverns_vein_cache 8", "g": "gold 240 caverns-chest-vein",
                    "e": "spot lost-wisp-vein"})
cascade = room("room_caverns_cascade", "Каскад", 136, 51, 64, 24,
               keys={"k": "spot crystal-beetle-cascade", "p": "spot prism-eye-cascade", "f": "spot cascade-head"})
far = room("room_caverns_far_seam", "Далекий штрек", 200, 51, 60, 24, flags="optional",
           keys={"m": "spot dead-miner-far", "s": "spot geode-slug-far", "t": "spot crystal-springboards"})
rift = room("room_caverns_citadel_rift", "Розлам до Цитаделі", 260, 51, 40, 24, flags="optional needs=crystal-dash exit=E",
            keys={"r": "spot rift-crystals"})
useam = room("room_caverns_upper_seam", "Верхній забій", 136, 75, 60, 24, flags="optional",
             keys={"m": "spot dead-miner-useam", "l": "spot stalactite-lurker-useam", "b": "spot cave-bat-useam"})
channel = room("room_caverns_lair_channel", "Шлюз під лігвом", 196, 75, 40, 24, flags="optional",
               keys={"l": "spot lair-lock-lever", "f": "spot lair-falls-foot"})
pcache = room("room_caverns_prism_cache", "Схрон за водоспадом", 236, 75, 24, 24, flags="secret",
              keys={"w": "breakable explore-wall-caverns_prism_cache 8", "g": "gold 300 caverns-chest-prism",
                    "i": "item ring_of_endurance caverns-prism-ring"})
approach = room("room_caverns_lair_approach", "Підступ до лігва", 136, 99, 40, 24,
                keys={"i": "spot inscription-lair", "k": "spot crystal-beetle-approach", "p": "spot prism-eye-approach"})
lair = room("room_caverns_beast_lair", "Лігво Кришталевого Звіра", 176, 99, 84, 44, flags="boss=crystal-beast",
            keys={"b": "spot crystal-beast", "r": "spot crystal-dash-reward", "t": "spot lair-bridge"})

# =========================== doors ===========================
door(mouth, drift, 2, 9)
door(drift, vein, 2, 9)                 # the walled drift: lever on the vein side (shortcut)
door(vein, shaft, 2, 9)
door(mouth, trial, 2, 9)
door(drift, face, 60, 71)
door(face, timber, 26, 33)
door(timber, shaft, 26, 33)
door(face, shrine, 24, 35)
door(shrine, drip, 50, 57)              # the shrine lock: lever on the shrine side
door(drip, shaft, 50, 57)
door(scaffold, shaft, 74, 81)
door(geode, scaffold, 74, 81)
door(lake, shaft, 122, 133)
door(lake, lshrine, -25, -18)           # the lake lock: lever on the shrine side
door(lshrine, foreman, -25, -18)
door(shaft, still, 29, 36)
door(still, lshrine, 202, 207)          # one-way drop beside the still lake's fall
door(still, dcache, 2, 9)
door(shaft, cascade, 53, 60)
door(cascade, still, 180, 185)          # the cascade falls into the still lake (one-way)
door(cascade, far, 53, 60)
door(far, rift, 53, 60)
exit_side(rift, 'E', 2, 9)
door(shaft, useam, 77, 84)
door(useam, channel, 77, 84)            # the lair lock: lever on the channel side
door(channel, pcache, 77, 84)
door(shaft, approach, 101, 108)
door(approach, lair, 101, 108)
door(lair, channel, 228, 233)           # after the beast: its crystal plug breaks, a drop to the channel

# =========================== ladders ===========================
# The trunk: from the lake floor (-25) to the lair approach (101); W rooms open on L ledges, E rooms on R ledges.
t_st = ladder(122, steps(-22, -1) + steps(2, 101))
for st, want in [(2, 'L'), (26, 'L'), (50, 'L'), (74, 'L'), (29, 'R'), (53, 'R'), (77, 'R'), (101, 'R')]:
    assert t_st[st] == want, (st, t_st[st])
ladder(60, steps(5, 23))                # lower drift -> old face (floor 26)
ladder(24, steps(29, 47))               # old face -> shrine (floor 50)
ladder(146, steps(5, 26))               # still lake floor -> the scaffold bank at 29
still.ledge(2, 15, 28)                  # timber bank out of the trunk over the still lake
mouth.ledge(14, 17, 4)                  # exit S: a ledge beside the hole, the ring over it (the Forest side climbs in)

# =========================== contents ===========================
exit_side(mouth, 'S', 6, 13)
mouth.put('o', 10, 14); mouth.put('s', 22, 2); mouth.put('i', 30, 2); mouth.put('b', 36, 16)
drift.put('m', 20, 2); drift.put('k', 40, 2); drift.put('r', 8, 2)
vein.put('l', 6, 2); vein.put('i', 14, 2)
face.put('m', 24, 2); face.put('n', 52, 2); face.put('t', 34, 2); face.put('g', 36, 2); face.put('b', 30, 16)
timber.put('s', 12, 2); timber.put('g', 24, 2); timber.put('l', 30, 18)
timber.ledge(10, 17, 7); timber.ledge(22, 29, 9)           # cribbing to climb over
shrine.put('a', 12, 2); shrine.put('c', 16, 2); shrine.put('l', 50, 2)
drip.put('k', 14, 2); drip.put('p', 30, 16); drip.put('s', 44, 2); drip.put('l', 22, 19)
drip.ledge(24, 31, 7); drip.ledge(36, 43, 10)               # crystal shelves: the prism eye watches the floor between
scaffold.put('h', 6, 2); scaffold.put('b', 24, 16); scaffold.put('c', 40, 16); scaffold.put('e', 14, 2)
scaffold.ledge(20, 27, 6); scaffold.ledge(32, 39, 9)
geode.put('w', geode.w - 2, 2); geode.put('g', 8, 2); geode.put('l', 14, 2)
# the trial: a crystal chasm right behind the door — only the charged dash crosses it (spikes until then)
trial.fill(36, 57, 1, 1, '^'); trial.fill(36, 57, 0, 0, '#')
for c0, r in [(28, 6), (18, 10), (8, 14)]:
    trial.ledge(c0, c0 + 3, r)                              # crystal perches the dash bounces between
trial.put('t', 60, 2); trial.put('c', 30, 2); trial.put('k', 9, 15)
shaft.put('i', 11, 6); shaft.put('p', 7, 40); shaft.put('b', 7, 70); shaft.put('w', 7, 90)
# the lake: a broad shore; the deep middle is water to swim with depths-breath later (decoration here)
lake.put('i', 30, 2); lake.put('m', 50, 2); lake.put('s', 110, 2); lake.put('g', 140, 2); lake.put('q', 100, 2)
lake.ledge(60, 67, 6); lake.ledge(100, 107, 6)              # scaffold stairs to the trunk
lshrine.put('a', 20, 2); lshrine.put('l', 6, 2)
foreman.put('f', 36, 2); foreman.put('r', 48, 2); foreman.put('c', 14, 2)
foreman.ledge(12, 19, 8); foreman.ledge(36, 43, 8)          # ore chutes to dodge the drill charge on
still.put('k', 30, 2); still.put('l', 50, 30); still.put('q', 58, 2)
dcache.put('w', 0, 2); dcache.put('g', 10, 2); dcache.put('e', 16, 2)
cascade.put('k', 20, 2); cascade.put('p', 30, 16); cascade.put('f', 42, 2)
far.put('m', 20, 2); far.put('s', 40, 2); far.put('t', 30, 2)
far.ledge(12, 17, 7); far.ledge(26, 31, 10); far.ledge(42, 47, 7)   # crystal springboards (decoration until the dash)
rift.fill(4, 25, 1, 1, '^')            # the rift: 22 cells of crystal spikes, the Citadel beyond
rift.put('r', 30, 2)
useam.put('m', 20, 2); useam.put('l', 34, 18); useam.put('b', 46, 16)
channel.put('l', 6, 2); channel.put('f', 30, 2)
pcache.put('w', 0, 2); pcache.put('g', 10, 2); pcache.put('i', 16, 2)
approach.put('i', 30, 2); approach.put('k', 16, 2); approach.put('p', 24, 14)
# the lair: a wide floor, three crystal springboards, a bridge over the falls in the middle
lair.put('b', 50, 2); lair.put('r', 62, 2); lair.put('t', 40, 2)
for c0, r in [(14, 7), (36, 12), (62, 7)]:
    lair.ledge(c0, c0 + 5, r)

links = [
    "link room_caverns_lower_drift room_caverns_amethyst_vein shortcut=shortcut_caverns_walled_drift from=room_caverns_amethyst_vein",
    "link room_caverns_crystal_shrine room_caverns_dripping_hall shortcut=shortcut_caverns_shrine_lock from=room_caverns_crystal_shrine",
    "link room_caverns_lake room_caverns_lower_shrine shortcut=shortcut_caverns_lake_lock from=room_caverns_lower_shrine",
    "link room_caverns_upper_seam room_caverns_lair_channel shortcut=shortcut_caverns_lair_lock from=room_caverns_lair_channel",
    "link room_caverns_still_lake room_caverns_lower_shrine oneway",
    "link room_caverns_cascade room_caverns_still_lake oneway",
    "link room_caverns_beast_lair room_caverns_lair_channel oneway",
    "link room_caverns_lair_approach room_caverns_beast_lair door",
    "link room_caverns_mouth room_caverns_crystal_path ability=crystal-dash",
    "link room_caverns_far_seam room_caverns_citadel_rift ability=crystal-dash",
]

header = """# Кришталеві Печери, по карті користувача (reference/maps/07_crystal_caverns.webp). ЧЕРНЕТКА з хмари (2026-10-19):
# згенеровано скриптом caverns.py і ще не перевірено LevelCheck — локальна сесія проганяє перевірку й правку.
# Формат — gamedesign/LEVEL_TOOL.md; зона — gamedesign/CAVERNS.md.
# Origin тимчасовий: шов із Лісом (Дупло мисливця, кільця гака) локальна сесія ставить сама; вхід — люк у Гирлі шахти.
# Головний шлях: Гирло шахти → Нижній штрек → Старий забій → Кришталевий вівтар (вівтар, засув) → Крапельня →
# Водоспад-стовбур (риштування) → Підступ до лігва → Лігво Кришталевого Звіра.
# Друга дорога: Старий забій → Зала кріплення → стовбур. Замки карти — засуви з одного боку (shortcut).
zone zone_crystal_caverns
cell 0.5
origin -100 -300
abilities dash double-jump grapple
"""
text = header + '\n' + '\n\n'.join(r.text() for r in rooms) + '\n\n' + '\n'.join(links) + '\n'
here = os.path.dirname(os.path.abspath(__file__))
open(os.path.join(here, "caverns.txt"), "w", encoding="utf-8").write(text)

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
