#!/bin/zsh
# Арт карти світу для гри (gamedesign/WORLD_MAP.md): острови, картки, туман і кімнати, намальовані
# тим самим кодом, що й погоджений макет (design/ui/world-map/*.js), через headless Chrome.
# Кімнати беруться з design/ui/world-map/map-rooms.json (LevelTools.ExportMapRooms).
# Запуск: tools/map-art/render.sh [islands|rooms|all]   (за замовчуванням all)

ROOT="${0:A:h:h:h}"
OUT="$ROOT/Assets/Game/Art/Map"
source "$ROOT/tools/_shared/shot.sh"
URL="http://localhost:8123/design/ui/world-map/render.html"
WHAT="${1:-all}"
serve "$URL"
mkdir -p "$OUT/Islands" "$OUT/Cards" "$OUT/Rooms"
if [[ "$WHAT" == all || "$WHAT" == islands ]]; then
  for art in tomb forest village marsh depths crystal citadel capital cathedral aqueducts ice observatory palace shadow throne; do
    shot "$OUT/Islands/$art.png" 736 608 "$URL?island=$art" 3000
    shot "$OUT/Cards/$art.png" 744 392 "$URL?card=$art" 3000
  done
  shot "$OUT/fog.png" 1470 900 "$URL?fog=1" 3000
  shot "$OUT/glow.png" 128 128 "$URL?glow=1" 3000
fi
if [[ "$WHAT" == all || "$WHAT" == rooms ]]; then
  find "$OUT/Rooms" -name "*.png" -delete
  python3 - "$ROOT/design/ui/world-map/map-rooms.json" <<'PY' | while IFS=$'\t' read -r id query w h; do shot "$OUT/Rooms/$id.png" "$w" "$h" "$URL?$query" 3000; done
import json, sys
for r in json.load(open(sys.argv[1]))["rooms"]:
    w, h = round(r["w"]), round(r["h"])
    s = 2 if h < 100 else 1
    q = f"room={r['id']}&art={r['art']}&w={w}&h={h}&s={s}" + ("&boss=1" if r["boss"] else "")
    print(f"{r['id']}\t{q}\t{w * s}\t{h * s}")
PY
fi
echo "map art: $(ls "$OUT/Islands"/*.png | wc -l | tr -d ' ') islands, $(ls "$OUT/Rooms"/*.png | wc -l | tr -d ' ') rooms"
