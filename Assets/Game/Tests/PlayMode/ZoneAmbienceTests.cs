using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The living environment: each zone breathes its own air, and dark shapes stand in front of
    /// the play at the edges of the frame — never in a boss arena, and thinning while the player
    /// is behind them.
    /// </summary>
    public sealed class ZoneAmbienceTests
    {
        private const string Tomb = "zone_forgotten_tomb";
        private const string Forest = "zone_withered_forest";

        private PlayerMotor2D _motor;
        private ZoneAmbience _air;
        private WorldGraph _graph;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            yield return SceneManager.LoadSceneAsync("TombTrial");
            _motor = Object.FindAnyObjectByType<PlayerCharacter>().GetComponent<PlayerMotor2D>();
            _air = Object.FindAnyObjectByType<ZoneAmbience>();
            _graph = Object.FindAnyObjectByType<GameProgress>().World;
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            GamePause.Clear();
            SaveSystem.Delete();
        }

        private static IEnumerator WaitUntil(System.Func<bool> condition, float seconds)
        {
            var deadline = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator EachZoneBreathesItsOwnAirAndTheAirFollowsTheView()
        {
            Assert.That(_air, Is.Not.Null, "TombTrial has no zone air.");
            Assert.That(_air.IsBreathing(Tomb), Is.True, "The Tomb starts without its dust.");
            Assert.That(_air.IsBreathing(Forest), Is.False);
            var dust = _air.GetComponentsInChildren<ParticleSystem>().First(p => p.name == "TombDust");
            yield return WaitUntil(() => dust.particleCount > 5, 1f);
            Assert.That(dust.particleCount, Is.GreaterThan(0), "The air should already be full on load.");

            var entrance = _graph.Areas.First(a => a.AreaId == "room_forest_entrance").WorldBounds;
            _motor.ResetTo(new Vector2(entrance.center.x, entrance.yMin + 3f));
            yield return WaitUntil(() => _air.ActiveZone == Forest, 3f);

            Assert.That(_air.IsBreathing(Forest), Is.True, "The forest has no air of its own.");
            Assert.That(_air.IsBreathing(Tomb), Is.False, "The Tomb's dust followed into the forest.");
            yield return null;
            Assert.That(Mathf.Abs(_air.transform.position.x - Camera.main.transform.position.x),
                Is.LessThan(0.01f), "The air did not ride with the view.");
        }

        [UnityTest]
        public IEnumerator TheForegroundStandsAtTheEdgesOfBothZonesButNeverInAnArena()
        {
            yield return null;
            var shapes = Object.FindObjectsByType<ForegroundSilhouette>();
            Assert.That(shapes.Length, Is.GreaterThan(6), "Too little foreground to give depth.");

            var arenas = _graph.Areas.Where(a => a.IsBossArena).Select(a => a.WorldBounds).ToArray();
            var zones = shapes.Select(s => _graph.AreaAt("TombTrial", s.Origin)?.ZoneId).ToArray();
            Assert.That(zones, Does.Contain(Tomb));
            Assert.That(zones, Does.Contain(Forest));
            foreach (var shape in shapes)
            {
                var renderer = shape.GetComponent<SpriteRenderer>();
                Assert.That(renderer.sortingLayerName, Is.EqualTo("Foreground"));
                foreach (var arena in arenas)
                {
                    Assert.That(arena.Contains(shape.Origin), Is.False,
                        $"{shape.name} stands in a boss arena.");
                }
            }
        }

        [UnityTest]
        public IEnumerator AShapeThinsWhileThePlayerIsBehindIt()
        {
            // A shape standing on the floor, so the player can stand in it.
            var shape = Object.FindObjectsByType<ForegroundSilhouette>()
                .Where(s => s.GetComponent<SpriteRenderer>().sprite.pivot.y < 1f)
                .OrderBy(s => Vector2.Distance(s.transform.position, _motor.transform.position))
                .First();
            yield return null;
            Assert.That(shape.Alpha, Is.EqualTo(1f));

            // Stand the player where the shape is drawn.
            var bounds = shape.GetComponent<SpriteRenderer>().bounds;
            _motor.ResetTo(new Vector2(bounds.center.x, bounds.center.y));
            yield return WaitUntil(() => shape.Alpha <= ForegroundSilhouette.BehindAlpha + 0.01f, 2f);
            Assert.That(shape.Alpha, Is.LessThanOrEqualTo(ForegroundSilhouette.BehindAlpha + 0.01f),
                "The shape stayed solid over the player.");
        }
    }
}
