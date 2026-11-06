#!/usr/bin/env python3
"""Cuts the rows tools/enemies/render.js made into the Tomb's enemy and boss frames.

  python tools/enemies/split.py

Each frame goes to Assets/Game/Art/Source/Final/Characters/<folder>/<State>/<prefix>_<state>_NN.png (drawn art,
the generator writes it over the placeholder on every build) and straight onto the placeholder's own file, so the
open Editor shows it without a build. Then in Unity: CharacterNormals.AttachAll() for those frames.
"""
import json
import time
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


def save(image, path):
    # an open Unity Editor locks a PNG for a moment while it imports the one written before (Windows)
    for attempt in range(40):
        try:
            image.save(path)
            return
        except OSError:
            if attempt == 39:
                raise
            time.sleep(0.5)


def frames_of(job, sheet):
    """The state's frames off the sheet render.js shot: a grid of padded frames (px each side, pt above), or the
    old single row of bare ones."""
    px, pt, cols = job.get("px", 0), job.get("pt", 0), job.get("cols", job["n"])
    W, H = job["w"] + 2 * px, job["h"] + pt
    return [sheet.crop(((i % cols) * W, (i // cols) * H, (i % cols + 1) * W, (i // cols + 1) * H)) for i in range(job["n"])]


def trim(job, frames):
    """One size for the state: the old frame, grown evenly at the sides and upward as far as any frame of the
    state is drawn (2026-11-06: arcs and lunges past the frame were cut straight). The feet stay at the middle of
    the bottom edge, where the sprite's pivot is (PlaceholderRaster: 0.5, 0), so nothing moves in the game."""
    px, pt, w, h = job.get("px", 0), job.get("pt", 0), job["w"], job["h"]
    side = top = 0
    for f in frames:
        box = f.getchannel("A").point(lambda v: 255 if v > 6 else 0).getbbox()
        if box is None:
            continue
        side = max(side, px - box[0], box[2] - (px + w))
        top = max(top, pt - box[1])
    side = min(px, side + 2 if side > 0 else 0)
    top = min(pt, top + 2 if top > 0 else 0)
    if (side == px and px > 0) or (top == pt and pt > 0):
        print(f"  {job['actor']} {job['state']}: drawn past the room given ({side}/{px} side, {top}/{pt} top)")
    return [f.crop((px - side, pt - top, px + w + side, pt + h)) for f in frames], side, top


def main():
    jobs = json.loads((SRC / "jobs.json").read_text(encoding="utf-8"))
    written = grown = 0
    for job in jobs:
        sheet = Image.open(SRC / f"{job['actor']}__{job['state']}.png").convert("RGBA")
        frames, side, top = trim(job, frames_of(job, sheet))
        grown += 1 if side or top else 0
        for i, raw in enumerate(frames):
            frame = shade(raw)
            name = f"{job['prefix']}_{job['state'].lower()}_{i:02d}.png"
            for base in (FINAL, ART):
                target = base / job["folder"] / job["state"] / name
                target.parent.mkdir(parents=True, exist_ok=True)
                save(frame, target)
            written += 1
    print(f"enemies: {written} frames written, {grown} state(s) given a wider or taller frame")


if __name__ == "__main__":
    main()
