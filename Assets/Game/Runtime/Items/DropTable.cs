using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>One possible drop: an item or a purse of currency.</summary>
    [Serializable]
    public sealed class DropEntry
    {
        [Tooltip("Leave empty for a currency drop.")]
        [SerializeField] private string _itemId = string.Empty;
        [SerializeField] private bool _isCurrency = false;
        [Tooltip("Chance from 0 to 1. Guaranteed entries ignore it.")]
        [SerializeField, Range(0f, 1f)] private float _chance = 1f;
        [SerializeField] private bool _isGuaranteed = false;
        [SerializeField, Min(0)] private int _minAmount = 1;
        [SerializeField, Min(0)] private int _maxAmount = 1;

        public string ItemId => _itemId;
        public bool IsCurrency => _isCurrency;
        public float Chance => _chance;
        public bool IsGuaranteed => _isGuaranteed;
        public int MinAmount => Mathf.Min(_minAmount, _maxAmount);
        public int MaxAmount => Mathf.Max(_minAmount, _maxAmount);

        public bool ShouldDrop(System.Random random)
        {
            if (_isGuaranteed || _chance >= 1f)
            {
                return true;
            }

            return _chance > 0f && random.NextDouble() < _chance;
        }

        /// <summary>Inclusive of both ends, so a 1–2 entry really can give two.</summary>
        public int RollAmount(System.Random random)
        {
            return random.Next(MinAmount, MaxAmount + 1);
        }
    }

    /// <summary>One rolled result: what to spawn and how much of it.</summary>
    public readonly struct DropResult
    {
        public readonly string ItemId;
        public readonly bool IsCurrency;
        public readonly int Amount;

        public DropResult(string itemId, bool isCurrency, int amount)
        {
            ItemId = itemId;
            IsCurrency = isCurrency;
            Amount = amount;
        }
    }

    /// <summary>
    /// What an enemy leaves behind. Every entry is rolled on its own, so one death can produce
    /// coins and a material at once.
    /// </summary>
    [CreateAssetMenu(menuName = "Forgotten Kingdom/Drop Table", fileName = "DropTable")]
    public sealed class DropTable : ScriptableObject
    {
        [SerializeField] private DropEntry[] _entries = Array.Empty<DropEntry>();

        public int EntryCount => _entries.Length;
        public DropEntry Entry(int index) => _entries[index];

        /// <summary>
        /// Rolls the table once. The caller supplies the generator, so a test can make the result
        /// repeatable without the game needing a fixed seed.
        /// </summary>
        public List<DropResult> Roll(System.Random random)
        {
            var results = new List<DropResult>();
            if (_entries == null)
            {
                return results;
            }

            random ??= new System.Random();
            foreach (var entry in _entries)
            {
                if (entry == null || !entry.ShouldDrop(random))
                {
                    continue;
                }

                var amount = entry.RollAmount(random);
                if (amount <= 0)
                {
                    continue;
                }

                results.Add(new DropResult(entry.ItemId, entry.IsCurrency, amount));
            }

            return results;
        }
    }
}
