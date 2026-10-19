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
        [Header("Coin magnet (GAME_FEEL_IDEAS.md, item 15)")]
        [Tooltip("Coins this close to the hero fly to him. Items never do: a find is chosen, not swept up.")]
        [SerializeField, Min(0f)] private float _magnetRadius = 2f;
        [Tooltip("Units per second gained every second of flight: a coin leaves slowly and arrives fast.")]
        [SerializeField, Min(0f)] private float _magnetAcceleration = 40f;
        [SerializeField, Min(0f)] private float _magnetMaxSpeed = 18f;
        [Tooltip("Where on the hero the coin flies to, above his feet (his pivot).")]
        [SerializeField] private float _magnetTargetHeight = 0.8f;
        [Tooltip("Closer than this the coin counts as caught even if the trigger has not fired yet.")]
        [SerializeField, Min(0.01f)] private float _magnetCatchDistance = 0.25f;
        private const float PlayerSearchInterval = 1f;
        private Vector3 _restPosition;
        private bool _isCollected;
        private PlayerCharacter _magnetTarget;
        private float _nextPlayerSearch;
        private float _magnetSpeed;
        private bool _isAttracted;

        /// <summary>Raised with the id and amount actually taken; the hook for audio and VFX.</summary>
        public static event Action<WorldPickup> Collected;

        /// <summary>
        /// Scales every coin's magnet radius; 1 is the plain hero. Left for the charm
        /// «Бурштинова намистина» (CHARMS.md: 2 → 4 units), which is not built yet.
        /// </summary>
        public static float MagnetRadiusMultiplier { get; set; } = 1f;

        /// <summary>How close the hero must be for this pickup to fly to him; 0 for anything but coins.</summary>
        public float MagnetRadius => _isCurrency && _autoCollect
            ? _magnetRadius * Mathf.Max(0f, MagnetRadiusMultiplier)
            : 0f;

        /// <summary>True once a coin has started flying; it never stops halfway.</summary>
        public bool IsAttracted => _isAttracted;

        public string ItemId => _itemId;
        public bool IsCurrency => _isCurrency;
        public int Amount => _amount;
        public string PersistentId => _persistentId;
        public bool IsPersistent => !string.IsNullOrEmpty(_persistentId);
        public bool IsCollected => _isCollected;
        public ItemDefinition Definition { get; private set; }

        public bool CanInteract => !_isCollected && isActiveAndEnabled;

        /// <summary>A shard of a flask found in the world rather than bought (item 2, stage D).</summary>
        public bool IsFlaskShard => !_isCurrency && _itemId.StartsWith(MerchantStock.FlaskShardPrefix);

        /// <summary>A shard of the health or mana vessel (item 4 of the big list).</summary>
        public bool IsVesselShard => !_isCurrency && VesselShards.TryKind(_itemId, out _);

        public string Prompt => Definition != null
            ? $"Підібрати: {Definition.DisplayName}"
            : IsFlaskShard ? "Підібрати: Уламок фляги"
            : VesselShards.TryKind(_itemId, out var kind) && !_isCurrency
                ? $"Підібрати: {VesselShards.ShardName(kind)}"
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
            _isAttracted = false;
            _magnetSpeed = 0f;
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
            if (UpdateMagnet())
            {
                return;
            }

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

        /// <summary>
        /// Pulls a coin to the hero once he comes within <see cref="MagnetRadius"/>. The speed grows
        /// every frame, so the coin peels off gently and lands with a snap; once it has started it
        /// keeps going even if he walks away, as in Hollow Knight. Scaled time: a pause holds it.
        /// Returns true while the coin is flying, so the bob does not fight the flight.
        /// </summary>
        private bool UpdateMagnet()
        {
            var radius = MagnetRadius;
            if (radius <= 0f || _isCollected)
            {
                return false;
            }

            var player = FindMagnetTarget();
            if (player == null || !player.IsAlive)
            {
                return _isAttracted;
            }

            var target = (Vector2)(player.transform.position + Vector3.up * _magnetTargetHeight);
            var position = (Vector2)transform.position;
            var distance = Vector2.Distance(position, target);
            if (!_isAttracted)
            {
                if (distance > radius)
                {
                    return false;
                }

                _isAttracted = true;
                _magnetSpeed = 0f;
            }

            _magnetSpeed = Mathf.Min(_magnetMaxSpeed,
                _magnetSpeed + _magnetAcceleration * Time.deltaTime);
            var next = Vector2.MoveTowards(position, target, _magnetSpeed * Time.deltaTime);
            transform.position = new Vector3(next.x, next.y, transform.position.z);
            if (Vector2.Distance(next, target) <= _magnetCatchDistance)
            {
                TryCollect(player);
            }

            return true;
        }

        /// <summary>
        /// The hero lives in the world scene and coins in zone scenes, so the reference cannot be
        /// serialized; it is looked up once and, when there is no hero (menus, some tests), retried
        /// at most once a second rather than every frame.
        /// </summary>
        private PlayerCharacter FindMagnetTarget()
        {
            if (_magnetTarget == null && Time.unscaledTime >= _nextPlayerSearch)
            {
                _magnetTarget = FindAnyObjectByType<PlayerCharacter>();
                _nextPlayerSearch = Time.unscaledTime + PlayerSearchInterval;
            }

            return _magnetTarget;
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
            else if (IsFlaskShard || IsVesselShard)
            {
                // Not an item: one more flask or a piece of a vessel, counted from the granted
                // rewards by FlaskShards and PlayerStats.
                _progress.GrantReward(_itemId);
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
                : Definition != null ? Definition.PickupColor
                : VesselShards.TryKind(_itemId, out var kind) ? VesselShards.PickupColor(kind)
                : new Color(0.7f, 0.7f, 0.75f);
        }
    }
}
