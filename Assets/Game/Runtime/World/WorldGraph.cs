using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The shape of the kingdom as data: its zones, the areas inside them, how the areas connect,
    /// which altars stand where and what the abilities mean to the world.
    ///
    /// Everything that needs the world's shape reads it from here — the map, fast travel, discovery,
    /// save migration, validation. Nothing about the map is drawn by hand in the interface, and a
    /// new region is new entries in this asset rather than a new screen.
    ///
    /// It replaced the zone registry of PHASE 24: which zone an altar belongs to is now a question
    /// about the checkpoint's entry, answered in the same place as everything else.
    ///
    /// Lookups go through dictionaries built on first use, so a world of fifteen zones costs the
    /// same per query as a world of two.
    /// </summary>
    [CreateAssetMenu(menuName = "Forgotten Kingdom/World Graph", fileName = "WorldGraph")]
    public sealed class WorldGraph : ScriptableObject
    {
        [SerializeField] private ZoneDefinition[] _zones = Array.Empty<ZoneDefinition>();
        [SerializeField] private WorldAreaDefinition[] _areas = Array.Empty<WorldAreaDefinition>();
        [SerializeField] private WorldConnection[] _connections = Array.Empty<WorldConnection>();
        [SerializeField] private CheckpointDefinition[] _checkpoints =
            Array.Empty<CheckpointDefinition>();
        [SerializeField] private AbilityDefinition[] _abilities = Array.Empty<AbilityDefinition>();
        [Tooltip("The world map's pictures: islands, cards, rooms, fog.")]
        [SerializeField] private MapArt _mapArt;
        [Tooltip("The roads of the world map between regions.")]
        [SerializeField] private WorldRoad[] _roads = Array.Empty<WorldRoad>();
        [Tooltip("Where a new game begins; the only area known before the player has moved.")]
        [SerializeField] private string _startAreaId = string.Empty;
        [Tooltip("What must be true before altars offer fast travel.")]
        [SerializeField] private WorldRequirement _fastTravelRequirement;
        [Tooltip("A progression id that unlocks fast travel on its own; the debug command grants it.")]
        [SerializeField] private string _fastTravelOverrideId = "fast-travel";

        private Dictionary<string, ZoneDefinition> _zoneIndex;
        private Dictionary<string, WorldAreaDefinition> _areaIndex;
        private Dictionary<string, WorldConnection> _connectionIndex;
        private Dictionary<string, CheckpointDefinition> _checkpointIndex;
        private Dictionary<string, AbilityDefinition> _abilityIndex;
        private Dictionary<string, List<WorldConnection>> _connectionsByArea;
        private Dictionary<string, List<CheckpointDefinition>> _checkpointsByArea;
        private Dictionary<string, List<WorldAreaDefinition>> _areasByZone;

        public IReadOnlyList<ZoneDefinition> Zones => _zones;
        public IReadOnlyList<WorldAreaDefinition> Areas => _areas;
        public IReadOnlyList<WorldConnection> Connections => _connections;
        public IReadOnlyList<CheckpointDefinition> Checkpoints => _checkpoints;
        public IReadOnlyList<AbilityDefinition> Abilities => _abilities;
        public IReadOnlyList<WorldRoad> Roads => _roads;
        public MapArt MapArt => _mapArt;
        public string StartAreaId => _startAreaId;
        public WorldRequirement FastTravelRequirement => _fastTravelRequirement;
        public string FastTravelOverrideId => _fastTravelOverrideId;

        /// <summary>The zone a run starts in; also the fallback for anything unrecognised.</summary>
        public ZoneDefinition DefaultZone
        {
            get
            {
                var start = Area(_startAreaId);
                return (start != null ? Zone(start.ZoneId) : null)
                    ?? (_zones != null && _zones.Length > 0 ? _zones[0] : null);
            }
        }

        // ---------- lookups ----------

        // Each lookup builds the index first and only then reads the field: passing the field as
        // an argument before the index exists would hand over the null it still holds.
        public ZoneDefinition Zone(string zoneId)
        {
            EnsureIndex();
            return Find(_zoneIndex, zoneId);
        }

        public WorldAreaDefinition Area(string areaId)
        {
            EnsureIndex();
            return Find(_areaIndex, areaId);
        }

        public WorldConnection Connection(string connectionId)
        {
            EnsureIndex();
            return Find(_connectionIndex, connectionId);
        }

        public CheckpointDefinition Checkpoint(string checkpointId)
        {
            EnsureIndex();
            return Find(_checkpointIndex, checkpointId);
        }

        public AbilityDefinition Ability(string abilityId)
        {
            EnsureIndex();
            return Find(_abilityIndex, abilityId);
        }

        public IReadOnlyList<WorldConnection> ConnectionsOf(string areaId)
        {
            EnsureIndex();
            return FindList(_connectionsByArea, areaId);
        }

        public IReadOnlyList<CheckpointDefinition> CheckpointsIn(string areaId)
        {
            EnsureIndex();
            return FindList(_checkpointsByArea, areaId);
        }

        public IReadOnlyList<WorldAreaDefinition> AreasIn(string zoneId)
        {
            EnsureIndex();
            return FindList(_areasByZone, zoneId);
        }

        public ZoneDefinition ZoneForCheckpoint(string checkpointId)
        {
            var checkpoint = Checkpoint(checkpointId);
            return checkpoint != null ? Zone(checkpoint.ZoneId) : null;
        }

        public WorldAreaDefinition AreaForCheckpoint(string checkpointId)
        {
            var checkpoint = Checkpoint(checkpointId);
            return checkpoint != null ? Area(checkpoint.AreaId) : null;
        }

        /// <summary>
        /// The zone a save should come back in. A checkpoint this build does not know — a file from
        /// a later version, or a hand-edited one — falls back to the recorded zone, and then to the
        /// first zone, rather than leaving the player nowhere.
        /// </summary>
        public ZoneDefinition Resolve(string checkpointId, string zoneId)
        {
            return ZoneForCheckpoint(checkpointId) ?? Zone(zoneId) ?? DefaultZone;
        }

        /// <summary>
        /// The area at a point of a scene. Trigger-only areas are skipped — they announce
        /// themselves — and where two areas overlap the smaller, more specific one wins.
        /// </summary>
        public WorldAreaDefinition AreaAt(string sceneName, Vector2 position)
        {
            EnsureIndex();
            WorldAreaDefinition best = null;
            var bestSize = float.MaxValue;
            foreach (var area in _areas)
            {
                if (area == null || area.DiscoveryRule != AreaDiscoveryRule.OnEnter
                    || !area.WorldBounds.Contains(position))
                {
                    continue;
                }

                var zone = Zone(area.ZoneId);
                if (zone != null && !string.IsNullOrEmpty(sceneName)
                    && !string.IsNullOrEmpty(zone.SceneName)
                    && !string.Equals(zone.SceneName, sceneName, StringComparison.Ordinal))
                {
                    continue;
                }

                var size = area.WorldBounds.width * area.WorldBounds.height;
                if (size < bestSize)
                {
                    best = area;
                    bestSize = size;
                }
            }

            return best;
        }

        /// <summary>What the world calls an ability; optional and combat kinds for unknown ids.</summary>
        public AbilityKind KindOf(string abilityId)
        {
            var ability = Ability(abilityId);
            return ability != null ? ability.Kind : AbilityKind.CombatSkill;
        }

        // ---------- index ----------

        private static T Find<T>(Dictionary<string, T> index, string id) where T : class
        {
            return !string.IsNullOrEmpty(id) && index.TryGetValue(id, out var value) ? value : null;
        }

        private static IReadOnlyList<T> FindList<T>(Dictionary<string, List<T>> index, string id)
        {
            return !string.IsNullOrEmpty(id) && index.TryGetValue(id, out var list)
                ? list
                : (IReadOnlyList<T>)Array.Empty<T>();
        }

        private void EnsureIndex()
        {
            if (_areaIndex != null)
            {
                return;
            }

            // First entry wins on a duplicate id; the validator reports the duplicate, the game
            // keeps running on a well-defined answer instead of throwing mid-play.
            _zoneIndex = Index(_zones, zone => zone.ZoneId);
            _areaIndex = Index(_areas, area => area.AreaId);
            _connectionIndex = Index(_connections, connection => connection.ConnectionId);
            _checkpointIndex = Index(_checkpoints, checkpoint => checkpoint.CheckpointId);
            _abilityIndex = Index(_abilities, ability => ability.AbilityId);

            _connectionsByArea = new Dictionary<string, List<WorldConnection>>();
            foreach (var connection in _connections ?? Array.Empty<WorldConnection>())
            {
                if (connection == null)
                {
                    continue;
                }

                Group(_connectionsByArea, connection.FromAreaId, connection);
                if (!string.Equals(connection.FromAreaId, connection.ToAreaId,
                        StringComparison.Ordinal))
                {
                    Group(_connectionsByArea, connection.ToAreaId, connection);
                }
            }

            _checkpointsByArea = new Dictionary<string, List<CheckpointDefinition>>();
            foreach (var checkpoint in _checkpoints ?? Array.Empty<CheckpointDefinition>())
            {
                if (checkpoint != null)
                {
                    Group(_checkpointsByArea, checkpoint.AreaId, checkpoint);
                }
            }

            _areasByZone = new Dictionary<string, List<WorldAreaDefinition>>();
            foreach (var area in _areas ?? Array.Empty<WorldAreaDefinition>())
            {
                if (area != null)
                {
                    Group(_areasByZone, area.ZoneId, area);
                }
            }
        }

        private static Dictionary<string, T> Index<T>(T[] items, Func<T, string> key) where T : class
        {
            var index = new Dictionary<string, T>(StringComparer.Ordinal);
            foreach (var item in items ?? Array.Empty<T>())
            {
                var id = item != null ? key(item) : null;
                if (!string.IsNullOrEmpty(id))
                {
                    index.TryAdd(id, item);
                }
            }

            return index;
        }

        private static void Group<T>(Dictionary<string, List<T>> index, string key, T item)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            if (!index.TryGetValue(key, out var list))
            {
                list = new List<T>();
                index[key] = list;
            }

            list.Add(item);
        }

        private void OnEnable() => ClearIndex();

        private void OnValidate() => ClearIndex();

        private void ClearIndex()
        {
            _zoneIndex = null;
            _areaIndex = null;
            _connectionIndex = null;
            _checkpointIndex = null;
            _abilityIndex = null;
            _connectionsByArea = null;
            _checkpointsByArea = null;
            _areasByZone = null;
        }
    }
}
