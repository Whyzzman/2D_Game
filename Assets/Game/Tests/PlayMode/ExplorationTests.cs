using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// What exploring pays (item 4 of the big list): vessel shards lie in the secret pockets, on
    /// their shelves; four of health or three of mana widen a bar by 15% and fill it, and the
    /// banner says how far along the vessel is.
    /// </summary>
    public sealed class ExplorationTests
    {
        private PlayerCharacter _player;
        private GameProgress _progress;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            yield return TestWorld.Load();
            yield return TestWorld.LoadEveryZone();
            _progress = Object.FindAnyObjectByType<GameProgress>();
            _player = Object.FindAnyObjectByType<PlayerCharacter>();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SaveSystem.Delete();
            yield return null;
        }

        private static WorldPickup[] Shards(VesselKind kind) =>
            Object.FindObjectsByType<WorldPickup>(FindObjectsInactive.Include)
                .Where(p => VesselShards.TryKind(p.ItemId, out var k) && k == kind && !p.IsCurrency)
                .OrderBy(p => p.transform.position.x)
                .ToArray();

        [UnityTest]
        public IEnumerator ShardsLieOnTheShelvesOfSecretPockets()
        {
            var health = Shards(VesselKind.Health);
            var mana = Shards(VesselKind.Mana);
            Assert.That(health.Length, Is.GreaterThanOrEqualTo(4), "Enough health shards for a vessel.");
            Assert.That(mana.Length, Is.GreaterThanOrEqualTo(3), "Enough mana shards for a vessel.");

            var graph = _progress.World;
            foreach (var shard in health.Concat(mana))
            {
                var at = (Vector2)shard.transform.position;
                Assert.That(shard.IsPersistent, Is.True, $"{shard.name} is remembered once taken.");
                Assert.That(graph.Areas.Any(a => a.IsOptional && a.WorldBounds.Contains(at)), Is.True,
                    $"{shard.name} at {at} is in an optional area, not on the main path.");
                var shelf = Physics2D.RaycastAll(at, Vector2.down, 1.5f)
                    .FirstOrDefault(h => !h.collider.isTrigger && h.collider.GetComponentInParent<PlayerCharacter>() == null);
                Assert.That(shelf.collider, Is.Not.Null, $"{shard.name} at {at} lies on a shelf.");
            }

            var ids = health.Concat(mana).Select(s => s.PersistentId).ToList();
            Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Count), "Every shard has its own id.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator FourHealthShardsWidenAndFillTheBar()
        {
            var health = _player.Actor.Health;
            var before = health.Max;
            var hud = Object.FindAnyObjectByType<HudController>();
            var shards = Shards(VesselKind.Health).Take(4).ToArray();
            health.SetCurrent(before * 0.5f);

            for (var i = 0; i < 3; i++)
            {
                Assert.That(shards[i].TryCollect(_player), Is.True);
                yield return null;
            }

            Assert.That(health.Max, Is.EqualTo(before).Within(0.01f), "Three shards are not a vessel yet.");
            StringAssert.Contains("3/4", hud.ZoneTitleText, "The banner counts the shards.");

            Assert.That(shards[3].TryCollect(_player), Is.True);
            yield return null;
            Assert.That(health.Max, Is.EqualTo(before * 1.15f).Within(0.5f), "The fourth makes a vessel: +15%.");
            Assert.That(health.Current, Is.EqualTo(health.Max).Within(0.01f), "A whole vessel fills the bar.");
            Assert.That(VesselShards.Vessels(_progress, VesselKind.Health), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ThreeManaShardsWidenTheManaBar()
        {
            var mana = _player.GetComponent<Mana>();
            var before = mana.Max;
            foreach (var shard in Shards(VesselKind.Mana).Take(3))
            {
                Assert.That(shard.TryCollect(_player), Is.True);
                yield return null;
            }

            Assert.That(mana.Max, Is.EqualTo(before * 1.15f).Within(0.5f));
            StringAssert.Contains("МАНА", Object.FindAnyObjectByType<HudController>().ZoneTitleText);
        }
    }
}
