using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

namespace ForgottenKingdom
{
    /// <summary>
    /// Measures a built game room by room: the hero is held still in the middle of every area of
    /// the world graph, the zones stream in as they would, and each room's frame times and memory
    /// go to a tab-separated report.
    ///
    /// Inert unless the game is started with <c>-perfProbe "report.tsv"</c>. Like the screenshot
    /// tour, it runs on a fresh game and puts the player's save files back before it quits.
    /// </summary>
    public sealed class PerformanceProbe : MonoBehaviour
    {
        private const float SettleSeconds = 1.5f;
        private const float SampleSeconds = 3f;

        private string _report;
        private string _shots;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartIfRequested()
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, "-perfProbe");
            if (index < 0 || index + 1 >= args.Length)
            {
                return;
            }

            // A/B for KI-012: a larger upload ring and slice keep big painted textures off the
            // main thread (-uploadBuffer 64 -uploadSlice 4); without the flags the project's values stay.
            var buffer = Array.IndexOf(args, "-uploadBuffer");
            if (buffer >= 0 && buffer + 1 < args.Length && int.TryParse(args[buffer + 1], out var megabytes))
            {
                QualitySettings.asyncUploadBufferSize = megabytes;
            }

            var slice = Array.IndexOf(args, "-uploadSlice");
            if (slice >= 0 && slice + 1 < args.Length && int.TryParse(args[slice + 1], out var milliseconds))
            {
                QualitySettings.asyncUploadTimeSlice = milliseconds;
            }

            var probe = new GameObject("PerformanceProbe").AddComponent<PerformanceProbe>();
            probe._report = args[index + 1];
            // -roomShots <folder>: a picture of every room as the probe stands in it, for the user to look the
            // zones through without playing (2026-11-12)
            var shots = Array.IndexOf(args, "-roomShots");
            probe._shots = shots >= 0 && shots + 1 < args.Length ? args[shots + 1] : null;
            DontDestroyOnLoad(probe.gameObject);
        }

        private IEnumerator Start()
        {
            Application.runInBackground = true;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            var stash = SaveStash.Take("perf-save-stash");
            SaveSystem.Delete();

            yield return SceneManager.LoadSceneAsync(MainMenuController.GameSceneName);
            yield return new WaitForSecondsRealtime(2f);

            var motor = FindAnyObjectByType<PlayerMotor2D>();
            var body = motor.GetComponent<Rigidbody2D>();
            var streamer = ZoneStreamer.Current;
            var graph = FindAnyObjectByType<GameProgress>().World;

            var hitches = HitchLog.Begin(Path.ChangeExtension(_report, null) + "-hitches.tsv");
            var lines = new StringBuilder(
                "zone\tarea\tavg_fps\tlow1_fps\tmax_ms\tallocated_mb\treserved_mb\ttexture_mb\tgfx_mb\n");
            var all = new List<float>();
            foreach (var area in graph.Areas.Where(a => a.WorldBounds.width > 0f))
            {
                var zone = graph.Zone(area.ZoneId);
                if (zone == null || !zone.IsImplemented)
                {
                    continue;
                }

                var point = area.WorldBounds.center;
                if (streamer != null)
                {
                    yield return streamer.PrepareAt(point);
                }

                hitches.Place = area.AreaId;
                if (!string.IsNullOrEmpty(_shots))
                {
                    // for the picture the hero stands on the floor under the room's middle and the camera frames
                    // him as in play, not hanging in the air in the middle of a hall (2026-11-12)
                    point = FloorUnder(point, area.WorldBounds);
                    // nothing may happen to the hero while he stands for the picture: no blows, no boss fight
                    // (a started fight held the camera in its arena for every room after it)
                    motor.GetComponent<CombatActor>()?.SetInvulnerable(ImmunitySource.Script, 9999f);
                    foreach (var encounter in FindObjectsByType<BossEncounter>(FindObjectsSortMode.None))
                    {
                        foreach (var trigger in encounter.GetComponents<Collider2D>())
                        {
                            trigger.enabled = false;
                        }
                    }

                    motor.enabled = true;
                    body.simulated = true;
                    motor.ResetTo(point);
                    yield return new WaitForSecondsRealtime(0.8f);
                }
                else
                {
                    motor.ResetTo(point);
                }

                motor.enabled = false;
                body.simulated = false;
                yield return new WaitForSecondsRealtime(SettleSeconds);
                if (!string.IsNullOrEmpty(_shots))
                {
                    Directory.CreateDirectory(_shots);
                    ScreenCapture.CaptureScreenshot(Path.Combine(_shots, $"{area.ZoneId}__{area.AreaId}.png"));
                    yield return null;
                    yield return null;
                }

                var frames = new List<float>();
                var end = Time.realtimeSinceStartup + SampleSeconds;
                while (Time.realtimeSinceStartup < end)
                {
                    yield return null;
                    frames.Add(Time.unscaledDeltaTime);
                }

                all.AddRange(frames);
                lines.Append(Row(area.ZoneId, area.AreaId, frames));
            }

            lines.Append(Row("ALL", "-", all));
            File.WriteAllText(_report, lines.ToString());
            hitches.Flush();

            // Progress writes itself on quit; it goes first so the restored files stay restored.
            Destroy(FindAnyObjectByType<GameProgress>());
            yield return null;
            stash.Restore();
            Application.Quit();
        }

        /// <summary>
        /// A point just above the ground under <paramref name="point"/> inside the room: up out of stone first,
        /// then down to the first ground. Without ground in the room, the point itself.
        /// </summary>
        private static Vector2 FloorUnder(Vector2 point, Rect room)
        {
            var ground = LayerMask.GetMask("Ground");
            var probe = point;
            while (probe.y < room.yMax && Physics2D.OverlapPoint(probe, ground) != null)
            {
                probe.y += 0.5f;
            }

            var hit = Physics2D.Raycast(probe, Vector2.down, probe.y - room.yMin, ground);
            return hit.collider != null ? hit.point + Vector2.up * 1.2f : point;
        }

        private static string Row(string zone, string area, List<float> frames)
        {
            const float mb = 1024f * 1024f;
            var sorted = frames.OrderByDescending(f => f).ToList();
            var worst = sorted.Take(Mathf.Max(1, sorted.Count / 100)).Average();
            return string.Format(CultureInfo.InvariantCulture,
                "{0}\t{1}\t{2:F1}\t{3:F1}\t{4:F1}\t{5:F0}\t{6:F0}\t{7:F0}\t{8:F0}\n",
                zone, area, frames.Count / frames.Sum(), 1f / worst, sorted[0] * 1000f,
                Profiler.GetTotalAllocatedMemoryLong() / mb, Profiler.GetTotalReservedMemoryLong() / mb,
                Texture.currentTextureMemory / mb, Profiler.GetAllocatedMemoryForGraphicsDriver() / mb);
        }
    }
}
