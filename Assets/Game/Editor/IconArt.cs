using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The game's icon set, drawn to one grammar: 96 px, a 7 px main stroke, a 4 px secondary one,
    /// the same inset from the edge, a dark under-stroke so every icon reads on any frame, and a
    /// colour per identity — Warrior steel and crimson, Hunter gold and muted green, Mage blue and
    /// violet, Shadow violet and crimson; elements in their Visual Bible colours.
    ///
    /// Shape carries the meaning as well as colour: fire rises, frost is a six-armed star, poison
    /// is a drop with a skull-like hollow, bleed a falling drop, so none depends on hue alone.
    ///
    /// Procedural; the hand-drawn set in ART_ASSET_LIST.md keeps these names and sizes.
    /// </summary>
    public static class IconArt
    {
        public const string Root = "Assets/Game/Art/UI/Icons";
        private const int Size = 96;

        private static readonly Color32 Steel = new Color32(185, 192, 201, 255);
        private static readonly Color32 SteelDark = new Color32(110, 116, 126, 255);
        private static readonly Color32 Gold = new Color32(201, 161, 74, 255);
        private static readonly Color32 GoldBright = new Color32(232, 205, 138, 255);
        private static readonly Color32 Leather = new Color32(92, 70, 50, 255);
        private static readonly Color32 Crimson = new Color32(168, 38, 52, 255);
        private static readonly Color32 HuntGreen = new Color32(120, 150, 92, 255);
        private static readonly Color32 MageBlue = new Color32(107, 143, 255, 255);
        private static readonly Color32 MageViolet = new Color32(176, 125, 255, 255);
        private static readonly Color32 ShadowViolet = new Color32(155, 77, 255, 255);
        private static readonly Color32 ShadowCore = new Color32(26, 15, 38, 255);
        private static readonly Color32 Fire = new Color32(255, 140, 51, 255);
        private static readonly Color32 FireCore = new Color32(255, 217, 140, 255);
        private static readonly Color32 Frost = new Color32(143, 214, 255, 255);
        private static readonly Color32 Poison = new Color32(127, 168, 60, 255);
        private static readonly Color32 Bleed = new Color32(142, 27, 40, 255);
        private static readonly Color32 BleedLight = new Color32(217, 105, 79, 255);
        private static readonly Color32 Under = new Color32(8, 7, 6, 200);

        public static Dictionary<string, Sprite> Generate()
        {
            PlaceholderImport.EnsureFolder(Root);
            var icons = new Dictionary<string, Sprite>();
            Write(icons, "icon_sword", Sword(false));
            Write(icons, "icon_greatsword", Sword(true));
            Write(icons, "icon_warrior", Warrior());
            Write(icons, "icon_hunter", Hunter());
            Write(icons, "icon_mage", Mage());
            Write(icons, "icon_shadow", Shadow());
            Write(icons, "icon_fire", FireIcon());
            Write(icons, "icon_frost", FrostIcon());
            Write(icons, "icon_arcane", Arcane());
            Write(icons, "icon_poison", PoisonIcon());
            Write(icons, "icon_bleed", BleedIcon());
            Write(icons, "icon_checkpoint", Checkpoint());
            Write(icons, "icon_currency", Currency());
            return icons;
        }

        /// <summary>
        /// The HUD's furniture: iron frames with a gold hairline and bevel for bars and slots,
        /// a sheen that makes a fill read as lit liquid rather than a flat rectangle, an ornament
        /// rule for titles, and the screen vignette that darkens the corners of play.
        /// </summary>
        public static Dictionary<string, Sprite> GenerateFrames()
        {
            PlaceholderImport.EnsureFolder(FrameRoot);
            var frames = new Dictionary<string, Sprite>();
            Write(frames, "ui_bar_frame", Frame(96, 32, 3, true), FrameRoot);
            Write(frames, "ui_bar_frame_thin", Frame(48, 16, 2, false), FrameRoot);
            Write(frames, "ui_slot_frame", SlotFrame(), FrameRoot);
            Write(frames, "ui_sheen", Sheen(), FrameRoot);
            Write(frames, "ui_vignette", Vignette(), FrameRoot);
            Write(frames, "ui_ornament", Ornament(), FrameRoot);
            return frames;
        }

        public const string FrameRoot = "Assets/Game/Art/UI/Frames";

        private static void Write(Dictionary<string, Sprite> into, string name, Canvas canvas)
        {
            Write(into, name, canvas, Root);
        }

        private static void Write(Dictionary<string, Sprite> into, string name, Canvas canvas,
            string folder)
        {
            var path = $"{folder}/{name}.png";
            PlaceholderImport.WriteIfChanged(path, canvas.EncodePng());
            PlaceholderImport.ConfigureImporter(path, new Vector2(0.5f, 0.5f));
            into[name] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>A stroke with the dark under-stroke every icon shares.</summary>
        private static void Line(Canvas c, Vector2 a, Vector2 b, float width, Color32 color)
        {
            c.Thick(a, b, width + 5, width + 5, Under);
            c.Thick(a, b, width, width, color);
        }

        private static Canvas Sword(bool great)
        {
            var c = new Canvas(Size, Size);
            var tip = new Vector2(82, 82);
            var guard = great ? new Vector2(30, 30) : new Vector2(34, 34);
            Line(c, guard, tip, great ? 13 : 9, Steel);
            c.Thick(guard + new Vector2(3, 3), tip - new Vector2(4, 4), 2, 1, new Color32(235, 240, 245, 255));
            Line(c, guard + new Vector2(-14, 14), guard + new Vector2(14, -14), great ? 8 : 7, Gold);
            Line(c, guard, guard + new Vector2(-18, -18), great ? 8 : 7, Leather);
            c.Ellipse(guard + new Vector2(-21, -21), 6, 6, Gold);
            return c;
        }

        private static Canvas Warrior()
        {
            // Two crossed blades over a round shield boss: the discipline of the royal guard.
            var c = new Canvas(Size, Size);
            c.Ellipse(new Vector2(48, 48), 30, 30, Under);
            c.Ellipse(new Vector2(48, 48), 26, 26, new Color32(70, 20, 28, 255));
            c.Ellipse(new Vector2(48, 48), 26, 26, Crimson);
            c.Ellipse(new Vector2(48, 48), 18, 18, new Color32(70, 20, 28, 255));
            Line(c, new Vector2(18, 18), new Vector2(78, 78), 6, Steel);
            Line(c, new Vector2(78, 18), new Vector2(18, 78), 6, Steel);
            c.Ellipse(new Vector2(48, 48), 7, 7, Steel);
            return c;
        }

        private static Canvas Hunter()
        {
            // An arrow with long fletching across a thin gold bow arc.
            var c = new Canvas(Size, Size);
            for (var i = 0; i <= 40; i++)
            {
                var a = Mathf.Lerp(-1.1f, 1.1f, i / 40f);
                var p = new Vector2(34 + Mathf.Cos(a) * 30, 48 + Mathf.Sin(a) * 34);
                c.Ellipse(p, 4.5f, 4.5f, Under);
                c.Ellipse(p, 3f, 3f, Gold);
            }

            Line(c, new Vector2(14, 48), new Vector2(80, 48), 5, SteelDark);
            c.Triangle(new Vector2(90, 48), new Vector2(76, 58), new Vector2(76, 38), Steel);
            Line(c, new Vector2(14, 48), new Vector2(24, 58), 4, HuntGreen);
            Line(c, new Vector2(14, 48), new Vector2(24, 38), 4, HuntGreen);
            return c;
        }

        private static Canvas Mage()
        {
            // A clean rune circle — the geometry of learned magic.
            var c = new Canvas(Size, Size);
            Ring(c, new Vector2(48, 48), 32, 6, MageBlue);
            Ring(c, new Vector2(48, 48), 18, 4, MageViolet);
            for (var i = 0; i < 4; i++)
            {
                var a = i * Mathf.PI / 2f + Mathf.PI / 4f;
                var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Line(c, new Vector2(48, 48) + d * 20, new Vector2(48, 48) + d * 36, 4, MageBlue);
            }

            c.Ellipse(new Vector2(48, 48), 6, 6, MageViolet);
            return c;
        }

        private static Canvas Shadow()
        {
            // A crescent of black light with a violet rim and a crimson eye.
            var c = new Canvas(Size, Size);
            c.Ellipse(new Vector2(48, 48), 34, 34, ShadowViolet);
            c.Ellipse(new Vector2(48, 48), 30, 30, ShadowCore);
            c.Ellipse(new Vector2(62, 56), 26, 26, new Color32(0, 0, 0, 0));
            c.Ellipse(new Vector2(40, 44), 6, 4, Crimson);
            return c;
        }

        private static Canvas FireIcon()
        {
            var c = new Canvas(Size, Size);
            Flame(c, new Vector2(48, 14), 26, 62, Under, 4);
            Flame(c, new Vector2(48, 14), 26, 62, Fire, 0);
            Flame(c, new Vector2(48, 16), 12, 34, FireCore, 0);
            return c;
        }

        private static Canvas FrostIcon()
        {
            var c = new Canvas(Size, Size);
            var centre = new Vector2(48, 48);
            for (var i = 0; i < 3; i++)
            {
                var a = i * Mathf.PI / 3f + Mathf.PI / 2f;
                var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 36;
                Line(c, centre - d, centre + d, 6, Frost);
                foreach (var end in new[] { centre + d * 0.7f, centre - d * 0.7f })
                {
                    var n = new Vector2(-d.y, d.x).normalized * 9;
                    var back = (end - centre).normalized * 9;
                    Line(c, end, end - back + n, 4, Frost);
                    Line(c, end, end - back - n, 4, Frost);
                }
            }

            return c;
        }

        private static Canvas Arcane()
        {
            var c = new Canvas(Size, Size);
            Ring(c, new Vector2(48, 48), 28, 6, MageBlue);
            c.Ellipse(new Vector2(48, 48), 11, 11, MageViolet);
            foreach (var d in new[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right })
            {
                Line(c, new Vector2(48, 48) + d * 34, new Vector2(48, 48) + d * 44, 5, MageBlue);
            }

            return c;
        }

        private static Canvas PoisonIcon()
        {
            var c = new Canvas(Size, Size);
            Drop(c, new Vector2(48, 44), 26, Under, 4);
            Drop(c, new Vector2(48, 44), 26, Poison, 0);
            c.Ellipse(new Vector2(40, 42), 5, 6, new Color32(40, 56, 20, 255));
            c.Ellipse(new Vector2(56, 42), 5, 6, new Color32(40, 56, 20, 255));
            c.Rect(44, 28, 52, 32, new Color32(40, 56, 20, 255));
            return c;
        }

        private static Canvas BleedIcon()
        {
            var c = new Canvas(Size, Size);
            Drop(c, new Vector2(48, 40), 26, Under, 4);
            Drop(c, new Vector2(48, 40), 26, Bleed, 0);
            Line(c, new Vector2(38, 34), new Vector2(46, 26), 4, BleedLight);
            return c;
        }

        private static Canvas Checkpoint()
        {
            // The crown-and-flame over an altar block — the kingdom's mark and the safe place.
            var c = new Canvas(Size, Size);
            c.Rect(22, 8, 74, 30, Under);
            c.Rect(26, 10, 70, 28, SteelDark);
            c.Rect(26, 26, 70, 28, Steel);
            Flame(c, new Vector2(48, 30), 16, 40, Fire, 0);
            Flame(c, new Vector2(48, 31), 8, 22, FireCore, 0);
            foreach (var x in new[] { 30f, 48f, 66f })
            {
                c.Triangle(new Vector2(x - 6, 76), new Vector2(x + 6, 76),
                    new Vector2(x, x == 48f ? 92 : 86), Gold);
            }

            c.Rect(24, 72, 72, 78, Gold);
            return c;
        }

        private static Canvas Currency()
        {
            var c = new Canvas(Size, Size);
            c.Ellipse(new Vector2(48, 48), 34, 34, Under);
            c.Ellipse(new Vector2(48, 48), 30, 30, new Color32(138, 116, 52, 255));
            c.Ellipse(new Vector2(48, 50), 26, 26, Gold);
            c.Ellipse(new Vector2(48, 50), 18, 18, new Color32(160, 128, 58, 255));
            foreach (var x in new[] { 40f, 48f, 56f })
            {
                c.Triangle(new Vector2(x - 4, 50), new Vector2(x + 4, 50),
                    new Vector2(x, x == 48f ? 64 : 60), GoldBright);
            }

            c.Rect(38, 46, 58, 50, GoldBright);
            return c;
        }

        // ---------- frames ----------

        private static readonly Color32 IronDark = new Color32(26, 21, 17, 255);
        private static readonly Color32 Iron = new Color32(58, 47, 37, 255);
        private static readonly Color32 IronLight = new Color32(107, 90, 66, 255);
        private static readonly Color32 GoldDim = new Color32(138, 116, 52, 255);

        /// <summary>
        /// An iron band around a transparent centre: black outside, a bevel lit from above, a gold
        /// hairline on the inner edge and a darker inner lip the fill sits under.
        /// </summary>
        private static Canvas Frame(int width, int height, int band, bool rivets)
        {
            var c = new Canvas(width, height);
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var d = Mathf.Min(Mathf.Min(x, width - 1 - x), Mathf.Min(y, height - 1 - y));
                    if (d == 0)
                    {
                        c.Set(x, y, new Color32(4, 3, 3, 255));
                    }
                    else if (d < band)
                    {
                        // Light from above: the upper band is brighter than the lower one.
                        var t = y / (float)(height - 1);
                        c.Set(x, y, Color32.Lerp(Iron, IronLight, t));
                    }
                    else if (d == band)
                    {
                        c.Set(x, y, y > height / 2 ? GoldDim : new Color32(96, 78, 38, 255));
                    }
                    else if (d == band + 1)
                    {
                        c.Set(x, y, new Color32(0, 0, 0, 120));
                    }
                }
            }

            if (rivets)
            {
                foreach (var p in new[] { new Vector2(5, 5), new Vector2(width - 5, 5),
                    new Vector2(5, height - 5), new Vector2(width - 5, height - 5) })
                {
                    c.Ellipse(p, 2.2f, 2.2f, Gold);
                }
            }

            return c;
        }

        /// <summary>A slot: a heavier frame with gold corner brackets and a sunken, shaded well.</summary>
        private static Canvas SlotFrame()
        {
            const int size = 64;
            var c = Frame(size, size, 4, false);
            for (var y = 6; y < size - 6; y++)
            {
                for (var x = 6; x < size - 6; x++)
                {
                    // Inner shade: darker toward the edges, so the icon sits in a recess.
                    var d = Mathf.Min(Mathf.Min(x - 6, size - 7 - x), Mathf.Min(y - 6, size - 7 - y));
                    var a = (byte)Mathf.Clamp(90 - d * 18, 0, 90);
                    if (a > 0)
                    {
                        c.Set(x, y, new Color32(0, 0, 0, a));
                    }
                }
            }

            foreach (var corner in new[] { new Vector2Int(0, 0), new Vector2Int(1, 0),
                new Vector2Int(0, 1), new Vector2Int(1, 1) })
            {
                var cx = corner.x == 0 ? 1 : size - 2;
                var cy = corner.y == 0 ? 1 : size - 2;
                var sx = corner.x == 0 ? 1 : -1;
                var sy = corner.y == 0 ? 1 : -1;
                for (var i = 0; i < 12; i++)
                {
                    for (var w = 0; w < 3; w++)
                    {
                        c.Set(cx + sx * i, cy + sy * w, Gold);
                        c.Set(cx + sx * w, cy + sy * i, Gold);
                    }
                }
            }

            return c;
        }

        private static Canvas Sheen()
        {
            var c = new Canvas(4, 64);
            for (var y = 0; y < 64; y++)
            {
                var t = y / 63f;
                Color32 tone;
                if (t > 0.72f)
                {
                    // Top highlight: the fill reads as lit from above.
                    tone = new Color32(255, 240, 220, (byte)Mathf.Lerp(0, 110, (t - 0.72f) / 0.28f));
                }
                else if (t < 0.4f)
                {
                    tone = new Color32(0, 0, 0, (byte)Mathf.Lerp(120, 0, t / 0.4f));
                }
                else
                {
                    tone = new Color32(0, 0, 0, 0);
                }

                for (var x = 0; x < 4; x++)
                {
                    c.Set(x, y, tone);
                }
            }

            return c;
        }

        private static Canvas Vignette()
        {
            const int w = 256;
            const int h = 144;
            var c = new Canvas(w, h);
            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    var dx = (x + 0.5f) / w * 2f - 1f;
                    var dy = (y + 0.5f) / h * 2f - 1f;
                    var d = Mathf.Sqrt(dx * dx * 0.9f + dy * dy * 1.1f);
                    var a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1.45f, d));
                    // The bottom sinks a little deeper, under the HUD's resources and slots.
                    a = Mathf.Clamp01(a + Mathf.Max(0f, -dy - 0.6f) * 0.35f);
                    c.Set(x, y, new Color32(3, 2, 4, (byte)(a * 215f)));
                }
            }

            return c;
        }

        /// <summary>A gold rule that fades at both ends, with a lozenge at the centre.</summary>
        private static Canvas Ornament()
        {
            const int w = 512;
            const int h = 24;
            var c = new Canvas(w, h);
            for (var x = 0; x < w; x++)
            {
                var edge = Mathf.Abs(x - w / 2f) / (w / 2f);
                var a = (byte)(Mathf.Clamp01(1f - edge * edge) * 255f);
                var tone = new Color32(Gold.r, Gold.g, Gold.b, a);
                c.Set(x, h / 2, tone);
                c.Set(x, h / 2 - 1, new Color32(0, 0, 0, (byte)(a / 2)));
            }

            var centre = new Vector2(w / 2f, h / 2f);
            c.Quad(centre + new Vector2(-16, 0), centre + new Vector2(0, 9),
                centre + new Vector2(16, 0), centre + new Vector2(0, -9), Gold);
            c.Quad(centre + new Vector2(-8, 0), centre + new Vector2(0, 4.5f),
                centre + new Vector2(8, 0), centre + new Vector2(0, -4.5f), new Color32(70, 20, 28, 255));
            foreach (var side in new[] { -1f, 1f })
            {
                c.Ellipse(centre + new Vector2(side * 30, 0), 3, 3, Gold);
                c.Ellipse(centre + new Vector2(side * 42, 0), 2, 2, GoldDim);
            }

            return c;
        }

        // ---------- shapes ----------

        private static void Ring(Canvas c, Vector2 centre, float radius, float thickness, Color32 tone)
        {
            for (var y = 0; y < c.Height; y++)
            {
                for (var x = 0; x < c.Width; x++)
                {
                    var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), centre);
                    if (Mathf.Abs(d - radius) <= thickness * 0.5f + 2.5f && c.Alpha(x, y) == 0)
                    {
                        c.Set(x, y, Under);
                    }
                }
            }

            for (var y = 0; y < c.Height; y++)
            {
                for (var x = 0; x < c.Width; x++)
                {
                    var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), centre);
                    if (Mathf.Abs(d - radius) <= thickness * 0.5f)
                    {
                        c.Set(x, y, tone);
                    }
                }
            }
        }

        /// <summary>A flame: a teardrop pointing up, with a slight lean.</summary>
        private static void Flame(Canvas c, Vector2 baseCentre, float halfWidth, float height,
            Color32 tone, float grow)
        {
            for (var i = 0; i <= 30; i++)
            {
                var t = i / 30f;
                var width = Mathf.Sin(Mathf.Lerp(0.2f, Mathf.PI, t)) * halfWidth * (1f - t * 0.5f);
                var p = baseCentre + new Vector2(Mathf.Sin(t * 2.4f) * 4f, t * height);
                c.Ellipse(p, width + grow, width * 0.6f + grow + 2f, tone);
            }
        }

        private static void Drop(Canvas c, Vector2 centre, float radius, Color32 tone, float grow)
        {
            c.Ellipse(centre, radius + grow, radius + grow, tone);
            c.Triangle(centre + new Vector2(-radius * 0.72f - grow, radius * 0.7f),
                centre + new Vector2(radius * 0.72f + grow, radius * 0.7f),
                centre + new Vector2(0, radius * 1.9f + grow), tone);
        }
    }
}
