using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The Withered Forest as its text (item 4 of the 15-zone list, `Levels/forest.txt`): the hero's
    /// own movement stepped through every jump proves the main path, that nothing strands him, and
    /// that each gate is shut without its ability and open with it (`gamedesign/LEVEL_TOOL.md`).
    /// </summary>
    public sealed class ForestLayoutTests
    {
        private static LevelZone Forest() =>
            LevelZone.Parse(File.ReadAllText(Path.Combine(Application.dataPath, "Game/Levels/forest.txt")));

        [Test]
        public void TheForestsTextIsWalkableAndEveryGateHolds()
        {
            var forest = Forest();
            Assert.That(LevelCheck.Problems(forest), Is.Empty);
            Assert.That(forest.ZoneId, Is.EqualTo("zone_withered_forest"));
            Assert.That(forest.Abilities, Is.EqualTo(new[] { AbilityIds.Dash }), "Nothing but the dash on the main path.");
        }

        [Test]
        public void TheForestFollowsTheUsersMap()
        {
            var forest = Forest();
            // reference/maps/02_withered_forest.webp: the arch in the west, the great tree in the
            // middle, the tower in the north, water and mushrooms below, the Warden in the east;
            // three skulls (rooms behind walls), three orange gates (second jump), three blue (grapple).
            Assert.That(forest.Rooms.Count, Is.InRange(25, 40));
            Assert.That(forest.Markers().Count(m => m.Kind == "breakable"), Is.EqualTo(3), "Three skulls.");
            Assert.That(forest.Rooms.Count(r => r.NeedsAbility == AbilityIds.Grapple), Is.EqualTo(3), "Three rings.");
            Assert.That(forest.Links.Count(l => l.Ability == AbilityIds.DoubleJump), Is.GreaterThanOrEqualTo(3));
            Assert.That(forest.Markers().Count(m => m.Kind == "altar"), Is.EqualTo(4));
            var all = forest.Rooms.Select(r => forest.WorldBounds(r)).ToArray();
            var arena = forest.WorldBounds(forest.Room("room_forest_warden_arena"));
            Assert.That(arena.center.x, Is.GreaterThan(all.Max(r => r.xMax) - 40f), "The Warden in the east.");
            Assert.That(forest.Room("room_forest_warden_arena").BossId, Is.EqualTo("hollow-warden"));
            var tower = forest.WorldBounds(forest.Room("room_forest_watchtower"));
            Assert.That(tower.yMax, Is.GreaterThan(all.Max(r => r.yMax) - 2f), "The tower in the north.");
            var pool = forest.WorldBounds(forest.Room("room_forest_mushroom_pool"));
            Assert.That(pool.yMin, Is.EqualTo(all.Min(r => r.yMin)).Within(0.01f), "The mushrooms at the bottom.");
        }

        [Test]
        public void ItJoinsTheTombsTunnelAndTheRoadOn()
        {
            var forest = Forest();
            var tunnel = forest.WorldBounds(forest.Room("room_forest_root_tunnel"));
            Assert.That(tunnel.xMin, Is.EqualTo(62.5f).Within(0.001f), "The Tomb's way out is at 62.5.");
            var start = LevelCheck.Start(forest);
            Assert.That(forest.CellCorner(start.Cell).y, Is.EqualTo(0f).Within(0.001f), "Level with the Tomb's floor.");
            var edge = forest.WorldBounds(forest.Room("room_forest_far_gate"));
            Assert.That(edge.xMax, Is.EqualTo(318f).Within(0.001f), "The road on begins at 318.");
            Assert.That(forest.Room("room_forest_far_gate").ZoneExits, Does.Contain("E"));
        }
    }
}
