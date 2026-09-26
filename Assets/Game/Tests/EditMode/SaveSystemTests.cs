using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The save file on its own: what a new game contains, what survives a round trip, and how a
    /// damaged or outdated file is handled. These touch the real save path, so they clean up.
    /// </summary>
    public sealed class SaveSystemTests
    {
        [SetUp]
        public void SetUp()
        {
            SaveSystem.Delete();
        }

        [TearDown]
        public void TearDown()
        {
            SaveSystem.Delete();
        }

        [Test]
        public void ANewGameStartsWithNothingBeatenAndDashAlreadyOwned()
        {
            var data = GameProgressData.CreateNew();

            Assert.That(data.version, Is.EqualTo(GameProgressData.CurrentVersion));
            Assert.That(data.checkpointId, Is.Empty);
            Assert.That(data.activatedCheckpoints, Is.Empty);
            Assert.That(data.defeatedBosses, Is.Empty);
            Assert.That(data.acquiredRewards, Is.Empty);
            Assert.That(data.player.level, Is.EqualTo(1));
            Assert.That(data.player.experience, Is.Zero);
            Assert.That(data.player.currency, Is.Zero);
            Assert.That(data.player.unlockedAbilities, Does.Contain(AbilityIds.Dash),
                "Dash already works in the game, so a new save must not take it away.");
            Assert.That(data.createdAtUtc, Is.Not.Empty);
        }

        [Test]
        public void ProgressSurvivesARoundTripUnchanged()
        {
            var data = GameProgressData.CreateNew();
            data.checkpointId = "tomb-altar-02";
            data.respawnPosition = new Vector2(25f, 0.8f);
            data.MarkCheckpointActivated("tomb-altar-01");
            data.MarkCheckpointActivated("tomb-altar-02");
            data.MarkBossDefeated("guardian-of-the-tomb");
            data.GrantReward("guardian-core");
            data.player.currency = 42;
            data.player.maxHealth = 120f;

            Assert.That(SaveSystem.Write(data), Is.True);
            var loaded = SaveSystem.Read();

            Assert.That(loaded.checkpointId, Is.EqualTo("tomb-altar-02"));
            Assert.That(loaded.respawnPosition, Is.EqualTo(new Vector2(25f, 0.8f)));
            Assert.That(loaded.activatedCheckpoints, Is.EquivalentTo(
                new[] { "tomb-altar-01", "tomb-altar-02" }));
            Assert.That(loaded.IsBossDefeated("guardian-of-the-tomb"), Is.True);
            Assert.That(loaded.HasReward("guardian-core"), Is.True);
            Assert.That(loaded.player.currency, Is.EqualTo(42));
            Assert.That(loaded.player.maxHealth, Is.EqualTo(120f));
            Assert.That(loaded.lastSavedAtUtc, Is.Not.Empty);
        }

        [Test]
        public void NoTransientCombatStateEverReachesTheFile()
        {
            var data = GameProgressData.CreateNew();
            data.MarkBossDefeated("guardian-of-the-tomb");
            SaveSystem.Write(data);

            var json = File.ReadAllText(SaveSystem.FilePath);
            foreach (var forbidden in new[]
                     {
                         "isBlocking", "isDashing", "combo", "stagger", "aggro", "currentHealth",
                         "currentStamina", "invulnerab"
                     })
            {
                Assert.That(json.ToLowerInvariant(), Does.Not.Contain(forbidden.ToLowerInvariant()),
                    $"'{forbidden}' is combat state and must never be persisted.");
            }
        }

        [Test]
        public void RewardsAndBossesAreNeverStoredTwice()
        {
            var data = GameProgressData.CreateNew();
            for (var i = 0; i < 3; i++)
            {
                data.MarkBossDefeated("guardian-of-the-tomb");
                data.GrantReward("guardian-core");
                data.MarkCheckpointActivated("tomb-altar-01");
            }

            Assert.That(data.defeatedBosses.Count, Is.EqualTo(1));
            Assert.That(data.acquiredRewards.Count, Is.EqualTo(1));
            Assert.That(data.activatedCheckpoints.Count, Is.EqualTo(1));
        }

        [Test]
        public void DuplicatesAlreadyInAFileAreCleanedUpOnLoad()
        {
            var data = GameProgressData.CreateNew();
            data.defeatedBosses.Add("guardian-of-the-tomb");
            data.defeatedBosses.Add("guardian-of-the-tomb");
            data.acquiredRewards.Add("guardian-core");
            data.acquiredRewards.Add("guardian-core");
            data.acquiredRewards.Add(string.Empty);
            File.WriteAllText(SaveSystem.FilePath, JsonUtility.ToJson(data, true));

            var loaded = SaveSystem.Read();
            Assert.That(loaded.defeatedBosses.Count, Is.EqualTo(1));
            Assert.That(loaded.acquiredRewards.Count, Is.EqualTo(1));
        }

        [Test]
        public void BrokenValuesAreClampedInsteadOfCrashing()
        {
            var data = GameProgressData.CreateNew();
            data.player.level = -5;
            data.player.currency = -100;
            data.player.maxHealth = 0f;
            data.player.maxStamina = -20f;
            data.totalPlayTimeSeconds = -3f;
            File.WriteAllText(SaveSystem.FilePath, JsonUtility.ToJson(data, true));

            var loaded = SaveSystem.Read();
            Assert.That(loaded.player.level, Is.EqualTo(1));
            Assert.That(loaded.player.currency, Is.Zero);
            Assert.That(loaded.player.maxHealth, Is.GreaterThan(0f));
            Assert.That(loaded.player.maxStamina, Is.GreaterThan(0f));
            Assert.That(loaded.totalPlayTimeSeconds, Is.Zero);
        }

        [Test]
        public void AnOlderFileLoadsAndIsBroughtUpToDate()
        {
            // A version one file: no player block, no timestamps, no ability list.
            File.WriteAllText(SaveSystem.FilePath,
                "{\"version\":1,\"checkpointId\":\"tomb-altar-01\","
                + "\"respawnPosition\":{\"x\":-13.8,\"y\":0.8},"
                + "\"defeatedBosses\":[\"guardian-of-the-tomb\"]}");

            var loaded = SaveSystem.Read();

            Assert.That(loaded.version, Is.EqualTo(GameProgressData.CurrentVersion));
            Assert.That(loaded.checkpointId, Is.EqualTo("tomb-altar-01"));
            Assert.That(loaded.IsBossDefeated("guardian-of-the-tomb"), Is.True);
            Assert.That(loaded.player, Is.Not.Null, "Missing sections default instead of failing.");
            Assert.That(loaded.player.unlockedAbilities, Does.Contain(AbilityIds.Dash));
        }

        [Test]
        public void ADamagedSaveFallsBackToTheBackupInsteadOfCrashing()
        {
            var data = GameProgressData.CreateNew();
            data.MarkBossDefeated("guardian-of-the-tomb");
            SaveSystem.Write(data);

            // Write again so the good file becomes the backup, then wreck the main one.
            data.GrantReward("guardian-core");
            SaveSystem.Write(data);
            File.WriteAllText(SaveSystem.FilePath, "{ this is not json ");

            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            var loaded = SaveSystem.Read();
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.IsBossDefeated("guardian-of-the-tomb"), Is.True,
                "The backup has to carry the progress through.");
            Assert.That(File.Exists(SaveSystem.FilePath), Is.True,
                "A damaged file is never silently thrown away before recovery is tried.");
        }

        [Test]
        public void AnUnreadableSaveWithNoBackupStillStartsAFreshGame()
        {
            File.WriteAllText(SaveSystem.FilePath, "not json at all");

            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            var loaded = SaveSystem.Read();
            var hasValid = SaveSystem.HasValidSave();
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.defeatedBosses, Is.Empty);
            Assert.That(hasValid, Is.False, "Continue must not be offered for a broken file.");
        }

        [Test]
        public void HasValidSaveIsFalseUntilSomethingIsWritten()
        {
            Assert.That(SaveSystem.HasValidSave(), Is.False);
            SaveSystem.Write(GameProgressData.CreateNew());
            Assert.That(SaveSystem.HasValidSave(), Is.True);
            SaveSystem.Delete();
            Assert.That(SaveSystem.HasValidSave(), Is.False);
            Assert.That(File.Exists(SaveSystem.BackupPath), Is.False,
                "Deleting progress clears the backup too.");
        }

        [Test]
        public void UnknownIdsFromAFutureBuildAreIgnoredNotFatal()
        {
            var data = GameProgressData.CreateNew();
            data.MarkBossDefeated("boss-that-no-longer-exists");
            data.GrantReward("reward-from-another-build");
            SaveSystem.Write(data);

            var loaded = SaveSystem.Read();
            Assert.That(loaded.IsBossDefeated("guardian-of-the-tomb"), Is.False);
            Assert.That(loaded.HasReward("guardian-core"), Is.False);
            Assert.That(loaded.defeatedBosses, Does.Contain("boss-that-no-longer-exists"),
                "Unknown ids are kept, not dropped, so an older build cannot erase progress.");
        }
    }
}
