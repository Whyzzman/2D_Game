using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// Draws the Forgotten Tomb: tiles, props, background layers, the checkpoint altar, the boss
    /// gate and the arena backdrop.
    ///
    /// This is not final art and does not pretend to be. It exists because the questions a level
    /// has to answer cannot wait on a painted set: does a platform edge read, does the pit read as
    /// dangerous before you reach it, does the player's dark silhouette survive against stone, does
    /// the route to the Guardian feel like it is escalating. Palette, contrast bands and the royal
    /// motif follow `VISUAL_BIBLE.md`, so painted tiles drop in against the same contract.
    ///
    /// Everything is generated from a fixed seed, so a rebuild produces identical bytes and only
    /// genuinely changed files are rewritten.
    /// </summary>
    public static class PlaceholderTombArt
    {
        public const string Root = "Assets/Game/Art/Environment/Tomb";
        private const int Unit = 100;

        // The environment palette from Visual Bible §3. Muted on purpose: the gameplay plane is
        // the most contrasted thing on screen and everything here stays under it.
        private static readonly Color32 Clear = new Color32(0, 0, 0, 0);
        private static readonly Color32 Charcoal = new Color32(0x14, 0x11, 0x0F, 0xFF);
        private static readonly Color32 Stone = new Color32(0x2A, 0x25, 0x20, 0xFF);
        private static readonly Color32 StoneLit = new Color32(0x3A, 0x34, 0x2C, 0xFF);
        private static readonly Color32 StoneDark = new Color32(0x1D, 0x19, 0x16, 0xFF);
        private static readonly Color32 ColdGrey = new Color32(0x3E, 0x42, 0x47, 0xFF);
        private static readonly Color32 ColdBlue = new Color32(0x2F, 0x3E, 0x52, 0xFF);
        private static readonly Color32 Moss = new Color32(0x33, 0x45, 0x3B, 0xFF);
        private static readonly Color32 Brown = new Color32(0x4A, 0x3B, 0x2E, 0xFF);
        private static readonly Color32 Corruption = new Color32(0x2B, 0x1F, 0x3A, 0xFF);
        private static readonly Color32 Carmine = new Color32(0x3A, 0x16, 0x20, 0xFF);
        private static readonly Color32 FadedGold = new Color32(0x8A, 0x74, 0x34, 0xFF);
        // What can be stood on or run into: warmer and a step lighter than everything behind it,
        // which is cold and blue, with a bright worn edge on top — the Hollow Knight rule of a dark
        // world where the next foothold is never in doubt.
        private static readonly Color32 SolidStone = new Color32(0x44, 0x3C, 0x33, 0xFF);
        private static readonly Color32 SolidLit = new Color32(0x5C, 0x52, 0x45, 0xFF);
        private static readonly Color32 SolidDark = new Color32(0x2C, 0x26, 0x20, 0xFF);
        private static readonly Color32 SolidLip = new Color32(0xA6, 0x93, 0x72, 0xFF);
        private static readonly Color32 SolidLipBright = new Color32(0xD2, 0xBE, 0x94, 0xFF);
        private static readonly Color32 WarmFlame = new Color32(0xF0, 0xB4, 0x41, 0xFF);
        private static readonly Color32 WarmPale = new Color32(0xFF, 0xE2, 0xA8, 0xFF);

        /// <summary>Every texture this generator writes, by logical name.</summary>
        public static Dictionary<string, Sprite> Generate()
        {
            PlaceholderImport.EnsureFolder($"{Root}/Tiles");
            PlaceholderImport.EnsureFolder($"{Root}/Backgrounds");
            PlaceholderImport.EnsureFolder($"{Root}/Props");
            PlaceholderImport.EnsureFolder($"{Root}/Interactive");
            PlaceholderImport.EnsureFolder($"{Root}/Arena");
            PlaceholderImport.EnsureFolder($"{Root}/Decals");

            var sprites = new Dictionary<string, Sprite>();

            // Tiles repeat every four units rather than every one: the seam still exists, but it
            // arrives four times less often and lands on a mortar line where it belongs.
            Add(sprites, "Tiles", "env_tomb_floor_01", FloorTile(), centred: true);
            Add(sprites, "Tiles", "env_tomb_wall_01", WallTile(), centred: true);
            Add(sprites, "Tiles", "env_tomb_ceiling_01", CeilingTile(), centred: true);
            Add(sprites, "Tiles", "env_tomb_platform_01", PlatformTile(false), centred: true);
            Add(sprites, "Tiles", "env_tomb_platform_broken_01", PlatformTile(true),
                centred: true);
            Add(sprites, "Tiles", "env_tomb_trim_01", TrimTile(), centred: true);
            Add(sprites, "Tiles", "env_tomb_chain_tile_01", ChainTile(), centred: true);

            Add(sprites, "Backgrounds", "env_tomb_bg_far_01", GothicBackdrop.Layer(0));
            Add(sprites, "Backgrounds", "env_tomb_bg_mid_01", GothicBackdrop.Layer(1));
            Add(sprites, "Backgrounds", "env_tomb_bg_near_01", GothicBackdrop.Layer(2));

            Add(sprites, "Props", "env_tomb_pillar_01", Pillar(false));
            Add(sprites, "Props", "env_tomb_pillar_broken_01", Pillar(true));
            Add(sprites, "Props", "env_tomb_arch_01", Arch());
            Add(sprites, "Props", "env_tomb_statue_knight_01", Statue(false));
            Add(sprites, "Props", "env_tomb_statue_broken_01", Statue(true));
            Add(sprites, "Props", "env_tomb_sarcophagus_01", Sarcophagus(false));
            Add(sprites, "Props", "env_tomb_sarcophagus_broken_01", Sarcophagus(true));
            Add(sprites, "Props", "env_tomb_banner_01", Banner());
            Add(sprites, "Props", "env_tomb_chains_01", Chain(), centred: true);
            Add(sprites, "Props", "env_tomb_candles_01", Candles());
            Add(sprites, "Props", "env_tomb_rubble_01", Rubble());
            Add(sprites, "Props", "env_tomb_roots_01", Roots());
            Add(sprites, "Props", "env_tomb_bones_01", Bones());
            Add(sprites, "Props", "env_tomb_memorial_01", Memorial());
            Add(sprites, "Props", "env_tomb_grave_marker_01", GraveMarker());
            Add(sprites, "Props", "env_tomb_urn_01", Urn());

            Add(sprites, "Interactive", "env_tomb_checkpoint_base", AltarBase());
            Add(sprites, "Interactive", "env_tomb_checkpoint_flame_cold", AltarFlame(false));
            Add(sprites, "Interactive", "env_tomb_checkpoint_flame_lit", AltarFlame(true));
            Add(sprites, "Interactive", "env_tomb_glow_warm", Glow(WarmFlame), centred: true);
            Add(sprites, "Interactive", "env_tomb_boss_gate", BossGate());
            Add(sprites, "Interactive", "env_tomb_boss_barrier", Barrier(), centred: true);

            Add(sprites, "Arena", "env_tomb_arena_backdrop", ArenaBackdrop());
            Add(sprites, "Arena", "env_tomb_arena_floor_crest", ArenaCrest(), centred: true);

            Add(sprites, "Decals", "env_tomb_decal_crack_01", Crack(), centred: true);
            Add(sprites, "Decals", "env_tomb_decal_moss_01", MossPatch(), centred: true);
            Add(sprites, "Decals", "env_tomb_decal_rune_01", Rune(), centred: true);
            Add(sprites, "Decals", "env_tomb_pit_depth", PitDepth(), centred: true);

            return sprites;
        }

        /// <summary>
        /// Writes one texture. Ground-standing art is pivoted at its base so it sits on the floor
        /// line; tiles and overlays are pivoted centre so they can be stretched symmetrically.
        /// </summary>
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
                Debug.LogError($"Tomb texture failed to import: {path}");
            }

            into[name] = sprite;
        }

        // ---------- tiles ----------

        /// <summary>
        /// Solid floor: dense courses with a bright top lip. The lip is the whole point — it is how
        /// the player reads where the floor ends, which the Visual Bible made a rule.
        /// </summary>
        private static Canvas FloorTile()
        {
            var c = new Canvas(Unit * 4, Unit);
            var rng = new System.Random(101);
            c.Rect(0, 0, c.Width, c.Height, SolidStone);
            Courses(c, rng, 50, 25, SolidDark, SolidLit);
            Speckle(c, rng, 240, SolidDark, 0, c.Height - 12);
            Lip(c, rng);
            return c;
        }

        /// <summary>Wall: vertical courses, lower contrast than the floor, moss and damage.</summary>
        private static Canvas WallTile()
        {
            var c = new Canvas(Unit * 2, Unit * 2);
            var rng = new System.Random(202);
            c.Rect(0, 0, c.Width, c.Height, Blend(SolidStone, SolidDark, 0.45f));
            Courses(c, rng, 40, 34, SolidDark, SolidStone);
            Speckle(c, rng, 300, SolidDark, 0, c.Height);
            // One faint streak of moss: five bright ones read as scratches on the screen.
            Vein(c, rng, rng.Next(0, c.Width), rng.Next(0, c.Height), 22, Blend(Moss, SolidDark, 0.5f));

            return c;
        }

        private static Canvas CeilingTile()
        {
            var c = new Canvas(Unit * 4, Unit);
            var rng = new System.Random(303);
            c.Rect(0, 0, c.Width, c.Height, StoneDark);
            Courses(c, rng, 50, 30, Charcoal, Stone);
            // Underside is the darkest thing in the room; nothing reads against it by design.
            c.Rect(0, 0, c.Width, 8, Charcoal);
            Speckle(c, rng, 200, Charcoal, 0, c.Height);
            return c;
        }

        /// <summary>
        /// A platform reads as walkable by having a pronounced carried lip and a shadowed
        /// underside. The broken variant loses stone from its edge but keeps the same lip, because
        /// a platform that stops looking solid stops being trusted.
        /// </summary>
        private static Canvas PlatformTile(bool broken)
        {
            var c = new Canvas(Unit * 3, 60);
            var rng = new System.Random(broken ? 405 : 404);
            c.Rect(0, 0, c.Width, c.Height, SolidStone);
            Courses(c, rng, 46, 20, SolidDark, SolidLit);
            Lip(c, rng);
            // Carried lip: the underside is cut back so the edge casts its own line.
            c.Rect(0, 0, c.Width, 10, Charcoal);
            if (broken)
            {
                for (var i = 0; i < 9; i++)
                {
                    var x = rng.Next(0, c.Width);
                    c.Ellipse(new Vector2(x, rng.Next(8, 26)), rng.Next(6, 16), rng.Next(5, 12),
                        Clear);
                }
            }

            Speckle(c, rng, 120, SolidDark, 10, c.Height - 12);
            return c;
        }

        /// <summary>
        /// The worn top of anything that can be stood on: a pale band with a brighter line along
        /// it, broken here and there by chips, and a dark line under it that lifts it off the
        /// stone. It is what the eye looks for first.
        /// </summary>
        private static void Lip(Canvas c, System.Random rng)
        {
            c.Rect(0, c.Height - 13, c.Width, c.Height - 10, SolidDark);
            c.Rect(0, c.Height - 10, c.Width, c.Height, SolidLip);
            c.Rect(0, c.Height - 3, c.Width, c.Height, SolidLipBright);
            for (var i = 0; i < c.Width / 40; i++)
            {
                var x = rng.Next(0, c.Width);
                c.Rect(x, c.Height - 10, x + rng.Next(4, 12), c.Height - rng.Next(4, 8), SolidLit);
            }
        }

        /// <summary>
        /// A chain that repeats: four links to the tile, so it can hang any length from a
        /// platform to the vault above it (SceneTombLook).
        /// </summary>
        private static Canvas ChainTile()
        {
            var c = new Canvas(24, 64);
            for (var y = 8; y < 64; y += 16)
            {
                c.Ellipse(new Vector2(12, y), 7f, 8f, Blend(ColdGrey, SolidDark, 0.25f));
                c.Ellipse(new Vector2(12, y), 3.5f, 4.5f, Clear);
            }

            return c;
        }

        /// <summary>Faded royal trim: the motif that ties tiles, banners and the arena together.</summary>
        private static Canvas TrimTile()
        {
            var c = new Canvas(Unit * 4, 26);
            c.Rect(0, 0, c.Width, c.Height, StoneDark);
            c.Rect(0, 3, c.Width, 6, FadedGold);
            c.Rect(0, c.Height - 6, c.Width, c.Height - 3, FadedGold);
            for (var x = 20; x < c.Width; x += 50)
            {
                Crest(c, new Vector2(x, c.Height * 0.5f), 8f, FadedGold, StoneDark);
            }

            return c;
        }

        // ---------- backgrounds ----------

        /// <summary>The background layers themselves are drawn by <see cref="GothicBackdrop"/>.</summary>

        /// <summary>
        /// A shaft of cold light falling through a crack. It lightens the stone it crosses rather
        /// than being painted over it, so the wall stays a wall — and it fades out downward, the
        /// way light actually loses itself in dust.
        /// </summary>
        private static void Shaft(Canvas c, int x, int height, Color32 tone)
        {
            for (var y = 0; y < height; y++)
            {
                var t = y / (float)height;
                var half = Mathf.Lerp(8f, 46f, 1f - t);
                // Strongest where it enters at the top, almost gone by the floor.
                var strength = Mathf.Lerp(0.02f, 0.3f, Mathf.Pow(1f - t, 1.6f));
                c.TintOver(x - half, y, x + half, y + 1, tone, strength);
            }
        }

        // ---------- props ----------

        private static Canvas Pillar(bool broken)
        {
            var c = new Canvas(130, broken ? 300 : 520);
            var rng = new System.Random(broken ? 611 : 610);
            var top = c.Height - (broken ? 0 : 46);
            c.Rect(24, 0, 106, top, Stone);
            Courses(c, rng, 26, 30, StoneDark, StoneLit);
            // Base and capital: without them a pillar is just a rectangle.
            c.Rect(8, 0, 122, 30, StoneLit);
            c.Rect(16, 30, 114, 44, Stone);
            if (!broken)
            {
                c.Rect(10, top, 120, c.Height, StoneLit);
                c.Rect(20, top - 14, 110, top, Stone);
                c.Rect(30, top + 16, 100, top + 22, FadedGold);
            }
            else
            {
                // A clean horizontal break would read as a cut; bite chunks out of the top.
                for (var i = 0; i < 12; i++)
                {
                    c.Ellipse(new Vector2(rng.Next(20, 110), top - rng.Next(0, 26)),
                        rng.Next(8, 20), rng.Next(6, 16), Clear);
                }
            }

            Speckle(c, rng, 300, StoneDark, 0, c.Height);
            return c;
        }

        private static Canvas Arch()
        {
            var c = new Canvas(420, 300);
            var rng = new System.Random(620);
            c.Rect(0, 0, 80, 210, Stone);
            c.Rect(340, 0, 420, 210, Stone);
            Vault(c, new Vector2(210, 210), 170f, 86f, Stone);
            c.Rect(0, 200, 420, 216, StoneLit);
            Courses(c, rng, 40, 26, StoneDark, StoneLit);
            Speckle(c, rng, 320, StoneDark, 0, c.Height);
            return c;
        }

        /// <summary>
        /// A fallen knight statue. Deliberately squarer and more frontal than any enemy: the brief
        /// is explicit that a statue must never be mistaken for something that will attack.
        /// </summary>
        private static Canvas Statue(bool broken)
        {
            var c = new Canvas(160, 280);
            var rng = new System.Random(broken ? 631 : 630);
            c.Rect(20, 0, 140, 34, StoneLit);
            c.Rect(30, 34, 130, 48, Stone);
            var top = broken ? 170 : 250;
            c.Rect(52, 48, 108, top, Stone);
            c.Rect(36, 120, 124, 175, Stone);
            // A grounded sword: the pose reads as a memorial, not as a guard on duty.
            c.Rect(74, 40, 86, 150, ColdGrey);
            c.Rect(56, 142, 104, 152, ColdGrey);
            if (!broken)
            {
                c.Ellipse(new Vector2(80, top + 16), 26f, 30f, Stone);
                c.Rect(62, top + 34, 98, top + 44, FadedGold);
            }
            else
            {
                for (var i = 0; i < 10; i++)
                {
                    c.Ellipse(new Vector2(rng.Next(46, 116), top - rng.Next(0, 30)),
                        rng.Next(8, 18), rng.Next(6, 14), Clear);
                }

                c.Ellipse(new Vector2(130, 20), 24f, 16f, Stone);
            }

            Courses(c, rng, 30, 40, StoneDark, StoneLit);
            Speckle(c, rng, 220, StoneDark, 0, c.Height);
            return c;
        }

        private static Canvas Sarcophagus(bool broken)
        {
            var c = new Canvas(300, 130);
            var rng = new System.Random(broken ? 641 : 640);
            c.Rect(10, 0, 290, 92, Stone);
            c.Rect(0, 92, 300, 118, StoneLit);
            c.Rect(20, 118, 280, 128, Stone);
            Courses(c, rng, 48, 30, StoneDark, StoneLit);
            Crest(c, new Vector2(150, 50), 24f, FadedGold, Stone);
            if (broken)
            {
                c.Rect(150, 92, 300, 118, Clear);
                c.Rect(150, 60, 290, 92, Charcoal);
                for (var i = 0; i < 8; i++)
                {
                    c.Ellipse(new Vector2(rng.Next(140, 290), rng.Next(80, 120)),
                        rng.Next(6, 16), rng.Next(5, 12), Clear);
                }
            }

            Speckle(c, rng, 260, StoneDark, 0, c.Height);
            return c;
        }

        private static Canvas Banner()
        {
            var c = new Canvas(100, 280);
            var rng = new System.Random(650);
            c.Rect(10, 258, 90, 268, ColdGrey);
            c.Rect(18, 40, 82, 262, Blend(Carmine, Stone, 0.25f));
            Crest(c, new Vector2(50, 180), 20f, FadedGold, Blend(Carmine, Stone, 0.25f));
            // Torn hem: an old banner does not end in a straight line.
            for (var i = 0; i < 14; i++)
            {
                c.Ellipse(new Vector2(rng.Next(18, 82), 40 + rng.Next(0, 26)),
                    rng.Next(5, 13), rng.Next(6, 16), Clear);
            }

            Speckle(c, rng, 120, Charcoal, 40, 262);
            return c;
        }

        private static Canvas Chain()
        {
            var c = new Canvas(24, 300);
            for (var y = 4; y < 300; y += 16)
            {
                c.Ellipse(new Vector2(12, y), 7f, 8f, ColdGrey);
                c.Ellipse(new Vector2(12, y), 3.5f, 4.5f, Clear);
            }

            return c;
        }

        private static Canvas Candles()
        {
            var c = new Canvas(120, 90);
            var rng = new System.Random(660);
            var heights = new[] { 44, 62, 34, 52 };
            for (var i = 0; i < heights.Length; i++)
            {
                var x = 20 + i * 26;
                c.Rect(x - 7, 0, x + 7, heights[i], new Color32(0xC9, 0xBD, 0xA0, 0xFF));
                c.Rect(x - 9, 0, x + 9, 8, Blend(Stone, Charcoal, 0.3f));
                c.Ellipse(new Vector2(x, heights[i] + 8), 4f, 8f, WarmFlame);
                c.Ellipse(new Vector2(x, heights[i] + 6), 2f, 4f, WarmPale);
            }

            Speckle(c, rng, 40, Charcoal, 0, 20);
            return c;
        }

        private static Canvas Rubble()
        {
            // A heap of broken stones, largest at the bottom. Square chips here read as a
            // rendering fault, so every piece is a rounded lump with a lit top.
            var c = new Canvas(220, 80);
            var rng = new System.Random(670);
            for (var i = 0; i < 14; i++)
            {
                var t = i / 13f;
                var x = 30f + (float)rng.NextDouble() * 160f;
                var r = Mathf.Lerp(22f, 8f, t) * (0.8f + (float)rng.NextDouble() * 0.4f);
                var y = r * 0.55f + t * 26f;
                c.Ellipse(new Vector2(x, y), r, r * 0.7f, StoneDark);
                c.Ellipse(new Vector2(x - r * 0.15f, y + r * 0.2f), r * 0.75f, r * 0.45f, Stone);
                c.Ellipse(new Vector2(x - r * 0.25f, y + r * 0.38f), r * 0.4f, r * 0.18f, StoneLit);
            }

            return c;
        }

        private static Canvas Roots()
        {
            var c = new Canvas(220, 280);
            var rng = new System.Random(680);
            for (var i = 0; i < 7; i++)
            {
                var x = rng.Next(20, 200);
                Vein(c, rng, x, rng.Next(200, 278), 180, Blend(Brown, Charcoal, 0.35f));
            }

            for (var i = 0; i < 4; i++)
            {
                Vein(c, rng, rng.Next(20, 200), rng.Next(40, 200), 90, Moss);
            }

            return c;
        }

        private static Canvas Bones()
        {
            var c = new Canvas(140, 46);
            var bone = new Color32(0xB8, 0xB0, 0x9A, 0xFF);
            c.Thick(new Vector2(20, 12), new Vector2(72, 18), 8f, 8f, bone);
            c.Thick(new Vector2(60, 28), new Vector2(112, 14), 6f, 6f, bone);
            c.Ellipse(new Vector2(112, 22), 14f, 13f, bone);
            c.Ellipse(new Vector2(118, 22), 4f, 5f, Charcoal);
            c.Ellipse(new Vector2(106, 22), 4f, 5f, Charcoal);
            return c;
        }

        /// <summary>A sword and shield set into the floor: a grave that names a soldier.</summary>
        private static Canvas Memorial()
        {
            var c = new Canvas(130, 190);
            var rng = new System.Random(690);
            c.Rect(24, 0, 106, 26, StoneLit);
            c.Rect(60, 20, 72, 176, ColdGrey);
            c.Rect(40, 150, 92, 160, ColdGrey);
            ShieldInto(c, new Vector2(46, 78), 54f, 76f, ColdGrey, FadedGold);
            Speckle(c, rng, 90, StoneDark, 0, 30);
            return c;
        }

        private static Canvas GraveMarker()
        {
            var c = new Canvas(90, 140);
            var rng = new System.Random(700);
            c.Rect(14, 0, 76, 110, Stone);
            c.Ellipse(new Vector2(45, 110), 31f, 26f, Stone);
            c.Rect(30, 40, 60, 46, FadedGold);
            c.Rect(42, 30, 48, 70, FadedGold);
            Courses(c, rng, 30, 26, StoneDark, StoneLit);
            Speckle(c, rng, 90, StoneDark, 0, 140);
            return c;
        }

        private static Canvas Urn()
        {
            var c = new Canvas(80, 110);
            c.Rect(24, 0, 56, 12, Stone);
            c.Ellipse(new Vector2(40, 48), 28f, 38f, Stone);
            c.Rect(28, 82, 52, 98, Stone);
            c.Rect(22, 96, 58, 106, StoneLit);
            c.Rect(24, 44, 56, 50, FadedGold);
            return c;
        }

        // ---------- interactive ----------

        /// <summary>
        /// The altar: a small stone pedestal with an inset blade and the royal crest. Compact on
        /// purpose — the Visual Bible asks it not to compete with the architecture, only to be
        /// unmistakable.
        /// </summary>
        private static Canvas AltarBase()
        {
            var c = new Canvas(180, 160);
            var rng = new System.Random(710);
            c.Rect(14, 0, 166, 26, StoneLit);
            c.Rect(30, 26, 150, 44, Stone);
            c.Rect(42, 44, 138, 108, Stone);
            c.Rect(26, 108, 154, 130, StoneLit);
            Courses(c, rng, 40, 24, StoneDark, StoneLit);
            Crest(c, new Vector2(90, 76), 22f, FadedGold, Stone);
            // The inset blade the flame rises from.
            c.Rect(84, 118, 96, 156, ColdGrey);
            c.Rect(68, 126, 112, 134, ColdGrey);
            Speckle(c, rng, 140, StoneDark, 0, c.Height);
            return c;
        }

        private static Canvas AltarFlame(bool lit)
        {
            var c = new Canvas(90, 130);
            if (!lit)
            {
                // Cold: a dead ember and nothing else. It must be obvious this is not yet safety.
                c.Ellipse(new Vector2(45, 18), 16f, 12f, Blend(Charcoal, ColdGrey, 0.3f));
                c.Ellipse(new Vector2(45, 16), 8f, 6f, Charcoal);
                return c;
            }

            for (var y = 0; y < 110; y++)
            {
                var t = y / 110f;
                var half = Mathf.Lerp(26f, 3f, t * t);
                var tone = Blend(WarmFlame, WarmPale, t);
                c.Rect(45 - half, y, 45 + half, y + 1, tone);
            }

            c.Ellipse(new Vector2(45, 34), 12f, 26f, WarmPale);
            return c;
        }

        /// <summary>A soft radial glow, used as fake light; the project has no 2D light rig.</summary>
        private static Canvas Glow(Color32 tone)
        {
            var c = new Canvas(256, 256);
            var centre = new Vector2(128, 128);
            for (var y = 0; y < 256; y++)
            {
                for (var x = 0; x < 256; x++)
                {
                    var d = Vector2.Distance(new Vector2(x, y), centre) / 128f;
                    if (d >= 1f)
                    {
                        continue;
                    }

                    var falloff = (1f - d) * (1f - d);
                    c.Set(x, y, new Color32(tone.r, tone.g, tone.b,
                        (byte)Mathf.RoundToInt(falloff * 200f)));
                }
            }

            return c;
        }

        /// <summary>
        /// The arena gate: heavy stone, iron bands, ancient seals and the royal crest. It has to
        /// read as the most important door in the level before the player is anywhere near it.
        /// </summary>
        private static Canvas BossGate()
        {
            var c = new Canvas(460, 700);
            var rng = new System.Random(720);
            // Solid masonry first, then the doorway carved out of it. Building it the other way
            // round leaves the arch as a hole in the wall rather than as stone above the door.
            c.Rect(0, 0, 460, 700, Stone);
            c.Rect(70, 0, 390, 560, Blend(Charcoal, ColdGrey, 0.25f));
            VaultFill(c, new Vector2(230, 560), 160f, 110f, Blend(Charcoal, ColdGrey, 0.25f));
            for (var y = 70; y < 540; y += 120)
            {
                c.Rect(78, y, 382, y + 22, ColdGrey);
                c.Ellipse(new Vector2(100, y + 11), 7f, 7f, Blend(ColdGrey, Charcoal, 0.4f));
                c.Ellipse(new Vector2(360, y + 11), 7f, 7f, Blend(ColdGrey, Charcoal, 0.4f));
            }

            c.Rect(224, 0, 236, 560, Stone);
            Crest(c, new Vector2(230, 330), 62f, FadedGold, Blend(Charcoal, ColdGrey, 0.25f));
            // Ancient seals, and the corruption working at them.
            foreach (var y in new[] { 180, 470 })
            {
                c.Ellipse(new Vector2(230, y), 34f, 34f, Blend(FadedGold, Charcoal, 0.45f));
                c.Ellipse(new Vector2(230, y), 22f, 22f, Blend(Charcoal, ColdGrey, 0.25f));
            }

            for (var i = 0; i < 9; i++)
            {
                Vein(c, rng, rng.Next(80, 380), rng.Next(20, 560), 70, Corruption);
            }

            Courses(c, rng, 60, 40, StoneDark, StoneLit);
            Speckle(c, rng, 500, StoneDark, 0, c.Height);
            return c;
        }

        /// <summary>
        /// The arena seal. A dense, unmistakably solid surface: a translucent shimmer would invite
        /// the player to try a Shadow Dash through it, and that is a lesson best not taught.
        /// </summary>
        private static Canvas Barrier()
        {
            var c = new Canvas(120, 700);
            var rng = new System.Random(730);
            c.Rect(0, 0, 120, 700, Blend(Corruption, Charcoal, 0.25f));
            c.Rect(8, 0, 112, 700, Blend(Corruption, Carmine, 0.35f));
            for (var y = 0; y < 700; y += 58)
            {
                c.Rect(16, y + 8, 104, y + 14, Blend(FadedGold, Corruption, 0.4f));
                Crest(c, new Vector2(60, y + 34), 16f, Blend(FadedGold, Corruption, 0.3f),
                    Blend(Corruption, Carmine, 0.35f));
            }

            c.Rect(0, 0, 10, 700, FadedGold);
            c.Rect(110, 0, 120, 700, FadedGold);
            Speckle(c, rng, 200, Corruption, 0, 700);
            return c;
        }

        // ---------- arena ----------

        /// <summary>
        /// The monumental tomb behind the Guardian: the focal point of the whole slice. Symmetry
        /// and scale do the work — this is the one place in the level that was built to be looked
        /// at rather than fought through.
        /// </summary>
        private static Canvas ArenaBackdrop()
        {
            var c = new Canvas(1600, 900);
            var rng = new System.Random(740);
            var back = Blend(Charcoal, ColdBlue, 0.3f);
            var form = Blend(back, ColdGrey, 0.3f);
            c.Rect(0, 0, 1600, 900, back);

            // A great vault, flanked by colonnades that shrink towards the edges.
            for (var i = 0; i < 4; i++)
            {
                var inset = i * 64;
                c.Rect(300 + inset, 0, 1300 - inset, 640 - i * 40, Blend(form, back, i * 0.18f));
            }

            Vault(c, new Vector2(800, 600), 400f, 220f, form);
            c.Rect(240, 0, 340, 700, form);
            c.Rect(1260, 0, 1360, 700, form);
            for (var x = 120; x < 1600; x += 380)
            {
                c.Rect(x, 0, x + 60, 460, Blend(form, back, 0.35f));
            }

            // The royal sarcophagus on its dais, dead centre. It is the focal point of the whole
            // slice, so it is allowed more contrast than anything else this far back — but still
            // less than the Guardian who will be standing in front of it.
            var tomb = Blend(form, StoneLit, 0.5f);
            c.Rect(620, 40, 980, 90, Blend(tomb, StoneLit, 0.3f));
            c.Rect(660, 90, 940, 230, tomb);
            c.Rect(640, 230, 960, 270, Blend(tomb, StoneLit, 0.35f));
            Crest(c, new Vector2(800, 160), 54f, Blend(FadedGold, form, 0.1f), tomb);

            // Flanking knight statues, and banners the centuries have not taken down.
            StatueInto(c, 520, 40, 150, 300, Blend(form, StoneLit, 0.22f), false);
            StatueInto(c, 1080, 40, 150, 300, Blend(form, StoneLit, 0.22f), false);
            BannerInto(c, 430, 640, 70, 300, Blend(Carmine, back, 0.4f));
            BannerInto(c, 1170, 640, 70, 300, Blend(ColdBlue, back, 0.35f));

            Shaft(c, 800, 900, Blend(back, ColdBlue, 0.6f));
            for (var i = 0; i < 16; i++)
            {
                Vein(c, rng, rng.Next(300, 1300), rng.Next(40, 620), 120,
                    Blend(Corruption, back, 0.35f));
            }

            Speckle(c, rng, 900, Blend(back, Charcoal, 0.4f), 0, 900);
            return c;
        }

        /// <summary>
        /// The crest inlaid in the arena floor. It marks the centre, which is exactly what a
        /// player needs to read during a charge or a slam.
        /// </summary>
        private static Canvas ArenaCrest()
        {
            var c = new Canvas(700, 240);
            var centre = new Vector2(350, 120);
            for (var ring = 0; ring < 3; ring++)
            {
                var rx = 330f - ring * 60f;
                var ry = 112f - ring * 20f;
                c.Ellipse(centre, rx, ry, Blend(FadedGold, StoneDark, 0.55f + ring * 0.12f));
                c.Ellipse(centre, rx - 8f, ry - 4f, Clear);
            }

            Crest(c, centre, 56f, Blend(FadedGold, StoneDark, 0.4f), Clear);
            return c;
        }

        // ---------- decals ----------

        private static Canvas Crack()
        {
            var c = new Canvas(160, 160);
            var rng = new System.Random(750);
            Vein(c, rng, 80, 80, 110, Charcoal);
            Vein(c, rng, 80, 80, 90, Charcoal);
            return c;
        }

        private static Canvas MossPatch()
        {
            var c = new Canvas(180, 120);
            var rng = new System.Random(760);
            for (var i = 0; i < 40; i++)
            {
                c.Ellipse(new Vector2(rng.Next(10, 170), rng.Next(10, 110)),
                    rng.Next(6, 20), rng.Next(4, 14), Moss);
            }

            return c;
        }

        private static Canvas Rune()
        {
            var c = new Canvas(100, 100);
            c.Ellipse(new Vector2(50, 50), 42f, 42f, Blend(FadedGold, Charcoal, 0.4f));
            c.Ellipse(new Vector2(50, 50), 34f, 34f, Clear);
            c.Rect(46, 16, 54, 84, Blend(FadedGold, Charcoal, 0.4f));
            c.Rect(22, 46, 78, 54, Blend(FadedGold, Charcoal, 0.4f));
            return c;
        }

        /// <summary>
        /// The pit's depth: darkness that gets darker, with a broken lip at the top. A kill zone
        /// the player cannot see is a cheap death, so this is drawn to be obvious.
        /// </summary>
        private static Canvas PitDepth()
        {
            var c = new Canvas(600, 400);
            var rng = new System.Random(770);
            for (var y = 0; y < 400; y++)
            {
                var t = y / 400f;
                var tone = Blend(Charcoal, Blend(Corruption, Charcoal, 0.4f), 1f - t);
                c.Rect(0, y, 600, y + 1,
                    new Color32(tone.r, tone.g, tone.b, (byte)Mathf.RoundToInt(255f * (1f - t * 0.25f))));
            }

            // Broken lip: stone gives way rather than ending in a clean line.
            for (var i = 0; i < 26; i++)
            {
                var x = rng.Next(0, 600);
                c.Rect(x, 380 - rng.Next(0, 26), x + rng.Next(10, 30), 400, Stone);
            }

            Speckle(c, rng, 300, Blend(Corruption, Charcoal, 0.5f), 0, 340);
            return c;
        }

        // ---------- shared drawing ----------

        /// <summary>
        /// Mortar lines. Rows are offset so courses never line up into a grid, and every stroke
        /// lands only on stone that is already there — otherwise a prop's transparent surround
        /// fills with a floating lattice.
        /// </summary>
        private static void Courses(Canvas c, System.Random rng, int blockWidth, int blockHeight,
            Color32 mortar, Color32 highlight)
        {
            for (var y = 0; y < c.Height; y += blockHeight)
            {
                c.RectOver(0, y, c.Width, y + 2, mortar);
                var offset = (y / blockHeight) % 2 == 0 ? 0 : blockWidth / 2;
                for (var x = offset; x < c.Width; x += blockWidth)
                {
                    c.RectOver(x, y, x + 2, y + blockHeight, mortar);
                    if (rng.Next(0, 5) == 0)
                    {
                        c.RectOver(x + 3, y + blockHeight - 4, x + blockWidth - 2,
                            y + blockHeight - 2, highlight);
                    }
                }
            }
        }

        private static void Speckle(Canvas c, System.Random rng, int count, Color32 tone,
            int minY, int maxY)
        {
            for (var i = 0; i < count; i++)
            {
                var x = rng.Next(0, c.Width);
                var y = rng.Next(minY, Mathf.Max(minY + 1, maxY));
                c.RectOver(x, y, x + rng.Next(1, 4), y + rng.Next(1, 3), tone);
            }
        }

        /// <summary>A wandering line: cracks, roots and corruption veins are all this shape.</summary>
        private static void Vein(Canvas c, System.Random rng, float x, float y, int length,
            Color32 tone)
        {
            var angle = (float)(rng.NextDouble() * Math.PI * 2.0);
            var width = 3.5f;
            for (var i = 0; i < length; i++)
            {
                angle += (float)(rng.NextDouble() - 0.5) * 0.35f;
                x += Mathf.Cos(angle);
                y += Mathf.Sin(angle);
                c.Ellipse(new Vector2(x, y), width, width, tone);
                width = Mathf.Max(1f, width - 0.02f);
            }
        }

        /// <summary>Fills the inside of an arch: the opening, rather than the masonry around it.</summary>
        private static void VaultFill(Canvas c, Vector2 springLine, float halfWidth, float rise,
            Color32 tone)
        {
            for (var y = 0; y <= rise; y++)
            {
                var t = y / rise;
                var half = halfWidth * Mathf.Sqrt(Mathf.Max(0f, 1f - t * t));
                c.Rect(springLine.x - half, springLine.y + y, springLine.x + half,
                    springLine.y + y + 1, tone);
            }
        }

        /// <summary>A half-round vault, used for arches, gates and the arena ceiling.</summary>
        private static void Vault(Canvas c, Vector2 springLine, float halfWidth, float rise,
            Color32 tone)
        {
            for (var y = 0; y <= rise; y++)
            {
                var t = y / rise;
                var half = halfWidth * Mathf.Sqrt(Mathf.Max(0f, 1f - t * t));
                c.Rect(springLine.x - halfWidth, springLine.y + y, springLine.x - half,
                    springLine.y + y + 1, tone);
                c.Rect(springLine.x + half, springLine.y + y, springLine.x + halfWidth,
                    springLine.y + y + 1, tone);
            }
        }

        private static void ArchInto(Canvas c, float centreX, float baseY, float width,
            float height, Color32 tone, Color32 hollow)
        {
            var half = width * 0.5f;
            c.Rect(centreX - half, baseY, centreX + half, baseY + height, tone);
            c.Rect(centreX - half + 26, baseY, centreX + half - 26, baseY + height - 90, hollow);
            Vault(c, new Vector2(centreX, baseY + height - 90), half - 26f, 70f, hollow);
        }

        private static void BannerInto(Canvas c, float x, float topY, float width, float height,
            Color32 tone)
        {
            c.Rect(x - width * 0.5f, topY - height, x + width * 0.5f, topY, tone);
            c.Rect(x - width * 0.6f, topY - 6, x + width * 0.6f, topY, ColdGrey);
        }

        private static void StatueInto(Canvas c, float x, float baseY, float width, float height,
            Color32 tone, bool broken)
        {
            var half = width * 0.5f;
            c.Rect(x - half, baseY, x + half, baseY + height * 0.12f, tone);
            var top = broken ? height * 0.62f : height * 0.82f;
            c.Rect(x - half * 0.45f, baseY + height * 0.12f, x + half * 0.45f, baseY + top, tone);
            c.Rect(x - half * 0.8f, baseY + height * 0.42f, x + half * 0.8f,
                baseY + height * 0.58f, tone);
            if (!broken)
            {
                c.Ellipse(new Vector2(x, baseY + top + height * 0.08f), half * 0.4f,
                    height * 0.09f, tone);
            }
        }

        private static void ShieldInto(Canvas c, Vector2 anchor, float width, float height,
            Color32 tone, Color32 trim)
        {
            var half = width * 0.5f;
            c.Quad(new Vector2(anchor.x - half, anchor.y + height * 0.5f),
                new Vector2(anchor.x + half, anchor.y + height * 0.5f),
                new Vector2(anchor.x + half * 0.9f, anchor.y - height * 0.1f),
                new Vector2(anchor.x - half * 0.9f, anchor.y - height * 0.1f), tone);
            c.Triangle(new Vector2(anchor.x - half * 0.9f, anchor.y - height * 0.1f),
                new Vector2(anchor.x + half * 0.9f, anchor.y - height * 0.1f),
                new Vector2(anchor.x, anchor.y - height * 0.5f), tone);
            c.Ellipse(anchor + new Vector2(0f, height * 0.12f), half * 0.34f, half * 0.34f, trim);
        }

        /// <summary>
        /// The kingdom's crest. One shape, reused on trim, banners, sarcophagi, the gate, the altar
        /// and the arena floor — it is what makes the whole tomb feel like one place.
        /// </summary>
        private static void Crest(Canvas c, Vector2 centre, float radius, Color32 tone,
            Color32 hollow)
        {
            ShieldInto(c, centre, radius * 1.7f, radius * 2.1f, tone, tone);
            if (hollow.a > 0)
            {
                ShieldInto(c, centre, radius * 1.2f, radius * 1.5f, hollow, hollow);
            }

            c.Rect(centre.x - radius * 0.12f, centre.y - radius * 0.55f,
                centre.x + radius * 0.12f, centre.y + radius * 0.6f, tone);
            c.Rect(centre.x - radius * 0.5f, centre.y + radius * 0.1f,
                centre.x + radius * 0.5f, centre.y + radius * 0.3f, tone);
        }

        private static Color32 Blend(Color32 a, Color32 b, float t) => Color32.Lerp(a, b, t);
    }
}
