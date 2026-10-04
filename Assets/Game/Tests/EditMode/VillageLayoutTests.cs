using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The Fallen Village as its text (item 5 of the 15-zone list, `Levels/village.txt`): walkable
    /// with what the hero is sure to have there (the dash and the Warden's second jump), every
    /// grapple gate shut without the grapple, and placed over the capital where the forest's edge
    /// climbs up to it (`gamedesign/LEVEL_TOOL.md`, `WORLD_DESIGN.md`).
    /// </summary>
    public sealed class VillageLayoutTests
    {
        private static LevelZone Village() =>
            LevelZone.Parse(File.ReadAllText(Path.Combine(Application.dataPath, "Game/Levels/village.txt")));

        [Test]
        public void TheVillagesTextIsWalkableAndEveryGateHolds()
        {
            var village = Village();
            Assert.That(LevelCheck.Problems(village), Is.Empty);
            Assert.That(village.ZoneId, Is.EqualTo("zone_fallen_village"));
            Assert.That(village.Abilities, Is.EquivalentTo(new[] { AbilityIds.Dash, AbilityIds.DoubleJump }),
                "The Warden's second jump is in hand by the time the hill is climbed.");
        }

        [Test]
        public void TheVillageFollowsTheUsersMap()
        {
            var village = Village();
            // reference/maps/03_fallen_village.webp: the square with the well in the middle, the
            // bell tower in the north-east, the church in the south-east, five red rooms, the feather.
            Assert.That(village.Rooms.Count, Is.InRange(18, 30));
            Assert.That(village.Rooms.Count(r => r.IsSecret && r.Id != "room_village_forge_loft"), Is.EqualTo(5), "Five red rooms.");
            Assert.That(village.Markers().Count(m => m.Kind == "breakable"), Is.EqualTo(5));
            Assert.That(village.Markers().Count(m => m.Kind == "altar"), Is.EqualTo(3));
            var all = village.Rooms.Select(r => village.WorldBounds(r)).ToArray();
            var bell = village.WorldBounds(village.Room("room_village_bell"));
            Assert.That(bell.yMax, Is.EqualTo(all.Max(r => r.yMax)).Within(0.01f), "The bell at the top.");
            Assert.That(bell.center.x, Is.GreaterThan(all.Average(r => r.center.x)), "In the east.");
            Assert.That(village.Room("room_village_bell").BossId, Is.EqualTo("bellringer"));
            Assert.That(village.Room("room_village_feather").NeedsAbility, Is.EqualTo(AbilityIds.Grapple),
                "The feather: the grapple's first place, and the way down to the capital.");
        }

        [Test]
        public void ItStandsOverTheCapitalAndMeetsTheForestsEdge()
        {
            var village = Village();
            Assert.That(village.Rooms.All(r => village.WorldBounds(r).yMin >= 18.9f), Is.True,
                "Nothing reaches down into the capital (it ends at 18).");
            var hill = village.WorldBounds(village.Room("room_village_hill"));
            var forest = LevelZone.Parse(File.ReadAllText(Path.Combine(Application.dataPath, "Game/Levels/forest.txt")));
            var edge = forest.WorldBounds(forest.Room("room_forest_far_gate"));
            Assert.That(hill.yMin, Is.EqualTo(edge.yMax).Within(0.001f), "The hill stands on the forest's edge.");
            Assert.That(hill.xMin, Is.LessThan(edge.xMax));
            Assert.That(village.Room("room_village_hill").ZoneExits, Does.Contain("S"));
            Assert.That(forest.Room("room_forest_far_gate").ZoneExits, Does.Contain("N"));
        }
    }
}
