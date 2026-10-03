using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The level tool's reach check against the real hero (item 1 of the 15-zone list): the same
    /// small layouts are built as colliders far from the zones, and the hero, driven by the keyboard,
    /// makes every jump the strict check allows, and nothing the generous one forbids. If the motor
    /// changes, this is what says the layouts' rule of thumb has gone stale.
    /// </summary>
    public sealed class LevelReachTests
    {
        private static readonly Vector2 Origin = new Vector2(3000f, 300f);
        private Keyboard _keyboard;
        private InputSettings _previousSettings;
        private InputSettings _testSettings;
        private bool _wasRunningInBackground;
        private float _previousMaximumDeltaTime;
        private PlayerMotor2D _motor;
        private GameProgress _progress;
        private GameObject _level;

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
            _previousMaximumDeltaTime = Time.maximumDeltaTime;
            Time.maximumDeltaTime = Time.fixedDeltaTime;
            _keyboard = InputSystem.AddDevice<Keyboard>("LevelReachKeyboard");
            SaveSystem.Delete();
            yield return TestWorld.Load();
            _motor = Object.FindAnyObjectByType<PlayerMotor2D>();
            _progress = Object.FindAnyObjectByType<GameProgress>();
            yield return Keys();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_level != null) { Object.Destroy(_level); }
            if (_keyboard != null) { InputSystem.RemoveDevice(_keyboard); }
            InputSystem.settings = _previousSettings;
            Object.Destroy(_testSettings);
            Application.runInBackground = _wasRunningInBackground;
            Time.maximumDeltaTime = _previousMaximumDeltaTime;
            SaveSystem.Delete();
            yield return null;
        }

        // ---------- layouts (the same as LevelLayoutTests) ----------

        private static string[] Box(int width, int height)
        {
            var rows = new string[height];
            for (var r = 0; r < height; r++)
            {
                var y = height - 1 - r;
                rows[r] = y < 2 || y == height - 1 ? new string('#', width) : "#" + new string('.', width - 2) + "#";
            }

            return rows;
        }

        private static void Put(string[] rows, int x, int y, char c)
        {
            var chars = rows[rows.Length - 1 - y].ToCharArray();
            chars[x] = c;
            rows[rows.Length - 1 - y] = new string(chars);
        }

        private static LevelZone Parse(string[] grid)
        {
            var text = new StringBuilder($"zone zone_test\norigin {Origin.x} {Origin.y}\nroom room_test at 0 0\nkey s start\ngrid\n");
            foreach (var row in grid) { text.Append(row).Append('\n'); }
            return LevelZone.Parse(text.Append("end\n").ToString());
        }

        private static LevelZone Shelf(int height)
        {
            var grid = Box(40, 22);
            for (var x = 14; x < 28; x++) { Put(grid, x, 2 + height - 1, '='); }
            Put(grid, 4, 2, 's');
            return Parse(grid);
        }

        private static LevelZone Gap(int width)
        {
            var grid = Box(64, 22);
            for (var y = 2; y < 6; y++) { for (var x = 1; x < 63; x++) { Put(grid, x, y, '#'); } }
            for (var y = 1; y < 6; y++) { for (var x = 20; x < 20 + width; x++) { Put(grid, x, y, '.'); } }
            Put(grid, 4, 6, 's');
            return Parse(grid);
        }

        private static bool Predicts(LevelZone zone, int row, int fromX, bool secondJump, bool generous)
        {
            var abilities = new HashSet<string>();
            if (secondJump) { abilities.Add(AbilityIds.DoubleJump); }
            var reach = new LevelReach(zone, abilities, generous: generous);
            var start = reach.SegmentUnder(LevelCheck.Start(zone).Cell);
            return reach.Forward(start).Any(i => reach.Segments[i].Row == row && reach.Segments[i].XMax >= fromX);
        }

        // ---------- the hero ----------

        [UnityTest, Timeout(120000)]
        public IEnumerator TheHeroClimbsWhatTheCheckAllowsAndNoMore()
        {
            foreach (var (height, second) in new[] { (3, false), (4, false), (6, true), (7, true), (8, true) })
            {
                var zone = Shelf(height);
                var strict = Predicts(zone, 2 + height, 14, second, false);
                var generous = Predicts(zone, 2 + height, 14, second, true);
                yield return Build(zone, second);
                yield return Place(zone, new Vector2Int(4, 2));
                yield return Hold(0.25f, Key.D);
                yield return Keys(Key.D, Key.Space);
                if (second)
                {
                    yield return UntilApex();
                    yield return Keys(Key.D);
                    yield return Keys(Key.D, Key.Space);
                }

                yield return Hold(0.9f, Key.D, Key.Space);
                yield return Keys();
                yield return new WaitForSeconds(0.8f);
                var top = zone.CellCorner(new Vector2Int(0, 2 + height)).y;
                var onShelf = _motor.IsGrounded && Feet().y > top - 0.1f;
                Agree($"Shelf {height * 0.5f} up (second jump {second})", strict, onShelf, generous);
            }
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator TheHeroClearsTheGapsTheCheckAllowsAndNoMore()
        {
            foreach (var width in new[] { 8, 10, 11 })
            {
                var zone = Gap(width);
                var strict = Predicts(zone, 6, 20 + width + 2, false, false);
                var generous = Predicts(zone, 6, 20 + width + 2, false, true);
                yield return Build(zone, false);
                yield return Place(zone, new Vector2Int(4, 6));
                var edge = zone.CellCorner(new Vector2Int(20, 0)).x;
                var deadline = Time.realtimeSinceStartup + 4f;
                while (_motor.transform.position.x < edge - 0.1f && Time.realtimeSinceStartup < deadline)
                {
                    yield return Keys(Key.D);
                }

                yield return Keys(Key.D, Key.Space);
                yield return Hold(1.2f, Key.D, Key.Space);
                yield return Keys();
                yield return new WaitForSeconds(0.5f);
                var across = Feet().y > zone.CellCorner(new Vector2Int(0, 6)).y - 0.1f
                    && Feet().x > zone.CellCorner(new Vector2Int(20 + width, 0)).x;
                Agree($"Gap {width * 0.5f}", strict, across, generous);
            }
        }

        // ---------- helpers ----------

        /// <summary>
        /// What the strict model allows the hero must do (the main path counts on it); what the hero
        /// does the generous model must allow (a gate counts on that).
        /// </summary>
        private void Agree(string what, bool strict, bool real, bool generous)
        {
            if (strict)
            {
                Assert.That(real, Is.True, $"{what}: the strict check allows it, the hero ended at {Feet()}.");
            }

            if (real)
            {
                Assert.That(generous, Is.True, $"{what}: the hero made it ({Feet()}), the generous check says no.");
            }
        }

        private IEnumerator Build(LevelZone zone, bool secondJump)
        {
            Assert.That(zone.Errors, Is.Empty);
            if (_level != null)
            {
                Object.Destroy(_level);
                yield return null;
            }

            _level = new GameObject("LevelReachTest");
            LevelSolids.BuildColliders(zone, _level.transform, 9);
            Physics2D.SyncTransforms();
            if (secondJump)
            {
                _progress.UnlockAbility(AbilityIds.DoubleJump);
            }
            else if (_progress.HasAbility(AbilityIds.DoubleJump))
            {
                _progress.Player.unlockedAbilities.Remove(AbilityIds.DoubleJump);
                SaveSystem.Write(_progress.Data);
                _progress.LoadGame();
            }
        }

        private IEnumerator Place(LevelZone zone, Vector2Int cell)
        {
            yield return Keys();
            _motor.ResetTo(zone.CellCorner(cell) + new Vector2(0f, 0.85f));
            yield return new WaitForSeconds(0.4f);
        }

        private Vector2 Feet() => (Vector2)_motor.transform.position - new Vector2(0f, 0.8f);

        private IEnumerator UntilApex()
        {
            var deadline = Time.realtimeSinceStartup + 2f;
            while (_motor.Velocity.y <= 0.2f && Time.realtimeSinceStartup < deadline) { yield return new WaitForFixedUpdate(); }
            while (_motor.Velocity.y > 0.2f && Time.realtimeSinceStartup < deadline) { yield return new WaitForFixedUpdate(); }
        }

        private IEnumerator Keys(params Key[] keys)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys));
            yield return null;
        }

        private IEnumerator Hold(float seconds, params Key[] keys)
        {
            var until = Time.time + seconds;
            while (Time.time < until)
            {
                yield return Keys(keys);
            }
        }
    }
}
