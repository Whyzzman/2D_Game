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
        [SerializeField, Min(0f)] private float _releasedJumpSpeed = 4.5f;
        [SerializeField, Min(0f)] private float _coyoteTime = 0.10f;
        [SerializeField, Min(0f)] private float _jumpBuffer = 0.12f;
        [Tooltip("Extra jumps in the air, once the ability is owned. Gated by progression, not here.")]
        [SerializeField, Min(0)] private int _airJumps = 1;
        [Tooltip("A shade weaker than the ground jump, so the second one reads as a recovery.")]
        [SerializeField, Min(0f)] private float _airJumpSpeed = 9.5f;

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
        public float CoyoteTime => _coyoteTime;
        public float JumpBuffer => _jumpBuffer;
        public int AirJumps => _airJumps;
        public float AirJumpSpeed => _airJumpSpeed;
    }
}
