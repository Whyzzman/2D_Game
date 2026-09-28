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
    /// Hero animation 2.0 in the level (2026-09-28): a dash leaves cold silhouettes that fade, the
    /// body leans into a run and back out of it, and a long drop stretches the body, lands on one
    /// knee — and the first step cuts the kneel, since it is visual only.
    /// </summary>
    public sealed class HeroMotionTests
    {
        private const int Ground = 1 << 9;
        private Keyboard _keyboard;
        private InputSettings _previousSettings;
        private InputSettings _testSettings;
        private bool _wasRunningInBackground;
        private PlayerCharacter _player;
        private PlayerMotor2D _motor;
        private PlayerAnimationController _animation;

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
            _keyboard = InputSystem.AddDevice<Keyboard>("HeroMotionTestKeyboard");

            SaveSystem.Delete();
            KeyBindings.ResetAll();
            FeedbackSettings.ResetToDefaults();
            yield return SceneManager.LoadSceneAsync("TombTrial");
            _player = Object.FindAnyObjectByType<PlayerCharacter>();
            _motor = _player.GetComponent<PlayerMotor2D>();
            _animation = _player.GetComponent<PlayerAnimationController>();
            yield return WaitUntil(() => _motor.IsGrounded, 2f);
            yield return Wait(0.3f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_keyboard != null) { InputSystem.RemoveDevice(_keyboard); }
            if (_previousSettings != null) { InputSystem.settings = _previousSettings; }
            if (_testSettings != null) { Object.Destroy(_testSettings); }
            Application.runInBackground = _wasRunningInBackground;
            FeedbackSettings.ResetToDefaults();
            GamePause.Clear();
            SaveSystem.Delete();
            yield return null;
        }

        [UnityTest]
        public IEnumerator ADashLeavesColdSilhouettesThatFade()
        {
            var afterimages = _player.GetComponent<DashAfterimages>();
            Assert.That(afterimages, Is.Not.Null, "The player prefab has no DashAfterimages.");

            // Leftwards, as the dust test does: the start has room that way.
            Hold(Key.A);
            yield return Wait(0.2f);
            Hold(Key.A, Key.L);
            var most = 0;
            var xs = new System.Collections.Generic.HashSet<int>();
            var cold = true;
            var until = Time.realtimeSinceStartup + 0.4f;
            while (Time.realtimeSinceStartup < until)
            {
                yield return null;
                most = Mathf.Max(most, afterimages.ActiveCount);
                foreach (var ghost in afterimages.Ghosts.Where(g => g.enabled))
                {
                    xs.Add(Mathf.RoundToInt(ghost.transform.position.x * 10f));
                    cold &= ghost.color.b > ghost.color.r && ghost.sprite != null;
                }
            }

            Hold();
            Assert.That(most, Is.GreaterThanOrEqualTo(2), "A dash leaves more than one silhouette.");
            Assert.That(xs.Count, Is.GreaterThanOrEqualTo(2), "They are left along the path, not on the hero.");
            Assert.That(cold, Is.True, "Cold silver, the hero's edge — not a dark smoke.");

            yield return Wait(0.6f);
            Assert.That(afterimages.ActiveCount, Is.Zero, "And they are gone within a moment.");
        }

        [UnityTest]
        public IEnumerator ARunLeansInWhenItStartsAndBackWhenItStops()
        {
            var lean = _player.GetComponent<HeroLean>();
            Assert.That(lean, Is.Not.Null, "The player prefab has no HeroLean.");

            Hold(Key.D);
            var forward = 0f;
            var until = Time.realtimeSinceStartup + 0.35f;
            while (Time.realtimeSinceStartup < until)
            {
                yield return null;
                forward = Mathf.Min(forward, lean.Angle);
            }

            Hold();
            var back = 0f;
            until = Time.realtimeSinceStartup + 0.35f;
            while (Time.realtimeSinceStartup < until)
            {
                yield return null;
                back = Mathf.Max(back, lean.Angle);
            }

            Assert.That(forward, Is.LessThan(-1f), "Starting a run tips the body forward.");
            Assert.That(back, Is.GreaterThan(1f), "Stopping leans it back.");
            Assert.That(Mathf.Max(-forward, back), Is.LessThanOrEqualTo(HeroLean.MaxLean * 1.5f), "A lean, not a fall.");

            yield return Wait(1f);
            Assert.That(Mathf.Abs(lean.Angle), Is.LessThan(0.3f), "And it stands upright again.");
        }

        [UnityTest]
        public IEnumerator ALongDropStretchesThenKneelsAndAStepCutsTheKneel()
        {
            var footwork = _player.GetComponent<MovementFeedback>();
            Assert.That(FindDrop(8f, out var top), Is.True, "No open drop near the start to fall down.");
            _motor.ResetTo(top);

            var stretch = 0f;
            var knelt = false;
            yield return WaitUntil(() =>
            {
                stretch = Mathf.Max(stretch, footwork.FallStretchNow);
                knelt |= _animation.State == PlayerAnimationState.HardLand;
                return knelt;
            }, 4f);

            Assert.That(stretch, Is.GreaterThan(0.01f), "A long fall stretches the body along its speed.");
            Assert.That(stretch, Is.LessThanOrEqualTo(MovementFeedback.FallStretch + 0.001f));
            Assert.That(knelt, Is.True, "A drop taller than any jump lands on one knee.");

            Hold(Key.D);
            yield return Wait(0.12f);
            Hold();
            Assert.That(_animation.State, Is.Not.EqualTo(PlayerAnimationState.HardLand),
                "Visual only: the first step ends the kneel.");
        }

        [UnityTest]
        public IEnumerator AnOrdinaryJumpStillLandsLightly()
        {
            var sawHard = false;
            var sawLand = false;
            Hold(Key.Space);
            yield return Wait(0.3f);
            Hold();
            yield return WaitUntil(() =>
            {
                sawHard |= _animation.State == PlayerAnimationState.HardLand;
                sawLand |= _animation.State == PlayerAnimationState.Land;
                return sawLand;
            }, 3f);

            Assert.That(sawLand, Is.True);
            Assert.That(sawHard, Is.False, "A full jump is no fall to kneel from.");
        }

        // ---------- helpers ----------

        /// <summary>
        /// A spot near the start with open air <paramref name="height"/> above solid ground, so the
        /// hero can be dropped from there onto it.
        /// </summary>
        private bool FindDrop(float height, out Vector2 top)
        {
            var start = (Vector2)_player.transform.position;
            for (var step = 0; step <= 60; step++)
            {
                var x = start.x + (step % 2 == 0 ? 1f : -1f) * (step / 2) * 1.5f;
                var floor = Physics2D.Raycast(new Vector2(x, start.y + 0.5f), Vector2.down, 6f, Ground);
                if (!floor || floor.distance < 0.05f)
                {
                    continue;
                }

                top = new Vector2(x, floor.point.y + height);
                var fall = Physics2D.BoxCast(top, new Vector2(0.9f, 1.7f), 0f, Vector2.down, height + 2f, Ground);
                if (fall && fall.distance > 0.01f && Mathf.Abs(fall.point.y - floor.point.y) < 0.3f
                    && !Physics2D.OverlapBox(top, new Vector2(0.9f, 1.7f), 0f, Ground))
                {
                    return true;
                }
            }

            top = default;
            return false;
        }

        private void Hold(params Key[] keys) => InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys));

        private static IEnumerator Wait(float seconds)
        {
            var until = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < until)
            {
                yield return null;
            }
        }

        private static IEnumerator WaitUntil(System.Func<bool> condition, float seconds)
        {
            var deadline = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
        }
    }
}
