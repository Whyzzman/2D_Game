using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgottenKingdom
{
    /// <summary>
    /// One resource bar of the HUD: the fill and the pale trail of what was just lost behind it.
    /// Split out of <see cref="HudController"/> (2026-10-19, HUD feel, UI_UX_GUIDE.md §2а).
    ///
    /// A loss drops the fill at once — the hit must read on the frame it lands — and the trail
    /// waits a beat, then slides down, quickly while far behind and gently as it arrives. A gain
    /// (a flask, mana coming back) fills smoothly instead of jumping. <see cref="Dropped"/> and
    /// <see cref="Brimmed"/> tell the HUD when to flash. It writes a style only when a shown value
    /// actually moves: UI Toolkit lays the panel out again on every write.
    /// </summary>
    internal sealed class HudMeter
    {
        /// <summary>How long the trail waits before following a loss.</summary>
        public const float LossDelay = 0.45f;
        /// <summary>The trail's slowest pace, a share a second; it runs faster while far behind.</summary>
        private const float LossSpeed = 0.5f;
        /// <summary>How quickly a gain closes on its target (per second, exponential).</summary>
        private const float GainRate = 9f;
        private const float ShownEpsilon = 0.0005f;

        private VisualElement _loss;
        private float _shown = -1f;
        private float _target = -1f;
        private float _trail;
        private float _trailWait;
        private float _writtenShown = -1f;
        private float _writtenTrail = -1f;

        public VisualElement Fill { get; private set; }

        /// <summary>The bar itself (<c>{prefix}-frame</c>), for the flashes laid over it.</summary>
        public VisualElement Frame { get; private set; }

        /// <summary>The share now drawn, which trails a gain for a moment.</summary>
        public float Shown => _shown;

        /// <summary>A loss landed, by how much of the bar.</summary>
        public event Action<float> Dropped;

        /// <summary>The bar came up to full from below.</summary>
        public event Action Brimmed;

        public void Bind(VisualElement root, string prefix)
        {
            Fill = root.Q($"{prefix}-fill");
            _loss = root.Q($"{prefix}-loss");
            Frame = root.Q($"{prefix}-frame");
        }

        public void Snap(float share)
        {
            _shown = share;
            _target = share;
            _trail = share;
            _trailWait = 0f;
            Write();
        }

        public void Set(float share, float dt)
        {
            if (_shown < 0f)
            {
                Snap(share);
                return;
            }

            var wasFull = _target >= 0.999f;
            if (share < _shown - 0.0001f)
            {
                Dropped?.Invoke(_shown - share);
                _trailWait = LossDelay;
                _shown = share;
            }
            else if (share > _shown)
            {
                // Eased, but never so slowly that the last sliver hangs.
                var step = (share - _shown) * (1f - Mathf.Exp(-GainRate * dt));
                _shown = Mathf.Min(share, _shown + Mathf.Max(step, 0.05f * dt));
            }

            _target = share;
            if (!wasFull && share >= 0.999f)
            {
                Brimmed?.Invoke();
            }

            if (_trail < _shown)
            {
                _trail = _shown;
            }
            else if (_trailWait > 0f)
            {
                _trailWait -= dt;
            }
            else
            {
                var gap = _trail - _shown;
                _trail = Mathf.MoveTowards(_trail, _shown, (LossSpeed + gap * 3f) * dt);
            }

            Write();
        }

        private void Write()
        {
            if (Fill != null && Mathf.Abs(_shown - _writtenShown) > ShownEpsilon)
            {
                _writtenShown = _shown;
                Fill.style.width = Length.Percent(_shown * 99f);
                Fill.style.borderRightWidth = _shown > 0.005f && _shown < 0.995f ? 2f : 0f;
            }

            if (_loss != null && Mathf.Abs(_trail - _writtenTrail) > ShownEpsilon)
            {
                _writtenTrail = _trail;
                _loss.style.width = Length.Percent(_trail * 99f);
            }
        }
    }
}
