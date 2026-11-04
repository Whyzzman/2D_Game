#!/usr/bin/env python3
"""Cuts the rows tools/enemies/render.js made into the Tomb's enemy and boss frames.

  python tools/enemies/split.py

Each frame goes to Assets/Game/Art/Source/Final/Characters/<folder>/<State>/<prefix>_<state>_NN.png (drawn art,
the generator writes it over the placeholder on every build) and straight onto the placeholder's own file, so the
open Editor shows it without a build. Then in Unity: CharacterNormals.AttachAll() for those frames.
"""
import json
import tempfile
from pathlib import Path

import sys

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/characters"))
from shade import shade  # noqa: E402  the painted light on each frame
SRC = Path(tempfile.gettempdir()) / "enemy-frames"
ART = ROOT / "Assets/Game/Art/Characters"
FINAL = ROOT / "Assets/Game/Art/Source/Final/Characters"


def main():
    jobs = json.loads((SRC / "jobs.json").read_text(encoding="utf-8"))
    written = 0
    for job in jobs:
        row = Image.open(SRC / f"{job['actor']}__{job['state']}.png").convert("RGBA")
        for i in range(job["n"]):
            frame = shade(row.crop((i * job["w"], 0, (i + 1) * job["w"], job["h"])))
            name = f"{job['prefix']}_{job['state'].lower()}_{i:02d}.png"
            for base in (FINAL, ART):
                target = base / job["folder"] / job["state"] / name
                target.parent.mkdir(parents=True, exist_ok=True)
                frame.save(target)
            written += 1
    print(f"enemies: {written} frames written")


if __name__ == "__main__":
    main()
