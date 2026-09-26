using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The defensive loop driven through the real player and the real input actions: dash windows,
    /// guard break and poise break, and the clean state a respawn has to produce.
    /// </summary>
    public sealed class PlayerDefenseSceneTests
    {
        private Keyboard _keyboard;
        private PlayerCharacter _player;
        private PlayerMotor2D _motor;
        private Stamina _stamina;
        private InputSettings _previousSettings;
        private InputSettings _testSettings;
        private bool _wasRunningInBackground;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _previousSettings = InputSystem.settings;
            if (_previousSettings.hideFlags == HideFlags.HideAndDontSave)
            {
                _previousSettings = Object.Instantiate(_previousSettings);
            }
            _testSettings = Object.Instantiate(_previousSettings);
            _testSettings.hideFlags = HideFlags.HideAndDontSave;
            _testSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            _testSettings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings = _testSettings;
            _wasRunningInBackground = Application.runInBackground;
            Application.runInBackground = true;
            _keyboard = InputSystem.AddDevice<Keyboard>("DefenseTestKeyboard");

            SaveSystem.Delete();
            yield return SceneManager.LoadSceneAsync("TombTrial");
            _player = Object.FindAnyObjectByType<PlayerCharacter>();
            _motor = _player.GetComponent<PlayerMotor2D>();
            _stamina = _player.Stamina;
            // Open ground, away from the dummies and the sparring partner.
            _motor.ResetTo(new Vector2(-12f, 0.85f));
            yield return Neutral(6);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_keyboard != null) { InputSystem.RemoveDevice(_keyboard); }
            InputSystem.settings = _previousSettings;
            Object.Destroy(_testSettings);
            Application.runInBackground = _wasRunningInBackground;
            Time.timeScale = 1f;
            SaveSystem.Delete();
            yield return null;
        }

        [UnityTest]
        public IEnumerator DashMovesThePlayerAndChargesStaminaExactlyOnce()
        {
            var startX = _player.transform.position.x;
            var stamina = _stamina.Current;
            var cost = _motor.DashSettings.StaminaCost;

            yield return PressDash();
            Assert.That(_player.Action, Is.EqualTo(PlayerAction.Dash));
            yield return Realtime(0.3f);

            Assert.That(_player.transform.position.x - startX, Is.GreaterThan(1.5f));
            Assert.That(stamina - _stamina.Current, Is.EqualTo(cost).Within(0.01f),
                "One dash must be paid for once.");
            Assert.That(_motor.IsDashing, Is.False);
            Assert.That(_player.Action, Is.Not.EqualTo(PlayerAction.Dash),
                "The dash has to hand the player back to normal movement.");
        }

        [UnityTest]
        public IEnumerator DashIsLimitedByItsCooldownNotByStamina()
        {
            yield return PressDash();
            Assert.That(_motor.IsDashing, Is.True);
            yield return Realtime(0.2f);
            yield return PressDash();
            Assert.That(_motor.IsDashing, Is.False, "The cooldown has to block the second dash.");

            yield return Realtime(0.5f);
            _stamina.Drain(_stamina.Current);
            yield return PressDash();
            // Free since 2026-09-26, as in Hollow Knight: only the cooldown limits it.
            Assert.That(_motor.IsDashing, Is.True, "The dash needs no stamina.");
        }

        [UnityTest]
        public IEnumerator InvincibilityCoversOnlyPartOfTheDash()
        {
            yield return PressDash();
            yield return Realtime(0.06f);
            var duringWindow = _player.Actor.ApplyDamage(new DamageInfo(15f, Vector2.left));
            Assert.That(duringWindow.Outcome, Is.EqualTo(DamageOutcome.Ignored),
                "The dash window has to swallow the hit.");
            Assert.That(_player.Health.Current, Is.EqualTo(_player.Health.Max));

            yield return Realtime(0.35f);
            var afterWindow = _player.Actor.ApplyDamage(new DamageInfo(15f, Vector2.left));
            Assert.That(afterWindow.Outcome, Is.EqualTo(DamageOutcome.Hit),
                "Once the window closes the player is hittable again.");
        }

        [UnityTest]
        public IEnumerator ADashCancelsAnAttackOnlyOnceItsCancelWindowOpens()
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.J));
            yield return Realtime(0.04f);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.J, Key.L));
            yield return Realtime(0.05f);

            // Light 1 becomes cancelable 0.16 s in; this is still its startup.
            Assert.That(_player.Action, Is.EqualTo(PlayerAction.LightAttack),
                "A dash must not cut through the startup frames.");
            yield return Realtime(0.2f);
            Assert.That(_player.Action, Is.EqualTo(PlayerAction.Dash),
                "The buffered dash fires as soon as the cancel window opens.");
            yield return Neutral(4);
        }

        [UnityTest]
        public IEnumerator ADashDoesNotPassThroughAWall()
        {
            _motor.ResetTo(new Vector2(-13.5f, 0.85f));
            yield return Neutral(4);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.A));
            yield return Realtime(0.1f);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.A, Key.L));
            yield return Realtime(0.4f);
            yield return Neutral(4);

            Assert.That(_player.transform.position.x, Is.GreaterThan(-15.2f),
                "Invincibility is not a pass through level collision.");
        }

        [UnityTest]
        public IEnumerator OnlyOneAirDashIsAllowedBeforeLanding()
        {
            // Dashing in the air is a traversal ability now, not something a new character has.
            // What this test is about is the one-per-airborne rule, so the ability is granted.
            Object.FindAnyObjectByType<GameProgress>().UnlockAbility(AbilityIds.AirDash);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space));
            yield return Realtime(0.12f);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            Assert.That(_motor.IsGrounded, Is.False);

            yield return PressDash();
            Assert.That(_motor.IsDashing, Is.True, "The first air dash is allowed.");
            Assert.That(_motor.AirDashesUsed, Is.EqualTo(1));
            yield return Realtime(0.55f);

            if (!_motor.IsGrounded)
            {
                yield return PressDash();
                Assert.That(_motor.IsDashing, Is.False, "Only one air dash before landing.");
            }

            yield return Realtime(1.2f);
            yield return Neutral(6);
            Assert.That(_motor.IsGrounded, Is.True);
            Assert.That(_motor.AirDashesUsed, Is.Zero, "Landing gives the air dash back.");
        }

        [UnityTest]
        public IEnumerator BlockingSlowsThePlayerWithoutStoppingHim()
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.D));
            yield return Realtime(0.4f);
            var walkSpeed = Mathf.Abs(_motor.Velocity.x);

            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.D, Key.Q));
            yield return Realtime(0.35f);

            Assert.That(_player.Action, Is.EqualTo(PlayerAction.Block));
            Assert.That(Mathf.Abs(_motor.Velocity.x), Is.LessThan(walkSpeed * 0.5f));
            Assert.That(Mathf.Abs(_motor.Velocity.x), Is.GreaterThan(0.1f));

            yield return Neutral(4);
            Assert.That(_player.Defense.IsBlocking, Is.False,
                "Releasing the button must drop the guard.");
            Assert.That(_player.Action, Is.EqualTo(PlayerAction.None));
        }

        [UnityTest]
        public IEnumerator AGuardBreakLocksThePlayerOutAndThenReleasesHim()
        {
            _stamina.Drain(_stamina.Current - 5f);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Q));
            yield return Realtime(0.3f);
            Assert.That(_player.Defense.Phase, Is.EqualTo(GuardPhase.Block));

            _player.Actor.ApplyDamage(new DamageInfo(10f, Vector2.left, guardDamage: 40f));
            yield return new WaitForFixedUpdate();

            Assert.That(_player.Action, Is.EqualTo(PlayerAction.GuardBroken));
            Assert.That(_player.Defense.IsBlocking, Is.False);
            Assert.That(_stamina.Current, Is.Zero);

            // Nothing may be started while the guard is broken.
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.L, Key.J));
            yield return Realtime(0.12f);
            Assert.That(_player.Action, Is.EqualTo(PlayerAction.GuardBroken));
            Assert.That(_motor.IsDashing, Is.False);

            yield return Neutral(2);
            yield return Realtime(1.1f);
            Assert.That(_player.Action, Is.EqualTo(PlayerAction.None),
                "The player comes back after the guard-break window.");
        }

        [UnityTest]
        public IEnumerator BreakingPoiseStunsThePlayer()
        {
            var threshold = _player.Stagger.Threshold;
            _player.Actor.ApplyDamage(new DamageInfo(1f, Vector2.left, staggerDamage: threshold + 5f));
            yield return new WaitForFixedUpdate();

            Assert.That(_player.Action, Is.EqualTo(PlayerAction.Stunned));
            Assert.That(_player.Stagger.IsStunned, Is.True);
            Assert.That(_player.Stagger.Current, Is.Zero, "A break empties the meter.");

            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Q, Key.L));
            yield return Realtime(0.15f);
            Assert.That(_player.Action, Is.EqualTo(PlayerAction.Stunned));
            Assert.That(_player.Defense.IsBlocking, Is.False);
            yield return Neutral(2);
        }

        [UnityTest]
        public IEnumerator RespawnGivesACleanDefensiveState()
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Q));
            yield return Realtime(0.25f);
            _stamina.Drain(_stamina.Current - 2f);
            _player.Actor.ApplyDamage(new DamageInfo(5f, Vector2.left, guardDamage: 40f));
            yield return new WaitForFixedUpdate();
            Assert.That(_player.Action, Is.EqualTo(PlayerAction.GuardBroken));

            yield return Neutral(2);
            _player.Respawn(new Vector2(-12f, 0.85f));
            yield return new WaitForFixedUpdate();

            Assert.That(_player.Action, Is.EqualTo(PlayerAction.None));
            Assert.That(_player.Defense.IsBlocking, Is.False);
            Assert.That(_player.Stagger.IsStunned, Is.False);
            Assert.That(_player.Stagger.Current, Is.Zero);
            Assert.That(_player.Actor.IsInvulnerable, Is.False);
            Assert.That(_stamina.Current, Is.EqualTo(_stamina.Max));
            Assert.That(_player.Health.Current, Is.EqualTo(_player.Health.Max));
        }

        [UnityTest]
        public IEnumerator TheSparringPartnerAttacksAndCanBeBlocked()
        {
            var attacker = Object.FindAnyObjectByType<TrainingAttacker>();
            Assert.That(attacker, Is.Not.Null, "The training area needs a sparring partner.");

            _motor.ResetTo(new Vector2(attacker.transform.position.x - 1.6f, 0.85f));
            yield return Neutral(4);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Q));

            var health = _player.Health.Current;
            var stamina = _stamina.Current;
            var sawAttack = false;
            var deadline = Time.realtimeSinceStartup + 8f;
            while (!sawAttack && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
                sawAttack = attacker.IsAttacking;
            }

            Assert.That(sawAttack, Is.True, "The partner has to swing on its own.");
            yield return Realtime(1.2f);
            yield return Neutral(4);

            Assert.That(_stamina.Current, Is.LessThan(stamina),
                "Holding the guard against a swing costs stamina.");
            Assert.That(_player.Health.Current, Is.EqualTo(health).Within(0.01f)
                .Or.LessThan(health), "A blocked swing must not be a free hit on health.");
        }

        private IEnumerator PressDash()
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.L));
            yield return Realtime(0.05f);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return new WaitForFixedUpdate();
        }

        private IEnumerator Neutral(int frames)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return null;
            for (var i = 0; i < frames; i++)
            {
                yield return new WaitForFixedUpdate();
            }
            yield return null;
        }

        /// <summary>Hit stop freezes the scaled clock, so waits run on real time.</summary>
        private static IEnumerator Realtime(float seconds)
        {
            var end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end)
            {
                yield return null;
            }
            yield return new WaitForFixedUpdate();
        }
    }
}
