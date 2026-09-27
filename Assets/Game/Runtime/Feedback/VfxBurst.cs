using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// One short-lived visual burst: a spark, a slash, a splash of flame. Plays its frames once
    /// and hands itself back to the pool.
    ///
    /// Pooled rather than instantiated because these are the most frequent objects in a fight —
    /// every sword hit makes one — and creating and destroying them per swing is the cheapest way
    /// to turn a smooth fight into a stuttering one.
    ///
    /// It runs on unscaled time on purpose. A hit-stop freezes the world to sell the impact; the
    /// impact effect itself must keep playing through that freeze or the hit reads as a hitch.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class VfxBurst : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private UnityEngine.Rendering.Universal.Light2D _light;
        private float _lightPeak;
        private AnimationClipDefinition _clip;
        private float _elapsed;
        private float _duration;
        private VfxPool _pool;
        private Color _fadeColor = Color.white;
        private bool _isLight;

        public bool IsPlaying { get; private set; }

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _light = GetComponentInChildren<UnityEngine.Rendering.Universal.Light2D>(true);
        }

        /// <summary>
        /// Starts a burst. Everything that could have been left over from the previous use is set
        /// here rather than assumed: a pooled object that inherits last time's rotation or tint is
        /// the classic pooling bug, and it looks like a rendering glitch rather than a logic one.
        /// </summary>
        public void Play(FeedbackProfile profile, Vector2 position, Vector2 direction,
            float scale, VfxPool pool) => Play(profile, position, direction, scale, pool, Color.white);

        /// <summary>The same, tinted: one pale burst serves every element of magic.</summary>
        public void Play(FeedbackProfile profile, Vector2 position, Vector2 direction,
            float scale, VfxPool pool, Color tint)
        {
            _pool = pool;
            _clip = profile.Burst;
            _isLight = false;
            _elapsed = 0f;
            _duration = _clip != null ? _clip.Duration : 0f;
            IsPlaying = _duration > 0f;

            transform.position = position;
            var angle = profile.FacesHitDirection && !profile.MirrorsToDirection
                && direction.sqrMagnitude > 0.0001f
                ? Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg
                : 0f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
            var size = Mathf.Max(0.01f, profile.Scale * scale);
            var mirror = profile.MirrorsToDirection && direction.x < 0f ? -1f : 1f;
            transform.localScale = new Vector3(size * mirror, size, size);

            _renderer.enabled = true;
            _renderer.color = profile.Tint * tint;
            _renderer.sortingLayerName = profile.SortingLayer;
            _renderer.sortingOrder = profile.SortingOrder;
            _renderer.sprite = _clip != null ? _clip.FrameAt(0f) : null;
            if (!IsPlaying)
            {
                Recycle();
            }
        }

        /// <summary>
        /// Starts a fading light pulse instead of a frame sequence. Lights are the most expensive
        /// effect here and the easiest to leak, so they run through the same pooled object and the
        /// same lifetime as everything else rather than getting a system of their own.
        /// </summary>
        public void PlayLight(Sprite sprite, Color color, float radius, float duration,
            Vector2 position, VfxPool pool)
        {
            _pool = pool;
            _clip = null;
            _elapsed = 0f;
            _duration = Mathf.Max(0.01f, duration);
            _fadeColor = color;
            _isLight = true;
            IsPlaying = true;

            transform.position = position;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one * Mathf.Max(0.1f, radius);
            _renderer.enabled = true;
            _renderer.sprite = sprite;
            _renderer.color = color;
            _renderer.sortingLayerName = "VFXBack";
            _renderer.sortingOrder = 0;
            if (_light != null)
            {
                // The light lives at the burst's scale of one; its reach is set in world units.
                _light.transform.localScale = Vector3.one / Mathf.Max(0.1f, radius);
                _light.color = new Color(color.r, color.g, color.b, 1f);
                _light.pointLightOuterRadius = Mathf.Max(0.5f, radius * 1.7f);
                _light.pointLightInnerRadius = 0f;
                _lightPeak = 1.8f * Mathf.Clamp01(color.a + 0.4f);
                _light.intensity = _lightPeak;
                _light.enabled = true;
            }
        }

        private void LateUpdate()
        {
            if (!IsPlaying)
            {
                return;
            }

            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed >= _duration)
            {
                Recycle();
                return;
            }

            if (_isLight)
            {
                // A pulse is brightest at birth and gone by the end; it never lingers as a
                // stray light nobody remembers spawning.
                var remaining = 1f - _elapsed / _duration;
                _renderer.color = new Color(_fadeColor.r, _fadeColor.g, _fadeColor.b,
                    _fadeColor.a * remaining);
                if (_light != null)
                {
                    _light.intensity = _lightPeak * remaining * remaining;
                }

                return;
            }

            var frame = _clip.FrameAt(_elapsed);
            if (frame != null)
            {
                _renderer.sprite = frame;
            }
        }

        private void Recycle()
        {
            IsPlaying = false;
            _renderer.enabled = false;
            if (_light != null)
            {
                _light.enabled = false;
            }

            _clip = null;
            if (_pool != null)
            {
                _pool.Return(this);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>Stops immediately without returning; used when the world is being cleared.</summary>
        public void Cancel()
        {
            IsPlaying = false;
            _clip = null;
            if (_renderer != null)
            {
                _renderer.enabled = false;
            }

            if (_light != null)
            {
                _light.enabled = false;
            }
        }
    }
}
