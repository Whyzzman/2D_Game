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
    /// The two trials (item 4 of the big list, stage C): the trial of arms on the Ash Road —
    /// three waves behind closed walls, a prize only the first time, called off by a death — and
    /// the Thorn Path under the great tree's roots, whose hops are made here with real keys.
    /// </summary>
    public sealed class TrialTests
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
            _keyboard = InputSystem.AddDevice<Keyboard>("TrialKeyboard");
            KeyBindings.ResetAll();
            SaveSystem.Delete();
            yield return TestWorld.Load();
            _progress = Object.FindAnyObjectByType<GameProgress>();
            _progress.MarkBossDefeated(TombScene.GuardianId);
            _progress.MarkBossDefeated(TombScene.WardenId);
            yield return TestWorld.LoadEveryZone();
            _player = Object.FindAnyObjectByType<PlayerCharacter>();
            _motor = _player.GetComponent<PlayerMotor2D>();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_keyboard != null) { InputSystem.RemoveDevice(_keyboard); }
            InputSystem.settings = _previousSettings;
            Object.Destroy(_testSettings);
            Time.maximumDeltaTime = 0.3333333f;
            SaveSystem.Delete();
            yield return null;
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

        private static IEnumerator Realtime(float seconds)
        {
            var until = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < until)
            {
                yield return null;
            }
        }

        private static TrialArena Arms() => Object.FindObjectsByType<TrialArena>().Single();

        private IEnumerator BeatTheWaves(TrialArena trial, HudController hud)
        {
            for (var wave = 1; wave <= trial.WaveCount; wave++)
            {
                yield return Until(() => trial.CurrentWave == wave && trial.AliveCount > 0, 6f,
                    $"Wave {wave} comes.");
                StringAssert.Contains($"{wave}/{trial.WaveCount}", hud.ZoneTitleText, "The banner names the wave.");
                foreach (var enemy in trial.Alive.ToList())
                {
                    enemy.Actor.ClearInvulnerability();
                    enemy.Actor.ApplyDamage(new DamageInfo(float.MaxValue, Vector2.left, isUnblockable: true,
                        attacker: _player.gameObject));
                }
            }

            yield return Until(() => !trial.IsRunning, 6f, "The last wave beaten ends the trial.");
        }

        [UnityTest]
        public IEnumerator TheArmsTrialClosesTheRoadAndPaysOnlyTheFirstTime()
        {
            _player.Actor.SetInvulnerable(ImmunitySource.Script, 120f);
            var trial = Arms();
            var hud = Object.FindAnyObjectByType<HudController>();
            var walls = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
                .Where(t => t.name.StartsWith("TrialWall")).Select(t => t.gameObject).ToArray();
            var shard = Object.FindObjectsByType<WorldPickup>(FindObjectsInactive.Include)
                .Single(p => p.ItemId == VesselShards.HealthPrefix + "trial-ash-road");
            Assert.That(walls.Length, Is.EqualTo(2));
            Assert.That(walls.Any(w => w.activeSelf), Is.False, "The road is open before the gong.");
            Assert.That(shard.gameObject.activeSelf, Is.False, "No prize before the trial.");

            _motor.ResetTo(new Vector2(330.5f, 1f));
            yield return Realtime(0.3f);
            Assert.That(trial.CanInteract, Is.True);
            trial.Interact(_player);
            Assert.That(walls.All(w => w.activeSelf), Is.True, "The gong closes the road both ways.");
            yield return BeatTheWaves(trial, hud);

            Assert.That(walls.Any(w => w.activeSelf), Is.False, "Won, the road opens.");
            Assert.That(trial.IsWon, Is.True);
            Assert.That(shard.gameObject.activeSelf, Is.True, "The prize is laid out.");
            Assert.That(shard.TryCollect(_player), Is.True);

            // Again: the waves come, the prize does not.
            trial.Interact(_player);
            yield return BeatTheWaves(trial, hud);
            Assert.That(shard.gameObject.activeSelf, Is.False, "A taken prize is not laid out twice.");
        }

        [UnityTest]
        public IEnumerator ADeathCallsTheTrialOff()
        {
            var trial = Arms();
            _motor.ResetTo(new Vector2(330.5f, 1f));
            yield return Realtime(0.3f);
            trial.Interact(_player);
            yield return Until(() => trial.AliveCount > 0, 6f, "The first wave comes.");
            var spawned = trial.Alive.ToList();

            _player.Actor.ClearInvulnerability();
            _player.Actor.ApplyDamage(new DamageInfo(float.MaxValue, Vector2.left, isUnblockable: true));
            yield return Until(() => !trial.IsRunning, 8f, "Death calls the trial off.");
            yield return null;
            Assert.That(spawned.All(e => e == null), Is.True, "Its enemies are gone.");
            Assert.That(trial.IsWon, Is.False);
            Assert.That(Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
                .Where(t => t.name.StartsWith("TrialWall")).Any(t => t.gameObject.activeSelf), Is.False);
        }

        // ---------- the Thorn Path ----------

        /// <summary>
        /// The Thorn Path's perches as cells of the Forest's text (`Levels/forest.txt`, columns
        /// from–to and the top): four in the gallery under the great tree's roots, the landing past
        /// the hatch, two over the thorns and the last ledge with the shard.
        /// </summary>
        private static readonly Vector3Int[] PerchCells =
        {
            new Vector3Int(206, 209, 65), new Vector3Int(214, 217, 71), new Vector3Int(222, 225, 77),
            new Vector3Int(230, 233, 83), new Vector3Int(234, 241, 88), new Vector3Int(246, 249, 94),
            new Vector3Int(254, 257, 100), new Vector3Int(262, 279, 106)
        };

        /// <summary>A perch in the world: x from, x to, top.</summary>
        private static Vector4 Perch(int index)
        {
            var cells = PerchCells[index];
            var layout = ForestScene.Layout;
            var from = layout.CellCorner(new Vector2Int(cells.x, cells.z));
            var to = layout.CellCorner(new Vector2Int(cells.y + 1, cells.z));
            return new Vector4(from.x, to.x, from.y, 0f);
        }

        /// <summary>
        /// One hop as a player makes it: run east, jump at the edge, jump again at the top when
        /// asked, dash at the top of that when asked, let go of the run over the next perch.
        /// </summary>
        private IEnumerator Hop(int from, bool secondJump, bool dash)
        {
            var start = Perch(from);
            var target = Perch(from + 1);
            _motor.ResetTo(new Vector2(start.x + 0.5f, start.z + 0.9f));
            yield return Realtime(0.35f);

            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.D));
            // Near the edge, short of the thorns that start right past the landing, or against the
            // wall where a perch meets one (the last perch under the hatch steps up through it).
            yield return Until(() => _player.transform.position.x >= start.y - 0.7f, 2f, $"Runs to the edge of perch {from}.");
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.D, Key.Space));
            yield return Until(() => !_motor.IsGrounded, 1f, "Takes off.");
            if (secondJump)
            {
                yield return Until(() => _motor.Velocity.y <= 0.5f, 1.5f, "The first jump tops out.");
                InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.D));
                yield return Realtime(0.03f);
                InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.D, Key.Space));
                yield return Realtime(0.1f);
            }

            if (dash)
            {
                yield return Until(() => _motor.Velocity.y <= 0.5f, 1.5f, "The second jump tops out.");
                InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.D, Key.Space, Key.L));
                yield return Realtime(0.1f);
            }

            var centre = (target.x + target.y) * 0.5f;
            var deadline = Time.realtimeSinceStartup + 2f;
            while (_player.transform.position.x < centre - 0.2f && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space));
            yield return Until(() => _motor.IsGrounded, 3f, "Lands somewhere.");
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
        }

        [UnityTest]
        public IEnumerator EveryHopOfTheThornPathIsMadeWithTheKeysItAsksFor()
        {
            _player.Actor.SetInvulnerable(ImmunitySource.Script, 120f);
            _progress.UnlockAbility(AbilityIds.DoubleJump);
            _progress.UnlockAbility(AbilityIds.AirDash);
            foreach (var enemy in Object.FindObjectsByType<EnemyController>())
            {
                enemy.gameObject.SetActive(false);
            }

            Time.maximumDeltaTime = Time.fixedDeltaTime;
            // Every hop past the first perch asks for the second jump.
            for (var i = 0; i < PerchCells.Length - 1; i++)
            {
                yield return Hop(i, true, false);
                var at = _player.transform.position;
                var target = Perch(i + 1);
                // The position is the body's centre, 0.8 over the feet.
                Assert.That(at.y, Is.EqualTo(target.z + 0.8f).Within(0.3f), $"Hop {i}→{i + 1} lands on the perch: at {at}.");
                Assert.That(at.x, Is.InRange(target.x - 0.3f, target.y + 0.3f), $"Hop {i}→{i + 1}: at {at}.");
            }

            var shard = Object.FindObjectsByType<WorldPickup>()
                .Single(p => p.ItemId == VesselShards.ManaPrefix + "thorn-course");
            Assert.That(Vector2.Distance(shard.transform.position, _player.transform.position), Is.LessThan(2.5f),
                "The mana shard waits on the last perch.");
        }

        [UnityTest]
        public IEnumerator WithoutTheSecondJumpTheFirstHopFallsShort()
        {
            _player.Actor.SetInvulnerable(ImmunitySource.Script, 120f);
            foreach (var enemy in Object.FindObjectsByType<EnemyController>())
            {
                enemy.gameObject.SetActive(false);
            }

            Time.maximumDeltaTime = Time.fixedDeltaTime;
            yield return Hop(0, false, false);
            Assert.That(_player.transform.position.y, Is.LessThan(Perch(1).z + 0.5f),
                "One jump does not reach the second perch: the path is the second jump's.");
        }
    }
}
