#!/usr/bin/env bash
# Repaints whole zones after their text changed, without Unity: painter (render.js) → Midjourney backdrops (place.py,
# zones that have them) → drawn props (dress.py). Then in Unity, one per zone (batchmode, Editor closed):
#   Unity -batchmode -quit -executeMethod ForgottenKingdom.EditorTools.ScenePasses.ApplyZone -zone <zone>
#   tools/level-art/repaint.sh tomb forest village capital marsh
set -eu
cd "$(dirname "$0")/../.."
PY=.venv/bin/python
for zone in "$@"; do
  echo "== $zone render $(date +%H:%M:%S)"
  node tools/level-art/render.js "$zone" 4
  if ls design/levels/midjourney/"$zone"/room_* >/dev/null 2>&1; then
    echo "== $zone place"
    "$PY" tools/midjourney/place.py "$zone"
  fi
  echo "== $zone dress"
  "$PY" tools/midjourney/dress.py "$zone"
done
echo "ALLDONE $(date +%H:%M:%S)"
