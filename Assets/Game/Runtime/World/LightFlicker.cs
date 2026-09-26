using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ForgottenKingdom
{
    /// <summary>
    /// A candle's unsteady light. Noise rather than a sine: a flame that pulses on a beat reads as
    /// a machine. Each light gets its own phase so a row of candles never breathes in unison.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light2D))]
    public sealed class LightFlicker : MonoBehaviour
    {
        [SerializeField, Range(0f, 0.5f)] private float _amount = 0.14f;
        [SerializeField, Min(0.1f)] private float _speed = 3.1f;

        private Light2D _light;
        private float _base;
        private float _seed;

        private void Awake()
        {
            _light = GetComponent<Light2D>();
            _base = _light.intensity;
            _seed = Random.value * 100f;
        }

        private void Update()
        {
            var noise = Mathf.PerlinNoise(_seed, Time.time * _speed) - 0.5f;
            _light.intensity = _base * (1f + noise * 2f * _amount);
        }
    }
}
