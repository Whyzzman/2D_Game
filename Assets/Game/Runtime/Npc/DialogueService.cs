using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The conversation in progress (item 70): whose, which topic, which page, how much of it is
    /// typed. Talking as in Hollow Knight — pages and no choice of reply; only the merchant ends
    /// with «Торгувати / Піти». The world is paused while it runs, so nothing walks up to a hero
    /// who is reading; typing runs on unscaled time. <see cref="DialogueView"/> draws it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DialogueService : MonoBehaviour
    {
        /// <summary>Characters typed per second (the mockup's ~45).</summary>
        public const float TypingSpeed = 45f;

        [SerializeField] private GameProgress _progress;
        [SerializeField] private PlayerCharacter _player;
        [SerializeField] private ShopService _shop;

        private PlayerInputReader _input;
        private float _typed;

        public event Action Opened;
        public event Action Closed;
        /// <summary>A new page began, or the merchant's choice came up.</summary>
        public event Action PageChanged;

        public bool IsOpen { get; private set; }
        public NpcDefinition Speaker { get; private set; }
        public DialogueTopic Topic { get; private set; }
        public int Page { get; private set; }
        public int PageCount => Topic != null && Topic.pages != null ? Topic.pages.Length : 0;

        /// <summary>After the last page of a merchant's topic: «Торгувати» or «Піти».</summary>
        public bool IsChoosing { get; private set; }

        public string PageText => Topic != null && Page < PageCount ? Topic.pages[Page] ?? string.Empty : string.Empty;

        /// <summary>How much of the page is showing; the whole of it once typing is done.</summary>
        public int VisibleCharacters => Mathf.Min(PageText.Length, Mathf.FloorToInt(_typed));
        public bool IsTyping => IsOpen && !IsChoosing && VisibleCharacters < PageText.Length;

        private void Awake()
        {
            if (_progress == null) { _progress = FindAnyObjectByType<GameProgress>(); }
            if (_player == null) { _player = FindAnyObjectByType<PlayerCharacter>(); }
            if (_shop == null) { _shop = FindAnyObjectByType<ShopService>(); }
            _input = _player != null ? _player.GetComponent<PlayerInputReader>() : null;
        }

        private void OnDisable() => Close();

        /// <summary>Starts talking to a character. False when there is nothing to say or trade.</summary>
        public bool Begin(NpcDefinition speaker)
        {
            if (IsOpen || speaker == null || (_player != null && !_player.IsAlive))
            {
                return false;
            }

            var topic = speaker.Pick(_progress);
            if (topic == null && !speaker.IsMerchant)
            {
                return false;
            }

            Speaker = speaker;
            Topic = topic;
            Page = 0;
            _typed = 0f;
            IsChoosing = topic == null;
            IsOpen = true;
            _progress?.GrantReward(speaker.MetId);
            _input?.SetGameplayEnabled(false);
            GamePause.Push();
            Opened?.Invoke();
            ReachedPage();
            return true;
        }

        private void Update()
        {
            if (IsTyping)
            {
                _typed += Time.unscaledDeltaTime * TypingSpeed;
            }
        }

        /// <summary>
        /// The interact press: finishes the typing first, then turns the page; past the last page
        /// the conversation ends, or the merchant asks whether to trade.
        /// </summary>
        public void Advance()
        {
            if (!IsOpen || IsChoosing)
            {
                return;
            }

            if (IsTyping)
            {
                _typed = PageText.Length;
                return;
            }

            if (Page + 1 < PageCount)
            {
                Page++;
                _typed = 0f;
                ReachedPage();
                return;
            }

            if (Speaker != null && Speaker.IsMerchant)
            {
                IsChoosing = true;
                PageChanged?.Invoke();
                return;
            }

            Close();
        }

        /// <summary>The merchant's «Торгувати»: the conversation gives way to the cart.</summary>
        public void Trade()
        {
            var merchant = Speaker;
            Close();
            if (merchant != null && merchant.IsMerchant && _shop != null)
            {
                _shop.Open(merchant);
            }
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            IsChoosing = false;
            GamePause.Pop();
            _input?.SetGameplayEnabled(true);
            Closed?.Invoke();
        }

        private void ReachedPage()
        {
            // Heard once the last page is on screen: leaving then still counts, leaving earlier not.
            if (Topic != null && Page == PageCount - 1 && Speaker != null)
            {
                _progress?.GrantReward(Speaker.HeardId(Topic));
            }

            PageChanged?.Invoke();
        }
    }
}
