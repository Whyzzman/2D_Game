using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// What the hero's movement leaves in the world, as in Hollow Knight: a puff of dust when the
    /// feet leave or strike the ground, when a run starts or turns, and off a wall slid down or
    /// leapt from — and a hair of squash and stretch on the body at the same moments.
    ///
    /// Dust marks changes, not motion: running on at full speed stays clean, so the moments that
    /// matter are the ones the eye catches. Everything here reads the movement and nothing feeds
    /// back into it; the squash scales the drawn body about the feet and never touches physics.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MovementFeedback : MonoBehaviour
    {
        /// <summary>Stretch on leaving the ground: about six percent taller.</summary>
        public const float JumpStretch = 0.06f;

        /// <summary>Squash on the hardest landing: about eight percent shorter.</summary>
        public const float LandSquash = 0.08f;

        /// <summary>Slower than this, touching down is a step, not a landing worth dust.</summary>
        public const float LandDustSpeed = 3f;

        /// <summary>
        /// A long fall stretches the body a little along its speed (hero animation 2.0): nothing
        /// until the fall is faster than any jump's, then up to four percent at full speed.
        /// </summary>
        public const float FallStretch = 0.04f;
        private const float FallStretchFrom = 11f;

        private const float HardLandingSpeed = 18f;
        private const float SquashTime = 0.14f;
        private const float KickCooldown = 0.18f;
        private const float WallDustInterval = 0.11f;
        private const float RespawnQuiet = 0.25f;
        private const float StartWindow = 0.3f;
        private const float TurnRecovery = 0.45f;

        [SerializeField] private PlayerMotor2D _motor;
        [SerializeField] private WallMovement _wall;
        [SerializeField] private PlayerAnimationController _animation;

        private CapsuleCollider2D _capsule;
        private Transform _visual;
        private Vector3 _visualPosition;
        private float _feetLocalY;

        private float _squash;
        private float _squashPeak;
        private float _squashElapsed = float.MaxValue;
        private bool _isSquashed;
        private float _pulse;
        private float _fallStretch;

        private float _fallSpeed;
        private bool _wasGrounded;
        private float _runSpeedSeen;
        private float _previousSpeed;
        private float _stillAt = float.MinValue;
        private int _facing;
        private float _lastKick = float.MinValue;
        private float _nextWallDust;
        private float _quietUntil;

        /// <summary>Current squash: above zero stretched tall, below zero pressed flat.</summary>
        public float Squash => _isSquashed ? _squash : 0f;

        /// <summary>The share of <see cref="Squash"/> that comes from falling fast.</summary>
        public float FallStretchNow => _isSquashed ? _fallStretch : 0f;

        private void Awake()
        {
            if (_motor == null) { _motor = GetComponent<PlayerMotor2D>(); }
            if (_wall == null) { _wall = GetComponent<WallMovement>(); }
            if (_animation == null) { _animation = GetComponent<PlayerAnimationController>(); }
            _capsule = GetComponent<CapsuleCollider2D>();
            _visual = _motor != null ? _motor.Visual : null;
            if (_visual != null) { _visualPosition = _visual.localPosition; }
            _feetLocalY = _capsule != null ? _capsule.offset.y - _capsule.size.y * 0.5f : -0.8f;
        }

        private void OnEnable()
        {
            if (_motor != null)
            {
                _motor.Jumped += OnJumped;
                _motor.AirJumped += OnAirJumped;
                _motor.Respawned += OnRespawned;
            }

            if (_wall != null) { _wall.StateChanged += OnWallState; }
            if (_animation != null) { _animation.Landed += OnLanded; }
            _facing = _motor != null ? _motor.FacingDirection : 1;
        }

        private void OnDisable()
        {
            if (_motor != null)
            {
                _motor.Jumped -= OnJumped;
                _motor.AirJumped -= OnAirJumped;
                _motor.Respawned -= OnRespawned;
            }

            if (_wall != null) { _wall.StateChanged -= OnWallState; }
            if (_animation != null) { _animation.Landed -= OnLanded; }
            ClearSquash();
        }

        /// <summary>
        /// The squash as a scale about the feet: taller is narrower and flatter is wider, so the
        /// body keeps roughly its bulk instead of inflating.
        /// </summary>
        public static Vector2 ScaleFor(float squash) =>
            new Vector2(1f - squash * 0.8f, 1f + squash);

        /// <summary>How hard a landing squashes, from the speed the feet struck the ground at.</summary>
        public static float LandingWeight(float fallSpeed) =>
            Mathf.Lerp(0.35f, 1f, Mathf.InverseLerp(LandDustSpeed, HardLandingSpeed, fallSpeed));

        private void FixedUpdate()
        {
            if (_motor == null)
            {
                return;
            }

            var velocity = _motor.Velocity;
            var grounded = _motor.IsGrounded;
            if (!grounded)
            {
                _fallSpeed = Mathf.Max(_fallSpeed, -velocity.y);
            }

            var falling = !grounded && !_motor.IsDashing && !FeedbackSettings.ReducedMotion;
            _fallStretch = falling
                ? FallStretch * Mathf.InverseLerp(FallStretchFrom, HardLandingSpeed, -velocity.y)
                : 0f;

            TrackRun(grounded, velocity.x);
            TrackWallSlide();
            _wasGrounded = grounded;
        }

        /// <summary>
        /// A kick of dust when a run starts from standing and when it turns at speed. Running on
        /// makes none: at full speed Hollow Knight's ground is clean, and so is ours.
        /// </summary>
        private void TrackRun(bool grounded, float speedX)
        {
            var facing = _motor.FacingDirection;
            var speed = Mathf.Abs(speedX);
            var runSpeed = Mathf.Max(0.1f, _motor.RunSpeed);
            if (grounded && _wasGrounded && !_motor.IsDashing)
            {
                var turned = facing != _facing && _runSpeedSeen > runSpeed * 0.6f;
                // Crossing into a run shortly after standing: the ramp up takes a few steps, so
                // stillness is remembered for a moment rather than required on the same step.
                var started = speed > runSpeed * 0.3f && _previousSpeed <= runSpeed * 0.3f
                    && Time.time - _stillAt < StartWindow
                    // Out of a turn the speed climbs again from zero; that is the same kick.
                    && Time.time - _lastKick > TurnRecovery;
                if (turned || started)
                {
                    Kick(facing);
                }
            }

            if (!grounded || speed < runSpeed * 0.15f)
            {
                _stillAt = grounded ? Time.time : float.MinValue;
            }

            // A decaying memory of speed, so a turn through zero still counts as a turn at speed.
            _runSpeedSeen = grounded ? Mathf.Max(speed, _runSpeedSeen - runSpeed * 5f * Time.fixedDeltaTime) : speed;
            _previousSpeed = speed;
            _facing = facing;
        }

        private void Kick(int facing)
        {
            if (Time.time - _lastKick < KickCooldown || IsQuiet)
            {
                return;
            }

            _lastKick = Time.time;
            CombatFeedbackService.Play(FeedbackKind.RunDust, Feet, new Vector2(facing, 0f), 1f, GroundTint);
        }

        private void TrackWallSlide()
        {
            if (_wall == null || _wall.State != WallMovementState.Slide || Time.time < _nextWallDust)
            {
                return;
            }

            _nextWallDust = Time.time + WallDustInterval;
            var side = _wall.WallSide != 0 ? _wall.WallSide : _motor.FacingDirection;
            CombatFeedbackService.Play(FeedbackKind.WallSlideDust, WallPoint(side, 0.35f),
                new Vector2(-side, 0f));
        }

        private void OnWallState(WallMovementState state)
        {
            if (state != WallMovementState.Jump || IsQuiet)
            {
                return;
            }

            var side = _wall.WallSide != 0 ? _wall.WallSide : -_motor.FacingDirection;
            CombatFeedbackService.Play(FeedbackKind.WallJump, WallPoint(side, 0f), new Vector2(-side, 0f));
            StartSquash(JumpStretch);
        }

        private void OnJumped()
        {
            _fallSpeed = 0f;
            if (IsQuiet)
            {
                return;
            }

            CombatFeedbackService.Play(FeedbackKind.JumpDust, Feet, Vector2.zero, 1f, GroundTint);
            StartSquash(JumpStretch);
        }

        private void OnAirJumped()
        {
            _fallSpeed = 0f;
            CombatFeedbackService.Play(FeedbackKind.AirJump, Feet);
            StartSquash(JumpStretch * 0.7f);
        }

        private void OnLanded()
        {
            var fall = _fallSpeed;
            _fallSpeed = 0f;
            if (fall < LandDustSpeed || IsQuiet)
            {
                return;
            }

            var weight = LandingWeight(fall);
            CombatFeedbackService.Play(FeedbackKind.LandDust, Feet, Vector2.right,
                Mathf.Lerp(0.7f, 1.25f, weight), GroundTint);
            StartSquash(-LandSquash * weight);
        }

        private void OnRespawned()
        {
            _quietUntil = Time.time + RespawnQuiet;
            _fallSpeed = 0f;
            _runSpeedSeen = 0f;
            _stillAt = float.MinValue;
            ClearSquash();
        }

        private bool IsQuiet => Time.time < _quietUntil;

        /// <summary>The dust takes the colour of what it rose from (GAME_FEEL_IDEAS item 4).</summary>
        private Color GroundTint => Surfaces.DustTint(Surfaces.Under(Feet));

        private Vector2 Feet => _capsule != null
            ? new Vector2(_capsule.bounds.center.x, _capsule.bounds.min.y)
            : (Vector2)transform.position;

        private Vector2 WallPoint(int side, float height)
        {
            if (_capsule == null)
            {
                return transform.position;
            }

            var bounds = _capsule.bounds;
            return new Vector2(bounds.center.x + bounds.extents.x * side, bounds.min.y + bounds.size.y * height);
        }

        // ---------- squash ----------

        private void StartSquash(float peak)
        {
            if (FeedbackSettings.ReducedMotion || _visual == null)
            {
                return;
            }

            _squashPeak = peak;
            _pulse = peak;
            _squash = peak + _fallStretch;
            _squashElapsed = 0f;
            _isSquashed = true;
        }

        /// <summary>
        /// Runs after the motor has flipped the visual to face, so the squash rides on top of the
        /// facing instead of fighting it; with nothing to show it hands the visual back untouched.
        /// </summary>
        private void LateUpdate()
        {
            if (_visual == null)
            {
                return;
            }

            _squashElapsed += Time.deltaTime;
            var t = Mathf.Clamp01(_squashElapsed / SquashTime);
            var ease = 1f - t;
            _pulse = t >= 1f ? 0f : _squashPeak * ease * ease;
            var squash = _pulse + _fallStretch;
            if (Mathf.Approximately(squash, 0f))
            {
                ClearSquash();
                return;
            }

            _isSquashed = true;
            _squash = squash;
            var scale = ScaleFor(_squash);
            var authored = _motor.VisualScale;
            _visual.localScale = new Vector3(Mathf.Abs(authored.x) * _motor.FacingDirection * scale.x,
                authored.y * scale.y, authored.z);
            // Scaled about the feet, not the middle: a landing presses down into the floor it hit.
            _visual.localPosition = _visualPosition + new Vector3(0f, _feetLocalY * (1f - scale.y), 0f);
        }

        private void ClearSquash()
        {
            if (!_isSquashed)
            {
                return;
            }

            _isSquashed = false;
            _squash = 0f;
            _pulse = 0f;
            _squashElapsed = float.MaxValue;
            if (_visual != null && _motor != null)
            {
                var authored = _motor.VisualScale;
                _visual.localScale = new Vector3(Mathf.Abs(authored.x) * _motor.FacingDirection,
                    authored.y, authored.z);
                _visual.localPosition = _visualPosition;
            }
        }
    }
}
