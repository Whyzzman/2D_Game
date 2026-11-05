#!/usr/bin/env bash
# EditMode tests in the live Editor: tools/tests/run.sh "Ns.ClassA|Ns.ClassB" (regex of names; empty = all).
# Writes Artifacts/tests.txt. Never Play Mode.
set -u
cd "$(dirname "$0")/../.."
cp tools/tests/ZzTestReport.cs Assets/Game/Editor/ZzTestReport.cs
rm -f Artifacts/tests.txt
unity command eval 'UnityEditor.AssetDatabase.Refresh(); return 1;' >/dev/null 2>&1
sleep 15
for _ in $(seq 1 60); do case "$(unity command editor_status 2>&1)" in *'"status":"ready"'*'"compiling":false'*) break;; esac; sleep 5; done
filter="${1:-}"
groups=""; [ -n "$filter" ] && groups="groupNames = new[] { \"$filter\" },"
unity command eval "var api = UnityEngine.ScriptableObject.CreateInstance<UnityEditor.TestTools.TestRunner.Api.TestRunnerApi>(); api.RegisterCallbacks(new ForgottenKingdom.EditorTools.ZzTestReport()); api.Execute(new UnityEditor.TestTools.TestRunner.Api.ExecutionSettings(new UnityEditor.TestTools.TestRunner.Api.Filter { testMode = UnityEditor.TestTools.TestRunner.Api.TestMode.EditMode, $groups })); return 1;" >/dev/null 2>&1
for _ in $(seq 1 240); do [ -f Artifacts/tests.txt ] && break; sleep 5; done
cat Artifacts/tests.txt 2>/dev/null || echo "no result"
rm -f Assets/Game/Editor/ZzTestReport.cs Assets/Game/Editor/ZzTestReport.cs.meta
unity command eval 'UnityEditor.AssetDatabase.Refresh(); return 1;' >/dev/null 2>&1
