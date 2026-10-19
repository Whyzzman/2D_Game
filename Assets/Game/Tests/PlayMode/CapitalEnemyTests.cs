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
    /// The capital's enemies at work (item 2, stage C, 2026-09-28): the gargoyle sleeps until the
    /// hero comes close, the raven swoops from the air and falls when killed, the priest mends a
    /// wounded ally and steps away from a hero at arm's length.
    /// </summary>
    public sealed class CapitalEnemyTests
    {
        private PlayerCharacter _player;
        private PlayerMotor2D _motor;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            yield return TestWorld.Load();
            var progress = Object.FindAnyObjectByType<GameProgress>();
            progress.MarkBossDefeated(TombScene.GuardianId);
            progress.MarkBossDefeated(TombScene.WardenId);
            yield return TestWorld.LoadEveryZone();
            _player = Object.FindAnyObjectByType<PlayerCharacter>();
            _motor = _player.GetComponent<PlayerMotor2D>();
            // The fights here are watched, not survived.
            _player.Actor.SetInvulnerable(ImmunitySource.Script, 120f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SaveSystem.Delete();
            yield return null;
        }

        /// <summary>The thing of that kind nearest a place: the capital has three tiers, so by both axes.</summary>
        private static T Nearest<T>(Vector2 at) where T : Component =>
            Object.FindObjectsByType<T>(FindObjectsInactive.Exclude)
                .OrderBy(c => ((Vector2)c.transform.position - at).sqrMagnitude).First();

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
        public IEnumerator TheGargoyleSleepsUntilTheHeroComesClose()
        {
            // The stone guard before the palace (Levels/capital.txt): the hero comes in by the tower's door.
            var approach = CapitalScene.Room("room_capital_palace_approach");
            var gargoyle = Nearest<EnemyDormancy>(new Vector2(approach.center.x, approach.yMin + 2f));
            _motor.ResetTo(new Vector2(approach.xMin + 1.5f, approach.yMin + 2f));
            yield return new WaitForSeconds(1f);
            Assert.That(gargoyle.IsAsleep, Is.True, "Eleven away: a statue.");
            Assert.That(gargoyle.GetComponent<EnemyController>().IsHeld, Is.True);

            _motor.ResetTo(new Vector2(gargoyle.transform.position.x - 2.5f, approach.yMin + 2f));
            yield return Until(() => !gargoyle.IsAsleep, 3f, "Close by, it tears itself loose.");
            Assert.That(gargoyle.GetComponent<EnemyController>().IsHeld, Is.False);
        }

        [UnityTest]
        public IEnumerator TheRavenDivesFromTheAirAndFallsWhenKilled()
        {
            var square = CapitalScene.Room("room_capital_angel_square");
            var raven = Nearest<EnemyFlight>(new Vector2(square.center.x, square.yMin + 7f));
            var startY = raven.transform.position.y;
            var dived = false;
            raven.DiveStarted += _ => dived = true;
            _motor.ResetTo(new Vector2(raven.transform.position.x + 2f, square.yMin + 2f));
            yield return Until(() => dived, 8f, "It sees the hero below and dives.");
            Assert.That(raven.GetComponent<Rigidbody2D>().gravityScale, Is.EqualTo(0f), "Alive, it flies.");

            raven.GetComponent<CombatActor>().ApplyDamage(new DamageInfo(float.MaxValue, Vector2.up,
                isUnblockable: true, isParryable: false));
            yield return Until(() => raven.transform.position.y < square.yMin + 2.5f, 4f, "Dead, it falls out of the sky.");
            Assert.That(startY, Is.GreaterThan(square.yMin + 5f), "It had been flying.");
        }

        [UnityTest]
        public IEnumerator ThePriestMendsAWoundedAllyAndStepsAwayFromTheHero()
        {
            // The temple (Levels/capital.txt): a priest on the dais, a stone guard below it.
            var dais = CapitalScene.Spot("temple-dais");
            var priest = Nearest<EnemySupport>(dais);
            var ally = Nearest<EnemyDormancy>(dais).GetComponent<CombatActor>();
            ally.Health.SetCurrent(ally.Health.Max * 0.4f);
            var mended = false;
            var stepped = false;
            priest.Healed += _ => mended = true;
            priest.Blinked += (_, _) => stepped = true;

            // On the dais with it, out of its arm's reach: it sees the hero, fights, and mends.
            _motor.ResetTo(new Vector2(priest.transform.position.x + 4f, dais.y + 0.85f));
            var enemy = priest.GetComponent<EnemyController>();
            yield return Until(() => mended, 10f,
                $"Seeing the hero, it mends the gargoyle. priest={enemy.State} "
                + $"at={priest.transform.position} hero={_player.transform.position}");
            Assert.That(ally.Health.Normalized, Is.GreaterThan(0.6f));

            var before = priest.transform.position;
            _motor.ResetTo(new Vector2(before.x - 1.2f, before.y));
            yield return Until(() => stepped, 6f, "At arm's length, it steps away.");
            Assert.That(Mathf.Abs(priest.transform.position.x - _player.transform.position.x), Is.GreaterThan(2.6f));
        }
    }
}
