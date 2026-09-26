using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// A worn piece. Defence and stagger resistance reach the combat pipeline through the player's
    /// derived stats, never by touching Health or the poise meter directly.
    /// </summary>
    [CreateAssetMenu(menuName = "Forgotten Kingdom/Armor", fileName = "Armor")]
    public sealed class ArmorDefinition : EquipmentDefinition
    {
        [SerializeField] private EquipmentSlot _slot = EquipmentSlot.Chest;
        [SerializeField, Min(0f)] private float _defense = 0f;
        [Tooltip("Reserved for the elemental pass; unused while only physical damage exists.")]
        [SerializeField, Min(0f)] private float _magicDefense = 0f;
        [SerializeField, Min(0f)] private float _weight = 0f;
        [Tooltip("Extra share of poise damage ignored, added to the player's own resistance.")]
        [SerializeField, Range(0f, 0.5f)] private float _staggerResistanceBonus = 0f;

        public EquipmentSlot Slot => _slot;
        public float Defense => _defense;
        public float MagicDefense => _magicDefense;
        public float Weight => _weight;
        public float StaggerResistanceBonus => _staggerResistanceBonus;

        public override EquipmentSlot DefaultSlot => _slot;

        public override bool FitsSlot(EquipmentSlot slot)
        {
            return slot == _slot;
        }

        public override void DescribeInto(List<string> lines)
        {
            lines.Add($"Захист {_defense:0.#}");
            lines.Add($"Вага {_weight:0.#}");
            if (_staggerResistanceBonus > 0f)
            {
                lines.Add($"Опір оглушенню +{_staggerResistanceBonus * 100f:0}%");
            }

            base.DescribeInto(lines);
        }

        public override bool Validate()
        {
            if (EquipmentSlots.IsWeapon(_slot) || EquipmentSlots.IsAccessory(_slot))
            {
                Debug.LogError($"Armor '{name}' points at {_slot}, which is not an armour slot.",
                    this);
                return false;
            }

            return base.Validate();
        }
    }
}
