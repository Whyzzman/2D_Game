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
    /// The map window from design/ui/world-map/world-map.html (Approval 1, 2026-10-03), drawn for
    /// <see cref="WorldMapScreen"/>, which keeps the behaviour. It opens on the world: islands of the
    /// regions, the chosen one's card on the right. Arrows or the left stick choose a region, Enter or
    /// A goes into it (or a click on a chosen island); there the arrows walk the rooms, the right stick
    /// or a right-button drag pans, the wheel, Page Up/Down or the triggers zoom, R or Y marks with the
    /// quill. Escape or B steps back out to the world, then closes; M closes from either. The map is
    /// the first tab of the pause's window: Q/E or LB/RB, or a click on a tab, turn to the inventory's
    /// pages. Opened from the pause, it returns there.
    /// </summary>
    public sealed class MapView : IHudOverlay
    {
        private const string OpenClass = "map--open";
        private const float StickPanPerSecond = 0.9f;
        private const float ZoomPerSecond = 2f;

        private static readonly (string name, InventoryView.Page page)[] Tabs =
        {
            ("items", InventoryView.Page.Items), ("equipment", InventoryView.Page.Equipment),
            ("character", InventoryView.Page.Character), ("skills", InventoryView.Page.Skills),
            ("spells", InventoryView.Page.Spells), ("people", InventoryView.Page.People),
            ("bestiary", InventoryView.Page.Bestiary)
        };

        private readonly VisualElement _overlay;
        private readonly MapCanvas _canvas = new MapCanvas();
        private readonly WorldMapCanvas _world = new WorldMapCanvas();
        private readonly Label _heading;
        private readonly Label _sub;
        private readonly VisualElement _card;
        private readonly VisualElement _meta;
        private readonly Label _infoName;
        private readonly VisualElement _infoLines;
        private readonly VisualElement _legend;
        private readonly VisualElement _hints;
        private readonly List<VisualElement> _halos = new List<VisualElement>();
        private WorldMapScreen _screen;
        private WorldState _worldState;
        private Action _onClosed;
        private Vector2 _dragFrom;
        private bool _dragging;
        private int _openedFrame = -1;
        private MenuInput.Repeater _horizontal;
        private MenuInput.Repeater _vertical;
        private Rect _shownVisible;
        private string _shownSelected;
        private string _shownFocus;
        private MapLevel _shownLevel;

        public MapView(VisualElement root)
        {
            _overlay = root?.Q("map");
            if (_overlay == null)
            {
                return;
            }

            _heading = _overlay.Q<Label>("map-zone");
            _sub = _overlay.Q<Label>("map-sub");
            _card = _overlay.Q("map-card");
            _meta = _overlay.Q("map-meta");
            _infoName = _overlay.Q<Label>("map-info-name");
            _infoLines = _overlay.Q("map-info-lines");
            _legend = _overlay.Q("map-legend");
            _hints = _overlay.Q("map-hints");
            _overlay.Query(className: "map__halo").ForEach(_halos.Add);
            var sheet = _overlay.Q("map-sheet");
            sheet?.Add(_world);
            sheet?.Add(_canvas);
            var compass = _overlay.Q("map-compass");
            if (compass != null) { compass.generateVisualContent += DrawCompass; }

            _canvas.RegisterCallback<WheelEvent>(OnWheel);
            _canvas.RegisterCallback<PointerDownEvent>(OnPointerDown);
            _canvas.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            _canvas.RegisterCallback<PointerUpEvent>(_ => _dragging = false);
            _world.RegisterCallback<PointerDownEvent>(OnWorldPointerDown);
            foreach (var (name, page) in Tabs)
            {
                var tab = _overlay.Q<Button>("map-tab-" + name);
                if (tab != null) { tab.clicked += () => Switch(page); }
            }
        }

        public bool IsOpen => _overlay != null && _overlay.ClassListContains(OpenClass);
        public WorldMapScreen Screen => _screen;

        /// <summary>
        /// Set by the HUD: turn to an inventory page, carrying the way back (the pause) along. Without
        /// it the tabs do nothing.
        /// </summary>
        public Action<InventoryView.Page, Action> SwitchToInventory { get; set; }

        public void Attach(WorldMapScreen screen, WorldState world)
        {
            Detach();
            _screen = screen;
            _worldState = world;
            if (_screen == null || _overlay == null)
            {
                return;
            }

            _screen.AddPresenter(this);
            _screen.Opened += OnOpened;
            _screen.Closed += OnClosed;
            _screen.ViewChanged += OnViewChanged;
            _screen.LevelChanged += OnLevelChanged;
        }

        public void Detach()
        {
            if (_screen != null)
            {
                _screen.Opened -= OnOpened;
                _screen.Closed -= OnClosed;
                _screen.ViewChanged -= OnViewChanged;
                _screen.LevelChanged -= OnLevelChanged;
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

        /// <summary>One step back: from a region to the world, from the world closed.</summary>
        public void Back() => _screen?.Back();

        /// <summary>Closes the map for another tab of the same window and hands it the way back.</summary>
        public Action Handoff()
        {
            var onClosed = _onClosed;
            _onClosed = null;
            _screen?.Close();
            return onClosed;
        }

        /// <summary>Q/E or LB/RB: the tab before the map is the bestiary, the one after it the items.</summary>
        public void Turn(int direction) => Switch(direction < 0 ? InventoryView.Page.Bestiary : InventoryView.Page.Items);

        private void Switch(InventoryView.Page page)
        {
            if (SwitchToInventory == null || !IsOpen)
            {
                return;
            }

            SwitchToInventory(page, Handoff());
        }

        private MapArt Art => _worldState != null && _worldState.Graph != null ? _worldState.Graph.MapArt : null;

        private void OnOpened()
        {
            _overlay.AddToClassList(OpenClass);
            _openedFrame = Time.frameCount;
            _canvas.Art = Art;
            _canvas.TintOf = zoneId => _worldState?.Graph?.Zone(zoneId)?.MapTint ?? new Color32(42, 90, 78, 255);
            foreach (var halo in _halos) { halo.style.backgroundImage = Art != null ? Art.Glow : null; }
            Redraw();
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

        private void OnLevelChanged()
        {
            if (_screen.Level == MapLevel.Zone)
            {
                FitZone();
            }

            Redraw();
        }

        /// <summary>A region's own map opens as close as its known rooms allow, all of them in view.</summary>
        private void FitZone()
        {
            var bounds = _screen.ZoneBounds(_screen.FocusZoneId);
            var viewport = _screen.Viewport;
            if (bounds.width <= 0f)
            {
                return;
            }

            const float margin = 2f;
            var fit = Mathf.Min(viewport.ViewSize.x / (bounds.width + margin), viewport.ViewSize.y / (bounds.height + margin));
            _screen.Zoom(Mathf.Clamp(fit, viewport.MinZoom, viewport.MaxZoom) / viewport.Zoom);
            var visible = viewport.Visible;
            if (visible.width >= bounds.width && visible.height >= bounds.height)
            {
                viewport.CenterOn(bounds.center);
            }
        }

        // ---------- input ----------

        public void Tick()
        {
            if (!IsOpen || _screen == null || Time.frameCount == _openedFrame)
            {
                return;
            }

            var keyboard = Keyboard.current;
            var pad = Gamepad.current;
            if (MenuInput.PadBackPressed())
            {
                Back();
                return;
            }

            var turn = (keyboard != null && keyboard.qKey.wasPressedThisFrame) || (pad != null && pad.leftShoulder.wasPressedThisFrame) ? -1
                : (keyboard != null && keyboard.eKey.wasPressedThisFrame) || (pad != null && pad.rightShoulder.wasPressedThisFrame) ? 1 : 0;
            if (turn != 0)
            {
                Turn(turn);
                return;
            }

            var step = new Vector2(_horizontal.Step(MenuInput.ReadHorizontal()), -_vertical.Step(MenuInput.ReadVertical()));
            if (step != Vector2.zero)
            {
                _screen.MoveCursor(step);
            }

            if (_screen.Level == MapLevel.World)
            {
                if (MenuInput.Confirmed())
                {
                    _screen.EnterZone();
                }
            }
            else
            {
                TickZone(keyboard, pad);
            }

            // One cheap comparison a frame; the drawing is rebuilt only when something moved.
            if (_screen.Viewport.Visible != _shownVisible || _screen.SelectedAreaId != _shownSelected
                || _screen.FocusZoneId != _shownFocus || _screen.Level != _shownLevel)
            {
                Redraw();
            }
        }

        private void TickZone(Keyboard keyboard, Gamepad pad)
        {
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
        }

        private void OnWheel(WheelEvent evt)
        {
            if (_screen != null && _screen.Level == MapLevel.Zone)
            {
                _screen.Zoom(evt.delta.y < 0f ? 1.15f : 1f / 1.15f);
            }

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

        /// <summary>A click chooses a region; a click on the chosen one goes into it.</summary>
        private void OnWorldPointerDown(PointerDownEvent evt)
        {
            var zone = _world.ZoneAt(evt.localPosition);
            if (zone == null || _screen == null)
            {
                return;
            }

            if (zone == _screen.FocusZoneId)
            {
                _screen.EnterZone(zone);
            }
            else
            {
                _screen.FocusZone(zone);
                Redraw();
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
            _shownFocus = _screen.FocusZoneId;
            _shownLevel = _screen.Level;
            var atWorld = _screen.Level == MapLevel.World;
            UiParts.Show(_world, atWorld);
            UiParts.Show(_canvas, !atWorld);
            var zone = _screen.Atlas?.Zone(_screen.FocusZoneId);
            if (atWorld)
            {
                _world.Show(_screen.Atlas, _screen.FocusZoneId, Art);
                _heading.text = "Карта світу";
                _sub.text = zone != null ? zone.Zone.DisplayName : string.Empty;
                DrawZoneCard(zone);
            }
            else
            {
                _canvas.Show(view, _shownVisible, _shownSelected, showLabels: false, _screen.FocusZoneId);
                _heading.text = zone != null ? zone.Zone.DisplayName : string.Empty;
                _sub.text = "Карта світу  ›  " + _heading.text;
                DrawRoomCard(view.Node(_shownSelected));
            }

            DrawStats(zone);
            BuildLegend(atWorld);
            BuildHints(atWorld);
        }

        private void DrawZoneCard(ZoneMapEntry zone)
        {
            var picture = zone != null && Art != null ? Art.Card(zone.Zone.Art) : null;
            _card.style.backgroundImage = picture;
            _card.EnableInClassList("map__card--none", picture == null);
            _infoName.text = zone != null ? zone.Zone.DisplayName : string.Empty;
            _infoLines.Clear();
            if (zone != null && !string.IsNullOrEmpty(zone.Zone.Description))
            {
                _infoLines.Add(UiParts.Text(zone.Zone.Description, "map__line"));
            }

            if (zone != null && zone.IsKnown && !zone.CanEnter)
            {
                _infoLines.Add(Hint("Карти цього краю ще немає."));
            }
        }

        /// <summary>The chosen room: its name, what is in it, and why a way out is shut.</summary>
        private void DrawRoomCard(MapNode node)
        {
            _card.EnableInClassList("map__card--none", true);
            _infoLines.Clear();
            if (node == null)
            {
                _infoName.text = string.Empty;
                return;
            }

            _infoName.text = node.Label;
            foreach (var line in DescribeRoom(node, _screen.View, _worldState != null ? _worldState.Graph : null))
            {
                _infoLines.Add(line.EndsWith(WorldMapBuilder.UnreachableHint + ".") || line.EndsWith(WorldMapBuilder.ShortcutHint + ".")
                    ? Hint(line) : UiParts.Text(line, "map__line"));
            }
        }

        private static Label Hint(string text)
        {
            var label = UiParts.Text(text, "map__line");
            label.AddToClassList("map__line--hint");
            return label;
        }

        private void DrawStats(ZoneMapEntry zone)
        {
            _meta.Clear();
            if (zone == null)
            {
                return;
            }

            AddStat("Дослідження", zone.ExploredPercent + "%");
            AddStat("Вівтарі", $"{zone.ShrinesLit} / {zone.ShrinesTotal}");
            AddStat("Боси", $"{zone.BossesBeaten} / {zone.BossesTotal}");
            AddStat("Таємниці", $"{zone.Completion.SecretsFound} / {zone.Completion.SecretsTotal}");
            if (_screen.Level == MapLevel.Zone && _screen.HasQuill)
            {
                var marks = 0;
                foreach (var node in _screen.View.Nodes) { marks += node.MarkColor >= 0 ? 1 : 0; }
                AddStat("Мітки пера", $"{marks} / {MapIcons.MarkColors.Length}");
            }
        }

        private void AddStat(string name, string value)
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.AddToClassList("map__stat");
            row.Add(UiParts.Text(name, "map__stat-name"));
            row.Add(UiParts.Text(value, "map__stat-value"));
            _meta.Add(row);
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

        private void BuildLegend(bool atWorld)
        {
            _legend.Clear();
            if (atWorld)
            {
                AddLegend(MapGlyph.Player, "Ви тут");
                AddLegend(MapGlyph.RoomExplored, "Кімната (досліджена)");
                AddLegend(MapGlyph.RoomUnexplored, "Кімната (не досліджена)");
                AddLegend(MapGlyph.PathOpen, "Шлях (відкритий)");
                AddLegend(MapGlyph.PathHidden, "Шлях (прихований)");
                AddLegend(MapGlyph.Altar, "Вівтар");
                AddLegend(MapGlyph.Boss, "Бос");
                AddLegend(MapGlyph.Gate, "Ворота (потрібен прогрес)");
                AddLegend(MapGlyph.Unknown, "Невідома зона");
                return;
            }

            var travel = _worldState != null && _worldState.IsFastTravelUnlocked;
            AddLegend(MapGlyph.Player, "Ви тут");
            AddLegend(travel ? MapGlyph.Travel : MapGlyph.Altar, travel ? "Вівтар · мандри" : "Вівтар");
            AddLegend(MapGlyph.Boss, "Бос");
            AddLegend(MapGlyph.Secret, "Таємниця");
            AddLegend(MapGlyph.Rumour, "Чутка торговця");
            AddLegend(MapGlyph.Ability, "Потрібна здібність");
            AddLegend(MapGlyph.Ring, "Кільця гака");
            AddLegend(MapGlyph.Gate, "Ворота");
            AddLegend(MapGlyph.ShortcutShut, "Шорткат (закритий)");
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

        private void BuildHints(bool atWorld)
        {
            _hints.Clear();
            if (atWorld)
            {
                AddHint("Зони", "←↑→↓", "L");
                AddHint("До зони", "Enter", "A");
                AddHint("Назад", "M", "B");
                return;
            }

            AddHint("Кімнати", "←↑→↓", "L");
            AddHint("Зсув", "ПКМ", "R");
            AddHint("Масштаб", "PgUp/PgDn", "LT", "RT");
            if (_screen.HasQuill) { AddHint("Мітка", "R", "Y"); }
            AddHint("До світу", "Esc", "B");
        }

        private void AddHint(string text, string key, params string[] pad)
        {
            var hint = new VisualElement { pickingMode = PickingMode.Ignore };
            hint.AddToClassList("hint");
            hint.Add(UiParts.Text(key, "key"));
            foreach (var button in pad)
            {
                var label = UiParts.Text(button, "key");
                label.AddToClassList("key--pad");
                hint.Add(label);
            }

            hint.Add(UiParts.Text(text, "hint__text"));
            _hints.Add(hint);
        }

        /// <summary>The mockup's compass: two rings, the needle north–south in gold, east–west darker.</summary>
        private static void DrawCompass(MeshGenerationContext context)
        {
            var painter = context.painter2D;
            var rect = context.visualElement.contentRect;
            var c = rect.center;
            var s = Mathf.Min(rect.width, rect.height) / 100f;
            MapIcons.Circle(painter, c, 40f * s, null, MapIcons.Hex("8a7434"), 1.5f);
            MapIcons.Circle(painter, c, 31f * s, null, MapIcons.Hex("5c4c2c"), 1f);
            MapIcons.Path(painter, "M0,-46 L7,0 L0,46 L-7,0 Z", c, s);
            painter.fillColor = MapIcons.Gold;
            painter.Fill();
            MapIcons.Path(painter, "M-46,0 L0,-7 L46,0 L0,7 Z", c, s);
            painter.fillColor = MapIcons.Hex("8a7434");
            painter.Fill();
            MapIcons.Path(painter, "M0,-46 L7,0 L0,0 Z", c, s);
            painter.fillColor = MapIcons.GoldBright;
            painter.Fill();
            MapIcons.Circle(painter, c, 4f * s, MapIcons.Hex("0d0c0a"), MapIcons.GoldBright, 1f);
        }
    }
}
