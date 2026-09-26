using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Every number that turns progress into power, in one asset. The level cost, what a level is
    /// worth, and how attributes become health, stamina and carrying capacity all live here, so
    /// balancing the game never means editing code.
    /// </summary>
    [CreateAssetMenu(menuName = "Forgotten Kingdom/Progression Curve", fileName = "ProgressionCurve")]
    public sealed class ProgressionCurve : ScriptableObject
    {
        [Header("Levels")]
        [Tooltip("Experience the first level costs; later levels scale off this.")]
        [SerializeField, Min(1)] private int _baseExperience = 100;
        [Tooltip("Above 1 makes each level cost disproportionately more.")]
        [SerializeField, Min(0.1f)] private float _experienceExponent = 0.85f;
        [Tooltip("Costs are rounded up to this, so the numbers read as designed values.")]
        [SerializeField, Min(1)] private int _experienceRounding = 10;
        [SerializeField, Min(1)] private int _maxLevel = 100;
        [SerializeField, Min(0)] private int _statPointsPerLevel = 1;

        [Header("Starting attributes")]
        [SerializeField, Min(0)] private int _startingStrength = 5;
        [SerializeField, Min(0)] private int _startingDexterity = 5;
        [SerializeField, Min(0)] private int _startingVitality = 5;
        [SerializeField, Min(0)] private int _startingEndurance = 5;
        [SerializeField, Min(0)] private int _startingIntelligence = 5;
        [SerializeField, Min(0)] private int _startingWill = 5;
        [SerializeField, Min(0f)] private float _shadowScaleFactor = 0.035f;
        [SerializeField, Min(0f)] private float _statusResistancePerWill = 0.003f;
        [SerializeField, Range(0f, 0.8f)] private float _maxWillResistance = 0.3f;
        [SerializeField, Min(1)] private int _statCap = 99;

        [Header("Health and stamina")]
        [SerializeField, Min(1f)] private float _baseHealth = 100f;
        [SerializeField, Min(1f)] private float _baseStamina = 100f;
        [Tooltip("Health per point of vitality below the first breakpoint.")]
        [SerializeField, Min(0f)] private float _healthPerVitality = 10f;
        [SerializeField, Min(0f)] private float _healthPerVitalityMid = 7f;
        [SerializeField, Min(0f)] private float _healthPerVitalityHigh = 4f;
        [SerializeField, Min(0f)] private float _staminaPerEndurance = 6f;
        [SerializeField, Min(0f)] private float _staminaPerEnduranceMid = 4f;
        [SerializeField, Min(0f)] private float _staminaPerEnduranceHigh = 2f;
        [Tooltip("Attribute value where the rate drops to the middle band.")]
        [SerializeField, Min(1)] private int _softCap = 20;
        [Tooltip("Attribute value where the rate drops to the lowest band.")]
        [SerializeField, Min(1)] private int _hardCap = 40;

        [Header("Mana")]
        [SerializeField, Min(1f)] private float _baseMana = 100f;
        [SerializeField, Min(0f)] private float _manaPerIntelligence = 8f;
        [SerializeField, Min(0f)] private float _manaPerIntelligenceMid = 6f;
        [SerializeField, Min(0f)] private float _manaPerIntelligenceHigh = 3f;
        [Tooltip("How much one point of intelligence is worth to a spell, before its own scaling.")]
        [SerializeField, Min(0f)] private float _spellScaleFactor = 0.035f;

        [Header("Carrying capacity")]
        [SerializeField, Min(0f)] private float _baseEquipLoad = 30f;
        [SerializeField, Min(0f)] private float _equipLoadPerEndurance = 2f;

        [Header("Weapon scaling")]
        [Tooltip("How much one point of a scaling attribute is worth, before the weapon's own "
            + "coefficient. Small on purpose: ten points should be felt, not decisive.")]
        [SerializeField, Min(0f)] private float _scaleFactor = 0.03f;
        [Tooltip("Damage kept while wielding a weapon whose requirements are not met.")]
        [SerializeField, Range(0.1f, 1f)] private float _requirementPenalty = 0.6f;

        public int MaxLevel => _maxLevel;
        public int StatPointsPerLevel => _statPointsPerLevel;
        public int StatCap => _statCap;
        public float BaseHealth => _baseHealth;
        public float BaseStamina => _baseStamina;
        public float BaseMana => _baseMana;
        public float ScaleFactor => _scaleFactor;
        public float SpellScaleFactor => _spellScaleFactor;
        public float ShadowPowerFor(int will) => 1f + Mathf.Max(0, will) * _shadowScaleFactor;
        public float StatusResistanceFor(int will) => Mathf.Min(_maxWillResistance,
            Mathf.Max(0, will) * _statusResistancePerWill);
        public float RequirementPenalty => _requirementPenalty;

        public CharacterStatBlock StartingStats()
        {
            return new CharacterStatBlock
            {
                strength = _startingStrength,
                dexterity = _startingDexterity,
                vitality = _startingVitality,
                endurance = _startingEndurance,
                intelligence = _startingIntelligence,
                will = _startingWill
            };
        }

        public int StartingStat(CharacterStat stat)
        {
            return StartingStats().Get(stat);
        }

        /// <summary>
        /// Experience needed to go from <paramref name="level"/> to the next one. Always at least
        /// one, so no curve setting can produce a free level or a division by zero.
        /// </summary>
        public int ExperienceForNextLevel(int level)
        {
            if (level >= _maxLevel)
            {
                return 0;
            }

            var raw = _baseExperience * Mathf.Pow(Mathf.Max(1, level), _experienceExponent);
            var rounded = Mathf.CeilToInt(raw / _experienceRounding) * _experienceRounding;
            return Mathf.Max(1, rounded);
        }

        public float HealthFor(int vitality)
        {
            return _baseHealth + Banded(_startingVitality, vitality, _healthPerVitality,
                _healthPerVitalityMid, _healthPerVitalityHigh);
        }

        public float StaminaFor(int endurance)
        {
            return _baseStamina + Banded(_startingEndurance, endurance, _staminaPerEndurance,
                _staminaPerEnduranceMid, _staminaPerEnduranceHigh);
        }

        public float ManaFor(int intelligence)
        {
            return _baseMana + Banded(_startingIntelligence, intelligence, _manaPerIntelligence,
                _manaPerIntelligenceMid, _manaPerIntelligenceHigh);
        }

        public float EquipLoadFor(int endurance)
        {
            return _baseEquipLoad + Mathf.Max(0, endurance) * _equipLoadPerEndurance;
        }

        /// <summary>
        /// Points above the starting value pay the first rate up to the soft cap, a lower rate to
        /// the hard cap, and the lowest above it. Today the rates can be set equal for a flat
        /// curve; the bands are here so tuning later is a number, not a rewrite.
        /// </summary>
        private float Banded(int start, int value, float low, float mid, float high)
        {
            var total = 0f;
            for (var point = start; point < value; point++)
            {
                total += point < _softCap ? low : point < _hardCap ? mid : high;
            }

            return total;
        }
    }
}
