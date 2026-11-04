#!/usr/bin/env bash
# Runs one editor step in the live Unity Editor through the Unity CLI and waits until the Editor is idle again.
# Refuses while the Editor is in Play Mode (AGENTS.md: never touch a playing Editor).
#   tools/level-art/unity-step.sh "ForgottenKingdom.EditorTools.SceneForest.Apply();"
set -u
status() { unity command editor_status 2>&1 | grep -o '"status":"[a-z]*"\|"compiling":[a-z]*\|"playMode":"[a-z]*"' | tr '\n' ' '; }
s=$(status)
case "$s" in *'"playMode":"stopped"'*) ;; *) echo "editor not idle: $s"; exit 1;; esac
unity command eval "$1 return 1;" >/dev/null 2>&1
sleep 5
for _ in $(seq 1 160); do
  case "$(status)" in *'"status":"ready"'*'"compiling":false'*) echo "done: $1"; exit 0;; esac
  sleep 5
done
echo "timed out waiting: $1"; exit 1
