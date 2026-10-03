using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgottenKingdom
{
    /// <summary>
    /// The world map (design/ui/world-map/world-map.html, frames 1, 3, 4): the regions as islands in
    /// the fog, each its own colour; roads with their rooms; a plaque with the name and the boss and
    /// altar under every known island; a «?» on a region a road leads to; nothing at all past that.
    ///
    /// The canvas is the mockup's own 1470 × 900, so the regions' positions are the mockup's numbers.
    /// Layers, bottom up: fog, roads, islands, a second veil of fog, plaques, symbols.
    /// </summary>
    public sealed class WorldMapCanvas : VisualElement
    {
        public static readonly Vector2 Size = new Vector2(1470f, 900f);
        private static readonly Color RoadLit = new Color32(216, 203, 180, 255);
        private static readonly Color RoadDim = new Color32(111, 102, 90, 255);
        private static readonly Color Silhouette = new Color(0.16f, 0.15f, 0.14f, 0.75f);

        private readonly VisualElement _fog = Layer("world-canvas__fog");
        private readonly VisualElement _roads = Layer("world-canvas__layer");
        private readonly VisualElement _islands = Layer("world-canvas__layer");
        private readonly VisualElement _veil = Layer("world-canvas__veil");
        private readonly VisualElement _plaques = Layer("world-canvas__layer");
        private readonly VisualElement _symbols = Layer("world-canvas__layer");
        private WorldAtlas _atlas;
        private string _focus;
        private MapArt _art;

        public WorldMapCanvas()
        {
            AddToClassList("world-canvas");
            Add(_fog);
            Add(_roads);
            Add(_islands);
            Add(_veil);
            Add(_plaques);
            Add(_symbols);
            _roads.generateVisualContent += DrawRoads;
            _symbols.generateVisualContent += DrawSymbols;
        }

        public void Show(WorldAtlas atlas, string focusZoneId, MapArt art)
        {
            var rebuilt = atlas != _atlas || art != _art;
            _atlas = atlas;
            _focus = focusZoneId;
            _art = art;
            if (rebuilt)
            {
                Rebuild();
            }

            _symbols.MarkDirtyRepaint();
        }

        /// <summary>The known region whose island is under a point of this canvas.</summary>
        public string ZoneAt(Vector2 local)
        {
            if (_atlas == null)
            {
                return null;
            }

            string best = null;
            var bestDistance = float.MaxValue;
            foreach (var zone in _atlas.Zones)
            {
                if (!zone.IsKnown) { continue; }
                var delta = local - zone.Zone.WorldPosition;
                var distance = delta.magnitude;
                if (Mathf.Abs(delta.x) < 150f && delta.y > -160f && delta.y < 90f && distance < bestDistance)
                {
                    best = zone.ZoneId;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private static VisualElement Layer(string name)
        {
            var layer = new VisualElement { pickingMode = PickingMode.Ignore };
            layer.AddToClassList(name);
            return layer;
        }

        private void Rebuild()
        {
            _islands.Clear();
            _plaques.Clear();
            _fog.style.backgroundImage = _art != null ? _art.Fog : null;
            _veil.style.backgroundImage = _art != null ? _art.Fog : null;
            _roads.MarkDirtyRepaint();
            if (_atlas == null)
            {
                return;
            }

            foreach (var zone in _atlas.Zones)
            {
                if (zone.State == ZoneMapState.Hidden) { continue; }
                var picture = _art != null ? _art.Island(zone.Zone.Art) : null;
                if (picture != null)
                {
                    var island = new VisualElement { pickingMode = PickingMode.Ignore };
                    island.AddToClassList("world-canvas__island");
                    island.style.backgroundImage = picture;
                    var at = zone.Zone.WorldPosition - MapArt.IslandAnchor;
                    island.style.left = at.x;
                    island.style.top = at.y;
                    island.style.width = MapArt.IslandSize.x;
                    island.style.height = MapArt.IslandSize.y;
                    if (zone.State == ZoneMapState.Silhouette)
                    {
                        island.style.unityBackgroundImageTintColor = Silhouette;
                    }

                    _islands.Add(island);
                }

                if (zone.IsKnown)
                {
                    var plaque = UiParts.Text(zone.Zone.DisplayName, "world-canvas__plaque");
                    plaque.AddToClassList("display");
                    plaque.style.left = zone.Zone.WorldPosition.x;
                    plaque.style.top = zone.Zone.WorldPosition.y + 20f;
                    _plaques.Add(plaque);
                }
            }
        }

        // ---------- roads (under the islands) ----------

        private void DrawRoads(MeshGenerationContext context)
        {
            if (_atlas == null)
            {
                return;
            }

            var painter = context.painter2D;
            foreach (var road in _atlas.Roads)
            {
                if (!road.IsShown && !road.IsHinted) { continue; }
                Curve(road, out var a, out var control, out var b);
                if (road.IsShown)
                {
                    StrokeCurve(painter, a, control, b, new Color(0f, 0f, 0f, 0.6f), 5f);
                    StrokeCurve(painter, a, control, b, RoadLit, 2.4f);
                }
                else
                {
                    var points = new List<Vector2>();
                    for (var i = 0; i <= 24; i++) { points.Add(Bezier(a, control, b, i / 24f)); }
                    MapIcons.Dashed(painter, points, Vector2.zero, 1f, RoadDim, 1.6f, 2f, 7f);
                }

                var nodes = road.IsShown ? 4 : 2;
                for (var i = 1; i <= nodes; i++)
                {
                    var p = Bezier(a, control, b, i / (nodes + 1f));
                    var explored = road.IsShown && i % 3 != 0;
                    MapIcons.Circle(painter, p, 6f, explored ? RoadLit : MapIcons.Ink, road.IsShown ? RoadLit : RoadDim, 1.6f);
                    if (explored) { MapIcons.Circle(painter, p, 2.4f, MapIcons.Ink, null, 0f); }
                }
            }
        }

        private void Curve(RoadMapEntry road, out Vector2 a, out Vector2 control, out Vector2 b)
        {
            a = _atlas.Zone(road.Road.FromZoneId).Zone.WorldPosition + new Vector2(0f, 18f);
            b = _atlas.Zone(road.Road.ToZoneId).Zone.WorldPosition + new Vector2(0f, 18f);
            control = (a + b) * 0.5f + new Vector2(0f, 26f);
        }

        private static Vector2 Bezier(Vector2 a, Vector2 c, Vector2 b, float t) =>
            (1f - t) * (1f - t) * a + 2f * (1f - t) * t * c + t * t * b;

        private static void StrokeCurve(Painter2D painter, Vector2 a, Vector2 control, Vector2 b, Color color, float width)
        {
            painter.BeginPath();
            painter.MoveTo(a);
            // As a cubic: Painter2D's QuadraticCurveTo strays to the origin.
            painter.BezierCurveTo(a + (control - a) * (2f / 3f), b + (control - b) * (2f / 3f), b);
            MapIcons.Stroke(painter, color, width);
        }

        // ---------- symbols (over everything) ----------

        private void DrawSymbols(MeshGenerationContext context)
        {
            if (_atlas == null)
            {
                return;
            }

            var painter = context.painter2D;
            foreach (var road in _atlas.Roads)
            {
                if (!road.Road.IsGated || (!road.IsShown && !road.IsHinted)) { continue; }
                Curve(road, out var a, out var control, out var b);
                MapIcons.Draw(painter, MapGlyph.Gate, Bezier(a, control, b, 0.5f) - new Vector2(0f, 0f), 24f);
            }

            foreach (var zone in _atlas.Zones)
            {
                var at = zone.Zone.WorldPosition;
                if (zone.State == ZoneMapState.Silhouette)
                {
                    MapIcons.Draw(painter, MapGlyph.Unknown, at + new Vector2(0f, 30f), 28f);
                    continue;
                }

                if (!zone.IsKnown) { continue; }
                var marks = new List<(MapGlyph glyph, bool active)>();
                if (zone.BossesTotal > 0) { marks.Add((zone.HasBossAlive ? MapGlyph.Boss : MapGlyph.BossBeaten, zone.HasBossAlive)); }
                if (zone.ShrinesTotal > 0) { marks.Add((MapGlyph.Altar, zone.ShrinesLit > 0)); }
                var x = at.x - (marks.Count - 1) * 20f;
                foreach (var (glyph, active) in marks)
                {
                    MapIcons.Draw(painter, glyph, new Vector2(x, at.y + 72f), 26f, active);
                    x += 40f;
                }

                if (zone.ZoneId == _focus)
                {
                    DashedEllipse(painter, at + new Vector2(0f, 8f), new Vector2(170f, 92f), new Color(0.79f, 0.63f, 0.29f, 0.6f));
                }

                if (zone.IsCurrent)
                {
                    MapIcons.Draw(painter, MapGlyph.Player, at + new Vector2(40f, -4f), 34f);
                }
            }
        }

        private static void DashedEllipse(Painter2D painter, Vector2 c, Vector2 radius, Color color)
        {
            painter.BeginPath();
            const int Dots = 64;
            for (var i = 0; i < Dots; i++)
            {
                var angle = i * Mathf.PI * 2f / Dots;
                var p = c + new Vector2(Mathf.Cos(angle) * radius.x, Mathf.Sin(angle) * radius.y);
                painter.MoveTo(p);
                painter.LineTo(p + new Vector2(0.5f, 0f));
            }

            MapIcons.Stroke(painter, color, 2.2f);
        }
    }
}
