
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
