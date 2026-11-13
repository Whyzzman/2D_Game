#!/usr/bin/env bash
# All painted zones after a repaint (batchmode, Editor closed): LevelCheck → every zone's scene → dev build →
# room shots of the whole world (the probe runs to its end and puts the save back itself).
#   tools/level-art/apply-all-shoot.sh
set -u
cd "$(dirname "$0")/../.."
U="/Applications/Unity/Hub/Editor/6000.5.10f1/Unity.app/Contents/MacOS/Unity"
P="$(pwd)"
run() { "$U" -batchmode -quit -projectPath "$P" -executeMethod "$1" "${@:2}" -logFile "$P/Artifacts/step.log" || { echo "FAILED $1"; grep -m5 "error CS" Artifacts/step.log; exit 1; }; }
run ForgottenKingdom.EditorTools.LevelTidy.ComfortReport
grep "LevelCheck" Artifacts/LevelComfort.txt
for z in tomb forest village capital marsh; do
  run ForgottenKingdom.EditorTools.ScenePasses.ApplyZone -zone "$z"; echo "applied $z $(date +%H:%M)"
done
git checkout -- Assets/Game/Scenes/MainMenu.unity Assets/Game/Scenes/TombTrial.unity ProjectSettings/Physics2DSettings.asset ProjectSettings/UnityConnectSettings.asset ProjectSettings/ProjectSettings.asset 2>/dev/null
run ForgottenKingdom.EditorTools.BuildTools.BuildMac -development -buildOutput "$P/Artifacts/GalleryBuild/ForgottenKingdom.app"
grep -q "Build Succeeded" Artifacts/step.log || { echo "BUILD FAILED"; exit 1; }
rm -rf Artifacts/room-shots
"Artifacts/GalleryBuild/ForgottenKingdom.app/Contents/MacOS/Тінь Забутого Королівства" -mute -perfProbe "$P/Artifacts/probe-q.tsv" -roomShots "$P/Artifacts/room-shots" -logFile "$P/Artifacts/probe-q.log"
ls Artifacts/room-shots | wc -l
echo SHOTSDONE
