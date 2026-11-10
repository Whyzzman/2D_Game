using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// Placeholder art for the things the hero can break (GAME_FEEL_IDEAS item 14): a funeral urn for
    /// the Tomb and the Capital, a heap of skulls for crypts, a crate for the Forest and the Village.
    /// Round, leaning shapes rather than boxes; each from its own seed. Feet at the bottom middle, as
    /// every prop. Rewritten by every build; drawn art replaces the file and is listed in `Art/Painted.txt`.
    /// </summary>
    public static class PlaceholderPropArt
    {
        public const string Root = "Assets/Game/Art/Environment/Breakables";
        public const string Urn = "env_break_urn_01";
        public const string Skulls = "env_break_skulls_01";
        public const string Crate = "env_break_crate_01";

        private static readonly Color32 Clay = new Color32(122, 92, 70, 255);
        private static readonly Color32 ClayDark = new Color32(70, 50, 40, 255);
        private static readonly Color32 ClayLit = new Color32(196, 160, 118, 255);
        private static readonly Color32 Bone = new Color32(206, 196, 172, 255);
        private static readonly Color32 BoneDark = new Color32(120, 110, 96, 255);
        private static readonly Color32 Socket = new Color32(28, 24, 22, 255);
        private static readonly Color32 Wood = new Color32(112, 80, 52, 255);
        private static readonly Color32 WoodDark = new Color32(62, 44, 30, 255);
        private static readonly Color32 WoodLit = new Color32(178, 136, 92, 255);
        private static readonly Color32 Iron = new Color32(70, 72, 78, 255);
        private static readonly Color32 Outline = new Color32(12, 10, 9, 220);
        private static readonly Color32 Rim = new Color32(206, 214, 232, 255);

        public static Dictionary<string, Sprite> Generate()
        {
            var sprites = new Dictionary<string, Sprite>();
            Add(sprites, Urn, DrawUrn());
            Add(sprites, Skulls, DrawSkulls());
            Add(sprites, Crate, DrawCrate());
            return sprites;
        }

        /// <summary>Which of them a zone breaks, by its rooms' prefix.</summary>
        public static string ForRoom(string roomId)
        {
            var id = roomId ?? string.Empty;
            if (id.StartsWith("room_forest") || id.StartsWith("room_village") || id.StartsWith("room_marsh"))
            {
                return Crate;
            }

            return id.Contains("crypt") || id.Contains("ossuary") || id.Contains("grave") ? Skulls : Urn;
        }

        private static void Add(Dictionary<string, Sprite> into, string name, Canvas canvas)
        {
            canvas.Silhouette(Outline, Rim, 0.35f);
            var path = $"{Root}/{name}.png";
            PlaceholderImport.EnsureFolder(Root);
            PlaceholderImport.WriteIfChanged(path, canvas.EncodePng());
            PlaceholderImport.ConfigureImporter(path, new Vector2(0.5f, 0f));
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogError($"Breakable texture failed to import: {path}");
            }

            into[name] = sprite;
        }

        /// <summary>A pot-bellied funeral urn with a narrow neck and a chipped lip, leaning a little.</summary>
        private static Canvas DrawUrn()
        {
            var c = new Canvas(80, 100);
            var rng = new System.Random(7101);
            c.Ellipse(new Vector2(40, 38), 27, 32, Clay);
            c.Ellipse(new Vector2(40, 8), 16, 7, ClayDark);
            c.Quad(new Vector2(29, 64), new Vector2(51, 64), new Vector2(47, 84), new Vector2(33, 84), Clay);
            c.Ellipse(new Vector2(40, 86), 14, 5, ClayDark);
            c.Ellipse(new Vector2(40, 87), 11, 3, Socket);
            // the light from above and behind: a soft crescent on the shoulder
            c.Ellipse(new Vector2(31, 50), 8, 16, ClayLit);
            c.Ellipse(new Vector2(34, 46), 6, 14, Clay);
            // a band of painted marks and a chip out of the lip
            for (var x = 18; x < 62; x += 7 + rng.Next(0, 3)) { c.Ellipse(new Vector2(x, 40), 2.2f, 3f, ClayDark); }
            c.Triangle(new Vector2(46, 88), new Vector2(54, 88), new Vector2(50, 80), new Color32(0, 0, 0, 0));
            return c;
        }

        /// <summary>A heap of skulls on a few long bones, the top one tipped toward the viewer.</summary>
        private static Canvas DrawSkulls()
        {
            var c = new Canvas(100, 80);
            c.Thick(new Vector2(8, 8), new Vector2(92, 14), 6, 6, BoneDark);
            c.Thick(new Vector2(12, 16), new Vector2(88, 6), 5, 5, Bone);
            foreach (var (x, y, r) in new[] { (28f, 26f, 17f), (60f, 24f, 16f), (44f, 50f, 17f) })
            {
                c.Ellipse(new Vector2(x, y), r, r * 0.92f, Bone);
                c.Ellipse(new Vector2(x - r * 0.35f, y - r * 0.1f), r * 0.24f, r * 0.28f, Socket);
                c.Ellipse(new Vector2(x + r * 0.35f, y - r * 0.1f), r * 0.24f, r * 0.28f, Socket);
                c.Triangle(new Vector2(x - 2, y - r * 0.45f), new Vector2(x + 2, y - r * 0.45f), new Vector2(x, y - r * 0.25f), Socket);
                c.Ellipse(new Vector2(x - r * 0.3f, y + r * 0.45f), r * 0.35f, r * 0.2f, new Color32(232, 224, 204, 255));
            }

            return c;
        }

        /// <summary>A crate with iron corners, one plank sprung and a sag in its lid.</summary>
        private static Canvas DrawCrate()
        {
            var c = new Canvas(90, 80);
            c.Quad(new Vector2(6, 2), new Vector2(84, 2), new Vector2(82, 66), new Vector2(8, 70), Wood);
            for (var y = 14; y < 66; y += 17) { c.Thick(new Vector2(8, y), new Vector2(82, y - 1), 2.5f, 2.5f, WoodDark); }
            c.Thick(new Vector2(10, 6), new Vector2(80, 64), 5, 5, WoodDark);
            c.Thick(new Vector2(12, 66), new Vector2(46, 61), 4, 4, WoodLit);
            c.Thick(new Vector2(46, 61), new Vector2(80, 64), 4, 4, WoodLit);
            c.Thick(new Vector2(60, 30), new Vector2(88, 40), 5, 4, Wood);
            foreach (var (x, y) in new[] { (9f, 5f), (81f, 5f), (80f, 63f), (10f, 66f) }) { c.Ellipse(new Vector2(x, y), 5, 5, Iron); }
            return c;
        }
    }
}
