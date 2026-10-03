using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The map screen's behaviour: opening and closing, pausing, what it shows, and moving around
    /// it. Drawing it waits for the HTML to be approved, the same as every other screen; this is
    /// what that drawing will bind to.
    ///
    /// The view is built only while the screen is open — once on opening, and again only when a
    /// world event says something changed. A closed map costs nothing, and an open one never
    /// recomputes the graph per frame.
    ///
    /// It follows the inventory's rules: it pauses the game, it cannot be opened by a dead player
    /// or in the middle of a transition, and it does not open on top of another menu.
    /// </summary>
    /// <summary>Which map is up: the world of regions, or one region's rooms.</summary>
    public enum MapLevel
    {
        World,
        Zone
    }

    [DisallowMultipleComponent]
    public sealed class WorldMapScreen : MonoBehaviour
    {
        [SerializeField] private WorldState _world;
        [SerializeField] private PlayerCharacter _player;
        [SerializeField] private PlayerRespawner _respawner;
        [SerializeField] private FastTravelService _travel;
        [SerializeField] private bool _pausesGame = true;
        [Tooltip("How much of the map one screen shows at zoom 1, in map units.")]
        [SerializeField] private Vector2 _viewSize = new Vector2(48f, 27f);
        [SerializeField, Min(0.1f)] private float _minZoom = 0.5f;
        [SerializeField, Min(0.1f)] private float _maxZoom = 3f;

        private readonly HashSet<object> _presenters = new HashSet<object>();
        private PlayerInputReader _input;
        private WorldMapViewport _viewport;
        private bool _isDirty;

        public event Action Opened;
        public event Action Closed;

        /// <summary>Raised whenever the view was rebuilt; the drawing redraws from it.</summary>
        public event Action<WorldMapView> ViewChanged;

        public bool IsOpen { get; private set; }
        public WorldMapView View { get; private set; }
        public WorldMapViewport Viewport => _viewport;

        /// <summary>
        /// The world of regions or one region's rooms (item 2 of the 15-zone list). The map opens
        /// on the world with the hero's region chosen; Enter goes into a region, back comes out.
        /// </summary>
        public MapLevel Level { get; private set; } = MapLevel.World;

        /// <summary>The region chosen on the world map, and the one whose rooms are shown.</summary>
        public string FocusZoneId { get; private set; } = string.Empty;

        /// <summary>The islands and roads; built with the room view.</summary>
        public WorldAtlas Atlas { get; private set; }

        /// <summary>Raised when the map goes from the world into a region or back.</summary>
        public event Action LevelChanged;

        /// <summary>The room the cursor is on. Starts on the player.</summary>
        public string SelectedAreaId { get; private set; } = string.Empty;

        /// <summary>Developer view: every room revealed, hidden ones included.</summary>
        public bool RevealAll { get; set; }

        /// <summary>How many times the view has been built; tests hold the map to "only when needed".</summary>
        public int BuildCount { get; private set; }

        private void Awake()
        {
            if (_world == null) { _world = FindAnyObjectByType<WorldState>(); }
            if (_player == null) { _player = FindAnyObjectByType<PlayerCharacter>(); }
            if (_respawner == null) { _respawner = FindAnyObjectByType<PlayerRespawner>(); }
            if (_travel == null) { _travel = FindAnyObjectByType<FastTravelService>(); }
            _input = _player != null ? _player.GetComponent<PlayerInputReader>() : null;
            _viewport = new WorldMapViewport(_minZoom, _maxZoom);
        }

        private void OnEnable()
        {
            if (_input != null) { _input.MapToggled += OnTogglePressed; }
            if (_world != null)
            {
                _world.Changed += OnWorldChanged;
                _world.Reloaded += MarkDirty;
                _world.AreaEntered += OnAreaEntered;
            }

            if (Progress != null) { Progress.MapMarksChanged += MarkDirty; }
        }

        private void OnDisable()
        {
            if (_input != null) { _input.MapToggled -= OnTogglePressed; }
            if (_world != null)
            {
                _world.Changed -= OnWorldChanged;
                _world.Reloaded -= MarkDirty;
                _world.AreaEntered -= OnAreaEntered;
            }

            if (Progress != null) { Progress.MapMarksChanged -= MarkDirty; }

            if (IsOpen)
            {
                Close();
            }
        }

        private void Update()
        {
            if (IsOpen && _isDirty)
            {
                Rebuild();
            }
        }

        /// <summary>
        /// Whether the map may open now. A transition, a death or another menu already holding the
        /// pause all say no; a boss fight does not, because the inventory is not barred from one
        /// either and the two follow the same rule.
        /// </summary>
        public bool CanOpen => !IsOpen && _world != null && _world.Graph != null
            && _player != null && _player.IsAlive
            && (_respawner == null || !_respawner.IsSequenceRunning)
            && (_travel == null || !_travel.IsTraveling)
            && !GamePause.IsPaused;

        /// <summary>
        /// True when something draws this screen. The key opens it only then: with nothing on
        /// screen a keypress would freeze the world behind an invisible menu. A release build has
        /// no drawing yet, so the key does nothing there until the real screen registers itself.
        /// </summary>
        public bool HasPresenter => _presenters.Count > 0;

        public void AddPresenter(object presenter)
        {
            if (presenter != null) { _presenters.Add(presenter); }
        }

        public void RemovePresenter(object presenter)
        {
            _presenters.Remove(presenter);
            if (!HasPresenter)
            {
                Close();
            }
        }

        /// <summary>The player's key. Closing always works; opening needs something to draw it.</summary>
        private void OnTogglePressed()
        {
            if (IsOpen || HasPresenter)
            {
                Toggle();
            }
        }

        public void Toggle()
        {
            if (IsOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        public void Open()
        {
            if (!CanOpen)
            {
                return;
            }

            IsOpen = true;
            _input?.SetGameplayEnabled(false);
            if (_pausesGame)
            {
                GamePause.Push();
            }

            SelectedAreaId = _world.CurrentAreaId;
            Level = MapLevel.World;
            FocusZoneId = _world.CurrentZoneId;
            Rebuild();
            var current = View.Node(SelectedAreaId);
            _viewport.CenterOn(current != null ? current.Area.MapPosition : View.ContentBounds.center);
            Opened?.Invoke();
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            if (_pausesGame)
            {
                GamePause.Pop();
            }

            _input?.SetGameplayEnabled(true);
            // The view is dropped with the screen; nothing about the map is kept alive closed.
            View = null;
            Atlas = null;
            Closed?.Invoke();
        }

        // ---------- navigation ----------

        /// <summary>Stick, arrows or a mouse drag, in screens.</summary>
        public void Pan(Vector2 delta)
        {
            if (IsOpen)
            {
                _viewport.Pan(delta);
            }
        }

        /// <summary>Scroll wheel or triggers; above one zooms in.</summary>
        public void Zoom(float factor)
        {
            if (IsOpen)
            {
                _viewport.ZoomBy(factor);
            }
        }

        /// <summary>
        /// Moves the controller cursor: to the next known region on the world map, to the next known
        /// room of the region on its own map.
        /// </summary>
        public void MoveCursor(Vector2 direction)
        {
            if (!IsOpen || View == null)
            {
                return;
            }

            if (Level == MapLevel.World)
            {
                var zone = Atlas?.Step(FocusZoneId, direction);
                if (zone != null) { FocusZoneId = zone.ZoneId; }
                return;
            }

            var next = WorldMapViewport.Step(View, SelectedAreaId, direction, InFocus);
            if (next != null)
            {
                SelectedAreaId = next.AreaId;
                _viewport.CenterOn(next.Area.MapPosition);
            }
        }

        /// <summary>Chooses a region on the world map (a click on its island).</summary>
        public void FocusZone(string zoneId)
        {
            if (IsOpen && Atlas?.Zone(zoneId) is { IsKnown: true })
            {
                FocusZoneId = zoneId;
            }
        }

        /// <summary>
        /// Into a region's own map: the chosen one, or <paramref name="zoneId"/>. Only a known, built
        /// region has rooms to show. The cursor starts on the hero if he is there, else on the first
        /// known room.
        /// </summary>
        public bool EnterZone(string zoneId = null)
        {
            var entry = Atlas?.Zone(zoneId ?? FocusZoneId);
            if (!IsOpen || entry == null || !entry.CanEnter)
            {
                return false;
            }

            FocusZoneId = entry.ZoneId;
            Level = MapLevel.Zone;
            if (View.Node(SelectedAreaId)?.Area.ZoneId != FocusZoneId)
            {
                var first = View.Nodes.Find(n => n.IsKnown && n.Area.ZoneId == FocusZoneId);
                SelectedAreaId = first != null ? first.AreaId : string.Empty;
            }

            ConfigureViewport();
            var selected = View.Node(SelectedAreaId);
            if (selected != null) { _viewport.CenterOn(selected.Area.MapPosition); }
            LevelChanged?.Invoke();
            return true;
        }

        /// <summary>From a region back out to the world.</summary>
        public void ToWorld()
        {
            if (!IsOpen || Level == MapLevel.World)
            {
                return;
            }

            Level = MapLevel.World;
            LevelChanged?.Invoke();
        }

        /// <summary>Escape or the pad's back: a region's map goes back to the world, the world closes.</summary>
        public void Back()
        {
            if (Level == MapLevel.Zone)
            {
                ToWorld();
            }
            else
            {
                Close();
            }
        }

        private bool InFocus(MapNode node) => Level != MapLevel.Zone || node.Area.ZoneId == FocusZoneId;

        /// <summary>The room view's bounds of the chosen region: what its own map pans and zooms over.</summary>
        public Rect ZoneBounds(string zoneId)
        {
            var bounds = new Rect();
            var any = false;
            foreach (var node in View.Nodes)
            {
                if (node.State == MapNodeState.Hidden || node.Area.ZoneId != zoneId) { continue; }
                var rect = node.Rect;
                bounds = any ? Rect.MinMaxRect(Mathf.Min(bounds.xMin, rect.xMin), Mathf.Min(bounds.yMin, rect.yMin),
                    Mathf.Max(bounds.xMax, rect.xMax), Mathf.Max(bounds.yMax, rect.yMax)) : rect;
                any = true;
            }

            return any ? bounds : View.ContentBounds;
        }

        private void ConfigureViewport()
        {
            _viewport.Configure(Level == MapLevel.Zone ? ZoneBounds(FocusZoneId) : View.ContentBounds, _viewSize);
        }

        /// <summary>Points the cursor at a room the mouse clicked; ignored for rooms not drawn.</summary>
        public void Select(string areaId)
        {
            var node = View?.Node(areaId);
            if (node != null && node.IsKnown && InFocus(node))
            {
                SelectedAreaId = areaId;
            }
        }

        private GameProgress Progress => _world != null ? _world.Progress : null;

        /// <summary>Whether the player owns the merchant's quill and may mark rooms.</summary>
        public bool HasQuill => Progress != null && Progress.HasReward(MerchantStock.QuillRewardId);

        /// <summary>The quill on the selected room: marks it, or takes its mark off (item 70).</summary>
        public bool ToggleMarkOnSelection()
        {
            if (!IsOpen || Progress == null || string.IsNullOrEmpty(SelectedAreaId))
            {
                return false;
            }

            var changed = Progress.ToggleMapMark(SelectedAreaId);
            if (changed)
            {
                Rebuild();
            }

            return changed;
        }

        // ---------- building ----------

        private void OnWorldChanged(WorldFlag flag, string id) => MarkDirty();

        private void OnAreaEntered(string previous, string next) => MarkDirty();

        private void MarkDirty() => _isDirty = true;

        private void Rebuild()
        {
            _isDirty = false;
            View = WorldMapBuilder.Build(_world.Graph, _world, _world.CurrentAreaId,
                _world.CurrentZoneId, _world.IsFastTravelUnlocked, RevealAll,
                Progress != null ? Progress.MapMarks : null);
            Atlas = WorldAtlas.Build(_world.Graph, _world, View, _world.CurrentZoneId);
            ConfigureViewport();
            BuildCount++;
            ViewChanged?.Invoke(View);
        }
    }
}
