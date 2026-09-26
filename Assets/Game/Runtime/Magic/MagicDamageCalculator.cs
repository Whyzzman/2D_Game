using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>What a spell is worth in a particular caster's hands, broken into its parts.</summary>
    public readonly struct SpellDamageBreakdown
    {
        public readonly float SpellBase;
        public readonly float IntelligenceBonus;
        /// <summary>Everything the skill tree adds, as one multiplier.</summary>
        public readonly float SkillMultiplier;

        public SpellDamageBreakdown(float spellBase, float intelligenceBonus,
            float skillMultiplier)
        {
            SpellBase = spellBase;
            IntelligenceBonus = intelligenceBonus;
            SkillMultiplier = skillMultiplier;
        }

        public float Total => (SpellBase + IntelligenceBonus) * SkillMultiplier;

        public override string ToString()
        {
            return $"base {SpellBase:0.#} + int {IntelligenceBonus:0.#}"
                + $" x{SkillMultiplier:0.##} = {Total:0.#}";
        }
    }

    /// <summary>
    /// The one place a spell's damage is worked out. Casting, the spellbook panel and the debug
    /// overlay all ask this, so the number a player reads is the number they deal — the same
    /// arrangement weapons have with <see cref="WeaponDamageCalculator"/>.
    /// </summary>
    public static class MagicDamageCalculator
    {
        /// <summary>
        /// Intelligence raises a spell by a share of its own base, weighted by that spell's
        /// scaling: Arcane Burst gains more from the same point than Frost Shard does.
        /// </summary>
        public static SpellDamageBreakdown Evaluate(SpellDefinition spell, ProgressionCurve curve,
            int intelligence, float skillMultiplier = 1f)
        {
            if (spell == null)
            {
                return new SpellDamageBreakdown(0f, 0f, 1f);
            }

            var scale = curve != null ? curve.SpellScaleFactor : 0f;
            var bonus = spell.BaseDamage * spell.IntelligenceScaling
                * Mathf.Max(0, intelligence) * scale;
            return new SpellDamageBreakdown(spell.BaseDamage, bonus,
                Mathf.Max(0f, skillMultiplier));
        }

        /// <summary>
        /// Turns a spell into a hit. Everything a receiver needs is in the result, including the
        /// element and the tags, so resistance and future passives work without special cases.
        /// </summary>
        public static DamageInfo BuildDamage(SpellDefinition spell, float damage,
            GameObject caster, Object source, Vector2 direction, bool isCritical = false)
        {
            if (spell == null)
            {
                return new DamageInfo(0f, direction);
            }

            return new DamageInfo(damage, direction, spell.Element, caster, source, default,
                spell.Knockback, spell.StaggerDamage, spell.HitStop, isCritical,
                !spell.IsBlockable, spell.GuardDamage, false, spell.Tags, spell.StatusBuildup);
        }
    }
}
