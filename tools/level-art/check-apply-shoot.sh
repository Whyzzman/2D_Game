#!/usr/bin/env bash
# One Unity pass after level/hero art changed (batchmode, Editor closed): LevelCheck → zone scene → hero normals →
# dev build → the first N room shots (the probe is stopped after N; the player's save is put back).
#   tools/level-art/check-apply-shoot.sh tomb 8
set -u
cd "$(dirname "$0")/../.."
U="/Applications/Unity/Hub/Editor/6000.5.10f1/Unity.app/Contents/MacOS/Unity"
P="$(pwd)"; ZONE="$1"; N="${2:-6}"
run() { "$U" -batchmode -quit -projectPath "$P" -executeMethod "$1" "${@:2}" -logFile "$P/Artifacts/step.log" || { echo "FAILED $1"; exit 1; }; }
run ForgottenKingdom.EditorTools.LevelTidy.ComfortReport
sed -n "/== $ZONE/,/^== [a-z]*:/p" Artifacts/LevelComfort.txt | head -8
grep -q "== $ZONE: LevelCheck OK" Artifacts/LevelComfort.txt || { echo "LEVELCHECK FAILED"; exit 1; }
run ForgottenKingdom.EditorTools.ScenePasses.ApplyZone -zone "$ZONE"
run ForgottenKingdom.EditorTools.CharacterNormals.AttachAll
git checkout -- Assets/Game/Scenes/MainMenu.unity ProjectSettings/Physics2DSettings.asset ProjectSettings/UnityConnectSettings.asset 2>/dev/null
run ForgottenKingdom.EditorTools.BuildTools.BuildMac -development -buildOutput "$P/Artifacts/GalleryBuild/ForgottenKingdom.app"
grep -q "Build Succeeded" Artifacts/step.log || { echo "BUILD FAILED"; exit 1; }
rm -rf Artifacts/room-shots
("Artifacts/GalleryBuild/ForgottenKingdom.app/Contents/MacOS/Тінь Забутого Королівства" -mute -perfProbe "$P/Artifacts/probe-q.tsv" -roomShots "$P/Artifacts/room-shots" -logFile "$P/Artifacts/probe-q.log" &)
until [ "$(ls Artifacts/room-shots 2>/dev/null | wc -l)" -ge "$N" ]; do sleep 2; done
sleep 3; pkill -f "GalleryBuild/ForgottenKingdom.app/Contents/MacOS"; sleep 2
D=/var/folders/m3/62819vpj12v1ktsx4lgnkccr0000gn/T/com.DefaultCompany.2D-URP/perf-save-stash
B="$HOME/Library/Application Support/com.DefaultCompany.2D-URP"
cp -p "$D"/forgotten-kingdom.save.json "$D"/forgotten-kingdom.save.bak "$B"/ && echo "save restored"
ls Artifacts/room-shots
echo SHOTSDONE
