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
    /// «Король-Заклятий» in play (item 2 of the big list, stage D): the throne hall seals behind
    /// the hero and the King's death opens it; with the grapple (the Bellringer's since item 5 of
    /// the 15 zones) a ring lifts the hero onto the royal gallery, and the finds it reaches in the
    /// old zones are really there.
    /// </summary>
    public sealed class KingTests
    {
        private const string King = "ashen-king";
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
            _keyboard = InputSystem.AddDevice<Keyboard>("KingTestKeyboard");
            KeyBindings.ResetAll();
            SaveSystem.Delete();
            yield return TestWorld.Load();
            _progress = Object.FindAnyObjectByType<GameProgress>();
            _progress.MarkBossDefeated(TombScene.GuardianId);
            _progress.MarkBossDefeated(TombScene.WardenId);
            yield return TestWorld.LoadEveryZone();
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
            GameTimeScale.SetPaused(false);
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

        [UnityTest]
        public IEnumerator TheHallSealsAndTheKingsDeathOpensIt()
        {
            var encounter = Object.FindObjectsByType<BossEncounter>().Single(e => e.BossId == King);
            var king = Object.FindObjectsByType<BossController>().Single(b => b.BossId == King);
            Assert.That(encounter.IsArenaSealed, Is.False);

            var hall = CapitalScene.Room("room_capital_throne_hall");
            _motor.ResetTo(new Vector2(hall.xMin + 7f, hall.yMin + 1.85f));
            yield return Until(() => encounter.IsFightActive, 4f, "Past the threshold, the fight begins.");
            Assert.That(encounter.IsArenaSealed, Is.True, "The hall seals behind the hero.");

            yield return Realtime(2.5f);
            king.Actor.ClearInvulnerability();
            king.Actor.ApplyDamage(new DamageInfo(float.MaxValue, Vector2.left, isUnblockable: true,
                attacker: _player.gameObject));
            yield return Until(() => _progress.IsBossDefeated(King), 6f, "He dies.");
            Assert.That(_progress.AcquiredRewards, Does.Contain("king_seal"));
            // The grapple is the Village's Bellringer's now (item 5 of the 15 zones, VillageTests).
            Assert.That(_progress.HasAbility(AbilityIds.Grapple), Is.False, "The King hands over no ability.");
            yield return Until(() => !encounter.IsArenaSealed, 6f, "His death opens the hall.");
        }

        /// <summary>
        /// A player's climb by a ring: face it, throw the line, at the top of the pop jump again
        /// straight up, and step toward the shelf once over it. Returns where the hero lands.
        /// </summary>
        private IEnumerator ClimbByRing(Vector2 start, Key towardRing, Key towardShelf, float overShelfY)
        {
            _progress.UnlockAbility(AbilityIds.Grapple);
            _progress.UnlockAbility(AbilityIds.DoubleJump);
            foreach (var enemy in Object.FindObjectsByType<EnemyController>())
            {
                enemy.gameObject.SetActive(false);
            }

            Time.maximumDeltaTime = Time.fixedDeltaTime;
            _motor.ResetTo(start);
            yield return Realtime(0.3f);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(towardRing));
            yield return Realtime(0.08f);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return Realtime(0.4f);

            var grapple = _player.GetComponent<PlayerGrapple>();
            var released = false;
            void OnReleased() => released = true;
            grapple.Released += OnReleased;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.G));
            yield return Realtime(0.05f);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return Until(() => released, 3f, $"From {start} the line catches a ring and lets go above it.");
            grapple.Released -= OnReleased;

            yield return Until(() => _motor.Velocity.y <= 0.5f, 2f, "The pop tops out.");
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space));
            var deadline = Time.realtimeSinceStartup + 1.2f;
            while (_player.transform.position.y < overShelfY && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(towardShelf, Key.Space));
            yield return Realtime(0.35f);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return Until(() => _motor.IsGrounded, 3f, "Lands somewhere.");
            Time.maximumDeltaTime = 0.3333333f;
        }

        [UnityTest]
        public IEnumerator ARingPastItsEndLiftsTheHeroOntoTheRoyalGallery()
        {
            // From the approach's high step (Levels/capital.txt) the line reaches the ring hung in the hole
            // of the vault; the gallery's floor runs either side of the hole.
            var gallery = CapitalScene.Room("room_capital_royal_gallery");
            var floor = gallery.yMin + 1f;
            yield return ClimbByRing(CapitalScene.Ground("gallery-climb"), Key.D, Key.A, floor + 0.9f);
            Assert.That(_player.transform.position.y, Is.GreaterThan(floor),
                $"On the gallery, not back on the floor: at {_player.transform.position}.");
            Assert.That(_player.transform.position.x, Is.InRange(gallery.xMin + 1f, gallery.xMax - 1f));
        }

        [UnityTest]
        public IEnumerator FromTheCavesHighGroundARingReachesTheNest()
        {
            // The collapsed crypt's high ground; the ring hangs over it by the west wall, the nest
            // opens in that wall ten cells over the ground (Levels/tomb.txt).
            var nest = TombScene.Layout.WorldBounds(TombScene.Layout.Room("room_tomb_crypt_nest"));
            var floor = nest.yMin + 1f;
            yield return ClimbByRing(TombScene.Spot("grapple-hint") + new Vector2(0f, 0.85f), Key.A, Key.A, floor + 0.9f);
            Assert.That(_player.transform.position.y, Is.GreaterThan(floor),
                $"On the nest's floor: at {_player.transform.position}.");
            Assert.That(_player.transform.position.x, Is.LessThan(nest.xMax), "Inside the nest.");
        }

        [UnityTest]
        public IEnumerator FromTheCrownsHighGroundARingReachesTheHuntersHollow()
        {
            // The crown's high ground by its west wall; the ring hangs over it, the hollow opens in
            // that wall ten cells over the ground (Levels/forest.txt).
            var hollow = ForestScene.Room("room_forest_hunter_hollow");
            var floor = hollow.yMin + 1f;
            yield return ClimbByRing(ForestScene.Ground("grapple-hint"), Key.A, Key.A, floor + 0.9f);
            Assert.That(_player.transform.position.y, Is.GreaterThan(floor),
                $"In the hollow: at {_player.transform.position}.");
            Assert.That(_player.transform.position.x, Is.LessThan(hollow.xMax), "Inside the hollow.");
        }

        [UnityTest]
        public IEnumerator TheCacheAndTheHollowHoldAFlaskShardARelicAndAPurse()
        {
            var pickups = Object.FindObjectsByType<WorldPickup>(FindObjectsInactive.Include);
            var shard = pickups.Single(p => p.PersistentId == "grapple-tomb-flask");
            var talon = pickups.Single(p => p.PersistentId == "grapple-forest-talon");
            var purse = pickups.Single(p => p.PersistentId == "grapple-forest-purse");
            Assert.That(TombScene.Layout.WorldBounds(TombScene.Layout.Room("room_tomb_crypt_nest"))
                .Contains(shard.transform.position), Is.True, "The flask shard lies in the Tomb's nest.");
            Assert.That(ForestScene.InRoom(talon, "room_forest_hunter_hollow"), Is.True, "The relic lies in the hollow.");
            Assert.That(purse.Amount, Is.EqualTo(400));

            var shards = _player.GetComponent<FlaskShards>();
            var before = shards.Count;
            Assert.That(shard.TryCollect(_player), Is.True);
            yield return null;
            Assert.That(shards.Count, Is.EqualTo(before + 1), "The shard is one more flask.");
            Assert.That(talon.TryCollect(_player), Is.True);
            Assert.That(_progress.HasItem("hunter_talon"), Is.True);

            var rings = GrappleRing.All.Select(r => r.Anchor).ToList();
            Assert.That(rings.Any(r => r.x < -60f && r.y > 10f), Is.True, "A ring over the collapsed crypt.");
            Assert.That(rings.Any(r => ForestScene.Room("room_forest_crown").Contains(r)), Is.True, "A ring over the crown.");
        }
    }
}
