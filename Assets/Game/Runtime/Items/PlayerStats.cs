using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The player's real numbers: saved progression as the base, everything worn on top, resolved
    /// in one pass every time the loadout changes. Nothing else adds a bonus onto a live stat, so
    /// equipping and unequipping the same ring for an hour leaves the character exactly as it
    /// started. It also answers the damage pipeline's question about armour.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerStats : MonoBehaviour, IDamageMitigation
    {
        [SerializeField] private PlayerEquipment _equipment;
        [SerializeField] private PlayerProgressionService _progression;
        [SerializeField] private PlayerSkills _skills;
        [SerializeField] private PlayerMotor2D _motor;
        [SerializeField] private TemporaryBuffs _buffs;
        [SerializeField] private Health _health;
        [SerializeField] private Stamina _stamina;
        [SerializeField] private Mana _mana;
        [SerializeField] private StaggerMeter _stagger;
        [SerializeField] private GameProgress _progress;
        [Tooltip("Larger means armour matters less per point; the curve never reaches zero damage.")]
        [SerializeField, Min(1f)] private float _defenseConstant = 60f;
        [Tooltip("Share of a hit that always gets through, however heavy the armour.")]
        [SerializeField, Range(0.05f, 1f)] private float _minimumDamageShare = 0.15f;
        [Tooltip("The player's own poise resistance before any armour.")]
        [SerializeField, Range(0f, 0.9f)] private float _baseStaggerResistance = 0f;
        private readonly StatBlock _stats = new StatBlock();
        private readonly List<StatModifier> _modifiers = new List<StatModifier>();
        private readonly List<string> _sources = new List<string>();
        private readonly CharacterStatBlock _finalStats = new CharacterStatBlock();
        private WeaponDamageBreakdown _weaponDamage;
        private float _baseMaxHealth = 100f;
        private float _baseMaxStamina = 100f;
        private float _baseMaxMana = 100f;

        /// <summary>Raised after a recalculation so the HUD reads the new maxima once.</summary>
        public event Action StatsChanged;

        public float MaxHealth => _stats.Get(StatType.MaxHealth);
        public float MaxStamina => _stats.Get(StatType.MaxStamina);
        public float MaxMana => _stats.Get(StatType.MaxMana);
        public float ShadowPower => Curve != null
            ? Curve.ShadowPowerFor(GetFinalStat(CharacterStat.Will)) : 1f;
        public float StatusResistance(StatusType type)
        {
            var stat = type == StatusType.Bleed ? StatType.BleedResistance
                : type == StatusType.Poison ? StatType.PoisonResistance
                : type == StatusType.Burn ? StatType.BurnResistance : StatType.FrostStatusResistance;
            return Mathf.Clamp((Curve != null
                ? Curve.StatusResistanceFor(GetFinalStat(CharacterStat.Will)) : 0f)
                + _stats.Get(stat), 0f, 0.95f);
        }

        /// <summary>The balancing curve everything derived is worked out from.</summary>
        public ProgressionCurve Curve => _progression != null ? _progression.Curve : null;
        public float PhysicalDefense => Mathf.Max(0f, _stats.Get(StatType.PhysicalDefense));
        public float AttackPower => _stats.Get(StatType.AttackPower);
        public float EquipmentWeight => Mathf.Max(0f, _stats.Get(StatType.EquipmentWeight));
        public float StaggerResistance =>
            Mathf.Clamp(_stats.Get(StatType.StaggerResistance), 0f, 0.95f);

        /// <summary>Every worn modifier with the item it came from; the debug overlay prints it.</summary>
        public IReadOnlyList<StatModifier> ActiveModifiers => _modifiers;
        public IReadOnlyList<string> ModifierSources => _sources;

        /// <summary>What the active weapon is worth in these hands, in its parts.</summary>
        public WeaponDamageBreakdown WeaponDamage => _weaponDamage;
        public float ActiveWeaponAttack => _weaponDamage.ScaledBase;
        public bool MeetsWeaponRequirements => _weaponDamage.MeetsRequirements;
        public float MaxEquipLoad { get; private set; }
        public float EquipLoadRatio => MaxEquipLoad > 0f ? EquipmentWeight / MaxEquipLoad : 0f;

        /// <summary>
        /// The attribute as it really counts: what the player chose, plus anything worn that
        /// grants it. Weapon requirements and scaling both read this, so a ring of strength
        /// genuinely lets a heavier weapon be picked up.
        /// </summary>
        public int GetFinalStat(CharacterStat stat)
        {
            return _finalStats.Get(stat);
        }

        public int GetBaseStat(CharacterStat stat)
        {
            return _progression != null ? _progression.GetBase(stat) : 0;
        }

        public int GetAllocatedStat(CharacterStat stat)
        {
            return _progression != null ? _progression.GetAllocated(stat) : 0;
        }

        public int GetEquipmentBonus(CharacterStat stat)
        {
            return _finalStats.Get(stat) - (_progression != null ? _progression.GetChosen(stat) : 0);
        }

        private void Awake()
        {
            if (_equipment == null) { _equipment = GetComponent<PlayerEquipment>(); }
            if (_progression == null) { _progression = FindAnyObjectByType<PlayerProgressionService>(); }
            if (_skills == null) { _skills = FindAnyObjectByType<PlayerSkills>(); }
            if (_motor == null) { _motor = GetComponent<PlayerMotor2D>(); }
            if (_buffs == null) { _buffs = GetComponent<TemporaryBuffs>(); }
            if (_health == null) { _health = GetComponent<Health>(); }
            if (_stamina == null) { _stamina = GetComponent<Stamina>(); }
            if (_mana == null) { _mana = GetComponent<Mana>(); }
            if (_stagger == null) { _stagger = GetComponent<StaggerMeter>(); }
            if (_progress == null) { _progress = FindAnyObjectByType<GameProgress>(); }
            _baseMaxHealth = _health != null ? _health.Max : _baseMaxHealth;
            _baseMaxStamina = _stamina != null ? _stamina.Max : _baseMaxStamina;
        }

        private void OnEnable()
        {
            if (_equipment != null)
            {
                _equipment.EquipmentChanged += Recalculate;
            }
            if (_progression != null)
            {
                _progression.StatChanged += OnStatChanged;
                _progression.LevelChanged += OnLevelChanged;
            }
            if (_skills != null) { _skills.SkillsChanged += Recalculate; }
            if (_buffs != null) { _buffs.BuffsChanged += OnBuffsChanged; }
            if (_progress != null)
            {
                _progress.WorldChanged += OnWorldChanged;
                _progress.Loaded += Recalculate;
            }
        }

        private void OnDisable()
        {
            if (_equipment != null)
            {
                _equipment.EquipmentChanged -= Recalculate;
            }
            if (_progression != null)
            {
                _progression.StatChanged -= OnStatChanged;
                _progression.LevelChanged -= OnLevelChanged;
            }
            if (_skills != null) { _skills.SkillsChanged -= Recalculate; }
            if (_buffs != null) { _buffs.BuffsChanged -= OnBuffsChanged; }
            if (_progress != null)
            {
                _progress.WorldChanged -= OnWorldChanged;
                _progress.Loaded -= Recalculate;
            }
        }

        /// <summary>
        /// A vessel shard picked up: the bars grow once four (or three) are whole, and a vessel
        /// made whole fills its bar, as a completed mask does in Hollow Knight.
        /// </summary>
        private void OnWorldChanged(WorldFlag flag, string id)
        {
            if (flag != WorldFlag.RewardGranted || !VesselShards.TryKind(id, out var kind))
            {
                return;
            }

            Recalculate();
            if (VesselShards.Partial(_progress, kind) == 0)
            {
                if (kind == VesselKind.Health) { _health?.RestoreToFull(); }
                else { _mana?.RestoreToFull(); }
            }
        }

        /// <summary>
        /// A temporary bonus reaches stamina regeneration here rather than in the buff store, so
        /// the buff system stays a plain timer and knows nothing about what it is buffing.
        /// </summary>
        private void OnBuffsChanged()
        {
            _stamina?.SetRegenerationBonus(_buffs.Value(SkillEffectType.AdrenalineRegen));
            // A buff that changes a lasting stat goes through the ordinary recalculation, so a
            // buff expiring can never leave its bonus behind.
            Recalculate();
        }

        private void OnStatChanged(CharacterStat stat, int previous, int current) => Recalculate();

        /// <summary>
        /// A level on its own changes nothing until its point is spent, but the recalculation is
        /// cheap and keeps the screen honest if that ever stops being true.
        /// </summary>
        private void OnLevelChanged(int previous, int current) => Recalculate();

        /// <summary>
        /// Saved progression sets the floor everything else builds on. Once attributes exist the
        /// curve decides the bars and these values are only the fallback for a scene that has no
        /// progression service at all.
        /// </summary>
        public void SetBaseProgression(PlayerProgressionData progression)
        {
            if (progression == null)
            {
                return;
            }

            _baseMaxHealth = Mathf.Max(1f, progression.maxHealth);
            _baseMaxStamina = Mathf.Max(1f, progression.maxStamina);
            Recalculate();
        }

        /// <summary>
        /// Base values, then every worn modifier, then the result pushed into the live components.
        /// The bars keep their current fill: a bigger maximum is not free health, and a smaller one
        /// only clamps.
        /// </summary>
        public void Recalculate()
        {
            // The order matters and is fixed: what the player chose, then what they wear, then
            // the attributes those two add up to, then everything derived from them. Nothing is
            // ever added onto last time's answer.
            _stats.ClearModifiers();
            _modifiers.Clear();
            _sources.Clear();
            foreach (var stat in CharacterStats.Allocatable)
            {
                _stats.SetBase(ToStatType(stat),
                    _progression != null ? _progression.GetChosen(stat) : 0);
            }

            _stats.SetBase(StatType.PhysicalDefense, 0f);
            _stats.SetBase(StatType.StaggerResistance, _baseStaggerResistance);
            _stats.SetBase(StatType.EquipmentWeight, 0f);
            _stats.SetBase(StatType.AttackPower, 0f);
            _stats.SetBase(StatType.MaxHealth, 0f);
            _stats.SetBase(StatType.MaxStamina, 0f);
            _stats.SetBase(StatType.MaxMana, 0f);

            if (_equipment != null)
            {
                _equipment.CollectModifiers(_modifiers, _sources);
                _stats.Apply(_modifiers);
                ApplyArmor();
            }

            // Skills that change a lasting stat go through the same pass as equipment, so a
            // reload recomputes them once rather than stacking them onto the previous answer.
            if (_skills != null)
            {
                _stats.Apply(new StatModifier(StatType.StaggerResistance, ModifierOperation.Add,
                    _skills.Effect(SkillEffectType.StaggerResistance)));
                _stats.Apply(new StatModifier(StatType.MaxMana, ModifierOperation.Add,
                    _skills.Effect(SkillEffectType.MaxMana)));
            }

            if (_buffs != null)
            {
                _stats.Apply(new StatModifier(StatType.StaggerResistance, ModifierOperation.Add,
                    _buffs.Value(SkillEffectType.SecondWindResistance)));
            }

            foreach (var stat in CharacterStats.Allocatable)
            {
                _finalStats.Set(stat, Mathf.RoundToInt(_stats.Get(ToStatType(stat))));
            }

            // Attributes decide the size of the bars; the modifier list only adds on top.
            var curve = _progression != null ? _progression.Curve : null;
            var healthFromStats = curve != null
                ? curve.HealthFor(_finalStats.vitality)
                : _baseMaxHealth;
            var staminaFromStats = curve != null
                ? curve.StaminaFor(_finalStats.endurance)
                : _baseMaxStamina;
            var manaFromStats = curve != null
                ? curve.ManaFor(_finalStats.intelligence)
                : _baseMaxMana;
            _stats.SetBase(StatType.MaxHealth, healthFromStats);
            _stats.SetBase(StatType.MaxStamina, staminaFromStats);
            _stats.SetBase(StatType.MaxMana, manaFromStats);
            // Whole vessels found in the world widen the bars by a share of all the rest.
            _stats.Apply(new StatModifier(StatType.MaxHealth, ModifierOperation.Multiply,
                VesselShards.Multiplier(_progress, VesselKind.Health)));
            _stats.Apply(new StatModifier(StatType.MaxMana, ModifierOperation.Multiply,
                VesselShards.Multiplier(_progress, VesselKind.Mana)));
            MaxEquipLoad = curve != null ? curve.EquipLoadFor(_finalStats.endurance) : 0f;

            EvaluateWeapon(curve);

            _health?.SetMax(MaxHealth, false);
            _stamina?.SetMax(MaxStamina, false);
            _mana?.SetMax(MaxMana, false);
            _stagger?.SetResistance(StaggerResistance);
            _motor?.SetDashCooldownScale(_skills != null
                ? _skills.Multiplier(SkillEffectType.DashCooldown)
                : 1f);
            StatsChanged?.Invoke();
        }

        /// <summary>
        /// Works out what the weapon in hand is worth. A weapon whose requirements are no longer
        /// met is not torn out of the player's hands mid-fight; it simply hits for less, which is
        /// legible and never interrupts a fight.
        /// </summary>
        private void EvaluateWeapon(ProgressionCurve curve)
        {
            var weapon = _equipment != null ? _equipment.ActiveWeaponDefinition : null;
            var meets = weapon == null
                || weapon.MeetsRequirements(_finalStats.strength, _finalStats.dexterity);
            _weaponDamage = WeaponDamageCalculator.Evaluate(weapon, curve, _finalStats.strength,
                _finalStats.dexterity, _stats.Get(StatType.AttackPower), meets);
        }

        private static StatType ToStatType(CharacterStat stat)
        {
            switch (stat)
            {
                case CharacterStat.Strength: return StatType.Strength;
                case CharacterStat.Dexterity: return StatType.Dexterity;
                case CharacterStat.Vitality: return StatType.Vitality;
                case CharacterStat.Endurance: return StatType.Endurance;
                case CharacterStat.Will: return StatType.Will;
                default: return StatType.Intelligence;
            }
        }

        /// <summary>
        /// Armour's defence, weight and poise bonus are ordinary stats too; they are read off the
        /// worn pieces rather than authored as modifier lines, because every armour has them.
        /// </summary>
        private void ApplyArmor()
        {
            foreach (var slot in EquipmentSlots.All)
            {
                var armor = _equipment.ArmorIn(slot);
                if (armor == null)
                {
                    continue;
                }

                _stats.Apply(new StatModifier(StatType.PhysicalDefense, ModifierOperation.Add,
                    armor.Defense));
                _stats.Apply(new StatModifier(StatType.EquipmentWeight, ModifierOperation.Add,
                    armor.Weight));
                if (armor.StaggerResistanceBonus > 0f)
                {
                    _stats.Apply(new StatModifier(StatType.StaggerResistance,
                        ModifierOperation.Add, armor.StaggerResistanceBonus));
                }
            }
        }

        /// <summary>
        /// Armour never subtracts flat damage, which would make a heavy set immune to weak hits.
        /// It scales the blow instead, with diminishing returns and a floor, so every attack always
        /// hurts a little and no equipment can produce negative damage.
        /// </summary>
        public float Mitigate(in DamageInfo info)
        {
            if (info.Amount <= 0f)
            {
                return 0f;
            }

            var defense = PhysicalDefense;
            var armour = defense > 0f
                ? Mathf.Max(_minimumDamageShare, _defenseConstant / (_defenseConstant + defense))
                : 1f;
            return info.Amount * armour * ShieldFactor;
        }

        /// <summary>
        /// What a ward spell currently absorbs. It is a share, never all of it: a shield that
        /// made the player untouchable would replace dodging and blocking rather than join them.
        /// </summary>
        public float ShieldFactor => _buffs != null
            ? Mathf.Clamp(1f - _buffs.Value(SkillEffectType.SpellDamageReduction), 0.2f, 1f)
            : 1f;
    }
}
