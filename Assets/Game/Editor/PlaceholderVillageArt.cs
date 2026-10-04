using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// Placeholder environment art for the Fallen Village (item 5 of the 15-zone list, the user's map
    /// `reference/maps/03_fallen_village.webp`): sepia timber, wattle and thatch, and the embers of
    /// fires nobody tends any more.
    ///
    /// Same contract as the other zones' sets — not final art. The palette says where the player
    /// is: the Tomb is cold stone, the Forest wet teal, the Capital ash; the Village is brown wood and
    /// dried mud under a smoky dusk, with ember orange kept for what glows (windows, lanterns, the
    /// forge), never for the ground, so the gameplay plane stays the clearest thing.
    /// </summary>
    public static class PlaceholderVillageArt
    {
        public const string Root = "Assets/Game/Art/Environment/FallenVillage";
        private const int Unit = 100;

        private static readonly Color32 Clear = new Color32(0, 0, 0, 0);
        private static readonly Color32 Soot = new Color32(0x16, 0x11, 0x0E, 0xFF);
        private static readonly Color32 WoodDark = new Color32(0x2C, 0x1F, 0x16, 0xFF);
        private static readonly Color32 Wood = new Color32(0x4A, 0x34, 0x24, 0xFF);
        private static readonly Color32 WoodLit = new Color32(0x6E, 0x50, 0x36, 0xFF);
        private static readonly Color32 Daub = new Color32(0x5C, 0x4A, 0x38, 0xFF);
        private static readonly Color32 DaubDark = new Color32(0x3E, 0x31, 0x26, 0xFF);
        private static readonly Color32 Earth = new Color32(0x30, 0x25, 0x1C, 0xFF);
        private static readonly Color32 Stone = new Color32(0x4C, 0x44, 0x3C, 0xFF);
        private static readonly Color32 StoneLit = new Color32(0x6C, 0x62, 0x56, 0xFF);
        private static readonly Color32 Thatch = new Color32(0x6A, 0x56, 0x34, 0xFF);
        private static readonly Color32 ThatchDark = new Color32(0x45, 0x37, 0x22, 0xFF);
        private static readonly Color32 Ember = new Color32(0xD0, 0x6A, 0x26, 0xFF);
        private static readonly Color32 EmberPale = new Color32(0xF4, 0xA8, 0x52, 0xFF);
        private static readonly Color32 Iron = new Color32(0x33, 0x30, 0x2E, 0xFF);
        private static readonly Color32 IronLit = new Color32(0x5A, 0x55, 0x50, 0xFF);
        private static readonly Color32 Bronze = new Color32(0x6E, 0x52, 0x2C, 0xFF);
        private static readonly Color32 BronzeLit = new Color32(0x9C, 0x7A, 0x42, 0xFF);
        private static readonly Color32 SkyTop = new Color32(0x14, 0x10, 0x10, 0xFF);
        private static readonly Color32 SkyLow = new Color32(0x4A, 0x30, 0x22, 0xFF);
        private static readonly Color32 Cloth = new Color32(0x6A, 0x2A, 0x20, 0xFF);

        /// <summary>Every texture this generator writes, by logical name.</summary>
        public static Dictionary<string, Sprite> Generate()
        {
            PlaceholderImport.EnsureFolder($"{Root}/Tiles");
            PlaceholderImport.EnsureFolder($"{Root}/Backgrounds");
            PlaceholderImport.EnsureFolder($"{Root}/Props");

            var sprites = new Dictionary<string, Sprite>();
            Add(sprites, "Tiles", "env_village_planks_01", PlankFloor(), centred: true);
            Add(sprites, "Tiles", "env_village_cobble_01", Cobbles(), centred: true);
            Add(sprites, "Tiles", "env_village_timber_01", TimberWall(), centred: true);
            Add(sprites, "Tiles", "env_village_stone_01", StoneWall(), centred: true);
            Add(sprites, "Tiles", "env_village_ledge_01", PlankLedge(false), centred: true);
            Add(sprites, "Tiles", "env_village_ledge_broken_01", PlankLedge(true), centred: true);

            Add(sprites, "Backgrounds", "env_village_wall_01", InteriorWall(), centred: true);
            Add(sprites, "Backgrounds", "env_village_cellar_01", CellarWall(), centred: true);
            Add(sprites, "Backgrounds", "env_village_sky_01", DuskSky(), centred: true);

            Add(sprites, "Props", "env_village_house_01", House(5101, false));
            Add(sprites, "Props", "env_village_house_02", House(5102, true));
            Add(sprites, "Props", "env_village_well_01", Well());
            Add(sprites, "Props", "env_village_bell_01", Bell());
            Add(sprites, "Props", "env_village_church_01", Church());
            Add(sprites, "Props", "env_village_forge_01", Forge());
            Add(sprites, "Props", "env_village_hay_01", Hay());
            Add(sprites, "Props", "env_village_fence_01", Fence());
            Add(sprites, "Props", "env_village_lantern_01", Lantern());
            Add(sprites, "Props", "env_village_garland_01", Garland());
            Add(sprites, "Props", "env_village_barrel_01", Barrels());
            Add(sprites, "Props", "env_village_tree_01", DeadTree());
            Add(sprites, "Props", "env_village_cart_01", Cart());
            Add(sprites, "Props", "env_village_stall_01", Stall());
            Add(sprites, "Props", "env_village_cross_01", GraveCross());
            return sprites;
        }

        private static void Add(Dictionary<string, Sprite> into, string folder, string name, Canvas canvas,
            bool centred = false)
        {
            var path = $"{Root}/{folder}/{name}.png";
            PlaceholderImport.WriteIfChanged(path, canvas.EncodePng());
            PlaceholderImport.ConfigureImporter(path, centred ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 0f),
                repeat: folder == "Tiles" || folder == "Backgrounds");
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogError($"Village texture failed to import: {path}");
            }

            into[name] = sprite;
        }

        // ---------- tiles ----------

        /// <summary>Boards laid over packed earth, a pale lit edge on top as on every floor of the game.</summary>
        private static Canvas PlankFloor()
        {
            var c = new Canvas(Unit * 4, Unit);
            var rng = new System.Random(5001);
            c.Rect(0, 0, c.Width, c.Height, Earth);
            Speckle(c, rng, 260, DaubDark, 0, 70);
            for (var x = 0; x < c.Width; x += 50)
            {
                var tone = rng.Next(0, 3) == 0 ? WoodDark : Wood;
                c.Rect(x + 1, 74, x + 49, c.Height - 4, tone);
                c.Rect(x + 1, 74, x + 3, c.Height - 4, WoodDark);
            }

            c.Rect(0, c.Height - 4, c.Width, c.Height, WoodLit);
            return c;
        }

        /// <summary>The square's cobbles: round stones in mud.</summary>
        private static Canvas Cobbles()
        {
            var c = new Canvas(Unit * 4, Unit);
            var rng = new System.Random(5002);
            c.Rect(0, 0, c.Width, c.Height, Earth);
            for (var i = 0; i < 70; i++)
            {
                var x = rng.Next(0, c.Width);
                var y = rng.Next(8, c.Height - 10);
                var r = rng.Next(7, 13);
                var tone = rng.Next(0, 3) == 0 ? DaubDark : Stone;
                c.Ellipse(new Vector2(x, y), r, r * 0.75f, tone);
                c.Ellipse(new Vector2(x - 2, y + 2), r * 0.6f, r * 0.4f, Blend(tone, StoneLit, 0.3f));
            }

            c.Rect(0, c.Height - 4, c.Width, c.Height, StoneLit);
            return c;
        }

        /// <summary>Walls and the faces of raised ground: wattle and daub between dark beams.</summary>
        private static Canvas TimberWall()
        {
            var c = new Canvas(Unit * 2, Unit * 2);
            var rng = new System.Random(5003);
            c.Rect(0, 0, c.Width, c.Height, Daub);
            Speckle(c, rng, 500, DaubDark, 0, c.Height);
            for (var x = 0; x < c.Width; x += 100)
            {
                c.Rect(x, 0, x + 14, c.Height, WoodDark);
            }

            c.Rect(0, 92, c.Width, 104, WoodDark);
            c.Thick(new Vector2(14, 104), new Vector2(100, 200), 7f, 7f, WoodDark);
            c.Thick(new Vector2(114, 0), new Vector2(200, 92), 7f, 7f, WoodDark);
            return c;
        }

        /// <summary>The church's and the crypt's stone: rough coursed rubble.</summary>
        private static Canvas StoneWall()
        {
            var c = new Canvas(Unit * 2, Unit * 2);
            var rng = new System.Random(5004);
            c.Rect(0, 0, c.Width, c.Height, Soot);
            for (var y = 0; y < c.Height; y += 28)
            {
                var offset = (y / 28) % 2 == 0 ? 0 : 30;
                for (var x = -offset; x < c.Width; x += 60)
                {
                    var tone = Blend(Stone, Earth, rng.Next(0, 40) / 100f);
                    c.Ellipse(new Vector2(x + 30, y + 14), 28, 12, tone);
                    c.Ellipse(new Vector2(x + 26, y + 18), 18, 5, Blend(tone, StoneLit, 0.3f));
                }
            }

            return c;
        }

        /// <summary>A plank shelf on two brackets; the rotten one sags and is missing a board.</summary>
        private static Canvas PlankLedge(bool broken)
        {
            var c = new Canvas(Unit * 2, Unit / 2);
            var rng = new System.Random(broken ? 5006 : 5005);
            c.Rect(0, 0, c.Width, c.Height, Clear);
            c.Rect(0, 16, c.Width, c.Height - 4, Wood);
            c.Rect(0, c.Height - 4, c.Width, c.Height, WoodLit);
            for (var x = 30; x < c.Width; x += 70)
            {
                c.Thick(new Vector2(x, 16), new Vector2(x - 18, 0), 5f, 3f, WoodDark);
            }

            if (broken)
            {
                c.Rect(rng.Next(40, 120), 16, rng.Next(130, 160), 30, Clear);
            }

            Speckle(c, rng, 60, WoodDark, 16, c.Height - 4);
            return c;
        }

        // ---------- backgrounds ----------

        /// <summary>The inside of a house or a barn: plaster between posts, a shelf, soot over the hearth.</summary>
        private static Canvas InteriorWall()
        {
            var c = new Canvas(Unit * 4, Unit * 4);
            var rng = new System.Random(5101);
            c.Rect(0, 0, c.Width, c.Height, DaubDark);
            Speckle(c, rng, 900, Earth, 0, c.Height);
            for (var x = 0; x < c.Width; x += 130)
            {
                c.Rect(x, 0, x + 18, c.Height, WoodDark);
            }

            c.Rect(0, 250, c.Width, 264, WoodDark);
            c.TintOver(160, 0, 260, c.Height, Soot, 0.5f);
            return c;
        }

        /// <summary>Under the village: stone, barrels and the dark between them.</summary>
        private static Canvas CellarWall()
        {
            var big = new Canvas(Unit * 4, Unit * 4);
            for (var y = 0; y < big.Height; y++)
            {
                for (var x = 0; x < big.Width; x++)
                {
                    big.Set(x, y, Blend(Soot, Stone, ((x * 7 + y * 13) % 17) / 60f));
                }
            }

            var rng = new System.Random(5102);
            for (var i = 0; i < 9; i++)
            {
                var x = rng.Next(30, big.Width - 30);
                var y = rng.Next(30, big.Height - 30);
                big.Ellipse(new Vector2(x, y), 34, 22, Blend(Stone, Earth, 0.5f));
                big.Ellipse(new Vector2(x - 4, y + 6), 20, 8, Blend(Stone, StoneLit, 0.2f));
            }

            return big;
        }

        /// <summary>
        /// Dusk over the roofs: a smoky brown sky, a row of far roofs and chimneys, and the glow of
        /// fires on the low cloud. Tiles sideways.
        /// </summary>
        private static Canvas DuskSky()
        {
            var c = new Canvas(Unit * 8, Unit * 6);
            for (var y = 0; y < c.Height; y++)
            {
                var t = y / (float)c.Height;
                var row = Blend(SkyLow, SkyTop, Mathf.SmoothStep(0f, 1f, t));
                for (var x = 0; x < c.Width; x++)
                {
                    c.Set(x, y, row);
                }
            }

            var rng = new System.Random(5103);
            for (var i = 0; i < 6; i++)
            {
                // Smoke drifting over the village, soft and wide.
                var x = rng.Next(0, c.Width);
                c.TintOver(x - 160, 260 + i * 30, x + 160, 300 + i * 30, Earth, 0.25f);
            }

            var roof = Blend(SkyTop, Soot, 0.5f);
            for (var x = -40; x < c.Width + 40; x += rng.Next(80, 140))
            {
                var w = rng.Next(60, 110);
                var h = rng.Next(60, 120);
                c.Rect(x, 0, x + w, h, roof);
                c.Triangle(new Vector2(x - 10, h), new Vector2(x + w + 10, h), new Vector2(x + w * 0.5f, h + rng.Next(40, 70)), roof);
                if (rng.Next(0, 3) == 0)
                {
                    c.Rect(x + w * 0.7f, h, x + w * 0.7f + 10, h + 50, roof);
                }

                if (rng.Next(0, 2) == 0)
                {
                    c.Rect(x + w * 0.3f, h * 0.4f, x + w * 0.3f + 10, h * 0.4f + 12, Blend(Ember, roof, 0.4f));
                }
            }

            return c;
        }

        // ---------- props ----------

        /// <summary>A timber house under a steep thatch, one window still lit. The second stands on stilts.</summary>
        private static Canvas House(int seed, bool stilts)
        {
            var c = new Canvas(Unit * 4, Unit * 5);
            var rng = new System.Random(seed);
            var floor = stilts ? 90f : 0f;
            var eaves = floor + rng.Next(200, 250);
            if (stilts)
            {
                for (var x = 70; x < c.Width - 40; x += 90)
                {
                    c.Rect(x, 0, x + 12, floor, WoodDark);
                }

                c.Rect(40, floor - 10, c.Width - 40, floor, Wood);
            }

            c.Rect(60, floor, c.Width - 60, eaves, Daub);
            Speckle(c, rng, 400, DaubDark, (int)floor, (int)eaves);
            c.Rect(60, floor, 72, eaves, WoodDark);
            c.Rect(c.Width - 72, floor, c.Width - 60, eaves, WoodDark);
            c.Rect(60, floor + (eaves - floor) * 0.5f, c.Width - 60, floor + (eaves - floor) * 0.5f + 10, WoodDark);
            // The roof: a soft-shouldered thatch, overhanging.
            c.Quad(new Vector2(30, eaves), new Vector2(c.Width - 30, eaves), new Vector2(c.Width * 0.62f, eaves + 170),
                new Vector2(c.Width * 0.38f, eaves + 170), Thatch);
            c.Ellipse(new Vector2(c.Width * 0.5f, eaves + 165), c.Width * 0.14f, 22, Thatch);
            for (var i = 0; i < 18; i++)
            {
                var x = rng.Next(40, c.Width - 40);
                c.Thick(new Vector2(x, eaves + 4), new Vector2(x + rng.Next(-8, 8), eaves + rng.Next(40, 140)), 2f, 1f, ThatchDark);
            }

            // Door and a window, one lit.
            c.Rect(c.Width * 0.3f, floor, c.Width * 0.3f + 50, floor + 110, WoodDark);
            var lit = rng.Next(0, 2) == 0;
            c.Rect(c.Width * 0.6f, floor + 80, c.Width * 0.6f + 54, floor + 136, lit ? Ember : Soot);
            if (lit)
            {
                c.Rect(c.Width * 0.6f + 8, floor + 88, c.Width * 0.6f + 46, floor + 128, EmberPale);
            }

            c.Rect(c.Width * 0.6f + 24, floor + 80, c.Width * 0.6f + 30, floor + 136, WoodDark);
            return c;
        }

        /// <summary>The well on the square: a stone ring, a little roof on posts, the bucket on its rope.</summary>
        private static Canvas Well()
        {
            var c = new Canvas(Unit * 3, Unit * 3);
            c.Rect(50, 0, 250, 80, Stone);
            c.Ellipse(new Vector2(150, 80), 104, 18, StoneLit);
            c.Ellipse(new Vector2(150, 80), 84, 11, Soot);
            c.Rect(66, 80, 78, 210, WoodDark);
            c.Rect(222, 80, 234, 210, WoodDark);
            c.Quad(new Vector2(40, 200), new Vector2(260, 200), new Vector2(190, 270), new Vector2(110, 270), Thatch);
            c.Rect(78, 176, 222, 184, Wood);
            c.Thick(new Vector2(150, 176), new Vector2(150, 120), 2f, 2f, WoodLit);
            c.Ellipse(new Vector2(150, 112), 14, 12, Wood);
            return c;
        }

        /// <summary>The great bell, cracked down one side, hanging from its beam.</summary>
        private static Canvas Bell()
        {
            var c = new Canvas(Unit * 4, Unit * 4);
            c.Rect(20, 360, 380, 390, WoodDark);
            c.Rect(190, 300, 210, 360, Iron);
            // A bell's profile: shoulder, waist, flaring lip — smooth curves, not a trapezoid.
            for (var y = 40; y <= 300; y++)
            {
                var t = (y - 40) / 260f;
                var half = Mathf.Lerp(150f, 70f, Mathf.Pow(t, 0.55f)) - (t > 0.85f ? (t - 0.85f) * 200f : 0f);
                var tone = Blend(Bronze, BronzeLit, Mathf.Clamp01(1f - Mathf.Abs(t - 0.4f) * 2f) * 0.6f);
                c.Rect(200 - half, y, 200 + half, y + 1, tone);
            }

            c.Ellipse(new Vector2(200, 44), 150, 14, Bronze);
            c.Rect(60, 70, 340, 80, Blend(Bronze, Soot, 0.4f));
            c.Thick(new Vector2(250, 60), new Vector2(232, 200), 3f, 1f, Soot);
            c.Ellipse(new Vector2(200, 30), 16, 16, Iron);
            return c;
        }

        /// <summary>The church front: a pointed door, a round window glowing, a little belfry.</summary>
        private static Canvas Church()
        {
            var c = new Canvas(Unit * 6, Unit * 8);
            c.Rect(60, 0, 540, 480, Stone);
            c.Quad(new Vector2(40, 480), new Vector2(560, 480), new Vector2(330, 680), new Vector2(270, 680), Stone);
            c.Rect(270, 640, 330, 780, Stone);
            c.Quad(new Vector2(256, 780), new Vector2(344, 780), new Vector2(300, 800), new Vector2(300, 800), Soot);
            c.Rect(240, 0, 360, 200, Soot);
            c.Ellipse(new Vector2(300, 200), 60, 60, Soot);
            c.Ellipse(new Vector2(300, 380), 70, 70, Ember);
            c.Ellipse(new Vector2(300, 380), 52, 52, EmberPale);
            for (var a = 0; a < 6; a++)
            {
                var d = Canvas.Rotate(new Vector2(0, 66), a * 60f);
                c.Thick(new Vector2(300, 380), new Vector2(300, 380) + d, 4f, 4f, Soot);
            }

            c.RectOver(60, 0, 66, 480, StoneLit);
            return c;
        }

        /// <summary>The forge: a hearth still glowing, the anvil, tongs on the wall.</summary>
        private static Canvas Forge()
        {
            var c = new Canvas(Unit * 4, Unit * 3);
            c.Rect(20, 0, 200, 130, Stone);
            c.Ellipse(new Vector2(110, 130), 80, 40, Soot);
            c.Ellipse(new Vector2(110, 110), 60, 22, Ember);
            c.Ellipse(new Vector2(110, 116), 36, 10, EmberPale);
            c.Rect(80, 130, 140, 290, Stone);
            c.Rect(250, 0, 290, 70, Iron);
            c.Rect(220, 70, 340, 100, Iron);
            c.Ellipse(new Vector2(345, 86), 18, 10, Iron);
            c.RectOver(220, 96, 340, 100, IronLit);
            c.Thick(new Vector2(360, 260), new Vector2(380, 160), 3f, 3f, Iron);
            return c;
        }

        /// <summary>A haystack: soft, round, a few loose stalks.</summary>
        private static Canvas Hay()
        {
            var c = new Canvas(Unit * 3, Unit * 2);
            var rng = new System.Random(5201);
            c.Ellipse(new Vector2(150, 40), 140, 90, Thatch);
            c.Ellipse(new Vector2(150, 120), 90, 60, Thatch);
            c.Rect(0, 0, c.Width, 6, Clear);
            for (var i = 0; i < 40; i++)
            {
                var x = rng.Next(30, 270);
                var y = rng.Next(10, 160);
                if (c.IsOpaque(x, y))
                {
                    c.Thick(new Vector2(x, y), new Vector2(x + rng.Next(-14, 14), y + rng.Next(6, 20)), 1.5f, 1f, ThatchDark);
                }
            }

            return c;
        }

        /// <summary>A wattle fence, posts and woven withies, one gap.</summary>
        private static Canvas Fence()
        {
            var c = new Canvas(Unit * 4, Unit);
            for (var x = 10; x < c.Width; x += 60)
            {
                c.Rect(x, 0, x + 8, 92, WoodDark);
            }

            for (var row = 0; row < 3; row++)
            {
                var y = 24 + row * 22;
                for (var x = 0; x < c.Width; x += 2)
                {
                    if (x > 230 && x < 290) { continue; }
                    c.Set(x, y + (int)(Mathf.Sin(x * 0.1f + row) * 3f), Wood);
                    c.Set(x, y + 1 + (int)(Mathf.Sin(x * 0.1f + row) * 3f), Wood);
                }
            }

            return c;
        }

        /// <summary>A lantern on a crooked post: the warm light of the streets.</summary>
        private static Canvas Lantern()
        {
            var c = new Canvas(Unit, Unit * 3);
            c.Thick(new Vector2(50, 0), new Vector2(46, 250), 5f, 4f, WoodDark);
            c.Thick(new Vector2(46, 250), new Vector2(76, 262), 3f, 3f, WoodDark);
            c.Thick(new Vector2(76, 262), new Vector2(76, 236), 1f, 1f, Iron);
            c.Ellipse(new Vector2(76, 222), 13, 17, Ember);
            c.Ellipse(new Vector2(76, 222), 7, 10, EmberPale);
            return c;
        }

        /// <summary>A line of rag pennants strung across the street, sagging.</summary>
        private static Canvas Garland()
        {
            var c = new Canvas(Unit * 6, Unit);
            for (var x = 0; x < c.Width; x++)
            {
                var y = 90 - Mathf.Sin(x / (float)c.Width * Mathf.PI) * 40f;
                c.Set(x, (int)y, WoodDark);
                if (x % 40 == 10)
                {
                    c.Triangle(new Vector2(x - 12, y), new Vector2(x + 12, y), new Vector2(x, y - 30), x % 80 == 10 ? Cloth : Daub);
                }
            }

            return c;
        }

        private static Canvas Barrels()
        {
            var c = new Canvas(Unit * 2, Unit * 2);
            foreach (var (x, y) in new[] { (55f, 0f), (145f, 0f), (100f, 95f) })
            {
                c.Ellipse(new Vector2(x, y + 45), 42, 48, Wood);
                c.Rect(x - 44, y + 20, x + 44, y + 26, Iron);
                c.Rect(x - 44, y + 64, x + 44, y + 70, Iron);
                c.Ellipse(new Vector2(x - 14, y + 50), 8, 30, WoodLit);
            }

            c.Rect(0, 0, c.Width, 2, Clear);
            return c;
        }

        /// <summary>A dead tree on the square, its branches like hands.</summary>
        private static Canvas DeadTree()
        {
            var c = new Canvas(Unit * 3, Unit * 4);
            c.Thick(new Vector2(150, 0), new Vector2(140, 220), 22f, 12f, WoodDark);
            c.Thick(new Vector2(140, 220), new Vector2(80, 330), 10f, 3f, WoodDark);
            c.Thick(new Vector2(140, 220), new Vector2(210, 340), 9f, 3f, WoodDark);
            c.Thick(new Vector2(146, 160), new Vector2(240, 230), 7f, 2f, WoodDark);
            c.Thick(new Vector2(110, 290), new Vector2(60, 300), 4f, 1f, WoodDark);
            c.Thick(new Vector2(180, 300), new Vector2(230, 380), 4f, 1f, WoodDark);
            return c;
        }

        private static Canvas Cart()
        {
            var c = new Canvas(Unit * 3, Unit * 2);
            c.Rect(30, 60, 250, 120, Wood);
            c.Rect(30, 116, 250, 122, WoodLit);
            c.Thick(new Vector2(250, 80), new Vector2(298, 50), 4f, 3f, WoodDark);
            foreach (var x in new[] { 80f, 200f })
            {
                c.Ellipse(new Vector2(x, 50), 48, 48, WoodDark);
                c.Ellipse(new Vector2(x, 50), 38, 38, Clear);
                c.Thick(new Vector2(x - 38, 50), new Vector2(x + 38, 50), 3f, 3f, WoodDark);
                c.Thick(new Vector2(x, 12), new Vector2(x, 88), 3f, 3f, WoodDark);
            }

            return c;
        }

        /// <summary>A market stall: a cloth awning on poles over a plank counter.</summary>
        private static Canvas Stall()
        {
            var c = new Canvas(Unit * 3, Unit * 3);
            c.Rect(30, 0, 40, 220, WoodDark);
            c.Rect(260, 0, 270, 220, WoodDark);
            c.Rect(20, 80, 280, 96, Wood);
            for (var i = 0; i < 6; i++)
            {
                var x = 10 + i * 48;
                c.Quad(new Vector2(x, 210), new Vector2(x + 48, 210), new Vector2(x + 48, 250), new Vector2(x, 250),
                    i % 2 == 0 ? Cloth : Daub);
                c.Ellipse(new Vector2(x + 24, 210), 24, 10, i % 2 == 0 ? Cloth : Daub);
            }

            c.Ellipse(new Vector2(90, 110), 20, 12, Thatch);
            c.Ellipse(new Vector2(200, 106), 26, 10, Bronze);
            return c;
        }

        private static Canvas GraveCross()
        {
            var c = new Canvas(Unit, Unit * 2);
            c.Thick(new Vector2(50, 0), new Vector2(53, 150), 7f, 6f, WoodDark);
            c.Thick(new Vector2(22, 112), new Vector2(82, 118), 5f, 5f, WoodDark);
            c.Ellipse(new Vector2(50, 0), 36, 10, Earth);
            return c;
        }

        // ---------- helpers ----------

        private static void Speckle(Canvas c, System.Random rng, int count, Color32 tone, int fromY, int toY)
        {
            for (var i = 0; i < count; i++)
            {
                var x = rng.Next(0, c.Width);
                var y = rng.Next(fromY, Mathf.Max(fromY + 1, toY));
                if (c.IsOpaque(x, y))
                {
                    c.Set(x, y, tone);
                }
            }
        }

        private static Color32 Blend(Color32 a, Color32 b, float t) => Color32.Lerp(a, b, Mathf.Clamp01(t));
    }
}
