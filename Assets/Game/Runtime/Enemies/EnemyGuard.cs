using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// A shield: its own resource, separate from health and from poise. Frontal blockable attacks
    /// are paid for out of Guard; when Guard runs out the shield breaks and the owner is wide open.
    /// The component also measures how hard the target is leaning on the shield, which is what the
    /// AI answers with a bash.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyGuard : MonoBehaviour, IDamageGuard
    {
        [Header("Guard resource")]
        [SerializeField, Min(1f)] private float _maxGuard = 100f;
        [SerializeField, Min(0f)] private float _recoveryPerSecond = 18f;
        [SerializeField, Min(0f)] private float _recoveryDelay = 2f;
        [Tooltip("Share of Guard returned when the broken shield comes back up.")]
        [SerializeField, Range(0.1f, 1f)] private float _recoveryAfterBreak = 0.55f;
        [SerializeField, Min(0f)] private float _breakDuration = 1.6f;

        [Header("Mitigation")]
        [Tooltip("Share of blocked damage that still reaches health.")]
        [SerializeField, Range(0f, 1f)] private float _blockedDamageLeak = 0.1f;
        [Tooltip("Share of poise damage a blocked hit still delivers.")]
        [SerializeField, Range(0f, 1f)] private float _blockedStaggerShare = 0.3f;
        [SerializeField, Min(0f)] private float _parryGuardDamage = 25f;

        [Header("AI timing")]
        [Tooltip("Delay before the shield actually comes up; an attack inside it gets through.")]
        [SerializeField, Min(0f)] private float _reactionDelay = 0.22f;
        [SerializeField, Min(0f)] private float _minGuardDuration = 0.5f;
        [SerializeField, Min(0f)] private float _maxGuardDuration = 1.5f;
        [SerializeField, Min(0f)] private float _guardCooldown = 0.6f;
        [Tooltip("Guard is not raised below this share of the meter.")]
        [SerializeField, Range(0f, 1f)] private float _minimumGuardToRaise = 0.15f;

        [Header("Pressure")]
        [Tooltip("Blocked hits at or below this guard damage count as light pressure.")]
        [SerializeField, Min(0f)] private float _lightGuardDamage = 25f;
        [SerializeField, Min(0f)] private float _pressurePerLightHit = 1f;
        [SerializeField, Min(0f)] private float _pressureDecayPerSecond = 0.7f;
        private float _currentGuard;
        private float _recoveryBlockedUntil;
        private float _raisedAt = float.NegativeInfinity;
        private IFacing _facing;

        public event Action<float, float> GuardChanged;
        public event Action GuardBroken;

        /// <summary>Raised with the incoming hit whenever the shield actually stops something.</summary>
        public event Action<DamageInfo> GuardHit;

        public float MaxGuard => _maxGuard;
        public float CurrentGuard => _currentGuard;
        public float GuardNormalized => _maxGuard > 0f ? Mathf.Clamp01(_currentGuard / _maxGuard) : 0f;
        public float Pressure { get; private set; }
        public float BreakDuration => _breakDuration;
        public float ReactionDelay => _reactionDelay;
        public float MinGuardDuration => _minGuardDuration;
        public float MaxGuardDuration => _maxGuardDuration;
        public float GuardCooldown => _guardCooldown;
        public bool IsBroken { get; private set; }

        /// <summary>True once the shield is up AND the reaction delay has elapsed.</summary>
        public bool IsGuarding => IsRaised && !IsBroken
            && Time.time - _raisedAt >= _reactionDelay;

        /// <summary>True from the moment the AI decided to guard, delay included.</summary>
        public bool IsRaised { get; private set; }

        public bool CanRaiseGuard => !IsBroken && GuardNormalized > _minimumGuardToRaise;

        private void Awake()
        {
            _facing = GetComponent<IFacing>();
            _currentGuard = _maxGuard;
        }

        private void Update()
        {
            DecayPressure();
            if (IsRaised || IsBroken || _currentGuard >= _maxGuard
                || Time.time < _recoveryBlockedUntil)
            {
                return;
            }

            RestoreGuard(_recoveryPerSecond * Time.deltaTime);
        }

        public void RaiseGuard()
        {
            if (IsRaised || !CanRaiseGuard)
            {
                return;
            }

            IsRaised = true;
            _raisedAt = Time.time;
        }

        public void LowerGuard()
        {
            IsRaised = false;
        }

        /// <summary>How long the shield has been up, reaction delay included.</summary>
        public float TimeRaised => IsRaised ? Time.time - _raisedAt : 0f;

        public void SpendGuard(float amount)
        {
            if (amount <= 0f || IsBroken)
            {
                return;
            }

            _currentGuard = Mathf.Max(0f, _currentGuard - amount);
            _recoveryBlockedUntil = Time.time + _recoveryDelay;
            GuardChanged?.Invoke(_currentGuard, _maxGuard);
            if (_currentGuard <= 0f)
            {
                Break();
            }
        }

        public void RestoreGuard(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            _currentGuard = Mathf.Min(_maxGuard, _currentGuard + amount);
            GuardChanged?.Invoke(_currentGuard, _maxGuard);
        }

        /// <summary>Called when the break window ends; the shield comes back partly charged.</summary>
        public void RecoverFromBreak()
        {
            IsBroken = false;
            IsRaised = false;
            _currentGuard = _maxGuard * _recoveryAfterBreak;
            _recoveryBlockedUntil = Time.time + _recoveryDelay;
            Pressure = 0f;
            GuardChanged?.Invoke(_currentGuard, _maxGuard);
        }

        public void ResetGuard()
        {
            IsBroken = false;
            IsRaised = false;
            Pressure = 0f;
            _currentGuard = _maxGuard;
            _recoveryBlockedUntil = 0f;
            _raisedAt = float.NegativeInfinity;
            GuardChanged?.Invoke(_currentGuard, _maxGuard);
        }

        /// <summary>A parry rocks the shield as well as the poise.</summary>
        public void ApplyParryDamage()
        {
            SpendGuard(_parryGuardDamage);
        }

        public GuardResult Evaluate(in DamageInfo info)
        {
            if (!IsGuarding || info.IsUnblockable || !IsFrontal(info))
            {
                return GuardResult.None;
            }

            var guardDamage = info.EffectiveGuardDamage;
            if (guardDamage <= _lightGuardDamage)
            {
                // Only mindless light pressure builds up; a heavy blow is a fair answer instead.
                Pressure += _pressurePerLightHit;
            }

            GuardHit?.Invoke(info);
            SpendGuard(guardDamage);
            return new GuardResult(GuardResponse.Blocked, _blockedDamageLeak, _blockedStaggerShare);
        }

        public void ResetPressure()
        {
            Pressure = 0f;
        }

        private bool IsFrontal(in DamageInfo info)
        {
            var facing = _facing?.FacingDirection ?? 1;
            return facing * info.HitDirection.x < 0f;
        }

        private void Break()
        {
            IsBroken = true;
            IsRaised = false;
            Pressure = 0f;
            GuardBroken?.Invoke();
        }

        private void DecayPressure()
        {
            if (Pressure > 0f)
            {
                Pressure = Mathf.Max(0f, Pressure - _pressureDecayPerSecond * Time.deltaTime);
            }
        }
    }
}
