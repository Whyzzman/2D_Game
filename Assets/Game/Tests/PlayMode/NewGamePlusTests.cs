using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// «Нова гра+» (item 5 of the big list, stage C): offered on the title only after the ending,
    /// asked first; the hero keeps the level, gold and items and loses the movement abilities,
    /// the world starts again, and enemies take and deal half again as much.
    /// </summary>
    public sealed class NewGamePlusTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SaveSystem.Delete();
            yield return null;
        }

        private static GameProgressData Finished()
        {
            var data = GameProgressData.CreateNew();
            data.completed = true;
            data.deaths = 9;
            data.player.level = 12;
            data.player.currency = 500;
            data.player.unlockedAbilities.Add(AbilityIds.DoubleJump);
            data.MarkBossDefeated(TombScene.GuardianId);
            data.kills.Add(new KillCount { id = "ZombieSoldier", count = 30 });
            return data;
        }

        private static IEnumerator Menu()
        {
            yield return SceneManager.LoadSceneAsync(MainMenuController.SceneName);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheTitleOffersItOnlyAfterTheEnd()
        {
            var unfinished = Finished();
            unfinished.completed = false;
            SaveSystem.Write(unfinished);
            yield return Menu();
            var menu = Object.FindAnyObjectByType<MainMenuController>();
            Assert.That(menu.CanStartNewGamePlus, Is.False, "Not before the King falls.");
            menu.NewGamePlusRun();
            Assert.That(menu.IsConfirmOpen, Is.False);

            SaveSystem.Write(Finished());
            menu.Refresh();
            Assert.That(menu.CanStartNewGamePlus, Is.True);
            menu.NewGamePlusRun();
            Assert.That(menu.IsConfirmOpen, Is.True, "The finished run is at stake: ask first.");
            menu.CancelNewGame();
            Assert.That(SaveSystem.Read().completed, Is.True, "Cancel keeps it.");
        }

        [UnityTest]
        public IEnumerator TheHeroGoesOnAndTheWorldStartsAgain()
        {
            SaveSystem.Write(Finished());
            yield return Menu();
            var menu = Object.FindAnyObjectByType<MainMenuController>();
            menu.NewGamePlusRun();
            menu.ConfirmNewGame();
            var deadline = Time.realtimeSinceStartup + 6f;
            while (SceneManager.GetActiveScene().name != MainMenuController.GameSceneName
                && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            yield return TestWorld.WaitReady();
            var progress = Object.FindAnyObjectByType<GameProgress>();
            var data = progress.Data;
            Assert.That(data.newGamePlus, Is.EqualTo(1));
            Assert.That(NewGamePlus.Scale, Is.EqualTo(NewGamePlus.EnemyScale));
            Assert.That(data.player.level, Is.EqualTo(12), "The level stays.");
            Assert.That(data.player.currency, Is.EqualTo(500), "The gold stays.");
            Assert.That(data.inventory.HasItem(ItemIds.StarterWeapon), Is.True, "The items stay.");
            Assert.That(progress.HasAbility(AbilityIds.DoubleJump), Is.False, "Movement is learnt again.");
            Assert.That(progress.HasAbility(AbilityIds.Dash), Is.True);
            Assert.That(progress.IsBossDefeated(TombScene.GuardianId), Is.False, "The bosses stand again.");
            Assert.That(data.kills, Is.Empty, "The bestiary starts again.");
            Assert.That(data.completed, Is.False);
            Assert.That(data.deaths, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator EnemiesTakeAndDealHalfAgainAsMuch()
        {
            yield return TestWorld.Load();
            var (health, loss) = Measure();
            Assert.That(NewGamePlus.Scale, Is.EqualTo(1f));

            var plus = GameProgressData.CreateNew();
            plus.newGamePlus = 1;
            SaveSystem.Write(plus);
            yield return TestWorld.Load();
            var (tougher, harder) = Measure();
            Assert.That(tougher, Is.EqualTo(health * NewGamePlus.EnemyScale).Within(0.01f), "Enemy health.");
            Assert.That(harder, Is.EqualTo(loss * NewGamePlus.EnemyScale).Within(0.01f), "Damage to the hero.");
        }

        /// <summary>A zombie's full health, and what a blow of 10 takes from the hero.</summary>
        private static (float, float) Measure()
        {
            var zombie = Object.FindObjectsByType<EnemyController>()
                .First(e => e.Definition != null && e.Definition.name == "ZombieSoldier");
            var player = Object.FindAnyObjectByType<PlayerCharacter>();
            player.Actor.ClearInvulnerability();
            var before = player.Actor.Health.Current;
            player.Actor.ApplyDamage(new DamageInfo(10f, Vector2.left, attacker: zombie.gameObject));
            return (zombie.Actor.Health.Max, before - player.Actor.Health.Current);
        }
    }
}
