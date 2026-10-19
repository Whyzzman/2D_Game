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
            yield return TestWorld.Load();
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
        public IEnumerator AKillingBlowThrowsTheBodyWhichThenCrumbles()
        {
            // The body is thrown along the blow and lands; since 2026-09-28 (visual item 4, the
            // user's choice) it no longer lies there until a rest but crumbles to ash. A rest still
            // brings the enemy back whole.
            var side = OpenSide();
            var start = _zombie.transform.position.x;
            _zombie.Actor.ApplyDamage(Blow(99999f, side, 3f));
            Assert.That(_zombie.IsAlive, Is.False);

            var motor = _zombie.GetComponent<EnemyMotor2D>();
            var visual = _zombie.transform.Find("Visual");
            var crumble = _zombie.GetComponent<CorpseCrumble>();
            yield return new WaitForSeconds(0.3f);
            yield return WaitUntil(motor.ProbeGround, 3f);

            Assert.That((_zombie.transform.position.x - start) * side, Is.GreaterThan(0.3f),
                "The body was not thrown along the blow.");
            Assert.That(motor.ProbeGround(), Is.True, "The body did not come to rest on the floor.");
            Assert.That(visual.gameObject.activeSelf, Is.True, "The body is seen to fall before it goes.");
            var body = _zombie.GetComponent<Collider2D>();
            Assert.That((body.excludeLayers.value & (1 << LayerMask.NameToLayer("Player"))) != 0, Is.True,
                "The player would trip over the corpse.");

            yield return WaitUntil(() => crumble.IsGone, 6f);
            Assert.That(crumble.IsGone, Is.True, "The body crumbled away.");
            Assert.That(visual.gameObject.activeSelf, Is.False);

            _zombie.ResetEnemy();
            yield return null;
            Assert.That(_zombie.IsAlive, Is.True);
            Assert.That(visual.gameObject.activeSelf, Is.True, "A rest brings the body back.");
            Assert.That(visual.Find("Body").GetComponent<SpriteRenderer>().color.a, Is.EqualTo(1f).Within(0.01f),
                "Whole, not half crumbled.");
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
        public IEnumerator TheWindUpDrawsABreathAndFlashesThePointJustBeforeTheBlow()
        {
            var attack = _zombie.Definition.Attack(0).Attack;
            Assume.That(attack.Windup, Is.GreaterThan(TelegraphCues.FlashLead + 0.05f));
            var body = _zombie.GetComponentInChildren<EnemyAnimationController>().Body.transform;
            _played.Clear();
            Assert.That(_zombie.Combat.TryStartAttack(attack, 0.1f), Is.True);
            yield return null;
            var glints = _played.FindAll(k => k == FeedbackKind.EnemyTelegraph).Count;

            var started = Time.time;
            var tallest = 1f;
            var flashedAt = -1f;
            while (Time.time - started < attack.Windup + 0.1f)
            {
                tallest = Mathf.Max(tallest, body.localScale.y);
                if (flashedAt < 0f && _played.FindAll(k => k == FeedbackKind.EnemyTelegraph).Count > glints)
                {
                    flashedAt = Time.time - started;
                }

                yield return null;
            }

            Assert.That(tallest, Is.GreaterThan(1.01f), "The body draws a breath through the wind-up.");
            Assert.That(flashedAt, Is.GreaterThan(0f), "The point flashed before the blow.");
            Assert.That(flashedAt, Is.EqualTo(attack.Windup - TelegraphCues.FlashLead).Within(0.12f),
                "About a fifth of a second before it lands.");
            yield return WaitUntil(() => !_zombie.IsAttacking, 3f);
            yield return null;
            Assert.That(body.localScale, Is.EqualTo(Vector3.one), "The breath is let go.");
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
