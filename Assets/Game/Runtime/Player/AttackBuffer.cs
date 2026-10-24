using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// How long a pressed attack keeps waiting for its turn (GAME_FEEL_IDEAS.md, item 3).
    ///
    /// Ordinarily the press waits the combat buffer (0.15 s). A dash is one action the press may
    /// fall on the tail of, and it is longer than the buffer: a swing pressed early in that tail
    /// would lapse a step before the dash ends — or a step after, depending on which of the motor
    /// and the character runs first — and the game would eat it. So a press in the dash's last
    /// stretch waits for the dash to end, plus a little grace; one pressed earlier is a press the
    /// player made too soon, and lapses as before.
    /// </summary>
    public static class AttackBuffer
    {
        /// <summary>
        /// When a press made at <paramref name="now"/> stops waiting. <paramref name="dashRemaining"/>
        /// is what is left of a running dash, zero or less without one.
        /// </summary>
        public static float WaitsUntil(float now, float bufferTime, float dashRemaining,
            float dashTailWindow, float dashTailGrace)
        {
            var until = now + Mathf.Max(0f, bufferTime);
            if (dashRemaining > 0f && dashRemaining <= dashTailWindow)
            {
                until = Mathf.Max(until, now + dashRemaining + Mathf.Max(0f, dashTailGrace));
            }

            return until;
        }
    }
}
