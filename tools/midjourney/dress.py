#!/usr/bin/env python3
"""Stands the painted props of a zone (Nano Banana, 2026-11-11) in its rooms, on the near layer.

  python tools/midjourney/dress.py tomb|forest|village|capital|marsh     -> every room of the zone
  python tools/midjourney/dress.py tomb room_tomb_entrance

Pictures: design/levels/midjourney/<zone>/props/prop_<zone>_<name>.png, one object each on flat magenta.
They are cut (sprites.py `cut`), graded a step darker than the playfield and pasted on
Assets/Game/Art/Levels/<Zone>/Rooms/<room>_near.png: standing things on the stone floor of the room's text
(Assets/Game/Levels/<zone>.txt), hanging ones from its ceiling, cobwebs in its upper corners. The near layer
is rebuilt from place.py's dark frame first, so running this again gives the same picture.
Every room draws its own mix from its own seed and no room shares another's set (WORLD rule: nothing
repeats from room to room). Order after a change of the text: render.js → place.py → dress.py, then in Unity
ContentBuilder.RefreshLevelArt (gamedesign/LEVEL_TOOL.md, «Нарисованный уровень»).
"""
import hashlib
import random
import re
import subprocess
import sys
from pathlib import Path

from PIL import Image, ImageEnhance

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(Path(__file__).resolve().parent))
from sprites import cut  # noqa: E402
from place import border  # noqa: E402

PPU = 48  # the near layer (tools/level-art/jobs.js NEAR_PPU)
# name: (height in world units, how it sits, weight). Heights beside the hero (2.2 units tall).
PROPS = {
    "tomb": {
        "effigy_statue": (3.6, "stand", 1.0),
        "mourner_statue": (3.4, "stand", 1.0),
        "sarcophagus": (1.5, "stand", 0.9),
        "candelabra": (2.5, "stand", 1.2),
        "broken_column": (3.2, "stand", 0.8),
        "crown_altar": (1.3, "stand", 0.5),
        "urn": (1.3, "stand", 1.1),
        "bone_pile": (0.85, "stand", 1.1),
        "banner": (3.0, "hang", 1.0),
        "chains": (3.4, "hang", 1.0),
        "roots": (3.0, "hang", 0.8),
        "cobweb": (1.7, "corner", 1.0),
        # second set (2026-11-13): stands once its picture is drawn (Sprites.has)
        "iron_lamp": (3.0, "stand", 0.9),
        "kneeling_knight": (2.6, "stand", 0.7),
        "stone_bench": (1.0, "stand", 0.8),
        "broken_coffin": (1.1, "stand", 0.9),
        "offering_bowl": (0.9, "stand", 0.9),
        "grave_slab": (1.6, "stand", 0.9),
        "censer": (2.4, "hang", 0.9),
        "iron_cage": (2.6, "hang", 0.7),
    },
    "forest": {
        "stump": (1.6, "stand", 1.1),
        "gravestone": (1.9, "stand", 1.0),
        "abbey_arch": (4.2, "stand", 0.7),
        "hunter_shrine": (2.4, "stand", 0.6),
        "mushrooms": (1.1, "stand", 1.0),
        "fallen_log": (1.0, "stand", 1.0),
        "cart_wheel": (1.3, "stand", 0.7),
        "thorns": (0.9, "stand", 1.0),
        "deer_skull": (2.6, "stand", 0.6),
        "vines": (3.2, "hang", 1.0),
        "roots": (3.0, "hang", 0.9),
        "cobweb": (1.6, "corner", 0.8),
        # second set (2026-11-13): stands once its picture is drawn (Sprites.has)
        "lamp_post": (3.0, "stand", 0.8),
        "signpost": (2.2, "stand", 0.7),
        "fern": (1.2, "stand", 1.2),
        "broken_cart": (1.6, "stand", 0.6),
        "hollow_log": (1.1, "stand", 0.9),
        "rune_stone": (2.4, "stand", 0.7),
        "dead_bush": (1.4, "stand", 1.0),
        "cocoon": (2.0, "hang", 0.7),
    },
    "village": {
        "barrels": (1.4, "stand", 1.1),
        "cart": (1.6, "stand", 0.8),
        "well": (2.3, "stand", 0.6),
        "fence": (1.1, "stand", 1.0),
        "fallen_bell": (1.8, "stand", 0.5),
        "scarecrow": (2.8, "stand", 0.6),
        "wayside_cross": (2.6, "stand", 0.7),
        "brazier": (1.7, "stand", 0.9),
        "jugs": (1.0, "stand", 1.1),
        "lantern": (2.6, "hang", 1.0),
        "herbs": (2.2, "hang", 0.9),
        "cobweb": (1.6, "corner", 0.8),
        # second set (2026-11-13): stands once its picture is drawn (Sprites.has)
        "hay_stack": (1.6, "stand", 0.9),
        "signpost": (2.3, "stand", 0.7),
        "wheelbarrow": (1.2, "stand", 0.8),
        "woodpile": (1.1, "stand", 1.0),
        "market_stall": (2.6, "stand", 0.6),
        "bench": (0.9, "stand", 0.8),
        "laundry_line": (1.6, "hang", 0.8),
        "hanging_sign": (1.8, "hang", 0.7),
    },
    # a prop whose picture is not drawn yet is left out (Sprites.has): 7 of 12 on 2026-11-11 (Gemini's limit)
    "capital": {
        "angel_statue": (3.6, "stand", 0.8),
        "armor_stand": (2.6, "stand", 1.0),
        "weapon_rack": (1.8, "stand", 1.0),
        "gilded_brazier": (2.2, "stand", 1.0),
        "barricade": (1.6, "stand", 0.8),
        "fallen_head": (1.7, "stand", 0.5),
        "rubble": (0.9, "stand", 1.2),
        "shields": (1.1, "stand", 1.0),
        "lectern": (2.0, "stand", 0.6),
        "crimson_banner": (3.0, "hang", 1.0),
        "chandelier": (2.4, "hang", 0.8),
        "cage": (2.8, "hang", 0.7),
        # second set (2026-11-13): stands once its picture is drawn (Sprites.has)
        "street_lamp": (3.2, "stand", 0.9),
        "broken_fountain": (2.2, "stand", 0.5),
        "toppled_column": (1.2, "stand", 0.9),
        "royal_urn": (1.5, "stand", 0.9),
        "stone_bench": (1.0, "stand", 0.8),
        "iron_fence": (1.4, "stand", 0.9),
        "hanging_lantern": (2.2, "hang", 0.9),
        "torn_tapestry": (3.2, "hang", 0.8),
    },
    "marsh": {
        "rotten_stump": (1.7, "stand", 1.1),
        "leaning_cross": (2.4, "stand", 0.8),
        "reeds": (1.5, "stand", 1.2),
        "sunken_boat": (1.2, "stand", 0.6),
        "swamp_lantern": (2.8, "stand", 0.8),
        "bone_totem": (2.6, "stand", 0.6),
        "nets": (1.9, "stand", 0.8),
        "glow_fungus": (1.0, "stand", 1.0),
        "witch_cauldron": (1.4, "stand", 0.5),
        "moss": (2.6, "hang", 1.0),
        "charm_bottles": (2.2, "hang", 0.8),
        "roots": (3.0, "hang", 0.9),
        # second set (2026-11-13): stands once its picture is drawn (Sprites.has)
        "dead_tree": (3.4, "stand", 0.8),
        "plank_fence": (1.2, "stand", 1.0),
        "fish_rack": (2.0, "stand", 0.7),
        "mossy_idol": (2.2, "stand", 0.6),
        "drowned_cart": (1.3, "stand", 0.6),
        "skull_pole": (2.4, "stand", 0.7),
        "cocoon": (2.2, "hang", 0.7),
        "fishing_lantern": (2.0, "hang", 0.8),
    },
}
# Rooms by character: what may stand there (others are skipped in that room).
CAVE_WORDS = ("cache", "nest", "slope", "collapsed", "falls", "flooded", "drowned", "cell", "lower")
ALLOWED = {
    "tomb": {
        "arena": ["effigy_statue", "mourner_statue", "banner", "chains", "candelabra", "cobweb",
                 "iron_lamp", "kneeling_knight", "stone_bench", "broken_coffin", "offering_bowl", "grave_slab", "censer", "iron_cage"],
        "secret": ["urn", "bone_pile", "crown_altar", "cobweb", "candelabra", "roots"],
        "cave": ["bone_pile", "broken_column", "urn", "roots", "chains", "cobweb", "sarcophagus",
                 "iron_lamp", "kneeling_knight", "stone_bench", "broken_coffin", "offering_bowl", "grave_slab", "censer", "iron_cage"],
        "hall": list(PROPS["tomb"]),
    },
    "forest": {
        # under the open sky the stone above is only the frame: nothing hangs from it (front.js rule)
        "open": ["stump", "gravestone", "abbey_arch", "hunter_shrine", "mushrooms", "fallen_log", "cart_wheel",
                 "thorns", "deer_skull",
                 "lamp_post", "signpost", "fern", "broken_cart", "hollow_log", "rune_stone", "dead_bush"],
        "enclosed": ["stump", "gravestone", "fallen_log", "mushrooms", "thorns", "deer_skull", "vines", "roots",
                     "cobweb", "hunter_shrine",
                 "lamp_post", "signpost", "fern", "broken_cart", "hollow_log", "rune_stone", "dead_bush", "cocoon"],
        "underground": ["mushrooms", "roots", "cobweb", "stump", "fallen_log",
                 "lamp_post", "signpost", "fern", "broken_cart", "hollow_log", "rune_stone", "dead_bush", "cocoon"],
        "poison": ["mushrooms", "thorns", "deer_skull", "stump", "vines", "roots",
                 "lamp_post", "signpost", "fern", "broken_cart", "hollow_log", "rune_stone", "dead_bush", "cocoon"],
        "arena": ["gravestone", "abbey_arch", "deer_skull", "thorns", "stump",
                 "lamp_post", "signpost", "fern", "broken_cart", "hollow_log", "rune_stone", "dead_bush"],
        "secret": ["mushrooms", "cart_wheel", "cobweb", "roots", "fallen_log"],
    },
    "village": {
        "open": ["barrels", "cart", "well", "fence", "scarecrow", "wayside_cross", "brazier", "jugs",
                 "hay_stack", "signpost", "wheelbarrow", "woodpile", "market_stall", "bench"],
        "stone": ["fallen_bell", "brazier", "jugs", "barrels", "cobweb", "lantern", "wayside_cross",
                 "hay_stack", "signpost", "wheelbarrow", "woodpile", "market_stall", "bench", "laundry_line", "hanging_sign"],
        "house": ["barrels", "jugs", "herbs", "lantern", "cobweb", "brazier", "fence",
                 "hay_stack", "signpost", "wheelbarrow", "woodpile", "market_stall", "bench", "laundry_line", "hanging_sign"],
        "arena": ["fallen_bell", "brazier", "lantern", "wayside_cross", "barrels",
                 "hay_stack", "signpost", "wheelbarrow", "woodpile", "market_stall", "bench", "laundry_line", "hanging_sign"],
        "secret": ["jugs", "barrels", "cobweb", "herbs"],
    },
    "capital": {
        "open": ["angel_statue", "barricade", "fallen_head", "rubble", "gilded_brazier", "weapon_rack", "shields",
                 "street_lamp", "broken_fountain", "toppled_column", "royal_urn", "stone_bench", "iron_fence"],
        "crypts": ["angel_statue", "gilded_brazier", "rubble", "armor_stand", "weapon_rack", "fallen_head", "lectern",
                   "cage", "chandelier",
                 "street_lamp", "broken_fountain", "toppled_column", "royal_urn", "stone_bench", "iron_fence", "hanging_lantern", "torn_tapestry"],
        "inside": ["armor_stand", "weapon_rack", "gilded_brazier", "rubble", "angel_statue", "lectern", "shields",
                   "crimson_banner", "chandelier", "cage",
                 "street_lamp", "broken_fountain", "toppled_column", "royal_urn", "stone_bench", "iron_fence", "hanging_lantern", "torn_tapestry"],
        "arena": ["angel_statue", "gilded_brazier", "armor_stand", "fallen_head", "rubble", "barricade",
                  "crimson_banner", "chandelier",
                 "street_lamp", "broken_fountain", "toppled_column", "royal_urn", "stone_bench", "iron_fence", "hanging_lantern", "torn_tapestry"],
        "secret": ["rubble", "weapon_rack", "armor_stand", "gilded_brazier", "angel_statue", "shields", "cage"],
    },
    # open marsh: nothing hangs from the frame over the sky (front.js rule, as in the Forest)
    "marsh": {
        "open": ["rotten_stump", "leaning_cross", "reeds", "sunken_boat", "swamp_lantern", "bone_totem", "nets",
                 "glow_fungus",
                 "dead_tree", "plank_fence", "fish_rack", "mossy_idol", "drowned_cart", "skull_pole"],
        "poison": ["rotten_stump", "reeds", "sunken_boat", "bone_totem", "glow_fungus", "leaning_cross", "nets",
                 "dead_tree", "plank_fence", "fish_rack", "mossy_idol", "drowned_cart", "skull_pole"],
        "enclosed": ["witch_cauldron", "charm_bottles", "moss", "nets", "glow_fungus", "leaning_cross", "roots",
                     "rotten_stump",
                 "dead_tree", "plank_fence", "fish_rack", "mossy_idol", "drowned_cart", "skull_pole", "cocoon", "fishing_lantern"],
        "underground": ["glow_fungus", "roots", "moss", "rotten_stump", "bone_totem",
                 "dead_tree", "plank_fence", "fish_rack", "mossy_idol", "drowned_cart", "skull_pole", "cocoon", "fishing_lantern"],
        "arena": ["bone_totem", "leaning_cross", "reeds", "rotten_stump", "swamp_lantern",
                 "dead_tree", "plank_fence", "fish_rack", "mossy_idol", "drowned_cart", "skull_pole"],
        "secret": ["charm_bottles", "glow_fungus", "moss", "witch_cauldron", "sunken_boat"],
    },
}
DARKEN, DESATURATE = 0.78, 0.85  # the near wall sits behind the playfield: a step darker and duller
SOLID = "#%"
# big landmarks stand once in a room
UNIQUE = {"kneeling_knight", "broken_cart", "rune_stone", "market_stall", "broken_fountain", "mossy_idol",
          "drowned_cart", "dead_tree", "fish_rack", "signpost", "sarcophagus", "crown_altar", "effigy_statue", "mourner_statue", "abbey_arch", "hunter_shrine",
          "cart_wheel", "deer_skull", "fallen_head", "angel_statue", "well", "fallen_bell", "scarecrow", "wayside_cross", "cart",
          "lectern", "sunken_boat", "bone_totem", "witch_cauldron"}


def parse(text):
    zone = {"cell": 0.5, "rooms": []}
    room, grid = None, None
    for raw in text.splitlines():
        if grid is not None:
            if raw.strip() == "end":
                room["rows"] = grid[::-1]  # rows[y], bottom up
                room["h"], room["w"] = len(grid), len(grid[0])
                zone["rooms"].append(room)
                room, grid = None, None
            else:
                grid.append(raw.rstrip())
            continue
        words = raw.split("#")[0].split() if not raw.startswith("#") else []
        if not words:
            continue
        if words[0] == "cell":
            zone["cell"] = float(words[1])
        elif words[0] == "room":
            room = {"id": words[1], "flags": []}
        elif words[0] == "flags" and room:
            room["flags"] = words[1:]
        elif words[0] == "grid" and room:
            grid = []
    return zone


def cell(room, x, y):
    if y < 0 or y >= room["h"] or x < 0 or x >= len(room["rows"][y]):
        return "#"
    return room["rows"][y][x]


def clear(room, x0, x1, y0, y1):
    """Plain air in the box — no ledge, door, enemy, pickup or spike drawn over."""
    return all(cell(room, x, y) == "." for x in range(x0, x1) for y in range(y0, y1))


def room_lists(zone_name):
    """Room classes of a zone as the painter's looks.js keeps them (Forest: enclosed, underground, poison;
    Village: open streets, stone church and cellars)."""
    text = (ROOT / "design/levels/painter/looks.js").read_text(encoding="utf-8")
    start = text.find(zone_name + ": {")
    out = {}
    for name in ("enclosed", "underground", "poison", "open", "stone", "crypts"):
        m = re.search(name + r": \[([^\]]*)\]", text[start:]) if start >= 0 else None
        out[name] = set(re.findall(r'"([^"]+)"', m.group(1))) if m else set()
    return out


def kind_of(zone_name, room, lists):
    if any(f.startswith("boss=") for f in room["flags"]):
        return "arena"
    if "secret" in room["flags"]:
        return "secret"
    if zone_name == "forest":
        for name in ("underground", "poison", "enclosed"):
            if room["id"] in lists[name]:
                return name
        return "open"
    if zone_name == "village":
        for name in ("stone", "open"):
            if room["id"] in lists[name]:
                return name
        return "house"
    if zone_name == "capital":
        for name in ("crypts", "open"):
            if room["id"] in lists[name]:
                return name
        return "inside"
    if zone_name == "marsh":
        for name in ("underground", "enclosed", "poison"):
            if room["id"] in lists[name]:
                return name
        return "open"
    if any(w in room["id"] for w in CAVE_WORDS):
        return "cave"
    return "hall"


class Sprites:
    def __init__(self, zone):
        self.dir = ROOT / "design/levels/midjourney" / zone / "props"
        self.zone = zone
        self.cache = {}

    def has(self, name):
        return (self.dir / f"prop_{self.zone}_{name}.png").exists()

    def get(self, name, height_px, flip):
        key = (name, height_px, flip)
        if key not in self.cache:
            if name not in self.cache:
                src = Image.open(self.dir / f"prop_{self.zone}_{name}.png")
                obj = cut(src)
                obj = obj.crop(obj.getchannel("A").point(lambda v: 255 if v > 24 else 0).getbbox())
                # a web is see-through: the magenta behind its strands stays in it, so it goes grey
                rgb = ImageEnhance.Color(obj.convert("RGB")).enhance(0 if name == "cobweb" else DESATURATE)
                rgb = ImageEnhance.Brightness(rgb).enhance(DARKEN)
                rgb.putalpha(obj.getchannel("A"))
                self.cache[name] = rgb
            base = self.cache[name]
            w = max(1, round(base.width * height_px / base.height))
            img = base.resize((w, height_px), Image.LANCZOS)
            self.cache[key] = img.transpose(Image.FLIP_LEFT_RIGHT) if flip else img
        return self.cache[key]


def spots(room):
    """Floor runs (stone under plain air) and ceiling runs (stone over plain air), in cells."""
    floors, ceilings = [], []
    for y in range(1, room["h"] - 1):
        for kind, other in (("floor", y - 1), ("ceiling", y + 1)):
            run = None
            for x in range(room["w"] + 1):
                ok = x < room["w"] and cell(room, x, y) == "." and cell(room, x, other) in SOLID
                if ok and run is None:
                    run = x
                elif not ok and run is not None:
                    (floors if kind == "floor" else ceilings).append((run, x, y))
                    run = None
    return floors, ceilings


def dress(zone_name, zone, room, sprites, used_sets, lists):
    rng = random.Random(int(hashlib.md5(room["id"].encode()).hexdigest()[:8], 16))
    kind = kind_of(zone_name, room, lists)
    allowed = [n for n in ALLOWED[zone_name][kind] if sprites.has(n)]
    props = PROPS[zone_name]
    c = zone["cell"]
    cpx = round(c * PPU)  # pixels per cell
    floors, ceilings = spots(room)
    width_units = room["w"] * c
    # This room's own set: a few kinds, never the same set as a room before it.
    count = 3 if kind in ("secret", "cave", "underground", "house") else 4
    for _ in range(50):
        pool = sorted(allowed, key=lambda n: rng.random() ** (1 / props[n][2]), reverse=True)
        chosen = frozenset(pool[:min(count, len(pool))])
        if chosen not in used_sets:
            break
    used_sets.add(chosen)
    taken = []  # (x0, x1, y0, y1) boxes in cells already filled

    def free(box):
        return all(box[1] <= t[0] or box[0] >= t[1] or box[3] <= t[2] or box[2] >= t[3] for t in taken)

    placed = []
    budget = max(2, round(width_units / 7))
    standing = [n for n in sorted(chosen) if props[n][1] == "stand"]  # a set walks in the hash seed's order
    hanging = [n for n in sorted(chosen) if props[n][1] == "hang"]
    def stand(name):
        h_units = props[name][0] * (0.9 + rng.random() * 0.2)
        img = sprites.get(name, round(h_units * PPU), rng.random() < 0.5)
        wc = -(-img.width // cpx) + 1  # width in cells with a little air
        hc = -(-img.height // cpx)
        options = [(sx, y) for x0, x1, y in floors for sx in range(x0 + 1, x1 - wc)]
        rng.shuffle(options)
        for sx, y in options:
            box = (sx - 1, sx + wc + 1, y, y + hc)
            if free(box) and clear(room, sx, sx + wc, y, y + hc):
                taken.append(box)
                placed.append((img, sx * cpx + (wc * cpx - img.width) // 2, (room["h"] - y) * cpx - img.height + 2))
                return True
        return False

    order = sorted(standing, key=lambda n: rng.random())
    for name in order:  # each of the room's own kinds once, then a second of them while there is room
        if len(placed) < budget:
            stand(name)
    for name in order:
        if len(placed) < budget and name not in UNIQUE:
            stand(name)
    if not placed:  # a narrow room: whatever small thing of its kind fits
        for name in ("bone_pile", "urn", "candelabra", "mushrooms", "stump", "thorns", "jugs", "barrels", "rubble",
                     "glow_fungus", "reeds"):
            if name in allowed and stand(name):
                break
    for name in sorted(hanging, key=lambda n: rng.random()):
        h_units = props[name][0] * (0.8 + rng.random() * 0.3)
        img = sprites.get(name, round(h_units * PPU), rng.random() < 0.5)
        wc, hc = -(-img.width // cpx) + 1, -(-img.height // cpx)
        for x0, x1, y in sorted(ceilings, key=lambda r: rng.random()):
            if x1 - x0 < max(wc + 2, 6):  # a real vault, not a nub of rock: from a nub the thing hung in the air
                continue
            sx = rng.randint(x0 + 1, x1 - wc - 1)
            top = y + 1
            # it hangs no lower than 2.4 units over the floor under it (front.js rule): the hero stays seen
            gap = next((top - fy for fy in range(top - 1, -1, -1) if cell(room, sx + wc // 2, fy - 1) in SOLID), top)
            if (gap * c) - h_units < 2.4:
                continue
            box = (sx - 1, sx + wc + 1, top - hc, top)
            if free(box) and clear(room, sx, sx + wc, top - hc, top):
                taken.append(box)
                placed.append((img, sx * cpx + (wc * cpx - img.width) // 2, (room["h"] - top) * cpx - 2))
                break
    if "cobweb" in chosen:
        corners = [(x0, y, False) for x0, x1, y in ceilings if cell(room, x0 - 1, y) in SOLID] + \
                  [(x1, y, True) for x0, x1, y in ceilings if cell(room, x1, y) in SOLID]
        rng.shuffle(corners)
        for x, y, right in corners[:2]:
            img = sprites.get("cobweb", round(props["cobweb"][0] * (0.8 + rng.random() * 0.4) * PPU), right)
            px = x * cpx - (img.width if right else 0)
            placed.append((img, px, (room["h"] - y - 1) * cpx))
    return sorted(chosen), placed


def main():
    if len(sys.argv) < 2:
        sys.exit("usage: dress.py <zone> [room_id ...]")
    zone_name, only = sys.argv[1], set(sys.argv[2:])
    zone = parse((ROOT / "Assets/Game/Levels" / f"{zone_name}.txt").read_text(encoding="utf-8"))
    rooms_dir = ROOT / "Assets/Game/Art/Levels" / zone_name.capitalize() / "Rooms"
    sprites = Sprites(zone_name)
    used_sets = set()
    lists = room_lists(zone_name)
    backdrops = ROOT / "design/levels/midjourney" / zone_name
    painted = [r["id"] for r in zone["rooms"] if not any((backdrops / f"{r['id']}{e}").exists()
                                                         for e in (".png", ".jpg", ".jpeg", ".webp"))]
    painted = [r for r in painted if not only or r in only]
    if painted:  # the painter's near wall, fresh, so a second run is the same
        subprocess.run(["node", str(ROOT / "tools/level-art/render.js"), zone_name, "4",
                        ",".join(f"{r}_near" for r in painted)], check=True)
    for room in zone["rooms"]:
        near = rooms_dir / f"{room['id']}_near.png"
        if not near.exists():
            continue
        chosen, placed = dress(zone_name, zone, room, sprites, used_sets, lists)
        if only and room["id"] not in only:
            continue
        if room["id"] not in painted:
            border(near)  # back to place.py's frame, so a second run is the same
        layer = Image.open(near).convert("RGBA")
        for img, x, y in placed:
            layer.alpha_composite(img, (max(0, x), max(0, y)))
        layer.save(near)
        print(f"{room['id']}: {len(placed)} props ({', '.join(chosen)})")


if __name__ == "__main__":
    main()
