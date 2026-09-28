using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The grapple in play (item 2 of the big list, 2026-09-28): nothing without the ability; with
    /// it, its own key throws the line to the ring in front and the hero is pulled up to it and
    /// let go above it with the air moves back.
    /// </summary>
    public sealed class GrappleTests
    {
        private Keyboard _keyboard;
        private InputSettings _previousSettings;
        private InputSettings _testSettings;
        private PlayerCharacter _player;
        private PlayerMotor2D _motor;
        private PlayerGrapple _grapple;
        private GameProgress _progress;
        private GameObject _ring;

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
            _keyboard = InputSystem.AddDevice<Keyboard>("GrappleTestKeyboard");
            KeyBindings.ResetAll();
            SaveSystem.Delete();
            yield return TestWorld.Load();
            _player = Object.FindAnyObjectByType<PlayerCharacter>();
            _motor = _player.GetComponent<PlayerMotor2D>();
            _grapple = _player.GetComponent<PlayerGrapple>();
            _progress = Object.FindAnyObjectByType<GameProgress>();
            // Under the open sky by the forest's first altar, with nobody about.
            foreach (var enemy in Object.FindObjectsByType<EnemyController>())
            {
                enemy.gameObject.SetActive(false);
            }

            _motor.ResetTo(new Vector2(86f, 1f));
            yield return new WaitForSeconds(0.4f);
            _ring = new GameObject("TestRing");
            _ring.transform.position = (Vector2)_player.transform.position + new Vector2(3f * _motor.FacingDirection, 4f);
            _ring.AddComponent<GrappleRing>();
            yield return new WaitForSeconds(0.3f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_ring != null) { Object.Destroy(_ring); }
            if (_keyboard != null) { InputSystem.RemoveDevice(_keyboard); }
            InputSystem.settings = _previousSettings;
            Object.Destroy(_testSettings);
            SaveSystem.Delete();
            yield return null;
        }

        private IEnumerator Press(Key key)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(key));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return null;
        }

        [UnityTest]
        public IEnumerator WithoutTheAbilityTheKeyDoesNothing()
        {
            Assert.That(_progress.HasAbility(AbilityIds.Grapple), Is.False);
            var before = _player.transform.position;
            yield return Press(Key.G);
            yield return new WaitForSeconds(0.4f);
            Assert.That(_grapple.IsPulling, Is.False);
            Assert.That(_player.transform.position.y, Is.LessThan(before.y + 0.5f));
            Assert.That(_ring.GetComponent<GrappleRing>().IsTargeted, Is.False, "No glow for what cannot be used.");
        }

        [UnityTest]
        public IEnumerator TheKeyPullsTheHeroUpToTheRingAndLetsGoAboveIt()
        {
            _progress.UnlockAbility(AbilityIds.Grapple);
            yield return new WaitForSeconds(0.1f);
            Assert.That(_ring.GetComponent<GrappleRing>().IsTargeted, Is.True, "The ring in reach glows.");
            var latched = false;
            var released = false;
            _grapple.Latched += _ => latched = true;
            _grapple.Released += () => released = true;

            yield return Press(Key.G);
            Assert.That(latched, Is.True, "G throws the line.");
            var deadline = Time.realtimeSinceStartup + 3f;
            var highest = float.MinValue;
            while (Time.realtimeSinceStartup < deadline && !released)
            {
                highest = Mathf.Max(highest, _player.transform.position.y);
                yield return null;
            }

            Assert.That(released, Is.True, "Let go on arrival.");
            // The pop after letting go carries on up for a moment; its top is what counts.
            var popUntil = Time.realtimeSinceStartup + 0.5f;
            while (Time.realtimeSinceStartup < popUntil)
            {
                highest = Mathf.Max(highest, _player.transform.position.y);
                yield return null;
            }

            Assert.That(highest + 1.1f, Is.GreaterThan(_ring.transform.position.y),
                $"Carried up to the ring, hand over it at the top: feet reached {highest:0.00}.");
            Assert.That(_motor.IsAirJumpReady || !_motor.CanDoubleJump, Is.True, "The air jump is back.");
        }
    }
}
