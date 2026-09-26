using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Turns a rolled drop table into pickups on the ground and keeps track of the ones that are
    /// only temporary. Enemies ask this to spawn loot; they hold no random logic of their own.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LootService : MonoBehaviour
    {
        [SerializeField] private GameProgress _progress;
        [SerializeField] private WorldPickup _pickupPrefab;
        [Tooltip("Horizontal spread so several drops do not land on top of each other.")]
        [SerializeField, Min(0f)] private float _scatter = 0.45f;
        [SerializeField] private float _spawnHeight = 0.35f;
        private readonly List<WorldPickup> _transientPickups = new List<WorldPickup>();
        private System.Random _random = new System.Random();

        /// <summary>Raised with everything one death produced, so the HUD can group the messages.</summary>
        public event Action<IReadOnlyList<DropResult>> LootDropped;

        public int TransientPickupCount
        {
            get
            {
                Prune();
                return _transientPickups.Count;
            }
        }

        /// <summary>Lets a test make rolls repeatable; the game leaves it alone.</summary>
        public void SetRandom(System.Random random)
        {
            _random = random ?? new System.Random();
        }

        private void Awake()
        {
            if (_progress == null)
            {
                _progress = FindAnyObjectByType<GameProgress>();
            }
        }

        public List<DropResult> Roll(DropTable table)
        {
            return table != null ? table.Roll(_random) : new List<DropResult>();
        }

        /// <summary>Rolls a table and drops the result at a position, scattered a little.</summary>
        public List<DropResult> SpawnDrops(DropTable table, Vector2 origin)
        {
            var results = Roll(table);
            for (var i = 0; i < results.Count; i++)
            {
                var offset = results.Count > 1
                    ? (i - (results.Count - 1) * 0.5f) * _scatter
                    : 0f;
                Spawn(results[i], origin + new Vector2(offset, _spawnHeight));
            }

            if (results.Count > 0)
            {
                LootDropped?.Invoke(results);
            }

            return results;
        }

        public WorldPickup Spawn(DropResult result, Vector2 position)
        {
            if (_pickupPrefab == null)
            {
                Debug.LogError("LootService has no pickup prefab; nothing can be dropped.", this);
                return null;
            }

            if (!result.IsCurrency && _progress != null && _progress.Items != null
                && !_progress.Items.Contains(result.ItemId))
            {
                Debug.LogError($"Drop refers to unknown item '{result.ItemId}'; nothing spawned.");
                return null;
            }

            var pickup = Instantiate(_pickupPrefab, position, Quaternion.identity, transform);
            // Enemy loot is temporary: it carries no persistent id and is swept up on a reset.
            pickup.Configure(_progress, result.ItemId, result.IsCurrency, result.Amount,
                string.Empty, true);
            _transientPickups.Add(pickup);
            return pickup;
        }

        /// <summary>
        /// Clears loot nobody picked up. Called on a world reset so resting cannot be farmed by
        /// piling up drops that were never collected.
        /// </summary>
        public void ClearTransientLoot()
        {
            foreach (var pickup in _transientPickups)
            {
                if (pickup != null)
                {
                    Destroy(pickup.gameObject);
                }
            }

            _transientPickups.Clear();
        }

        private void Prune()
        {
            for (var i = _transientPickups.Count - 1; i >= 0; i--)
            {
                if (_transientPickups[i] == null || _transientPickups[i].IsCollected)
                {
                    _transientPickups.RemoveAt(i);
                }
            }
        }
    }
}
