using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>What a weapon is worth in a particular pair of hands, broken into its parts.</summary>
    public readonly struct WeaponDamageBreakdown
    {
        public readonly float WeaponBase;
        public readonly float StrengthBonus;
        public readonly float DexterityBonus;
        public readonly float FlatAttackPower;
        /// <summary>1 when the requirements are met, the penalty share when they are not.</summary>
        public readonly float RequirementFactor;
        public readonly bool MeetsRequirements;

        public WeaponDamageBreakdown(float weaponBase, float strengthBonus, float dexterityBonus,
            float flatAttackPower, float requirementFactor, bool meetsRequirements)
        {
            WeaponBase = weaponBase;
            StrengthBonus = strengthBonus;
            DexterityBonus = dexterityBonus;
            FlatAttackPower = flatAttackPower;
            RequirementFactor = requirementFactor;
            MeetsRequirements = meetsRequirements;
        }

        /// <summary>The number every swing is built from.</summary>
        public float ScaledBase =>
            (WeaponBase + StrengthBonus + DexterityBonus + FlatAttackPower) * RequirementFactor;

        public override string ToString()
        {
            return $"base {WeaponBase:0.#} + str {StrengthBonus:0.#} + dex {DexterityBonus:0.#}"
                + $" + flat {FlatAttackPower:0.#}"
                + (MeetsRequirements ? string.Empty : $" x{RequirementFactor:0.##} (requirements)")
                + $" = {ScaledBase:0.#}";
        }
    }

    /// <summary>
    /// The one place a weapon's attack power is worked out. Combat, the inventory screen and the
    /// debug overlay all ask this, so the number a player reads is the number they deal.
    /// </summary>
    public static class WeaponDamageCalculator
    {
        /// <summary>
        /// Attributes raise a weapon by a share of its own base, weighted by that weapon's
        /// scaling coefficients: a greatsword with strong strength scaling gains far more from
        /// the same point than a sword does, which is what makes a build mean anything.
        /// </summary>
        public static WeaponDamageBreakdown Evaluate(WeaponDefinition weapon, ProgressionCurve curve,
            int strength, int dexterity, float flatAttackPower = 0f, bool meetsRequirements = true)
        {
            if (weapon == null)
            {
                return new WeaponDamageBreakdown(0f, 0f, 0f, 0f, 1f, true);
            }

            var scale = curve != null ? curve.ScaleFactor : 0f;
            var strengthBonus = weapon.BaseDamage * weapon.StrengthScaling
                * Mathf.Max(0, strength) * scale;
            var dexterityBonus = weapon.BaseDamage * weapon.DexterityScaling
                * Mathf.Max(0, dexterity) * scale;
            var penalty = meetsRequirements || curve == null ? 1f : curve.RequirementPenalty;
            return new WeaponDamageBreakdown(weapon.BaseDamage, strengthBonus, dexterityBonus,
                flatAttackPower, penalty, meetsRequirements);
        }

        /// <summary>Damage one particular swing lands, before the target's armour.</summary>
        public static float AttackDamage(in WeaponDamageBreakdown breakdown, AttackDefinition attack)
        {
            return attack != null ? breakdown.ScaledBase * attack.DamageMultiplier : 0f;
        }
    }
}
