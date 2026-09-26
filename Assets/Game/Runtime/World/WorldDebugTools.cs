using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ForgottenKingdom
{
    /// <summary>
    /// Development-only view of the world graph and the commands to test it.
    ///
    /// ` toggles the panel. While it is up: G discovers the current area, H every area, U opens
    /// every shortcut, T unlocks fast travel, O grants the double jump, P teleports to the next
    /// altar, V reveals hidden rooms on the map.
    ///
    /// While the panel is up it also stands in for the interface that does not exist yet: it
    /// presents the altar's menu (arrows, Enter, Esc) and draws the map when it is open, so the
    /// whole loop can be tried by hand in the Editor. It lives on the developer-only object, so a
    /// release build never has it; nothing in it is reachable from any production screen.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldDebugTools : MonoBehaviour
    {
        [SerializeField] private bool _isVisible;
        [SerializeField] private WorldState _world;
        [SerializeField] private WorldMapScreen _map;
        [SerializeField] private ShrineMenu _shrine;
        [SerializeField] private FastTravelService _travel;
        [SerializeField] private PlayerRespawner _respawner;

        private readonly StringBuilder _text = new StringBuilder();
        private GUIStyle _style;
        private GUIStyle _small;
        private int _shrineCursor;
        private int _teleportIndex;
        private string _titleCard = string.Empty;
        private float _titleCardUntil;

        public bool IsVisible => _isVisible;

        private void Awake()
        {
            if (_world == null) { _world = FindAnyObjectByType<WorldState>(); }
            if (_map == null) { _map = FindAnyObjectByType<WorldMapScreen>(); }
            if (_shrine == null) { _shrine = FindAnyObjectByType<ShrineMenu>(); }
            if (_travel == null) { _travel = FindAnyObjectByType<FastTravelService>(); }
            if (_respawner == null) { _respawner = FindAnyObjectByType<PlayerRespawner>(); }
        }

        private void OnEnable()
        {
            if (_world != null) { _world.ZoneTitleRequested += ShowTitleCard; }
            // Until the real map is drawn, this readout is how a developer build shows it.
            _map?.AddPresenter(this);
            SetVisible(_isVisible);
        }

        private void OnDisable()
        {
            if (_world != null) { _world.ZoneTitleRequested -= ShowTitleCard; }
            _shrine?.RemovePresenter(this);
            _map?.RemovePresenter(this);
        }

        public void SetVisible(bool isVisible)
        {
            _isVisible = isVisible;
            // Only while the panel is up does an altar open its menu; otherwise it rests at once,
            // exactly as a player without an interface would expect.
            if (_shrine == null)
            {
                return;
            }

            if (isVisible)
            {
                _shrine.AddPresenter(this);
            }
            else
            {
                _shrine.RemovePresenter(this);
            }
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.backquoteKey.wasPressedThisFrame)
            {
                SetVisible(!_isVisible);
            }

            if (!_isVisible || _world == null)
            {
                return;
            }

            if (_shrine != null && _shrine.IsOpen)
            {
                DriveShrineMenu(keyboard);
                return;
            }

            if (_map != null && _map.IsOpen)
            {
                DriveMap(keyboard);
            }

            if (keyboard.gKey.wasPressedThisFrame) { DiscoverCurrentArea(); }
            if (keyboard.hKey.wasPressedThisFrame) { DiscoverAllAreas(); }
            if (keyboard.uKey.wasPressedThisFrame) { UnlockAllShortcuts(); }
            if (keyboard.tKey.wasPressedThisFrame) { UnlockFastTravel(); }
            if (keyboard.oKey.wasPressedThisFrame) { _world.Progress?.UnlockAbility(AbilityIds.DoubleJump); }
            if (keyboard.pKey.wasPressedThisFrame) { TeleportToNextCheckpoint(); }
            if (keyboard.vKey.wasPressedThisFrame && _map != null)
            {
                _map.RevealAll = !_map.RevealAll;
            }
        }

        // ---------- commands ----------

        public void DiscoverCurrentArea()
        {
            _world.Progress?.MarkAreaDiscovered(_world.CurrentAreaId);
        }

        public void DiscoverAllAreas()
        {
            var graph = _world.Graph;
            if (graph == null)
            {
                return;
            }

            foreach (var area in graph.Areas)
            {
                _world.Progress.MarkAreaDiscovered(area.AreaId);
            }
        }

        public void UnlockAllShortcuts()
        {
            var graph = _world.Graph;
            if (graph == null)
            {
                return;
            }

            foreach (var connection in graph.Connections)
            {
                if (connection.IsShortcut && !string.IsNullOrEmpty(connection.PersistentUnlockId))
                {
                    _world.UnlockShortcut(connection.PersistentUnlockId);
                }
            }
        }

        public void UnlockFastTravel()
        {
            var graph = _world.Graph;
            if (graph != null && _world.Progress != null)
            {
                _world.Progress.GrantReward(graph.FastTravelOverrideId);
                _world.Progress.Save();
            }
        }

        /// <summary>
        /// Goes through the respawner's own journey, so the fade, the reset and the save behave as
        /// they would in play — but skips every rule, which is why it exists only here.
        /// </summary>
        public void TeleportToNextCheckpoint()
        {
            var graph = _world.Graph;
            if (graph == null || graph.Checkpoints.Count == 0 || _respawner == null)
            {
                return;
            }

            for (var tries = 0; tries < graph.Checkpoints.Count; tries++)
            {
                _teleportIndex = (_teleportIndex + 1) % graph.Checkpoints.Count;
                var target = _respawner.FindCheckpoint(graph.Checkpoints[_teleportIndex].CheckpointId);
                if (target != null)
                {
                    _world.Progress?.MarkCheckpointActivated(target.CheckpointId);
                    _respawner.BeginTravel(target);
                    return;
                }
            }
        }

        // ---------- stand-ins for the interface ----------

        private void DriveShrineMenu(Keyboard keyboard)
        {
            var destinations = _travel != null && _shrine.Origin != null
                ? _travel.Destinations(_shrine.Origin.CheckpointId)
                : null;
            var count = 2 + (destinations?.Count ?? 0);
            if (keyboard.downArrowKey.wasPressedThisFrame) { _shrineCursor = (_shrineCursor + 1) % count; }
            if (keyboard.upArrowKey.wasPressedThisFrame) { _shrineCursor = (_shrineCursor + count - 1) % count; }
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                _shrine.Close();
                return;
            }

            if (!keyboard.enterKey.wasPressedThisFrame)
            {
                return;
            }

            if (_shrineCursor == 0)
            {
                _shrine.Rest();
            }
            else if (_shrineCursor == 1)
            {
                _shrine.Close();
            }
            else if (destinations != null)
            {
                _shrine.TravelTo(destinations[_shrineCursor - 2].CheckpointId);
            }

            _shrineCursor = 0;
        }

        private void DriveMap(Keyboard keyboard)
        {
            var direction = Vector2.zero;
            if (keyboard.leftArrowKey.wasPressedThisFrame) { direction += Vector2.left; }
            if (keyboard.rightArrowKey.wasPressedThisFrame) { direction += Vector2.right; }
            if (keyboard.upArrowKey.wasPressedThisFrame) { direction += Vector2.up; }
            if (keyboard.downArrowKey.wasPressedThisFrame) { direction += Vector2.down; }
            if (direction != Vector2.zero) { _map.MoveCursor(direction); }
            if (keyboard.pageUpKey.wasPressedThisFrame) { _map.Zoom(1.25f); }
            if (keyboard.pageDownKey.wasPressedThisFrame) { _map.Zoom(0.8f); }
        }

        private void ShowTitleCard(ZoneDefinition zone)
        {
            _titleCard = zone != null ? zone.TitleCard : string.Empty;
            _titleCardUntil = Time.unscaledTime + 3f;
        }

        private void OnGUI()
        {
            if (!_isVisible || _world == null)
            {
                return;
            }

            _style ??= new GUIStyle(GUI.skin.label) { fontSize = 13, richText = true };
            _small ??= new GUIStyle(GUI.skin.label) { fontSize = 10, alignment = TextAnchor.MiddleCenter };
            DrawPanel();
            if (_shrine != null && _shrine.IsOpen)
            {
                DrawShrine();
            }

            if (_map != null && _map.IsOpen && _map.View != null)
            {
                DrawMap(_map.View);
            }

            if (Time.unscaledTime < _titleCardUntil)
            {
                var big = new GUIStyle(_style) { fontSize = 28, alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(0f, Screen.height * 0.3f, Screen.width, 40f),
                    _titleCard.ToUpperInvariant(), big);
            }
        }

        private void DrawPanel()
        {
            var graph = _world.Graph;
            _text.Clear();
            _text.AppendLine("WORLD  ` hide · G area · H all areas · U shortcuts · T fast travel · "
                + "O double jump · P teleport · V reveal");
            _text.Append("  zone: ").Append(_world.CurrentZoneId)
                .Append("   area: ").AppendLine(_world.CurrentAreaId);
            if (graph != null)
            {
                _text.Append("  discovered: ").Append(_world.Progress.DiscoveredAreas.Count)
                    .Append('/').Append(graph.Areas.Count)
                    .Append("   shortcuts: ").Append(_world.Progress.UnlockedShortcuts.Count)
                    .Append("   fast travel: ").AppendLine(_world.IsFastTravelUnlocked ? "open" : "locked");
                foreach (var connection in graph.ConnectionsOf(_world.CurrentAreaId))
                {
                    var requirements = connection.Requirements();
                    if (requirements.Length == 0)
                    {
                        continue;
                    }

                    _text.Append("  gate ").Append(connection.ConnectionId).Append(": ")
                        .Append(string.Join(", ", System.Array.ConvertAll(requirements, r => r.ToString())))
                        .AppendLine(WorldRules.IsOpen(connection, _world) ? "  [open]" : "  [shut]");
                }
            }

            _text.Append("  nearest altar: ").AppendLine(NearestCheckpoint());
            if (_travel != null && !string.IsNullOrEmpty(_travel.LastMessage))
            {
                _text.Append("  travel: ").AppendLine(_travel.LastMessage);
            }

            GUI.Label(new Rect(12f, 460f, 900f, 170f), _text.ToString(), _style);
        }

        private string NearestCheckpoint()
        {
            var graph = _world.Graph;
            if (graph == null || _respawner == null)
            {
                return "-";
            }

            var player = FindAnyObjectByType<PlayerCharacter>();
            string best = "-";
            var bestDistance = float.MaxValue;
            foreach (var definition in graph.Checkpoints)
            {
                var checkpoint = _respawner.FindCheckpoint(definition.CheckpointId);
                if (checkpoint == null || player == null)
                {
                    continue;
                }

                var distance = Mathf.Abs(checkpoint.transform.position.x - player.transform.position.x);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = $"{definition.DisplayName} ({definition.CheckpointId}, {distance:0} m"
                        + (_world.IsCheckpointActivated(definition.CheckpointId) ? ", lit)" : ", unlit)");
                }
            }

            return best;
        }

        private void DrawShrine()
        {
            _text.Clear();
            _text.AppendLine($"<b>{_shrine.Origin.DisplayName}</b>   ↑↓ Enter · Esc");
            _text.AppendLine((_shrineCursor == 0 ? "> " : "  ") + "Відпочити");
            _text.AppendLine((_shrineCursor == 1 ? "> " : "  ") + "Піти");
            if (_travel != null)
            {
                var destinations = _travel.Destinations(_shrine.Origin.CheckpointId);
                _text.AppendLine(_travel.IsUnlocked ? "Мандрувати:" : "Мандрувати — ще закрито");
                for (var i = 0; i < destinations.Count; i++)
                {
                    var d = destinations[i];
                    _text.Append(_shrineCursor == i + 2 ? "> " : "  ")
                        .Append(d.Zone != null ? d.Zone.DisplayName : "?").Append(" — ")
                        .Append(d.Checkpoint.DisplayName)
                        .AppendLine(d.IsCurrent ? "  (тут)" : d.IsSelectable ? string.Empty
                            : "  · " + FastTravelService.Describe(d.Availability));
                }
            }

            GUI.Box(new Rect(Screen.width - 430f, 40f, 410f, 260f), GUIContent.none);
            GUI.Label(new Rect(Screen.width - 420f, 46f, 400f, 250f), _text.ToString(), _style);
        }

        /// <summary>A plain schematic, not the map's art; enough to check what the model says.</summary>
        private void DrawMap(WorldMapView view)
        {
            var frame = new Rect(40f, 40f, Screen.width - 80f, 380f);
            GUI.Box(frame, view.ZoneHeader + "   ←↑→↓ cursor · PgUp/PgDn zoom · M close");
            var visible = _map.Viewport.Visible;
            Vector2 ToScreen(Vector2 p) => new Vector2(
                frame.x + (p.x - visible.xMin) / visible.width * frame.width,
                frame.yMax - (p.y - visible.yMin) / visible.height * frame.height);

            foreach (var link in view.Links)
            {
                var a = view.Node(link.Connection.FromAreaId);
                var b = view.Node(link.Connection.ToAreaId);
                if (a == null || b == null)
                {
                    continue;
                }

                var mid = (ToScreen(a.Area.MapPosition) + ToScreen(b.Area.MapPosition)) * 0.5f;
                var mark = link.State == MapLinkState.Open ? (link.IsZoneTransition ? "══" : "—")
                    : link.State == MapLinkState.ShortcutLocked ? "┄" : link.State == MapLinkState.Sealed ? "▣" : "✕";
                GUI.Label(new Rect(mid.x - 12f, mid.y - 8f, 24f, 16f), mark, _small);
            }

            foreach (var node in view.Nodes)
            {
                var min = ToScreen(new Vector2(node.Rect.xMin, node.Rect.yMax));
                var max = ToScreen(new Vector2(node.Rect.xMax, node.Rect.yMin));
                var rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
                if (!rect.Overlaps(frame))
                {
                    continue;
                }

                var label = node.State == MapNodeState.Unexplored ? "?" : node.Label;
                foreach (var icon in node.Icons)
                {
                    label += " " + icon.Type.ToString()[0] + (icon.IsActive ? "" : "°");
                }

                if (node.IsCurrent) { label = "◆ " + label; }
                if (node.AreaId == _map.SelectedAreaId) { label = "[" + label + "]"; }
                GUI.Box(rect, label, _small);
            }
        }
    }
}
