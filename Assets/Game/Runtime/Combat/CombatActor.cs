using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The one place a hit is resolved for any creature. The pipeline runs in a fixed order:
    /// alive check, immunity, parry, block, health, poise, knockback, reaction events. Player,
    /// enemies and bosses all use this component instead of their own copy.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health), typeof(Invulnerability))]
    public sealed class CombatActor : MonoBehaviour, IDamageable
    {
        [SerializeField] private Faction _faction = Faction.Enemy;
        [SerializeField] private Health _health;
        [SerializeField] private StaggerMeter _stagger;
        [SerializeField] private Invulnerability _invulnerability;
        [SerializeField] private Rigidbody2D _body;
        [SerializeField, Min(0f)] private float _knockbackLift = 1.5f;
        [SerializeField, Range(0f, 1f)] private float _knockbackResistance = 0f;
        [SerializeField, Min(0f)] private float _hitInvulnerability = 0.12f;
        private IDamageGuard _guard;
        private IDamageMitigation _mitigation;
        private Resistances _resistances;
        private StatusEffects _statuses;
        public StatusEffects Statuses => _statuses;
        private bool _isResolved;
        private bool _isPlayer;
        private bool _isToughened;

        /// <summary>Raised for every hit that was not ignored, including blocks and parries.</summary>
        public event Action<DamageInfo, DamageResult> DamageTaken;

        /// <summary>Raised when immunity swallowed a hit; the hook a perfect dodge will use.</summary>
        public event Action<DamageInfo> AttackDodged;

        /// <summary>
        /// Raised on the ATTACKER when its blow was parried. The defender's guard reports it,
        /// because a parry is resolved on the defender and the attacker would never hear about it
        /// otherwise.
        /// </summary>
        public event Action<DamageInfo> AttackParried;
        public event Action<DamageInfo> Killed;

        public Faction Faction => _faction;

        public Health Health
        {
            get
            {
                EnsureReferences();
                return _health;
            }
        }

        public StaggerMeter Stagger
        {
            get
            {
                EnsureReferences();
                return _stagger;
            }
        }

        public Invulnerability Invulnerability
        {
            get
            {
                EnsureReferences();
                return _invulnerability;
            }
        }

        public bool IsAlive => Health != null && Health.IsAlive;
        public bool IsInvulnerable => Invulnerability != null && Invulnerability.IsInvulnerable;

        private void Reset()
        {
            _health = GetComponent<Health>();
            _stagger = GetComponent<StaggerMeter>();
            _invulnerability = GetComponent<Invulnerability>();
            _body = GetComponent<Rigidbody2D>();
        }

        private void Awake()
        {
            EnsureReferences();
        }

        /// <summary>
        /// Resolved on demand as well as in Awake: another component's OnEnable may reach for
        /// Health or Stagger before this component's Awake has run.
        /// </summary>
        private void EnsureReferences()
        {
            if (_isResolved)
            {
                return;
            }

            _isResolved = true;
            _isPlayer = GetComponent<PlayerCharacter>() != null;
            if (_health == null)
            {
                _health = GetComponent<Health>();
            }
            if (_stagger == null)
            {
                _stagger = GetComponent<StaggerMeter>();
            }
            if (_invulnerability == null)
            {
                _invulnerability = GetComponent<Invulnerability>();
            }
            if (_body == null)
            {
                _body = GetComponent<Rigidbody2D>();
            }
            _guard = GetComponent<IDamageGuard>();
            _mitigation = GetComponent<IDamageMitigation>();
            _resistances = GetComponent<Resistances>();
            _statuses = GetComponent<StatusEffects>();
        }

        /// <summary>What this creature resists, or null when it resists nothing in particular.</summary>
        public Resistances Resistances
        {
            get
            {
                EnsureReferences();
                return _resistances;
            }
        }

        /// <summary>
        /// «Нова гра+»: an enemy's health raised once, when it first stands in the world; the
        /// world's resets refill it to the raised maximum.
        /// </summary>
        public void Toughen(float scale)
        {
            EnsureReferences();
            if (_isToughened || scale <= 1f || _health == null)
            {
                return;
            }

            _isToughened = true;
            _health.SetMax(_health.Max * scale, true);
        }

        public DamageResult ApplyDamage(in DamageInfo info)
        {
            if (!IsAlive)
            {
                return new DamageResult(DamageOutcome.Ignored);
            }

            if (IsInvulnerable && (!info.IsStatusDamage
                || Invulnerability.IsActive(ImmunitySource.Script)
                || Invulnerability.IsActive(ImmunitySource.Respawn)))
            {
                // Immunity swallows the whole hit: no health, no poise, no knockback, no reaction.
                AttackDodged?.Invoke(info);
                return new DamageResult(DamageOutcome.Ignored);
            }

            var guard = !info.IsStatusDamage && _guard != null ? _guard.Evaluate(info) : GuardResult.None;
            if (guard.Response == GuardResponse.Parried)
            {
                var parried = new DamageResult(DamageOutcome.Parried);
                DamageTaken?.Invoke(info, parried);
                return parried;
            }

            // Armour comes after the shield and before health: a blocked hit is reduced by both,
            // and this stays the only place either one is applied.
            // Resistance comes first: what a fire hit is worth against this creature is a
            // property of the creature, not of the shield or the armour in front of it.
            // «Нова гра+» hits the hero harder; nothing else changes what a blow is worth here.
            var amount = _isPlayer ? info.Amount * NewGamePlus.Scale : info.Amount;
            var resisted = _resistances != null
                ? amount * _resistances.Multiplier(info.Type)
                : amount;
            var mitigated = _mitigation != null
                ? Mathf.Max(0f, _mitigation.Mitigate(info.Scaled(resisted, info.StaggerDamage,
                    info.GuardDamage, info.IsCritical)))
                : resisted;
            var isBlocked = guard.Response == GuardResponse.Blocked;
            var damage = isBlocked ? mitigated * guard.DamageMultiplier : mitigated;
            var applied = _health.Remove(damage);
            var isLethal = !_health.IsAlive;
            if (isBlocked)
            {
                // A shield soaks most of the shock, but a heavy blow still rocks the guard.
                _stagger?.Add(info.StaggerDamage * guard.StaggerMultiplier);
            }
            else if (!info.IsStatusDamage)
            {
                _stagger?.Add(info.StaggerDamage);
                ApplyKnockback(info.Knockback, info.HitDirection);
                if (_hitInvulnerability > 0f && !isLethal)
                {
                    SetInvulnerable(_hitInvulnerability);
                }
            }

            var result = new DamageResult(
                isBlocked ? DamageOutcome.Blocked : DamageOutcome.Hit, applied, isLethal);
            DamageTaken?.Invoke(info, result);
            if (isLethal)
            {
                Killed?.Invoke(info);
            }
            else { _statuses?.Receive(info, isBlocked); }
            return result;
        }

        /// <summary>Called by a defender's guard after it parries this actor's attack.</summary>
        public void NotifyParried(in DamageInfo info)
        {
            AttackParried?.Invoke(info);
        }

        public void SetInvulnerable(float duration)
        {
            Invulnerability?.Activate(ImmunitySource.HitRecovery, duration);
        }

        public void SetInvulnerable(ImmunitySource source, float duration)
        {
            Invulnerability?.Activate(source, duration);
        }

        public void ClearInvulnerability()
        {
            Invulnerability?.Clear();
        }

        /// <summary>0 moves with every blow, 1 never moves.</summary>
        public float KnockbackResistance => _knockbackResistance;

        public void ApplyKnockback(float force, Vector2 direction)
        {
            if (_body == null || force <= 0f)
            {
                return;
            }

            var scaled = force * (1f - _knockbackResistance);
            var horizontal = direction.x >= 0f ? 1f : -1f;
            _body.linearVelocity = new Vector2(horizontal * scaled, _knockbackLift);
        }
    }
}
