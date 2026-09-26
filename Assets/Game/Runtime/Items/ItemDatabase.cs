using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The catalogue of every item in the game, looked up by id. Built once into a dictionary, so
    /// a pickup never walks a list, and it refuses to hide a duplicate or missing id.
    /// </summary>
    [CreateAssetMenu(menuName = "Forgotten Kingdom/Item Database", fileName = "ItemDatabase")]
    public sealed class ItemDatabase : ScriptableObject
    {
        [SerializeField] private ItemDefinition[] _items = System.Array.Empty<ItemDefinition>();
        private Dictionary<string, ItemDefinition> _byId;

        public IReadOnlyList<ItemDefinition> Items => _items;

        public bool TryGetItem(string itemId, out ItemDefinition item)
        {
            EnsureIndex();
            item = null;
            return !string.IsNullOrEmpty(itemId) && _byId.TryGetValue(itemId, out item);
        }

        public ItemDefinition GetItem(string itemId)
        {
            return TryGetItem(itemId, out var item) ? item : null;
        }

        public bool Contains(string itemId)
        {
            return TryGetItem(itemId, out _);
        }

        /// <summary>Rebuilds the lookup; call after editing the list at runtime.</summary>
        public void Rebuild()
        {
            _byId = new Dictionary<string, ItemDefinition>();
            foreach (var item in _items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.ItemId))
                {
                    Debug.LogError($"Item database '{name}' holds an entry without an id.", this);
                    continue;
                }

                if (!_byId.TryAdd(item.ItemId, item))
                {
                    // Two items answering to the same id would make a save ambiguous.
                    Debug.LogError($"Item id '{item.ItemId}' is used by more than one item; "
                        + "ids must be unique.", this);
                }
            }
        }

        private void EnsureIndex()
        {
            if (_byId == null)
            {
                Rebuild();
            }
        }

        private void OnEnable()
        {
            _byId = null;
        }

        private void OnValidate()
        {
            _byId = null;
        }
    }
}
