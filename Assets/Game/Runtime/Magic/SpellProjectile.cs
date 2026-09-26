using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// A flying spell. It carries a finished <see cref="DamageInfo"/> and delivers it through the
    /// ordinary <see cref="Hurtbox"/>, so a fire bolt is resisted, blocked and mitigated by
    /// exactly the same code a sword swing is. It never outlives its range, and it never passes
    /// through a wall.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class SpellProjectile : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _visual;
        [Tooltip("Carried light in the projectile's own colour: fire lights the walls warm as it "
            + "passes, frost cold. Optional.")]
        [SerializeField] private UnityEngine.Rendering.Universal.Light2D _light;
        [SerializeField] private LayerMask _targetMask;
        [SerializeField] private LayerMask _obstacleMask;
        [Tooltip("Safety net; the range it was fired with normally ends it first.")]
        [SerializeField, Min(0.1f)] private float _maxLifetime = 6f;
        private readonly HashSet<CombatActor> _alreadyHit = new HashSet<CombatActor>();
        private readonly Collider2D[] _overlaps = new Collider2D[8];
        private ContactFilter2D _filter;
        private DamageInfo _damage;
        private Faction _faction = Faction.Player;
        private Vector2 _direction = Vector2.right;
        private float _speed;
        private float _remainingDistance;
        private float _expiresAt;
        private int _remainingHits = 1;
        private bool _isLive;

        /// <summary>Raised on every target actually damaged; the hook for impact effects.</summary>
        public static event Action<SpellProjectile, CombatActor, DamageResult> Hit;

        /// <summary>Every projectile currently in flight, so a reset can sweep them up.</summary>
        private static readonly List<SpellProjectile> Live = new List<SpellProjectile>();

        public Faction Faction => _faction;
        public bool IsLive => _isLive;

        private void Awake()
        {
            _filter = new ContactFilter2D { useTriggers = true };
            _filter.SetLayerMask(_targetMask);
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnEnable()
        {
            Live.Add(this);
        }

        private void OnDisable()
        {
            Live.Remove(this);
        }

        /// <summary>Arms the projectile. Everything it needs is decided by the caster.</summary>
        public void Launch(in DamageInfo damage, Faction faction, Vector2 direction, float speed,
            float range, int pierceCount, Color color)
        {
            _damage = damage;
            _faction = faction;
            _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            _speed = Mathf.Max(0.1f, speed);
            _remainingDistance = Mathf.Max(0.5f, range);
            _remainingHits = Mathf.Max(1, pierceCount);
            _expiresAt = Time.time + _maxLifetime;
            _alreadyHit.Clear();
            _isLive = true;
            if (_visual != null)
            {
                _visual.color = color;
            }

            if (_light != null)
            {
                _light.color = new Color(color.r, color.g, color.b, 1f);
                _light.enabled = true;
            }

            transform.right = _direction;
        }

        private void FixedUpdate()
        {
            if (!_isLive)
            {
                return;
            }

            if (Time.time >= _expiresAt)
            {
                Expire();
                return;
            }

            var step = _speed * Time.fixedDeltaTime;
            // Swept, not teleported: a fast bolt cannot step over a thin wall or a narrow enemy.
            var wall = Physics2D.Raycast(transform.position, _direction, step, _obstacleMask);
            if (wall.collider != null)
            {
                transform.position = wall.point;
                Expire();
                return;
            }

            transform.position += (Vector3)(_direction * step);
            _remainingDistance -= step;
            Sweep();
            if (_isLive && _remainingDistance <= 0f)
            {
                Expire();
            }
        }

        private void Sweep()
        {
            var bounds = GetComponent<Collider2D>().bounds;
            var count = Physics2D.OverlapBox(bounds.center, bounds.size, 0f, _filter, _overlaps);
            for (var i = 0; i < count; i++)
            {
                var hurtbox = _overlaps[i].GetComponentInParent<Hurtbox>();
                if (hurtbox?.Owner == null || IsSameSide(hurtbox.Owner)
                    || !_alreadyHit.Add(hurtbox.Owner))
                {
                    continue;
                }

                var contact = _overlaps[i].ClosestPoint(bounds.center);
                var result = hurtbox.Owner.ApplyDamage(_damage.AimedAt(contact, _direction));
                if (result.HasConnected)
                {
                    HitStop.Request(_damage.HitStop);
                    Hit?.Invoke(this, hurtbox.Owner, result);
                }

                // A projectile is spent whether or not the target was invulnerable: it struck.
                if (--_remainingHits <= 0)
                {
                    Expire();
                    return;
                }
            }
        }

        /// <summary>A caster never hits its own side; neutral targets are always valid.</summary>
        private bool IsSameSide(CombatActor target)
        {
            return target.Faction != Faction.Neutral && target.Faction == _faction;
        }

        private void Expire()
        {
            _isLive = false;
            Destroy(gameObject);
        }

        /// <summary>
        /// Clears every projectile of one side. Death and resting call this, so a bolt fired a
        /// moment before dying cannot arrive after the world has been put back.
        /// </summary>
        public static void ClearAll(Faction faction)
        {
            for (var i = Live.Count - 1; i >= 0; i--)
            {
                var projectile = Live[i];
                if (projectile != null && projectile._faction == faction)
                {
                    projectile.Expire();
                }
            }
        }

        public static int LiveCount(Faction faction)
        {
            var count = 0;
            foreach (var projectile in Live)
            {
                if (projectile != null && projectile._isLive && projectile._faction == faction)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
