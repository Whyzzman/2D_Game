using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The HUD and the title screen are part of the real game, not an overlay for tests: they
    /// have to be in the scenes, show what the player's state is, and react to a death, a boss
    /// and a new game.
    /// </summary>
    public sealed class HudAndMenuTests
    {
        [TearDown]
        public void TearDown()
        {
            SaveSystem.Delete();
            Time.timeScale = 1f;
        }

        private static IEnumerator LoadGame()
        {
            SaveSystem.Delete();
            yield return SceneManager.LoadSceneAsync(MainMenuController.GameSceneName);
            yield return new WaitForSeconds(0.3f);
        }

        [UnityTest]
        public IEnumerator TheGameSceneHasARealHudWiredToThePlayer()
        {
            yield return LoadGame();
            var hud = Object.FindAnyObjectByType<HudController>();
            Assert.That(hud, Is.Not.Null, "The game has to have a HUD.");
            var root = hud.GetComponent<UIDocument>().rootVisualElement;
            Assert.That(root.Q("hp-fill"), Is.Not.Null);
            Assert.That(root.Q("boss"), Is.Not.Null);

            var player = Object.FindAnyObjectByType<PlayerCharacter>();
            player.Actor.Health.SetCurrent(player.Health.Max * 0.5f);
            yield return new WaitForSeconds(0.1f);
            var width = root.Q("hp-fill").style.width.value;
            Assert.That(width.value, Is.EqualTo(49.5f).Within(1f), "Half health, half the bar.");
            Assert.That(root.Q<Label>("hp-value").text, Does.StartWith(
                Mathf.CeilToInt(player.Health.Current).ToString()));
            Assert.That(hud.IsBossBarVisible, Is.False, "No boss bar outside a fight.");
        }

        [UnityTest]
        public IEnumerator TheHudRewritesALabelOnlyWhenItsValueChanges()
        {
            // Every text or style write makes UI Toolkit lay the panel out again; a HUD that
            // rebuilt the HP string each frame paid that cost for nothing.
            yield return LoadGame();
            var root = Object.FindAnyObjectByType<HudController>()
                .GetComponent<UIDocument>().rootVisualElement;
            var label = root.Q<Label>("hp-value");
            var shown = label.text;
            for (var i = 0; i < 10; i++)
            {
                yield return null;
            }

            Assert.That(ReferenceEquals(label.text, shown), Is.True,
                "Unchanged health must not produce a new HP string.");

            var player = Object.FindAnyObjectByType<PlayerCharacter>();
            player.Actor.Health.SetCurrent(player.Health.Max - 10f);
            yield return null;
            yield return null;
            Assert.That(label.text, Does.StartWith(
                Mathf.CeilToInt(player.Health.Current).ToString()));
        }

        [UnityTest]
        public IEnumerator TheBossBarAppearsWithTheFightAndNamesTheBoss()
        {
            yield return LoadGame();
            var hud = Object.FindAnyObjectByType<HudController>();
            var motor = Object.FindAnyObjectByType<PlayerMotor2D>();
            motor.ResetTo(new Vector2(43.5f, 0.85f));

            var deadline = Time.realtimeSinceStartup + 6f;
            while (!hud.IsBossBarVisible && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(hud.IsBossBarVisible, Is.True, "Walking into the arena raises the boss bar.");
            var root = hud.GetComponent<UIDocument>().rootVisualElement;
            Assert.That(root.Q<Label>("boss-name").text, Is.EqualTo(TombScene.Guardian().DisplayName));
        }

        [UnityTest]
        public IEnumerator DeathIsShownAndFadesRatherThanCutting()
        {
            yield return LoadGame();
            var hud = Object.FindAnyObjectByType<HudController>();
            Object.FindAnyObjectByType<PlayerRespawner>().Kill();

            yield return new WaitForSeconds(0.3f);
            Assert.That(hud.IsDeathShown, Is.True, "ВИ ЗАГИНУЛИ has to appear.");

            var deadline = Time.realtimeSinceStartup + 4f;
            while (hud.FadeAmount < 0.95f && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(hud.FadeAmount, Is.GreaterThan(0.95f), "The screen fades to black.");
        }

        [UnityTest]
        public IEnumerator NewGameOverASaveAsksBeforeErasingIt()
        {
            SaveSystem.Write(GameProgressData.CreateNew());
            Assume.That(SaveSystem.HasValidSave(), Is.True);
            yield return SceneManager.LoadSceneAsync(MainMenuController.SceneName);
            yield return null;
            var menu = Object.FindAnyObjectByType<MainMenuController>();

            menu.NewGame();
            Assert.That(menu.IsConfirmOpen, Is.True, "A save is at stake: ask first.");
            Assert.That(SaveSystem.HasValidSave(), Is.True, "Nothing is erased by asking.");

            menu.CancelNewGame();
            Assert.That(menu.IsConfirmOpen, Is.False);
            yield return new WaitForSeconds(1.2f);
            Assert.That(SaveSystem.HasValidSave(), Is.True, "Cancel keeps the save.");
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(MainMenuController.SceneName));

            menu.NewGame();
            menu.ConfirmNewGame();
            var deadline = Time.realtimeSinceStartup + 5f;
            while (SceneManager.GetActiveScene().name != MainMenuController.GameSceneName
                && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(MainMenuController.GameSceneName));
        }

        [UnityTest]
        public IEnumerator TheTitleScreenOffersNewGameAndDisablesContinueWithoutASave()
        {
            SaveSystem.Delete();
            yield return SceneManager.LoadSceneAsync(MainMenuController.SceneName);
            yield return null;

            var menu = Object.FindAnyObjectByType<MainMenuController>();
            Assert.That(menu, Is.Not.Null);
            Assert.That(menu.CanContinue, Is.False, "Nothing to continue.");
            Assert.That(Object.FindAnyObjectByType<UnityEngine.Rendering.Universal.Light2D>(),
                Is.Not.Null, "The title screen is a lit scene, not a flat colour.");

            menu.NewGame();
            var deadline = Time.realtimeSinceStartup + 5f;
            while (SceneManager.GetActiveScene().name != MainMenuController.GameSceneName
                && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(MainMenuController.GameSceneName));
        }

        [UnityTest]
        public IEnumerator NewGameFromTheMenuKeepsTheSettings()
        {
            GameSettings.Set(data => data.screenShake = 0.25f);
            GameSettings.Save();
            yield return SceneManager.LoadSceneAsync(MainMenuController.SceneName);
            yield return null;

            Object.FindAnyObjectByType<MainMenuController>().NewGame();
            yield return new WaitForSeconds(1.5f);

            GameSettings.Unload();
            Assert.That(GameSettings.Current.screenShake, Is.EqualTo(0.25f).Within(0.001f));
            GameSettings.ResetToDefaults();
            GameSettings.Save();
        }
    }
}
