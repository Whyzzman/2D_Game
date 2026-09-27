using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// One swing: its timing windows, its hitbox and the hit it produces. Designers tune attacks
    /// here instead of in code, and a new weapon is a new set of these.
    /// </summary>
    [Serializable]
    public sealed class AttackDefinition
    {
        [SerializeField] private string _displayName = "Attack";
        [SerializeField] private string _animationState = string.Empty;
        [Tooltip("Flat damage, used only when no weapon drives this attack (enemies, traps).")]
        [SerializeField, Min(0f)] private float _damage = 10f;
        [Tooltip("Share of the wielding weapon's base damage this swing deals.")]
        [SerializeField, Min(0f)] private float _damageMultiplier = 1f;
        [SerializeField] private DamageType _damageType = DamageType.Physical;
        [SerializeField, Min(0f)] private float _staminaCost = 0f;
        [SerializeField, Min(0f)] private float _staggerDamage = 10f;
        [SerializeField, Min(0f)] private float _knockback = 4f;
        [Tooltip("Stamina a blocker pays; falls back to the poise damage when left at 0.")]
        [SerializeField, Min(0f)] private float _guardDamage = 0f;
        [SerializeField, Min(0f)] private float _hitStop = 0.04f;
        [SerializeField] private bool _isUnblockable = false;
        [Tooltip("Off for heavy moves that must be dodged rather than parried.")]
        [SerializeField] private bool _isParryable = true;

        [Header("Timing (seconds)")]
        [SerializeField, Min(0f)] private float _windup = 0.12f;
        [SerializeField, Min(0.01f)] private float _activeTime = 0.08f;
        [SerializeField, Min(0f)] private float _recovery = 0.22f;
        [Tooltip("Time from the attack start after which a buffered follow-up may chain.")]
        [SerializeField, Min(0f)] private float _comboLinkTime = 0.26f;
        [Tooltip("Time from the attack start after which a dash may cancel it.")]
        [SerializeField, Min(0f)] private float _cancelableAfter = 0.2f;

        [Header("Hitbox")]
        [SerializeField] private Vector2 _hitboxOffset = new Vector2(0.85f, 0.1f);
        [SerializeField] private Vector2 _hitboxSize = new Vector2(1.3f, 1.1f);
        [Tooltip("Share of normal movement speed the attacker keeps while this attack plays.")]
        [SerializeField, Range(0f, 1f)] private float _movementScale = 0.2f;

        [SerializeField] private StatusBuildup[] _statusBuildup = Array.Empty<StatusBuildup>();
        public string DisplayName => _displayName;
        public string AnimationState => _animationState;
        public float Damage => _damage;
        public float DamageMultiplier => _damageMultiplier;
        public DamageType DamageType => _damageType;
        public float StaminaCost => _staminaCost;
        public float StaggerDamage => _staggerDamage;
        public float Knockback => _knockback;
        public float GuardDamage => _guardDamage;
        public float HitStop => _hitStop;
        public bool IsUnblockable => _isUnblockable;
        public bool IsParryable => _isParryable;
        public float Windup => _windup;
        public float ActiveTime => _activeTime;
        public float Recovery => _recovery;
        public float ComboLinkTime => _comboLinkTime;
        public float CancelableAfter => _cancelableAfter;
        public Vector2 HitboxOffset => _hitboxOffset;
        public Vector2 HitboxSize => _hitboxSize;
        public float MovementScale => _movementScale;
        public float Duration => _windup + _activeTime + _recovery;

        /// <summary>
        /// The same swing aimed another way: its damage, cost and timing, with its own hitbox and
        /// animation. How the swings up and down are made from a weapon's first swing.
        /// </summary>
        public AttackDefinition Aimed(Vector2 hitboxOffset, Vector2 hitboxSize, string animationState)
        {
            var aimed = (AttackDefinition)MemberwiseClone();
            aimed._hitboxOffset = hitboxOffset;
            aimed._hitboxSize = hitboxSize;
            aimed._animationState = animationState;
            aimed._displayName = _displayName + " (" + animationState + ")";
            return aimed;
        }

        public DamageInfo BuildDamage(GameObject attacker, UnityEngine.Object source, int facing)
        {
            return new DamageInfo(_damage, new Vector2(facing, 0f), _damageType, attacker, source,
                default, _knockback, _staggerDamage, _hitStop, false, _isUnblockable, _guardDamage,
                _isParryable, DamageTags.Melee, _statusBuildup);
        }

        /// <summary>
        /// Turns a player swing into a hit. The attack power the attributes and equipment produce
        /// arrives already worked out, from <see cref="WeaponDamageCalculator"/>; this only
        /// applies the share belonging to this particular swing. The flat <see cref="Damage"/> is
        /// deliberately ignored, so a weapon attack has exactly one authoritative source.
        /// </summary>
        public DamageInfo BuildWeaponDamage(WeaponDefinition weapon, float scaledBase,
            GameObject attacker, UnityEngine.Object source, int facing, bool isCritical = false)
        {
            if (weapon == null)
            {
                return BuildDamage(attacker, source, facing);
            }

            var damage = scaledBase * _damageMultiplier;
            if (isCritical)
            {
                damage *= weapon.CritMultiplier;
            }

            var stagger = _staggerDamage * weapon.StaggerMultiplier;
            var guard = _guardDamage * weapon.StaggerMultiplier;
            return new DamageInfo(damage, new Vector2(facing, 0f), _damageType, attacker, source,
                default, _knockback * weapon.KnockbackMultiplier, stagger, _hitStop, isCritical,
                _isUnblockable, guard, _isParryable, DamageTags.Melee, _statusBuildup);
        }

        /// <summary>Stamina this swing costs with that weapon in hand.</summary>
        public float StaminaCostFor(WeaponDefinition weapon)
        {
            return weapon != null ? _staminaCost * weapon.StaminaMultiplier : _staminaCost;
        }
    }
}
