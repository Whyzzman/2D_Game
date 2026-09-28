using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The world in several scenes (item 3, the user's choices of 2026-09-28): the world scene
    /// holds the player and the services, each zone its own level; the neighbours stay loaded so
    /// a border is crossed on foot; a zone that goes and comes back keeps its beaten enemies
    /// beaten; travel and respawn load a far zone under the fade.
    /// </summary>
    public sealed class ZoneStreamingTests
    {
        private const string Tomb = "zone_forgotten_tomb";
        private const string Forest = "zone_withered_forest";
        private ZoneStreamer _streamer;
        private PlayerRespawner _respawner;
        private PlayerCharacter _player;
        private GameProgress _progress;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            yield return TestWorld.Load();
            _streamer = ZoneStreamer.Current;
            _respawner = Object.FindAnyObjectByType<PlayerRespawner>();
            _player = Object.FindAnyObjectByType<PlayerCharacter>();
            _progress = Object.FindAnyObjectByType<GameProgress>();
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

        private IEnumerator Settled() => Until(() => !_streamer.IsBusy, 15f, "The zones settle.");

        private string ZoneOf(Component thing) => _streamer.ZoneAt(thing.transform.position);

        private string SceneOfZone(string zoneId) => _progress.World.Zone(zoneId).ContentScene;

        [UnityTest]
        public IEnumerator TheWorldSceneHoldsThePlayerAndEachZoneItsOwnLevel()
        {
            Assert.That(_streamer, Is.Not.Null);
            Assert.That(_streamer.IsLoaded(Tomb) && _streamer.IsLoaded(Forest), Is.True,
                "Starting in the Tomb, the forest next door is already in.");
            Assert.That(_streamer.CurrentZoneId, Is.EqualTo(Tomb));

            foreach (var core in new Component[] { _player, _respawner, _progress,
                Object.FindAnyObjectByType<HudController>(), Camera.main })
            {
                Assert.That(core.gameObject.scene.name, Is.EqualTo("TombTrial"), core.name);
            }

            var level = Object.FindObjectsByType<Checkpoint>(FindObjectsInactive.Include).Cast<Component>()
                .Concat(Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Include))
                .Concat(Object.FindObjectsByType<BossEncounter>(FindObjectsInactive.Include))
                .Concat(Object.FindObjectsByType<WorldPickup>(FindObjectsInactive.Include))
                .Concat(Object.FindObjectsByType<Npc>(FindObjectsInactive.Include))
                .Concat(Object.FindObjectsByType<LockedChest>(FindObjectsInactive.Include))
                .Where(c => c.gameObject.scene.name != "DontDestroyOnLoad")
                .ToList();
            Assert.That(level.Count, Is.GreaterThan(60));
            foreach (var thing in level)
            {
                var zone = ZoneOf(thing);
                Assert.That(thing.gameObject.scene.name, Is.EqualTo(SceneOfZone(zone)),
                    $"{thing.name} at {thing.transform.position} belongs to {zone}.");
            }

            var world = SceneManager.GetSceneByName("TombTrial");
            var ground = world.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Collider2D>(true))
                .Where(c => c.gameObject.layer == 9)
                .ToList();
            Assert.That(ground, Is.Empty, "No level ground is left in the world scene: "
                + string.Join(", ", ground.Select(c => c.name)));
            yield return null;
        }

        [UnityTest]
        public IEnumerator AZoneThatGoesAndComesBackKeepsItsBeatenEnemiesBeaten()
        {
            var forestEnemies = Object.FindObjectsByType<EnemyController>()
                .Where(e => ZoneOf(e) == Forest).OrderBy(e => e.transform.position.x).ToList();
            Assert.That(forestEnemies.Count, Is.GreaterThan(2));
            var beaten = forestEnemies[0];
            var standing = forestEnemies[1];
            var beatenKey = beaten.SpawnKey;
            var standingKey = standing.SpawnKey;
            beaten.GetComponent<CombatActor>().ApplyDamage(new DamageInfo(float.MaxValue, Vector2.up,
                isUnblockable: true, isParryable: false));
            yield return null;
            Assert.That(beaten.IsAlive, Is.False);

            _streamer.NeighbourDepth = 0;
            yield return Until(() => !_streamer.IsLoaded(Forest), 10f, "The forest is let go while the hero is in the Tomb.");
            yield return Settled();
            Assert.That(Object.FindObjectsByType<EnemyController>().Any(e => e.SpawnKey == beatenKey), Is.False);
            Assert.That(_respawner.Enemies.IsBeaten(beatenKey), Is.True, "Remembered while its zone is gone.");

            _streamer.NeighbourDepth = 1;
            yield return Until(() => _streamer.IsLoaded(Forest), 10f, "The forest comes back as a neighbour.");
            yield return Settled();
            var again = Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Include);
            Assert.That(again.Single(e => e.SpawnKey == beatenKey).IsAlive, Is.False, "Still beaten.");
            Assert.That(again.Single(e => e.SpawnKey == standingKey).IsAlive, Is.True, "Only the beaten one.");

            _respawner.ResetWorld(WorldResetReason.CheckpointRest);
            yield return null;
            Assert.That(again.Single(e => e.SpawnKey == beatenKey).IsAlive, Is.True, "A rest brings it back.");
            Assert.That(_respawner.Enemies.IsBeaten(beatenKey), Is.False);
        }

        [UnityTest]
        public IEnumerator TravelAndRespawnLoadAFarZoneUnderTheFade()
        {
            var tombAltar = _respawner.FindCheckpoint("tomb-altar-01");
            Assert.That(tombAltar, Is.Not.Null);
            var tombSpot = tombAltar.RespawnPosition;
            _streamer.NeighbourDepth = 0;
            yield return Until(() => !_streamer.IsLoaded(Forest), 10f, "Only the Tomb is loaded.");
            yield return Settled();
            Assert.That(_respawner.FindCheckpoint("forest-entrance"), Is.Null);

            var arrived = false;
            Assert.That(_respawner.BeginTravel("forest-entrance", ok => arrived = ok), Is.True);
            yield return Until(() => !_respawner.IsSequenceRunning, 15f, "The journey ends.");
            Assert.That(arrived, Is.True, "The forest was loaded under the fade and its altar found.");
            var forestAltar = _respawner.FindCheckpoint("forest-entrance");
            Assert.That(Vector2.Distance(_player.transform.position, forestAltar.RespawnPosition), Is.LessThan(1f));
            yield return Until(() => !_streamer.IsLoaded(Tomb), 10f, "The Tomb behind is let go.");
            Assert.That(_player.IsAlive, Is.True);

            // Dying in the forest with the Tomb's altar as the last rest brings the Tomb back first.
            _respawner.SetRespawnPoint(tombSpot, "tomb-altar-01");
            _respawner.Kill();
            yield return Until(() => _player.IsAlive && !_respawner.IsSequenceRunning, 20f, "Back on its feet.");
            Assert.That(_streamer.IsLoaded(Tomb), Is.True);
            Assert.That(Vector2.Distance(_player.transform.position, tombSpot), Is.LessThan(1f));
            yield return new WaitForSeconds(0.5f);
            Assert.That(_player.GetComponent<PlayerMotor2D>().IsGrounded, Is.True, "Standing on the Tomb's floor, not falling.");
        }
    }
}
