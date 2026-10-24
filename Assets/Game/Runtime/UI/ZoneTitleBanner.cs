using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgottenKingdom
{
    /// <summary>
    /// How the zone card is timed against the music (GAME_FEEL_IDEAS.md, item 20); a field of the
    /// HUD so it is tuned without a rebuild.
    /// </summary>
    [Serializable]
    public sealed class ZoneTitleTiming
    {
        [Header("The full card, the first time a region is entered")]
        [Min(0.05f)] public float fadeIn = 0.9f;
        [Min(0f)] public float hold = 2.6f;
        [Min(0.05f)] public float fadeOut = 1.1f;

        [Header("The short card, on coming back")]
        [Min(0.05f)] public float shortFadeIn = 0.45f;
        [Min(0f)] public float shortHold = 1.1f;
        [Min(0.05f)] public float shortFadeOut = 0.7f;
        [Tooltip("Seconds before the same region's short card may show again; a border walked back "
            + "and forth must not flash its name every time.")]
        [Min(0f)] public float shortCooldown = 45f;

        [Header("Music")]
        [Tooltip("How far the region's theme has faded in when the card appears: its first phrase is heard.")]
        [Range(0f, 1f)] public float musicArrival = 0.35f;
        [Tooltip("The card never waits longer than this for the music, in seconds.")]
        [Min(0f)] public float maxWait = 2.5f;
    }

    /// <summary>
    /// The line across the middle of the screen that fades in and out: a zone's name on entering
    /// it, and since item 4 of the big list a vessel shard's count on picking one up. Moved out of
    /// <see cref="HudController"/>, which only feeds it text and time.
    ///
    /// A zone's name waits for the zone's music (GAME_FEEL_IDEAS.md, item 20, 2026-10-19): the card
    /// rises as the region's theme crossfades in, so name and first phrase arrive together. The
    /// first visit shows the full card; later visits a short one, at most once per cooldown.
    /// </summary>
    public sealed class ZoneTitleBanner
    {
        private readonly VisualElement _banner;
        private readonly Label _text;
        private readonly ZoneTitleTiming _timing;
        private readonly Dictionary<string, float> _lastShown = new Dictionary<string, float>(StringComparer.Ordinal);
        private float _clock = -1f;
        private float _fadeIn;
        private float _hold;
        private float _fadeOut;
        private string _pendingText;
        private string _pendingZone;
        private bool _pendingIsFull;
        private float _pendingWaited;

        public ZoneTitleBanner(VisualElement root, ZoneTitleTiming timing = null)
        {
            _banner = root.Q("zone-title");
            _text = root.Q<Label>("zone-title-text");
            _timing = timing ?? new ZoneTitleTiming();
            UseFull();
        }

        public string Text => _text != null ? _text.text : string.Empty;
        public bool IsShowing => _clock >= 0f;

        /// <summary>A zone card waiting for its music to come in.</summary>
        public bool IsWaitingForMusic => _pendingZone != null;

        /// <summary>True when the card on screen is the short one of a return visit.</summary>
        public bool IsShort { get; private set; }

        /// <summary>A message that is not a zone (a shard, a wave): shown now, and it replaces a waiting zone card.</summary>
        public void Show(string text)
        {
            _pendingZone = null;
            UseFull();
            Begin(text);
        }

        /// <summary>
        /// A region entered. <paramref name="isFirstVisit"/> asks for the full card; a later visit
        /// gets the short one, unless that region's card was on screen within the cooldown. Either
        /// waits for the region's theme (<see cref="MusicDirector.ThemeArrival"/>).
        /// </summary>
        public void ShowZone(string zoneId, string text, bool isFirstVisit)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            zoneId ??= string.Empty;
            // The first-visit request arrives just after the plain entry of the same crossing; it
            // upgrades the short card that entry queued, keeping its wait, rather than adding a second.
            var samePending = _pendingZone == zoneId;
            if (!isFirstVisit && !samePending && _lastShown.TryGetValue(zoneId, out var last)
                && Time.unscaledTime - last < _timing.shortCooldown)
            {
                return;
            }

            if (!samePending)
            {
                _pendingWaited = 0f;
                _pendingIsFull = false;
            }

            _pendingZone = zoneId;
            _pendingText = text;
            _pendingIsFull |= isFirstVisit;
            TryShowPending();
        }

        public void Hide()
        {
            _clock = -1f;
            _pendingZone = null;
            if (_banner != null) { _banner.style.opacity = 0f; }
        }

        public void Tick(float dt)
        {
            if (_pendingZone != null)
            {
                _pendingWaited += dt;
                TryShowPending();
            }

            if (_clock < 0f || _banner == null)
            {
                return;
            }

            _clock += dt;
            float alpha;
            if (_clock < _fadeIn)
            {
                alpha = _clock / _fadeIn;
            }
            else if (_clock < _fadeIn + _hold)
            {
                alpha = 1f;
            }
            else
            {
                alpha = 1f - (_clock - _fadeIn - _hold) / _fadeOut;
            }

            if (alpha <= 0f && _clock > _fadeIn)
            {
                alpha = 0f;
                _clock = -1f;
            }

            _banner.style.opacity = Mathf.Clamp01(alpha);
        }

        private void TryShowPending()
        {
            var music = MusicDirector.Instance;
            var arrived = music == null || music.ThemeArrival(_pendingZone) >= _timing.musicArrival;
            if (!arrived && _pendingWaited < _timing.maxWait)
            {
                return;
            }

            var zoneId = _pendingZone;
            var isFull = _pendingIsFull;
            _pendingZone = null;
            if (isFull)
            {
                UseFull();
            }
            else
            {
                _fadeIn = _timing.shortFadeIn;
                _hold = _timing.shortHold;
                _fadeOut = _timing.shortFadeOut;
            }

            IsShort = !isFull;
            _lastShown[zoneId] = Time.unscaledTime;
            Begin(_pendingText);
        }

        private void UseFull()
        {
            _fadeIn = Mathf.Max(0.05f, _timing.fadeIn);
            _hold = Mathf.Max(0f, _timing.hold);
            _fadeOut = Mathf.Max(0.05f, _timing.fadeOut);
            IsShort = false;
        }

        private void Begin(string text)
        {
            if (_text == null || string.IsNullOrEmpty(text))
            {
                return;
            }

            _text.text = text.ToUpperInvariant();
            _clock = 0f;
        }
    }
}
