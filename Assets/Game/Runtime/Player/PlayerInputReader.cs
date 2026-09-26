using UnityEngine;
using UnityEngine.InputSystem;

namespace ForgottenKingdom
{
    [DisallowMultipleComponent]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset _inputActions;
        private InputActionAsset _runtimeActions;
        private InputAction _move;
        private InputAction _jump;
        private InputAction _lightAttack;
        private InputAction _heavyAttack;
        private InputAction _dash;
        private InputAction _block;
        private InputAction _heal;
        private InputAction _interact;
        private InputAction _inventory;
        private InputAction _map;
        private InputAction _pause;
        private InputAction _switchWeapon;
        private InputAction _abilityModifier;
        private readonly InputAction[] _activeSkills = new InputAction[2];
        private readonly InputAction[] _spells = new InputAction[3];
        private int _pressedSkillSlot = -1;
        private int _pressedSpellSlot = -1;
        private bool _hasJumpPress;
        private bool _hasSwitchWeaponPress;
        private bool _isGameplaySuspended;
        private bool _hasLightAttackPress;
        private bool _hasHeavyAttackPress;
        private bool _hasDashPress;
        private bool _hasHealPress;
        private bool _hasInteractPress;
        private double _jumpPressedAt;
        private bool _hasFocus = true;
        private bool _isPaused;
        private bool _waitingForRelease;
        private bool _isBlockUnderModifier;

        private void Awake()
        {
            if (_inputActions == null)
            {
                Debug.LogError("PlayerInputReader requires the project's Input Actions asset.", this);
                enabled = false;
                return;
            }

            // Own only our actions; never disable the project-wide UI or another player's input.
            _runtimeActions = Instantiate(_inputActions);
            _move = _runtimeActions.FindAction("Player/Move", true);
            _jump = _runtimeActions.FindAction("Player/Jump", true);
            _lightAttack = _runtimeActions.FindAction("Player/LightAttack", true);
            _heavyAttack = _runtimeActions.FindAction("Player/HeavyAttack", true);
            _dash = _runtimeActions.FindAction("Player/Dash", true);
            _block = _runtimeActions.FindAction("Player/Block", true);
            _heal = _runtimeActions.FindAction("Player/Heal", true);
            _interact = _runtimeActions.FindAction("Player/Interact", true);
            _inventory = _runtimeActions.FindAction("Player/Inventory", true);
            _map = _runtimeActions.FindAction("Player/Map", true);
            _pause = _runtimeActions.FindAction("Player/Pause", true);
            _switchWeapon = _runtimeActions.FindAction("Player/SwitchWeapon", true);
            _activeSkills[0] = _runtimeActions.FindAction("Player/ActiveSkill1", true);
            _activeSkills[1] = _runtimeActions.FindAction("Player/ActiveSkill2", true);
            _spells[0] = _runtimeActions.FindAction("Player/CastSpell1", true);
            _spells[1] = _runtimeActions.FindAction("Player/CastSpell2", true);
            _spells[2] = _runtimeActions.FindAction("Player/CastSpell3", true);
            _abilityModifier = _runtimeActions.FindAction("Player/AbilityModifier", true);
        }

        private void OnEnable()
        {
            if (_runtimeActions == null)
            {
                return;
            }

            _waitingForRelease = false;
            ClearPresses();
            _jump.performed += OnJumpPerformed;
            _lightAttack.performed += OnLightAttackPerformed;
            _heavyAttack.performed += OnHeavyAttackPerformed;
            _dash.performed += OnDashPerformed;
            _heal.performed += OnHealPerformed;
            _interact.performed += OnInteractPerformed;
            _inventory.performed += OnInventoryPerformed;
            _map.performed += OnMapPerformed;
            _pause.performed += OnPausePerformed;
            _switchWeapon.performed += OnSwitchWeaponPerformed;
            _block.performed += OnBlockPerformed;
            _block.canceled += OnBlockCanceled;
            _abilityModifier.Enable();
            _activeSkills[0].performed += OnActiveSkill1;
            _activeSkills[1].performed += OnActiveSkill2;
            _activeSkills[0].Enable();
            _activeSkills[1].Enable();
            _spells[0].performed += OnSpell1;
            _spells[1].performed += OnSpell2;
            _spells[2].performed += OnSpell3;
            _spells[0].Enable();
            _spells[1].Enable();
            _spells[2].Enable();
            _inventory.Enable();
            _map.Enable();
            _pause.Enable();
            _switchWeapon.Enable();
            _move.Enable();
            _jump.Enable();
            _lightAttack.Enable();
            _heavyAttack.Enable();
            _dash.Enable();
            _block.Enable();
            _heal.Enable();
            _interact.Enable();
        }

        private void OnDisable()
        {
            if (_runtimeActions == null)
            {
                return;
            }

            _jump.performed -= OnJumpPerformed;
            _lightAttack.performed -= OnLightAttackPerformed;
            _heavyAttack.performed -= OnHeavyAttackPerformed;
            _dash.performed -= OnDashPerformed;
            _heal.performed -= OnHealPerformed;
            _interact.performed -= OnInteractPerformed;
            _inventory.performed -= OnInventoryPerformed;
            _map.performed -= OnMapPerformed;
            _pause.performed -= OnPausePerformed;
            _switchWeapon.performed -= OnSwitchWeaponPerformed;
            _block.performed -= OnBlockPerformed;
            _block.canceled -= OnBlockCanceled;
            _abilityModifier.Disable();
            _activeSkills[0].performed -= OnActiveSkill1;
            _activeSkills[1].performed -= OnActiveSkill2;
            _activeSkills[0].Disable();
            _activeSkills[1].Disable();
            _spells[0].performed -= OnSpell1;
            _spells[1].performed -= OnSpell2;
            _spells[2].performed -= OnSpell3;
            _spells[0].Disable();
            _spells[1].Disable();
            _spells[2].Disable();
            _inventory.Disable();
            _map.Disable();
            _pause.Disable();
            _switchWeapon.Disable();
            _move.Disable();
            _jump.Disable();
            _lightAttack.Disable();
            _heavyAttack.Disable();
            _dash.Disable();
            _block.Disable();
            _heal.Disable();
            _interact.Disable();
            ClearInput();
        }

        private void OnDestroy()
        {
            if (_runtimeActions != null)
            {
                Destroy(_runtimeActions);
            }
        }

        public MovementInput Consume()
        {
            if (!IsReceivingInput)
            {
                return default;
            }

            var horizontal = _move.ReadValue<Vector2>().x;
            var isJumpHeld = _jump.IsPressed();
            if (_waitingForRelease)
            {
                _waitingForRelease = Mathf.Abs(horizontal) > 0.01f || isJumpHeld;
                ClearPresses();
                return default;
            }

            var input = new MovementInput(horizontal, _hasJumpPress, isJumpHeld,
                (float)System.Math.Max(0d, Time.timeAsDouble - _jumpPressedAt));
            _hasJumpPress = false;
            return input;
        }

        public CombatInput ConsumeCombat()
        {
            if (!IsReceivingInput || _waitingForRelease)
            {
                return default;
            }

            var input = new CombatInput(_hasLightAttackPress, _hasHeavyAttackPress, _hasDashPress,
                _block.IsPressed() && !_isBlockUnderModifier, _hasHealPress, _hasInteractPress, _hasSwitchWeaponPress,
                _pressedSkillSlot, _pressedSpellSlot);
            _pressedSkillSlot = -1;
            _pressedSpellSlot = -1;
            _hasLightAttackPress = false;
            _hasHeavyAttackPress = false;
            _hasDashPress = false;
            _hasHealPress = false;
            _hasInteractPress = false;
            _hasSwitchWeaponPress = false;
            return input;
        }

        /// <summary>
        /// Raised when the inventory button is pressed. It is deliberately not part of
        /// <see cref="CombatInput"/>: the screen must still hear the button while gameplay input
        /// is suspended, which is exactly when it needs to close again.
        /// </summary>
        public event System.Action InventoryToggled;

        /// <summary>The map button; like the inventory's, it is heard while the map is open.</summary>
        public event System.Action MapToggled;

        /// <summary>Escape or Start. Heard while gameplay is suspended: it is also what closes the pause.</summary>
        public event System.Action PauseToggled;

        /// <summary>
        /// Silences movement and combat without disabling the component, so the menu keeps its own
        /// button working. Held keys are dropped, so gameplay never resumes mid-swing.
        /// </summary>
        public void SetGameplayEnabled(bool isEnabled)
        {
            if (_isGameplaySuspended == !isEnabled)
            {
                return;
            }

            _isGameplaySuspended = !isEnabled;
            ClearInput();
        }

        public void ClearInput()
        {
            ClearPresses();
            _jumpPressedAt = 0d;
            // Teleport/focus regain must not turn a stale held button into another action.
            _waitingForRelease = true;
        }

        private bool IsReceivingInput => isActiveAndEnabled && _hasFocus && !_isPaused
            && !_isGameplaySuspended && _runtimeActions != null;

        private void ClearPresses()
        {
            _hasJumpPress = false;
            _hasLightAttackPress = false;
            _hasHeavyAttackPress = false;
            _hasDashPress = false;
            _hasHealPress = false;
            _hasInteractPress = false;
            _hasSwitchWeaponPress = false;
            _pressedSkillSlot = -1;
            _pressedSpellSlot = -1;
        }

        private bool IsPressAccepted => _hasFocus && !_isPaused && !_waitingForRelease
            && !_isGameplaySuspended;

        /// <summary>
        /// True while the pad's ability modifier (LT) is held. Skills and spells share their face
        /// and shoulder buttons with attacks, the guard and interaction, and the modifier is what
        /// tells them apart — so under it those buttons belong to the ability bar alone.
        /// </summary>
        private bool IsModifierHeld => _abilityModifier != null && _abilityModifier.IsPressed();

        /// <summary>A press of a button the ability bar also uses; void while the modifier is held.</summary>
        private bool IsPlainPressAccepted => IsPressAccepted && !IsModifierHeld;

        /// <summary>
        /// A guard raised with the modifier held was the first skill's button, not a block. It
        /// stays ignored until the button is released, so letting go of LT first does not turn
        /// the skill press into a guard. A guard already up when LT is pulled is left alone.
        /// </summary>
        private void OnBlockPerformed(InputAction.CallbackContext context)
        {
            _isBlockUnderModifier = IsModifierHeld;
        }

        private void OnBlockCanceled(InputAction.CallbackContext context)
        {
            _isBlockUnderModifier = false;
        }

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            if (!IsPressAccepted)
            {
                return;
            }

            _hasJumpPress = true;
            _jumpPressedAt = Time.timeAsDouble;
        }

        private void OnLightAttackPerformed(InputAction.CallbackContext context)
        {
            _hasLightAttackPress |= IsPlainPressAccepted;
        }

        private void OnHeavyAttackPerformed(InputAction.CallbackContext context)
        {
            _hasHeavyAttackPress |= IsPlainPressAccepted;
        }

        private void OnDashPerformed(InputAction.CallbackContext context)
        {
            _hasDashPress |= IsPressAccepted;
        }

        private void OnHealPerformed(InputAction.CallbackContext context)
        {
            _hasHealPress |= IsPressAccepted;
        }

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            _hasInteractPress |= IsPlainPressAccepted;
        }

        private void OnSwitchWeaponPerformed(InputAction.CallbackContext context)
        {
            _hasSwitchWeaponPress |= IsPressAccepted;
        }

        private void OnActiveSkill1(InputAction.CallbackContext context) => PressSkill(0);

        private void OnActiveSkill2(InputAction.CallbackContext context) => PressSkill(1);

        /// <summary>
        /// Only one slot can be waiting at a time: pressing both in the same frame runs the one
        /// pressed last rather than queueing a skill the player has already changed their mind
        /// about.
        /// </summary>
        private void PressSkill(int slot)
        {
            if (IsPressAccepted)
            {
                _pressedSkillSlot = slot;
            }
        }

        private void OnSpell1(InputAction.CallbackContext context) => PressSpell(0);

        private void OnSpell2(InputAction.CallbackContext context) => PressSpell(1);

        private void OnSpell3(InputAction.CallbackContext context) => PressSpell(2);

        /// <summary>One spell waits at a time, for the same reason one skill does.</summary>
        private void PressSpell(int slot)
        {
            if (IsPressAccepted)
            {
                _pressedSpellSlot = slot;
            }
        }

        private void OnInventoryPerformed(InputAction.CallbackContext context)
        {
            // Deliberately not gated on gameplay input: this is the button that closes the screen.
            if (_hasFocus && !_isPaused)
            {
                InventoryToggled?.Invoke();
            }
        }

        private void OnPausePerformed(InputAction.CallbackContext context)
        {
            if (_hasFocus && !_isPaused)
            {
                PauseToggled?.Invoke();
            }
        }

        private void OnMapPerformed(InputAction.CallbackContext context)
        {
            if (_hasFocus && !_isPaused)
            {
                MapToggled?.Invoke();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            _hasFocus = hasFocus;
            ClearInput();
        }

        private void OnApplicationPause(bool isPaused)
        {
            _isPaused = isPaused;
            ClearInput();
        }
    }
}
