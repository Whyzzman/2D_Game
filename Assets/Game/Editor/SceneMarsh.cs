using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// Puts the Plague Marsh built from its text into the world (the fifth level, MARSH.md): its scene is
    /// emptied (or made), the world put together, the marsh built from <c>Levels/marsh.txt</c> with its
    /// lights and air, the layers that place things in every zone rebuilt over it, the world split again,
    /// and the scene passes run after. Run after <c>ContentBuilder.Build</c>. Safe to run again.
    /// Run: -executeMethod ForgottenKingdom.EditorTools.SceneMarsh.Apply
    /// </summary>
    public static class SceneMarsh
    {
        [MenuItem("Forgotten Kingdom/Scene/Apply Marsh From Text")]
        public static void Apply()
        {
            var path = WorldScenes.ZoneScenePath(WorldScenes.MarshSceneName);
            if (System.IO.File.Exists(path))
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                foreach (var root in scene.GetRootGameObjects())
                {
                    Object.DestroyImmediate(root);
                }

                EditorSceneManager.SaveScene(scene);
            }
            else
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, path);
            }

            // The zone scenes are loaded by name, so the marsh goes into the build after the capital.
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(s => s.path == path))
            {
                var at = scenes.FindIndex(s => s.path.EndsWith(WorldScenes.CapitalSceneName + ".unity")) + 1;
                scenes.Insert(at > 0 ? at : scenes.Count, new EditorBuildSettingsScene(path, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }

            WorldScenes.Edit(() =>
            {
                var level = GameObject.Find("Level");
                if (level == null)
                {
                    Debug.LogError("No Level to build the Marsh in.");
                    return;
                }

                ContentBuilder.BuildMarsh(level.transform);
                ContentBuilder.LightMarsh(level.transform.Find(ContentBuilder.MarshRootName));
                ContentBuilder.ConfigureMarshAtmosphere();
                ContentBuilder.BuildExploration();
                SceneNpcs.Build();
                SceneCharacterLight.Build();
            });
            SceneAmbience.Apply();
            SceneScreenGrade.Apply();
            SceneMusic.Apply();
            Debug.Log("Marsh from text applied to the world.");
        }
    }
}
