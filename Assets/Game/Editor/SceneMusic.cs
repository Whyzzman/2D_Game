using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// Puts the procedural score (item 9, the user's choices of 2026-09-28) into the scenes: each
    /// region's track and fight layer on the level's <see cref="MusicDirector"/>, one boss track
    /// both arenas share with its phase-two drums, and a director of its own on the title screen.
    ///
    /// Scenes are the source of truth, so this is a scene edit, run once and again whenever the
    /// recipe changes; it touches only the directors. The content build uses the same recipe for a
    /// scene it builds from scratch.
    /// Run: -executeMethod ForgottenKingdom.EditorTools.SceneMusic.Apply
    /// </summary>
    public static class SceneMusic
    {
        private const string LevelPath = "Assets/Game/Scenes/TombTrial.unity";
        private const string MenuPath = "Assets/Game/Scenes/MainMenu.unity";
        private const string TombZone = "zone_forgotten_tomb";
        private const string ForestZone = "zone_withered_forest";
        private const string CapitalZone = "zone_ashen_capital";
        private const string MenuMusicName = "Music";

        [MenuItem("Forgotten Kingdom/Scene/Apply Music")]
        public static void Apply()
        {
            var score = PlaceholderMusicArt.Generate();

            var level = EditorSceneManager.OpenScene(LevelPath, OpenSceneMode.Single);
            var director = Object.FindAnyObjectByType<MusicDirector>();
            if (director == null)
            {
                Debug.LogError("TombTrial has no MusicDirector to give the score to.");
                return;
            }

            ConfigureLevel(director, score);
            EditorSceneManager.MarkSceneDirty(level);
            EditorSceneManager.SaveScene(level);

            var menu = EditorSceneManager.OpenScene(MenuPath, OpenSceneMode.Single);
            AddMenuMusic(score);
            EditorSceneManager.MarkSceneDirty(menu);
            EditorSceneManager.SaveScene(menu);
            Debug.Log("Music applied to TombTrial and MainMenu.");
        }

        /// <summary>Three regions, one fight: the bosses share a track and its phase-two drums.</summary>
        public static void ConfigureLevel(MusicDirector director, PlaceholderMusicArt.Score score)
        {
            var serialized = new SerializedObject(director);
            var palettes = serialized.FindProperty("_palettes");
            palettes.arraySize = 3;
            Palette(palettes.GetArrayElementAtIndex(0), TombZone, score.Tomb, score.TombPulse, score);
            Palette(palettes.GetArrayElementAtIndex(1), ForestZone, score.Forest, score.ForestPulse, score);
            Palette(palettes.GetArrayElementAtIndex(2), CapitalZone, score.Capital, score.CapitalPulse, score);
            serialized.FindProperty("_mainMenu").objectReferenceValue = score.Menu;
            serialized.FindProperty("_startState").enumValueIndex = (int)MusicState.Exploration;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Palette(SerializedProperty palette, string zone, AudioClip exploration,
            AudioClip pulse, PlaceholderMusicArt.Score score)
        {
            palette.FindPropertyRelative("_zoneId").stringValue = zone;
            palette.FindPropertyRelative("_exploration").objectReferenceValue = exploration;
            palette.FindPropertyRelative("_combatLayer").objectReferenceValue = pulse;
            // No separate intro: the fight track starts as the arena seals.
            palette.FindPropertyRelative("_bossIntro").objectReferenceValue = null;
            palette.FindPropertyRelative("_bossPhaseOne").objectReferenceValue = score.Boss;
            // The same clip for both phases, so the turn adds the drums instead of restarting.
            palette.FindPropertyRelative("_bossPhaseTwo").objectReferenceValue = score.Boss;
            palette.FindPropertyRelative("_bossLayer").objectReferenceValue = score.BossDrums;
            palette.FindPropertyRelative("_victory").objectReferenceValue = score.Victory;
        }

        /// <summary>The title screen's own director, which plays nothing but the menu theme.</summary>
        public static void AddMenuMusic(PlaceholderMusicArt.Score score)
        {
            var old = GameObject.Find(MenuMusicName);
            if (old != null)
            {
                Object.DestroyImmediate(old);
            }

            var host = new GameObject(MenuMusicName);
            var director = host.AddComponent<MusicDirector>();
            var serialized = new SerializedObject(director);
            serialized.FindProperty("_palettes").arraySize = 0;
            serialized.FindProperty("_mainMenu").objectReferenceValue = score.Menu;
            serialized.FindProperty("_startState").enumValueIndex = (int)MusicState.MainMenu;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
