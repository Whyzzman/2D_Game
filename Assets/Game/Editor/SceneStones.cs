using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// Re-dresses the inscribed stones already standing in the world with the painted steles
    /// (<see cref="ContentBuilder.DressStone"/>): only each stone's look changes — its place, trigger
    /// and words stay. Running it again is safe.
    /// Run: -executeMethod ForgottenKingdom.EditorTools.SceneStones.Apply
    /// </summary>
    public static class SceneStones
    {
        [MenuItem("Forgotten Kingdom/Scene/Apply Painted Stones")]
        public static void Apply()
        {
            var dressed = 0;
            WorldScenes.Edit(() =>
            {
                foreach (var inscription in Object.FindObjectsByType<Inscription>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    var stone = inscription.gameObject;
                    ContentBuilder.DressStone(stone, ContentBuilder.IsForestStone(stone.name));
                    dressed++;
                }
            });
            Debug.Log($"Painted stones applied to the world: {dressed}.");
        }
    }
}
