using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// How an enemy moves while it waits in reach (2026-11-07, user: «рухи мобів»; decided by Claude under standing
    /// approval). Inside striking distance it used to freeze until its next swing. Now a melee enemy keeps the hero
    /// in a band: it gives ground when he is in its face (so it no longer walks through him), and shifts in and out
    /// on its own beat while its swing or its group's turn comes round. It always faces him and never steps off a
    /// ledge or into a wall. The archer's retreat moved here from <see cref="EnemyController"/> with it.
    /// </summary>
    public sealed class EnemySpacing
    {
        /// <summary>Inside this share of the attack range the enemy backs off.</summary>
        public const float NearShare = 0.45f;
        /// <summary>Of the chase speed: the weave in and out.</summary>
        public const float WeaveSpeed = 0.3f;
        /// <summary>Of the chase speed: backing off a hero in its face.</summary>
        public const float BackOffSpeed = 0.5f;
        /// <summary>One beat of the weave: hold, step in, hold, step out.</summary>
        public const float WeaveBeat = 0.6f;

        private readonly EnemyMotor2D _motor;
        private readonly Transform _self;
        private readonly EnemyDefinition _definition;
        private readonly float _phase;

        public EnemySpacing(EnemyMotor2D motor, Transform self, EnemyDefinition definition)
        {
            _motor = motor;
            _self = self;
            _definition = definition;
            // each enemy keeps its own rhythm, so a pack does not sway in step
            _phase = Random.Range(0f, 4f * WeaveBeat);
        }

        /// <summary>
        /// The archer's footwork. A ranged enemy that stood still while being hit in the face would be a free kill
        /// and would make the bow pointless, so inside its retreat range it backs away — still facing the player,
        /// and never over a ledge or into a wall. When it has nowhere left to go it stops retreating and fights,
        /// rather than grinding against the scenery until it dies.
        /// </summary>
        public bool TryKeepDistance(Transform target, float horizontal)
        {
            if (!_definition.KeepsDistance || horizontal > _definition.RetreatRange || target == null)
            {
                return false;
            }

            var away = target.position.x > _self.position.x ? -1 : 1;
            if (!CanStep(away))
            {
                return false;
            }

            _motor.DriveToward(away, _definition.RetreatSpeed);
            return true;
        }

        /// <summary>
        /// In reach, between swings: keeps the hero between the near line and <paramref name="holdAt"/>.
        /// </summary>
        public void Hold(float horizontal, float holdAt)
        {
            if (_definition.KeepsDistance)
            {
                _motor.Stop();
                return;
            }

            var near = _definition.AttackRange * NearShare;
            var weave = _definition.ChaseSpeed * WeaveSpeed;
            var beat = Mathf.FloorToInt((Time.time + _phase) / WeaveBeat) % 4;
            int step;
            float speed;
            if (horizontal < near)
            {
                step = -1;
                speed = _definition.ChaseSpeed * BackOffSpeed;
            }
            else
            {
                // a weave step stays inside the band
                step = beat == 1 && horizontal - weave * WeaveBeat > near ? 1
                    : beat == 3 && horizontal + weave * WeaveBeat < holdAt ? -1
                    : 0;
                speed = weave;
            }

            var direction = step * _motor.FacingDirection;
            if (step == 0 || !CanStep(direction))
            {
                _motor.Stop();
                return;
            }

            _motor.DriveToward(direction, speed);
        }

        private bool CanStep(int direction) => _motor.HasGroundToward(direction) && !_motor.HasWallToward(direction);
    }
}
