using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>The sandbox ramp: up and down it without stopping, hopping or sliding.</summary>
    public sealed class SlopeTests
    {
        private Keyboard _keyboard;
        private InputSettings _previousSettings;
        private InputSettings _testSettings;
        private bool _wasRunningInBackground;
        private PlayerMotor2D _motor;

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
            _keyboard = InputSystem.AddDevice<Keyboard>("SlopeTestKeyboard");
            SaveSystem.Delete();
            KeyBindings.ResetAll();
            yield return SceneManager.LoadSceneAsync("CoreMovement");
            _motor = Object.FindAnyObjectByType<PlayerMotor2D>();
            yield return new WaitForSeconds(0.2f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            InputSystem.RemoveDevice(_keyboard);
            InputSystem.settings = _previousSettings;
            Object.Destroy(_testSettings);
            Application.runInBackground = _wasRunningInBackground;
            yield return null;
        }

        [UnityTest]
        public IEnumerator RunningOverTheRampKeepsTheFeetOnTheGround()
        {
            Assume.That(GameObject.Find("Slope"), Is.Not.Null, "The sandbox has a ramp.");
            _motor.ResetTo(new Vector2(-6.1f, 0.85f));
            var deadline = Time.realtimeSinceStartup + 2f;
            while (!_motor.IsGrounded && Time.realtimeSinceStartup < deadline) { yield return null; }

            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.D));
            var airborneSteps = 0;
            var highest = _motor.transform.position.y;
            var sawSlope = false;
            deadline = Time.realtimeSinceStartup + 1.2f;
            while (_motor.transform.position.x < -1.8f && Time.realtimeSinceStartup < deadline)
            {
                yield return new WaitForFixedUpdate();
                if (!_motor.IsGrounded) { airborneSteps++; }
                sawSlope |= _motor.IsOnSlope;
                highest = Mathf.Max(highest, _motor.transform.position.y);
            }

            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            Assert.That(_motor.transform.position.x, Is.GreaterThan(-1.8f), "Up the rise and down again without stopping.");
            Assert.That(sawSlope, Is.True);
            Assert.That(highest, Is.GreaterThan(1.4f), "Climbed the ramp rather than stopping at its foot.");
            Assert.That(airborneSteps, Is.LessThanOrEqualTo(3), "No hopping off the downhill side.");
        }

        [UnityTest]
        public IEnumerator StandingOnTheRampDoesNotSlide()
        {
            _motor.ResetTo(new Vector2(-4.8f, 1.6f));
            var deadline = Time.realtimeSinceStartup + 2f;
            while (!_motor.IsGrounded && Time.realtimeSinceStartup < deadline) { yield return null; }
            yield return new WaitForSeconds(0.1f);
            var start = _motor.transform.position;
            yield return new WaitForSeconds(1f);
            Assert.That(Vector2.Distance(start, _motor.transform.position), Is.LessThan(0.05f));
        }
    }
}
