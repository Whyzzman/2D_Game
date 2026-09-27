using UnityEngine;

namespace ForgottenKingdom
{
    public enum WallMovementState { None, Slide, Jump, Run }

    [DisallowMultipleComponent]
    public sealed class WallMovement : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _slideSpeed = 2.5f;
        [SerializeField, Min(0f)] private float _jumpUpSpeed = 10f;
        [SerializeField, Min(0f)] private float _jumpAwaySpeed = 7f;
        [SerializeField, Min(0f)] private float _regrabDelay = 0.25f;
        [SerializeField, Min(0f)] private float _runSpeed = 4f;
        [SerializeField, Min(0f)] private float _runDuration = 0.8f;
        [SerializeField, Min(0f)] private float _runStaminaPerSecond = 25f;
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

        public Vector2 Step(Vector2 velocity, MovementInput input, int wallDirection,
            bool grounded, bool allowed, float deltaTime)
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
                    if (input.IsJumpPressed)
                    {
                        _jumpDirection = -wallDirection;
                        WallSide = wallDirection;
                        velocity = new Vector2(_jumpDirection * _jumpAwaySpeed, _jumpUpSpeed);
                        _regrabRemaining = _regrabDelay;
                        State = WallMovementState.Jump;
                    }
                    // Holding jump against the wall runs up it; a fresh press above leaps off.
                    else if (input.IsJumpHeld && _runUsed < _runDuration
                        && _stamina != null && _stamina.TrySpend(RunStaminaPerSecond * deltaTime))
                    {
                        _runUsed += deltaTime;
                        velocity = new Vector2(0f, _runSpeed);
                        State = WallMovementState.Run;
                    }
                    else if (velocity.y < 0f)
                    {
                        velocity.y = Mathf.Max(velocity.y, -_slideSpeed);
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
