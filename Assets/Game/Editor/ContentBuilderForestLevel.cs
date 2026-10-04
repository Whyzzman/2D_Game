using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The Withered Forest's materials: its surfaces, its props, its plants, its parallax and its
    /// sound. The rooms themselves are built from their text (<c>ContentBuilderForestRooms</c>).
    /// </summary>
    public static partial class ContentBuilder
    {
        private static Dictionary<string, Sprite> _forest;

        /// <summary>Which tile a piece of forest is surfaced with; purely visual.</summary>
        private enum ForestSurface { Ground, Road, Cliff, Plank, BrokenPlank, RootWall }

        private static Sprite ForestArt(string name) =>
            _forest != null && _forest.TryGetValue(name, out var sprite) ? sprite : null;

        private static Sprite ForestTileFor(ForestSurface surface)
        {
            switch (surface)
            {
                case ForestSurface.Road: return ForestArt("env_forest_road_01");
                case ForestSurface.Cliff: return ForestArt("env_forest_cliff_01");
                case ForestSurface.Plank: return ForestArt("env_forest_platform_01");
                case ForestSurface.BrokenPlank: return ForestArt("env_forest_platform_broken_01");
                case ForestSurface.RootWall: return ForestArt("env_forest_rootwall_01");
                default: return ForestArt("env_forest_ground_01");
            }
        }

        /// <summary>
        /// A solid piece of forest. Same rule as the Tomb's <c>Block</c>: the collider is made from
        /// the gameplay size first and the art is fitted to it, never the other way round.
        /// </summary>
        private static GameObject ForestBlock(Transform parent, string name, Vector2 centre,
            Vector2 size, ForestSurface surface = ForestSurface.Ground)
        {
            var block = new GameObject(name);
            block.transform.SetParent(parent);
            block.transform.position = centre;
            block.layer = GroundLayer;
            var collider = block.AddComponent<BoxCollider2D>();
            collider.size = size;

            var visual = FindOrCreateChild(block, "Visual");
            var renderer = visual.AddComponent<SpriteRenderer>();
            var tile = ForestTileFor(surface);
            if (tile != null)
            {
                renderer.sprite = tile;
                renderer.drawMode = SpriteDrawMode.Tiled;
                renderer.tileMode = SpriteTileMode.Continuous;
                renderer.size = size;
            }
            else
            {
                visual.transform.localScale = new Vector3(size.x, size.y, 1f);
                renderer.sprite = _sprite;
                renderer.sharedMaterial = _material;
                renderer.color = new Color(0.18f, 0.2f, 0.17f);
            }

            SetSorting(renderer, "GameplayEnvironment", 0);
            return block;
        }

        /// <summary>Dressing. Never carries a collider, exactly as in the Tomb.</summary>
        private static GameObject ForestProp(Transform parent, string spriteName, Vector2 position,
            string layer, int order, float scale = 1f, bool flip = false, float alpha = 1f)
        {
            var sprite = ForestArt(spriteName);
            if (sprite == null)
            {
                return null;
            }

            var prop = new GameObject(spriteName);
            prop.transform.SetParent(parent);
            prop.transform.position = position;
            prop.transform.localScale = new Vector3(flip ? -scale : scale, scale, 1f);
            var renderer = prop.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(1f, 1f, 1f, alpha);
            SetSorting(renderer, layer, order);
            return prop;
        }

        /// <summary>
        /// A hazard rooted in the level. Its cycle runs from the moment the scene loads, so its
        /// rhythm can be watched from outside its reach before anyone steps into it.
        /// </summary>
        private static void PlacePlant(Transform parent, GameObject prefab, Vector2 position)
        {
            if (prefab == null)
            {
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(parent);
            instance.transform.position = position;
        }

        /// <summary>
        /// The forest's own backdrop. It moves faster than the Tomb's, because trees at different
        /// depths separate far more than walls in a corridor do, and a still forest reads as a
        /// painted flat.
        /// </summary>
        private static void BuildForestParallax(Transform parent, Transform camera)
        {
            var layers = new[]
            {
                // Twenty units tall (ForestBackdrop). The forest is a tall zone now, so the strips
                // keep their height on the screen (ParallaxLayer._lockVertical) and scroll sideways.
                // SceneForestLook.Build fits them again after.
                ("env_forest_bg_far_01", "BackgroundFar", 0.12f, 0.04f, -4f, 1f),
                ("env_forest_bg_mid_01", "BackgroundMid", 0.32f, 0.09f, -4f, 1f),
                ("env_forest_bg_near_01", "BackgroundNear", 0.58f, 0.16f, -4f, 1f)
            };

            var root = Section(parent, "ForestParallax");
            SetString(root.gameObject.AddComponent<ZoneBackdrop>(), "_zoneId", ForestZoneId);
            foreach (var (spriteName, layer, factor, vertical, height, alpha) in layers)
            {
                var art = ForestArt(spriteName);
                if (art == null)
                {
                    continue;
                }

                var strip = new GameObject(spriteName);
                strip.transform.SetParent(root);
                // Centred on the zone rather than on the origin, so the strip covers it without
                // needing to be absurdly wide.
                strip.transform.position = new Vector3(185f, height, 0f);
                var renderer = strip.AddComponent<SpriteRenderer>();
                if (layer != "BackgroundNear")
                {
                    renderer.sharedMaterial = _material;
                }
                renderer.sprite = art;
                renderer.drawMode = SpriteDrawMode.Tiled;
                renderer.tileMode = SpriteTileMode.Continuous;
                renderer.size = new Vector2(art.bounds.size.x * 3f, art.bounds.size.y);
                renderer.color = new Color(1f, 1f, 1f, alpha);
                SetSorting(renderer, layer, 1);

                var parallax = strip.AddComponent<ParallaxLayer>();
                SetFloat(parallax, "_factor", factor);
                SetFloat(parallax, "_verticalFactor", vertical);
                // Twenty units of layer, its bottom eight under the view's centre: always behind the view.
                SetBool(parallax, "_lockVertical", true);
                SetFloat(parallax, "_verticalOffset", -8f);
                SetReference(parallax, "_camera", camera);
            }
        }

        /// <summary>
        /// Gives the ambience and the music a palette per region. Without this the forest would
        /// sound exactly like the crypt, which would undo most of the work of building it.
        /// </summary>
        private static void ConfigureZoneAudio(GameObject systems)
        {
            var ambience = systems.GetComponent<AmbienceController>();
            if (ambience != null)
            {
                var serialized = new SerializedObject(ambience);
                var palettes = serialized.FindProperty("_palettes");
                palettes.arraySize = 2;

                var tomb = palettes.GetArrayElementAtIndex(0);
                tomb.FindPropertyRelative("_zoneId").stringValue = TombZoneId;
                tomb.FindPropertyRelative("_bed").enumValueIndex = (int)AudioId.AmbienceTombBase;
                var tombDetails = tomb.FindPropertyRelative("_details");
                tombDetails.arraySize = 4;
                tombDetails.GetArrayElementAtIndex(0).enumValueIndex =
                    (int)AudioId.AmbienceStoneSettle;
                tombDetails.GetArrayElementAtIndex(1).enumValueIndex = (int)AudioId.AmbienceChain;
                tombDetails.GetArrayElementAtIndex(2).enumValueIndex = (int)AudioId.AmbienceDrip;
                tombDetails.GetArrayElementAtIndex(3).enumValueIndex = (int)AudioId.AmbienceWind;
                tomb.FindPropertyRelative("_minimumGap").floatValue = 7f;
                tomb.FindPropertyRelative("_maximumGap").floatValue = 21f;

                var forest = palettes.GetArrayElementAtIndex(1);
                forest.FindPropertyRelative("_zoneId").stringValue = ForestZoneId;
                forest.FindPropertyRelative("_bed").enumValueIndex = (int)AudioId.AmbienceForestBase;
                var forestDetails = forest.FindPropertyRelative("_details");
                forestDetails.arraySize = 5;
                forestDetails.GetArrayElementAtIndex(0).enumValueIndex = (int)AudioId.AmbienceLeaves;
                forestDetails.GetArrayElementAtIndex(1).enumValueIndex = (int)AudioId.AmbienceBranch;
                forestDetails.GetArrayElementAtIndex(2).enumValueIndex =
                    (int)AudioId.AmbienceInsects;
                forestDetails.GetArrayElementAtIndex(3).enumValueIndex =
                    (int)AudioId.AmbienceDistantBeast;
                forestDetails.GetArrayElementAtIndex(4).enumValueIndex = (int)AudioId.AmbienceWind;
                // A living wood is busier than a sealed crypt, so its details come closer together.
                forest.FindPropertyRelative("_minimumGap").floatValue = 5f;
                forest.FindPropertyRelative("_maximumGap").floatValue = 15f;

                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            var music = systems.GetComponent<MusicDirector>();
            if (music == null)
            {
                return;
            }

            // The procedural score: a track and fight layer per region, one shared boss track.
            SceneMusic.ConfigureLevel(music, PlaceholderMusicArt.Generate());
        }
    }
}
