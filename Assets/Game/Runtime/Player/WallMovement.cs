using UnityEngine;

namespace ForgottenKingdom
{
    public enum WallMovementState { None, Slide, Jump, Run }

    [DisallowMultipleComponent]
    public sealed class WallMovement : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _slideSpeed = 2.5f;
        [SerializeField, Min(0f)] private float _jumpUpSpeed = 10f;
        [Tooltip("The leap off a wall before wall-run is found. Low enough that leaping off one wall and "
            + "steering back loses height each time (≈1.5 a round at 7; at 10 it gained ≈0.3 and climbed "
            + "any wall), so the gates built on the second jump hold.")]
        [SerializeField, Min(0f)] private float _basicJumpUpSpeed = 7f;
        [SerializeField, Min(0f)] private float _jumpAwaySpeed = 7f;
        [SerializeField, Min(0f)] private float _regrabDelay = 0.25f;
        [SerializeField, Min(0f)] private float _runSpeed = 4f;
        [SerializeField, Min(0f)] private float _runDuration = 0.8f;
        [SerializeField, Min(0f)] private float _runStaminaPerSecond = 25f;
        /// <summary>How fast the hero must already fall for the wall to take him into a slide.</summary>
        public const float SlideStartSpeed = 1.5f;
        private float _regrabRemaining;
        private float _runUsed;
        private float _jumpDirection;
        private Stamina _stamina;
        private PlayerSkills _skills;
        public WallMovementState State { get; private set; }
        public event System.Action<WallMovementState> StateChanged;

        /// <summary>Which side the wall was on at the last slide or leap: -1 left, 1 right.</summary>
        public int WallSide { get; private set; }

        private void Awake()
        {
            _stamina = GetComponent<Stamina>();
            _skills = FindAnyObjectByType<PlayerSkills>();
        }

        /// <summary>What a second on the wall costs now, after "Ощадливий рух".</summary>
        public float RunStaminaPerSecond => _runStaminaPerSecond
            * (_skills != null ? _skills.Multiplier(SkillEffectType.WallRunStaminaCost) : 1f);

        /// <param name="canRun">Running up the wall: the wall-run ability. Sliding and leaping off need nothing.</param>
        public Vector2 Step(Vector2 velocity, MovementInput input, int wallDirection,
            bool grounded, bool allowed, float deltaTime, bool canRun = true)
        {
            var previous = State;
            State = WallMovementState.None;
            if (grounded) { _runUsed = 0f; }
            _regrabRemaining = Mathf.Max(0f, _regrabRemaining - deltaTime);
            if (allowed && !grounded)
            {
                if (_regrabRemaining > 0f)
                {
                    velocity.x = _jumpDirection * _jumpAwaySpeed;
                    State = WallMovementState.Jump;
                }
                else if (wallDirection != 0 && input.Horizontal * wallDirection > 0.1f)
                {
                    // the leap off is taken only from a cling (sliding down, running up); a press while still
                    // rising past the wall is the second jump's (the motor leaves it to the logic)
                    var clinging = previous == WallMovementState.Slide || previous == WallMovementState.Run;
                    if (input.IsJumpPressed && clinging)
                    {
                        _jumpDirection = -wallDirection;
                        WallSide = wallDirection;
                        velocity = new Vector2(_jumpDirection * _jumpAwaySpeed, canRun ? _jumpUpSpeed : _basicJumpUpSpeed);
                        _regrabRemaining = _regrabDelay;
                        State = WallMovementState.Jump;
                    }
                    // Holding jump against the wall runs up it; a fresh press above leaps off.
                    else if (canRun && input.IsJumpHeld && _runUsed < _runDuration
                        && _stamina != null && _stamina.TrySpend(RunStaminaPerSecond * deltaTime))
                    {
                        _runUsed += deltaTime;
                        velocity = new Vector2(0f, _runSpeed);
                        State = WallMovementState.Run;
                    }
                    // the cling begins once the hero is truly falling, not at the top of a jump: around the apex a
                    // press beside a block is the second jump up onto it (2026-11-05, «стрибок на стінку блока»)
                    else if (velocity.y < -SlideStartSpeed)
                    {
                        // held down, the hero lets the wall go faster
                        velocity.y = Mathf.Max(velocity.y, -_slideSpeed * (input.Vertical < -0.5f ? 2.5f : 1f));
                        WallSide = wallDirection;
                        State = WallMovementState.Slide;
                    }
                }
            }
            if (previous != State) { StateChanged?.Invoke(State); }
            return velocity;
        }

        public void ResetMovement()
        {
            _regrabRemaining = _runUsed = _jumpDirection = 0f;
            State = WallMovementState.None;
            StateChanged?.Invoke(State);
        }
    }
}
