using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// What is worn, as plain ids. One field per slot rather than a list, because the slots are a
    /// fixed set and a file that has lost one should still load the rest.
    /// </summary>
    [Serializable]
    public sealed class EquipmentLoadout
    {
        public string mainWeapon = string.Empty;
        public string secondaryWeapon = string.Empty;
        public string head = string.Empty;
        public string chest = string.Empty;
        public string gloves = string.Empty;
        public string boots = string.Empty;
        public string accessory1 = string.Empty;
        public string accessory2 = string.Empty;
        /// <summary>True when the secondary weapon is the one in hand.</summary>
        public bool isSecondaryActive;

        public string Get(EquipmentSlot slot)
        {
            switch (slot)
            {
                case EquipmentSlot.MainWeapon: return mainWeapon;
                case EquipmentSlot.SecondaryWeapon: return secondaryWeapon;
                case EquipmentSlot.Head: return head;
                case EquipmentSlot.Chest: return chest;
                case EquipmentSlot.Gloves: return gloves;
                case EquipmentSlot.Boots: return boots;
                case EquipmentSlot.Accessory1: return accessory1;
                case EquipmentSlot.Accessory2: return accessory2;
                default: return string.Empty;
            }
        }

        public void Set(EquipmentSlot slot, string itemId)
        {
            itemId ??= string.Empty;
            switch (slot)
            {
                case EquipmentSlot.MainWeapon: mainWeapon = itemId; break;
                case EquipmentSlot.SecondaryWeapon: secondaryWeapon = itemId; break;
                case EquipmentSlot.Head: head = itemId; break;
                case EquipmentSlot.Chest: chest = itemId; break;
                case EquipmentSlot.Gloves: gloves = itemId; break;
                case EquipmentSlot.Boots: boots = itemId; break;
                case EquipmentSlot.Accessory1: accessory1 = itemId; break;
                case EquipmentSlot.Accessory2: accessory2 = itemId; break;
            }
        }

        public bool IsEmpty(EquipmentSlot slot)
        {
            return string.IsNullOrEmpty(Get(slot));
        }

        public bool Contains(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return false;
            }

            foreach (var slot in EquipmentSlots.All)
            {
                if (Get(slot) == itemId)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Turns nulls from a hand-edited file into empty slots.</summary>
        public void Normalize()
        {
            foreach (var slot in EquipmentSlots.All)
            {
                Set(slot, Get(slot) ?? string.Empty);
            }

            if (string.IsNullOrEmpty(secondaryWeapon))
            {
                // Nothing to be active in; otherwise a load would arrive holding an empty hand.
                isSecondaryActive = false;
            }
        }

        public EquipmentLoadout Clone()
        {
            var copy = new EquipmentLoadout { isSecondaryActive = isSecondaryActive };
            foreach (var slot in EquipmentSlots.All)
            {
                copy.Set(slot, Get(slot));
            }

            return copy;
        }

        public bool SameAs(EquipmentLoadout other)
        {
            if (other == null || other.isSecondaryActive != isSecondaryActive)
            {
                return false;
            }

            foreach (var slot in EquipmentSlots.All)
            {
                if (other.Get(slot) != Get(slot))
                {
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>Ids that the game itself depends on; never spelled inline.</summary>
    public static class ItemIds
    {
        public const string StarterWeapon = "old_sword";
        public const string Greatsword = "iron_greatsword";
    }
}
