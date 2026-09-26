using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Global scalars for how loud combat feedback is allowed to be.
    ///
    /// They exist from the start rather than being retrofitted, because screen shake and flashes
    /// are the two effects most likely to make a game unplayable for someone, and a system that
    /// cannot be turned down is one that has to be rewritten later. Every one of them can go to
    /// zero, and combat must still be fully readable at zero — the particles and the animation
    /// carry the information, the camera only emphasises it.
    ///
    /// Static because time scale and the camera are global by nature; there is nothing per-scene
    /// about "this player does not want screen shake".
    /// </summary>
    public static class FeedbackSettings
    {
        private static float _screenShake = 1f;
        private static float _flash = 1f;
        private static float _hitStop = 1f;

        /// <summary>0 disables camera shake entirely. Combat stays readable without it.</summary>
        public static float ScreenShakeMultiplier
        {
            get => _screenShake;
            set => _screenShake = Mathf.Clamp(value, 0f, 2f);
        }

        /// <summary>0 disables every screen flash.</summary>
        public static float FlashIntensity
        {
            get => _flash;
            set => _flash = Mathf.Clamp01(value);
        }

        /// <summary>
        /// Scales every freeze. Kept separate from the other two because hit-stop is closer to
        /// gameplay feel than to comfort, and someone may want one without the other.
        /// </summary>
        public static float HitStopMultiplier
        {
            get => _hitStop;
            set => _hitStop = Mathf.Clamp(value, 0f, 2f);
        }

        /// <summary>Turns down everything that moves the camera or the whole screen at once.</summary>
        public static bool ReducedMotion
        {
            get => _screenShake <= 0.001f && _flash <= 0.001f;
            set
            {
                if (value)
                {
                    ScreenShakeMultiplier = 0f;
                    FlashIntensity = 0f;
                }
                else
                {
                    ScreenShakeMultiplier = 1f;
                    FlashIntensity = 1f;
                }
            }
        }

        /// <summary>Puts everything back; used by tests and when leaving a session.</summary>
        public static void ResetToDefaults()
        {
            _screenShake = 1f;
            _flash = 1f;
            _hitStop = 1f;
        }
    }
}
