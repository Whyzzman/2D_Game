using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgottenKingdom
{
    /// <summary>The numbers of the low-health cue; serialized on the HUD so they can be tuned.</summary>
    [Serializable]
    public sealed class LowHealthSettings
    {
        [Tooltip("Share of health under which the world starts to pale and the heart is heard.")]
        [SerializeField, Range(0.05f, 0.6f)] private float _threshold = 0.25f;
        [SerializeField, Min(0.05f)] private float _fadeIn = 0.8f;
        [SerializeField, Min(0.05f)] private float _fadeOut = 1.2f;
        [Tooltip("Opacity of the pale edge at full strength. Gentle: it is felt, not read.")]
        [SerializeField, Range(0f, 1f)] private float _edgeStrength = 0.55f;
        [Tooltip("Seconds between heartbeats.")]
        [SerializeField, Min(0.3f)] private float _beatPeriod = 0.95f;
        [Tooltip("How loud a heartbeat is at full strength; 0 leaves only the picture.")]
        [SerializeField, Range(0f, 1f)] private float _beatVolume = 0.55f;
        [Tooltip("How much the edge swells on each beat; none with reduced motion.")]
        [SerializeField, Range(0f, 0.5f)] private float _pulseDepth = 0.18f;
        [SerializeField, Min(0.05f)] private float _pulseDecay = 0.35f;

        public float Threshold => _threshold;
        public float FadeIn => _fadeIn;
        public float FadeOut => _fadeOut;
        public float EdgeStrength => _edgeStrength;
        public float BeatPeriod => _beatPeriod;
        public float BeatVolume => _beatVolume;
        public float PulseDepth => _pulseDepth;
        public float PulseDecay => _pulseDecay;
    }

    /// <summary>
    /// Low health felt in the world, not only on the bar (GAME_FEEL_IDEAS item 17): under a quarter
    /// of health the edges of the screen go pale and cold, as if colour drained from them, and a
    /// quiet heartbeat is heard. Both ease in, and ease out when health rises again or the hero
    /// dies. The edge swells a little with each beat; with reduced motion it holds still.
    ///
    /// A HUD overlay rather than a post-process: it costs one element, it works with the picture
    /// effects switched off, and nothing in the renderer has to know about health. Its texture is
    /// a pale radial edge made once in code, so no asset or scene changes are needed. Owned and
    /// ticked by <see cref="HudController"/>; the sound goes through <see cref="AudioManager"/>.
    /// </summary>
    public sealed class LowHealthCue
    {
        /// <summary>The cold grey the edge drains to.</summary>
        private static readonly Color EdgeColor = new Color(0.62f, 0.64f, 0.68f, 1f);

        private static Texture2D _edgeTexture;

        private readonly LowHealthSettings _settings;
        private readonly VisualElement _veil;
        private float _amount;
        private float _beatClock;
        private float _pulse;
        private float _writtenOpacity = -1f;

        /// <summary>Raised on each heartbeat with its volume; the HUD turns it into a sound.</summary>
        public event Action<float> Beat;

        /// <param name="root">The HUD's tree; null runs the rule without a picture (tests).</param>
        public LowHealthCue(VisualElement root, LowHealthSettings settings)
        {
            _settings = settings ?? new LowHealthSettings();
            _beatClock = _settings.BeatPeriod * 0.5f;
            if (root != null)
            {
                _veil = CreateVeil(root);
            }
        }

        /// <summary>How far the cue is in, 0..1.</summary>
        public float Amount => _amount;

        /// <summary>The edge's opacity right now.</summary>
        public float Opacity { get; private set; }

        /// <summary>The overlay element, for tests.</summary>
        public VisualElement Veil => _veil;

        /// <param name="dt">Unscaled seconds: a hit-stop must not stop the heart.</param>
        public void Tick(float dt, float healthShare, bool isAlive)
        {
            var wanted = isAlive && healthShare > 0f && healthShare < _settings.Threshold;
            var target = wanted ? 1f : 0f;
            _amount = Mathf.MoveTowards(_amount, target,
                dt / (target > _amount ? _settings.FadeIn : _settings.FadeOut));

            _pulse = Mathf.Max(0f, _pulse - dt / _settings.PulseDecay);
            if (wanted && !GameTimeScale.IsPaused)
            {
                _beatClock += dt;
                if (_beatClock >= _settings.BeatPeriod)
                {
                    _beatClock = 0f;
                    _pulse = 1f;
                    if (_settings.BeatVolume > 0f)
                    {
                        Beat?.Invoke(_settings.BeatVolume * _amount);
                    }
                }
            }
            else
            {
                // The first beat of a new low comes a moment after the edge starts to pale.
                _beatClock = _settings.BeatPeriod * 0.5f;
            }

            var swell = FeedbackSettings.ReducedMotion
                ? 1f - _settings.PulseDepth * 0.5f
                : 1f - _settings.PulseDepth + _settings.PulseDepth * _pulse;
            Opacity = _amount * _settings.EdgeStrength * swell;
            Write();
        }

        /// <summary>Gone at once: death's own screen or a world reset takes over.</summary>
        public void Clear()
        {
            _amount = 0f;
            _pulse = 0f;
            Opacity = 0f;
            Write();
        }

        private void Write()
        {
            if (_veil == null || Mathf.Abs(Opacity - _writtenOpacity) < 0.002f)
            {
                return;
            }

            _writtenOpacity = Opacity;
            _veil.style.opacity = Opacity;
            _veil.style.display = Opacity > 0.001f ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>A full-screen element just over the HUD's dark vignette, under everything else.</summary>
        private static VisualElement CreateVeil(VisualElement root)
        {
            var veil = new VisualElement { name = "low-health", pickingMode = PickingMode.Ignore };
            veil.style.position = Position.Absolute;
            veil.style.left = 0f;
            veil.style.top = 0f;
            veil.style.right = 0f;
            veil.style.bottom = 0f;
            veil.style.opacity = 0f;
            veil.style.display = DisplayStyle.None;
            veil.style.backgroundImage = new StyleBackground(Background.FromTexture2D(EdgeTexture()));
            veil.style.backgroundSize = new BackgroundSize(Length.Percent(100f), Length.Percent(100f));

            var vignette = root.Q("vignette");
            var parent = vignette?.parent ?? root.Q("hud") ?? root;
            var index = vignette != null ? parent.IndexOf(vignette) + 1 : 0;
            parent.Insert(index, veil);
            return veil;
        }

        /// <summary>Pale at the edges, clear in the middle; made once and shared.</summary>
        private static Texture2D EdgeTexture()
        {
            if (_edgeTexture != null)
            {
                return _edgeTexture;
            }

            const int width = 128;
            const int height = 72;
            var pixels = new Color[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var dx = (x + 0.5f) / width * 2f - 1f;
                    var dy = (y + 0.5f) / height * 2f - 1f;
                    var distance = Mathf.Sqrt(dx * dx * 0.85f + dy * dy * 1.15f);
                    var alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1.35f, distance));
                    pixels[y * width + x] = new Color(EdgeColor.r, EdgeColor.g, EdgeColor.b, alpha * 0.8f);
                }
            }

            _edgeTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "low_health_edge",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };
            _edgeTexture.SetPixels(pixels);
            _edgeTexture.Apply(false, true);
            return _edgeTexture;
        }
    }
}
