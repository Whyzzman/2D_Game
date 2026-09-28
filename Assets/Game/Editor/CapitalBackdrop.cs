using UnityEngine;
using Paint = ForgottenKingdom.EditorTools.ForestBackdrop.Paint;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The Ashen Capital's three parallax layers, drawn to the Forest's layout (2048 × 2000, the
    /// floor four units up) so they are placed and fitted the same way. Far: a bruised red sky,
    /// the palace on its hill with smoke going up. Mid: roofs and towers of the burnt city. Near:
    /// arcades and columns close behind the street. Every layer is darker and flatter than the
    /// ground; the one warm thing is the glow low on the horizon.
    /// </summary>
    public static class CapitalBackdrop
    {
        private const int Width = ForestBackdrop.Width;
        private const int Height = ForestBackdrop.Height;
        private const int Floor = 400;

        private static readonly Color32 SkyTop = ForestBackdrop.SkyTop;
        private static readonly Color32 SkyHigh = new Color32(26, 18, 22, 255);
        private static readonly Color32 SkyLow = new Color32(78, 38, 32, 255);
        private static readonly Color32 Glow = new Color32(150, 70, 40, 255);
        private static readonly Color32 Smoke = new Color32(56, 44, 46, 255);
        private static readonly Color32 Palace = new Color32(28, 22, 25, 255);
        private static readonly Color32 Roofs = new Color32(34, 28, 30, 255);
        private static readonly Color32 RoofRim = new Color32(78, 52, 44, 255);
        private static readonly Color32 NearStone = new Color32(26, 23, 24, 255);
        private static readonly Color32 NearRim = new Color32(70, 60, 56, 255);
        private static readonly Color32 Window = new Color32(110, 50, 30, 255);

        public static Canvas Layer(int depth)
        {
            switch (depth)
            {
                case 0: return Far();
                case 1: return Mid();
                default: return Near();
            }
        }

        private static Canvas Far()
        {
            var p = new Paint(Width, Height, wrap: true);
            for (var y = 0; y < Height; y++)
            {
                var t = Mathf.Clamp01((y - Floor) / (float)(Height - Floor));
                var low = Color32.Lerp(SkyLow, SkyHigh, Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, t * 2f)));
                var colour = Color32.Lerp(low, SkyTop, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.4f, 1f, t)));
                p.Rect(0f, y, Width, y + 1, y == Height - 1 ? SkyTop : colour, 1f);
            }

            // The fire that never quite went out, low along the horizon.
            p.HazeBand(Floor, Floor + 420f, Glow, 0.5f);
            var rng = new System.Random(3601);
            // Smoke columns rising and leaning with the wind.
            for (var i = 0; i < 5; i++)
            {
                var x = 200f + i * 400f + rng.Next(-60, 60);
                for (var k = 0; k < 18; k++)
                {
                    var c = new Vector2(x + k * 14f, Floor + 250f + k * 55f);
                    p.SoftEllipse(c, 60f + k * 6f, 40f + k * 3f, Smoke, 0.12f);
                }
            }

            // The palace on its hill: towers, a dome caved in.
            for (var x = 0; x < Width; x++)
            {
                var hill = Floor + 140f + 80f * Mathf.Exp(-Mathf.Pow((x - 1100f) / 420f, 2f))
                    + 16f * Mathf.Sin(x * 0.01f);
                p.FillColumn(x, 0, hill, Palace, 1f);
            }

            p.Rect(960f, Floor + 200f, 1240f, Floor + 420f, Palace, 1f);
            p.Disc(new Vector2(1100f, Floor + 420f), 110f, Palace, 1f);
            // The caved-in dome: sky showing through, never a hole in the sky layer itself.
            p.Disc(new Vector2(1150f, Floor + 470f), 50f, Color32.Lerp(SkyLow, Glow, 0.3f), 1f);
            foreach (var (x, h) in new[] { (930f, 380f), (1270f, 320f), (1020f, 470f) })
            {
                p.Rect(x - 22f, Floor + 200f, x + 22f, Floor + 200f + h, Palace, 1f);
                p.Triangle(new Vector2(x - 26f, Floor + 200f + h), new Vector2(x + 26f, Floor + 200f + h),
                    new Vector2(x, Floor + 260f + h), Palace);
            }

            p.HazeBand(Floor, Floor + 200f, Smoke, 0.35f);
            return p.ToCanvas();
        }

        private static Canvas Mid()
        {
            var p = new Paint(Width, Height, wrap: true);
            var rng = new System.Random(3602);
            var x = 0f;
            while (x < Width)
            {
                var w = rng.Next(90, 200);
                var h = rng.Next(180, 420);
                p.Rect(x, 0, x + w, Floor + h, Roofs, 1f);
                if (rng.Next(0, 2) == 0)
                {
                    p.Triangle(new Vector2(x, Floor + h), new Vector2(x + w, Floor + h),
                        new Vector2(x + w * 0.5f, Floor + h + w * 0.45f), Roofs);
                }

                p.Rect(x, Floor + h - 4f, x + w, Floor + h, RoofRim, 0.6f);
                // A few windows still lit from inside by embers.
                for (var i = 0; i < 3; i++)
                {
                    if (rng.Next(0, 4) != 0) { continue; }
                    var wx = x + rng.Next(10, Mathf.Max(11, w - 30));
                    var wy = Floor + rng.Next(40, Mathf.Max(41, h - 60));
                    p.Rect(wx, wy, wx + 16f, wy + 26f, Window, 0.7f);
                }

                x += w + rng.Next(-10, 30);
            }

            // Two bell towers standing above the roofs.
            foreach (var tx in new[] { 520f, 1580f })
            {
                p.Rect(tx - 40f, 0, tx + 40f, Floor + 720f, Roofs, 1f);
                p.Triangle(new Vector2(tx - 48f, Floor + 720f), new Vector2(tx + 48f, Floor + 720f),
                    new Vector2(tx, Floor + 860f), Roofs);
                p.Rect(tx - 16f, Floor + 560f, tx + 16f, Floor + 640f, SkyLow, 1f);
            }

            p.HazeBand(Floor, Floor + 260f, Smoke, 0.45f);
            return p.ToCanvas();
        }

        private static Canvas Near()
        {
            var p = new Paint(Width, Height, wrap: true);
            var rng = new System.Random(3603);
            // An arcade: round arches on square piers, broken here and there.
            const float bay = 256f;
            for (var i = 0; i < Width / bay; i++)
            {
                var x = i * bay;
                var pier = 46f;
                var whole = rng.Next(0, 4) != 0;
                var top = whole ? Floor + 520f : Floor + rng.Next(160, 360);
                p.Rect(x, 0, x + pier, top, NearStone, 1f);
                p.Rect(x, 0, x + 4f, top, NearRim, 0.6f);
                if (whole)
                {
                    var centre = new Vector2(x + bay * 0.5f + pier * 0.5f, Floor + 520f);
                    for (var y = (int)(Floor + 520f); y < Floor + 520f + bay * 0.6f; y++)
                    {
                        for (var px = (int)x; px < x + bay + pier; px++)
                        {
                            var d = Vector2.Distance(new Vector2(px, y), centre);
                            if (d >= bay * 0.5f - 6f && d <= bay * 0.5f + 34f)
                            {
                                p.Blend(px, y, NearStone, 1f);
                            }
                        }
                    }
                }
            }

            p.Rect(0, 0, Width, Floor + 30f, NearStone, 1f);
            p.FadeTop((int)(Floor + 900f), Height);
            return p.ToCanvas();
        }
    }
}
