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
    /// The Fallen Village in play (item 5 of the 15-zone list): its own scene, loaded as the
    /// forest's and the capital's neighbour; the hatch under the bell shut behind the hero when the
    /// Bellringer wakes, his death handing over the grapple; Hnat at the well once the Warden falls.
    /// </summary>
    public sealed class VillageTests
    {
        private const string Bellringer = "bellringer";
        private const string Village = "zone_fallen_village";
        private PlayerCharacter _player;
        private PlayerMotor2D _motor;
        private GameProgress _progress;
        private ZoneStreamer _streamer;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            yield return TestWorld.Load();
            _progress = Object.FindAnyObjectByType<GameProgress>();
            _streamer = Object.FindAnyObjectByType<ZoneStreamer>();
            _progress.MarkBossDefeated(TombScene.GuardianId);
            _progress.MarkBossDefeated(TombScene.WardenId);
            _player = Object.FindAnyObjectByType<PlayerCharacter>();
            _motor = _player.GetComponent<PlayerMotor2D>();
            _player.Actor.SetInvulnerable(ImmunitySource.Script, 120f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            GameTimeScale.SetPaused(false);
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

        private static IEnumerator Realtime(float seconds)
        {
            var until = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < until)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator TheVillageIsItsOwnSceneLoadedOverTheForestsEdge()
        {
            Assert.That(_streamer.IsLoaded(Village), Is.False, "Two crossings from the Tomb: not loaded.");
            _motor.ResetTo(ForestScene.Ground("capital-seal", -2f));
            yield return Until(() => _streamer.IsLoaded(Village) && !_streamer.IsBusy, 15f,
                "At the forest's edge, the village up the hill comes in.");
            var altars = Object.FindObjectsByType<Checkpoint>(FindObjectsInactive.Include)
                .Where(c => c.CheckpointId.StartsWith("village-")).Select(c => c.CheckpointId).ToList();
            Assert.That(altars, Is.EquivalentTo(new[] { "village-well", "village-belfry", "village-church" }));
            var bellringer = Object.FindObjectsByType<BossController>(FindObjectsInactive.Include)
                .Single(b => b.BossId == Bellringer);
            Assert.That(bellringer.gameObject.scene.name, Is.EqualTo("Zone_Village"));
        }

        [UnityTest]
        public IEnumerator TheHatchSealsAndTheBellringersDeathOpensItAndGivesTheGrapple()
        {
            yield return TestWorld.LoadEveryZone();
            var encounter = Object.FindObjectsByType<BossEncounter>().Single(e => e.BossId == Bellringer);
            var boss = Object.FindObjectsByType<BossController>().Single(b => b.BossId == Bellringer);
            Assert.That(encounter.IsArenaSealed, Is.False);
            Assert.That(_progress.HasAbility(AbilityIds.Grapple), Is.False);

            _motor.ResetTo((Vector2)encounter.transform.position + new Vector2(0f, -1f));
            yield return Until(() => encounter.IsFightActive, 4f, "Under the bell, the fight begins.");
            Assert.That(encounter.IsArenaSealed, Is.True, "The hatch is shut behind the hero.");

            yield return Realtime(2.5f);
            boss.Actor.ClearInvulnerability();
            boss.Actor.ApplyDamage(new DamageInfo(float.MaxValue, Vector2.left, isUnblockable: true,
                attacker: _player.gameObject));
            yield return Until(() => _progress.IsBossDefeated(Bellringer), 6f, "He dies.");
            Assert.That(_progress.HasAbility(AbilityIds.Grapple), Is.True, "The Bellringer's gift is the grapple.");
            Assert.That(_progress.AcquiredRewards, Does.Contain("bell_clapper"));

            // The card that teaches the grapple holds the game until it is read, as a player would.
            var card = Object.FindAnyObjectByType<HudController>().AbilityCard;
            yield return Until(() => card.IsOpen, 4f, "The grapple is announced on its card.");
            Assert.That(card.Showing, Is.EqualTo(AbilityIds.Grapple));
            card.Continue();
            yield return Until(() => !encounter.IsArenaSealed, 6f, "His death opens the hatch.");
        }

        [UnityTest]
        public IEnumerator TheOldRoadIntoTheCapitalIsBarredAndOpensFromTheCapitalsSide()
        {
            yield return TestWorld.LoadEveryZone();
            var bars = GameObject.Find("CapitalRoadGate");
            Assert.That(bars, Is.Not.Null);
            Assert.That(bars.activeSelf, Is.True, "Barred: the way in is through the Village.");
            var winch = Object.FindObjectsByType<Shortcut>().Single(s => s.ShortcutId == "shortcut_capital_forest_road");
            Assert.That(winch.transform.position.x, Is.GreaterThan(bars.transform.position.x),
                "The winch is on the capital's side.");
            winch.Interact(_player);
            Assert.That(bars.activeSelf, Is.False, "One pull and the way back to the forest is open.");
            Assert.That(_progress.IsShortcutUnlocked("shortcut_capital_forest_road"), Is.True);
        }
    }
}
