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
        private Vector2 _smoothingVelocity;
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

        /// <summary>Where the camera is heading, before shake; tests measure framing against this.</summary>
        public Vector2 TargetPosition => _target != null
            ? (Vector2)_target.transform.position + _framingOffset + new Vector2(_lead, 0f)
            : (Vector2)transform.position;

        public Vector2 FramingOffset => _framingOffset;

        private void Awake()
        {
            _depth = transform.position.z;
            _camera = GetComponent<Camera>();
            _baseSize = _camera.orthographicSize;
            _zoomTarget = _baseSize;
            _zoom = _baseSize;
            if (_shaker == null) { _shaker = GetComponent<CameraShaker>(); }
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
            var basePosition = (Vector2)transform.position
                - (_shaker != null ? _shaker.CurrentOffset : Vector2.zero);
            var position = Vector2.SmoothDamp(basePosition, TargetPosition,
                ref _smoothingVelocity, _smoothTime, Mathf.Infinity, Time.deltaTime);
            var shake = _shaker != null ? _shaker.CurrentOffset : Vector2.zero;
            transform.position = new Vector3(position.x + shake.x, position.y + shake.y, _depth);

            UpdateZoom();
        }

        public void SnapToTarget()
        {
            _smoothingVelocity = Vector2.zero;
            _lead = 0f;
            _leadVelocity = 0f;
            if (_target != null)
            {
                var position = TargetPosition;
                transform.position = new Vector3(position.x, position.y, _depth);
            }
        }

        /// <summary>Eases the view to a size relative to its normal one (1 = normal).</summary>
        public void SetZoom(float relativeSize, float smoothTime)
        {
            _zoomTarget = _baseSize * Mathf.Clamp(relativeSize, 0.6f, 1.6f);
            _zoomSmoothTime = Mathf.Max(0.05f, smoothTime);
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
