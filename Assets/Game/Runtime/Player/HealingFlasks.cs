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
    ///
    /// A healing item from the bag (<see cref="TryBeginItem"/>) is taken the same way — the same
    /// pose, the same commitment and the same spill — only it spends the item, not a charge.
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
        private int _bonusCharges;
        private float _amount;
        private float _elapsed;
        private bool _hasHealed;
        private Health _health;

        /// <summary>Charges left; the HUD shows these.</summary>
        public event Action<int, int> ChargesChanged;
        public event Action DrinkStarted;
        public event Action<float> Healed;
        /// <summary>A hit landed before the heal point and the drink was lost.</summary>
        public event Action Spilled;

        public int MaxCharges => _maxCharges + _bonusCharges;
        public int Charges => _charges < 0 ? MaxCharges : _charges;

        /// <summary>
        /// Flasks added by shards from the merchant (item 70). A new flask arrives full; losing
        /// one (a reload of an older save) takes the spare charge with it.
        /// </summary>
        public void SetBonusCharges(int bonus)
        {
            bonus = Mathf.Max(0, bonus);
            if (bonus == _bonusCharges)
            {
                return;
            }

            var gained = bonus - _bonusCharges;
            _bonusCharges = bonus;
            _charges = Mathf.Clamp(Charges + Mathf.Max(0, gained), 0, MaxCharges);
            ChargesChanged?.Invoke(_charges, MaxCharges);
        }
        public float HealAmount => _healAmount;
        public float DrinkTime => _drinkTime;
        public bool IsDrinking { get; private set; }

        /// <summary>While drinking: a flask rather than an item from the bag.</summary>
        public bool IsDrinkingFlask { get; private set; }

        /// <summary>Whether a drink could start now: not already drinking, alive and hurt.</summary>
        public bool CanHeal => !IsDrinking && _health != null && _health.IsAlive
            && _health.Current < _health.Max;

        public void Bind(Health health)
        {
            _health = health;
            if (_charges < 0)
            {
                _charges = MaxCharges;
            }
        }

        /// <summary>
        /// Starts a drink: needs a charge and something to heal. A full-health press does nothing
        /// rather than wasting a flask.
        /// </summary>
        public bool TryBegin()
        {
            if (Charges <= 0 || !CanHeal)
            {
                return false;
            }

            _charges = Charges - 1;
            Begin(_healAmount, true);
            ChargesChanged?.Invoke(_charges, MaxCharges);
            return true;
        }

        /// <summary>Starts eating or drinking a healing item; the caller spends the item.</summary>
        public bool TryBeginItem(float amount)
        {
            if (amount <= 0f || !CanHeal)
            {
                return false;
            }

            Begin(amount, false);
            return true;
        }

        private void Begin(float amount, bool flask)
        {
            _amount = amount;
            _elapsed = 0f;
            _hasHealed = false;
            IsDrinking = true;
            IsDrinkingFlask = flask;
            DrinkStarted?.Invoke();
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
                var healed = _health != null ? _health.Heal(_amount) : 0f;
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
            if (_charges == MaxCharges)
            {
                return;
            }

            _charges = MaxCharges;
            ChargesChanged?.Invoke(_charges, MaxCharges);
        }
    }
}
