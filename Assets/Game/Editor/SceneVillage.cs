using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// Puts the Village built from its text into the world (item 5 of the 15-zone list): its scene
    /// is emptied (or made), the world put together, the Village built from <c>Levels/village.txt</c>
    /// with its lights and air, the layers that place things in every zone rebuilt over it
    /// (exploration, inscriptions, people), the world split again, and the scene passes that dress or
    /// light the level run after. Run after <c>ContentBuilder.Build</c>, which writes the graph the
    /// split reads. Safe to run again.
    /// Run: -executeMethod ForgottenKingdom.EditorTools.SceneVillage.Apply
    /// </summary>
    public static class SceneVillage
    {
        [MenuItem("Forgotten Kingdom/Scene/Apply Village From Text")]
        public static void Apply()
        {
            var path = WorldScenes.ZoneScenePath(WorldScenes.VillageSceneName);
            if (System.IO.File.Exists(path))
            {
                var villageScene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                foreach (var root in villageScene.GetRootGameObjects())
                {
                    Object.DestroyImmediate(root);
                }

                EditorSceneManager.SaveScene(villageScene);
            }

            WorldScenes.Edit(() =>
            {
                var level = GameObject.Find("Level");
                if (level == null)
                {
                    Debug.LogError("No Level to build the Village in.");
                    return;
                }

                ContentBuilder.BuildVillage(level.transform);
                ContentBuilder.LightVillage(level.transform.Find(ContentBuilder.VillageRootName));
                ContentBuilder.ConfigureVillageAtmosphere();
                ContentBuilder.BuildExploration();
                SceneNpcs.Build();
                SceneCharacterLight.Build();
            });
            SceneAmbience.Apply();
            SceneScreenGrade.Apply();
            SceneMusic.Apply();
            Debug.Log("Village from text applied to the world.");
        }
    }
}
