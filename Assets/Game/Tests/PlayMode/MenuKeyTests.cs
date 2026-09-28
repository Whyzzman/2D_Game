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
            yield return TestWorld.Load();
            _inventory = Object.FindAnyObjectByType<InventoryScreen>();
            _map = Object.FindAnyObjectByType<WorldMapScreen>();
            yield return Keys();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_inventory != null && _inventory.IsOpen) { _inventory.Close(); }
            if (_map != null && _map.IsOpen) { _map.Close(); }
            KeyBindings.ResetAll();
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

        // ---------- rebinding (controls.html) ----------

        private static BindingRow Row(string label) =>
            System.Array.Find(KeyBindings.Rows, row => row.Label == label);

        [UnityTest]
        public IEnumerator AReboundJumpKeyJumpsAndTheOldOneNoLongerDoes()
        {
            var motor = Object.FindAnyObjectByType<PlayerMotor2D>();
            var deadline = Time.realtimeSinceStartup + 3f;
            while (!motor.IsGrounded && Time.realtimeSinceStartup < deadline) { yield return null; }
            Assume.That(motor.IsGrounded, Is.True);

            using (var editor = new BindingEditor())
            {
                Assert.That(editor.Assign(Row("Стрибок"), KeyBindings.PrimarySlot, "<Keyboard>/u").Change,
                    Is.EqualTo(BindingChange.Assigned));
            }

            yield return Tap(Key.Space);
            yield return new WaitForSeconds(0.15f);
            Assert.That(motor.IsGrounded, Is.True, "Space is not the jump any more.");

            yield return Tap(Key.U);
            deadline = Time.realtimeSinceStartup + 1f;
            while (motor.IsGrounded && Time.realtimeSinceStartup < deadline) { yield return null; }
            Assert.That(motor.IsGrounded, Is.False, "The new key jumps.");
        }

        [UnityTest]
        public IEnumerator TheControlsScreenTakesTheNextKeyAndEscapeOnlyCancelsTheWait()
        {
            var pause = Object.FindAnyObjectByType<HudController>().Pause;
            yield return Tap(Key.Escape);
            Assume.That(pause.IsOpen, Is.True);
            pause.OpenControls();
            yield return null;
            var controls = pause.Controls;
            Assert.That(pause.IsControlsOpen, Is.True);
            var dash = System.Array.IndexOf(KeyBindings.Rows, Row("Ривок"));

            Assert.That(controls.BeginListening(dash, KeyBindings.PrimarySlot), Is.True);
            yield return Tap(Key.LeftShift);
            var deadline = Time.realtimeSinceStartup + 1f;
            while (controls.IsListening && Time.realtimeSinceStartup < deadline) { yield return null; }
            Assert.That(controls.Editor.Glyph(Row("Ривок"), KeyBindings.PrimarySlot), Is.EqualTo("Shift"));
            Assert.That(KeyBindings.Glyph("Dash", gamepad: false), Is.EqualTo("Shift"));

            yield return null;
            Assert.That(controls.BeginListening(dash, KeyBindings.PrimarySlot), Is.True);
            yield return Tap(Key.Escape);
            yield return null;
            Assert.That(controls.IsListening, Is.False);
            Assert.That(pause.IsControlsOpen, Is.True, "Escape cancelled the wait, not the screen.");
            Assert.That(controls.Editor.Glyph(Row("Ривок"), KeyBindings.PrimarySlot), Is.EqualTo("Shift"));

            yield return Tap(Key.Escape);
            Assert.That(pause.IsControlsOpen, Is.False, "A second Escape leaves the screen.");
            Assert.That(pause.IsOpen, Is.True);
        }

        [UnityTest]
        public IEnumerator TheHudGlyphsFollowTheKeys()
        {
            var glyphs = Object.FindAnyObjectByType<HudController>().KeyGlyphs;
            Assert.That(glyphs.TextOf("Heal"), Is.EqualTo("R"));
            using (var editor = new BindingEditor())
            {
                editor.Assign(Row("Фляга"), KeyBindings.PrimarySlot, "<Keyboard>/g");
            }

            yield return null;
            Assert.That(glyphs.TextOf("Heal"), Is.EqualTo("G"));
            Assert.That(glyphs.TextOf("ActiveSkill1"), Is.EqualTo("1"));
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
