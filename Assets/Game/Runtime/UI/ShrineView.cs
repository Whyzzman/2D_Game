using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgottenKingdom
{
    /// <summary>
    /// The altar's menu and the travel list from design/ui/map/map.html (frames 2 and 3), drawn
    /// for <see cref="ShrineMenu"/>, which keeps the behaviour. Resting, travelling, the
    /// character's screens (the window hands back to the altar when it closes) and leaving.
    /// «Мандрувати» appears only when travel leads anywhere; the list holds lit altars only,
    /// grouped by zone, with the chosen one shown on a small map. Escape or B step back.
    /// </summary>
    public sealed class ShrineView : IHudOverlay
    {
        private const string OpenClass = "shrine--open";
        private const string TravelClass = "shrine--travel";

        private readonly VisualElement _overlay;
        private readonly Label _title;
        private readonly List<Button> _items = new List<Button>();
        private readonly List<System.Action> _actions = new List<System.Action>();
        private readonly Button _travelButton;
        private readonly Label _from;
        private readonly VisualElement _list;
        private readonly Label _problem;
        private readonly MapCanvas _miniMap = new MapCanvas();
        private readonly List<Button> _rows = new List<Button>();
        private readonly List<TravelDestination> _destinations = new List<TravelDestination>();
        private ShrineMenu _shrine;
        private WorldState _world;
        private readonly InventoryView _inventory;
        private MenuInput.Repeater _repeater;
        private int _focus;
        private int _row;
        private int _openedFrame = -1;

        public ShrineView(VisualElement root, InventoryView inventory)
        {
            _overlay = root?.Q("shrine");
            _inventory = inventory;
            if (_overlay == null)
            {
                return;
            }

            _title = _overlay.Q<Label>("shrine-title");
            Bind("shrine-rest", () => _shrine?.Rest());
            _travelButton = Bind("shrine-travel", ShowTravel);
            Bind("shrine-character", () => OpenWindow(InventoryView.Page.Character));
            Bind("shrine-skills", () => OpenWindow(InventoryView.Page.Skills));
            Bind("shrine-equipment", () => OpenWindow(InventoryView.Page.Items));
            Bind("shrine-leave", () => _shrine?.Close());
            _from = _overlay.Q<Label>("travel-from");
            _list = _overlay.Q("travel-list");
            _problem = _overlay.Q<Label>("travel-problem");
            _overlay.Q("travel-map")?.Add(_miniMap);
        }

        public bool IsOpen => _overlay != null && _overlay.ClassListContains(OpenClass);
        public bool IsTravelShown => IsOpen && _overlay.ClassListContains(TravelClass);

        /// <summary>The lit altars offered, in list order, for tests and tools.</summary>
        public IReadOnlyList<TravelDestination> Destinations => _destinations;

        /// <summary>Which menu entries can be chosen now, in order.</summary>
        public IEnumerable<string> VisibleItems
        {
            get
            {
                foreach (var item in _items)
                {
                    if (item.style.display != DisplayStyle.None)
                    {
                        yield return item.name;
                    }
                }
            }
        }

        public void Attach(ShrineMenu shrine, WorldState world)
        {
            Detach();
            _shrine = shrine;
            _world = world;
            if (_shrine == null || _overlay == null)
            {
                return;
            }

            _shrine.AddPresenter(this);
            _shrine.Opened += OnOpened;
            _shrine.Closed += OnClosed;
        }

        public void Detach()
        {
            if (_shrine != null)
            {
                _shrine.Opened -= OnOpened;
                _shrine.Closed -= OnClosed;
                _shrine.RemovePresenter(this);
            }

            _shrine = null;
            _overlay?.RemoveFromClassList(OpenClass);
        }

        /// <summary>Escape or B: from the list back to the menu, from the menu away.</summary>
        public void Back()
        {
            if (IsTravelShown)
            {
                ShowMenu();
            }
            else
            {
                _shrine?.Close();
            }
        }

        private void OnOpened()
        {
            _overlay.AddToClassList(OpenClass);
            _openedFrame = Time.frameCount;
            ShowMenu();
        }

        private void OnClosed()
        {
            _overlay.RemoveFromClassList(OpenClass);
            _overlay.RemoveFromClassList(TravelClass);
        }

        public void ShowMenu()
        {
            _overlay.RemoveFromClassList(TravelClass);
            if (_title != null && _shrine?.Origin != null)
            {
                _title.text = _shrine.Origin.DisplayName.ToUpperInvariant();
            }

            UiParts.Show(_travelButton, _shrine != null && _shrine.CanOfferTravel);
            SetFocus(0);
        }

        /// <summary>The character's screens from the altar; closing them comes back here.</summary>
        public void OpenWindow(InventoryView.Page page)
        {
            var origin = _shrine?.Origin;
            if (origin == null || _inventory == null)
            {
                return;
            }

            _shrine.Close();
            if (!_inventory.Open(page, () => _shrine?.Open(origin)))
            {
                _shrine.Open(origin);
            }
        }

        // ---------- travel ----------

        public void ShowTravel()
        {
            if (_shrine == null || !_shrine.CanOfferTravel)
            {
                return;
            }

            _overlay.AddToClassList(TravelClass);
            _from.text = $"Звідси: {_shrine.Origin.DisplayName}";
            _problem.text = string.Empty;
            BuildRows();
            var first = _destinations.FindIndex(destination => destination.IsSelectable);
            SelectRow(Mathf.Max(0, first));
        }

        public TravelResult TravelTo(int index)
        {
            if (_shrine == null || index < 0 || index >= _destinations.Count)
            {
                return TravelResult.Locked;
            }

            var result = _shrine.TravelTo(_destinations[index].CheckpointId);
            if (result != TravelResult.Started)
            {
                _problem.text = FastTravelService.Describe(result);
            }

            return result;
        }

        private void BuildRows()
        {
            _list.Clear();
            _rows.Clear();
            _destinations.Clear();
            string zone = null;
            foreach (var destination in _shrine.Travel.Destinations(_shrine.Origin.CheckpointId))
            {
                var zoneName = destination.Zone != null ? destination.Zone.DisplayName : string.Empty;
                if (zoneName != zone)
                {
                    zone = zoneName;
                    _list.Add(UiParts.Text(zoneName.ToUpperInvariant(), "travel__zone"));
                }

                var index = _destinations.Count;
                _destinations.Add(destination);
                var row = new Button();
                row.AddToClassList("travel__row");
                row.EnableInClassList("travel__row--current", destination.IsCurrent);
                row.EnableInClassList("travel__row--shut", !destination.IsCurrent && !destination.IsSelectable);
                row.Add(new MapGlyphElement(destination.IsCurrent ? MapGlyph.Here : MapGlyph.Travel));
                row.Add(UiParts.Text(destination.Checkpoint.DisplayName, "travel__name"));
                var note = destination.IsCurrent ? "ви тут"
                    : !destination.IsSelectable ? FastTravelService.Describe(destination.Availability)
                    : destination.Area != null && destination.Area.DisplayName != destination.Checkpoint.DisplayName
                        ? destination.Area.DisplayName
                        : string.Empty;
                row.Add(UiParts.Text(note, "travel__note"));
                row.clicked += () =>
                {
                    SelectRow(index);
                    TravelTo(index);
                };
                row.RegisterCallback<PointerEnterEvent>(_ => SelectRow(index));
                _list.Add(row);
                _rows.Add(row);
            }
        }

        private void SelectRow(int index)
        {
            _row = Mathf.Clamp(index, 0, Mathf.Max(0, _rows.Count - 1));
            for (var i = 0; i < _rows.Count; i++)
            {
                _rows[i].EnableInClassList("travel__row--focused", i == _row);
            }

            DrawMiniMap();
        }

        private void DrawMiniMap()
        {
            if (_world == null || _world.Graph == null || _row >= _destinations.Count)
            {
                return;
            }

            var view = WorldMapBuilder.Build(_world.Graph, _world, _world.CurrentAreaId,
                _world.CurrentZoneId, _world.IsFastTravelUnlocked);
            var destination = _destinations[_row];
            // About twenty map units across, centred on the chosen altar, as in the mockup.
            var size = new Vector2(20f, 20f * 820f / 1100f);
            var visible = new Rect(destination.MapPosition - size * 0.5f, size);
            _miniMap.Show(view, visible, destination.Area != null ? destination.Area.AreaId : null, showLabels: false);
        }

        // ---------- input ----------

        public void Tick()
        {
            if (!IsOpen || Time.frameCount == _openedFrame)
            {
                return;
            }

            if (MenuInput.PadBackPressed())
            {
                Back();
                return;
            }

            var step = _repeater.Step(MenuInput.ReadVertical());
            if (IsTravelShown)
            {
                if (step != 0) { MoveRow(step); }
                if (MenuInput.Confirmed()) { TravelTo(_row); }
                return;
            }

            if (step != 0) { MoveFocus(step); }
            if (MenuInput.Confirmed() && _focus >= 0 && _focus < _actions.Count)
            {
                _actions[_focus]?.Invoke();
            }
        }

        private void MoveRow(int step)
        {
            // Past the current altar and any the way is shut to.
            for (var i = 1; i <= _rows.Count; i++)
            {
                var next = (_row + step * i + _rows.Count * 4) % _rows.Count;
                if (_destinations[next].IsSelectable)
                {
                    SelectRow(next);
                    return;
                }
            }
        }

        private Button Bind(string name, System.Action action)
        {
            var button = _overlay.Q<Button>(name);
            if (button == null)
            {
                return null;
            }

            button.clicked += action;
            var index = _items.Count;
            _items.Add(button);
            _actions.Add(action);
            button.RegisterCallback<PointerEnterEvent>(_ => SetFocus(index));
            return button;
        }

        private void MoveFocus(int step)
        {
            for (var i = 1; i <= _items.Count; i++)
            {
                var next = (_focus + step * i + _items.Count * 4) % _items.Count;
                if (_items[next].style.display != DisplayStyle.None)
                {
                    SetFocus(next);
                    return;
                }
            }
        }

        private void SetFocus(int index)
        {
            _focus = Mathf.Clamp(index, 0, Mathf.Max(0, _items.Count - 1));
            for (var i = 0; i < _items.Count; i++)
            {
                _items[i].EnableInClassList("btn--focused", i == _focus);
            }
        }
    }
}
