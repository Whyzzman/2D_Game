# Quick preview of a level text: draws all rooms (stone, ledges, spikes, keys) into a PNG. Usage: view_zone.py in.txt out.png
import sys
from PIL import Image, ImageDraw
src, out = sys.argv[1], sys.argv[2]
rooms, cur = [], None
for line in open(src, encoding="utf-8"):
    line = line.rstrip('\n')
    if line.startswith("room "):
        parts = line.split()
        x, y = int(parts[-2]), int(parts[-1])
        cur = {"id": parts[1], "x": x, "y": y, "grid": [], "in": False, "flags": ""}
    elif cur and line.startswith("flags"): cur["flags"] = line
    elif cur and line == "grid": cur["in"] = True
    elif cur and line == "end": rooms.append(cur); cur = None
    elif cur and cur["in"]: cur["grid"].append(line)
minx = min(r["x"] for r in rooms); maxx = max(r["x"] + len(r["grid"][0]) for r in rooms)
miny = min(r["y"] for r in rooms); maxy = max(r["y"] + len(r["grid"]) for r in rooms)
S = 4
img = Image.new("RGB", ((maxx - minx) * S, (maxy - miny) * S), (12, 12, 16))
d = ImageDraw.Draw(img)
for r in rooms:
    h = len(r["grid"])
    tint = (40, 60, 90) if "needs" in r["flags"] else (70, 40, 70) if "secret" in r["flags"] else (60, 60, 60) if "optional" in r["flags"] else (90, 80, 60)
    for i, row in enumerate(r["grid"]):
        for j, ch in enumerate(row):
            gx, gy = r["x"] + j, r["y"] + (h - 1 - i)
            px, py = (gx - minx) * S, (maxy - 1 - gy) * S
            col = {'#': tint, '=': (220, 200, 120), '^': (60, 200, 220), '.': (18, 18, 24)}.get(ch, (255, 80, 80))
            d.rectangle([px, py, px + S - 1, py + S - 1], fill=col)
    d.rectangle([(r["x"] - minx) * S, (maxy - r["y"] - h) * S, (r["x"] - minx + len(r["grid"][0])) * S - 1, (maxy - r["y"]) * S - 1], outline=(255, 255, 255))
    d.text(((r["x"] - minx) * S + 6, (maxy - r["y"] - h) * S + 6), r["id"].split('_', 2)[-1], fill=(255, 255, 255))
img.save(out)
print(img.size)
