using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The first people of the world (item 70, the user's choice of 2026-09-28): Hnat the buyer
    /// with his cart, Myroslava the pilgrim with a lantern on her staff, and the ghost of Severyn
    /// of the old guard — plus the merchant's locked chest, shut and open. Drawn facing right,
    /// pivot at the feet, 100 PPU, under Characters so they get normal maps like everyone else.
    /// Their edge is a soft neutral: not the hero's silver, not an enemy's warmth.
    /// </summary>
    public static class PlaceholderNpcArt
    {
        public const string Folder = "Assets/Game/Art/Characters/Npc";
        public const string GnatPath = Folder + "/npc_gnat.png";
        public const string MyroslavaPath = Folder + "/npc_myroslava.png";
        public const string SeverynPath = Folder + "/npc_severyn.png";
        public const string OstapPath = Folder + "/npc_ostap.png";
        public const string ChestClosedPath = Folder + "/chest_locked.png";
        public const string ChestOpenPath = Folder + "/chest_open.png";

        private static readonly Color32 Outline = new Color32(8, 7, 6, 230);
        private static readonly Color32 Edge = new Color32(176, 166, 146, 255);
        private static readonly Color32 Wood = new Color32(0x4A, 0x3B, 0x2E, 0xFF);
        private static readonly Color32 WoodDark = new Color32(0x2E, 0x25, 0x1D, 0xFF);
        private static readonly Color32 Iron = new Color32(0x3E, 0x42, 0x47, 0xFF);
        private static readonly Color32 Coat = new Color32(0x4E, 0x3E, 0x30, 0xFF);
        private static readonly Color32 CoatDark = new Color32(0x33, 0x29, 0x21, 0xFF);
        private static readonly Color32 Sack = new Color32(0x6B, 0x5C, 0x44, 0xFF);
        private static readonly Color32 Skin = new Color32(0x7A, 0x6F, 0x66, 0xFF);
        private static readonly Color32 Robe = new Color32(0x3B, 0x40, 0x48, 0xFF);
        private static readonly Color32 RobeLight = new Color32(0x55, 0x5C, 0x66, 0xFF);
        private static readonly Color32 Flame = new Color32(0xF0, 0xB4, 0x41, 0xFF);
        private static readonly Color32 FlameCore = new Color32(0xFF, 0xE2, 0x9A, 0xFF);
        private static readonly Color32 Gold = new Color32(0xB0, 0x8A, 0x3E, 0xFF);
        private static readonly Color32 Ghost = new Color32(0x8C, 0xA2, 0xB8, 0xFF);
        private static readonly Color32 GhostLight = new Color32(0xC2, 0xD4, 0xE4, 0xFF);
        private static readonly Color32 GhostDark = new Color32(0x5A, 0x6C, 0x80, 0xFF);

        public static (Sprite gnat, Sprite myroslava, Sprite severyn, Sprite ostap, Sprite chestClosed, Sprite chestOpen) Generate()
        {
            PlaceholderImport.EnsureFolder(Folder);
            Write(GnatPath, Gnat());
            Write(MyroslavaPath, Myroslava());
            Write(SeverynPath, Severyn());
            Write(OstapPath, Ostap());
            Write(ChestClosedPath, Chest(open: false));
            Write(ChestOpenPath, Chest(open: true));
            return (Load(GnatPath), Load(MyroslavaPath), Load(SeverynPath), Load(OstapPath), Load(ChestClosedPath),
                Load(ChestOpenPath));
        }

        private static Sprite Load(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        private static void Write(string path, Canvas canvas)
        {
            PlaceholderImport.WriteIfChanged(path, canvas.EncodePng());
            PlaceholderImport.ConfigureImporter(path);
        }

        /// <summary>A stooped man in a wide hat, pack on his back, pulling a laden cart behind him.</summary>
        private static Canvas Gnat()
        {
            var c = new Canvas(280, 210);

            // The cart, behind him on the left: two wheels, a plank bed, sacks and a lantern pole.
            c.Rect(20f, 44f, 150f, 96f, Wood);
            for (var x = 32f; x < 150f; x += 24f)
            {
                c.Rect(x, 46f, x + 2f, 94f, WoodDark);
            }

            c.Thick(new Vector2(18f, 96f), new Vector2(152f, 96f), 5f, 5f, WoodDark);
            c.Ellipse(new Vector2(52f, 108f), 20f, 16f, Sack);
            c.Ellipse(new Vector2(88f, 112f), 24f, 20f, CoatDark);
            c.Ellipse(new Vector2(122f, 106f), 18f, 14f, Sack);
            c.Rect(96f, 96f, 140f, 122f, Wood);
            c.Thick(new Vector2(28f, 96f), new Vector2(28f, 176f), 4f, 3f, WoodDark);
            c.Thick(new Vector2(28f, 176f), new Vector2(40f, 176f), 3f, 3f, WoodDark);
            c.Thick(new Vector2(40f, 176f), new Vector2(40f, 164f), 2f, 2f, Iron);
            c.Ellipse(new Vector2(40f, 157f), 7f, 9f, Flame);
            c.Ellipse(new Vector2(40f, 156f), 3.5f, 5f, FlameCore);
            foreach (var wheel in new[] { 50f, 124f })
            {
                c.Ellipse(new Vector2(wheel, 34f), 32f, 32f, WoodDark);
                c.Ellipse(new Vector2(wheel, 34f), 25f, 25f, Wood);
                for (var i = 0; i < 4; i++)
                {
                    var a = i * Mathf.PI / 4f;
                    var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 24f;
                    c.Thick(new Vector2(wheel, 34f) - d, new Vector2(wheel, 34f) + d, 3f, 3f, WoodDark);
                }

                c.Ellipse(new Vector2(wheel, 34f), 5f, 5f, Iron);
            }

            // The shafts he pulls by.
            c.Thick(new Vector2(150f, 70f), new Vector2(200f, 82f), 5f, 4f, Wood);

            // Him: legs, a long coat bent forward under the pack, a wide-brimmed hat.
            const float x0 = 214f;
            c.Thick(new Vector2(x0 - 8f, 4f), new Vector2(x0 - 4f, 56f), 9f, 11f, CoatDark);
            c.Thick(new Vector2(x0 + 12f, 4f), new Vector2(x0 + 6f, 56f), 9f, 11f, CoatDark);
            c.Thick(new Vector2(x0 - 12f, 3f), new Vector2(x0 - 2f, 3f), 6f, 6f, WoodDark);
            c.Thick(new Vector2(x0 + 8f, 3f), new Vector2(x0 + 18f, 3f), 6f, 6f, WoodDark);
            c.Quad(new Vector2(x0 - 22f, 40f), new Vector2(x0 + 24f, 40f), new Vector2(x0 + 22f, 120f),
                new Vector2(x0 - 10f, 126f), Coat);
            c.Ellipse(new Vector2(x0 - 22f, 118f), 22f, 28f, Sack);
            c.Thick(new Vector2(x0 - 30f, 100f), new Vector2(x0 - 12f, 138f), 3f, 3f, WoodDark);
            c.Thick(new Vector2(x0 + 14f, 110f), new Vector2(x0 - 8f, 84f), 8f, 7f, CoatDark);
            c.Ellipse(new Vector2(x0 - 10f, 82f), 5f, 5f, Skin);
            c.Ellipse(new Vector2(x0 + 12f, 138f), 13f, 14f, Skin);
            c.Ellipse(new Vector2(x0 + 9f, 150f), 30f, 6f, WoodDark);
            c.Ellipse(new Vector2(x0 + 9f, 158f), 15f, 10f, CoatDark);
            c.Ellipse(new Vector2(x0 + 18f, 136f), 2f, 2f, WoodDark);
            c.Thick(new Vector2(x0 + 8f, 128f), new Vector2(x0 + 22f, 130f), 3f, 2f, CoatDark);

            c.Silhouette(Outline, Edge, 0.4f);
            return c;
        }

        /// <summary>A broad smith in a leather apron, hammer in hand, by an anvil; the forge's glow on him.</summary>
        private static Canvas Ostap()
        {
            var c = new Canvas(220, 210);
            // The anvil at his feet, on the right.
            c.Rect(140f, 4f, 196f, 24f, WoodDark);
            c.Quad(new Vector2(126f, 24f), new Vector2(206f, 24f), new Vector2(200f, 42f), new Vector2(132f, 42f), Iron);
            c.Triangle(new Vector2(200f, 42f), new Vector2(216f, 38f), new Vector2(200f, 34f), Iron);
            c.Ellipse(new Vector2(166f, 46f), 22f, 4f, Flame);
            // Him: legs apart, broad trunk, apron, bare forearms.
            const float x0 = 82f;
            c.Thick(new Vector2(x0 - 16f, 4f), new Vector2(x0 - 12f, 64f), 11f, 13f, CoatDark);
            c.Thick(new Vector2(x0 + 16f, 4f), new Vector2(x0 + 12f, 64f), 11f, 13f, CoatDark);
            c.Thick(new Vector2(x0 - 22f, 3f), new Vector2(x0 - 8f, 3f), 7f, 7f, WoodDark);
            c.Thick(new Vector2(x0 + 8f, 3f), new Vector2(x0 + 22f, 3f), 7f, 7f, WoodDark);
            c.Quad(new Vector2(x0 - 34f, 58f), new Vector2(x0 + 34f, 58f), new Vector2(x0 + 30f, 150f),
                new Vector2(x0 - 30f, 150f), Coat);
            c.Quad(new Vector2(x0 - 22f, 30f), new Vector2(x0 + 22f, 30f), new Vector2(x0 + 18f, 132f),
                new Vector2(x0 - 18f, 132f), Sack);
            // The near arm down with the hammer, the far one on his hip.
            c.Thick(new Vector2(x0 + 30f, 140f), new Vector2(x0 + 46f, 96f), 10f, 9f, Skin);
            c.Thick(new Vector2(x0 + 46f, 96f), new Vector2(x0 + 52f, 60f), 4f, 4f, Wood);
            c.Rect(x0 + 40f, 52f, x0 + 66f, 66f, Iron);
            c.Thick(new Vector2(x0 - 30f, 140f), new Vector2(x0 - 40f, 104f), 10f, 9f, Skin);
            // Head: a short beard, a soot-dark cap.
            c.Ellipse(new Vector2(x0 + 2f, 166f), 17f, 18f, Skin);
            c.Ellipse(new Vector2(x0 + 4f, 154f), 15f, 9f, CoatDark);
            c.Ellipse(new Vector2(x0, 180f), 17f, 8f, WoodDark);
            c.Ellipse(new Vector2(x0 + 10f, 168f), 2f, 2f, WoodDark);
            c.Silhouette(Outline, Edge, 0.4f);
            return c;
        }

        /// <summary>A tall hooded pilgrim; a staff with a small lantern that still has fire in it.</summary>
        private static Canvas Myroslava()
        {
            var c = new Canvas(140, 200);
            const float cx = 62f;
            c.Triangle(new Vector2(cx - 34f, 2f), new Vector2(cx + 30f, 2f), new Vector2(cx, 150f), Robe);
            c.Quad(new Vector2(cx - 18f, 60f), new Vector2(cx + 16f, 60f), new Vector2(cx + 14f, 150f),
                new Vector2(cx - 16f, 150f), Robe);
            c.Thick(new Vector2(cx - 14f, 4f), new Vector2(cx - 2f, 140f), 3f, 3f, RobeLight);
            c.Thick(new Vector2(cx - 18f, 96f), new Vector2(cx + 16f, 94f), 3f, 3f, Sack);
            c.Thick(new Vector2(cx + 10f, 94f), new Vector2(cx + 14f, 70f), 2f, 2f, Sack);
            // The hood, the face half in its shadow.
            c.Ellipse(new Vector2(cx, 160f), 17f, 20f, Robe);
            c.Triangle(new Vector2(cx - 16f, 162f), new Vector2(cx + 4f, 176f), new Vector2(cx - 10f, 186f), Robe);
            c.Ellipse(new Vector2(cx + 7f, 156f), 6f, 8f, Skin);
            c.Ellipse(new Vector2(cx + 5f, 161f), 7f, 4f, Robe);
            // The staff in her far hand, the lantern hung from its crook.
            c.Thick(new Vector2(cx + 34f, 2f), new Vector2(cx + 34f, 178f), 4f, 3f, Wood);
            c.Thick(new Vector2(cx + 34f, 178f), new Vector2(cx + 44f, 172f), 3f, 3f, Wood);
            c.Thick(new Vector2(cx + 16f, 110f), new Vector2(cx + 32f, 118f), 7f, 6f, Robe);
            c.Ellipse(new Vector2(cx + 33f, 118f), 4f, 4f, Skin);
            c.Thick(new Vector2(cx + 44f, 172f), new Vector2(cx + 44f, 162f), 1.5f, 1.5f, Iron);
            c.Rect(cx + 38f, 146f, cx + 50f, 162f, Iron);
            c.Ellipse(new Vector2(cx + 44f, 153f), 4f, 6f, Flame);
            c.Ellipse(new Vector2(cx + 44f, 152f), 2f, 3f, FlameCore);
            c.Silhouette(Outline, Edge, 0.4f);
            return c;
        }

        /// <summary>The ghost of a guard: helm and breastplate, a spear, all pale and see-through.</summary>
        private static Canvas Severyn()
        {
            var c = new Canvas(150, 214);
            const float cx = 66f;
            c.Thick(new Vector2(cx - 10f, 4f), new Vector2(cx - 8f, 70f), 10f, 12f, GhostDark);
            c.Thick(new Vector2(cx + 12f, 4f), new Vector2(cx + 8f, 70f), 10f, 12f, GhostDark);
            // A tattered cloak fading away at the hem.
            c.Quad(new Vector2(cx - 30f, 20f), new Vector2(cx + 10f, 30f), new Vector2(cx + 14f, 146f),
                new Vector2(cx - 22f, 144f), GhostDark);
            c.Quad(new Vector2(cx - 18f, 66f), new Vector2(cx + 20f, 66f), new Vector2(cx + 22f, 142f),
                new Vector2(cx - 20f, 142f), Ghost);
            c.Ellipse(new Vector2(cx + 2f, 116f), 18f, 20f, GhostLight);
            c.Thick(new Vector2(cx - 18f, 70f), new Vector2(cx + 20f, 70f), 6f, 6f, GhostDark);
            // The helm and its plume.
            c.Ellipse(new Vector2(cx + 2f, 160f), 15f, 17f, Ghost);
            c.Rect(cx - 2f, 150f, cx + 16f, 156f, GhostDark);
            c.Thick(new Vector2(cx, 174f), new Vector2(cx - 18f, 190f), 6f, 2f, GhostLight);
            // The spear, held upright at rest.
            c.Thick(new Vector2(cx + 34f, 2f), new Vector2(cx + 34f, 196f), 3.5f, 3f, GhostDark);
            c.Triangle(new Vector2(cx + 29f, 194f), new Vector2(cx + 39f, 194f), new Vector2(cx + 34f, 212f), GhostLight);
            c.Thick(new Vector2(cx + 16f, 128f), new Vector2(cx + 32f, 112f), 8f, 7f, Ghost);
            c.Silhouette(new Color32(20, 28, 36, 200), GhostLight, 0.6f);
            c.Fade(0.72f);
            return c;
        }

        /// <summary>An iron-bound chest; open, its lid stands back and gold shows inside.</summary>
        private static Canvas Chest(bool open)
        {
            var c = new Canvas(120, 96);
            c.Rect(12f, 2f, 108f, 48f, Wood);
            c.Rect(12f, 2f, 108f, 6f, WoodDark);
            foreach (var x in new[] { 26f, 94f })
            {
                c.Rect(x - 3f, 2f, x + 3f, 48f, Iron);
            }

            if (open)
            {
                c.Quad(new Vector2(14f, 48f), new Vector2(106f, 48f), new Vector2(110f, 86f),
                    new Vector2(18f, 86f), WoodDark);
                c.Rect(16f, 42f, 104f, 50f, CoatDark);
                c.Ellipse(new Vector2(46f, 48f), 14f, 5f, Gold);
                c.Ellipse(new Vector2(70f, 49f), 12f, 4f, Gold);
            }
            else
            {
                c.Quad(new Vector2(12f, 48f), new Vector2(108f, 48f), new Vector2(102f, 72f),
                    new Vector2(18f, 72f), Wood);
                foreach (var x in new[] { 26f, 94f })
                {
                    c.Rect(x - 3f, 48f, x + 3f, 71f, Iron);
                }

                c.Rect(52f, 34f, 68f, 54f, Gold);
                c.Rect(58f, 38f, 62f, 46f, WoodDark);
            }

            c.Silhouette(Outline, Edge, 0.35f);
            return c;
        }
    }
}
