#!/usr/bin/env python3
"""Far-plane pictures for the 20 Marsh rooms (2026-11-13) -> design/levels/midjourney/marsh/<room id>.png.

  python3 tools/nanobanana/marsh_rooms.py [room_marsh_edge ...]

Prompt = room text + ", " + the tail ("open" / "enclosed") taken verbatim from forest/prompts.json; one Forest
picture goes as a style reference. Existing files are skipped; the failed ones are printed at the end.
The API has no 2:1, so the wide rooms are asked as 16:9 (the Forest pictures are 1456x832 as well).
"""
import json
import subprocess
import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / "design/levels/midjourney"
REF = BASE / "forest/room_forest_entrance.png"
STYLE = (". The attached picture is only a reference for the manner of painting (line, layering, haze), "
         "not for the subject or the colours.")
ASPECT = {"2:1": "16:9"}

ROOMS = [
    ("room_marsh_edge", "open", "2:1", "edge of a moonlit swamp, last dead trees of a forest sinking into black water, cold grey-blue mist, pale moon glow"),
    ("room_marsh_pier", "open", "2:1", "rotten wooden pier on stilts over still water, moored broken boats, cold blue moonlit fog, a single lantern glow far away"),
    ("room_marsh_north_bridge", "open", "2:1", "long plank causeway across open swamp water, reeds, drowned fence posts, cold silver-blue mist"),
    ("room_marsh_lair_stair", "open", "9:16", "steep cliff of tangled roots and antlers rising out of the swamp, cold blue mist, pale sickly glow at the top"),
    ("room_marsh_rotten_road", "open", "2:1", "sunken log road through a swamp at dusk, leaning dead trees, olive and amber haze, low sun glow through fog"),
    ("room_marsh_graveyard", "open", "2:1", "flooded graveyard, leaning crosses and tombstones half under water, olive-amber dusk fog, faint candle glow"),
    ("room_marsh_drowned_ruins", "open", "2:1", "drowned stone ruins of a village, arches and walls rising from swamp water, amber dusk mist"),
    ("room_marsh_rotten_tree", "open", "9:16", "inside view up a colossal rotten tree in a swamp, hanging moss, shelf fungus, amber light through holes in the bark"),
    ("room_marsh_shrine", "enclosed", "3:2", "shrine of a drowned saint, a weathered statue under a canopy of roots, offering candles, olive-amber glow on wet stone"),
    ("room_marsh_wreck", "open", "2:1", "huge wrecked wooden ship sunk in a swamp, broken ribs of the hull, dark emerald mist, pale green glow from inside"),
    ("room_marsh_stilt_hut", "enclosed", "3:2", "interior of a fisherman's hut on stilts, nets and dried fish hanging, dark emerald gloom, light through gaps in the planks"),
    ("room_marsh_witch_hut", "enclosed", "3:2", "interior of a swamp witch's hut, hanging bottles and bundles of herbs, a cauldron glow, dark emerald and one warm ember light"),
    ("room_marsh_chapel", "enclosed", "3:2", "ruined swamp chapel overgrown with moss, broken stained glass, roots through the roof, dark emerald light"),
    ("room_marsh_chapel_cache", "enclosed", "3:2", "small hidden crypt behind a chapel, wet stone niches, roots, dark emerald gloom with a faint gold glint"),
    ("room_marsh_chest_nook", "enclosed", "3:2", "small hollow under tangled roots, dripping moss, dark emerald gloom, faint gold glint"),
    ("room_marsh_hold_cache", "enclosed", "3:2", "flooded hold of a wrecked ship, barrels and broken beams, dark emerald water light from above"),
    ("room_marsh_stag_lair", "open", "2:1", "vast poisoned clearing in the heart of the swamp, giant antler-like dead trees, acid green fog, sickly glow from below"),
    ("room_marsh_toxic_drain", "enclosed", "9:16", "stone drain shaft dripping glowing green poison, corroded pipes and roots, acid green haze"),
    ("room_marsh_mushroom_cave", "enclosed", "2:1", "cave of giant glowing mushrooms, acid green and teal bioluminescence, spores drifting in the dark"),
    ("room_marsh_mushroom_cache", "enclosed", "3:2", "small grotto under giant mushroom caps, soft green glow, drifting spores"),
]


def main():
    tails = json.loads((BASE / "forest/prompts.json").read_text(encoding="utf-8"))
    only = set(sys.argv[1:])
    failed = []
    for room, kind, ar, text in ROOMS:
        out = BASE / "marsh" / f"{room}.png"
        if only and room not in only:
            continue
        if out.exists():
            print("exists", out.name, flush=True)
            continue
        prompt = f"{text}, {tails[kind]}{STYLE}"
        for attempt in range(3):
            run = subprocess.run([sys.executable, str(ROOT / "tools/nanobanana/generate.py"), "one", prompt, str(out),
                                  "--ar", ASPECT.get(ar, ar), "--ref", str(REF)], capture_output=True, text=True)
            print(out.name, "|", (run.stdout + run.stderr).strip()[:300], flush=True)
            if out.exists():
                break
            time.sleep(30 * (attempt + 1))
        else:
            failed.append(out.name)
    print("FAILED:", failed, flush=True)


if __name__ == "__main__":
    main()
