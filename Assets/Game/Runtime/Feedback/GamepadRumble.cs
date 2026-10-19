using UnityEngine;
using UnityEngine.InputSystem;

namespace ForgottenKingdom
{
    /// <summary>
    /// The gamepad's rumble for combat moments (GAME_FEEL_IDEAS.md, item 6, 2026-10-19): the
    /// <see cref="FeedbackProfile.Haptic"/> hook that sat unread since PHASE 20. Like the shake, a
    /// weaker pulse while a stronger one runs is ignored rather than added. The heavy (low) motor
    /// carries the weight and the light (high) one the bite, at half. Real time, so a hit-stop does
    /// not hold the motors on; stopped on pause, on a respawn and when the service goes away.
    /// Scaled by <see cref="FeedbackSettings.HapticsMultiplier"/>; at zero the motors are never touched.
    /// </summary>
    public sealed class GamepadRumble
    {
        /// <summary>Length of a pulse: a tap for a light hit, a long shudder for a boss's fall.</summary>
        public static float DurationFor(float strength) => 0.06f + 0.22f * Mathf.Clamp01(strength);

        private float _strength;
        private float _until;
        private bool _running;

        /// <summary>The pulse now running, for tools and tests; 0 when still.</summary>
        public float Strength => _running ? _strength : 0f;

        public void Pulse(float haptic)
        {
            var strength = Mathf.Clamp01(haptic) * FeedbackSettings.HapticsMultiplier;
            if (strength <= 0.001f || (_running && strength <= _strength))
            {
                return;
            }

            _strength = strength;
            _until = Time.unscaledTime + DurationFor(strength);
            _running = true;
            Gamepad.current?.SetMotorSpeeds(strength, strength * 0.5f);
        }

        public void Tick()
        {
            if (_running && (Time.unscaledTime >= _until || GamePause.IsPaused))
            {
                Stop();
            }
        }

        public void Stop()
        {
            if (!_running)
            {
                return;
            }

            _running = false;
            _strength = 0f;
            Gamepad.current?.SetMotorSpeeds(0f, 0f);
        }
    }
}
