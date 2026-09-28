using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The weight of a blow in the body that takes it (visual item 4, the user's choice of
    /// 2026-09-28): a flat squash and a tilt away from the hit, springing back in a fifth of a
    /// second, over whatever clip is playing — even mid-swing, where no drawn hurt frame can go.
    ///
    /// Drives the body sprite's own transform, pivoted at the feet, so the figure squats into the
    /// floor rather than off it. Presentation only: colliders, hitboxes and the motor never see
    /// it. A blocked blow is the guard's own clip, so it does not squash.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HitRecoil : MonoBehaviour
    {
        [SerializeField] private CombatActor _actor;
        [Tooltip("The body sprite's transform: pivoted at the feet, unscaled at rest.")]
        [SerializeField] private Transform _body;
        [Tooltip("Share of height lost at the moment of impact; width grows to match.")]
        [SerializeField, Range(0f, 0.4f)] private float _squash = 0.14f;
        [Tooltip("Degrees the body tilts away from the blow.")]
        [SerializeField, Range(0f, 25f)] private float _tilt = 9f;
        [SerializeField, Min(1f)] private float _stiffness = 420f;
        [SerializeField, Range(0.05f, 1f)] private float _damping = 0.45f;

        // One spring for the blow, 1 at impact; the squash and the tilt are shares of it.
        private float _amount;
        private float _velocity;
        private float _direction = 1f;

        public float Amount => _amount;

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

            _amount = 0f;
            _velocity = 0f;
            Apply();
        }

        private void OnDamageTaken(DamageInfo info, DamageResult result)
        {
            if (result.Outcome != DamageOutcome.Hit)
            {
                return;
            }

            var push = info.HitDirection.x;
            if (Mathf.Abs(push) < 0.01f && info.Attacker != null)
            {
                push = transform.position.x - info.Attacker.transform.position.x;
            }

            _direction = push >= 0f ? 1f : -1f;
            _amount = 1f;
            _velocity = 0f;
            Apply();
        }

        private void LateUpdate()
        {
            var dt = Time.deltaTime;
            if (dt <= 0f || (_amount == 0f && _velocity == 0f))
            {
                return;
            }

            var omega = Mathf.Sqrt(_stiffness);
            _velocity += (-_stiffness * _amount - 2f * _damping * omega * _velocity) * dt;
            _amount += _velocity * dt;
            if (Mathf.Abs(_amount) < 0.002f && Mathf.Abs(_velocity) < 0.02f)
            {
                _amount = 0f;
                _velocity = 0f;
            }

            Apply();
        }

        private void Apply()
        {
            if (_body == null)
            {
                return;
            }

            var squash = _squash * _amount;
            _body.localScale = new Vector3(1f + squash * 0.7f, 1f - squash, 1f);
            // Pushed to the right, the top leans right: a negative tilt in world terms. The visual
            // may be mirrored, so the lean is taken back through the parent's facing.
            var facing = _body.parent != null && _body.parent.lossyScale.x < 0f ? -1f : 1f;
            _body.localRotation = Quaternion.Euler(0f, 0f, -_tilt * _amount * _direction * facing);
        }
    }
}
