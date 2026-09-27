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
    /// The swings up and down and the pogo, as in Hollow Knight: up anywhere, down only in the
    /// air; a swing down onto an enemy or spikes throws the player up and gives the air moves
    /// back; touching spikes hurts and puts the player back on safe ground.
    /// </summary>
    public sealed class PogoTests
    {
        private Keyboard _keyboard;
        private InputSettings _previousSettings;
        private InputSettings _testSettings;
        private bool _wasRunningInBackground;
        private PlayerCharacter _player;
        private PlayerMotor2D _motor;
        private PlayerCombat _combat;

        private IEnumerator Load(string scene)
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
            _keyboard = InputSystem.AddDevice<Keyboard>("PogoTestKeyboard");

            SaveSystem.Delete();
            KeyBindings.ResetAll();
            yield return SceneManager.LoadSceneAsync(scene);
            _player = Object.FindAnyObjectByType<PlayerCharacter>();
            _motor = _player.GetComponent<PlayerMotor2D>();
            _combat = _player.GetComponent<PlayerCombat>();
            yield return new WaitForSeconds(0.2f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_keyboard != null) { InputSystem.RemoveDevice(_keyboard); }
            if (_previousSettings != null) { InputSystem.settings = _previousSettings; }
            if (_testSettings != null) { Object.Destroy(_testSettings); }
            Application.runInBackground = _wasRunningInBackground;
            GamePause.Clear();
            SaveSystem.Delete();
            yield return null;
        }

        private IEnumerator Press(params Key[] keys)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys));
            yield return new WaitForSecondsRealtime(0.05f);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return null;
        }

        private IEnumerator WaitUntil(System.Func<bool> condition, float seconds)
        {
            var deadline = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
        }

        private IEnumerator WaitForSwingToEnd()
        {
            yield return WaitUntil(() => !_combat.IsAttacking && _player.Action == PlayerAction.None, 2f);
            yield return new WaitForSeconds(0.3f);
        }

        [UnityTest]
        public IEnumerator UpAimsUpAnywhereAndDownOnlyInTheAir()
        {
            yield return Load("CoreMovement");
            _motor.ResetTo(new Vector2(2f, 0.85f));
            yield return WaitUntil(() => _motor.IsGrounded, 2f);

            yield return Press(Key.S, Key.J);
            Assert.That(_combat.IsAttacking, Is.True);
            Assert.That(_combat.CurrentDirection, Is.EqualTo(AttackDirection.Side),
                "On the ground down and attack is an ordinary swing.");
            yield return WaitForSwingToEnd();

            yield return Press(Key.W, Key.J);
            Assert.That(_combat.CurrentDirection, Is.EqualTo(AttackDirection.Up));
            var animation = _player.GetComponent<PlayerAnimationController>();
            yield return WaitUntil(() => animation.State == PlayerAnimationState.AttackUp, 0.3f);
            Assert.That(animation.State, Is.EqualTo(PlayerAnimationState.AttackUp));
            yield return WaitForSwingToEnd();

            _motor.ResetTo(new Vector2(2f, 4f));
            yield return new WaitForFixedUpdate();
            yield return Press(Key.S, Key.J);
            Assert.That(_combat.CurrentDirection, Is.EqualTo(AttackDirection.Down), "In the air, down aims down.");
        }

        [UnityTest]
        public IEnumerator ASwingDownOntoSpikesBouncesWithoutHurting()
        {
            yield return Load("CoreMovement");
            var spikes = Object.FindAnyObjectByType<Spikes>();
            Assert.That(spikes, Is.Not.Null, "The sandbox has spikes to practise on.");
            var health = _player.Health.Current;
            var pogos = 0;
            _combat.Pogoed += () => pogos++;

            var over = spikes.transform.position;
            _motor.ResetTo(new Vector2(over.x, over.y + 2.1f));
            yield return new WaitForFixedUpdate();
            Assert.That(_combat.TryStartLight(AttackDirection.Down), Is.True);
            yield return WaitUntil(() => pogos > 0, 1f);

            Assert.That(pogos, Is.EqualTo(1));
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(_motor.Velocity.y, Is.GreaterThan(0f), "Thrown up off the spikes.");
            yield return new WaitForSeconds(0.2f);
            Assert.That(_player.Health.Current, Is.EqualTo(health), "A pogo off spikes does not hurt.");
        }

        [UnityTest]
        public IEnumerator TouchingSpikesHurtsAndReturnsToTheLastSafeGround()
        {
            yield return Load("CoreMovement");
            var spikes = Object.FindAnyObjectByType<Spikes>();
            // Flat ground past the sandbox's ramp.
            _motor.ResetTo(new Vector2(spikes.transform.position.x + 6.5f, 0.85f));
            yield return WaitUntil(() => _motor.IsGrounded, 2f);
            yield return new WaitForSeconds(0.2f);
            var safe = _motor.LastSafeGround;
            var health = _player.Health.Current;

            _motor.ResetTo(new Vector2(spikes.transform.position.x, 0.85f));
            yield return WaitUntil(() => _player.Health.Current < health, 1f);
            Assert.That(_player.Health.Current, Is.LessThan(health));
            yield return new WaitForSeconds(0.6f);
            Assert.That(_player.transform.position.x, Is.EqualTo(safe.x).Within(0.6f),
                "Back where the ground was safe, as in Hollow Knight.");
        }

        [UnityTest]
        public IEnumerator ASwingDownOntoAnEnemyBouncesAndGivesTheAirMovesBack()
        {
            yield return Load("TombTrial");
            var dummy = Object.FindAnyObjectByType<TrainingDummy>();
            Assume.That(dummy, Is.Not.Null);
            var pogos = 0;
            _combat.Pogoed += () => pogos++;
            var hits = dummy.HitCount;

            var at = dummy.transform.position;
            _motor.ResetTo(new Vector2(at.x, at.y + 1.9f));
            yield return new WaitForFixedUpdate();
            Assert.That(_combat.TryStartLight(AttackDirection.Down), Is.True);
            yield return WaitUntil(() => pogos > 0, 1f);

            Assert.That(dummy.HitCount, Is.GreaterThan(hits), "The swing down hurts what it lands on.");
            Assert.That(pogos, Is.EqualTo(1), "One bounce per swing.");
            // The bounce is taken on the next physics step.
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(_motor.IsBouncing, Is.True);
            Assert.That(_motor.Velocity.y, Is.GreaterThan(0f));
            Assert.That(_motor.AirDashesUsed, Is.Zero);
        }
    }
}
