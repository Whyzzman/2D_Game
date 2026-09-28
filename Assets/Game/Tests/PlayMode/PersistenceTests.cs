using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// Progress across a restart. Reloading the scene makes every system read the file again from
    /// scratch, which is the same path a relaunched game takes.
    /// </summary>
    public sealed class PersistenceTests
    {
        private PlayerCharacter _player;
        private PlayerMotor2D _motor;
        private PlayerRespawner _respawner;
        private GameProgress _progress;
        private BossEncounter _encounter;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            yield return LoadLevel();
        }

        [TearDown]
        public void TearDown()
        {
            SaveSystem.Delete();
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator AFreshLevelHasNoSaveAndNoProgress()
        {
            Assert.That(_progress.HasSaveOnDisk, Is.False);
            Assert.That(_progress.Data.checkpointId, Is.Empty);
            Assert.That(_progress.DefeatedBosses, Is.Empty);
            Assert.That(_progress.AcquiredRewards, Is.Empty);
            Assert.That(_progress.Player.unlockedAbilities, Does.Contain(AbilityIds.Dash));
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator AnAltarSurvivesARestart()
        {
            var altar = Altars().Last();
            yield return LightAltar(altar);
            Assert.That(SaveSystem.HasValidSave(), Is.True, "Lighting an altar writes the save.");

            yield return LoadLevel();

            Assert.That(_progress.Data.checkpointId, Is.EqualTo(altar.CheckpointId));
            Assert.That(_respawner.ActiveCheckpointId, Is.EqualTo(altar.CheckpointId));
            var restored = Altars().First(candidate => candidate.CheckpointId == altar.CheckpointId);
            Assert.That(restored.IsActivated, Is.True, "A lit altar stays lit.");
            Assert.That(restored.IsCurrent, Is.True);
            Assert.That(_player.transform.position.x,
                Is.EqualTo(restored.RespawnPosition.x).Within(0.6f),
                "Continue puts the player back at the last altar.");
            Assert.That(_player.Health.Current, Is.EqualTo(_player.Health.Max),
                "A load always starts rested.");
            Assert.That(_player.Stamina.Current, Is.EqualTo(_player.Stamina.Max));
        }

        [UnityTest]
        public IEnumerator ABeatenBossStaysBeatenAndItsRewardIsNotGivenTwice()
        {
            var altar = Altars().Last();
            yield return LightAltar(altar);
            yield return EnterArena();
            var rewardId = _encounter.RewardId;
            yield return KillBoss();
            Assert.That(_progress.HasReward(rewardId), Is.True);

            yield return LoadLevel();

            Assert.That(_progress.IsBossDefeated("guardian-of-the-tomb"), Is.True);
            Assert.That(_encounter.IsBossDefeated, Is.True);
            Assert.That(_encounter.Boss.gameObject.activeSelf, Is.False,
                "A beaten Guardian is not in the room any more.");
            Assert.That(_encounter.IsArenaSealed, Is.False, "The arena stays open.");
            Assert.That(_progress.AcquiredRewards.Count, Is.EqualTo(1));

            // Walking back in must not restart anything or hand the core over again.
            var granted = 0;
            _encounter.RewardGranted += (_, _) => granted++;
            yield return EnterArena();
            Assert.That(_encounter.IsFightActive, Is.False);
            Assert.That(granted, Is.Zero);
            Assert.That(_progress.AcquiredRewards.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator QuittingMidFightLosesNothingButAlsoBanksNothing()
        {
            var altar = Altars().Last();
            yield return LightAltar(altar);
            yield return EnterArena();
            _encounter.Boss.Actor.Health.SetCurrent(_encounter.Boss.Actor.Health.Max * 0.3f);
            yield return new WaitForFixedUpdate();

            // No save happens during a fight, so the reload sees an untouched Guardian.
            yield return LoadLevel();

            Assert.That(_encounter.IsBossDefeated, Is.False);
            Assert.That(_encounter.Boss.gameObject.activeSelf, Is.True);
            Assert.That(_encounter.Boss.State, Is.EqualTo(BossState.Dormant));
            Assert.That(_encounter.Boss.Phase, Is.EqualTo(1));
            Assert.That(_encounter.Boss.Actor.Health.Current,
                Is.EqualTo(_encounter.Boss.Actor.Health.Max));
            Assert.That(_respawner.ActiveCheckpointId, Is.EqualTo(altar.CheckpointId));
        }

        [UnityTest]
        public IEnumerator OrdinaryEnemiesAreNeverSavedAsDead()
        {
            yield return LightAltar(Altars().First());
            var zombie = Object.FindObjectsByType<EnemyController>()
                .First(enemy => enemy.name.StartsWith("ZombieSoldier"));
            zombie.Actor.ClearInvulnerability();
            zombie.Actor.ApplyDamage(new DamageInfo(9999f, Vector2.right));
            yield return new WaitForFixedUpdate();
            Assert.That(zombie.State, Is.EqualTo(EnemyState.Dead));
            _progress.SaveNow();

            yield return LoadLevel();

            var restored = Object.FindObjectsByType<EnemyController>()
                .First(enemy => enemy.name.StartsWith("ZombieSoldier"));
            Assert.That(restored.IsAlive, Is.True, "Trash mobs always come back.");
            Assert.That(restored.Actor.Health.Current, Is.EqualTo(restored.Actor.Health.Max));
        }

        [UnityTest]
        public IEnumerator AnUnknownCheckpointFallsBackToTheLevelSpawn()
        {
            var data = GameProgressData.CreateNew();
            data.checkpointId = "altar-from-a-level-that-does-not-exist";
            data.respawnPosition = new Vector2(999f, 999f);
            SaveSystem.Write(data);

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(
                "Saved checkpoint .* is not in this level"));
            yield return LoadLevel();

            Assert.That(_respawner.ActiveCheckpointId, Is.Empty);
            Assert.That(_progress.Data.checkpointId, Is.Empty,
                "The stale id is dropped so the next save is clean.");
            Assert.That(_player.transform.position.x, Is.LessThan(100f),
                "The player is not dropped at a position from another level.");
        }

        [UnityTest]
        public IEnumerator NewGameWipesTheBeatenBossAndTheReward()
        {
            yield return LightAltar(Altars().Last());
            yield return EnterArena();
            yield return KillBoss();
            Assert.That(_progress.IsBossDefeated("guardian-of-the-tomb"), Is.True);

            _respawner.StartNewGame();
            yield return new WaitForFixedUpdate();

            Assert.That(_progress.IsBossDefeated("guardian-of-the-tomb"), Is.False);
            Assert.That(_progress.AcquiredRewards, Is.Empty);
            Assert.That(_progress.ActivatedCheckpoints, Is.Empty);
            Assert.That(_progress.Data.checkpointId, Is.Empty);
            Assert.That(_respawner.ActiveCheckpointId, Is.Empty);
            Assert.That(_progress.Player.level, Is.EqualTo(1));
            Assert.That(_progress.Player.unlockedAbilities, Does.Contain(AbilityIds.Dash),
                "A new game must not take dash away from a build that already has it.");

            yield return LoadLevel();
            Assert.That(_encounter.IsBossDefeated, Is.False, "The Guardian is back for a new run.");
            Assert.That(_encounter.Boss.gameObject.activeSelf, Is.True);
        }

        [UnityTest]
        public IEnumerator SavedProgressionSizesTheBarsBeforePlayBegins()
        {
            // The bars are worked out from the attributes in the file, never stored: a saved
            // maximum could drift away from what the attributes say and there would be no way to
            // tell which one was right.
            var data = GameProgressData.CreateNew();
            data.player.stats.vitality = 12;
            data.player.stats.endurance = 10;
            SaveSystem.Write(data);

            yield return LoadLevel();

            var curve = Object.FindAnyObjectByType<PlayerProgressionService>().Curve;
            Assert.That(_player.Health.Max, Is.EqualTo(curve.HealthFor(12)).Within(0.01f));
            Assert.That(_player.Health.Current, Is.EqualTo(_player.Health.Max).Within(0.01f),
                "A load still starts rested.");
            Assert.That(_player.Stamina.Max, Is.EqualTo(curve.StaminaFor(10)).Within(0.01f));
            Assert.That(_player.Stamina.Current, Is.EqualTo(_player.Stamina.Max).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator SeveralProgressEventsAtOnceProduceOneConsistentFile()
        {
            yield return LightAltar(Altars().Last());
            yield return EnterArena();
            yield return KillBoss();
            yield return new WaitForSecondsRealtime(0.5f);

            var written = SaveSystem.Read();
            Assert.That(written.IsBossDefeated("guardian-of-the-tomb"), Is.True);
            Assert.That(written.HasReward(_encounter.RewardId), Is.True,
                "The kill and its reward land in the same file, never one without the other.");
            Assert.That(written.checkpointId, Is.Not.Empty);
            Assert.That(File.Exists(SaveSystem.FilePath), Is.True);
        }

        // ---------- helpers ----------

        private IEnumerator LoadLevel()
        {
            yield return TestWorld.Load();
            _player = Object.FindAnyObjectByType<PlayerCharacter>();
            _motor = _player.GetComponent<PlayerMotor2D>();
            _respawner = Object.FindAnyObjectByType<PlayerRespawner>();
            _progress = Object.FindAnyObjectByType<GameProgress>();
            _encounter = TombScene.GuardianEncounter();
            yield return new WaitForFixedUpdate();
        }

        private Checkpoint[] Altars()
        {
            // The Tomb's altars; these tests are about the Guardian's side of the world.
            return TombScene.TombAltars();
        }

        private IEnumerator LightAltar(Checkpoint altar)
        {
            _player.GetComponent<PlayerCombatStatus>().Clear();
            _motor.ResetTo(new Vector2(altar.transform.position.x, altar.transform.position.y + 0.1f));
            yield return new WaitForFixedUpdate();
            altar.Interact(_player);
            _progress.SaveNow();
            yield return new WaitForFixedUpdate();
        }

        private IEnumerator EnterArena()
        {
            _motor.ResetTo(new Vector2(42f, 1f));
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
        }

        private IEnumerator KillBoss()
        {
            var boss = _encounter.Boss;
            boss.Actor.ClearInvulnerability();
            boss.Actor.ApplyDamage(new DamageInfo(float.MaxValue, Vector2.left,
                isUnblockable: true, attacker: _player.gameObject));
            yield return new WaitForFixedUpdate();
        }
    }
}
