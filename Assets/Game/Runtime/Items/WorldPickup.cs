using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Something lying on the ground. The same component covers a purse of coins dropped by an
    /// enemy and a unique weapon placed by hand: what differs is the data and whether it is
    /// remembered after being taken.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class WorldPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameProgress _progress;
        [Tooltip("Leave empty for a currency pickup.")]
        [SerializeField] private string _itemId = string.Empty;
        [SerializeField] private bool _isCurrency = false;
        [SerializeField, Min(1)] private int _amount = 1;
        [Tooltip("Set for hand-placed pickups that must stay taken; empty for enemy drops.")]
        [SerializeField] private string _persistentId = string.Empty;
        [Tooltip("Walked into rather than pressed; right for coins and materials.")]
        [SerializeField] private bool _autoCollect = true;
        [SerializeField] private SpriteRenderer _visual;
        [SerializeField, Min(0f)] private float _bobHeight = 0.12f;
        [SerializeField, Min(0f)] private float _bobSpeed = 2.5f;
        private Vector3 _restPosition;
        private bool _isCollected;

        /// <summary>Raised with the id and amount actually taken; the hook for audio and VFX.</summary>
        public static event Action<WorldPickup> Collected;

        public string ItemId => _itemId;
        public bool IsCurrency => _isCurrency;
        public int Amount => _amount;
        public string PersistentId => _persistentId;
        public bool IsPersistent => !string.IsNullOrEmpty(_persistentId);
        public bool IsCollected => _isCollected;
        public ItemDefinition Definition { get; private set; }

        public bool CanInteract => !_isCollected && isActiveAndEnabled;

        public string Prompt => Definition != null
            ? $"Підібрати: {Definition.DisplayName}"
            : $"Підібрати {_amount}";

        /// <summary>Configures a pickup spawned at runtime by the loot service.</summary>
        public void Configure(GameProgress progress, string itemId, bool isCurrency, int amount,
            string persistentId, bool autoCollect)
        {
            _progress = progress;
            _itemId = itemId;
            _isCurrency = isCurrency;
            _amount = Mathf.Max(1, amount);
            _persistentId = persistentId;
            _autoCollect = autoCollect;
            _isCollected = false;
            ResolveDefinition();
            ApplyVisual();
        }

        private void Awake()
        {
            _restPosition = transform.position;
            var pickupCollider = GetComponent<Collider2D>();
            pickupCollider.isTrigger = true;
        }

        private void Start()
        {
            if (_progress == null)
            {
                _progress = FindAnyObjectByType<GameProgress>();
            }

            ResolveDefinition();
            ApplyVisual();

            // A hand-placed pickup that was taken in an earlier session is simply not here.
            if (IsPersistent && _progress != null && _progress.IsPickupCollected(_persistentId))
            {
                gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (_bobHeight <= 0f)
            {
                return;
            }

            var offset = Mathf.Sin(Time.time * _bobSpeed) * _bobHeight;
            transform.position = _restPosition + new Vector3(0f, offset, 0f);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_autoCollect)
            {
                return;
            }

            var player = other.GetComponentInParent<PlayerCharacter>();
            if (player != null)
            {
                TryCollect(player);
            }
        }

        public void Interact(PlayerCharacter player)
        {
            TryCollect(player);
        }

        /// <summary>
        /// The whole collection flow. The world object only disappears once the item really landed
        /// in the player's progression, so a pickup can never vanish without being granted.
        /// </summary>
        public bool TryCollect(PlayerCharacter player)
        {
            if (_isCollected || player == null || !player.IsAlive || _progress == null)
            {
                return false;
            }

            if (_isCurrency)
            {
                _progress.AddCurrency(_amount);
            }
            else if (_progress.AddItem(_itemId, _amount) != AddItemResult.Added)
            {
                // An unknown id or an already-owned unique: leave it where it is and say so.
                return false;
            }

            _isCollected = true;
            if (IsPersistent)
            {
                _progress.MarkPickupCollected(_persistentId);
                _progress.SaveNow();
            }

            Collected?.Invoke(this);
            gameObject.SetActive(false);
            return true;
        }

        private void ResolveDefinition()
        {
            Definition = !_isCurrency && _progress != null && _progress.Items != null
                ? _progress.Items.GetItem(_itemId)
                : null;
        }

        private void ApplyVisual()
        {
            _restPosition = transform.position;
            if (_visual == null)
            {
                return;
            }

            _visual.color = _isCurrency
                ? new Color(0.92f, 0.78f, 0.32f)
                : (Definition != null ? Definition.PickupColor : new Color(0.7f, 0.7f, 0.75f));
        }
    }
}
