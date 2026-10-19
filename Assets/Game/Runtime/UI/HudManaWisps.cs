using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgottenKingdom
{
    /// <summary>
    /// Mana made visible (GAME_FEEL_IDEAS item 5): every swing that pays mana sends a small cold
    /// spark from the struck body along a curve to the end of the mana bar, and the bar brightens
    /// when it arrives — Hollow Knight's soul, which teaches "strike to fill" without a word.
    ///
    /// The bar itself already shows the true value at once; the spark is only the story of where it
    /// came from, so it never delays or changes the number. With reduced motion only the bar
    /// brightens. Part of the HUD document; <see cref="HudController"/> ticks it.
    /// </summary>
    public sealed class HudManaWisps : IDisposable
    {
        private const int PoolSize = 8;
        private const float FlightTime = 0.36f;
        private const float FlashTime = 0.18f;
        private const float HaloSize = 22f;
        private const float CoreSize = 8f;

        private readonly VisualElement _layer;
        private readonly VisualElement _frame;
        private readonly VisualElement _fill;
        private readonly WeaponManaGain _gain;
        private readonly Wisp[] _wisps = new Wisp[PoolSize];
        private float _flashLeft;
        private int _next;

        private sealed class Wisp
        {
            public VisualElement Element;
            public Vector2 From;
            public Vector2 Bend;
            public float Age = -1f;
            public float Duration;
        }

        /// <summary>Sparks in flight now; for tests and the debug readout.</summary>
        public int ActiveCount
        {
            get
            {
                var count = 0;
                foreach (var wisp in _wisps) { count += wisp.Age >= 0f ? 1 : 0; }
                return count;
            }
        }

        /// <summary>True while the bar is brightened by an arrival.</summary>
        public bool IsFlashing => _flashLeft > 0f;

        public HudManaWisps(VisualElement root, WeaponManaGain gain)
        {
            _gain = gain;
            _frame = root.Q("mana-frame");
            _fill = root.Q("mana-fill");
            _layer = new VisualElement { name = "hud-wisps", pickingMode = PickingMode.Ignore };
            _layer.AddToClassList("hud-wisps");
            root.Add(_layer);
            for (var i = 0; i < PoolSize; i++)
            {
                var element = new VisualElement { pickingMode = PickingMode.Ignore };
                element.AddToClassList("hud-wisp");
                var core = new VisualElement { pickingMode = PickingMode.Ignore };
                core.AddToClassList("hud-wisp__core");
                element.Add(core);
                element.style.display = DisplayStyle.None;
                _layer.Add(element);
                _wisps[i] = new Wisp { Element = element };
            }

            if (_gain != null) { _gain.ManaPaid += OnManaPaid; }
        }

        public void Dispose()
        {
            if (_gain != null) { _gain.ManaPaid -= OnManaPaid; }
            _frame?.RemoveFromClassList("bar--mana-flash");
            _layer.RemoveFromHierarchy();
        }

        private void OnManaPaid(Vector2 world, float amount)
        {
            if (FeedbackSettings.ReducedMotion || _layer.panel == null || Camera.main == null)
            {
                Flash();
                return;
            }

            var from = RuntimePanelUtils.CameraTransformWorldToPanel(_layer.panel, world, Camera.main);
            from = _layer.WorldToLocal(from);
            var wisp = _wisps[_next];
            _next = (_next + 1) % _wisps.Length;
            wisp.From = from;
            // Each spark bows its own way, upward first, so a flurry reads as several and not one.
            var side = (_next % 2 == 0 ? 1f : -1f) * UnityEngine.Random.Range(40f, 110f);
            wisp.Bend = new Vector2(side, -UnityEngine.Random.Range(60f, 140f));
            wisp.Duration = FlightTime * UnityEngine.Random.Range(0.85f, 1.15f);
            wisp.Age = 0f;
            wisp.Element.style.display = DisplayStyle.Flex;
            Place(wisp, 0f);
        }

        public void Tick(float dt)
        {
            foreach (var wisp in _wisps)
            {
                if (wisp.Age < 0f) { continue; }

                wisp.Age += dt;
                var t = Mathf.Clamp01(wisp.Age / wisp.Duration);
                Place(wisp, t);
                if (t >= 1f)
                {
                    wisp.Age = -1f;
                    wisp.Element.style.display = DisplayStyle.None;
                    Flash();
                }
            }

            if (_flashLeft > 0f)
            {
                _flashLeft -= dt;
                if (_flashLeft <= 0f) { _frame?.RemoveFromClassList("bar--mana-flash"); }
            }
        }

        private void Flash()
        {
            _flashLeft = FlashTime;
            _frame?.AddToClassList("bar--mana-flash");
        }

        /// <summary>
        /// Along a quadratic curve to where the fill ends now, easing in: slow off the body, fast
        /// into the bar, shrinking as it lands so the arrival reads as being absorbed.
        /// </summary>
        private void Place(Wisp wisp, float t)
        {
            var to = Target();
            var mid = (wisp.From + to) * 0.5f + wisp.Bend;
            var eased = t * t;
            var a = Vector2.Lerp(wisp.From, mid, eased);
            var b = Vector2.Lerp(mid, to, eased);
            var point = Vector2.Lerp(a, b, eased);
            var size = Mathf.Lerp(HaloSize, CoreSize, eased);
            var style = wisp.Element.style;
            style.left = point.x - size * 0.5f;
            style.top = point.y - size * 0.5f;
            style.width = size;
            style.height = size;
            style.opacity = t < 0.1f ? t / 0.1f : 1f;
        }

        private Vector2 Target()
        {
            var bar = _fill ?? _frame;
            if (bar == null) { return Vector2.zero; }

            var bounds = bar.worldBound;
            return _layer.WorldToLocal(new Vector2(bounds.xMax, bounds.center.y));
        }
    }
}
