using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// Gives a room the shape of the way through it (2026-11-12, the user: «the hero is out of proportion with the
    /// rooms; rooms should differ in size and shape»; decided by Claude under the standing approval). The rooms were
    /// rectangles of air with a few ledges in them: most of the screen was empty backdrop and the hero was lost in it.
    ///
    /// The air the hero uses is measured, not guessed: <see cref="LevelReach"/> with every ability, strict and
    /// generous, traces the body through every move that lands, and the body standing on every floor it reaches.
    /// Air further from that space than a margin — a margin that wanders with noise, so the new stone edge is a
    /// cave's and not a ruler's — becomes stone. Doors, things in the room and enemies keep air round them; a room
    /// under the open sky keeps its sky (nothing is filled above the space it uses); boss arenas are left to hand.
    /// Only '.' turns into '#'; nothing else in the text changes. Then <see cref="LevelCheck"/> must stay clean.
    ///
    /// Run: -executeMethod ForgottenKingdom.EditorTools.LevelShape.Fill [-zone tomb] → the zone texts are rewritten,
    /// what changed goes to Artifacts/LevelShape.txt.
    /// </summary>
    public static class LevelShape
    {
        /// <summary>Air kept round the used space, in cells: the least, and how far noise widens it.</summary>
        private const int Margin = 3;
        private const int MarginNoise = 6;
        private const int ThingRoom = 4;
        private const int EnemyRoom = 7;
        private const int DoorRoom = 5;
        private const int TightMargin = 2;
        private const int TightNoise = 4;

        private static readonly string[] EveryAbility =
        {
            AbilityIds.Dash, AbilityIds.DoubleJump, AbilityIds.AirDash, AbilityIds.WallRun,
            AbilityIds.ShadowDash, AbilityIds.Grapple,
        };

        public static void Fill()
        {
            var args = Environment.GetCommandLineArgs();
            var at = Array.IndexOf(args, "-zone");
            var only = at >= 0 && at + 1 < args.Length ? args[at + 1] : null;
            // -rooms a,b: only those rooms, shaped tight — to the way the zone asks for (its abilities and each
            // gate's one at a time, the strict model), with a narrower margin: the room takes the shape of its path
            var roomsAt = Array.IndexOf(args, "-rooms");
            _tight = roomsAt >= 0 && roomsAt + 1 < args.Length ? new HashSet<string>(args[roomsAt + 1].Split(',')) : null;
            var report = new System.Text.StringBuilder();
            foreach (var path in Directory.GetFiles("Assets/Game/Levels", "*.txt").OrderBy(p => p))
            {
                var name = Path.GetFileNameWithoutExtension(path);
                if (only != null && name != only)
                {
                    continue;
                }

                var text = File.ReadAllText(path);
                var before = LevelCheck.Problems(LevelZone.Parse(text));
                var shaped = Shape(text, name, report);
                var after = LevelCheck.Problems(LevelZone.Parse(shaped));
                report.AppendLine($"== {name}: check before {before.Count}, after {after.Count}"
                    + (after.Count > before.Count ? " — NEW: " + string.Join(" | ", after.Except(before)) : string.Empty));
                if (after.Count <= before.Count)
                {
                    File.WriteAllText(path, shaped, new System.Text.UTF8Encoding(false));
                }
            }

            Directory.CreateDirectory("Artifacts");
            File.WriteAllText("Artifacts/LevelShape.txt", report.ToString());
            Debug.Log(report.ToString());
        }

        /// <summary>The zone's text with the dead air of its rooms turned to stone.</summary>
        private static HashSet<string> _tight;

        public static string Shape(string text, string zoneName, System.Text.StringBuilder report)
        {
            var zone = LevelZone.Parse(text);
            var used = UsedSpace(zone);
            var sky = SkyRooms(zoneName, zone);
            var fill = new HashSet<Vector2Int>();
            var seed = zoneName.GetHashCode() & 0xffff;
            foreach (var room in zone.Rooms)
            {
                if (!string.IsNullOrEmpty(room.BossId) || room.IsSecret || _tight != null && !_tight.Contains(room.Id))
                {
                    continue;
                }

                var margin = _tight != null ? TightMargin : Margin;
                var noise = _tight != null ? TightNoise : MarginNoise;

                var keep = new HashSet<Vector2Int>(used.Where(room.Contains));
                foreach (var marker in room.Markers)
                {
                    var radius = marker.Kind == "enemy" ? EnemyRoom : ThingRoom;
                    Around(keep, marker.Cell, radius);
                }

                // the openings in the room's edge: the way on, whatever the model did there
                for (var x = 0; x < room.Width; x++)
                {
                    foreach (var y in new[] { 0, room.Height - 1 })
                    {
                        if (room.Local(x, y) == LevelCell.Empty) Around(keep, room.Origin + new Vector2Int(x, y), DoorRoom);
                    }
                }

                for (var y = 0; y < room.Height; y++)
                {
                    foreach (var x in new[] { 0, room.Width - 1 })
                    {
                        if (room.Local(x, y) == LevelCell.Empty) Around(keep, room.Origin + new Vector2Int(x, y), DoorRoom);
                    }
                }

                if (keep.Count == 0)
                {
                    continue;
                }

                var distance = Distances(room, keep);
                var open = sky.Contains(room.Id);
                var top = new int[room.Width];
                for (var x = 0; x < room.Width; x++)
                {
                    top[x] = -1;
                    for (var y = room.Height - 1; y >= 0 && top[x] < 0; y--)
                    {
                        if (keep.Contains(room.Origin + new Vector2Int(x, y))) top[x] = y;
                    }
                }

                var count = 0;
                for (var y = 0; y < room.Height; y++)
                {
                    for (var x = 0; x < room.Width; x++)
                    {
                        if (room.Local(x, y) != LevelCell.Empty)
                        {
                            continue;
                        }

                        // under the open sky the air above what is used stays air, as far as the nearest used column
                        if (open && y > NearTop(top, x))
                        {
                            continue;
                        }

                        var cell = room.Origin + new Vector2Int(x, y);
                        var wander = Mathf.PerlinNoise((cell.x + seed) * 0.11f, (cell.y - seed) * 0.11f);
                        if (distance[x, y] > margin + wander * noise)
                        {
                            fill.Add(cell);
                            count++;
                        }
                    }
                }

                if (count > 0)
                {
                    report.AppendLine($"{room.Id}: {count} cells of air to stone ({100 * count / (room.Width * room.Height)}% of the room)");
                }
            }

            Tidy(zone, fill, used);
            foreach (var room in zone.Rooms.Where(r => !string.IsNullOrEmpty(r.BossId) || r.IsSecret))
            {
                var stray = fill.Count(room.Contains);
                if (stray > 0) report.AppendLine($"{room.Id}: {stray} stray cells kept as air (boss or secret room)");
                fill.RemoveWhere(room.Contains);
            }

            return Write(text, zone, fill);
        }

        /// <summary>Every cell the hero's body passes through, with every ability, by either model.</summary>
        private static HashSet<Vector2Int> UsedSpace(LevelZone zone)
        {
            var start = zone.Markers().FirstOrDefault(m => m.Kind == "start");
            var space = new HashSet<Vector2Int>();
            if (start == null)
            {
                return space;
            }

            var kits = new List<HashSet<string>> { new HashSet<string>(zone.Abilities) };
            if (_tight == null)
            {
                kits.Add(new HashSet<string>(EveryAbility));
            }
            else
            {
                foreach (var gate in zone.Rooms.Select(r => r.NeedsAbility).Where(a => !string.IsNullOrEmpty(a)).Distinct())
                {
                    kits.Add(new HashSet<string>(zone.Abilities) { gate });
                }
            }

            foreach (var kit in kits)
            {
                foreach (var generous in _tight == null ? new[] { false, true } : new[] { false })
                {
                    var reach = new LevelReach(zone, kit, generous: generous, trace: true);
                    var from = reach.SegmentUnder(start.Cell);
                    if (from < 0)
                    {
                        continue;
                    }

                    // forward from the start, and what leads back to it: a floor reached only from another
                    // zone's door still counts if the way home starts there
                    var reached = reach.Forward(from);
                    space.UnionWith(reach.Space(reached));
                }
            }

            return space;
        }

        private static int NearTop(int[] top, int x)
        {
            var best = -1;
            for (var d = 0; d < top.Length; d++)
            {
                if (x - d >= 0 && top[x - d] >= 0) best = Mathf.Max(best, top[x - d]);
                if (x + d < top.Length && top[x + d] >= 0) best = Mathf.Max(best, top[x + d]);
                if (best >= 0) return best + Margin + 4;
            }

            return int.MaxValue;
        }

        private static void Around(HashSet<Vector2Int> keep, Vector2Int at, int radius)
        {
            for (var dy = -radius; dy <= radius; dy++)
            {
                for (var dx = -radius; dx <= radius; dx++)
                {
                    if (dx * dx + dy * dy <= radius * radius) keep.Add(at + new Vector2Int(dx, dy));
                }
            }
        }

        /// <summary>Steps from each air cell of the room to the nearest kept cell, through air (8-way).</summary>
        private static int[,] Distances(LevelRoom room, HashSet<Vector2Int> keep)
        {
            var d = new int[room.Width, room.Height];
            var queue = new Queue<Vector2Int>();
            for (var y = 0; y < room.Height; y++)
            {
                for (var x = 0; x < room.Width; x++)
                {
                    d[x, y] = int.MaxValue;
                    if (keep.Contains(room.Origin + new Vector2Int(x, y)))
                    {
                        d[x, y] = 0;
                        queue.Enqueue(new Vector2Int(x, y));
                    }
                }
            }

            while (queue.Count > 0)
            {
                var p = queue.Dequeue();
                for (var dy = -1; dy <= 1; dy++)
                {
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        var q = new Vector2Int(p.x + dx, p.y + dy);
                        if (q.x < 0 || q.y < 0 || q.x >= room.Width || q.y >= room.Height || d[q.x, q.y] <= d[p.x, p.y] + 1)
                        {
                            continue;
                        }

                        d[q.x, q.y] = d[p.x, p.y] + 1;
                        queue.Enqueue(q);
                    }
                }
            }

            return d;
        }

        /// <summary>
        /// No crumbs: a fill cell with fewer than three filled or stone neighbours stays air, and a pocket of air
        /// shut in by the new stone (out of the used space, under 16 cells) is filled too.
        /// </summary>
        private static void Tidy(LevelZone zone, HashSet<Vector2Int> fill, HashSet<Vector2Int> used)
        {
            bool Stone(Vector2Int c) => fill.Contains(c) || zone.IsSolid(c) && zone.At(c) != LevelCell.Ledge;
            for (var pass = 0; pass < 2; pass++)
            {
                var drop = fill.Where(c =>
                {
                    var n = 0;
                    if (Stone(c + Vector2Int.left)) n++;
                    if (Stone(c + Vector2Int.right)) n++;
                    if (Stone(c + Vector2Int.up)) n++;
                    if (Stone(c + Vector2Int.down)) n++;
                    return n < 2;
                }).ToList();
                foreach (var c in drop) fill.Remove(c);
            }

            foreach (var room in zone.Rooms)
            {
                var seen = new HashSet<Vector2Int>();
                for (var y = 0; y < room.Height; y++)
                {
                    for (var x = 0; x < room.Width; x++)
                    {
                        var start = room.Origin + new Vector2Int(x, y);
                        if (seen.Contains(start) || room.Local(x, y) != LevelCell.Empty || fill.Contains(start)) continue;
                        var pocket = new List<Vector2Int>();
                        var touchesUse = false;
                        var queue = new Queue<Vector2Int>();
                        queue.Enqueue(start);
                        seen.Add(start);
                        while (queue.Count > 0 && pocket.Count < 64)
                        {
                            var p = queue.Dequeue();
                            pocket.Add(p);
                            if (used.Contains(p)) touchesUse = true;
                            foreach (var q in new[] { p + Vector2Int.left, p + Vector2Int.right, p + Vector2Int.up, p + Vector2Int.down })
                            {
                                if (!room.Contains(q)) { touchesUse = true; continue; }
                                if (seen.Contains(q) || fill.Contains(q) || zone.At(q) != LevelCell.Empty) continue;
                                seen.Add(q);
                                queue.Enqueue(q);
                            }
                        }

                        if (!touchesUse && queue.Count == 0 && pocket.Count < 16) fill.UnionWith(pocket);
                    }
                }
            }
        }

        /// <summary>
        /// Rooms under the open sky, as the painter sorts them (design/levels/painter/looks.js): the Village's and
        /// the Capital's <c>open</c>, every Forest and Marsh room not <c>enclosed</c> or <c>underground</c>.
        /// </summary>
        private static HashSet<string> SkyRooms(string zoneName, LevelZone zone)
        {
            var looks = File.ReadAllText("design/levels/painter/looks.js");
            var block = Regex.Match(looks, $@"\n    {zoneName}: \{{(.*?)\n    \}}", RegexOptions.Singleline).Groups[1].Value;
            HashSet<string> List(string key)
            {
                var m = Regex.Match(block, $@"\b{key}: \[(.*?)\]", RegexOptions.Singleline);
                return new HashSet<string>(Regex.Matches(m.Groups[1].Value, "\"([^\"]+)\"").Cast<Match>().Select(x => x.Groups[1].Value));
            }

            if (zoneName == "village" || zoneName == "capital")
            {
                return List("open");
            }

            if (zoneName == "forest" || zoneName == "marsh")
            {
                var closed = List("enclosed");
                closed.UnionWith(List("underground"));
                return new HashSet<string>(zone.Rooms.Select(r => r.Id).Where(id => !closed.Contains(id)));
            }

            return new HashSet<string>();
        }

        private static string Write(string text, LevelZone zone, HashSet<Vector2Int> fill)
        {
            var newline = text.Contains("\r\n") ? "\r\n" : "\n";
            var lines = text.Replace("\r\n", "\n").Split('\n');
            foreach (var room in zone.Rooms)
            {
                var start = Array.FindIndex(lines, l => l.StartsWith($"room {room.Id} ", StringComparison.Ordinal));
                var grid = Array.FindIndex(lines, start, l => l.Trim() == "grid") + 1;
                for (var r = 0; r < room.Height; r++)
                {
                    var y = room.Height - 1 - r;
                    var row = lines[grid + r].ToCharArray();
                    for (var x = 0; x < room.Width && x < row.Length; x++)
                    {
                        if (row[x] == '.' && fill.Contains(room.Origin + new Vector2Int(x, y)))
                        {
                            row[x] = '#';
                        }
                    }

                    lines[grid + r] = new string(row);
                }
            }

            return string.Join(newline, lines);
        }
    }
}
