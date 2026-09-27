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
    /// The Tomb camera's framing: a jump does not rock the view, the view never shows past the
    /// world's ends, keeps to the player's room and slides into the next, and a boss fight keeps
    /// it on the arena.
    /// </summary>
    public sealed class CameraFramingTests
    {
        private Keyboard _keyboard;
        private InputSettings _previousSettings;
        private InputSettings _testSettings;
        private bool _wasRunningInBackground;
        private PlayerMotor2D _motor;
        private CameraFollow2D _follow;
        private Camera _camera;

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
            _keyboard = InputSystem.AddDevice<Keyboard>("CameraTestKeyboard");

            SaveSystem.Delete();
            yield return SceneManager.LoadSceneAsync("TombTrial");
            _motor = Object.FindAnyObjectByType<PlayerMotor2D>();
            _follow = Object.FindAnyObjectByType<CameraFollow2D>();
            _camera = _follow.GetComponent<Camera>();
            yield return Keys();
            yield return Realtime(0.5f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_keyboard != null) { InputSystem.RemoveDevice(_keyboard); }
            InputSystem.settings = _previousSettings;
            Object.Destroy(_testSettings);
            Application.runInBackground = _wasRunningInBackground;
            GameTimeScale.Reset();
            SaveSystem.Delete();
            yield return null;
        }

        [UnityTest]
        public IEnumerator AJumpDoesNotRockTheViewVertically()
        {
            Assume.That(_motor.IsGrounded, Is.True);
            var restingY = _follow.TargetPosition.y;
            var startY = _motor.transform.position.y;
            var rise = 0f;
            var drift = 0f;
            yield return Keys(Key.Space);
            var end = Time.realtimeSinceStartup + 1.2f;
            while (Time.realtimeSinceStartup < end)
            {
                rise = Mathf.Max(rise, _motor.transform.position.y - startY);
                drift = Mathf.Max(drift, Mathf.Abs(_follow.TargetPosition.y - restingY));
                yield return null;
            }

            yield return Keys();
            Assume.That(rise, Is.GreaterThan(1.5f), "The jump has to happen for this to mean anything.");
            Assert.That(drift, Is.LessThan(0.05f), "A plain jump moved the view.");
        }

        [UnityTest]
        public IEnumerator TheViewStopsAtTheWorldsWestEnd()
        {
            var bounds = _follow.ActiveBounds;
            Assert.That(bounds.HasValue, Is.True, "The world graph gives the camera its extent.");

            _motor.ResetTo(new Vector2(bounds.Value.xMin + 1f, _motor.transform.position.y));
            yield return Realtime(1f);

            var halfWidth = _camera.orthographicSize * _camera.aspect;
            Assert.That(_follow.TargetPosition.x - halfWidth,
                Is.GreaterThanOrEqualTo(bounds.Value.xMin - 0.01f));
            Assert.That(_camera.transform.position.x - halfWidth,
                Is.GreaterThanOrEqualTo(bounds.Value.xMin - 0.1f), "Nothing past the end is shown.");
        }

        [UnityTest]
        public IEnumerator TheViewKeepsToTheRoomAndShowsLittleUnderTheFloor()
        {
            var director = _follow.GetComponent<CameraDirector>();
            var hall = director.Rooms.Rooms[director.Rooms.IndexOf("room_tomb_burial_hall")];
            _motor.ResetTo(new Vector2(hall.Area.center.x, 0.85f));
            yield return Realtime(1f);

            Assert.That(_follow.ActiveBounds, Is.EqualTo(hall.View));
            var halfHeight = _camera.orthographicSize;
            var halfWidth = halfHeight * _camera.aspect;
            var view = _camera.transform.position;
            Assert.That(view.y - halfHeight, Is.GreaterThanOrEqualTo(-1f - CameraRooms.FloorMargin - 0.1f),
                "Only a strip of ground shows under the floor, not the void below it.");
            if (hall.View.width > halfWidth * 2f)
            {
                Assert.That(view.x - halfWidth, Is.GreaterThanOrEqualTo(hall.View.xMin - 0.1f));
                Assert.That(view.x + halfWidth, Is.LessThanOrEqualTo(hall.View.xMax + 0.1f));
            }
        }

        [UnityTest]
        public IEnumerator TheTombsNarrowRoomsShareOneFrameSoTheViewFollowsThroughThem()
        {
            var rooms = _follow.GetComponent<CameraDirector>().Rooms;
            var hall = rooms.IndexOf("room_tomb_burial_hall");
            // Both are narrower than any screen shape, so they share a frame whatever the aspect.
            Assert.That(rooms.IndexOf("room_tomb_entrance"), Is.EqualTo(hall),
                "Rooms narrower than the screen are joined rather than slid between.");
            Assert.That(rooms.IndexOf("room_tomb_guardian_arena"), Is.Not.EqualTo(hall),
                "The arena keeps its own frame.");
            var halfWidth = _camera.orthographicSize * _camera.aspect;
            foreach (var room in rooms.Rooms)
            {
                Assert.That(room.View.width >= halfWidth * 2f - 0.01f || room.KeepApart
                    || room.AreaIds.Count > 1 || rooms.Count == 1, Is.True,
                    $"{room.AreaIds[0]} is narrower than the view and was left alone.");
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator CrossingIntoTheNextRoomSlidesTheViewWithoutACut()
        {
            var rooms = _follow.GetComponent<CameraDirector>().Rooms;
            var entrance = rooms.Rooms[rooms.IndexOf("room_forest_entrance")];
            var road = rooms.Rooms[rooms.IndexOf("room_forest_royal_road")];
            Assume.That(rooms.IndexOf("room_forest_entrance"),
                Is.Not.EqualTo(rooms.IndexOf("room_forest_royal_road")));
            _motor.ResetTo(new Vector2(entrance.Area.xMax - 0.7f, 0.85f));
            yield return Realtime(1f);

            var slid = false;
            var biggestStep = 0f;
            var last = _camera.transform.position;
            yield return Keys(Key.D);
            var end = Time.realtimeSinceStartup + 1.5f;
            while (Time.realtimeSinceStartup < end)
            {
                yield return null;
                slid |= _follow.IsChangingRooms;
                biggestStep = Mathf.Max(biggestStep, Vector2.Distance(last, _camera.transform.position));
                last = _camera.transform.position;
            }

            yield return Keys();
            Assert.That(_motor.transform.position.x, Is.GreaterThan(road.Area.xMin + CameraRooms.SwitchMargin),
                "The player has to reach the next room for this to mean anything.");
            Assert.That(slid, Is.True, "The view slid across instead of cutting.");
            Assert.That(biggestStep, Is.LessThan(0.6f), "No frame jumps the view.");
            Assert.That(_follow.ActiveBounds, Is.EqualTo(road.View));
        }

        [UnityTest]
        public IEnumerator ABossFightKeepsTheViewOnTheArena()
        {
            _motor.ResetTo(new Vector2(42f, 1f));
            var end = Time.realtimeSinceStartup + 3f;
            while (Time.realtimeSinceStartup < end && !_follow.HasFightBounds)
            {
                yield return null;
            }

            var arena = _follow.ActiveBounds;
            Assert.That(_follow.HasFightBounds && arena.HasValue, Is.True, "The fight gave the camera its arena.");
            // Let the zoom settle before measuring the view against the arena.
            yield return Realtime(2f);

            var rect = arena.Value;
            var target = _follow.TargetPosition;
            var halfWidth = _camera.orthographicSize * _camera.aspect;
            if (rect.width <= halfWidth * 2f)
            {
                Assert.That(target.x, Is.EqualTo(rect.center.x).Within(0.01f),
                    "An arena narrower than the view is held centred.");
            }
            else
            {
                Assert.That(target.x - halfWidth, Is.GreaterThanOrEqualTo(rect.xMin - 0.01f));
                Assert.That(target.x + halfWidth, Is.LessThanOrEqualTo(rect.xMax + 0.01f));
            }
        }

        private IEnumerator Keys(params Key[] keys)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys));
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
