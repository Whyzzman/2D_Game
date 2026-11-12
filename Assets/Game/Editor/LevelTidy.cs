using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// Tidies the parkour of a zone's text (user's request 2026-10-27: «comfortable and logical»): ledges
    /// that hang a step away from a wall grow into it like a bracket, slivers widen, two pieces on one row
    /// close their gap, a ledge set a hair too high for a comfortable jump comes down a row, and ledges
    /// that lead nowhere go. Every change is kept only if the zone still checks clean (<see cref="LevelCheck"/>),
    /// every stone floor and every lettered thing reached before is reached still, and nothing the
    /// comfortable jump reached is lost. Only <c>=</c> cells change; stone and the room edges stay.
    /// Runs off the main thread (a zone takes minutes); progress goes to <c>Artifacts/LevelTidy/&lt;zone&gt;.log</c>.
    /// </summary>
    public static class LevelTidy
    {
        /// <summary>The comfortable jump, as a share of the strict model's (which is itself 0,97 of the hero's).</summary>
        private const float ComfortJump = 0.95f;
        private const float ComfortRun = 0.95f;
        /// <summary>A ledge ending this close to a wall grows into it.</summary>
        private const int AttachGap = 3;
        private const int MinWidth = 4;
        private const int MergeGap = 2;

        private sealed class Ledge
        {
            public LevelRoom Room;
            public int Y;
            public int X0;
            public int X1;
            public int Width => X1 - X0 + 1;
            public override string ToString() => $"{Room.Id} y{Y} x{X0}…{X1}";
        }

        private sealed class Baseline
        {
            public HashSet<long> Strict;
            public HashSet<long> Comfort;
            /// <summary>Every cell stood on, ledges included: a ledge that only leads to other ledges still leads somewhere.</summary>
            public HashSet<long> All;
            /// <summary>
            /// The same for the comfortable jump. Without it a shaft's steps went as «dead ends» while the
            /// room was still reached by a jump at the very edge — and a slightly different hero lost it.
            /// </summary>
            public HashSet<long> ComfortAll;
            /// <summary>From each stone floor, how many moves to each other: a second, shorter way up is a way, not a spare.</summary>
            public Dictionary<long, Dictionary<long, int>> Ways;
            public Dictionary<LevelMarker, int> MarkerRows;
        }

        public static System.Threading.Tasks.Task Start(string zoneName)
        {
            var path = $"Assets/Game/Levels/{zoneName}.txt";
            var text = File.ReadAllText(path);
            var comfort = Comfort();
            Directory.CreateDirectory("Artifacts/LevelTidy");
            var logPath = $"Artifacts/LevelTidy/{zoneName}.log";
            File.WriteAllText(logPath, "started\n");
            return System.Threading.Tasks.Task.Run(() =>
            {
                var log = new System.Text.StringBuilder();
                void Log(string line)
                {
                    log.AppendLine(line);
                    File.WriteAllText(logPath, log.ToString());
                }

                try
                {
                    var result = Tidy(text, comfort, Log);
                    if (result != null)
                    {
                        File.WriteAllText(path, result, new System.Text.UTF8Encoding(false));
                    }

                    Log("DONE");
                }
                catch (Exception e)
                {
                    Log("ERROR " + e);
                }
            });
        }

        /// <summary>
        /// Where a zone asks for a jump at the very edge: every floor and ledge the strict model reaches
        /// and the comfortable one does not, by room, for the zone's own abilities and with every gate's.
        /// Also the zone's <see cref="LevelCheck"/> problems. Goes to Artifacts/LevelComfort.txt.
        /// Run: -executeMethod ForgottenKingdom.EditorTools.LevelTidy.ComfortReport
        /// </summary>
        public static void ComfortReport()
        {
            var comfort = Comfort();
            var report = new System.Text.StringBuilder();
            foreach (var path in Directory.GetFiles("Assets/Game/Levels", "*.txt"))
            {
                var zone = LevelZone.Parse(File.ReadAllText(path));
                var problems = LevelCheck.Problems(zone);
                report.AppendLine($"== {Path.GetFileNameWithoutExtension(path)}: "
                    + (problems.Count == 0 ? "LevelCheck OK" : string.Join(" | ", problems)));
                var own = new HashSet<string>(zone.Abilities);
                var every = new HashSet<string>(own);
                foreach (var room in zone.Rooms)
                {
                    if (!string.IsNullOrEmpty(room.NeedsAbility))
                    {
                        every.Add(room.NeedsAbility);
                    }
                }

                HardSpots(zone, own, comfort, "own abilities", report);
                if (every.Count > own.Count)
                {
                    HardSpots(zone, every, comfort, "every gate's ability", report);
                }
            }

            Directory.CreateDirectory("Artifacts");
            File.WriteAllText("Artifacts/LevelComfort.txt", report.ToString());
        }

        /// <summary>
        /// KI-009: rooms built for one ability that every other ability of the game reaches without it.
        /// Run: -executeMethod ForgottenKingdom.EditorTools.LevelTidy.BypassReport → Artifacts/LevelBypasses.txt
        /// </summary>
        public static void BypassReport()
        {
            var every = new[] { AbilityIds.Dash, AbilityIds.DoubleJump, AbilityIds.AirDash, AbilityIds.WallRun,
                AbilityIds.ShadowDash, AbilityIds.Grapple };
            var report = new System.Text.StringBuilder();
            foreach (var path in Directory.GetFiles(LevelsDir(), "*.txt"))
            {
                var bypasses = LevelCheck.Bypasses(LevelZone.Parse(File.ReadAllText(path)), every);
                report.AppendLine($"== {Path.GetFileNameWithoutExtension(path)}: "
                    + (bypasses.Count == 0 ? "no bypass" : string.Join(" | ", bypasses)));
            }

            Directory.CreateDirectory("Artifacts");
            File.WriteAllText(LevelsDir() == "Assets/Game/Levels" ? "Artifacts/LevelBypasses.txt" : "Artifacts/LevelBypasses-other.txt",
                report.ToString());
        }

        /// <summary>The zone texts to report on: <c>-levels &lt;dir&gt;</c> (another version, to compare), else the game's.</summary>
        private static string LevelsDir()
        {
            var args = Environment.GetCommandLineArgs();
            var at = Array.IndexOf(args, "-levels");
            return at >= 0 && at + 1 < args.Length ? args[at + 1] : "Assets/Game/Levels";
        }

        private static void HardSpots(LevelZone zone, HashSet<string> abilities, MovementSettings comfort,
            string label, System.Text.StringBuilder report)
        {
            var start = LevelCheck.Start(zone).Cell;
            var strict = new LevelReach(zone, abilities);
            var loose = new LevelReach(zone, abilities, comfort);
            var easy = new HashSet<long>();
            foreach (var index in loose.Forward(loose.SegmentUnder(start)))
            {
                var segment = loose.Segments[index];
                if (!segment.IsRing)
                {
                    for (var x = segment.XMin; x <= segment.XMax; x++) easy.Add(Pack(x, segment.Row));
                }
            }

            report.AppendLine($"  -- {label} ({string.Join(" ", abilities)})");
            foreach (var index in strict.Forward(strict.SegmentUnder(start)).OrderBy(i => strict.Segments[i].RoomId))
            {
                var segment = strict.Segments[index];
                if (segment.IsRing)
                {
                    continue;
                }

                var hard = false;
                for (var x = segment.XMin; x <= segment.XMax && !hard; x++) hard = !easy.Contains(Pack(x, segment.Row));
                if (hard)
                {
                    var room = zone.Room(segment.RoomId);
                    var local = room == null ? "" : $" (in room: x {segment.XMin - room.Origin.x}…{segment.XMax - room.Origin.x}, "
                        + $"row from top {room.Origin.y + room.Height - segment.Row})";
                    report.AppendLine($"     {segment}{local}");
                }
            }
        }

        private static MovementSettings Comfort()
        {
            var source = new MovementSettings();
            var scaled = new MovementSettings();
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(source), scaled);
            string Inv(float v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
            JsonUtility.FromJsonOverwrite("{\"_jumpSpeed\":" + Inv(source.JumpSpeed * ComfortJump)
                + ",\"_airJumpSpeed\":" + Inv(source.AirJumpSpeed * ComfortJump)
                + ",\"_runSpeed\":" + Inv(source.RunSpeed * ComfortRun) + "}", scaled);
            return scaled;
        }

        private static string Tidy(string text, MovementSettings comfort, Action<string> log)
        {
            var zone = LevelZone.Parse(text);
            var problems = LevelCheck.Problems(zone);
            if (problems.Count > 0)
            {
                log("zone does not check clean, nothing done:\n  " + string.Join("\n  ", problems));
                return null;
            }

            var abilities = new HashSet<string>(zone.Abilities);
            var baseline = Measure(zone, abilities, comfort);
            log($"baseline: {baseline.Strict.Count} stone cells reached, {baseline.Comfort.Count} comfortably, {baseline.MarkerRows.Count} things");
            var changed = 0;

            // How many moves a way between two floors may grow by: none, except while anchoring slabs to the
            // architecture — a balcony may cost a jump, and a logical room is worth it.
            var slack = 0;
            var reject = "";

            bool Keep(string what, HashSet<long> gone = null)
            {
                var now = Measure(zone, abilities, comfort);
                if (!now.Strict.IsSupersetOf(baseline.Strict) || !now.Comfort.IsSupersetOf(baseline.Comfort))
                {
                    reject = "a floor lost";
                    return false;
                }

                if (baseline.All.Any(c => !now.All.Contains(c) && (gone == null || !gone.Contains(c)))
                    || baseline.ComfortAll.Any(c => !now.ComfortAll.Contains(c) && (gone == null || !gone.Contains(c))))
                {
                    reject = "a ledge lost";
                    return false;
                }

                foreach (var way in baseline.Ways)
                {
                    if (!now.Ways.TryGetValue(way.Key, out var reached))
                    {
                        return false;
                    }

                    foreach (var to in way.Value)
                    {
                        if (!reached.TryGetValue(to.Key, out var moves) || moves > to.Value + slack)
                        {
                            reject = "a way longer";
                            return false;
                        }
                    }
                }

                foreach (var pair in baseline.MarkerRows)
                {
                    if (!now.MarkerRows.TryGetValue(pair.Key, out var row) || row != pair.Value)
                    {
                        reject = "a thing moved";
                        return false;
                    }
                }

                if (LevelCheck.Problems(zone).Count > 0)
                {
                    reject = "LevelCheck";
                    return false;
                }

                // what the comfortable jump newly reaches is kept from now on
                baseline.Comfort.UnionWith(now.Comfort);
                baseline.All = now.All;
                baseline.ComfortAll = now.ComfortAll;
                if (slack == 0) baseline.Ways = now.Ways; // the slack is counted from the ways before anchoring, never piled up
                changed++;
                log("  " + what);
                return true;
            }

            bool Try(Ledge ledge, List<(int x, int y)> fill, List<(int x, int y)> clear, string what)
            {
                if (fill.Any(c => !Free(ledge.Room, c.x, c.y)))
                {
                    return false;
                }

                // the cells stood on above a ledge that goes may go with it, and nothing else
                var gone = new HashSet<long>();
                foreach (var c in clear)
                {
                    var cell = ledge.Room.Origin + new Vector2Int(c.x, c.y + 1);
                    for (var dx = -1; dx <= 1; dx++) gone.Add(Pack(cell.x + dx, cell.y));
                }

                foreach (var c in fill) ledge.Room.Cells[c.y, c.x] = LevelCell.Ledge;
                foreach (var c in clear) ledge.Room.Cells[c.y, c.x] = LevelCell.Empty;
                if (Keep(what, gone))
                {
                    return true;
                }

                foreach (var c in fill) ledge.Room.Cells[c.y, c.x] = LevelCell.Empty;
                foreach (var c in clear) ledge.Room.Cells[c.y, c.x] = LevelCell.Ledge;
                return false;
            }

            List<(int, int)> Span(int y, int from, int to)
            {
                var list = new List<(int, int)>();
                for (var x = Math.Min(from, to); x <= Math.Max(from, to); x++) list.Add((x, y));
                return list;
            }

            // 0. The first step (user, 2026-10-28: «I cannot jump onto the first block»): a ledge
            // should be reached by a comfortable jump from the floor right under it. If not, it comes
            // down a row or three, or a step is put in under its end.
            log("step up:");
            foreach (var ledge in Ledges(zone))
            {
                if (ReachedFromBelow(zone, abilities, comfort, ledge))
                {
                    continue;
                }

                var done = false;
                for (var down = 1; down <= 3 && !done; down++)
                {
                    var target = ledge.Y - down;
                    var fill = Span(target, ledge.X0, ledge.X1);
                    if (target <= 0 || !fill.All(c => Free(ledge.Room, c.Item1, c.Item2))
                        || !AirAbove(ledge.Room, target, ledge.X0, ledge.X1))
                    {
                        break;
                    }

                    foreach (var c in fill) ledge.Room.Cells[c.Item2, c.Item1] = LevelCell.Ledge;
                    foreach (var c in Span(ledge.Y, ledge.X0, ledge.X1)) ledge.Room.Cells[c.Item2, c.Item1] = LevelCell.Empty;
                    var moved = new Ledge { Room = ledge.Room, Y = target, X0 = ledge.X0, X1 = ledge.X1 };
                    if (ReachedFromBelow(zone, abilities, comfort, moved) && Keep($"{ledge} comes down {down} to be reached from the floor"))
                    {
                        done = true;
                    }
                    else
                    {
                        foreach (var c in fill) ledge.Room.Cells[c.Item2, c.Item1] = LevelCell.Empty;
                        foreach (var c in Span(ledge.Y, ledge.X0, ledge.X1)) ledge.Room.Cells[c.Item2, c.Item1] = LevelCell.Ledge;
                    }
                }

                // a step three rows lower beside either end, or right under the near end
                var floorY = FloorUnder(ledge.Room, ledge.Y, (ledge.X0 + ledge.X1) / 2);
                if (done || floorY < 0)
                {
                    continue;
                }

                foreach (var stepY in new[] { ledge.Y - 3, ledge.Y - 2 })
                {
                    if (done || stepY - floorY < 2)
                    {
                        break;
                    }

                    foreach (var (x0, x1) in new[] { (ledge.X0 - 5, ledge.X0 - 2), (ledge.X1 + 2, ledge.X1 + 5), (ledge.X0, ledge.X0 + 3), (ledge.X1 - 3, ledge.X1) })
                    {
                        var fill = Span(stepY, x0, x1);
                        if (!fill.All(c => Free(ledge.Room, c.Item1, c.Item2)) || !AirAbove(ledge.Room, stepY, x0, x1))
                        {
                            continue;
                        }

                        foreach (var c in fill) ledge.Room.Cells[c.Item2, c.Item1] = LevelCell.Ledge;
                        var step = new Ledge { Room = ledge.Room, Y = stepY, X0 = x0, X1 = x1 };
                        if (ReachedFromBelow(zone, abilities, comfort, step) && ReachedFromLedge(zone, abilities, comfort, step, ledge)
                            && Keep($"{ledge} gets a step under it at y{stepY} x{x0}…{x1}"))
                        {
                            done = true;
                            break;
                        }

                        foreach (var c in fill) ledge.Room.Cells[c.Item2, c.Item1] = LevelCell.Empty;
                    }
                }

                if (!done)
                {
                    log($"  {ledge}: not reached from the floor under it, no fix found");
                }
            }

            // 1. Brackets: a ledge a short step from a wall grows into it.
            log("attach:");
            foreach (var ledge in Ledges(zone))
            {
                var left = GapToWall(ledge.Room, ledge.Y, ledge.X0, -1);
                var right = GapToWall(ledge.Room, ledge.Y, ledge.X1, 1);
                if (left == 0 || right == 0)
                {
                    continue;
                }

                if (left <= AttachGap && (left <= right || right > AttachGap))
                {
                    Try(ledge, Span(ledge.Y, ledge.X0 - left, ledge.X0 - 1), new List<(int, int)>(), $"{ledge} grows {left} into the west wall");
                }
                else if (right <= AttachGap)
                {
                    Try(ledge, Span(ledge.Y, ledge.X1 + 1, ledge.X1 + right), new List<(int, int)>(), $"{ledge} grows {right} into the east wall");
                }
            }

            // 2. Two pieces on one row with a crack between them become one.
            log("merge:");
            foreach (var room in zone.Rooms)
            {
                var row = Ledges(zone).Where(l => l.Room == room).OrderBy(l => l.Y).ThenBy(l => l.X0).ToList();
                for (var i = 0; i + 1 < row.Count; i++)
                {
                    var a = row[i];
                    var b = row[i + 1];
                    var gap = b.X0 - a.X1 - 1;
                    if (a.Y == b.Y && gap > 0 && gap <= MergeGap)
                    {
                        Try(a, Span(a.Y, a.X1 + 1, b.X0 - 1), new List<(int, int)>(), $"{a} and {b} close a gap of {gap}");
                    }
                }
            }

            // 3. Slivers widen to a comfortable landing.
            log("widen:");
            foreach (var ledge in Ledges(zone).Where(l => l.Width < MinWidth))
            {
                var need = MinWidth - ledge.Width;
                var west = need / 2;
                var east = need - west;
                if (!Try(ledge, Span(ledge.Y, ledge.X0 - west, ledge.X1 + east), new List<(int, int)>(), $"{ledge} widens to {MinWidth}"))
                {
                    if (!Try(ledge, Span(ledge.Y, ledge.X1 + 1, ledge.X1 + need), new List<(int, int)>(), $"{ledge} widens east to {MinWidth}"))
                    {
                        Try(ledge, Span(ledge.Y, ledge.X0 - need, ledge.X0 - 1), new List<(int, int)>(), $"{ledge} widens west to {MinWidth}");
                    }
                }
            }

            // 4. A ledge a hair too high comes down a row, if that lets the comfortable jump reach more.
            log("lower:");
            foreach (var ledge in Ledges(zone))
            {
                if (ledge.Y <= 1)
                {
                    continue;
                }

                var before = baseline.Comfort.Count;
                var down = Span(ledge.Y - 1, ledge.X0, ledge.X1);
                var up = Span(ledge.Y, ledge.X0, ledge.X1);
                if (!down.All(c => Free(ledge.Room, c.Item1, c.Item2)) || !AirAbove(ledge.Room, ledge.Y - 1, ledge.X0, ledge.X1))
                {
                    continue;
                }

                foreach (var c in down) ledge.Room.Cells[c.Item2, c.Item1] = LevelCell.Ledge;
                foreach (var c in up) ledge.Room.Cells[c.Item2, c.Item1] = LevelCell.Empty;
                var now = Measure(zone, abilities, comfort);
                var better = now.Comfort.Count > before && now.Comfort.IsSupersetOf(baseline.Comfort);
                if (!better || !Keep($"{ledge} comes down a row"))
                {
                    foreach (var c in down) ledge.Room.Cells[c.Item2, c.Item1] = LevelCell.Empty;
                    foreach (var c in up) ledge.Room.Cells[c.Item2, c.Item1] = LevelCell.Ledge;
                }
            }

            // 4b. Anchors (2026-11-02, user: «every parkour and wall logical and pleasant»): a slab hanging
            // in mid-hall reads as nonsense in a stone crypt. It moves along its row to the nearer wall
            // (a balcony), or reaches it (a bridge), or stands on a two-wide column from the floor under it.
            // Rooms built around chains keep their hanging slabs.
            log("anchor:");
            slack = 2;
            foreach (var ledge in Ledges(zone))
            {
                if (ledge.Room.Id.Contains("chain") || GapToWall(ledge.Room, ledge.Y, ledge.X0, -1) == 0
                    || GapToWall(ledge.Room, ledge.Y, ledge.X1, 1) == 0)
                {
                    continue;
                }

                reject = "no wall in reach, no free cells or floor for a column";
                var left = WallDistance(ledge.Room, ledge.Y, ledge.X0, -1, 20);
                var right = WallDistance(ledge.Room, ledge.Y, ledge.X1, 1, 20);
                var here = Span(ledge.Y, ledge.X0, ledge.X1);
                var done = false;
                // move to the nearer wall, then the farther
                foreach (var (gap, side) in new[] { (left, -1), (right, 1) }.Where(o => o.Item1 > 0).OrderBy(o => o.Item1))
                {
                    if (done) break;
                    var x0 = side < 0 ? ledge.X0 - gap : ledge.X0 + gap;
                    var moved = Span(ledge.Y, x0, x0 + ledge.Width - 1);
                    var newCells = moved.Where(c => c.Item1 < ledge.X0 || c.Item1 > ledge.X1).ToList();
                    var oldCells = here.Where(c => c.Item1 < x0 || c.Item1 > x0 + ledge.Width - 1).ToList();
                    if (newCells.All(c => Free(ledge.Room, c.Item1, c.Item2)) && AirAbove(ledge.Room, ledge.Y, x0, x0 + ledge.Width - 1))
                    {
                        done = Try(ledge, newCells, oldCells, $"{ledge} moves {gap} to the {(side < 0 ? "west" : "east")} wall (a balcony)");
                    }
                }

                foreach (var (gap, side) in new[] { (left, -1), (right, 1) }.Where(o => o.Item1 > 0 && o.Item1 <= 6).OrderBy(o => o.Item1))
                {
                    if (done) break;
                    var fill = side < 0 ? Span(ledge.Y, ledge.X0 - gap, ledge.X0 - 1) : Span(ledge.Y, ledge.X1 + 1, ledge.X1 + gap);
                    done = Try(ledge, fill, new List<(int, int)>(), $"{ledge} reaches the {(side < 0 ? "west" : "east")} wall (a bridge)");
                }

                if (!done)
                {
                    var mid = (ledge.X0 + ledge.X1) / 2;
                    var floor = FloorUnder(ledge.Room, ledge.Y, mid);
                    if (floor >= 0 && ledge.Y - floor >= 2 && ledge.Y - floor <= 12
                        && ledge.Room.Cells[floor, mid] == LevelCell.Solid && ledge.Room.Cells[floor, mid + 1] == LevelCell.Solid)
                    {
                        var column = new List<(int, int)>();
                        for (var y = floor + 1; y < ledge.Y; y++) { column.Add((mid, y)); column.Add((mid + 1, y)); }
                        if (column.All(c => Free(ledge.Room, c.Item1, c.Item2)))
                        {
                            foreach (var c in column) ledge.Room.Cells[c.Item2, c.Item1] = LevelCell.Solid;
                            var gone = new HashSet<long>();
                            if (Keep($"{ledge} stands on a column from the floor", gone))
                            {
                                done = true;
                            }
                            else
                            {
                                foreach (var c in column) ledge.Room.Cells[c.Item2, c.Item1] = LevelCell.Empty;
                            }
                        }
                    }
                }

                if (!done)
                {
                    log($"  {ledge}: still hangs ({reject})");
                }
            }

            slack = 0;

            // 5. Dead ends go: a ledge whose loss makes no way between two floors a move longer and
            // takes no lettered thing its footing. Twice, so a stair to nowhere goes from its top step
            // down; a climb that shortens any way stays.
            log("prune:");
            for (var pass = 0; pass < 2; pass++)
            {
                var order = Ledges(zone)
                    .OrderByDescending(l => l.Room.Origin.y + l.Y)
                    .ThenBy(l => l.Width).ToList();
                foreach (var ledge in order)
                {
                    Try(ledge, new List<(int, int)>(), Span(ledge.Y, ledge.X0, ledge.X1), $"{ledge} goes (a dead end)");
                }
            }

            var after = Measure(zone, abilities, comfort);
            log($"after: {after.Strict.Count} stone cells reached, {after.Comfort.Count} comfortably; {changed} changes");
            var tight = new List<string>();
            foreach (var room in zone.Rooms)
            {
                var strictCells = after.Strict.Count(c => room.Contains(Unpack(c)));
                var comfortCells = after.Comfort.Count(c => room.Contains(Unpack(c)));
                if (comfortCells < strictCells)
                {
                    tight.Add($"{room.Id}: {strictCells - comfortCells} of {strictCells} floor cells need a sharp jump");
                }
            }

            log("tight spots:\n  " + (tight.Count == 0 ? "none" : string.Join("\n  ", tight)));
            return Write(text, zone);
        }

        /// <summary>The local row of the first stone or ledge under a cell of the room; -1 if there is none in it.</summary>
        private static int FloorUnder(LevelRoom room, int y, int x)
        {
            for (var row = y - 1; row >= 0; row--)
            {
                var cell = room.Cells[row, x];
                if (cell == LevelCell.Solid || cell == LevelCell.Smooth || cell == LevelCell.Ledge)
                {
                    return row;
                }

                if (cell == LevelCell.Spikes)
                {
                    return -1;
                }
            }

            return -1;
        }

        private static int SegmentOn(LevelReach reach, LevelRoom room, int y, int x) =>
            reach.SegmentUnder(room.Origin + new Vector2Int(x, y + 1));

        /// <summary>
        /// Whether a comfortable jump lands the hero on the ledge from somewhere lower — a floor, another
        /// ledge, a wall's other side. A ledge only fallen onto from above is a first step nobody can take.
        /// </summary>
        private static bool ReachedFromBelow(LevelZone zone, HashSet<string> abilities, MovementSettings comfort, Ledge ledge)
        {
            var reach = new LevelReach(zone, abilities, comfort);
            var to = SegmentOn(reach, ledge.Room, ledge.Y, (ledge.X0 + ledge.X1) / 2);
            if (to < 0)
            {
                return false;
            }

            var row = reach.Segments[to].Row;
            for (var from = 0; from < reach.Segments.Count; from++)
            {
                if (from != to && !reach.Segments[from].IsRing && reach.Segments[from].Row < row && reach.HasMove(from, to))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ReachedFromLedge(LevelZone zone, HashSet<string> abilities, MovementSettings comfort, Ledge from, Ledge to)
        {
            var reach = new LevelReach(zone, abilities, comfort);
            var a = SegmentOn(reach, from.Room, from.Y, (from.X0 + from.X1) / 2);
            var b = SegmentOn(reach, to.Room, to.Y, (to.X0 + to.X1) / 2);
            return a >= 0 && b >= 0 && reach.HasMove(a, b);
        }

        private static long Pack(int x, int y) => ((long)x << 32) ^ (uint)y;
        private static Vector2Int Unpack(long c) => new Vector2Int((int)(c >> 32), (int)(uint)c);

        private static Baseline Measure(LevelZone zone, HashSet<string> abilities, MovementSettings comfort)
        {
            var start = LevelCheck.Start(zone).Cell;
            var strict = new LevelReach(zone, abilities);
            var loose = new LevelReach(zone, abilities, comfort);
            var strictForward = strict.Forward(strict.SegmentUnder(start));
            var markers = new Dictionary<LevelMarker, int>();
            foreach (var marker in zone.Markers())
            {
                var under = strict.SegmentUnder(marker.Cell);
                if (under >= 0 && strictForward.Contains(under))
                {
                    markers[marker] = strict.Segments[under].Row;
                }
            }

            var comfortStart = loose.SegmentUnder(start);
            var all = new HashSet<long>();
            foreach (var index in strictForward)
            {
                var segment = strict.Segments[index];
                if (!segment.IsRing)
                {
                    for (var x = segment.XMin; x <= segment.XMax; x++) all.Add(Pack(x, segment.Row));
                }
            }

            // Each stone floor goes by its first stone cell: ledges change, stone does not.
            var floors = new Dictionary<int, long>();
            foreach (var index in strictForward)
            {
                var segment = strict.Segments[index];
                if (segment.IsRing)
                {
                    continue;
                }

                for (var x = segment.XMin; x <= segment.XMax; x++)
                {
                    var below = zone.At(new Vector2Int(x, segment.Row - 1));
                    if (below == LevelCell.Solid || below == LevelCell.Smooth)
                    {
                        floors[index] = Pack(x, segment.Row);
                        break;
                    }
                }
            }

            // How many moves from each stone floor to each other: the whole zone is one loop (no
            // traps), so everything is reached from everywhere the long way round — what a climb
            // gives is a short way, and a climb that goes would make some way longer.
            var next = new Dictionary<int, List<int>>();
            foreach (var a in strictForward)
            {
                var list = new List<int>();
                foreach (var b in strictForward)
                {
                    if (a != b && strict.HasMove(a, b)) list.Add(b);
                }

                next[a] = list;
            }

            var ways = new Dictionary<long, Dictionary<long, int>>();
            foreach (var floor in floors)
            {
                var dist = new Dictionary<int, int> { [floor.Key] = 0 };
                var queue = new Queue<int>();
                queue.Enqueue(floor.Key);
                while (queue.Count > 0)
                {
                    var at = queue.Dequeue();
                    foreach (var to in next[at])
                    {
                        if (!dist.ContainsKey(to))
                        {
                            dist[to] = dist[at] + 1;
                            queue.Enqueue(to);
                        }
                    }
                }

                var reached = new Dictionary<long, int>();
                foreach (var pair in dist)
                {
                    if (floors.TryGetValue(pair.Key, out var other))
                    {
                        reached[other] = pair.Value;
                    }
                }

                ways[floor.Value] = reached;
            }

            var comfortAll = new HashSet<long>();
            if (comfortStart >= 0)
            {
                foreach (var index in loose.Forward(comfortStart))
                {
                    var segment = loose.Segments[index];
                    if (!segment.IsRing)
                    {
                        for (var x = segment.XMin; x <= segment.XMax; x++) comfortAll.Add(Pack(x, segment.Row));
                    }
                }
            }

            return new Baseline
            {
                All = all,
                ComfortAll = comfortAll,
                Ways = ways,
                Strict = StoneCells(zone, strict, strictForward),
                Comfort = comfortStart < 0 ? new HashSet<long>() : StoneCells(zone, loose, loose.Forward(comfortStart)),
                MarkerRows = markers
            };
        }

        private static HashSet<long> StoneCells(LevelZone zone, LevelReach reach, HashSet<int> segments)
        {
            var cells = new HashSet<long>();
            foreach (var index in segments)
            {
                var segment = reach.Segments[index];
                if (segment.IsRing)
                {
                    continue;
                }

                for (var x = segment.XMin; x <= segment.XMax; x++)
                {
                    var below = zone.At(new Vector2Int(x, segment.Row - 1));
                    if (below == LevelCell.Solid || below == LevelCell.Smooth)
                    {
                        cells.Add(Pack(x, segment.Row));
                    }
                }
            }

            return cells;
        }

        private static List<Ledge> Ledges(LevelZone zone)
        {
            var list = new List<Ledge>();
            foreach (var room in zone.Rooms)
            {
                for (var y = 0; y < room.Height; y++)
                {
                    for (var x = 0; x < room.Width; x++)
                    {
                        if (room.Cells[y, x] != LevelCell.Ledge)
                        {
                            continue;
                        }

                        var end = x;
                        while (end + 1 < room.Width && room.Cells[y, end + 1] == LevelCell.Ledge) end++;
                        list.Add(new Ledge { Room = room, Y = y, X0 = x, X1 = end });
                        x = end;
                    }
                }
            }

            return list;
        }

        /// <summary>Air cells between the ledge's end and a wall of its own room on its row; 0 if it touches one, int.MaxValue if none in reach.</summary>
        /// <summary>Empty cells between the ledge's end and stone on its row, up to <paramref name="max"/>; -1 if no stone that near.</summary>
        private static int WallDistance(LevelRoom room, int y, int from, int step, int max)
        {
            for (var gap = 0; gap <= max; gap++)
            {
                var x = from + step * (gap + 1);
                if (x < 0 || x >= room.Width) return -1;
                if (Solid(room.Cells[y, x])) return gap;
                if (room.Cells[y, x] != LevelCell.Empty) return -1;
            }

            return -1;
        }

        private static int GapToWall(LevelRoom room, int y, int from, int step)
        {
            for (var gap = 0; gap <= AttachGap; gap++)
            {
                var x = from + step * (gap + 1);
                if (x <= 0 || x >= room.Width - 1)
                {
                    // the room's edge column: its stone counts as a wall, an opening does not
                    return x >= 0 && x < room.Width && Solid(room.Cells[y, x]) ? gap : int.MaxValue;
                }

                if (Solid(room.Cells[y, x]))
                {
                    return gap;
                }

                if (room.Cells[y, x] != LevelCell.Empty)
                {
                    return int.MaxValue;
                }
            }

            return int.MaxValue;
        }

        private static bool Solid(LevelCell cell) => cell == LevelCell.Solid || cell == LevelCell.Smooth;

        /// <summary>An empty inner cell with no lettered thing in it: the only kind a ledge may take.</summary>
        private static bool Free(LevelRoom room, int x, int y) =>
            x > 0 && x < room.Width - 1 && y > 0 && y < room.Height - 1 && room.Cells[y, x] == LevelCell.Empty
            && !room.Markers.Any(m => m.Cell == room.Origin + new Vector2Int(x, y));

        private static bool AirAbove(LevelRoom room, int y, int x0, int x1)
        {
            for (var x = x0; x <= x1; x++)
            {
                for (var dy = 1; dy <= 4 && y + dy < room.Height; dy++)
                {
                    if (room.Cells[y + dy, x] != LevelCell.Empty && !(dy == 1 && room.Cells[y + dy, x] == LevelCell.Ledge))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>The zone's text with each room's grid rewritten from its cells; everything else as it was.</summary>
        private static string Write(string text, LevelZone zone)
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
                        var c = row[x];
                        if (c == '=' && room.Cells[y, x] == LevelCell.Empty)
                        {
                            row[x] = '.';
                        }
                        else if (c == '.' && room.Cells[y, x] == LevelCell.Ledge)
                        {
                            row[x] = '=';
                        }
                    }

                    lines[grid + r] = new string(row);
                }
            }

            return string.Join(newline, lines);
        }
    }
}
