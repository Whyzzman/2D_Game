using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>How two areas are joined. The map draws each kind differently.</summary>
    public enum ConnectionType
    {
        Normal,
        Door,
        Gate,
        Elevator,
        /// <summary>A fall. One-way by nature; the way back, if any, is a separate connection.</summary>
        Drop,
        AbilityGate,
        BossGate,
        Shortcut,
        /// <summary>The border between two regions; drawn heavier than an ordinary passage.</summary>
        ZoneTransition
    }

    /// <summary>Which way a connection leaves its first area, as it reads on the map.</summary>
    public enum MapDirection
    {
        East,
        West,
        North,
        South
    }

    /// <summary>
    /// The small symbol an area carries. Only the first six are drawn today; the rest reserve a
    /// place for merchants, people and quests so adding one later is data, not a new screen.
    /// </summary>
    public enum MapIconType
    {
        None,
        Checkpoint,
        Boss,
        Gate,
        FastTravel,
        Secret,
        Merchant,
        Npc,
        Quest,
        SpecialDoor,
        /// <summary>A place a bought rumour points to (item 70). Appended: stored by number.</summary>
        Rumour
    }

    /// <summary>When an area counts as visited.</summary>
    public enum AreaDiscoveryRule
    {
        /// <summary>The moment the player stands inside the area's world bounds.</summary>
        OnEnter,

        /// <summary>
        /// Only when an <see cref="AreaDiscoveryTrigger"/> reports it. Used for secret pockets,
        /// whose bounds sit next to the path: brushing past the ledge must not reveal the room.
        /// </summary>
        OnTrigger
    }

    /// <summary>
    /// What kind of thing an ability is, for the one question the world asks of it: may the main
    /// path depend on it?
    /// </summary>
    public enum AbilityKind
    {
        /// <summary>
        /// Movement that progression guarantees — everyone who gets past its source has it. The
        /// only kind a mandatory gate may ask for.
        /// </summary>
        TraversalProgression,

        /// <summary>
        /// Movement that can be skipped. It may open secrets and alternatives, never the way on.
        /// </summary>
        OptionalTraversal,

        /// <summary>
        /// Part of a build, chosen on the skill tree. A player who never takes it must still be
        /// able to finish the game, so no gate of any importance may ask for it.
        /// </summary>
        CombatSkill
    }

    /// <summary>What a gate, a door or a connection waits for.</summary>
    public enum RequirementKind
    {
        None,
        Ability,
        BossDefeated,
        /// <summary>A progression reward or key item the player was given (e.g. a royal seal).</summary>
        Progression,
        Shortcut,
        /// <summary>Content that does not exist yet. Never satisfied; the map shows it as sealed.</summary>
        Unavailable
    }

    /// <summary>
    /// One condition, stored as data so a door, a gate component and the map all read the same
    /// thing instead of each spelling out its own rule.
    /// </summary>
    [Serializable]
    public struct WorldRequirement
    {
        [SerializeField] private RequirementKind _kind;
        [SerializeField] private string _id;

        public WorldRequirement(RequirementKind kind, string id)
        {
            _kind = kind;
            _id = id ?? string.Empty;
        }

        public RequirementKind Kind => _kind;
        public string Id => _id ?? string.Empty;
        public bool IsNone => _kind == RequirementKind.None;

        public static WorldRequirement None => new WorldRequirement(RequirementKind.None, string.Empty);

        public override string ToString() => IsNone ? "none" : $"{_kind}:{Id}";
    }

    /// <summary>A region of the kingdom.</summary>
    [Serializable]
    public sealed class ZoneDefinition
    {
        [SerializeField] private string _zoneId = string.Empty;
        [Tooltip("Localisation key or plain name; the map header and the title card read this.")]
        [SerializeField] private string _displayName = string.Empty;
        [Tooltip("The short card shown the first time the zone is entered.")]
        [SerializeField] private string _titleCard = string.Empty;
        [Tooltip("Scene holding the zone. Both zones share one scene today; the field is what "
            + "lets that stop being true without hunting for scene names elsewhere.")]
        [SerializeField] private string _sceneName = string.Empty;
        [Tooltip("Where the player appears entering this zone with no altar lit in it.")]
        [SerializeField] private Vector2 _defaultSpawn;
        [Tooltip("False for a region that is only hinted at. Nothing in it can be travelled to.")]
        [SerializeField] private bool _isImplemented = true;
        [Tooltip("Tint the map uses for this region's rooms.")]
        [SerializeField] private Color _mapTint = new Color(0.78f, 0.7f, 0.52f);

        public string ZoneId => _zoneId;
        public string DisplayName => _displayName;
        public string TitleCard => string.IsNullOrEmpty(_titleCard) ? _displayName : _titleCard;
        public string SceneName => _sceneName;
        public Vector2 DefaultSpawn => _defaultSpawn;
        public bool IsImplemented => _isImplemented;
        public Color MapTint => _mapTint;
    }

    /// <summary>
    /// One room or stretch of a zone. Areas are what the map draws and what discovery records.
    ///
    /// Connections and checkpoints are not duplicated here: the graph indexes them by area, so an
    /// area's exits and altars always agree with the connection and checkpoint lists.
    /// </summary>
    [Serializable]
    public sealed class WorldAreaDefinition
    {
        [SerializeField] private string _areaId = string.Empty;
        [SerializeField] private string _zoneId = string.Empty;
        [SerializeField] private string _displayName = string.Empty;
        [Tooltip("Centre of the room block on the map, in map units.")]
        [SerializeField] private Vector2 _mapPosition;
        [SerializeField] private Vector2 _mapSize = new Vector2(4f, 2f);
        [Tooltip("Where the area is in its scene. Used to tell which area the player stands in.")]
        [SerializeField] private Rect _worldBounds;
        [SerializeField] private bool _isSecret;
        [Tooltip("Off the main path. The validator never counts on reaching it.")]
        [SerializeField] private bool _isOptional;
        [SerializeField] private AreaDiscoveryRule _discoveryRule = AreaDiscoveryRule.OnEnter;
        [Tooltip("The ability this place is associated with, for the map's hint only. The level "
            + "geometry is what actually keeps the player out.")]
        [SerializeField] private string _requiredAbilityId = string.Empty;
        [SerializeField] private MapIconType _iconType = MapIconType.None;
        [Tooltip("For a boss arena: whose. The icon never shows before the arena is discovered.")]
        [SerializeField] private string _bossId = string.Empty;

        public string AreaId => _areaId;
        public string ZoneId => _zoneId;
        public string DisplayName => _displayName;
        public Vector2 MapPosition => _mapPosition;
        public Vector2 MapSize => _mapSize;
        public Rect WorldBounds => _worldBounds;
        public bool IsSecret => _isSecret;
        public bool IsOptional => _isOptional || _isSecret;
        public AreaDiscoveryRule DiscoveryRule => _discoveryRule;
        public string RequiredAbilityId => _requiredAbilityId;
        public MapIconType IconType => _iconType;
        public string BossId => _bossId;
        public bool IsBossArena => !string.IsNullOrEmpty(_bossId);
        public Rect MapRect => new Rect(_mapPosition - _mapSize * 0.5f, _mapSize);
    }

    /// <summary>
    /// A way from one area to another. A bidirectional connection is one entry that resolves both
    /// ways; a drop or a one-way door is marked so, and its way back — if it has one — is another
    /// entry, which keeps the difference between "no way back" and "a forgotten way back" visible.
    /// </summary>
    [Serializable]
    public sealed class WorldConnection
    {
        [SerializeField] private string _connectionId = string.Empty;
        [SerializeField] private string _fromAreaId = string.Empty;
        [SerializeField] private string _toAreaId = string.Empty;
        [SerializeField] private MapDirection _direction = MapDirection.East;
        [SerializeField] private ConnectionType _type = ConnectionType.Normal;
        [SerializeField] private bool _isBidirectional = true;
        [SerializeField] private string _requiredAbilityId = string.Empty;
        [Tooltip("A progression reward or key item id.")]
        [SerializeField] private string _requiredProgressionId = string.Empty;
        [SerializeField] private string _requiredBossId = string.Empty;
        [SerializeField] private bool _isShortcut;
        [Tooltip("The shortcut id whose unlocking opens this connection for good.")]
        [SerializeField] private string _persistentUnlockId = string.Empty;
        [Tooltip("For a shortcut: the area its lever stands in. Reaching that area is enough to "
            + "open it, which is how the validator tells a door opened from the far side from a "
            + "door nobody can open.")]
        [SerializeField] private string _openedFromAreaId = string.Empty;
        [Tooltip("Leads somewhere that is not built yet. Drawn sealed, never travelled.")]
        [SerializeField] private bool _isFuture;

        public string ConnectionId => _connectionId;
        public string FromAreaId => _fromAreaId;
        public string ToAreaId => _toAreaId;
        public MapDirection Direction => _direction;
        public ConnectionType Type => _type;
        public bool IsBidirectional => _isBidirectional && _type != ConnectionType.Drop;
        public string RequiredAbilityId => _requiredAbilityId;
        public string RequiredProgressionId => _requiredProgressionId;
        public string RequiredBossId => _requiredBossId;
        public bool IsShortcut => _isShortcut || _type == ConnectionType.Shortcut;
        public string PersistentUnlockId => _persistentUnlockId;
        public string OpenedFromAreaId => _openedFromAreaId;
        public bool IsFuture => _isFuture;
        public bool IsZoneTransition => _type == ConnectionType.ZoneTransition;

        /// <summary>
        /// Every condition on this connection. All of them must hold; an empty list means open.
        /// </summary>
        public WorldRequirement[] Requirements()
        {
            var list = new System.Collections.Generic.List<WorldRequirement>(4);
            if (_isFuture)
            {
                list.Add(new WorldRequirement(RequirementKind.Unavailable, _toAreaId));
            }

            if (!string.IsNullOrEmpty(_requiredAbilityId))
            {
                list.Add(new WorldRequirement(RequirementKind.Ability, _requiredAbilityId));
            }

            if (!string.IsNullOrEmpty(_requiredBossId))
            {
                list.Add(new WorldRequirement(RequirementKind.BossDefeated, _requiredBossId));
            }

            if (!string.IsNullOrEmpty(_requiredProgressionId))
            {
                list.Add(new WorldRequirement(RequirementKind.Progression, _requiredProgressionId));
            }

            if (IsShortcut && !string.IsNullOrEmpty(_persistentUnlockId))
            {
                list.Add(new WorldRequirement(RequirementKind.Shortcut, _persistentUnlockId));
            }

            return list.ToArray();
        }

        /// <summary>True when the player can go from <paramref name="areaId"/> through this.</summary>
        public bool LeadsFrom(string areaId)
        {
            return string.Equals(_fromAreaId, areaId, StringComparison.Ordinal)
                || (IsBidirectional && string.Equals(_toAreaId, areaId, StringComparison.Ordinal));
        }

        /// <summary>The far end, seen from <paramref name="areaId"/>.</summary>
        public string Other(string areaId)
        {
            return string.Equals(_fromAreaId, areaId, StringComparison.Ordinal) ? _toAreaId : _fromAreaId;
        }

        public bool Touches(string areaId)
        {
            return string.Equals(_fromAreaId, areaId, StringComparison.Ordinal)
                || string.Equals(_toAreaId, areaId, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// The database entry for an altar. The scene's <see cref="Checkpoint"/> carries only the id;
    /// what it is called, where it is and whether it can be travelled to live here, once.
    /// </summary>
    [Serializable]
    public sealed class CheckpointDefinition
    {
        [SerializeField] private string _checkpointId = string.Empty;
        [SerializeField] private string _displayName = string.Empty;
        [SerializeField] private string _zoneId = string.Empty;
        [SerializeField] private string _areaId = string.Empty;
        [SerializeField] private bool _canFastTravel = true;

        public string CheckpointId => _checkpointId;
        public string DisplayName => _displayName;
        public string ZoneId => _zoneId;
        public string AreaId => _areaId;
        public bool CanFastTravel => _canFastTravel;
    }

    /// <summary>
    /// What the world knows about an ability: what kind it is and, for progression movement, which
    /// boss guarantees it. The movement code still asks only "does the player have it".
    /// </summary>
    [Serializable]
    public sealed class AbilityDefinition
    {
        [SerializeField] private string _abilityId = string.Empty;
        [SerializeField] private string _displayName = string.Empty;
        [SerializeField] private AbilityKind _kind = AbilityKind.OptionalTraversal;
        [Tooltip("For guaranteed movement: the boss whose defeat hands it over. Empty means the "
            + "player has it from the start.")]
        [SerializeField] private string _guaranteedByBossId = string.Empty;

        public string AbilityId => _abilityId;
        public string DisplayName => _displayName;
        public AbilityKind Kind => _kind;
        public string GuaranteedByBossId => _guaranteedByBossId;
    }
}
