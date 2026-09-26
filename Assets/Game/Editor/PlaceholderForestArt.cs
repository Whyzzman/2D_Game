using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// Placeholder environment art for the Withered Forest.
    ///
    /// Written to the same contract as the Tomb's set and with the same honesty: it is not final
    /// art. What it is for is the question the level cannot answer without it — does a forest built
    /// from these pieces read as a different place from a crypt while still belonging to the same
    /// kingdom, and is the gameplay plane still the clearest thing on screen once trees are in
    /// front of it.
    ///
    /// The palette answers the first half. Zone one is stone, cold grey and cold blue; zone two is
    /// wet brown, dark green and a teal fog, with the same faded royal gold running through the
    /// ruins so the road and the watchtowers are visibly the same kingdom's. Poison green is used
    /// sparingly and only where something is dangerous, which is what stops the forest reading as
    /// bright and friendly — the brief was explicit that it must not.
    ///
    /// The second half is answered by contrast discipline: every background layer is darker and
    /// flatter than the ground the player stands on, and the one thing allowed to be bright is a
    /// hazard.
    /// </summary>
    public static class PlaceholderForestArt
    {
        public const string Root = "Assets/Game/Art/Environment/WitheredForest";
        private const int Unit = 100;

        private static readonly Color32 Clear = new Color32(0, 0, 0, 0);
        private static readonly Color32 Night = new Color32(0x0E, 0x12, 0x12, 0xFF);
        private static readonly Color32 BarkDark = new Color32(0x24, 0x1E, 0x19, 0xFF);
        private static readonly Color32 Bark = new Color32(0x33, 0x2A, 0x21, 0xFF);
        private static readonly Color32 BarkLit = new Color32(0x45, 0x39, 0x2C, 0xFF);
        private static readonly Color32 WetBrown = new Color32(0x3B, 0x30, 0x25, 0xFF);
        private static readonly Color32 Soil = new Color32(0x2C, 0x24, 0x1C, 0xFF);
        private static readonly Color32 Moss = new Color32(0x2E, 0x3E, 0x2C, 0xFF);
        private static readonly Color32 MossLit = new Color32(0x3E, 0x52, 0x38, 0xFF);
        private static readonly Color32 Teal = new Color32(0x24, 0x38, 0x3A, 0xFF);
        private static readonly Color32 TealPale = new Color32(0x3A, 0x55, 0x58, 0xFF);
        private static readonly Color32 FogBlue = new Color32(0x44, 0x5A, 0x62, 0xFF);
        private static readonly Color32 Moonlight = new Color32(0xAE, 0xC4, 0xC9, 0xFF);
        private static readonly Color32 RoadStone = new Color32(0x3C, 0x3A, 0x33, 0xFF);
        private static readonly Color32 RoadLit = new Color32(0x50, 0x4C, 0x42, 0xFF);
        private static readonly Color32 FadedGold = new Color32(0x8A, 0x74, 0x34, 0xFF);
        private static readonly Color32 Poison = new Color32(0x6E, 0xA8, 0x3C, 0xFF);
        private static readonly Color32 PoisonPale = new Color32(0x9B, 0xD0, 0x55, 0xFF);
        private static readonly Color32 Violet = new Color32(0x4A, 0x2C, 0x63, 0xFF);

        /// <summary>Every texture this generator writes, by logical name.</summary>
        public static Dictionary<string, Sprite> Generate()
        {
            PlaceholderImport.EnsureFolder($"{Root}/Tiles");
            PlaceholderImport.EnsureFolder($"{Root}/Backgrounds");
            PlaceholderImport.EnsureFolder($"{Root}/Props");
            PlaceholderImport.EnsureFolder($"{Root}/Arena");
            PlaceholderImport.EnsureFolder($"{Root}/Decals");

            var sprites = new Dictionary<string, Sprite>();

            Add(sprites, "Tiles", "env_forest_ground_01", GroundTile(), centred: true);
            Add(sprites, "Tiles", "env_forest_road_01", RoadTile(), centred: true);
            Add(sprites, "Tiles", "env_forest_cliff_01", CliffTile(), centred: true);
            Add(sprites, "Tiles", "env_forest_platform_01", PlankTile(false), centred: true);
            Add(sprites, "Tiles", "env_forest_platform_broken_01", PlankTile(true), centred: true);
            Add(sprites, "Tiles", "env_forest_rootwall_01", RootWallTile(), centred: true);

            Add(sprites, "Backgrounds", "env_forest_bg_far_01", Background(0));
            Add(sprites, "Backgrounds", "env_forest_bg_mid_01", Background(1));
            Add(sprites, "Backgrounds", "env_forest_bg_near_01", Background(2));

            Add(sprites, "Props", "env_forest_trunk_01", Trunk(false));
            Add(sprites, "Props", "env_forest_trunk_dead_01", Trunk(true));
            Add(sprites, "Props", "env_forest_stump_01", Stump());
            Add(sprites, "Props", "env_forest_roots_01", RootMass());
            Add(sprites, "Props", "env_forest_flora_01", CorruptFlora());
            Add(sprites, "Props", "env_forest_watchtower_01", Watchtower());
            Add(sprites, "Props", "env_forest_arch_ruin_01", RuinedArch());
            Add(sprites, "Props", "env_forest_milestone_01", Milestone());
            Add(sprites, "Props", "env_forest_statue_broken_01", BrokenStatue());
            Add(sprites, "Props", "env_forest_bridge_01", BridgeSection());
            Add(sprites, "Props", "env_forest_canopy_01", Canopy(), centred: true);
            Add(sprites, "Props", "env_forest_fog_01", FogBank(), centred: true);

            Add(sprites, "Arena", "env_forest_arena_backdrop", ArenaBackdrop());
            Add(sprites, "Arena", "env_forest_arena_crest", ArenaCrest(), centred: true);

            Add(sprites, "Decals", "env_forest_decal_moss_01", MossPatch(), centred: true);
            Add(sprites, "Decals", "env_forest_decal_puddle_01", Puddle(), centred: true);
            Add(sprites, "Decals", "env_forest_decal_spores_01", Spores(), centred: true);

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
                Debug.LogError($"Forest texture failed to import: {path}");
            }

            into[name] = sprite;
        }

        // ---------- tiles ----------

        /// <summary>
        /// Wet forest floor. The bright top lip is the same rule the Tomb's floor follows and for
        /// the same reason: it is how the player reads where the ground ends. Everything below it
        /// is soil and root, dark enough that a character never gets lost against it.
        /// </summary>
        private static Canvas GroundTile()
        {
            var c = new Canvas(Unit * 4, Unit);
            var rng = new System.Random(4101);
            c.Rect(0, 0, c.Width, c.Height, Soil);

            // Roots running through the soil, horizontal so they read as strata not as clutter.
            for (var i = 0; i < 14; i++)
            {
                var y = rng.Next(4, c.Height - 16);
                var x = rng.Next(0, c.Width);
                var length = rng.Next(30, 120);
                Vein(c, rng, x, y, length, BarkDark, horizontal: true);
            }

            Speckle(c, rng, 380, BarkDark, 0, c.Height - 12);
            // Turf: a band of moss over the soil, then the lit lip on top of that.
            c.Rect(0, c.Height - 12, c.Width, c.Height - 5, Moss);
            c.Rect(0, c.Height - 5, c.Width, c.Height, MossLit);
            for (var x = 0; x < c.Width; x += 3)
            {
                // A ragged grass edge, so the turf line is not a ruled pencil stroke.
                var blade = rng.Next(0, 4);
                c.Rect(x, c.Height - 5, x + 2, c.Height - 5 + blade, MossLit);
            }

            return c;
        }

        /// <summary>The royal road: cut flags, sunken and split, with grass coming up through.</summary>
        private static Canvas RoadTile()
        {
            var c = new Canvas(Unit * 4, Unit);
            var rng = new System.Random(4202);
            c.Rect(0, 0, c.Width, c.Height, Soil);
            c.Rect(0, c.Height - 26, c.Width, c.Height, RoadStone);

            // Flagstones, unevenly sunk: the road has been left alone for a long time.
            for (var x = 0; x < c.Width; x += 66)
            {
                var drop = rng.Next(0, 7);
                c.Rect(x + 2, c.Height - 24 + drop, x + 62, c.Height - drop, RoadLit);
                c.Rect(x + 2, c.Height - 24 + drop, x + 62, c.Height - 20 + drop, RoadStone);
            }

            Speckle(c, rng, 200, Soil, c.Height - 26, c.Height);
            // Moss in every joint, which is the thing that says "no one has walked this in years".
            for (var x = 0; x < c.Width; x += 66)
            {
                c.Rect(x - 2, c.Height - 26, x + 3, c.Height, Moss);
            }

            c.Rect(0, c.Height - 3, c.Width, c.Height, MossLit);
            return c;
        }

        /// <summary>Rock face, root-shot. Lower contrast than anything walkable.</summary>
        private static Canvas CliffTile()
        {
            var c = new Canvas(Unit * 2, Unit * 2);
            var rng = new System.Random(4303);
            c.Rect(0, 0, c.Width, c.Height, Teal);
            for (var i = 0; i < 26; i++)
            {
                var x = rng.Next(0, c.Width);
                var y = rng.Next(0, c.Height);
                var w = rng.Next(14, 46);
                var h = rng.Next(10, 30);
                c.Rect(x, y, x + w, y + h,
                    rng.Next(0, 2) == 0 ? Night : Blend(Teal, TealPale, 0.35f));
            }

            Speckle(c, rng, 420, Night, 0, c.Height);
            // Roots crawling down the face: the forest is taking the rock too.
            for (var i = 0; i < 6; i++)
            {
                Vein(c, rng, rng.Next(0, c.Width), c.Height - 4, rng.Next(50, 150), BarkDark,
                    horizontal: false);
            }

            return c;
        }

        /// <summary>A plank walkway, whole or half rotted away.</summary>
        private static Canvas PlankTile(bool broken)
        {
            var c = new Canvas(Unit * 2, Unit / 2);
            var rng = new System.Random(broken ? 4405 : 4404);
            c.Rect(0, 0, c.Width, c.Height, Clear);
            for (var x = 0; x < c.Width; x += 26)
            {
                if (broken && rng.Next(0, 4) == 0)
                {
                    // A missing plank is a gap you can see through, not a darker plank.
                    continue;
                }

                var top = c.Height - (broken ? rng.Next(4, 9) : 3);
                c.Rect(x + 1, 6, x + 24, top, broken ? WetBrown : Bark);
                c.Rect(x + 1, top - 4, x + 24, top, BarkLit);
            }

            // The beam under them, and the iron straps holding it together.
            c.Rect(0, 2, c.Width, 8, BarkDark);
            for (var x = 12; x < c.Width; x += 52)
            {
                c.Rect(x, 2, x + 4, c.Height - 4, Night);
            }

            if (broken)
            {
                Speckle(c, rng, 60, Moss, 0, c.Height);
            }

            return c;
        }

        /// <summary>A wall of woven root — the forest's own architecture, blocking the way.</summary>
        private static Canvas RootWallTile()
        {
            var c = new Canvas(Unit * 2, Unit * 2);
            var rng = new System.Random(4506);
            c.Rect(0, 0, c.Width, c.Height, BarkDark);
            for (var i = 0; i < 34; i++)
            {
                var x = rng.Next(-20, c.Width);
                var y = rng.Next(-20, c.Height);
                var w = rng.Next(10, 22);
                var h = rng.Next(60, 180);
                c.Rect(x, y, x + w, y + h, rng.Next(0, 3) == 0 ? Bark : BarkDark);
                c.Rect(x + w - 4, y, x + w, y + h, BarkLit);
            }

            Speckle(c, rng, 260, Violet, 0, c.Height);
            Speckle(c, rng, 90, Moss, 0, c.Height);
            return c;
        }

        // ---------- backgrounds ----------

        /// <summary>
        /// Three depths of forest. The far layer is fog with the capital's tower barely in it; the
        /// middle is a treeline; the near is trunks. Each is flatter and darker than the layer in
        /// front, so depth is read as depth rather than as clutter.
        ///
        /// The columns are spaced to divide the width exactly, because a background that does not
        /// tile is a seam that scrolls past the player every few seconds.
        /// </summary>
        private static Canvas Background(int depth)
        {
            var c = new Canvas(Unit * 16, Unit * 9);
            var rng = new System.Random(4600 + depth);
            var sky = depth == 0 ? Blend(Night, FogBlue, 0.45f)
                : depth == 1 ? Blend(Night, Teal, 0.55f) : Night;
            c.Rect(0, 0, c.Width, c.Height, sky);

            if (depth == 0)
            {
                // Moon, and the fog bands it lights. The one bright thing in the whole zone.
                c.Ellipse(new Vector2(c.Width * 0.72f, c.Height * 0.78f), 44f, 44f,
                    Blend(sky, Moonlight, 0.75f));
                for (var i = 0; i < 6; i++)
                {
                    var y = c.Height * (0.30f + i * 0.09f);
                    c.Rect(0, y, c.Width, y + 14f, Blend(sky, FogBlue, 0.3f - i * 0.03f));
                }

                // The capital's tower, far off and broken: the reason any of this is happening.
                var towerX = c.Width * 0.28f;
                c.Rect(towerX - 16f, c.Height * 0.38f, towerX + 16f, c.Height * 0.72f,
                    Blend(sky, Night, 0.55f));
                c.Triangle(new Vector2(towerX - 22f, c.Height * 0.70f),
                    new Vector2(towerX + 22f, c.Height * 0.70f),
                    new Vector2(towerX - 4f, c.Height * 0.80f), Blend(sky, Night, 0.6f));
            }

            // Treelines. Spacing divides 1600 exactly at every depth, so the layer tiles.
            var spacing = depth == 0 ? 200 : depth == 1 ? 160 : 400;
            var tone = depth == 0 ? Blend(sky, Night, 0.4f)
                : depth == 1 ? Blend(Night, Bark, 0.35f) : BarkDark;
            for (var x = 0; x < c.Width; x += spacing)
            {
                var jitter = rng.Next(-spacing / 5, spacing / 5);
                var trunkX = x + spacing * 0.5f + jitter;
                var height = c.Height * (depth == 0 ? 0.40f : depth == 1 ? 0.62f : 1.05f)
                    + rng.Next(-40, 60);
                var width = depth == 0 ? 10f : depth == 1 ? 20f : 52f;
                c.Rect(trunkX - width, 0f, trunkX + width, height, tone);

                // Bare branches, forking upward. Dead: no canopy mass at all.
                var forks = depth == 2 ? 5 : 3;
                for (var b = 0; b < forks; b++)
                {
                    var side = b % 2 == 0 ? 1f : -1f;
                    var at = height * (0.55f + b * 0.09f);
                    var reach = width * (depth == 2 ? 3.2f : 4.5f) * (0.6f + b * 0.12f);
                    c.Thick(new Vector2(trunkX, at),
                        new Vector2(trunkX + side * reach, at + reach * 0.55f),
                        width * 0.45f, width * 0.14f, tone);
                }
            }

            if (depth < 2)
            {
                // Ground fog sitting in the treeline, which is what makes the forest feel wet.
                for (var i = 0; i < 4; i++)
                {
                    var y = 30f + i * 26f;
                    c.TintOver(0f, y, c.Width, y + 22f, FogBlue, 0.18f - i * 0.03f);
                }
            }

            return c;
        }

        // ---------- props ----------

        private static Canvas Trunk(bool dead)
        {
            var c = new Canvas(Unit * 2, Unit * 7);
            var rng = new System.Random(dead ? 4702 : 4701);
            var body = dead ? BarkDark : Bark;
            var lean = dead ? 7f : 2f;

            // Roots flaring at the base, so it grows out of the ground instead of being planted.
            for (var i = -3; i <= 3; i++)
            {
                if (i == 0) { continue; }
                c.Thick(new Vector2(c.Width * 0.5f, 60f),
                    new Vector2(c.Width * 0.5f + i * 26f, 2f), 14f, 6f, BarkDark);
            }

            var top = new Vector2(c.Width * 0.5f + lean * 6f, c.Height - 40f);
            c.Thick(new Vector2(c.Width * 0.5f, 10f), top, 46f, 24f, body);
            // A lit edge down one side; without it a trunk is a brown rectangle.
            c.Thick(new Vector2(c.Width * 0.5f + 16f, 10f),
                new Vector2(top.x + 8f, top.y), 8f, 5f, BarkLit);

            for (var b = 0; b < 5; b++)
            {
                var side = b % 2 == 0 ? 1f : -1f;
                var at = Vector2.Lerp(new Vector2(c.Width * 0.5f, 10f), top, 0.55f + b * 0.1f);
                c.Thick(at, at + new Vector2(side * (60f + b * 12f), 50f + b * 16f), 13f, 4f, body);
            }

            if (dead)
            {
                // Split open and hollow, with the corruption inside rather than on it.
                c.Rect(c.Width * 0.5f - 10f, 90f, c.Width * 0.5f + 8f, c.Height * 0.55f, Night);
                c.Rect(c.Width * 0.5f - 5f, 110f, c.Width * 0.5f + 3f, c.Height * 0.45f, Violet);
            }
            else
            {
                Speckle(c, rng, 120, Moss, 0, (int)(c.Height * 0.4f));
            }

            return c;
        }

        private static Canvas Stump()
        {
            var c = new Canvas(Unit * 2, Unit);
            var rng = new System.Random(4703);
            c.Rect(c.Width * 0.5f - 44f, 0f, c.Width * 0.5f + 44f, 58f, Bark);
            c.Rect(c.Width * 0.5f - 44f, 50f, c.Width * 0.5f + 44f, 58f, BarkLit);
            // A jagged break rather than a saw cut: nothing felled this, it came down.
            for (var x = -44; x < 44; x += 11)
            {
                var h = rng.Next(0, 22);
                c.Rect(c.Width * 0.5f + x, 58f, c.Width * 0.5f + x + 10f, 58f + h, Bark);
            }

            c.Ellipse(new Vector2(c.Width * 0.5f, 56f), 40f, 9f, WetBrown);
            c.Ellipse(new Vector2(c.Width * 0.5f, 56f), 22f, 5f, BarkDark);
            Speckle(c, rng, 70, Moss, 0, 40);
            return c;
        }

        private static Canvas RootMass()
        {
            var c = new Canvas(Unit * 3, Unit * 2);
            var rng = new System.Random(4704);
            for (var i = 0; i < 18; i++)
            {
                var x = rng.Next(10, c.Width - 10);
                var height = rng.Next(40, 150);
                c.Thick(new Vector2(x, 0f),
                    new Vector2(x + rng.Next(-50, 50), height), rng.Next(8, 18), 4f,
                    rng.Next(0, 3) == 0 ? Bark : BarkDark);
            }

            Speckle(c, rng, 140, Violet, 0, c.Height / 2);
            Speckle(c, rng, 100, Moss, 0, 40);
            return c;
        }

        /// <summary>
        /// The corrupted growth. It is the one prop allowed to be bright, because a player has to
        /// be able to tell at a glance which plants are decoration and which ones are a hazard.
        /// </summary>
        private static Canvas CorruptFlora()
        {
            var c = new Canvas(Unit * 2, Unit * 2);
            var rng = new System.Random(4705);
            for (var i = 0; i < 7; i++)
            {
                var baseX = c.Width * 0.5f + rng.Next(-60, 60);
                var height = rng.Next(60, 140);
                var tipX = baseX + rng.Next(-24, 24);
                c.Thick(new Vector2(baseX, 0f), new Vector2(tipX, height), 7f, 3f, Moss);
                // A bulb at the top, lit from inside: this is the part that spits.
                c.Ellipse(new Vector2(tipX, height), 15f, 19f, Blend(Moss, Poison, 0.55f));
                c.Ellipse(new Vector2(tipX, height + 3f), 8f, 11f, PoisonPale);
                for (var p = 0; p < 4; p++)
                {
                    c.Triangle(new Vector2(tipX - 14f, height + 8f),
                        new Vector2(tipX + 14f, height + 8f),
                        new Vector2(tipX + rng.Next(-20, 20), height + 34f),
                        Blend(Poison, PoisonPale, 0.4f));
                }
            }

            return c;
        }

        private static Canvas Watchtower()
        {
            var c = new Canvas(Unit * 4, Unit * 8);
            var rng = new System.Random(4706);
            var left = c.Width * 0.5f - 90f;
            var right = c.Width * 0.5f + 90f;

            // Sheared off at an angle: it did not fall, it was broken and left.
            c.Rect(left, 0f, right, c.Height * 0.62f, RoadStone);
            c.Triangle(new Vector2(left, c.Height * 0.62f), new Vector2(right, c.Height * 0.62f),
                new Vector2(right, c.Height * 0.80f), RoadStone);
            for (var y = 0; y < c.Height * 0.78f; y += 34)
            {
                c.Rect(left, y, right, y + 3f, Blend(RoadStone, Night, 0.5f));
            }

            Speckle(c, rng, 300, Blend(RoadStone, Night, 0.4f), 0, (int)(c.Height * 0.78f));

            // An arrow slit, and the royal crest above the door: the kingdom's own tower.
            c.Rect(c.Width * 0.5f - 9f, c.Height * 0.36f, c.Width * 0.5f + 9f, c.Height * 0.50f,
                Night);
            c.Rect(c.Width * 0.5f - 34f, 0f, c.Width * 0.5f + 34f, 96f, Night);
            c.Ellipse(new Vector2(c.Width * 0.5f, 118f), 17f, 21f, FadedGold);
            c.Ellipse(new Vector2(c.Width * 0.5f, 118f), 9f, 12f, RoadStone);

            // The forest reclaiming it: roots up one side, moss along every course.
            for (var i = 0; i < 9; i++)
            {
                Vein(c, rng, left + rng.Next(0, 24), rng.Next(0, (int)(c.Height * 0.6f)),
                    rng.Next(40, 120), BarkDark, horizontal: false);
            }

            Speckle(c, rng, 200, Moss, 0, (int)(c.Height * 0.5f));
            return c;
        }

        private static Canvas RuinedArch()
        {
            var c = new Canvas(Unit * 4, Unit * 4);
            var rng = new System.Random(4707);
            c.Rect(40f, 0f, 92f, 300f, RoadStone);
            c.Rect(c.Width - 92f, 0f, c.Width - 40f, 224f, RoadStone);
            // The span is gone from the middle; only the two springing stones are left.
            c.Rect(40f, 300f, 150f, 336f, RoadStone);
            c.Rect(c.Width - 140f, 224f, c.Width - 40f, 254f, RoadStone);
            Speckle(c, rng, 240, Blend(RoadStone, Night, 0.45f), 0, 340);
            Speckle(c, rng, 150, Moss, 0, 180);
            for (var i = 0; i < 6; i++)
            {
                Vein(c, rng, 50 + rng.Next(0, 40), rng.Next(0, 280), rng.Next(40, 110), BarkDark,
                    horizontal: false);
            }

            return c;
        }

        /// <summary>
        /// A milestone off the royal road. It is the zone's quietest piece of storytelling and the
        /// only one that carries gold at eye level, so the player looks at it.
        /// </summary>
        private static Canvas Milestone()
        {
            var c = new Canvas(Unit, Unit * 2);
            var rng = new System.Random(4708);
            c.Rect(c.Width * 0.5f - 22f, 0f, c.Width * 0.5f + 22f, 140f, RoadStone);
            c.Rect(c.Width * 0.5f - 22f, 132f, c.Width * 0.5f + 22f, 140f, RoadLit);
            // Leaning, because the ground under the road has moved.
            c.Rect(c.Width * 0.5f - 26f, 0f, c.Width * 0.5f - 18f, 120f, Blend(RoadStone, Night, 0.4f));
            c.Ellipse(new Vector2(c.Width * 0.5f, 104f), 12f, 15f, FadedGold);
            for (var i = 0; i < 3; i++)
            {
                c.Rect(c.Width * 0.5f - 13f, 58f + i * 14f, c.Width * 0.5f + 13f, 62f + i * 14f,
                    Blend(RoadStone, Night, 0.6f));
            }

            Speckle(c, rng, 90, Moss, 0, 70);
            return c;
        }

        private static Canvas BrokenStatue()
        {
            var c = new Canvas(Unit * 2, Unit * 3);
            var rng = new System.Random(4709);
            c.Rect(c.Width * 0.5f - 46f, 0f, c.Width * 0.5f + 46f, 40f, RoadStone);
            c.Rect(c.Width * 0.5f - 34f, 40f, c.Width * 0.5f + 34f, 150f, RoadStone);
            // Headless and one-armed; the forest has taken the rest.
            c.Thick(new Vector2(c.Width * 0.5f - 24f, 140f),
                new Vector2(c.Width * 0.5f - 62f, 96f), 18f, 12f, RoadStone);
            c.Rect(c.Width * 0.5f - 34f, 150f, c.Width * 0.5f + 6f, 168f,
                Blend(RoadStone, Night, 0.5f));
            for (var i = 0; i < 7; i++)
            {
                Vein(c, rng, c.Width * 0.5f + rng.Next(-30, 30), rng.Next(0, 140),
                    rng.Next(30, 90), BarkDark, horizontal: false);
            }

            Speckle(c, rng, 160, Moss, 0, 120);
            return c;
        }

        private static Canvas BridgeSection()
        {
            var c = new Canvas(Unit * 4, Unit * 2);
            var rng = new System.Random(4710);
            // The deck, then the ropes and the posts that hold it. Drawn hanging, because it is
            // strung across a gap rather than standing on anything.
            c.Rect(0f, 150f, c.Width, 168f, Bark);
            for (var x = 0; x < c.Width; x += 24)
            {
                if (rng.Next(0, 6) == 0) { continue; }
                c.Rect(x + 2f, 150f, x + 20f, 172f, rng.Next(0, 3) == 0 ? WetBrown : Bark);
            }

            c.Rect(0f, 146f, c.Width, 152f, BarkDark);
            for (var x = 0; x < c.Width; x += 96)
            {
                c.Rect(x + 6f, 150f, x + 18f, 196f, BarkDark);
                c.Thick(new Vector2(x + 12f, 196f), new Vector2(x + 108f, 196f), 5f, 5f, WetBrown);
            }

            Speckle(c, rng, 120, Moss, 140, 176);
            return c;
        }

        /// <summary>
        /// Overhead branches for the foreground layer. Drawn thin and mostly transparent: it frames
        /// the shot, and the Visual Bible's rule is that it must never hide an attack.
        /// </summary>
        private static Canvas Canopy()
        {
            var c = new Canvas(Unit * 8, Unit * 3);
            var rng = new System.Random(4711);
            for (var i = 0; i < 9; i++)
            {
                var x = rng.Next(0, c.Width);
                var start = new Vector2(x, c.Height);
                var end = new Vector2(x + rng.Next(-150, 150), c.Height - rng.Next(60, 200));
                c.Thick(start, end, rng.Next(14, 26), 5f, BarkDark);
                for (var b = 0; b < 3; b++)
                {
                    var at = Vector2.Lerp(start, end, 0.4f + b * 0.2f);
                    c.Thick(at, at + new Vector2(rng.Next(-70, 70), -rng.Next(20, 70)), 7f, 2f,
                        BarkDark);
                }
            }

            return c;
        }

        private static Canvas FogBank()
        {
            var c = new Canvas(Unit * 8, Unit * 2);
            var rng = new System.Random(4712);
            for (var i = 0; i < 26; i++)
            {
                var x = rng.Next(0, c.Width);
                var y = rng.Next(20, c.Height - 20);
                var rx = rng.Next(60, 170);
                var ry = rng.Next(14, 40);
                c.Ellipse(new Vector2(x, y), rx, ry,
                    new Color32(FogBlue.r, FogBlue.g, FogBlue.b, (byte)rng.Next(14, 34)));
            }

            return c;
        }

        // ---------- arena ----------

        /// <summary>
        /// The Warden's clearing: a dead tree at the centre, a ruined shrine behind it and the moon
        /// directly above. Open sky, which is the single strongest contrast with the Guardian's
        /// vaulted crypt — the player should know they are outdoors before the fight starts.
        /// </summary>
        private static Canvas ArenaBackdrop()
        {
            var c = new Canvas(Unit * 16, Unit * 9);
            var rng = new System.Random(4713);
            c.Rect(0, 0, c.Width, c.Height, Blend(Night, Teal, 0.35f));

            // Moon and its light, centred: the arena is lit from above and from nowhere else.
            var moon = new Vector2(c.Width * 0.5f, c.Height * 0.82f);
            c.Ellipse(moon, 120f, 120f, Blend(Night, Moonlight, 0.18f));
            c.Ellipse(moon, 62f, 62f, Blend(Night, Moonlight, 0.85f));

            // The great dead tree, filling the middle and reaching off the top of the frame.
            var trunkX = c.Width * 0.5f;
            c.Thick(new Vector2(trunkX, 0f), new Vector2(trunkX + 20f, c.Height * 0.72f),
                150f, 60f, Night);
            for (var b = 0; b < 8; b++)
            {
                var side = b % 2 == 0 ? 1f : -1f;
                var at = new Vector2(trunkX + b * 3f, c.Height * (0.40f + b * 0.045f));
                c.Thick(at, at + new Vector2(side * (200f + b * 40f), 120f + b * 26f),
                    36f - b * 2f, 8f, Night);
            }

            // The shrine behind it, half swallowed. Royal stone, same as the road.
            for (var i = -1; i <= 1; i += 2)
            {
                var x = trunkX + i * 420f;
                c.Rect(x - 40f, 0f, x + 40f, c.Height * 0.44f, Blend(RoadStone, Night, 0.55f));
                c.Rect(x - 56f, c.Height * 0.44f, x + 56f, c.Height * 0.48f,
                    Blend(RoadStone, Night, 0.5f));
            }

            // Ground fog across the whole clearing, heaviest at the feet.
            for (var i = 0; i < 6; i++)
            {
                c.TintOver(0f, i * 24f, c.Width, i * 24f + 26f, FogBlue, 0.22f - i * 0.03f);
            }

            Speckle(c, rng, 200, Blend(Night, Violet, 0.5f), 0, (int)(c.Height * 0.3f));
            return c;
        }

        /// <summary>The ring of roots the Warden rises out of, painted on the arena floor.</summary>
        private static Canvas ArenaCrest()
        {
            var c = new Canvas(Unit * 8, Unit * 2);
            var rng = new System.Random(4714);
            var centre = new Vector2(c.Width * 0.5f, c.Height * 0.5f);
            c.Ellipse(centre, 330f, 70f, new Color32(Violet.r, Violet.g, Violet.b, 40));
            c.Ellipse(centre, 300f, 58f, new Color32(Night.r, Night.g, Night.b, 0));
            for (var i = 0; i < 22; i++)
            {
                var angle = i / 22f * Mathf.PI * 2f;
                var from = centre + new Vector2(Mathf.Cos(angle) * 150f, Mathf.Sin(angle) * 28f);
                var to = centre + new Vector2(Mathf.Cos(angle) * 320f, Mathf.Sin(angle) * 64f);
                c.Thick(from, to, 9f, 3f, new Color32(BarkDark.r, BarkDark.g, BarkDark.b, 170));
            }

            Speckle(c, rng, 120, new Color32(Poison.r, Poison.g, Poison.b, 90), 0, c.Height);
            return c;
        }

        // ---------- decals ----------

        private static Canvas MossPatch()
        {
            var c = new Canvas(Unit * 2, Unit);
            var rng = new System.Random(4715);
            for (var i = 0; i < 40; i++)
            {
                c.Ellipse(new Vector2(rng.Next(0, c.Width), rng.Next(0, c.Height)),
                    rng.Next(8, 26), rng.Next(4, 12),
                    new Color32(Moss.r, Moss.g, Moss.b, (byte)rng.Next(60, 150)));
            }

            return c;
        }

        private static Canvas Puddle()
        {
            var c = new Canvas(Unit * 2, Unit / 2);
            var rng = new System.Random(4716);
            c.Ellipse(new Vector2(c.Width * 0.5f, c.Height * 0.5f), 84f, 18f,
                new Color32(Teal.r, Teal.g, Teal.b, 150));
            c.Ellipse(new Vector2(c.Width * 0.5f, c.Height * 0.55f), 62f, 11f,
                new Color32(TealPale.r, TealPale.g, TealPale.b, 90));
            Speckle(c, rng, 40, new Color32(Moonlight.r, Moonlight.g, Moonlight.b, 60), 0, c.Height);
            return c;
        }

        private static Canvas Spores()
        {
            var c = new Canvas(Unit * 2, Unit * 2);
            var rng = new System.Random(4717);
            for (var i = 0; i < 70; i++)
            {
                c.Ellipse(new Vector2(rng.Next(0, c.Width), rng.Next(0, c.Height)),
                    rng.Next(2, 6), rng.Next(2, 6),
                    new Color32(PoisonPale.r, PoisonPale.g, PoisonPale.b, (byte)rng.Next(40, 120)));
            }

            return c;
        }

        // ---------- shared marks ----------

        private static void Speckle(Canvas c, System.Random rng, int count, Color32 tone,
            int fromY, int toY)
        {
            for (var i = 0; i < count; i++)
            {
                var x = rng.Next(0, c.Width);
                var y = rng.Next(fromY, Mathf.Max(fromY + 1, toY));
                // Only over something that is already there, so a mark never adds silhouette.
                c.RectOver(x, y, x + rng.Next(1, 4), y + rng.Next(1, 3), tone);
            }
        }

        private static void Vein(Canvas c, System.Random rng, float x, float y, int length,
            Color32 tone, bool horizontal)
        {
            var px = x;
            var py = y;
            for (var i = 0; i < length; i++)
            {
                c.RectOver(px, py, px + 2f, py + 2f, tone);
                if (horizontal)
                {
                    px += 1f;
                    py += rng.Next(-1, 2) * 0.5f;
                }
                else
                {
                    py -= 1f;
                    px += rng.Next(-1, 2) * 0.5f;
                }
            }
        }

        private static Color32 Blend(Color32 a, Color32 b, float t) => Color32.Lerp(a, b, t);
    }
}
