using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The pause, pressed for real: Escape freezes the game and silences the player, the
    /// settings back out before the pause does, the unported screens stay disabled, and the
    /// way out saves and reaches the title.
    /// </summary>
    public sealed class PauseMenuTests
    {
        private Keyboard _keyboard;
        private Gamepad _gamepad;
        private InputSettings _previousSettings;
        private InputSettings _testSettings;
        private bool _wasRunningInBackground;
        private PauseMenu _pause;
        private PlayerCharacter _player;

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
            _keyboard = InputSystem.AddDevice<Keyboard>("PauseTestKeyboard");
            _gamepad = InputSystem.AddDevice<Gamepad>("PauseTestGamepad");

            SaveSystem.Delete();
            yield return SceneManager.LoadSceneAsync(MainMenuController.GameSceneName);
            _pause = Object.FindAnyObjectByType<HudController>().Pause;
            _player = Object.FindAnyObjectByType<PlayerCharacter>();
            yield return Keys();
            yield return Realtime(0.3f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_pause != null && _pause.IsOpen) { _pause.Resume(); }
            if (_keyboard != null) { InputSystem.RemoveDevice(_keyboard); }
            if (_gamepad != null) { InputSystem.RemoveDevice(_gamepad); }
            InputSystem.settings = _previousSettings;
            Object.Destroy(_testSettings);
            Application.runInBackground = _wasRunningInBackground;
            GamePause.Clear();
            GameTimeScale.Reset();
            SaveSystem.Delete();
            yield return null;
        }

        [UnityTest]
        public IEnumerator EscapeFreezesTheGameAndEscapeAgainLetsItGo()
        {
            Assert.That(_pause, Is.Not.Null, "The HUD carries the pause.");
            yield return Tap(Key.Escape);
            Assert.That(_pause.IsOpen, Is.True);
            Assert.That(GamePause.IsPaused, Is.True);
            Assert.That(Time.timeScale, Is.Zero);

            yield return Tap(Key.Escape);
            Assert.That(_pause.IsOpen, Is.False);
            Assert.That(GamePause.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator StartOnThePadOpensItAndBackClosesIt()
        {
            yield return Pad(new GamepadState().WithButton(GamepadButton.Start));
            yield return Pad(new GamepadState());
            Assert.That(_pause.IsOpen, Is.True);

            yield return Pad(new GamepadState().WithButton(GamepadButton.East));
            yield return Pad(new GamepadState());
            Assert.That(_pause.IsOpen, Is.False);
        }

        [UnityTest]
        public IEnumerator WhilePausedTheAttackKeyDoesNothing()
        {
            yield return Tap(Key.Escape);
            Assume.That(_pause.IsOpen, Is.True);
            yield return Tap(Key.J);
            Assert.That(_player.Action, Is.Not.EqualTo(PlayerAction.LightAttack));
        }

        [UnityTest]
        public IEnumerator ScreensNotYetInTheGameAreShownButDisabled()
        {
            yield return Tap(Key.Escape);
            Assert.That(_pause.EnabledButtons.ToArray(), Is.EqualTo(new[]
                { "pause-resume", "pause-options", "pause-main-menu" }));
        }

        [UnityTest]
        public IEnumerator EscapeClosesTheSettingsBeforeThePause()
        {
            yield return Tap(Key.Escape);
            _pause.OpenOptions();
            Assert.That(_pause.IsOptionsOpen, Is.True);

            yield return Tap(Key.Escape);
            Assert.That(_pause.IsOptionsOpen, Is.False);
            Assert.That(_pause.IsOpen, Is.True, "Only the settings closed.");
        }

        [UnityTest]
        public IEnumerator TheWayOutSavesAndReachesTheTitle()
        {
            yield return Tap(Key.Escape);
            _pause.ReturnToMainMenu();
            var end = Time.realtimeSinceStartup + 5f;
            while (SceneManager.GetActiveScene().name != MainMenuController.SceneName
                   && Time.realtimeSinceStartup < end)
            {
                yield return null;
            }

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(MainMenuController.SceneName));
            Assert.That(SaveSystem.HasValidSave(), Is.True, "Leaving saved the game.");
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(GamePause.IsPaused, Is.False);
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

        private IEnumerator Pad(GamepadState state)
        {
            InputSystem.QueueStateEvent(_gamepad, state);
            yield return null;
        }

        private static IEnumerator Realtime(float seconds)
        {
            var end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end)
            {
                yield return null;
            }
        }
    }
}
