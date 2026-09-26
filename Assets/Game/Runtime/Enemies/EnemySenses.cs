using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// What an ordinary enemy can perceive: whether it sees its target, has a clear line to it,
    /// has it in striking reach, and whether the way ahead is open. Pure questions — nothing here
    /// changes state or moves the enemy — so <see cref="EnemyController"/> keeps the decisions and
    /// this keeps the reading of the world, and either can change without the other.
    ///
    /// Split out of <see cref="EnemyController"/> unchanged (item 26 of the 2026-09-26 list); the
    /// numbers all come from the <see cref="EnemyDefinition"/>.
    /// </summary>
    public sealed class EnemySenses
    {
        private readonly EnemyDefinition _definition;
        private readonly EnemyMotor2D _motor;
        private readonly Transform _self;

        public EnemySenses(EnemyDefinition definition, EnemyMotor2D motor, Transform self)
        {
            _definition = definition;
            _motor = motor;
            _self = self;
        }

        /// <summary>
        /// Sight is a cone of sorts: far to the front, short behind, never past the vertical reach,
        /// and never through solid level geometry.
        /// </summary>
        public bool CanSee(PlayerCharacter target)
        {
            if (target == null || !target.IsAlive)
            {
                return false;
            }

            var delta = (Vector2)target.transform.position - (Vector2)_self.position;
            if (Mathf.Abs(delta.y) > _definition.VerticalDetectionRange)
            {
                return false;
            }

            var isInFront = Mathf.Sign(delta.x) == _motor.FacingDirection
                || Mathf.Abs(delta.x) < _definition.FacingDeadZone;
            var range = isInFront ? _definition.DetectionRange : _definition.BehindDetectionRange;
            return delta.magnitude <= range && HasLineOfSight(target);
        }

        public bool HasLineOfSight(PlayerCharacter target)
        {
            return target != null && _motor.HasLineOfSight(target.transform.position);
        }

        public bool IsInStrikingRange(PlayerCharacter target)
        {
            if (target == null || !target.IsAlive)
            {
                return false;
            }

            var delta = (Vector2)target.transform.position - (Vector2)_self.position;
            // A target on the floor above must not be hit through the ceiling.
            return Mathf.Abs(delta.x) <= _definition.AttackRange
                && Mathf.Abs(delta.y) <= _definition.VerticalAttackRange;
        }

        /// <summary>A ledge or a wall in the facing direction.</summary>
        public bool IsBlockedAhead()
        {
            return !_motor.HasGroundAhead() || _motor.HasWallAhead();
        }
    }
}
