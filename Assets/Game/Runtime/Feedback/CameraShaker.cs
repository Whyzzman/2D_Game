using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>How hard the camera is kicked. Named tiers rather than raw numbers per call site.</summary>
    public enum ShakeProfile
    {
        None,
        Tiny,
        Small,
        Medium,
        Large,
        BossLarge
    }

    /// <summary>
    /// The one place the camera is allowed to shake.
    ///
    /// Two rules make it safe. Overlapping shakes take the strongest rather than adding up, so a
    /// five-target spell cannot multiply into something unplayable. And the offset is clamped, so
    /// even a bug upstream cannot throw the view off the player.
    ///
    /// It never owns the camera's position — <see cref="CameraFollow2D"/> still decides where the
    /// camera is, and asks this for an offset. That ordering is explicit instead of depending on
    /// which component Unity happens to update first.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CameraShaker : MonoBehaviour
    {
        [Tooltip("Hard cap on displacement in world units, whatever is requested.")]
        [SerializeField, Min(0f)] private float _maximumOffset = 0.85f;
        [SerializeField, Min(0.01f)] private float _frequency = 28f;
        private float _amplitude;
        private float _decayPerSecond;
        private float _verticalBias = 1f;
        private float _seed;

        public Vector2 CurrentOffset { get; private set; }

        /// <summary>The strongest shake currently running, for the debug readout.</summary>
        public float CurrentAmplitude => _amplitude;

        private void Awake() => _seed = Random.value * 100f;

        /// <summary>
        /// Requests a shake. A weaker request while a stronger one is still running is ignored
        /// rather than added: that is what keeps a burst of small hits from compounding.
        /// </summary>
        public void Shake(ShakeProfile profile, float scale = 1f, float verticalBias = 1f)
        {
            var (amplitude, duration) = Tier(profile);
            amplitude *= Mathf.Max(0f, scale) * FeedbackSettings.ScreenShakeMultiplier;
            if (amplitude <= 0.0001f || duration <= 0f)
            {
                return;
            }

            if (amplitude <= _amplitude)
            {
                return;
            }

            _amplitude = Mathf.Min(amplitude, _maximumOffset);
            _decayPerSecond = _amplitude / duration;
            _verticalBias = Mathf.Max(0f, verticalBias);
        }

        /// <summary>
        /// The tiers. A slash is a nudge; a boss slam is the largest thing in the game and still
        /// small enough that the player never loses track of where they are.
        /// </summary>
        private static (float amplitude, float duration) Tier(ShakeProfile profile)
        {
            switch (profile)
            {
                case ShakeProfile.Tiny: return (0.035f, 0.07f);
                case ShakeProfile.Small: return (0.075f, 0.11f);
                case ShakeProfile.Medium: return (0.16f, 0.18f);
                case ShakeProfile.Large: return (0.3f, 0.26f);
                case ShakeProfile.BossLarge: return (0.5f, 0.38f);
                default: return (0f, 0f);
            }
        }

        private void LateUpdate()
        {
            if (_amplitude <= 0f)
            {
                CurrentOffset = Vector2.zero;
                return;
            }

            // Unscaled, so a shake still plays out during a hit-stop freeze instead of hanging.
            _amplitude = Mathf.Max(0f, _amplitude - _decayPerSecond * Time.unscaledDeltaTime);
            var time = Time.unscaledTime * _frequency;
            var x = (Mathf.PerlinNoise(_seed, time) - 0.5f) * 2f;
            var y = (Mathf.PerlinNoise(_seed + 37f, time) - 0.5f) * 2f * _verticalBias;
            CurrentOffset = Vector2.ClampMagnitude(new Vector2(x, y) * _amplitude, _maximumOffset);
        }

        /// <summary>Drops any shake instantly; used on death, respawn and scene changes.</summary>
        public void Clear()
        {
            _amplitude = 0f;
            _decayPerSecond = 0f;
            CurrentOffset = Vector2.zero;
        }

        private void OnDisable() => Clear();
    }
}
