using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>Plain hit points. Shared by the player, enemies, bosses and destructibles.</summary>
    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float _maxHealth = 100f;
        private float _current;

        /// <summary>Raised with current and maximum health so UI never polls this component.</summary>
        public event Action<float, float> HealthChanged;
        public event Action Died;

        public float Max => _maxHealth;
        public float Current => _current;
        public float Normalized => _maxHealth > 0f ? _current / _maxHealth : 0f;
        public bool IsAlive => _current > 0f;

        private void Awake()
        {
            _current = _maxHealth;
        }

        private void OnEnable()
        {
            HealthChanged?.Invoke(_current, _maxHealth);
        }

        /// <summary>Returns the health actually removed, which is less than <paramref name="amount"/>
        /// when the hit is lethal.</summary>
        public float Remove(float amount)
        {
            if (amount <= 0f || !IsAlive)
            {
                return 0f;
            }

            var applied = Mathf.Min(amount, _current);
            _current -= applied;
            HealthChanged?.Invoke(_current, _maxHealth);
            if (!IsAlive)
            {
                Died?.Invoke();
            }
            return applied;
        }

        public float Heal(float amount)
        {
            if (amount <= 0f || !IsAlive)
            {
                return 0f;
            }

            var restored = Mathf.Min(amount, _maxHealth - _current);
            _current += restored;
            HealthChanged?.Invoke(_current, _maxHealth);
            return restored;
        }

        /// <summary>Used by respawn and by checkpoints; revives a dead actor as well.</summary>
        public void RestoreToFull()
        {
            _current = _maxHealth;
            HealthChanged?.Invoke(_current, _maxHealth);
        }

        /// <summary>Sets current health outright; used by authoring, tests and training dummies.</summary>
        public void SetCurrent(float health)
        {
            _current = Mathf.Clamp(health, 0f, _maxHealth);
            HealthChanged?.Invoke(_current, _maxHealth);
            if (!IsAlive)
            {
                Died?.Invoke();
            }
        }

        public void SetMax(float maxHealth, bool restore)
        {
            _maxHealth = Mathf.Max(1f, maxHealth);
            _current = restore ? _maxHealth : Mathf.Min(_current, _maxHealth);
            HealthChanged?.Invoke(_current, _maxHealth);
        }
    }
}
