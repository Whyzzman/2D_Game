using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// Rooms behind breakable walls (item 4 of the big list, stage B): five walls, each at the end
    /// of a ledge with a shard in the dark behind it; three of the hero's blows bring one down,
    /// the veil lifts, and the fall is remembered after a load.
    /// </summary>
    public sealed class SecretRoomTests
    {
        private Keyboard _keyboard;
        private InputSettings _previousSettings;
        private InputSettings _testSettings;
        private PlayerCharacter _player;
        private PlayerMotor2D _motor;
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
            _keyboard = InputSystem.AddDevice<Keyboard>("SecretRoomKeyboard");
            KeyBindings.ResetAll();
            SaveSystem.Delete();
            yield return TestWorld.Load();
            yield return TestWorld.LoadEveryZone();
            _progress = Object.FindAnyObjectByType<GameProgress>();
            _player = Object.FindAnyObjectByType<PlayerCharacter>();
            _motor = _player.GetComponent<PlayerMotor2D>();
            _player.Actor.SetInvulnerable(ImmunitySource.Script, 120f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_keyboard != null) { InputSystem.RemoveDevice(_keyboard); }
            InputSystem.settings = _previousSettings;
            Object.Destroy(_testSettings);
            SaveSystem.Delete();
            yield return null;
        }

        private static IEnumerator Realtime(float seconds)
        {
            var until = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < until)
            {
                yield return null;
            }
        }

        private static IEnumerator Until(Func<bool> condition, float seconds, string what)
        {
            var deadline = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(condition(), Is.True, what);
        }

        private static BreakableWall Wall(string room) =>
            Object.FindObjectsByType<BreakableWall>(FindObjectsInactive.Include)
                .Single(w => w.WallId.EndsWith(room));

        [UnityTest]
        public IEnumerator EightWallsEachHideAFindInTheDark()
        {
            // Four in the old zones, four in the Tomb's text (its three caches and the walled cell).
            var walls = Object.FindObjectsByType<BreakableWall>(FindObjectsInactive.Include);
            Assert.That(walls.Length, Is.EqualTo(8));
            Assert.That(walls.Select(w => w.WallId).Distinct().Count(), Is.EqualTo(8));
            var finds = Object.FindObjectsByType<WorldPickup>(FindObjectsInactive.Include);
            var shards = finds.Where(p => p.IsVesselShard).ToArray();
            foreach (var wall in walls)
            {
                Assert.That(wall.IsBroken, Is.False, $"{wall.WallId} stands in a new game.");
                Assert.That(wall.VeilAlpha, Is.GreaterThan(0.9f), $"{wall.WallId}: the room is dark.");
                var at = (Vector2)wall.transform.position;
                Assert.That(finds.Any(s => Mathf.Abs(s.transform.position.x - at.x) < 5f
                    && Mathf.Abs(s.transform.position.y - at.y) < 2f), Is.True,
                    $"{wall.WallId} at {at} has a find behind it.");
            }

            Assert.That(shards.Count(s => VesselShards.TryKind(s.ItemId, out var k) && k == VesselKind.Health),
                Is.EqualTo(8), "Eight health shards: four in pockets, three behind walls, one for a trial.");
            Assert.That(shards.Count(s => VesselShards.TryKind(s.ItemId, out var k) && k == VesselKind.Mana),
                Is.EqualTo(6), "Six mana shards: three in pockets, two behind walls, one for a trial.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator ThreeBlowsFromTheMasonsCellBringTheWallDownForGood()
        {
            var wall = Wall("tomb_walled_cell");
            foreach (var enemy in Object.FindObjectsByType<EnemyController>())
            {
                enemy.gameObject.SetActive(false);
            }

            // On the mason's cell's ledge by the walled cell's door, a step from the wall, facing it.
            var stone = wall.GetComponent<Collider2D>().bounds;
            _motor.ResetTo(new Vector2(stone.max.x + 1.5f, stone.min.y + 0.85f));
            yield return Realtime(0.3f);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.A));
            yield return Realtime(0.06f);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return Realtime(0.3f);

            for (var blow = 1; blow <= 3; blow++)
            {
                var before = wall.Hits;
                InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.J));
                yield return Realtime(0.08f);
                InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
                yield return Until(() => wall.Hits > before, 2f, $"Blow {blow} lands on the wall.");
                yield return Realtime(0.55f);
                if (blow < 3)
                {
                    Assert.That(wall.IsBroken, Is.False, $"{blow} blow(s) are not enough.");
                }
            }

            Assert.That(wall.IsBroken, Is.True, "The third blow brings it down.");
            Assert.That(wall.GetComponent<Collider2D>().enabled, Is.False, "The way in is open.");
            yield return Until(() => wall.VeilAlpha < 0.01f, 3f, "The veil over the room lifts.");
            Assert.That(_progress.IsPickupCollected(wall.WallId), Is.True, "Remembered.");

            // Walk in: the room is really there, and its shard is in reach.
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.A));
            yield return Until(() => _player.transform.position.x < stone.min.x - 1f, 3f, "The hero walks into the cell.");
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            Assert.That(_player.transform.position.y, Is.GreaterThan(stone.min.y), "On the cell's floor.");

            _progress.Save();
            _progress.LoadGame();
            yield return null;
            Assert.That(wall.IsBroken, Is.True, "A load finds the wall already down.");
        }
    }
}
