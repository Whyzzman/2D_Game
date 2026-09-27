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
    /// The inventory and map keys, pressed for real. A screen opens from its key only while
    /// something draws it — the HUD does now — because a key that paused the world behind an
    /// invisible menu looked exactly like a frozen game.
    /// </summary>
    public sealed class MenuKeyTests
    {
        private Keyboard _keyboard;
        private InputSettings _previousSettings;
        private InputSettings _testSettings;
        private bool _wasRunningInBackground;
        private InventoryScreen _inventory;
        private WorldMapScreen _map;

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
            _keyboard = InputSystem.AddDevice<Keyboard>("MenuKeyKeyboard");

            SaveSystem.Delete();
            yield return SceneManager.LoadSceneAsync("TombTrial");
            _inventory = Object.FindAnyObjectByType<InventoryScreen>();
            _map = Object.FindAnyObjectByType<WorldMapScreen>();
            yield return Keys();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_inventory != null && _inventory.IsOpen) { _inventory.Close(); }
            if (_map != null && _map.IsOpen) { _map.Close(); }
            GamePause.Clear();
            Time.timeScale = 1f;
            if (_keyboard != null) { InputSystem.RemoveDevice(_keyboard); }
            InputSystem.settings = _previousSettings;
            Object.Destroy(_testSettings);
            Application.runInBackground = _wasRunningInBackground;
            SaveSystem.Delete();
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheInventoryKeyOpensAndClosesTheWindow()
        {
            Assert.That(_inventory.HasPresenter, Is.True, "The HUD draws the inventory.");

            yield return Tap(Key.I);
            Assert.That(_inventory.IsOpen, Is.True);
            Assert.That(GamePause.IsPaused, Is.True);

            yield return Tap(Key.I);
            Assert.That(_inventory.IsOpen, Is.False);
            Assert.That(GamePause.IsPaused, Is.False);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator WithNothingToDrawItTheInventoryKeyDoesNotFreezeTheGame()
        {
            Object.FindAnyObjectByType<CombatDebugOverlay>().enabled = false;
            Object.FindAnyObjectByType<HudController>().enabled = false;
            Assert.That(_inventory.HasPresenter, Is.False);

            yield return Tap(Key.I);
            yield return Tap(Key.Tab);
            Assert.That(_inventory.IsOpen, Is.False);
            Assert.That(GamePause.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TheMapKeyOpensAndClosesTheMap()
        {
            Assert.That(_map.HasPresenter, Is.True, "The HUD draws the map.");

            yield return Tap(Key.M);
            Assert.That(_map.IsOpen, Is.True);
            Assert.That(GamePause.IsPaused, Is.True);

            yield return Tap(Key.M);
            Assert.That(_map.IsOpen, Is.False);
            Assert.That(GamePause.IsPaused, Is.False);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator WithNothingToDrawItTheMapKeyDoesNotFreezeTheGame()
        {
            Object.FindAnyObjectByType<HudController>().enabled = false;
            Assert.That(_map.HasPresenter, Is.False);

            yield return Tap(Key.M);
            Assert.That(_map.IsOpen, Is.False);
            Assert.That(GamePause.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator LosingItsLastPresenterClosesAnOpenScreen()
        {
            yield return Tap(Key.I);
            Assert.That(_inventory.IsOpen, Is.True);

            Object.FindAnyObjectByType<CombatDebugOverlay>().enabled = false;
            Assert.That(_inventory.IsOpen, Is.True, "The HUD still draws it.");
            Object.FindAnyObjectByType<HudController>().enabled = false;
            Assert.That(_inventory.IsOpen, Is.False, "Nothing on screen must not hold the world paused.");
            Assert.That(GamePause.IsPaused, Is.False);
            yield return null;
            LogAssert.NoUnexpectedReceived();
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
