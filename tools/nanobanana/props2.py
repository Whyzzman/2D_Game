#!/usr/bin/env python3
"""Second decor set: runs tools/nanobanana/generate.py one per prop, skips files that exist, retries twice."""
import subprocess
import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / "design/levels/midjourney"
TAIL = ("single object, isolated on a flat solid pure magenta background (#ff00ff), centered, whole object visible "
        "with margin, straight-on side view, hand-painted 2D game asset, painterly dark gothic fantasy, soft ink "
        "outlines, soft rim light from the upper left, muted desaturated palette. No background scene, no floor, "
        "no ground shadow, no text, no people, no frame, not a photo, not photorealistic, not a 3D render. "
        "The attached picture is only a style reference (line, palette, paint handling, magenta background): "
        "draw a different object, the one described.")
HANG = ", hanging down from the top edge of the picture, the chain or rope runs out of the top edge, nothing below it"
T, W, S = "9:16", "3:2", "1:1"

ITEMS = {
    "tomb": ("candelabra", "chains", [
        ("iron_lamp", T, "tall wrought iron standing lamp on a slender pole with a tripod foot, a caged dim candle lantern at the top, rusted black iron", False),
        ("kneeling_knight", S, "weathered stone statue of an armoured knight kneeling on one knee, head bowed, hands resting on a sword set point down, cracked grey stone, on a low plinth", False),
        ("stone_bench", W, "low heavy stone bench of a crypt, carved worn legs, a chipped slab seat, cracks and dust", False),
        ("broken_coffin", W, "old broken wooden coffin lying on its side, the lid split and askew, rusted iron bands, splintered dark planks", False),
        ("offering_bowl", S, "shallow stone offering bowl on a short carved pedestal foot, tarnished coins and wax drips on the rim", False),
        ("grave_slab", S, "leaning old gravestone slab tilted to one side, a worn carved crown and illegible grooves, chipped corners, grey stone", False),
        ("censer", T, "tarnished brass church censer with a pierced lid hanging on three thin chains, a faint wisp of pale smoke", True),
        ("iron_cage", T, "rusted iron gibbet cage with riveted bars, empty, hanging on a heavy chain", True),
    ]),
    "forest": ("stump", "vines", [
        ("lamp_post", T, "old leaning wooden lantern post overgrown with moss and ivy, a dead rusted iron lantern hanging from its arm", False),
        ("signpost", S, "weathered wooden roadside signpost with two crooked blank arrow boards pointing different ways, moss on the post", False),
        ("fern", W, "dense low fern bush, drooping withered fronds, dull dark green and brown", False),
        ("broken_cart", W, "broken abandoned wooden cart tilted on one remaining wheel, a snapped axle, rotten planks, moss and dead leaves", False),
        ("hollow_log", W, "big hollow fallen tree log lying on its side, the dark hollow opening visible at one end, bark, moss and small pale mushrooms", False),
        ("rune_stone", T, "tall narrow standing stone carved with old runes, moss in the grooves, lichen, a faint dull green glow in the runes", False),
        ("dead_bush", S, "leafless dead bush, tangled dry thorny twigs, grey brown", False),
        ("cocoon", T, "large pale silk spider cocoon, a wrapped oblong bundle, hanging on a thick strand of cobweb, dusty threads trailing", True),
    ]),
    "village": ("barrels", "lantern", [
        ("hay_stack", S, "round old haystack with a wooden pole through the middle, grey damp straw, a pitchfork stuck in the side", False),
        ("signpost", S, "crooked wooden village signpost with one blank hanging board and one blank arrow board, weathered grey wood, a rusty nail", False),
        ("wheelbarrow", W, "old wooden wheelbarrow with one iron-rimmed wheel, cracked planks, a few turnips and rags inside", False),
        ("woodpile", W, "neat stack of split firewood logs between two short stakes, an axe stuck in the chopping block beside it", False),
        ("market_stall", S, "abandoned wooden market stall with a torn faded striped cloth awning, an empty counter, a couple of empty crates", False),
        ("bench", W, "plain rough wooden village bench, worn plank seat, crooked legs, weathered grey wood", False),
        ("laundry_line", W, "sagging rope clothesline with grey torn rags and a patched shirt pinned to it, the rope runs from the left edge to the right edge near the top of the picture, the rags hang down from it", None),
        ("hanging_sign", S, "blank weathered wooden tavern signboard with an iron frame, hanging on two rusty chains", True),
    ]),
    "capital": ("gilded_brazier", "chandelier", [
        ("street_lamp", T, "tall ornate wrought iron street lamp, a fluted pole with scrollwork and a glass lantern head with a dim amber glow, tarnished gilding", False),
        ("broken_fountain", S, "broken stone city fountain, a cracked round basin and a shattered central tiered column, dry, dead leaves, traces of gilding", False),
        ("toppled_column", W, "toppled fluted marble column lying on its side broken in two drums, a fallen ornate capital beside it", False),
        ("royal_urn", S, "large ornamental royal stone urn on a square pedestal, carved garlands and a crown emblem, cracked, traces of gold", False),
        ("stone_bench", W, "ornate marble palace garden bench with carved lion-paw legs and a scrolled back, chipped and stained", False),
        ("iron_fence", W, "one section of ornate wrought iron fence with spear-tip finials and scrollwork between two stone posts, a few bars bent", False),
        ("hanging_lantern", T, "ornate gilded iron and glass palace lantern with a dim amber glow, hanging on a chain", True),
        ("torn_tapestry", T, "long torn royal tapestry hanging from a wooden rod at the top, faded crimson and gold with a woven crown emblem, the lower edge ripped into shreds", True),
    ]),
    "marsh": ("rotten_stump", "charm_bottles", [
        ("dead_tree", T, "tall dead leafless swamp tree, a twisted bare trunk with crooked bare branches, peeling grey bark, strands of hanging moss", False),
        ("plank_fence", W, "short section of crooked rotten plank fence, uneven grey boards on two stakes, moss and mud stains, one board missing", False),
        ("fish_rack", S, "wooden fish drying rack of lashed poles with rows of dried fish hanging from the crossbars", False),
        ("mossy_idol", S, "squat old carved stone idol with a crude frog-like face, half covered in thick moss and lichen, leaning slightly", False),
        ("drowned_cart", W, "old wooden cart sunk to its axle and tilted, rotten planks draped with slimy weeds and moss, one wheel broken", False),
        ("skull_pole", T, "tall crooked wooden pole with several animal and human skulls tied to it with rope, feathers and rag strips", False),
        ("cocoon", T, "large damp greenish silk cocoon, a wrapped oblong bundle with moss on it, hanging on a thick strand of web and swamp fibre", True),
        ("fishing_lantern", T, "small rusty fisherman's lantern with a pale green glow, a few fish hooks and a cork float tied to it, hanging on a frayed rope", True),
    ]),
}

failed = []
only = set(sys.argv[1:])
for zone, (stand_ref, hang_ref, items) in ITEMS.items():
    for name, ar, text, hang in items:
        out = BASE / zone / "props" / f"prop_{zone}_{name}.png"
        if only and f"{zone}_{name}" not in only:
            continue
        if out.exists():
            print("exists", out.name, flush=True)
            continue
        ref = BASE / zone / "props" / f"prop_{zone}_{hang_ref if hang is not False else stand_ref}.png"
        prompt = f"{text}{HANG if hang else ''}, {TAIL}"
        for attempt in range(3):
            run = subprocess.run([sys.executable, str(ROOT / "tools/nanobanana/generate.py"), "one", prompt, str(out),
                                  "--ar", ar, "--ref", str(ref)], capture_output=True, text=True)
            print(out.name, "|", (run.stdout + run.stderr).strip()[:300], flush=True)
            if out.exists():
                break
            time.sleep(30 * (attempt + 1))
        else:
            failed.append(out.name)
print("FAILED:", failed, flush=True)
