using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ForgottenKingdom
{
    /// <summary>
    /// A candle, lantern or brazier flame that gutters when the hero dashes past it
    /// (GAME_FEEL_IDEAS item 13): the light drops to a quarter, flickers and climbs back over a
    /// second and a bit. It never goes out for good and never darkens a fight for long — the
    /// world noticed the hero, that is all. Altar fires are left alone: they belong to the hero.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light2D))]
    public sealed class LightGust : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float _radius = 2.6f;
        [SerializeField, Range(0f, 1f)] private float _dip = 0.25f;
        [SerializeField, Min(0.1f)] private float _recovery = 1.2f;

        private Light2D _light;
        private float _base;
        private float _since = float.MaxValue;
        private int _seed;

        /// <summary>True while the flame is recovering from a gust.</summary>
        public bool IsGuttering => _since < _recovery;

        private void Awake()
        {
            _light = GetComponent<Light2D>();
            _base = _light.intensity;
            _seed = Mathf.Abs(GetEntityId().GetHashCode()) % 97;
        }

        private void OnEnable()
        {
            HeroSignals.Attach();
            HeroSignals.Dashed += OnDashed;
        }

        private void OnDisable()
        {
            HeroSignals.Dashed -= OnDashed;
            _since = float.MaxValue;
            if (_light != null) { _light.intensity = _base; }
        }

        private void OnDashed(Vector2 at, float facing)
        {
            if (Vector2.Distance(at, transform.position) <= _radius)
            {
                _since = 0f;
            }
        }

        private void Update()
        {
            // A zone can stream in before the hero exists; the search is throttled to once a second.
            if (HeroSignals.Hero == null) { HeroSignals.Attach(); }
            if (_since >= _recovery)
            {
                return;
            }

            _since += Time.deltaTime;
            var t = Mathf.Clamp01(_since / _recovery);
            // Down at once, then back with a flicker that calms as the flame takes hold again.
            var flicker = (Mathf.PerlinNoise(_seed, _since * 14f) - 0.5f) * 0.5f * (1f - t);
            var share = Mathf.Lerp(_dip, 1f, t * t) + flicker;
            _light.intensity = _base * Mathf.Clamp(share, 0f, 1.1f);
            if (_since >= _recovery) { _light.intensity = _base; }
        }
    }
}
