using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The character's own attributes, as opposed to the derived numbers in
    /// <see cref="StatType"/>. All six are now real: every one of them can be raised with a
    /// point and every one of them changes something the character does.
    /// </summary>
    public enum CharacterStat
    {
        Strength,
        Dexterity,
        Vitality,
        Endurance,
        Intelligence,
        /// <summary>Reserved for a later pass; never allocated today.</summary>
        Will
    }

    public static class CharacterStats
    {
        /// <summary>The attributes a point may currently go into, in screen order.</summary>
        public static readonly CharacterStat[] Allocatable =
        {
            CharacterStat.Strength, CharacterStat.Dexterity,
            CharacterStat.Vitality, CharacterStat.Endurance, CharacterStat.Intelligence,
            CharacterStat.Will
        };

        public static string Display(CharacterStat stat)
        {
            switch (stat)
            {
                case CharacterStat.Strength: return "Сила";
                case CharacterStat.Dexterity: return "Спритність";
                case CharacterStat.Vitality: return "Живучість";
                case CharacterStat.Endurance: return "Витривалість";
                case CharacterStat.Intelligence: return "Інтелект";
                case CharacterStat.Will: return "Воля";
                default: return stat.ToString();
            }
        }

        public static string Describe(CharacterStat stat)
        {
            switch (stat)
            {
                case CharacterStat.Strength:
                    return "Підвищує урон зброї, що масштабується від сили.";
                case CharacterStat.Dexterity:
                    return "Підвищує урон зброї, що масштабується від спритності.";
                case CharacterStat.Vitality:
                    return "Підвищує максимум здоров'я.";
                case CharacterStat.Endurance:
                    return "Підвищує витривалість і межу ваги спорядження.";
                case CharacterStat.Intelligence:
                    return "Підвищує запас мани та шкоду заклять.";
                default:
                    return string.Empty;
            }
        }
    }

    /// <summary>
    /// The numbers a player actually chooses, stored as plain data. Everything else about the
    /// character is worked out from these, the level and what is worn.
    /// </summary>
    [Serializable]
    public sealed class CharacterStatBlock
    {
        public int strength;
        public int dexterity;
        public int vitality;
        public int endurance;
        public int intelligence;
        public int will;

        public int Get(CharacterStat stat)
        {
            switch (stat)
            {
                case CharacterStat.Strength: return strength;
                case CharacterStat.Dexterity: return dexterity;
                case CharacterStat.Vitality: return vitality;
                case CharacterStat.Endurance: return endurance;
                case CharacterStat.Intelligence: return intelligence;
                case CharacterStat.Will: return will;
                default: return 0;
            }
        }

        public void Set(CharacterStat stat, int value)
        {
            value = Mathf.Max(0, value);
            switch (stat)
            {
                case CharacterStat.Strength: strength = value; break;
                case CharacterStat.Dexterity: dexterity = value; break;
                case CharacterStat.Vitality: vitality = value; break;
                case CharacterStat.Endurance: endurance = value; break;
                case CharacterStat.Intelligence: intelligence = value; break;
                case CharacterStat.Will: will = value; break;
            }
        }

        public void Add(CharacterStat stat, int amount)
        {
            Set(stat, Get(stat) + amount);
        }

        public int Total => strength + dexterity + vitality + endurance + intelligence + will;

        public void Validate()
        {
            strength = Mathf.Max(0, strength);
            dexterity = Mathf.Max(0, dexterity);
            vitality = Mathf.Max(0, vitality);
            endurance = Mathf.Max(0, endurance);
            intelligence = Mathf.Max(0, intelligence);
            will = Mathf.Max(0, will);
        }

        public CharacterStatBlock Clone()
        {
            return new CharacterStatBlock
            {
                strength = strength, dexterity = dexterity,
                vitality = vitality, endurance = endurance, intelligence = intelligence, will = will
            };
        }
    }
}
