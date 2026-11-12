using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Everything equipment is allowed to change. Adding a stat here is all a new kind of bonus
    /// needs; nothing checks for a particular item.
    /// </summary>
    public enum StatType
    {
        MaxHealth,
        MaxStamina,
        PhysicalDefense,
        /// <summary>Share of incoming poise damage ignored, 0 to 0.95.</summary>
        StaggerResistance,
        /// <summary>Carried weight. Tracked now; movement penalties come later.</summary>
        EquipmentWeight,
        /// <summary>A flat bonus on top of the active weapon's base damage.</summary>
        AttackPower,
        // Character attributes. Equipment may grant them, and they then count for weapon
        // requirements and scaling exactly as allocated points do. Appended to the end so
        // existing assets keep their serialized values.
        Strength,
        Dexterity,
        Vitality,
        Endurance,
        Intelligence,
        MaxMana,
        Will,
        BleedResistance,
        PoisonResistance,
        BurnResistance,
        FrostStatusResistance
    }

    public enum ModifierOperation
    {
        Add,
        Multiply
    }

    /// <summary>
    /// One line of "this item changes that stat by this much". Authored on equipment, never
    /// written in code as a special case.
    /// </summary>
    [Serializable]
    public sealed class StatModifier
    {
        [SerializeField] private StatType _stat = StatType.MaxHealth;
        [SerializeField] private ModifierOperation _operation = ModifierOperation.Add;
        [SerializeField] private float _value;

        public StatModifier() { }

        public StatModifier(StatType stat, ModifierOperation operation, float value)
        {
            _stat = stat;
            _operation = operation;
            _value = value;
        }

        public StatType Stat => _stat;
        public ModifierOperation Operation => _operation;
        public float Value => _value;

        /// <summary>Human-readable line for the item panel, e.g. "Максимум здоров'я +10".</summary>
        public string Describe()
        {
            var name = StatNames.Display(_stat);
            if (_operation == ModifierOperation.Multiply)
            {
                var percent = Mathf.RoundToInt((_value - 1f) * 100f);
                return $"{name} {(percent >= 0 ? "+" : string.Empty)}{percent}%";
            }

            return $"{name} {(_value >= 0f ? "+" : string.Empty)}{_value:0.##}";
        }
    }

    public static class StatNames
    {
        public static string Display(StatType stat)
        {
            switch (stat)
            {
                case StatType.MaxHealth: return "Максимум здоров'я";
                case StatType.MaxStamina: return "Максимум витривалості";
                case StatType.PhysicalDefense: return "Захист";
                case StatType.StaggerResistance: return "Опір оглушенню";
                case StatType.EquipmentWeight: return "Вага";
                case StatType.AttackPower: return "Сила атаки";
                case StatType.Strength: return "Сила";
                case StatType.Dexterity: return "Спритність";
                case StatType.Vitality: return "Живучість";
                case StatType.Endurance: return "Витривалість";
                case StatType.Intelligence: return "Інтелект";
                case StatType.MaxMana: return "Максимум мани";
                default: return stat.ToString();
            }
        }
    }

    /// <summary>
    /// Base values plus every active modifier, resolved in one pass. Nothing ever adds a bonus
    /// straight onto a live stat, so equipping and unequipping the same ring a hundred times ends
    /// exactly where it started.
    /// </summary>
    public sealed class StatBlock
    {
        private readonly Dictionary<StatType, float> _bases = new Dictionary<StatType, float>();
        private readonly Dictionary<StatType, float> _added = new Dictionary<StatType, float>();
        private readonly Dictionary<StatType, float> _multiplied = new Dictionary<StatType, float>();

        public void SetBase(StatType stat, float value)
        {
            _bases[stat] = value;
        }

        public float GetBase(StatType stat)
        {
            return _bases.TryGetValue(stat, out var value) ? value : 0f;
        }

        /// <summary>Wipes the accumulated modifiers; base values survive.</summary>
        public void ClearModifiers()
        {
            _added.Clear();
            _multiplied.Clear();
        }

        public void Apply(StatModifier modifier)
        {
            if (modifier == null)
            {
                return;
            }

            if (modifier.Operation == ModifierOperation.Multiply)
            {
                _multiplied.TryGetValue(modifier.Stat, out var current);
                _multiplied[modifier.Stat] = (current == 0f ? 1f : current) * modifier.Value;
                return;
            }

            _added.TryGetValue(modifier.Stat, out var added);
            _added[modifier.Stat] = added + modifier.Value;
        }

        public void Apply(IReadOnlyList<StatModifier> modifiers)
        {
            if (modifiers == null)
            {
                return;
            }

            for (var i = 0; i < modifiers.Count; i++)
            {
                Apply(modifiers[i]);
            }
        }

        /// <summary>Base, then every addition, then every multiplier. Always recomputed, never
        /// accumulated onto the previous answer.</summary>
        public float Get(StatType stat)
        {
            var value = GetBase(stat);
            if (_added.TryGetValue(stat, out var added))
            {
                value += added;
            }

            if (_multiplied.TryGetValue(stat, out var multiplier) && multiplier != 0f)
            {
                value *= multiplier;
            }

            return value;
        }
    }
}
