using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// Component-level checks for the defensive half of the damage pipeline: immunity sources,
    /// directional blocking, the parry window and poise.
    /// </summary>
    public sealed class DefensiveCombatTests
    {
        private const int TargetLayer = 10;
        private GameObject _target;
        private GameObject _attacker;

        [TearDown]
        public void TearDown()
        {
            if (_target != null) { Object.Destroy(_target); }
            if (_attacker != null) { Object.Destroy(_attacker); }
            Time.timeScale = 1f;
        }

        [Test]
        public void ImmunitySourcesDoNotCancelEachOther()
        {
            var actor = CreateDefender(out _, out _);
            var immunity = actor.Invulnerability;
            immunity.Activate(ImmunitySource.Dodge, 5f);
            immunity.Activate(ImmunitySource.HitRecovery, 0.01f);
            immunity.Deactivate(ImmunitySource.HitRecovery);

            Assert.That(immunity.IsActive(ImmunitySource.Dodge), Is.True,
                "Ending one source must leave the others running.");
            Assert.That(immunity.IsInvulnerable, Is.True);
            immunity.Clear();
            Assert.That(immunity.IsInvulnerable, Is.False);
        }

        [Test]
        public void ImmunityIgnoresDamagePoiseAndKnockbackAndReportsADodge()
        {
            var actor = CreateDefender(out _, out _, stagger: 20f);
            var body = _target.GetComponent<Rigidbody2D>();
            DamageInfo? dodged = null;
            actor.AttackDodged += info => dodged = info;
            actor.SetInvulnerable(ImmunitySource.Dodge, 1f);

            var result = actor.ApplyDamage(new DamageInfo(30f, Vector2.left, knockback: 9f,
                staggerDamage: 50f, attacker: _attacker));

            Assert.That(result.Outcome, Is.EqualTo(DamageOutcome.Ignored));
            Assert.That(actor.Health.Current, Is.EqualTo(actor.Health.Max));
            Assert.That(actor.Stagger.Current, Is.Zero);
            Assert.That(actor.Stagger.IsStunned, Is.False);
            Assert.That(body.linearVelocity, Is.EqualTo(Vector2.zero));
            Assert.That(dodged.HasValue, Is.True, "The dodge hook must fire for a future counter.");
        }

        [UnityTest]
        public IEnumerator BlockPaysStaminaEqualToTheGuardDamage()
        {
            var actor = CreateDefender(out var defense, out var stamina);
            yield return null;
            defense.BeginBlock();
            yield return WaitPastParryWindow(defense);

            var before = stamina.Current;
            var result = actor.ApplyDamage(new DamageInfo(20f, Vector2.left, guardDamage: 18f,
                staggerDamage: 40f, attacker: _attacker));

            Assert.That(result.Outcome, Is.EqualTo(DamageOutcome.Blocked));
            Assert.That(actor.Health.Current, Is.EqualTo(actor.Health.Max));
            Assert.That(before - stamina.Current, Is.EqualTo(18f).Within(0.01f),
                "Guard damage, not health damage, sets the stamina price.");
            Assert.That(actor.Stagger.Current, Is.Zero, "A blocked hit does not fill poise.");
        }

        [UnityTest]
        public IEnumerator AnUnblockableHitGoesStraightThroughARaisedGuard()
        {
            var actor = CreateDefender(out var defense, out var stamina);
            yield return null;
            var flagged = false;
            defense.UnblockableHit += _ => flagged = true;
            defense.BeginBlock();
            yield return WaitPastParryWindow(defense);

            var before = stamina.Current;
            var result = actor.ApplyDamage(new DamageInfo(20f, Vector2.left, isUnblockable: true,
                attacker: _attacker));

            Assert.That(result.Outcome, Is.EqualTo(DamageOutcome.Hit));
            Assert.That(actor.Health.Current, Is.EqualTo(actor.Health.Max - 20f));
            Assert.That(stamina.Current, Is.EqualTo(before), "An unblockable hit costs no stamina.");
            Assert.That(flagged, Is.True, "The unblockable tell hook must fire.");
        }

        [UnityTest]
        public IEnumerator AnUnparryableHitInsideTheWindowIsOnlyBlocked()
        {
            var actor = CreateDefender(out var defense, out _);
            yield return null;
            defense.BeginBlock();
            Assert.That(defense.Phase, Is.EqualTo(GuardPhase.Parry));

            var result = actor.ApplyDamage(new DamageInfo(20f, Vector2.left, isParryable: false,
                guardDamage: 10f, attacker: _attacker));
            Assert.That(result.Outcome, Is.EqualTo(DamageOutcome.Blocked));
        }

        [UnityTest]
        public IEnumerator OnlyTheFirstHitOfAGuardRaiseIsParried()
        {
            var actor = CreateDefender(out var defense, out _);
            var attackerActor = CreateAttacker();
            yield return null;
            defense.BeginBlock();

            var first = actor.ApplyDamage(Swing());
            Assert.That(first.Outcome, Is.EqualTo(DamageOutcome.Parried));
            Assert.That(attackerActor.Stagger.Current, Is.GreaterThan(0f).Or.EqualTo(0f));

            var second = actor.ApplyDamage(Swing());
            Assert.That(second.Outcome, Is.EqualTo(DamageOutcome.Blocked),
                "Holding the guard must not parry a whole flurry.");
            Assert.That(defense.Phase, Is.EqualTo(GuardPhase.Block));
        }

        [UnityTest]
        public IEnumerator ReRaisingTheGuardTooSoonSkipsTheParryWindow()
        {
            var actor = CreateDefender(out var defense, out _, parryCooldown: 1f);
            yield return null;
            defense.BeginBlock();
            defense.EndBlock();
            defense.BeginBlock();

            Assert.That(defense.Phase, Is.EqualTo(GuardPhase.Block),
                "Mashing the guard button must not hand out free parry windows.");
            Assert.That(actor.ApplyDamage(Swing()).Outcome, Is.EqualTo(DamageOutcome.Blocked));
        }

        [UnityTest]
        public IEnumerator AMissedParryWindowSettlesIntoAnOrdinaryBlock()
        {
            var actor = CreateDefender(out var defense, out _);
            yield return null;
            defense.BeginBlock();
            yield return WaitPastParryWindow(defense);

            Assert.That(defense.Phase, Is.EqualTo(GuardPhase.Block));
            Assert.That(defense.IsBlocking, Is.True, "The guard stays up after a missed parry.");
            Assert.That(actor.ApplyDamage(Swing()).Outcome, Is.EqualTo(DamageOutcome.Blocked));
        }

        [UnityTest]
        public IEnumerator AGuardStopsStaminaFromRegenerating()
        {
            CreateDefender(out var defense, out var stamina, regeneration: 100f);
            yield return null;
            stamina.Drain(50f);
            var drained = stamina.Current;
            defense.BeginBlock();
            yield return new WaitForSeconds(1.6f);

            Assert.That(stamina.Current, Is.EqualTo(drained).Within(0.01f),
                "A raised guard must not out-heal the stamina an attacker takes off it.");
            defense.EndBlock();
            yield return new WaitForSeconds(1.6f);
            Assert.That(stamina.Current, Is.GreaterThan(drained));
        }

        [UnityTest]
        public IEnumerator PoiseRecoversAfterItsDelayAndResetsAfterAStun()
        {
            var actor = CreateDefender(out _, out _, stagger: 40f);
            var stagger = actor.Stagger;
            JsonUtility.FromJsonOverwrite(
                "{\"_threshold\":40,\"_recoveryDelay\":0.2,\"_recoveryPerSecond\":100,"
                + "\"_stunDuration\":0.3}", stagger);
            stagger.ResetStagger();

            Assert.That(stagger.Add(20f), Is.False);
            Assert.That(stagger.Current, Is.EqualTo(20f).Within(0.01f));
            yield return new WaitForSeconds(0.7f);
            Assert.That(stagger.Current, Is.Zero, "Poise comes back after the quiet spell.");

            Assert.That(stagger.Add(50f), Is.True, "Crossing the threshold breaks poise.");
            Assert.That(stagger.IsStunned, Is.True);
            Assert.That(stagger.Current, Is.Zero, "The meter empties on a break.");
            yield return new WaitForSeconds(0.45f);
            Assert.That(stagger.IsStunned, Is.False);
        }

        [Test]
        public void PoiseResistanceReducesIncomingStagger()
        {
            var actor = CreateDefender(out _, out _, stagger: 100f, resistance: 0.5f);
            actor.Stagger.Add(40f);
            Assert.That(actor.Stagger.Current, Is.EqualTo(20f).Within(0.01f));
        }

        [Test]
        public void ADeadActorIgnoresEverything()
        {
            var actor = CreateDefender(out var defense, out _);
            actor.Health.SetCurrent(0f);
            defense.BeginBlock();

            var result = actor.ApplyDamage(Swing());
            Assert.That(result.Outcome, Is.EqualTo(DamageOutcome.Ignored));
            Assert.That(actor.IsAlive, Is.False);
        }

        private DamageInfo Swing()
        {
            return new DamageInfo(20f, Vector2.left, guardDamage: 8f, staggerDamage: 10f,
                attacker: _attacker);
        }

        /// <summary>
        /// JSON has no locale: a value formatted under a comma-decimal culture would not parse.
        /// </summary>
        private static string Num(float value)
        {
            return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private static IEnumerator WaitPastParryWindow(PlayerDefense defense)
        {
            yield return new WaitForSeconds(defense.ParryWindow + 0.08f);
        }

        private CombatActor CreateDefender(out PlayerDefense defense, out Stamina stamina,
            float stagger = 1000f, float resistance = 0f, float regeneration = 0f,
            float parryCooldown = 0f)
        {
            _target = new GameObject("Defender") { layer = TargetLayer };
            _target.SetActive(false);
            var collider = _target.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            var body = _target.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            var health = _target.AddComponent<Health>();
            var staggerMeter = _target.AddComponent<StaggerMeter>();
            JsonUtility.FromJsonOverwrite(
                "{\"_threshold\":" + Num(stagger) + ",\"_resistance\":" + Num(resistance)
                + ",\"_recoveryDelay\":5,\"_stunDuration\":1}", staggerMeter);
            var staminaComponent = _target.AddComponent<Stamina>();
            JsonUtility.FromJsonOverwrite(
                "{\"_maxStamina\":100,\"_regenerationPerSecond\":" + Num(regeneration)
                + ",\"_regenerationDelay\":0,\"_guardingRegenerationScale\":0}", staminaComponent);
            var playerDefense = _target.AddComponent<PlayerDefense>();
            JsonUtility.FromJsonOverwrite(
                "{\"_parryWindow\":0.14,\"_parryCooldown\":" + Num(parryCooldown)
                + ",\"_parryStaminaCost\":0,\"_parryStaggerDamage\":45,\"_parryHitStop\":0,"
                + "\"_staminaPerGuardDamage\":1,\"_guardBreakStun\":0.9}", playerDefense);
            _target.AddComponent<Invulnerability>();
            var actor = _target.AddComponent<CombatActor>();
            _target.AddComponent<Hurtbox>();
            _target.AddComponent<FixedFacing>();
            _target.SetActive(true);
            health.SetMax(100f, true);

            defense = playerDefense;
            stamina = staminaComponent;
            return actor;
        }

        private CombatActor CreateAttacker()
        {
            _attacker = new GameObject("Attacker");
            _attacker.SetActive(false);
            _attacker.AddComponent<Health>();
            var stagger = _attacker.AddComponent<StaggerMeter>();
            JsonUtility.FromJsonOverwrite("{\"_threshold\":40,\"_stunDuration\":2}", stagger);
            _attacker.AddComponent<Invulnerability>();
            var actor = _attacker.AddComponent<CombatActor>();
            _attacker.SetActive(true);
            return actor;
        }

        /// <summary>Stands in for a motor: the test defender always faces right.</summary>
        private sealed class FixedFacing : MonoBehaviour, IFacing
        {
            public int FacingDirection => 1;
        }
    }
}
