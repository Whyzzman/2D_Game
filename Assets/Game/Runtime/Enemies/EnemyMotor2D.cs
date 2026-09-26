using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Ground movement for walking enemies: horizontal drive, facing, and the probes the AI uses to
    /// avoid walking off ledges or into walls. Vertical motion is left to Rigidbody gravity.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class EnemyMotor2D : MonoBehaviour, IFacing
    {
        [SerializeField] private LayerMask _groundMask;
        [SerializeField] private Transform _visual;
        [SerializeField] private Vector2 _groundProbeOffset = new Vector2(0f, -0.9f);
        [SerializeField] private Vector2 _ledgeProbeOffset = new Vector2(0.45f, -0.85f);
        [SerializeField, Min(0.01f)] private float _ledgeProbeDistance = 0.6f;
        [SerializeField] private Vector2 _wallProbeOffset = new Vector2(0f, -0.35f);
        [SerializeField, Min(0.01f)] private float _wallProbeDistance = 0.55f;
        private Rigidbody2D _body;
        private StatusEffects _statuses;
        private Vector3 _visualScale = Vector3.one;
        private int _facing = 1;

        public int FacingDirection => _facing;
        public Vector2 Position => _body != null ? _body.position : (Vector2)transform.position;
        public bool IsGrounded { get; private set; }

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _statuses = GetComponent<StatusEffects>();
            _body.freezeRotation = true;
            if (_visual != null)
            {
                _visualScale = _visual.localScale;
            }

            if (_groundMask.value == 0)
            {
                Debug.LogError("EnemyMotor2D requires a Ground layer mask.", this);
                enabled = false;
            }
        }

        private void FixedUpdate()
        {
            IsGrounded = Physics2D.OverlapCircle(
                (Vector2)transform.position + _groundProbeOffset, 0.12f, _groundMask) != null;
        }

        public void Drive(float speed)
        {
            _body.linearVelocity = new Vector2(speed * _facing * (_statuses != null ? _statuses.MovementMultiplier : 1f), _body.linearVelocity.y);
        }

        public void Stop()
        {
            _body.linearVelocity = new Vector2(0f, _body.linearVelocity.y);
        }

        /// <summary>Sets the whole velocity at once, used by leaping attacks.</summary>
        public void Launch(Vector2 velocity)
        {
            _body.linearVelocity = velocity;
        }

        /// <summary>Moves the body without leaving an interpolation smear; used by enemy reset.</summary>
        public void Teleport(Vector2 position)
        {
            _body.linearVelocity = Vector2.zero;
            _body.angularVelocity = 0f;
            var interpolation = _body.interpolation;
            _body.interpolation = RigidbodyInterpolation2D.None;
            _body.position = position;
            transform.position = new Vector3(position.x, position.y, transform.position.z);
            _body.interpolation = interpolation;
        }

        /// <summary>Downward acceleration this body actually experiences, for leap planning.</summary>
        public float Gravity => Mathf.Abs(Physics2D.gravity.y) * Mathf.Max(0.01f, _body.gravityScale);

        public void Face(int direction)
        {
            if (direction == 0 || direction == _facing)
            {
                return;
            }

            _facing = direction > 0 ? 1 : -1;
            if (_visual != null)
            {
                _visual.localScale = new Vector3(
                    Mathf.Abs(_visualScale.x) * _facing, _visualScale.y, _visualScale.z);
            }
        }

        public void FaceTowards(float worldX)
        {
            var delta = worldX - transform.position.x;
            if (Mathf.Abs(delta) > 0.05f)
            {
                Face(delta > 0f ? 1 : -1);
            }
        }

        /// <summary>True when solid ground continues in front of the enemy.</summary>
        public bool HasGroundAhead() => HasGroundToward(_facing);

        public bool HasWallAhead() => HasWallToward(_facing);

        /// <summary>
        /// The same ledge probe, aimed either way. An enemy that keeps its distance walks backwards
        /// while still facing its target, and it has to be able to see the drop behind it — the
        /// probe that follows facing would happily march it off a cliff.
        /// </summary>
        public bool HasGroundToward(int direction)
        {
            var sign = direction >= 0 ? 1 : -1;
            var origin = (Vector2)transform.position
                + new Vector2(_ledgeProbeOffset.x * sign, _ledgeProbeOffset.y);
            return Physics2D.Raycast(origin, Vector2.down, _ledgeProbeDistance, _groundMask);
        }

        public bool HasWallToward(int direction)
        {
            var sign = direction >= 0 ? 1 : -1;
            var origin = (Vector2)transform.position + _wallProbeOffset;
            return Physics2D.Raycast(origin, new Vector2(sign, 0f), _wallProbeDistance, _groundMask);
        }

        /// <summary>
        /// Drives along a world direction while leaving facing alone, so a retreating archer keeps
        /// its bow pointed at the player.
        /// </summary>
        public void DriveToward(int direction, float speed)
        {
            var sign = direction >= 0 ? 1 : -1;
            var scale = _statuses != null ? _statuses.MovementMultiplier : 1f;
            _body.linearVelocity = new Vector2(speed * sign * scale, _body.linearVelocity.y);
        }

        /// <summary>Straight line of sight to a point, blocked by ground geometry.</summary>
        public bool HasLineOfSight(Vector2 target)
        {
            var origin = (Vector2)transform.position;
            var delta = target - origin;
            return !Physics2D.Raycast(origin, delta.normalized, delta.magnitude, _groundMask);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            var ledge = (Vector2)transform.position
                + new Vector2(_ledgeProbeOffset.x * _facing, _ledgeProbeOffset.y);
            Gizmos.DrawLine(ledge, ledge + Vector2.down * _ledgeProbeDistance);
            var wall = (Vector2)transform.position + _wallProbeOffset;
            Gizmos.DrawLine(wall, wall + new Vector2(_facing * _wallProbeDistance, 0f));
        }
    }
}
