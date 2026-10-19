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
source "$ROOT/tools/_shared/shot.sh"
URL="http://localhost:8123/design/levels/painter/index.html"
serve "$URL"
mkdir -p "$OUT/Terrain" "$OUT/Rooms" "$OUT/Breakables"
# Чекати лише на свої знімки: голий wait чекав би й на сервер, піднятий вище у фоні, — вічно.
pids=()
node "$ROOT/tools/level-art/jobs.js" "$ZONE" | while read kind name w h query; do
  case $kind in terrain) dir=Terrain;; breakable) dir=Breakables;; *) dir=Rooms;; esac
  shot "$OUT/$dir/$name.png" "$w" "$h" "$URL?zone=$ZONE&$query" &
  pids+=($!)
  if (( ${#pids} >= JOBS )); then wait $pids; pids=(); fi
done
(( ${#pids} )) && wait $pids
echo "level art: $(ls "$OUT/Terrain" | grep -c png) terrain, $(ls "$OUT/Rooms" | grep -c png) rooms, $(ls "$OUT/Breakables" | grep -c png) walls -> $OUT"
