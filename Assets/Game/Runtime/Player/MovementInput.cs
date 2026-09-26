namespace ForgottenKingdom
{
    public readonly struct MovementInput
    {
        public readonly float Horizontal;
        public readonly bool IsRunning;
        public readonly bool IsJumpPressed;
        public readonly bool IsJumpHeld;
        public readonly float JumpPressAge;

        public MovementInput(float horizontal, bool isRunning = false,
            bool isJumpPressed = false, bool isJumpHeld = false, float jumpPressAge = 0f)
        {
            Horizontal = horizontal;
            IsRunning = isRunning;
            IsJumpPressed = isJumpPressed;
            IsJumpHeld = isJumpHeld;
            JumpPressAge = jumpPressAge;
        }
    }

    public enum MovementState
    {
        Idle,
        Walk,
        Run,
        Jump,
        Fall,
        Dash
    }

    /// <summary>
    /// Combat and utility buttons, read next to <see cref="MovementInput"/> so gameplay code never
    /// touches a specific key or pad button.
    /// </summary>
    public readonly struct CombatInput
    {
        public readonly bool IsLightAttackPressed;
        public readonly bool IsHeavyAttackPressed;
        public readonly bool IsDashPressed;
        public readonly bool IsBlockHeld;
        public readonly bool IsHealPressed;
        public readonly bool IsInteractPressed;
        public readonly bool IsSwitchWeaponPressed;
        /// <summary>Which skill slot was pressed, or -1 for none.</summary>
        public readonly int PressedSkillSlot;
        /// <summary>Which spell slot was pressed, or -1 for none.</summary>
        public readonly int PressedSpellSlot;

        public CombatInput(bool isLightAttackPressed = false, bool isHeavyAttackPressed = false,
            bool isDashPressed = false, bool isBlockHeld = false, bool isHealPressed = false,
            bool isInteractPressed = false, bool isSwitchWeaponPressed = false,
            int pressedSkillSlot = -1, int pressedSpellSlot = -1)
        {
            IsSwitchWeaponPressed = isSwitchWeaponPressed;
            PressedSkillSlot = pressedSkillSlot;
            PressedSpellSlot = pressedSpellSlot;
            IsLightAttackPressed = isLightAttackPressed;
            IsHeavyAttackPressed = isHeavyAttackPressed;
            IsDashPressed = isDashPressed;
            IsBlockHeld = isBlockHeld;
            IsHealPressed = isHealPressed;
            IsInteractPressed = isInteractPressed;
        }
    }
}
