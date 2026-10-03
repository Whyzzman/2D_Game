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
        public void TheCapitalIsABuiltZoneOfEightRoomsFourSecretsAndThreeAltars()
        {
            var zone = _graph.Zone(Capital);
            Assert.That(zone, Is.Not.Null);
            Assert.That(zone.IsImplemented, Is.True);
            Assert.That(zone.DisplayName, Is.EqualTo("Зруйнована Столиця"), "Named as on the user's map (WORLD_DESIGN.md).");
            Assert.That(zone.ContentScene, Is.EqualTo("Zone_Capital"));

            var areas = _graph.AreasIn(Capital);
            Assert.That(areas.Count(a => !a.IsOptional), Is.EqualTo(8));
            Assert.That(areas.Where(a => a.IsSecret).Select(a => a.RequiredAbilityId),
                Is.EquivalentTo(new[] { AbilityIds.AirDash, AbilityIds.Grapple, string.Empty, string.Empty }),
                "Two behind abilities, two behind breakable walls (item 4).");
            Assert.That(_graph.Checkpoints.Count(c => c.ZoneId == Capital), Is.EqualTo(3));
            Assert.That(areas.Single(a => a.IsBossArena).BossId, Is.EqualTo(King));
        }

        [Test]
        public void TheWardensDeathOpensTheRoadAndTheKingGivesTheGrapple()
        {
            var road = _graph.Connection("conn_forest_to_capital");
            Assert.That(road.IsZoneTransition, Is.True);
            Assert.That(road.RequiredBossId, Is.EqualTo(Warden));
            Assert.That(_graph.Area(road.ToAreaId).ZoneId, Is.EqualTo(Capital));

            var grapple = _graph.Ability(AbilityIds.Grapple);
            Assert.That(grapple.Kind, Is.EqualTo(AbilityKind.TraversalProgression));
            Assert.That(grapple.GuaranteedByBossId, Is.EqualTo(King));
            Assert.That(WorldGraphValidator.Validate(_graph), Is.Empty,
                "The main path, the capital's included, needs no optional ability.");
        }
    }
}
