using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The Ashen Capital in the world (item 2 of the big list, 2026-09-28): the road from the forest
    /// opens only with the Warden's death, the capital is its own scene loaded as the forest's
    /// neighbour, its altars stand on its ground, and the merchant moves there.
    /// </summary>
    public sealed class CapitalTests
    {
        private const string Capital = "zone_ashen_capital";
        private GameProgress _progress;
        private PlayerMotor2D _motor;
        private ZoneStreamer _streamer;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            yield return TestWorld.Load();
            _progress = Object.FindAnyObjectByType<GameProgress>();
            _motor = Object.FindAnyObjectByType<PlayerMotor2D>();
            _streamer = ZoneStreamer.Current;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SaveSystem.Delete();
            yield return null;
        }

        private static IEnumerator Until(Func<bool> condition, float seconds, string what)
        {
            var deadline = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(condition(), Is.True, what);
        }

        [UnityTest]
        public IEnumerator TheCapitalIsItsOwnSceneLoadedAsTheForestsNeighbour()
        {
            Assert.That(_streamer.IsLoaded(Capital), Is.False, "Two crossings from the Tomb: not loaded.");
            _motor.ResetTo(new Vector2(90f, 1f));
            yield return Until(() => _streamer.IsLoaded(Capital) && !_streamer.IsBusy, 15f,
                "In the forest, the capital next door comes in.");

            var altars = Object.FindObjectsByType<Checkpoint>(FindObjectsInactive.Include)
                .Where(c => c.CheckpointId.StartsWith("capital-")).ToList();
            Assert.That(altars.Select(a => a.CheckpointId),
                Is.EquivalentTo(new[] { "capital-gate", "capital-temple", "capital-palace" }));
            foreach (var altar in altars)
            {
                Assert.That(altar.gameObject.scene.name, Is.EqualTo("Zone_Capital"));
                var ground = Physics2D.Raycast((Vector2)altar.transform.position, Vector2.down, 2f, 1 << 9);
                Assert.That((bool)ground, Is.True, $"{altar.CheckpointId} stands on the capital's ground.");
            }
        }

        [UnityTest]
        public IEnumerator TheMerchantMovesToTheCityGateOnceTheBellringerFalls()
        {
            _motor.ResetTo(new Vector2(300f, 1f));
            yield return Until(() => _streamer.IsLoaded(Capital) && !_streamer.IsBusy, 15f, "Capital loaded.");
            _progress.MarkBossDefeated(TombScene.GuardianId);
            _progress.MarkBossDefeated(TombScene.WardenId);
            // The village's well is his stall between the Warden and the Bellringer (item 5 of the 15 zones).
            _progress.MarkBossDefeated("bellringer");
            yield return null;
            var gnat = Object.FindObjectsByType<Npc>(FindObjectsInactive.Include)
                .Where(n => n.Definition != null && n.Definition.NpcId == "gnat" && n.IsPresent)
                .ToList();
            Assert.That(gnat.Count, Is.EqualTo(1));
            Assert.That(gnat[0].transform.position.x, Is.InRange(CapitalScene.Room("room_capital_city_gate").xMin, CapitalScene.Room("room_capital_city_gate").xMax), "By the city gate.");
        }
    }
}
