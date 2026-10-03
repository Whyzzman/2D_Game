using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// Readability of the Tomb (the user's request of 2026-09-28: keep the style, make it clear
    /// what is solid and take away what looks wrong). Nothing here has a collider or moves a
    /// block; it only dresses what is there:
    /// the walls of the Bell Shaft, which hung from nothing, are carried up into the vault; every
    /// floating slab hangs from two chains; the far bank of fog, which greyed the whole middle of
    /// the frame, is thinner. The tiles, the background piers, the rubble and the finds' halo are
    /// the generator's (<see cref="PlaceholderTombArt"/>, <see cref="GothicBackdrop"/>).
    ///
    /// A scene edit like the other Scene*.Apply: it replaces its own "TombLook" object and sets
    /// the fog to a fixed value, so running it again changes nothing.
    /// Run: -executeMethod ForgottenKingdom.EditorTools.SceneTombLook.Apply
    /// </summary>
    public static class SceneTombLook
    {
        public const string RootName = "TombLook";
        private const string TileFolder = PlaceholderTombArt.Root + "/Tiles";
        private const string TombZone = "zone_forgotten_tomb";
        private const float TombEastEdge = 62.5f;
        private const float VaultTop = 20f;
        private const float FarFogAlpha = 0.11f;
        private const int GroundLayer = 9;

        [MenuItem("Forgotten Kingdom/Scene/Apply Tomb Look")]
        public static void Apply()
        {
            WorldScenes.Edit(() =>
            {
                var level = GameObject.Find("Level");
                if (level == null)
                {
                    Debug.LogError("No Level to dress.");
                    return;
                }

                var old = level.transform.Find(RootName);
                if (old != null)
                {
                    Object.DestroyImmediate(old.gameObject);
                }

                var root = new GameObject(RootName).transform;
                root.SetParent(level.transform, false);
                Build(level.transform, root);
            });
            Debug.Log("Tomb look applied to the world.");
        }

        /// <summary>The recipe, for a scene opened whole.</summary>
        public static void Build(Transform level, Transform root)
        {
            var wall = AssetDatabase.LoadAssetAtPath<Sprite>($"{TileFolder}/env_tomb_wall_01.png");
            var chain = AssetDatabase.LoadAssetAtPath<Sprite>($"{TileFolder}/env_tomb_chain_tile_01.png");
            Physics2D.SyncTransforms();
            var previous = Physics2D.queriesHitTriggers;
            Physics2D.queriesHitTriggers = false;
            var layout = level.Find(ContentBuilder.TombRootName);
            foreach (var box in Solids(level))
            {
                var bounds = box.bounds;
                var upright = bounds.size.y > bounds.size.x * 2f;
                // The Tomb built from its text has walls to its vaults already; only its slabs hang.
                var fromText = layout != null && box.transform.IsChildOf(layout);
                if (upright && bounds.min.y > 1f && wall != null && !fromText)
                {
                    CarryUp(root, wall, box, bounds);
                }
                else if (!upright && bounds.size.y <= 0.7f && bounds.size.x <= 7f && (bounds.min.y > 0.8f || fromText)
                    && chain != null)
                {
                    Hang(root, chain, box, bounds.min.x + 0.35f, bounds.max.y);
                    Hang(root, chain, box, bounds.max.x - 0.35f, bounds.max.y);
                }
            }

            Physics2D.queriesHitTriggers = previous;
            ThinTheFog();
        }

        /// <summary>
        /// The Tomb's solid blocks as they stand, less the exploration layer (its hidden rooms
        /// are walled in already) and anything switched off, such as the arena's barrier.
        /// </summary>
        private static IEnumerable<BoxCollider2D> Solids(Transform level)
        {
            var exploration = level.Find("Exploration");
            foreach (var box in level.GetComponentsInChildren<BoxCollider2D>())
            {
                if (box.isTrigger || box.gameObject.layer != GroundLayer || box.bounds.center.x > TombEastEdge
                    || (exploration != null && box.transform.IsChildOf(exploration)))
                {
                    continue;
                }

                yield return box;
            }
        }

        /// <summary>From the top of a hanging wall up to whatever is above it, or into the dark.</summary>
        private static void CarryUp(Transform root, Sprite wall, BoxCollider2D box, Bounds bounds)
        {
            var top = Above(new Vector2(bounds.center.x, bounds.max.y), box);
            var height = top - bounds.max.y;
            if (height < 0.2f)
            {
                return;
            }

            var piece = new GameObject($"{box.name}_Up");
            piece.transform.SetParent(root, false);
            piece.transform.position = new Vector2(bounds.center.x, bounds.max.y + height * 0.5f);
            var renderer = piece.AddComponent<SpriteRenderer>();
            renderer.sprite = wall;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = new Vector2(bounds.size.x, height);
            // A touch darker than the wall itself: out of reach, it should not ask to be climbed.
            renderer.color = new Color(0.72f, 0.72f, 0.78f);
            Sorting(renderer, -1);
        }

        private static void Hang(Transform root, Sprite chain, BoxCollider2D box, float x, float from)
        {
            var top = Above(new Vector2(x, from), box);
            var height = top - from;
            if (height < 0.5f)
            {
                return;
            }

            var piece = new GameObject($"{box.name}_Chain");
            piece.transform.SetParent(root, false);
            piece.transform.position = new Vector2(x, from + height * 0.5f);
            var renderer = piece.AddComponent<SpriteRenderer>();
            renderer.sprite = chain;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = new Vector2(0.2f, height);
            Sorting(renderer, -2);
        }

        /// <summary>The underside of the first solid thing over a point, or the vault.</summary>
        private static float Above(Vector2 point, Collider2D self)
        {
            // Up to the old vault, or for the rooms of the rebuilt Tomb (up to 73 high) to their ceiling.
            foreach (var hit in Physics2D.RaycastAll(point + Vector2.up * 0.02f, Vector2.up, Mathf.Max(VaultTop - point.y, 40f)))
            {
                if (hit.collider != self && !hit.collider.isTrigger && hit.collider.gameObject.layer == GroundLayer)
                {
                    return hit.point.y;
                }
            }

            return Mathf.Max(VaultTop, point.y);
        }

        private static void ThinTheFog()
        {
            foreach (var renderer in Object.FindObjectsByType<SpriteRenderer>())
            {
                var bank = renderer.transform.parent;
                if (renderer.name == "FogBand" && bank != null && bank.name == "Fog_" + TombZone
                    && renderer.sortingLayerName == "BackgroundMid")
                {
                    var color = renderer.color;
                    color.a = FarFogAlpha;
                    renderer.color = color;
                    EditorUtility.SetDirty(renderer);
                }
            }
        }

        private static void Sorting(SpriteRenderer renderer, int order)
        {
            renderer.sortingLayerName = "GameplayEnvironment";
            renderer.sortingOrder = order;
        }
    }
}
