using System;
using UnityEngine;

namespace ForgottenKingdom
{
    public enum SkillActivationResult
    {
        Activated,
        EmptySlot,
        NotUnlocked,
        OnCooldown,
        NotEnoughStamina,
        /// <summary>Dead, stunned, guard-broken, mid-swing or already using a skill.</summary>
        BusyOrIncapacitated,
        InvalidWeaponType,
        NotEnoughMana
    }

    /// <summary>
    /// The two skills the player has to hand, and everything about using them: what is in each
    /// slot, whether it may fire, its cooldown and the swing itself. The swing is an ordinary
    /// <see cref="AttackDefinition"/> driven through the ordinary hitbox, so an active skill is
    /// blocked, parried and mitigated exactly like any other attack.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ActiveSkillController : MonoBehaviour
    {
        public const int SlotCount = 2;

        [SerializeField] private PlayerSkills _skills;
        [SerializeField] private PlayerEquipment _equipment;
        [SerializeField] private PlayerStats _stats;
        [SerializeField] private PlayerOffense _offense;
        [SerializeField] private PlayerMotor2D _motor;
        [SerializeField] private Stamina _stamina;
        [SerializeField] private Hitbox _hitbox;
        [SerializeField] private CombatActor _actor;
        [SerializeField] private GameProgress _progress;
        private readonly AttackRunner _runner = new AttackRunner();
        private readonly string[] _slots = new string[SlotCount];
        private readonly float[] _readyAt = new float[SlotCount];
        private IFacing _facing;
        private ShadowAbilities _shadow;
        private Mana _mana;
        private PlayerCharacter _character;
        private SkillDefinition _running;
        private int _runningSlot = -1;

        public event Action<int, SkillDefinition> SlotChanged;
        public event Action<SkillDefinition> SkillStarted;
        public event Action<SkillDefinition> SkillEnded;

        public bool IsRunning => _runner.IsBusy;
        public SkillDefinition RunningSkill => _running;

        public string SlotSkillId(int slot) =>
            slot >= 0 && slot < SlotCount ? _slots[slot] ?? string.Empty : string.Empty;

        public SkillDefinition SlotSkill(int slot)
        {
            var id = SlotSkillId(slot);
            return _skills != null && _skills.Database != null && !string.IsNullOrEmpty(id)
                ? _skills.Database.GetSkill(id)
                : null;
        }

        /// <summary>Seconds left before this slot may fire again; zero when it is ready.</summary>
        public float CooldownRemaining(int slot)
        {
            return slot >= 0 && slot < SlotCount
                ? Mathf.Max(0f, _readyAt[slot] - Time.time)
                : 0f;
        }

        public float CooldownNormalized(int slot)
        {
            var skill = SlotSkill(slot);
            var length = skill?.Active != null ? skill.Active.Cooldown : 0f;
            return length > 0f ? Mathf.Clamp01(CooldownRemaining(slot) / length) : 0f;
        }

        public bool IsReady(int slot) => CooldownRemaining(slot) <= 0f;

        private void Awake()
        {
            if (_skills == null) { _skills = FindAnyObjectByType<PlayerSkills>(); }
            if (_progress == null) { _progress = FindAnyObjectByType<GameProgress>(); }
            if (_equipment == null) { _equipment = GetComponent<PlayerEquipment>(); }
            if (_stats == null) { _stats = GetComponent<PlayerStats>(); }
            if (_offense == null) { _offense = GetComponent<PlayerOffense>(); }
            if (_motor == null) { _motor = GetComponent<PlayerMotor2D>(); }
            if (_stamina == null) { _stamina = GetComponent<Stamina>(); }
            if (_actor == null) { _actor = GetComponent<CombatActor>(); }
            _facing = GetComponent<IFacing>();
            _shadow = GetComponent<ShadowAbilities>();
            _mana = GetComponent<Mana>();
            _character = GetComponent<PlayerCharacter>();
        }

        private void OnEnable()
        {
            _runner.ActiveStarted += OnActiveStarted;
            _runner.ActiveEnded += OnActiveEnded;
            _runner.Finished += OnFinished;
        }

        private void OnDisable()
        {
            _runner.ActiveStarted -= OnActiveStarted;
            _runner.ActiveEnded -= OnActiveEnded;
            _runner.Finished -= OnFinished;
            Cancel();
        }

        private void Start()
        {
            LoadSlots();
        }

        // ---------- slots ----------

        public void LoadSlots()
        {
            if (_progress == null)
            {
                return;
            }

            SetSlotInternal(0, _progress.Data.player.activeSkillSlot1);
            SetSlotInternal(1, _progress.Data.player.activeSkillSlot2);
        }

        /// <summary>
        /// Puts an unlocked active skill in a slot. The same skill cannot sit in both slots, and
        /// an empty string clears one.
        /// </summary>
        public bool AssignSlot(int slot, string skillId)
        {
            if (slot < 0 || slot >= SlotCount)
            {
                return false;
            }

            if (string.IsNullOrEmpty(skillId))
            {
                SetSlotInternal(slot, string.Empty);
                WriteBack();
                return true;
            }

            if (_skills == null || _skills.Database == null
                || !_skills.Database.TryGetSkill(skillId, out var skill)
                || !skill.IsActive || !_skills.HasSkill(skillId))
            {
                return false;
            }

            // Holding the same skill twice would be two cooldowns for one ability.
            for (var other = 0; other < SlotCount; other++)
            {
                if (other != slot && _slots[other] == skillId)
                {
                    SetSlotInternal(other, string.Empty);
                }
            }

            SetSlotInternal(slot, skillId);
            WriteBack();
            return true;
        }

        private void SetSlotInternal(int slot, string skillId)
        {
            skillId ??= string.Empty;
            // A skill that is no longer owned, or no longer in the build, leaves the bar.
            if (!string.IsNullOrEmpty(skillId)
                && (_skills == null || !_skills.HasSkill(skillId)))
            {
                skillId = string.Empty;
            }

            if (_slots[slot] == skillId)
            {
                return;
            }

            _slots[slot] = skillId;
            SlotChanged?.Invoke(slot, SlotSkill(slot));
        }

        private void WriteBack()
        {
            if (_progress == null)
            {
                return;
            }

            _progress.Data.player.activeSkillSlot1 = _slots[0] ?? string.Empty;
            _progress.Data.player.activeSkillSlot2 = _slots[1] ?? string.Empty;
            _progress.Save();
        }

        // ---------- using them ----------

        public SkillActivationResult CanActivate(int slot)
        {
            var skill = SlotSkill(slot);
            if (skill == null)
            {
                return string.IsNullOrEmpty(SlotSkillId(slot))
                    ? SkillActivationResult.EmptySlot
                    : SkillActivationResult.NotUnlocked;
            }

            if (!_skills.HasSkill(skill.SkillId))
            {
                return SkillActivationResult.NotUnlocked;
            }

            if (IsRunning || (_character != null && (!_character.IsAlive
                || _character.Action == PlayerAction.Stunned || _character.Action == PlayerAction.GuardBroken
                || _character.Action == PlayerAction.Hurt || _character.Action == PlayerAction.Casting)))
            {
                return SkillActivationResult.BusyOrIncapacitated;
            }

            if (!IsReady(slot))
            {
                return SkillActivationResult.OnCooldown;
            }

            if (skill.Active.RequiresMeleeWeapon && !HasMeleeWeapon())
            {
                return SkillActivationResult.InvalidWeaponType;
            }

            if (skill.Active.ManaCost > 0f && (_mana == null || !_mana.Has(ManaCost(skill))))
            {
                return SkillActivationResult.NotEnoughMana;
            }

            if (_stamina != null && !_stamina.Has(StaminaCost(skill)))
            {
                return SkillActivationResult.NotEnoughStamina;
            }

            return SkillActivationResult.Activated;
        }

        /// <summary>
        /// Fires a slot. Nothing is spent and no cooldown starts unless the skill really begins,
        /// so a refused press costs the player nothing.
        /// </summary>
        public SkillActivationResult Activate(int slot)
        {
            var verdict = CanActivate(slot);
            if (verdict != SkillActivationResult.Activated)
            {
                return verdict;
            }

            var skill = SlotSkill(slot);
            var data = skill.Active;
            if (_stamina != null && !_stamina.TrySpend(StaminaCost(skill)))
            {
                return SkillActivationResult.NotEnoughStamina;
            }

            if (data.ManaCost > 0f) { _mana.TrySpend(ManaCost(skill)); }
            _running = skill;
            _runningSlot = slot;
            _readyAt[slot] = Time.time + data.Cooldown;
            _hitbox.End();
            if (data.ShadowAction != ShadowAction.None) { _shadow?.Begin(data.ShadowAction); }
            _runner.Begin(data.Attack, 1f);
            _offense?.BeginSwing(SwingKind.ActiveSkill);
            // A lunging skill owns the player's movement for its whole length; a planted one
            // sets zero and simply stands its ground.
            _motor?.ApplyImpulse(new Vector2((_facing?.FacingDirection ?? 1) * data.LungeSpeed, 0f));
            if (data.Invulnerability > 0f)
            {
                _actor?.SetInvulnerable(ImmunitySource.Script, data.Invulnerability);
            }

            SkillStarted?.Invoke(skill);
            return SkillActivationResult.Activated;
        }

        public void Tick(float deltaTime)
        {
            if (!_runner.IsBusy)
            {
                return;
            }

            _runner.Tick(deltaTime);
        }

        public void Cancel()
        {
            _runner.Cancel();
            _shadow?.Cancel();
            _hitbox?.End();
            _running = null;
            _runningSlot = -1;
        }

        /// <summary>Death and resting both clear cooldowns; they are never saved.</summary>
        public void ResetCooldowns()
        {
            for (var i = 0; i < SlotCount; i++)
            {
                _readyAt[i] = 0f;
            }
        }

        private float CostScale(SkillDefinition skill) => skill.Active.ShadowAction != ShadowAction.None
            ? Mathf.Clamp(_skills.Multiplier(SkillEffectType.ShadowCost), 0.25f, 1f) : 1f;
        public float ManaCost(SkillDefinition skill) => skill.Active.ManaCost * CostScale(skill);
        public float StaminaCost(SkillDefinition skill) => skill.Active.Attack.StaminaCost * CostScale(skill);

        private bool HasMeleeWeapon()
        {
            var weapon = _equipment != null ? _equipment.ActiveWeaponDefinition : null;
            if (weapon == null)
            {
                return false;
            }

            return weapon.WeaponType != WeaponType.Bow && weapon.WeaponType != WeaponType.Staff;
        }

        private void OnActiveStarted(AttackDefinition attack)
        {
            if (_running != null && _running.Active.ShadowAction != ShadowAction.None && _shadow != null)
            {
                if (_shadow.Action == ShadowAction.Strike) { _shadow.StrikeDisplacement(); }
                if (_shadow.Action == ShadowAction.Strike || _shadow.CanCut)
                {
                    _hitbox.Begin(_shadow.BuildDamage(attack), attack.HitboxOffset,
                        attack.HitboxSize, _shadow.Facing);
                }
                return;
            }
            var facing = _facing?.FacingDirection ?? 1;
            var scaledBase = _stats != null ? _stats.ActiveWeaponAttack : 0f;
            var weapon = _equipment != null ? _equipment.ActiveWeaponDefinition : null;
            _offense?.BeginSwing(SwingKind.ActiveSkill);
            _hitbox.Begin(attack.BuildWeaponDamage(weapon, scaledBase, gameObject, this, facing),
                attack.HitboxOffset, attack.HitboxSize, facing);
        }

        private void OnActiveEnded()
        {
            _hitbox.End();
        }

        private void OnFinished(AttackDefinition attack)
        {
            var finished = _running;
            _shadow?.Cancel();
            _running = null;
            _runningSlot = -1;
            SkillEnded?.Invoke(finished);
        }
    }
}
