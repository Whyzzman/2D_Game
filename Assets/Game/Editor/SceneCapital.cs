using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// Puts the Ashen Capital into the world (item 2 of the big list, 2026-09-28): its level,
    /// the road from the forest opened by the Warden, its light and room tone, and the merchant
    /// moving there. The world is put together, built and split again, so the capital lands in
    /// its own scene, <c>Zone_Capital</c>; its grade, air and music are the other scene tools'
    /// part and are applied after. Safe to run again.
    /// Run: -executeMethod ForgottenKingdom.EditorTools.SceneCapital.Apply
    /// </summary>
    public static class SceneCapital
    {
        [MenuItem("Forgotten Kingdom/Scene/Apply Ashen Capital")]
        public static void Apply()
        {
            // Built from its text since item 6a; the old root goes by name inside the build. The scene is
            // not emptied: the forest's bars and winch on the old road stand in it by place.
            WorldScenes.Edit(() =>
            {
                ContentBuilder.BuildCapital();
                ContentBuilder.ConfigureCapitalAtmosphere();
                ContentBuilder.BuildExploration();
                SceneNpcs.Build();
                SceneCharacterLight.Build();
            });
            SceneScreenGrade.Apply();
            SceneMusic.Apply();
            SceneAmbience.Apply();
            Debug.Log("Ashen Capital applied to the world.");
        }
    }
}
