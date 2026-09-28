using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace ForgottenKingdom
{
    /// <summary>
    /// A window of the HUD document that holds the game still: the pause backs out of whichever
    /// is open before it opens itself.
    /// </summary>
    public interface IHudOverlay
    {
        bool IsOpen { get; }

        /// <summary>Escape or the pad's back button: one step back, or closed.</summary>
        void Back();
    }

    /// <summary>
    /// The world map from design/ui/map/map.html, drawn for <see cref="WorldMapScreen"/>, which
    /// keeps the behaviour: what is shown, the cursor, pan and zoom and their limits. Arrows or
    /// the left stick walk the rooms, the right stick or a right-button drag pans, the wheel,
    /// Page Up/Down or the triggers zoom, a click picks a room; M, Escape or B close it. Opened
    /// from the pause, it returns there.
    /// </summary>
    public sealed class MapView : IHudOverlay
    {
        private const string OpenClass = "map--open";
        private const float StickPanPerSecond = 0.9f;
        private const float ZoomPerSecond = 2f;

        private readonly VisualElement _overlay;
        private readonly MapCanvas _canvas = new MapCanvas();
        private readonly Label _zone;
        private readonly VisualElement _meta;
        private readonly Label _infoName;
        private readonly VisualElement _infoLines;
        private readonly VisualElement _legend;
        private WorldMapScreen _screen;
        private WorldState _world;
        private Action _onClosed;
        private Vector2 _dragFrom;
        private bool _dragging;
        private int _openedFrame = -1;
        private MenuInput.Repeater _horizontal;
        private MenuInput.Repeater _vertical;
        private Rect _shownVisible;
        private string _shownSelected;

        public MapView(VisualElement root)
        {
            _overlay = root?.Q("map");
            if (_overlay == null)
            {
                return;
            }

            _zone = _overlay.Q<Label>("map-zone");
            _meta = _overlay.Q("map-meta");
            _infoName = _overlay.Q<Label>("map-info-name");
            _infoLines = _overlay.Q("map-info-lines");
            _legend = _overlay.Q("map-legend");
            var sheet = _overlay.Q("map-sheet");
            sheet?.Add(_canvas);
            _canvas.RegisterCallback<WheelEvent>(OnWheel);
            _canvas.RegisterCallback<PointerDownEvent>(OnPointerDown);
            _canvas.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            _canvas.RegisterCallback<PointerUpEvent>(_ => _dragging = false);
        }

        public bool IsOpen => _overlay != null && _overlay.ClassListContains(OpenClass);
        public WorldMapScreen Screen => _screen;

        public void Attach(WorldMapScreen screen, WorldState world)
        {
            Detach();
            _screen = screen;
            _world = world;
            if (_screen == null || _overlay == null)
            {
                return;
            }

            _screen.AddPresenter(this);
            _screen.Opened += OnOpened;
            _screen.Closed += OnClosed;
            _screen.ViewChanged += OnViewChanged;
        }

        public void Detach()
        {
            if (_screen != null)
            {
                _screen.Opened -= OnOpened;
                _screen.Closed -= OnClosed;
                _screen.ViewChanged -= OnViewChanged;
                _screen.RemovePresenter(this);
            }

            _screen = null;
            _onClosed = null;
            _overlay?.RemoveFromClassList(OpenClass);
        }

        /// <summary>Opens the map; <paramref name="onClosed"/> runs when it closes (the pause's way back).</summary>
        public bool Open(Action onClosed = null)
        {
            if (_screen == null)
            {
                return false;
            }

            _onClosed = onClosed;
            _screen.Open();
            if (!_screen.IsOpen)
            {
                _onClosed = null;
            }

            return _screen.IsOpen;
        }

        public void Back() => _screen?.Close();

        private void OnOpened()
        {
            _overlay.AddToClassList(OpenClass);
            _openedFrame = Time.frameCount;
            FitToExplored();
            BuildLegend();
            Redraw();
        }

        /// <summary>
        /// Opens as close as what has been explored allows, as the mockup does: a single room
        /// fills a good part of the sheet at the start, the whole world fits once it is known.
        /// </summary>
        private void FitToExplored()
        {
            var view = _screen.View;
            var viewport = _screen.Viewport;
            if (view == null || view.ContentBounds.width <= 0f)
            {
                return;
            }

            const float margin = 4f;
            var fit = Mathf.Min(viewport.ViewSize.x / (view.ContentBounds.width + margin),
                viewport.ViewSize.y / (view.ContentBounds.height + margin));
            _screen.Zoom(Mathf.Clamp(fit, viewport.MinZoom, viewport.MaxZoom) / viewport.Zoom);
            // When everything known fits, it is shown whole rather than half off one edge.
            var visible = viewport.Visible;
            if (visible.width >= view.ContentBounds.width && visible.height >= view.ContentBounds.height)
            {
                viewport.CenterOn(view.ContentBounds.center);
            }
        }

        private void OnClosed()
        {
            _overlay.RemoveFromClassList(OpenClass);
            _dragging = false;
            var onClosed = _onClosed;
            _onClosed = null;
            onClosed?.Invoke();
        }

        private void OnViewChanged(WorldMapView view)
        {
            if (IsOpen)
            {
                Redraw();
            }
        }

        // ---------- input ----------

        public void Tick()
        {
            if (!IsOpen || _screen == null || Time.frameCount == _openedFrame)
            {
                return;
            }

            if (MenuInput.PadBackPressed())
            {
                Back();
                return;
            }

            var step = new Vector2(_horizontal.Step(MenuInput.ReadHorizontal()),
                -_vertical.Step(MenuInput.ReadVertical()));
            if (step != Vector2.zero)
            {
                _screen.MoveCursor(step);
            }

            var keyboard = Keyboard.current;
            var pad = Gamepad.current;
            var dt = Time.unscaledDeltaTime;
            // The merchant's quill: R or the pad's top button marks the chosen room (item 70).
            if (_screen.HasQuill && ((keyboard != null && keyboard.rKey.wasPressedThisFrame)
                || (pad != null && pad.buttonNorth.wasPressedThisFrame)))
            {
                _screen.ToggleMarkOnSelection();
            }

            if (pad != null)
            {
                var stick = pad.rightStick.ReadValue();
                if (stick.sqrMagnitude > 0.04f)
                {
                    _screen.Pan(stick * StickPanPerSecond * dt * _screen.Viewport.ViewSize.x * 0.5f);
                }
            }

            var zoomIn = (keyboard != null && (keyboard.pageUpKey.isPressed || keyboard.equalsKey.isPressed))
                || (pad != null && pad.rightTrigger.isPressed);
            var zoomOut = (keyboard != null && (keyboard.pageDownKey.isPressed || keyboard.minusKey.isPressed))
                || (pad != null && pad.leftTrigger.isPressed);
            if (zoomIn != zoomOut)
            {
                _screen.Zoom(Mathf.Pow(ZoomPerSecond, zoomIn ? dt : -dt));
            }

            // One cheap comparison a frame; the drawing is rebuilt only when something moved.
            if (_screen.Viewport.Visible != _shownVisible || _screen.SelectedAreaId != _shownSelected)
            {
                Redraw();
            }
        }

        private void OnWheel(WheelEvent evt)
        {
            _screen?.Zoom(evt.delta.y < 0f ? 1.15f : 1f / 1.15f);
            evt.StopPropagation();
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button == 1)
            {
                _dragging = true;
                _dragFrom = evt.localPosition;
                return;
            }

            var area = _canvas.AreaAt(evt.localPosition);
            if (area != null)
            {
                _screen?.Select(area);
            }
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_dragging || _screen == null)
            {
                return;
            }

            var delta = (Vector2)evt.localPosition - _dragFrom;
            _dragFrom = evt.localPosition;
            // Dragging moves the sheet with the hand: the centre goes the other way.
            _screen.Pan(new Vector2(-delta.x, delta.y) / _canvas.Scale * _screen.Viewport.Zoom);
        }

        // ---------- drawing ----------

        private void Redraw()
        {
            var view = _screen?.View;
            if (view == null)
            {
                return;
            }

            _shownVisible = _screen.Viewport.Visible;
            _shownSelected = _screen.SelectedAreaId;
            _canvas.Show(view, _shownVisible, _shownSelected, showLabels: true);
            _zone.text = view.ZoneHeader.ToUpperInvariant();
            DrawMeta(view);
            DrawInfo(view.Node(_shownSelected));
        }

        private void DrawMeta(WorldMapView view)
        {
            _meta.Clear();
            var graph = _world != null ? _world.Graph : null;
            foreach (var pair in view.Completion)
            {
                var name = graph?.Zone(pair.Key)?.DisplayName ?? pair.Key;
                AddMeta(name, $"{pair.Value.Discovered} / {pair.Value.Total}");
                if (pair.Value.SecretsTotal > 0 && pair.Value.SecretsFound > 0)
                {
                    AddMeta("секрети", $"{pair.Value.SecretsFound} / {pair.Value.SecretsTotal}");
                }
            }
        }

        private void AddMeta(string name, string value)
        {
            var item = new VisualElement { pickingMode = PickingMode.Ignore };
            item.AddToClassList("map__meta-item");
            item.Add(UiParts.Text(name, "map__meta-name"));
            item.Add(UiParts.Text(value, "map__meta-value"));
            _meta.Add(item);
        }

        /// <summary>The chosen room: its name, what is in it, and why a way out is shut.</summary>
        private void DrawInfo(MapNode node)
        {
            _infoLines.Clear();
            if (node == null)
            {
                _infoName.text = string.Empty;
                return;
            }

            _infoName.text = node.Label;
            foreach (var line in DescribeRoom(node, _screen.View, _world != null ? _world.Graph : null))
            {
                _infoLines.Add(UiParts.Text(line, "map__info-line"));
            }
        }

        public static List<string> DescribeRoom(MapNode node, WorldMapView view, WorldGraph graph)
        {
            var lines = new List<string>();
            if (node.IsCurrent)
            {
                lines.Add("Ви тут.");
            }

            if (node.State == MapNodeState.Charted)
            {
                lines.Add("З купленої карти — ви тут ще не бували.");
            }

            if (node.MarkColor >= 0)
            {
                lines.Add("Ваша позначка пером.");
            }

            foreach (var icon in node.Icons)
            {
                switch (icon.Type)
                {
                    case MapIconType.Checkpoint:
                    case MapIconType.FastTravel:
                        var altar = graph?.Checkpoint(icon.Id)?.DisplayName ?? "Вівтар";
                        lines.Add(icon.IsActive
                            ? $"{altar} — запалено{(icon.Type == MapIconType.FastTravel ? ", можна мандрувати" : string.Empty)}."
                            : $"{altar} — не запалено.");
                        break;
                    case MapIconType.Boss:
                        lines.Add(icon.IsActive ? "Тут чекає страж місця." : "Страж місця переможений.");
                        break;
                    case MapIconType.Secret:
                        lines.Add("Таємне місце.");
                        break;
                    case MapIconType.Rumour:
                        lines.Add("Чутка Скупника: тут щось сховано.");
                        break;
                }
            }

            if (view == null)
            {
                return lines;
            }

            var hints = new HashSet<string>();
            foreach (var link in view.Links)
            {
                if (link.State != MapLinkState.Open && !string.IsNullOrEmpty(link.Hint)
                    && (link.Connection.FromAreaId == node.AreaId || link.Connection.ToAreaId == node.AreaId)
                    && hints.Add(link.Hint))
                {
                    lines.Add(link.Hint + ".");
                }
            }

            return lines;
        }

        private void BuildLegend()
        {
            _legend.Clear();
            var travel = _world != null && _world.IsFastTravelUnlocked;
            AddLegend(MapGlyph.Player, "Ви тут");
            AddLegend(travel ? MapGlyph.Travel : MapGlyph.Altar, travel ? "Вівтар · мандри" : "Вівтар");
            AddLegend(MapGlyph.Boss, "Страж місця");
            AddLegend(MapGlyph.Gate, "Поки що не пройти");
            AddLegend(MapGlyph.Secret, "Таємне місце");
            AddLegend(MapGlyph.Rumour, "Чутка");
            if (_screen != null && _screen.HasQuill)
            {
                AddLegend(MapGlyph.Here, "R / Y — позначка пером");
            }
        }

        private void AddLegend(MapGlyph glyph, string text)
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.AddToClassList("map__legend-row");
            row.Add(new MapGlyphElement(glyph));
            row.Add(UiParts.Text(text, "map__legend-text"));
            _legend.Add(row);
        }
    }
}
