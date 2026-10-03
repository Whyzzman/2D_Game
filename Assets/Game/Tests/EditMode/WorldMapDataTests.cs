using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The world map of fifteen regions (item 2 of the 15-zone list, design/ui/world-map): every
    /// region an island with its picture, card and place on the mockup's canvas; the roads of
    /// WORLD_DESIGN.md; and corridors that run from wall to facing wall, never across a room.
    /// </summary>
    public sealed class WorldMapDataTests
    {
        private const string GraphPath = "Assets/Game/Data/WorldGraph.asset";
        private WorldGraph _graph;

        [SetUp]
        public void SetUp()
        {
            _graph = UnityEditor.AssetDatabase.LoadAssetAtPath<WorldGraph>(GraphPath);
            Assert.That(_graph, Is.Not.Null);
        }

        [Test]
        public void FifteenRegionsStandOnTheWorldMapWithTheirPictures()
        {
            var regions = _graph.Zones.Where(z => z.IsOnWorldMap).ToArray();
            Assert.That(regions.Length, Is.EqualTo(15));
            Assert.That(regions.Select(z => z.Art).Distinct().Count(), Is.EqualTo(15), "Each island is its own.");
            Assert.That(regions.Where(z => z.IsImplemented).Select(z => z.ZoneId), Is.EquivalentTo(new[]
                { "zone_forgotten_tomb", "zone_withered_forest", "zone_ashen_capital" }), "Three are built so far.");
            foreach (var zone in regions)
            {
                Assert.That(new Rect(0f, 0f, 1470f, 900f).Contains(zone.WorldPosition), Is.True, zone.ZoneId);
                Assert.That(zone.DisplayName, Is.Not.Empty);
                Assert.That(zone.Description, Is.Not.Empty, zone.ZoneId);
                Assert.That(_graph.MapArt, Is.Not.Null, "The generator points the graph at the map's pictures.");
                Assert.That(_graph.MapArt.Island(zone.Art), Is.Not.Null, $"island of {zone.ZoneId}");
                Assert.That(_graph.MapArt.Card(zone.Art), Is.Not.Null, $"card of {zone.ZoneId}");
            }

            Assert.That(_graph.MapArt.Fog, Is.Not.Null);
            Assert.That(_graph.MapArt.Glow, Is.Not.Null);
        }

        [Test]
        public void EveryRoomOfABuiltRegionHasItsOwnPicture()
        {
            foreach (var area in _graph.Areas)
            {
                var zone = _graph.Zone(area.ZoneId);
                if (area.IsSecret || zone == null || !zone.IsImplemented)
                {
                    continue;
                }

                Assert.That(_graph.MapArt.Room(area.AreaId), Is.Not.Null,
                    $"{area.AreaId}: run ContentBuilder.ExportMapRooms and tools/map-art/render.sh.");
            }
        }

        [Test]
        public void TheRoadsJoinTheRegionsAsThePlanSays()
        {
            Assert.That(_graph.Roads.Count, Is.EqualTo(19));
            foreach (var road in _graph.Roads)
            {
                Assert.That(_graph.Zone(road.FromZoneId)?.IsOnWorldMap, Is.True, road.FromZoneId);
                Assert.That(_graph.Zone(road.ToZoneId)?.IsOnWorldMap, Is.True, road.ToZoneId);
            }

            // Every region can be reached by road from the Tomb.
            var reached = new System.Collections.Generic.HashSet<string> { "zone_forgotten_tomb" };
            for (var changed = true; changed;)
            {
                changed = false;
                foreach (var road in _graph.Roads)
                {
                    if (reached.Contains(road.FromZoneId) && reached.Add(road.ToZoneId)) { changed = true; }
                    if (reached.Contains(road.ToZoneId) && reached.Add(road.FromZoneId)) { changed = true; }
                }
            }

            Assert.That(reached.Count, Is.EqualTo(15));
            Assert.That(_graph.Roads.Single(r => r.Touches("zone_royal_palace") && r.Touches("zone_ashen_capital")).IsGated,
                Is.True, "The palace waits for the three seals.");
        }

        [Test]
        public void ACorridorRunsFromWallToFacingWall()
        {
            var left = new Rect(0f, 0f, 2f, 2f);
            var right = new Rect(2.3f, 0.5f, 2f, 3f);
            var level = MapCorridors.Between(left, right, 0.13f);
            Assert.That(level.IsHorizontal, Is.True);
            Assert.That(level.From.x, Is.EqualTo(2f).Within(1e-4f), "It leaves the left room's east wall…");
            Assert.That(level.To.x, Is.EqualTo(2.3f).Within(1e-4f), "…and enters the right one's west wall.");
            Assert.That(level.From.y, Is.InRange(0.5f, 2f), "Through the height both share.");

            var below = new Rect(0f, -3f, 3f, 2f);
            var upright = MapCorridors.Between(left, below, 0.13f);
            Assert.That(upright.IsHorizontal, Is.False);
            Assert.That(upright.From.y, Is.EqualTo(0f).Within(1e-4f), "From the floor of the upper room…");
            Assert.That(upright.To.y, Is.EqualTo(-1f).Within(1e-4f), "…to the ceiling of the lower one.");
            Assert.That(upright.From.x, Is.InRange(0f, 2f));

            var swapped = MapCorridors.Between(right, left, 0.13f);
            Assert.That(swapped.From, Is.EqualTo(level.To), "Asked the other way round, it runs the other way.");
        }
    }
}
