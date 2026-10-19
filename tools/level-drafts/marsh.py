# Чернетка Чумних Боліт: кімнати описані тут, сітки малює скрипт. Джерело істини — marsh.txt.
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

def shaft_up(r, c0, top_hole, levels):
    """Alternating ledges under a ceiling hole: levels = list of (stand height, side 'L'|'R')."""
    for stand, side in levels:
        c = c0 if side == 'L' else c0 + 10
        r.ledge(c, c + 3, stand - 1)

# ---------------- middle row (y = 0): the main road ----------------
edge = room("room_marsh_edge", "Край болота", 0, 0, 50, flags="exit=W",
            keys={"s": "start", "z": "spot corpse-eater-edge", "t": "spot dead-tree"})
edge.open_side('W', 2, 9); edge.open_side('E', 2, 9)
edge.put('s', 6, 2); edge.put('t', 14, 2)
# a first taste of the poison: a narrow pool to jump, ledge over it
edge.ledge(24, 29, 1, '^'); edge.put('z', 38, 2)

pier = room("room_marsh_pier", "Причал на палях", 50, 0, 70,
            keys={"a": "altar marsh-altar-01 «Вівтар причалу»", "z": "spot corpse-eater-pier",
                  "l": "spot leech-pier", "b": "spot rotten-boards-pier"})
pier.open_side('W', 2, 9); pier.open_side('E', 2, 9); pier.open_side('N', 6, 19)
pier.put('a', 40, 2); pier.put('z', 58, 2); pier.put('l', 26, 2)
# up to the stilt hut: ledges alternating under the hole in the ceiling
shaft_up(pier, 6, None, [(6, 'L'), (10, 'R'), (14, 'L'), (18, 'R'), (22, 'L')])
pier.put('b', 30, 2)

road = room("room_marsh_rotten_road", "Гнила гать", 120, 0, 44,
            keys={"d": "spot plague-doctor-road", "b": "spot rotten-boards-road"})
road.open_side('W', 2, 9); road.open_side('E', 2, 9); road.open_side('S', 20, 29)
road.put('d', 36, 2); road.put('b', 12, 2)
# the drop to the graveyard is a hole in the floor; boards over its west half keep the road walkable
road.ledge(20, 24, 1)

tree = room("room_marsh_rotten_tree", "Гниле дерево", 164, 0, 70,
            keys={"c": "spot hanging-cage", "d": "spot plague-doctor-tree", "l": "spot leech-tree",
                  "p": "spot poison-plant-tree"})
tree.open_side('W', 2, 9); tree.open_side('E', 2, 9); tree.open_side('N', 50, 61); tree.open_side('S', 10, 20)
tree.put('c', 32, 12); tree.ledge(28, 37, 11)          # the cage hangs over a root platform
tree.put('d', 44, 2); tree.put('l', 24, 2); tree.put('p', 60, 2)
shaft_up(tree, 50, None, [(6, 'L'), (10, 'R'), (14, 'L'), (18, 'R'), (22, 'L')])
# boards over the hole down to the wreck, east half, so the road goes on
tree.ledge(16, 20, 1)

ruins = room("room_marsh_drowned_ruins", "Затоплені руїни", 234, 0, 60, flags="optional",
             keys={"r": "spot trial-rotten-boards", "g": "gold 160 marsh-ruins-purse"})
ruins.open_side('W', 2, 9)
# the trial «Гнилі містки»: poison all along the floor, boards and pillars over it
ruins.ledge(4, 55, 1, '^')
for c0, r in [(2, 2), (8, 4), (16, 5), (24, 4), (32, 6), (40, 5), (48, 4)]:
    ruins.ledge(c0, c0 + 4, r)
ruins.ledge(52, 57, 2); ruins.put('g', 54, 3); ruins.put('r', 4, 3)

# ---------------- upper row (y = 26): the hut, the saint, the witch, the lair ----------------
hut = room("room_marsh_stilt_hut", "Хижа на палях", 50, 26, 56,
           keys={"k": "spot ferryman-hut", "z": "spot leech-hut"})
hut.open_side('S', 6, 19); hut.open_side('E', 2, 9)
hut.put('k', 32, 2); hut.put('z', 46, 2)

chest = room("room_marsh_chest_nook", "Закуток зі скринею", 106, 26, 30,
             keys={"g": "gold 240 marsh-chest-nook", "o": "spot rope-bridge-chest"})
chest.open_side('W', 2, 9); chest.open_side('E', 2, 9)
chest.put('g', 14, 2); chest.put('o', 22, 2)

shrine = room("room_marsh_shrine", "Святиня Утопленої", 136, 26, 60,
              keys={"a": "altar marsh-altar-02 «Вівтар святині»", "x": "spot drowned-saint-statue",
                    "d": "spot plague-doctor-shrine"})
shrine.open_side('W', 2, 9); shrine.open_side('E', 2, 9)
shrine.put('x', 30, 2); shrine.put('a', 22, 2); shrine.put('d', 48, 2)

witch = room("room_marsh_witch_hut", "Хата відьми", 196, 26, 40,
             keys={"y": "spot yaryna", "f": "spot witch-fire"})
witch.open_side('W', 2, 9); witch.open_side('E', 2, 9); witch.open_side('S', 18, 29)
witch.put('y', 8, 2); witch.put('f', 12, 2)
# the floor of the hut is boards over the hole to the tree, east half kept
witch.ledge(24, 29, 1)

stair = room("room_marsh_lair_stair", "Сходи до лігва", 236, 26, 24,
             keys={"l": "spot leech-stair"})
stair.open_side('W', 2, 9); stair.open_side('E', 2, 9); stair.open_side('N', 4, 15)
stair.put('l', 18, 2)
shaft_up(stair, 4, None, [(6, 'L'), (10, 'R'), (14, 'L'), (18, 'R'), (22, 'L')])

lair = room("room_marsh_stag_lair", "Лігво Чумного Оленя", 260, 26, 84, flags="boss=plague-stag",
            keys={"b": "spot plague-stag", "m": "spot mask-reward"})
lair.open_side('W', 2, 9)
lair.put('b', 60, 2); lair.put('m', 72, 2)
for c0, r in [(14, 7), (34, 9), (56, 7)]:
    lair.ledge(c0, c0 + 7, r)                       # roots to dodge the antler sweep on

# ---------------- top row (y = 52): the ruined chapel ----------------
chapel = room("room_marsh_chapel", "Зруйнована каплиця", 200, 52, 60, flags="optional",
              keys={"h": "shard health marsh-chapel", "d": "spot plague-doctor-chapel", "v": "spot chapel-window"})
chapel.open_side('S', 40, 51); chapel.open_side('E', 2, 9)
chapel.put('h', 12, 2); chapel.put('d', 24, 2); chapel.put('v', 30, 10)
chapel.ledge(47, 51, 1)    # a lip by the hole so the chapel floor stays whole to the west

bridge = room("room_marsh_north_bridge", "Північна кладка", 260, 52, 50, flags="optional",
              keys={"z": "spot leech-bridge"})
bridge.open_side('W', 2, 9); bridge.open_side('E', 2, 9)
bridge.ledge(10, 39, 1, '^')
bridge.ledge(8, 41, 3)          # the rope bridge over the poison
bridge.put('z', 30, 4)

nest = room("room_marsh_chapel_cache", "Схрон за каплицею", 310, 52, 24, flags="secret",
            keys={"w": "breakable explore-wall-marsh_chapel_cache 8", "g": "gold 300 marsh-chest-chapel",
                  "k": "shard mana marsh-chapel-cache"})
nest.open_side('W', 2, 9); nest.put('w', 0, 2); nest.put('g', 10, 2); nest.put('k', 16, 2)

# ---------------- lower row (y = -26): the graveyard, the mushrooms, the wreck ----------------
cave_cache = room("room_marsh_mushroom_cache", "Схрон під грибами", 36, -26, 24, flags="secret",
                  keys={"w": "breakable explore-wall-marsh_mushroom_cache 8", "g": "gold 200 marsh-chest-mushroom",
                        "h": "shard health marsh-mushroom-cache"})
cave_cache.open_side('E', 2, 9); cave_cache.put('w', 23, 2); cave_cache.put('g', 8, 2); cave_cache.put('h', 14, 2)

cave = room("room_marsh_mushroom_cave", "Грибна печера", 60, -26, 50, flags="optional",
            keys={"p": "spot poison-plant-cave", "q": "spot poison-plant-cave-2", "u": "spot glowing-mushrooms"})
cave.open_side('W', 2, 9); cave.open_side('E', 2, 9)
cave.put('p', 18, 2); cave.put('q', 34, 2); cave.put('u', 26, 2)

grave = room("room_marsh_graveyard", "Затоплений цвинтар", 110, -26, 54, flags="optional",
             keys={"o": "ring", "z": "spot corpse-eater-grave", "x": "spot sunk-graves"})
grave.open_side('W', 2, 9); grave.open_side('E', 2, 9); grave.open_side('N', 30, 39)
grave.put('z', 20, 2); grave.put('x', 10, 2)
# back up to the road: ledges, and a ring over the last one for the grapple
shaft_up(grave, 30, None, [(6, 'L'), (10, 'R'), (14, 'L'), (18, 'R'), (22, 'L')])
grave.put('o', 34, 22)

wreck = room("room_marsh_wreck", "Затонулий корабель", 164, -26, 90, flags="optional",
             keys={"a": "altar marsh-altar-03 «Вівтар на кормі»", "o": "ring", "z": "spot corpse-eater-wreck",
                   "k": "spot sunken-ship", "l": "spot leech-wreck"})
wreck.open_side('W', 2, 9); wreck.open_side('N', 10, 20); wreck.open_side('E', 2, 9)
# black water under the hull; the ship's deck and the pier are the floor
wreck.ledge(24, 69, 1, '^')
wreck.ledge(24, 69, 4)                 # the deck over the water
wreck.put('k', 46, 5); wreck.put('a', 56, 5); wreck.put('z', 36, 5); wreck.put('l', 64, 5)
shaft_up(wreck, 10, None, [(6, 'L'), (10, 'R'), (14, 'L'), (18, 'R'), (22, 'L')])
wreck.put('o', 78, 14); wreck.ledge(80, 85, 9)    # the mast top, reached by the ring

drain = room("room_marsh_toxic_drain", "Отруйний стік", 254, -26, 40, flags="optional needs=plague-mask exit=E",
             keys={"m": "spot mask-water"})
drain.open_side('W', 2, 9); drain.open_side('E', 2, 9)
# deep poison across the whole floor: only the mask lets anyone walk it
drain.ledge(2, 37, 1, '^'); drain.ledge(2, 37, 2, '^'); drain.put('m', 4, 3)

drain_cache = room("room_marsh_hold_cache", "Схрон у трюмі", 294, -26, 24, flags="secret needs=plague-mask",
                   keys={"w": "breakable explore-wall-marsh_hold_cache 8", "g": "gold 280 marsh-chest-hold",
                         "h": "shard mana marsh-hold-cache"})
drain_cache.open_side('W', 2, 9); drain_cache.put('w', 0, 2); drain_cache.put('g', 10, 2); drain_cache.put('h', 16, 2)

links = [
    "link room_marsh_lair_stair room_marsh_stag_lair door",
    "link room_marsh_wreck room_marsh_toxic_drain ability=plague-mask",
]

header = """# Чумні Болота, по карті користувача (reference/maps/05_plague_marsh.webp). ЧЕРНЕТКА з хмари (2026-10-19):
# згенеровано скриптом і ще не перевірено LevelCheck — локальна сесія проганяє перевірку й правку.
# Формат — gamedesign/LEVEL_TOOL.md; зона — gamedesign/MARSH.md.
# Стоїть на схід від Села: Дорога на болота (village.txt) відчиняється на захід у Край болота.
# Головний шлях: Край болота → Причал на палях (вівтар) → Хижа на палях → Закуток зі скринею →
# Святиня Утопленої (вівтар) → Хата відьми (Ярина) → Сходи до лігва → Лігво Чумного Оленя.
# Друга дорога: Гнила гать → Гниле дерево → угору в Хату відьми. Отрута (^) — ядовита вода: до маски шкодить.
zone zone_plague_marsh
cell 0.5
origin 523 51
abilities dash double-jump grapple
"""
text = header + '\n' + '\n\n'.join(r.text() for r in rooms) + '\n\n' + '\n'.join(links) + '\n'
open("marsh.txt", "w", encoding="utf-8").write(text)

# --- sanity: rooms do not overlap; every open edge cell faces an open cell of a neighbour or an exit ---
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
print('\n'.join(sorted(set(problems))) or 'edges ok')
print(len(rooms), 'rooms')
