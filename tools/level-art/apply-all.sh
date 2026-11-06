#!/usr/bin/env bash
# Every painted zone and the scene passes after it, in the live Editor, once it is idle (never in Play Mode):
#   tools/level-art/apply-all.sh [file-to-wait-for]   (the optional file is a render log that must say ALLDONE first)
set -u
cd "$(dirname "$0")/../.."
until unity command editor_status 2>&1 | grep -q '"status":"ready".*"compiling":false.*"playMode":"stopped"'; do sleep 20; done
echo "editor idle at $(date +%H:%M)"
unity command eval 'UnityEditor.AssetDatabase.Refresh(); UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation(); return 1;' >/dev/null 2>&1
sleep 30
for s in "ForgottenKingdom.EditorTools.ContentBuilder.RefreshLevelArt();" \
         "ForgottenKingdom.EditorTools.SceneTomb.Apply();" "ForgottenKingdom.EditorTools.SceneForest.Apply();" \
         "ForgottenKingdom.EditorTools.SceneVillage.Apply();" "ForgottenKingdom.EditorTools.SceneCapital.Apply();" \
         "ForgottenKingdom.EditorTools.SceneMarsh.Apply();" "ForgottenKingdom.EditorTools.SceneReactive.Apply();" \
         "ForgottenKingdom.EditorTools.SceneStones.Apply();" "ForgottenKingdom.EditorTools.SceneLevelLights.Apply();"; do
  tools/level-art/unity-step.sh "$s" || exit 1
done
if [ -n "${1:-}" ]; then until grep -q ALLDONE "$1" 2>/dev/null; do sleep 20; done; fi
unity command eval 'UnityEditor.AssetDatabase.Refresh(); return 1;' >/dev/null 2>&1
sleep 20
tools/level-art/unity-step.sh "ForgottenKingdom.EditorTools.CharacterNormals.AttachAll();"
echo "APPLIED"
unity command console 2>&1 | grep -o '"level":"error","logType":"[A-Za-z]*","message":"[^"]\{0,250\}' | grep -v api/exec | tail -8
