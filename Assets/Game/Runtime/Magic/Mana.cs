using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The resource spells are paid for. Deliberately a separate bar from
    /// <see cref="Stamina"/>: stamina is the body and mana is the mind, and a build that spends
    /// one should still have the other. Current mana is never saved — a load, a respawn and a
    /// rest all start the player full.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Mana : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float _maxMana = 100f;
        [SerializeField, Min(0f)] private float _regenerationPerSecond = 14f;
        [Tooltip("Pause after a spell before mana starts coming back.")]
        [SerializeField, Min(0f)] private float _regenerationDelay = 2f;
        private float _current;
        private float _regenerationBlockedUntil;
        private bool _isCasting;

        public event Action<float, float> ManaChanged;

        /// <summary>Raised when a spell was refused for want of mana; the bar flashes on this.</summary>
        public event Action Insufficient;

        public float Max => _maxMana;
        public float Current => _current;
        public float Normalized => _maxMana > 0f ? _current / _maxMana : 0f;

        private void Awake()
        {
            _current = _maxMana;
        }

        private void OnEnable()
        {
            ManaChanged?.Invoke(_current, _maxMana);
        }

        private void Update()
        {
            // Mana does not gather while a spell is being shaped, and not until the pause after
            // the last one has passed.
            if (_isCasting || _regenerationPerSecond <= 0f || _current >= _maxMana
                || Time.time < _regenerationBlockedUntil)
            {
                return;
            }

            _current = Mathf.Min(_maxMana, _current + _regenerationPerSecond * Time.deltaTime);
            ManaChanged?.Invoke(_current, _maxMana);
        }

        public bool Has(float cost)
        {
            return cost <= 0f || _current >= cost;
        }

        /// <summary>
        /// Spends the whole cost or nothing, and says so when it could not — a spell is never
        /// half paid for, and the caller learns why without inspecting the bar.
        /// </summary>
        public bool TrySpend(float cost)
        {
            if (!Has(cost))
            {
                Insufficient?.Invoke();
                return false;
            }

            _current = Mathf.Max(0f, _current - Mathf.Max(0f, cost));
            _regenerationBlockedUntil = Time.time + _regenerationDelay;
            ManaChanged?.Invoke(_current, _maxMana);
            return true;
        }

        public float Restore(float amount)
        {
            if (amount <= 0f)
            {
                return 0f;
            }

            var restored = Mathf.Min(amount, _maxMana - _current);
            _current += restored;
            ManaChanged?.Invoke(_current, _maxMana);
            return restored;
        }

        /// <summary>Told while a spell is being cast, so the bar does not refill mid-cast.</summary>
        public void SetCasting(bool isCasting)
        {
            _isCasting = isCasting;
        }

        /// <summary>Applied from derived stats; a larger pool comes from intelligence.</summary>
        public void SetMax(float maxMana, bool restore)
        {
            _maxMana = Mathf.Max(1f, maxMana);
            _current = restore ? _maxMana : Mathf.Min(_current, _maxMana);
            ManaChanged?.Invoke(_current, _maxMana);
        }

        public void RestoreToFull()
        {
            _current = _maxMana;
            _regenerationBlockedUntil = 0f;
            _isCasting = false;
            ManaChanged?.Invoke(_current, _maxMana);
        }
    }
}
