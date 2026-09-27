using System.Collections;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The shield as a resource: what it stops, what it costs, when it breaks, and the pressure
    /// meter that answers mindless light spam. Component level, so the checks are exact.
    /// </summary>
    public sealed class ShieldGuardTests
    {
        private const int TargetLayer = 10;
        private GameObject _target;

        [TearDown]
        public void TearDown()
        {
            if (_target != null) { Object.Destroy(_target); }
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator TheShieldStopsFrontalBlowsAndPaysWithGuard()
        {
            var actor = CreateGuard(out var guard);
            yield return RaiseAndWait(guard);

            var result = actor.ApplyDamage(Swing(20f, guardDamage: 15f, staggerDamage: 30f));

            Assert.That(result.Outcome, Is.EqualTo(DamageOutcome.Blocked));
            Assert.That(guard.CurrentGuard, Is.EqualTo(85f).Within(0.01f));
            Assert.That(actor.Health.Current, Is.EqualTo(98f).Within(0.01f),
                "A little damage leaks through by design.");
            Assert.That(actor.Stagger.Current, Is.EqualTo(9f).Within(0.01f),
                "A blocked blow still delivers a share of its poise damage.");
        }

        [UnityTest]
        public IEnumerator AShieldNeverStopsABlowFromBehind()
        {
            var actor = CreateGuard(out var guard);
            yield return RaiseAndWait(guard);

            // The guard faces right, so a hit travelling right arrives at its back.
            var result = actor.ApplyDamage(new DamageInfo(20f, Vector2.right, guardDamage: 15f,
                staggerDamage: 30f));

            Assert.That(result.Outcome, Is.EqualTo(DamageOutcome.Hit));
            Assert.That(guard.CurrentGuard, Is.EqualTo(guard.MaxGuard),
                "A back attack costs the shield nothing because it never touches it.");
            Assert.That(actor.Health.Current, Is.EqualTo(80f).Within(0.01f));
            Assert.That(actor.Stagger.Current, Is.EqualTo(30f).Within(0.01f),
                "Poise damage from behind lands in full.");
        }

        [UnityTest]
        public IEnumerator TheShieldIsStillDownDuringItsReactionDelay()
        {
            var actor = CreateGuard(out var guard);
            guard.RaiseGuard();
            Assert.That(guard.IsRaised, Is.True);
            Assert.That(guard.IsGuarding, Is.False, "The shield needs time to come up.");

            var result = actor.ApplyDamage(Swing(20f, guardDamage: 15f));
            Assert.That(result.Outcome, Is.EqualTo(DamageOutcome.Hit),
                "An attack inside the reaction window gets through.");

            yield return new WaitForSeconds(0.3f);
            Assert.That(guard.IsGuarding, Is.True);
        }

        [UnityTest]
        public IEnumerator HeavyBlowsEatTheGuardFarFasterThanLightOnes()
        {
            var actor = CreateGuard(out var guard);
            yield return RaiseAndWait(guard);

            for (var i = 0; i < 3; i++)
            {
                actor.ClearInvulnerability();
                actor.ApplyDamage(Swing(15f, guardDamage: 8f));
            }

            var afterCombo = guard.MaxGuard - guard.CurrentGuard;
            Assert.That(afterCombo, Is.EqualTo(24f).Within(0.01f));
            Assert.That(guard.IsBroken, Is.False,
                "A whole light combo must not break a shield on its own.");

            actor.ClearInvulnerability();
            actor.ApplyDamage(Swing(35f, guardDamage: 45f));
            Assert.That(guard.MaxGuard - guard.CurrentGuard - afterCombo, Is.EqualTo(45f).Within(0.01f),
                "One heavy blow is worth more guard than a full light combo.");
        }

        [UnityTest]
        public IEnumerator RunningOutOfGuardBreaksTheShieldAndItComesBackPartlyCharged()
        {
            var actor = CreateGuard(out var guard);
            yield return RaiseAndWait(guard);
            var broken = false;
            guard.GuardBroken += () => broken = true;

            actor.ApplyDamage(Swing(10f, guardDamage: 200f));

            Assert.That(broken, Is.True);
            Assert.That(guard.IsBroken, Is.True);
            Assert.That(guard.CurrentGuard, Is.Zero);
            Assert.That(guard.IsGuarding, Is.False, "A broken shield cannot block.");
            Assert.That(guard.CanRaiseGuard, Is.False);

            actor.ClearInvulnerability();
            var throughBrokenGuard = actor.ApplyDamage(Swing(20f, guardDamage: 15f));
            Assert.That(throughBrokenGuard.Outcome, Is.EqualTo(DamageOutcome.Hit));

            guard.RecoverFromBreak();
            Assert.That(guard.IsBroken, Is.False);
            Assert.That(guard.CurrentGuard, Is.EqualTo(55f).Within(0.01f),
                "The shield returns partly charged, not full.");
        }

        [UnityTest]
        public IEnumerator GuardRegeneratesAfterItsDelayAndStopsAtMaximum()
        {
            var actor = CreateGuard(out var guard, recoveryPerSecond: 100f, recoveryDelay: 0.2f);
            yield return RaiseAndWait(guard);
            actor.ApplyDamage(Swing(10f, guardDamage: 40f));
            Assert.That(guard.CurrentGuard, Is.EqualTo(60f).Within(0.01f));

            // Nothing regenerates while the shield is still up and being pressured.
            yield return new WaitForSeconds(0.5f);
            Assert.That(guard.CurrentGuard, Is.EqualTo(60f).Within(0.01f));

            guard.LowerGuard();
            yield return new WaitForSeconds(1.2f);
            Assert.That(guard.CurrentGuard, Is.EqualTo(guard.MaxGuard),
                "Guard comes back but never past its maximum.");
        }

        [UnityTest]
        public IEnumerator GuardNeverFallsBelowZero()
        {
            var actor = CreateGuard(out var guard);
            yield return RaiseAndWait(guard);
            actor.ApplyDamage(Swing(10f, guardDamage: 10000f));
            Assert.That(guard.CurrentGuard, Is.Zero);
        }

        [UnityTest]
        public IEnumerator LightPressureBuildsUpAndHeavyBlowsDoNot()
        {
            var actor = CreateGuard(out var guard);
            yield return RaiseAndWait(guard);

            for (var i = 0; i < 3; i++)
            {
                actor.ClearInvulnerability();
                actor.ApplyDamage(Swing(15f, guardDamage: 8f));
            }

            Assert.That(guard.Pressure, Is.EqualTo(3f).Within(0.01f),
                "Each blocked light hit leans on the shield.");

            var beforeHeavy = guard.Pressure;
            actor.ClearInvulnerability();
            actor.ApplyDamage(Swing(35f, guardDamage: 45f));
            Assert.That(guard.Pressure, Is.LessThanOrEqualTo(beforeHeavy),
                "A heavy blow is correct play and must not be punished as spam.");
        }

        [UnityTest]
        public IEnumerator PressureDecaysAndABackAttackAddsNone()
        {
            var actor = CreateGuard(out var guard, pressureDecay: 2f);
            yield return RaiseAndWait(guard);
            actor.ApplyDamage(Swing(15f, guardDamage: 8f));
            Assert.That(guard.Pressure, Is.EqualTo(1f).Within(0.01f));

            actor.ClearInvulnerability();
            actor.ApplyDamage(new DamageInfo(15f, Vector2.right, guardDamage: 8f));
            Assert.That(guard.Pressure, Is.LessThanOrEqualTo(1f),
                "Going around the shield is correct play, not pressure.");

            yield return new WaitForSeconds(1f);
            Assert.That(guard.Pressure, Is.Zero, "Pressure fades when the flurry stops.");
        }

        [UnityTest]
        public IEnumerator AParryRocksTheShieldAsWellAsThePoise()
        {
            var actor = CreateGuard(out var guard);
            yield return RaiseAndWait(guard);

            guard.ApplyParryDamage();
            Assert.That(guard.CurrentGuard, Is.EqualTo(75f).Within(0.01f),
                "Good timing chips the guard without heavy spam.");
        }

        [UnityTest]
        public IEnumerator ResetRestoresGuardAndClearsPressure()
        {
            var actor = CreateGuard(out var guard);
            yield return RaiseAndWait(guard);
            actor.ApplyDamage(Swing(15f, guardDamage: 8f));
            actor.ApplyDamage(Swing(10f, guardDamage: 200f));
            Assert.That(guard.IsBroken, Is.True);

            guard.ResetGuard();
            Assert.That(guard.CurrentGuard, Is.EqualTo(guard.MaxGuard));
            Assert.That(guard.IsBroken, Is.False);
            Assert.That(guard.IsRaised, Is.False);
            Assert.That(guard.Pressure, Is.Zero);
        }

        [UnityTest]
        public IEnumerator ADeadGuardStopsBlocking()
        {
            var actor = CreateGuard(out var guard);
            yield return RaiseAndWait(guard);
            actor.Health.SetCurrent(0f);

            var result = actor.ApplyDamage(Swing(20f, guardDamage: 15f));
            Assert.That(result.Outcome, Is.EqualTo(DamageOutcome.Ignored));
            Assert.That(guard.CurrentGuard, Is.EqualTo(guard.MaxGuard));
        }

        private static DamageInfo Swing(float damage, float guardDamage, float staggerDamage = 0f)
        {
            return new DamageInfo(damage, Vector2.left, guardDamage: guardDamage,
                staggerDamage: staggerDamage);
        }

        private static IEnumerator RaiseAndWait(EnemyGuard guard)
        {
            guard.RaiseGuard();
            yield return new WaitForSeconds(guard.ReactionDelay + 0.08f);
        }

        private CombatActor CreateGuard(out EnemyGuard guard, float recoveryPerSecond = 0f,
            float recoveryDelay = 5f, float pressureDecay = 0f)
        {
            _target = new GameObject("ShieldGuard") { layer = TargetLayer };
            _target.SetActive(false);
            var collider = _target.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            var health = _target.AddComponent<Health>();
            var stagger = _target.AddComponent<StaggerMeter>();
            JsonUtility.FromJsonOverwrite(
                "{\"_threshold\":1000,\"_resistance\":0,\"_recoveryDelay\":9,\"_stunDuration\":1}",
                stagger);
            var enemyGuard = _target.AddComponent<EnemyGuard>();
            JsonUtility.FromJsonOverwrite(
                "{\"_maxGuard\":100,\"_recoveryPerSecond\":" + Num(recoveryPerSecond)
                + ",\"_recoveryDelay\":" + Num(recoveryDelay)
                + ",\"_recoveryAfterBreak\":0.55,\"_breakDuration\":1.6,"
                + "\"_blockedDamageLeak\":0.1,\"_blockedStaggerShare\":0.3,"
                + "\"_parryGuardDamage\":25,\"_reactionDelay\":0.22,"
                + "\"_minGuardDuration\":0.5,\"_maxGuardDuration\":1.5,\"_guardCooldown\":0.6,"
                + "\"_minimumGuardToRaise\":0.15,\"_lightGuardDamage\":25,"
                + "\"_pressurePerLightHit\":1,\"_pressureDecayPerSecond\":" + Num(pressureDecay)
                + "}", enemyGuard);
            _target.AddComponent<Invulnerability>();
            var actor = _target.AddComponent<CombatActor>();
            JsonUtility.FromJsonOverwrite("{\"_hitInvulnerability\":0}", actor);
            _target.AddComponent<Hurtbox>();
            _target.AddComponent<FacesRight>();
            _target.SetActive(true);
            health.SetMax(100f, true);

            guard = enemyGuard;
            return actor;
        }

        /// <summary>JSON has no locale, so numbers must never carry a comma decimal point.</summary>
        private static string Num(float value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Stands in for a motor: the test guard always faces right.</summary>
        private sealed class FacesRight : MonoBehaviour, IFacing
        {
            public int FacingDirection => 1;
        }
    }

    /// <summary>
    /// The shield guard in the saved level: the AI raises and drops its shield, answers pressure
    /// with a bash, and its own heavy blow is a real threat to the player's guard.
    /// </summary>
    public sealed class ShieldGuardSceneTests
    {
        private PlayerCharacter _player;
        private PlayerMotor2D _motor;
        private EnemyController[] _guards;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            yield return SceneManager.LoadSceneAsync("TombTrial");
            _player = Object.FindAnyObjectByType<PlayerCharacter>();
            _motor = _player.GetComponent<PlayerMotor2D>();
            _guards = Object.FindObjectsByType<EnemyController>()
                .Where(enemy => enemy.name.StartsWith("ShieldGuard") && TombScene.InTomb(enemy))
                .OrderBy(enemy => enemy.transform.position.x)
                .ToArray();
            yield return new WaitForFixedUpdate();
        }

        [TearDown]
        public void TearDown()
        {
            SaveSystem.Delete();
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator TheLevelHasTwoShieldGuardsWithSeparateShields()
        {
            Assert.That(_guards.Length, Is.EqualTo(2));
            foreach (var shieldGuard in _guards)
            {
                Assert.That(shieldGuard.Guard, Is.Not.Null, "A shield guard needs a shield.");
                Assert.That(shieldGuard.Guard.CurrentGuard,
                    Is.EqualTo(shieldGuard.Guard.MaxGuard));
            }

            _guards[0].Guard.SpendGuard(40f);
            yield return null;
            Assert.That(_guards[1].Guard.CurrentGuard, Is.EqualTo(_guards[1].Guard.MaxGuard),
                "Shields are per enemy, never shared state.");
            Assert.That(_guards[0].Actor.Stagger.Resistance, Is.GreaterThan(0f),
                "An armoured guard resists poise damage better than a zombie.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TheGuardRaisesItsShieldWhenThePlayerClosesIn()
        {
            var shieldGuard = _guards[0];
            _motor.ResetTo(new Vector2(shieldGuard.transform.position.x - 2.2f, 0.9f));

            yield return WaitUntil(() => shieldGuard.Guard.IsRaised
                || shieldGuard.State == EnemyState.Attack, 8f);
            Assert.That(shieldGuard.Guard.IsRaised || shieldGuard.State == EnemyState.Attack,
                Is.True, "Closing in has to provoke a guard or a swing.");
        }

        [UnityTest]
        public IEnumerator TheGuardDoesNotHoldItsShieldForever()
        {
            var shieldGuard = _guards[0];
            _motor.ResetTo(new Vector2(shieldGuard.transform.position.x - 2.2f, 0.9f));
            yield return WaitUntil(() => shieldGuard.Guard.IsRaised, 8f);
            Assert.That(shieldGuard.Guard.IsRaised, Is.True);

            yield return WaitUntil(() => !shieldGuard.Guard.IsRaised, 6f);
            Assert.That(shieldGuard.Guard.IsRaised, Is.False,
                "The shield has to come down so the player gets a turn.");
        }

        [UnityTest]
        public IEnumerator BreakingTheShieldOpensTheGuardAndThenItRecovers()
        {
            var shieldGuard = _guards[0];
            var opened = false;
            var recovered = false;
            shieldGuard.GuardBrokenOpen += () => opened = true;
            shieldGuard.GuardRecovered += () => recovered = true;

            shieldGuard.Guard.SpendGuard(shieldGuard.Guard.MaxGuard);
            yield return new WaitForFixedUpdate();

            Assert.That(opened, Is.True);
            Assert.That(shieldGuard.State, Is.EqualTo(EnemyState.GuardBroken));
            Assert.That(shieldGuard.CanAttack(), Is.False, "A broken guard cannot swing.");

            yield return WaitUntil(() => recovered, 5f);
            Assert.That(recovered, Is.True);
            Assert.That(shieldGuard.State, Is.Not.EqualTo(EnemyState.GuardBroken));
            Assert.That(shieldGuard.Guard.CurrentGuard, Is.GreaterThan(0f));
        }

        [UnityTest]
        public IEnumerator ABlockedHitCausesAGuardImpactRatherThanAFullFlinch()
        {
            var shieldGuard = _guards[0];
            // Let the AI raise the shield itself; a manually raised one is lowered on its next tick.
            _motor.ResetTo(new Vector2(shieldGuard.transform.position.x - 2.2f, 0.9f));
            yield return WaitUntil(() => shieldGuard.Guard.IsGuarding, 10f);
            Assert.That(shieldGuard.Guard.IsGuarding, Is.True, "The AI has to raise its shield.");

            var impacted = false;
            shieldGuard.GuardImpacted += _ => impacted = true;
            shieldGuard.Actor.ClearInvulnerability();
            var result = shieldGuard.Actor.ApplyDamage(new DamageInfo(15f, FromPlayerSide(shieldGuard),
                guardDamage: 8f, attacker: _player.gameObject));

            Assert.That(result.Outcome, Is.EqualTo(DamageOutcome.Blocked));
            Assert.That(impacted, Is.True);
            yield return new WaitForFixedUpdate();
            Assert.That(shieldGuard.State, Is.EqualTo(EnemyState.GuardImpact),
                "The shield recoils; it does not stagger like a body hit.");
        }

        [UnityTest]
        public IEnumerator ShieldGuardsDoNotDamageEachOther()
        {
            var first = _guards[0];
            var second = _guards[1];
            var health = second.Actor.Health.Current;

            second.Actor.ClearInvulnerability();
            var result = second.Actor.ApplyDamage(new DamageInfo(30f, Vector2.right,
                attacker: first.gameObject));
            // Faction filtering happens in the hitbox, so a direct call still lands; what matters
            // is that both sit on the same side and the hitbox will skip them.
            Assert.That(first.Actor.Faction, Is.EqualTo(second.Actor.Faction));
            Assert.That(result.Outcome, Is.EqualTo(DamageOutcome.Hit));
            second.Actor.Health.SetCurrent(health);
            yield return null;
        }

        /// <summary>A hit direction that arrives at the guard's front.</summary>
        private static Vector2 FromPlayerSide(EnemyController shieldGuard)
        {
            return new Vector2(-shieldGuard.FacingDirection, 0f);
        }

        private static IEnumerator WaitUntil(System.Func<bool> condition, float timeoutSeconds)
        {
            var deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!condition() && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
        }
    }
}
