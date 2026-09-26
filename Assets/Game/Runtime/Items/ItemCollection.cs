using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>One item id and how many of it are owned.</summary>
    [Serializable]
    public sealed class ItemStack
    {
        public string itemId = string.Empty;
        public int amount;
        /// <summary>Running number from when it was first picked up; drives "newest first".</summary>
        public int acquiredOrder;
        /// <summary>Reserved for the inventory screen's pin action; nothing reads it yet.</summary>
        public bool isFavorite;

        public ItemStack() { }

        public ItemStack(string id, int count, int order = 0)
        {
            itemId = id;
            amount = count;
            acquiredOrder = order;
        }
    }

    public enum AddItemResult
    {
        Added,
        /// <summary>A unique item that is already owned; nothing changed.</summary>
        AlreadyOwned,
        InvalidItem
    }

    /// <summary>
    /// What the player owns, as plain data. It stores totals rather than slots: how those totals
    /// are split into stacks is a question for the inventory screen, not for the save file.
    /// </summary>
    [Serializable]
    public sealed class ItemCollection
    {
        [SerializeField] private List<ItemStack> _stacks = new List<ItemStack>();
        [NonSerialized] private Dictionary<string, ItemStack> _byId;

        public IReadOnlyList<ItemStack> Stacks => _stacks;
        public int DistinctItemCount => _stacks.Count;

        public int GetAmount(string itemId)
        {
            EnsureIndex();
            return !string.IsNullOrEmpty(itemId) && _byId.TryGetValue(itemId, out var stack)
                ? stack.amount
                : 0;
        }

        public bool HasItem(string itemId, int amount = 1)
        {
            return amount > 0 && GetAmount(itemId) >= amount;
        }

        public AddItemResult Add(string itemId, int amount, bool isUnique)
        {
            if (string.IsNullOrEmpty(itemId) || amount <= 0)
            {
                return AddItemResult.InvalidItem;
            }

            EnsureIndex();
            if (_byId.TryGetValue(itemId, out var stack))
            {
                if (isUnique)
                {
                    return AddItemResult.AlreadyOwned;
                }

                stack.amount += amount;
                return AddItemResult.Added;
            }

            var added = new ItemStack(itemId, isUnique ? 1 : amount, NextOrder());
            _stacks.Add(added);
            _byId[itemId] = added;
            return AddItemResult.Added;
        }

        public bool TryRemove(string itemId, int amount)
        {
            if (amount <= 0 || !HasItem(itemId, amount))
            {
                return false;
            }

            var stack = _byId[itemId];
            stack.amount -= amount;
            if (stack.amount <= 0)
            {
                _stacks.Remove(stack);
                _byId.Remove(itemId);
            }

            return true;
        }

        public void Clear()
        {
            _stacks.Clear();
            _byId = null;
        }

        /// <summary>Everything owned, in the order it was acquired. The screen sorts its own copy.</summary>
        public IReadOnlyList<ItemStack> GetItems()
        {
            EnsureIndex();
            return _stacks;
        }

        /// <summary>
        /// Owned items of one category. The collection stores only ids, so the catalogue has to
        /// say what each id is; an id the catalogue no longer knows is skipped rather than shown.
        /// </summary>
        public List<ItemStack> GetItemsByCategory(ItemDatabase catalogue, ItemCategory category)
        {
            EnsureIndex();
            var matches = new List<ItemStack>();
            if (catalogue == null)
            {
                return matches;
            }

            foreach (var stack in _stacks)
            {
                if (catalogue.TryGetItem(stack.itemId, out var definition)
                    && definition.Category == category)
                {
                    matches.Add(stack);
                }
            }

            return matches;
        }

        private int NextOrder()
        {
            var highest = 0;
            foreach (var stack in _stacks)
            {
                if (stack != null && stack.acquiredOrder > highest)
                {
                    highest = stack.acquiredOrder;
                }
            }

            return highest + 1;
        }

        /// <summary>
        /// Merges duplicate entries and drops broken ones. A file written by an older build, or
        /// edited by hand, becomes usable instead of confusing.
        /// </summary>
        public void Normalize()
        {
            _stacks ??= new List<ItemStack>();
            var merged = new Dictionary<string, ItemStack>();
            foreach (var stack in _stacks)
            {
                if (stack == null || string.IsNullOrEmpty(stack.itemId) || stack.amount <= 0)
                {
                    continue;
                }

                if (merged.TryGetValue(stack.itemId, out var existing))
                {
                    existing.amount += stack.amount;
                    continue;
                }

                merged[stack.itemId] = stack;
            }

            _stacks = new List<ItemStack>(merged.Values);
            _byId = merged;
        }

        private void EnsureIndex()
        {
            if (_byId != null)
            {
                return;
            }

            Normalize();
        }
    }
}
