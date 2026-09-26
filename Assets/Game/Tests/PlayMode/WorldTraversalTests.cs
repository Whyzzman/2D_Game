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
    /// The world walked for real, with the keyboard: the whole main path east and back again, and
    /// the jumps the ability gates are built around — made with the ability and failed without it.
    ///
    /// A map that says two rooms are connected proves nothing; these runs are what caught the
    /// PHASE 24 forest being impassable. Enemies and hazards are switched off so that what is
    /// measured is the level, not a fight.
    /// </summary>
    public sealed class WorldTraversalTests
    {
        private Keyboard _keyboard;
        private InputSettings _previousSettings;
        private InputSettings _testSettings;
        private bool _wasRunningInBackground;
        private PlayerCharacter _player;
        private PlayerMotor2D _motor;
        private GameProgress _progress;
        private WorldState _world;

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
            _keyboard = InputSystem.AddDevice<Keyboard>("WorldTraversalKeyboard");

            SaveSystem.Delete();
            yield return SceneManager.LoadSceneAsync("TombTrial");
            _player = Object.FindAnyObjectByType<PlayerCharacter>();
            _motor = _player.GetComponent<PlayerMotor2D>();
            _progress = Object.FindAnyObjectByType<GameProgress>();
            _world = Object.FindAnyObjectByType<WorldState>();
            Quiet();
            yield return Keys();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            if (_keyboard != null) { InputSystem.RemoveDevice(_keyboard); }
            InputSystem.settings = _previousSettings;
            Object.Destroy(_testSettings);
            Application.runInBackground = _wasRunningInBackground;
            SaveSystem.Delete();
            yield return null;
        }

        // ---------- the main path ----------

        [UnityTest, Timeout(240000)]
        public IEnumerator TheWholeMainPathIsWalkableEastAndBackWithoutAnyAbility()
        {
            // Both bosses beaten, as a player walking this far would have them. No air dash, no
            // wall run, no second jump: the walk must need none of them.
            _progress.MarkBossDefeated(TombScene.GuardianId);
            _progress.MarkBossDefeated(TombScene.WardenId);
            Assume.That(_progress.HasAbility(AbilityIds.AirDash), Is.False);
            Assume.That(_progress.HasAbility(AbilityIds.WallRun), Is.False);
            Assume.That(_progress.HasAbility(AbilityIds.DoubleJump), Is.False);
            yield return null;

            Time.timeScale = 4f;
            yield return Walk(+1, 312f, "east to the sealed road beyond the Warden",
                pullWinches: false);
            Assert.That(_world.CurrentAreaId, Is.EqualTo("room_forest_far_gate"));
            Assert.That(_progress.IsShortcutUnlocked(ContentBuilderIds.RoadGate), Is.False,
                "East is walkable over the archer's ledge without the shortcut.");

            yield return Walk(-1, -13f, "back west to the Tomb's first altar", pullWinches: true);
            Assert.That(_world.CurrentZoneId, Is.EqualTo("zone_forgotten_tomb"));
            Assert.That(_world.CurrentAreaId, Is.EqualTo("room_tomb_entrance"));
            Assert.That(_progress.IsShortcutUnlocked(ContentBuilderIds.RoadGate), Is.True,
                "West, the road gate's winch is what opened the way.");
            foreach (var area in _progress.World.Areas.Where(a => !a.IsOptional
                         && _progress.World.Zone(a.ZoneId).IsImplemented))
            {
                Assert.That(_progress.IsAreaDiscovered(area.AreaId), Is.True,
                    $"The walk passed through {area.AreaId}, so the map has it.");
            }
        }

        // ---------- ability gates ----------

        [UnityTest]
        public IEnumerator TheOssuaryShelfNeedsTheSecondJump()
        {
            // From the guards' ledge: up, and at the top of the jump, up again and left.
            yield return JumpForShelf(hasDoubleJump: false);
            Assert.That(_progress.IsAreaDiscovered("room_tomb_royal_ossuary"), Is.False,
                "One jump and a wall run cannot reach it.");

            yield return JumpForShelf(hasDoubleJump: true);
            Assert.That(_progress.IsAreaDiscovered("room_tomb_royal_ossuary"), Is.True,
                $"Ended at {_player.transform.position} after the double jump.");
            Assert.That(_player.transform.position.y, Is.GreaterThan(7.6f),
                $"Standing on the shelf, not below it: ended at {_player.transform.position}.");
            Assert.That(_motor.IsGrounded, Is.True, $"Landed; ended at {_player.transform.position}.");
        }

        [UnityTest]
        public IEnumerator TheReliquaryNeedsADashInTheAir()
        {
            yield return LeapForReliquary(hasAirDash: false);
            Assert.That(_progress.IsAreaDiscovered("room_tomb_reliquary"), Is.False,
                "A running jump falls short.");

            yield return LeapForReliquary(hasAirDash: true);
            Assert.That(_progress.IsAreaDiscovered("room_tomb_reliquary"), Is.True,
                $"Ended at {_player.transform.position} after the air dash.");
        }

        // ---------- helpers ----------

        private IEnumerator JumpForShelf(bool hasDoubleJump)
        {
            SetAbility(AbilityIds.DoubleJump, hasDoubleJump);
            SetAbility(AbilityIds.WallRun, true);
            yield return Place(new Vector2(15.9f, 5.3f));

            yield return Keys(Key.Space);
            yield return UntilApex(0.2f);
            yield return Keys();
            yield return Keys(Key.Space, Key.A);
            yield return Hold(0.6f, Key.Space, Key.A);
            // Let go once over the shelf. Holding left for another 0.8 s ran the player off the
            // shelf's far edge (it is three units wide), so the room was reached but the landing
            // was on the floor below — which is what this test failed on until PHASE 26.
            yield return Keys();
            yield return new WaitForSeconds(0.9f);
        }

        private IEnumerator LeapForReliquary(bool hasAirDash)
        {
            SetAbility(AbilityIds.AirDash, hasAirDash);
            SetAbility(AbilityIds.DoubleJump, false);
            yield return Place(new Vector2(13.6f, 4.1f));

            // A run west along the second step, a jump off its end, a dash at the top.
            yield return Hold(0.35f, Key.A, Key.LeftShift);
            yield return Keys(Key.A, Key.LeftShift, Key.Space);
            yield return UntilApex(0.5f);
            yield return Keys(Key.A, Key.LeftShift, Key.Space, Key.LeftCtrl);
            yield return Hold(0.8f, Key.A, Key.LeftShift);
            yield return Keys();
            yield return new WaitForSeconds(0.6f);
        }

        /// <summary>
        /// Runs one way until reaching <paramref name="targetX"/>, jumping when blocked or at an
        /// edge, and pulling a shortcut's winch when one is in reach and shut. Fails if it stops
        /// making progress, which is what a wall on the main path looks like.
        /// </summary>
        private IEnumerator Walk(int direction, float targetX, string leg, bool pullWinches)
        {
            var run = direction > 0 ? Key.D : Key.A;
            var bestX = _player.transform.position.x;
            var lastProgressAt = Time.time;
            var blockedFor = 0f;
            while ((targetX - _player.transform.position.x) * direction > 0f)
            {
                if (pullWinches)
                {
                    PullNearbyWinch();
                }

                var x = _player.transform.position.x;
                if ((x - bestX) * direction > 0.25f)
                {
                    bestX = x;
                    lastProgressAt = Time.time;
                }

                Assert.That(Time.time - lastProgressAt, Is.LessThan(10f),
                    $"Stuck at x={x:0.0}, y={_player.transform.position.y:0.0} walking {leg}.");
                Assert.That(_player.IsAlive, Is.True, $"Died at x={x:0.0} walking {leg}.");

                blockedFor = Mathf.Abs(_motor.Velocity.x) < 1f ? blockedFor + Time.deltaTime : 0f;
                if (_motor.IsGrounded && (blockedFor > 0.08f || !GroundAhead(direction)))
                {
                    yield return Jump(run);
                    blockedFor = 0f;
                    continue;
                }

                yield return Keys(run, Key.LeftShift);
            }

            yield return Keys();
        }

        private IEnumerator Jump(Key run)
        {
            yield return Keys(run, Key.LeftShift, Key.Space);
            var deadline = Time.time + 1.2f;
            while (Time.time < deadline && (_motor.Velocity.y > 0f || _motor.IsGrounded))
            {
                yield return Keys(run, Key.LeftShift, Key.Space);
                if (_motor.IsGrounded && Time.time > deadline - 1.1f)
                {
                    break;
                }
            }

            yield return Keys(run, Key.LeftShift);
        }

        /// <summary>
        /// Waits for the jump to leave the ground and then to slow to <paramref name="speed"/>.
        /// Checking the speed straight away is wrong: the press lands before the physics step
        /// that applies it, so the first reading is still zero.
        /// </summary>
        private IEnumerator UntilApex(float speed)
        {
            var deadline = Time.realtimeSinceStartup + 2f;
            while (_motor.Velocity.y <= speed && Time.realtimeSinceStartup < deadline)
            {
                yield return new WaitForFixedUpdate();
            }

            while (_motor.Velocity.y > speed && Time.realtimeSinceStartup < deadline)
            {
                yield return new WaitForFixedUpdate();
            }
        }

        private bool GroundAhead(int direction)
        {
            var feet = (Vector2)_player.transform.position + new Vector2(0.9f * direction, -0.6f);
            return Physics2D.Raycast(feet, Vector2.down, 0.9f, 1 << 9);
        }

        private void PullNearbyWinch()
        {
            foreach (var shortcut in Object.FindObjectsByType<Shortcut>())
            {
                if (!shortcut.IsUnlocked && Vector2.Distance(shortcut.transform.position,
                        _player.transform.position) < 2.5f)
                {
                    shortcut.Interact(_player);
                }
            }
        }

        private void SetAbility(string id, bool owned)
        {
            if (owned)
            {
                _progress.UnlockAbility(id);
            }
            else
            {
                _progress.Player.unlockedAbilities.Remove(id);
                // The set is rebuilt from the list on load; reload it so the motor sees the loss.
                var data = _progress.Data;
                SaveSystem.Write(data);
                _progress.LoadGame();
            }
        }

        private IEnumerator Place(Vector2 position)
        {
            yield return Keys();
            _motor.ResetTo(position);
            yield return new WaitForSeconds(0.3f);
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

        /// <summary>Everything that could fight or poison the walker is switched off.</summary>
        private static void Quiet()
        {
            foreach (var enemy in Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Include))
            {
                enemy.gameObject.SetActive(false);
            }

            foreach (var boss in Object.FindObjectsByType<BossController>(FindObjectsInactive.Include))
            {
                boss.gameObject.SetActive(false);
            }

            foreach (var hazard in Object.FindObjectsByType<HazardZone>(FindObjectsInactive.Include))
            {
                hazard.gameObject.SetActive(false);
            }

            foreach (var dummy in Object.FindObjectsByType<TrainingDummy>(FindObjectsInactive.Include))
            {
                dummy.gameObject.SetActive(false);
            }

            foreach (var attacker in Object.FindObjectsByType<TrainingAttacker>(FindObjectsInactive.Include))
            {
                attacker.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>Ids the traversal tests share with the content builder, spelled once.</summary>
    internal static class ContentBuilderIds
    {
        public const string RoadGate = "shortcut_forest_road_gate";
        public const string CryptBridge = "shortcut_tomb_crypt_bridge";
    }
}
