using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>How the locomotion solver should treat input on this step.</summary>
    public enum LocomotionMode
    {
        Normal,
        /// <summary>
        /// Attacks: the player still drifts at a fraction of normal speed, but cannot jump and
        /// cannot turn around, so the swing stays aimed where it started.
        /// </summary>
        Restricted,
        /// <summary>Rooted actions such as blocking: gravity and braking, but no player input.</summary>
        NoInput,
        /// <summary>Knockback and stun: horizontal velocity is left alone while gravity applies.</summary>
        Knockback
    }

    public sealed class PlayerMovementLogic
    {
        private const float MotionThreshold = 0.01f;
        /// <summary>Below this share of a full push the stick reads as a walk, for animation.</summary>
        private const float HalfTilt = 0.5f;
        /// <summary>Keeps accumulated float error from stretching a timer by one physics step.</summary>
        private const float TimerEpsilon = 0.0001f;
        private readonly MovementSettings _settings;
        private readonly DashSettings _dash;
        private float _coyoteRemaining = -1f;
        private float _bufferRemaining = -1f;
        private bool _awaitingGroundExit;
        private float _dashRemaining;
        private float _dashCooldownRemaining;
        private float _dashElapsed;
        private int _dashDirection = 1;
        private int _airDashesUsed;
        private int _airJumpsUsed;

        public PlayerMovementLogic(MovementSettings settings, DashSettings dash = null)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _dash = dash ?? new DashSettings();
        }

        public MovementState State { get; private set; } = MovementState.Idle;
        public int FacingDirection { get; private set; } = 1;
        public bool IsGrounded { get; private set; }
        public bool IsDashing => _dashRemaining > TimerEpsilon;

        /// <summary>Time since the running dash started; drives its invincibility window.</summary>
        public float DashElapsed => _dashElapsed;

        public int AirDashesUsed => _airDashesUsed;
        public int AirJumpsUsed => _airJumpsUsed;

        /// <summary>
        /// How many jumps the player may take in the air. Zero until the Hollow Warden's sigil is
        /// taken, which is why it is a setting rather than a constant: the solver should not have
        /// to know that progression exists, and progression should not have to know how a jump
        /// works. Raised on every step by the motor from what the save says is owned.
        /// </summary>
        public int AirJumpsAllowed { get; set; }

        /// <summary>True when a second jump is available right now.</summary>
        public bool IsAirJumpReady => !IsGrounded && !IsDashing && _airJumpsUsed < AirJumpsAllowed;

        /// <summary>Raised when a jump is actually taken in the air; the hook for its puff and sound.</summary>
        public event Action AirJumped;

        /// <summary>
        /// A dash is available when none is running, the cooldown has elapsed, and the player is
        /// either on the ground or still has an air dash left.
        /// </summary>
        /// <summary>
        /// Scales the wait between dashes. Trained footwork shortens it; it is a multiplier rather
        /// than a subtraction so no amount of it can reach zero and make dashing free.
        /// </summary>
        public float CooldownScale { get; set; } = 1f;

        public bool IsDashReady => !IsDashing
            && _dashCooldownRemaining <= 0f
            && (IsGrounded || _airDashesUsed < _dash.AirDashes);

        public bool TryBeginDash(int direction)
        {
            if (!IsDashReady)
            {
                return false;
            }

            if (!IsGrounded)
            {
                _airDashesUsed++;
            }

            _dashDirection = direction >= 0 ? 1 : -1;
            FacingDirection = _dashDirection;
            _dashRemaining = _dash.DashDuration;
            _dashElapsed = 0f;
            _dashCooldownRemaining = _dash.DashDuration + _dash.Cooldown * CooldownScale;
            return true;
        }

        public void CancelDash()
        {
            _dashRemaining = 0f;
            _dashElapsed = 0f;
        }

        public Vector2 Step(Vector2 velocity, bool hasGround, MovementInput input, float deltaTime,
            LocomotionMode mode = LocomotionMode.Normal, float movementScale = 1f)
        {
            if (deltaTime <= 0f)
            {
                return velocity;
            }

            _dashCooldownRemaining = Mathf.Max(0f, _dashCooldownRemaining - deltaTime);
            if (!hasGround)
            {
                _awaitingGroundExit = false;
            }

            IsGrounded = hasGround && !_awaitingGroundExit && velocity.y <= MotionThreshold;
            if (IsGrounded)
            {
                // Air dashes and air jumps come back only on a confirmed landing, never on a wall
                // or a slope. Refreshing them on wall contact is what turns a wall into a ladder.
                _airDashesUsed = 0;
                _airJumpsUsed = 0;
            }

            if (IsDashing)
            {
                return StepDash(deltaTime);
            }

            _coyoteRemaining = IsGrounded ? _settings.CoyoteTime : _coyoteRemaining - deltaTime;
            if (mode == LocomotionMode.Restricted)
            {
                // Keep steering, drop the jump: an attack must not be turned into a leap.
                input = new MovementInput(input.Horizontal);
            }
            else if (mode != LocomotionMode.Normal)
            {
                input = default;
            }

            _bufferRemaining = input.IsJumpPressed
                ? _settings.JumpBuffer - Mathf.Max(0f, input.JumpPressAge)
                : _bufferRemaining - deltaTime;

            var horizontal = Mathf.Clamp(input.Horizontal, -1f, 1f);
            var hasDirection = Mathf.Abs(horizontal) > MotionThreshold;
            if (hasDirection && mode != LocomotionMode.Restricted)
            {
                FacingDirection = horizontal > 0f ? 1 : -1;
            }

            if (mode != LocomotionMode.Knockback)
            {
                // The player always runs; how far the stick is pushed still sets the pace. A
                // restricted action drifts from the old walking pace, so an attack or a raised
                // guard keeps the slow creep it was tuned for rather than twice that.
                var speed = mode == LocomotionMode.Restricted
                    ? _settings.WalkSpeed * Mathf.Clamp01(movementScale)
                    : _settings.RunSpeed;
                var acceleration = IsGrounded
                    ? (hasDirection ? _settings.GroundAcceleration : _settings.GroundDeceleration)
                    : (hasDirection ? _settings.AirAcceleration : _settings.AirDeceleration);
                velocity.x = Mathf.MoveTowards(velocity.x, horizontal * speed, acceleration * deltaTime);
            }

            if (_bufferRemaining >= 0f && _coyoteRemaining >= 0f && !_awaitingGroundExit)
            {
                velocity.y = _settings.JumpSpeed;
                _bufferRemaining = -1f;
                _coyoteRemaining = -1f;
                _awaitingGroundExit = true;
                IsGrounded = false;
            }
            else if (input.IsJumpPressed && IsAirJumpReady)
            {
                // Deliberately a fresh press rather than the buffer: a press made just before
                // landing is meant for the landing, and spending the second jump on it would feel
                // like the game stealing it. The upward speed is set, not added, so falling fast
                // does not make the recovery weaker.
                _airJumpsUsed++;
                velocity.y = _settings.AirJumpSpeed;
                _bufferRemaining = -1f;
                AirJumped?.Invoke();
            }

            if (IsGrounded)
            {
                velocity.y = 0f;
            }
            else
            {
                if (!input.IsJumpHeld && velocity.y > _settings.ReleasedJumpSpeed)
                {
                    velocity.y = _settings.ReleasedJumpSpeed;
                }

                var gravity = velocity.y > 0f ? _settings.RiseGravity : _settings.FallGravity;
                velocity.y = Mathf.Max(velocity.y - gravity * deltaTime, -_settings.MaxFallSpeed);
            }

            State = IsGrounded
                ? (Mathf.Abs(velocity.x) <= MotionThreshold
                    ? MovementState.Idle
                    : (Mathf.Abs(horizontal) < HalfTilt ? MovementState.Walk : MovementState.Run))
                : (velocity.y > 0f ? MovementState.Jump : MovementState.Fall);
            return velocity;
        }

        public void Reset()
        {
            _coyoteRemaining = -1f;
            _bufferRemaining = -1f;
            _awaitingGroundExit = false;
            _dashRemaining = 0f;
            _dashElapsed = 0f;
            _dashCooldownRemaining = 0f;
            _dashDirection = 1;
            _airDashesUsed = 0;
            _airJumpsUsed = 0;
            IsGrounded = false;
            State = MovementState.Idle;
            FacingDirection = 1;
        }

        private Vector2 StepDash(float deltaTime)
        {
            _dashRemaining -= deltaTime;
            _dashElapsed += deltaTime;
            // A jump pressed before the dash must not fire on the far side of it.
            _coyoteRemaining -= deltaTime;
            _bufferRemaining -= deltaTime;
            State = MovementState.Dash;
            // The dash is a flat horizontal burst: gravity and input are suspended for its duration.
            var speed = _dash.DashSpeed
                * (_dashRemaining > TimerEpsilon ? 1f : _dash.ExitSpeedFraction);
            return new Vector2(_dashDirection * speed, 0f);
        }
    }
}
