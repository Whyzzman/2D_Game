using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgottenKingdom
{
    /// <summary>The map's symbols (design/ui/world-map/world-map.html), shared by both maps, the legend and the travel list.</summary>
    public enum MapGlyph
    {
        Altar,
        Travel,
        Boss,
        Secret,
        Gate,
        Player,
        /// <summary>The quill's mark, and «you are here» in the travel list.</summary>
        Here,
        /// <summary>A place a bought rumour points to.</summary>
        Rumour,
        Unknown,
        ShortcutOpen,
        ShortcutShut,
        /// <summary>A way that wants an ability the hero has not got.</summary>
        Ability,
        /// <summary>Grapple rings on the way.</summary>
        Ring,
        RoomExplored,
        RoomUnexplored,
        PathOpen,
        PathHidden,
        /// <summary>A beaten boss: the same skull, its light gone.</summary>
        BossBeaten
    }

    /// <summary>
    /// Draws the mockup's symbols with Painter2D. The shapes are the mockup's own SVG paths, read
    /// by <see cref="Path"/>, at the mockup's 24-pixel size scaled to the one asked for, so the
    /// game's map and the approved one share every line.
    /// </summary>
    public static class MapIcons
    {
        public static readonly Color Ink = new Color32(21, 19, 16, 255);
        public static readonly Color Bone = new Color32(216, 203, 180, 255);
        public static readonly Color Gold = new Color32(201, 161, 74, 255);
        public static readonly Color GoldBright = new Color32(232, 205, 138, 255);
        public static readonly Color Dim = new Color32(95, 87, 76, 255);

        /// <summary>The quill's five colours, in the order the marks take them.</summary>
        public static readonly Color[] MarkColors =
        {
            new Color32(196, 85, 47, 255),
            new Color32(111, 168, 214, 255),
            new Color32(127, 168, 92, 255),
            new Color32(201, 161, 74, 255),
            new Color32(216, 203, 180, 255)
        };

        public static void Draw(Painter2D painter, MapGlyph glyph, Vector2 c, float size = 24f, bool active = true)
        {
            var s = size / 24f;
            painter.lineJoin = LineJoin.Round;
            painter.lineCap = LineCap.Round;
            switch (glyph)
            {
                case MapGlyph.Boss:
                case MapGlyph.BossBeaten:
                {
                    var on = glyph == MapGlyph.Boss && active;
                    Circle(painter, c, 13f * s, on ? Hex("5a1018") : Hex("2a2522"), on ? Hex("e04a4a") : Dim, 1.6f * s);
                    Path(painter, "M-6,-2 Q-6,-9 0,-9 Q6,-9 6,-2 L5,3 L2,3 L2,6 L-2,6 L-2,3 L-5,3 Z", c, s);
                    painter.fillColor = on ? Hex("f2c7c0") : Dim;
                    painter.Fill();
                    Circle(painter, c + new Vector2(-2.6f, -2.5f) * s, 1.7f * s, on ? Hex("5a1018") : Hex("2a2522"), null, 0f);
                    Circle(painter, c + new Vector2(2.6f, -2.5f) * s, 1.7f * s, on ? Hex("5a1018") : Hex("2a2522"), null, 0f);
                    Path(painter, "M-9,-8 L-6,-12 L-5,-7 M9,-8 L6,-12 L5,-7", c, s);
                    Stroke(painter, on ? Hex("e04a4a") : Dim, 1.4f * s);
                    break;
                }
                case MapGlyph.Altar:
                case MapGlyph.Travel:
                {
                    var lit = active;
                    if (glyph == MapGlyph.Travel && lit)
                    {
                        Circle(painter, c, 17f * s, null, new Color(0.5f, 0.77f, 1f, 0.35f), 1f * s);
                    }

                    Circle(painter, c, 13f * s, lit ? Hex("132433") : Hex("1d2228"), lit ? Hex("7fc4ff") : Dim, 1.6f * s);
                    Path(painter, "M0,-9 L5,-2 L3,8 L-3,8 L-5,-2 Z", c, s);
                    Stroke(painter, lit ? Hex("bfe3ff") : Dim, 1.6f * s);
                    Path(painter, "M0,-4 L2,1 L0,4 L-2,1 Z", c, s);
                    painter.fillColor = lit ? Hex("e9f6ff") : Dim;
                    painter.Fill();
                    break;
                }
                case MapGlyph.Player:
                    Circle(painter, c, 20f * s, new Color(0.94f, 0.71f, 0.25f, 0.18f), null, 0f);
                    Path(painter, "M0,-15 L7,0 L0,15 L-7,0 Z", c, s);
                    painter.fillColor = Hex("f0b441");
                    painter.Fill();
                    Stroke(painter, Hex("fff1c4"), 1.3f * s);
                    Path(painter, "M0,-8 L3,0 L0,8 L-3,0 Z", c, s);
                    painter.fillColor = Hex("fff6d8");
                    painter.Fill();
                    break;
                case MapGlyph.Here:
                    Path(painter, "M0,0 L-9,-22 A10,10 0 1 1 9,-22 Z", c + new Vector2(0f, 11f) * s, s);
                    painter.fillColor = Hex("3e6fa8");
                    painter.Fill();
                    Stroke(painter, Hex("bfe3ff"), 1.5f * s);
                    Circle(painter, c + new Vector2(0f, -16f) * s, 3.5f * s, Hex("bfe3ff"), null, 0f);
                    break;
                case MapGlyph.Gate:
                    RoundRect(painter, new Rect(c.x - 10f * s, c.y - 11f * s, 20f * s, 22f * s), 3f * s);
                    painter.fillColor = Hex("1b150d");
                    painter.Fill();
                    Stroke(painter, Gold, 1.5f * s);
                    Path(painter, "M-5,10 L-5,-2 Q-5,-7 0,-7 Q5,-7 5,-2 L5,10 M-2,10 L-2,-1 M2,10 L2,-1", c, s);
                    Stroke(painter, GoldBright, 1.4f * s);
                    break;
                case MapGlyph.ShortcutOpen:
                case MapGlyph.ShortcutShut:
                {
                    var open = glyph == MapGlyph.ShortcutOpen;
                    Path(painter, "M-8,-10 L8,-10 L8,10 L-8,10 Z", c, s);
                    painter.fillColor = open ? Hex("13293b") : Hex("14161a");
                    painter.Fill();
                    Stroke(painter, open ? Hex("7fc4ff") : Hex("5f6670"), 1.5f * s);
                    Path(painter, "M-8,-5 L8,-5 M-8,0 L8,0 M-8,5 L8,5", c, s);
                    Stroke(painter, open ? Hex("bfe3ff") : Hex("5f6670"), 1.4f * s);
                    break;
                }
                case MapGlyph.Rumour:
                    Circle(painter, c, 12f * s, Ink, Bone, 1.5f * s);
                    Path(painter, "M0,-6 L0,2 M0,5 L0,6", c, s);
                    Stroke(painter, Hex("f0e6d2"), 2.4f * s);
                    break;
                case MapGlyph.Unknown:
                    Circle(painter, c, 13f * s, Hex("121110"), null, 0f);
                    DashedCircle(painter, c, 13f * s, Dim, 1.4f * s, 10);
                    Path(painter, "M-3.5,-4 Q-3.5,-8 0,-8 Q4,-8 4,-4.5 Q4,-2 0,0 L0,2.5 M0,5.5 L0,6", c, s);
                    Stroke(painter, Hex("8e8375"), 1.8f * s);
                    break;
                case MapGlyph.Secret:
                    Dashed(painter, new[] { new Vector2(-11f, -9f), new Vector2(11f, -9f), new Vector2(11f, 9f), new Vector2(-11f, 9f), new Vector2(-11f, -9f) }, c, s, Gold, 1.2f * s, 3f * s, 2f * s);
                    Path(painter, "M-5,-1 Q-5,-6 0,-6 Q5,-6 5,-1 L4,3 L-4,3 Z", c, s);
                    painter.fillColor = Hex("e8dcc3");
                    painter.Fill();
                    Circle(painter, c + new Vector2(-2f, -1.5f) * s, 1.2f * s, Hex("1a1410"), null, 0f);
                    Circle(painter, c + new Vector2(2f, -1.5f) * s, 1.2f * s, Hex("1a1410"), null, 0f);
                    break;
                case MapGlyph.Ability:
                    Path(painter, "M0,-14 L14,0 L0,14 L-14,0 Z", c, s);
                    painter.fillColor = Hex("2b1d09");
                    painter.Fill();
                    Stroke(painter, Hex("f0a83a"), 1.6f * s);
                    Path(painter, "M0,-5 L0,5 M-4,-1 L0,-5 L4,-1", c, s);
                    Stroke(painter, Hex("ffd27a"), 1.8f * s);
                    break;
                case MapGlyph.Ring:
                    Circle(painter, c, 12f * s, Hex("0f2330"), Hex("6fd0ff"), 1.6f * s);
                    Circle(painter, c, 5f * s, null, Hex("bfeaff"), 2f * s);
                    Path(painter, "M0,5 L0,9", c, s);
                    Stroke(painter, Hex("bfeaff"), 2f * s);
                    break;
                case MapGlyph.RoomExplored:
                case MapGlyph.RoomUnexplored:
                    Path(painter, "M-14,0 L14,0", c, s);
                    Stroke(painter, Bone, 1.6f * s);
                    Circle(painter, c, 6f * s, glyph == MapGlyph.RoomExplored ? Bone : Ink, Bone, 1.6f * s);
                    if (glyph == MapGlyph.RoomExplored) { Circle(painter, c, 2.4f * s, Ink, null, 0f); }
                    break;
                case MapGlyph.PathOpen:
                    Path(painter, "M-14,0 L14,0", c, s);
                    Stroke(painter, Bone, 2.4f * s);
                    break;
                case MapGlyph.PathHidden:
                    Dashed(painter, new[] { new Vector2(-14f, 0f), new Vector2(14f, 0f) }, c, s, Hex("8e8375"), 1.8f * s, 2f * s, 5f * s);
                    break;
            }
        }

        /// <summary>A pin in one of the quill's colours, its point on <paramref name="c"/>'s foot.</summary>
        public static void DrawMark(Painter2D painter, Vector2 c, int color)
        {
            Path(painter, "M0,9 L-6,-1 L-4,-8 L4,-8 L6,-1 Z", c, 1f);
            painter.fillColor = MarkColors[Mathf.Clamp(color, 0, MarkColors.Length - 1)];
            painter.Fill();
            Stroke(painter, new Color32(11, 9, 7, 255), 1.2f);
        }

        // ---------- drawing helpers, shared with both canvases ----------

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var color);
            return color;
        }

        public static void Stroke(Painter2D painter, Color color, float width)
        {
            painter.strokeColor = color;
            painter.lineWidth = width;
            painter.Stroke();
        }

        public static void Circle(Painter2D painter, Vector2 c, float radius, Color? fill, Color? stroke, float width)
        {
            painter.BeginPath();
            // Without a point to start from, Painter2D draws the arc's lead-in from the origin.
            painter.MoveTo(c + new Vector2(radius, 0f));
            painter.Arc(c, radius, Angle.Degrees(0f), Angle.Degrees(360f));
            painter.ClosePath();
            if (fill.HasValue) { painter.fillColor = fill.Value; painter.Fill(); }
            if (stroke.HasValue && width > 0f) { Stroke(painter, stroke.Value, width); }
        }

        public static void RoundRect(Painter2D painter, Rect r, float radius)
        {
            painter.BeginPath();
            painter.MoveTo(new Vector2(r.xMin + radius, r.yMin));
            painter.ArcTo(new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax), radius);
            painter.ArcTo(new Vector2(r.xMax, r.yMax), new Vector2(r.xMin, r.yMax), radius);
            painter.ArcTo(new Vector2(r.xMin, r.yMax), new Vector2(r.xMin, r.yMin), radius);
            painter.ArcTo(new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), radius);
            painter.ClosePath();
        }

        public static void Rectangle(Painter2D painter, Rect box)
        {
            painter.BeginPath();
            painter.MoveTo(new Vector2(box.xMin, box.yMin));
            painter.LineTo(new Vector2(box.xMax, box.yMin));
            painter.LineTo(new Vector2(box.xMax, box.yMax));
            painter.LineTo(new Vector2(box.xMin, box.yMax));
            painter.ClosePath();
        }

        public static void Line(Painter2D painter, Vector2 a, Vector2 b, Color color, float width)
        {
            painter.BeginPath();
            painter.MoveTo(a);
            painter.LineTo(b);
            Stroke(painter, color, width);
        }

        /// <summary>Painter2D has no dash pattern; the dashes are laid by hand along a polyline.</summary>
        public static void Dashed(Painter2D painter, IReadOnlyList<Vector2> points, Vector2 offset, float scale,
            Color color, float width, float on, float off)
        {
            painter.BeginPath();
            for (var i = 0; i + 1 < points.Count; i++)
            {
                var a = offset + points[i] * scale;
                var b = offset + points[i + 1] * scale;
                var length = Vector2.Distance(a, b);
                if (length < 0.01f) { continue; }
                var direction = (b - a) / length;
                for (var t = 0f; t < length; t += on + off)
                {
                    painter.MoveTo(a + direction * t);
                    painter.LineTo(a + direction * Mathf.Min(length, t + on));
                }
            }

            Stroke(painter, color, width);
        }

        public static void DashedRect(Painter2D painter, Rect box, Color color, float width, float on, float off)
        {
            Dashed(painter, new[]
            {
                new Vector2(box.xMin, box.yMin), new Vector2(box.xMax, box.yMin), new Vector2(box.xMax, box.yMax),
                new Vector2(box.xMin, box.yMax), new Vector2(box.xMin, box.yMin)
            }, Vector2.zero, 1f, color, width, on, off);
        }

        private static void DashedCircle(Painter2D painter, Vector2 c, float radius, Color color, float width, int dashes)
        {
            painter.BeginPath();
            for (var i = 0; i < dashes; i++)
            {
                var a = i * 360f / dashes;
                painter.MoveTo(c + new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad)) * radius);
                painter.Arc(c, radius, Angle.Degrees(a), Angle.Degrees(a + 360f / dashes * 0.55f));
            }

            Stroke(painter, color, width);
        }

        /// <summary>
        /// Lays an SVG path (M, L, Q, C, A, Z, upper case — the mockup's own strings) on the painter,
        /// scaled by <paramref name="scale"/> around <paramref name="at"/>. Opens a new path; Z closes it.
        /// </summary>
        public static void Path(Painter2D painter, string svg, Vector2 at, float scale)
        {
            painter.BeginPath();
            var tokens = Tokens(svg);
            var i = 0;
            var command = 'M';
            var current = Vector2.zero;
            Vector2 P(int index) => at + new Vector2(tokens[index].number, tokens[index + 1].number) * scale;
            while (i < tokens.Count)
            {
                if (tokens[i].command != '\0')
                {
                    command = tokens[i].command;
                    i++;
                    if (command == 'Z')
                    {
                        painter.ClosePath();
                        continue;
                    }
                }

                switch (command)
                {
                    case 'M':
                        current = P(i); painter.MoveTo(current); i += 2; command = 'L';
                        break;
                    case 'L':
                        current = P(i); painter.LineTo(current); i += 2;
                        break;
                    case 'Q':
                    {
                        // As a cubic: Painter2D's QuadraticCurveTo started the shape at the origin
                        // (a sliver from the corner to the symbol), its BezierCurveTo does not.
                        var control = P(i);
                        var end = P(i + 2);
                        painter.BezierCurveTo(current + (control - current) * (2f / 3f), end + (control - end) * (2f / 3f), end);
                        current = end;
                        i += 4;
                        break;
                    }
                    case 'C':
                        painter.BezierCurveTo(P(i), P(i + 2), P(i + 4)); current = P(i + 4); i += 6;
                        break;
                    case 'A':
                    {
                        // Only the pin's arc is used: a circle through the current point and the end.
                        var end = P(i + 5);
                        var radius = tokens[i].number * scale;
                        var centre = (current + end) * 0.5f + new Vector2(0f, -Mathf.Sqrt(Mathf.Max(0f, radius * radius - (end - current).sqrMagnitude / 4f)));
                        var from = Mathf.Atan2(current.y - centre.y, current.x - centre.x) * Mathf.Rad2Deg;
                        var to = Mathf.Atan2(end.y - centre.y, end.x - centre.x) * Mathf.Rad2Deg;
                        if (to < from) { to += 360f; }
                        painter.Arc(centre, radius, Angle.Degrees(from), Angle.Degrees(to), ArcDirection.Clockwise);
                        current = end;
                        i += 7;
                        break;
                    }
                    default:
                        i++;
                        break;
                }
            }

        }

        private static List<(char command, float number)> Tokens(string svg)
        {
            var tokens = new List<(char, float)>();
            var i = 0;
            while (i < svg.Length)
            {
                var c = svg[i];
                if (char.IsLetter(c)) { tokens.Add((c, 0f)); i++; continue; }
                if (c == '-' || c == '.' || char.IsDigit(c))
                {
                    var start = i;
                    i++;
                    while (i < svg.Length && (char.IsDigit(svg[i]) || svg[i] == '.')) { i++; }
                    tokens.Add(('\0', float.Parse(svg.Substring(start, i - start), CultureInfo.InvariantCulture)));
                    continue;
                }

                i++;
            }

            return tokens;
        }
    }

    /// <summary>One map symbol on its own, for the legend and the travel list.</summary>
    public sealed class MapGlyphElement : VisualElement
    {
        private readonly MapGlyph _glyph;
        private readonly bool _active;

        public MapGlyphElement(MapGlyph glyph, bool active = true)
        {
            _glyph = glyph;
            _active = active;
            pickingMode = PickingMode.Ignore;
            AddToClassList("map-glyph");
            generateVisualContent += context =>
                MapIcons.Draw(context.painter2D, _glyph, contentRect.center, Mathf.Min(contentRect.width, contentRect.height) * 0.85f, _active);
        }
    }
}
