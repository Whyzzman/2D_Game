using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The Ashen Capital as data (item 2 of the big list, the user's choices of 2026-09-28): eight
    /// rooms, two secrets, three altars, the road from the forest opened by the Warden, and the
    /// grapple — the King's gift — asked for by no room of the main path.
    /// </summary>
    public sealed class CapitalDataTests
    {
        private const string Capital = "zone_ashen_capital";
        private const string Warden = "hollow-warden";
        private const string King = "ashen-king";
        private WorldGraph _graph;

        [SetUp]
        public void SetUp()
        {
            _graph = AssetDatabase.LoadAssetAtPath<WorldGraph>("Assets/Game/Data/WorldGraph.asset");
            Assert.That(_graph, Is.Not.Null);
        }

        [Test]
        public void TheCapitalIsABuiltZoneOfThreeTiersFiveSecretsAndThreeAltars()
        {
            var zone = _graph.Zone(Capital);
            Assert.That(zone, Is.Not.Null);
            Assert.That(zone.IsImplemented, Is.True);
            Assert.That(zone.DisplayName, Is.EqualTo("Зруйнована Столиця"), "Named as on the user's map (WORLD_DESIGN.md).");
            Assert.That(zone.ContentScene, Is.EqualTo("Zone_Capital"));

            var areas = _graph.AreasIn(Capital);
            // Since item 6a it is built from its text by the user's map (Levels/capital.txt): eighteen
            // rooms on three tiers round the bell tower, the main path along the upper one.
            Assert.That(areas.Count, Is.EqualTo(19));
            Assert.That(areas.Count(a => !a.IsOptional), Is.EqualTo(7));
            Assert.That(areas.Where(a => a.IsSecret).Select(a => a.RequiredAbilityId),
                Is.EquivalentTo(new[] { AbilityIds.Grapple, AbilityIds.Grapple, string.Empty, string.Empty, string.Empty }),
                "The royal gallery behind the grapple and the treasury past it; three behind breakable walls alone.");
            Assert.That(_graph.Checkpoints.Count(c => c.ZoneId == Capital), Is.EqualTo(3));
            // The Commander's hall, then the King's throne past it (item 6b).
            Assert.That(areas.Where(a => a.IsBossArena).Select(a => a.BossId), Is.EquivalentTo(new[] { "ash-commander", King }));
        }

        [Test]
        public void TheVillageLeadsIntoTheCapitalAndTheBellringerGivesTheGrapple()
        {
            // Since item 5 of the 15 zones: up the hill to the Village, over its wall by the grapple's
            // ring and down into the capital; the old road from the forest is a shortcut opened from
            // the capital's side.
            var drop = _graph.Connection("conn_village_to_capital");
            Assert.That(drop.IsBidirectional, Is.False);
            Assert.That(_graph.Area(drop.FromAreaId).RequiredAbilityId, Is.EqualTo(AbilityIds.Grapple));
            Assert.That(_graph.Area(drop.ToAreaId).ZoneId, Is.EqualTo(Capital));
            var road = _graph.Connection("conn_forest_to_capital");
            Assert.That(road.IsShortcut, Is.True);
            Assert.That(_graph.Area(road.ToAreaId).ZoneId, Is.EqualTo(Capital));

            var grapple = _graph.Ability(AbilityIds.Grapple);
            Assert.That(grapple.Kind, Is.EqualTo(AbilityKind.TraversalProgression));
            Assert.That(grapple.GuaranteedByBossId, Is.EqualTo("bellringer"));
            Assert.That(WorldGraphValidator.Validate(_graph), Is.Empty,
                "The main path, the capital's included, needs no optional ability.");
        }
    }
}
