using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace ForgottenKingdom
{
    /// <summary>
    /// Walks a built game through a fixed set of shots and saves each as a PNG, so "before" and
    /// "after" are pictures of the real game rather than of the scene hierarchy.
    ///
    /// Inert unless the game is started with <c>-screenshotTour "folder"</c>. It never touches the
    /// player's progress: the save files are copied aside first, the tour runs on a fresh game, and
    /// the originals are put back before the game quits.
    /// </summary>
    public sealed class ScreenshotTour : MonoBehaviour
    {
        private string _folder;
        private string _stash;
        private Keyboard _keyboard;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartIfRequested()
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, "-screenshotTour");
            if (index < 0 || index + 1 >= args.Length)
            {
                return;
            }

            var tour = new GameObject("ScreenshotTour").AddComponent<ScreenshotTour>();
            tour._folder = args[index + 1];
            DontDestroyOnLoad(tour.gameObject);
        }

        private IEnumerator Start()
        {
            Directory.CreateDirectory(_folder);
            Application.runInBackground = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            _keyboard = InputSystem.AddDevice<Keyboard>("TourKeyboard");
            StashSave();
            SaveSystem.Delete();
            // The build opens on the title screen; it is part of what the player sees first.
            if (SceneManager.GetActiveScene().name == MainMenuController.SceneName)
            {
                yield return SceneManager.LoadSceneAsync(MainMenuController.SceneName);
                yield return Wait(2.5f);
                yield return Shot("00_main_menu");
            }

            yield return SceneManager.LoadSceneAsync(MainMenuController.GameSceneName);
            yield return Wait(1.5f);

            var player = FindAnyObjectByType<PlayerCharacter>();
            var motor = player.GetComponent<PlayerMotor2D>();
            var respawner = FindAnyObjectByType<PlayerRespawner>();

            yield return Shot("01_player_tomb");

            // Sword combat against the first zombie, facing it.
            yield return Place(motor, new Vector2(12.4f, 0.85f));
            yield return Tap(Key.A, 0.08f);
            yield return Tap(Key.J, 0.05f);
            yield return Wait(0.35f);
            yield return Tap(Key.J, 0.05f);
            yield return Wait(0.12f);
            yield return Shot("02_sword_combat");
            yield return Wait(1.2f);

            // A lit altar.
            var altar = respawner.FindCheckpoint("tomb-altar-01");
            yield return Place(motor, (Vector2)altar.transform.position + new Vector2(1.4f, 0.1f));
            altar.Interact(player);
            yield return Wait(1.2f);
            yield return Shot("03_checkpoint");

            // Shield guard, guard up.
            yield return Place(motor, new Vector2(31.9f, 0.85f));
            yield return Tap(Key.D, 0.08f);
            yield return Wait(0.6f);
            yield return Tap(Key.J, 0.05f);
            yield return Wait(0.15f);
            yield return Shot("04_shield_guard");
            yield return Wait(1f);

            // The Guardian's arena: walking in wakes him.
            yield return Place(motor, new Vector2(43.5f, 0.85f));
            yield return Wait(2.8f);
            yield return Shot("05_guardian_arena");

            var boss = FindObjectsByType<BossController>(FindObjectsInactive.Exclude)
                .FirstOrDefault(b => b.BossId == "guardian-of-the-tomb");
            if (boss != null)
            {
                boss.Actor.Health.SetCurrent(boss.Actor.Health.Max * 0.45f);
                yield return Wait(4.5f);
                yield return Shot("06_boss_phase2");
            }

            // The forest, for the second zone's look.
            yield return Place(motor, new Vector2(95f, 0.85f));
            yield return Wait(1.5f);
            yield return Shot("07_forest");

            // Death: the HUD's fade and its words, not an instant teleport.
            respawner.Kill();
            yield return Wait(1.3f);
            yield return Shot("08_death");
            yield return Wait(3f);

            // Progress writes itself on quit; it goes first so the restored files stay restored.
            Destroy(FindAnyObjectByType<GameProgress>());
            yield return null;
            RestoreSave();
            InputSystem.RemoveDevice(_keyboard);
            Application.Quit();
        }

        private IEnumerator Place(PlayerMotor2D motor, Vector2 position)
        {
            motor.GetComponent<PlayerCombatStatus>()?.Clear();
            motor.ResetTo(position);
            yield return Wait(1f);
        }

        private IEnumerator Tap(Key key, float seconds)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(key));
            yield return Wait(seconds);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return null;
        }

        private IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(_folder, name + ".png"));
            yield return null;
            yield return null;
        }

        private static IEnumerator Wait(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
        }

        private void StashSave()
        {
            _stash = Path.Combine(Application.temporaryCachePath, "tour-save-stash");
            Directory.CreateDirectory(_stash);
            foreach (var file in new[] { SaveSystem.FilePath, SaveSystem.BackupPath })
            {
                var target = Path.Combine(_stash, Path.GetFileName(file));
                if (File.Exists(file))
                {
                    File.Copy(file, target, true);
                }
                else if (File.Exists(target))
                {
                    File.Delete(target);
                }
            }
        }

        private void RestoreSave()
        {
            SaveSystem.Delete();
            foreach (var file in new[] { SaveSystem.FilePath, SaveSystem.BackupPath })
            {
                var source = Path.Combine(_stash, Path.GetFileName(file));
                if (File.Exists(source))
                {
                    File.Copy(source, file, true);
                }
            }
        }
    }
}
