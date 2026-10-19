using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgottenKingdom
{
    /// <summary>
    /// The merchant's cart from design/ui/shop/shop.html (frames 1–3), drawn for
    /// <see cref="ShopService"/>, which keeps the rules. Goods of one kind share a row showing the
    /// next one for sale and how many are left; a sold row stays, greyed, with «Продано»; a price
    /// the purse cannot meet turns red with the shortfall. A bought rumour shows its words before
    /// the cart comes back. Up and down choose; interact, Enter or A buy; Escape or B leave.
    /// </summary>
    public sealed class ShopView : IHudOverlay
    {
        private const string OpenClass = "shop--open";
        private const string RumourClass = "shop--rumour";

        /// <summary>One line of the list: goods of one kind, the next of them for sale.</summary>
        public sealed class Row
        {
            public readonly List<MerchantGood> Goods = new List<MerchantGood>();
            public MerchantGood Next;
            public int Left;
            public VisualElement Element;
        }

        private readonly VisualElement _overlay;
        private readonly Label _purse;
        private readonly ScrollView _list;
        private readonly Label _kind;
        private readonly Label _name;
        private readonly Label _description;
        private readonly Label _effect;
        private readonly Label _price;
        private readonly Label _problem;
        private readonly Button _buy;
        private readonly Label _rumourQuote;
        private readonly Label _rumourWhere;
        private readonly List<Row> _rows = new List<Row>();
        private ShopService _shop;
        private PlayerInputReader _input;
        private MenuInput.Repeater _repeater;
        private int _focus;
        private int _openedFrame = -1;

        public ShopView(VisualElement root)
        {
            _overlay = root?.Q("shop");
            if (_overlay == null)
            {
                return;
            }

            _purse = _overlay.Q<Label>("shop-purse");
            _list = _overlay.Q<ScrollView>("shop-goods");
            _kind = _overlay.Q<Label>("shop-kind");
            _name = _overlay.Q<Label>("shop-name");
            _description = _overlay.Q<Label>("shop-desc");
            _effect = _overlay.Q<Label>("shop-effect");
            _price = _overlay.Q<Label>("shop-price");
            _problem = _overlay.Q<Label>("shop-problem");
            _buy = _overlay.Q<Button>("shop-buy");
            _rumourQuote = _overlay.Q<Label>("shop-rumour-quote");
            _rumourWhere = _overlay.Q<Label>("shop-rumour-where");
            if (_buy != null) { _buy.clicked += () => BuyFocused(); }
            var ok = _overlay.Q<Button>("shop-rumour-ok");
            if (ok != null) { ok.clicked += HideRumour; }
        }

        public bool IsOpen => _overlay != null && _overlay.ClassListContains(OpenClass);
        public bool IsRumourShown => IsOpen && _overlay.ClassListContains(RumourClass);
        public IReadOnlyList<Row> Rows => _rows;
        public int Focus => _focus;
        public string Problem => _problem != null ? _problem.text : string.Empty;

        public void Attach(ShopService shop, PlayerCharacter player)
        {
            Detach();
            _shop = shop;
            _input = player != null ? player.GetComponent<PlayerInputReader>() : null;
            if (_shop == null || _overlay == null)
            {
                return;
            }

            _shop.Opened += OnOpened;
            _shop.Closed += OnClosed;
            _shop.Purchased += OnPurchased;
        }

        public void Detach()
        {
            if (_shop != null)
            {
                _shop.Opened -= OnOpened;
                _shop.Closed -= OnClosed;
                _shop.Purchased -= OnPurchased;
            }

            _shop = null;
            _overlay?.RemoveFromClassList(OpenClass);
        }

        public void Back()
        {
            if (IsRumourShown)
            {
                HideRumour();
            }
            else
            {
                _shop?.Close();
            }
        }

        private void OnOpened()
        {
            _overlay.AddToClassList(OpenClass);
            _overlay.RemoveFromClassList(RumourClass);
            _openedFrame = Time.frameCount;
            Build();
            Select(Mathf.Max(0, _rows.FindIndex(row => row.Left > 0)));
        }

        private void OnClosed()
        {
            _overlay.RemoveFromClassList(OpenClass);
            _overlay.RemoveFromClassList(RumourClass);
        }

        private void OnPurchased(MerchantGood good)
        {
            Build();
            Select(_focus);
            if (good.kind != GoodKind.Rumour)
            {
                return;
            }

            var zone = _shop.Progress != null && _shop.Progress.World != null
                ? _shop.Progress.World.Zone(good.zoneId)
                : null;
            _rumourQuote.text = $"«{good.quote}»";
            _rumourWhere.text = $"◆ Позначено на карті · {(zone != null ? zone.DisplayName : string.Empty)}";
            _overlay.AddToClassList(RumourClass);
        }

        private void HideRumour() => _overlay.RemoveFromClassList(RumourClass);

        // ---------- the list ----------

        private void Build()
        {
            _rows.Clear();
            _list.Clear();
            if (_shop == null)
            {
                return;
            }

            string group = null;
            foreach (var good in _shop.Offered)
            {
                var row = _rows.Find(r => r.Goods[0].kind == good.kind && r.Goods[0].displayName == good.displayName);
                if (row == null)
                {
                    row = new Row();
                    _rows.Add(row);
                }

                row.Goods.Add(good);
            }

            for (var i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                row.Left = 0;
                row.Next = null;
                foreach (var good in row.Goods)
                {
                    if (!_shop.IsSold(good))
                    {
                        row.Left++;
                        row.Next ??= good;
                    }
                }

                var shown = row.Next ?? row.Goods[row.Goods.Count - 1];
                var heading = IsTravel(shown.kind) ? "МАНДРИ" : "СИЛА";
                if (heading != group)
                {
                    group = heading;
                    _list.Add(UiParts.Text(heading, "shop__group"));
                }

                row.Element = BuildRow(row, shown, i);
                _list.Add(row.Element);
            }

            if (_purse != null && _shop.Progress != null)
            {
                _purse.text = _shop.Progress.Currency.ToString("N0");
            }
        }

        private static bool IsTravel(GoodKind kind) =>
            kind == GoodKind.ZoneMap || kind == GoodKind.Quill || kind == GoodKind.Rumour;

        private VisualElement BuildRow(Row row, MerchantGood shown, int index)
        {
            var element = new Button();
            element.AddToClassList("shop__row");
            var icon = new VisualElement { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("shop__row-icon");
            element.Add(icon);
            var text = new VisualElement { pickingMode = PickingMode.Ignore };
            text.AddToClassList("shop__row-text");
            text.Add(UiParts.Text(shown.displayName, "shop__row-name"));
            var sub = row.Goods.Count > 1 && row.Left > 0 ? $"лишилось {row.Left}" : shown.subtitle;
            text.Add(UiParts.Text(sub, "shop__row-sub"));
            element.Add(text);
            var sold = row.Next == null;
            element.Add(UiParts.Text(sold ? "ПРОДАНО" : shown.price.ToString("N0"), "shop__row-price"));
            element.EnableInClassList("shop__row--sold", sold);
            element.EnableInClassList("shop__row--poor", !sold && _shop.Check(row.Next) is PurchaseResult.TooPoor or PurchaseResult.NoIron or PurchaseResult.NotYet);
            element.clicked += () =>
            {
                Select(index);
                BuyFocused();
            };
            element.RegisterCallback<PointerEnterEvent>(_ => Select(index));
            return element;
        }

        private void Select(int index)
        {
            _focus = Mathf.Clamp(index, 0, Mathf.Max(0, _rows.Count - 1));
            for (var i = 0; i < _rows.Count; i++)
            {
                _rows[i].Element?.EnableInClassList("shop__row--focused", i == _focus);
            }

            if (_rows.Count > 0)
            {
                _list.ScrollTo(_rows[_focus].Element);
            }

            DrawDetail();
        }

        private void DrawDetail()
        {
            var row = _focus < _rows.Count ? _rows[_focus] : null;
            var good = row?.Next ?? (row != null ? row.Goods[row.Goods.Count - 1] : null);
            if (good == null)
            {
                _name.text = _description.text = _effect.text = _price.text = _problem.text = _kind.text = string.Empty;
                return;
            }

            _kind.text = KindName(good.kind);
            _name.text = good.displayName;
            _description.text = good.description;
            _effect.text = string.IsNullOrEmpty(good.effect) ? string.Empty : "▲ " + good.effect;
            var result = row.Next != null ? _shop.Check(row.Next) : PurchaseResult.AlreadySold;
            _price.text = row.Next != null ? $"● {good.price:N0}" : string.Empty;
            _problem.text = _shop.Describe(good, result);
            _buy?.EnableInClassList("shop__buy-button--off", result != PurchaseResult.Bought);
        }

        private static string KindName(GoodKind kind)
        {
            switch (kind)
            {
                case GoodKind.ZoneMap: return "МАНДРИ · КАРТА";
                case GoodKind.Quill: return "МАНДРИ · ПЕРО";
                case GoodKind.Rumour: return "МАНДРИ · ЧУТКА";
                case GoodKind.Lockpick: return "СИЛА · ВИТРАТНЕ";
                case GoodKind.FlaskShard: return "СИЛА · ФЛЯГА";
                default: return "СИЛА · РЕЛІКВІЯ";
            }
        }

        /// <summary>Buys the chosen row's next good. The refusal is shown, never silent.</summary>
        public PurchaseResult BuyFocused()
        {
            if (_shop == null || _focus >= _rows.Count)
            {
                return PurchaseResult.Unavailable;
            }

            var row = _rows[_focus];
            if (row.Next == null)
            {
                _problem.text = _shop.Describe(row.Goods[0], PurchaseResult.AlreadySold);
                return PurchaseResult.AlreadySold;
            }

            var result = _shop.Buy(row.Next);
            if (result != PurchaseResult.Bought)
            {
                _problem.text = _shop.Describe(row.Next, result);
            }

            return result;
        }

        // ---------- input ----------

        public void Tick()
        {
            if (!IsOpen || Time.frameCount == _openedFrame)
            {
                return;
            }

            if (MenuInput.PadBackPressed())
            {
                Back();
                return;
            }

            var pressed = MenuInput.Confirmed() || (_input != null && _input.InteractPressedThisFrame);
            if (IsRumourShown)
            {
                if (pressed) { HideRumour(); }
                return;
            }

            var step = _repeater.Step(MenuInput.ReadVertical());
            if (step != 0 && _rows.Count > 0)
            {
                Select((_focus + step + _rows.Count) % _rows.Count);
            }

            if (pressed)
            {
                BuyFocused();
            }
        }
    }
}
