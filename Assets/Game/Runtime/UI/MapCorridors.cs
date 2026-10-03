using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>A short corridor between two rooms of a zone map, in map units (y up).</summary>
    public readonly struct MapCorridor
    {
        public readonly bool IsHorizontal;
        /// <summary>The wall it leaves and the wall it enters, on its centre line.</summary>
        public readonly Vector2 From;
        public readonly Vector2 To;
        public readonly float Width;

        public MapCorridor(bool horizontal, Vector2 from, Vector2 to, float width)
        {
            IsHorizontal = horizontal;
            From = from;
            To = to;
            Width = width;
        }

        public Vector2 Middle => (From + To) * 0.5f;
    }

    /// <summary>
    /// Where a passage between two rooms is drawn (the user's correction of 2026-10-03: no lines
    /// from centre to centre across other rooms, no door hanging in the air). Rooms side by side get
    /// a level corridor through the middle of the height they share, rooms one above the other an
    /// upright one through the width they share; its two ends are the facing walls, where the doors
    /// stand. Rooms that share neither are joined at their nearest corners' facing walls.
    /// </summary>
    public static class MapCorridors
    {
        public static MapCorridor Between(Rect a, Rect b, float width)
        {
            var apartX = a.xMax <= b.xMin || b.xMax <= a.xMin;
            var apartY = a.yMax <= b.yMin || b.yMax <= a.yMin;
            var sharedY = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
            var sharedX = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);
            bool horizontal;
            if (apartX && sharedY > 0f) { horizontal = true; }
            else if (apartY && sharedX > 0f) { horizontal = false; }
            else if (apartX && apartY) { horizontal = Gap(a.xMin, a.xMax, b.xMin, b.xMax) >= Gap(a.yMin, a.yMax, b.yMin, b.yMax); }
            else { horizontal = Mathf.Abs(a.center.x - b.center.x) >= Mathf.Abs(a.center.y - b.center.y); }

            if (horizontal)
            {
                var left = a.xMin < b.xMin ? a : b;
                var right = left == a ? b : a;
                var low = Mathf.Max(a.yMin, b.yMin);
                var high = Mathf.Min(a.yMax, b.yMax);
                var y = high > low ? Centre(low, high, width) : (left.center.y + right.center.y) * 0.5f;
                var corridor = new MapCorridor(true, new Vector2(left.xMax, y), new Vector2(right.xMin, y), width);
                return left == a ? corridor : Flip(corridor);
            }

            var lower = a.yMin < b.yMin ? a : b;
            var upper = lower == a ? b : a;
            var l = Mathf.Max(a.xMin, b.xMin);
            var r = Mathf.Min(a.xMax, b.xMax);
            var x = r > l ? Centre(l, r, width) : (lower.center.x + upper.center.x) * 0.5f;
            var upright = new MapCorridor(false, new Vector2(x, lower.yMax), new Vector2(x, upper.yMin), width);
            return lower == a ? upright : Flip(upright);
        }

        /// <summary>The middle of a shared span, kept a corridor's width off its ends.</summary>
        private static float Centre(float from, float to, float width)
        {
            var middle = (from + to) * 0.5f;
            return to - from > width * 2f ? Mathf.Clamp(middle, from + width, to - width) : middle;
        }

        private static float Gap(float aMin, float aMax, float bMin, float bMax) =>
            Mathf.Max(bMin - aMax, aMin - bMax);

        private static MapCorridor Flip(MapCorridor c) => new MapCorridor(c.IsHorizontal, c.To, c.From, c.Width);
    }
}
