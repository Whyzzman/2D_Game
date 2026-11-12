using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    public enum WeaponType
    {
        Sword,
        Greatsword,
        Dagger,
        Spear,
        Bow,
        Staff
    }

    /// <summary>
    /// Authored weapon data: its base damage, how its attacks scale off that, and the timings of
    /// the whole moveset. <see cref="PlayerCombat"/> reads everything from here, so adding a weapon
    /// never means touching combat code.
    /// </summary>
    [CreateAssetMenu(menuName = "Forgotten Kingdom/Weapon", fileName = "Weapon")]
    public sealed class WeaponDefinition : EquipmentDefinition
    {
        [SerializeField] private string _displayName = "Sword";
        [SerializeField] private WeaponType _weaponType = WeaponType.Sword;
        [Tooltip("Every attack's damage is this multiplied by that attack's own multiplier.")]
        [SerializeField, Min(0f)] private float _baseDamage = 20f;
        [Tooltip("Multiplies every attack timing; above 1 is faster.")]
        [SerializeField, Min(0.1f)] private float _attackSpeed = 1f;
        [Tooltip("Multiplies each attack's authored stamina cost.")]
        [SerializeField, Min(0f)] private float _staminaMultiplier = 1f;
        [SerializeField, Min(0f)] private float _staggerMultiplier = 1f;
        [SerializeField, Min(0f)] private float _knockbackMultiplier = 1f;
        [SerializeField, Range(0f, 1f)] private float _critChance = 0f;
        [SerializeField, Min(1f)] private float _critMultiplier = 1.5f;
        [SerializeField, Min(0f)] private float _comboResetTime = 0.7f;

        [Header("Attribute scaling")]
        [Tooltip("Share of the base damage one point of strength adds, before the global factor.")]
        [SerializeField, Min(0f)] private float _strengthScaling = 0.5f;
        [SerializeField, Min(0f)] private float _dexterityScaling = 0.4f;
        [Tooltip("Reserved for the magic pass; nothing reads it yet.")]
        [SerializeField, Min(0f)] private float _intelligenceScaling = 0f;

        [Header("Attribute requirements")]
        [SerializeField, Min(0)] private int _strengthRequirement = 0;
        [SerializeField, Min(0)] private int _dexterityRequirement = 0;

        [Header("Moveset")]
        [SerializeField] private AttackDefinition[] _lightCombo =
        {
            new AttackDefinition()
        };
        [SerializeField] private AttackDefinition _heavyAttack = new AttackDefinition();

        [Header("Visual")]
        [Tooltip("Placeholder tint for the weapon held in the player's hand.")]
        [SerializeField] private Color _visualColor = new Color(0.82f, 0.84f, 0.9f);
        [SerializeField] private Vector2 _visualSize = new Vector2(0.14f, 0.95f);
        [SerializeField] private Vector2 _visualOffset = new Vector2(0.28f, 0.1f);
        [SerializeField] private Sprite _visualSprite;

        public string DisplayName => _displayName;
        public WeaponType WeaponType => _weaponType;
        public float BaseDamage => _baseDamage;
        public float AttackSpeed => _attackSpeed;
        public float StaminaMultiplier => _staminaMultiplier;
        public float StaggerMultiplier => _staggerMultiplier;
        public float KnockbackMultiplier => _knockbackMultiplier;
        public float CritChance => _critChance;
        public float CritMultiplier => _critMultiplier;
        public float ComboResetTime => _comboResetTime;
        public float StrengthScaling => _strengthScaling;
        public float DexterityScaling => _dexterityScaling;
        public float IntelligenceScaling => _intelligenceScaling;
        public int StrengthRequirement => _strengthRequirement;
        public int DexterityRequirement => _dexterityRequirement;
        public bool HasRequirements => _strengthRequirement > 0 || _dexterityRequirement > 0;

        public int RequirementFor(CharacterStat stat)
        {
            switch (stat)
            {
                case CharacterStat.Strength: return _strengthRequirement;
                case CharacterStat.Dexterity: return _dexterityRequirement;
                default: return 0;
            }
        }

        /// <summary>Whether these hands can wield it. Only the final attribute counts, so a ring
        /// that grants strength really does let a heavier weapon be picked up.</summary>
        public bool MeetsRequirements(int strength, int dexterity)
        {
            return strength >= _strengthRequirement && dexterity >= _dexterityRequirement;
        }
        public int ComboLength => _lightCombo != null ? _lightCombo.Length : 0;
        public AttackDefinition HeavyAttack => _heavyAttack;
        public bool HasHeavyAttack => _heavyAttack != null;
        public Color VisualColor => _visualColor;
        public Vector2 VisualSize => _visualSize;
        public Vector2 VisualOffset => _visualOffset;
        public Sprite VisualSprite => _visualSprite;

        public override EquipmentSlot DefaultSlot => EquipmentSlot.MainWeapon;

        public override bool FitsSlot(EquipmentSlot slot)
        {
            return EquipmentSlots.IsWeapon(slot);
        }

        public AttackDefinition LightAttack(int comboIndex)
        {
            if (_lightCombo == null || _lightCombo.Length == 0)
            {
                return null;
            }

            return _lightCombo[Mathf.Clamp(comboIndex, 0, _lightCombo.Length - 1)];
        }

        /// <summary>Summary for the item panel; the per-attack numbers stay in the data.</summary>
        public override void DescribeInto(List<string> lines)
        {
            lines.Add($"Тип: {TypeName(_weaponType)}");
            lines.Add($"Шкода {_baseDamage:0.#}");
            lines.Add($"Швидкість {_attackSpeed:0.##}×");
            lines.Add($"Витривалість {_staminaMultiplier:0.##}×");
            lines.Add($"Оглушення {_staggerMultiplier:0.##}×");
            lines.Add($"Масштаб: сила {_strengthScaling:0.##}, спритність {_dexterityScaling:0.##}");
            if (HasRequirements)
            {
                lines.Add($"Вимоги: сила {_strengthRequirement}, спритність {_dexterityRequirement}");
            }

            base.DescribeInto(lines);
        }

        public override bool Validate()
        {
            var isValid = base.Validate();
            if (ComboLength == 0)
            {
                Debug.LogError($"Weapon '{name}' has no light attacks; it could not swing.", this);
                isValid = false;
            }

            if (_heavyAttack == null)
            {
                // Not fatal: PlayerCombat simply refuses the heavy input for this weapon.
                Debug.LogWarning($"Weapon '{name}' has no heavy attack; the heavy input will be "
                    + "ignored while it is equipped.", this);
            }

            return isValid;
        }

        public static string TypeName(WeaponType type)
        {
            switch (type)
            {
                case WeaponType.Sword: return "Меч";
                case WeaponType.Greatsword: return "Дворучний меч";
                case WeaponType.Dagger: return "Кинджали";
                case WeaponType.Spear: return "Спис";
                case WeaponType.Bow: return "Лук";
                case WeaponType.Staff: return "Посох";
                default: return type.ToString();
            }
        }
    }
}
