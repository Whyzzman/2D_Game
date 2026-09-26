using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Whether the player is currently in a fight. Measured from observable combat activity —
    /// blows given and taken — and it lapses after a quiet spell. Resting at an altar checks it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerCombatStatus : MonoBehaviour
    {
        [SerializeField] private CombatActor _actor;
        [SerializeField] private PlayerCombat _combat;
        [Tooltip("Quiet time after the last blow before the fight counts as over.")]
        [SerializeField, Min(0f)] private float _combatTimeout = 4f;
        private float _lastCombatAt = float.NegativeInfinity;

        public event Action<bool> CombatStateChanged;

        public bool IsInCombat => Time.time - _lastCombatAt <= _combatTimeout;
        public float TimeSinceCombat => Time.time - _lastCombatAt;

        private void Awake()
        {
            if (_actor == null) { _actor = GetComponent<CombatActor>(); }
            if (_combat == null) { _combat = GetComponent<PlayerCombat>(); }
        }

        private void OnEnable()
        {
            if (_actor != null)
            {
                _actor.DamageTaken += OnDamageTaken;
                _actor.AttackDodged += OnDodged;
            }
            if (_combat != null)
            {
                _combat.HitLanded += OnHitLanded;
            }
        }

        private void OnDisable()
        {
            if (_actor != null)
            {
                _actor.DamageTaken -= OnDamageTaken;
                _actor.AttackDodged -= OnDodged;
            }
            if (_combat != null)
            {
                _combat.HitLanded -= OnHitLanded;
            }
        }

        /// <summary>Marks combat activity right now.</summary>
        public void Report()
        {
            var wasInCombat = IsInCombat;
            _lastCombatAt = Time.time;
            if (!wasInCombat)
            {
                CombatStateChanged?.Invoke(true);
            }
        }

        /// <summary>Ends the fight outright; a respawn and a rest both do this.</summary>
        public void Clear()
        {
            var wasInCombat = IsInCombat;
            _lastCombatAt = float.NegativeInfinity;
            if (wasInCombat)
            {
                CombatStateChanged?.Invoke(false);
            }
        }

        private void OnDamageTaken(DamageInfo info, DamageResult result)
        {
            Report();
        }

        private void OnDodged(DamageInfo info)
        {
            Report();
        }

        private void OnHitLanded(CombatActor target, DamageResult result)
        {
            Report();
        }
    }
}
