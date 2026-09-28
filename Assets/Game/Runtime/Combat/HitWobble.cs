using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// A struck thing on a post rocks away from the blow and settles: a damped spring on one
    /// transform's tilt, driven by <see cref="CombatActor.DamageTaken"/>. Presentation only — the
    /// collider never tilts, the body it belongs to is not moved.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HitWobble : MonoBehaviour
    {
        [SerializeField] private CombatActor _actor;
        [Tooltip("The transform that rocks; pivoted at the foot of the post.")]
        [SerializeField] private Transform _target;
        [SerializeField, Min(0f)] private float _kick = 9f;
        [SerializeField, Min(0.1f)] private float _stiffness = 90f;
        [SerializeField, Min(0f)] private float _damping = 7f;
        [SerializeField, Min(1f)] private float _maxAngle = 14f;

        private float _angle;
        private float _velocity;

        public float Angle => _angle;

        private void Awake()
        {
            if (_actor == null)
            {
                _actor = GetComponentInParent<CombatActor>();
            }
        }

        private void OnEnable()
        {
            if (_actor != null)
            {
                _actor.DamageTaken += OnDamageTaken;
            }
        }

        private void OnDisable()
        {
            if (_actor != null)
            {
                _actor.DamageTaken -= OnDamageTaken;
            }

            _angle = 0f;
            _velocity = 0f;
            Apply();
        }

        private void OnDamageTaken(DamageInfo info, DamageResult result)
        {
            // Away from the hit: pushed to the right, the top rocks right, which is a negative tilt.
            var push = info.HitDirection.x;
            if (Mathf.Abs(push) < 0.01f && info.Attacker != null)
            {
                push = transform.position.x - info.Attacker.transform.position.x;
            }

            _velocity += (push >= 0f ? -1f : 1f) * _kick * 10f;
        }

        private void Update()
        {
            var dt = Time.deltaTime;
            if (dt <= 0f)
            {
                return;
            }

            _velocity += (-_stiffness * _angle - _damping * _velocity) * dt;
            _angle = Mathf.Clamp(_angle + _velocity * dt, -_maxAngle, _maxAngle);
            Apply();
        }

        private void Apply()
        {
            if (_target != null)
            {
                _target.localRotation = Quaternion.Euler(0f, 0f, _angle);
            }
        }
    }
}
