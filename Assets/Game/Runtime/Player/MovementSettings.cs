using System;
using UnityEngine;

namespace ForgottenKingdom
{
    [Serializable]
    public sealed class MovementSettings
    {
        [Tooltip("Base for the slow drift allowed during attacks, blocks, casts and weapon swaps. "
            + "There is no walking gait: on the ground the player always runs.")]
        [SerializeField, Min(0f)] private float _walkSpeed = 3f;
        [Tooltip("Ground speed at full stick or a held key; a half-tilted stick moves at half of it.")]
        [SerializeField, Min(0f)] private float _runSpeed = 6.5f;
        [SerializeField, Min(0f)] private float _groundAcceleration = 60f;
        [SerializeField, Min(0f)] private float _groundDeceleration = 80f;
        [SerializeField, Min(0f)] private float _airAcceleration = 30f;
        [SerializeField, Min(0f)] private float _airDeceleration = 20f;
        [SerializeField, Min(0f)] private float _jumpSpeed = 11f;
        [SerializeField, Min(0f)] private float _riseGravity = 30f;
        [SerializeField, Min(0f)] private float _fallGravity = 48f;
        [SerializeField, Min(0f)] private float _maxFallSpeed = 20f;
        [Tooltip("Upward speed a released jump is brought down to.")]
        [SerializeField, Min(0f)] private float _releasedJumpSpeed = 4.5f;
        [Tooltip("How fast a released jump sheds speed down to the released speed. The cut used "
            + "to be instant, a jolt; 200 takes two physics steps and adds ~0.12 to a tap.")]
        [SerializeField, Min(0f)] private float _releasedJumpDeceleration = 200f;
        [Tooltip("Below this vertical speed a held jump is at its apex and floats.")]
        [SerializeField, Min(0f)] private float _apexSpeedThreshold = 2f;
        [Tooltip("Share of gravity near the apex while jump is held. 0.5 nearly doubles the hang "
            + "(about 0.1 to 0.2 s) and raises a full jump from 1.91 to 1.97 at 50 Hz.")]
        [SerializeField, Range(0.1f, 1f)] private float _apexGravityMultiplier = 0.5f;
        [SerializeField, Min(0f)] private float _coyoteTime = 0.10f;
        [SerializeField, Min(0f)] private float _jumpBuffer = 0.12f;
        [Tooltip("Extra jumps in the air, once the ability is owned. Gated by progression, not here.")]
        [SerializeField, Min(0)] private int _airJumps = 1;
        [Tooltip("A shade weaker than the ground jump, so the second one reads as a recovery.")]
        [SerializeField, Min(0f)] private float _airJumpSpeed = 9.5f;
        [Tooltip("Upward speed of a pogo off an enemy or spikes. Fixed height: letting go of jump "
            + "does not cut it, as in Hollow Knight.")]
        [SerializeField, Min(0f)] private float _pogoSpeed = 9.5f;

        public float WalkSpeed => _walkSpeed;
        public float RunSpeed => _runSpeed;
        public float GroundAcceleration => _groundAcceleration;
        public float GroundDeceleration => _groundDeceleration;
        public float AirAcceleration => _airAcceleration;
        public float AirDeceleration => _airDeceleration;
        public float JumpSpeed => _jumpSpeed;
        public float RiseGravity => _riseGravity;
        public float FallGravity => _fallGravity;
        public float MaxFallSpeed => _maxFallSpeed;
        public float ReleasedJumpSpeed => _releasedJumpSpeed;
        public float ReleasedJumpDeceleration => _releasedJumpDeceleration;
        public float ApexSpeedThreshold => _apexSpeedThreshold;
        public float ApexGravityMultiplier => _apexGravityMultiplier;
        public float CoyoteTime => _coyoteTime;
        public float JumpBuffer => _jumpBuffer;
        public int AirJumps => _airJumps;
        public float AirJumpSpeed => _airJumpSpeed;
        public float PogoSpeed => _pogoSpeed;
    }
}
