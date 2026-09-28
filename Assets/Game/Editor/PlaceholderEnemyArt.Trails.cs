using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The strips the enemies' live pieces are drawn with (visual item 4, the user's choice of
    /// 2026-09-28: secondary motion on everyone who has something loose). Same contract as the
    /// hero's cloak edge: root on the left, where it hides under the drawn body, the free end on
    /// the right; same outline and a warm rim, so it reads as part of the figure.
    /// </summary>
    public static partial class PlaceholderEnemyArt
    {
        public const string TrailFolder = "Assets/Game/Art/Characters/Trails";

        /// <summary>One live piece: its strip and how it hangs.</summary>
        public readonly struct TrailSpec
        {
            public readonly string Texture;
            public readonly float Length;
            public readonly float Width;
            public readonly float Stiffness;
            public readonly float Damping;

            public TrailSpec(string texture, float length, float width, float stiffness, float damping)
            {
                Texture = texture;
                Length = length;
                Width = width;
                Stiffness = stiffness;
                Damping = damping;
            }
        }

        /// <summary>Which prefab hangs what, by the prefab's root name. The guard has nothing loose.</summary>
        public static readonly Dictionary<string, TrailSpec> Trails = new Dictionary<string, TrailSpec>
        {
            ["ZombieSoldier"] = new TrailSpec(TrailFolder + "/trail_zombie_rags.png", 0.34f, 0.16f, 11f, 0.34f),
            ["CorruptedHound"] = new TrailSpec(TrailFolder + "/trail_hound_tail.png", 0.34f, 0.07f, 16f, 0.42f),
            ["ForestArcher"] = new TrailSpec(TrailFolder + "/trail_archer_hood.png", 0.3f, 0.11f, 12f, 0.38f),
            ["RootShaman"] = new TrailSpec(TrailFolder + "/trail_shaman_robe.png", 0.3f, 0.2f, 10f, 0.4f),
            ["RoyalSpearman"] = new TrailSpec(TrailFolder + "/trail_spearman_scarf.png", 0.44f, 0.09f, 9f, 0.3f),
            ["GuardianOfTheTomb"] = new TrailSpec(TrailFolder + "/trail_guardian_cape.png", 0.6f, 0.36f, 8f, 0.4f),
            ["HollowWarden"] = new TrailSpec(TrailFolder + "/trail_warden_roots.png", 0.62f, 0.3f, 9f, 0.36f)
        };

        /// <summary>Writes every strip and returns them by prefab name.</summary>
        public static Dictionary<string, Texture2D> GenerateTrails()
        {
            PlaceholderImport.EnsureFolder(TrailFolder);
            var zombie = BuildFor(Actor.Zombie);
            var hound = ForestBuildFor(Actor.Hound);
            var archer = ForestBuildFor(Actor.Archer);
            var shaman = ForestBuildFor(Actor.Shaman);
            var spearman = ForestBuildFor(Actor.Spearman);
            var guardian = BuildFor(Actor.BossPhaseOne);
            var warden = ForestBuildFor(Actor.WardenPhaseOne);

            var result = new Dictionary<string, Texture2D>();
            Write(result, "ZombieSoldier", Cloth(zombie.Cloth, torn: 3, holes: true));
            Write(result, "CorruptedHound", Tail(hound.Armor, hound.Cloth));
            Write(result, "ForestArcher", Cloth(archer.Cloth, torn: 1, holes: false));
            Write(result, "RootShaman", Fringe(shaman.Cloth));
            Write(result, "RoyalSpearman", Scarf(spearman.Cloth, spearman.Trim));
            Write(result, "GuardianOfTheTomb", Cloth(guardian.Cloth, torn: 3, holes: false));
            Write(result, "HollowWarden", Roots(warden.Metal, warden.Cloth));
            return result;
        }

        private static void Write(Dictionary<string, Texture2D> into, string key, Canvas canvas)
        {
            canvas.Silhouette(new Color32(6, 5, 5, 225), EnemyRim, 0.42f);
            var path = Trails[key].Texture;
            PlaceholderImport.WriteIfChanged(path, canvas.EncodePng());
            PlaceholderImport.ConfigureImporter(path);
            into[key] = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>A length of cloth ending in <paramref name="torn"/> ragged tongues.</summary>
        private static Canvas Cloth(Color32 tone, int torn, bool holes)
        {
            var c = new Canvas(64, 24);
            c.Thick(new Vector2(-6f, 12f), new Vector2(46f, 12f), 16f, 12f, tone);
            for (var i = 0; i < torn; i++)
            {
                var y = 6f + i * (12f / Mathf.Max(1, torn - 1));
                c.Triangle(new Vector2(44f, y - 3f), new Vector2(44f, y + 3f),
                    new Vector2(54f + (i % 2) * 6f, y), tone);
            }

            if (holes)
            {
                c.Ellipse(new Vector2(30f, 10f), 2.5f, 2f, new Color32(0, 0, 0, 0));
                c.Ellipse(new Vector2(40f, 15f), 2f, 1.6f, new Color32(0, 0, 0, 0));
            }

            // A darker fold along the lower edge.
            c.RectOver(0f, 5f, 64f, 8f, Color32.Lerp(tone, Charcoal, 0.4f));
            return c;
        }

        private static Canvas Tail(Color32 hide, Color32 corruption)
        {
            var c = new Canvas(64, 24);
            c.Thick(new Vector2(-4f, 12f), new Vector2(58f, 12f), 9f, 2f, hide);
            for (var i = 0; i < 3; i++)
            {
                c.Ellipse(new Vector2(12f + i * 14f, 13f), 2f, 1.6f, corruption);
            }

            return c;
        }

        /// <summary>A robe's hem coming apart into hanging threads.</summary>
        private static Canvas Fringe(Color32 tone)
        {
            var c = new Canvas(64, 24);
            c.Thick(new Vector2(-6f, 12f), new Vector2(30f, 12f), 18f, 16f, tone);
            for (var i = 0; i < 6; i++)
            {
                var y = 5f + i * 2.8f;
                c.Thick(new Vector2(28f, y), new Vector2(50f + (i * 7) % 12, y + (i % 2 == 0 ? 1f : -1f)),
                    2f, 1f, tone);
            }

            return c;
        }

        private static Canvas Scarf(Color32 tone, Color32 trim)
        {
            var c = new Canvas(64, 24);
            c.Thick(new Vector2(-6f, 12f), new Vector2(50f, 12f), 10f, 9f, tone);
            c.Triangle(new Vector2(48f, 16f), new Vector2(48f, 8f), new Vector2(60f, 13f), tone);
            // The faded royal stripe.
            c.RectOver(0f, 11f, 60f, 13f, Color32.Lerp(trim, tone, 0.45f));
            return c;
        }

        /// <summary>The Warden's cloak is roots: a braid of them, splitting into strands at the end.</summary>
        private static Canvas Roots(Color32 bark, Color32 corruption)
        {
            var c = new Canvas(64, 24);
            c.Thick(new Vector2(-6f, 12f), new Vector2(26f, 12f), 16f, 12f, bark);
            for (var i = 0; i < 5; i++)
            {
                var y = 6f + i * 3f;
                c.Thick(new Vector2(22f, y), new Vector2(56f + (i % 3) * 3f, y + (i - 2) * 1.5f), 3.5f, 1f, bark);
            }

            c.Ellipse(new Vector2(14f, 12f), 2.2f, 2f, corruption);
            return c;
        }
    }
}
