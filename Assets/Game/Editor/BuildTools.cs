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
    /// <c>-executeMethod ForgottenKingdom.EditorTools.BuildTools.BuildMac -buildOutput "path"</c>
    /// </summary>
    public static class BuildTools
    {
        public static void BuildWindows()
        {
            Build(BuildTarget.StandaloneWindows64, "ForgottenKingdom.exe");
        }

        /// <summary>
        /// Apple Silicon only, on Mono (the user's choice, 2026-09-27).
        /// </summary>
        public static void BuildMac()
        {
            SetMacArchitecture("ARM64");
            Build(BuildTarget.StandaloneOSX, "ForgottenKingdom.app");
        }

        private static void Build(BuildTarget target, string defaultName)
        {
            var output = Argument("-buildOutput")
                ?? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Build", defaultName));
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = target,
                options = BuildOptions.None
            });

            Debug.Log($"Build {report.summary.result}: {output}, {report.summary.totalErrors} errors.");
            if (report.summary.result != BuildResult.Succeeded)
            {
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// The macOS settings live in the Mac build module, which a Windows machine may not have,
        /// so they are reached by name rather than compiled against.
        /// </summary>
        private static void SetMacArchitecture(string architecture)
        {
            var settings = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("UnityEditor.OSXStandalone.UserBuildSettings"))
                .FirstOrDefault(t => t != null);
            var property = settings?.GetProperty("architecture");
            if (property == null)
            {
                throw new InvalidOperationException("The macOS build module is not installed.");
            }

            property.SetValue(null, Enum.Parse(property.PropertyType, architecture));
        }

        private static string Argument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
