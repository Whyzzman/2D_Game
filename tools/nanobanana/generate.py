#!/usr/bin/env python3
"""Pictures from Google's Nano Banana (Gemini image models) over the API, instead of Midjourney (2026-11-11,
user's choice). The key is read from .env in the project root (GEMINI_API_KEY=..., the file is not in git) and
goes nowhere but Google's API. No packages needed.

  python tools/nanobanana/generate.py --models                       image models this key can use
  python tools/nanobanana/generate.py enemies Bellringer PlagueStag  sheets for the puppet rig, 2 variants each ->
                                                                     design/characters/enemies/parts/<Actor>/nb_<n>.png
  python tools/nanobanana/generate.py one "<prompt>" out.png [--ar 1:2] [--ref picture.png]

Enemy prompts come from design/characters/enemies/parts/prompts.json (the same text Midjourney got). Pick a
variant by copying it over source.png, then tools/enemies/puppet.py cuts it. Model: GEMINI_IMAGE_MODEL in .env
or the default below; --n sets the number of variants.
"""
import base64
import json
import mimetypes
import sys
import urllib.error
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PARTS = ROOT / "design/characters/enemies/parts"
API = "https://generativelanguage.googleapis.com/v1beta"
DEFAULT_MODEL = "gemini-3.1-flash-image"


def env():
    values = {}
    path = ROOT / ".env"
    if path.exists():
        for line in path.read_text(encoding="utf-8").splitlines():
            if "=" in line and not line.lstrip().startswith("#"):
                key, value = line.split("=", 1)
                values[key.strip()] = value.strip().strip('"').strip("'")
    if not values.get("GEMINI_API_KEY"):
        sys.exit("no GEMINI_API_KEY in .env (project root); create a key at https://aistudio.google.com/apikey")
    return values


def call(url, key, body=None):
    request = urllib.request.Request(url, data=json.dumps(body).encode() if body else None,
                                     headers={"x-goog-api-key": key, "Content-Type": "application/json"})
    try:
        with urllib.request.urlopen(request, timeout=300) as response:
            return json.loads(response.read())
    except urllib.error.HTTPError as error:
        message = error.read().decode("utf-8", "replace")
        try:
            message = json.loads(message)["error"]["message"]
        except Exception:
            pass
        sys.exit(f"Gemini API {error.code}: {message[:600]}")


def picture(settings, prompt, out, aspect="3:2", refs=()):
    parts = [{"text": prompt}]
    for ref in refs:
        ref = Path(ref)
        parts.append({"inline_data": {"mime_type": mimetypes.guess_type(ref.name)[0] or "image/png",
                                      "data": base64.b64encode(ref.read_bytes()).decode()}})
    model = settings.get("GEMINI_IMAGE_MODEL", DEFAULT_MODEL)
    answer = call(f"{API}/models/{model}:generateContent", settings["GEMINI_API_KEY"],
                  {"contents": [{"parts": parts}],
                   "generationConfig": {"responseModalities": ["IMAGE"], "imageConfig": {"aspectRatio": aspect}}})
    for candidate in answer.get("candidates", []):
        for part in candidate.get("content", {}).get("parts", []):
            data = part.get("inlineData") or part.get("inline_data")
            if data:
                out = Path(out)
                out.parent.mkdir(parents=True, exist_ok=True)
                out.write_bytes(base64.b64decode(data["data"]))
                print(f"saved {out.relative_to(ROOT) if out.is_relative_to(ROOT) else out} ({data.get('mimeType', '?')})")
                return True
    print("no picture in the answer:", json.dumps(answer)[:400])
    return False


def enemy_prompt(spec, figure, weapon):
    style = spec["style"].split(" --")[0]
    banned = "No background scene, no floor, no ground shadow, no text, no frame, not a photo, not 3D"
    if weapon == "none":
        style = style.replace("wide empty space between the figure and the weapon", "wide empty space around the creature")
        return (f"2D game enemy for a dark fantasy metroidvania, {figure}, side view facing right, full body, "
                f"a single creature alone, {style}. {banned}.")
    return (f"2D game enemy for a dark fantasy metroidvania, {figure}, side view facing right, full body, "
            f"on the left the figure with empty hands, on the right separately {weapon} lying horizontally "
            f"with its handle on the left, {style}. {banned}, the hands do not hold the weapon.")


def main():
    args = sys.argv[1:]
    def option(name, default=None):
        if name in args:
            at = args.index(name)
            value = args[at + 1]
            del args[at:at + 2]
            return value
        return default
    count = int(option("--n", "2"))
    aspect = option("--ar", "3:2")
    ref = option("--ref")
    settings = env()
    if "--models" in args:
        listed = call(f"{API}/models?pageSize=200", settings["GEMINI_API_KEY"])
        for model in listed.get("models", []):
            if "image" in model["name"]:
                print(model["name"].split("/")[-1], "-", model.get("displayName", ""))
        return
    if args and args[0] == "enemies":
        spec = json.loads((PARTS / "prompts.json").read_text(encoding="utf-8"))
        items = {name: (figure, weapon) for name, figure, weapon in spec["items"]}
        for actor in args[1:]:
            if actor not in items:
                sys.exit(f"{actor}: not in prompts.json ({', '.join(items)})")
            for n in range(1, count + 1):
                picture(settings, enemy_prompt(spec, *items[actor]), PARTS / actor / f"nb_{n}.png", aspect,
                        [ref] if ref else [])
        return
    if len(args) == 3 and args[0] == "one":
        picture(settings, args[1], Path(args[2]).resolve(), aspect, [ref] if ref else [])
        return
    sys.exit(__doc__)


if __name__ == "__main__":
    main()
