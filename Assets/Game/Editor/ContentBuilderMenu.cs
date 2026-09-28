using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The title screen scene: a lit, fogged view of the Tomb's nave with the menu over it. It is
    /// the first scene of the build; the game itself stays <c>TombTrial</c>, which every test and
    /// the menu load by name.
    /// </summary>
    public static partial class ContentBuilder
    {
        private const string MainMenuScenePath = "Assets/Game/Scenes/MainMenu.unity";
        private const string MainMenuUxmlPath = UiFolder + "/Menu/MainMenu.uxml";

        private static void BuildMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Fresh handles after the new scene; the generators only rewrite what changed.
            _material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            _tomb = PlaceholderTombArt.Generate();
            _shapes = ProductionSpriteArt.Generate();

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            cameraObject.AddComponent<AudioListener>();
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.03f, 0.03f, 0.045f);
            cameraObject.transform.position = new Vector3(0f, 3.4f, -10f);
            SceneScreenGrade.ConfigureMenuCamera(camera);

            var systems = new GameObject("Systems");
            SceneMusic.AddMenuMusic(PlaceholderMusicArt.Generate());
            var ambient = FindOrCreateChild(systems, "GlobalLight");
            AddLight(ambient, Light2D.LightType.Global, new Color(0.55f, 0.6f, 0.78f), 0.55f);

            var nave = new GameObject("Nave").transform;
            BuildMenuBackdrop(nave);
            BuildMenuDressing(nave);
            BuildMenuLights(nave);
            BuildFog(nave, -16f, 16f);

            var shaftSprite = Shape("shape_light_shaft");
            foreach (var (x, tilt, width) in new[] { (-3.2f, 7f, 2.6f), (3.6f, -6f, 2.2f) })
            {
                var beam = new GameObject("LightShaft");
                beam.transform.SetParent(nave, false);
                beam.transform.position = new Vector2(x, 5.5f);
                beam.transform.rotation = Quaternion.Euler(0f, 0f, tilt);
                beam.transform.localScale = new Vector3(width, 12f, 1f);
                var renderer = beam.AddComponent<SpriteRenderer>();
                renderer.sprite = shaftSprite;
                renderer.color = new Color(0.72f, 0.8f, 1f, 0.12f);
                renderer.sharedMaterial = _material;
                SetSorting(renderer, "BackgroundNear", 8);
            }

            var settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MainMenuUxmlPath);
            if (settings == null || tree == null)
            {
                Debug.LogError("The menu panel settings or MainMenu.uxml are missing; the title "
                    + "screen was not built.");
                return;
            }

            var menuObject = new GameObject("MainMenu");
            var document = menuObject.AddComponent<UIDocument>();
            document.panelSettings = settings;
            document.visualTreeAsset = tree;
            var menu = menuObject.AddComponent<MainMenuController>();
            SetReference(menu, "_vignette",
                AssetDatabase.LoadAssetAtPath<Sprite>($"{IconArt.FrameRoot}/ui_vignette.png"));
            SetReference(menu, "_ornament",
                AssetDatabase.LoadAssetAtPath<Sprite>($"{IconArt.FrameRoot}/ui_ornament.png"));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
            PutSceneFirstInBuild(MainMenuScenePath);
        }

        /// <summary>The same three painted layers as the Tomb's parallax, standing still.</summary>
        private static void BuildMenuBackdrop(Transform nave)
        {
            foreach (var (name, layer) in new[]
            {
                ("env_tomb_bg_far_01", "BackgroundFar"),
                ("env_tomb_bg_mid_01", "BackgroundMid"),
                ("env_tomb_bg_near_01", "BackgroundNear")
            })
            {
                var art = Tomb(name);
                if (art == null)
                {
                    continue;
                }

                var strip = new GameObject(name);
                strip.transform.SetParent(nave);
                strip.transform.position = new Vector3(0f, -4f, 0f);
                var renderer = strip.AddComponent<SpriteRenderer>();
                renderer.sprite = art;
                renderer.drawMode = SpriteDrawMode.Tiled;
                renderer.tileMode = SpriteTileMode.Continuous;
                renderer.size = new Vector2(Mathf.Max(30f, art.bounds.size.x), art.bounds.size.y);
                if (layer != "BackgroundNear")
                {
                    renderer.sharedMaterial = _material;
                    renderer.color = new Color(0.8f, 0.8f, 0.85f);
                }

                SetSorting(renderer, layer, 0);
            }
        }

        private static void BuildMenuDressing(Transform nave)
        {
            var floorTile = Tomb("env_tomb_floor_01");
            if (floorTile != null)
            {
                var floor = new GameObject("Floor");
                floor.transform.SetParent(nave);
                floor.transform.position = new Vector2(0f, -0.5f);
                var renderer = floor.AddComponent<SpriteRenderer>();
                renderer.sprite = floorTile;
                renderer.drawMode = SpriteDrawMode.Tiled;
                renderer.tileMode = SpriteTileMode.Continuous;
                renderer.size = new Vector2(30f, 1f);
                SetSorting(renderer, "GameplayEnvironment", 0);
            }

            var wallTile = Tomb("env_tomb_wall_01");
            if (wallTile != null)
            {
                var below = new GameObject("Foundation");
                below.transform.SetParent(nave);
                below.transform.position = new Vector2(0f, -3f);
                var renderer = below.AddComponent<SpriteRenderer>();
                renderer.sprite = wallTile;
                renderer.drawMode = SpriteDrawMode.Tiled;
                renderer.tileMode = SpriteTileMode.Continuous;
                renderer.size = new Vector2(30f, 4f);
                renderer.color = new Color(0.45f, 0.45f, 0.5f);
                SetSorting(renderer, "GameplayEnvironment", -3);
            }

            var depth = Shape("shape_depth_gradient");
            if (depth != null)
            {
                var shade = new GameObject("FoundationDepth");
                shade.transform.SetParent(nave);
                shade.transform.position = new Vector2(0f, -2.6f);
                shade.transform.localScale = new Vector3(30f, 4.4f, 1f);
                var renderer = shade.AddComponent<SpriteRenderer>();
                renderer.sprite = depth;
                renderer.sharedMaterial = _material;
                SetSorting(renderer, "GameplayEnvironment", -2);
            }

            // A symmetrical nave: pillars at the edges, the knights keeping watch, the altar
            // under the arch where the menu sits.
            Prop(nave, "env_tomb_arch_01", new Vector2(0f, 0f), "EnvironmentBack", 0, 1.9f);
            Prop(nave, "env_tomb_pillar_01", new Vector2(-9.6f, 0f), "EnvironmentBack", 1, 1.25f);
            Prop(nave, "env_tomb_pillar_broken_01", new Vector2(9.6f, 0f), "EnvironmentBack", 1, 1.25f, true);
            Prop(nave, "env_tomb_statue_knight_01", new Vector2(-6.2f, 0f), "EnvironmentBack", 2, 1.15f);
            Prop(nave, "env_tomb_statue_knight_01", new Vector2(6.2f, 0f), "EnvironmentBack", 2, 1.15f, true);
            Prop(nave, "env_tomb_memorial_01", new Vector2(0f, 0f), "EnvironmentBack", 3, 1.1f);
            Prop(nave, "env_tomb_sarcophagus_broken_01", new Vector2(-3.4f, 0f), "EnvironmentBack", 3);
            Prop(nave, "env_tomb_urn_01", new Vector2(3.1f, 0f), "EnvironmentBack", 4);
            Prop(nave, "env_tomb_chains_01", new Vector2(-8f, 7.5f), "EnvironmentBack", 4);
            Prop(nave, "env_tomb_chains_01", new Vector2(8.2f, 7.8f), "EnvironmentBack", 4, 1f, true);
            Prop(nave, "env_tomb_candles_01", new Vector2(-1.6f, 0f), "GameplayEnvironment", 4);
            Prop(nave, "env_tomb_candles_01", new Vector2(1.7f, 0f), "GameplayEnvironment", 4, 1f, true);
            Prop(nave, "env_tomb_candles_01", new Vector2(-7.4f, 0f), "GameplayEnvironment", 4);
            Prop(nave, "env_tomb_bones_01", new Vector2(4.8f, 0f), "GameplayEnvironment", 2);
            Prop(nave, "env_tomb_rubble_01", new Vector2(-5f, 0f), "GameplayEnvironment", 2);
        }

        private static void BuildMenuLights(Transform nave)
        {
            BuildCandleLights(nave);

            // The altar under the arch: the warmest thing on screen, with its embers.
            var altar = LightChild(nave, "AltarGlow", new Vector2(0f, 1.2f));
            AddLight(altar, Light2D.LightType.Point, new Color(1f, 0.68f, 0.34f), 1.1f, 6.5f, 0.6f, 0.5f);
            altar.AddComponent<LightFlicker>();
            var embers = BuildEmbers(altar);
            embers.transform.localPosition = new Vector3(0f, -0.9f, 0f);
            var main = embers.main;
            main.playOnAwake = true;
            main.maxParticles = 70;
            var emission = embers.emission;
            emission.rateOverTime = 16f;
            var shape = embers.shape;
            shape.radius = 0.9f;

            // Cold light from the vaults on the knights and the title.
            foreach (var (x, y) in new[] { (-6f, 9f), (6f, 9f) })
            {
                var host = LightChild(nave, "VaultLight", new Vector2(x, y));
                AddLight(host, Light2D.LightType.Point, new Color(0.62f, 0.72f, 1f), 0.8f, 9f, 1f, 0.5f);
            }
        }

        private static void BuildFog(Transform nave, float from, float to)
        {
            var fog = Shape("shape_fog_band");
            if (fog == null)
            {
                return;
            }

            var tone = new Color(0.55f, 0.62f, 0.78f);
            FogLayer(nave, fog, (from + to) * 0.5f, to - from, 2.8f, 5f,
                new Color(tone.r, tone.g, tone.b, 0.22f), "BackgroundMid", 20, 0.35f);
            FogLayer(nave, fog, (from + to) * 0.5f, to - from, 0.5f, 1.8f,
                new Color(tone.r, tone.g, tone.b, 0.2f), "EnvironmentBack", 40, 0.5f);
            FogLayer(nave, fog, (from + to) * 0.5f, to - from, -0.1f, 1.2f,
                new Color(tone.r, tone.g, tone.b, 0.16f), "Foreground", 5, 0.7f);
        }

        private static void PutSceneFirstInBuild(string path)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(
                EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == path);
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
