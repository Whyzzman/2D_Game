using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// How a blow moves an ordinary enemy, as in Hollow Knight: every landed hit shoves it back a
    /// short, smooth slide it cannot act through, and a killing blow throws the body off its feet
    /// to land and lie where it fell.
    ///
    /// The slide is measured in distance, not speed: a sword and a greatsword both come out
    /// between 0.6 of a unit and just under one (raised from 0.5–0.9 on 2026-10-19, combat feel), so a heavier weapon reads heavier without punting
    /// enemies out of reach of the next swing. Split out of <see cref="EnemyController"/>, which
    /// keeps the decisions; this only moves the body.
    /// </summary>
    public sealed class EnemyKnockback
    {
        /// <summary>The slide of the lightest blow.</summary>
        public const float MinimumSlide = 0.6f;

        /// <summary>The slide of the heaviest blow; kept under one unit so combos still connect.</summary>
        public const float MaximumSlide = 0.95f;

        /// <summary>How long the slide lasts; the enemy acts again when it ends.</summary>
        public const float SlideTime = 0.16f;

        /// <summary>A killed body is thrown this fast along the blow, and this fast up.</summary>
        public static readonly Vector2 CorpseThrow = new Vector2(4.5f, 5.5f);

        /// <summary>A body still falling after this long went into a pit and is hidden.</summary>
        private const float LostCorpseTime = 2.5f;

        private readonly EnemyMotor2D _motor;
        private readonly Collider2D _body;
        private readonly LayerMask _ground;
        private float _slideUntil = float.MinValue;
        private float _slideSpeed;
        private float _deceleration;
        private int _slideDirection;
        private float _airborneSince = -1f;
        private readonly LayerMask _livingExclusions;

        public EnemyKnockback(EnemyMotor2D motor, Collider2D body, LayerMask ground)
        {
            _motor = motor;
            _body = body;
            _ground = ground;
            _livingExclusions = body != null ? body.excludeLayers : (LayerMask)0;
        }

        public bool IsSliding => Time.time < _slideUntil;

        /// <summary>How far a blow of this knockback slides a body with no resistance.</summary>
        public static float SlideDistance(float force) =>
            force <= 0f ? 0f : Mathf.Lerp(MinimumSlide, MaximumSlide, Mathf.InverseLerp(3f, 12f, force));

        /// <summary>Starts the slide away from a landed blow.</summary>
        public void Begin(in DamageInfo info, float resistance)
        {
            var distance = SlideDistance(info.Knockback) * (1f - Mathf.Clamp01(resistance));
            if (distance <= 0.01f)
            {
                return;
            }

            _slideDirection = info.HitDirection.x >= 0f ? 1 : -1;
            // A linear stop: covers the distance in the slide time from twice the average speed.
            _slideSpeed = 2f * distance / SlideTime;
            _deceleration = _slideSpeed / SlideTime;
            _slideUntil = Time.time + SlideTime;
        }

        /// <summary>
        /// Runs the slide for one physics step. It stops at a ledge or a wall: a shove is a shove,
        /// not a way to throw enemies into pits the level never meant them to fall into.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!IsSliding)
            {
                return;
            }

            if (!_motor.HasGroundToward(_slideDirection) || _motor.HasWallToward(_slideDirection))
            {
                Stop();
                return;
            }

            var remaining = Mathf.Max(0f, _slideSpeed - _deceleration * (SlideTime - (_slideUntil - Time.time)));
            _motor.SetHorizontalVelocity(_slideDirection * remaining);
        }

        public void Stop()
        {
            if (IsSliding)
            {
                _motor.SetHorizontalVelocity(0f);
            }

            _slideUntil = float.MinValue;
        }

        /// <summary>
        /// Throws a killed body along the blow. It keeps colliding with the level alone, so it lands
        /// and lies on the floor, while the player, blades and other enemies pass through it.
        /// </summary>
        public void ThrowCorpse(in DamageInfo info)
        {
            Stop();
            if (_body != null)
            {
                _body.excludeLayers = ~_ground;
            }

            var along = info.HitDirection.x;
            var direction = Mathf.Abs(along) < 0.01f || info.IsStatusDamage ? 0f : Mathf.Sign(along);
            _motor.SetVelocity(new Vector2(direction * CorpseThrow.x,
                direction == 0f ? 0f : CorpseThrow.y));
            _airborneSince = -1f;
        }

        /// <summary>True once a thrown body has been falling so long that it went into a pit.</summary>
        public bool IsCorpseLost(bool grounded)
        {
            if (grounded)
            {
                _airborneSince = -1f;
                return false;
            }

            if (_airborneSince < 0f)
            {
                _airborneSince = Time.time;
            }

            return Time.time - _airborneSince > LostCorpseTime;
        }

        /// <summary>Puts the body back to a living enemy's collisions.</summary>
        public void Restore()
        {
            _slideUntil = float.MinValue;
            _airborneSince = -1f;
            if (_body != null)
            {
                _body.excludeLayers = _livingExclusions;
            }
        }
    }
}
