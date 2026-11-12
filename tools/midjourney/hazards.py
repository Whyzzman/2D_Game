#!/usr/bin/env python3
"""Turns the Nano Banana pictures of the bosses' hazards into the sprites the hazard prefabs show.

  python tools/midjourney/hazards.py            -> all six
  python tools/midjourney/hazards.py rubble     -> only that one

Pictures: design/levels/midjourney/hazards/hazard_<name>.png, one object on flat magenta (prompts in
gamedesign/BOSSES_SIGNATURES.md, «Рисованные зоны»). Each is cut (sprites.py `cut`), trimmed to the object,
scaled to at most MAX px on its long side and written over the generator's placeholder
Assets/Game/Art/Shapes/shape_hazard_<name>.png, which is listed in Art/Painted.txt so the generator leaves it.
The prefab fits the sprite into the zone by its own bounds (ContentBuilder.BuildHazardPrefab), so the canvas
may be any size. Then in Unity: ContentBuilder.Build.
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(Path(__file__).resolve().parent))
sys.path.insert(0, str(ROOT / "tools/_shared"))
from sprites import cut  # noqa: E402
from painted import mark  # noqa: E402

SOURCE = ROOT / "design/levels/midjourney/hazards"
TARGET = ROOT / "Assets/Game/Art/Shapes"
NAMES = ["bell_stone", "rubble", "plague_pool", "skull", "ember_scar", "oath_fire"]
MAX = 512
# haze and dust over magenta come out pinkish: there red and blue are pulled down to the green — in the pool
# everywhere (its ooze is green anyway), in the rest only where blue tops green, so bone, fire and embers stay
ONLY_VIOLET = {"plague_pool": False}


def unpink(obj, only_violet):
    px = np.asarray(obj, dtype=np.float32)
    r, g, b = px[..., 0], px[..., 1], px[..., 2]
    where = b > g if only_violet else np.ones_like(g, bool)
    pink = where & ((r > g) | (b > g))
    px[..., 0] = np.where(where, np.minimum(r, g), r)
    px[..., 2] = np.where(where, np.minimum(b, g * 0.92), b)
    px[..., 3] = np.where(pink, px[..., 3] * 0.75, px[..., 3])
    return Image.fromarray(px.clip(0, 255).astype(np.uint8), "RGBA")


def main():
    only = set(sys.argv[1:])
    written = []
    for name in NAMES:
        if only and name not in only:
            continue
        source = SOURCE / f"hazard_{name}.png"
        if not source.exists():
            print(f"{name}: no picture, skipped")
            continue
        obj = cut(Image.open(source))
        obj = unpink(obj, ONLY_VIOLET.get(name, True))
        obj = obj.crop(obj.getchannel("A").point(lambda a: 255 if a > 8 else 0).getbbox())
        scale = min(1.0, MAX / max(obj.size))
        if scale < 1.0:
            obj = obj.resize((round(obj.width * scale), round(obj.height * scale)), Image.LANCZOS)
        target = TARGET / f"shape_hazard_{name}.png"
        obj.save(target)
        written.append(target)
        print(f"{name}: {obj.width}x{obj.height} -> {target.relative_to(ROOT)}")
    mark(written)


if __name__ == "__main__":
    main()
