using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgottenKingdom
{
    /// <summary>
    /// One zone's map (design/ui/world-map/world-map.html, frame 2): rooms as painted halls cut out of
    /// the dark, each its own picture (<see cref="MapArt"/>, never repeated); between neighbours a
    /// short straight corridor from wall to facing wall (<see cref="MapCorridors"/>), a door on each
    /// wall, what bars the way on the corridor itself. Unexplored rooms and the ways to them are
    /// dashed; secrets a gold dashed hiding place; the chosen room has a gold frame.
    ///
    /// Drawn in three layers: corridors under everything, the rooms' pictures as children, then
    /// frames, doors and symbols on top. The altar's travel list shows the same canvas small.
    /// </summary>
    public sealed class MapCanvas : VisualElement
    {
        private static readonly Color Frame = new Color32(207, 211, 196, 230);
        private static readonly Color Shell = new Color32(11, 13, 12, 255);
        private static readonly Color DoorFill = new Color32(239, 230, 207, 255);
        private static readonly Color DoorDim = new Color32(85, 96, 90, 255);
        private static readonly Color FogLine = new Color32(125, 138, 128, 255);
        private static readonly Color FogFill = new Color(20f / 255f, 24f / 255f, 22f / 255f, 0.55f);
        private static readonly Color Secret = new Color32(201, 161, 74, 255);
        private static readonly Color BossFill = new Color32(74, 20, 26, 255);
        private const float CorridorWidth = 0.13f;

        private readonly VisualElement _rooms = new VisualElement { pickingMode = PickingMode.Ignore };
        private readonly VisualElement _top = new VisualElement { pickingMode = PickingMode.Ignore };
        private readonly Dictionary<string, VisualElement> _pictures = new Dictionary<string, VisualElement>();
        private WorldMapView _view;
        private Rect _visible;
        private string _selected = string.Empty;
        private string _zoneId;

        public MapCanvas()
        {
            AddToClassList("map-canvas");
            _rooms.AddToClassList("map-canvas__layer");
            _top.AddToClassList("map-canvas__layer");
            Add(_rooms);
            Add(_top);
            generateVisualContent += DrawCorridors;
            _top.generateVisualContent += DrawTop;
            RegisterCallback<GeometryChangedEvent>(_ => PlacePictures());
        }

        /// <summary>The rooms' pictures; without it the halls are drawn as plain tinted shapes.</summary>
        public MapArt Art { get; set; }

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

        /// <summary>
        /// Shows <paramref name="view"/> through <paramref name="visible"/>. With a zone, only that
        /// zone's rooms are drawn (its own map); without one, every room in view (the travel list).
        /// </summary>
        public void Show(WorldMapView view, Rect visible, string selectedAreaId, bool showLabels, string zoneId = null)
        {
            _view = view;
            _visible = visible;
            _selected = selectedAreaId ?? string.Empty;
            _zoneId = zoneId;
            PlacePictures();
            MarkDirtyRepaint();
            _top.MarkDirtyRepaint();
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

        /// <summary>The known room under a point of this element, for mouse clicks.</summary>
        public string AreaAt(Vector2 local)
        {
            if (_view == null)
            {
                return null;
            }

            foreach (var node in _view.Nodes)
            {
                if (node.IsKnown && Shown(node) && ToScreen(node.Rect).Contains(local))
                {
                    return node.AreaId;
                }
            }

            return null;
        }

        private bool Shown(MapNode node) =>
            node != null && node.State != MapNodeState.Hidden && (_zoneId == null || node.Area.ZoneId == _zoneId);

        private bool HasPicture(MapNode node) =>
            node.State == MapNodeState.Discovered && !node.Area.IsSecret && Art != null && Art.Room(node.AreaId) != null;

        // ---------- pictures ----------

        private void PlacePictures()
        {
            var keep = new HashSet<string>();
            if (_view != null && contentRect.width > 1f)
            {
                foreach (var node in _view.Nodes)
                {
                    if (!Shown(node) || !HasPicture(node))
                    {
                        continue;
                    }

                    if (!_pictures.TryGetValue(node.AreaId, out var picture))
                    {
                        picture = new VisualElement { pickingMode = PickingMode.Ignore };
                        picture.AddToClassList("map-canvas__room");
                        picture.style.backgroundImage = Art.Room(node.AreaId);
                        _rooms.Add(picture);
                        _pictures[node.AreaId] = picture;
                    }

                    var box = ToScreen(node.Rect);
                    picture.style.left = box.x;
                    picture.style.top = box.y;
                    picture.style.width = box.width;
                    picture.style.height = box.height;
                    keep.Add(node.AreaId);
                }
            }

            foreach (var id in new List<string>(_pictures.Keys))
            {
                if (!keep.Contains(id))
                {
                    _pictures[id].RemoveFromHierarchy();
                    _pictures.Remove(id);
                }
            }
        }

        // ---------- corridors (under the rooms) ----------

        private IEnumerable<(MapLink link, MapNode from, MapNode to, MapCorridor corridor)> Corridors()
        {
            if (_view == null)
            {
                yield break;
            }

            var seen = new HashSet<string>();
            foreach (var link in _view.Links)
            {
                var from = _view.Node(link.Connection.FromAreaId);
                var to = _view.Node(link.Connection.ToAreaId);
                if (link.State == MapLinkState.Hidden || !Shown(from) || !Shown(to))
                {
                    continue;
                }

                // Two connections between the same rooms (a drop and a shortcut) share one corridor.
                var pair = string.CompareOrdinal(from.AreaId, to.AreaId) < 0 ? from.AreaId + "|" + to.AreaId : to.AreaId + "|" + from.AreaId;
                if (!seen.Add(pair))
                {
                    continue;
                }

                yield return (link, from, to, MapCorridors.Between(from.Rect, to.Rect, CorridorWidth));
            }
        }

        private static bool IsFaint(MapLink link, MapNode from, MapNode to) =>
            link.State != MapLinkState.Open || from.State != MapNodeState.Discovered || to.State != MapNodeState.Discovered;

        private void DrawCorridors(MeshGenerationContext context)
        {
            var painter = context.painter2D;
            foreach (var (link, from, to, corridor) in Corridors())
            {
                var box = CorridorBox(corridor);
                if (IsFaint(link, from, to))
                {
                    MapIcons.Rectangle(painter, box);
                    painter.fillColor = FogFill;
                    painter.Fill();
                    MapIcons.DashedRect(painter, box, FogLine, 1.2f, 4f, 4f);
                    continue;
                }

                var tint = Tint(from.Area.ZoneId);
                var shell = corridor.IsHorizontal ? new Rect(box.x, box.y - 3f, box.width, box.height + 6f) : new Rect(box.x - 3f, box.y, box.width + 6f, box.height);
                MapIcons.Rectangle(painter, shell);
                painter.fillColor = Shell;
                painter.Fill();
                MapIcons.Rectangle(painter, box);
                painter.fillColor = tint;
                painter.Fill();
                // Two pale edges along it, as the passages of the zone maps have.
                if (corridor.IsHorizontal)
                {
                    MapIcons.Line(painter, new Vector2(box.xMin, box.yMin), new Vector2(box.xMax, box.yMin), Frame, 1.6f);
                    MapIcons.Line(painter, new Vector2(box.xMin, box.yMax), new Vector2(box.xMax, box.yMax), Frame, 1.6f);
                }
                else
                {
                    MapIcons.Line(painter, new Vector2(box.xMin, box.yMin), new Vector2(box.xMin, box.yMax), Frame, 1.6f);
                    MapIcons.Line(painter, new Vector2(box.xMax, box.yMin), new Vector2(box.xMax, box.yMax), Frame, 1.6f);
                }
            }
        }

        private Rect CorridorBox(MapCorridor corridor)
        {
            var a = ToScreen(corridor.From);
            var b = ToScreen(corridor.To);
            var width = Mathf.Clamp(corridor.Width * Scale, 7f, 22f);
            return corridor.IsHorizontal
                ? Rect.MinMaxRect(Mathf.Min(a.x, b.x), a.y - width * 0.5f, Mathf.Max(a.x, b.x), a.y + width * 0.5f)
                : Rect.MinMaxRect(a.x - width * 0.5f, Mathf.Min(a.y, b.y), a.x + width * 0.5f, Mathf.Max(a.y, b.y));
        }

        // ---------- frames, doors, symbols (over the rooms) ----------

        private void DrawTop(MeshGenerationContext context)
        {
            var painter = context.painter2D;
            if (_view == null)
            {
                return;
            }

            foreach (var node in _view.Nodes)
            {
                if (Shown(node)) { DrawRoom(painter, node); }
            }

            foreach (var (link, from, to, corridor) in Corridors())
            {
                var faint = IsFaint(link, from, to);
                Door(painter, corridor, true, faint);
                Door(painter, corridor, false, faint);
                var glyph = GlyphOf(link);
                if (glyph.HasValue)
                {
                    var box = CorridorBox(corridor);
                    MapIcons.Draw(painter, glyph.Value, box.center, Mathf.Clamp(Scale * 0.2f, 16f, 24f));
                }
            }

            foreach (var node in _view.Nodes)
            {
                if (Shown(node)) { DrawSymbols(painter, node); }
            }
        }

        /// <summary>A pale bar exactly on the room's wall, across the corridor.</summary>
        private void Door(Painter2D painter, MapCorridor corridor, bool atFrom, bool faint)
        {
            var at = ToScreen(atFrom ? corridor.From : corridor.To);
            var across = Mathf.Clamp(corridor.Width * Scale, 7f, 22f) + 6f;
            var bar = corridor.IsHorizontal ? new Rect(at.x - 2.5f, at.y - across * 0.5f, 5f, across) : new Rect(at.x - across * 0.5f, at.y - 2.5f, across, 5f);
            MapIcons.Rectangle(painter, bar);
            painter.fillColor = faint ? DoorDim : DoorFill;
            painter.Fill();
            MapIcons.Stroke(painter, new Color32(42, 36, 28, 255), 1f);
        }

        private static MapGlyph? GlyphOf(MapLink link)
        {
            var connection = link.Connection;
            if (connection.IsShortcut)
            {
                return link.State == MapLinkState.ShortcutLocked ? MapGlyph.ShortcutShut : MapGlyph.ShortcutOpen;
            }

            if (link.State == MapLinkState.Sealed || connection.Type == ConnectionType.BossGate
                || connection.Type == ConnectionType.Door || connection.Type == ConnectionType.Gate)
            {
                return link.State == MapLinkState.Open && connection.Type == ConnectionType.Door ? (MapGlyph?)null : MapGlyph.Gate;
            }

            if (connection.RequiredAbilityId == AbilityIds.Grapple)
            {
                return MapGlyph.Ring;
            }

            return link.State == MapLinkState.Blocked || link.ShowsGateIcon ? MapGlyph.Ability : (MapGlyph?)null;
        }

        private void DrawRoom(Painter2D painter, MapNode node)
        {
            var box = ToScreen(node.Rect);
            if (node.State == MapNodeState.Unexplored)
            {
                MapIcons.Rectangle(painter, box);
                painter.fillColor = FogFill;
                painter.Fill();
                MapIcons.DashedRect(painter, box, FogLine, 1.4f, 6f, 5f);
                return;
            }

            if (node.Area.IsSecret)
            {
                MapIcons.Rectangle(painter, box);
                painter.fillColor = new Color(14f / 255f, 12f / 255f, 10f / 255f, 0.85f);
                painter.Fill();
                MapIcons.DashedRect(painter, box, Secret, 1.4f, 5f, 4f);
                return;
            }

            var tint = Tint(node.Area.ZoneId);
            if (node.State == MapNodeState.Charted)
            {
                // From a bought map: a pale dashed outline, never as solid as a room walked through.
                MapIcons.Rectangle(painter, box);
                painter.fillColor = new Color(tint.r, tint.g, tint.b, 0.25f);
                painter.Fill();
                MapIcons.DashedRect(painter, box, new Color(Frame.r, Frame.g, Frame.b, 0.55f), 1.4f, 7f, 4f);
                return;
            }

            if (!HasPicture(node))
            {
                MapIcons.Rectangle(painter, box);
                painter.fillColor = node.Area.IsBossArena ? BossFill : tint;
                painter.Fill();
            }

            MapIcons.Rectangle(painter, box);
            var selected = node.AreaId == _selected;
            MapIcons.Stroke(painter, selected ? MapIcons.Gold : Frame, selected ? 2.4f : 1.8f);
            if (selected)
            {
                MapIcons.DashedRect(painter, new Rect(box.x - 7f, box.y - 7f, box.width + 14f, box.height + 14f),
                    new Color(MapIcons.Gold.r, MapIcons.Gold.g, MapIcons.Gold.b, 0.5f), 1f, 2f, 5f);
            }
        }

        private void DrawSymbols(Painter2D painter, MapNode node)
        {
            if (node.State == MapNodeState.Unexplored)
            {
                return;
            }

            var box = ToScreen(node.Rect);
            var size = Mathf.Clamp(Mathf.Min(box.width, box.height) * 0.42f, 16f, 30f);
            if (node.Area.IsSecret)
            {
                MapIcons.Draw(painter, node.Icons.Exists(i => i.Type == MapIconType.Rumour) && node.State != MapNodeState.Discovered
                    ? MapGlyph.Rumour : MapGlyph.Secret, box.center, size);
                return;
            }

            var symbols = new List<(MapGlyph glyph, bool active)>();
            foreach (var icon in node.Icons)
            {
                switch (icon.Type)
                {
                    case MapIconType.Checkpoint: symbols.Add((MapGlyph.Altar, icon.IsActive)); break;
                    case MapIconType.FastTravel: symbols.Add((MapGlyph.Travel, icon.IsActive)); break;
                    case MapIconType.Boss: symbols.Add((icon.IsActive ? MapGlyph.Boss : MapGlyph.BossBeaten, icon.IsActive)); break;
                    case MapIconType.Rumour: symbols.Add((MapGlyph.Rumour, true)); break;
                }
            }

            if (node.IsCurrent)
            {
                symbols.Insert(0, (MapGlyph.Player, true));
            }

            var at = box.center - new Vector2((symbols.Count - 1) * size * 0.6f, 0f);
            foreach (var (glyph, active) in symbols)
            {
                MapIcons.Draw(painter, glyph, at, glyph == MapGlyph.Player ? size * 1.3f : size, active);
                at.x += size * 1.2f;
            }

            if (node.MarkColor >= 0)
            {
                MapIcons.DrawMark(painter, new Vector2(box.xMax - 12f, box.yMin + 14f), node.MarkColor);
            }
        }

        /// <summary>The zone's room colour from the graph; a corridor takes it too.</summary>
        private Color Tint(string zoneId) => TintOf != null ? TintOf(zoneId) : (Color)new Color32(42, 90, 78, 255);

        /// <summary>Set by the map: a zone's room colour (<see cref="ZoneDefinition.MapTint"/>).</summary>
        public System.Func<string, Color> TintOf { get; set; }
    }
}
