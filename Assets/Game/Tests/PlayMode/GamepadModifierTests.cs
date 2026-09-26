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
    /// The pad's ability layer, pressed for real. Held LT turns LB/RB into the skills and X/Y/B
    /// into the spells; without it the same buttons attack, guard and interact — never both at
    /// once. The reader is read directly, with the character and motor switched off so nothing
    /// else consumes the presses first.
    /// </summary>
    public sealed class GamepadModifierTests
    {
        private Gamepad _gamepad;
        private InputSettings _previousSettings;
        private InputSettings _testSettings;
        private bool _wasRunningInBackground;
        private PlayerInputReader _input;

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
            _gamepad = InputSystem.AddDevice<Gamepad>("ModifierTestGamepad");

            yield return SceneManager.LoadSceneAsync("CoreMovement");
            var motor = Object.FindAnyObjectByType<PlayerMotor2D>();
            Assert.That(motor, Is.Not.Null);
            _input = motor.GetComponent<PlayerInputReader>();
            var character = motor.GetComponent<PlayerCharacter>();
            if (character != null) { character.enabled = false; }
            motor.enabled = false;

            yield return Pad(new GamepadState());
            // Switching the motor off drops held input; one neutral read lets it through again.
            _input.Consume();
            _input.ConsumeCombat();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_gamepad != null) { InputSystem.RemoveDevice(_gamepad); }
            InputSystem.settings = _previousSettings;
            Object.Destroy(_testSettings);
            Application.runInBackground = _wasRunningInBackground;
            yield return null;
        }

        [UnityTest]
        public IEnumerator LeftBumperAloneRaisesTheGuardAndFiresNoSkill()
        {
            yield return Pad(Buttons(GamepadButton.LeftShoulder));
            var input = _input.ConsumeCombat();
            Assert.That(input.IsBlockHeld, Is.True);
            Assert.That(input.PressedSkillSlot, Is.EqualTo(-1));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator LeftTriggerAndLeftBumperFireTheFirstSkillWithoutAGuard()
        {
            yield return Pad(WithTrigger());
            yield return Pad(WithTrigger(GamepadButton.LeftShoulder));
            var input = _input.ConsumeCombat();
            Assert.That(input.PressedSkillSlot, Is.EqualTo(0));
            Assert.That(input.IsBlockHeld, Is.False);

            // Letting go of LT first must not turn the skill press into a guard.
            yield return Pad(Buttons(GamepadButton.LeftShoulder));
            Assert.That(_input.ConsumeCombat().IsBlockHeld, Is.False);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator LeftTriggerAndRightBumperFireTheSecondSkillWithoutAHeavyAttack()
        {
            yield return Pad(WithTrigger());
            yield return Pad(WithTrigger(GamepadButton.RightShoulder));
            var input = _input.ConsumeCombat();
            Assert.That(input.PressedSkillSlot, Is.EqualTo(1));
            Assert.That(input.IsHeavyAttackPressed, Is.False);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator LeftTriggerAndFaceButtonsCastTheThreeSpellsWithoutAttacking()
        {
            yield return Pad(WithTrigger());
            var buttons = new[] { GamepadButton.West, GamepadButton.North, GamepadButton.East };
            for (var slot = 0; slot < buttons.Length; slot++)
            {
                yield return Pad(WithTrigger(buttons[slot]));
                var input = _input.ConsumeCombat();
                Assert.That(input.PressedSpellSlot, Is.EqualTo(slot), buttons[slot].ToString());
                Assert.That(input.IsLightAttackPressed, Is.False);
                Assert.That(input.IsInteractPressed, Is.False);
                yield return Pad(WithTrigger());
            }

            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator WithoutTheTriggerTheFaceButtonAttacksAndCastsNothing()
        {
            yield return Pad(Buttons(GamepadButton.West));
            var input = _input.ConsumeCombat();
            Assert.That(input.IsLightAttackPressed, Is.True);
            Assert.That(input.PressedSpellSlot, Is.EqualTo(-1));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator AQuickTapOfTheNorthButtonInteracts()
        {
            // One frame down, then up: no hold is needed to use an altar or a door.
            yield return Pad(Buttons(GamepadButton.North));
            yield return Pad(new GamepadState());
            Assert.That(_input.ConsumeCombat().IsInteractPressed, Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator AGuardAlreadyUpStaysUpWhenTheTriggerIsPulled()
        {
            yield return Pad(Buttons(GamepadButton.LeftShoulder));
            Assert.That(_input.ConsumeCombat().IsBlockHeld, Is.True);

            yield return Pad(WithTrigger(GamepadButton.LeftShoulder));
            var input = _input.ConsumeCombat();
            Assert.That(input.IsBlockHeld, Is.True);
            Assert.That(input.PressedSkillSlot, Is.EqualTo(-1));
            LogAssert.NoUnexpectedReceived();
        }

        private static GamepadState Buttons(params GamepadButton[] buttons)
        {
            var state = new GamepadState();
            foreach (var button in buttons)
            {
                state = state.WithButton(button);
            }

            return state;
        }

        private static GamepadState WithTrigger(params GamepadButton[] buttons)
        {
            var state = Buttons(buttons);
            state.leftTrigger = 1f;
            return state;
        }

        private IEnumerator Pad(GamepadState state)
        {
            InputSystem.QueueStateEvent(_gamepad, state);
            yield return null;
        }
    }
}
