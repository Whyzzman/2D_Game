using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// Puts the Forest built from its text into the world (item 4 of the 15-zone list): the old
    /// Forest's scene is emptied, the world put together, the Forest built from
    /// <c>Levels/forest.txt</c> with its lights, the layers that place things in every zone rebuilt
    /// over it (exploration, inscriptions, people), the world split again, and the scene passes that
    /// dress or light the level run after. Run after <c>ContentBuilder.Build</c>, which writes the
    /// graph the split reads. Safe to run again.
    /// Run: -executeMethod ForgottenKingdom.EditorTools.SceneForest.Apply
    /// </summary>
    public static class SceneForest
    {
        [MenuItem("Forgotten Kingdom/Scene/Apply Forest From Text")]
        public static void Apply()
        {
            var path = WorldScenes.ZoneScenePath(WorldScenes.ForestSceneName);
            var forestScene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            foreach (var root in forestScene.GetRootGameObjects())
            {
                Object.DestroyImmediate(root);
            }

            EditorSceneManager.SaveScene(forestScene);

            WorldScenes.Edit(() =>
            {
                var level = GameObject.Find("Level");
                if (level == null)
                {
                    Debug.LogError("No Level to build the Forest in.");
                    return;
                }

                ContentBuilder.BuildForest(level.transform);
                ContentBuilder.LightForest(level.transform.Find(ContentBuilder.ForestRootName));
                var respawner = Object.FindAnyObjectByType<PlayerRespawner>();
                if (respawner != null)
                {
                    var serialized = new SerializedObject(respawner);
                    serialized.FindProperty("_worldFloorY").floatValue = ContentBuilder.WorldFloorY;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }

                ContentBuilder.BuildExploration();
                SceneNpcs.Build();
                SceneCharacterLight.Build();
            });
            SceneForestLook.Apply();
            SceneAmbience.Apply();
            SceneScreenGrade.Apply();
            SceneMusic.Apply();
            Debug.Log("Forest from text applied to the world.");
        }
    }
}
