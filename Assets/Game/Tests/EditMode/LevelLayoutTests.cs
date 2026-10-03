using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The level tool (item 1 of the 15-zone list): the text format, the merging of cells into
    /// blocks, the openings that become connections, and the reach check — which must agree with the
    /// jump the motor really makes (1.97 up, ≈3.43 with the second jump, a little under 5 across).
    /// </summary>
    public sealed class LevelLayoutTests
    {
        /// <summary>A room drawn row by row, top first, with a solid frame and a floor two cells thick.</summary>
        internal static string[] Box(int width, int height)
        {
            var rows = new string[height];
            for (var r = 0; r < height; r++)
            {
                var y = height - 1 - r;
                rows[r] = y < 2 || y == height - 1 ? new string('#', width) : "#" + new string('.', width - 2) + "#";
            }

            return rows;
        }

        /// <summary>Writes one character at (x, y), y counted from the bottom.</summary>
        internal static void Put(string[] rows, int x, int y, char c)
        {
            var r = rows.Length - 1 - y;
            var chars = rows[r].ToCharArray();
            chars[x] = c;
            rows[r] = new string(chars);
        }

        internal static string Zone(string header, params (string room, string flags, int x, int y, string[] grid, string keys)[] rooms)
        {
            var text = new StringBuilder(header).Append('\n');
            foreach (var room in rooms)
            {
                text.Append($"room {room.room} «{room.room}» at {room.x} {room.y}\n");
                if (!string.IsNullOrEmpty(room.flags)) { text.Append("flags ").Append(room.flags).Append('\n'); }
                if (!string.IsNullOrEmpty(room.keys)) { text.Append(room.keys).Append('\n'); }
                text.Append("grid\n");
                foreach (var row in room.grid) { text.Append(row).Append('\n'); }
                text.Append("end\n");
            }

            return text.ToString();
        }

        /// <summary>A shelf <paramref name="height"/> cells over the floor, east of the start.</summary>
        internal static LevelZone ShelfZone(int height, string abilities = "")
        {
            var grid = Box(40, 22);
            for (var x = 14; x < 28; x++) { Put(grid, x, 2 + height - 1, '='); }
            Put(grid, 4, 2, 's');
            return LevelZone.Parse(Zone($"zone zone_test\nabilities {abilities}",
                ("room_test", string.Empty, 0, 0, grid, "key s start")));
        }

        /// <summary>A pit <paramref name="width"/> cells wide in a floor six cells thick.</summary>
        internal static LevelZone GapZone(int width)
        {
            var grid = Box(64, 22);
            for (var y = 2; y < 6; y++) { for (var x = 1; x < 63; x++) { Put(grid, x, y, '#'); } }
            for (var y = 1; y < 6; y++) { for (var x = 20; x < 20 + width; x++) { Put(grid, x, y, '.'); } }
            Put(grid, 4, 6, 's');
            return LevelZone.Parse(Zone("zone zone_test", ("room_test", string.Empty, 0, 0, grid, "key s start")));
        }

        internal static bool Reaches(LevelZone zone, int row, int fromX, ICollection<string> abilities)
        {
            var reach = new LevelReach(zone, abilities);
            var start = reach.SegmentUnder(LevelCheck.Start(zone).Cell);
            return reach.Forward(start).Any(i => !reach.Segments[i].IsRing && reach.Segments[i].Row == row
                && reach.Segments[i].XMax >= fromX);
        }

        // ---------- the format ----------

        [Test]
        public void TwoRoomsWithAMatchingOpeningAreConnected()
        {
            var west = Box(20, 12);
            var east = Box(20, 12);
            for (var y = 2; y < 7; y++) { Put(west, 19, y, '.'); Put(east, 0, y, '.'); }
            Put(west, 3, 2, 's');
            Put(west, 8, 2, 'a');
            Put(east, 10, 2, 'z');
            var zone = LevelZone.Parse(Zone("zone zone_test\ncell 0.5\norigin 100 4",
                ("room_west", string.Empty, 0, 0, west, "key s start\nkey a altar test-altar «Вівтар»"),
                ("room_east", "secret needs=double-jump", 20, 0, east, "key z enemy ZombieSoldier")));

            Assert.That(zone.Errors, Is.Empty);
            Assert.That(zone.Rooms.Select(r => (r.Width, r.Height)), Is.EqualTo(new[] { (20, 12), (20, 12) }));
            Assert.That(zone.WorldBounds(zone.Room("room_east")), Is.EqualTo(new Rect(110f, 4f, 10f, 6f)));
            Assert.That(zone.Room("room_east").IsSecret, Is.True);
            Assert.That(zone.Room("room_east").NeedsAbility, Is.EqualTo(AbilityIds.DoubleJump));
            var altar = zone.Markers().Single(m => m.Kind == "altar");
            Assert.That(altar.Cell, Is.EqualTo(new Vector2Int(8, 2)));
            Assert.That(altar.Args, Is.EqualTo(new[] { "test-altar", "Вівтар" }));
            var link = zone.Links.Single();
            Assert.That((link.From, link.To, link.Direction), Is.EqualTo(("room_east", "room_west", MapDirection.West)));
        }

        [Test]
        public void MistakesAreNamedNotBuilt()
        {
            var grid = Box(12, 8);
            Put(grid, 11, 3, '.');
            Put(grid, 4, 2, 'q');
            grid[1] = grid[1].Substring(1);
            var zone = LevelZone.Parse(Zone("zone zone_test\nwhat", ("room_a", "shiny", 0, 0, grid, "key b start")));
            var errors = string.Join("\n", zone.Errors);
            StringAssert.Contains("unknown «what»", errors);
            StringAssert.Contains("unknown flag «shiny»", errors);
            StringAssert.Contains("«q» is neither a cell nor a key", errors);
            StringAssert.Contains("key «b» is never placed", errors);
            StringAssert.Contains("row is 11 wide", errors);
            StringAssert.Contains("leads nowhere", errors);
        }

        [Test]
        public void CellsBecomeFewBlocks()
        {
            var grid = Box(30, 12);
            for (var x = 10; x < 16; x++) { Put(grid, x, 6, '='); }
            for (var x = 20; x < 23; x++) { Put(grid, x, 2, '^'); }
            var zone = LevelZone.Parse(Zone("zone zone_test", ("room_a", string.Empty, 0, 0, grid, string.Empty)));
            var blocks = LevelSolids.Blocks(zone);
            // Floor, ceiling, two walls, the ledge, the spikes.
            Assert.That(blocks.Count, Is.EqualTo(6), string.Join(", ", blocks.Select(b => $"{b.Kind} {b.Cells}")));
            var solidCells = 0;
            for (var y = 0; y < 12; y++) { for (var x = 0; x < 30; x++) { if (zone.IsSolid(new Vector2Int(x, y))) { solidCells++; } } }
            Assert.That(blocks.Where(b => b.Kind != LevelCell.Spikes).Sum(b => b.Cells.width * b.Cells.height), Is.EqualTo(solidCells));
        }

        // ---------- the reach ----------

        [Test]
        public void TheReachIsTheRealJump()
        {
            var none = new HashSet<string>();
            var second = new HashSet<string> { AbilityIds.DoubleJump };
            Assert.That(Reaches(ShelfZone(3), 5, 14, none), Is.True, "1.5 is under a jump (1.97).");
            Assert.That(Reaches(ShelfZone(4), 6, 14, none), Is.False, "2.0 is over it.");
            Assert.That(Reaches(ShelfZone(6), 8, 14, second), Is.True, "3.0 is under the second jump (3.43).");
            Assert.That(Reaches(ShelfZone(7), 9, 14, second), Is.False, "3.5 is over it.");
            Assert.That(Reaches(GapZone(8), 6, 30, none), Is.True, "4.0 across is a running jump.");
            Assert.That(Reaches(GapZone(11), 6, 33, none), Is.False, "5.5 across is not.");
        }

        [Test]
        public void TheCheckFindsAStrandingPitAndAWrongGate()
        {
            Assert.That(LevelCheck.Problems(ShelfZone(3)), Is.Empty);
            var pit = LevelCheck.Problems(GapZone(8));
            Assert.That(pit.Any(p => p.StartsWith("stranded")), Is.True, "A pit 2.5 deep has no way out: " + string.Join("; ", pit));

            // A secret room up a 3.0 shelf: says it needs the second jump, and does.
            var west = Box(30, 22);
            var east = Box(12, 22);
            for (var x = 18; x < 29; x++) { Put(west, x, 7, '='); }
            for (var y = 8; y < 13; y++) { Put(west, 29, y, '.'); Put(east, 0, y, '.'); }
            for (var x = 1; x < 11; x++) { for (var y = 2; y < 8; y++) { Put(east, x, y, '#'); } }
            Put(west, 3, 2, 's');
            var text = Zone("zone zone_test",
                ("room_hall", string.Empty, 0, 0, west, "key s start"),
                ("room_shelf", "secret needs=double-jump", 30, 0, east, string.Empty));
            Assert.That(LevelCheck.Problems(LevelZone.Parse(text)), Is.Empty);
            var wrong = text.Replace("needs=double-jump", "needs=air-dash");
            var problems = LevelCheck.Problems(LevelZone.Parse(wrong));
            Assert.That(problems, Has.Some.Contains("room_shelf is not reached even with air-dash"));
        }

        [Test]
        public void ARingLiftsTheHeroWithTheGrappleOnly()
        {
            var grid = Box(40, 30);
            for (var x = 20; x < 30; x++) { Put(grid, x, 15, '='); }
            Put(grid, 31, 17, 'r');
            Put(grid, 4, 2, 's');
            var zone = LevelZone.Parse(Zone("zone zone_test", ("room_a", string.Empty, 0, 0, grid, "key s start\nkey r ring")));
            var withSecond = new HashSet<string> { AbilityIds.DoubleJump };
            Assert.That(Reaches(zone, 16, 20, withSecond), Is.False, "7.0 up is past every jump.");
            withSecond.Add(AbilityIds.Grapple);
            Assert.That(Reaches(zone, 16, 20, withSecond), Is.True, "The ring, the pop and the second jump make it.");
        }
    }
}
