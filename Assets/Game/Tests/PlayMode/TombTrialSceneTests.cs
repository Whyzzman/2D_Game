using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// End-to-end checks on the saved Tomb Trial level: wiring, checkpoint respawn, the boss arena
    /// and the progress that survives a restart.
    /// </summary>
    public sealed class TombTrialSceneTests
    {
        private PlayerCharacter _player;
        private PlayerMotor2D _motor;
        private PlayerRespawner _respawner;
        private GameProgress _progress;
        private BossEncounter _arena;
        private BossController _boss;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            yield return TestWorld.Load();
            _player = Object.FindAnyObjectByType<PlayerCharacter>();
            _motor = Object.FindAnyObjectByType<PlayerMotor2D>();
            _respawner = Object.FindAnyObjectByType<PlayerRespawner>();
            _progress = Object.FindAnyObjectByType<GameProgress>();
            _arena = TombScene.GuardianEncounter();
            _boss = TombScene.Guardian();
            yield return new WaitForFixedUpdate();
        }

        [TearDown]
        public void TearDown()
        {
            SaveSystem.Delete();
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator AFlaskHealsTheStandingPlayerAndAnAltarRefillsIt()
        {
            yield return new WaitForSeconds(0.3f);
            Assume.That(_motor.IsGrounded, Is.True);
            _player.Actor.Health.SetCurrent(40f);
            var flasks = _player.Flasks;
            var charges = flasks.Charges;

            Assert.That(_player.TryHeal(), Is.True);
            Assert.That(_player.Action, Is.EqualTo(PlayerAction.Heal), "The drink holds the player.");
            yield return new WaitForSeconds(flasks.DrinkTime + 0.2f);

            Assert.That(_player.Health.Current, Is.EqualTo(40f + flasks.HealAmount).Within(0.01f));
            Assert.That(flasks.Charges, Is.EqualTo(charges - 1));
            Assert.That(_player.Action, Is.EqualTo(PlayerAction.None), "Free again after the drink.");

            _player.ResetCombatState();
            Assert.That(flasks.Charges, Is.EqualTo(flasks.MaxCharges), "Resting fills the flasks.");
        }

        [UnityTest]
        public IEnumerator ADebouncedSaveDoesNotBringBackADeletedFile()
        {
            // The old session's debounced write used to land after a delete: a new game started
            // with a boss already beaten, and the arena test after the boss test failed by timing.
            _progress.MarkBossDefeated(_boss.BossId);
            _progress.Save();
            SaveSystem.Delete();
            var end = Time.realtimeSinceStartup + 0.6f;
            while (Time.realtimeSinceStartup < end)
            {
                yield return null;
            }

            Assert.That(SaveSystem.Exists, Is.False, "A save asked for before the delete came back.");
        }

        [Test]
        public void EveryMovingBodyIsInterpolated()
        {
            // Physics steps at 50 Hz; a body without interpolation stutters on a 60 or 120 Hz
            // screen next to the smoothly interpolated player and camera.
            var bodies = new System.Collections.Generic.List<Rigidbody2D>();
            foreach (var enemy in Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Include))
            {
                bodies.Add(enemy.GetComponent<Rigidbody2D>());
            }

            foreach (var boss in Object.FindObjectsByType<BossController>(FindObjectsInactive.Include))
            {
                bodies.Add(boss.GetComponent<Rigidbody2D>());
            }

            foreach (var dummy in Object.FindObjectsByType<TrainingDummy>(FindObjectsInactive.Include))
            {
                bodies.Add(dummy.GetComponent<Rigidbody2D>());
            }

            foreach (var attacker in Object.FindObjectsByType<TrainingAttacker>(FindObjectsInactive.Include))
            {
                bodies.Add(attacker.GetComponent<Rigidbody2D>());
            }

            bodies.Add(_player.GetComponent<Rigidbody2D>());
            Assert.That(bodies, Has.Count.GreaterThan(5));
            foreach (var body in bodies)
            {
                Assert.That(body, Is.Not.Null);
                Assert.That(body.interpolation, Is.EqualTo(RigidbodyInterpolation2D.Interpolate),
                    body.name);
            }
        }

        [UnityTest]
        public IEnumerator TheLevelContainsEveryPieceOfTheSlice()
        {
            Assert.That(_player, Is.Not.Null);
            Assert.That(_respawner, Is.Not.Null);
            Assert.That(_progress, Is.Not.Null);
            Assert.That(_arena, Is.Not.Null);
            Assert.That(_boss, Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<Checkpoint>(), Is.Not.Null);
            Assert.That(TombScene.TombEnemies().Length,
                Is.EqualTo(TombScene.Layout.Markers().Count(m => m.Kind == "enemy")),
                "Every enemy the Tomb's text places, and no other.");
            Assert.That(Object.FindAnyObjectByType<CameraFollow2D>(), Is.Not.Null);
            Assert.That(_player.Health.Current, Is.EqualTo(_player.Health.Max));

            yield return new WaitForSeconds(0.5f);
            Assert.That(_motor.IsGrounded, Is.True, "The player must start standing on the floor.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator AnAltarBecomesTheRespawnPointAndRefillsThePlayer()
        {
            var checkpoint = Object.FindAnyObjectByType<Checkpoint>();
            _player.Health.Remove(50f);
            checkpoint.Interact(_player);

            Assert.That(_respawner.ActiveCheckpointId, Is.EqualTo(checkpoint.CheckpointId));
            Assert.That(_player.Health.Current, Is.EqualTo(_player.Health.Max));

            // Saves are debounced, so the file appears a moment after the altar is lit.
            var deadline = Time.realtimeSinceStartup + 2f;
            while (!SaveSystem.Exists && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(SaveSystem.Exists, Is.True, "Resting stores progress.");
        }

        [UnityTest]
        public IEnumerator DeathReturnsThePlayerToTheLastAltarAtFullHealth()
        {
            var checkpoint = Object.FindAnyObjectByType<Checkpoint>();
            checkpoint.Interact(_player);
            var altar = checkpoint.transform.position;

            _motor.ResetTo(TombScene.Ground(10f));
            yield return new WaitForFixedUpdate();
            _player.Actor.ClearInvulnerability();
            _player.Actor.ApplyDamage(new DamageInfo(9999f, Vector2.right));
            Assert.That(_player.IsAlive, Is.False);
            Assert.That(_player.Action, Is.EqualTo(PlayerAction.Dead));

            yield return new WaitForSeconds(2f);
            Assert.That(_player.IsAlive, Is.True, "The player comes back after the death delay.");
            Assert.That(_player.Health.Current, Is.EqualTo(_player.Health.Max));
            Assert.That(_player.transform.position.x, Is.EqualTo(altar.x).Within(0.6f));
            Assert.That(_player.Action, Is.EqualTo(PlayerAction.None));
        }

        [UnityTest]
        public IEnumerator FallingOutOfTheLevelIsFatalAndSendsThePlayerBack()
        {
            // Under the Tomb's lowest pool, past the world's floor (-30).
            _motor.ResetTo(new Vector2(20f, -45f));
            yield return new WaitForFixedUpdate();

            // Out of bounds is a death, so it takes the death delay and the fade to come back.
            var deadline = Time.realtimeSinceStartup + 8f;
            while (Time.realtimeSinceStartup < deadline
                && !(_player.IsAlive && _player.transform.position.y > -25f))
            {
                yield return null;
            }

            Assert.That(_player.IsAlive, Is.True);
            Assert.That(_player.transform.position.y, Is.GreaterThan(-25f));
        }

        [UnityTest]
        public IEnumerator EnteringTheArenaWakesTheBossAndSealsTheExit()
        {
            Assert.That(_boss.State, Is.EqualTo(BossState.Dormant));
            var started = false;
            _arena.FightStarted += _ => started = true;

            _motor.ResetTo(TombScene.ArenaEntry());
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.That(started, Is.True, "Walking in must start the fight.");
            Assert.That(_arena.IsFightActive, Is.True);
            yield return new WaitForSeconds(0.2f);
            Assert.That(_boss.State, Is.Not.EqualTo(BossState.Dormant));
        }

        [UnityTest]
        public IEnumerator TheBossEntersItsSecondPhaseBelowHalfHealth()
        {
            _motor.ResetTo(TombScene.ArenaEntry());
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            var reportedPhase = 1;
            _boss.PhaseChanged += phase => reportedPhase = phase;
            _boss.Actor.Health.Remove(_boss.Actor.Health.Max * 0.55f);
            yield return new WaitForSeconds(5f);

            Assert.That(_boss.Phase, Is.EqualTo(2));
            Assert.That(reportedPhase, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator DefeatingTheBossOpensTheArenaAndIsWrittenToTheSave()
        {
            _motor.ResetTo(TombScene.ArenaEntry());
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            var ended = false;
            _arena.FightEnded += _ => ended = true;
            _boss.Actor.ClearInvulnerability();
            _boss.Actor.ApplyDamage(new DamageInfo(9999f, Vector2.left, attacker: _player.gameObject));
            yield return new WaitForFixedUpdate();

            Assert.That(ended, Is.True);
            Assert.That(_boss.State, Is.EqualTo(BossState.Dead));
            Assert.That(_arena.IsFightActive, Is.False);
            Assert.That(_progress.IsBossDefeated(_boss.BossId), Is.True);

            var reloaded = SaveSystem.Read();
            Assert.That(reloaded.IsBossDefeated(_boss.BossId), Is.True,
                "A restart must not bring the boss back.");
        }
    }
}
