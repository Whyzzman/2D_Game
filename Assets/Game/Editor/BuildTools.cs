using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// Builds the game from the command line. Every earlier phase built through a throwaway
    /// script; this keeps the one way that is known to work.
    ///
    /// <c>-executeMethod ForgottenKingdom.EditorTools.BuildTools.BuildWindows -buildOutput "path"</c>
    /// </summary>
    public static class BuildTools
    {
        public static void BuildWindows()
        {
            var output = Argument("-buildOutput")
                ?? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Build", "ForgottenKingdom.exe"));
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });

            Debug.Log($"Build {report.summary.result}: {output}, {report.summary.totalErrors} errors.");
            if (report.summary.result != BuildResult.Succeeded)
            {
                EditorApplication.Exit(1);
            }
        }

        private static string Argument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
