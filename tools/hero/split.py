#!/usr/bin/env python3
"""Cuts the rows render.js made into the hero's frames (tools/hero/render.js).

  python tools/hero/split.py

Each frame goes to Assets/Game/Art/Source/Final/Characters/Player/<State>/player_<state>_NN.png (drawn art, the
generator writes it over the placeholder on every build) and straight onto the placeholder's own file, so the
open Editor shows it without a build. Then in Unity: CharacterNormals.AttachAll() for the hero's frames.
"""
import json
import os
import tempfile
from pathlib import Path

import sys

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/characters"))
from shade import shade, hero_rim  # noqa: E402  the painted light on each frame
SRC = Path(tempfile.gettempdir()) / "hero-frames"
PLAYER = ROOT / "Assets/Game/Art/Characters/Player"
FINAL = ROOT / "Assets/Game/Art/Source/Final/Characters/Player"


def main():
    jobs = json.loads((SRC / "jobs.json").read_text(encoding="utf-8"))
    written = 0
    for job in jobs:
        row = Image.open(SRC / (job["folder"].replace("/", "__") + ".png")).convert("RGBA")
        folder = PLAYER / job["folder"]
        for i in range(job["n"]):
            name = f"player_{job['state'].lower()}_{i:02d}.png"
            frame = hero_rim(shade(row.crop((i * 256, 0, i * 256 + 256, 224))))
            for target in (FINAL / job["folder"] / name, folder / name):
                target.parent.mkdir(parents=True, exist_ok=True)
                frame.save(target)
            written += 1
    print(f"hero: {written} frames written")


if __name__ == "__main__":
    main()
