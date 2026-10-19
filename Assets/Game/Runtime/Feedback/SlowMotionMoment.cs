using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// A short stretch of slowed world time, borrowed from <see cref="GameTimeScale"/>: a parry
    /// lets the world drift at a third of its speed for a breath after the freeze
    /// (GAME_FEEL_IDEAS item 9), so the moment is felt with the whole body, not only seen.
    ///
    /// The base scale is borrowed, never owned: the slow starts only when nobody else holds the
    /// world slow (a boss's death, the debug slow motion), and it is handed back only if it is
    /// still the one this moment set. The clock is real time and does not run while a hit-stop
    /// freeze or a menu holds the world still, so the slow follows the freeze instead of hiding
    /// under it. Reduced motion turns it off; the hit-stop setting scales its length, to nothing
    /// at zero.
    ///
    /// A plain class, owned and ticked by <see cref="CombatFeedbackService"/>.
    /// </summary>
    public sealed class SlowMotionMoment
    {
        private float _scale = 1f;
        private float _remaining;
        private bool _isSlowing;

        /// <summary>True while this moment holds the world slow.</summary>
        public bool IsRunning => _isSlowing;

        /// <param name="scale">The world's speed meanwhile, 0..1.</param>
        /// <param name="seconds">Real seconds at full hit-stop setting.</param>
        public void Begin(float scale, float seconds)
        {
            if (FeedbackSettings.ReducedMotion)
            {
                return;
            }

            var length = seconds * FeedbackSettings.HitStopMultiplier;
            if (length <= 0.001f || scale >= 0.999f)
            {
                return;
            }

            if (_isSlowing)
            {
                // A second parry inside the first extends it rather than stacking.
                _remaining = Mathf.Max(_remaining, length);
                return;
            }

            if (GameTimeScale.BaseScale < 0.999f)
            {
                // Someone else already holds the world slow; theirs is the moment.
                return;
            }

            _scale = Mathf.Clamp(scale, 0.05f, 1f);
            _remaining = length;
            _isSlowing = true;
            GameTimeScale.SetBaseScale(_scale);
        }

        /// <param name="realDeltaTime">Unscaled seconds: the moment runs on the clock it slows.</param>
        public void Tick(float realDeltaTime)
        {
            if (!_isSlowing || GameTimeScale.IsPaused || GameTimeScale.IsFrozen)
            {
                return;
            }

            _remaining -= realDeltaTime;
            if (_remaining <= 0f)
            {
                Stop();
            }
        }

        /// <summary>Ends the slow now; the base scale goes back only if it is still ours.</summary>
        public void Stop()
        {
            if (!_isSlowing)
            {
                return;
            }

            _isSlowing = false;
            _remaining = 0f;
            if (Mathf.Approximately(GameTimeScale.BaseScale, _scale))
            {
                GameTimeScale.SetBaseScale(1f);
            }
        }
    }
}
