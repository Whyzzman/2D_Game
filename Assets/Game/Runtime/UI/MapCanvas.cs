using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgottenKingdom
{
    /// <summary>The map's symbols (design/ui/map/map.html), shared by the rooms and the legend.</summary>
    public enum MapGlyph
    {
        Altar,
        Travel,
        Boss,
        Secret,
        Gate,
        Player,
        Here
    }

    /// <summary>
    /// Draws a <see cref="WorldMapView"/> as the approved map sheet: cartography rather than a
    /// navigator — dark stone, faint route lines, room silhouettes, gold only where the player is
    /// and at the altars. Rooms, links and symbols come from the same coordinates as the
    /// WorldGraph asset; names are labels laid over the drawing, shown only where they fit.
    /// </summary>
    public sealed class MapCanvas : VisualElement
    {
        private static readonly Color Grid = new Color(138f / 255f, 116f / 255f, 52f / 255f, 0.06f);
        private static readonly Color Route = new Color32(138, 116, 52, 255);
        private static readonly Color Border = new Color32(201, 161, 74, 255);
        private static readonly Color Shut = new Color32(107, 90, 66, 255);
        private static readonly Color Dim = new Color32(95, 87, 76, 255);
        private static readonly Color Fog = new Color(142f / 255f, 131f / 255f, 117f / 255f, 0.35f);
        private static readonly Color FogFill = new Color(90f / 255f, 80f / 255f, 64f / 255f, 0.10f);
        private static readonly Color Selection = new Color32(232, 205, 138, 255);

        private readonly List<Label> _labels = new List<Label>();
        private WorldMapView _view;
        private Rect _visible;
        private string _selected = string.Empty;
        private bool _showLabels;

        public MapCanvas()
        {
            AddToClassList("map-canvas");
            generateVisualContent += Draw;
            RegisterCallback<GeometryChangedEvent>(_ => PlaceLabels());
        }

        /// <summary>Screen pixels per map unit right now.</summary>
        public float Scale
        {
            get
            {
                var rect = contentRect;
                return _visible.width > 0f && _visible.height > 0f
                    ? Mathf.Min(rect.width / _visible.width, rect.height / _visible.height)
                    : 1f;
            }
        }

        public void Show(WorldMapView view, Rect visible, string selectedAreaId, bool showLabels)
        {
            _view = view;
            _visible = visible;
            _selected = selectedAreaId ?? string.Empty;
            _showLabels = showLabels;
            PlaceLabels();
            MarkDirtyRepaint();
        }

        public Vector2 ToScreen(Vector2 point)
        {
            var rect = contentRect;
            var scale = Scale;
            var offset = new Vector2((rect.width - _visible.width * scale) * 0.5f,
                (rect.height - _visible.height * scale) * 0.5f);
            return new Vector2(offset.x + (point.x - _visible.xMin) * scale,
                offset.y + (_visible.yMax - point.y) * scale);
        }

        /// <summary>The on-screen box of a map rectangle (map y points up, screen y down).</summary>
        public Rect ToScreen(Rect area)
        {
            var topLeft = ToScreen(new Vector2(area.xMin, area.yMax));
            var bottomRight = ToScreen(new Vector2(area.xMax, area.yMin));
            return Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
        }

        /// <summary>The discovered room under a point of this element, for mouse clicks.</summary>
        public string AreaAt(Vector2 local)
        {
            if (_view == null)
            {
                return null;
            }

            foreach (var node in _view.Nodes)
            {
                if (node.State == MapNodeState.Discovered && ToScreen(node.Rect).Contains(local))
                {
                    return node.AreaId;
                }
            }

            return null;
        }

        private void PlaceLabels()
        {
            foreach (var label in _labels)
            {
                label.RemoveFromHierarchy();
            }

            _labels.Clear();
            if (_view == null || !_showLabels || contentRect.width < 1f)
            {
                return;
            }

            foreach (var node in _view.Nodes)
            {
                var box = ToScreen(node.Rect);
                var name = node.Label;
                // A name that does not fit is not squeezed in: the selected room's panel carries it.
                if (string.IsNullOrEmpty(name) || name.Length * 7.4f > box.width - 16f || box.height < 26f)
                {
                    continue;
                }

                var label = UiParts.Text(name, "map-canvas__label");
                label.style.left = box.x + 8f;
                label.style.top = box.y + 6f;
                Add(label);
                _labels.Add(label);
            }
        }

        private void Draw(MeshGenerationContext context)
        {
            var painter = context.painter2D;
            var rect = contentRect;
            DrawGrid(painter, rect);
            if (_view == null)
            {
                return;
            }

            foreach (var link in _view.Links)
            {
                DrawLink(painter, link);
            }

            foreach (var node in _view.Nodes)
            {
                DrawRoom(painter, node);
            }
        }

        private static void DrawGrid(Painter2D painter, Rect rect)
        {
            painter.strokeColor = Grid;
            painter.lineWidth = 1f;
            painter.BeginPath();
            for (var x = 199.5f; x < rect.width; x += 200f)
            {
                painter.MoveTo(new Vector2(x, 0f));
                painter.LineTo(new Vector2(x, rect.height));
            }

            for (var y = 199.5f; y < rect.height; y += 200f)
            {
                painter.MoveTo(new Vector2(0f, y));
                painter.LineTo(new Vector2(rect.width, y));
            }

            painter.Stroke();
        }

        private void DrawLink(Painter2D painter, MapLink link)
        {
            var from = _view.Node(link.Connection.FromAreaId);
            var to = _view.Node(link.Connection.ToAreaId);
            if (from == null || to == null)
            {
                return;
            }

            var a = ToScreen(from.Area.MapPosition);
            var b = ToScreen(to.Area.MapPosition);
            if (link.Connection.IsShortcut)
            {
                // A shortcut runs beside the main route between the same rooms, not over it.
                a.y -= 6f;
                b.y -= 6f;
            }

            switch (link.State)
            {
                case MapLinkState.Open:
                    Line(painter, a, b, link.IsZoneTransition ? Border : Route, link.IsZoneTransition ? 3.2f : 1.4f);
                    break;
                case MapLinkState.ShortcutLocked:
                    Dashed(painter, a, b, Shut, 1.4f, 4f, 5f);
                    break;
                case MapLinkState.Blocked:
                    Dashed(painter, a, b, Dim, 1.2f, 2f, 4f);
                    break;
                default:
                    Dashed(painter, a, b, Dim, 1.4f, 6f, 6f);
                    break;
            }

            var middle = (a + b) * 0.5f;
            if (link.IsOneWay && link.State == MapLinkState.Open)
            {
                var direction = (b - a).normalized;
                var normal = new Vector2(-direction.y, direction.x);
                var tip = middle - normal * 10f + direction * 4f;
                painter.strokeColor = Route;
                painter.lineWidth = 1.3f;
                painter.BeginPath();
                painter.MoveTo(tip - direction * 8f + normal * 4f);
                painter.LineTo(tip);
                painter.LineTo(tip - direction * 8f - normal * 4f);
                painter.Stroke();
            }

            if (link.ShowsGateIcon || link.State == MapLinkState.Sealed)
            {
                DrawGlyph(painter, MapGlyph.Gate, middle, true);
            }
        }

        private void DrawRoom(Painter2D painter, MapNode node)
        {
            var box = ToScreen(node.Rect);
            if (node.State == MapNodeState.Unexplored)
            {
                Rectangle(painter, box);
                painter.fillColor = FogFill;
                painter.Fill();
                DashedRect(painter, box, Fog, 1f, 3f, 5f);
                return;
            }

            var tint = Tint(node.Area.ZoneId);
            Rectangle(painter, box);
            painter.fillColor = new Color(tint.r, tint.g, tint.b, 0.16f);
            painter.Fill();
            painter.strokeColor = new Color(tint.r, tint.g, tint.b, 0.75f);
            painter.lineWidth = 1.2f;
            painter.Stroke();

            var at = new Vector2(box.xMax - 16f, box.yMax - 16f);
            foreach (var icon in node.Icons)
            {
                var glyph = GlyphFor(icon.Type);
                if (glyph == null)
                {
                    continue;
                }

                DrawGlyph(painter, glyph.Value, at, icon.IsActive);
                at.x -= 26f;
            }

            if (node.IsCurrent)
            {
                DrawGlyph(painter, MapGlyph.Player, box.center + new Vector2(0f, 4f), true);
            }

            if (node.AreaId == _selected)
            {
                var ring = new Rect(box.x - 4f, box.y - 4f, box.width + 8f, box.height + 8f);
                Rectangle(painter, ring);
                painter.strokeColor = Selection;
                painter.lineWidth = 2f;
                painter.Stroke();
            }
        }

        private static MapGlyph? GlyphFor(MapIconType type)
        {
            switch (type)
            {
                case MapIconType.Checkpoint: return MapGlyph.Altar;
                case MapIconType.FastTravel: return MapGlyph.Travel;
                case MapIconType.Boss: return MapGlyph.Boss;
                case MapIconType.Secret: return MapGlyph.Secret;
                case MapIconType.Gate:
                case MapIconType.SpecialDoor: return MapGlyph.Gate;
                default: return null;
            }
        }

        /// <summary>Zone tints from the mockup: bone for the Tomb, moss for the Forest.</summary>
        private static Color Tint(string zoneId)
        {
            zoneId ??= string.Empty;
            if (zoneId.Contains("tomb")) { return new Color32(184, 168, 138, 255); }
            if (zoneId.Contains("forest")) { return new Color32(143, 168, 120, 255); }
            return new Color32(106, 106, 112, 255);
        }

        /// <summary>One symbol, centred on a point, in the mockup's pixel sizes.</summary>
        public static void DrawGlyph(Painter2D painter, MapGlyph glyph, Vector2 c, bool active)
        {
            var gold = new Color32(232, 205, 138, 255);
            var flame = new Color32(240, 180, 65, 255);
            var dim = new Color32(111, 100, 82, 255);
            painter.lineJoin = LineJoin.Round;
            switch (glyph)
            {
                case MapGlyph.Altar:
                    Poly(painter, c, 0f, -9f, 6f, 0f, 0f, 9f, -6f, 0f);
                    if (active) { painter.fillColor = flame; painter.Fill(); }
                    painter.strokeColor = active ? (Color)gold : dim;
                    painter.lineWidth = 1.5f;
                    painter.Stroke();
                    break;
                case MapGlyph.Travel:
                    painter.BeginPath();
                    painter.Arc(c, 9f, Angle.Degrees(0f), Angle.Degrees(360f));
                    painter.strokeColor = gold;
                    painter.lineWidth = 1.5f;
                    painter.Stroke();
                    Poly(painter, c, 0f, -5f, 4f, 0f, 0f, 5f, -4f, 0f);
                    painter.fillColor = flame;
                    painter.Fill();
                    break;
                case MapGlyph.Boss:
                    Poly(painter, c, -9f, 6f, -9f, -6f, -4f, -1f, 0f, -8f, 4f, -1f, 9f, -6f, 9f, 6f);
                    painter.strokeColor = active ? new Color32(192, 48, 63, 255) : dim;
                    painter.lineWidth = 1.6f;
                    painter.Stroke();
                    if (!active)
                    {
                        Line(painter, c + new Vector2(-10f, 8f), c + new Vector2(10f, -8f), dim, 1.4f);
                    }

                    break;
                case MapGlyph.Secret:
                    Poly(painter, c, 0f, -8f, 2.5f, -2.5f, 8f, 0f, 2.5f, 2.5f, 0f, 8f, -2.5f, 2.5f, -8f, 0f, -2.5f, -2.5f);
                    painter.fillColor = new Color32(155, 143, 106, 255);
                    painter.Fill();
                    break;
                case MapGlyph.Gate:
                    Rectangle(painter, new Rect(c.x - 7f, c.y - 7f, 14f, 14f));
                    painter.fillColor = new Color32(15, 13, 11, 255);
                    painter.Fill();
                    painter.strokeColor = new Color32(142, 131, 117, 255);
                    painter.lineWidth = 1.2f;
                    painter.Stroke();
                    Line(painter, c + new Vector2(-3f, -7f), c + new Vector2(-3f, 7f), new Color32(142, 131, 117, 255), 1.2f);
                    Line(painter, c + new Vector2(3f, -7f), c + new Vector2(3f, 7f), new Color32(142, 131, 117, 255), 1.2f);
                    break;
                case MapGlyph.Player:
                    painter.BeginPath();
                    painter.Arc(c, 14f, Angle.Degrees(0f), Angle.Degrees(360f));
                    painter.strokeColor = new Color(232f / 255f, 205f / 255f, 138f / 255f, 0.45f);
                    painter.lineWidth = 1f;
                    painter.Stroke();
                    Poly(painter, c, 0f, -10f, 7f, 0f, 0f, 10f, -7f, 0f);
                    painter.fillColor = gold;
                    painter.Fill();
                    painter.strokeColor = new Color32(11, 9, 7, 255);
                    painter.lineWidth = 1.5f;
                    painter.Stroke();
                    break;
                default:
                    Poly(painter, c, 0f, -8f, 6f, 0f, 0f, 8f, -6f, 0f);
                    painter.fillColor = gold;
                    painter.Fill();
                    break;
            }
        }

        private static void Poly(Painter2D painter, Vector2 c, params float[] xy)
        {
            painter.BeginPath();
            painter.MoveTo(c + new Vector2(xy[0], xy[1]));
            for (var i = 2; i + 1 < xy.Length; i += 2)
            {
                painter.LineTo(c + new Vector2(xy[i], xy[i + 1]));
            }

            painter.ClosePath();
        }

        private static void Rectangle(Painter2D painter, Rect box)
        {
            painter.BeginPath();
            painter.MoveTo(new Vector2(box.xMin, box.yMin));
            painter.LineTo(new Vector2(box.xMax, box.yMin));
            painter.LineTo(new Vector2(box.xMax, box.yMax));
            painter.LineTo(new Vector2(box.xMin, box.yMax));
            painter.ClosePath();
        }

        private static void Line(Painter2D painter, Vector2 a, Vector2 b, Color color, float width)
        {
            painter.strokeColor = color;
            painter.lineWidth = width;
            painter.BeginPath();
            painter.MoveTo(a);
            painter.LineTo(b);
            painter.Stroke();
        }

        /// <summary>Painter2D has no dash pattern; the dashes are laid by hand.</summary>
        private static void Dashed(Painter2D painter, Vector2 a, Vector2 b, Color color, float width,
            float on, float off)
        {
            var length = Vector2.Distance(a, b);
            if (length < 0.5f)
            {
                return;
            }

            var direction = (b - a) / length;
            painter.strokeColor = color;
            painter.lineWidth = width;
            painter.BeginPath();
            for (var t = 0f; t < length; t += on + off)
            {
                painter.MoveTo(a + direction * t);
                painter.LineTo(a + direction * Mathf.Min(length, t + on));
            }

            painter.Stroke();
        }

        private static void DashedRect(Painter2D painter, Rect box, Color color, float width, float on, float off)
        {
            var a = new Vector2(box.xMin, box.yMin);
            var b = new Vector2(box.xMax, box.yMin);
            var c = new Vector2(box.xMax, box.yMax);
            var d = new Vector2(box.xMin, box.yMax);
            Dashed(painter, a, b, color, width, on, off);
            Dashed(painter, b, c, color, width, on, off);
            Dashed(painter, c, d, color, width, on, off);
            Dashed(painter, d, a, color, width, on, off);
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
                MapCanvas.DrawGlyph(context.painter2D, _glyph, contentRect.center, _active);
        }
    }
}
