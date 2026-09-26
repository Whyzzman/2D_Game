using System;
using UnityEngine;

namespace ForgottenKingdom
{
    public enum EnemyAttackKind
    {
        /// <summary>The bread-and-butter swing.</summary>
        Normal,
        /// <summary>Slow, heavily telegraphed, expensive for a guard to eat.</summary>
        Heavy,
        /// <summary>A short defensive shove that answers pressure on the shield.</summary>
        ShieldBash
    }

    /// <summary>
    /// One attack an enemy may choose, with the context that makes it a sensible choice. The AI
    /// picks between these instead of cycling a list, so a new move is a data entry.
    /// </summary>
    [Serializable]
    public sealed class EnemyAttackOption
    {
        [SerializeField] private EnemyAttackKind _kind = EnemyAttackKind.Normal;
        [SerializeField] private AttackDefinition _attack = new AttackDefinition();
        [SerializeField, Min(0f)] private float _cooldown = 0f;
        [SerializeField, Min(0f)] private float _minRange = 0f;
        [SerializeField, Min(0f)] private float _maxRange = 2f;
        [Tooltip("Relative chance among the options that are currently usable.")]
        [SerializeField, Min(0f)] private float _weight = 1f;
        [Tooltip("Only offered once the target has pressured the shield this much.")]
        [SerializeField, Min(0f)] private float _requiredPressure = 0f;
        [Tooltip("Extra weight while the target is holding a guard up.")]
        [SerializeField, Min(0f)] private float _weightAgainstGuard = 0f;
        [Tooltip("Time from the swing start after which it stops tracking the target.")]
        [SerializeField, Min(0f)] private float _facingLockTime = 0.12f;
        [Tooltip("What this attack sends out: an arrow, a poison orb, a pool. None for a swing.")]
        [SerializeField] private AttackSpawn _spawn = new AttackSpawn();
        [Tooltip("Velocity thrown into the attack as it turns active: x forward, y up. Zero stands "
            + "still. This is what makes a hound's leap a leap rather than a bite at a distance.")]
        [SerializeField] private Vector2 _lunge = Vector2.zero;

        public EnemyAttackKind Kind => _kind;
        public AttackDefinition Attack => _attack;
        public float Cooldown => _cooldown;
        public float MinRange => _minRange;
        public float MaxRange => _maxRange;
        public float Weight => _weight;
        public float RequiredPressure => _requiredPressure;
        public float WeightAgainstGuard => _weightAgainstGuard;
        public float FacingLockTime => _facingLockTime;
        public AttackSpawn Spawn => _spawn;
        public Vector2 Lunge => _lunge;

        public bool IsUsable(float distance, float pressure)
        {
            return _attack != null
                && distance >= _minRange
                && distance <= _maxRange
                && pressure >= _requiredPressure;
        }

        public float ScoreFor(bool isTargetGuarding)
        {
            return _weight + (isTargetGuarding ? _weightAgainstGuard : 0f);
        }
    }
}
