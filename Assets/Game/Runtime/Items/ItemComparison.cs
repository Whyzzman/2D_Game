using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>Which way a number moved, judged for the player rather than by its sign.</summary>
    public enum ComparisonTrend
    {
        None,
        Better,
        Worse,
        Same
    }

    /// <summary>One line of the item panel: "Шкода  34 → 52  ▲ +18".</summary>
    public readonly struct ComparisonRow
    {
        public readonly string Label;
        /// <summary>The worn item's value; empty when there is nothing to compare against.</summary>
        public readonly string From;
        public readonly string To;
        public readonly string Delta;
        public readonly ComparisonTrend Trend;

        public ComparisonRow(string label, string from, string to, string delta,
            ComparisonTrend trend)
        {
            Label = label;
            From = from;
            To = to;
            Delta = delta;
            Trend = trend;
        }

        public bool IsComparison => !string.IsNullOrEmpty(From);
    }

    /// <summary>
    /// The numbers of the inventory's comparison panel (design/ui/inventory/inventory.html): the
    /// item under the cursor against the one worn in the slot it would take. Weapon damage is what
    /// it would be in this character's hands, from the same calculator combat uses, so the panel
    /// never promises a number the sword will not deal. Nothing here decides which item is better
    /// overall; each row only says whether its own number went the player's way.
    /// </summary>
    public static class ItemComparison
    {
        /// <summary>The character the comparison is made for.</summary>
        public readonly struct Wielder
        {
            public readonly int Strength;
            public readonly int Dexterity;
            public readonly ProgressionCurve Curve;

            public Wielder(int strength, int dexterity, ProgressionCurve curve)
            {
                Strength = strength;
                Dexterity = dexterity;
                Curve = curve;
            }
        }

        /// <summary>
        /// Rows for <paramref name="candidate"/>. With nothing else worn, or with the candidate
        /// itself worn, they are its plain values; otherwise each carries the worn value and the
        /// change. Items that are not weapons or armour have no rows.
        /// </summary>
        public static List<ComparisonRow> Rows(ItemDefinition candidate, ItemDefinition worn,
            in Wielder wielder)
        {
            return Rows(candidate != null ? candidate.Equipment : null,
                worn != null && worn != candidate ? worn.Equipment : null, wielder);
        }

        /// <inheritdoc cref="Rows(ItemDefinition, ItemDefinition, in Wielder)"/>
        public static List<ComparisonRow> Rows(EquipmentDefinition candidate,
            EquipmentDefinition worn, in Wielder wielder)
        {
            var rows = new List<ComparisonRow>();
            var against = worn != candidate ? worn : null;
            if (candidate is WeaponDefinition weapon)
            {
                AddWeaponRows(rows, weapon, against as WeaponDefinition, wielder);
            }
            else if (candidate is ArmorDefinition armor)
            {
                AddArmorRows(rows, armor, against as ArmorDefinition);
            }

            return rows;
        }

        /// <summary>
        /// The scaling letter the item panel shows. The coefficient multiplies the weapon's base
        /// per attribute point, so the letters are fixed cuts of it: 0.9 and up is A.
        /// </summary>
        public static string ScalingGrade(float coefficient)
        {
            if (coefficient >= 0.9f) { return "A"; }
            if (coefficient >= 0.7f) { return "B"; }
            if (coefficient >= 0.4f) { return "C"; }
            if (coefficient >= 0.2f) { return "D"; }
            return coefficient > 0f ? "E" : "—";
        }

        private static void AddWeaponRows(List<ComparisonRow> rows, WeaponDefinition candidate,
            WeaponDefinition worn, in Wielder wielder)
        {
            var compare = worn != null;
            rows.Add(Number("Шкода", Damage(candidate, wielder),
                compare ? Damage(worn, wielder) : (float?)null, higherIsBetter: true, "0"));
            rows.Add(Number("Шкода рівновазі", candidate.StaggerMultiplier,
                compare ? worn.StaggerMultiplier : (float?)null, higherIsBetter: true, "0.##", "×"));
            rows.Add(Number("Витривалість за удар", candidate.StaminaMultiplier,
                compare ? worn.StaminaMultiplier : (float?)null, higherIsBetter: false, "0.##", "×"));
            rows.Add(Number("Швидкість атаки", candidate.AttackSpeed,
                compare ? worn.AttackSpeed : (float?)null, higherIsBetter: true, "0.##", "×"));
            rows.Add(Grade("Масштаб. Сила", candidate.StrengthScaling,
                compare ? worn.StrengthScaling : (float?)null));
            rows.Add(Grade("Масштаб. Спритність", candidate.DexterityScaling,
                compare ? worn.DexterityScaling : (float?)null));
        }

        private static void AddArmorRows(List<ComparisonRow> rows, ArmorDefinition candidate,
            ArmorDefinition worn)
        {
            var compare = worn != null;
            rows.Add(Number("Захист", candidate.Defense,
                compare ? worn.Defense : (float?)null, higherIsBetter: true, "0.#"));
            rows.Add(Number("Маг. захист", candidate.MagicDefense,
                compare ? worn.MagicDefense : (float?)null, higherIsBetter: true, "0.#"));
            rows.Add(Number("Опір рівноваги", candidate.StaggerResistanceBonus * 100f,
                compare ? worn.StaggerResistanceBonus * 100f : (float?)null, higherIsBetter: true,
                "0", suffix: "%"));
            rows.Add(Number("Вага", candidate.Weight,
                compare ? worn.Weight : (float?)null, higherIsBetter: false, "0.0"));
        }

        /// <summary>Attack power in these hands, with the penalty for unmet requirements.</summary>
        public static float Damage(WeaponDefinition weapon, in Wielder wielder)
        {
            var meets = weapon.MeetsRequirements(wielder.Strength, wielder.Dexterity);
            return WeaponDamageCalculator.Evaluate(weapon, wielder.Curve, wielder.Strength,
                wielder.Dexterity, 0f, meets).ScaledBase;
        }

        private static ComparisonRow Number(string label, float value, float? worn,
            bool higherIsBetter, string format, string prefix = "", string suffix = "")
        {
            var to = prefix + value.ToString(format) + suffix;
            if (worn == null)
            {
                return new ComparisonRow(label, string.Empty, to, string.Empty, ComparisonTrend.None);
            }

            var from = prefix + worn.Value.ToString(format) + suffix;
            // The change between the numbers as shown, so "23 → 23" never claims "+1".
            var delta = Shown(value, format) - Shown(worn.Value, format);
            var shownDelta = Mathf.Abs(delta).ToString(format);
            if (shownDelta == 0f.ToString(format))
            {
                return new ComparisonRow(label, from, to, "=", ComparisonTrend.Same);
            }

            var better = delta > 0f == higherIsBetter;
            // A true minus sign, as in the approved mockup.
            var sign = delta > 0f ? "+" : "−";
            return new ComparisonRow(label, from, to, sign + shownDelta + suffix,
                better ? ComparisonTrend.Better : ComparisonTrend.Worse);
        }

        private static float Shown(float value, string format)
        {
            return float.Parse(value.ToString(format, CultureInfo.InvariantCulture),
                CultureInfo.InvariantCulture);
        }

        private static ComparisonRow Grade(string label, float value, float? worn)
        {
            var to = ScalingGrade(value);
            if (worn == null)
            {
                return new ComparisonRow(label, string.Empty, to, string.Empty, ComparisonTrend.None);
            }

            var from = ScalingGrade(worn.Value);
            if (from == to)
            {
                return new ComparisonRow(label, from, to, "=", ComparisonTrend.Same);
            }

            var better = value > worn.Value;
            return new ComparisonRow(label, from, to, better ? "вище" : "нижче",
                better ? ComparisonTrend.Better : ComparisonTrend.Worse);
        }
    }
}
