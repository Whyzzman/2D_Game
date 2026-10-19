using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Coins picked up one after another ring higher and higher (GAME_FEEL_IDEAS.md, item 15,
    /// 2026-10-19): each coin within <see cref="Window"/> seconds of the last sounds
    /// <see cref="StepSemitones"/> above it, up to <see cref="MaxSteps"/> steps, and a pause starts
    /// over from the authored pitch. A scattered purse sweeping in becomes a little run up a scale.
    /// Real time, so the pause menu cannot break a run in two. Only the pitch moves.
    /// </summary>
    public sealed class CoinChime
    {
        private int _step = -1;
        private float _last = float.NegativeInfinity;

        public CoinChime(float window, int stepSemitones, int maxSteps)
        {
            Window = Mathf.Max(0f, window);
            StepSemitones = Mathf.Max(0, stepSemitones);
            MaxSteps = Mathf.Max(0, maxSteps);
        }

        /// <summary>The longest gap between two coins that still counts as one run.</summary>
        public float Window { get; }
        public int StepSemitones { get; }
        public int MaxSteps { get; }

        /// <summary>How many steps up the latest coin sat (0 for the first of a run).</summary>
        public int Step => Mathf.Max(0, _step);

        /// <summary>A coin taken at <paramref name="now"/>; returns the pitch to ring it at.</summary>
        public float Register(float now)
        {
            _step = now - _last <= Window ? Mathf.Min(_step + 1, MaxSteps) : 0;
            _last = now;
            return Mathf.Pow(2f, _step * StepSemitones / 12f);
        }
    }
}
