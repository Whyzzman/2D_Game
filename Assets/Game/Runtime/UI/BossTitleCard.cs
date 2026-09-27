using UnityEngine;
using UnityEngine.UIElements;

namespace ForgottenKingdom
{
    /// <summary>
    /// The boss's name on the screen as its arena seals, from design/ui/boss-title (approved
    /// 2026-09-27): the epithet, the name drawing in from wide spacing and a rule opening under
    /// it; it holds, then fades. It never blocks input or dims the screen — the boss is playing
    /// its harmless intro at the time. With reduced motion only the fade remains.
    ///
    /// Split out of <see cref="HudController"/>, which only says when to show it.
    /// </summary>
    public sealed class BossTitleCard
    {
        public const float FadeIn = 0.6f;
        public const float Hold = 2.2f;
        public const float FadeOut = 0.8f;
        public const float Total = FadeIn + Hold + FadeOut;

        /// <summary>Letters start this far apart and draw in over the first third of the card.</summary>
        private const float WideSpacing = 22f;
        private const float Spacing = 8f;
        private const float DrawIn = Total * 0.3f;
        private const float RuleWidth = 770f;

        private readonly VisualElement _card;
        private readonly Label _epithet;
        private readonly Label _name;
        private readonly VisualElement _rule;
        private float _clock = -1f;

        public BossTitleCard(VisualElement root)
        {
            _card = root?.Q("boss-title");
            _epithet = root?.Q<Label>("boss-title-epithet");
            _name = root?.Q<Label>("boss-title-name");
            _rule = root?.Q("boss-title-rule");
        }

        public bool IsShowing => _clock >= 0f;

        public string Name => _name != null ? _name.text : string.Empty;

        public string Epithet => _epithet != null ? _epithet.text : string.Empty;

        public void Show(string name, string epithet)
        {
            if (_card == null)
            {
                return;
            }

            if (_name != null) { _name.text = name ?? string.Empty; }
            if (_epithet != null)
            {
                _epithet.text = string.IsNullOrEmpty(epithet) ? string.Empty : epithet.ToUpperInvariant();
                _epithet.style.display = string.IsNullOrEmpty(epithet) ? DisplayStyle.None : DisplayStyle.Flex;
            }

            _clock = 0f;
            Tick(0f);
        }

        public void Hide()
        {
            _clock = -1f;
            if (_card != null) { _card.style.opacity = 0f; }
        }

        public void Tick(float dt)
        {
            if (_clock < 0f || _card == null)
            {
                return;
            }

            _clock += dt;
            if (_clock >= Total)
            {
                Hide();
                return;
            }

            _card.style.opacity = Opacity(_clock);
            var drawn = FeedbackSettings.ReducedMotion ? 1f : Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_clock / DrawIn));
            if (_name != null) { _name.style.letterSpacing = Mathf.Lerp(WideSpacing, Spacing, drawn); }
            if (_rule != null) { _rule.style.width = RuleWidth * drawn; }
        }

        /// <summary>The card's opacity at a moment: in, held, out.</summary>
        public static float Opacity(float time)
        {
            if (time < 0f || time >= Total) { return 0f; }
            if (time < FadeIn) { return time / FadeIn; }
            if (time < FadeIn + Hold) { return 1f; }
            return 1f - (time - FadeIn - Hold) / FadeOut;
        }
    }
}
