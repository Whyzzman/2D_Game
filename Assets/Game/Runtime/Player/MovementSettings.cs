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
        [SerializeField, Min(0f)] private float _runSpeed = 6.8f;
        [SerializeField, Min(0f)] private float _groundAcceleration = 150f;
        [SerializeField, Min(0f)] private float _groundDeceleration = 180f;
        [SerializeField, Min(0f)] private float _airAcceleration = 120f;
        // Starting, stopping and steering are all but instant on the ground and in the air, as in
        // Hollow Knight (user's request 2026-10-27, «the hero's mechanics like Hollow Knight»): a full
        // run is reached in about 0.04 s (250 was tried: LevelReach then stranded a Tomb floor — the
        // model's jumps need a run-up to differ from a standing start). Heights and gaps are untouched —
        // the jump speed, gravities and the apex stay, the levels are built on them.
        [Tooltip("Braking in the air with no direction held: 120 stops a full run in about 0.05 s; "
            + "it can only shorten a jump, never lengthen it.")]
        [SerializeField, Min(0f)] private float _airDeceleration = 120f;
        [Tooltip("Steering against the current drift in the air: a reversal is three times as quick "
            + "as speeding up, as in Hollow Knight, so a jump can be corrected mid-air. Speeding up "
            + "the same way stays at the air acceleration, so no gap gets wider.")]
        [SerializeField, Min(0f)] private float _airTurnAcceleration = 250f;
        [SerializeField, Min(0f)] private float _jumpSpeed = 11f;
        [SerializeField, Min(0f)] private float _riseGravity = 30f;
        [SerializeField, Min(0f)] private float _fallGravity = 48f;
        [SerializeField, Min(0f)] private float _maxFallSpeed = 20f;
        [Tooltip("Upward speed a released jump is brought down to.")]
        [SerializeField, Min(0f)] private float _releasedJumpSpeed = 1.5f;
        [Tooltip("How fast a released jump sheds speed down to the released speed. 600 cuts it within "
            + "a step or two, as in Hollow Knight: let go and the rise ends where it is.")]
        [SerializeField, Min(0f)] private float _releasedJumpDeceleration = 600f;
        [Tooltip("Below this vertical speed a held jump is at its apex and floats.")]
        [SerializeField, Min(0f)] private float _apexSpeedThreshold = 2f;
        [Tooltip("Share of gravity near the apex while jump is held. 0.5 nearly doubles the hang "
            + "(about 0.1 to 0.2 s) and raises a full jump from 1.91 to 1.97 at 50 Hz.")]
        [SerializeField, Range(0.1f, 1f)] private float _apexGravityMultiplier = 0.5f;
        [SerializeField, Min(0f)] private float _coyoteTime = 0.10f;
        [Tooltip("A press this long before landing, or before a dash ends, still jumps.")]
        [SerializeField, Min(0f)] private float _jumpBuffer = 0.15f;
        [Tooltip("Extra jumps in the air, once the ability is owned. Gated by progression, not here.")]
        [SerializeField, Min(0)] private int _airJumps = 1;
        [Tooltip("A shade weaker than the ground jump, so the second one reads as a recovery.")]
        [SerializeField, Min(0f)] private float _airJumpSpeed = 9.5f;
        [Tooltip("Upward speed of a pogo off an enemy or spikes. Fixed height: letting go of jump "
            + "does not cut it, as in Hollow Knight.")]
        [SerializeField, Min(0f)] private float _pogoSpeed = 9.5f;
        [Tooltip("How far the feet may miss a ledge's corner sideways, falling toward it, and still "
            + "be nudged onto it (GAME_FEEL_IDEAS.md, item 2). Measured from the body's side; zero "
            + "turns it off.")]
        [SerializeField, Min(0f)] private float _ledgeForgiveness = 0.1f;

        public float WalkSpeed => _walkSpeed;
        public float RunSpeed => _runSpeed;
        public float GroundAcceleration => _groundAcceleration;
        public float GroundDeceleration => _groundDeceleration;
        public float AirAcceleration => _airAcceleration;
        public float AirDeceleration => _airDeceleration;
        public float AirTurnAcceleration => _airTurnAcceleration;
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
        public float LedgeForgiveness => _ledgeForgiveness;
    }
}
