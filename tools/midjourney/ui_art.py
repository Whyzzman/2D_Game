#!/usr/bin/env python3
"""Turns the Nano Banana pictures of the HUD's icons and frames into the game's UI sprites.

  python tools/midjourney/ui_art.py            -> previews in design/art/icons/out/ (the game is not touched)
  python tools/midjourney/ui_art.py --install  -> over the placeholders in Assets/Game/Art/UI/, listed in Painted.txt

Pictures: design/art/icons/<name>.png, one object on flat magenta, one Gemini chat (prompts in
gamedesign/ART_ASSET_LIST.md §6). Each is keyed (`key`), trimmed and fitted into the placeholder's
canvas — same name, same size (IconArt / the UI frames), so the HUD shows it with no other change:
icons 96 × 96 with a small margin; the slot frame 64 × 64 edge to edge (its 9-slice border is 14 px);
the title ornament 512 × 24, centred. Then in Unity: ContentBuilder.Build (it leaves listed files alone).
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageEnhance, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(Path(__file__).resolve().parent))
sys.path.insert(0, str(ROOT / "tools/_shared"))
from painted import mark  # noqa: E402

SOURCE = ROOT / "design/art/icons"
PREVIEW = SOURCE / "out"
UI = ROOT / "Assets/Game/Art/UI"
ICONS = ["sword", "greatsword", "warrior", "hunter", "mage", "shadow", "fire", "frost", "arcane", "poison",
         "bleed", "checkpoint", "currency"]
MARGIN = 4  # px of the 96 icon canvas left empty on each side


def key(image):
    """Gemini's magenta is flat (about 249, 2, 237), so a chroma key does better here than `sprites.cut`
    (built for Midjourney's gradient: it eats crimson and leaves enclosed magenta, e.g. inside the frame).
    How magenta a pixel is — min(r, b) − g — gives its alpha: the key itself is clear, anything under LO
    (crimson, bone) is solid, a violet glow comes out partly clear and bluer, and a rim pixel, a mix of the object and the key, is unmixed
    back to the object's own colour, so no pink or grey halo is left."""
    px = np.asarray(image.convert("RGB"), dtype=np.float32)
    corners = np.concatenate([px[:8, :8].reshape(-1, 3), px[-8:, -8:].reshape(-1, 3),
                              px[:8, -8:].reshape(-1, 3), px[-8:, :8].reshape(-1, 3)])
    k = np.median(corners, axis=0)
    m = np.minimum(px[..., 0], px[..., 2]) - px[..., 1]
    km = min(k[0], k[2]) - k[1]
    alpha = np.clip(1 - (m - LO) / (km - LO), 0, 1)
    alpha[alpha < 0.06] = 0  # the key's own noise
    a = np.maximum(alpha, 1e-3)[..., None]
    rgb = np.clip((px - (1 - a) * k) / a, 0, 255)
    out = np.dstack([rgb, alpha * 255]).astype(np.uint8)
    return Image.fromarray(out, "RGBA")


LO = 12  # magenta-ness at or below which a pixel is wholly the object (a mix is linear above it)


def trimmed(name):
    obj = key(Image.open(SOURCE / f"{name}.png"))
    return obj.crop(obj.getchannel("A").point(lambda a: 255 if a > 90 else 0).getbbox())


def small(obj, w, h):
    """Down to w × h in steps (a single big LANCZOS step blurs thin strokes like a bowstring), then a light
    sharpen so the icon reads at 96 px."""
    while obj.width > w * 2 or obj.height > h * 2:
        obj = obj.resize((max(w, obj.width // 2), max(h, obj.height // 2)), Image.LANCZOS)
    obj = obj.resize((w, h), Image.LANCZOS)
    return obj.filter(ImageFilter.UnsharpMask(radius=0.8, percent=60, threshold=2))


def into(obj, cw, ch, margin):
    scale = min((cw - 2 * margin) / obj.width, (ch - 2 * margin) / obj.height)
    w, h = max(1, round(obj.width * scale)), max(1, round(obj.height * scale))
    canvas = Image.new("RGBA", (cw, ch), (0, 0, 0, 0))
    canvas.alpha_composite(small(obj, w, h), ((cw - w) // 2, (ch - h) // 2))
    return canvas


def ornament(obj):
    """512 × 24: the crown at the middle keeps its shape at the canvas height; the gold line either side is
    stretched to reach the edges (it only thins out, so stretching it changes nothing but its length)."""
    scale = 24 / obj.height
    obj = small(obj, max(1, round(obj.width * scale)), 24)
    a = np.asarray(obj.getchannel("A"))
    tall = np.where((a > 40).sum(axis=0) > 6)[0]  # columns of the crown: more than a thin line
    c0, c1 = (tall.min(), tall.max() + 1) if len(tall) else (obj.width // 2, obj.width // 2)
    mid = obj.crop((c0, 0, c1, 24))
    side = (512 - mid.width) // 2
    left = obj.crop((0, 0, c0, 24)).resize((side, 24), Image.LANCZOS)
    right = obj.crop((c1, 0, obj.width, 24)).resize((512 - mid.width - side, 24), Image.LANCZOS)
    canvas = Image.new("RGBA", (512, 24), (0, 0, 0, 0))
    canvas.alpha_composite(left, (0, 0))
    canvas.alpha_composite(mid, (side, 0))
    canvas.alpha_composite(right, (side + mid.width, 0))
    return canvas


def build():
    out = {}
    for icon in ICONS:
        name = f"icon_{icon}"
        if (SOURCE / f"{name}.png").exists():
            out[f"Icons/{name}.png"] = into(trimmed(name), 96, 96, MARGIN)
    if (SOURCE / "ui_slot_frame.png").exists():
        # the frame fills its canvas: the HUD's 9-slice takes 14 px from each edge as the border
        frame = trimmed("ui_slot_frame")
        out["Frames/ui_slot_frame.png"] = small(frame, 64, 64)
    if (SOURCE / "ui_ornament.png").exists():
        out["Frames/ui_ornament.png"] = ornament(trimmed("ui_ornament"))
    return out


def main():
    install = "--install" in sys.argv
    written = []
    for rel, img in build().items():
        target = (UI if install else PREVIEW) / rel
        target.parent.mkdir(parents=True, exist_ok=True)
        img.save(target)
        written.append(target)
        print(f"{rel}: {img.width}x{img.height} -> {target.relative_to(ROOT)}")
    if install:
        mark(written)


if __name__ == "__main__":
    main()
