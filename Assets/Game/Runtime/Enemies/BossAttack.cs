using System;
using UnityEngine;

namespace ForgottenKingdom
{
    public enum BossAttackKind
    {
        SwordSlash,
        ShieldBash,
        Charge,
        TripleCombo,
        JumpSlam,
        // The Hollow Warden's moves. Appended rather than inserted: these are serialized as
        // numbers, and renumbering the Guardian's attacks would silently rewrite its fight.
        GlaiveSlash,
        LongThrust,
        Sweep,
        RootShot,
        RootSlam,
        PoisonField,
        LeapStrike
    }

    /// <summary>How the boss moves while an attack plays.</summary>
    public enum BossAttackMovement
    {
        None,
        /// <summary>Runs forward during the windup, as the charge does.</summary>
        Charge,
        /// <summary>Leaps toward the player's position and lands with the active window.</summary>
        Leap
    }

    /// <summary>
    /// One boss move: which phase unlocks it, at what distance it is chosen, how the boss moves and
    /// which swings it plays.
    /// </summary>
    [Serializable]
    public sealed class BossAttack
    {
        [SerializeField] private BossAttackKind _kind = BossAttackKind.SwordSlash;
        [Tooltip("Lowest phase that may use this attack.")]
        [SerializeField, Min(1)] private int _fromPhase = 1;
        [Tooltip("Highest phase that may use it; 0 means every later phase too.")]
        [SerializeField, Min(0)] private int _toPhase = 0;
        [Tooltip("Time from the swing start during which it still turns with the target.")]
        [SerializeField, Min(0f)] private float _trackingTime = 0.2f;
        [SerializeField, Min(0f)] private float _minRange = 0f;
        [SerializeField, Min(0f)] private float _maxRange = 2.4f;
        [SerializeField, Min(0f)] private float _cooldown = 3f;
        [SerializeField] private BossAttackMovement _movement = BossAttackMovement.None;
        [SerializeField, Min(0f)] private float _movementSpeed = 9f;
        [Tooltip("Pause between the swings of a multi-hit attack.")]
        [SerializeField, Min(0f)] private float _swingInterval = 0.12f;
        [SerializeField] private bool _endsWithShockwave = false;
        [Tooltip("What the attack sends out when it turns active: a root shot, a poison field.")]
        [SerializeField] private AttackSpawn _spawn = new AttackSpawn();
        [SerializeField] private AttackDefinition[] _swings = { new AttackDefinition() };

        public BossAttackKind Kind => _kind;
        public int FromPhase => _fromPhase;
        public int ToPhase => _toPhase;
        public float TrackingTime => _trackingTime;
        public float MinRange => _minRange;
        public float MaxRange => _maxRange;
        public float Cooldown => _cooldown;
        public BossAttackMovement Movement => _movement;
        public float MovementSpeed => _movementSpeed;
        public float SwingInterval => _swingInterval;
        public bool EndsWithShockwave => _endsWithShockwave;
        public AttackSpawn Spawn => _spawn;
        public int SwingCount => _swings != null ? _swings.Length : 0;

        public AttackDefinition Swing(int index)
        {
            if (_swings == null || _swings.Length == 0)
            {
                return null;
            }

            return _swings[Mathf.Clamp(index, 0, _swings.Length - 1)];
        }

        public bool IsUsable(int phase, float distance)
        {
            return phase >= _fromPhase
                && (_toPhase <= 0 || phase <= _toPhase)
                && distance >= _minRange
                && distance <= _maxRange;
        }
    }
}
