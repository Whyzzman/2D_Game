using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// Looking at a layout before it is built: every `Levels/*.txt` checked (<see cref="LevelCheck"/>)
    /// and drawn as a picture — rock, ledges, spikes, things, and in green the floors the hero can
    /// reach from the start with the zone's promised abilities. Pictures go to
    /// `Artifacts/LevelPreviews/<file>.png`, next to the reference maps they are compared with.
    /// Run: -executeMethod ForgottenKingdom.EditorTools.LevelTools.RenderPreviews
    /// </summary>
    public static class LevelTools
    {
        private const string OutputFolder = "Artifacts/LevelPreviews";
        private const int Pixels = 4;

        [MenuItem("Forgotten Kingdom/Levels/Check and Draw Layouts")]
        public static void RenderPreviews()
        {
            Directory.CreateDirectory(OutputFolder);
            if (!Directory.Exists(ContentBuilder.LevelFolder))
            {
                Debug.Log("No layouts yet.");
                return;
            }

            foreach (var path in Directory.GetFiles(ContentBuilder.LevelFolder, "*.txt"))
            {
                var zone = LevelZone.Parse(File.ReadAllText(path));
                var problems = LevelCheck.Problems(zone);
                var name = Path.GetFileNameWithoutExtension(path);
                File.WriteAllBytes(Path.Combine(OutputFolder, name + ".png"), Draw(zone).EncodeToPNG());
                Debug.Log(problems.Count == 0
                    ? $"Layout {name}: OK ({zone.Rooms.Count} rooms)."
                    : $"Layout {name}: {problems.Count} problem(s):\n  " + string.Join("\n  ", problems));
            }
        }

        public static Texture2D Draw(LevelZone zone)
        {
            if (zone.Rooms.Count == 0)
            {
                return new Texture2D(1, 1);
            }

            var bounds = zone.Rooms[0].Bounds;
            foreach (var room in zone.Rooms)
            {
                bounds.xMin = Mathf.Min(bounds.xMin, room.Bounds.xMin);
                bounds.yMin = Mathf.Min(bounds.yMin, room.Bounds.yMin);
                bounds.xMax = Mathf.Max(bounds.xMax, room.Bounds.xMax);
                bounds.yMax = Mathf.Max(bounds.yMax, room.Bounds.yMax);
            }

            var texture = new Texture2D(bounds.width * Pixels, bounds.height * Pixels, TextureFormat.RGBA32, false);
            var pixels = new Color32[texture.width * texture.height];
            for (var i = 0; i < pixels.Length; i++) { pixels[i] = new Color32(8, 8, 10, 255); }

            foreach (var room in zone.Rooms)
            {
                var air = room.IsSecret ? new Color32(52, 34, 60, 255)
                    : !string.IsNullOrEmpty(room.BossId) ? new Color32(70, 24, 24, 255)
                    : room.IsOptional ? new Color32(30, 40, 52, 255) : new Color32(34, 36, 44, 255);
                for (var y = 0; y < room.Height; y++)
                {
                    for (var x = 0; x < room.Width; x++)
                    {
                        var colour = room.Local(x, y) switch
                        {
                            LevelCell.Solid => new Color32(96, 92, 88, 255),
                            LevelCell.Smooth => new Color32(80, 96, 118, 255),
                            LevelCell.Ledge => new Color32(128, 98, 64, 255),
                            LevelCell.Spikes => new Color32(190, 40, 40, 255),
                            _ => air
                        };
                        Fill(pixels, texture.width, room.Origin.x + x - bounds.x, room.Origin.y + y - bounds.y, colour, 0);
                    }
                }
            }

            Reached(zone, bounds, pixels, texture.width);
            foreach (var marker in zone.Markers())
            {
                Fill(pixels, texture.width, marker.Cell.x - bounds.x, marker.Cell.y - bounds.y, MarkerColour(marker.Kind), 0);
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        private static void Reached(LevelZone zone, RectInt bounds, Color32[] pixels, int width)
        {
            var start = LevelCheck.Start(zone);
            if (start == null || !zone.IsValid)
            {
                return;
            }

            var reach = new LevelReach(zone, new HashSet<string>(zone.Abilities));
            var from = reach.SegmentUnder(start.Cell);
            if (from < 0)
            {
                return;
            }

            foreach (var index in reach.Forward(from))
            {
                var segment = reach.Segments[index];
                if (segment.IsRing)
                {
                    continue;
                }

                for (var x = segment.XMin - 1; x <= segment.XMax; x++)
                {
                    // A thin green line along the floor, a pixel above it.
                    var px = (x - bounds.x) * Pixels;
                    var py = (segment.Row - bounds.y) * Pixels;
                    for (var i = 0; i < Pixels; i++)
                    {
                        Put(pixels, width, px + i, py, new Color32(70, 200, 90, 255));
                    }
                }
            }
        }

        private static Color32 MarkerColour(string kind) => kind switch
        {
            "start" => new Color32(255, 255, 255, 255),
            "altar" => new Color32(250, 200, 70, 255),
            "enemy" => new Color32(230, 60, 60, 255),
            "gold" => new Color32(240, 220, 120, 255),
            "item" => new Color32(150, 230, 255, 255),
            "ring" => new Color32(80, 220, 230, 255),
            "urn" => new Color32(200, 150, 100, 255),
            _ => new Color32(255, 0, 255, 255)
        };

        private static void Fill(Color32[] pixels, int width, int cx, int cy, Color32 colour, int inset)
        {
            for (var y = inset; y < Pixels - inset; y++)
            {
                for (var x = inset; x < Pixels - inset; x++)
                {
                    Put(pixels, width, cx * Pixels + x, cy * Pixels + y, colour);
                }
            }
        }

        private static void Put(Color32[] pixels, int width, int x, int y, Color32 colour)
        {
            var index = y * width + x;
            if (x >= 0 && x < width && index >= 0 && index < pixels.Length)
            {
                pixels[index] = colour;
            }
        }
    }
}
