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
            WorldScenes.Edit(() =>
            {
                ContentBuilder.BuildCapital();
                ContentBuilder.ConfigureCapitalAtmosphere();
                SceneNpcs.Build();
            });
            SceneScreenGrade.Apply();
            SceneMusic.Apply();
            SceneAmbience.Apply();
            Debug.Log("Ashen Capital applied to the world.");
        }
    }
}
