using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// A trinket. It has no stats of its own beyond the modifiers it carries, which is the point:
    /// every accessory in the game is one asset with a list of stat lines and no code.
    /// </summary>
    [CreateAssetMenu(menuName = "Forgotten Kingdom/Accessory", fileName = "Accessory")]
    public sealed class AccessoryDefinition : EquipmentDefinition
    {
        [Tooltip("Shown in the item panel above the stat lines.")]
        [SerializeField, TextArea] private string _effectText = string.Empty;

        public string EffectText => _effectText;

        public override EquipmentSlot DefaultSlot => EquipmentSlot.Accessory1;

        public override bool FitsSlot(EquipmentSlot slot)
        {
            return EquipmentSlots.IsAccessory(slot);
        }

        public override void DescribeInto(List<string> lines)
        {
            if (!string.IsNullOrWhiteSpace(_effectText))
            {
                lines.Add(_effectText);
            }

            base.DescribeInto(lines);
        }

        public override bool Validate()
        {
            if (Modifiers == null || Modifiers.Count == 0)
            {
                Debug.LogWarning($"Accessory '{name}' has no modifiers; wearing it does nothing.",
                    this);
            }

            return base.Validate();
        }
    }
}
