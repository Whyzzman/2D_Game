using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Follows the player. Beyond plain smoothing it frames the shot — the player sits below the
    /// centre so the floor is not a line across the middle of the screen with nothing under it —
    /// leads into the direction of travel, and can zoom for a boss or punch in for a parry.
    ///
    /// Every one of those defaults to off: the movement sandbox keeps the exact follow its tests
    /// measure, and the Tomb turns them on in its own camera.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private PlayerMotor2D _target;
        [SerializeField, Min(0.01f)] private float _smoothTime = 0.12f;
        [Tooltip("Optional. Its offset is added after following, never instead of it.")]
        [SerializeField] private CameraShaker _shaker;
        [Tooltip("Where the player sits relative to the centre. Positive y puts the view above "
            + "the player, so the floor sits low in the frame.")]
        [SerializeField] private Vector2 _framingOffset;
        [Tooltip("How far ahead of a running player the view leads, in units. Zero standing still.")]
        [SerializeField, Min(0f)] private float _lookAhead;
        [SerializeField, Min(0.01f)] private float _lookAheadSmoothTime = 0.45f;
        [Tooltip("Speed at which the lead reaches its full distance.")]
        [SerializeField, Min(0.1f)] private float _lookAheadFullSpeed = 6.5f;
        [Header("Vertical dead zone")]
        [Tooltip("How far above the last footing the player may rise before the view follows. "
            + "Zero turns the dead zone off and the view tracks every jump.")]
        [SerializeField, Min(0f)] private float _verticalDeadZoneAbove;
        [Tooltip("How far below the last footing the player may drop before the view follows.")]
        [SerializeField, Min(0f)] private float _verticalDeadZoneBelow;
        [Tooltip("How far the view runs ahead of a long fall, so the landing is seen in time.")]
        [SerializeField, Min(0f)] private float _fallLookAhead;
        [Tooltip("Falling faster than this counts as a long fall. An ordinary jump lands at ~14.")]
        [SerializeField, Min(0f)] private float _fallLookAheadSpeed = 15f;
        [SerializeField, Min(0.01f)] private float _fallLookAheadSmoothTime = 0.5f;
        [Header("Looking up and down")]
        [Tooltip("How far the view moves when the player stands still holding up or down "
            + "(GAME_FEEL_IDEAS.md, item 1). Zero turns it off.")]
        [SerializeField, Min(0f)] private float _lookDistance = 3f;
        [Tooltip("How long up or down has to be held, standing still, before the view moves: a "
            + "swing up or a pogo never moves it.")]
        [SerializeField, Min(0f)] private float _lookDelay = 0.4f;
        [SerializeField, Min(0.01f)] private float _lookSmoothTime = 0.35f;
        [Tooltip("The camera itself, not only where it is heading, stays inside the room once it is "
            + "in: the smoothing lag of a zoom out and a shake against a wall never show past it.")]
        [SerializeField] private bool _hardRoomEdges = true;
        [Tooltip("How far past the frame the view may already be and still be pulled to its edge at "
            + "once: a zoom's step in one frame. Farther (a frame set with no slide) is left to the "
            + "smoothing, so the view never cuts.")]
        [SerializeField, Min(0f)] private float _hardEdgePull = 0.1f;
        private Vector2 _smoothingVelocity;
        private Vector2 _appliedShake;
        private float _anchorY;
        private float _fallLead;
        private float _fallLeadVelocity;
        private Rect? _worldBounds;
        private Rect? _roomBounds;
        private Rect? _previousRoomBounds;
        private float _roomBlendStart;
        private float _roomBlendTime;
        private Rect? _fightBounds;
        private float _lead;
        private float _leadVelocity;
        private float _depth;
        private Camera _camera;
        private float _baseSize;
        private float _zoomTarget;
        private float _zoom;
        private float _zoomVelocity;
        private float _zoomSmoothTime = 0.8f;
        private float _punchUntil;
        private float _punchAmount;
        private PlayerInputReader _input;
        private float _lookHeld;
        private int _lookSign;
        private float _look;
        private float _lookVelocity;

        /// <summary>Where the camera is heading, before shake; tests measure framing against this.</summary>
        public Vector2 TargetPosition => _target != null
            ? Clamp(new Vector2(_target.transform.position.x + _lead, _anchorY + _fallLead + _look)
                + _framingOffset)
            : (Vector2)transform.position;

        /// <summary>The rectangle the view is kept inside right now, if any.</summary>
        public Rect? ActiveBounds => _fightBounds ?? _roomBounds ?? _worldBounds;

        /// <summary>True while a boss fight holds the view on its arena.</summary>
        public bool HasFightBounds => _fightBounds.HasValue;

        /// <summary>True while the view is sliding from one room into the next.</summary>
        public bool IsChangingRooms => _fightBounds == null && _previousRoomBounds.HasValue
            && Time.time < _roomBlendStart + _roomBlendTime;

        private bool HasVerticalDeadZone => _verticalDeadZoneAbove > 0f;

        public Vector2 FramingOffset => _framingOffset;

        /// <summary>
        /// The developer's flight (<see cref="DeveloperNoclip"/>): the view follows the hero in both
        /// axes with no dead zone, room, arena or world limit.
        /// </summary>
        public bool IsFree { get; set; }

        private void Awake()
        {
            _depth = transform.position.z;
            _camera = GetComponent<Camera>();
            _baseSize = _camera.orthographicSize;
            _zoomTarget = _baseSize;
            _zoom = _baseSize;
            if (_shaker == null) { _shaker = GetComponent<CameraShaker>(); }
            if (_target != null) { _input = _target.GetComponent<PlayerInputReader>(); }
            if (_target == null)
            {
                Debug.LogError("CameraFollow2D requires a Player target.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (_target != null)
            {
                _target.Respawned += SnapToTarget;
                SnapToTarget();
            }
        }

        private void OnDisable()
        {
            if (_target != null)
            {
                _target.Respawned -= SnapToTarget;
            }
        }

        private void LateUpdate()
        {
            UpdateVertical();
            UpdateLook();
            if (_lookAhead > 0f)
            {
                // Led by speed, not by facing: a player standing still is centred, and turning on
                // the spot does not swing the whole view.
                var speed = _target.Velocity.x;
                var wanted = Mathf.Clamp(speed / _lookAheadFullSpeed, -1f, 1f) * _lookAhead;
                _lead = Mathf.SmoothDamp(_lead, wanted, ref _leadVelocity, _lookAheadSmoothTime,
                    Mathf.Infinity, Time.deltaTime);
            }

            // Following happens against the un-shaken position, so a shake never feeds back into
            // the smoothing and never leaves the camera drifted once it has decayed.
            var basePosition = (Vector2)transform.position - _appliedShake;
            var position = Vector2.SmoothDamp(basePosition, TargetPosition,
                ref _smoothingVelocity, _smoothTime, Mathf.Infinity, Time.deltaTime);
            position = HoldInside(basePosition, position);
            var shake = _shaker != null ? _shaker.CurrentOffset : Vector2.zero;
            var shaken = HoldInside(position, position + shake);
            _appliedShake = shaken - position;
            transform.position = new Vector3(shaken.x, shaken.y, _depth);

            UpdateZoom();
        }

        public void SnapToTarget()
        {
            _smoothingVelocity = Vector2.zero;
            _lead = 0f;
            _leadVelocity = 0f;
            _fallLead = 0f;
            _fallLeadVelocity = 0f;
            _look = 0f;
            _lookVelocity = 0f;
            _lookHeld = 0f;
            _previousRoomBounds = null;
            _appliedShake = Vector2.zero;
            if (_target != null)
            {
                _anchorY = _target.transform.position.y;
                var position = TargetPosition;
                transform.position = new Vector3(position.x, position.y, _depth);
            }
        }

        /// <summary>
        /// The whole world's extent: the view never shows past it. Null lifts the limit.
        /// </summary>
        public void SetWorldBounds(Rect? bounds) => _worldBounds = bounds;

        /// <summary>
        /// The room the player is in; it takes over from the world's limit. A change of room slides
        /// the view across over <paramref name="blendTime"/> seconds instead of cutting, as Hollow
        /// Knight's rooms do; zero cuts, for a respawn or a jump across the map.
        /// </summary>
        public void SetRoomBounds(Rect? bounds, float blendTime)
        {
            if (bounds == _roomBounds)
            {
                return;
            }

            _previousRoomBounds = blendTime > 0f && _roomBounds.HasValue ? _roomBounds : null;
            _roomBounds = bounds;
            _roomBlendStart = Time.time;
            _roomBlendTime = Mathf.Max(0.01f, blendTime);
        }

        /// <summary>
        /// A boss arena for the length of the fight; it replaces the world's limit until cleared,
        /// so the camera stays on the arena instead of drifting past its walls.
        /// </summary>
        public void SetFightBounds(Rect? bounds) => _fightBounds = bounds;

        /// <summary>
        /// Keeps the vertical anchor at the last footing, the way Hollow Knight's camera does: a
        /// jump alone does not move the view. It follows on landing, when the player climbs past
        /// the dead zone (a wall run, a tall drop), and runs ahead of a long fall.
        /// </summary>
        private void UpdateVertical()
        {
            if (_target == null)
            {
                return;
            }

            var y = _target.transform.position.y;
            if (!HasVerticalDeadZone || IsFree || _target.IsGrounded)
            {
                _anchorY = y;
            }
            else if (y > _anchorY + _verticalDeadZoneAbove)
            {
                _anchorY = y - _verticalDeadZoneAbove;
            }
            else if (y < _anchorY - _verticalDeadZoneBelow)
            {
                _anchorY = y + _verticalDeadZoneBelow;
            }

            var falling = _fallLookAhead > 0f && _target.Velocity.y < -_fallLookAheadSpeed;
            _fallLead = Mathf.SmoothDamp(_fallLead, falling ? -_fallLookAhead : 0f,
                ref _fallLeadVelocity, _fallLookAheadSmoothTime, Mathf.Infinity, Time.deltaTime);
        }

        /// <summary>
        /// Hollow Knight's look up and down: standing still and holding up or down for
        /// <see cref="_lookDelay"/> seconds eases the view that way; letting go, moving or leaving
        /// the ground brings it back. The room's bounds still hold, so a floor at the bottom of the
        /// room stops a look down there.
        /// </summary>
        private void UpdateLook()
        {
            var vertical = _input != null ? _input.AimVertical : 0f;
            var standing = _target != null && _target.IsGrounded && Mathf.Abs(_target.Velocity.x) < 0.2f;
            var sign = vertical > 0.5f ? 1 : vertical < -0.5f ? -1 : 0;
            if (!standing || IsFree || sign == 0 || sign != _lookSign)
            {
                _lookHeld = 0f;
            }

            _lookSign = standing ? sign : 0;
            _lookHeld += Time.deltaTime;
            var wanted = LookOffset(_lookSign, _lookHeld, _lookDelay, _lookDistance);
            _look = Mathf.SmoothDamp(_look, wanted, ref _lookVelocity, _lookSmoothTime,
                Mathf.Infinity, Time.deltaTime);
        }

        /// <summary>
        /// Where the look wants the view, in units: nothing until the hold has lasted the delay,
        /// then the whole distance up (+) or down (-).
        /// </summary>
        public static float LookOffset(int direction, float heldFor, float delay, float distance) =>
            direction != 0 && heldFor >= delay ? Mathf.Sign(direction) * distance : 0f;

        /// <summary>
        /// Moves a view centre so the whole view stays inside the active bounds. A rectangle
        /// narrower than the view centres it instead, which is what holds a small arena still; one
        /// lower than the view keeps its floor at the bottom of the screen rather than showing as
        /// much empty space under it as over it. Between rooms it eases from the old room's
        /// framing to the new one's. Past the world's ends it never goes, whatever the room.
        /// </summary>
        private Vector2 Clamp(Vector2 center)
        {
            var bounds = ActiveBounds;
            if (!bounds.HasValue || _camera == null || IsFree)
            {
                return center;
            }

            var clamped = ClampTo(bounds.Value, center);
            if (IsChangingRooms)
            {
                var t = Mathf.SmoothStep(0f, 1f, (Time.time - _roomBlendStart) / _roomBlendTime);
                clamped = Vector2.Lerp(ClampTo(_previousRoomBounds.Value, center), clamped, t);
            }

            // A room narrower than the view would centre past the world's end; the world wins.
            return _worldBounds.HasValue ? ClampTo(_worldBounds.Value, clamped) : clamped;
        }

        /// <summary>
        /// The hard edge of a room, as in Hollow Knight: on each axis where the view already was
        /// inside the frame (or as good as, <see cref="_hardEdgePull"/>), the next position is kept
        /// inside it too. Only the target is clamped
        /// otherwise, so the smoothing lag of a widening zoom and a shake against a wall would show
        /// a strip past the room. An axis that was outside (a frame set at once, with no slide) is
        /// left to the smoothing, which brings it in without a cut; from then on it holds.
        /// </summary>
        private Vector2 HoldInside(Vector2 previous, Vector2 next)
        {
            if (!_hardRoomEdges || !ActiveBounds.HasValue || _camera == null || IsFree)
            {
                return next;
            }

            var held = Clamp(previous);
            var clamped = Clamp(next);
            if (Mathf.Abs(held.x - previous.x) <= _hardEdgePull)
            {
                next.x = clamped.x;
            }

            if (Mathf.Abs(held.y - previous.y) <= _hardEdgePull)
            {
                next.y = clamped.y;
            }

            return next;
        }

        private Vector2 ClampTo(Rect rect, Vector2 center)
        {
            var halfHeight = _camera.orthographicSize;
            var halfWidth = halfHeight * _camera.aspect;
            center.x = rect.width <= halfWidth * 2f
                ? rect.center.x
                : Mathf.Clamp(center.x, rect.xMin + halfWidth, rect.xMax - halfWidth);
            center.y = rect.height <= halfHeight * 2f
                ? rect.yMin + halfHeight
                : Mathf.Clamp(center.y, rect.yMin + halfHeight, rect.yMax - halfHeight);
            return center;
        }

        /// <summary>Eases the view to a size relative to its normal one (1 = normal).</summary>
        public void SetZoom(float relativeSize, float smoothTime)
        {
            _zoomTarget = _baseSize * Mathf.Clamp(relativeSize, 0.6f, 1.6f);
            _zoomSmoothTime = Mathf.Max(0.05f, smoothTime);
        }

        /// <summary>The developer's flight looks wider than any fight would: quickly, up to five times.</summary>
        public void SetFreeZoom(float relativeSize)
        {
            _zoomTarget = _baseSize * Mathf.Clamp(relativeSize, 0.6f, 5f);
            _zoomSmoothTime = 0.1f;
        }

        /// <summary>A very short push in: the parry's camera beat. Real time, so hit-stop cannot hold it.</summary>
        public void Punch(float amount, float duration)
        {
            _punchAmount = Mathf.Clamp(amount, 0f, 0.3f);
            _punchUntil = Time.unscaledTime + Mathf.Max(0.02f, duration);
        }

        private void UpdateZoom()
        {
            if (_camera == null)
            {
                return;
            }

            _zoom = Mathf.SmoothDamp(_zoom, _zoomTarget, ref _zoomVelocity, _zoomSmoothTime,
                Mathf.Infinity, Time.unscaledDeltaTime);
            var punch = Time.unscaledTime < _punchUntil ? _punchAmount : 0f;
            var size = _zoom * (1f - punch);
            if (!Mathf.Approximately(_camera.orthographicSize, size))
            {
                _camera.orthographicSize = size;
            }
        }
    }
}
