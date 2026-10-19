import sys, re
from PIL import Image, ImageDraw
src = open(sys.argv[1], encoding='utf-8').read().splitlines()
rooms=[]; i=0
while i < len(src):
    m = re.match(r'room (\S+) «(.*)» at (-?\d+) (-?\d+)', src[i])
    if m:
        rid, name, x, y = m.group(1), m.group(2), int(m.group(3)), int(m.group(4))
        while src[i] != 'grid': i += 1
        i += 1; g=[]
        while src[i] != 'end': g.append(src[i]); i += 1
        rooms.append((rid, x, y, g))
    i += 1
xs=[x for _,x,_,_ in rooms]+[x+len(g[0]) for _,x,_,g in rooms]; ys=[y for _,_,y,_ in rooms]+[y+len(g) for _,_,y,g in rooms]
x0,x1,y0,y1=min(xs),max(xs),min(ys),max(ys); s=3
im=Image.new('RGB',((x1-x0)*s,(y1-y0)*s),(10,10,14)); d=ImageDraw.Draw(im)
col={'#':(70,74,86),'=':(160,140,90),'^':(40,160,200),'.':(22,24,30)}
for rid,x,y,g in rooms:
    h=len(g)
    for r,row in enumerate(g):
        for c,ch in enumerate(row):
            wy=y+h-1-r; px=(x+c-x0)*s; py=(y1-1-wy)*s
            d.rectangle([px,py,px+s-1,py+s-1],fill=col.get(ch,(240,80,80)))
im.save(sys.argv[2])
