using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// Every ledge of the written zones (<c>=</c>, built as <c>Ledge_x_y</c>) becomes a
    /// <see cref="OneWayPlatform"/>: a jump from under it lands on top instead of knocking the head
    /// (user's request 2026-10-25). <c>BuildLayout</c> adds it to new builds; this brings the zone
    /// scenes already built up to date. Which blocks are ledges is read from the zones' text, so a
    /// ledge the text has since turned to stone (a gate kept shut) stays solid without a rebuild.
    /// Touches nothing else; running it again changes nothing.
    /// -executeMethod ForgottenKingdom.EditorTools.SceneOneWayLedges.Apply
    /// </summary>
    public static class SceneOneWayLedges
    {
        [MenuItem("Forgotten Kingdom/Scene/Apply One-Way Ledges")]
        public static void Apply()
        {
            WorldScenes.Edit(Build);
            Debug.Log("One-way ledges applied to the world.");
        }

        /// <summary>The recipe: every loaded collider named as a ledge block the text still calls a ledge.</summary>
        public static void Build()
        {
            var ledges = LedgeNames();
            var oneWay = 0;
            var solid = 0;
            foreach (var collider in Object.FindObjectsByType<BoxCollider2D>(FindObjectsInactive.Include))
            {
                var name = collider.gameObject.name;
                if (!name.StartsWith("Ledge_") || collider.isTrigger)
                {
                    continue;
                }

                var platform = collider.GetComponent<OneWayPlatform>();
                if (ledges.Contains(name))
                {
                    if (platform == null)
                    {
                        collider.gameObject.AddComponent<OneWayPlatform>();
                        EditorUtility.SetDirty(collider.gameObject);
                    }

                    oneWay++;
                }
                else
                {
                    if (platform != null)
                    {
                        Object.DestroyImmediate(platform);
                        var effector = collider.GetComponent<PlatformEffector2D>();
                        if (effector != null)
                        {
                            Object.DestroyImmediate(effector);
                        }

                        collider.usedByEffector = false;
                        EditorUtility.SetDirty(collider.gameObject);
                    }

                    solid++;
                }
            }

            Debug.Log($"SceneOneWayLedges: {oneWay} ledges are one-way, {solid} kept solid.");
        }

        /// <summary>The block names of every ledge in the zones' text, as <c>BuildLayout</c> names them.</summary>
        public static HashSet<string> LedgeNames()
        {
            var names = new HashSet<string>();
            if (!Directory.Exists(ContentBuilder.LevelFolder))
            {
                return names;
            }

            foreach (var path in Directory.GetFiles(ContentBuilder.LevelFolder, "*.txt"))
            {
                var zone = LevelZone.Parse(File.ReadAllText(path));
                foreach (var piece in LevelSolids.Blocks(zone))
                {
                    if (piece.Kind == LevelCell.Ledge)
                    {
                        names.Add($"{piece.Kind}_{piece.Cells.x}_{piece.Cells.y}");
                    }
                }
            }

            return names;
        }
    }
}
