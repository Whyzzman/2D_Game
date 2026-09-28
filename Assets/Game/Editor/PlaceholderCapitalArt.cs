using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// Placeholder environment art for the Ashen Capital (item 2 of the big list, 2026-09-28):
    /// the kingdom's burnt seat, reached by the royal road that runs through the forest.
    ///
    /// Same contract and same honesty as the Tomb's and the Forest's sets — not final art. The
    /// palette says where the player is: the Tomb is cold stone, the Forest wet brown and teal;
    /// the Capital is ash grey and charcoal under a bruised red sky, with the same faded royal
    /// gold in its ruins so it is visibly the same kingdom. Ember orange is kept for what glows
    /// and what burns, never for the ground, so the gameplay plane stays the clearest thing.
    /// </summary>
    public static class PlaceholderCapitalArt
    {
        public const string Root = "Assets/Game/Art/Environment/AshenCapital";
        private const int Unit = 100;

        private static readonly Color32 Clear = new Color32(0, 0, 0, 0);
        private static readonly Color32 Soot = new Color32(0x14, 0x12, 0x13, 0xFF);
        private static readonly Color32 Charcoal = new Color32(0x22, 0x1F, 0x20, 0xFF);
        private static readonly Color32 Ash = new Color32(0x3A, 0x36, 0x35, 0xFF);
        private static readonly Color32 AshLit = new Color32(0x58, 0x52, 0x4E, 0xFF);
        private static readonly Color32 AshPale = new Color32(0x7A, 0x72, 0x6B, 0xFF);
        private static readonly Color32 Stone = new Color32(0x44, 0x3E, 0x3B, 0xFF);
        private static readonly Color32 StoneLit = new Color32(0x62, 0x5A, 0x53, 0xFF);
        private static readonly Color32 Mortar = new Color32(0x26, 0x22, 0x22, 0xFF);
        private static readonly Color32 Scorch = new Color32(0x1B, 0x16, 0x15, 0xFF);
        private static readonly Color32 Ember = new Color32(0xC8, 0x5A, 0x22, 0xFF);
        private static readonly Color32 EmberPale = new Color32(0xF0, 0x9A, 0x48, 0xFF);
        private static readonly Color32 FadedGold = new Color32(0x8A, 0x74, 0x34, 0xFF);
        private static readonly Color32 Banner = new Color32(0x5A, 0x1E, 0x22, 0xFF);
        private static readonly Color32 BannerDark = new Color32(0x38, 0x14, 0x17, 0xFF);

        /// <summary>Every texture this generator writes, by logical name.</summary>
        public static Dictionary<string, Sprite> Generate()
        {
            PlaceholderImport.EnsureFolder($"{Root}/Tiles");
            PlaceholderImport.EnsureFolder($"{Root}/Backgrounds");
            PlaceholderImport.EnsureFolder($"{Root}/Props");

            var sprites = new Dictionary<string, Sprite>();
            Add(sprites, "Tiles", "env_capital_flagstone_01", FlagstoneTile(), centred: true);
            Add(sprites, "Tiles", "env_capital_masonry_01", MasonryTile(), centred: true);
            Add(sprites, "Tiles", "env_capital_ledge_01", LedgeTile(), centred: true);
            Add(sprites, "Tiles", "env_capital_rubble_01", RubbleTile(), centred: true);

            Add(sprites, "Backgrounds", "env_capital_bg_far_01", CapitalBackdrop.Layer(0));
            Add(sprites, "Backgrounds", "env_capital_bg_mid_01", CapitalBackdrop.Layer(1));
            Add(sprites, "Backgrounds", "env_capital_bg_near_01", CapitalBackdrop.Layer(2));

            Add(sprites, "Props", "env_capital_gate_01", CityGate());
            Add(sprites, "Props", "env_capital_house_01", HouseShell(3501));
            Add(sprites, "Props", "env_capital_house_02", HouseShell(3502));
            Add(sprites, "Props", "env_capital_column_01", BrokenColumn());
            Add(sprites, "Props", "env_capital_statue_01", HeadlessKing());
            Add(sprites, "Props", "env_capital_banner_01", TornBanner());
            Add(sprites, "Props", "env_capital_tree_burnt_01", BurntTree());
            Add(sprites, "Props", "env_capital_cart_01", BurntCart());
            Add(sprites, "Props", "env_capital_bell_01", FallenBell());
            Add(sprites, "Props", "env_capital_brazier_01", Brazier());
            Add(sprites, "Props", "env_capital_temple_01", TempleFront());
            Add(sprites, "Props", "env_capital_throne_01", Throne());
            Add(sprites, "Props", "env_capital_ash_bank_01", AshBank(), centred: true);
            return sprites;
        }

        private static void Add(Dictionary<string, Sprite> into, string folder, string name,
            Canvas canvas, bool centred = false)
        {
            var path = $"{Root}/{folder}/{name}.png";
            PlaceholderImport.WriteIfChanged(path, canvas.EncodePng());
            PlaceholderImport.ConfigureImporter(path,
                centred ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 0f),
                repeat: folder == "Tiles" || folder == "Backgrounds");
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogError($"Capital texture failed to import: {path}");
            }

            into[name] = sprite;
        }

        // ---------- tiles ----------

        /// <summary>
        /// The capital's streets: big cut flags, cracked and scorched, with ash in every joint.
        /// A pale lit lip on top, as on every floor of the game, is how the edge reads.
        /// </summary>
        private static Canvas FlagstoneTile()
        {
            var c = new Canvas(Unit * 4, Unit);
            var rng = new System.Random(3101);
            c.Rect(0, 0, c.Width, c.Height, Mortar);
            for (var row = 0; row < 3; row++)
            {
                var y0 = row * 32 + 2;
                var offset = row % 2 == 0 ? 0 : 40;
                for (var x = -offset; x < c.Width; x += 80)
                {
                    var tone = rng.Next(0, 4) == 0 ? Scorch : Stone;
                    c.Rect(x + 2, y0, x + 78, y0 + 29, tone);
                    c.Rect(x + 2, y0 + 26, x + 78, y0 + 29, Blend(tone, StoneLit, 0.4f));
                    if (rng.Next(0, 3) == 0)
                    {
                        // A crack running across the flag.
                        Vein(c, rng, x + rng.Next(10, 60), y0 + rng.Next(4, 24), rng.Next(16, 40), Mortar, true);
                    }
                }
            }

            Speckle(c, rng, 260, Ash, 0, c.Height - 6);
            c.Rect(0, c.Height - 6, c.Width, c.Height - 2, Ash);
            c.Rect(0, c.Height - 2, c.Width, c.Height, AshPale);
            return c;
        }

        /// <summary>Walls and the faces of raised ground: coursed ashlar, soot-blackened upward.</summary>
        private static Canvas MasonryTile()
        {
            var c = new Canvas(Unit * 2, Unit * 2);
            var rng = new System.Random(3102);
            c.Rect(0, 0, c.Width, c.Height, Mortar);
            for (var y = 0; y < c.Height; y += 25)
            {
                var offset = (y / 25) % 2 == 0 ? 0 : 25;
                for (var x = -offset; x < c.Width; x += 50)
                {
                    var soot = Mathf.Clamp01(y / (float)c.Height) * 0.6f;
                    var tone = Blend(Stone, Scorch, soot + rng.Next(0, 20) / 100f);
                    c.Rect(x + 1, y + 1, x + 49, y + 24, tone);
                    c.Rect(x + 1, y + 21, x + 49, y + 24, Blend(tone, StoneLit, 0.25f));
                }
            }

            Speckle(c, rng, 220, Charcoal, 0, c.Height);
            return c;
        }

        /// <summary>A stone balcony slab on a corbel, what the capital's upper walks are made of.</summary>
        private static Canvas LedgeTile()
        {
            var c = new Canvas(Unit * 2, Unit / 2);
            var rng = new System.Random(3103);
            c.Rect(0, 0, c.Width, c.Height, Clear);
            c.Rect(0, 14, c.Width, c.Height - 3, Stone);
            c.Rect(0, c.Height - 3, c.Width, c.Height, AshPale);
            c.Rect(0, 14, c.Width, 18, Charcoal);
            for (var x = 20; x < c.Width; x += 60)
            {
                // Corbels under the slab.
                c.Triangle(new Vector2(x - 12, 14), new Vector2(x + 12, 14), new Vector2(x, 0), Charcoal);
            }

            Speckle(c, rng, 60, Ash, 16, c.Height - 3);
            return c;
        }

        /// <summary>A wall of fallen masonry and charred beams: what blocks a street.</summary>
        private static Canvas RubbleTile()
        {
            var c = new Canvas(Unit * 2, Unit * 2);
            var rng = new System.Random(3104);
            c.Rect(0, 0, c.Width, c.Height, Charcoal);
            for (var i = 0; i < 40; i++)
            {
                var x = rng.Next(-20, c.Width);
                var y = rng.Next(-10, c.Height);
                var w = rng.Next(20, 50);
                var h = rng.Next(14, 30);
                var tone = rng.Next(0, 3) == 0 ? Ash : Stone;
                c.Rect(x, y, x + w, y + h, tone);
                c.Rect(x, y + h - 3, x + w, y + h, Blend(tone, AshPale, 0.3f));
            }

            for (var i = 0; i < 5; i++)
            {
                // Charred beams across the heap.
                var y = rng.Next(20, c.Height - 20);
                c.Rect(0, y, c.Width, y + 8, Scorch);
            }

            Speckle(c, rng, 90, Ember, 0, 30);
            return c;
        }

        // ---------- props ----------

        /// <summary>The city gate: two towers and a round arch, its doors long gone.</summary>
        private static Canvas CityGate()
        {
            var c = new Canvas(Unit * 8, Unit * 8);
            var rng = new System.Random(3201);
            const float towerW = 170f;
            c.Rect(0, 0, towerW, 640, Stone);
            c.Rect(c.Width - towerW, 0, c.Width, 560, Stone);
            // Battlements, broken on the right.
            for (var x = 0f; x < towerW; x += 40f)
            {
                c.Rect(x, 640, x + 24, 672, Stone);
            }

            c.Triangle(new Vector2(c.Width - towerW, 560), new Vector2(c.Width, 560),
                new Vector2(c.Width - towerW, 610), Stone);
            // The wall between them, pierced by the arch.
            c.Rect(towerW, 300, c.Width - towerW, 520, Stone);
            var centre = new Vector2(c.Width * 0.5f, 300f);
            var r = (c.Width - towerW * 2f) * 0.5f;
            for (var y = 300; y < 300 + r; y++)
            {
                for (var x = (int)towerW; x < c.Width - towerW; x++)
                {
                    if (Vector2.Distance(new Vector2(x, y), centre) < r - 10f)
                    {
                        c.Set(x, y, Clear);
                    }
                }
            }

            c.Rect(towerW, 0, c.Width - towerW, 300, Clear);
            for (var y = 30; y < 640; y += 40)
            {
                c.Rect(0, y, towerW, y + 3, Mortar);
                if (y < 560) { c.Rect(c.Width - towerW, y, c.Width, y + 3, Mortar); }
            }

            // The royal crest over the arch, in the kingdom's gold.
            c.Ellipse(new Vector2(c.Width * 0.5f, 470f), 30f, 36f, FadedGold);
            c.Ellipse(new Vector2(c.Width * 0.5f, 470f), 16f, 20f, Stone);
            Speckle(c, rng, 700, Scorch, 200, 640);
            c.RectOver(0, 0, 6, 640, StoneLit);
            return c;
        }

        /// <summary>A burnt-out house: the front wall standing, windows empty, the roof beams bare.</summary>
        private static Canvas HouseShell(int seed)
        {
            var c = new Canvas(Unit * 4, Unit * 6);
            var rng = new System.Random(seed);
            var top = rng.Next(380, 520);
            c.Rect(30, 0, c.Width - 30, top, Stone);
            // Gable, half gone.
            c.Triangle(new Vector2(30, top), new Vector2(c.Width * 0.5f, top + 70), new Vector2(c.Width * 0.5f, top), Stone);
            for (var i = 0; i < 4; i++)
            {
                var x = 60 + i * 80;
                c.Rect(x, top + 10, x + 8, top + 60 - i * 12, Scorch);
            }

            for (var row = 0; row < 2; row++)
            {
                for (var col = 0; col < 3; col++)
                {
                    var x = 70 + col * 100;
                    var y = 140 + row * 170;
                    if (y + 90 > top) { continue; }
                    c.Rect(x, y, x + 50, y + 90, Soot);
                    // Soot licks up from each window where the fire came out.
                    c.TintOver(x - 10, y + 90, x + 60, y + 150, Scorch, 0.6f);
                }
            }

            c.Rect(c.Width * 0.5f - 30, 0, c.Width * 0.5f + 30, 110, Soot);
            Speckle(c, rng, 300, Charcoal, 0, top);
            c.RectOver(30, 0, 34, top, StoneLit);
            return c;
        }

        private static Canvas BrokenColumn()
        {
            var c = new Canvas(Unit, Unit * 4);
            var rng = new System.Random(3301);
            c.Rect(30, 0, 70, 300, Stone);
            c.Triangle(new Vector2(30, 300), new Vector2(70, 300), new Vector2(40, 340), Stone);
            c.Rect(20, 0, 80, 20, Charcoal);
            for (var x = 36; x < 66; x += 8)
            {
                c.Rect(x, 20, x + 2, 300, Mortar);
            }

            c.RectOver(30, 0, 33, 300, StoneLit);
            Speckle(c, rng, 60, Scorch, 0, 300);
            return c;
        }

        /// <summary>The old king on his plinth, his head struck off. The quiet story of the zone.</summary>
        private static Canvas HeadlessKing()
        {
            var c = new Canvas(Unit * 2, Unit * 4);
            var rng = new System.Random(3302);
            c.Rect(20, 0, 180, 70, Stone);
            c.Rect(20, 66, 180, 70, StoneLit);
            // Robed body.
            c.Triangle(new Vector2(40, 70), new Vector2(160, 70), new Vector2(100, 300), Ash);
            c.Rect(70, 200, 130, 300, Ash);
            // Shoulders, and nothing above them but a broken neck.
            c.Rect(55, 280, 145, 310, Ash);
            c.Rect(88, 310, 112, 322, Charcoal);
            // A sword held point-down, gilded once.
            c.Rect(97, 90, 103, 260, FadedGold);
            c.Rect(84, 250, 116, 258, FadedGold);
            Speckle(c, rng, 120, Scorch, 70, 310);
            return c;
        }

        private static Canvas TornBanner()
        {
            var c = new Canvas(Unit, Unit * 3);
            var rng = new System.Random(3303);
            c.Rect(0, 280, c.Width, 288, Charcoal);
            for (var x = 10; x < 90; x++)
            {
                var length = 60 + (int)(140 * Mathf.PerlinNoise(x * 0.09f, 3.1f)) + rng.Next(0, 10);
                c.Rect(x, 280 - length, x + 1, 280, x < 20 || x > 80 ? BannerDark : Banner);
            }

            c.Ellipse(new Vector2(50, 220), 16, 18, FadedGold);
            c.Ellipse(new Vector2(50, 220), 8, 10, Banner);
            return c;
        }

        private static Canvas BurntTree()
        {
            var c = new Canvas(Unit * 3, Unit * 5);
            var rng = new System.Random(3304);
            c.Thick(new Vector2(150, 0), new Vector2(140, 330), 40, 20, Soot);
            for (var i = 0; i < 6; i++)
            {
                var from = new Vector2(140 + rng.Next(-6, 6), 200 + i * 25);
                var to = from + new Vector2(rng.Next(-120, 120), rng.Next(40, 140));
                c.Thick(from, to, 10, 2, Soot);
            }

            Speckle(c, rng, 20, Ember, 0, 60);
            return c;
        }

        private static Canvas BurntCart()
        {
            var c = new Canvas(Unit * 3, Unit * 2);
            c.Rect(20, 50, 280, 70, Scorch);
            c.Rect(30, 70, 270, 120, Charcoal);
            for (var x = 40; x < 260; x += 30)
            {
                c.Rect(x, 70, x + 6, 130, Soot);
            }

            c.Ellipse(new Vector2(70, 45), 40, 40, Soot);
            c.Ellipse(new Vector2(70, 45), 30, 30, Clear);
            c.Ellipse(new Vector2(230, 30), 36, 22, Soot);
            return c;
        }

        /// <summary>A great bell, fallen from the tower and cracked through.</summary>
        private static Canvas FallenBell()
        {
            var c = new Canvas(Unit * 3, Unit * 3);
            for (var y = 0; y < 240; y++)
            {
                var half = Mathf.Lerp(130f, 60f, Mathf.Pow(y / 240f, 0.7f));
                c.Rect(150 - half, y, 150 + half, y + 1, Blend(FadedGold, Charcoal, 0.55f));
            }

            c.Rect(20, 0, 280, 14, Blend(FadedGold, Charcoal, 0.3f));
            c.Ellipse(new Vector2(150, 250), 30, 20, Blend(FadedGold, Charcoal, 0.55f));
            c.Thick(new Vector2(150, 0), new Vector2(170, 180), 8, 3, Soot);
            return c;
        }

        /// <summary>A brazier still burning: the one warm light in a street, and the colour of danger.</summary>
        private static Canvas Brazier()
        {
            var c = new Canvas(Unit, Unit * 2);
            c.Rect(46, 0, 54, 110, Charcoal);
            c.Rect(20, 110, 80, 130, Charcoal);
            c.Ellipse(new Vector2(50, 150), 26, 30, Ember);
            c.Ellipse(new Vector2(50, 158), 14, 20, EmberPale);
            return c;
        }

        private static Canvas TempleFront()
        {
            var c = new Canvas(Unit * 6, Unit * 7);
            var rng = new System.Random(3401);
            c.Rect(40, 0, c.Width - 40, 60, Stone);
            c.Rect(40, 56, c.Width - 40, 60, StoneLit);
            for (var i = 0; i < 5; i++)
            {
                var x = 80 + i * 105;
                var h = i == 3 ? 260 : 460;
                c.Rect(x, 60, x + 40, 60 + h, Stone);
                c.RectOver(x, 60, x + 4, 60 + h, StoneLit);
            }

            // The pediment, cracked across.
            c.Rect(40, 520, c.Width - 40, 560, Stone);
            c.Triangle(new Vector2(40, 560), new Vector2(c.Width - 40, 560), new Vector2(c.Width * 0.5f, 680), Stone);
            c.Thick(new Vector2(200, 560), new Vector2(280, 660), 6, 2, Soot);
            c.Ellipse(new Vector2(c.Width * 0.5f, 610), 22, 26, FadedGold);
            Speckle(c, rng, 400, Scorch, 60, 680);
            return c;
        }

        /// <summary>The throne, raised on steps, empty. Whoever sat here is waiting in front of it.</summary>
        private static Canvas Throne()
        {
            var c = new Canvas(Unit * 4, Unit * 5);
            for (var i = 0; i < 4; i++)
            {
                c.Rect(40 + i * 30, i * 30, c.Width - 40 - i * 30, i * 30 + 30, i % 2 == 0 ? Stone : Ash);
                c.Rect(40 + i * 30, i * 30 + 27, c.Width - 40 - i * 30, i * 30 + 30, StoneLit);
            }

            c.Rect(140, 120, 260, 180, Charcoal);
            c.Rect(150, 180, 250, 440, Charcoal);
            c.Triangle(new Vector2(150, 440), new Vector2(250, 440), new Vector2(200, 490), Charcoal);
            c.Rect(160, 190, 240, 420, Banner);
            c.Rect(130, 170, 150, 260, Charcoal);
            c.Rect(250, 170, 270, 260, Charcoal);
            c.Ellipse(new Vector2(200, 380), 18, 22, FadedGold);
            return c;
        }

        /// <summary>Drifting ash, laid in banks along the streets like the forest's fog.</summary>
        private static Canvas AshBank()
        {
            var c = new Canvas(512, 128);
            var rng = new System.Random(3501);
            for (var i = 0; i < 70; i++)
            {
                var centre = new Vector2(rng.Next(0, c.Width), rng.Next(30, 90));
                c.Ellipse(centre, rng.Next(30, 90), rng.Next(8, 20), new Color32(AshPale.r, AshPale.g, AshPale.b, 36));
            }

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

        private static void Vein(Canvas c, System.Random rng, float x, float y, int length, Color32 tone,
            bool horizontal)
        {
            for (var i = 0; i < length; i++)
            {
                c.SetWrapped((int)x, (int)y, tone);
                if (horizontal) { x += 1f; y += rng.Next(-1, 2) * 0.6f; }
                else { y -= 1f; x += rng.Next(-1, 2) * 0.6f; }
            }
        }

        private static Color32 Blend(Color32 a, Color32 b, float t) => Color32.Lerp(a, b, Mathf.Clamp01(t));
    }
}
