using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    public sealed class CoreMovementTests
    {
        private Keyboard _keyboard;
        private Gamepad _gamepad;
        private PlayerMotor2D _player;
        private PlayerInputReader _input;
        private Rigidbody2D _body;
        private InputSettings _previousSettings;
        private InputSettings _testSettings;
        private bool _wasRunningInBackground;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _previousSettings = InputSystem.settings;
            // Input System destroys its temporary defaults when settings are replaced.
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
            _keyboard = InputSystem.AddDevice<Keyboard>("MovementTestKeyboard");
            _gamepad = InputSystem.AddDevice<Gamepad>("MovementTestGamepad");
            yield return SceneManager.LoadSceneAsync("CoreMovement");
            _player = Object.FindAnyObjectByType<PlayerMotor2D>();
            Assert.That(_player, Is.Not.Null, "Saved scene must contain the Player prefab.");
            _input = _player.GetComponent<PlayerInputReader>();
            _body = _player.GetComponent<Rigidbody2D>();
            yield return NeutralFrames(5);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_keyboard != null) { InputSystem.RemoveDevice(_keyboard); }
            if (_gamepad != null) { InputSystem.RemoveDevice(_gamepad); }
            InputSystem.settings = _previousSettings;
            Object.Destroy(_testSettings);
            Application.runInBackground = _wasRunningInBackground;
            yield return null;
        }

        [UnityTest]
        public IEnumerator SavedSceneHasWiringAndStableGroundedPlayer()
        {
            Assert.That(_player.IsGrounded, Is.True);
            Assert.That(_body.gravityScale, Is.Zero);
            Assert.That(_body.constraints & RigidbodyConstraints2D.FreezeRotation, Is.Not.Zero);
            Assert.That(_body.collisionDetectionMode, Is.EqualTo(CollisionDetectionMode2D.Continuous));
            Assert.That(_player.GetComponent<CapsuleCollider2D>().sharedMaterial.friction, Is.Zero);
            Assert.That(Object.FindAnyObjectByType<CameraFollow2D>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<ReturnToStart>(), Is.Not.Null);
            var start = _body.position;
            yield return Frames(20);
            Assert.That(Vector2.Distance(start, _body.position), Is.LessThan(0.03f));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator KeyboardRunReleaseAndReenableWork()
        {
            // No key is held to run: the keyboard always moves at the running pace.
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.D));
            yield return Frames(15);
            Assert.That(_body.linearVelocity.x, Is.EqualTo(6.5f).Within(0.05f));
            yield return NeutralFrames(5);
            Assert.That(Mathf.Abs(_body.linearVelocity.x), Is.LessThan(0.05f));
            _input.enabled = false;
            _input.enabled = true;
            yield return NeutralFrames(3);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.A));
            yield return Frames(10);
            Assert.That(_body.linearVelocity.x, Is.LessThan(-6.4f));
            Assert.That(_player.FacingDirection, Is.EqualTo(-1));
        }

        [UnityTest]
        public IEnumerator VirtualGamepadRunAndJumpUseExistingBindings()
        {
            InputSystem.QueueStateEvent(_gamepad, new GamepadState { leftStick = Vector2.right });
            yield return Frames(8);
            Assert.That(_body.linearVelocity.x, Is.EqualTo(6.5f).Within(0.05f));
            InputSystem.QueueStateEvent(_gamepad, new GamepadState().WithButton(GamepadButton.South));
            yield return Frames(3);
            Assert.That(_body.linearVelocity.y, Is.GreaterThan(0f));
            Assert.That(_player.State, Is.EqualTo(MovementState.Jump));
            yield return NeutralFrames(45);
            Assert.That(_player.IsGrounded, Is.True);
        }

        [UnityTest]
        public IEnumerator HeldJumpDoesNotRepeatAndShortJumpIsLower()
        {
            var startY = _body.position.y;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space));
            var fullPeak = startY;
            for (var i = 0; i < 55; i++)
            {
                yield return Frames(1);
                fullPeak = Mathf.Max(fullPeak, _body.position.y);
            }
            Assert.That(_player.IsGrounded, Is.True);
            Assert.That(fullPeak - startY, Is.GreaterThan(1.7f));
            yield return NeutralFrames(3);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space));
            yield return Frames(2);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            var shortPeak = _body.position.y;
            for (var i = 0; i < 45; i++)
            {
                yield return Frames(1);
                shortPeak = Mathf.Max(shortPeak, _body.position.y);
            }
            Assert.That(shortPeak - startY, Is.LessThan((fullPeak - startY) * 0.7f));
        }

        [UnityTest]
        public IEnumerator CeilingAndWallDoNotBecomeGroundOrTrapPlayer()
        {
            _player.ResetTo(new Vector2(17.5f, 0.82f));
            yield return NeutralFrames(5);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space));
            var peak = _body.position.y;
            for (var i = 0; i < 40; i++)
            {
                yield return Frames(1);
                peak = Mathf.Max(peak, _body.position.y);
            }
            Assert.That(peak, Is.LessThan(1.65f), "Head must not pass through the low ceiling.");
            Assert.That(_player.IsGrounded, Is.True);

            _player.ResetTo(new Vector2(21f, 2f));
            yield return NeutralFrames(2);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.D));
            yield return Frames(4);
            Assert.That(_player.IsGrounded, Is.False, "A wall contact must not allow jumping.");
            yield return Frames(11);
            Assert.That(_body.position.x, Is.LessThan(21.3f));
            Assert.That(_body.position.y, Is.LessThan(3f), "No friction sticking to a wall.");
            yield return NeutralFrames(30);
            Assert.That(_player.IsGrounded, Is.True);
        }

        [UnityTest]
        public IEnumerator FallsLandOnPlatformsWithoutTunneling()
        {
            _player.ResetTo(new Vector2(4f, 8f));
            yield return NeutralFrames(60);
            Assert.That(_player.IsGrounded, Is.True);
            Assert.That(_body.position.y, Is.EqualTo(2.1f).Within(0.12f));
        }

        [UnityTest]
        public IEnumerator CameraFollowsSmoothlyAndReturnClearsMotionAndJump()
        {
            var camera = Camera.main;
            var startCamera = camera.transform.position;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.D));
            yield return Frames(10);
            Assert.That(camera.transform.position.x, Is.GreaterThan(startCamera.x));
            Assert.That(camera.transform.position.x, Is.LessThan(_player.transform.position.x));
            yield return NeutralFrames(20);
            Assert.That(Mathf.Abs(camera.transform.position.x - _player.transform.position.x),
                Is.LessThan(0.15f));

            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space, Key.D));
            _body.position = new Vector2(9f, -9f);
            yield return Frames(4);
            Assert.That(_body.position.x, Is.EqualTo(0f).Within(0.1f));
            Assert.That(_body.position.y, Is.GreaterThan(0.7f));
            Assert.That(Mathf.Abs(_body.linearVelocity.x), Is.LessThan(0.01f));
            Assert.That(_body.linearVelocity.y, Is.LessThanOrEqualTo(0.01f));
            Assert.That(Vector2.Distance(camera.transform.position, _player.transform.position),
                Is.LessThan(0.15f));
            yield return NeutralFrames(3);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space));
            yield return Frames(2);
            Assert.That(_body.linearVelocity.y, Is.GreaterThan(0f));
        }

        [UnityTest]
        public IEnumerator FocusLossAndComponentResetDiscardOldPress()
        {
            _player.enabled = false;
            _input.enabled = false;
            _input.enabled = true;
            yield return NeutralFrames(2);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space, Key.D));
            yield return null;
            _input.SendMessage("OnApplicationFocus", false);
            Assert.That(_input.Consume().IsJumpPressed, Is.False);
            Assert.That(_input.Consume().Horizontal, Is.Zero);
            _input.SendMessage("OnApplicationFocus", true);
            Assert.That(_input.Consume().IsJumpPressed, Is.False);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return null;
            _input.Consume();
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space));
            yield return null;
            Assert.That(_input.Consume().IsJumpPressed, Is.True);
            Assert.That(_input.Consume().IsJumpPressed, Is.False);
            _player.enabled = true;
        }

        [UnityTest]
        public IEnumerator AHeldDirectionMovesOnAtOnceWhenControlComesBack()
        {
            _player.enabled = false;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.D, Key.Space));
            yield return null;
            // Closing a menu hands control back with the keys still held.
            _input.SetGameplayEnabled(false);
            _input.SetGameplayEnabled(true);

            var input = _input.Consume();
            Assert.That(input.Horizontal, Is.EqualTo(1f), "The held direction must not be ignored.");
            Assert.That(input.IsJumpPressed, Is.False, "A held jump must not fire a new one.");
            Assert.That(input.IsJumpHeld, Is.False, "Nor extend a jump it did not start.");

            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.D));
            yield return null;
            _input.Consume();
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.D, Key.Space));
            yield return null;
            Assert.That(_input.Consume().IsJumpPressed, Is.True, "Released once, jump works again.");
            yield return NeutralFrames(2);
            _player.enabled = true;
        }

        [UnityTest]
        public IEnumerator FullCourseIsTraversableWithMappedInput()
        {
            yield return CaptureFrame("start");
            yield return RunTo(1.7f);
            yield return JumpTo(4f, 1.3f);
            yield return JumpTo(7f, 2.7f);
            yield return JumpTo(10f, 4.1f);
            yield return CaptureFrame("platforms");
            yield return RunTo(20.1f);
            yield return NeutralFrames(12);
            Assert.That(_player.IsGrounded, Is.True);
            yield return JumpTo(22f, 1.6f);
            yield return RunTo(28f);
            yield return NeutralFrames(15);
            Assert.That(_body.position.x, Is.GreaterThan(27.5f));
            Assert.That(_body.position.x, Is.LessThan(30f));
            Assert.That(_player.IsGrounded, Is.True);
            yield return CaptureFrame("finish");
        }

        private static IEnumerator CaptureFrame(string name)
        {
            var directory = System.Environment.GetEnvironmentVariable("PHASE1_CAPTURE_DIR");
            if (string.IsNullOrEmpty(directory))
            {
                yield break;
            }
            System.IO.Directory.CreateDirectory(directory);
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(directory, name + ".png"));
            yield return Frames(5);
        }

        private IEnumerator RunTo(float x)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.D));
            for (var i = 0; i < 300 && _body.position.x < x; i++)
            {
                yield return Frames(1);
            }
            Assert.That(_body.position.x, Is.GreaterThanOrEqualTo(x), "Could not run to " + x);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return Frames(1);
        }

        private IEnumerator JumpTo(float x, float surfaceY)
        {
            yield return NeutralFrames(3);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.D, Key.Space));
            var hasLeftGround = false;
            var braking = false;
            for (var i = 0; i < 100; i++)
            {
                yield return Frames(1);
                hasLeftGround |= !_player.IsGrounded;
                if (!braking && _body.position.x >= x - 0.85f)
                {
                    InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space));
                    braking = true;
                }
                if (hasLeftGround && _player.IsGrounded)
                {
                    break;
                }
            }
            yield return NeutralFrames(5);
            Assert.That(hasLeftGround, Is.True);
            Assert.That(_player.IsGrounded, Is.True);
            Assert.That(_body.position.y, Is.EqualTo(surfaceY + 0.8f).Within(0.13f),
                "Could not land on the platform at " + x);
        }

        [UnityTest]
        public IEnumerator AJumpThatClipsALedgeCornerSlidesPastIt()
        {
            yield return NeutralFrames(5);
            var start = _body.position;
            // The ledge's left edge overlaps the right of the head by 0.15, inside the correction.
            var ledge = CeilingLedge(start, leftEdgeOffset: 0.25f);
            var (peak, endX) = default((float, float));
            yield return JumpAndMeasure(start, result => (peak, endX) = result);
            Object.Destroy(ledge);

            Assert.That(peak, Is.GreaterThan(1.4f), "The jump died on the corner.");
            Assert.That(endX, Is.LessThan(start.x - 0.1f), "Nudged away from the ledge.");
            Assert.That(endX, Is.GreaterThan(start.x - 0.21f), "No more than the correction.");
        }

        [UnityTest]
        public IEnumerator AJumpIntoARealCeilingIsStillStopped()
        {
            yield return NeutralFrames(5);
            var start = _body.position;
            // Overlapping by 0.35, past the 0.2 correction: this is a ceiling, not a corner.
            var ledge = CeilingLedge(start, leftEdgeOffset: 0.05f);
            var peak = 0f;
            yield return JumpAndMeasure(start, result => peak = result.Item1);
            Object.Destroy(ledge);

            Assert.That(peak, Is.LessThan(0.7f));
        }

        [UnityTest]
        public IEnumerator AFallThatMissesALedgeCornerByAHairLandsOnIt()
        {
            yield return NeutralFrames(5);
            var floor = _body.position.y - 0.8f;
            // The body's side starts 0.05 short of the ledge and the feet a hair over its top:
            // steering held toward it closes ~0.04 before they cross, never enough to stand on the
            // corner unaided. The nudge needs the steering held, so it is held through the fall.
            yield return FallBesideLedge(floor, gap: 0.05f, above: 0.02f, steerFrames: 8);
            Assert.That(_player.IsGrounded, Is.True);
            Assert.That(_body.position.y - 0.8f, Is.EqualTo(floor + LedgeHeight).Within(0.05f),
                "Nudged onto the ledge instead of scraping down its face.");
        }

        [UnityTest]
        public IEnumerator AFallThatClearlyMissesALedgeStillFallsPastIt()
        {
            yield return NeutralFrames(5);
            var floor = _body.position.y - 0.8f;
            yield return FallBesideLedge(floor, gap: 0.6f, above: 0.3f, steerFrames: 4);
            Assert.That(_body.position.y - 0.8f, Is.EqualTo(floor).Within(0.05f),
                "Past the forgiveness the fall is an honest miss.");
        }

        private const float LedgeHeight = 1f;

        /// <summary>
        /// Drops the player, steering right for <paramref name="steerFrames"/>, with the feet
        /// <paramref name="above"/> over the top of a ledge whose left face is <paramref name="gap"/>
        /// past the body's right side.
        /// </summary>
        private IEnumerator FallBesideLedge(float floor, float gap, float above, int steerFrames)
        {
            var x = _body.position.x;
            // Layer 9 is Ground.
            var ledge = new GameObject("ForgivenessLedge") { layer = 9 };
            var box = ledge.AddComponent<BoxCollider2D>();
            box.size = new Vector2(3f, LedgeHeight);
            ledge.transform.position = new Vector3(x + 0.4f + gap + 1.5f, floor + LedgeHeight * 0.5f, 0f);
            Physics2D.SyncTransforms();

            _player.ResetTo(new Vector2(x, floor + LedgeHeight + above + 0.8f));
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.D));
            yield return Frames(steerFrames);
            yield return NeutralFrames(40);
            Object.Destroy(ledge);
        }

        /// <summary>A ledge whose underside sits 0.5 above the standing player's head.</summary>
        private static GameObject CeilingLedge(Vector2 start, float leftEdgeOffset)
        {
            // Layer 9 is Ground; the project's layers are used by index and have no names.
            var ledge = new GameObject("CornerLedge") { layer = 9 };
            var box = ledge.AddComponent<BoxCollider2D>();
            box.size = new Vector2(3f, 0.5f);
            ledge.transform.position = new Vector3(start.x + leftEdgeOffset + 1.5f,
                start.y + 0.8f + 0.5f + 0.25f, 0f);
            Physics2D.SyncTransforms();
            return ledge;
        }

        private IEnumerator JumpAndMeasure(Vector2 start, System.Action<(float, float)> report)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space));
            var peak = 0f;
            for (var i = 0; i < 45; i++)
            {
                yield return Frames(1);
                peak = Mathf.Max(peak, _body.position.y - start.y);
            }

            report((peak, _body.position.x));
            yield return NeutralFrames(40);
        }

        private IEnumerator NeutralFrames(int count)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            InputSystem.QueueStateEvent(_gamepad, new GamepadState());
            yield return Frames(count);
        }

        private static IEnumerator Frames(int count)
        {
            // An input update must precede simulation; then wait for actual physics steps.
            yield return null;
            for (var i = 0; i < count; i++)
            {
                yield return new WaitForFixedUpdate();
            }
            yield return null;
        }
    }
}
