using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// A chest with a lock (item 70): opening it spends one lockpick from the merchant and pays
    /// out gold and, if set, an item. Opened for good — the save remembers it as a collected
    /// pickup, so it stays open after a rest and a reload.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LockedChest : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _chestId = "chest-01";
        [SerializeField, Min(0)] private int _gold = 150;
        [SerializeField] private string _itemId = string.Empty;
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private Sprite _closedSprite;
        [SerializeField] private Sprite _openSprite;
        [SerializeField] private GameProgress _progress;

        public string ChestId => _chestId;
        public bool IsOpen { get; private set; }
        public bool HasLockpick => _progress != null && _progress.HasItem(MerchantStock.LockpickItemId);

        public bool CanInteract => !IsOpen && HasLockpick;

        public string Prompt => IsOpen ? string.Empty
            : HasLockpick ? "Відімкнути скриню (відмичка)" : "Замкнено — потрібна відмичка";

        private void Awake()
        {
            if (_progress == null) { _progress = FindAnyObjectByType<GameProgress>(); }
        }

        private void OnEnable()
        {
            if (_progress != null) { _progress.Loaded += Refresh; }
            Refresh();
        }

        private void OnDisable()
        {
            if (_progress != null) { _progress.Loaded -= Refresh; }
        }

        private void Refresh()
        {
            IsOpen = _progress != null && _progress.IsPickupCollected(_chestId);
            if (_renderer != null)
            {
                _renderer.sprite = IsOpen ? _openSprite : _closedSprite;
            }
        }

        public void Interact(PlayerCharacter player)
        {
            if (!CanInteract || !_progress.TryRemoveItem(MerchantStock.LockpickItemId))
            {
                return;
            }

            _progress.MarkPickupCollected(_chestId);
            if (_gold > 0)
            {
                _progress.AddCurrency(_gold);
            }

            if (!string.IsNullOrEmpty(_itemId))
            {
                _progress.AddItem(_itemId);
            }

            _progress.Save();
            AudioManager.Play(AudioId.ChestOpen, transform.position);
            Refresh();
        }
    }
}
