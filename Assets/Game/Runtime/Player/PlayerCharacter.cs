using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The player's decision layer: it reads input, runs the action state machine and tells the
    /// motor, the weapon and the shield what to do. Movement physics stay in
    /// <see cref="PlayerMotor2D"/> and hit resolution stays in <see cref="CombatActor"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMotor2D), typeof(PlayerInputReader), typeof(CombatActor))]
    public sealed class PlayerCharacter : MonoBehaviour, ILocomotionAuthority
    {
        [SerializeField] private PlayerMotor2D _motor;
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private CombatActor _actor;
        [SerializeField] private PlayerCombat _combat;
        [SerializeField] private PlayerDefense _defense;
        [SerializeField] private Stamina _stamina;
        [SerializeField] private StaggerMeter _stagger;
        [SerializeField] private PlayerEquipment _equipment;
        [SerializeField] private PlayerStats _stats;
        [SerializeField] private PlayerSkills _skills;
        [SerializeField] private ActiveSkillController _activeSkills;
        [SerializeField] private TemporaryBuffs _buffs;
        [SerializeField] private PlayerOffense _offense;
        [SerializeField] private SpellCaster _caster;
        [SerializeField] private Mana _mana;
        [Tooltip("How long the hands are busy swapping weapons; no attack may start during it.")]
        [SerializeField, Min(0f)] private float _weaponSwitchTime = 0.28f;
        [Tooltip("Share of normal speed kept while swapping weapons.")]
        [SerializeField, Range(0f, 1f)] private float _weaponSwitchMovementScale = 0.6f;
        [Tooltip("How long the bonus from a perfect dodge lasts.")]
        [SerializeField, Min(0f)] private float _momentumDuration = 3f;
        [Tooltip("How long the bonus after a kill lasts.")]
        [SerializeField, Min(0f)] private float _adrenalineDuration = 3f;
        [Tooltip("Health share below which a second wind may trigger.")]
        [SerializeField, Range(0.05f, 0.9f)] private float _secondWindThreshold = 0.3f;
        [SerializeField, Min(0f)] private float _secondWindDuration = 6f;
        [SerializeField, Min(0f)] private float _secondWindCooldown = 30f;
        [Tooltip("How long a pressed action keeps waiting for the current action to end.")]
        [SerializeField, Min(0f)] private float _inputBufferTime = 0.15f;
        [SerializeField, Min(0f)] private float _hurtDuration = 0.28f;
        [SerializeField, Min(0f)] private float _deathDelay = 1.2f;
        [Tooltip("Share of normal speed kept while the guard is up.")]
        [SerializeField, Range(0f, 1f)] private float _blockMovementScale = 0.28f;
        [Tooltip("Estus-style flasks: charges, heal, drink time. Refilled at altars and on respawn.")]
        [SerializeField] private HealingFlasks _flasks = new HealingFlasks();
        private readonly PlayerStateMachine _state = new PlayerStateMachine();
        private float _bufferedLightUntil;
        private float _bufferedHeavyUntil;
        private float _bufferedDashUntil;
        private float _hurtEndsAt;
        private float _incapacitatedUntil;
        private float _deathEndsAt;
        private float _weaponSwitchEndsAt;
        private float _secondWindReadyAt;
        private bool _isDeathReported;
        private bool _isDashImmunityArmed;

        /// <summary>Raised once the death animation window has elapsed; the checkpoint system
        /// answers it with <see cref="Respawn"/>.</summary>
        public event Action ReadyToRespawn;
        public event Action Died;
        public event Action InteractPressed;
        public event Action<PlayerAction, PlayerAction> ActionChanged;

        // Feedback hooks for animation, audio and VFX. They fire even without assets attached.
        public event Action DashStarted;
        public event Action<DamageInfo> AttackDodged;

        /// <summary>A hit swallowed by the sharp opening frames of the dash window.</summary>
        public event Action<DamageInfo> PerfectDodge;
        public event Action<DamageInfo> Parried;
        public event Action<DamageInfo> Blocked;
        public event Action GuardBroken;
        public event Action<float> Stunned;

        public PlayerAction Action => _state.Current;
        public CombatActor Actor => _actor;
        public PlayerCombat Combat => _combat;
        public PlayerDefense Defense => _defense;
        public PlayerEquipment Equipment => _equipment;
        public PlayerStats Stats => _stats;
        public Health Health => _actor != null ? _actor.Health : null;
        public Stamina Stamina => _stamina;
        public StaggerMeter Stagger => _stagger;
        public bool IsAlive => _actor != null && _actor.IsAlive;
        public HealingFlasks Flasks => _flasks;

        public LocomotionMode LocomotionMode
        {
            get
            {
                switch (_state.Current)
                {
                    case PlayerAction.Hurt:
                    case PlayerAction.Dead:
                        return LocomotionMode.Knockback;
                    case PlayerAction.LightAttack:
                    case PlayerAction.HeavyAttack:
                    case PlayerAction.Block:
                    case PlayerAction.Parry:
                    case PlayerAction.WeaponSwitch:
                        return LocomotionMode.Restricted;
                    case PlayerAction.ActiveSkill:
                        // The skill owns the movement for its whole length, lunge or not.
                        return LocomotionMode.Knockback;
                    case PlayerAction.Casting:
                        // A caster keeps their feet — slowed, but not rooted, and gravity still
                        // applies in the air.
                        return LocomotionMode.Restricted;
                    case PlayerAction.Stunned:
                    case PlayerAction.GuardBroken:
                    case PlayerAction.Heal:
                        return LocomotionMode.NoInput;
                    default:
                        return LocomotionMode.Normal;
                }
            }
        }

        /// <summary>Each restricted action carries how much the player may still drift.</summary>
        public float MovementScale
        {
            get
            {
                if (_state.IsBlocking)
                {
                    return _blockMovementScale;
                }

                if (_state.Current == PlayerAction.WeaponSwitch)
                {
                    return _weaponSwitchMovementScale;
                }

                if (_state.Current == PlayerAction.Casting)
                {
                    return _caster != null ? _caster.MovementScale : 0.3f;
                }

                return _combat?.CurrentAttack?.MovementScale ?? 1f;
            }
        }

        private void Awake()
        {
            if (_motor == null) { _motor = GetComponent<PlayerMotor2D>(); }
            if (_input == null) { _input = GetComponent<PlayerInputReader>(); }
            if (_actor == null) { _actor = GetComponent<CombatActor>(); }
            if (_combat == null) { _combat = GetComponent<PlayerCombat>(); }
            if (_defense == null) { _defense = GetComponent<PlayerDefense>(); }
            if (_stamina == null) { _stamina = GetComponent<Stamina>(); }
            if (_stagger == null) { _stagger = GetComponent<StaggerMeter>(); }
            if (_equipment == null) { _equipment = GetComponent<PlayerEquipment>(); }
            if (_stats == null) { _stats = GetComponent<PlayerStats>(); }
            if (_activeSkills == null) { _activeSkills = GetComponent<ActiveSkillController>(); }
            if (_buffs == null) { _buffs = GetComponent<TemporaryBuffs>(); }
            if (_offense == null) { _offense = GetComponent<PlayerOffense>(); }
            if (_caster == null) { _caster = GetComponent<SpellCaster>(); }
            if (_mana == null) { _mana = GetComponent<Mana>(); }
            if (_skills == null) { _skills = FindAnyObjectByType<PlayerSkills>(); }
            _flasks.Bind(_actor != null ? _actor.Health : null);
        }

        private void OnEnable()
        {
            _state.Reset();
            _state.ActionChanged += OnActionChanged;
            _actor.DamageTaken += OnDamageTaken;
            _actor.AttackDodged += OnAttackDodged;
            _actor.Killed += OnKilled;
            if (_combat != null) { _combat.HitLanded += OnHitLanded; }
            if (_activeSkills != null) { _activeSkills.SkillStarted += OnSkillStarted; }
            if (_stagger != null)
            {
                _stagger.Staggered += OnStaggered;
            }
            if (_defense != null)
            {
                _defense.GuardBroken += OnGuardBroken;
            }
        }

        private void OnDisable()
        {
            _state.ActionChanged -= OnActionChanged;
            _actor.DamageTaken -= OnDamageTaken;
            _actor.AttackDodged -= OnAttackDodged;
            _actor.Killed -= OnKilled;
            if (_combat != null) { _combat.HitLanded -= OnHitLanded; }
            if (_activeSkills != null) { _activeSkills.SkillStarted -= OnSkillStarted; }
            if (_stagger != null)
            {
                _stagger.Staggered -= OnStaggered;
            }
            if (_defense != null)
            {
                _defense.GuardBroken -= OnGuardBroken;
            }
            _defense?.EndBlock();
            _combat?.Cancel();
        }

        private void FixedUpdate()
        {
            var deltaTime = Time.fixedDeltaTime;
            var input = _input.ConsumeCombat();
            if (input.IsInteractPressed)
            {
                InteractPressed?.Invoke();
            }
            BufferPresses(input);
            _state.Tick(deltaTime);
            _combat?.Tick(deltaTime);
            _activeSkills?.Tick(deltaTime);
            _caster?.Tick(deltaTime);
            _flasks.Tick(deltaTime);
            _state.IsAttackCancelable = _combat != null && _combat.IsCancelable;
            UpdateDashImmunity();

            FinishCurrentAction(input);
            if (_state.Current == PlayerAction.Dead)
            {
                TryReportDeath();
                return;
            }

            StartNextAction(input);
        }

        public void Respawn(Vector2 position)
        {
            ResetCombatState();
            _actor.Health.RestoreToFull();
            _stamina?.RestoreToFull();
            _mana?.RestoreToFull();
            _isDeathReported = false;
            _motor.ResetTo(position);
        }

        /// <summary>
        /// Applies saved progression. Only long-term values arrive here: the bars themselves are
        /// always refilled, because a load starts the player rested.
        /// </summary>
        public void ApplyProgression(PlayerProgressionData progression)
        {
            if (progression == null)
            {
                return;
            }

            if (_stats != null)
            {
                // Progression is only the base: equipment is layered on top, and the bars end up
                // at the derived maximum rather than at the raw saved one.
                _stats.SetBaseProgression(progression);
                _flasks.Refill();
                _actor.Health.RestoreToFull();
                _stamina?.RestoreToFull();
                _mana?.RestoreToFull();
                return;
            }

            _actor.Health.SetMax(progression.maxHealth, true);
            _stamina?.SetMax(progression.maxStamina, true);
        }

        /// <summary>
        /// Drops every transient defensive state. Respawning and resting at an altar both go
        /// through here, so a guard break or a stun can never survive either.
        /// </summary>
        public void ResetCombatState()
        {
            _actor.Statuses?.Clear();
            _combat?.Cancel();
            _defense?.EndBlock();
            // Temporary bonuses and skill cooldowns belong to the fight that just ended; what was
            // learned does not, and is never touched here.
            _activeSkills?.Cancel();
            _activeSkills?.ResetCooldowns();
            _caster?.ResetCasting();
            _buffs?.Clear();
            // Resting and respawning both come through here, and both fill the flasks.
            _flasks.Refill();
            _motor.CancelDash();
            _stagger?.ResetStagger();
            _actor.ClearInvulnerability();
            _state.Reset();
            _isDashImmunityArmed = false;
            _incapacitatedUntil = 0f;
            _hurtEndsAt = 0f;
            _weaponSwitchEndsAt = 0f;
            ClearBuffers();
        }

        /// <summary>
        /// The dash is only invincible during its configured window, not for its whole length, so
        /// a mistimed dash still gets punished.
        /// </summary>
        private void UpdateDashImmunity()
        {
            if (!_motor.IsDashing)
            {
                _isDashImmunityArmed = false;
                return;
            }

            var settings = _motor.DashSettings;
            if (_isDashImmunityArmed || _motor.DashElapsed < settings.InvincibilityDelay)
            {
                return;
            }

            _isDashImmunityArmed = true;
            _actor.SetInvulnerable(ImmunitySource.Dodge, settings.InvincibilityDuration);
        }

        private void BufferPresses(in CombatInput input)
        {
            var until = Time.time + _inputBufferTime;
            if (input.IsLightAttackPressed) { _bufferedLightUntil = until; }
            if (input.IsHeavyAttackPressed) { _bufferedHeavyUntil = until; }
            if (input.IsDashPressed) { _bufferedDashUntil = until; }
        }

        private void ClearBuffers()
        {
            _bufferedLightUntil = 0f;
            _bufferedHeavyUntil = 0f;
            _bufferedDashUntil = 0f;
        }

        private void FinishCurrentAction(in CombatInput input)
        {
            switch (_state.Current)
            {
                case PlayerAction.LightAttack:
                case PlayerAction.HeavyAttack:
                    if (_combat == null || !_combat.IsAttacking)
                    {
                        _state.ExitTo();
                    }
                    break;
                case PlayerAction.Dash:
                    if (!_motor.IsDashing)
                    {
                        _state.ExitTo();
                    }
                    break;
                case PlayerAction.Hurt:
                    if (Time.time >= _hurtEndsAt)
                    {
                        _state.ExitTo();
                    }
                    break;
                case PlayerAction.WeaponSwitch:
                    if (Time.time >= _weaponSwitchEndsAt)
                    {
                        _state.ExitTo();
                    }
                    break;
                case PlayerAction.ActiveSkill:
                    if (_activeSkills == null || !_activeSkills.IsRunning)
                    {
                        _state.ExitTo();
                    }
                    break;
                case PlayerAction.Casting:
                    if (_caster == null || !_caster.IsCasting)
                    {
                        _state.ExitTo();
                    }
                    break;
                case PlayerAction.Heal:
                    if (!_flasks.IsDrinking)
                    {
                        _state.ExitTo();
                    }
                    break;
                case PlayerAction.Stunned:
                case PlayerAction.GuardBroken:
                    if (Time.time >= _incapacitatedUntil
                        && !(_stagger != null && _stagger.IsStunned))
                    {
                        ClearBuffers();
                        _state.ExitTo();
                    }
                    break;
                case PlayerAction.Parry:
                case PlayerAction.Block:
                    if (!input.IsBlockHeld)
                    {
                        _defense?.EndBlock();
                        _state.ExitTo();
                    }
                    else if (_state.Current == PlayerAction.Parry && _defense != null
                        && _defense.Phase != GuardPhase.Parry)
                    {
                        // The parry window closed with the button still down: settle into a block.
                        _state.ExitTo(PlayerAction.Block);
                    }
                    break;
            }
        }

        private void StartNextAction(in CombatInput input)
        {
            if (TryBufferedDash())
            {
                return;
            }

            if (input.PressedSkillSlot >= 0 && TryUseSkill(input.PressedSkillSlot))
            {
                return;
            }

            if (input.PressedSpellSlot >= 0
                && TryCastSpell(input.PressedSpellSlot) == CastResult.Casting)
            {
                return;
            }

            if (input.IsSwitchWeaponPressed && TrySwitchWeapon())
            {
                return;
            }

            if (input.IsHealPressed && TryHeal())
            {
                return;
            }

            if (TryAttack(ref _bufferedHeavyUntil, PlayerAction.HeavyAttack))
            {
                return;
            }

            if (TryAttack(ref _bufferedLightUntil, PlayerAction.LightAttack))
            {
                return;
            }

            if (input.IsBlockHeld)
            {
                TryBlock();
                return;
            }

            if (_state.IsBlocking)
            {
                _defense?.EndBlock();
                _state.ExitTo();
            }
        }

        /// <summary>
        /// Drinks a flask, if the player may commit to it: standing still for the drink, with no
        /// swing or dodge out of it once started — the price of healing mid-fight.
        /// </summary>
        public bool TryHeal()
        {
            if (!_state.CanEnter(PlayerAction.Heal) || _motor.IsDashing || !_motor.IsGrounded
                || (_combat != null && _combat.IsAttacking) || !_flasks.TryBegin())
            {
                return false;
            }

            _defense?.EndBlock();
            _state.ExitTo(PlayerAction.Heal);
            return true;
        }

        /// <summary>
        /// Swaps to the other weapon. The press is not buffered: unlike an attack, a swap the
        /// player could not make is one they no longer want by the time the window opens.
        /// </summary>
        public bool TrySwitchWeapon()
        {
            // A running swing blocks the swap on its own, whoever started it: the live hitbox
            // carries the old weapon's numbers and must finish with the weapon that made it.
            if (_equipment == null || !_equipment.HasSecondaryWeapon
                || !_state.CanEnter(PlayerAction.WeaponSwitch) || _motor.IsDashing
                || (_combat != null && _combat.IsAttacking))
            {
                return false;
            }

            if (!_equipment.SwitchWeapon())
            {
                return false;
            }

            // The combo belongs to the weapon that was put away; PlayerCombat drops it on the
            // weapon-changed event, and the guard comes down with it.
            _defense?.EndBlock();
            _weaponSwitchEndsAt = Time.time + _weaponSwitchTime;
            _state.ExitTo(PlayerAction.WeaponSwitch);
            return true;
        }

        /// <summary>
        /// Fires a skill from the bar. The state machine decides whether the player may commit at
        /// all; the controller decides whether that particular skill is ready, affordable and
        /// usable with the weapon in hand.
        /// </summary>
        public SkillActivationResult TryActivateSkill(int slot)
        {
            if (_activeSkills == null || !_state.CanEnter(PlayerAction.ActiveSkill)
                || _motor.IsDashing || (_combat != null && _combat.IsAttacking))
            {
                return SkillActivationResult.BusyOrIncapacitated;
            }

            var result = _activeSkills.Activate(slot);
            if (result != SkillActivationResult.Activated)
            {
                return result;
            }

            _combat?.Cancel();
            _defense?.EndBlock();
            _state.ExitTo(PlayerAction.ActiveSkill);
            return result;
        }

        private bool TryUseSkill(int slot)
        {
            return TryActivateSkill(slot) == SkillActivationResult.Activated;
        }

        /// <summary>
        /// Begins a spell from the bar. Like a skill, the state machine decides whether the player
        /// may commit; the caster decides whether that spell is ready, affordable and castable
        /// from where they are standing.
        /// </summary>
        public CastResult TryCastSpell(int slot)
        {
            if (_caster == null || !_state.CanEnter(PlayerAction.Casting) || _motor.IsDashing
                || (_combat != null && _combat.IsAttacking))
            {
                return CastResult.BusyOrIncapacitated;
            }

            var result = _caster.BeginCast(slot);
            if (result != CastResult.Casting)
            {
                return result;
            }

            _combat?.Cancel();
            _defense?.EndBlock();
            _state.ExitTo(PlayerAction.Casting);
            return result;
        }

        private bool TryBufferedDash()
        {
            return Time.time <= _bufferedDashUntil && TryDash();
        }

        /// <summary>
        /// Dashes now, if the player may. Public alongside the other actions so anything driving
        /// the character — including a test — goes through the same rules the buffer does.
        /// </summary>
        public bool TryDash()
        {
            if (!_state.CanEnter(PlayerAction.Dash) || !_motor.IsDashReady)
            {
                return false;
            }

            // Stamina is charged once, only after every other condition has passed. The tree can
            // make a dash cheaper; it can never make it free.
            var cost = _motor.DashSettings.StaminaCost
                * (_skills != null ? _skills.Multiplier(SkillEffectType.DashStaminaCost) : 1f);
            if (_stamina != null && !_stamina.TrySpend(Mathf.Max(0f, cost)))
            {
                return false;
            }

            if (!_motor.TryBeginDash(_motor.DashDirection))
            {
                return false;
            }

            _bufferedDashUntil = 0f;
            _isDashImmunityArmed = false;
            _combat?.Cancel();
            _defense?.EndBlock();
            _state.ExitTo(PlayerAction.Dash);
            DashStarted?.Invoke();
            return true;
        }

        private bool TryAttack(ref float bufferedUntil, PlayerAction action)
        {
            if (Time.time > bufferedUntil || _combat == null || !_state.CanEnter(action))
            {
                return false;
            }

            // A swing already running may only be followed once it reaches its combo window.
            if (_combat.IsAttacking && !_combat.CanChain)
            {
                return false;
            }

            // Combat is told which kind of swing this is, so the damage pipeline knows whether
            // the heavy-attack skills apply to it.
            _offense?.BeginSwing(action == PlayerAction.HeavyAttack
                ? SwingKind.Heavy
                : SwingKind.Light);
            var started = action == PlayerAction.HeavyAttack
                ? _combat.TryStartHeavy()
                : _combat.TryStartLight(AimOfSwing());
            if (!started)
            {
                return false;
            }

            bufferedUntil = 0f;
            _defense?.EndBlock();
            _state.ExitTo(action);
            return true;
        }

        /// <summary>
        /// Up on the stick or W aims a light swing up; down, only in the air, aims it down — the
        /// pogo. On the ground, down with attack is an ordinary swing, as in Hollow Knight.
        /// </summary>
        private AttackDirection AimOfSwing()
        {
            var aim = _input != null ? _input.AimVertical : 0f;
            if (aim > 0.5f)
            {
                return AttackDirection.Up;
            }

            return aim < -0.5f && _motor != null && !_motor.IsGrounded
                ? AttackDirection.Down
                : AttackDirection.Side;
        }

        private void TryBlock()
        {
            if (_defense == null || _state.IsBlocking || !_state.CanEnter(PlayerAction.Block))
            {
                return;
            }

            _defense.BeginBlock();
            _state.ExitTo(_defense.Phase == GuardPhase.Parry
                ? PlayerAction.Parry
                : PlayerAction.Block);
        }

        private void OnDamageTaken(DamageInfo info, DamageResult result)
        {
            if (info.IsStatusDamage) { return; }
            switch (result.Outcome)
            {
                case DamageOutcome.Parried:
                    Parried?.Invoke(info);
                    return;
                case DamageOutcome.Blocked:
                    Blocked?.Invoke(info);
                    return;
                case DamageOutcome.Hit when !result.IsLethal:
                    _hurtEndsAt = Time.time + _hurtDuration;
                    _combat?.Cancel();
                    // A real hit takes the spell away. Nothing was paid for it yet, which is why
                    // reaching across the room is a risk rather than a free option.
                    _caster?.Interrupt();
                    // And a flask not yet swallowed is spilled.
                    _flasks.Interrupt();
                    _state.TryEnter(PlayerAction.Hurt);
                    TrySecondWind();
                    return;
            }
        }

        private void OnAttackDodged(DamageInfo info)
        {
            AttackDodged?.Invoke(info);
            if (!_motor.IsDashing)
            {
                return;
            }

            // A real attack went through the dash window and did nothing: that, and not merely
            // dashing, is what the tree rewards.
            PerfectDodge?.Invoke(info);
            var momentum = _skills != null ? _skills.Effect(SkillEffectType.MomentumDamage) : 0f;
            if (momentum > 0f)
            {
                _buffs?.Apply(BuffIds.Momentum, SkillEffectType.MomentumDamage, momentum,
                    _momentumDuration);
            }
        }

        /// <summary>
        /// Falling to the last of one's health braces the character for a moment. Cooldown-gated
        /// rather than once per fight, so it cannot be farmed by taking chip damage repeatedly,
        /// and it grants poise rather than health — no skill here heals.
        /// </summary>
        private void TrySecondWind()
        {
            var bonus = _skills != null
                ? _skills.Effect(SkillEffectType.SecondWindResistance)
                : 0f;
            if (bonus <= 0f || _buffs == null || Time.time < _secondWindReadyAt
                || Health == null || Health.Normalized > _secondWindThreshold)
            {
                return;
            }

            _secondWindReadyAt = Time.time + _secondWindCooldown;
            _buffs.Apply(BuffIds.SecondWind, SkillEffectType.SecondWindResistance, bonus,
                _secondWindDuration);
        }

        /// <summary>A kill can leave the player better placed for the next one.</summary>
        private void OnHitLanded(CombatActor target, DamageResult result)
        {
            if (!result.IsLethal || _skills == null)
            {
                return;
            }

            var adrenaline = _skills.Effect(SkillEffectType.AdrenalineRegen);
            if (adrenaline > 0f)
            {
                _buffs?.Apply(BuffIds.Adrenaline, SkillEffectType.AdrenalineRegen, adrenaline,
                    _adrenalineDuration);
            }
        }

        private void OnStaggered(float duration)
        {
            EnterIncapacitated(PlayerAction.Stunned, duration);
            Stunned?.Invoke(duration);
        }

        private void OnGuardBroken()
        {
            EnterIncapacitated(PlayerAction.GuardBroken, _defense.GuardBreakStun);
            GuardBroken?.Invoke();
        }

        private void EnterIncapacitated(PlayerAction action, float duration)
        {
            if (!_state.CanEnter(action))
            {
                return;
            }

            _incapacitatedUntil = Mathf.Max(_incapacitatedUntil, Time.time + duration);
            _flasks.Interrupt();
            _combat?.Cancel();
            _defense?.EndBlock();
            _motor.CancelDash();
            ClearBuffers();
            _state.ExitTo(action);
        }

        private void OnKilled(DamageInfo info)
        {
            _flasks.Interrupt();
            _activeSkills?.Cancel();
            _caster?.ResetCasting();
            _actor.ClearInvulnerability();
            _combat?.Cancel();
            _defense?.EndBlock();
            _motor.CancelDash();
            _stagger?.ResetStagger();
            ClearBuffers();
            _deathEndsAt = Time.time + _deathDelay;
            _isDeathReported = false;
            _state.ExitTo(PlayerAction.Dead);
            Died?.Invoke();
        }

        private void TryReportDeath()
        {
            if (_isDeathReported || Time.time < _deathEndsAt)
            {
                return;
            }

            _isDeathReported = true;
            ReadyToRespawn?.Invoke();
        }

        /// <summary>A skill's own swing replaces the weapon's, so the ordinary combo drops.</summary>
        private void OnSkillStarted(SkillDefinition skill)
        {
            _combat?.ResetCombo();
        }

        private void OnActionChanged(PlayerAction previous, PlayerAction next)
        {
            ActionChanged?.Invoke(previous, next);
        }
    }
}
