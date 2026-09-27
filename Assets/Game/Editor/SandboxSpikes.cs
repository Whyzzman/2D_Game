using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// Puts a patch of spikes into the CoreMovement sandbox, on the start floor left of the spawn,
    /// to practise the pogo against. Scenes are the source of truth, so this is a one-off edit of
    /// the sandbox (and a recipe if it is ever rebuilt), never part of ContentBuilder.Build.
    /// Run: -executeMethod ForgottenKingdom.EditorTools.SandboxSpikes.Add
    /// </summary>
    public static class SandboxSpikes
    {
        public const string ScenePath = "Assets/Game/Scenes/CoreMovement.unity";
        private const string ObjectName = "Spikes";

        [MenuItem("Forgotten Kingdom/Sandbox/Add Spikes")]
        public static void Add()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (GameObject.Find(ObjectName) == null)
            {
                var spikes = new GameObject(ObjectName);
                spikes.transform.position = new Vector3(-8f, 0.2f, 0f);
                spikes.AddComponent<Spikes>();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("Spikes added to the CoreMovement sandbox.");
            }
            else
            {
                Debug.Log("The CoreMovement sandbox already has spikes.");
            }
        }
    }
}
