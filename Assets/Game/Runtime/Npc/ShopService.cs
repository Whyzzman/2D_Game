using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>How a purchase went.</summary>
    public enum PurchaseResult
    {
        Bought,
        AlreadySold,
        TooPoor,
        Unavailable
    }

    /// <summary>
    /// The merchant's trade: which goods are on the cart now, what they cost, and what buying one
    /// does to the save (item 70). The screen (<see cref="ShopView"/>) only draws this. While open
    /// the world is paused and the hero does not move, like the altar's menu.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShopService : MonoBehaviour
    {
        [SerializeField] private GameProgress _progress;
        [SerializeField] private PlayerCharacter _player;

        private readonly List<MerchantGood> _offered = new List<MerchantGood>();
        private PlayerInputReader _input;

        public event Action Opened;
        public event Action Closed;
        /// <summary>A good was bought; the screen shows a rumour's words from here.</summary>
        public event Action<MerchantGood> Purchased;

        public bool IsOpen { get; private set; }
        public NpcDefinition Merchant { get; private set; }
        public IReadOnlyList<MerchantGood> Offered => _offered;
        public GameProgress Progress => _progress;

        private void Awake()
        {
            if (_progress == null) { _progress = FindAnyObjectByType<GameProgress>(); }
            if (_player == null) { _player = FindAnyObjectByType<PlayerCharacter>(); }
            _input = _player != null ? _player.GetComponent<PlayerInputReader>() : null;
        }

        private void OnDisable() => Close();

        public void Open(NpcDefinition merchant)
        {
            if (IsOpen || merchant == null || merchant.Stock == null)
            {
                return;
            }

            Merchant = merchant;
            Refresh();
            IsOpen = true;
            _input?.SetGameplayEnabled(false);
            GamePause.Push();
            Opened?.Invoke();
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            GamePause.Pop();
            _input?.SetGameplayEnabled(true);
            Closed?.Invoke();
        }

        /// <summary>Everything on the cart now, sold ones included, in the stock's order.</summary>
        public void Refresh()
        {
            _offered.Clear();
            if (Merchant == null || Merchant.Stock == null)
            {
                return;
            }

            foreach (var good in Merchant.Stock.Goods)
            {
                if (good != null && (string.IsNullOrEmpty(good.requiresBoss)
                    || (_progress != null && _progress.IsBossDefeated(good.requiresBoss))))
                {
                    _offered.Add(good);
                }
            }
        }

        public bool IsSold(MerchantGood good) =>
            good != null && _progress != null && _progress.HasReward(MerchantStock.BoughtId(good));

        public PurchaseResult Check(MerchantGood good)
        {
            if (good == null || _progress == null || !_offered.Contains(good))
            {
                return PurchaseResult.Unavailable;
            }

            if (IsSold(good))
            {
                return PurchaseResult.AlreadySold;
            }

            return _progress.Currency < good.price ? PurchaseResult.TooPoor : PurchaseResult.Bought;
        }

        public PurchaseResult Buy(MerchantGood good)
        {
            var result = Check(good);
            if (result != PurchaseResult.Bought || !_progress.TrySpendCurrency(good.price))
            {
                return result == PurchaseResult.Bought ? PurchaseResult.TooPoor : result;
            }

            _progress.GrantReward(MerchantStock.BoughtId(good));
            Deliver(good);
            _progress.Save();
            AudioManager.Play(AudioId.ShopBuy, transform.position);
            Purchased?.Invoke(good);
            return PurchaseResult.Bought;
        }

        private void Deliver(MerchantGood good)
        {
            switch (good.kind)
            {
                case GoodKind.ZoneMap:
                    _progress.GrantReward(MerchantStock.MapRewardId(good.zoneId));
                    break;
                case GoodKind.Quill:
                    _progress.GrantReward(MerchantStock.QuillRewardId);
                    break;
                case GoodKind.Rumour:
                    _progress.GrantReward(MerchantStock.RumourRewardId(good.target));
                    break;
                case GoodKind.Lockpick:
                    _progress.AddItem(MerchantStock.LockpickItemId);
                    break;
                case GoodKind.FlaskShard:
                    _progress.GrantReward(MerchantStock.FlaskShardPrefix + good.id);
                    break;
                case GoodKind.Relic:
                    _progress.AddItem(good.target);
                    break;
            }
        }

        /// <summary>The problem in words, for the screen: never a silent refusal.</summary>
        public string Describe(MerchantGood good, PurchaseResult result)
        {
            switch (result)
            {
                case PurchaseResult.AlreadySold: return "Продано";
                case PurchaseResult.TooPoor:
                    return $"Бракує {Mathf.Max(0, good.price - (_progress != null ? _progress.Currency : 0))} золота";
                case PurchaseResult.Unavailable: return "Не продається";
                default: return string.Empty;
            }
        }
    }
}
