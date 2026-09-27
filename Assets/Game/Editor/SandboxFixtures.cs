using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// Things to practise on in the CoreMovement sandbox, on the start floor left of the spawn:
    /// a patch of spikes for the pogo and a ramp for walking on slopes. Scenes are the source of
    /// truth, so this is a one-off edit of the sandbox (and the recipe if it is ever rebuilt),
    /// never part of ContentBuilder.Build. Each piece is added only if it is missing.
    /// Run: -executeMethod ForgottenKingdom.EditorTools.SandboxFixtures.Add
    /// </summary>
    public static class SandboxFixtures
    {
        public const string ScenePath = "Assets/Game/Scenes/CoreMovement.unity";
        private const int GroundLayer = 9;

        [MenuItem("Forgotten Kingdom/Sandbox/Add Practice Fixtures")]
        public static void Add()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var changed = false;
            if (GameObject.Find("Spikes") == null)
            {
                var spikes = new GameObject("Spikes");
                spikes.transform.position = new Vector3(-8f, 0.2f, 0f);
                spikes.AddComponent<Spikes>();
                changed = true;
            }

            if (GameObject.Find("Slope") == null)
            {
                // Up 0.9 over 1.5 and back down: about 31 degrees each way.
                var slope = new GameObject("Slope") { layer = GroundLayer };
                var collider = slope.AddComponent<PolygonCollider2D>();
                collider.points = new[] { new Vector2(-5.5f, 0f), new Vector2(-4f, 0.9f), new Vector2(-2.5f, 0f) };
                slope.AddComponent<GroundShapePlaceholder>();
                changed = true;
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            Debug.Log(changed ? "Practice fixtures added to the CoreMovement sandbox."
                : "The CoreMovement sandbox already has its practice fixtures.");
        }
    }
}
