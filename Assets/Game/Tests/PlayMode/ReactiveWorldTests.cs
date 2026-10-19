using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The world answering the hero (GAME_FEEL_IDEAS items 13-14): a prop swings from a gust and comes
    /// back to rest by itself; an urn bursts, pays its coins once, and stands again after a rest.
    /// </summary>
    public sealed class ReactiveWorldTests
    {
        private PlayerCharacter _player;
        private PlayerRespawner _respawner;
        private LootService _loot;
        private GameProgress _progress;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            yield return TestWorld.Load();
            _player = Object.FindAnyObjectByType<PlayerCharacter>();
            _respawner = Object.FindAnyObjectByType<PlayerRespawner>();
            _loot = Object.FindAnyObjectByType<LootService>();
            _progress = Object.FindAnyObjectByType<GameProgress>();
        }

        [TearDown]
        public void TearDown()
        {
            GamePause.Clear();
            SaveSystem.Delete();
            GameTimeScale.Reset();
        }

        [UnityTest]
        public IEnumerator AGustSwingsAPropThatComesBackToRestOnItsOwn()
        {
            var host = new GameObject("env_fg_tomb_chains_test");
            host.transform.position = _player.transform.position + new Vector3(30f, 0f, 0f);
            host.AddComponent<SpriteRenderer>();
            var prop = host.AddComponent<ReactiveProp>();
            var rest = host.transform.position;
            yield return null;

            ReactiveProp.Gust(host.transform.position, 1f, 90f, 1f);
            var started = Time.realtimeSinceStartup;
            var widest = 0f;
            while (Time.realtimeSinceStartup - started < 0.4f)
            {
                widest = Mathf.Max(widest, Mathf.Abs(prop.Angle));
                yield return null;
            }

            Assert.That(widest, Is.GreaterThan(1f), "The gust swings it.");
            started = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - started < 5f && Mathf.Abs(prop.Angle) > 0f) { yield return null; }
            Assert.That(prop.Angle, Is.Zero, "It settles by itself.");
            Assert.That(Vector3.Distance(host.transform.position, rest), Is.LessThan(0.001f),
                "And stands exactly where it was put.");
            Object.Destroy(host);
        }

        [UnityTest]
        public IEnumerator AnUrnPaysItsCoinsOnceAndStandsAgainAfterARest()
        {
            var urn = Object.FindObjectsByType<BreakableProp>(FindObjectsSortMode.None).FirstOrDefault();
            Assume.That(urn, Is.Not.Null, "The loaded zones carry urns from the level text.");
            Assume.That(_loot, Is.Not.Null);

            var before = _loot.TransientPickupCount;
            urn.Break(Vector2.right);
            Assert.That(urn.IsBroken, Is.True);
            Assert.That(_loot.TransientPickupCount, Is.GreaterThan(before), "The first break spills coins.");
            Assert.That(_progress.IsPickupCollected(urn.PropId), Is.True, "And remembers that it did.");

            var checkpoint = Object.FindAnyObjectByType<Checkpoint>();
            Assume.That(checkpoint, Is.Not.Null);
            _respawner.RestAt(checkpoint);
            var started = Time.realtimeSinceStartup;
            while (_respawner.IsSequenceRunning && Time.realtimeSinceStartup - started < 6f) { yield return null; }
            Assert.That(urn.IsBroken, Is.False, "A rest stands it up again.");

            before = _loot.TransientPickupCount;
            urn.Break(Vector2.left);
            Assert.That(urn.IsBroken, Is.True, "It breaks again…");
            Assert.That(_loot.TransientPickupCount, Is.EqualTo(before), "…but it is empty now: no farming.");
        }
    }
}
