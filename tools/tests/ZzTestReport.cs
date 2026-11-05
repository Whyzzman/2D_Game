// Temporary: copied into Assets/Game/Editor to run EditMode tests in the live Editor (tools/tests/run.sh), then removed.
using System.Text;
using UnityEditor.TestTools.TestRunner.Api;

namespace ForgottenKingdom.EditorTools
{
    public sealed class ZzTestReport : ICallbacks
    {
        private readonly StringBuilder _log = new StringBuilder();
        private int _passed, _failed;

        public void RunStarted(ITestAdaptor testsToRun) { }

        public void RunFinished(ITestResultAdaptor result)
        {
            System.IO.File.WriteAllText("Artifacts/tests.txt", $"passed {_passed} failed {_failed}\n" + _log);
        }

        public void TestStarted(ITestAdaptor test) { }

        public void TestFinished(ITestResultAdaptor result)
        {
            if (result.HasChildren) return;
            if (result.TestStatus == TestStatus.Passed) _passed++;
            else if (result.TestStatus == TestStatus.Failed)
            {
                _failed++;
                _log.AppendLine(result.FullName + ": " + result.Message);
            }
        }
    }
}
