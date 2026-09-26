using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Where a piece of equipment goes. The order matters only for the equipment screen.
    /// </summary>
    public enum EquipmentSlot
    {
        MainWeapon,
        SecondaryWeapon,
        Head,
        Chest,
        Gloves,
        Boots,
        Accessory1,
        Accessory2
    }

    public static class EquipmentSlots
    {
        public static readonly EquipmentSlot[] All =
        {
            EquipmentSlot.MainWeapon, EquipmentSlot.SecondaryWeapon,
            EquipmentSlot.Head, EquipmentSlot.Chest,
            EquipmentSlot.Gloves, EquipmentSlot.Boots,
            EquipmentSlot.Accessory1, EquipmentSlot.Accessory2
        };

        public static bool IsWeapon(EquipmentSlot slot)
        {
            return slot == EquipmentSlot.MainWeapon || slot == EquipmentSlot.SecondaryWeapon;
        }

        public static bool IsAccessory(EquipmentSlot slot)
        {
            return slot == EquipmentSlot.Accessory1 || slot == EquipmentSlot.Accessory2;
        }

        public static string Display(EquipmentSlot slot)
        {
            switch (slot)
            {
                case EquipmentSlot.MainWeapon: return "Основна зброя";
                case EquipmentSlot.SecondaryWeapon: return "Друга зброя";
                case EquipmentSlot.Head: return "Голова";
                case EquipmentSlot.Chest: return "Тулуб";
                case EquipmentSlot.Gloves: return "Рукавиці";
                case EquipmentSlot.Boots: return "Чоботи";
                case EquipmentSlot.Accessory1: return "Аксесуар 1";
                case EquipmentSlot.Accessory2: return "Аксесуар 2";
                default: return slot.ToString();
            }
        }
    }

    /// <summary>
    /// The equippable half of an item. An <see cref="ItemDefinition"/> points at one of these when
    /// the item can be worn or wielded; items that cannot simply have none. Weapons, armour and
    /// accessories differ only in what they add on top of this.
    /// </summary>
    public abstract class EquipmentDefinition : ScriptableObject
    {
        [Tooltip("The item id this equipment belongs to; must match its ItemDefinition.")]
        [SerializeField] private string _itemId = string.Empty;
        [Tooltip("Passive stat changes while this is worn.")]
        [SerializeField] private StatModifier[] _modifiers = System.Array.Empty<StatModifier>();

        public string ItemId => _itemId;
        public IReadOnlyList<StatModifier> Modifiers => _modifiers;

        public abstract bool FitsSlot(EquipmentSlot slot);

        /// <summary>The slot this goes into when the player just presses "equip".</summary>
        public abstract EquipmentSlot DefaultSlot { get; }

        /// <summary>Lines for the item panel. Weapons and armour add their own above these.</summary>
        public virtual void DescribeInto(List<string> lines)
        {
            if (_modifiers == null)
            {
                return;
            }

            foreach (var modifier in _modifiers)
            {
                if (modifier != null)
                {
                    lines.Add(modifier.Describe());
                }
            }
        }

        /// <summary>Reports authoring mistakes that would otherwise show up as a silent no-op.</summary>
        public virtual bool Validate()
        {
            if (!string.IsNullOrWhiteSpace(_itemId))
            {
                return true;
            }

            Debug.LogError($"Equipment '{name}' has no item id; nothing could ever equip it.", this);
            return false;
        }

        protected virtual void OnValidate()
        {
            Validate();
        }
    }
}
