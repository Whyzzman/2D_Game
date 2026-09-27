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
    /// The map, the altar's menu and the zone card are drawn by the HUD (MapView, ShrineView);
    /// this panel only reads the world out and breaks its rules for testing. It lives on the
    /// developer-only object, so a release build never has it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldDebugTools : MonoBehaviour
    {
        [SerializeField] private bool _isVisible;
        [SerializeField] private WorldState _world;
        [SerializeField] private WorldMapScreen _map;
        [SerializeField] private FastTravelService _travel;
        [SerializeField] private PlayerRespawner _respawner;

        private readonly StringBuilder _text = new StringBuilder();
        private GUIStyle _style;
        private int _teleportIndex;

        public bool IsVisible => _isVisible;

        private void Awake()
        {
            if (_world == null) { _world = FindAnyObjectByType<WorldState>(); }
            if (_map == null) { _map = FindAnyObjectByType<WorldMapScreen>(); }
            if (_travel == null) { _travel = FindAnyObjectByType<FastTravelService>(); }
            if (_respawner == null) { _respawner = FindAnyObjectByType<PlayerRespawner>(); }
        }

        private void OnEnable()
        {
            SetVisible(_isVisible);
        }

        private void OnDisable()
        {
            DebugPanels.Report(this, false);
        }

        public void SetVisible(bool isVisible)
        {
            _isVisible = isVisible;
            DebugPanels.Report(this, isVisible);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.backquoteKey.wasPressedThisFrame && DebugPanels.MayToggle(this))
            {
                SetVisible(!_isVisible);
            }

            if (!_isVisible || _world == null)
            {
                return;
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

        private void OnGUI()
        {
            if (!_isVisible || _world == null)
            {
                return;
            }

            _style ??= new GUIStyle(GUI.skin.label) { fontSize = 13, richText = true };
            DrawPanel();
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
    }
}
