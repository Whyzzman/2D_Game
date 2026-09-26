using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// How much of each kind of damage a creature shrugs off. Positive is resistance, negative is
    /// a weakness, and the range is clamped so no authoring mistake can make something immune or
    /// make a hit heal it.
    /// </summary>
    [Serializable]
    public sealed class ResistanceProfile
    {
        /// <summary>Worst case is taking double; best case is taking a tenth.</summary>
        public const float MinResistance = -1f;
        public const float MaxResistance = 0.9f;

        [Range(MinResistance, MaxResistance)] [SerializeField] private float _physical;
        [Range(MinResistance, MaxResistance)] [SerializeField] private float _fire;
        [Range(MinResistance, MaxResistance)] [SerializeField] private float _frost;
        [Range(MinResistance, MaxResistance)] [SerializeField] private float _lightning;
        [Range(MinResistance, MaxResistance)] [SerializeField] private float _dark;
        [Range(MinResistance, MaxResistance)] [SerializeField] private float _magic;

        public float Get(DamageType type)
        {
            switch (type)
            {
                case DamageType.Fire: return Clamp(_fire);
                case DamageType.Frost: return Clamp(_frost);
                case DamageType.Lightning: return Clamp(_lightning);
                case DamageType.Dark: return Clamp(_dark);
                case DamageType.Magic: return Clamp(_magic);
                case DamageType.Physical: return Clamp(_physical);
                // Poison and bleed have no resistance of their own yet; they read as magic
                // until their own systems exist, rather than silently ignoring resistance.
                default: return Clamp(_magic);
            }
        }

        public void Set(DamageType type, float value)
        {
            value = Clamp(value);
            switch (type)
            {
                case DamageType.Fire: _fire = value; break;
                case DamageType.Frost: _frost = value; break;
                case DamageType.Lightning: _lightning = value; break;
                case DamageType.Dark: _dark = value; break;
                case DamageType.Magic: _magic = value; break;
                default: _physical = value; break;
            }
        }

        /// <summary>
        /// What a hit of this kind is actually worth against this creature. A quarter resistance
        /// leaves three quarters; a quarter weakness adds a quarter.
        /// </summary>
        public float Multiplier(DamageType type)
        {
            return Mathf.Max(0.1f, 1f - Get(type));
        }

        private static float Clamp(float value)
        {
            return Mathf.Clamp(value, MinResistance, MaxResistance);
        }

        public static string Display(DamageType type)
        {
            switch (type)
            {
                case DamageType.Physical: return "Фізичний";
                case DamageType.Fire: return "Вогонь";
                case DamageType.Frost: return "Лід";
                case DamageType.Lightning: return "Блискавка";
                case DamageType.Dark: return "Темрява";
                case DamageType.Magic: return "Магія";
                default: return type.ToString();
            }
        }
    }

    /// <summary>
    /// Elemental resistance on a creature. Sits beside <see cref="CombatActor"/>, which asks it
    /// inside the one damage pipeline — so a fire spell, a burning trap and a flaming sword are
    /// all resisted the same way without any of them knowing this exists.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Resistances : MonoBehaviour
    {
        [SerializeField] private ResistanceProfile _profile = new ResistanceProfile();

        public ResistanceProfile Profile => _profile;

        public float Multiplier(DamageType type) => _profile.Multiplier(type);

        public float Get(DamageType type) => _profile.Get(type);
    }
}
