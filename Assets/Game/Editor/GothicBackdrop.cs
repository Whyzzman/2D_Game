using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The Tomb's three background layers as gothic architecture rather than stripes.
    ///
    /// Far: the nave of a buried cathedral — tall pointed windows letting in cold grey light,
    /// buttress piers between them, a rose in every other bay, haze pooled at the bottom.
    /// Mid: an arcade of pointed arches whose openings are cut through to the far layer, with
    /// knight statues in the bays and old banners hanging from the string course.
    /// Near: clustered piers and hanging chains; everything else transparent.
    ///
    /// Every layer is 2048 × 1600 px (20.48 × 16 units), drawn to sit from four units below the
    /// floor to twelve above it, so no camera position ever shows its edge. Horizontal rhythms
    /// divide 2048 exactly, so the layers tile without a seam. Values fall away with distance —
    /// the far layer is the lowest in contrast (Visual Bible §10) — and nothing here is brighter
    /// than the gameplay plane.
    ///
    /// Still procedural; see ART_ASSET_LIST.md for the hand-painted replacements.
    /// </summary>
    public static class GothicBackdrop
    {
        public const int Width = 2048;
        public const int Height = 1600;

        /// <summary>Pixel row of the gameplay floor inside the layer (four units up from the bottom).</summary>
        private const int Floor = 400;

        public static Canvas Layer(int depth)
        {
            switch (depth)
            {
                case 0: return Far();
                case 1: return Mid();
                default: return Near();
            }
        }

        // ---------- far: the nave ----------

        private static Canvas Far()
        {
            var c = new Canvas(Width, Height);
            var top = new Color32(10, 11, 16, 255);
            var low = new Color32(24, 28, 38, 255);
            for (var y = 0; y < Height; y++)
            {
                var t = y / (float)(Height - 1);
                c.Rect(0, y, Width, y + 1, Color32.Lerp(low, top, Mathf.SmoothStep(0f, 1f, t)));
            }

            var pier = new Color32(12, 13, 19, 255);
            var glassDim = new Color32(40, 50, 68, 255);
            var glassBright = new Color32(70, 84, 106, 255);
            var tracery = new Color32(18, 20, 28, 255);
            const int bay = 512;
            for (var bayX = 0; bayX < Width; bayX += bay)
            {
                var cx = bayX + bay / 2f;
                // A tall lancet window, glowing from within.
                var springY = Floor + 620;
                PointedArch(c, cx, Floor + 180, 150, springY - (Floor + 180), (x, y) =>
                {
                    var v = Mathf.InverseLerp(Floor + 180, springY + 130, y);
                    return Color32.Lerp(glassDim, glassBright, v);
                });
                // Mullions and a trefoil at the head.
                c.RectOver(cx - 3, Floor + 180, cx + 3, springY + 120, tracery);
                c.RectOver(cx - 40, Floor + 180, cx - 34, springY + 60, tracery);
                c.RectOver(cx + 34, Floor + 180, cx + 40, springY + 60, tracery);
                c.RectOver(cx - 75, springY - 10, cx + 75, springY - 4, tracery);
                Ring(c, new Vector2(cx, springY + 70), 26, 5, tracery);

                // Buttress piers between the windows.
                var px = bayX;
                c.Rect(px - 46, 0, px + 46, Floor + 900, pier);
                c.Triangle(new Vector2(px - 46, Floor + 900), new Vector2(px + 46, Floor + 900),
                    new Vector2(px, Floor + 1010), pier);

                // A rose window high in every other bay.
                if ((bayX / bay) % 2 == 1)
                {
                    var rose = new Vector2(cx, Floor + 1060);
                    DiscGradient(c, rose, 90, glassDim, glassBright);
                    for (var i = 0; i < 8; i++)
                    {
                        var a = i * Mathf.PI / 4f;
                        var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                        c.Thick(rose, rose + dir * 90, 5, 5, tracery);
                    }

                    Ring(c, rose, 90, 8, tracery);
                    Ring(c, rose, 34, 6, tracery);
                }
            }

            // Vault ribs across the top, and haze pooled at the floor of the nave.
            for (var x = 0; x < Width; x += 256)
            {
                Arc(c, x + 128, Floor + 1180, 180, 70, 6, new Color32(20, 22, 30, 255));
            }

            Haze(c, 0, Floor + 260, new Color32(38, 44, 58, 255), 0.55f);
            return c;
        }

        // ---------- mid: the arcade ----------

        private static Canvas Mid()
        {
            var c = new Canvas(Width, Height);
            var wall = new Color32(26, 26, 32, 255);
            var wallLit = new Color32(38, 38, 46, 255);
            var shadow = new Color32(15, 15, 20, 255);
            const int bay = 256;

            // The wall of the arcade from the floor to the gallery.
            c.Rect(0, 0, Width, Floor + 720, wall);
            // A clerestory band above, then it gives way to the far layer.
            c.Rect(0, Floor + 720, Width, Floor + 760, wallLit);
            for (var bayX = 0; bayX < Width; bayX += bay)
            {
                var cx = bayX + bay / 2f;
                // Openings are cut through: the far nave shows behind the arches.
                PointedArch(c, cx, Floor - 10, 176, 380, (x, y) => new Color32(0, 0, 0, 0));
                // Column between openings, lit on its left edge by the far windows.
                var col = bayX;
                c.Rect(col - 18, 0, col + 18, Floor + 520, shadow);
                c.Rect(col - 18, 0, col - 12, Floor + 520, wallLit);
                c.Rect(col - 30, Floor + 500, col + 30, Floor + 530, wallLit);
                // Gallery lancets above.
                PointedArch(c, cx, Floor + 560, 70, 90, (x, y) => new Color32(12, 13, 18, 255));

                var index = bayX / bay;
                if (index % 3 == 0)
                {
                    Statue(c, cx, Floor - 10, new Color32(22, 22, 28, 255), new Color32(44, 44, 52, 255));
                }
                else if (index % 3 == 1)
                {
                    Banner(c, cx, Floor + 500, 56, 260, new Color32(62, 20, 30, 255),
                        new Color32(120, 98, 48, 255));
                }
            }

            // String course and base: horizontal lines that tie the arcade together.
            c.RectOver(0, Floor + 470, Width, Floor + 484, wallLit);
            c.Rect(0, 0, Width, Floor - 10, shadow);
            Haze(c, 0, Floor + 200, new Color32(30, 34, 44, 255), 0.35f);
            return c;
        }

        // ---------- near: piers and chains ----------

        private static Canvas Near()
        {
            var c = new Canvas(Width, Height);
            // Cold and a little lifted, never near-black: a black pier reads as a wall to stop at,
            // and the solid stone in front must be the only thing that does.
            var pier = new Color32(27, 30, 40, 255);
            var rim = new Color32(46, 52, 66, 255);
            var chain = new Color32(30, 30, 34, 255);
            const int bay = 512;
            for (var bayX = 0; bayX < Width; bayX += bay)
            {
                // A clustered pier: three shafts, a moulded base, a capital with a rib springing.
                var x = bayX + 90f;
                foreach (var (dx, w) in new[] { (-34f, 26f), (0f, 34f), (34f, 26f) })
                {
                    c.Rect(x + dx - w / 2f, 0, x + dx + w / 2f, Height, pier);
                    c.Rect(x + dx - w / 2f, 0, x + dx - w / 2f + 4, Height, rim);
                }

                c.Rect(x - 62, Floor - 40, x + 62, Floor + 30, pier);
                c.Rect(x - 62, Floor + 26, x + 62, Floor + 30, rim);
                c.Rect(x - 58, Floor + 900, x + 58, Floor + 940, pier);
                c.Rect(x - 58, Floor + 936, x + 58, Floor + 940, rim);

                // Two chains hanging from the dark, at different lengths.
                ChainDown(c, bayX + 300, Height, Floor + 540, chain);
                ChainDown(c, bayX + 380, Height, Floor + 760, chain);
            }

            return c;
        }

        // ---------- drawing ----------

        /// <summary>
        /// An equilateral pointed arch: a rectangle up to the springing line and two intersecting
        /// arcs above it, each centred on the opposite springing point.
        /// </summary>
        private static void PointedArch(Canvas c, float cx, float baseY, float width, float legHeight,
            System.Func<int, int, Color32> fill)
        {
            var half = width * 0.5f;
            var spring = baseY + legHeight;
            var apex = spring + width * 0.866f;
            var left = new Vector2(cx - half, spring);
            var right = new Vector2(cx + half, spring);
            for (var y = Mathf.Max(0, (int)baseY); y < Mathf.Min(c.Height, (int)apex + 1); y++)
            {
                for (var x = (int)(cx - half); x <= (int)(cx + half); x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    var inside = y <= spring
                        || (Vector2.Distance(p, left) <= width && Vector2.Distance(p, right) <= width);
                    if (inside)
                    {
                        c.SetWrapped(x, y, fill(x, y));
                    }
                }
            }
        }

        private static void Ring(Canvas c, Vector2 centre, float radius, float thickness, Color32 tone)
        {
            for (var y = (int)(centre.y - radius - thickness); y <= centre.y + radius + thickness; y++)
            {
                for (var x = (int)(centre.x - radius - thickness); x <= centre.x + radius + thickness; x++)
                {
                    var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), centre);
                    if (Mathf.Abs(d - radius) <= thickness * 0.5f)
                    {
                        c.SetWrapped(x, y, tone);
                    }
                }
            }
        }

        private static void DiscGradient(Canvas c, Vector2 centre, float radius, Color32 edge, Color32 core)
        {
            for (var y = (int)(centre.y - radius); y <= centre.y + radius; y++)
            {
                for (var x = (int)(centre.x - radius); x <= centre.x + radius; x++)
                {
                    var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), centre) / radius;
                    if (d <= 1f)
                    {
                        c.SetWrapped(x, y, Color32.Lerp(core, edge, d));
                    }
                }
            }
        }

        private static void Arc(Canvas c, float cx, float cy, float halfWidth, float rise, float thickness,
            Color32 tone)
        {
            for (var i = 0; i <= 180; i++)
            {
                var a = i / 180f * Mathf.PI;
                var p = new Vector2(cx + Mathf.Cos(a) * halfWidth, cy + Mathf.Sin(a) * rise);
                for (var t = 0; t < thickness; t++)
                {
                    c.SetWrapped((int)p.x, (int)p.y + t, tone);
                }
            }
        }

        /// <summary>Lightens what is already painted towards a haze colour, strongest at the bottom.</summary>
        private static void Haze(Canvas c, int fromY, int toY, Color32 tone, float strength)
        {
            for (var y = fromY; y < toY; y++)
            {
                var t = 1f - (y - fromY) / (float)Mathf.Max(1, toY - fromY);
                c.TintOver(0, y, c.Width, y + 1, tone, strength * t * t);
            }
        }

        private static void Statue(Canvas c, float x, float baseY, Color32 tone, Color32 rim)
        {
            // A knight effigy standing guard: plinth, body, crossed hands on a sword, helm.
            c.Rect(x - 50, baseY, x + 50, baseY + 44, tone);
            c.Rect(x - 28, baseY + 44, x + 28, baseY + 250, tone);
            c.Rect(x - 40, baseY + 170, x + 40, baseY + 230, tone);
            c.Ellipse(new Vector2(x, baseY + 280), 24, 32, tone);
            c.Thick(new Vector2(x, baseY + 60), new Vector2(x, baseY + 210), 7, 7, rim);
            c.Rect(x - 18, baseY + 200, x + 18, baseY + 208, rim);
            c.Rect(x - 28, baseY + 44, x - 24, baseY + 250, rim);
        }

        private static void Banner(Canvas c, float x, float topY, float width, float height, Color32 cloth,
            Color32 gold)
        {
            var half = width * 0.5f;
            c.Rect(x - half - 8, topY - 6, x + half + 8, topY, new Color32(40, 40, 46, 255));
            c.Rect(x - half, topY - height, x + half, topY - 6, cloth);
            // A torn swallowtail hem.
            c.Triangle(new Vector2(x - half, topY - height), new Vector2(x, topY - height),
                new Vector2(x - half, topY - height - 40), cloth);
            c.Triangle(new Vector2(x, topY - height), new Vector2(x + half, topY - height),
                new Vector2(x + half, topY - height - 40), cloth);
            // The crown-and-flame in faded gold.
            var crest = new Vector2(x, topY - height * 0.42f);
            c.Triangle(crest + new Vector2(-14, -10), crest + new Vector2(-8, -10), crest + new Vector2(-11, 8), gold);
            c.Triangle(crest + new Vector2(-4, -10), crest + new Vector2(4, -10), crest + new Vector2(0, 14), gold);
            c.Triangle(crest + new Vector2(8, -10), crest + new Vector2(14, -10), crest + new Vector2(11, 8), gold);
            c.Rect(crest.x - 15, crest.y - 14, crest.x + 15, crest.y - 9, gold);
            c.Ellipse(crest + new Vector2(0, 22), 5, 8, gold);
        }

        private static void ChainDown(Canvas c, float x, int fromY, int toY, Color32 tone)
        {
            for (var y = toY; y < fromY; y += 22)
            {
                var odd = (y / 22) % 2 == 0;
                if (odd)
                {
                    Ring(c, new Vector2(x, y + 11), 8, 4, tone);
                }
                else
                {
                    c.Rect(x - 2, y, x + 2, y + 22, tone);
                }
            }
        }
    }
}
