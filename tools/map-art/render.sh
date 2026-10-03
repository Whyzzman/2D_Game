#!/bin/zsh
# Арт карти світу для гри (gamedesign/WORLD_MAP.md): острови, картки, туман і кімнати, намальовані
# тим самим кодом, що й погоджений макет (design/ui/world-map/*.js), через headless Chrome.
# Кімнати беруться з design/ui/world-map/map-rooms.json (LevelTools.ExportMapRooms).
# Запуск: tools/map-art/render.sh [islands|rooms|all]   (за замовчуванням all)

ROOT="${0:A:h:h:h}"
OUT="$ROOT/Assets/Game/Art/Map"
CHROME="/Applications/Google Chrome.app/Contents/MacOS/Google Chrome"
URL="http://localhost:8123/design/ui/world-map/render.html"
WHAT="${1:-all}"
if ! curl -s -o /dev/null "http://localhost:8123/design/ui/world-map/render.html"; then
  node "$ROOT/design/ui/_shared/serve.js" >/dev/null 2>&1 &
  SERVER=$!
  trap 'kill $SERVER 2>/dev/null' EXIT
  sleep 1
fi
PROFILE="$(mktemp -d)"
shot() { # shot <file> <w> <h> <query>
  # Chrome writes the picture and then may not quit on its own: wait for the file, then stop it.
  rm -f "$1"
  "$CHROME" --headless=new --disable-gpu --hide-scrollbars --default-background-color=00000000 \
    --user-data-dir="$PROFILE" --no-first-run --no-default-browser-check \
    --window-size="$2,$3" --virtual-time-budget=3000 --screenshot="$1" "$URL?$4" >/dev/null 2>&1 &
  local pid=$! waited=0
  while [[ ! -s "$1" && $waited -lt 300 ]]; do sleep 0.1; waited=$((waited + 1)); done
  sleep 0.3
  kill $pid 2>/dev/null; wait $pid 2>/dev/null
  [[ -s "$1" ]] || echo "failed: $1"
}
mkdir -p "$OUT/Islands" "$OUT/Cards" "$OUT/Rooms"
if [[ "$WHAT" == all || "$WHAT" == islands ]]; then
  for art in tomb forest village marsh depths crystal citadel capital cathedral aqueducts ice observatory palace shadow throne; do
    shot "$OUT/Islands/$art.png" 736 608 "island=$art"
    shot "$OUT/Cards/$art.png" 744 392 "card=$art"
  done
  shot "$OUT/fog.png" 1470 900 "fog=1"
  shot "$OUT/glow.png" 128 128 "glow=1"
fi
if [[ "$WHAT" == all || "$WHAT" == rooms ]]; then
  find "$OUT/Rooms" -name "*.png" -delete
  python3 - "$ROOT/design/ui/world-map/map-rooms.json" <<'PY' | while IFS=$'\t' read -r id query w h; do shot "$OUT/Rooms/$id.png" "$w" "$h" "$query"; done
import json, sys
for r in json.load(open(sys.argv[1]))["rooms"]:
    w, h = round(r["w"]), round(r["h"])
    q = f"room={r['id']}&art={r['art']}&w={w}&h={h}" + ("&boss=1" if r["boss"] else "")
    print(f"{r['id']}\t{q}\t{w}\t{h}")
PY
fi
rm -rf "$PROFILE"
echo "map art: $(ls "$OUT/Islands"/*.png | wc -l | tr -d ' ') islands, $(ls "$OUT/Rooms"/*.png | wc -l | tr -d ' ') rooms"
