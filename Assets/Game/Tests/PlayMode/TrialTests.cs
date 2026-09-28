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
    /// the Thorn Path over the Deep Forest, whose hops are made here with real keys.
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

        private static readonly Vector4[] Perches =
        {
            new Vector4(214f, 216.5f, 1.6f, 0f),
            new Vector4(219f, 220.5f, 3.2f, 0f),
            new Vector4(223.5f, 225f, 6.2f, 0f),
            new Vector4(234f, 235.5f, 6.2f, 0f),
            new Vector4(238f, 239.5f, 8.6f, 0f),
            new Vector4(242f, 245f, 10.4f, 0f)
        };

        /// <summary>
        /// One hop as a player makes it: run east, jump at the edge, jump again at the top when
        /// asked, dash at the top of that when asked, let go of the run over the next perch.
        /// </summary>
        private IEnumerator Hop(int from, bool secondJump, bool dash)
        {
            var start = Perches[from];
            var target = Perches[from + 1];
            _motor.ResetTo(new Vector2(start.x + 0.5f, start.z + 0.9f));
            yield return Realtime(0.35f);

            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.D));
            yield return Until(() => _player.transform.position.x >= start.y - 0.3f, 2f, "Runs to the edge.");
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
            var asks = new[] { (false, false), (true, false), (true, true), (true, false), (true, false) };
            for (var i = 0; i < asks.Length; i++)
            {
                yield return Hop(i, asks[i].Item1, asks[i].Item2);
                var at = _player.transform.position;
                var target = Perches[i + 1];
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
        public IEnumerator TheDashGapIsTooWideForJumpsAlone()
        {
            _player.Actor.SetInvulnerable(ImmunitySource.Script, 120f);
            _progress.UnlockAbility(AbilityIds.DoubleJump);
            foreach (var enemy in Object.FindObjectsByType<EnemyController>())
            {
                enemy.gameObject.SetActive(false);
            }

            Time.maximumDeltaTime = Time.fixedDeltaTime;
            yield return Hop(2, true, false);
            Assert.That(_player.transform.position.x, Is.LessThan(Perches[3].x),
                "Without the dash the hero falls short, into the thorns and back to the perch.");
        }
    }
}
