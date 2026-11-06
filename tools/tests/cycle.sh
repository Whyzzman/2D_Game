#!/usr/bin/env bash
# The verification copy's cycle on Windows (Assets/Game mirrored into it first with robocopy /MIR /XF Zz*.cs*):
# the scene passes the live Editor would run (ZzApplyScenes), then EditMode and PlayMode in full.
#   tools/tests/cycle.sh <tag>   ->  Artifacts/Verification/<tag>-{apply,edit,play}.xml and a summary with failures
set -u
cd "$(dirname "$0")/../.."
ROOT="$(pwd -W 2>/dev/null || pwd)"
V="$ROOT/Artifacts/Verification"
PY=/c/Users/Ryzen/AppData/Local/Python/bin/python
tag="${1:-cycle}"
cd Artifacts/Verification
unity test --mode EditMode --filter "ZzApplyScenes" --output "$V/$tag-apply.xml" "$V/Project" > "$tag-apply.log" 2>&1
echo "apply: $($PY "$ROOT/tools/tests/failures.py" "$V/$tag-apply.xml")"
unity test --mode EditMode --filter '^(?!.*\.Zz).*' --output "$V/$tag-edit.xml" "$V/Project" > "$tag-edit.log" 2>&1
echo "edit: $($PY "$ROOT/tools/tests/failures.py" "$V/$tag-edit.xml")"
if [ "${2:-}" != "--no-play" ]; then
  unity test --mode PlayMode --output "$V/$tag-play.xml" "$V/Project" > "$tag-play.log" 2>&1
  echo "play: $($PY "$ROOT/tools/tests/failures.py" "$V/$tag-play.xml")"
fi
echo CYCLEDONE
