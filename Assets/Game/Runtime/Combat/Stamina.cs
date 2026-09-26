using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Spent by dash, block and heavy attacks. Regeneration starts after a delay that every
    /// successful spend restarts.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Stamina : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float _maxStamina = 100f;
        [SerializeField, Min(0f)] private float _regenerationPerSecond = 35f;
        [SerializeField, Min(0f)] private float _regenerationDelay = 0.6f;
        [SerializeField, Min(0f)] private float _exhaustedRegenerationDelay = 1.2f;
        [Tooltip("Regeneration multiplier while a guard is raised; 0 stops it entirely.")]
        [SerializeField, Range(0f, 1f)] private float _guardingRegenerationScale = 0f;
        private float _current;
        private float _regenerationBlockedUntil;
        private float _regenerationBonus;
        private bool _isGuarding;

        public event Action<float, float> StaminaChanged;
        public event Action Exhausted;

        public float Max => _maxStamina;
        public float Current => _current;
        public float Normalized => _maxStamina > 0f ? _current / _maxStamina : 0f;

        private void Awake()
        {
            _current = _maxStamina;
        }

        private void OnEnable()
        {
            StaminaChanged?.Invoke(_current, _maxStamina);
        }

        /// <summary>
        /// Holding a guard slows or stops regeneration, so a raised shield cannot out-heal the
        /// stamina an attacker takes off it.
        /// </summary>
        public void SetGuarding(bool isGuarding)
        {
            _isGuarding = isGuarding;
        }

        private void Update()
        {
            var rate = _regenerationPerSecond * (1f + _regenerationBonus)
                * (_isGuarding ? _guardingRegenerationScale : 1f);
            if (rate <= 0f || _current >= _maxStamina || Time.time < _regenerationBlockedUntil)
            {
                return;
            }

            _current = Mathf.Min(_maxStamina, _current + rate * Time.deltaTime);
            StaminaChanged?.Invoke(_current, _maxStamina);
        }

        public bool Has(float cost)
        {
            return cost <= 0f || _current >= cost;
        }

        /// <summary>Spends the full cost or nothing, so an action never starts half paid.</summary>
        public bool TrySpend(float cost)
        {
            if (!Has(cost))
            {
                return false;
            }

            Spend(cost);
            return true;
        }

        /// <summary>Spends as much as is left; used by blocking, which may run the bar empty.</summary>
        public float Drain(float cost)
        {
            var spent = Mathf.Min(Mathf.Max(0f, cost), _current);
            Spend(spent);
            return spent;
        }

        /// <summary>Applied from progression; a larger bar comes from an upgrade, not from combat.</summary>
        public void SetMax(float maxStamina, bool restore)
        {
            _maxStamina = Mathf.Max(1f, maxStamina);
            _current = restore ? _maxStamina : Mathf.Min(_current, _maxStamina);
            StaminaChanged?.Invoke(_current, _maxStamina);
        }

        /// <summary>
        /// Brings the regeneration pause forward. A trained guard recovers sooner after a blocked
        /// hit; it can never bring the pause below now, so the bar does not refill during the hit.
        /// </summary>
        public void ShortenRegenerationDelay(float seconds)
        {
            if (seconds > 0f)
            {
                _regenerationBlockedUntil = Mathf.Max(Time.time, _regenerationBlockedUntil - seconds);
            }
        }

        /// <summary>Extra regeneration while a temporary bonus is running, such as adrenaline.</summary>
        public void SetRegenerationBonus(float bonus)
        {
            _regenerationBonus = Mathf.Max(-0.9f, bonus);
        }

        public void RestoreToFull()
        {
            _current = _maxStamina;
            _regenerationBlockedUntil = 0f;
            _regenerationBonus = 0f;
            _isGuarding = false;
            StaminaChanged?.Invoke(_current, _maxStamina);
        }

        private void Spend(float cost)
        {
            _current = Mathf.Max(0f, _current - cost);
            var isEmpty = _current <= 0f;
            _regenerationBlockedUntil = Time.time
                + (isEmpty ? _exhaustedRegenerationDelay : _regenerationDelay);
            StaminaChanged?.Invoke(_current, _maxStamina);
            if (isEmpty)
            {
                Exhausted?.Invoke();
            }
        }
    }
}
