using UnityEngine;
using UnityEngine.UIElements;

namespace ForgottenKingdom
{
    /// <summary>
    /// The purse in the top right (2026-10-19, UI_UX_GUIDE.md §2а): gold picked up is counted in,
    /// quickly while the gap is large and slowing as it closes, and the purse glows while it
    /// counts (<c>purse--gain</c>); gold spent or lost drops at once — no count-down to wait on.
    /// The label is written only when the shown number changes. Split out of <see cref="HudController"/>.
    /// </summary>
    internal sealed class HudPurse
    {
        /// <summary>How quickly the shown number closes on the real one (per second, exponential).</summary>
        private const float CountRate = 7f;
        private const float GlowHold = 0.25f;

        private readonly VisualElement _purse;
        private readonly Label _value;
        private float _shown = -1f;
        private int _written = -1;
        private float _glow;

        public HudPurse(VisualElement root)
        {
            _purse = root.Q("purse");
            _value = root.Q<Label>("purse-value");
        }

        public void Update(int currency, float dt)
        {
            if (_value == null)
            {
                return;
            }

            if (_shown < 0f || currency < _shown)
            {
                _shown = currency;
            }
            else if (currency > _shown)
            {
                var step = (currency - _shown) * (1f - Mathf.Exp(-CountRate * dt));
                _shown = Mathf.Min(currency, _shown + Mathf.Max(step, 30f * dt));
                _glow = GlowHold;
            }

            var shown = Mathf.FloorToInt(_shown + 0.0001f);
            if (shown != _written)
            {
                _written = shown;
                _value.text = shown.ToString("N0").Replace(',', ' ');
            }

            var glowing = _glow > 0f;
            _glow -= dt;
            _purse?.EnableInClassList("purse--gain", glowing);
        }
    }
}
