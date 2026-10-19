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
        private float _previousMaximumDeltaTime;
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
            // The walker reads the level and presses keys once a frame. On a loaded machine, at
            // four times speed, one frame used to cover several physics steps — half a metre of
            // running — so the jump from the road's step to the archer's ledge left early or late
            // and dropped the walker under the ledge. One step per frame at most keeps its timing
            // a player's, however slow the frames.
            _previousMaximumDeltaTime = Time.maximumDeltaTime;
            Time.maximumDeltaTime = Time.fixedDeltaTime;
            _keyboard = InputSystem.AddDevice<Keyboard>("WorldTraversalKeyboard");

            SaveSystem.Delete();
            yield return TestWorld.Load();
            _player = Object.FindAnyObjectByType<PlayerCharacter>();
            _motor = _player.GetComponent<PlayerMotor2D>();
            _progress = Object.FindAnyObjectByType<GameProgress>();
            _world = Object.FindAnyObjectByType<WorldState>();
            Quiet();
            // Zones loaded on the way (the capital, as the forest's neighbour) are quietened too.
            ZoneStreamer.ZonesChanged += Quiet;
            yield return Keys();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            ZoneStreamer.ZonesChanged -= Quiet;
            GameTimeScale.Reset();
            if (_keyboard != null) { InputSystem.RemoveDevice(_keyboard); }
            InputSystem.settings = _previousSettings;
            Object.Destroy(_testSettings);
            Application.runInBackground = _wasRunningInBackground;
            Time.maximumDeltaTime = _previousMaximumDeltaTime;
            SaveSystem.Delete();
            yield return null;
        }

        // ---------- the main path ----------

        [UnityTest, Timeout(420000)]
        public IEnumerator TheWholeMainPathIsWalkableEastAndBackWithoutAnyAbility()
        {
            // The Guardian and the Warden beaten, as a player walking this far would have them. No air dash, no
            // wall run, no second jump: the walk must need none of them.
            _progress.MarkBossDefeated(TombScene.GuardianId);
            _progress.MarkBossDefeated(TombScene.WardenId);
            Assume.That(_progress.HasAbility(AbilityIds.AirDash), Is.False);
            Assume.That(_progress.HasAbility(AbilityIds.WallRun), Is.False);
            Assume.That(_progress.HasAbility(AbilityIds.DoubleJump), Is.False);
            // The zones built from text (the Tomb, the Forest, the Village) have their main paths proven
            // by their texts, the hero's own movement stepped through every jump (TombLayoutTests,
            // ForestLayoutTests, VillageLayoutTests). Since item 5 of the 15 zones the capital is
            // entered from the Village above it; this walk starts on its old road, by the forest's bars.
            var road = ForestScene.Spot("capital-seal") + new Vector2(5.5f, 0.85f);
            // The capital is two crossings from the Tomb: it is loaded before the hero stands in it.
            yield return TestWorld.LoadEveryZone();
            Quiet();
            yield return Place(road);

            GameTimeScale.SetBaseScale(4f);
            // Since item 6a the capital is built from its text too, its main path (over the tower's
            // bridge to the throne) proven by LevelCheck with the hero's own movement; the walk keeps
            // the stretch the old road feeds, to the bell tower's door.
            yield return Walk(+1, CapitalScene.Room("room_capital_angel_square").xMax - 2f,
                "east along the capital's upper tier to the bell tower's door",
                pullWinches: false);
            Assert.That(_world.CurrentAreaId, Is.EqualTo("room_capital_angel_square"));

            yield return Walk(-1, road.x, "back west to the old road by the forest's bars", pullWinches: false);
            Assert.That(_world.CurrentZoneId, Is.EqualTo("zone_ashen_capital"));
            foreach (var area in _progress.World.Areas.Where(a => !a.IsOptional && a.ZoneId == "zone_ashen_capital"
                && CapitalScene.Room(a.AreaId).xMax <= CapitalScene.Room("room_capital_angel_square").xMax))
            {
                Assert.That(_progress.IsAreaDiscovered(area.AreaId), Is.True,
                    $"The walk passed through {area.AreaId}, so the map has it.");
            }
        }

        // ---------- ability gates ----------

        [UnityTest]
        public IEnumerator TheBellShaftNeedsTheSecondJump()
        {
            // From the last slab under the shaft's mouth: up, and at the top of the jump, up again
            // and right onto the shaft's first slab, five cells over the slab jumped from.
            var slab = TombScene.Spot("bell-glow").y;
            yield return JumpForBell(hasDoubleJump: false);
            Assert.That(_motor.IsGrounded && _player.transform.position.y > slab + 0.5f, Is.False,
                $"One jump cannot stand on the shaft's slab: ended at {_player.transform.position}.");

            yield return JumpForBell(hasDoubleJump: true);
            Assert.That(_player.transform.position.y, Is.GreaterThan(slab + 0.5f),
                $"Standing in the shaft after the double jump: ended at {_player.transform.position}.");
            Assert.That(_motor.IsGrounded, Is.True, $"Landed; ended at {_player.transform.position}.");
            Assert.That(_progress.IsAreaDiscovered("room_tomb_bell_shaft"), Is.True);
        }

        [UnityTest]
        public IEnumerator TheReliquaryNeedsADashInTheAir()
        {
            yield return LeapForReliquary(hasAirDash: false);
            Assert.That(OnThePedestal(), Is.False, $"A running jump falls short: ended at {_player.transform.position}.");

            yield return LeapForReliquary(hasAirDash: true);
            Assert.That(OnThePedestal(), Is.True, $"Ended at {_player.transform.position} after the air dash.");
            Assert.That(_progress.IsAreaDiscovered("room_tomb_reliquary"), Is.True);
        }

        // ---------- helpers ----------

        private IEnumerator JumpForBell(bool hasDoubleJump)
        {
            SetAbility(AbilityIds.DoubleJump, hasDoubleJump);
            SetAbility(AbilityIds.WallRun, false);
            // From the slab's east end: its west end lies under the shaft's next slab.
            yield return Place(TombScene.Spot("double-jump-hint") + new Vector2(1f, 0.85f));

            yield return Keys(Key.Space);
            yield return UntilApex(0.2f);
            yield return Keys();
            // Straight up through the mouth first, then over onto the slab: the entrance's vault
            // hangs right beside the mouth.
            yield return Keys(Key.Space);
            yield return UntilApex(1.5f);
            yield return Hold(0.4f, Key.D);
            yield return Keys();
            yield return new WaitForSeconds(0.9f);
        }

        private IEnumerator LeapForReliquary(bool hasAirDash)
        {
            SetAbility(AbilityIds.AirDash, hasAirDash);
            SetAbility(AbilityIds.DoubleJump, false);
            // The slab by the reliquary's door in the lower chain; the pedestal is twelve cells west.
            yield return Place(TombScene.Spot("air-dash-hint") + new Vector2(0.6f, 0.85f));

            yield return Hold(0.15f, Key.A);
            yield return Keys(Key.A, Key.Space);
            yield return UntilApex(0.5f);
            yield return Keys(Key.A, Key.Space, Key.L);
            yield return Hold(0.8f, Key.A);
            yield return Keys();
            yield return new WaitForSeconds(0.6f);
        }

        /// <summary>Standing on the reliquary's pedestal (two cells wide, west of the spikes).</summary>
        private bool OnThePedestal()
        {
            var room = TombScene.Layout.WorldBounds(TombScene.Layout.Room("room_tomb_reliquary"));
            return _player.IsAlive && _motor.IsGrounded && _player.transform.position.x < room.xMin + 2.6f
                && room.Contains(_player.transform.position);
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

                yield return Keys(run);
            }

            yield return Keys();
        }

        private IEnumerator Jump(Key run)
        {
            yield return Keys(run, Key.Space);
            var deadline = Time.time + 1.2f;
            while (Time.time < deadline && (_motor.Velocity.y > 0f || _motor.IsGrounded))
            {
                yield return Keys(run, Key.Space);
                if (_motor.IsGrounded && Time.time > deadline - 1.1f)
                {
                    break;
                }
            }

            yield return Keys(run);
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
