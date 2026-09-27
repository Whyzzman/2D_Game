using System;
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
    /// The animation layer in the running level. Two questions run through all of it: does the
    /// visual follow the gameplay state rather than lead it, and can a more important state ever be
    /// hidden by a less important one. The second is what produces bugs players notice — dying
    /// while a swing keeps playing, or being stunned but still looking like you are running.
    /// </summary>
    public sealed class PlayerAnimationTests
    {
        private Keyboard _keyboard;
        private PlayerCharacter _player;
        private PlayerAnimationController _animation;
        private PlayerMotor2D _motor;
        private PlayerCombat _combat;
        private PlayerEquipment _equipment;
        private GameProgress _progress;
        private SpriteRenderer _body;
        private InputSettings _previousSettings;
        private InputSettings _testSettings;
        private bool _wasRunningInBackground;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _previousSettings = InputSystem.settings;
            if (_previousSettings.hideFlags == HideFlags.HideAndDontSave)
            {
                _previousSettings = UnityEngine.Object.Instantiate(_previousSettings);
            }
            _testSettings = UnityEngine.Object.Instantiate(_previousSettings);
            _testSettings.hideFlags = HideFlags.HideAndDontSave;
            _testSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            _testSettings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings = _testSettings;
            _wasRunningInBackground = Application.runInBackground;
            Application.runInBackground = true;
            _keyboard = InputSystem.AddDevice<Keyboard>("AnimationTestKeyboard");

            SaveSystem.Delete();
            yield return SceneManager.LoadSceneAsync("TombTrial");
            _player = UnityEngine.Object.FindAnyObjectByType<PlayerCharacter>();
            _animation = _player.GetComponent<PlayerAnimationController>();
            _motor = _player.GetComponent<PlayerMotor2D>();
            _combat = _player.GetComponent<PlayerCombat>();
            _equipment = _player.GetComponent<PlayerEquipment>();
            _progress = UnityEngine.Object.FindAnyObjectByType<GameProgress>();
            _body = _player.transform.Find("Visual/Body").GetComponent<SpriteRenderer>();
            yield return Neutral(4);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_animation != null) { _animation.ForcedState = null; }
            if (_keyboard != null) { InputSystem.RemoveDevice(_keyboard); }
            InputSystem.settings = _previousSettings;
            UnityEngine.Object.Destroy(_testSettings);
            Application.runInBackground = _wasRunningInBackground;
            GamePause.Clear();
            Time.timeScale = 1f;
            SaveSystem.Delete();
            yield return null;
        }

        // ---------- wiring ----------

        [UnityTest]
        public IEnumerator ThePlayerShipsWithAnAnimationControllerAndSockets()
        {
            var sockets = _player.GetComponentInChildren<VisualSockets>();

            Assert.That(_animation, Is.Not.Null);
            Assert.That(_animation.Clip, Is.Not.Null);
            Assert.That(sockets, Is.Not.Null);
            Assert.That(sockets.IsComplete, Is.True,
                "Every effect socket is wired, or effects would silently spawn on the root.");
            yield break;
        }

        [UnityTest]
        public IEnumerator TheBodyShowsAFrameRatherThanAPlaceholderBox()
        {
            yield return null;

            Assert.That(_body.sprite, Is.Not.Null);
            Assert.That(_body.transform.localScale, Is.EqualTo(Vector3.one),
                "Frames carry their own size through pixels-per-unit; scaling would distort them.");
        }

        [UnityTest]
        public IEnumerator TheGroundSocketSitsOnTheFloorLine()
        {
            var sockets = _player.GetComponentInChildren<VisualSockets>();
            var feet = _player.transform.position.y - 0.8f;

            Assert.That(sockets.Position(VisualSocket.Ground).y, Is.EqualTo(feet).Within(0.05f));
            yield break;
        }

        // ---------- following gameplay ----------

        [UnityTest]
        public IEnumerator StandingStillLooksLikeStandingStill()
        {
            yield return Neutral(6);

            Assert.That(_animation.State, Is.EqualTo(PlayerAnimationState.Idle));
        }

        [UnityTest]
        public IEnumerator RunningLooksLikeRunning()
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.D));
            yield return WaitForSecondsRealtime(0.3f);

            Assert.That(_animation.State, Is.EqualTo(PlayerAnimationState.Run));
            yield return Neutral(2);
        }

        [UnityTest]
        public IEnumerator DeathOutranksEverythingElse()
        {
            // The worst version of this bug: killed mid-swing, and the swing keeps playing.
            _player.Actor.ApplyDamage(new DamageInfo(9999f, Vector2.right));
            yield return new WaitForFixedUpdate();
            yield return null;

            Assume.That(_player.Action, Is.EqualTo(PlayerAction.Dead));
            Assert.That(_animation.State, Is.EqualTo(PlayerAnimationState.Death));
        }

        [UnityTest]
        public IEnumerator DeathHoldsItsLastFrameRatherThanDisappearing()
        {
            _player.Actor.ApplyDamage(new DamageInfo(9999f, Vector2.right));
            yield return new WaitForFixedUpdate();
            yield return null;
            var clip = _animation.Clip;
            Assume.That(clip.IsLooping, Is.False);
            yield return WaitForSecondsRealtime(clip.Duration + 0.2f);

            Assert.That(_body.sprite, Is.SameAs(clip.Frames[clip.FrameCount - 1]));
        }

        [UnityTest]
        public IEnumerator BeingHurtOutranksMoving()
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.D));
            yield return WaitForSecondsRealtime(0.25f);
            Assume.That(_animation.State, Is.EqualTo(PlayerAnimationState.Run));

            _player.Actor.ApplyDamage(new DamageInfo(15f, Vector2.right, knockback: 4f));
            yield return new WaitForFixedUpdate();
            yield return null;

            Assume.That(_player.Action, Is.EqualTo(PlayerAction.Hurt));
            Assert.That(_animation.State, Is.EqualTo(PlayerAnimationState.Hurt));
            yield return Neutral(2);
        }

        [UnityTest]
        public IEnumerator BeingStunnedOutranksBeingHurt()
        {
            _player.GetComponent<StaggerMeter>().ForceStun(0.8f);
            yield return new WaitForFixedUpdate();
            yield return null;

            Assume.That(_player.Action, Is.EqualTo(PlayerAction.Stunned));
            Assert.That(_animation.State, Is.EqualTo(PlayerAnimationState.Stunned));
        }

        [UnityTest]
        public IEnumerator FallingLooksLikeFalling()
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space));
            yield return WaitForSecondsRealtime(0.08f);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());

            // Past the apex, on the way down. Polled against the clock rather than a frame count:
            // batchmode runs frames far faster than real time, so counting frames would give up
            // before the jump has even reached its peak.
            var falling = false;
            var deadline = Time.realtimeSinceStartup + 4f;
            while (!falling && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
                falling = _motor.State == MovementState.Fall;
            }

            Assume.That(falling, Is.True);
            // The animation is written in LateUpdate, so it catches up on the next frame. Following
            // gameplay by a frame is the design; leading it would be the bug.
            yield return null;
            Assert.That(_animation.State, Is.EqualTo(PlayerAnimationState.Fall));
        }

        [UnityTest]
        public IEnumerator ALandingOnTheRunGoesStraightIntoTheRun()
        {
            var landings = 0;
            var speedAtLanding = 0f;
            var landedAt = -1f;
            _animation.Landed += () =>
            {
                landings++;
                speedAtLanding = Mathf.Abs(_motor.Velocity.x);
                landedAt = Time.realtimeSinceStartup;
            };

            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.D));
            yield return WaitForSecondsRealtime(0.25f);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.D, Key.Space));
            yield return WaitForSecondsRealtime(0.08f);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.D));

            var sawLand = false;
            var deadline = Time.realtimeSinceStartup + 3f;
            while (Time.realtimeSinceStartup < deadline
                   && (landedAt < 0f || Time.realtimeSinceStartup < landedAt + 0.3f))
            {
                sawLand |= _animation.State == PlayerAnimationState.Land;
                yield return null;
            }

            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            Assert.That(landings, Is.GreaterThanOrEqualTo(1), "The landing is still announced, for its sound.");
            Assume.That(speedAtLanding, Is.GreaterThan(0.5f), "Landed on the run, not against a wall.");
            Assert.That(sawLand, Is.False, "A running landing must not slide along in the Land pose.");
        }

        [UnityTest]
        public IEnumerator TheRunClipIsPacedByTheRealGroundSpeed()
        {
            var pad = InputSystem.AddDevice<Gamepad>("AnimationTestGamepad");
            try
            {
                InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = new Vector2(0.5f, 0f) });
                yield return WaitForSecondsRealtime(0.3f);
                Assume.That(_animation.State, Is.EqualTo(PlayerAnimationState.Run));
                var share = Mathf.Abs(_motor.Velocity.x) / _motor.RunSpeed;
                Assume.That(share, Is.LessThan(0.8f), "A half-tilted stick has to move slower.");
                Assert.That(_animation.PlaybackSpeed, Is.EqualTo(share).Within(0.05f),
                    "The stride follows the ground speed, so the feet do not slide.");
                InputSystem.QueueStateEvent(pad, new GamepadState());
                yield return null;
            }
            finally
            {
                InputSystem.RemoveDevice(pad);
            }
        }

        [UnityTest]
        public IEnumerator LandingIsShownBeforeLocomotionResumes()
        {
            // A real jump rather than a teleport: teleporting upwards drops the player away from
            // the platform it was standing on, and it never comes back down onto anything.
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space));
            yield return WaitForSecondsRealtime(0.08f);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return WaitForSecondsRealtime(0.15f);
            Assume.That(_motor.IsGrounded, Is.False, "The jump has to leave the ground.");

            var seen = new System.Collections.Generic.List<PlayerAnimationState>();
            var landed = false;
            var deadline = Time.realtimeSinceStartup + 4f;
            while (!landed && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
                if (seen.Count == 0 || seen[seen.Count - 1] != _animation.State)
                {
                    seen.Add(_animation.State);
                }

                landed = _animation.State == PlayerAnimationState.Land;
            }

            Assert.That(landed, Is.True,
                "A landing that is never shown makes the fall feel weightless. Seen: "
                + string.Join(" -> ", seen));
        }

        // ---------- timing stays with gameplay ----------

        [UnityTest]
        public IEnumerator AnAttackClipIsFittedToTheRealSwing()
        {
            yield return PressLight();

            var attack = _combat.CurrentAttack;
            Assume.That(attack, Is.Not.Null);
            Assert.That(_animation.State, Is.EqualTo(PlayerAnimationState.Attack1));
            // The clip is stretched to the attack, never the other way round: retuning a weapon
            // must not be able to desynchronise the art from the active window.
            var expected = _animation.Clip.Duration / attack.Duration;
            Assert.That(_animation.PlaybackSpeed, Is.EqualTo(expected).Within(0.001f));
            yield return Neutral(2);
        }

        [UnityTest]
        public IEnumerator TheComboChainDrivesWhichSwingIsShown()
        {
            yield return PressLight();
            Assert.That(_animation.State, Is.EqualTo(PlayerAnimationState.Attack1));

            yield return WaitForSecondsRealtime(0.24f);
            yield return PressLight();

            Assert.That(_animation.State, Is.EqualTo(PlayerAnimationState.Attack2),
                "The art follows combat's own combo counter rather than keeping a second one.");
            yield return Neutral(2);
        }

        [UnityTest]
        public IEnumerator AHeavySwingHasItsOwnAnimation()
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.K));
            yield return WaitForSecondsRealtime(0.05f);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return null;

            Assume.That(_player.Action, Is.EqualTo(PlayerAction.HeavyAttack));
            Assert.That(_animation.State, Is.EqualTo(PlayerAnimationState.Heavy));
            yield return Neutral(2);
        }

        // ---------- weapons ----------

        [UnityTest]
        public IEnumerator SwitchingToTheGreatswordSwitchesTheSwings()
        {
            Assume.That(_animation.ActiveWeaponSet, Is.Null,
                "The starting sword uses the shared swings.");
            _progress.AddItem("iron_greatsword");
            // The greatsword has an attribute gate of its own; this test is about which clips play,
            // so the character is simply made strong enough to lift it.
            var progression = UnityEngine.Object.FindAnyObjectByType<PlayerProgressionService>();
            progression.SetStat(CharacterStat.Strength, 25);
            progression.SetStat(CharacterStat.Dexterity, 25);

            Assume.That(_equipment.Equip("iron_greatsword", EquipmentSlot.MainWeapon),
                Is.EqualTo(EquipResult.Equipped));
            yield return null;

            Assert.That(_animation.ActiveWeaponSet, Is.Not.Null);
            Assert.That(_animation.ActiveWeaponSet.WeaponType, Is.EqualTo(WeaponType.Greatsword));
        }

        // ---------- secondary motion (item 2, 2026-09-27) ----------

        [UnityTest]
        public IEnumerator TurningOnTheGroundIsInstantAndDressed()
        {
            yield return Neutral(6);
            Assert.That(_motor.FacingDirection, Is.EqualTo(1));

            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.A));
            var sawTurn = false;
            var until = Time.realtimeSinceStartup + 0.3f;
            while (Time.realtimeSinceStartup < until)
            {
                yield return null;
                sawTurn |= _animation.State == PlayerAnimationState.Turn;
            }

            Assert.That(_motor.FacingDirection, Is.EqualTo(-1), "The turn itself is not held up.");
            Assert.That(sawTurn, Is.True, "The facing flip on the ground shows the turn pose.");
            Assert.That(_animation.State, Is.EqualTo(PlayerAnimationState.Run),
                "Two frames, then the run: the pose never outstays the turn.");
            yield return Neutral(2);
        }

        [UnityTest]
        public IEnumerator StandingStillLongEnoughBringsTheIdleVariantOnce()
        {
            yield return Neutral(6);
            Assert.That(_animation.State, Is.EqualTo(PlayerAnimationState.Idle));

            _animation.Tick(PlayerAnimationController.IdleVariantDelay + 0.1f);
            _animation.Tick(0.01f);
            Assert.That(_animation.State, Is.EqualTo(PlayerAnimationState.IdleVariant));

            _animation.Tick(_animation.Clip.Duration + 0.05f);
            _animation.Tick(0.01f);
            Assert.That(_animation.State, Is.EqualTo(PlayerAnimationState.Idle),
                "It plays once and hands back to the stance.");

            _animation.Tick(PlayerAnimationController.IdleVariantGapMin - 0.5f);
            _animation.Tick(0.01f);
            Assert.That(_animation.State, Is.EqualTo(PlayerAnimationState.Idle),
                "The next one waits at least eight seconds.");
        }

        [UnityTest]
        public IEnumerator MovingCutsTheIdleVariantShortAndStartsTheWaitOver()
        {
            yield return Neutral(6);
            _animation.Tick(PlayerAnimationController.IdleVariantDelay + 0.1f);
            _animation.Tick(0.01f);
            Assert.That(_animation.State, Is.EqualTo(PlayerAnimationState.IdleVariant));

            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.D));
            yield return WaitForSecondsRealtime(0.25f);
            Assert.That(_animation.State, Is.EqualTo(PlayerAnimationState.Run));

            yield return Neutral(6);
            _animation.Tick(0.01f);
            Assert.That(_animation.State, Is.EqualTo(PlayerAnimationState.Idle),
                "Stopping again stands still; the flourish does not resume where it left off.");
        }

        [UnityTest]
        public IEnumerator TheCloaksEdgeHangsAtRestAndTrailsARun()
        {
            var tail = _player.GetComponentInChildren<CloakTail>();
            Assert.That(tail, Is.Not.Null, "The player ships with the cloak's live edge.");
            var line = tail.GetComponent<LineRenderer>();
            Assert.That(line.sortingLayerID, Is.EqualTo(_body.sortingLayerID));
            Assert.That(line.sortingOrder, Is.LessThan(_body.sortingOrder),
                "It comes out from under the drawn cloak, behind the body.");

            yield return Neutral(6);
            yield return WaitForSecondsRealtime(0.6f);
            var hanging = tail.Tip - tail.Root;
            Assert.That(hanging.y, Is.LessThan(-0.15f), "At rest the edge hangs down.");
            Assert.That(Vector2.Angle(hanging, tail.RestDirection), Is.LessThan(20f),
                "... along the line of the cloak's back edge.");

            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.D));
            yield return WaitForSecondsRealtime(0.6f);
            var running = tail.Tip - tail.Root;
            Assert.That(running.x, Is.LessThan(hanging.x - 0.06f),
                "Running right, the edge streams back to the left.");
            Assert.That(running.magnitude, Is.EqualTo(hanging.magnitude).Within(0.02f),
                "Cloth trails; it does not stretch.");
            yield return Neutral(2);
        }

        [UnityTest]
        public IEnumerator TheCloaksEdgeSwingsRoundAfterATurnRatherThanJumping()
        {
            var tail = _player.GetComponentInChildren<CloakTail>();
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.D));
            yield return WaitForSecondsRealtime(0.5f);

            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.A));
            var until = Time.realtimeSinceStartup + 0.3f;
            while (_motor.FacingDirection != -1 && Time.realtimeSinceStartup < until)
            {
                yield return null;
            }

            yield return null;
            // Facing left now; the back is to the right. The edge is still where the run left it.
            Assert.That(tail.Tip.x - tail.Root.x, Is.LessThan(0f),
                "Just after the turn the edge is still on the old side, in front.");

            yield return WaitForSecondsRealtime(0.5f);
            Assert.That(tail.Tip.x - tail.Root.x, Is.GreaterThan(0.05f),
                "... and it has swung round to trail the new run.");
            yield return Neutral(2);
        }

        // ---------- safety ----------

        [UnityTest]
        public IEnumerator EveryStateCanBePlayedWithoutBreakingAnything()
        {
            // The debug tools can pin any state. None may throw, leave a null sprite, or touch
            // gameplay — a debug key that changes the game is worse than no debug key.
            var before = _player.Action;
            foreach (PlayerAnimationState state in Enum.GetValues(typeof(PlayerAnimationState)))
            {
                _animation.ForcedState = state;
                yield return null;

                Assert.That(_animation.State, Is.EqualTo(state));
                Assert.That(_body.sprite, Is.Not.Null, $"{state} left the character invisible.");
            }

            _animation.ForcedState = null;
            Assert.That(_player.Action, Is.EqualTo(before), "Animation never drives gameplay.");
        }

        [UnityTest]
        public IEnumerator AControllerWithNoAnimationSetStillRuns()
        {
            var bare = new GameObject("BareAnimation");
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(
                "no animation set"));
            try
            {
                var controller = bare.AddComponent<PlayerAnimationController>();
                yield return null;
                controller.Tick(0.1f);

                Assert.That(controller.State, Is.EqualTo(PlayerAnimationState.Idle),
                    "Missing art is a visual gap, never a crash.");
            }
            finally
            {
                UnityEngine.Object.Destroy(bare);
            }
        }

        // ---------- helpers ----------

        private IEnumerator PressLight()
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.J));
            yield return WaitForSecondsRealtime(0.05f);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return null;
        }

        private IEnumerator Neutral(int frames)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return null;
            for (var i = 0; i < frames; i++)
            {
                yield return new WaitForFixedUpdate();
            }
            yield return null;
        }

        private static IEnumerator WaitForSecondsRealtime(float seconds)
        {
            var until = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < until)
            {
                yield return null;
            }
        }
    }
}
