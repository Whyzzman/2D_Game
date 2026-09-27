using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// How enemies answer a blow, as in Hollow Knight: a landed hit slides them back a little, a
    /// killing blow throws the body to lie where it falls, a wind-up glints on the weapon, and a
    /// broken balance shows stars over the head.
    /// </summary>
    public sealed class EnemyReactionTests
    {
        private EnemyController _zombie;
        private CombatFeedbackService _feedback;
        private readonly List<FeedbackKind> _played = new List<FeedbackKind>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            FeedbackSettings.ResetToDefaults();
            yield return SceneManager.LoadSceneAsync("TombTrial");
            _feedback = Object.FindAnyObjectByType<CombatFeedbackService>();
            _feedback.Played += OnPlayed;
            // The zombie furthest from the player, so nothing but the test touches it.
            var player = Object.FindAnyObjectByType<PlayerCharacter>();
            _zombie = Object.FindObjectsByType<EnemyController>()
                .Where(enemy => enemy.name.StartsWith("ZombieSoldier") && TombScene.InTomb(enemy))
                .OrderByDescending(enemy => Mathf.Abs(enemy.transform.position.x - player.transform.position.x))
                .First();
            _zombie.SetTarget(null);
            yield return WaitUntil(() => _zombie.GetComponent<EnemyMotor2D>().IsGrounded, 2f);
            yield return new WaitForSeconds(0.3f);
            _played.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            if (_feedback != null) { _feedback.Played -= OnPlayed; }
            FeedbackSettings.ResetToDefaults();
            GamePause.Clear();
            SaveSystem.Delete();
            GameTimeScale.Reset();
        }

        private void OnPlayed(FeedbackKind kind, Vector2 position) => _played.Add(kind);

        private static IEnumerator WaitUntil(System.Func<bool> condition, float seconds)
        {
            var deadline = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
        }

        /// <summary>Pushes the zombie toward open floor, whichever side it has.</summary>
        private int OpenSide()
        {
            var motor = _zombie.GetComponent<EnemyMotor2D>();
            return motor.HasGroundToward(1) && !motor.HasWallToward(1) ? 1 : -1;
        }

        private DamageInfo Blow(float amount, int direction, float knockback, float stagger = 5f) =>
            new DamageInfo(amount, new Vector2(direction, 0f), knockback: knockback,
                staggerDamage: stagger, hitPoint: _zombie.transform.position);

        [UnityTest]
        public IEnumerator ALandedHitSlidesTheEnemyBackAboutHalfAUnit()
        {
            var side = OpenSide();
            var start = _zombie.transform.position.x;
            _zombie.Actor.ApplyDamage(Blow(5f, side, 3f));
            yield return new WaitForSeconds(EnemyKnockback.SlideTime + 0.1f);

            var moved = (_zombie.transform.position.x - start) * side;
            Assert.That(moved, Is.InRange(0.3f, 1.1f), "A light blow should shove the enemy about half a unit.");
        }

        [UnityTest]
        public IEnumerator AKillingBlowThrowsTheBodyToLieWhereItFalls()
        {
            var side = OpenSide();
            var start = _zombie.transform.position.x;
            _zombie.Actor.ApplyDamage(Blow(99999f, side, 3f));
            Assert.That(_zombie.IsAlive, Is.False);

            var motor = _zombie.GetComponent<EnemyMotor2D>();
            yield return new WaitForSeconds(0.3f);
            yield return WaitUntil(motor.ProbeGround, 3f);
            yield return new WaitForSeconds(2.5f);

            Assert.That((_zombie.transform.position.x - start) * side, Is.GreaterThan(0.3f),
                "The body was not thrown along the blow.");
            Assert.That(motor.ProbeGround(), Is.True, "The body did not come to rest on the floor.");
            var visual = _zombie.transform.Find("Visual");
            Assert.That(visual.gameObject.activeSelf, Is.True, "The body vanished instead of lying there.");
            var body = _zombie.GetComponent<Collider2D>();
            Assert.That(body.enabled, Is.True);
            Assert.That((body.excludeLayers.value & (1 << LayerMask.NameToLayer("Player"))) != 0, Is.True,
                "The player would trip over the corpse.");

            _zombie.ResetEnemy();
            yield return null;
            Assert.That(_zombie.IsAlive, Is.True);
            Assert.That(body.excludeLayers.value, Is.Zero, "A revived enemy kept a corpse's collisions.");
        }

        [UnityTest]
        public IEnumerator AWindUpGlintsOnTheWeapon()
        {
            var attack = _zombie.Definition.Attack(0).Attack;
            Assert.That(_zombie.Combat.TryStartAttack(attack, 0.1f), Is.True);
            yield return null;

            Assert.That(_played, Does.Contain(FeedbackKind.EnemyTelegraph), "The wind-up did not glint.");
        }

        [UnityTest]
        public IEnumerator ABrokenBalanceShowsStarsUntilItRecovers()
        {
            var stars = _zombie.GetComponent<StunIndicator>();
            Assert.That(stars, Is.Not.Null, "Enemies are built without a stun indicator.");
            yield return null;
            Assert.That(stars.IsShowing, Is.False);

            _zombie.Actor.ApplyDamage(Blow(1f, OpenSide(), 0f, 9999f));
            yield return null;
            yield return null;
            Assert.That(_zombie.Actor.Stagger.IsStunned, Is.True);
            Assert.That(stars.IsShowing, Is.True, "A stunned enemy shows no stars.");

            yield return WaitUntil(() => !_zombie.Actor.Stagger.IsStunned, 6f);
            yield return null;
            Assert.That(stars.IsShowing, Is.False, "The stars outlived the stun.");
        }

        [UnityTest]
        public IEnumerator BothBossesCarryStars()
        {
            yield return null;
            foreach (var boss in Object.FindObjectsByType<BossController>(FindObjectsInactive.Include))
            {
                Assert.That(boss.GetComponent<StunIndicator>(), Is.Not.Null, $"{boss.name} has no stars.");
            }
        }
    }
}
