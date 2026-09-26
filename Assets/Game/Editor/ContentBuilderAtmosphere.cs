using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The lighting and atmosphere pass over the built level: real 2D lights, fog, light shafts,
    /// the ground the floors stand on, and the camera's framing.
    ///
    /// Before it, the whole level was lit evenly by nothing — every sprite at full value, no
    /// source anywhere — and the floor hung across the middle of the screen over an empty colour.
    /// It runs last, after the Tomb and the forest are placed, because most of it responds to
    /// what is already there: every candle gets its flame's light, every floor its foundation.
    ///
    /// Numbers follow Visual Bible §10–11: cold ambient, warm altars, the player always lit, the
    /// boss arena brighter and more contrasted than the rooms before it.
    /// </summary>
    public static partial class ContentBuilder
    {
        private static readonly Color TombAmbient = new Color(0.62f, 0.68f, 0.82f);
        private static readonly Color ForestAmbient = new Color(0.6f, 0.74f, 0.74f);
        private static readonly Color CandleLight = new Color(1f, 0.7f, 0.38f);

        private static void BuildAtmosphere(Transform level, GameObject systems, GameObject camera,
            GameObject player)
        {
            var root = Section(level, "Atmosphere");
            BuildGlobalLight(systems);
            BuildCandleLights(level);
            BuildArenaLights(root);
            BuildFoundations(level);
            BuildFog(root);
            BuildLightShafts(root);
            BuildPlayerLight(player);
            BuildCheckpointLights();
            BuildBossLights();
            FrameCamera(camera);
        }

        // ---------- lights ----------

        /// <summary>
        /// Adds a 2D light that affects every world layer and none of the interface ones. The
        /// target layers are written through the serializer: a light created from script otherwise
        /// starts with none and lights nothing, silently.
        /// </summary>
        private static Light2D AddLight(GameObject host, Light2D.LightType type, Color color,
            float intensity, float outerRadius = 3f, float innerRadius = 0f, float falloff = 0.55f)
        {
            var light = host.AddComponent<Light2D>();
            light.lightType = type;
            light.color = color;
            light.intensity = intensity;
            if (type == Light2D.LightType.Point)
            {
                light.pointLightOuterRadius = outerRadius;
                light.pointLightInnerRadius = innerRadius;
                light.falloffIntensity = falloff;
            }

            var serialized = new SerializedObject(light);
            var layers = serialized.FindProperty("m_ApplyToSortingLayers");
            var targets = SortingLayer.layers
                .Where(l => l.name != "WorldUI" && l.name != "ScreenUI")
                .Select(l => l.id)
                .ToArray();
            layers.arraySize = targets.Length;
            for (var i = 0; i < targets.Length; i++)
            {
                layers.GetArrayElementAtIndex(i).intValue = targets[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return light;
        }

        private static GameObject LightChild(Transform parent, string name, Vector2 position)
        {
            var host = new GameObject(name);
            host.transform.SetParent(parent, false);
            host.transform.position = position;
            return host;
        }

        /// <summary>The room's ambient light, eased between zones by ZoneLighting.</summary>
        private static void BuildGlobalLight(GameObject systems)
        {
            var host = FindOrCreateChild(systems, "GlobalLight");
            AddLight(host, Light2D.LightType.Global, TombAmbient, 0.78f);
            var zoning = host.AddComponent<ZoneLighting>();
            var serialized = new SerializedObject(zoning);
            var zones = serialized.FindProperty("_zones");
            zones.arraySize = 2;
            WriteZoneLight(zones.GetArrayElementAtIndex(0), TombZoneId, TombAmbient, 0.78f);
            WriteZoneLight(zones.GetArrayElementAtIndex(1), ForestZoneId, ForestAmbient, 0.86f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WriteZoneLight(SerializedProperty entry, string zoneId, Color color,
            float intensity)
        {
            entry.FindPropertyRelative("_zoneId").stringValue = zoneId;
            entry.FindPropertyRelative("_color").colorValue = color;
            entry.FindPropertyRelative("_intensity").floatValue = intensity;
        }

        /// <summary>Every candle cluster lights the stone around it.</summary>
        private static void BuildCandleLights(Transform level)
        {
            foreach (var renderer in level.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.sprite == null || !renderer.sprite.name.Contains("candles"))
                {
                    continue;
                }

                var host = LightChild(renderer.transform, "CandleLight",
                    (Vector2)renderer.transform.position + new Vector2(0f, 0.55f));
                host.transform.localScale = Vector3.one;
                AddLight(host, Light2D.LightType.Point, CandleLight, 0.95f, 3.2f, 0.2f, 0.6f);
                host.AddComponent<LightFlicker>();
            }
        }

        /// <summary>
        /// The two arenas are the brightest, highest-contrast rooms of their zones: cold light
        /// falling from above onto the fighting floor, and a deeper glow behind the boss.
        /// </summary>
        private static void BuildArenaLights(Transform root)
        {
            var arenas = Section(root, "ArenaLights");
            foreach (var (x, y, color, intensity, radius) in new[]
            {
                (44f, 8f, new Color(0.7f, 0.8f, 1f), 1.0f, 9f),
                (56f, 8f, new Color(0.7f, 0.8f, 1f), 1.0f, 9f),
                (50f, 3f, new Color(0.9f, 0.72f, 0.45f), 0.55f, 7f),
                (282f, 9f, new Color(0.72f, 0.88f, 0.92f), 1.0f, 11f),
                (295f, 9f, new Color(0.72f, 0.88f, 0.92f), 0.9f, 10f)
            })
            {
                var host = LightChild(arenas, "ArenaLight", new Vector2(x, y));
                AddLight(host, Light2D.LightType.Point, color, intensity, radius, 1.5f, 0.45f);
            }
        }

        /// <summary>A cool light carried by the player: never lost in the dark, never a torch.</summary>
        private static void BuildPlayerLight(GameObject player)
        {
            var host = FindOrCreateChild(player, "RimLight");
            host.transform.localPosition = new Vector3(-0.35f, 0.5f, 0f);
            AddLight(host, Light2D.LightType.Point, new Color(0.72f, 0.82f, 1f), 0.6f, 4.2f, 0.4f,
                0.7f);

            var controller = player.GetComponentInChildren<PlayerAnimationController>(true);
            var body = controller != null
                ? new SerializedObject(controller).FindProperty("_body").objectReferenceValue as SpriteRenderer
                : null;
            var trail = Ensure<AfterimageTrail>(player);
            SetReference(trail, "_player", player.GetComponent<PlayerCharacter>());
            SetReference(trail, "_shadow", player.GetComponent<ShadowAbilities>());
            SetReference(trail, "_body", body);
            SetReference(trail, "_material", _material);
        }

        /// <summary>
        /// Every altar gets a real light and embers. Dead, it gives off the faintest cold glint —
        /// enough to be found — and lit, it is the warmest thing in the zone.
        /// </summary>
        private static void BuildCheckpointLights()
        {
            foreach (var visual in Object.FindObjectsByType<CheckpointVisual>(FindObjectsInactive.Include))
            {
                var host = FindOrCreateChild(visual.gameObject, "AltarLight");
                host.transform.localPosition = new Vector3(0f, 1.1f, 0f);
                var light = AddLight(host, Light2D.LightType.Point, new Color(1f, 0.72f, 0.36f),
                    0.25f, 5.5f, 0.6f, 0.5f);
                var embers = BuildEmbers(visual.gameObject);
                SetReference(visual, "_light", light);
                SetReference(visual, "_embers", embers);
            }
        }

        private static ParticleSystem BuildEmbers(GameObject host)
        {
            var emberObject = FindOrCreateChild(host, "Embers");
            emberObject.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            var embers = Ensure<ParticleSystem>(emberObject);
            var main = embers.main;
            main.playOnAwake = false;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.78f, 0.4f),
                new Color(1f, 0.55f, 0.2f));
            main.gravityModifier = -0.05f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 40;
            var emission = embers.emission;
            emission.rateOverTime = 9f;
            var shape = embers.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.18f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            var noise = embers.noise;
            noise.enabled = true;
            noise.strength = 0.25f;
            noise.frequency = 0.6f;
            var fade = embers.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f),
                    new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            var renderer = emberObject.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = EmberMaterial();
            renderer.sortingLayerName = "VFXFront";
            renderer.sortingOrder = 2;
            return embers;
        }

        /// <summary>An unlit additive-looking particle material over the soft glow shape.</summary>
        private static Material EmberMaterial()
        {
            const string path = "Assets/Game/Art/Shapes/EmberParticles.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(_material.shader);
                AssetDatabase.CreateAsset(material, path);
            }

            var glow = Shape("shape_soft_glow");
            material.mainTexture = glow != null ? glow.texture : null;
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>A boss carries its own light: a cold gleam, then the violet of its broken core.</summary>
        private static void BuildBossLights()
        {
            foreach (var boss in Object.FindObjectsByType<BossController>(FindObjectsInactive.Include))
            {
                var host = FindOrCreateChild(boss.gameObject, "PhaseLight");
                host.transform.localPosition = new Vector3(0f, boss.BossId == GuardianBossId ? 1.3f : 1.1f, 0f);
                var light = AddLight(host, Light2D.LightType.Point, new Color(0.6f, 0.75f, 1f), 0.45f,
                    4.5f, 0.3f, 0.6f);
                var phase = host.AddComponent<BossPhaseLight>();
                SetReference(phase, "_boss", boss);
                SetReference(phase, "_light", light);
            }
        }

        // ---------- ground, fog, shafts ----------

        /// <summary>
        /// Under every floor: its masonry carried down into the dark. Before this the floor was a
        /// one-unit slab with the camera's clear colour below it — the most prototype-looking
        /// thing on screen. Gaps between floors are left alone, so a pit still reads as a pit.
        /// </summary>
        private static void BuildFoundations(Transform level)
        {
            var depthSprite = Shape("shape_depth_gradient");
            foreach (var box in level.GetComponentsInChildren<BoxCollider2D>(true))
            {
                var block = box.gameObject;
                if (block.layer != GroundLayer || box.isTrigger)
                {
                    continue;
                }

                var size = box.size;
                var top = block.transform.position.y + box.offset.y + size.y * 0.5f;
                if (size.y < 0.8f || size.y > 1.3f || size.x < 3f || Mathf.Abs(top) > 0.05f)
                {
                    continue;
                }

                var inForest = block.transform.position.x > 62.5f;
                var tile = inForest ? ForestArt("env_forest_cliff_01") : Tomb("env_tomb_wall_01");
                const float depth = 8f;
                var centre = new Vector2(block.transform.position.x + box.offset.x, top - 1f - depth * 0.5f);

                var masonry = new GameObject("Foundation");
                masonry.transform.SetParent(block.transform, true);
                masonry.transform.position = centre;
                var masonryRenderer = masonry.AddComponent<SpriteRenderer>();
                if (tile != null)
                {
                    masonryRenderer.sprite = tile;
                    masonryRenderer.drawMode = SpriteDrawMode.Tiled;
                    masonryRenderer.tileMode = SpriteTileMode.Continuous;
                    masonryRenderer.size = new Vector2(size.x, depth);
                }

                masonryRenderer.color = new Color(0.62f, 0.62f, 0.68f);
                SetSorting(masonryRenderer, "GameplayEnvironment", -3);

                var shade = new GameObject("FoundationDepth");
                shade.transform.SetParent(block.transform, true);
                shade.transform.position = new Vector2(centre.x, top - 1f + 0.4f);
                shade.transform.localScale = new Vector3(size.x, depth + 0.4f, 1f);
                var shadeRenderer = shade.AddComponent<SpriteRenderer>();
                shadeRenderer.sprite = depthSprite;
                shadeRenderer.sharedMaterial = _material;
                SetSorting(shadeRenderer, "GameplayEnvironment", -2);
            }
        }

        /// <summary>
        /// Two banks of fog per zone: a far one between the nave and the arcade, a low one lying
        /// on the floor behind the fight. Slow drift; never on the line of combat.
        /// </summary>
        private static void BuildFog(Transform root)
        {
            var fog = Shape("shape_fog_band");
            if (fog == null)
            {
                return;
            }

            foreach (var (zone, from, to, tone) in new[]
            {
                (TombZoneId, -30f, 66f, new Color(0.55f, 0.62f, 0.78f)),
                (ForestZoneId, 60f, 320f, new Color(0.58f, 0.72f, 0.7f))
            })
            {
                var bank = Section(root, "Fog_" + zone);
                SetString(bank.gameObject.AddComponent<ZoneBackdrop>(), "_zoneId", zone);
                FogLayer(bank, fog, (from + to) * 0.5f, to - from, 2.6f, 5f,
                    new Color(tone.r, tone.g, tone.b, 0.2f), "BackgroundMid", 20, 0.25f);
                FogLayer(bank, fog, (from + to) * 0.5f, to - from, 0.55f, 1.7f,
                    new Color(tone.r, tone.g, tone.b, 0.14f), "EnvironmentBack", 40, 0.4f);
            }
        }

        private static void FogLayer(Transform parent, Sprite fog, float x, float width, float y,
            float height, Color color, string layer, int order, float drift)
        {
            var band = new GameObject("FogBand");
            band.transform.SetParent(parent, false);
            band.transform.position = new Vector2(x, y);
            var renderer = band.AddComponent<SpriteRenderer>();
            renderer.sprite = fog;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            // The fog texture is 5.12 × 1.28 units; scaled to the wanted height, tiled along the zone.
            var scale = height / fog.bounds.size.y;
            band.transform.localScale = new Vector3(1f, scale, 1f);
            renderer.size = new Vector2(width, fog.bounds.size.y);
            renderer.color = color;
            renderer.sharedMaterial = _material;
            SetSorting(renderer, layer, order);
            SetFloat(band.AddComponent<FogDrift>(), "_speed", drift);
        }

        /// <summary>Cold light falling through the vaults: guides the eye, lights nothing.</summary>
        private static void BuildLightShafts(Transform root)
        {
            var shaft = Shape("shape_light_shaft");
            if (shaft == null)
            {
                return;
            }

            var shafts = Section(root, "LightShafts");
            SetString(shafts.gameObject.AddComponent<ZoneBackdrop>(), "_zoneId", TombZoneId);
            foreach (var (x, tilt, width) in new[]
            {
                (-9f, 8f, 2.6f), (4f, -6f, 2.2f), (16f, 7f, 3f), (30f, -5f, 2.4f), (46f, 6f, 3.4f),
                (54f, -7f, 3.4f)
            })
            {
                var beam = new GameObject("LightShaft");
                beam.transform.SetParent(shafts, false);
                beam.transform.position = new Vector2(x, 5f);
                beam.transform.rotation = Quaternion.Euler(0f, 0f, tilt);
                beam.transform.localScale = new Vector3(width, 11f, 1f);
                var renderer = beam.AddComponent<SpriteRenderer>();
                renderer.sprite = shaft;
                renderer.color = new Color(0.7f, 0.8f, 1f, 0.09f);
                renderer.sharedMaterial = _material;
                SetSorting(renderer, "BackgroundNear", 8);
            }
        }

        // ---------- camera ----------

        /// <summary>
        /// The player a little below centre, the view leading into the run, a controlled widen for
        /// the bosses and a punch for a parry. The clear colour becomes the darkest vault tone, so
        /// even an unexpected gap reads as distance rather than as an empty screen.
        /// </summary>
        private static void FrameCamera(GameObject camera)
        {
            var follow = camera.GetComponent<CameraFollow2D>();
            SetVector(follow, "_framingOffset", new Vector2(0f, 1.55f));
            SetFloat(follow, "_lookAhead", 1.3f);
            SetFloat(follow, "_smoothTime", 0.14f);
            Ensure<CameraDirector>(camera);
            camera.GetComponent<Camera>().backgroundColor = new Color(0.04f, 0.045f, 0.065f);
        }
    }
}
