using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The one place that writes <see cref="Time.timeScale"/>. Pausing, the hit-stop freeze and
    /// the debug slow motion each report their own state here, and the scale is worked out from
    /// all of them at once: a pause or a freeze stops time, otherwise the base scale applies.
    ///
    /// Before this, each of them saved the scale it found and put it back when done. A menu
    /// opened inside a hit-stop's few hundredths of a second saved the frozen zero, and the
    /// hit-stop then restored 1 underneath the open menu — the world ran behind it. Now nothing
    /// restores anything, so no system can undo another's state.
    ///
    /// Static rather than a component: time is global, and enter-play-mode without a domain
    /// reload keeps statics between sessions, which is why it resets itself on load.
    /// </summary>
    public static class GameTimeScale
    {
        private static bool _isPaused;
        private static bool _isFrozen;
        private static float _baseScale = 1f;

        /// <summary>A menu or screen is holding the game still.</summary>
        public static bool IsPaused => _isPaused;

        /// <summary>A hit-stop freeze is running.</summary>
        public static bool IsFrozen => _isFrozen;

        /// <summary>The scale when nothing stops time: 1 in play, lower for debug slow motion.</summary>
        public static float BaseScale => _baseScale;

        public static void SetPaused(bool isPaused)
        {
            _isPaused = isPaused;
            Apply();
        }

        public static void SetFrozen(bool isFrozen)
        {
            _isFrozen = isFrozen;
            Apply();
        }

        public static void SetBaseScale(float scale)
        {
            _baseScale = Mathf.Max(0f, scale);
            Apply();
        }

        /// <summary>Back to normal time; used when a scene starts over from scratch.</summary>
        public static void Reset()
        {
            _isPaused = false;
            _isFrozen = false;
            _baseScale = 1f;
            Apply();
        }

        private static void Apply()
        {
            Time.timeScale = _isPaused || _isFrozen ? 0f : _baseScale;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad()
        {
            _isPaused = false;
            _isFrozen = false;
            _baseScale = 1f;
        }
    }
}
