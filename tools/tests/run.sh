#!/usr/bin/env bash
# EditMode tests in the live Editor: tools/tests/run.sh "Ns.ClassA|Ns.ClassB" (regex of names; empty = all).
# Writes Artifacts/tests.txt. Never Play Mode.
set -u
cd "$(dirname "$0")/../.."
cp tools/tests/ZzTestReport.cs Assets/Game/Editor/ZzTestReport.cs
rm -f Artifacts/tests.txt
unity command eval 'UnityEditor.AssetDatabase.Refresh(); UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation(); return 1;' >/dev/null 2>&1
# wait until the helper is compiled in (the Editor may compile late when it is not in focus)
for _ in $(seq 1 120); do
  unity command eval 'return typeof(ForgottenKingdom.EditorTools.ContentBuilder).Assembly.GetType("ForgottenKingdom.EditorTools.ZzTestReport")!=null;' 2>/dev/null | grep -q '"result":true' && break
  sleep 5
done
filter="${1:-}"
groups=""; [ -n "$filter" ] && groups="groupNames = new[] { \"$filter\" },"
unity command eval "var api = UnityEngine.ScriptableObject.CreateInstance<UnityEditor.TestTools.TestRunner.Api.TestRunnerApi>(); api.RegisterCallbacks(new ForgottenKingdom.EditorTools.ZzTestReport()); api.Execute(new UnityEditor.TestTools.TestRunner.Api.ExecutionSettings(new UnityEditor.TestTools.TestRunner.Api.Filter { testMode = UnityEditor.TestTools.TestRunner.Api.TestMode.EditMode, $groups })); return 1;" >/dev/null 2>&1
for _ in $(seq 1 360); do [ -f Artifacts/tests.txt ] && break; sleep 5; done
cat Artifacts/tests.txt 2>/dev/null || echo "no result"
rm -f Assets/Game/Editor/ZzTestReport.cs Assets/Game/Editor/ZzTestReport.cs.meta
unity command eval 'UnityEditor.AssetDatabase.Refresh(); return 1;' >/dev/null 2>&1
