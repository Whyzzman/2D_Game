using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Healing flasks, in the manner of Dark Souls' Estus: a few charges, each drunk standing
    /// still, refilled at an altar and on respawn. Chosen by the user on 2026-09-26 (item 35);
    /// before this the game had no healing at all and only resting restored health.
    ///
    /// A drink commits: the charge is spent as it starts, the health arrives at the heal point
    /// part-way through, and a real hit before that point spills it — the charge is gone and
    /// nothing is healed. After the heal point a hit only cuts the rest of the animation short.
    /// All numbers are starting values for tuning.
    ///
    /// A plain class owned by <see cref="PlayerCharacter"/>, so the player prefab needs no new
    /// component and PlayerCharacter does not grow by the whole mechanic.
    /// </summary>
    [Serializable]
    public sealed class HealingFlasks
    {
        [SerializeField, Min(0)] private int _maxCharges = 3;
        [SerializeField, Min(0f)] private float _healAmount = 40f;
        [Tooltip("How long a drink holds the player still.")]
        [SerializeField, Min(0.05f)] private float _drinkTime = 0.8f;
        [Tooltip("Seconds into the drink when the health arrives; a hit before it spills the charge.")]
        [SerializeField, Min(0f)] private float _healPoint = 0.5f;

        private int _charges = -1;
        private float _elapsed;
        private bool _hasHealed;
        private Health _health;

        /// <summary>Charges left; the HUD shows these.</summary>
        public event Action<int, int> ChargesChanged;
        public event Action DrinkStarted;
        public event Action<float> Healed;
        /// <summary>A hit landed before the heal point and the drink was lost.</summary>
        public event Action Spilled;

        public int MaxCharges => _maxCharges;
        public int Charges => _charges < 0 ? _maxCharges : _charges;
        public float HealAmount => _healAmount;
        public float DrinkTime => _drinkTime;
        public bool IsDrinking { get; private set; }

        public void Bind(Health health)
        {
            _health = health;
            if (_charges < 0)
            {
                _charges = _maxCharges;
            }
        }

        /// <summary>
        /// Starts a drink: needs a charge and something to heal. A full-health press does nothing
        /// rather than wasting a flask.
        /// </summary>
        public bool TryBegin()
        {
            if (IsDrinking || Charges <= 0 || _health == null || !_health.IsAlive
                || _health.Current >= _health.Max)
            {
                return false;
            }

            _charges = Charges - 1;
            _elapsed = 0f;
            _hasHealed = false;
            IsDrinking = true;
            DrinkStarted?.Invoke();
            ChargesChanged?.Invoke(_charges, _maxCharges);
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (!IsDrinking)
            {
                return;
            }

            _elapsed += deltaTime;
            if (!_hasHealed && _elapsed >= _healPoint)
            {
                _hasHealed = true;
                var healed = _health != null ? _health.Heal(_healAmount) : 0f;
                Healed?.Invoke(healed);
            }

            if (_elapsed >= _drinkTime)
            {
                IsDrinking = false;
            }
        }

        /// <summary>A real hit: before the heal point the drink is spilled, after it merely ends.</summary>
        public void Interrupt()
        {
            if (!IsDrinking)
            {
                return;
            }

            IsDrinking = false;
            if (!_hasHealed)
            {
                Spilled?.Invoke();
            }
        }

        /// <summary>Resting and respawning fill every flask again.</summary>
        public void Refill()
        {
            IsDrinking = false;
            if (_charges == _maxCharges)
            {
                return;
            }

            _charges = _maxCharges;
            ChargesChanged?.Invoke(_charges, _maxCharges);
        }
    }
}
