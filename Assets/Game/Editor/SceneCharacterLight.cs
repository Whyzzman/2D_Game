using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// Light for the characters now that they are lit sprites with normal maps (visual item 3,
    /// 2026-09-28). The zone's ambient light stops at the Characters layer; a fill of their own
    /// keeps every figure as bright as it was when it was unlit, so the change can only add — a
    /// candle or a spell to one side lights that side on top. Every point light in the level
    /// reads normal maps.
    /// Run: -executeMethod ForgottenKingdom.EditorTools.SceneCharacterLight.Apply
    /// </summary>
    public static class SceneCharacterLight
    {
        public const string FillName = "CharacterFill";
        public const string CharacterLayer = "Characters";

        /// <summary>How far above the picture a light is taken to stand when it shades a normal map.</summary>
        public const float NormalDistance = 3f;

        /// <summary>Near-white and a touch cool, at full strength: the old unlit brightness.</summary>
        public static readonly Color FillColor = new Color(0.94f, 0.96f, 1f);
        public const float FillIntensity = 1f;

        [MenuItem("Forgotten Kingdom/Scene/Apply Character Light")]
        public static void Apply()
        {
            // Every light of the world, the zones' included.
            WorldScenes.Edit(Build);
            Debug.Log("Character light applied to the world.");
        }

        /// <summary>The recipe, also for a scene rebuilt from scratch: run after the lights exist.</summary>
        public static void Build()
        {
            var characters = SortingLayer.NameToID(CharacterLayer);
            Light2D ambient = null;
            foreach (var light in Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include))
            {
                if (light.name == FillName)
                {
                    continue;
                }

                if (light.lightType == Light2D.LightType.Global)
                {
                    ambient = light;
                    SetTargets(light, Targets(light).Where(id => id != characters).ToArray());
                }
                else
                {
                    UseNormalMaps(light, NormalDistance);
                }
            }

            if (ambient == null)
            {
                Debug.LogError("The level has no global light to take the characters out of.");
                return;
            }

            var old = ambient.transform.parent != null ? ambient.transform.parent.Find(FillName) : null;
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            var host = new GameObject(FillName);
            host.transform.SetParent(ambient.transform.parent, false);
            var fill = host.AddComponent<Light2D>();
            fill.lightType = Light2D.LightType.Global;
            fill.color = FillColor;
            fill.intensity = FillIntensity;
            SetTargets(fill, new[] { characters });
        }

        /// <summary>Normal maps on for a light: the fields are serialized-only in URP 17.</summary>
        public static void UseNormalMaps(Light2D light, float distance)
        {
            var serialized = new SerializedObject(light);
            serialized.FindProperty("m_NormalMapQuality").intValue = (int)Light2D.NormalMapQuality.Fast;
            serialized.FindProperty("m_NormalMapDistance").floatValue = distance;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static int[] Targets(Light2D light)
        {
            var list = new SerializedObject(light).FindProperty("m_ApplyToSortingLayers");
            var ids = new int[list.arraySize];
            for (var i = 0; i < ids.Length; i++)
            {
                ids[i] = list.GetArrayElementAtIndex(i).intValue;
            }

            return ids;
        }

        private static void SetTargets(Light2D light, int[] ids)
        {
            var serialized = new SerializedObject(light);
            var list = serialized.FindProperty("m_ApplyToSortingLayers");
            list.arraySize = ids.Length;
            for (var i = 0; i < ids.Length; i++)
            {
                list.GetArrayElementAtIndex(i).intValue = ids[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
