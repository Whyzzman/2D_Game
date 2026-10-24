using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgottenKingdom
{
    /// <summary>The curtain's numbers; a field of the HUD so they are tuned without a rebuild.</summary>
    [Serializable]
    public sealed class ZoneCurtainSettings
    {
        [Tooltip("Seconds the fog takes to cross the screen.")]
        [Min(0.1f)] public float duration = 1.25f;
        [Tooltip("Opacity of the thickest part of the near layer; the world is veiled, never hidden.")]
        [Range(0f, 1f)] public float frontOpacity = 0.78f;
        [Tooltip("Opacity of the far layer, which trails the near one for depth.")]
        [Range(0f, 1f)] public float backOpacity = 0.45f;
        [Tooltip("Width of the near layer as a share of the screen.")]
        [Min(0.5f)] public float frontWidth = 1.5f;
        [Tooltip("Width of the far layer as a share of the screen.")]
        [Min(0.5f)] public float backWidth = 2.1f;
        [Tooltip("How much of the run the far layer starts late; it reads as further away.")]
        [Range(0f, 0.5f)] public float backLag = 0.12f;
        [Tooltip("Reduced motion: the plain tinted fade's peak instead of the sweep.")]
        [Range(0f, 1f)] public float reducedMotionOpacity = 0.45f;
    }

    /// <summary>
    /// Crossing into another region (GAME_FEEL_IDEAS.md, item 19, 2026-10-19): a horizontal bank of
    /// fog in the colour of the region ahead sweeps across the world in the direction the hero walks,
    /// two layers at different widths and speeds so it has depth. It draws under the HUD and the
    /// title card and never fully hides the game: a border is still crossed on foot, with no wait.
    /// With reduced motion nothing slides; the screen only breathes the same colour in and out.
    ///
    /// The fog's soft, ragged edges are a texture made here once, white, and tinted per region,
    /// because USS has no gradients and the art generator has no reason to know about it.
    /// </summary>
    public sealed class ZoneCurtain
    {
        private const int TextureWidth = 256;
        private const int TextureHeight = 128;
        private static Texture2D _fog;

        private readonly VisualElement _root;
        private readonly VisualElement _curtain;
        private readonly VisualElement _front;
        private readonly VisualElement _back;
        private readonly ZoneCurtainSettings _settings;
        private float _clock = -1f;
        private int _direction = 1;
        private bool _isReduced;

        public ZoneCurtain(VisualElement root, ZoneCurtainSettings settings)
        {
            _root = root;
            _settings = settings ?? new ZoneCurtainSettings();
            _curtain = root.Q("zone-curtain");
            _front = root.Q("zone-curtain-front");
            _back = root.Q("zone-curtain-back");
            Hide();
        }

        public bool IsPlaying => _clock >= 0f;

        /// <summary>Which way the last sweep went: 1 left to right, -1 right to left.</summary>
        public int Direction => _direction;

        /// <summary>Starts a sweep tinted <paramref name="tint"/>, moving the way the hero walks.</summary>
        public void Play(Color tint, float heading)
        {
            if (_curtain == null || _front == null || _back == null)
            {
                return;
            }

            _direction = heading < 0f ? -1 : 1;
            _isReduced = FeedbackSettings.ReducedMotion;
            _clock = 0f;
            tint.a = 1f;
            _curtain.style.display = DisplayStyle.Flex;
            Dress(_front, tint, _isReduced ? 1f : _settings.frontWidth);
            Dress(_back, tint, _settings.backWidth);
            _back.style.display = _isReduced ? DisplayStyle.None : DisplayStyle.Flex;
            Tick(0f);
        }

        public void Hide()
        {
            _clock = -1f;
            if (_curtain != null)
            {
                _curtain.style.display = DisplayStyle.None;
            }
        }

        public void Tick(float dt)
        {
            if (_clock < 0f)
            {
                return;
            }

            _clock += dt;
            var t = _clock / Mathf.Max(0.1f, _settings.duration);
            if (t >= 1f)
            {
                Hide();
                return;
            }

            if (_isReduced)
            {
                // The fade is in the colour too; only the movement goes.
                _front.style.translate = new Translate(0f, 0f);
                _front.style.opacity = Mathf.Sin(t * Mathf.PI) * _settings.reducedMotionOpacity;
                return;
            }

            var screen = _root.resolvedStyle.width;
            if (float.IsNaN(screen) || screen <= 0f)
            {
                return;
            }

            Place(_front, t, screen, _settings.frontWidth, _settings.frontOpacity);
            var lagged = Mathf.Clamp01((t - _settings.backLag) / Mathf.Max(0.01f, 1f - _settings.backLag));
            Place(_back, lagged, screen, _settings.backWidth, _settings.backOpacity);
        }

        /// <summary>
        /// Slides a layer from wholly off one side to wholly off the other, eased so it drifts in,
        /// crosses the middle quickly and lingers going out, like a bank of mist on the wind.
        /// </summary>
        private void Place(VisualElement layer, float t, float screen, float widthShare, float opacity)
        {
            var width = screen * widthShare;
            var eased = Mathf.SmoothStep(0f, 1f, t);
            var x = Mathf.Lerp(-width, screen, eased);
            if (_direction < 0)
            {
                x = screen - width - x;
            }

            layer.style.translate = new Translate(x, 0f);
            layer.style.opacity = opacity;
        }

        private void Dress(VisualElement layer, Color tint, float widthShare)
        {
            layer.style.width = Length.Percent(widthShare * 100f);
            if (_isReduced)
            {
                layer.style.backgroundImage = new StyleBackground(StyleKeyword.None);
                layer.style.backgroundColor = tint;
                return;
            }

            layer.style.backgroundColor = new Color(0f, 0f, 0f, 0f);
            layer.style.backgroundImage = new StyleBackground(Fog());
            layer.style.unityBackgroundImageTintColor = tint;
        }

        /// <summary>
        /// White fog with soft sides and wisps: dense in the middle, fraying towards both edges, the
        /// fraying different on every row so the edge is a ragged bank, not a ruler line.
        /// </summary>
        private static Texture2D Fog()
        {
            if (_fog != null)
            {
                return _fog;
            }

            _fog = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, false)
            {
                name = "ZoneCurtainFog",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color32[TextureWidth * TextureHeight];
            for (var y = 0; y < TextureHeight; y++)
            {
                var v = y / (float)(TextureHeight - 1);
                for (var x = 0; x < TextureWidth; x++)
                {
                    var u = x / (float)(TextureWidth - 1);
                    // Where this row's edge frays: a slow wave plus finer wisps.
                    var ragged = Mathf.PerlinNoise(u * 3f, v * 2.2f) * 0.6f + Mathf.PerlinNoise(u * 9f + 17f, v * 7f) * 0.4f;
                    var body = Mathf.Pow(Mathf.Sin(u * Mathf.PI), 0.8f);
                    var alpha = Mathf.Clamp01(body * (0.55f + ragged * 0.75f) - 0.08f);
                    // Thinner at the very top and bottom, so the bank has no hard lid.
                    alpha *= Mathf.Lerp(0.7f, 1f, Mathf.Sin(v * Mathf.PI));
                    pixels[y * TextureWidth + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            _fog.SetPixels32(pixels);
            _fog.Apply(false, true);
            return _fog;
        }
    }
}
