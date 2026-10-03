using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The Forgotten Tomb as its text (item 3 of the 15-zone list, `Levels/tomb.txt`): the hero's own
    /// movement stepped through every jump proves the main path, that nothing strands him, and that
    /// each gate is shut without its ability and open with it (`gamedesign/LEVEL_TOOL.md`). The walk
    /// test starts at the Tomb's way out; this is what proves the Tomb itself.
    /// </summary>
    public sealed class TombLayoutTests
    {
        private static LevelZone Tomb() =>
            LevelZone.Parse(File.ReadAllText(Path.Combine(Application.dataPath, "Game/Levels/tomb.txt")));

        [Test]
        public void TheTombsTextIsWalkableAndEveryGateHolds()
        {
            var tomb = Tomb();
            Assert.That(LevelCheck.Problems(tomb), Is.Empty);
            Assert.That(tomb.ZoneId, Is.EqualTo("zone_forgotten_tomb"));
            Assert.That(tomb.Abilities, Is.EqualTo(new[] { AbilityIds.Dash }), "Nothing but the dash on the main path.");
        }

        [Test]
        public void TheTombFollowsTheUsersMap()
        {
            var tomb = Tomb();
            // reference/maps/01_forgotten_tomb.webp: about twenty halls, four altars, four secrets
            // (three caches behind walls and the urn behind the arena), the Guardian in the north-east.
            Assert.That(tomb.Rooms.Count, Is.InRange(20, 30));
            Assert.That(tomb.Markers().Count(m => m.Kind == "altar"), Is.EqualTo(4));
            Assert.That(tomb.Rooms.Count(r => r.IsSecret && string.IsNullOrEmpty(r.NeedsAbility)), Is.EqualTo(3),
                "Three caches behind breakable walls.");
            Assert.That(tomb.Markers().Count(m => m.Kind == "breakable"), Is.EqualTo(4),
                "Those three and the walled cell.");
            var arena = tomb.WorldBounds(tomb.Room("room_tomb_guardian_arena"));
            var all = tomb.Rooms.Select(r => tomb.WorldBounds(r)).ToArray();
            Assert.That(arena.yMax, Is.EqualTo(all.Max(r => r.yMax)), "The Guardian's hall is the highest.");
            Assert.That(arena.center.x, Is.GreaterThan(all.Average(r => r.center.x)), "And in the east.");
            Assert.That(tomb.Room("room_tomb_guardian_arena").BossId, Is.EqualTo("guardian-of-the-tomb"));
        }

        [Test]
        public void TheWayOutMeetsTheForestsTunnel()
        {
            var tomb = Tomb();
            var hall = tomb.Room("room_tomb_east_hall");
            Assert.That(hall.ZoneExits, Does.Contain("E"));
            var bounds = tomb.WorldBounds(hall);
            Assert.That(bounds.xMax, Is.EqualTo(62.5f).Within(0.001f), "The tunnel begins at 62.5.");
            var exitFloor = tomb.Markers().Single(m => m.Kind == "spot" && m.Arg(0) == "exit");
            Assert.That(tomb.CellCorner(exitFloor.Cell).y, Is.EqualTo(0f).Within(0.001f), "Level with the tunnel's floor.");
        }
    }
}
