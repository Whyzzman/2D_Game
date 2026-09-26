using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The one place the world's persistent state is asked about and where the player is in it.
    ///
    /// Doors, gates, shortcuts, the map and fast travel talk to this; none of them parses the save.
    /// It answers the <see cref="IWorldQuery"/> questions from <see cref="GameProgress"/>, knows the
    /// current zone and area, records discovery, and turns a zone change into what the player hears
    /// and sees — the region's music and ambience, and its title card the first time only.
    ///
    /// Where the player stands is read a few times a second against the graph's area bounds,
    /// plus whatever an <see cref="AreaDiscoveryTrigger"/> reports, and immediately after anything
    /// moves the player outright (a respawn, a fast travel). Nothing is recomputed per frame and
    /// nothing searches the scene.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-50)]
    public sealed class WorldState : MonoBehaviour, IWorldQuery
    {
        [SerializeField] private GameProgress _progress;
        [SerializeField] private PlayerCharacter _player;
        [SerializeField] private PlayerRespawner _respawner;
        [Tooltip("How often the player's area is re-read, in seconds of game time.")]
        [SerializeField, Min(0.02f)] private float _pollInterval = 0.2f;

        private readonly List<string> _triggerAreas = new List<string>();
        private PlayerMotor2D _motor;
        private float _nextPoll;
        private bool _hasPendingSave;
        private string _sceneName = string.Empty;

        /// <summary>The live instance, for components that cannot be wired in the Inspector.</summary>
        public static WorldState Current { get; private set; }

        /// <summary>A durable fact changed: an area found, a shortcut opened, a boss beaten.</summary>
        public event Action<WorldFlag, string> Changed;

        /// <summary>Progress was replaced wholesale — a load or a new game. Re-read everything.</summary>
        public event Action Reloaded;

        /// <summary>Previous area id, new area id. Raised on every change, known or new.</summary>
        public event Action<string, string> AreaEntered;

        /// <summary>Raised when the region changes; presentation follows it.</summary>
        public event Action<ZoneDefinition> ZoneEntered;

        /// <summary>The first time a region is entered; the HUD shows its title card.</summary>
        public event Action<ZoneDefinition> ZoneTitleRequested;

        public WorldGraph Graph => _progress != null ? _progress.World : null;
        public GameProgress Progress => _progress;
        public string CurrentAreaId { get; private set; } = string.Empty;
        public string CurrentZoneId { get; private set; } = string.Empty;
        public WorldAreaDefinition CurrentArea => Graph != null ? Graph.Area(CurrentAreaId) : null;
        public ZoneDefinition CurrentZone => Graph != null ? Graph.Zone(CurrentZoneId) : null;

        /// <summary>The last title card asked for, for anything that starts listening late.</summary>
        public ZoneDefinition LastTitleCard { get; private set; }

        private void Awake()
        {
            if (_progress == null) { _progress = FindAnyObjectByType<GameProgress>(); }
            if (_player == null) { _player = FindAnyObjectByType<PlayerCharacter>(); }
            if (_respawner == null) { _respawner = FindAnyObjectByType<PlayerRespawner>(); }
            _motor = _player != null ? _player.GetComponent<PlayerMotor2D>() : null;
            _sceneName = gameObject.scene.name;
        }

        private void OnEnable()
        {
            if (Current != null && Current != this)
            {
                Debug.LogError("A second WorldState was found; the world can only have one.", this);
                enabled = false;
                return;
            }

            Current = this;
            if (_progress != null)
            {
                _progress.WorldChanged += OnWorldChanged;
                _progress.Loaded += OnLoaded;
            }

            if (_motor != null)
            {
                _motor.Respawned += RefreshNow;
            }
        }

        private void OnDisable()
        {
            if (Current == this)
            {
                Current = null;
            }

            if (_progress != null)
            {
                _progress.WorldChanged -= OnWorldChanged;
                _progress.Loaded -= OnLoaded;
            }

            if (_motor != null)
            {
                _motor.Respawned -= RefreshNow;
            }
        }

        private void Start()
        {
            RefreshNow();
        }

        private void Update()
        {
            if (_hasPendingSave && !IsTransitioning)
            {
                // Discoveries are not worth a write of their own mid-transition; the transition
                // saves once the player is standing somewhere real.
                _hasPendingSave = false;
                _progress?.Save();
            }

            if (Time.time < _nextPoll)
            {
                return;
            }

            _nextPoll = Time.time + _pollInterval;
            Refresh();
        }

        /// <summary>True while the respawner is moving the player; nothing is saved meanwhile.</summary>
        public bool IsTransitioning => _respawner != null && _respawner.IsSequenceRunning;

        // ---------- where the player is ----------

        /// <summary>Re-reads the player's area at once; called after anything teleports them.</summary>
        public void RefreshNow()
        {
            _nextPoll = Time.time + _pollInterval;
            Refresh();
        }

        /// <summary>An area trigger reports the player entering or leaving it.</summary>
        public void ReportTrigger(string areaId, bool isInside)
        {
            if (string.IsNullOrEmpty(areaId))
            {
                return;
            }

            _triggerAreas.Remove(areaId);
            if (isInside)
            {
                _triggerAreas.Add(areaId);
            }

            RefreshNow();
        }

        private void Refresh()
        {
            var graph = Graph;
            if (graph == null || _player == null)
            {
                return;
            }

            var area = _triggerAreas.Count > 0
                ? graph.Area(_triggerAreas[_triggerAreas.Count - 1])
                : graph.AreaAt(_sceneName, _player.transform.position);
            if (area == null || string.Equals(area.AreaId, CurrentAreaId, StringComparison.Ordinal))
            {
                // Between two areas' bounds the player is still where they last were.
                return;
            }

            EnterArea(area);
        }

        /// <summary>Makes <paramref name="area"/> current: discovery, then the zone if it changed.</summary>
        public void EnterArea(WorldAreaDefinition area)
        {
            if (area == null)
            {
                return;
            }

            var previous = CurrentAreaId;
            CurrentAreaId = area.AreaId;
            if (_progress != null && _progress.MarkAreaDiscovered(area.AreaId))
            {
                _hasPendingSave = true;
            }

            AreaEntered?.Invoke(previous, area.AreaId);
            if (!string.Equals(area.ZoneId, CurrentZoneId, StringComparison.Ordinal))
            {
                EnterZone(Graph.Zone(area.ZoneId));
            }
        }

        private void EnterZone(ZoneDefinition zone)
        {
            if (zone == null)
            {
                return;
            }

            CurrentZoneId = zone.ZoneId;
            // The region's sound follows the region. Both calls ignore a repeat, so walking back
            // and forth over a border cannot restart anything.
            if (MusicDirector.Instance != null)
            {
                MusicDirector.Instance.SetZone(zone.ZoneId);
            }

            var ambience = FindAnyObjectByType<AmbienceController>();
            if (ambience != null)
            {
                ambience.SetZone(zone.ZoneId);
            }

            ZoneEntered?.Invoke(zone);
            if (_progress != null && _progress.MarkZoneIntroSeen(zone.ZoneId))
            {
                // Not worth a write of its own: it rides along with the next real save.
                LastTitleCard = zone;
                ZoneTitleRequested?.Invoke(zone);
            }
        }

        // ---------- queries ----------

        public bool IsAreaDiscovered(string areaId) =>
            _progress != null && _progress.IsAreaDiscovered(areaId);

        public bool IsShortcutUnlocked(string shortcutId) =>
            _progress != null && _progress.IsShortcutUnlocked(shortcutId);

        public bool IsBossDefeated(string bossId) =>
            _progress != null && _progress.IsBossDefeated(bossId);

        public bool IsCheckpointActivated(string checkpointId) =>
            _progress != null && _progress.IsCheckpointActivated(checkpointId);

        /// <summary>
        /// The ability question, for movement and for gates alike. It never looks at the skill
        /// tree: a skill that moves the player is still a combat skill to the world.
        /// </summary>
        public bool HasAbility(string abilityId) =>
            _progress != null && _progress.HasAbility(abilityId);

        public bool HasProgression(string progressionId) =>
            _progress != null && _progress.HasProgression(progressionId);

        public bool IsWorldPickupCollected(string pickupId) =>
            _progress != null && _progress.IsPickupCollected(pickupId);

        public bool IsMet(WorldRequirement requirement) => WorldRules.IsMet(requirement, this);

        public bool IsFastTravelUnlocked => WorldRules.IsFastTravelUnlocked(Graph, this);

        // ---------- changes ----------

        /// <summary>Opens a shortcut for good and writes it; a repeat does nothing.</summary>
        public bool UnlockShortcut(string shortcutId)
        {
            if (_progress == null || !_progress.UnlockShortcut(shortcutId))
            {
                return false;
            }

            _progress.Save();
            return true;
        }

        private void OnWorldChanged(WorldFlag flag, string id)
        {
            Changed?.Invoke(flag, id);
        }

        private void OnLoaded()
        {
            // A load can put the player in another zone entirely; forget where they were.
            CurrentAreaId = string.Empty;
            CurrentZoneId = string.Empty;
            _triggerAreas.Clear();
            Reloaded?.Invoke();
            RefreshNow();
        }
    }
}
