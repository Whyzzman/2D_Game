using System;
using UnityEngine;

namespace ForgottenKingdom
{
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
        private readonly AttackRunner _runner = new AttackRunner();
        private IFacing _facing;
        private int _comboIndex;
        private float _comboExpiresAt;

        public event Action<AttackDefinition> AttackStarted;
        public event Action<AttackDefinition> AttackEnded;
        public event Action<CombatActor, DamageResult> HitLanded;

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

        private void Awake()
        {
            _facing = GetComponent<IFacing>();
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

        public bool TryStartLight()
        {
            var weapon = Weapon;
            if (weapon == null)
            {
                return false;
            }

            var next = _runner.IsBusy || Time.time <= _comboExpiresAt
                ? (_comboIndex + 1) % Mathf.Max(1, weapon.ComboLength)
                : 0;
            if (!TryBegin(weapon.LightAttack(next), weapon))
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
            if (weapon == null || !TryBegin(weapon.HeavyAttack, weapon))
            {
                return false;
            }

            _comboIndex = 0;
            return true;
        }

        public void Tick(float deltaTime)
        {
            _runner.Tick(deltaTime);
        }

        public void Cancel()
        {
            _runner.Cancel();
            _hitbox?.End();
        }

        /// <summary>Drops the combo so a fresh weapon always starts from its first swing.</summary>
        public void ResetCombo()
        {
            _comboIndex = 0;
            _comboExpiresAt = 0f;
        }

        private bool TryBegin(AttackDefinition attack, WeaponDefinition weapon)
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
            _runner.Begin(attack, weapon.AttackSpeed);
            _comboExpiresAt = Time.time + attack.Duration / weapon.AttackSpeed
                + weapon.ComboResetTime;
            AttackStarted?.Invoke(attack);
            return true;
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
        }

        private void OnHit(CombatActor target, DamageResult result)
        {
            HitLanded?.Invoke(target, result);
        }

        /// <summary>A new weapon never inherits the old one's combo position or swing.</summary>
        private void OnActiveWeaponChanged(EquippedWeapon weapon)
        {
            Cancel();
            ResetCombo();
            _weaponVisual?.Show(weapon?.Definition ?? _fallbackWeapon);
        }
    }
}
