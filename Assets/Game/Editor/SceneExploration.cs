using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// Puts what exploring pays into the world (item 4 of the big list, 2026-09-28): vessel shards,
    /// secret rooms and trials, all under <c>Level/Exploration</c>. The world is put together,
    /// the layer rebuilt and the world split again. Safe to run again.
    /// Run: -executeMethod ForgottenKingdom.EditorTools.SceneExploration.Apply
    /// </summary>
    public static class SceneExploration
    {
        [MenuItem("Forgotten Kingdom/Scene/Apply Exploration Rewards")]
        public static void Apply()
        {
            WorldScenes.Edit(ContentBuilder.BuildExploration);
            Debug.Log("Exploration rewards applied to the world.");
        }
    }
}
