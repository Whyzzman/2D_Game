using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The hero leans into a speed change on the ground (hero animation 2.0, 2026-09-28): forward
    /// while the run picks up, back while it brakes, then upright again on a spring. The motor
    /// keeps Hollow Knight's near-instant start and stop — this only tilts the drawn body about
    /// its feet and never touches physics.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HeroLean : MonoBehaviour
    {
        /// <summary>The fullest lean, in degrees, at the motor's full ground acceleration.</summary>
        public const float MaxLean = 4f;

        /// <summary>A speed change this fast (share of the run speed per second) leans all the way.</summary>
        private const float FullLeanRate = 6f;

        [SerializeField] private PlayerMotor2D _motor;
        [SerializeField] private PlayerAnimationController _animation;
        [Tooltip("The drawn body, pivoted at the feet.")]
        [SerializeField] private Transform _body;
        [SerializeField, Min(0f)] private float _maxLean = MaxLean;
        [SerializeField, Min(0.1f)] private float _stiffness = 260f;
        [SerializeField, Min(0f)] private float _damping = 20f;

        private float _angle;
        private float _velocity;
        private float _target;
        private float _previousSpeed;
        private int _previousFacing;

        /// <summary>Current lean in degrees: positive is back, negative forward (drawn facing right).</summary>
        public float Angle => _angle;

        private void Awake()
        {
            if (_motor == null) { _motor = GetComponent<PlayerMotor2D>(); }
            if (_animation == null) { _animation = GetComponent<PlayerAnimationController>(); }
            if (_body == null && _animation != null && _animation.Body != null)
            {
                _body = _animation.Body.transform;
            }
        }

        private void OnDisable()
        {
            _angle = 0f;
            _velocity = 0f;
            _target = 0f;
            Apply();
        }

        private void FixedUpdate()
        {
            if (_motor == null)
            {
                return;
            }

            var facing = _motor.FacingDirection;
            var speed = _motor.Velocity.x * facing;
            var rate = (speed - _previousSpeed) / Time.fixedDeltaTime / Mathf.Max(0.1f, _motor.RunSpeed);
            // A turn flips the facing and with it the sign of the speed; the Turn frames dress that.
            var turned = facing != _previousFacing;
            _previousFacing = facing;
            _previousSpeed = speed;
            if (turned || !Leans())
            {
                _target = 0f;
                return;
            }

            // Speeding up tips the top forward (a negative tilt in the right-facing drawing).
            _target = -Mathf.Clamp(rate / FullLeanRate, -1f, 1f) * _maxLean;
        }

        private bool Leans()
        {
            if (FeedbackSettings.ReducedMotion || !_motor.IsGrounded || _motor.IsDashing)
            {
                return false;
            }

            var state = _animation != null ? _animation.State : PlayerAnimationState.Run;
            return state == PlayerAnimationState.Run || state == PlayerAnimationState.Idle;
        }

        private void LateUpdate()
        {
            var dt = Time.deltaTime;
            if (dt <= 0f)
            {
                return;
            }

            _velocity += (_stiffness * (_target - _angle) - _damping * _velocity) * dt;
            _angle = Mathf.Clamp(_angle + _velocity * dt, -_maxLean * 1.5f, _maxLean * 1.5f);
            Apply();
        }

        private void Apply()
        {
            if (_body != null)
            {
                _body.localRotation = Quaternion.Euler(0f, 0f, _angle);
            }
        }
    }
}
