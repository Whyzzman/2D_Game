using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// A run of hits climbing in pitch (GAME_FEEL_IDEAS.md, item 8, 2026-10-19): each landed blow
    /// within <see cref="Window"/> seconds of the last sounds a semitone higher, up to
    /// <see cref="MaxSteps"/>, so a combo is heard as rising rather than as the same sound again.
    /// A pause, or the hero being hit, starts it over. Real time, so a hit-stop does not break it.
    /// Only the pitch moves; which sound plays stays the binder's choice.
    /// </summary>
    public sealed class HitStreak
    {
        /// <summary>The longest gap between two hits that still counts as one run.</summary>
        public const float Window = 1f;
        public const int MaxSteps = 4;

        private int _step = -1;
        private float _last = float.NegativeInfinity;

        /// <summary>How many semitones up the current hit sits (0 for the first of a run).</summary>
        public int Step => Mathf.Max(0, _step);

        /// <summary>A landed hit at <paramref name="now"/>; returns the pitch to play it at.</summary>
        public float Register(float now)
        {
            _step = now - _last <= Window ? Mathf.Min(_step + 1, MaxSteps) : 0;
            _last = now;
            return PitchFor(_step);
        }

        /// <summary>
        /// The pitch for one combat moment as the binder plays it: landed blows climb the run, the
        /// hero being hurt starts it over, everything else plays as authored.
        /// </summary>
        public float PitchFor(FeedbackKind kind, float now)
        {
            switch (kind)
            {
                case FeedbackKind.LightHit:
                case FeedbackKind.HeavyHit:
                case FeedbackKind.CriticalHit:
                    return Register(now);
                case FeedbackKind.PlayerHurt:
                    Break();
                    return 1f;
                default:
                    return 1f;
            }
        }

        public void Break()
        {
            _step = -1;
            _last = float.NegativeInfinity;
        }

        /// <summary>Equal temperament: twelve steps to the octave.</summary>
        public static float PitchFor(int semitones) => Mathf.Pow(2f, semitones / 12f);
    }
}
