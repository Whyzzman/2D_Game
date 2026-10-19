# Малює чернетку рівня (txt) у PNG для перегляду: камінь, уступи, шипи, речі; підписи кімнат.
import sys, re
from PIL import Image, ImageDraw
src, out = sys.argv[1], sys.argv[2]
rooms = []; cur = None; grid = False
for line in open(src, encoding='utf-8'):
    line = line.rstrip('\n')
    m = re.match(r'room (\S+) «(.+)» at (-?\d+) (-?\d+)', line)
    if m: cur = dict(id=m[1], name=m[2], x=int(m[3]), y=int(m[4]), rows=[]); rooms.append(cur); continue
    if line == 'grid': grid = True; continue
    if line == 'end': grid = False; continue
    if grid: cur['rows'].append(line)
S = 4
x0 = min(r['x'] for r in rooms); x1 = max(r['x'] + len(r['rows'][0]) for r in rooms)
y0 = min(r['y'] for r in rooms); y1 = max(r['y'] + len(r['rows']) for r in rooms)
img = Image.new('RGB', ((x1 - x0) * S, (y1 - y0) * S), (12, 12, 16)); d = ImageDraw.Draw(img)
for r in rooms:
    h = len(r['rows'])
    for i, row in enumerate(r['rows']):
        wy = r['y'] + h - 1 - i
        for c, ch in enumerate(row):
            wx = r['x'] + c
            col = {'#': (70, 70, 80), '=': (200, 170, 90), '^': (200, 60, 60), '%': (90, 90, 110), '.': (28, 28, 36)}.get(ch, (90, 220, 120))
            X, Y = (wx - x0) * S, (y1 - 1 - wy) * S
            d.rectangle([X, Y, X + S - 1, Y + S - 1], fill=col)
    X, Y = (r['x'] - x0) * S, (y1 - r['y'] - h) * S
    d.rectangle([X, Y, X + len(r['rows'][0]) * S - 1, Y + h * S - 1], outline=(120, 120, 200))
    d.text((X + 10, Y + 10), r['id'].split('_', 2)[-1], fill=(255, 255, 255))
img.save(out)
print(img.size)
