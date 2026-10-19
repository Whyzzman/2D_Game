using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Mana from the blade, as Hollow Knight's soul comes from the nail (the user's choice,
    /// 2026-09-28): mana no longer returns on its own, and every weapon swing that lands on an
    /// enemy, a boss or the training dummy pays some back. A heavy swing pays more, a swing into
    /// a shield half. Spells and active skills pay nothing — magic has to be earned by fighting.
    ///
    /// Each target a swing strikes pays once, as each enemy the nail hits does.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponManaGain : MonoBehaviour
    {
        /// <summary>Share of the gain a swing into a shield still pays.</summary>
        public const float BlockedShare = 0.5f;

        [SerializeField] private PlayerCombat _combat;
        [SerializeField] private Mana _mana;
        [Tooltip("Mana a landed light, up or down swing pays: two swings buy the cheapest spell.")]
        [SerializeField, Min(0f)] private float _perHit = 10f;
        [SerializeField, Min(0f)] private float _perHeavyHit = 15f;

        public float PerHit => _perHit;
        public float PerHeavyHit => _perHeavyHit;

        /// <summary>
        /// A swing paid mana: where it came from in the world and how much. The HUD draws the
        /// payment travelling to the bar (GAME_FEEL_IDEAS item 5); nothing here waits for it.
        /// </summary>
        public event Action<Vector2, float> ManaPaid;

        private void Awake()
        {
            if (_combat == null) { _combat = GetComponent<PlayerCombat>(); }
            if (_mana == null) { _mana = GetComponent<Mana>(); }
        }

        private void OnEnable()
        {
            if (_combat != null) { _combat.HitLanded += OnHitLanded; }
        }

        private void OnDisable()
        {
            if (_combat != null) { _combat.HitLanded -= OnHitLanded; }
        }

        private void OnHitLanded(CombatActor target, DamageResult result)
        {
            // Active skills swing through the same hitbox; only the weapon's own swing counts.
            var attack = _combat.CurrentAttack;
            if (_mana == null || attack == null || !_combat.IsAttacking || target == null)
            {
                return;
            }

            // An urn is not a fight: breaking things pays coins, never mana, as in Hollow Knight.
            if (target.GetComponent<BreakableProp>() != null)
            {
                return;
            }

            var weapon = _combat.Weapon;
            var heavy = weapon != null && attack == weapon.HeavyAttack;
            var amount = Evaluate(result.Outcome, target.Faction, heavy ? _perHeavyHit : _perHit);
            if (amount > 0f)
            {
                _mana.Restore(amount);
                ManaPaid?.Invoke(Source(target), amount);
            }
        }

        /// <summary>The middle of what was struck, not its feet: the payment leaves the body.</summary>
        private static Vector2 Source(CombatActor target)
        {
            var body = target.GetComponent<Collider2D>();
            return body != null ? (Vector2)body.bounds.center : (Vector2)target.transform.position;
        }

        /// <summary>What one landed swing pays: nothing for a hit the target ignored or turned.</summary>
        public static float Evaluate(DamageOutcome outcome, Faction target, float gain)
        {
            if (target == Faction.Player)
            {
                return 0f;
            }

            switch (outcome)
            {
                case DamageOutcome.Hit:
                    return gain;
                case DamageOutcome.Blocked:
                    return gain * BlockedShare;
                default:
                    return 0f;
            }
        }
    }
}
