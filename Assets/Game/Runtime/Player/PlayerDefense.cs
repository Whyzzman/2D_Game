using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>Which part of the guard is currently up.</summary>
    public enum GuardPhase
    {
        None,
        /// <summary>The short window right after raising the guard, where a hit is parried.</summary>
        Parry,
        Block
    }

    /// <summary>
    /// Blocking, perfect parry and guard break. Registered as the player's
    /// <see cref="IDamageGuard"/>, so every incoming hit is filtered through it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerDefense : MonoBehaviour, IDamageGuard
    {
        [SerializeField] private Stamina _stamina;
        [SerializeField] private PlayerSkills _skills;

        [Header("Parry")]
        [Tooltip("A hit arriving this soon after the guard goes up is parried instead of blocked.")]
        [SerializeField, Min(0f)] private float _parryWindow = 0.14f;
        [Tooltip("Minimum gap between two parry windows, so mashing the button is not free.")]
        [SerializeField, Min(0f)] private float _parryCooldown = 0.45f;
        [SerializeField, Min(0f)] private float _parryStaminaCost = 0f;
        [SerializeField, Min(0f)] private float _parryStaggerDamage = 45f;
        [SerializeField, Min(0f)] private float _parryHitStop = 0.08f;

        [Header("Block")]
        [Tooltip("Share of blocked damage that still gets through; 0 blocks everything.")]
        [SerializeField, Range(0f, 1f)] private float _blockedDamageLeak = 0f;
        [Tooltip("Stamina paid per point of guard damage on a blocked hit.")]
        [SerializeField, Min(0f)] private float _staminaPerGuardDamage = 1f;
        [SerializeField, Min(0f)] private float _guardBreakStun = 0.9f;
        private IFacing _facing;
        private float _blockStartedAt = float.NegativeInfinity;
        private float _parryReadyAt;
        private bool _wasParryConsumed;

        public event Action BlockStarted;
        public event Action BlockEnded;
        public event Action<DamageInfo> Parried;
        public event Action<DamageInfo> BlockedHit;

        /// <summary>Raised when a hit could not be blocked at all; the hook for a red-flash tell.</summary>
        public event Action<DamageInfo> UnblockableHit;
        public event Action GuardBroken;

        public bool IsBlocking => Phase != GuardPhase.None;
        public float ParryWindow => _parryWindow;

        public GuardPhase Phase
        {
            get
            {
                if (float.IsNegativeInfinity(_blockStartedAt))
                {
                    return GuardPhase.None;
                }

                return !_wasParryConsumed && Time.time - _blockStartedAt <= _parryWindow
                    ? GuardPhase.Parry
                    : GuardPhase.Block;
            }
        }

        private void Awake()
        {
            _facing = GetComponent<IFacing>();
            if (_stamina == null)
            {
                _stamina = GetComponent<Stamina>();
            }
            if (_skills == null)
            {
                _skills = FindAnyObjectByType<PlayerSkills>();
            }
        }

        private void OnDisable()
        {
            EndBlock();
        }

        public void BeginBlock()
        {
            if (IsBlocking)
            {
                return;
            }

            _blockStartedAt = Time.time;
            // Raising the guard again too soon starts straight in the blocking phase.
            _wasParryConsumed = Time.time < _parryReadyAt;
            _parryReadyAt = Time.time + _parryCooldown;
            _stamina?.SetGuarding(true);
            BlockStarted?.Invoke();
        }

        public void EndBlock()
        {
            if (!IsBlocking)
            {
                return;
            }

            _blockStartedAt = float.NegativeInfinity;
            _wasParryConsumed = false;
            _stamina?.SetGuarding(false);
            BlockEnded?.Invoke();
        }

        public GuardResult Evaluate(in DamageInfo info)
        {
            if (!IsBlocking || !IsFacingAttacker(info))
            {
                return GuardResult.None;
            }

            if (info.IsUnblockable)
            {
                UnblockableHit?.Invoke(info);
                return GuardResult.None;
            }

            if (Phase == GuardPhase.Parry && info.IsParryable)
            {
                Parry(info);
                return new GuardResult(GuardResponse.Parried);
            }

            // A trained guard costs less to hold, but never nothing: a block that was free would
            // make every other defensive option pointless.
            var cost = info.EffectiveGuardDamage * _staminaPerGuardDamage
                * (_skills != null ? _skills.Multiplier(SkillEffectType.GuardCost) : 1f);
            cost = Mathf.Max(0f, cost);
            var paid = _stamina != null ? _stamina.Drain(cost) : cost;
            if (_skills != null && _stamina != null)
            {
                var recovery = _skills.Effect(SkillEffectType.GuardRecovery);
                if (recovery > 0f)
                {
                    _stamina.ShortenRegenerationDelay(recovery);
                }
            }

            BlockedHit?.Invoke(info);
            if (_stamina != null && paid < cost - 0.01f)
            {
                BreakGuard();
            }

            return new GuardResult(GuardResponse.Blocked, _blockedDamageLeak);
        }

        private bool IsFacingAttacker(in DamageInfo info)
        {
            // HitDirection travels from the attacker toward the target: a frontal hit arrives
            // against the direction the player is facing. Back attacks are never blocked.
            var facing = _facing?.FacingDirection ?? 1;
            return facing * info.HitDirection.x < 0f;
        }

        private void Parry(in DamageInfo info)
        {
            // One parry per guard raise: holding the button does not parry a whole flurry.
            _wasParryConsumed = true;
            _stamina?.Drain(_parryStaminaCost);
            HitStop.Request(_parryHitStop);
            if (info.Attacker != null)
            {
                var attacker = info.Attacker.GetComponentInParent<CombatActor>();
                if (attacker != null)
                {
                    var stagger = _parryStaggerDamage
                        * (_skills != null ? _skills.Multiplier(SkillEffectType.ParryStagger) : 1f);
                    attacker.Stagger?.Add(stagger);
                    // Tell the attacker so it can drop the swing even when its poise held.
                    attacker.NotifyParried(info);
                }
            }

            Parried?.Invoke(info);
        }

        /// <summary>
        /// Guard break is driven by stamina, not by poise, so it does not touch the stagger meter:
        /// <see cref="PlayerCharacter"/> answers this event with its own GuardBroken state.
        /// </summary>
        private void BreakGuard()
        {
            EndBlock();
            GuardBroken?.Invoke();
        }

        public float GuardBreakStun => _guardBreakStun;
    }
}
