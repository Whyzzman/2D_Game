using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Implemented by the component that decides whether the player may steer right now, so the
    /// motor does not need to know about attacks, stuns or healing.
    /// </summary>
    public interface ILocomotionAuthority
    {
        LocomotionMode LocomotionMode { get; }

        /// <summary>Share of normal speed allowed in <see cref="LocomotionMode.Restricted"/> (of the walk) and <see cref="LocomotionMode.Attacking"/> (of the run).</summary>
        float MovementScale { get; }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(PlayerInputReader))]
    public sealed class PlayerMotor2D : MonoBehaviour, IFacing
    {
        private const float GroundProbeDistance = 0.04f;
        private const float MinimumGroundNormal = 0.65f;
        [SerializeField] private MovementSettings _settings = new MovementSettings();
        [SerializeField] private DashSettings _dashSettings = new DashSettings();
        [SerializeField] private LayerMask _groundMask;
        [SerializeField] private Transform _visual;
        [Tooltip("How far sideways a rising jump may be nudged when only the corner of a ledge "
            + "catches the head. About a quarter of the body's width.")]
        [SerializeField, Min(0f)] private float _cornerCorrection = 0.2f;
        private const int CornerCorrectionSteps = 4;
        private const int LedgeForgivenessSteps = 4;
        private readonly RaycastHit2D[] _groundHits = new RaycastHit2D[8];
        private readonly Collider2D[] _cornerOverlaps = new Collider2D[4];
        private float _cornerShiftUsed;
        private Rigidbody2D _body;
        private CapsuleCollider2D _capsule;
        private readonly SlopeWalker _slope = new SlopeWalker();
        private const float GroundSnapDistance = 0.3f;
        private bool _wasGrounded;
        private Vector2 _groundNormal = Vector2.up;
        private PlayerInputReader _input;
        private PlayerMovementLogic _logic;
        private ILocomotionAuthority _authority;
        private ContactFilter2D _groundFilter;
        private Vector3 _visualScale;
        private float _lastHorizontalInput;
        private Vector2? _skillVelocity;
        private float _recoilSpeed;
        private float _recoilFallSpeed;
        private float _recoilUntil;
        private bool _recoilPending;
        public void SetSkillVelocity(Vector2? velocity) { _skillVelocity = velocity; }

        /// <summary>The air jump and air dash back, as a wall or a grapple ring gives them.</summary>
        public void RefreshAirMoves() => _logic?.RefreshAirMoves();
        private GameProgress _progress;
        private StatusEffects _statuses;
        private WallMovement _wallMovement;
        private readonly Collider2D[] _destinationHits = new Collider2D[32];
        public WallMovement WallMovement => _wallMovement;
        public bool CanAirDash => _progress == null || _progress.HasAbility(AbilityIds.AirDash);

        /// <summary>
        /// Whether the second jump is owned. Unlike the air dash, it defaults to <c>false</c> with
        /// no progression present: it is the Hollow Warden's reward, and a scene without a save
        /// must not hand it out for free.
        /// </summary>
        public bool CanDoubleJump => _progress != null && _progress.HasAbility(AbilityIds.DoubleJump);

        /// <summary>True when a second jump is available in the air right now.</summary>
        public bool IsAirJumpReady => _logic != null && _logic.IsAirJumpReady;

        /// <summary>Raised when the player actually takes the second jump.</summary>
        public event Action AirJumped;

        /// <summary>A jump left the ground.</summary>
        public event Action Jumped;

        public event Action<MovementState> StateChanged;
        public event Action Respawned;

        public MovementState State => _logic?.State ?? MovementState.Idle;
        public int FacingDirection => _logic?.FacingDirection ?? 1;
        public bool IsGrounded => _logic != null && _logic.IsGrounded;
        public bool IsDashing => _logic != null && _logic.IsDashing;
        public bool IsDashReady => _logic != null && _logic.IsDashReady && (IsGrounded || CanAirDash);
        public float DashElapsed => _logic?.DashElapsed ?? 0f;
        public int AirDashesUsed => _logic?.AirDashesUsed ?? 0;
        public DashSettings DashSettings => _dashSettings;

        /// <summary>Full ground speed; the run clip is paced against it.</summary>
        public float RunSpeed => _settings.RunSpeed;

        /// <summary>
        /// Where a dash would go right now: the direction being steered, or the facing direction
        /// when the stick is neutral.
        /// </summary>
        public int DashDirection => Mathf.Abs(_lastHorizontalInput) > 0.2f
            ? (_lastHorizontalInput > 0f ? 1 : -1)
            : FacingDirection;
        public Vector2 Velocity => _body != null ? _body.linearVelocity : Vector2.zero;

        /// <summary>The drawn body, flipped to face; presentation may squash it, never move it.</summary>
        public Transform Visual => _visual;

        /// <summary>The visual's authored scale, before facing is applied.</summary>
        public Vector3 VisualScale => _visualScale;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _capsule = GetComponent<CapsuleCollider2D>();
            _input = GetComponent<PlayerInputReader>();
            _authority = GetComponent<ILocomotionAuthority>();
            _progress = FindAnyObjectByType<GameProgress>();
            _statuses = GetComponent<StatusEffects>();
            _wallMovement = GetComponent<WallMovement>();
            _logic = new PlayerMovementLogic(_settings, _dashSettings);
            _logic.AirJumped += () => AirJumped?.Invoke();
            _logic.Jumped += () => Jumped?.Invoke();
            _groundFilter = new ContactFilter2D();
            _groundFilter.SetLayerMask(_groundMask);
            _groundFilter.useTriggers = false;
            _body.gravityScale = 0f;
            _body.freezeRotation = true;
            if (_groundMask.value == 0)
            {
                Debug.LogError("PlayerMotor2D requires a Ground layer mask.", this);
                enabled = false;
            }

            if (_visual != null)
            {
                _visualScale = _visual.localScale;
            }
        }

        private void OnEnable()
        {
            _logic?.Reset();
        }

        private void OnDisable()
        {
            if (_body != null)
            {
                _body.linearVelocity = Vector2.zero;
            }
            _logic?.Reset();
            _input?.ClearInput();
        }

        private void FixedUpdate()
        {
            var previousState = State;
            var velocity = _body.linearVelocity;
            // Walking up a slope rises along it; that is still standing on the ground.
            var hasGround = (velocity.y <= 0.01f || _slope.IsOnSlope) && ProbeGround();
            if (!hasGround && _wasGrounded && (velocity.y <= 0.01f || _slope.IsOnSlope))
            {
                // Over the crest of a slope, or onto the downhill side: stay on the ground rather
                // than being thrown off it. A real drop is deeper than this and is a fall.
                hasGround = SnapToGround();
            }
            var mode = _authority?.LocomotionMode ?? LocomotionMode.Normal;
            var movementScale = _authority?.MovementScale ?? 1f;
            var input = _input.Consume();
            _lastHorizontalInput = input.Horizontal;

            // Re-read every step rather than cached at start: the ability is granted mid-session,
            // by a boss reward, and the player should have it the moment it is handed over.
            _logic.AirJumpsAllowed = CanDoubleJump ? _settings.AirJumps : 0;
            var canWall = mode == LocomotionMode.Normal && !IsDashing
                && (_progress == null || _progress.HasAbility(AbilityIds.WallRun));
            var wall = _wallMovement != null && canWall ? ProbeWall() : 0;
            // A press while pushing into a wall in the air is the wall's jump, not the second one.
            _logic.IsWallJumpPending = wall != 0 && !IsGrounded && input.IsJumpPressed
                && input.Horizontal * wall > 0.1f;
            var nextVelocity = _logic.Step(_slope.ToFlat(velocity), hasGround, input,
                Time.fixedDeltaTime, mode, movementScale);
            _logic.IsWallJumpPending = false;
            if (_skillVelocity.HasValue) { nextVelocity = _skillVelocity.Value; }
            if (_wallMovement != null)
            {
                nextVelocity = _wallMovement.Step(nextVelocity, input, wall,
                    IsGrounded, canWall, Time.fixedDeltaTime);
                // Hollow Knight's rule: a wall gives the air jump and the air dash back.
                var wallState = _wallMovement.State;
                if (wallState == WallMovementState.Slide || wallState == WallMovementState.Run
                    || (wallState == WallMovementState.Jump && input.IsJumpPressed))
                {
                    _logic.RefreshAirMoves();
                }
            }
            if (!IsDashing && mode != LocomotionMode.Knockback && _statuses != null)
            {
                nextVelocity.x *= _statuses.MovementMultiplier;
            }

            if (!_skillVelocity.HasValue)
            {
                nextVelocity = ApplyRecoil(nextVelocity, mode);
            }

            nextVelocity = _skillVelocity.HasValue
                ? SlopeOff(nextVelocity)
                : _slope.FromFlat(nextVelocity, hasGround && IsGrounded,
                    SlopeAhead(nextVelocity, _groundNormal), MinimumGroundNormal);
            if (IsGrounded)
            {
                _cornerShiftUsed = 0f;
                // Where spikes send the player back to: the last ground stood on clear of them.
                if (_capsule != null && !Spikes.Overlap(_capsule.bounds.center, _capsule.bounds.size))
                {
                    LastSafeGround = _body.position;
                    HasSafeGround = true;
                }
            }

            if (!IsGrounded && nextVelocity.y < 0f && !_skillVelocity.HasValue && !IsDashing
                && mode != LocomotionMode.Knockback)
            {
                TryLedgeForgive(-nextVelocity.y * Time.fixedDeltaTime, input.Horizontal, nextVelocity.x);
            }

            // Along a slope the move is swept as one: split into axes, the rise itself reads as a wall.
            var constrained = _slope.IsOnSlope
                ? ConstrainAlong(nextVelocity, Time.fixedDeltaTime)
                : ConstrainVelocity(nextVelocity, Time.fixedDeltaTime);
            if (nextVelocity.y > 0f && constrained.y < nextVelocity.y - 0.001f
                && TryCornerCorrect(nextVelocity.y * Time.fixedDeltaTime, input.Horizontal))
            {
                constrained.y = nextVelocity.y;
            }

            _body.linearVelocity = constrained;
            _wasGrounded = IsGrounded && constrained.y <= Mathf.Max(0.01f, Mathf.Abs(constrained.x));
            if (_visual != null)
            {
                _visual.localScale = new Vector3(
                    Mathf.Abs(_visualScale.x) * FacingDirection, _visualScale.y, _visualScale.z);
            }
            if (previousState != State)
            {
                StateChanged?.Invoke(State);
            }
        }

        /// <summary>Starts a dash toward <paramref name="direction"/>; false when it is on cooldown.</summary>
        public bool TryBeginDash(int direction)
        {
            return IsDashReady && _logic.TryBeginDash(direction);
        }

        public void CancelDash()
        {
            _logic?.CancelDash();
        }

        /// <summary>A pogo off an enemy or spikes: up at the settings' speed, air moves back.</summary>
        public void Bounce()
        {
            _logic?.Bounce(_settings.PogoSpeed);
        }

        /// <summary>A throw upward at a speed of the caller's, not cut by a released jump (the grapple's pop).</summary>
        public void Bounce(float speed)
        {
            _logic?.Bounce(speed);
        }

        public bool IsBouncing => _logic != null && _logic.IsBouncing;

        /// <summary>
        /// The recoil of a landed hit, as in Hollow Knight: for a moment the horizontal speed is
        /// the recoil's, whatever the stick says, and a fall may be pressed a little faster. It
        /// nudges, it never throws — a dash, a skill or a knockback overrides it.
        /// </summary>
        public void Recoil(float horizontalSpeed, float minimumFallSpeed, float duration)
        {
            _recoilSpeed = horizontalSpeed;
            _recoilFallSpeed = minimumFallSpeed;
            _recoilUntil = Time.time + Mathf.Max(0f, duration);
            _recoilPending = true;
        }

        public bool IsRecoiling => Time.time < _recoilUntil;

        private Vector2 ApplyRecoil(Vector2 velocity, LocomotionMode mode)
        {
            if (IsDashing || mode == LocomotionMode.Knockback)
            {
                _recoilPending = false;
                return velocity;
            }

            if (!IsRecoiling)
            {
                // The push ends where it was meant to: the slowed movement of a swing would
                // otherwise take its time braking, and carry the player twice as far.
                if (_recoilPending && velocity.x * _recoilSpeed > 0f)
                {
                    velocity.x = 0f;
                }

                _recoilPending = false;
                return velocity;
            }

            if (!Mathf.Approximately(_recoilSpeed, 0f))
            {
                velocity.x = _recoilSpeed;
            }

            if (_recoilFallSpeed > 0f && !IsGrounded)
            {
                velocity.y = Mathf.Min(velocity.y, -_recoilFallSpeed);
            }

            return velocity;
        }

        /// <summary>What the player stands on; the camera reads the level's shape from it.</summary>
        public LayerMask GroundMask => _groundMask;

        /// <summary>The last spot on the ground clear of spikes; spikes return the player here.</summary>
        public Vector2 LastSafeGround { get; private set; }
        public bool HasSafeGround { get; private set; }

        /// <summary>Scales the dash cooldown; the skill tree's footwork passives use it.</summary>
        public void SetDashCooldownScale(float scale)
        {
            if (_logic != null)
            {
                _logic.CooldownScale = Mathf.Max(0.05f, scale);
            }
        }

        /// <summary>
        /// Sets velocity outright, the way knockback does. Only meaningful while the locomotion
        /// authority is not steering, which is exactly when a lunging attack owns the movement.
        /// </summary>
        public void ApplyImpulse(Vector2 velocity)
        {
            if (_body != null)
            {
                _body.linearVelocity = velocity;
            }
        }

        public void ResetTo(Vector2 position)
        {
            _slope.Reset();
            _wasGrounded = false;
            _body.linearVelocity = Vector2.zero;
            _body.angularVelocity = 0f;
            // Reset both transform and physics pose to discard interpolation across the teleport.
            var interpolation = _body.interpolation;
            _body.interpolation = RigidbodyInterpolation2D.None;
            _body.position = position;
            transform.position = new Vector3(position.x, position.y, transform.position.z);
            _body.interpolation = interpolation;
            _logic.Reset();
            _cornerShiftUsed = 0f;
            _skillVelocity = null;
            _recoilUntil = 0f;
            _recoilPending = false;
            _wallMovement?.ResetMovement();
            _input.ClearInput();
            if (_visual != null)
            {
                _visual.localScale = _visualScale;
            }
            StateChanged?.Invoke(State);
            Respawned?.Invoke();
        }

        /// <summary>
        /// Stops movement at solid geometry, one axis at a time. A wall stops only what goes into
        /// it: pressed against a step and jumping, the jump still happens. Sweeping the combined
        /// vector instead scaled the whole of it down to nothing on contact, so a jump made in
        /// contact with a step — the most natural way to climb one — silently did nothing.
        /// </summary>
        public Vector2 ConstrainVelocity(Vector2 velocity, float deltaTime)
        {
            var horizontal = ConstrainAlong(new Vector2(velocity.x, 0f), deltaTime);
            var vertical = ConstrainAlong(new Vector2(0f, velocity.y), deltaTime);
            return new Vector2(horizontal.x, vertical.y);
        }

        private Vector2 ConstrainAlong(Vector2 velocity, float deltaTime)
        {
            var distance = velocity.magnitude * deltaTime;
            if (distance <= 0f) { return velocity; }
            var direction = velocity.normalized;
            var count = _capsule.Cast(direction, _groundFilter, _groundHits, distance + 0.01f);
            var allowed = distance;
            for (var i = 0; i < count; i++)
            {
                if (Vector2.Dot(_groundHits[i].normal, direction) < -0.1f && Stops(_groundHits[i].collider, direction))
                {
                    allowed = Mathf.Min(allowed, Mathf.Max(0f, _groundHits[i].distance - 0.01f));
                }
            }
            return velocity * (allowed / distance);
        }

        /// <summary>Where the bottom of the body is when it stands at <paramref name="position"/>.</summary>
        private float FeetAt(Vector2 position) => position.y + _capsule.offset.y - _capsule.size.y * 0.5f;

        /// <summary>
        /// Whether a hit stops a move along <paramref name="direction"/>: a one-way ledge only stops a
        /// move down onto it from above; up and sideways the body passes through.
        /// </summary>
        private bool Stops(Collider2D collider, Vector2 direction) =>
            !OneWayPlatform.Is(collider) || (direction.y < -0.5f && OneWayPlatform.Stops(collider, FeetAt(_body.position)));

        /// <summary>Overlapping colliders that are solid to a body inside them: one-way ledges are not.</summary>
        private int SolidOverlaps(Vector2 center)
        {
            var count = Physics2D.OverlapCapsule(center, _capsule.size, _capsule.direction, 0f,
                _groundFilter, _cornerOverlaps);
            var solid = 0;
            for (var i = 0; i < count; i++)
            {
                if (!OneWayPlatform.Is(_cornerOverlaps[i]))
                {
                    solid++;
                }
            }

            return solid;
        }

        /// <summary>
        /// A jump that clips a ledge with only the edge of the head slides past it instead of
        /// dying on the spot. The body is nudged sideways by the smallest step that clears the
        /// rise — never more than <see cref="_cornerCorrection"/> over the whole jump, so a real
        /// ceiling still stops it rather than being crept past one step at a time. The side the
        /// player is steering toward is tried first.
        /// </summary>
        private bool TryCornerCorrect(float rise, float steering)
        {
            var available = _cornerCorrection - _cornerShiftUsed;
            if (available <= 0.0001f)
            {
                return false;
            }

            var first = steering < -0.1f ? -1 : 1;
            var step = _cornerCorrection / CornerCorrectionSteps;
            for (var i = 1; i <= CornerCorrectionSteps && step * i <= available + 0.0001f; i++)
            {
                for (var side = 0; side < 2; side++)
                {
                    var shift = new Vector2((side == 0 ? first : -first) * step * i, 0f);
                    if (IsRiseClearFrom(_body.position + shift, rise))
                    {
                        _body.position += shift;
                        _cornerShiftUsed += step * i;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// The feet's twin of the corner correction: a fall toward a ledge whose feet miss its
        /// corner by a hair lands on it instead of scraping down its face. When this step's fall
        /// would not land, the body is nudged toward the steered side by the smallest step that
        /// does — no farther than <see cref="MovementSettings.LedgeForgiveness"/> past the body's
        /// side, plus the rim of the rounded foot that is too steep to stand on (a capsule resting
        /// there only by its curve is not on the ground either, it would hang on the corner).
        /// </summary>
        private bool TryLedgeForgive(float fall, float steering, float horizontalSpeed)
        {
            var tolerance = _settings.LedgeForgiveness;
            var direction = steering > 0.1f ? 1 : steering < -0.1f ? -1 : 0;
            if (tolerance <= 0f || fall <= 0f || direction == 0 || horizontalSpeed * direction < 0f)
            {
                return false;
            }

            var radius = _capsule.size.x * 0.5f;
            var rim = radius * (1f - Mathf.Sqrt(1f - MinimumGroundNormal * MinimumGroundNormal));
            var reach = tolerance + rim;
            var position = _body.position;
            if (LandsFrom(position, fall))
            {
                return false;
            }

            var step = reach / LedgeForgivenessSteps;
            for (var i = 1; i <= LedgeForgivenessSteps; i++)
            {
                var shifted = position + new Vector2(direction * step * i, 0f);
                if (IsSpaceClear(shifted) && LandsFrom(shifted, fall))
                {
                    _body.position = shifted;
                    return true;
                }
            }

            return false;
        }

        /// <summary>Whether falling <paramref name="fall"/> from there meets ground flat enough to stand on.</summary>
        private bool LandsFrom(Vector2 position, float fall)
        {
            var count = Physics2D.CapsuleCast(position + _capsule.offset, _capsule.size,
                _capsule.direction, 0f, Vector2.down, _groundFilter, _groundHits, fall + 0.01f);
            for (var i = 0; i < count; i++)
            {
                if (_groundHits[i].rigidbody != _body && _groundHits[i].normal.y >= MinimumGroundNormal
                    && OneWayPlatform.Stops(_groundHits[i].collider, FeetAt(position)))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsSpaceClear(Vector2 position) => SolidOverlaps(position + _capsule.offset) == 0;

        private bool IsRiseClearFrom(Vector2 position, float rise)
        {
            var center = position + _capsule.offset;
            if (SolidOverlaps(center) > 0)
            {
                return false;
            }

            var count = Physics2D.CapsuleCast(center, _capsule.size, _capsule.direction, 0f,
                Vector2.up, _groundFilter, _groundHits, rise + 0.01f);
            for (var i = 0; i < count; i++)
            {
                if (_groundHits[i].normal.y < -0.1f && !OneWayPlatform.Is(_groundHits[i].collider))
                {
                    return false;
                }
            }

            return true;
        }

        public bool TryDisplace(Vector2 destination)
        {
            var delta = destination - _body.position;
            var safe = ConstrainVelocity(delta, 1f);
            if ((safe - delta).sqrMagnitude > 0.0001f) { return false; }
            var filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(~0);
            var count = Physics2D.OverlapCapsule(destination + _capsule.offset,
                _capsule.size, _capsule.direction, 0f, filter, _destinationHits);
            for (var i = 0; i < count; i++)
            {
                var hit = _destinationHits[i];
                if (hit.attachedRigidbody == _body || OneWayPlatform.Is(hit)) { continue; }
                if (hit.GetComponent<KillZone>() != null
                    || (!hit.isTrigger && ((_groundMask.value & (1 << hit.gameObject.layer)) != 0)))
                {
                    return false;
                }
            }
            _body.position = destination;
            transform.position = new Vector3(destination.x, destination.y, transform.position.z);
            _body.linearVelocity = Vector2.zero;
            return true;
        }

        private int ProbeWall()
        {
            var direction = _lastHorizontalInput > 0f ? 1 : -1;
            var count = _capsule.Cast(new Vector2(direction, 0f), _groundFilter, _groundHits, 0.08f);
            for (var i = 0; i < count; i++)
            {
                // A surface marked as smooth offers no grip; it stays solid, it just is not a wall.
                if (Mathf.Abs(_groundHits[i].normal.x) > 0.9f
                    && !_groundHits[i].collider.TryGetComponent<NonClimbableSurface>(out _)
                    && !OneWayPlatform.Is(_groundHits[i].collider))
                {
                    return direction;
                }
            }
            return 0;
        }

        /// <summary>Standing on something; remembers the flattest surface under the feet.</summary>
        private bool ProbeGround()
        {
            var count = _capsule.Cast(Vector2.down, _groundFilter, _groundHits, GroundProbeDistance);
            var found = false;
            _groundNormal = Vector2.up;
            for (var i = 0; i < count; i++)
            {
                if (_groundHits[i].collider != null
                    && _groundHits[i].rigidbody != _body
                    && _groundHits[i].normal.y >= MinimumGroundNormal
                    && OneWayPlatform.Stops(_groundHits[i].collider, FeetAt(_body.position))
                    && (!found || _groundHits[i].normal.y > _groundNormal.y))
                {
                    _groundNormal = _groundHits[i].normal;
                    found = true;
                }
            }
            return found;
        }

        /// <summary>
        /// On level ground running at the foot of a rise, the rise ahead is the surface to follow;
        /// swept sideways it would read as a wall and stop the player dead.
        /// </summary>
        private Vector2 SlopeAhead(Vector2 flat, Vector2 underFoot)
        {
            if (underFoot.y < 0.999f || Mathf.Abs(flat.x) < 0.01f || flat.y > 0f)
            {
                return underFoot;
            }

            var direction = new Vector2(Mathf.Sign(flat.x), 0f);
            var count = _capsule.Cast(direction, _groundFilter, _groundHits,
                Mathf.Abs(flat.x) * Time.fixedDeltaTime + 0.02f);
            for (var i = 0; i < count; i++)
            {
                var normal = _groundHits[i].normal;
                if (_groundHits[i].rigidbody != _body && normal.y >= MinimumGroundNormal && normal.y < 0.999f
                    && normal.x * direction.x < 0f && !OneWayPlatform.Is(_groundHits[i].collider))
                {
                    return normal;
                }
            }

            return underFoot;
        }

        private bool SnapToGround()
        {
            var count = _capsule.Cast(Vector2.down, _groundFilter, _groundHits, GroundSnapDistance);
            var nearest = float.MaxValue;
            for (var i = 0; i < count; i++)
            {
                if (_groundHits[i].rigidbody != _body && _groundHits[i].normal.y >= MinimumGroundNormal
                    && OneWayPlatform.Stops(_groundHits[i].collider, FeetAt(_body.position)))
                {
                    nearest = Mathf.Min(nearest, _groundHits[i].distance);
                }
            }

            if (nearest == float.MaxValue)
            {
                return false;
            }

            _body.position += Vector2.down * Mathf.Max(0f, nearest - 0.005f);
            Physics2D.SyncTransforms();
            return ProbeGround();
        }

        private Vector2 SlopeOff(Vector2 velocity)
        {
            _slope.Reset();
            return velocity;
        }

        /// <summary>True while walking along a slope; for tests and the animation.</summary>
        public bool IsOnSlope => _slope.IsOnSlope;
    }
}
