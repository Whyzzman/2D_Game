using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The streak a blade leaves while it is actually swinging.
    ///
    /// It emits during the attack's real active window and nowhere else, which is what makes it
    /// information rather than decoration: the trail is on screen exactly while the hitbox is live.
    /// That also means it cannot linger into recovery and suggest the swing is still dangerous
    /// when it is not.
    ///
    /// Width comes from the equipped weapon, so a greatsword reads heavier than a sword without
    /// anyone writing a second trail.
    ///
    /// The streak is drawn by sweeping its point along the blade's arc over the active window —
    /// across the front for a side swing, overhead for a swing up, under the feet for a swing
    /// down — so each direction leaves its own crescent, the way Hollow Knight's slashes read.
    /// Before, the point sat still in the hand and the streak showed only when the player moved.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TrailRenderer))]
    public sealed class WeaponTrail : MonoBehaviour
    {
        [SerializeField] private TrailRenderer _trail;
        [SerializeField] private PlayerCombat _combat;
        [Tooltip("Width for a one-handed weapon, and for anything with no data of its own.")]
        [SerializeField, Min(0.01f)] private float _lightWidth = 0.16f;
        [Tooltip("Width for a two-hander; the whole reason a greatsword feels heavier.")]
        [SerializeField, Min(0.01f)] private float _heavyWidth = 0.34f;
        [SerializeField, Min(0.01f)] private float _lifetime = 0.14f;
        [Tooltip("Radius of the swing's arc for a one-handed weapon; a two-hander reaches further.")]
        [SerializeField, Min(0.1f)] private float _arcRadius = 1.35f;

        private Vector3 _restLocalPosition;
        private AttackDefinition _attack;
        private AttackDirection _direction;
        private float _speed = 1f;
        private float _reach = 1f;
        private float _startedAt;
        private IFacing _facing;

        private void Awake()
        {
            if (_trail == null) { _trail = GetComponent<TrailRenderer>(); }
            if (_combat == null) { _combat = GetComponentInParent<PlayerCombat>(); }

            _trail.emitting = false;
            _trail.time = _lifetime;
            _trail.autodestruct = false;
            _restLocalPosition = transform.localPosition;
            _facing = _combat != null ? _combat.GetComponentInChildren<IFacing>() : null;
        }

        private void OnEnable()
        {
            if (_combat != null)
            {
                _combat.AttackStarted += OnAttackStarted;
                _combat.AttackEnded += OnAttackEnded;
            }
        }

        private void OnDisable()
        {
            if (_combat != null)
            {
                _combat.AttackStarted -= OnAttackStarted;
                _combat.AttackEnded -= OnAttackEnded;
            }

            Stop();
        }

        private void OnAttackStarted(AttackDefinition attack)
        {
            var weapon = _combat != null ? _combat.Weapon : null;
            var isTwoHanded = weapon != null && weapon.WeaponType == WeaponType.Greatsword;
            var width = isTwoHanded ? _heavyWidth : _lightWidth;
            _trail.widthMultiplier = width;
            _trail.time = isTwoHanded ? _lifetime * 1.35f : _lifetime;
            _attack = attack;
            _direction = _combat != null ? _combat.CurrentDirection : AttackDirection.Side;
            _speed = weapon != null ? Mathf.Max(0.1f, weapon.AttackSpeed) : 1f;
            _reach = isTwoHanded ? 1.3f : 1f;
            _startedAt = Time.time;
            MoveAlongArc();
            _trail.Clear();
            _trail.emitting = true;
        }

        private void LateUpdate()
        {
            if (_attack != null)
            {
                MoveAlongArc();
            }
        }

        private void MoveAlongArc()
        {
            if (_combat == null || _attack == null)
            {
                return;
            }

            var elapsed = (Time.time - _startedAt) * _speed;
            var t = Mathf.Clamp01((elapsed - _attack.Windup) / Mathf.Max(0.01f, _attack.ActiveTime));
            var facing = _facing?.FacingDirection ?? 1;
            transform.position = (Vector2)_combat.transform.position
                + ArcPoint(_direction, t, facing, _arcRadius * _reach);
        }

        /// <summary>
        /// Where the blade is along its arc, from the player's centre, at progress 0..1 through the
        /// active window. Angles are measured from the facing direction, counter-clockwise.
        /// </summary>
        public static Vector2 ArcPoint(AttackDirection direction, float t, int facing, float radius)
        {
            float from, to, pivotY;
            switch (direction)
            {
                case AttackDirection.Up:
                    // In front, straight over the head at the middle, down behind.
                    from = 0f; to = 180f; pivotY = 0.4f;
                    break;
                case AttackDirection.Down:
                    // In front, straight under the feet at the middle, up behind.
                    from = 0f; to = -180f; pivotY = 0.1f;
                    break;
                default:
                    // Overhead and behind, across the front, down by the feet.
                    from = 115f; to = -35f; pivotY = 0.2f;
                    break;
            }

            var angle = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t)) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angle) * radius * facing, pivotY + Mathf.Sin(angle) * radius);
        }

        private void OnAttackEnded(AttackDefinition attack) => Stop();

        private void Stop()
        {
            if (_trail == null)
            {
                return;
            }

            _trail.emitting = false;
            // Cleared rather than left to fade: a streak still hanging in the air after the swing
            // reads as a swing that is still happening.
            _trail.Clear();
            _attack = null;
            transform.localPosition = _restLocalPosition;
        }

        public bool IsEmitting => _trail != null && _trail.emitting;
    }
}
