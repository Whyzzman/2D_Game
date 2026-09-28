using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The world in several scenes (item 3, 2026-09-28), as data: each built zone names a scene
    /// that exists and is in the build, and which zones stay loaded follows from the graph alone.
    /// </summary>
    public sealed class WorldScenesDataTests
    {
        private const string GraphPath = "Assets/Game/Data/WorldGraph.asset";
        private const string Tomb = "zone_forgotten_tomb";
        private const string Forest = "zone_withered_forest";
        private const string East = "zone_unknown_east";
        private WorldGraph _graph;

        [SetUp]
        public void SetUp()
        {
            _graph = AssetDatabase.LoadAssetAtPath<WorldGraph>(GraphPath);
            Assert.That(_graph, Is.Not.Null);
        }

        [Test]
        public void EveryBuiltZoneHasItsOwnSceneInTheBuildAfterTheWorldScene()
        {
            var build = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToList();
            var world = build.IndexOf("Assets/Game/Scenes/TombTrial.unity");
            Assert.That(world, Is.GreaterThanOrEqualTo(0));
            foreach (var zone in _graph.Zones.Where(z => z.IsImplemented))
            {
                Assert.That(zone.SceneName, Is.EqualTo("TombTrial"), $"{zone.ZoneId} lives in the world scene's space.");
                Assert.That(zone.ContentScene, Is.Not.Empty, zone.ZoneId);
                var path = $"Assets/Game/Scenes/Zones/{zone.ContentScene}.unity";
                Assert.That(File.Exists(path), Is.True, path);
                Assert.That(build.IndexOf(path), Is.GreaterThan(world), $"{path} is loaded by name, so it is in the build.");
            }

            Assert.That(ZoneStreamingPlan.IsStreamed(_graph, East), Is.False, "The unbuilt east has nothing to load.");
        }

        [Test]
        public void TheZoneAndItsNeighboursStayLoadedAndNothingUnbuiltIsAskedFor()
        {
            Assert.That(ZoneStreamingPlan.Needed(_graph, Tomb, 0), Is.EquivalentTo(new[] { Tomb }));
            Assert.That(ZoneStreamingPlan.Needed(_graph, Tomb, 1), Is.EquivalentTo(new[] { Tomb, Forest }),
                "The forest is loaded while the hero is still in the Tomb, so the tunnel never waits.");
            Assert.That(ZoneStreamingPlan.Needed(_graph, Forest, 1), Is.EquivalentTo(new[] { Forest, Tomb }),
                "The road to the east is a future connection with no scene; only the Tomb is next door.");
            Assert.That(ZoneStreamingPlan.Needed(_graph, Forest, 5), Has.No.Member(East));
            Assert.That(ZoneStreamingPlan.Needed(_graph, East, 1), Is.Empty);
            Assert.That(ZoneStreamingPlan.Needed(_graph, "nowhere", 1), Is.Empty);
        }
    }
}
