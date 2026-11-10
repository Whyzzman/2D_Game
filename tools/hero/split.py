#!/usr/bin/env python3
"""Cuts the rows render.js made into the hero's frames (tools/hero/render.js).

  python tools/hero/split.py

Each frame goes straight onto the placeholder's own file, Assets/Game/Art/Characters/Player/<State>/
player_<state>_NN.png, so the open Editor shows it without a build, and into Art/Painted.txt, so the generator
never overwrites it. Then in Unity: CharacterNormals.AttachAll() for the hero's frames.
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
sys.path.insert(0, str(ROOT / "tools/_shared"))
from painted import mark  # noqa: E402  the generator leaves listed files alone


def save(image, path):
    # an open Unity Editor locks a PNG for a moment while it imports the one written before (Windows)
    import time
    for attempt in range(60):
        try:
            image.save(path)
            return
        except OSError:
            if attempt == 59:
                raise
            time.sleep(0.5)


def main():
    jobs = json.loads((SRC / "jobs.json").read_text(encoding="utf-8"))
    written = 0
    targets = []
    for job in jobs:
        row = Image.open(SRC / (job["folder"].replace("/", "__") + ".png")).convert("RGBA")
        folder = PLAYER / job["folder"]
        for i in range(job["n"]):
            name = f"player_{job['state'].lower()}_{i:02d}.png"
            frame = hero_rim(shade(row.crop((i * 256, 0, i * 256 + 256, 224))))
            target = folder / name
            target.parent.mkdir(parents=True, exist_ok=True)
            save(frame, target)
            targets.append(target)
            written += 1
    mark(targets)
    print(f"hero: {written} frames written")


if __name__ == "__main__":
    main()
