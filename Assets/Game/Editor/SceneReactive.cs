using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The world answers the hero (GAME_FEEL_IDEAS item 13): the dressing that should move — chains,
    /// garlands, lanterns, banners hanging; roots, branches and burnt saplings standing — gets a
    /// <see cref="ReactiveProp"/>, and the small flames (braziers, lanterns, candles; not altars) a
    /// <see cref="LightGust"/>. Chosen by name, so it touches only what it recognises; running it
    /// again changes nothing (the components are ensured and reconfigured, never doubled).
    /// Run after <c>Build</c> and the zones' own <c>Scene*.Apply</c>:
    /// -executeMethod ForgottenKingdom.EditorTools.SceneReactive.Apply
    /// </summary>
    public static class SceneReactive
    {
        [MenuItem("Forgotten Kingdom/Scene/Apply Reactive World")]
        public static void Apply()
        {
            WorldScenes.Edit(Build);
            Debug.Log("Reactive props and gusting lights applied to the world.");
        }

        /// <summary>The recipe: every loaded scene's sprites and lights, by name.</summary>
        public static void Build()
        {
            var props = 0;
            foreach (var renderer in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include))
            {
                if (!TryMotion(renderer.gameObject.name, out var motion, out var reach, out var angle))
                {
                    continue;
                }

                var prop = renderer.GetComponent<ReactiveProp>();
                if (prop == null) { prop = renderer.gameObject.AddComponent<ReactiveProp>(); }
                prop.Configure(motion, reach, angle);
                EditorUtility.SetDirty(prop);
                props++;
            }

            var lights = 0;
            foreach (var light in Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include))
            {
                if (light.lightType == Light2D.LightType.Global || !IsSmallFlame(light.gameObject.name))
                {
                    continue;
                }

                if (light.GetComponent<LightGust>() == null) { light.gameObject.AddComponent<LightGust>(); }
                lights++;
            }

            Debug.Log($"SceneReactive: {props} props sway, {lights} flames gutter.");
        }

        /// <summary>What sways and how: hanging things swing wide, heavy standing ones barely bend.</summary>
        public static bool TryMotion(string name, out ReactiveMotion motion, out float reach, out float maxAngle)
        {
            motion = ReactiveMotion.Hanging;
            reach = 1.4f;
            maxAngle = 12f;
            if (name.StartsWith("env_fg_tomb_chains") || name.StartsWith("env_village_garland"))
            {
                maxAngle = 9f;
                return true;
            }

            if (name.StartsWith("env_village_lantern") || name.StartsWith("env_capital_banner"))
            {
                reach = 1.2f;
                maxAngle = 7f;
                return true;
            }

            if (name.StartsWith("env_fg_forest_roots") || name.StartsWith("env_fg_forest_branch")
                || name.StartsWith("env_village_hay"))
            {
                motion = ReactiveMotion.Standing;
                reach = 1.6f;
                maxAngle = 5f;
                return true;
            }

            return false;
        }

        /// <summary>Braziers, lanterns, candles and the village's warm windows; never an altar's fire.</summary>
        public static bool IsSmallFlame(string name) =>
            !name.Contains("Altar")
            && (name.StartsWith("BrazierLight") || name.StartsWith("WarmLight") || name.Contains("Candle")
                || name.Contains("Lantern"));
    }
}
