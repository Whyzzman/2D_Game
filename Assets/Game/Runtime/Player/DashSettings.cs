using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Dash tuning. It is the first of the traversal abilities, so it is configured separately from
    /// the base locomotion instead of being folded into <see cref="MovementSettings"/>.
    /// </summary>
    [Serializable]
    public sealed class DashSettings
    {
        [SerializeField, Min(0f)] private float _dashSpeed = 16f;
        [SerializeField, Min(0.01f)] private float _dashDuration = 0.18f;
        [SerializeField, Min(0f)] private float _cooldown = 0.35f;
        [SerializeField, Min(0f)] private float _staminaCost = 20f;
        [Tooltip("Delay from the dash start before the invincibility window opens.")]
        [SerializeField, Min(0f)] private float _invincibilityDelay = 0.02f;
        [Tooltip("How long the invincibility lasts; shorter than the dash, so its end is punishable.")]
        [SerializeField, Min(0f)] private float _invincibilityDuration = 0.14f;
        [Tooltip("Share of dash speed kept when the dash ends, so the exit is not an abrupt stop.")]
        [SerializeField, Range(0f, 1f)] private float _exitSpeedFraction = 0.35f;
        [Tooltip("Dashes allowed while airborne before touching the ground again.")]
        [SerializeField, Min(0)] private int _airDashes = 1;

        public float DashSpeed => _dashSpeed;
        public float DashDuration => _dashDuration;
        public float Cooldown => _cooldown;
        public float StaminaCost => _staminaCost;
        public float InvincibilityDelay => _invincibilityDelay;
        public float InvincibilityDuration => _invincibilityDuration;
        public float ExitSpeedFraction => _exitSpeedFraction;
        public int AirDashes => _airDashes;
    }
}
