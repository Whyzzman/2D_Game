#!/usr/bin/env python3
"""A gallery of every room of the five zones, for the user to look the levels through without playing.

  build with -perfProbe "<report>.tsv" -roomShots Artifacts/room-shots   (PerformanceProbe, silent)
  .venv/bin/python tools/gallery/build.py [shots dir] [out dir]

Pictures come as <zone_id>__<room_id>.png; names and order from Assets/Game/Levels/*.txt. Writes
<out>/index.html and <out>/img/<zone>/<room>.jpg, published as an Artifact (the page plus its files).
"""
import html
import re
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
SHOTS = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / "Artifacts/room-shots"
OUT = Path(sys.argv[2]) if len(sys.argv) > 2 else ROOT / "Artifacts/gallery"
ZONES = [  # level text, the zone's name in the game, the short word for a room's label, its colour on the page
    ("tomb", "Забута Гробниця", "Гробниця", "#8fa3b8"), ("forest", "Зів'ялий Ліс", "Ліс", "#8fb57a"),
    ("village", "Занепале Село", "Село", "#d6a35c"), ("capital", "Зруйнована Столиця", "Столиця", "#d0705e"),
    ("marsh", "Чумні Болота", "Болота", "#a8b35a")]
WIDTH = 1600


def zone_info(name):
    text = (ROOT / "Assets/Game/Levels" / f"{name}.txt").read_text(encoding="utf-8")
    zone_id = re.search(r"^zone (\S+)", text, re.M).group(1)
    rooms = re.findall(r"^room (\S+) «([^»]+)»", text, re.M)
    return zone_id, rooms


def main():
    sections, nav, total = [], [], 0
    for name, title, short, colour in ZONES:
        zone_id, rooms = zone_info(name)
        cards = []
        for number, (room_id, room_name) in enumerate(rooms, 1):
            shot = SHOTS / f"{zone_id}__{room_id}.png"
            if not shot.exists():
                continue
            out = OUT / "img" / name / f"{room_id}.jpg"
            out.parent.mkdir(parents=True, exist_ok=True)
            image = Image.open(shot).convert("RGB")
            if image.width > WIDTH:
                image = image.resize((WIDTH, round(image.height * WIDTH / image.width)), Image.LANCZOS)
            image.save(out, "JPEG", quality=82, optimize=True)
            path = f"img/{name}/{room_id}.jpg"
            label = f"{short} {number}"
            cards.append(
                f'<figure class="room" tabindex="0" data-src="{path}" data-label="{html.escape(label)} · '
                f'{html.escape(room_name)}" data-id="{room_id}">'
                f'<img src="{path}" alt="{html.escape(room_name)}" loading="lazy" width="16" height="9">'
                f'<figcaption><span class="num">{number}</span><span class="name">{html.escape(room_name)}</span>'
                f'<code>{room_id}</code></figcaption></figure>')
        total += len(cards)
        nav.append(f'<a href="#{name}" style="--zone:{colour}">{html.escape(title)} <b>{len(cards)}</b></a>')
        sections.append(
            f'<section id="{name}" class="zone" style="--zone:{colour}"><h2>{html.escape(title)}'
            f'<small>{len(cards)} кімнат</small></h2><div class="grid">{"".join(cards)}</div></section>')

    page = (ROOT / "tools/gallery/page.html").read_text(encoding="utf-8")
    page = page.replace("{{NAV}}", "".join(nav)).replace("{{SECTIONS}}", "".join(sections)).replace(
        "{{TOTAL}}", str(total))
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / "index.html").write_text(page, encoding="utf-8")
    print(f"{total} rooms -> {OUT / 'index.html'}")


if __name__ == "__main__":
    main()
