using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The bag, the purse and the quill's marks: what the player carries, apart from the world's
    /// facts in <see cref="GameProgress"/>. Shop, loot, death and the smith touch this part.
    /// </summary>
    public sealed partial class GameProgress
    {
        public int GetItemAmount(string itemId)
        {
            return Data.inventory.GetAmount(itemId);
        }

        public bool HasItem(string itemId, int amount = 1)
        {
            return Data.inventory.HasItem(itemId, amount);
        }

        /// <summary>
        /// Adds an item by id. An id the database does not know is refused outright rather than
        /// quietly turning into something else, and a unique item is never counted twice.
        /// </summary>
        public AddItemResult AddItem(string itemId, int amount = 1)
        {
            if (_items != null && !_items.TryGetItem(itemId, out _))
            {
                Debug.LogError($"Unknown item id '{itemId}'; nothing was added.");
                return AddItemResult.InvalidItem;
            }

            var definition = _items != null ? _items.GetItem(itemId) : null;
            var isUnique = definition != null && definition.IsUnique;
            var result = Data.inventory.Add(itemId, amount, isUnique);
            if (result != AddItemResult.Added)
            {
                return result;
            }

            ItemAdded?.Invoke(itemId, amount, Data.inventory.GetAmount(itemId));
            if (definition != null && definition.AutosaveOnAcquire)
            {
                // Key and story items are written at once, so a crash cannot swallow them.
                SaveNow();
            }

            return AddItemResult.Added;
        }

        public bool TryRemoveItem(string itemId, int amount = 1)
        {
            if (!Data.inventory.TryRemove(itemId, amount))
            {
                return false;
            }

            ItemRemoved?.Invoke(itemId, amount, Data.inventory.GetAmount(itemId));
            return true;
        }

        public void AddCurrency(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Data.player.currency += amount;
            CurrencyChanged?.Invoke(Data.player.currency, amount);
        }

        /// <summary>Spends the full amount or nothing; currency can never go negative.</summary>
        public bool TrySpendCurrency(int amount)
        {
            if (amount <= 0 || Data.player.currency < amount)
            {
                return false;
            }

            Data.player.currency -= amount;
            CurrencyChanged?.Invoke(Data.player.currency, -amount);
            return true;
        }

        /// <summary>
        /// A death: the purse is left behind where the player can reach it again. Whatever an
        /// earlier death left is gone — only the latest can be taken back. Dying with an empty
        /// purse leaves nothing to find, and still loses the old gold.
        /// </summary>
        public void DropAllCurrency(Vector2 position)
        {
            var carried = Data.player.currency;
            Data.lostCurrency = carried;
            Data.lostCurrencyPosition = position;
            if (carried > 0)
            {
                Data.player.currency = 0;
                CurrencyChanged?.Invoke(0, -carried);
            }

            LostCurrencyChanged?.Invoke();
        }

        /// <summary>The quill's marks on the map (item 70).</summary>
        public IReadOnlyList<MapMark> MapMarks => Data.mapMarks;
        public event Action MapMarksChanged;

        /// <summary>
        /// The quill on a room: a room already marked loses its mark, an unmarked one takes the
        /// first free colour. False with no quill, or when all five colours are in use.
        /// </summary>
        public bool ToggleMapMark(string areaId)
        {
            if (string.IsNullOrEmpty(areaId) || !HasReward(MerchantStock.QuillRewardId))
            {
                return false;
            }

            var existing = Data.mapMarks.FindIndex(mark => mark.areaId == areaId);
            if (existing >= 0)
            {
                Data.mapMarks.RemoveAt(existing);
            }
            else
            {
                var color = -1;
                for (var i = 0; i < MapMark.Colors && color < 0; i++)
                {
                    if (!Data.mapMarks.Exists(mark => mark.color == i))
                    {
                        color = i;
                    }
                }

                if (color < 0)
                {
                    return false;
                }

                Data.mapMarks.Add(new MapMark { areaId = areaId, color = color });
            }

            MapMarksChanged?.Invoke();
            return true;
        }

        /// <summary>Takes the gold left at the last death back into the purse; returns how much.</summary>
        public int RecoverLostCurrency()
        {
            var amount = Data.lostCurrency;
            if (amount <= 0)
            {
                return 0;
            }

            Data.lostCurrency = 0;
            AddCurrency(amount);
            LostCurrencyChanged?.Invoke();
            return amount;
        }
    }
}
