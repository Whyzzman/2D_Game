using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The small shapes the game used to draw as stretched white squares: things on the ground,
    /// the warning over an enemy's head, arrows and bolts, poison clouds, the boss's shockwave —
    /// plus the soft pieces the lighting and atmosphere pass is built from.
    ///
    /// Each is 100 × 100 px, which at PPU 100 is exactly one unit, so every object that scaled the
    /// old square to its gameplay size keeps that scale and simply gains a real shape. They are
    /// drawn white or near-white wherever the game tints them (item colour, element colour,
    /// telegraph colour) so one sprite serves every tint.
    ///
    /// Still procedural — see ART_ASSET_LIST.md for the hand-drawn replacements.
    /// </summary>
    public static class ProductionSpriteArt
    {
        public const string Root = "Assets/Game/Art/Shapes";
        private const int Size = 100;

        public static Dictionary<string, Sprite> Generate()
        {
            PlaceholderImport.EnsureFolder(Root);
            var result = new Dictionary<string, Sprite>();
            Write(result, "shape_pickup_relic", Relic());
            Write(result, "shape_pickup_coins", Coins());
            Write(result, "shape_telegraph_sigil", Sigil());
            Write(result, "shape_projectile_bolt", Bolt());
            Write(result, "shape_projectile_arrow", Arrow());
            Write(result, "shape_projectile_orb", Orb());
            Write(result, "shape_hazard_mist", Mist(17));
            Write(result, "shape_shockwave", Shockwave());
            Write(result, "shape_soft_glow", Glow());
            Write(result, "shape_light_shaft", Shaft());
            Write(result, "shape_fog_band", FogBand(), new Vector2(0.5f, 0.5f), true);
            Write(result, "shape_depth_gradient", DepthGradient(), new Vector2(0.5f, 1f));
            return result;
        }

        private static void Write(Dictionary<string, Sprite> into, string name, Canvas canvas,
            Vector2? pivot = null, bool repeat = false)
        {
            var path = $"{Root}/{name}.png";
            PlaceholderImport.WriteIfChanged(path, canvas.EncodePng());
            PlaceholderImport.ConfigureImporter(path, pivot ?? new Vector2(0.5f, 0.5f), repeat);
            into[name] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ---------- shapes ----------

        /// <summary>A relic lying on the ground: a faceted gem in a small gold setting, glowing.</summary>
        private static Canvas Relic()
        {
            var c = new Canvas(Size, Size);
            RadialGlow(c, new Vector2(50, 50), 48, new Color32(255, 255, 255, 70));
            var gem = new Color32(235, 238, 245, 255);
            c.Quad(new Vector2(50, 84), new Vector2(72, 52), new Vector2(50, 16), new Vector2(28, 52), gem);
            // Facets: a darker lower half and a bright upper-left plane.
            c.Triangle(new Vector2(50, 16), new Vector2(72, 52), new Vector2(50, 46),
                new Color32(170, 176, 190, 255));
            c.Triangle(new Vector2(28, 52), new Vector2(50, 84), new Vector2(46, 56),
                new Color32(255, 255, 255, 255));
            c.Ellipse(new Vector2(43, 62), 4, 4, new Color32(255, 255, 255, 255));
            return c;
        }

        /// <summary>A small heap of old royal coins.</summary>
        private static Canvas Coins()
        {
            var c = new Canvas(Size, Size);
            RadialGlow(c, new Vector2(50, 40), 44, new Color32(255, 220, 140, 55));
            var gold = new Color32(236, 196, 96, 255);
            var shade = new Color32(150, 112, 40, 255);
            foreach (var (x, y) in new[] { (34, 26), (52, 24), (68, 28), (42, 38), (60, 40), (50, 52) })
            {
                c.Ellipse(new Vector2(x, y - 2), 13, 7, shade);
                c.Ellipse(new Vector2(x, y), 13, 7, gold);
                c.Ellipse(new Vector2(x - 3, y + 2), 5, 2, new Color32(255, 238, 180, 255));
            }

            return c;
        }

        /// <summary>
        /// The warning over an enemy's head: a four-pointed flare with a hollow centre. Tinted by
        /// the telegraph colour code (parry / block / dodge) and read by its shape as well.
        /// </summary>
        private static Canvas Sigil()
        {
            var c = new Canvas(Size, Size);
            RadialGlow(c, new Vector2(50, 50), 48, new Color32(255, 255, 255, 90));
            var white = new Color32(255, 255, 255, 255);
            c.Quad(new Vector2(50, 98), new Vector2(58, 50), new Vector2(50, 2), new Vector2(42, 50), white);
            c.Quad(new Vector2(2, 50), new Vector2(50, 58), new Vector2(98, 50), new Vector2(50, 42), white);
            c.Ellipse(new Vector2(50, 50), 16, 16, white);
            c.Ellipse(new Vector2(50, 50), 8, 8, new Color32(0, 0, 0, 0));
            return c;
        }

        /// <summary>A spell bolt: a hot core and a tapering tail, pointing right.</summary>
        private static Canvas Bolt()
        {
            var c = new Canvas(Size, Size);
            for (var i = 0; i < 40; i++)
            {
                var t = i / 39f;
                var x = Mathf.Lerp(8, 78, t);
                var r = Mathf.Lerp(4, 30, t * t);
                c.Ellipse(new Vector2(x, 50), r, r * 0.9f, new Color32(255, 255, 255, (byte)(40 + 120 * t)));
            }

            c.Ellipse(new Vector2(72, 50), 22, 22, new Color32(255, 255, 255, 220));
            c.Ellipse(new Vector2(74, 50), 12, 12, new Color32(255, 255, 255, 255));
            return c;
        }

        /// <summary>An arrow: dark shaft, iron head, pale fletching.</summary>
        private static Canvas Arrow()
        {
            var c = new Canvas(Size, Size);
            c.Thick(new Vector2(10, 50), new Vector2(80, 50), 9, 9, new Color32(120, 100, 76, 255));
            c.Triangle(new Vector2(98, 50), new Vector2(76, 70), new Vector2(76, 30), new Color32(210, 214, 222, 255));
            c.Quad(new Vector2(6, 62), new Vector2(26, 54), new Vector2(26, 46), new Vector2(6, 38),
                new Color32(232, 226, 210, 255));
            return c;
        }

        /// <summary>A thrown orb: soft edge, bright centre, faint swirl.</summary>
        private static Canvas Orb()
        {
            var c = new Canvas(Size, Size);
            RadialGlow(c, new Vector2(50, 50), 49, new Color32(255, 255, 255, 120));
            c.Ellipse(new Vector2(50, 50), 30, 30, new Color32(255, 255, 255, 230));
            c.Ellipse(new Vector2(42, 58), 10, 10, new Color32(255, 255, 255, 255));
            return c;
        }

        /// <summary>A low cloud of mist, soft on every side, denser at the bottom.</summary>
        private static Canvas Mist(int seed)
        {
            var c = new Canvas(Size, Size);
            var random = new System.Random(seed);
            for (var i = 0; i < 26; i++)
            {
                var x = 14 + (float)random.NextDouble() * 72;
                var y = 12 + (float)random.NextDouble() * 50;
                var r = 14 + (float)random.NextDouble() * 18;
                BlendEllipse(c, new Vector2(x, y), r, r * 0.75f, 70);
            }

            return c;
        }

        /// <summary>The Guardian's slam: a low crescent of force with a bright leading edge.</summary>
        private static Canvas Shockwave()
        {
            var c = new Canvas(Size, Size);
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    var nx = (x - 50) / 50f;
                    var ny = y / 100f;
                    var arc = 1f - Mathf.Abs(nx);
                    if (ny > arc * 0.9f)
                    {
                        continue;
                    }

                    var edge = Mathf.Clamp01(1f - Mathf.Abs(ny - arc * 0.85f) * 6f);
                    var alpha = (byte)Mathf.Clamp(60 + edge * 195, 0, 255);
                    c.Set(x, y, new Color32(255, 255, 255, alpha));
                }
            }

            return c;
        }

        /// <summary>A soft round glow; the body of every fake light and halo.</summary>
        private static Canvas Glow()
        {
            var c = new Canvas(Size, Size);
            RadialGlow(c, new Vector2(50, 50), 50, new Color32(255, 255, 255, 255));
            return c;
        }

        /// <summary>A vertical beam of light falling through a vault: bright at the top, fading down.</summary>
        private static Canvas Shaft()
        {
            var c = new Canvas(Size, Size);
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    var across = 1f - Mathf.Abs((x - 50) / 50f);
                    var down = y / 100f;
                    var alpha = Mathf.Pow(across, 2.2f) * Mathf.Lerp(0.05f, 1f, down);
                    if (alpha > 0.01f)
                    {
                        c.Set(x, y, new Color32(255, 255, 255, (byte)(alpha * 255)));
                    }
                }
            }

            return c;
        }

        /// <summary>
        /// A horizontal band of fog that tiles left to right without a seam: twenty soft billows,
        /// each an ellipse wider than tall and wrapped round the edges, over a thin even veil. It
        /// used to be two sines sheared by height, which drew diagonal stripes across every zone.
        /// </summary>
        private static Canvas FogBand()
        {
            const int width = 512;
            const int height = 128;
            var c = new Canvas(width, height);
            var rng = new System.Random(7301);
            var billows = new (float x, float y, float rx, float ry, float weight)[20];
            for (var i = 0; i < billows.Length; i++)
            {
                billows[i] = ((float)rng.NextDouble() * width, 46f + (float)rng.NextDouble() * 36f,
                    30f + (float)rng.NextDouble() * 50f, 10f + (float)rng.NextDouble() * 10f,
                    0.5f + (float)rng.NextDouble() * 0.4f);
            }

            for (var y = 0; y < height; y++)
            {
                var vertical = Mathf.Pow(Mathf.Sin(y / (float)(height - 1) * Mathf.PI), 1.6f);
                for (var x = 0; x < width; x++)
                {
                    var sum = 0f;
                    foreach (var (bx, by, rx, ry, weight) in billows)
                    {
                        // Distance across the wrap, so a billow at the right edge continues on the left.
                        var dx = Mathf.Repeat(x - bx + width * 0.5f, width) - width * 0.5f;
                        var dy = y - by;
                        var q = dx * dx / (rx * rx) + dy * dy / (ry * ry);
                        if (q < 9f)
                        {
                            sum += weight * Mathf.Exp(-q);
                        }
                    }

                    var alpha = Mathf.Clamp01(vertical * (0.25f + 0.75f * (1f - Mathf.Exp(-sum * 1.2f))));
                    if (alpha > 0.01f)
                    {
                        c.Set(x, y, new Color32(255, 255, 255, (byte)(alpha * 255)));
                    }
                }
            }

            return c;
        }

        /// <summary>
        /// Darkness that deepens downward, for what lies under a floor: masonry that fades into
        /// the depth instead of a flat colour below the level.
        /// </summary>
        private static Canvas DepthGradient()
        {
            var c = new Canvas(Size, Size);
            for (var y = 0; y < Size; y++)
            {
                var depth = 1f - y / (float)(Size - 1);
                var alpha = Mathf.SmoothStep(0.15f, 1f, depth);
                for (var x = 0; x < Size; x++)
                {
                    c.Set(x, y, new Color32(8, 7, 9, (byte)(alpha * 255)));
                }
            }

            return c;
        }

        // ---------- helpers ----------

        private static void RadialGlow(Canvas c, Vector2 centre, float radius, Color32 color)
        {
            for (var y = 0; y < c.Height; y++)
            {
                for (var x = 0; x < c.Width; x++)
                {
                    var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), centre) / radius;
                    if (d >= 1f)
                    {
                        continue;
                    }

                    var falloff = Mathf.Pow(1f - d, 2f);
                    var alpha = (byte)(color.a * falloff);
                    if (alpha > 0 && !c.IsOpaque(x, y))
                    {
                        c.Set(x, y, new Color32(color.r, color.g, color.b, alpha));
                    }
                }
            }
        }

        private static void BlendEllipse(Canvas c, Vector2 centre, float rx, float ry, byte strength)
        {
            for (var y = 0; y < c.Height; y++)
            {
                for (var x = 0; x < c.Width; x++)
                {
                    var dx = (x + 0.5f - centre.x) / rx;
                    var dy = (y + 0.5f - centre.y) / ry;
                    var d = dx * dx + dy * dy;
                    if (d >= 1f)
                    {
                        continue;
                    }

                    var add = strength * (1f - d);
                    var existing = c.Alpha(x, y);
                    var alpha = (byte)Mathf.Min(255, existing + add);
                    c.Set(x, y, new Color32(255, 255, 255, alpha));
                }
            }
        }
    }
}
