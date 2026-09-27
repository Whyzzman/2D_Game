using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The hero's footwork in the world, as in Hollow Knight: dust when the feet leave or strike
    /// the ground and when a run starts or turns, none while running on, and a slight squash and
    /// stretch of the body at takeoff and landing.
    /// </summary>
    public sealed class MovementFeedbackTests
    {
        private Keyboard _keyboard;
        private InputSettings _previousSettings;
        private InputSettings _testSettings;
        private bool _wasRunningInBackground;
        private PlayerCharacter _player;
        private PlayerMotor2D _motor;
        private MovementFeedback _footwork;
        private CombatFeedbackService _feedback;
        private readonly List<FeedbackKind> _played = new List<FeedbackKind>();

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
            _keyboard = InputSystem.AddDevice<Keyboard>("FootworkTestKeyboard");

            SaveSystem.Delete();
            KeyBindings.ResetAll();
            FeedbackSettings.ResetToDefaults();
            yield return SceneManager.LoadSceneAsync("TombTrial");
            _player = Object.FindAnyObjectByType<PlayerCharacter>();
            _motor = _player.GetComponent<PlayerMotor2D>();
            _footwork = _player.GetComponent<MovementFeedback>();
            _feedback = Object.FindAnyObjectByType<CombatFeedbackService>();
            _feedback.Played += OnPlayed;
            yield return WaitUntil(() => _motor.IsGrounded, 2f);
            yield return new WaitForSeconds(0.3f);
            _played.Clear();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_feedback != null) { _feedback.Played -= OnPlayed; }
            if (_keyboard != null) { InputSystem.RemoveDevice(_keyboard); }
            if (_previousSettings != null) { InputSystem.settings = _previousSettings; }
            if (_testSettings != null) { Object.Destroy(_testSettings); }
            Application.runInBackground = _wasRunningInBackground;
            FeedbackSettings.ResetToDefaults();
            GamePause.Clear();
            SaveSystem.Delete();
            yield return null;
        }

        private void OnPlayed(FeedbackKind kind, Vector2 position) => _played.Add(kind);

        private int Count(FeedbackKind kind) => _played.FindAll(k => k == kind).Count;

        private void Hold(params Key[] keys) => InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys));

        private IEnumerator WaitUntil(System.Func<bool> condition, float seconds)
        {
            var deadline = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator AJumpRaisesDustAndStretchesAndTheLandingPressesTheHeroDown()
        {
            Assert.That(_footwork, Is.Not.Null, "The player prefab has no MovementFeedback.");
            var tallest = 0f;
            var flattest = 0f;
            Hold(Key.Space);
            yield return WaitUntil(() =>
            {
                tallest = Mathf.Max(tallest, _footwork.Squash);
                return !_motor.IsGrounded && _motor.Velocity.y < 0f;
            }, 2f);
            Hold();
            Assert.That(Count(FeedbackKind.JumpDust), Is.EqualTo(1), "Leaving the ground raised no dust.");
            Assert.That(tallest, Is.GreaterThan(0f), "The body did not stretch at takeoff.");

            yield return WaitUntil(() =>
            {
                flattest = Mathf.Min(flattest, _footwork.Squash);
                return _motor.IsGrounded && Count(FeedbackKind.LandDust) > 0;
            }, 3f);
            yield return WaitUntil(() =>
            {
                flattest = Mathf.Min(flattest, _footwork.Squash);
                return _footwork.Squash == 0f;
            }, 1f);

            Assert.That(Count(FeedbackKind.LandDust), Is.EqualTo(1), "The landing raised no dust.");
            Assert.That(flattest, Is.LessThan(0f), "The body did not squash on landing.");
            Assert.That(_motor.Visual.localPosition, Is.EqualTo(Vector3.zero),
                "The squash left the body out of place.");
            Assert.That(Mathf.Abs(_motor.Visual.localScale.y), Is.EqualTo(Mathf.Abs(_motor.VisualScale.y)),
                "The squash left the body out of shape.");
        }

        [UnityTest]
        public IEnumerator ARunKicksDustWhenItStartsAndTurnsButNotWhileItRunsOn()
        {
            Hold(Key.D);
            yield return new WaitForSeconds(0.5f);
            Assert.That(Count(FeedbackKind.RunDust), Is.EqualTo(1), "Starting to run kicked no dust.");

            Hold(Key.A);
            yield return new WaitForSeconds(0.5f);
            Hold();
            Assert.That(Count(FeedbackKind.RunDust), Is.EqualTo(2),
                "Turning at speed should kick dust once, and running on none.");
        }

        [UnityTest]
        public IEnumerator ADashLeftTrailsItsStreakToTheRight()
        {
            Hold(Key.A);
            yield return new WaitForSeconds(0.25f);
            Hold(Key.A, Key.L);
            yield return WaitUntil(() => Count(FeedbackKind.Dash) > 0, 1f);
            Hold();
            Assert.That(Count(FeedbackKind.Dash), Is.GreaterThan(0), "The dash played no streak.");

            var mirrored = false;
            foreach (var burst in Object.FindObjectsByType<VfxBurst>())
            {
                mirrored |= burst.IsPlaying && burst.transform.localScale.x < 0f;
            }

            Assert.That(mirrored, Is.True, "A dash to the left drew its streak as if dashing right.");
        }

        [UnityTest]
        public IEnumerator WithReducedMotionTheDustStaysButTheBodyKeepsItsShape()
        {
            FeedbackSettings.ReducedMotion = true;
            var moved = 0f;
            Hold(Key.Space);
            yield return WaitUntil(() =>
            {
                moved = Mathf.Max(moved, Mathf.Abs(_footwork.Squash));
                return !_motor.IsGrounded && _motor.Velocity.y < 0f;
            }, 2f);
            Hold();
            yield return WaitUntil(() =>
            {
                moved = Mathf.Max(moved, Mathf.Abs(_footwork.Squash));
                return _motor.IsGrounded && Count(FeedbackKind.LandDust) > 0;
            }, 3f);

            Assert.That(Count(FeedbackKind.JumpDust), Is.EqualTo(1));
            Assert.That(Count(FeedbackKind.LandDust), Is.EqualTo(1));
            Assert.That(moved, Is.Zero, "Reduced motion still squashed the body.");
        }
    }
}
