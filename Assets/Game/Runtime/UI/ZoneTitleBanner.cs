using UnityEngine;
using UnityEngine.UIElements;

namespace ForgottenKingdom
{
    /// <summary>
    /// The line across the middle of the screen that fades in and out: a zone's name on entering
    /// it, and since item 4 of the big list a vessel shard's count on picking one up. Moved out of
    /// <see cref="HudController"/>, which only feeds it text and time.
    /// </summary>
    public sealed class ZoneTitleBanner
    {
        private const float FadeIn = 0.9f;
        private const float Hold = 2.6f;
        private const float FadeOut = 1.1f;
        private readonly VisualElement _banner;
        private readonly Label _text;
        private float _clock = -1f;

        public ZoneTitleBanner(VisualElement root)
        {
            _banner = root.Q("zone-title");
            _text = root.Q<Label>("zone-title-text");
        }

        public string Text => _text != null ? _text.text : string.Empty;
        public bool IsShowing => _clock >= 0f;

        public void Show(string text)
        {
            if (_text == null || string.IsNullOrEmpty(text))
            {
                return;
            }

            _text.text = text.ToUpperInvariant();
            _clock = 0f;
        }

        public void Hide()
        {
            _clock = -1f;
            if (_banner != null) { _banner.style.opacity = 0f; }
        }

        public void Tick(float dt)
        {
            if (_clock < 0f || _banner == null)
            {
                return;
            }

            _clock += dt;
            float alpha;
            if (_clock < FadeIn)
            {
                alpha = _clock / FadeIn;
            }
            else if (_clock < FadeIn + Hold)
            {
                alpha = 1f;
            }
            else
            {
                alpha = 1f - (_clock - FadeIn - Hold) / FadeOut;
            }

            if (alpha <= 0f && _clock > FadeIn)
            {
                alpha = 0f;
                _clock = -1f;
            }

            _banner.style.opacity = Mathf.Clamp01(alpha);
        }
    }
}
