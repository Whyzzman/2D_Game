using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// Puts the Tomb built from its text into the world (item 3 of the 15-zone list): the old
    /// Tomb's scene is emptied, the world put together, the Tomb built from <c>Levels/tomb.txt</c>
    /// with its lights, the layers that place things in every zone rebuilt over it (exploration,
    /// inscriptions, people), the world split again, and the scene passes that dress or light the
    /// level run after. Run after <c>ContentBuilder.Build</c>, which writes the graph the split
    /// reads. Safe to run again.
    /// Run: -executeMethod ForgottenKingdom.EditorTools.SceneTomb.Apply
    /// </summary>
    public static class SceneTomb
    {
        [MenuItem("Forgotten Kingdom/Scene/Apply Tomb From Text")]
        public static void Apply()
        {
            var path = WorldScenes.ZoneScenePath(WorldScenes.TombSceneName);
            var tombScene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            foreach (var root in tombScene.GetRootGameObjects())
            {
                Object.DestroyImmediate(root);
            }

            EditorSceneManager.SaveScene(tombScene);

            WorldScenes.Edit(() =>
            {
                var level = GameObject.Find("Level");
                if (level == null)
                {
                    Debug.LogError("No Level to build the Tomb in.");
                    return;
                }

                ContentBuilder.BuildTomb(level.transform);
                ContentBuilder.LightTomb(level.transform.Find(ContentBuilder.TombRootName));
                var spawn = GameObject.Find("Spawn");
                if (spawn != null)
                {
                    spawn.transform.position = ContentBuilder.TombSpot("start") + new Vector2(0f, 0.85f);
                }

                var respawner = Object.FindAnyObjectByType<PlayerRespawner>();
                if (respawner != null)
                {
                    var serialized = new SerializedObject(respawner);
                    serialized.FindProperty("_worldFloorY").floatValue = ContentBuilder.WorldFloorY;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }

                var player = Object.FindAnyObjectByType<PlayerCharacter>();
                if (player != null && spawn != null)
                {
                    player.transform.position = spawn.transform.position;
                }

                ContentBuilder.BuildExploration();
                SceneNpcs.Build();
                SceneCharacterLight.Build();
            });
            SceneTombLook.Apply();
            SceneAmbience.Apply();
            SceneScreenGrade.Apply();
            SceneMusic.Apply();
            Debug.Log("Tomb from text applied to the world.");
        }
    }
}
