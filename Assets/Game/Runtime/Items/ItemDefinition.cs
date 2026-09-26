using UnityEngine;

namespace ForgottenKingdom
{
    public enum ItemCategory
    {
        Material,
        Consumable,
        Weapon,
        Armor,
        Accessory,
        KeyItem,
        QuestItem,
        /// <summary>Story pickups that unlock something, such as the Guardian Core.</summary>
        Progression
    }

    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    /// <summary>
    /// One authored item. Everything the game stores about an item elsewhere is its
    /// <see cref="ItemId"/> and a count; this asset holds the rest.
    /// </summary>
    [CreateAssetMenu(menuName = "Forgotten Kingdom/Item", fileName = "Item")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [Tooltip("Stable, lowercase, never renamed: this is what a save file records.")]
        [SerializeField] private string _itemId = string.Empty;
        [SerializeField] private string _displayName = "Item";
        [SerializeField, TextArea] private string _description = string.Empty;
        [SerializeField] private ItemCategory _category = ItemCategory.Material;
        [SerializeField] private ItemRarity _rarity = ItemRarity.Common;
        [SerializeField] private Sprite _icon;
        [SerializeField] private bool _isStackable = true;
        [SerializeField, Min(1)] private int _maxStack = 99;
        [Tooltip("Owned at most once; a second pickup is refused rather than counted.")]
        [SerializeField] private bool _isUnique = false;
        [Tooltip("Writes the save the moment it is picked up; for key and story items.")]
        [SerializeField] private bool _autosaveOnAcquire = false;
        [Tooltip("Set for items that can be worn or wielded; empty for everything else.")]
        [SerializeField] private EquipmentDefinition _equipment;
        [SerializeField, Min(0)] private int _baseValue = 0;
        [Tooltip("Placeholder tint for the world pickup until real art exists.")]
        [SerializeField] private Color _pickupColor = new Color(0.8f, 0.78f, 0.6f);

        public string ItemId => _itemId;
        public string DisplayName => _displayName;
        public string Description => _description;
        public ItemCategory Category => _category;
        public ItemRarity Rarity => _rarity;
        public Sprite Icon => _icon;
        public bool IsStackable => _isStackable && !_isUnique;
        public int MaxStack => _isUnique ? 1 : _maxStack;
        public bool IsUnique => _isUnique;
        public bool AutosaveOnAcquire => _autosaveOnAcquire;
        public EquipmentDefinition Equipment => _equipment;
        public bool IsEquippable => _equipment != null;
        public WeaponDefinition Weapon => _equipment as WeaponDefinition;
        public int BaseValue => _baseValue;
        public Color PickupColor => _pickupColor;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_itemId))
            {
                Debug.LogError($"Item '{name}' has no id; a save could not record it.", this);
            }
            else if (_itemId != _itemId.Trim().ToLowerInvariant())
            {
                Debug.LogWarning($"Item id '{_itemId}' should be lowercase without spaces.", this);
            }

            if (_equipment != null && !string.IsNullOrEmpty(_equipment.ItemId)
                && _equipment.ItemId != _itemId)
            {
                // A mismatch here would equip one item and show another in the panel.
                Debug.LogError($"Item '{_itemId}' points at equipment for "
                    + $"'{_equipment.ItemId}'; the ids must match.", this);
            }
        }
    }
}
