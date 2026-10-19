#!/bin/zsh
# Арт рівнів, намальований painter'ом (design/levels/painter) з тексту зони, через headless Chrome:
# камінь площини шматками 16 × 16 юнітів (48 px/юніт) і тло кімнат (mid — паралакс, near — стіна).
# Потім у Unity: ContentBuilder.RefreshLevelArt (налаштування імпорту) і Scene<Зона>.Apply.
# Запуск: tools/level-art/render.sh tomb [паралельно=4]
ROOT="${0:A:h:h:h}"
ZONE="${1:-tomb}"
JOBS="${2:-4}"
CAP="${(C)ZONE}"
OUT="$ROOT/Assets/Game/Art/Levels/$CAP"
# Інший Chromium: CHROME=<шлях>; для chrome-headless-shell (малює й при сплячому дисплеї) — HEADLESS=
CHROME="${CHROME:-/Applications/Google Chrome.app/Contents/MacOS/Google Chrome}"
HEADLESS="${HEADLESS---headless=new}"
URL="http://localhost:8123/design/levels/painter/index.html"
if ! curl -s -o /dev/null "$URL"; then
  node "$ROOT/design/ui/_shared/serve.js" >/dev/null 2>&1 &
  SERVER=$!
  trap 'kill $SERVER 2>/dev/null' EXIT
  sleep 1
fi
mkdir -p "$OUT/Terrain" "$OUT/Rooms" "$OUT/Breakables"
shot() { # shot <file> <w> <h> <query>
  local profile="$(mktemp -d)"
  rm -f "$1"
  "$CHROME" ${=HEADLESS} --disable-gpu --hide-scrollbars --default-background-color=00000000 \
    --user-data-dir="$profile" --no-first-run --no-default-browser-check \
    --window-size="$2,$3" --virtual-time-budget=6000 --screenshot="$1" "$URL?zone=$ZONE&$4" >/dev/null 2>&1 &
  local pid=$! waited=0
  while [[ ! -s "$1" && $waited -lt 600 ]]; do sleep 0.1; waited=$((waited + 1)); done
  sleep 0.3
  kill $pid 2>/dev/null; wait $pid 2>/dev/null
  rm -rf "$profile"
  [[ -s "$1" ]] || echo "failed: $1"
}
# Чекати лише на свої знімки: голий wait чекав би й на сервер, піднятий вище у фоні, — вічно.
pids=()
node "$ROOT/tools/level-art/jobs.js" "$ZONE" | while read kind name w h query; do
  case $kind in terrain) dir=Terrain;; breakable) dir=Breakables;; *) dir=Rooms;; esac
  shot "$OUT/$dir/$name.png" "$w" "$h" "$query" &
  pids+=($!)
  if (( ${#pids} >= JOBS )); then wait $pids; pids=(); fi
done
(( ${#pids} )) && wait $pids
echo "level art: $(ls "$OUT/Terrain" | grep -c png) terrain, $(ls "$OUT/Rooms" | grep -c png) rooms, $(ls "$OUT/Breakables" | grep -c png) walls -> $OUT"
