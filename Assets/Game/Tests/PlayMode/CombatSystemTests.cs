using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// Component-level combat checks built in code, so hit resolution, guarding and poise are
    /// verified without depending on a scene layout.
    /// </summary>
    public sealed class CombatSystemTests
    {
        private const int TargetLayer = 10;
        private GameObject _target;
        private GameObject _attacker;

        [TearDown]
        public void TearDown()
        {
            if (_target != null) { Object.Destroy(_target); }
            if (_attacker != null) { Object.Destroy(_attacker); }
        }

        [Test]
        public void DamageReducesHealthAndReportsTheLethalHit()
        {
            var actor = CreateTarget(30f, out _);
            var first = actor.ApplyDamage(Hit(10f));
            Assert.That(first.Outcome, Is.EqualTo(DamageOutcome.Hit));
            Assert.That(first.AppliedDamage, Is.EqualTo(10f));
            Assert.That(actor.Health.Current, Is.EqualTo(20f));

            actor.ClearInvulnerability();
            var lethal = actor.ApplyDamage(Hit(999f));
            Assert.That(lethal.IsLethal, Is.True);
            Assert.That(lethal.AppliedDamage, Is.EqualTo(20f), "Overkill is not counted as damage.");
            Assert.That(actor.IsAlive, Is.False);
        }

        [Test]
        public void HealthStaysBetweenZeroAndItsMaximum()
        {
            var actor = CreateTarget(50f, out _);
            actor.Health.Remove(500f);
            Assert.That(actor.Health.Current, Is.Zero);
            Assert.That(actor.Health.Normalized, Is.Zero);

            actor.Health.RestoreToFull();
            Assert.That(actor.Health.Heal(200f), Is.Zero, "A full bar cannot be healed further.");
            Assert.That(actor.Health.Current, Is.EqualTo(50f));

            actor.Health.SetCurrent(-10f);
            Assert.That(actor.Health.Current, Is.Zero);
            actor.Health.SetCurrent(999f);
            Assert.That(actor.Health.Current, Is.EqualTo(50f));
        }

        [Test]
        public void StaminaStaysBetweenZeroAndItsMaximumAndGatesSpending()
        {
            var stamina = CreateStamina(40f);
            Assert.That(stamina.TrySpend(30f), Is.True);
            Assert.That(stamina.Current, Is.EqualTo(10f));
            Assert.That(stamina.TrySpend(30f), Is.False, "A partial payment must not be taken.");
            Assert.That(stamina.Current, Is.EqualTo(10f));

            Assert.That(stamina.Drain(999f), Is.EqualTo(10f));
            Assert.That(stamina.Current, Is.Zero);
            stamina.RestoreToFull();
            Assert.That(stamina.Current, Is.EqualTo(40f));
        }

        [Test]
        public void InvincibilityFramesIgnoreHitsEntirely()
        {
            var actor = CreateTarget(100f, out _);
            actor.SetInvulnerable(1f);
            var result = actor.ApplyDamage(Hit(25f));
            Assert.That(result.Outcome, Is.EqualTo(DamageOutcome.Ignored));
            Assert.That(actor.Health.Current, Is.EqualTo(100f));
        }

        [Test]
        public void KnockbackPushesTheTargetAwayFromTheAttacker()
        {
            var actor = CreateTarget(100f, out var body);
            actor.ApplyDamage(new DamageInfo(5f, Vector2.left, knockback: 8f));
            Assert.That(body.linearVelocity.x, Is.LessThan(-7f));
            Assert.That(body.linearVelocity.y, Is.GreaterThan(0f), "A hit lifts the target slightly.");
        }

        [Test]
        public void PoiseStunsOnlyWhenTheThresholdIsCrossed()
        {
            var actor = CreateTarget(200f, out _, stagger: 40f);
            var stunned = false;
            actor.Stagger.Staggered += _ => stunned = true;
            actor.ApplyDamage(new DamageInfo(1f, Vector2.right, staggerDamage: 20f));
            Assert.That(stunned, Is.False);
            actor.ClearInvulnerability();
            actor.ApplyDamage(new DamageInfo(1f, Vector2.right, staggerDamage: 25f));
            Assert.That(stunned, Is.True);
            Assert.That(actor.Stagger.IsStunned, Is.True);
        }

        [UnityTest]
        public IEnumerator AHitboxDamagesEachTargetOncePerActivation()
        {
            var actor = CreateTarget(100f, out _);
            _target.transform.position = new Vector3(1f, 0f, 0f);
            var hitbox = CreateHitbox();
            var hits = 0;
            hitbox.Hit += (_, _) => hits++;
            // Let the new colliders register with the physics scene before the first sweep.
            yield return new WaitForFixedUpdate();

            hitbox.Begin(new DamageInfo(7f, Vector2.right), new Vector2(1f, 0f),
                new Vector2(2f, 2f), 1);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(hits, Is.EqualTo(1));
            Assert.That(actor.Health.Current, Is.EqualTo(93f));

            hitbox.End();
            actor.ClearInvulnerability();
            hitbox.Begin(new DamageInfo(7f, Vector2.right), new Vector2(1f, 0f),
                new Vector2(2f, 2f), 1);
            yield return new WaitForFixedUpdate();
            Assert.That(hits, Is.EqualTo(2), "A new activation may hit the same target again.");
            hitbox.End();
        }

        [UnityTest]
        public IEnumerator AHitboxNeverDamagesItsOwnFaction()
        {
            var ally = CreateTarget(100f, out _);
            _target.transform.position = new Vector3(1f, 0f, 0f);
            var hitbox = CreateHitbox(Faction.Enemy);
            yield return new WaitForFixedUpdate();

            // Both sides are Enemy: the swing must pass through without a scratch.
            hitbox.Begin(new DamageInfo(20f, Vector2.right), new Vector2(1f, 0f),
                new Vector2(2f, 2f), 1);
            yield return new WaitForFixedUpdate();
            Assert.That(ally.Health.Current, Is.EqualTo(100f), "Enemies do not fight each other.");
            hitbox.End();

            // The same hitbox owned by the player's side does land.
            Object.DestroyImmediate(_attacker);
            var playerHitbox = CreateHitbox(Faction.Player);
            yield return new WaitForFixedUpdate();
            playerHitbox.Begin(new DamageInfo(20f, Vector2.right), new Vector2(1f, 0f),
                new Vector2(2f, 2f), 1);
            yield return new WaitForFixedUpdate();
            Assert.That(ally.Health.Current, Is.EqualTo(80f));
            playerHitbox.End();
        }

        [UnityTest]
        public IEnumerator BlockingStopsFrontalDamageAndDrainsStamina()
        {
            var defense = CreatePlayerDefense(out var actor, out var stamina);
            yield return null;
            defense.BeginBlock();
            // Wait past the parry window so the hit resolves as an ordinary block.
            yield return new WaitForSeconds(0.3f);

            var staminaBefore = stamina.Current;
            var result = actor.ApplyDamage(new DamageInfo(20f, Vector2.left, attacker: _attacker,
                guardDamage: 15f));
            Assert.That(result.Outcome, Is.EqualTo(DamageOutcome.Blocked));
            Assert.That(actor.Health.Current, Is.EqualTo(actor.Health.Max));
            Assert.That(stamina.Current, Is.LessThan(staminaBefore));
        }

        [UnityTest]
        public IEnumerator ABackAttackIgnoresTheGuard()
        {
            var defense = CreatePlayerDefense(out var actor, out _);
            yield return null;
            defense.BeginBlock();
            yield return new WaitForSeconds(0.3f);

            // The target faces right, so a hit travelling right comes from behind.
            var result = actor.ApplyDamage(new DamageInfo(20f, Vector2.right, attacker: _attacker));
            Assert.That(result.Outcome, Is.EqualTo(DamageOutcome.Hit));
            Assert.That(actor.Health.Current, Is.LessThan(actor.Health.Max));
        }

        [UnityTest]
        public IEnumerator AParryNegatesDamageAndStaggersTheAttacker()
        {
            var defense = CreatePlayerDefense(out var actor, out _);
            var attackerActor = CreateAttackerActor();
            yield return null;

            var parried = false;
            defense.Parried += _ => parried = true;
            defense.BeginBlock();
            var result = actor.ApplyDamage(new DamageInfo(30f, Vector2.left, attacker: _attacker));

            Assert.That(result.Outcome, Is.EqualTo(DamageOutcome.Parried));
            Assert.That(parried, Is.True);
            Assert.That(actor.Health.Current, Is.EqualTo(actor.Health.Max));
            Assert.That(attackerActor.Stagger.IsStunned, Is.True);
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator RunningOutOfStaminaBreaksTheGuard()
        {
            var defense = CreatePlayerDefense(out var actor, out var stamina, maxStamina: 10f);
            yield return null;
            var broken = false;
            defense.GuardBroken += () => broken = true;
            defense.BeginBlock();
            yield return new WaitForSeconds(0.3f);

            actor.ApplyDamage(new DamageInfo(40f, Vector2.left, attacker: _attacker,
                guardDamage: 40f));
            Assert.That(broken, Is.True);
            Assert.That(defense.IsBlocking, Is.False);
            Assert.That(stamina.Current, Is.Zero);
            // Guard break is a stamina failure, not a poise break: the meter is left alone and
            // PlayerCharacter answers the event with its own GuardBroken state.
            Assert.That(actor.Stagger.IsStunned, Is.False);
        }

        private static DamageInfo Hit(float amount)
        {
            return new DamageInfo(amount, Vector2.right);
        }

        private CombatActor CreateTarget(float health, out Rigidbody2D body, float stagger = 1000f)
        {
            _target = new GameObject("Target") { layer = TargetLayer };
            _target.SetActive(false);
            var collider = _target.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            body = _target.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            var healthComponent = _target.AddComponent<Health>();
            var staggerMeter = _target.AddComponent<StaggerMeter>();
            JsonUtility.FromJsonOverwrite(
                "{\"_threshold\":" + stagger + ",\"_recoveryDelay\":5,\"_stunDuration\":2}",
                staggerMeter);
            var actor = _target.AddComponent<CombatActor>();
            _target.AddComponent<Hurtbox>();
            _target.SetActive(true);
            healthComponent.SetMax(health, true);
            return actor;
        }

        private Stamina CreateStamina(float max)
        {
            _target = new GameObject("StaminaOwner");
            _target.SetActive(false);
            var stamina = _target.AddComponent<Stamina>();
            JsonUtility.FromJsonOverwrite(
                "{\"_maxStamina\":" + max + ",\"_regenerationPerSecond\":0}", stamina);
            _target.SetActive(true);
            return stamina;
        }

        private Hitbox CreateHitbox(Faction? ownerFaction = null)
        {
            _attacker = new GameObject("Attacker");
            _attacker.SetActive(false);
            if (ownerFaction.HasValue)
            {
                _attacker.AddComponent<Health>();
                var actor = _attacker.AddComponent<CombatActor>();
                JsonUtility.FromJsonOverwrite(
                    "{\"_faction\":" + (int)ownerFaction.Value + "}", actor);
            }

            var hitbox = _attacker.AddComponent<Hitbox>();
            JsonUtility.FromJsonOverwrite(
                "{\"_targetMask\":{\"m_Bits\":" + (1 << TargetLayer) + "},\"_drawGizmo\":false}",
                hitbox);
            _attacker.SetActive(true);
            return hitbox;
        }

        private PlayerDefense CreatePlayerDefense(out CombatActor actor, out Stamina stamina,
            float maxStamina = 100f)
        {
            _target = new GameObject("Defender") { layer = TargetLayer };
            _target.SetActive(false);
            var collider = _target.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            var body = _target.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            var health = _target.AddComponent<Health>();
            var staggerMeter = _target.AddComponent<StaggerMeter>();
            JsonUtility.FromJsonOverwrite("{\"_threshold\":1000,\"_stunDuration\":2}", staggerMeter);
            var staminaComponent = _target.AddComponent<Stamina>();
            JsonUtility.FromJsonOverwrite(
                "{\"_maxStamina\":" + maxStamina + ",\"_regenerationPerSecond\":0}",
                staminaComponent);
            var defense = _target.AddComponent<PlayerDefense>();
            JsonUtility.FromJsonOverwrite(
                "{\"_parryWindow\":0.18,\"_parryCooldown\":0,\"_staminaPerGuardDamage\":0.8,"
                + "\"_guardBreakStun\":0.9,\"_parryStaggerDamage\":45,\"_parryHitStop\":0}",
                defense);
            actor = _target.AddComponent<CombatActor>();
            _target.AddComponent<Hurtbox>();
            _target.AddComponent<FixedFacing>();
            _target.SetActive(true);
            health.SetMax(100f, true);
            stamina = staminaComponent;
            return defense;
        }

        private CombatActor CreateAttackerActor()
        {
            _attacker = new GameObject("Attacker");
            _attacker.SetActive(false);
            _attacker.AddComponent<Health>();
            var staggerMeter = _attacker.AddComponent<StaggerMeter>();
            JsonUtility.FromJsonOverwrite("{\"_threshold\":40,\"_stunDuration\":2}", staggerMeter);
            var actor = _attacker.AddComponent<CombatActor>();
            _attacker.SetActive(true);
            return actor;
        }

        /// <summary>Stands in for a motor: the test targets always face right.</summary>
        private sealed class FixedFacing : MonoBehaviour, IFacing
        {
            public int FacingDirection => 1;
        }
    }
}
