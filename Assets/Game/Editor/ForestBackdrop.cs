using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The Withered Forest's three background layers and the Warden's abbey (visual item 1, the
    /// user's choices of 2026-09-28: a misty sky with the moon, moderate ruins, the arena a roofless
    /// abbey among the trees).
    ///
    /// Far: the only opaque layer — a sky that darkens into the camera's clear colour at the top,
    /// the moon in a halo, thin cloud across it, low hills with dead trees on the ridge and the
    /// kingdom's broken spires in the mist. Mid: a transparent treeline of crooked trees, a ruined
    /// arch and a broken colonnade, with ground mist. Near: a few great trunks with a moonlit edge
    /// and hanging moss, fading out at the top so a climb never shows where they end.
    ///
    /// Each layer is 2048 × 2000 px (20.48 × 20 units), the floor four units up, so it covers the
    /// view from below the floor to the top of the forest's climbs. Everything is drawn wrapped,
    /// so the layers tile without a seam. Still procedural: see ART_ASSET_LIST.md.
    /// </summary>
    public static class ForestBackdrop
    {
        public const int Width = 2048;
        public const int Height = 2000;
        public const int ArenaWidth = 1600;
        public const int ArenaHeight = 1000;

        /// <summary>Pixel row of the gameplay floor inside a layer (four units up from the bottom).</summary>
        private const int Floor = 400;

        /// <summary>The camera's clear colour; the sky ends in it, so above the layer is more sky.</summary>
        public static readonly Color32 SkyTop = new Color32(10, 11, 17, 255);
        private static readonly Color32 SkyHigh = new Color32(17, 25, 29, 255);
        private static readonly Color32 SkyLow = new Color32(40, 58, 60, 255);
        private static readonly Color32 Moon = new Color32(206, 222, 218, 255);
        private static readonly Color32 MoonShade = new Color32(176, 194, 192, 255);
        private static readonly Color32 Halo = new Color32(112, 138, 138, 255);
        private static readonly Color32 Mist = new Color32(78, 102, 102, 255);
        private static readonly Color32 Hills = new Color32(24, 34, 36, 255);
        private static readonly Color32 Spires = new Color32(21, 29, 32, 255);
        private static readonly Color32 MidWood = new Color32(24, 29, 29, 255);
        private static readonly Color32 MidRim = new Color32(52, 66, 64, 255);
        private static readonly Color32 MidStone = new Color32(33, 38, 38, 255);
        private static readonly Color32 NearBark = new Color32(21, 19, 17, 255);
        private static readonly Color32 NearRim = new Color32(70, 84, 80, 255);
        private static readonly Color32 Moss = new Color32(38, 52, 40, 255);
        private static readonly Color32 MossLit = new Color32(58, 76, 56, 255);
        private static readonly Color32 Stone = new Color32(40, 44, 43, 255);
        private static readonly Color32 StoneLit = new Color32(74, 82, 78, 255);
        private static readonly Color32 StoneDark = new Color32(24, 27, 28, 255);

        public static Canvas Layer(int depth)
        {
            switch (depth)
            {
                case 0: return Far();
                case 1: return Mid();
                default: return Near();
            }
        }

        // ---------- far: sky, moon, hills, spires ----------

        private static Canvas Far()
        {
            var p = new Paint(Width, Height, wrap: true);
            Sky(p, Floor);

            var moon = new Vector2(1480f, Floor + 700f);
            MoonWithHalo(p, moon, 50f, 420f);

            // Thin cloud, two strands across the moon so it sits in the sky rather than on it.
            var rng = new System.Random(5101);
            for (var i = 0; i < 9; i++)
            {
                var centre = new Vector2(rng.Next(0, Width), Floor + rng.Next(420, 1150));
                if (i < 2)
                {
                    centre = moon + new Vector2(rng.Next(-160, 160), -30f + i * 64f);
                }

                var near = Mathf.Clamp01(1f - Vector2.Distance(centre, moon) / 600f);
                p.SoftEllipse(centre, rng.Next(220, 520), rng.Next(8, 20),
                    Color32.Lerp(Mist, Halo, near), 0.10f + near * 0.22f);
            }

            // The kingdom in the mist: an abbey spire and the capital's sheared tower.
            Spire(p, 430f, Floor + 180f, 60f, 520f, Spires);
            Spire(p, 520f, Floor + 180f, 34f, 330f, Spires);
            Tower(p, 1180f, Floor + 170f, 70f, 430f, Spires);

            // Low hills with a fringe of dead trees on the ridge; the ridge wraps with the layer.
            for (var x = 0; x < Width; x++)
            {
                var ridge = Floor + 170f + 55f * Wave(x, 2, 0.8f) + 30f * Wave(x, 5, 2.1f)
                    + 12f * Wave(x, 13, 0.4f);
                p.FillColumn(x, 0, ridge, Hills, 1f);
            }

            for (var x = 6; x < Width; x += rng.Next(9, 22))
            {
                var ridge = Floor + 170f + 55f * Wave(x, 2, 0.8f) + 30f * Wave(x, 5, 2.1f)
                    + 12f * Wave(x, 13, 0.4f);
                var height = rng.Next(18, 70);
                p.Stroke(new Vector2(x, ridge - 4f), new Vector2(x + rng.Next(-5, 6), ridge + height),
                    3f, 1f, Hills, 1f);
            }

            // Mist lying in the valley in front of the hills.
            p.HazeBand(Floor + 60f, Floor + 330f, Mist, 0.42f);
            return p.ToCanvas();
        }

        // ---------- mid: treeline, ruins, ground mist ----------

        private static Canvas Mid()
        {
            var p = new Paint(Width, Height, wrap: true);
            var rng = new System.Random(5202);

            // The ground the treeline stands on, a little above the floor so it reads as a bank.
            for (var x = 0; x < Width; x++)
            {
                var bank = Floor + 30f + 26f * Wave(x, 3, 1.3f) + 10f * Wave(x, 11, 0.2f);
                p.FillColumn(x, 0, bank, MidWood, 1f);
            }

            // Ruins: a broken pointed arch and a colonnade with one column still standing.
            RuinedArch(p, 720f, Floor + 20f, 230f, 420f, MidStone, MidRim);
            Colonnade(p, 1640f, Floor + 20f, MidStone, MidRim, rng);

            // Crooked trees, none alike.
            const int count = 11;
            for (var i = 0; i < count; i++)
            {
                var x = i * Width / (float)count + rng.Next(-50, 50);
                if (Mathf.Abs(x - 720f) < 150f || Mathf.Abs(x - 1640f) < 110f)
                {
                    x += 180f;
                }

                var height = rng.Next(620, 1180);
                var lean = rng.Next(-12, 13);
                Tree(p, new Vector2(x, Floor), height, rng.Next(20, 34), lean, MidWood, MidRim, 0.55f,
                    rng, 3);
            }

            p.HazeBand(Floor - 60f, Floor + 280f, Mist, 0.34f, addAlpha: 0.32f);
            return p.ToCanvas();
        }

        // ---------- near: great trunks ----------

        private static Canvas Near()
        {
            var p = new Paint(Width, Height, wrap: true);
            var rng = new System.Random(5303);
            foreach (var x in new[] { 240f, 1010f, 1660f })
            {
                var baseX = x + rng.Next(-60, 60);
                var width = rng.Next(80, 112);
                var lean = rng.Next(-40, 40);
                var top = new Vector2(baseX + lean, Height + 40f);

                // Roots flaring into the floor.
                for (var r = -3; r <= 3; r++)
                {
                    if (r == 0) { continue; }
                    p.Stroke(new Vector2(baseX + r * 10f, Floor + 60f),
                        new Vector2(baseX + r * 42f, Floor - 30f), 26f, 8f, NearBark, 1f);
                }

                p.Stroke(new Vector2(baseX, Floor - 40f), top, width, width * 0.62f, NearBark, 1f);
                // The moonlit edge down one side: without it the trunk is a dark bar.
                p.Stroke(new Vector2(baseX + width * 0.38f, Floor), top + new Vector2(width * 0.2f, 0f),
                    6f, 4f, NearRim, 0.75f);

                // Two heavy boughs, with moss hanging from them.
                for (var b = 0; b < 2; b++)
                {
                    var side = (b + (int)x) % 2 == 0 ? 1f : -1f;
                    var at = Vector2.Lerp(new Vector2(baseX, Floor), top,
                        0.42f + b * 0.18f + rng.Next(0, 6) * 0.01f);
                    var end = at + new Vector2(side * rng.Next(260, 420), rng.Next(60, 200));
                    var mid = Vector2.Lerp(at, end, 0.5f) + new Vector2(0f, rng.Next(20, 70));
                    p.Stroke(at, mid, 30f, 20f, NearBark, 1f);
                    p.Stroke(mid, end, 20f, 6f, NearBark, 1f);
                    for (var s = 0; s < 7; s++)
                    {
                        var t = 0.25f + s * 0.1f;
                        var from = t < 0.5f ? Vector2.Lerp(at, mid, t * 2f) : Vector2.Lerp(mid, end, (t - 0.5f) * 2f);
                        var length = rng.Next(60, 220);
                        p.Stroke(from, from + new Vector2(rng.Next(-8, 9), -length), 5f, 1.5f, Moss, 0.85f);
                    }
                }

                // Moss creeping up from the roots.
                for (var m = 0; m < 50; m++)
                {
                    var y = Floor + rng.Next(-20, 260);
                    var dx = rng.Next(-width / 2, width / 2);
                    p.SoftEllipse(new Vector2(baseX + dx, y), rng.Next(6, 16), rng.Next(3, 8),
                        m % 3 == 0 ? MossLit : Moss, 0.8f, onlyOver: true);
                }
            }

            // Fade out towards the top, so a climb sees the trunks vanish into the dark.
            p.FadeTop(Height - 380, Height);
            return p.ToCanvas();
        }

        // ---------- the Warden's abbey ----------

        /// <summary>
        /// A roofless abbey among the trees: the gable with its rose window broken open and the
        /// moon standing in it, the nave walls with empty lancets, a dead tree grown through the
        /// masonry and mist at the feet. 1600 × 1000 px, drawn to be shown at twice its size, the
        /// floor 100 px up; its sides fade out so it melts into the forest behind it.
        /// </summary>
        public static Canvas Abbey()
        {
            const int floor = 100;
            var p = new Paint(ArenaWidth, ArenaHeight, wrap: false);
            Sky(p, floor, 0.5f);
            var moon = new Vector2(ArenaWidth * 0.5f, floor + 360f);
            MoonWithHalo(p, moon, 44f, 300f);

            var rng = new System.Random(5404);
            p.SoftEllipse(moon + new Vector2(-120f, -34f), 260f, 9f, Halo, 0.24f);
            p.SoftEllipse(moon + new Vector2(150f, 52f), 200f, 7f, Halo, 0.18f);

            // Distant treeline behind the ruin.
            for (var x = 0; x < ArenaWidth; x++)
            {
                p.FillColumn(x, 0, floor + 120f + 40f * Mathf.Sin(x * 0.011f) + 18f * Mathf.Sin(x * 0.037f),
                    Hills, 1f);
            }

            // Nave walls on both sides, broken off at different heights, lancets open to the sky.
            // The top of the masonry in each column, for the ivy to hang from.
            var tops = new float[ArenaWidth];
            NaveWall(p, 150f, 560f, floor, 470f, 3, tops);
            NaveWall(p, 1040f, 1450f, floor, 430f, 3, tops);

            // The west gable: a pointed wall with the rose window cut clean through it.
            var gableLeft = 580f;
            var gableRight = 1020f;
            for (var x = (int)gableLeft; x < gableRight; x++)
            {
                var half = (gableRight - gableLeft) * 0.5f;
                var fromCentre = Mathf.Abs(x - (gableLeft + half)) / half;
                var top = floor + 560f + (1f - fromCentre) * 210f;
                // Broken on the right: the top of the gable has fallen.
                if (x > gableLeft + half * 1.25f)
                {
                    top = Mathf.Min(top, floor + 520f + 30f * Mathf.Sin(x * 0.09f));
                }

                p.FillColumn(x, floor, top, Stone, 1f);
                tops[x] = Mathf.Max(tops[x], top);
            }

            Courses(p, (int)gableLeft, (int)gableRight, floor, tops);

            // The rose: open, its tracery still standing, the moon in the middle of it.
            p.Disc(moon, 130f, new Color32(0, 0, 0, 0), 1f, cut: true);
            SkyInto(p, moon, 130f, floor, 0.5f);
            MoonWithHalo(p, moon, 44f, 150f);
            RingStroke(p, moon, 130f, 12f, StoneLit, 1f);
            RingStroke(p, moon, 128f, 20f, Stone, 1f);
            for (var i = 0; i < 8; i++)
            {
                var a = i * Mathf.PI / 4f + 0.2f;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                // Two spokes are broken off short.
                var reach = i == 2 || i == 5 ? 70f : 126f;
                p.Stroke(moon + dir * 50f, moon + dir * reach, 9f, 7f, Stone, 1f);
            }

            RingStroke(p, moon, 52f, 8f, Stone, 1f);
            // The west door, dark.
            PointedOpening(p, ArenaWidth * 0.5f, floor, 150f, 170f, StoneDark);
            // Lit edges on the gable, from the moon behind.
            p.Stroke(new Vector2(gableLeft, floor), new Vector2(gableLeft, floor + 560f), 5f, 5f, StoneLit, 0.6f);

            // Buttresses, stepped.
            foreach (var x in new[] { 150f, 560f, 1040f, 1450f })
            {
                p.Rect(x - 26f, floor, x + 26f, floor + 300f, StoneDark, 1f);
                p.Rect(x - 20f, floor + 300f, x + 20f, floor + 380f, StoneDark, 1f);
                p.Rect(x - 26f, floor + 296f, x + 26f, floor + 302f, StoneLit, 0.6f);
            }

            // The forest taking it back: ivy hanging short from the broken tops, moss at the foot.
            for (var i = 0; i < 40; i++)
            {
                var x = rng.Next(160, 1440);
                var top = tops[x];
                if (top <= floor + 40)
                {
                    continue;
                }

                p.Stroke(new Vector2(x, top - 2f), new Vector2(x + rng.Next(-6, 7), top - rng.Next(30, 110)),
                    rng.Next(3, 6), 1.5f, Moss, 0.85f, onlyOver: true);
            }

            for (var i = 0; i < 70; i++)
            {
                p.SoftEllipse(new Vector2(rng.Next(150, 1450), floor + rng.Next(0, 150)),
                    rng.Next(5, 12), rng.Next(2, 5), i % 4 == 0 ? MossLit : Moss, 0.7f, onlyOver: true);
            }

            // A dead tree grown up through the south wall, reaching over the nave.
            Tree(p, new Vector2(300f, floor), 760, 46, 14, NearBark, NearRim, 0.8f, rng, 4);

            // Mist at the feet, the floor below it dark.
            p.Rect(0f, 0f, ArenaWidth, floor, StoneDark, 1f);
            p.HazeBand(floor - 40f, floor + 180f, Mist, 0.5f);

            // Its sides fade into the forest behind it.
            p.FadeSides(360f);
            return p.ToCanvas();
        }

        // ---------- pieces ----------

        private static void Sky(Paint p, int floor, float heightScale = 1f)
        {
            for (var y = 0; y < p.Height; y++)
            {
                var t = Mathf.Clamp01((y - floor) / ((p.Height - floor) * heightScale));
                var low = Color32.Lerp(SkyLow, SkyHigh, Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, t * 1.8f)));
                var colour = Color32.Lerp(low, SkyTop, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1f, t)));
                if (heightScale >= 1f && y == p.Height - 1)
                {
                    colour = SkyTop;
                }

                p.Rect(0f, y, p.Width, y + 1, colour, 1f);
            }
        }

        /// <summary>Paints sky back into a hole cut through something, as if the hole were never filled.</summary>
        private static void SkyInto(Paint p, Vector2 centre, float radius, int floor, float heightScale)
        {
            for (var y = (int)(centre.y - radius); y <= centre.y + radius; y++)
            {
                var t = Mathf.Clamp01((y - floor) / ((p.Height - floor) * heightScale));
                var low = Color32.Lerp(SkyLow, SkyHigh, Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, t * 1.8f)));
                var colour = Color32.Lerp(low, SkyTop, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1f, t)));
                for (var x = (int)(centre.x - radius); x <= centre.x + radius; x++)
                {
                    if (Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), centre) <= radius && p.AlphaAt(x, y) == 0)
                    {
                        p.Blend(x, y, colour, 1f);
                    }
                }
            }
        }

        private static void MoonWithHalo(Paint p, Vector2 centre, float radius, float halo)
        {
            p.Glow(centre, halo, Halo, 0.38f);
            p.Glow(centre, radius * 2.2f, Halo, 0.35f);
            p.Disc(centre, radius, Moon, 1f);
            // A few darker seas, so it is a moon and not a lamp.
            p.Disc(centre + new Vector2(-radius * 0.3f, radius * 0.2f), radius * 0.28f, MoonShade, 0.7f);
            p.Disc(centre + new Vector2(radius * 0.25f, -radius * 0.3f), radius * 0.2f, MoonShade, 0.6f);
            p.Disc(centre + new Vector2(radius * 0.35f, radius * 0.35f), radius * 0.12f, MoonShade, 0.5f);
        }

        private static void Spire(Paint p, float x, float baseY, float width, float height, Color32 tone)
        {
            p.Rect(x - width * 0.5f, baseY, x + width * 0.5f, baseY + height, tone, 1f);
            var top = baseY + height;
            // A pointed top broken off short on one side: the spire is a ruin too. Never cut
            // back to transparency — this is the sky layer, and a hole would show the clear colour.
            p.Triangle(new Vector2(x - width * 0.5f, top), new Vector2(x + width * 0.5f, top),
                new Vector2(x - width * 0.1f, top + width * 1.9f), tone);
            p.Triangle(new Vector2(x - width * 0.1f, top + width * 1.9f), new Vector2(x + width * 0.5f, top),
                new Vector2(x + width * 0.2f, top + width * 0.9f), Color32.Lerp(tone, SkyLow, 0.35f));
            // Lancet slits.
            for (var i = 0; i < 3; i++)
            {
                var y = baseY + height * (0.35f + i * 0.2f);
                p.Rect(x - 4f, y, x + 4f, y + width * 0.6f, Color32.Lerp(tone, SkyLow, 0.5f), 1f);
            }
        }

        private static void Tower(Paint p, float x, float baseY, float width, float height, Color32 tone)
        {
            p.Rect(x - width * 0.5f, baseY, x + width * 0.5f, baseY + height * 0.8f, tone, 1f);
            // Sheared off at an angle.
            p.Triangle(new Vector2(x - width * 0.5f, baseY + height * 0.8f),
                new Vector2(x + width * 0.5f, baseY + height * 0.8f),
                new Vector2(x - width * 0.5f, baseY + height), tone);
            p.Rect(x - 5f, baseY + height * 0.5f, x + 5f, baseY + height * 0.62f,
                Color32.Lerp(tone, SkyLow, 0.5f), 1f);
        }

        private static void RuinedArch(Paint p, float cx, float baseY, float width, float legHeight,
            Color32 tone, Color32 rim)
        {
            var half = width * 0.5f;
            var pier = 34f;
            p.Rect(cx - half - pier, baseY, cx - half, baseY + legHeight, tone, 1f);
            p.Rect(cx + half, baseY, cx + half + pier, baseY + legHeight * 0.72f, tone, 1f);
            // Only the left half of the pointed head survives.
            var spring = baseY + legHeight;
            var right = new Vector2(cx + half, spring);
            for (var y = (int)spring; y < spring + width * 0.8f; y++)
            {
                for (var x = (int)(cx - half - pier); x < cx; x++)
                {
                    var d = Vector2.Distance(new Vector2(x, y), right);
                    if (d <= width + pier && d >= width)
                    {
                        p.Blend(x, y, tone, 1f);
                    }
                }
            }

            p.Rect(cx - half - pier, baseY + legHeight - 4f, cx - half, baseY + legHeight, rim, 0.8f);
            p.Rect(cx + half, baseY + legHeight * 0.72f - 4f, cx + half + pier, baseY + legHeight * 0.72f, rim, 0.8f);
        }

        private static void Colonnade(Paint p, float x, float baseY, Color32 tone, Color32 rim,
            System.Random rng)
        {
            // One whole column with its capital, two broken stumps and a fallen drum.
            p.Rect(x - 20f, baseY, x + 20f, baseY + 460f, tone, 1f);
            p.Rect(x - 32f, baseY + 460f, x + 32f, baseY + 486f, tone, 1f);
            p.Rect(x - 32f, baseY + 482f, x + 32f, baseY + 486f, rim, 0.8f);
            p.Rect(x + 120f, baseY, x + 160f, baseY + 210f, tone, 1f);
            p.Triangle(new Vector2(x + 120f, baseY + 210f), new Vector2(x + 160f, baseY + 210f),
                new Vector2(x + 120f, baseY + 250f), tone);
            p.Rect(x - 150f, baseY, x - 110f, baseY + 120f, tone, 1f);
            p.Rect(x + 190f, baseY, x + 270f, baseY + 36f, tone, 1f);
            p.Rect(x - 20f, baseY, x - 16f, baseY + 460f, rim, 0.5f);
            for (var i = 0; i < 6; i++)
            {
                p.Stroke(new Vector2(x + rng.Next(-18, 18), baseY + rng.Next(160, 440)),
                    new Vector2(x + rng.Next(-24, 24), baseY), 4f, 2f, Moss, 0.8f, onlyOver: true);
            }
        }

        private static void NaveWall(Paint p, float left, float right, int floor, float height,
            int lancets, float[] tops)
        {
            for (var x = (int)left; x < right; x++)
            {
                // A broken top: long slopes and a few stepped breaks, not a comb of teeth.
                var top = floor + height + 26f * Mathf.Sin(x * 0.013f) + 10f * Mathf.Sin(x * 0.041f)
                    - (x - left) / (right - left) * 60f - ((int)((x - left) / 70f) % 3 == 1 ? 22f : 0f);
                p.FillColumn(x, floor, top, Stone, 1f);
                tops[x] = Mathf.Max(tops[x], top);
            }

            Courses(p, (int)left, (int)right, floor, tops);
            var bay = (right - left) / lancets;
            for (var i = 0; i < lancets; i++)
            {
                var cx = left + bay * (i + 0.5f);
                var w = bay * 0.42f;
                // Open to the sky: the lancet shows what is behind the wall.
                PointedOpening(p, cx, floor + 110f, w, height * 0.5f, new Color32(0, 0, 0, 0), cut: true);
                SkyIntoRect(p, cx - w * 0.5f, floor + 110f, cx + w * 0.5f, floor + 110f + height * 0.5f + w,
                    floor);
                p.Rect(cx - w * 0.5f - 6f, floor + 104f, cx + w * 0.5f + 6f, floor + 110f, StoneLit, 0.7f);
            }

            p.Rect(left, floor + height * 0.72f, right, floor + height * 0.72f + 8f, StoneDark, 1f, onlyOver: true);
        }

        /// <summary>
        /// Stone courses, so a wall reads as masonry and not as flat card. Drawn on the stone
        /// before any opening is cut, so they never cross the sky.
        /// </summary>
        private static void Courses(Paint p, int left, int right, int floor, float[] tops)
        {
            for (var x = left; x < right; x++)
            {
                for (var y = floor + 36; y < tops[x] - 4f; y += 36)
                {
                    p.Rect(x, y, x + 1, y + 2, StoneDark, 0.55f, onlyOver: true);
                }
            }
        }

        private static void SkyIntoRect(Paint p, float x0, float y0, float x1, float y1, int floor)
        {
            for (var y = (int)y0; y < y1; y++)
            {
                var t = Mathf.Clamp01((y - floor) / ((p.Height - floor) * 0.5f));
                var low = Color32.Lerp(SkyLow, SkyHigh, Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, t * 1.8f)));
                var colour = Color32.Lerp(low, SkyTop, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1f, t)));
                for (var x = (int)x0; x < x1; x++)
                {
                    if (p.AlphaAt(x, y) == 0)
                    {
                        p.Blend(x, y, colour, 1f);
                    }
                }
            }

            // The treeline behind shows through the bottom of the opening.
            for (var x = (int)x0; x < x1; x++)
            {
                var ridge = floor + 120f + 40f * Mathf.Sin(x * 0.011f) + 18f * Mathf.Sin(x * 0.037f);
                for (var y = (int)y0; y < ridge; y++)
                {
                    p.Blend(x, y, Hills, 1f);
                }
            }
        }

        private static void PointedOpening(Paint p, float cx, float baseY, float width, float legHeight,
            Color32 fill, bool cut = false)
        {
            var half = width * 0.5f;
            var spring = baseY + legHeight;
            var left = new Vector2(cx - half, spring);
            var right = new Vector2(cx + half, spring);
            for (var y = (int)baseY; y <= spring + width * 0.87f; y++)
            {
                for (var x = (int)(cx - half); x <= cx + half; x++)
                {
                    var q = new Vector2(x + 0.5f, y + 0.5f);
                    if (y <= spring || (Vector2.Distance(q, left) <= width && Vector2.Distance(q, right) <= width))
                    {
                        if (cut)
                        {
                            p.Clear(x, y);
                        }
                        else
                        {
                            p.Blend(x, y, fill, 1f);
                        }
                    }
                }
            }
        }

        private static void RingStroke(Paint p, Vector2 centre, float radius, float thickness, Color32 tone,
            float alpha)
        {
            for (var y = (int)(centre.y - radius - thickness); y <= centre.y + radius + thickness; y++)
            {
                for (var x = (int)(centre.x - radius - thickness); x <= centre.x + radius + thickness; x++)
                {
                    var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), centre);
                    if (Mathf.Abs(d - radius) <= thickness * 0.5f)
                    {
                        p.Blend(x, y, tone, alpha);
                    }
                }
            }
        }

        /// <summary>
        /// A crooked tree: the trunk in three kinked segments, then forking boughs, each thinner
        /// and more bent than the last. A lit edge down the trunk's right side.
        /// </summary>
        private static void Tree(Paint p, Vector2 root, float height, float width, float lean,
            Color32 tone, Color32 rim, float rimAlpha, System.Random rng, int depth)
        {
            var point = root + new Vector2(0f, -30f);
            var angle = lean;
            var segment = height / 3f;
            var w = width;
            for (var s = 0; s < 3; s++)
            {
                angle += rng.Next(-9, 10);
                var next = point + Dir(angle) * segment;
                p.Stroke(point, next, w, w * 0.78f, tone, 1f);
                p.Stroke(point + new Vector2(w * 0.32f, 0f), next + new Vector2(w * 0.25f, 0f),
                    Mathf.Max(2f, w * 0.12f), Mathf.Max(1.5f, w * 0.1f), rim, rimAlpha);
                if (s > 0)
                {
                    Bough(p, next, angle + (s % 2 == 0 ? 38f : -38f) + rng.Next(-12, 12), segment * 0.7f,
                        w * 0.55f, tone, rng, depth - 1);
                }

                point = next;
                w *= 0.78f;
            }

            Bough(p, point, angle + rng.Next(-20, 20), segment * 0.6f, w, tone, rng, depth - 1);
            // Roots at the foot.
            p.Stroke(root + new Vector2(-width * 0.2f, 10f), root + new Vector2(-width * 1.2f, -20f), width * 0.4f, 3f, tone, 1f);
            p.Stroke(root + new Vector2(width * 0.2f, 10f), root + new Vector2(width * 1.3f, -24f), width * 0.4f, 3f, tone, 1f);
        }

        private static void Bough(Paint p, Vector2 from, float angle, float length, float width, Color32 tone,
            System.Random rng, int depth)
        {
            var mid = from + Dir(angle) * length * 0.5f;
            var bend = angle + rng.Next(-25, 26);
            var end = mid + Dir(bend) * length * 0.5f;
            p.Stroke(from, mid, width, width * 0.8f, tone, 1f);
            p.Stroke(mid, end, width * 0.8f, Mathf.Max(1.5f, width * 0.35f), tone, 1f);
            if (depth <= 0 || width < 3f)
            {
                return;
            }

            Bough(p, mid, bend + 34f + rng.Next(-10, 10), length * 0.6f, width * 0.55f, tone, rng, depth - 1);
            Bough(p, end, bend - 30f + rng.Next(-10, 10), length * 0.55f, width * 0.5f, tone, rng, depth - 1);
        }

        /// <summary>Degrees from straight up, positive leaning right.</summary>
        private static Vector2 Dir(float degrees)
        {
            var r = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(r), Mathf.Cos(r));
        }

        /// <summary>A sine whose period divides the layer width, so anything shaped by it tiles.</summary>
        private static float Wave(int x, int cycles, float phase) =>
            Mathf.Sin(x / (float)Width * Mathf.PI * 2f * cycles + phase);

        // ---------- painting ----------

        /// <summary>
        /// A premultiplied-free RGBA buffer with real "over" blending, which the shared
        /// <see cref="Canvas"/> does not do: backgrounds are built from soft light and haze.
        /// </summary>
        private sealed class Paint
        {
            public readonly int Width;
            public readonly int Height;
            private readonly Color32[] _px;
            private readonly bool _wrap;

            public Paint(int width, int height, bool wrap)
            {
                Width = width;
                Height = height;
                _wrap = wrap;
                _px = new Color32[width * height];
            }

            private int Index(int x, int y)
            {
                if (y < 0 || y >= Height)
                {
                    return -1;
                }

                if (_wrap)
                {
                    x = ((x % Width) + Width) % Width;
                }
                else if (x < 0 || x >= Width)
                {
                    return -1;
                }

                return y * Width + x;
            }

            public byte AlphaAt(int x, int y)
            {
                var i = Index(x, y);
                return i < 0 ? (byte)0 : _px[i].a;
            }

            public void Clear(int x, int y)
            {
                var i = Index(x, y);
                if (i >= 0)
                {
                    _px[i] = new Color32(0, 0, 0, 0);
                }
            }

            public void Blend(int x, int y, Color32 c, float a, bool onlyOver = false)
            {
                var i = Index(x, y);
                if (i < 0 || a <= 0f)
                {
                    return;
                }

                var d = _px[i];
                if (onlyOver && d.a == 0)
                {
                    return;
                }

                a = Mathf.Clamp01(a);
                var da = d.a / 255f;
                var outA = a + da * (1f - a);
                if (outA <= 0f)
                {
                    return;
                }

                float Mix(byte src, byte dst) => (src * a + dst * da * (1f - a)) / outA;
                _px[i] = new Color32((byte)Mathf.Round(Mix(c.r, d.r)), (byte)Mathf.Round(Mix(c.g, d.g)),
                    (byte)Mathf.Round(Mix(c.b, d.b)), onlyOver ? d.a : (byte)Mathf.Round(outA * 255f));
            }

            public void Rect(float x0, float y0, float x1, float y1, Color32 c, float a, bool onlyOver = false)
            {
                for (var y = (int)Mathf.Max(0, y0); y < Mathf.Min(Height, y1); y++)
                {
                    for (var x = (int)x0; x < x1; x++)
                    {
                        Blend(x, y, c, a, onlyOver);
                    }
                }
            }

            public void FillColumn(int x, float y0, float y1, Color32 c, float a)
            {
                for (var y = (int)Mathf.Max(0, y0); y < Mathf.Min(Height, y1); y++)
                {
                    Blend(x, y, c, a);
                }
            }

            public void Disc(Vector2 centre, float radius, Color32 c, float a, bool cut = false)
            {
                for (var y = (int)(centre.y - radius - 1); y <= centre.y + radius + 1; y++)
                {
                    for (var x = (int)(centre.x - radius - 1); x <= centre.x + radius + 1; x++)
                    {
                        var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), centre);
                        // One pixel of soft edge.
                        var cover = Mathf.Clamp01(radius + 0.5f - d);
                        if (cover <= 0f) { continue; }
                        if (cut)
                        {
                            if (cover >= 0.5f) { Clear(x, y); }
                        }
                        else
                        {
                            Blend(x, y, c, a * cover);
                        }
                    }
                }
            }

            /// <summary>Light falling off from a point with the square of the distance.</summary>
            public void Glow(Vector2 centre, float radius, Color32 c, float strength)
            {
                for (var y = (int)(centre.y - radius); y <= centre.y + radius; y++)
                {
                    for (var x = (int)(centre.x - radius); x <= centre.x + radius; x++)
                    {
                        var t = 1f - Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), centre) / radius;
                        if (t > 0f)
                        {
                            Blend(x, y, c, strength * t * t, onlyOver: true);
                        }
                    }
                }
            }

            public void SoftEllipse(Vector2 centre, float rx, float ry, Color32 c, float a, bool onlyOver = false)
            {
                for (var y = (int)(centre.y - ry); y <= centre.y + ry; y++)
                {
                    for (var x = (int)(centre.x - rx); x <= centre.x + rx; x++)
                    {
                        var dx = (x + 0.5f - centre.x) / rx;
                        var dy = (y + 0.5f - centre.y) / ry;
                        var d = dx * dx + dy * dy;
                        if (d < 1f)
                        {
                            Blend(x, y, c, a * (1f - d) * (1f - d), onlyOver);
                        }
                    }
                }
            }

            public void Triangle(Vector2 a, Vector2 b, Vector2 c, Color32 colour, bool cut = false)
            {
                var minX = (int)Mathf.Min(a.x, Mathf.Min(b.x, c.x));
                var maxX = (int)Mathf.Max(a.x, Mathf.Max(b.x, c.x));
                var minY = (int)Mathf.Min(a.y, Mathf.Min(b.y, c.y));
                var maxY = (int)Mathf.Max(a.y, Mathf.Max(b.y, c.y));
                for (var y = minY; y <= maxY; y++)
                {
                    for (var x = minX; x <= maxX; x++)
                    {
                        var q = new Vector2(x + 0.5f, y + 0.5f);
                        var ab = Side(q, a, b);
                        var bc = Side(q, b, c);
                        var ca = Side(q, c, a);
                        if ((ab >= 0f && bc >= 0f && ca >= 0f) || (ab <= 0f && bc <= 0f && ca <= 0f))
                        {
                            if (cut) { Clear(x, y); } else { Blend(x, y, colour, 1f); }
                        }
                    }
                }
            }

            private static float Side(Vector2 p, Vector2 a, Vector2 b) =>
                (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);

            /// <summary>A tapered line, as overlapping discs.</summary>
            public void Stroke(Vector2 a, Vector2 b, float widthA, float widthB, Color32 c, float alpha,
                bool onlyOver = false)
            {
                var steps = Mathf.CeilToInt(Vector2.Distance(a, b) / 1.5f) + 1;
                // Cover each pixel once: strokes are opaque shapes, and overlapping translucent
                // discs would stack into a darker line.
                var done = new System.Collections.Generic.HashSet<int>();
                for (var i = 0; i <= steps; i++)
                {
                    var t = i / (float)steps;
                    var q = Vector2.Lerp(a, b, t);
                    var r = Mathf.Lerp(widthA, widthB, t) * 0.5f;
                    for (var y = (int)(q.y - r - 1); y <= q.y + r + 1; y++)
                    {
                        for (var x = (int)(q.x - r - 1); x <= q.x + r + 1; x++)
                        {
                            var cover = Mathf.Clamp01(r + 0.5f - Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), q));
                            if (cover <= 0f) { continue; }
                            var key = y * 65536 + (((x % 65536) + 65536) % 65536);
                            if (cover >= 1f && !done.Add(key)) { continue; }
                            if (cover < 1f && done.Contains(key)) { continue; }
                            Blend(x, y, c, alpha * cover, onlyOver);
                        }
                    }
                }
            }

            /// <summary>
            /// Haze strongest at the bottom of the band. Over what is painted it lightens; with
            /// <paramref name="addAlpha"/> it also lays its own mist over empty space.
            /// </summary>
            public void HazeBand(float fromY, float toY, Color32 c, float strength, float addAlpha = 0f)
            {
                for (var y = (int)Mathf.Max(0, fromY); y < Mathf.Min(Height, toY); y++)
                {
                    var t = 1f - (y - fromY) / Mathf.Max(1f, toY - fromY);
                    var s = strength * t * t;
                    for (var x = 0; x < Width; x++)
                    {
                        var i = y * Width + x;
                        if (_px[i].a > 0)
                        {
                            Blend(x, y, c, s, onlyOver: true);
                        }
                        else if (addAlpha > 0f)
                        {
                            Blend(x, y, c, addAlpha * t * t);
                        }
                    }
                }
            }

            public void FadeTop(int from, int to)
            {
                for (var y = from; y < Mathf.Min(Height, to); y++)
                {
                    var k = 1f - (y - from) / (float)Mathf.Max(1, to - from);
                    k = k * k;
                    for (var x = 0; x < Width; x++)
                    {
                        var i = y * Width + x;
                        _px[i].a = (byte)Mathf.Round(_px[i].a * k);
                    }
                }
            }

            public void FadeSides(float band)
            {
                for (var x = 0; x < Width; x++)
                {
                    var edge = Mathf.Min(x, Width - 1 - x);
                    if (edge >= band) { continue; }
                    var k = Mathf.SmoothStep(0f, 1f, edge / band);
                    for (var y = 0; y < Height; y++)
                    {
                        var i = y * Width + x;
                        _px[i].a = (byte)Mathf.Round(_px[i].a * k);
                    }
                }
            }

            public Canvas ToCanvas()
            {
                var canvas = new Canvas(Width, Height);
                for (var y = 0; y < Height; y++)
                {
                    for (var x = 0; x < Width; x++)
                    {
                        var c = _px[y * Width + x];
                        if (c.a > 0)
                        {
                            canvas.Set(x, y, c);
                        }
                    }
                }

                return canvas;
            }
        }
    }
}
