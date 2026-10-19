using UnityEngine;
using UnityEngine.UIElements;

namespace ForgottenKingdom
{
    /// <summary>
    /// What the HUD does when the hero is hurt or near the end (2026-10-19, UI_UX_GUIDE.md §2а):
    /// <list type="bullet">
    /// <item>the health bar flashes pale for a moment (<c>bar--struck</c>) and, with motion on, shudders;</item>
    /// <item>the edges of the screen flush red, as strongly as the blow was large, and fade;</item>
    /// <item>under 30 % the fill breathes slowly between two reds, so low health is felt without
    /// looking at the number;</item>
    /// <item>the mana bar glints along its edge as it comes up full (<c>bar--brim</c>).</item>
    /// </list>
    /// The red edge follows the flash setting and the shudder the shake setting; at zero both are
    /// gone and the bar's own pale flash still says "hit". The colours and timings are in Hud.uss.
    /// </summary>
    internal sealed class HudVitals
    {
        private const float StruckTime = 0.14f;
        private const float BrimTime = 0.4f;
        private const float EdgePeak = 0.55f;
        private const float EdgeFade = 0.45f;
        private const float ShudderTime = 0.2f;
        private const float ShudderPixels = 4f;
        private const float BreathPeriod = 0.6f;

        private readonly VisualElement _healthBar;
        private readonly VisualElement _manaBar;
        private readonly VisualElement _healthFill;
        private readonly VisualElement _edge;
        private readonly VisualElement _group;
        private float _struck;
        private float _brim;
        private float _edgeLevel;
        private float _shudder;
        private float _breath;
        private bool _breathOut;
        private float _writtenEdge = -1f;
        private bool _shaking;
        private readonly HudMeter _healthMeter;
        private readonly HudMeter _manaMeter;

        public HudVitals(VisualElement root, HudMeter health, HudMeter mana, Sprite vignette)
        {
            _healthBar = health.Frame;
            _healthFill = health.Fill;
            _manaBar = mana.Frame;
            _group = root.Q("resources");
            _edge = root.Q("hurt-flash");
            if (_edge != null && vignette != null)
            {
                _edge.style.backgroundImage = new StyleBackground(vignette);
            }

            _healthMeter = health;
            _manaMeter = mana;
            health.Dropped += OnHurt;
            mana.Brimmed += OnBrimmed;
        }

        /// <summary>Lets go of the meters; they outlive a disabled HUD and must not keep two listeners.</summary>
        public void Dispose()
        {
            _healthMeter.Dropped -= OnHurt;
            _manaMeter.Brimmed -= OnBrimmed;
        }

        private void OnBrimmed()
        {
            _brim = BrimTime;
            _manaBar?.AddToClassList("bar--brim");
        }

        /// <summary>A blow took <paramref name="share"/> of the bar away.</summary>
        private void OnHurt(float share)
        {
            _struck = StruckTime;
            _healthBar?.AddToClassList("bar--struck");
            // A chip is a hint, a quarter of the bar is the full flush.
            var weight = Mathf.Clamp(share * 4f, 0.35f, 1f);
            _edgeLevel = Mathf.Max(_edgeLevel, EdgePeak * weight * FeedbackSettings.FlashIntensity);
            if (FeedbackSettings.ScreenShakeMultiplier > 0.001f)
            {
                _shudder = ShudderTime * Mathf.Min(1f, FeedbackSettings.ScreenShakeMultiplier);
            }
        }

        public void Tick(float dt, float healthShare, bool low)
        {
            if (_struck > 0f && (_struck -= dt) <= 0f)
            {
                _healthBar?.RemoveFromClassList("bar--struck");
            }

            if (_brim > 0f && (_brim -= dt) <= 0f)
            {
                _manaBar?.RemoveFromClassList("bar--brim");
            }

            _edgeLevel = Mathf.Max(0f, _edgeLevel - dt * EdgePeak / EdgeFade);
            if (_edge != null && Mathf.Abs(_edgeLevel - _writtenEdge) > 0.002f)
            {
                _writtenEdge = _edgeLevel;
                _edge.style.opacity = _edgeLevel;
            }

            Shudder(dt);
            Breathe(dt, low && healthShare > 0f);
        }

        private void Shudder(float dt)
        {
            if (_group == null)
            {
                return;
            }

            if (_shudder > 0f)
            {
                _shudder -= dt;
                var strength = Mathf.Clamp01(_shudder / ShudderTime) * ShudderPixels;
                _group.style.translate = new Translate(Random.Range(-1f, 1f) * strength,
                    Random.Range(-1f, 1f) * strength * 0.5f);
                _shaking = true;
            }
            else if (_shaking)
            {
                _shaking = false;
                _group.style.translate = new Translate(0f, 0f);
            }
        }

        /// <summary>
        /// Low health breathes: the class flips every <see cref="BreathPeriod"/> seconds and the
        /// USS transition does the rest, so nothing is written per frame.
        /// </summary>
        private void Breathe(float dt, bool low)
        {
            if (_healthFill == null)
            {
                return;
            }

            if (!low)
            {
                if (_breathOut)
                {
                    _breathOut = false;
                    _healthFill.RemoveFromClassList("bar__fill--breath");
                }

                _breath = 0f;
                return;
            }

            _breath += dt;
            if (_breath >= BreathPeriod)
            {
                _breath = 0f;
                _breathOut = !_breathOut;
                _healthFill.EnableInClassList("bar__fill--breath", _breathOut);
            }
        }
    }
}
