using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// Brings the Withered Forest up to the Tomb (visual item 1, the user's choices of 2026-09-28):
    /// the new sky-to-trunk backdrop fitted to the parallax strips, the Warden's roofless abbey
    /// behind the arena, moss and roots on every walkable edge, and moonlight falling through the
    /// trees.
    ///
    /// Scenes are the source of truth, so this is a scene edit, run once and again whenever the
    /// recipe changes. It touches only what it owns: the forest's parallax strips and arena
    /// backdrop (by name), the "ForestEdge*" children it adds to forest blocks and its own
    /// "ForestLook" object. Colliders are never touched.
    /// Run: -executeMethod ForgottenKingdom.EditorTools.SceneForestLook.Apply
    /// </summary>
    public static class SceneForestLook
    {
        public const string RootName = "ForestLook";
        public const string EdgePrefix = "ForestEdge";
        public const string ParallaxName = "ForestParallax";
        public const string ArenaBackdropName = "env_forest_arena_backdrop";

        /// <summary>The forest starts east of this; the Tomb's blocks are left alone.</summary>
        public const float ForestStartX = 62.5f;

        private const string ForestZone = "zone_withered_forest";
        private const string ShaftPath = "Assets/Game/Art/Shapes/shape_light_shaft.png";
        private const string UnlitPath = "Assets/Game/Art/PlaceholderUnlit.mat";
        private const int GroundLayer = 9;

        /// <summary>The surfaces that are ground to walk on, and so get a living edge.</summary>
        private static readonly HashSet<string> Walkable = new HashSet<string>
        {
            "env_forest_ground_01", "env_forest_road_01", "env_forest_cliff_01"
        };

        [MenuItem("Forgotten Kingdom/Scene/Apply Forest Look")]
        public static void Apply()
        {
            WorldScenes.Edit(Build);
            Debug.Log("Forest look applied to the world.");
        }

        /// <summary>The recipe, also for a scene rebuilt from scratch: run after the forest exists.</summary>
        public static void Build()
        {
            var art = PlaceholderForestArt.Generate();
            var unlit = AssetDatabase.LoadAssetAtPath<Material>(UnlitPath);
            // The painted Forest (item 4 of the zone rework) draws its sky, abbey and mossy edges
            // itself; the tiled one is fitted here. Moonlight falls on both.
            if (!ContentBuilder.HasPaintedArt(ContentBuilder.ForestArtFolder))
            {
                FitParallax(art, unlit);
                FitArenaBackdrop(art, unlit);
                DressEdges(art);
            }
            else
            {
                // The painted arena has its own backdrop (painter, then Midjourney): the old abbey
                // sprite would stand in front of it.
                var abbey = GameObject.Find(ArenaBackdropName);
                if (abbey != null)
                {
                    Object.DestroyImmediate(abbey);
                }
            }

            var old = GameObject.Find(RootName);
            if (old != null)
            {
                Object.DestroyImmediate(old);
            }

            var root = new GameObject(RootName).transform;
            BuildMoonlight(root, unlit);
        }

        // ---------- backdrop ----------

        /// <summary>
        /// Three screens wide, floor four units up, unscaled: the layers are drawn 20 units tall
        /// for exactly this placement, so the sky reaches past the top of every climb.
        /// </summary>
        private static void FitParallax(Dictionary<string, Sprite> art, Material unlit)
        {
            var parallax = GameObject.Find(ParallaxName);
            if (parallax == null)
            {
                Debug.LogError($"{ParallaxName} is missing from the scene.");
                return;
            }

            foreach (var renderer in parallax.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (!art.TryGetValue(renderer.name, out var sprite) || sprite == null)
                {
                    continue;
                }

                renderer.transform.position = new Vector3(185f, -4f, 0f);
                renderer.transform.localScale = Vector3.one;
                renderer.sprite = sprite;
                renderer.drawMode = SpriteDrawMode.Tiled;
                renderer.tileMode = SpriteTileMode.Continuous;
                renderer.size = new Vector2(sprite.bounds.size.x * 3f, sprite.bounds.size.y);
                renderer.color = Color.white;
                // Far and mid carry their own values; the near trunks catch the room's light.
                if (renderer.sortingLayerName != "BackgroundNear" && unlit != null)
                {
                    renderer.sharedMaterial = unlit;
                }

                EditorUtility.SetDirty(renderer);
                EditorUtility.SetDirty(renderer.transform);
            }
        }

        /// <summary>The abbey, shown at twice its drawn size across the whole arena and above it.</summary>
        private static void FitArenaBackdrop(Dictionary<string, Sprite> art, Material unlit)
        {
            var backdrop = GameObject.Find(ArenaBackdropName);
            if (backdrop == null || !art.TryGetValue(ArenaBackdropName, out var sprite))
            {
                Debug.LogError($"{ArenaBackdropName} is missing from the scene.");
                return;
            }

            var arena = ContentBuilder.ForestRoomBounds(ContentBuilder.ForestArenaRoom);
            backdrop.transform.position = new Vector3(arena.center.x, arena.yMin + 1f - 2f, 0f);
            backdrop.transform.localScale = new Vector3(2f, 2f, 1f);
            var renderer = backdrop.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.white;
            if (unlit != null)
            {
                renderer.sharedMaterial = unlit;
            }

            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(backdrop.transform);
        }

        // ---------- edges ----------

        /// <summary>
        /// Every walkable forest block gets a mat of moss along its top; a ledge off the ground
        /// gets roots hanging under it; an end that nothing else continues gets a clump that
        /// rounds its corner.
        /// </summary>
        private static void DressEdges(Dictionary<string, Sprite> art)
        {
            art.TryGetValue("env_forest_edge_top_01", out var top);
            art.TryGetValue("env_forest_edge_roots_01", out var roots);
            art.TryGetValue("env_forest_edge_cap_01", out var cap);

            var blocks = new List<(BoxCollider2D box, Bounds bounds)>();
            foreach (var box in Object.FindObjectsByType<BoxCollider2D>(FindObjectsInactive.Include))
            {
                if (box.gameObject.layer == GroundLayer && !box.isTrigger)
                {
                    blocks.Add((box, BoundsOf(box)));
                }
            }

            foreach (var (box, bounds) in blocks)
            {
                var block = box.transform;
                for (var i = block.childCount - 1; i >= 0; i--)
                {
                    if (block.GetChild(i).name.StartsWith(EdgePrefix))
                    {
                        Object.DestroyImmediate(block.GetChild(i).gameObject);
                    }
                }

                if (!IsWalkableForest(box, bounds))
                {
                    continue;
                }

                var topY = bounds.max.y;
                // Only a top the hero can stand on: a block with rock on it is inside the rock.
                var open = !Continues(blocks, box, new Vector2(bounds.center.x, topY + 0.1f));
                if (!open)
                {
                    continue;
                }

                // The mat's top row (30 of 56 px) sits on the collider's top.
                Strip(block, EdgePrefix + "Top", top, new Vector2(bounds.center.x, topY - 0.02f),
                    new Vector2(bounds.size.x + 0.1f, 0.56f), 2);

                var floating = !Continues(blocks, box, new Vector2(bounds.center.x, bounds.min.y - 0.1f))
                    && bounds.size.y <= 1.1f;
                if (floating && bounds.size.x >= 1.2f)
                {
                    Strip(block, EdgePrefix + "Roots", roots,
                        new Vector2(bounds.center.x, bounds.min.y - 0.44f),
                        new Vector2(bounds.size.x - 0.2f, 1f), -1);
                }

                foreach (var side in new[] { -1f, 1f })
                {
                    var probe = new Vector2(side > 0f ? bounds.max.x + 0.08f : bounds.min.x - 0.08f,
                        topY - 0.08f);
                    if (Continues(blocks, box, probe))
                    {
                        continue;
                    }

                    var corner = new GameObject(EdgePrefix + (side > 0f ? "CapRight" : "CapLeft"));
                    corner.transform.SetParent(block, false);
                    corner.transform.position = new Vector3(
                        (side > 0f ? bounds.max.x : bounds.min.x) + side * 0.1f, topY - 0.14f, 0f);
                    corner.transform.localScale = new Vector3(side, 1f, 1f);
                    corner.transform.rotation = Quaternion.identity;
                    var renderer = corner.AddComponent<SpriteRenderer>();
                    renderer.sprite = cap;
                    SetSorting(renderer, "GameplayEnvironment", 3);
                }
            }
        }

        private static bool IsWalkableForest(BoxCollider2D box, Bounds bounds)
        {
            if (bounds.center.x < ForestStartX || bounds.size.x < 0.8f
                || box.GetComponent<NonClimbableSurface>() != null)
            {
                return false;
            }

            var visual = box.transform.Find("Visual");
            var sprite = visual != null ? visual.GetComponent<SpriteRenderer>()?.sprite : null;
            return sprite != null && Walkable.Contains(sprite.name);
        }

        private static bool Continues(List<(BoxCollider2D box, Bounds bounds)> blocks, BoxCollider2D self,
            Vector2 probe)
        {
            foreach (var (box, bounds) in blocks)
            {
                if (box != self && bounds.Contains(new Vector3(probe.x, probe.y, bounds.center.z)))
                {
                    return true;
                }
            }

            return false;
        }

        private static Bounds BoundsOf(BoxCollider2D box)
        {
            var t = box.transform;
            var centre = t.TransformPoint(box.offset);
            var size = Vector2.Scale(box.size, new Vector2(Mathf.Abs(t.lossyScale.x), Mathf.Abs(t.lossyScale.y)));
            return new Bounds(new Vector3(centre.x, centre.y, 0f), new Vector3(size.x, size.y, 1f));
        }

        private static void Strip(Transform block, string name, Sprite sprite, Vector2 centre, Vector2 size,
            int order)
        {
            if (sprite == null)
            {
                return;
            }

            var strip = new GameObject(name);
            strip.transform.SetParent(block, false);
            strip.transform.position = centre;
            strip.transform.rotation = Quaternion.identity;
            var lossy = block.lossyScale;
            strip.transform.localScale = new Vector3(1f / Mathf.Max(0.0001f, Mathf.Abs(lossy.x)),
                1f / Mathf.Max(0.0001f, Mathf.Abs(lossy.y)), 1f);
            var renderer = strip.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = size;
            SetSorting(renderer, "GameplayEnvironment", order);
        }

        // ---------- moonlight ----------

        /// <summary>
        /// Shafts of moonlight slanting down through the trees of every open room, from the upper
        /// right where the moon stands; one straight down into the abbey. Guide the eye,
        /// light nothing — like the Tomb's.
        /// </summary>
        private static void BuildMoonlight(Transform root, Material unlit)
        {
            SetString(root.gameObject.AddComponent<ZoneBackdrop>(), "_zoneId", ForestZone);
            var shaft = AssetDatabase.LoadAssetAtPath<Sprite>(ShaftPath);
            if (shaft == null)
            {
                Debug.LogError($"{ShaftPath} is missing; run the content build first.");
                return;
            }

            // One shaft through the canopy of every open room, slanting from the moon's side.
            var zone = ContentBuilder.ForestLayout;
            foreach (var room in zone.Rooms)
            {
                if (room.IsSecret || ContentBuilder.ForestEnclosedRooms.Contains(room.Id))
                {
                    continue;
                }

                var bounds = zone.WorldBounds(room);
                var hash = Mathf.Abs(room.Id.GetHashCode());
                var x = Mathf.Lerp(bounds.xMin, bounds.xMax, 0.3f + (hash % 40) / 100f);
                var tilt = room.Id == ContentBuilder.ForestArenaRoom ? 0f : -10f - hash % 6;
                var width = Mathf.Clamp(bounds.width * 0.12f, 2f, 6f);
                var alpha = 0.06f + (hash % 3) * 0.01f;
                var beam = new GameObject("Moonlight");
                beam.transform.SetParent(root, false);
                beam.transform.position = new Vector2(x, bounds.center.y);
                beam.transform.rotation = Quaternion.Euler(0f, 0f, tilt);
                beam.transform.localScale = new Vector3(width, bounds.height, 1f);
                var renderer = beam.AddComponent<SpriteRenderer>();
                renderer.sprite = shaft;
                renderer.color = new Color(0.74f, 0.9f, 0.88f, alpha);
                if (unlit != null)
                {
                    renderer.sharedMaterial = unlit;
                }

                SetSorting(renderer, "BackgroundNear", 8);
            }
        }

        // ---------- helpers ----------

        private static void SetSorting(SpriteRenderer renderer, string layer, int order)
        {
            renderer.sortingLayerName = layer;
            renderer.sortingOrder = order;
        }

        private static void SetString(Object target, string field, string value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
