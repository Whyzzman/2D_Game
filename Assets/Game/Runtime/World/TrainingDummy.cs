using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// A target that only takes hits: it shows a health bar, flashes when struck and refills itself
    /// so combat can be tested repeatedly. It never attacks.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatActor))]
    public sealed class TrainingDummy : MonoBehaviour
    {
        [SerializeField] private CombatActor _actor;
        [SerializeField] private SpriteRenderer _body;
        [SerializeField] private Transform _healthFill;
        [SerializeField] private Color _hitFlash = new Color(1f, 0.85f, 0.7f);
        [SerializeField, Min(0f)] private float _flashTime = 0.09f;
        [Tooltip("Seconds before a destroyed dummy stands back up at full health.")]
        [SerializeField, Min(0f)] private float _refillDelay = 2.5f;
        private Color _baseColor = Color.white;
        private Vector3 _fillScale = Vector3.one;
        private float _flashUntil;
        private float _refillAt = -1f;

        public CombatActor Actor => _actor;

        /// <summary>Totals since the scene started, read by the debug overlay.</summary>
        public float TotalDamageTaken { get; private set; }
        public int HitCount { get; private set; }

        private void Awake()
        {
            if (_actor == null)
            {
                _actor = GetComponent<CombatActor>();
            }

            if (_body != null)
            {
                _baseColor = _body.color;
            }

            if (_healthFill != null)
            {
                _fillScale = _healthFill.localScale;
            }
        }

        private void OnEnable()
        {
            _actor.DamageTaken += OnDamageTaken;
            _actor.Health.HealthChanged += OnHealthChanged;
            OnHealthChanged(_actor.Health.Current, _actor.Health.Max);
        }

        private void OnDisable()
        {
            _actor.DamageTaken -= OnDamageTaken;
            _actor.Health.HealthChanged -= OnHealthChanged;
        }

        private void Update()
        {
            if (_flashUntil > 0f && Time.time >= _flashUntil)
            {
                _flashUntil = 0f;
                if (_body != null)
                {
                    _body.color = _baseColor;
                }
            }

            if (_refillAt > 0f && Time.time >= _refillAt)
            {
                _refillAt = -1f;
                _actor.ClearInvulnerability();
                _actor.Health.RestoreToFull();
            }
        }

        private void OnDamageTaken(DamageInfo info, DamageResult result)
        {
            if (result.Outcome == DamageOutcome.Ignored)
            {
                return;
            }

            HitCount++;
            TotalDamageTaken += result.AppliedDamage;
            if (_body != null)
            {
                _body.color = _hitFlash;
                _flashUntil = Time.time + _flashTime;
            }

            if (result.IsLethal)
            {
                _refillAt = Time.time + _refillDelay;
            }
        }

        private void OnHealthChanged(float current, float max)
        {
            if (_healthFill == null)
            {
                return;
            }

            var normalized = max > 0f ? current / max : 0f;
            _healthFill.localScale = new Vector3(_fillScale.x * normalized, _fillScale.y, _fillScale.z);
        }
    }
}
