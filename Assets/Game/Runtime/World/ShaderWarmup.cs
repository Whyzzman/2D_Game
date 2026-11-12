using System;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace ForgottenKingdom
{
    /// <summary>
    /// The shaders and render states the game needs, made before they are needed (KI-012, 2026-11-12; decided by
    /// Claude under the standing consent). A pipeline state built the first time a material meets a light in some
    /// room costs a frame of a few hundred milliseconds on Metal — the likeliest cause of the rare hitches «each time
    /// in another room». A build run with <c>-traceGraphicsStates "file.graphicsstate"</c> (with <c>-perfProbe</c>
    /// it walks every room) records them; the file goes to <c>Resources/GraphicsStates</c>, and every later start
    /// warms the collection for its own graphics API on worker threads, a little each frame, while the menu shows.
    /// </summary>
    public sealed class ShaderWarmup : MonoBehaviour
    {
        public const string Folder = "GraphicsStates";
        public const string TraceFlag = "-traceGraphicsStates";

        /// <summary>States made per frame while warming: enough to finish in the menu, few enough not to stall it.</summary>
        public const int PerFrame = 24;

        private static GraphicsStateCollection _trace;
        private static string _tracePath;

        /// <summary>The collections the game carries and the one being warmed now, for tests and the probe.</summary>
        public static GraphicsStateCollection Warming { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Start()
        {
            var args = Environment.GetCommandLineArgs();
            var at = Array.IndexOf(args, TraceFlag);
            if (at >= 0 && at + 1 < args.Length)
            {
                BeginTrace(args[at + 1]);
                return; // a trace records what the game makes by itself; warming would hide it
            }

            Warming = Pick(Resources.LoadAll<GraphicsStateCollection>(Folder), SystemInfo.graphicsDeviceType,
                Application.platform);
            if (Warming != null)
            {
                var host = new GameObject("ShaderWarmup");
                DontDestroyOnLoad(host);
                host.AddComponent<ShaderWarmup>();
            }
        }

        private JobHandle _job;

        private void Update()
        {
            if (Warming == null || Warming.isWarmedUp)
            {
                _job.Complete();
                Debug.Log($"[ShaderWarmup] {(Warming != null ? Warming.completedWarmupCount : 0)} states warmed");
                Destroy(gameObject);
                return;
            }

            // one batch at a time: the next is asked for once the last has finished
            if (_job.IsCompleted)
            {
                _job.Complete();
                _job = Warming.WarmUpProgressively(PerFrame, default);
            }
        }

        /// <summary>The collection recorded on this graphics API and platform, or none.</summary>
        public static GraphicsStateCollection Pick(GraphicsStateCollection[] all, GraphicsDeviceType api,
            RuntimePlatform platform)
        {
            if (all == null)
            {
                return null;
            }

            foreach (var collection in all)
            {
                if (collection != null && collection.graphicsDeviceType == api && collection.runtimePlatform == platform)
                {
                    return collection;
                }
            }

            return null;
        }

        private static void BeginTrace(string path)
        {
            _tracePath = path;
            _trace = new GraphicsStateCollection();
            _trace.BeginTrace();
            Application.quitting += EndTrace;
            Debug.Log($"[ShaderWarmup] tracing graphics states to {path}");
        }

        private static void EndTrace()
        {
            if (_trace == null)
            {
                return;
            }

            _trace.EndTrace();
            var saved = _trace.SaveToFile(_tracePath);
            Debug.Log($"[ShaderWarmup] {_trace.variantCount} variants, {_trace.totalGraphicsStateCount} states "
                + $"-> {_tracePath} ({(saved ? "saved" : "NOT saved")})");
            _trace = null;
        }
    }
}
