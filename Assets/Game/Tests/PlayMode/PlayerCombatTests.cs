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
    /// Drives the real player through the mapped input actions against a training dummy, so the
    /// combo, stamina and damage behave as they will under a human's hands.
    /// </summary>
    public sealed class PlayerCombatTests
    {
        private const float DummyStartHealth = 200f;
        private Keyboard _keyboard;
        private PlayerCharacter _player;
        private PlayerMotor2D _motor;
        private PlayerCombat _combat;
        private Stamina _stamina;
        private TrainingDummy _dummy;
        private Health _dummyHealth;
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
            // Batch-mode has no focused Game view; keep synthetic keyboard events in the test.
            _testSettings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings = _testSettings;
            _wasRunningInBackground = Application.runInBackground;
            Application.runInBackground = true;
            _keyboard = InputSystem.AddDevice<Keyboard>("CombatTestKeyboard");

            SaveSystem.Delete();
            yield return TestWorld.Load();
            _player = Object.FindAnyObjectByType<PlayerCharacter>();
            _motor = _player.GetComponent<PlayerMotor2D>();
            _combat = _player.GetComponent<PlayerCombat>();
            _stamina = _player.Stamina;
            _dummy = NearestDummy();
            _dummyHealth = _dummy.GetComponent<Health>();

            // Stand just within sword range of the dummy, facing it.
            _motor.ResetTo(new Vector2(_dummy.transform.position.x - 1.1f, TombScene.StartFeet.y + 0.85f));
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
        public IEnumerator TheFirstLightAttackDamagesTheDummyAndCostsNoStamina()
        {
            var stamina = _stamina.Current;
            yield return PressLightAttack();
            yield return WaitForSecondsRealtime(0.35f);

            Assert.That(_dummyHealth.Current,
                Is.EqualTo(DummyStartHealth - ExpectedLight(0)).Within(0.01f));
            // As in Hollow Knight, swinging the sword is free (item 37, 2026-09-26).
            Assert.That(_stamina.Current, Is.EqualTo(stamina).Within(0.01f));
            Assert.That(_dummy.HitCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator OneSwingHitsTheSameTargetOnlyOnce()
        {
            yield return PressLightAttack();
            yield return WaitForSecondsRealtime(0.5f);
            Assert.That(_dummy.HitCount, Is.EqualTo(1),
                "A single swing must not tick damage every frame.");
            Assert.That(_dummyHealth.Current,
                Is.EqualTo(DummyStartHealth - ExpectedLight(0)).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator TheThreeHitComboRunsInOrder()
        {
            // Holding forward while swinging, the way the combo is actually used: each hit nudges
            // the dummy back and the player follows at the attack's reduced speed.
            yield return PressLightAttack(true);
            yield return WaitForSecondsRealtime(0.24f);
            yield return PressLightAttack(true);
            yield return WaitForSecondsRealtime(0.24f);
            yield return PressLightAttack(true);
            yield return WaitForSecondsRealtime(0.6f);
            yield return Neutral(2);

            Assert.That(_dummy.HitCount, Is.EqualTo(3));
            // Each swing of the combo hits harder than the last, whatever the build.
            Assert.That(DummyStartHealth - _dummyHealth.Current,
                Is.EqualTo(ExpectedLight(0) + ExpectedLight(1) + ExpectedLight(2)).Within(0.05f));
        }

        [UnityTest]
        public IEnumerator TheComboRestartsFromTheFirstSwingAfterAPause()
        {
            yield return PressLightAttack(true);
            yield return WaitForSecondsRealtime(0.24f);
            yield return PressLightAttack(true);
            yield return Neutral(2);
            yield return WaitForSecondsRealtime(1.6f);

            // Step back into range so the test measures the combo index, not the distance.
            _motor.ResetTo(new Vector2(_dummy.transform.position.x - 1.1f, TombScene.StartFeet.y + 0.85f));
            yield return Neutral(4);
            var beforeRestart = _dummyHealth.Current;
            yield return PressLightAttack();
            yield return WaitForSecondsRealtime(0.4f);

            Assert.That(beforeRestart - _dummyHealth.Current,
                Is.EqualTo(ExpectedLight(0)).Within(0.01f),
                "After the pause the next press must be the first swing again.");
            Assert.That(_combat.ComboIndex, Is.Zero);
        }

        [UnityTest]
        public IEnumerator TheHeavyAttackHitsHarderAndCostsMore()
        {
            var stamina = _stamina.Current;
            yield return PressHeavyAttack();
            yield return WaitForSecondsRealtime(0.95f);

            Assert.That(DummyStartHealth - _dummyHealth.Current,
                Is.EqualTo(ExpectedHeavy()).Within(0.01f));
            Assert.That(stamina - _stamina.Current, Is.EqualTo(25f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator ALightAttackNeedsNoStaminaButAHeavyOneDoes()
        {
            _stamina.Drain(_stamina.Current - 4f);
            yield return Neutral(2);

            yield return PressHeavyAttack();
            yield return WaitForSecondsRealtime(0.6f);
            Assert.That(_dummy.HitCount, Is.Zero, "A heavy blow still needs its stamina.");

            yield return PressLightAttack();
            yield return WaitForSecondsRealtime(0.4f);
            Assert.That(_dummy.HitCount, Is.EqualTo(1), "The sword swings on an empty bar.");
        }

        [UnityTest]
        public IEnumerator StaminaRegeneratesAfterTheDelayAndAllowsAttackingAgain()
        {
            _stamina.Drain(_stamina.Current);
            Assert.That(_stamina.Current, Is.Zero);
            yield return WaitForSecondsRealtime(2.2f);

            Assert.That(_stamina.Current, Is.GreaterThan(10f), "Stamina must come back on its own.");
            yield return PressLightAttack();
            yield return WaitForSecondsRealtime(0.4f);
            Assert.That(_dummy.HitCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator AttackingKeepsTheHeroRunning()
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.D));
            yield return WaitForSecondsRealtime(0.4f);
            var runSpeed = Mathf.Abs(_motor.Velocity.x);
            Assert.That(runSpeed, Is.GreaterThan(2.5f));

            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.D, Key.J));
            yield return WaitForSecondsRealtime(0.12f);
            var attackSpeed = Mathf.Abs(_motor.Velocity.x);

            // As in Hollow Knight the swing does not root the hero (user's request 2026-10-27).
            Assert.That(_player.Action, Is.EqualTo(PlayerAction.LightAttack));
            Assert.That(attackSpeed, Is.GreaterThan(runSpeed * 0.6f), "The swing must not freeze the hero.");
            yield return Neutral(4);
        }

        [UnityTest]
        public IEnumerator TheAttackKnocksTheDummyBack()
        {
            var startX = _dummy.transform.position.x;
            yield return PressLightAttack();
            yield return WaitForSecondsRealtime(0.4f);
            Assert.That(_dummy.transform.position.x, Is.GreaterThan(startX + 0.03f),
                "The dummy is pushed away from the player.");
        }

        [UnityTest]
        public IEnumerator ThePlayerTakesDamageEntersHurtAndRecovers()
        {
            var health = _player.Health.Current;
            _player.Actor.ApplyDamage(new DamageInfo(20f, Vector2.right, knockback: 4f));
            yield return new WaitForFixedUpdate();

            Assert.That(_player.Health.Current, Is.EqualTo(health - 20f).Within(0.01f));
            Assert.That(_player.Action, Is.EqualTo(PlayerAction.Hurt));
            yield return WaitForSecondsRealtime(0.6f);
            Assert.That(_player.Action, Is.EqualTo(PlayerAction.None));
        }

        /// <summary>
        /// What a swing is worth right now, asked of the same calculator the game uses. Written
        /// out rather than hard-coded so tuning the scaling curve never silently breaks these.
        /// </summary>
        private float ExpectedLight(int comboIndex)
        {
            var weapon = _combat.Weapon;
            return WeaponDamageCalculator.AttackDamage(_player.Stats.WeaponDamage,
                weapon.LightAttack(comboIndex));
        }

        private float ExpectedHeavy()
        {
            return WeaponDamageCalculator.AttackDamage(_player.Stats.WeaponDamage,
                _combat.Weapon.HeavyAttack);
        }

        private TrainingDummy NearestDummy()
        {
            var dummies = Object.FindObjectsByType<TrainingDummy>();
            Assert.That(dummies.Length, Is.GreaterThan(0), "The training area needs a dummy.");
            var best = dummies[0];
            foreach (var dummy in dummies)
            {
                if (dummy.transform.position.x < best.transform.position.x)
                {
                    best = dummy;
                }
            }
            return best;
        }

        private IEnumerator PressLightAttack(bool holdForward = false)
        {
            InputSystem.QueueStateEvent(_keyboard, holdForward
                ? new KeyboardState(Key.D, Key.J)
                : new KeyboardState(Key.J));
            yield return WaitForSecondsRealtime(0.05f);
            InputSystem.QueueStateEvent(_keyboard, holdForward
                ? new KeyboardState(Key.D)
                : new KeyboardState());
            yield return null;
        }

        private IEnumerator PressHeavyAttack()
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.K));
            yield return WaitForSecondsRealtime(0.05f);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return null;
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

        /// <summary>
        /// Hit stop freezes the scaled clock, so waits use real time and never stall a test.
        /// </summary>
        private static IEnumerator WaitForSecondsRealtime(float seconds)
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
