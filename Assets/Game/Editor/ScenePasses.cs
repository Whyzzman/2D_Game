using System;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// A zone's text and painted art brought into its scene in one run, without a live Editor: the
    /// level art imported, the zone rebuilt from its text, and the passes that dress every zone
    /// after it (reactive things, stones, painted lights). The same steps as
    /// tools/level-art/apply-all.sh, for one zone.
    /// Run: -batchmode -quit -executeMethod ForgottenKingdom.EditorTools.ScenePasses.ApplyZone -zone tomb
    /// </summary>
    public static class ScenePasses
    {
        public static void ApplyZone()
        {
            var args = Environment.GetCommandLineArgs();
            var at = Array.IndexOf(args, "-zone");
            var zone = at >= 0 && at + 1 < args.Length ? args[at + 1].ToLowerInvariant() : string.Empty;
            ContentBuilder.RefreshLevelArt();
            switch (zone)
            {
                case "tomb": SceneTomb.Apply(); break;
                case "forest": SceneForest.Apply(); break;
                case "village": SceneVillage.Apply(); break;
                case "marsh": SceneMarsh.Apply(); break;
                case "capital": SceneCapital.Apply(); break;
                default:
                    Debug.LogError($"ScenePasses: unknown zone '{zone}' (tomb, forest, village, marsh, capital).");
                    return;
            }

            SceneReactive.Apply();
            SceneStones.Apply();
            SceneLevelLights.Apply();
            Debug.Log($"ScenePasses: {zone} applied.");
        }
    }
}
