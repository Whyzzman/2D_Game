using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>Which way a light swing goes. Up and down never join the combo.</summary>
    public enum AttackDirection
    {
        Side,
        Up,
        /// <summary>Only in the air; striking an enemy or spikes with it bounces the player up.</summary>
        Down
    }

    /// <summary>
    /// Executes the active weapon's attacks: combo order, stamina cost and the active hitbox
    /// window. It does not decide when attacking is allowed (<see cref="PlayerCharacter"/> does),
    /// nor which weapon is in hand (<see cref="PlayerEquipment"/> does), nor what a swing is worth
    /// (the weapon data does).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerCombat : MonoBehaviour
    {
        [SerializeField] private PlayerEquipment _equipment;
        [Tooltip("Used only when no equipment component is present, as in isolated test scenes.")]
        [SerializeField] private WeaponDefinition _fallbackWeapon;
        [SerializeField] private Hitbox _hitbox;
        [SerializeField] private Stamina _stamina;
        [SerializeField] private PlayerStats _stats;
        [SerializeField] private WeaponVisual _weaponVisual;

        [Header("Swings up and down (pogo)")]
        [Tooltip("Hitbox of the swing up, from the player's centre; the side swing's timing and damage.")]
        [SerializeField] private Vector2 _upHitboxOffset = new Vector2(0f, 1.35f);
        [SerializeField] private Vector2 _upHitboxSize = new Vector2(1.4f, 1.3f);
        [Tooltip("Hitbox of the swing down, below the feet; only in the air.")]
        [SerializeField] private Vector2 _downHitboxOffset = new Vector2(0f, -1.3f);
        [SerializeField] private Vector2 _downHitboxSize = new Vector2(1.3f, 1.3f);

        [Header("Recoil")]
        [Tooltip("Speed a landed side swing pushes the player back at; with the time below, ~0.4 "
            + "units — enough to feel, short enough that a combo held forward still connects "
            + "(TheThreeHitComboRunsInOrder).")]
        [SerializeField, Min(0f)] private float _recoilSpeed = 7.5f;
        [SerializeField, Min(0f)] private float _recoilTime = 0.05f;
        private PlayerMotor2D _motor;
        private bool _hasBounced;
        private bool _hasRecoiled;
        private readonly AttackRunner _runner = new AttackRunner();
        private IFacing _facing;
        private int _comboIndex;
        private float _comboExpiresAt;

        public event Action<AttackDefinition> AttackStarted;
        public event Action<AttackDefinition> AttackEnded;
        public event Action<CombatActor, DamageResult> HitLanded;

        /// <summary>A swing down struck something and threw the player up.</summary>
        public event Action Pogoed;

        /// <summary>A landed side or up swing pushed the player back (Hollow Knight's recoil).</summary>
        public event Action Recoiled;

        /// <summary>
        /// The weapon the next swing will use. It comes from the equipment loadout; the serialized
        /// fallback exists so a scene without equipment still swings something.
        /// </summary>
        public WeaponDefinition Weapon =>
            _equipment != null && _equipment.ActiveWeaponDefinition != null
                ? _equipment.ActiveWeaponDefinition
                : _fallbackWeapon;

        public bool IsAttacking => _runner.IsBusy;
        public bool CanChain => _runner.CanChain;
        public bool IsCancelable => _runner.CanCancel;
        public AttackDefinition CurrentAttack => _runner.Current;
        public int ComboIndex => _comboIndex;

        /// <summary>The direction of the swing in progress; Side when none is.</summary>
        public AttackDirection CurrentDirection { get; private set; }

        private void Awake()
        {
            _facing = GetComponent<IFacing>();
            _motor = GetComponent<PlayerMotor2D>();
            if (_equipment == null) { _equipment = GetComponent<PlayerEquipment>(); }
            if (_stats == null) { _stats = GetComponent<PlayerStats>(); }
            if (_hitbox == null)
            {
                Debug.LogError("PlayerCombat requires a hitbox.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            _runner.ActiveStarted += OnActiveStarted;
            _runner.ActiveEnded += OnActiveEnded;
            _runner.Finished += OnAttackFinished;
            if (_hitbox != null)
            {
                _hitbox.Hit += OnHit;
            }
            if (_equipment != null)
            {
                _equipment.ActiveWeaponChanged += OnActiveWeaponChanged;
            }
        }

        private void OnDisable()
        {
            _runner.ActiveStarted -= OnActiveStarted;
            _runner.ActiveEnded -= OnActiveEnded;
            _runner.Finished -= OnAttackFinished;
            if (_hitbox != null)
            {
                _hitbox.Hit -= OnHit;
            }
            if (_equipment != null)
            {
                _equipment.ActiveWeaponChanged -= OnActiveWeaponChanged;
            }
            Cancel();
        }

        private void Start()
        {
            OnActiveWeaponChanged(_equipment?.ActiveWeapon);
        }

        /// <summary>
        /// A light swing. Up and down are the weapon's first swing aimed that way — the same
        /// damage, cost and timing, as in Hollow Knight — and leave the combo where it was.
        /// </summary>
        public bool TryStartLight(AttackDirection direction = AttackDirection.Side)
        {
            var weapon = Weapon;
            if (weapon == null)
            {
                return false;
            }

            if (direction != AttackDirection.Side)
            {
                var first = weapon.LightAttack(0);
                if (first == null)
                {
                    return false;
                }

                var aimed = direction == AttackDirection.Up
                    ? first.Aimed(_upHitboxOffset, _upHitboxSize, "AttackUp")
                    : first.Aimed(_downHitboxOffset, _downHitboxSize, "AttackDown");
                return TryBegin(aimed, weapon, direction);
            }

            var next = _runner.IsBusy || Time.time <= _comboExpiresAt
                ? (_comboIndex + 1) % Mathf.Max(1, weapon.ComboLength)
                : 0;
            if (!TryBegin(weapon.LightAttack(next), weapon, AttackDirection.Side))
            {
                return false;
            }

            _comboIndex = next;
            return true;
        }

        public bool TryStartHeavy()
        {
            var weapon = Weapon;
            // A weapon with no heavy move simply ignores the input instead of throwing.
            if (weapon == null || !TryBegin(weapon.HeavyAttack, weapon, AttackDirection.Side))
            {
                return false;
            }

            _comboIndex = 0;
            return true;
        }

        public void Tick(float deltaTime)
        {
            _runner.Tick(deltaTime);
            // Spikes have no hurtbox; the swing down finds them itself while its hitbox is out.
            if (_hitbox.IsActive && CurrentDirection == AttackDirection.Down && !_hasBounced
                && Spikes.Overlap(DownHitboxCentre, _downHitboxSize))
            {
                Bounce();
            }
        }

        private Vector2 DownHitboxCentre => (Vector2)transform.position + _downHitboxOffset;

        /// <summary>One bounce per swing: the rise, and the dash and second jump back.</summary>
        private void Bounce()
        {
            _hasBounced = true;
            _motor?.Bounce();
            Pogoed?.Invoke();
        }

        public void Cancel()
        {
            _runner.Cancel();
            _hitbox?.End();
            CurrentDirection = AttackDirection.Side;
        }

        /// <summary>Drops the combo so a fresh weapon always starts from its first swing.</summary>
        public void ResetCombo()
        {
            _comboIndex = 0;
            _comboExpiresAt = 0f;
        }

        /// <summary>
        /// Starts a swing. The direction is set before anyone hears of the swing, so everything
        /// listening to <see cref="AttackStarted"/> — the trail, the sound — sees the right one.
        /// </summary>
        private bool TryBegin(AttackDefinition attack, WeaponDefinition weapon,
            AttackDirection direction)
        {
            if (attack == null || !enabled)
            {
                return false;
            }

            if (_stamina != null && !_stamina.TrySpend(attack.StaminaCostFor(weapon)))
            {
                return false;
            }

            _hitbox.End();
            _hasBounced = false;
            _hasRecoiled = false;
            CurrentDirection = direction;
            if (direction == AttackDirection.Side)
            {
                AimSideSwing();
            }
            _runner.Begin(attack, weapon.AttackSpeed);
            _comboExpiresAt = Time.time + attack.Duration / weapon.AttackSpeed
                + weapon.ComboResetTime;
            AttackStarted?.Invoke(attack);
            return true;
        }

        /// <summary>
        /// Each side swing goes where the player aims it (user's report 2026-10-28: «swinging right, I
        /// press left and he still swings right»): a mouse click — toward the cursor; keys or a stick —
        /// the way they are held. Within a swing the facing holds (<see cref="LocomotionMode.Attacking"/>).
        /// </summary>
        private void AimSideSwing()
        {
            if (_motor == null)
            {
                return;
            }

            var mouse = UnityEngine.InputSystem.Mouse.current;
            var camera = Camera.main;
            if (mouse != null && camera != null && (mouse.leftButton.isPressed || mouse.rightButton.isPressed))
            {
                var cursor = camera.ScreenToWorldPoint(mouse.position.ReadValue());
                var offset = cursor.x - transform.position.x;
                if (Mathf.Abs(offset) > 0.05f)
                {
                    _motor.Face(offset > 0f ? 1 : -1);
                }

                return;
            }

            if (Mathf.Abs(_motor.Steering) > 0.3f)
            {
                _motor.Face(_motor.Steering > 0f ? 1 : -1);
            }
        }

        private void OnActiveStarted(AttackDefinition attack)
        {
            var facing = _facing?.FacingDirection ?? 1;
            var weapon = Weapon;
            // The scaled base comes from the one calculator the whole game reads, so the number
            // the inventory screen shows is the number this swing actually deals.
            var scaledBase = _stats != null
                ? _stats.ActiveWeaponAttack
                : WeaponDamageCalculator.Evaluate(weapon, null, 0, 0).ScaledBase;
            var isCritical = weapon != null && weapon.CritChance > 0f
                && UnityEngine.Random.value < weapon.CritChance;
            _hitbox.Begin(attack.BuildWeaponDamage(weapon, scaledBase, gameObject, this, facing,
                isCritical), attack.HitboxOffset, attack.HitboxSize, facing);
        }

        private void OnActiveEnded()
        {
            _hitbox.End();
        }

        private void OnAttackFinished(AttackDefinition attack)
        {
            AttackEnded?.Invoke(attack);
            CurrentDirection = AttackDirection.Side;
        }

        private void OnHit(CombatActor target, DamageResult result)
        {
            HitLanded?.Invoke(target, result);
            if (CurrentDirection == AttackDirection.Down && !_hasBounced)
            {
                Bounce();
                return;
            }

            if (!_hasRecoiled && _motor != null)
            {
                var recoil = RecoilOf(CurrentDirection, result.Outcome, _motor.IsGrounded);
                if (recoil.push > 0f || recoil.fall > 0f)
                {
                    _hasRecoiled = true;
                    var facing = _facing?.FacingDirection ?? 1;
                    _motor.Recoil(-facing * recoil.push * _recoilSpeed, recoil.fall, _recoilTime);
                    Recoiled?.Invoke();
                }
            }
        }

        /// <summary>
        /// How a landed hit pushes the player back (the user's choice, 2026-09-27, after Hollow
        /// Knight): a side swing pushes away from what it hit, harder off a shield; a swing up in
        /// the air presses the fall; the swing down has its pogo instead. A hit the target ignored
        /// (its invulnerability) pushes nothing.
        /// </summary>
        public static (float push, float fall) RecoilOf(AttackDirection direction,
            DamageOutcome outcome, bool grounded)
        {
            if (outcome == DamageOutcome.Ignored)
            {
                return (0f, 0f);
            }

            switch (direction)
            {
                case AttackDirection.Side:
                    return (outcome == DamageOutcome.Blocked ? BlockedRecoilScale : 1f, 0f);
                case AttackDirection.Up:
                    return (0f, grounded ? 0f : UpRecoilFallSpeed);
                default:
                    return (0f, 0f);
            }
        }

        /// <summary>How much harder a shield pushes back than flesh.</summary>
        public const float BlockedRecoilScale = 1.6f;

        /// <summary>The fall a swing up presses in the air, in units per second.</summary>
        public const float UpRecoilFallSpeed = 3f;

        /// <summary>A new weapon never inherits the old one's combo position or swing.</summary>
        private void OnActiveWeaponChanged(EquippedWeapon weapon)
        {
            Cancel();
            ResetCombo();
            _weaponVisual?.Show(weapon?.Definition ?? _fallbackWeapon);
        }
    }
}
