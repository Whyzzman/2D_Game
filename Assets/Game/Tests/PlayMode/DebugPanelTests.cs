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
    /// The developer panels, pressed for real: closed, they leave gameplay keys alone — 1 to 5
    /// are skills and spells, F11 must not delete a save — and an open panel owns the keys so
    /// the others cannot fire on top of it.
    /// </summary>
    public sealed class DebugPanelTests
    {
        private Keyboard _keyboard;
        private InputSettings _previousSettings;
        private InputSettings _testSettings;
        private bool _wasRunningInBackground;
        private GameProgress _progress;

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
            _keyboard = InputSystem.AddDevice<Keyboard>("DebugPanelKeyboard");

            SaveSystem.Delete();
            yield return TestWorld.Load();
            _progress = Object.FindAnyObjectByType<GameProgress>();
            yield return Keys();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            var noclip = Object.FindAnyObjectByType<DeveloperNoclip>();
            if (noclip != null) { noclip.SetActive(false); }
            var open = DebugPanels.Open;
            if (open is CombatDebugOverlay)
            {
                yield return Tap(Key.F1);
            }

            if (_keyboard != null) { InputSystem.RemoveDevice(_keyboard); }
            InputSystem.settings = _previousSettings;
            Object.Destroy(_testSettings);
            Application.runInBackground = _wasRunningInBackground;
            GameTimeScale.Reset();
            SaveSystem.Delete();
            yield return null;
        }

        [UnityTest]
        public IEnumerator WithEveryPanelClosedTheGameplayKeysDoNoDebugWork()
        {
            Assume.That(DebugPanels.Open, Is.Null);
            _progress.SaveNow();
            var gold = _progress.Currency;

            yield return Tap(Key.Digit1);
            yield return Tap(Key.Digit5);
            yield return Tap(Key.F11);

            Assert.That(_progress.Currency, Is.EqualTo(gold), "1 is a skill, not +100 gold.");
            Assert.That(SaveSystem.Exists, Is.True, "F11 must not delete the save.");
        }

        [UnityTest]
        public IEnumerator AnOpenPanelOwnsTheKeysUntilItCloses()
        {
            var overlay = Object.FindAnyObjectByType<CombatDebugOverlay>();
            Assume.That(overlay, Is.Not.Null, "Debug tools exist in the Editor.");

            yield return Tap(Key.F1);
            Assert.That(DebugPanels.Open, Is.SameAs(overlay));

            // F2 is the overlay's "back to the altar" and also the animation panel's open key.
            yield return Tap(Key.F2);
            Assert.That(DebugPanels.Open, Is.SameAs(overlay), "Only one panel is ever open.");

            var gold = _progress.Currency;
            yield return Tap(Key.Digit1);
            Assert.That(_progress.Currency, Is.EqualTo(gold + 100), "Open, its keys work.");

            yield return Tap(Key.F1);
            Assert.That(DebugPanels.Open, Is.Null);
        }

        [UnityTest]
        public IEnumerator CtrlNFliesTheHeroThroughTheLevelAndLandsItAgain()
        {
            var noclip = Object.FindAnyObjectByType<DeveloperNoclip>();
            Assume.That(noclip, Is.Not.Null, "Debug tools exist in the Editor.");
            Assert.That(noclip.GetComponentInParent<DeveloperOnly>(true), Is.Not.Null,
                "A release build never has it.");
            var motor = Object.FindAnyObjectByType<PlayerMotor2D>();
            var body = motor.GetComponent<Rigidbody2D>();
            var camera = Object.FindAnyObjectByType<CameraFollow2D>();

            yield return Keys(Key.N);
            yield return Keys();
            Assert.That(noclip.IsActive, Is.False, "N alone is no command.");

            yield return Keys(Key.LeftCtrl, Key.N);
            yield return Keys();
            Assert.That(noclip.IsActive, Is.True);
            Assert.That(body.simulated, Is.False, "Walls, spikes and enemies pass through.");
            Assert.That(camera.IsFree, Is.True, "The view leaves its rooms.");

            // Straight down, through the floor and past the bottom of the world, unharmed.
            var start = (Vector2)motor.transform.position;
            yield return Keys(Key.S, Key.LeftShift);
            var until = Time.realtimeSinceStartup + 2f;
            while (motor.transform.position.y > start.y - 15f && Time.realtimeSinceStartup < until)
            {
                yield return null;
            }

            yield return Keys();
            Assert.That(motor.transform.position.y, Is.LessThan(start.y - 15f), "It flies through the ground.");
            Assert.That(Mathf.Abs(motor.transform.position.x - start.x), Is.LessThan(0.01f));

            motor.transform.position = start;
            yield return Keys(Key.LeftCtrl, Key.N);
            yield return Keys();
            Assert.That(noclip.IsActive, Is.False);
            Assert.That(body.simulated && motor.enabled, Is.True, "Landed, the hero walks again.");
            Assert.That(camera.IsFree, Is.False);
            Assert.That(Object.FindAnyObjectByType<PlayerCharacter>().IsAlive, Is.True);
        }

        private IEnumerator Tap(Key key)
        {
            yield return Keys(key);
            yield return Keys();
        }

        private IEnumerator Keys(params Key[] keys)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys));
            yield return null;
        }
    }
}
