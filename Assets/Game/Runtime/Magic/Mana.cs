using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The resource spells are paid for. Deliberately a separate bar from
    /// <see cref="Stamina"/>: stamina is the body and mana is the mind, and a build that spends
    /// one should still have the other. Current mana is never saved — a load, a respawn and a
    /// rest all start the player full.
    ///
    /// It does not come back on its own (the user's choice, 2026-09-28, after Hollow Knight's
    /// soul): striking enemies with the weapon refills it (<see cref="WeaponManaGain"/>), so
    /// magic is paid for by fighting, not by waiting.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Mana : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float _maxMana = 100f;
        private float _current;

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
            ManaChanged?.Invoke(_current, _maxMana);
        }
    }
}
