using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Profiling;

namespace ForgottenKingdom
{
    /// <summary>
    /// Writes down every long frame and what changed in it, to find the rare 0.3-0.7 s stalls
    /// (KNOWN_ISSUES KI-012). Each row holds the room, whether a zone was streaming, how much texture
    /// and driver memory appeared, collections, and the time spent in the engine markers that
    /// usually own such a frame (shader and pipeline creation, uploads, loading).
    ///
    /// Inert unless the game is started with <c>-hitchLog "hitches.tsv"</c>; <see cref="PerformanceProbe"/>
    /// starts it too, next to its report. Engine markers report only in a development build; a
    /// release build still gets the memory and streaming columns.
    /// </summary>
    public sealed class HitchLog : MonoBehaviour
    {
        /// <summary>A frame longer than this is a hitch: three frames at 30 FPS.</summary>
        public const float HitchSeconds = 0.1f;

        // Markers that own long frames on Metal and elsewhere. Missing ones stay invalid and read 0.
        private static readonly string[] MarkerNames =
        {
            "Shader.CreateGPUProgram",
            "Shader.Parse",
            "CreateGPUProgram",
            "Gfx.WaitForPresentOnGfxThread",
            "Texture.AwakeFromLoad",
            "AsyncUploadManager.ScheduleAsyncRead",
            "Loading.ReadObject",
            "Loading.AwakeFromLoad",
            "GC.Collect",
        };

        private readonly List<ProfilerRecorder> _recorders = new List<ProfilerRecorder>();
        private readonly StringBuilder _rows = new StringBuilder();
        private string _path;
        private long _lastTexture;
        private long _lastDriver;
        private int _lastCollections;
        private int _hitches;

        /// <summary>Where the current samples come from; the probe names its rooms here.</summary>
        public string Place { get; set; } = "-";

        public int Hitches => _hitches;

        public static HitchLog Current { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartIfRequested()
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, "-hitchLog");
            if (index >= 0 && index + 1 < args.Length)
            {
                Begin(args[index + 1]);
            }
        }

        public static HitchLog Begin(string path)
        {
            if (Current != null)
            {
                return Current;
            }

            var log = new GameObject("HitchLog").AddComponent<HitchLog>();
            log._path = path;
            DontDestroyOnLoad(log.gameObject);
            return log;
        }

        private void Awake()
        {
            Current = this;
            // Looked up by name among what this player exposes, so each recorder gets its own category.
            var available = new List<ProfilerRecorderHandle>();
            ProfilerRecorderHandle.GetAvailable(available);
            foreach (var marker in MarkerNames)
            {
                var recorder = default(ProfilerRecorder);
                foreach (var handle in available)
                {
                    var description = ProfilerRecorderHandle.GetDescription(handle);
                    if (description.Name == marker)
                    {
                        recorder = ProfilerRecorder.StartNew(description.Category, description.Name);
                        break;
                    }
                }

                _recorders.Add(recorder);
            }

            _rows.Append("frame\ttime_s\tplace\tms\tzone_streaming\ttexture_mb_delta\tgfx_mb_delta\tgc");
            foreach (var marker in MarkerNames)
            {
                _rows.Append('\t').Append(marker).Append("_ms");
            }

            _rows.Append('\n');
            _lastTexture = (long)Texture.currentTextureMemory;
            _lastDriver = Profiler.GetAllocatedMemoryForGraphicsDriver();
            _lastCollections = GC.CollectionCount(0);
        }

        private void Update()
        {
            var texture = (long)Texture.currentTextureMemory;
            var driver = Profiler.GetAllocatedMemoryForGraphicsDriver();
            var collections = GC.CollectionCount(0);
            var frame = Time.unscaledDeltaTime;

            if (frame >= HitchSeconds)
            {
                _hitches++;
                const float mb = 1024f * 1024f;
                var streamer = ZoneStreamer.Current;
                _rows.AppendFormat(CultureInfo.InvariantCulture, "{0}\t{1:F1}\t{2}\t{3:F0}\t{4}\t{5:F1}\t{6:F1}\t{7}",
                    Time.frameCount, Time.realtimeSinceStartup, Place, frame * 1000f,
                    streamer != null && streamer.IsBusy ? 1 : 0, (texture - _lastTexture) / mb,
                    (driver - _lastDriver) / mb, collections - _lastCollections);
                foreach (var recorder in _recorders)
                {
                    // Markers record nanoseconds of the previous frame: the long one just ended.
                    var ms = recorder.Valid ? recorder.LastValue / 1_000_000f : 0f;
                    _rows.AppendFormat(CultureInfo.InvariantCulture, "\t{0:F1}", ms);
                }

                _rows.Append('\n');
                Debug.LogWarning($"Hitch {frame * 1000f:F0} ms at {Place} (frame {Time.frameCount}).");
            }

            _lastTexture = texture;
            _lastDriver = driver;
            _lastCollections = collections;
        }

        /// <summary>Writes the rows so far; also done on quit.</summary>
        public void Flush()
        {
            if (!string.IsNullOrEmpty(_path))
            {
                File.WriteAllText(_path, _rows.ToString());
            }
        }

        private void OnApplicationQuit()
        {
            Flush();
        }

        private void OnDestroy()
        {
            foreach (var recorder in _recorders)
            {
                recorder.Dispose();
            }

            _recorders.Clear();
            if (Current == this)
            {
                Current = null;
            }
        }
    }
}
