using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// Real light from what the painter paints glowing (2026-11-04, «the game should look expensive»;
    /// decided by Claude): a warm point on every candle of the Tomb, a cold one on every pale mushroom of
    /// the Forest, read from <c>Art/Levels/&lt;Zone&gt;/lights.json</c> (tools/level-art/lights.js). They light
    /// the Characters layer only, on top of its fill (<see cref="SceneCharacterLight"/>), so a figure walking
    /// past a candle takes its warmth on that side and the painting stays as it was painted. One root per
    /// run, replaced whole; the world split hands each light to its zone.
    /// Run: -executeMethod ForgottenKingdom.EditorTools.SceneLevelLights.Apply
    /// </summary>
    public static class SceneLevelLights
    {
        public const string RootName = "PaintedLights";
        private static readonly string[] Zones = { "Tomb", "Forest", "Village", "Capital", "Marsh" };

        [Serializable]
        private sealed class LightList { public PaintedLight[] lights; }

        [Serializable]
        private sealed class PaintedLight { public string kind; public float x; public float y; }

        [MenuItem("Forgotten Kingdom/Scene/Apply Painted Lights")]
        public static void Apply()
        {
            var made = 0;
            WorldScenes.Edit(() => made = Build());
            Debug.Log($"Painted lights applied to the world: {made}.");
        }

        public static int Build()
        {
            foreach (var old in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (old != null && old.name == RootName && old.parent == null)
                {
                    Object.DestroyImmediate(old.gameObject);
                }
            }

            var root = new GameObject(RootName).transform;
            var characters = SortingLayer.NameToID(SceneCharacterLight.CharacterLayer);
            var made = 0;
            foreach (var zone in Zones)
            {
                var file = $"Assets/Game/Art/Levels/{zone}/lights.json";
                if (!File.Exists(file))
                {
                    continue;
                }

                var list = JsonUtility.FromJson<LightList>(File.ReadAllText(file));
                foreach (var painted in list.lights ?? Array.Empty<PaintedLight>())
                {
                    var look = Look(painted.kind);
                    var host = new GameObject(look.Name);
                    host.transform.SetParent(root, false);
                    // a little above the flame or the cap, so the light falls on a figure's flank, not its feet
                    host.transform.position = new Vector3(painted.x, painted.y + look.Lift, 0f);
                    var light = host.AddComponent<Light2D>();
                    light.lightType = Light2D.LightType.Point;
                    light.color = look.Color;
                    light.intensity = look.Intensity;
                    light.pointLightOuterRadius = look.Radius;
                    light.pointLightInnerRadius = 0.15f;
                    light.falloffIntensity = 0.7f;
                    SceneCharacterLight.UseNormalMaps(light, SceneCharacterLight.NormalDistance);
                    if (look.Flickers)
                    {
                        // a flame is never steady (2026-11-06): the candles and the lanterns breathe, each on its own
                        host.AddComponent<LightFlicker>();
                    }
                    var serialized = new SerializedObject(light);
                    var targets = serialized.FindProperty("m_ApplyToSortingLayers");
                    targets.arraySize = 1;
                    targets.GetArrayElementAtIndex(0).intValue = characters;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    made++;
                }
            }

            return made;
        }

        private readonly struct LightLook
        {
            public readonly string Name;
            public readonly Color Color;
            public readonly float Intensity;
            public readonly float Radius;
            public readonly float Lift;
            public readonly bool Flickers;

            public LightLook(string name, Color color, float intensity, float radius, float lift, bool flickers)
            {
                Name = name;
                Color = color;
                Intensity = intensity;
                Radius = radius;
                Lift = lift;
                Flickers = flickers;
            }
        }

        /// <summary>
        /// What each painted glow gives off. The Village's lanterns (2026-11-06: the one zone with no light of its
        /// own) are logged where the glass hangs, so they need no lift; amber, a little wider than a candle.
        /// </summary>
        private static LightLook Look(string kind)
        {
            switch (kind)
            {
                case "candle":
                    return new LightLook("CandleLight", new Color(1f, 0.7f, 0.42f), 0.85f, 3f, 0.45f, true);
                case "lantern":
                    return new LightLook("LanternLight", new Color(1f, 0.64f, 0.34f), 0.9f, 3.4f, 0f, true);
                default:
                    return new LightLook("MushroomLight", new Color(0.45f, 0.95f, 0.88f), 0.55f, 2.2f, 0.3f, false);
            }
        }
    }
}
