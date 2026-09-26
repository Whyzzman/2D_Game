using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>Where in its cycle a hazard currently is.</summary>
    public enum HazardPhase
    {
        /// <summary>Harmless and quiet; a repeating hazard waits here between bursts.</summary>
        Dormant,
        /// <summary>Visibly gathering. Nothing damages yet — this is the window to leave.</summary>
        Telegraph,
        Active,
        /// <summary>Spent. A one-shot zone destroys itself from here.</summary>
        Finished
    }

    /// <summary>
    /// A patch of ground that hurts to stand in: a poison plant's spore cloud, a shaman's toxic
    /// pool, the Warden's root eruption.
    ///
    /// It adds buildup through the existing <see cref="StatusEffects"/> rather than inventing an
    /// environmental poison of its own, so resistances, the Venom Charm and a poison build all
    /// behave here exactly as they do against a poisoned blade.
    ///
    /// Nothing ever becomes dangerous without first being visible: every zone passes through
    /// <see cref="HazardPhase.Telegraph"/>, including one dropped under the player's feet. A hazard
    /// that damaged on the frame it appeared would be unfair rather than difficult.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HazardZone : MonoBehaviour
    {
        [Header("Shape")]
        [SerializeField] private Vector2 _size = new Vector2(2.4f, 1.4f);
        [SerializeField] private Vector2 _offset = Vector2.zero;
        [SerializeField] private LayerMask _targetMask;

        [Header("Timing")]
        [Tooltip("Visible gathering before anything can be hurt. Never skipped.")]
        [SerializeField, Min(0.15f)] private float _telegraphTime = 0.7f;
        [SerializeField, Min(0.05f)] private float _activeTime = 2.2f;
        [Tooltip("Repeating hazards wait this long between bursts; 0 makes it a one-shot.")]
        [SerializeField, Min(0f)] private float _dormantTime = 2.6f;

        [Header("Effect")]
        [SerializeField] private StatusType _status = StatusType.Poison;
        [Tooltip("Buildup per second while standing inside.")]
        [SerializeField, Min(0f)] private float _buildupPerSecond = 45f;
        [Tooltip("Direct damage per tick; 0 makes this purely a status hazard.")]
        [SerializeField, Min(0f)] private float _damagePerTick = 0f;
        [SerializeField, Min(0.05f)] private float _tickInterval = 0.5f;
        [SerializeField] private DamageType _damageType = DamageType.Poison;
        [SerializeField, Min(0f)] private float _knockback = 0f;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer _visual;
        [SerializeField] private Color _telegraphColor = new Color(0.55f, 0.85f, 0.35f, 0.35f);
        [SerializeField] private Color _activeColor = new Color(0.45f, 0.95f, 0.3f, 0.7f);

        private readonly Collider2D[] _overlaps = new Collider2D[8];
        private ContactFilter2D _filter;
        private Faction _faction = Faction.Enemy;
        private GameObject _owner;
        private HazardPhase _phase = HazardPhase.Dormant;
        private float _phaseTimer;
        private float _tickTimer;
        private bool _isSpawned;

        /// <summary>Every zone dropped by an attack, so a reset can sweep them all up.</summary>
        private static readonly List<HazardZone> Spawned = new List<HazardZone>();

        /// <summary>Raised when the zone turns dangerous; the hook for its sound and its VFX.</summary>
        public event Action<HazardZone> Activated;

        /// <summary>
        /// The same moment for any zone at all. A spawned pool does not exist when the audio binder
        /// starts, so there is nothing for it to subscribe to one by one; this is the single place
        /// it can listen without the hazard needing to know what audio is.
        /// </summary>
        public static event Action<HazardZone> AnyActivated;

        /// <summary>Raised when a zone begins visibly gathering, one beat before it bites.</summary>
        public static event Action<HazardZone> AnyTelegraphed;

        public HazardPhase Phase => _phase;
        public Faction Faction => _faction;
        public bool IsRepeating => _dormantTime > 0f;
        public bool IsDangerous => _phase == HazardPhase.Active;
        public Vector2 Size => _size;

        /// <summary>How many attack-spawned zones are currently alive; the leak check reads it.</summary>
        public static int SpawnedCount => Spawned.Count;

        private void Awake()
        {
            _filter = new ContactFilter2D { useTriggers = true };
            _filter.SetLayerMask(_targetMask);
            ApplyVisual();
        }

        private void OnEnable()
        {
            if (_isSpawned)
            {
                Spawned.Add(this);
            }
        }

        private void OnDisable()
        {
            Spawned.Remove(this);
        }

        /// <summary>
        /// Arms a zone an attack dropped. A level hazard is never armed: it cycles on its own from
        /// the moment the scene loads, which is what lets the player learn its rhythm.
        /// </summary>
        public void Arm(Faction faction, GameObject owner)
        {
            _faction = faction;
            _owner = owner;
            if (!_isSpawned)
            {
                _isSpawned = true;
                Spawned.Add(this);
            }

            Enter(HazardPhase.Telegraph);
        }

        /// <summary>Puts a level hazard back to the start of its cycle.</summary>
        public void ResetHazard()
        {
            _tickTimer = 0f;
            Enter(IsRepeating ? HazardPhase.Dormant : HazardPhase.Telegraph);
        }

        private void Update()
        {
            if (_phase == HazardPhase.Finished)
            {
                return;
            }

            _phaseTimer += Time.deltaTime;
            switch (_phase)
            {
                case HazardPhase.Dormant when _phaseTimer >= _dormantTime:
                    Enter(HazardPhase.Telegraph);
                    break;
                case HazardPhase.Telegraph when _phaseTimer >= _telegraphTime:
                    Enter(HazardPhase.Active);
                    Activated?.Invoke(this);
                    AnyActivated?.Invoke(this);
                    break;
                case HazardPhase.Active:
                    TickActive();
                    break;
            }
        }

        private void TickActive()
        {
            _tickTimer -= Time.deltaTime;
            if (_tickTimer <= 0f)
            {
                _tickTimer = _tickInterval;
                Apply();
            }

            if (_phaseTimer < _activeTime)
            {
                return;
            }

            if (IsRepeating)
            {
                Enter(HazardPhase.Dormant);
                return;
            }

            Enter(HazardPhase.Finished);
            Destroy(gameObject);
        }

        private void Apply()
        {
            var centre = (Vector2)transform.position + _offset;
            var count = Physics2D.OverlapBox(centre, _size, 0f, _filter, _overlaps);
            for (var i = 0; i < count; i++)
            {
                var actor = _overlaps[i].GetComponentInParent<CombatActor>();
                if (actor == null || !actor.IsAlive || IsSameSide(actor))
                {
                    continue;
                }

                // Buildup is added directly rather than through a damage packet, so a zone that
                // does no damage still poisons, and a blocked hit's reduction never applies to
                // standing in a pool — there is nothing there to block.
                actor.Statuses?.Add(_status, _buildupPerSecond * _tickInterval, _owner);
                if (_damagePerTick > 0f)
                {
                    var direction = new Vector2(
                        Mathf.Sign(actor.transform.position.x - centre.x), 0.2f);
                    actor.ApplyDamage(new DamageInfo(_damagePerTick, direction, _damageType,
                        _owner, this, actor.transform.position, _knockback,
                        isParryable: false, tags: DamageTags.Area));
                }
            }
        }

        private bool IsSameSide(CombatActor actor)
        {
            return actor.Faction != Faction.Neutral && actor.Faction == _faction;
        }

        private void Enter(HazardPhase phase)
        {
            _phase = phase;
            _phaseTimer = 0f;
            if (phase == HazardPhase.Telegraph)
            {
                AnyTelegraphed?.Invoke(this);
            }

            if (phase == HazardPhase.Active)
            {
                // The first tick lands a moment after the zone turns, not on the same frame.
                _tickTimer = _tickInterval * 0.5f;
            }

            ApplyVisual();
        }

        private void ApplyVisual()
        {
            if (_visual == null)
            {
                return;
            }

            switch (_phase)
            {
                case HazardPhase.Telegraph:
                    _visual.enabled = true;
                    _visual.color = _telegraphColor;
                    break;
                case HazardPhase.Active:
                    _visual.enabled = true;
                    _visual.color = _activeColor;
                    break;
                default:
                    // A repeating hazard stays faintly visible while dormant: the player has to be
                    // able to see where the plant is, not only when it is spitting.
                    _visual.enabled = IsRepeating;
                    _visual.color = IsRepeating
                        ? new Color(_telegraphColor.r, _telegraphColor.g, _telegraphColor.b,
                            _telegraphColor.a * 0.35f)
                        : Color.clear;
                    break;
            }
        }

        /// <summary>
        /// Removes every zone an attack dropped. Death, resting and a boss reset call this, so a
        /// pool left by a fight can never still be burning after the world was put back.
        /// </summary>
        public static void ClearSpawned(Faction faction)
        {
            for (var i = Spawned.Count - 1; i >= 0; i--)
            {
                var zone = Spawned[i];
                if (zone == null)
                {
                    Spawned.RemoveAt(i);
                    continue;
                }

                if (zone._faction == faction)
                {
                    zone._phase = HazardPhase.Finished;
                    Destroy(zone.gameObject);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.5f, 1f, 0.3f, 0.35f);
            Gizmos.DrawCube((Vector2)transform.position + _offset, _size);
        }
    }
}
