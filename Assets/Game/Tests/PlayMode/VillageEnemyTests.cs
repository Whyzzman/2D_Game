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
    /// The village's own enemies at work (item 5b of the 15 zones, 2026-10-19): the watchdog goes
    /// no further than its chain whatever throws it, and the scarecrow lets out a flock that gives
    /// nothing and does not outlive it.
    /// </summary>
    public sealed class VillageEnemyTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            yield return TestWorld.Load();
            yield return TestWorld.LoadEveryZone();
            Object.FindAnyObjectByType<PlayerCharacter>().Actor.SetInvulnerable(ImmunitySource.Script, 120f);
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

        private static void Hit(Component target, float damage) =>
            target.GetComponent<CombatActor>().ApplyDamage(new DamageInfo(damage, Vector2.up,
                isUnblockable: true, isParryable: false));

        [UnityTest]
        public IEnumerator TheWatchdogGoesNoFurtherThanItsChain()
        {
            var leashes = Object.FindObjectsByType<EnemyLeash>(FindObjectsInactive.Exclude);
            Assert.That(leashes, Is.Not.Empty, "The village has its chained dogs.");
            var dog = leashes[0];
            var enemy = dog.GetComponent<EnemyController>();
            enemy.IsHeld = true;
            var motor = dog.GetComponent<EnemyMotor2D>();
            var body = dog.GetComponent<Rigidbody2D>();

            foreach (var side in new[] { 1f, -1f })
            {
                // A leap far harder than its own, straight out along the floor.
                var deadline = Time.realtimeSinceStartup + 1.5f;
                while (Time.realtimeSinceStartup < deadline)
                {
                    motor.SetVelocity(new Vector2(side * 30f, 0f));
                    yield return new WaitForFixedUpdate();
                    Assert.That(Mathf.Abs(body.position.x - dog.Stake.x),
                        Is.LessThanOrEqualTo(dog.Length + 0.05f), "The chain holds.");
                }
            }
        }

        [UnityTest]
        public IEnumerator TheScarecrowsFlockGivesNothingAndGoesWithIt()
        {
            var summoners = Object.FindObjectsByType<EnemySummoner>(FindObjectsInactive.Exclude);
            Assert.That(summoners, Is.Not.Empty, "The village has its scarecrows.");
            var scarecrow = summoners[0];
            var dormancy = scarecrow.GetComponent<EnemyDormancy>();
            Assert.That(dormancy.IsAsleep, Is.True, "On its pole, it is decoration.");

            Hit(scarecrow, 1f);
            yield return Until(() => scarecrow.Flock.Count > 0, 4f, "Struck, it wakes and the ravens burst out.");
            var flock = scarecrow.Flock.ToArray();
            Assert.That(flock.All(r => r.GetComponent<ExperienceReward>() == null
                && r.GetComponent<LootDropper>() == null), Is.True, "The flock is worth nothing.");

            var master = scarecrow.GetComponent<EnemyController>();
            var deadline = Time.realtimeSinceStartup + 4f;
            while (master.IsAlive && Time.realtimeSinceStartup < deadline)
            {
                // Through any moment of hurt immunity the first blow left.
                Hit(scarecrow, float.MaxValue);
                yield return null;
            }

            Assert.That(master.IsAlive, Is.False, "The scarecrow falls.");
            yield return null;
            Assert.That(flock.All(r => r == null), Is.True, "With the scarecrow gone, so is its flock.");
        }
    }
}
