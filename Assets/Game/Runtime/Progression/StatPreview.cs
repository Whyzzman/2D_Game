using System.Collections.Generic;

namespace ForgottenKingdom
{
    /// <summary>
    /// What one more point in an attribute would change, as the character screen lists it
    /// (design/ui/character/character.html, "Що дає …"). Worked out from the same curve and
    /// calculators the game uses, so the screen never promises a number the point will not give.
    /// </summary>
    public static class StatPreview
    {
        public static List<string> Lines(CharacterStat stat, PlayerStats stats,
            PlayerEquipment equipment, SpellCaster caster = null, PlayerSpellbook spellbook = null)
        {
            var lines = new List<string>();
            var curve = stats != null ? stats.Curve : null;
            if (curve == null)
            {
                return lines;
            }

            var current = stats.GetFinalStat(stat);
            var next = current + 1;
            switch (stat)
            {
                case CharacterStat.Strength:
                case CharacterStat.Dexterity:
                    AddAttack(lines, stat, stats, equipment, curve);
                    AddRequirements(lines, stat, current, equipment);
                    break;
                case CharacterStat.Vitality:
                    lines.Add($"Макс. здоров'я {curve.HealthFor(current):0} → {curve.HealthFor(next):0}");
                    break;
                case CharacterStat.Endurance:
                    lines.Add($"Макс. витривалість {curve.StaminaFor(current):0} → {curve.StaminaFor(next):0}");
                    lines.Add($"Межа навантаження {curve.EquipLoadFor(current):0.0} → {curve.EquipLoadFor(next):0.0}");
                    break;
                case CharacterStat.Intelligence:
                    lines.Add($"Макс. мана {curve.ManaFor(current):0} → {curve.ManaFor(next):0}");
                    var spell = caster != null && spellbook != null ? spellbook.SlotSpell(0) : null;
                    if (spell != null)
                    {
                        var after = MagicDamageCalculator.Evaluate(spell, curve, next,
                            caster.SkillMultiplierFor(spell)).Total;
                        lines.Add($"{spell.DisplayName}: шкода {caster.Evaluate(spell).Total:0.#} → {after:0.#}");
                    }

                    break;
                case CharacterStat.Will:
                    lines.Add($"Сила Тіні ×{curve.ShadowPowerFor(current):0.00} → ×{curve.ShadowPowerFor(next):0.00}");
                    lines.Add($"Опір станам від Волі {curve.StatusResistanceFor(current) * 100f:0.#}% → "
                        + $"{curve.StatusResistanceFor(next) * 100f:0.#}%");
                    break;
            }

            return lines;
        }

        private static void AddAttack(List<string> lines, CharacterStat stat, PlayerStats stats,
            PlayerEquipment equipment, ProgressionCurve curve)
        {
            var weapon = equipment != null ? equipment.ActiveWeaponDefinition : null;
            if (weapon == null)
            {
                return;
            }

            var strength = stats.GetFinalStat(CharacterStat.Strength);
            var dexterity = stats.GetFinalStat(CharacterStat.Dexterity);
            var raisedStrength = stat == CharacterStat.Strength ? strength + 1 : strength;
            var raisedDexterity = stat == CharacterStat.Dexterity ? dexterity + 1 : dexterity;
            var after = WeaponDamageCalculator.Evaluate(weapon, curve, raisedStrength, raisedDexterity,
                0f, weapon.MeetsRequirements(raisedStrength, raisedDexterity));
            lines.Add($"Атака {stats.ActiveWeaponAttack:0} → {after.ScaledBase:0}");
        }

        /// <summary>Owned weapons this attribute still holds back, and how far off they are.</summary>
        private static void AddRequirements(List<string> lines, CharacterStat stat, int current,
            PlayerEquipment equipment)
        {
            var catalogue = equipment != null ? equipment.Items : null;
            if (catalogue == null)
            {
                return;
            }

            foreach (var itemId in equipment.OwnedItemsFor(EquipmentSlot.MainWeapon))
            {
                var weapon = catalogue.GetItem(itemId)?.Weapon;
                var required = weapon != null ? weapon.RequirementFor(stat) : 0;
                if (required > current)
                {
                    lines.Add($"Вимога «{catalogue.GetItem(itemId).DisplayName}»: {required} — "
                        + $"лишилося {required - current}");
                }
            }
        }
    }
}
