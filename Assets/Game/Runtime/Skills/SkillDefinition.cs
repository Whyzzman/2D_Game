using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The trees a skill can belong to. Mage and Shadow are declared so the data, the database and
    /// the screen already handle them; nothing authors skills for them yet.
    /// </summary>
    public enum SkillBranch
    {
        Warrior,
        Hunter,
        Mage,
        Shadow
    }

    public enum SkillType
    {
        /// <summary>Always on once bought.</summary>
        Passive,
        /// <summary>Has to be put in a slot and pressed.</summary>
        Active
    }

    /// <summary>
    /// What a skill changes. Every entry is a question some system asks by name, so a new skill is
    /// an asset rather than an `if` somewhere in combat code.
    /// </summary>
    public enum SkillEffectType
    {
        /// <summary>Added share of damage on every melee attack.</summary>
        MeleeDamage,
        /// <summary>Added share on heavy attacks only, on top of the melee bonus.</summary>
        HeavyDamage,
        HeavyStagger,
        /// <summary>Added share of incoming poise damage ignored.</summary>
        StaggerResistance,
        /// <summary>Negative reduces the stamina a block costs.</summary>
        GuardCost,
        ParryStagger,
        /// <summary>Seconds cut from the regeneration pause after a block.</summary>
        GuardRecovery,
        /// <summary>Added share against a stunned or guard-broken target.</summary>
        StunnedDamage,
        /// <summary>Added share when the hit lands on a target's back.</summary>
        BackAttackDamage,
        DashStaminaCost,
        DashCooldown,
        CritChance,
        /// <summary>Added share with weapons that scale mainly off dexterity.</summary>
        DexterityWeaponDamage,
        /// <summary>Damage bonus granted for a while after a dodge that really avoided a hit.</summary>
        MomentumDamage,
        /// <summary>Stamina regeneration bonus granted for a while after a kill.</summary>
        AdrenalineRegen,
        /// <summary>Poise resistance granted for a while when health falls low.</summary>
        SecondWindResistance,
        // ---- magic ----
        /// <summary>Added share on every spell, whatever its element.</summary>
        SpellDamage,
        FireDamage,
        FrostDamage,
        /// <summary>Negative reduces what spells cost.</summary>
        SpellManaCost,
        /// <summary>Negative shortens how long spells take to shape.</summary>
        SpellCastTime,
        SpellCritChance,
        /// <summary>Added flat maximum mana.</summary>
        MaxMana,
        /// <summary>Mana returned when a spell lands the killing blow.</summary>
        SpellKillMana,
        /// <summary>Share of incoming damage removed while a shield spell holds.</summary>
        SpellDamageReduction,
        DarkDamage,
        BloodBuildup,
        ShadowCost,
        StatusTargetDamage,
        CuttingShadow,
        /// <summary>
        /// Stamina spent running up a wall. Added last so saved skill data keeps its numbers.
        /// Took over "Ощадливий рух" when the dash became free (item 37, 2026-09-26).
        /// </summary>
        WallRunStaminaCost
    }

    /// <summary>One line of "this skill changes that, by this much per rank".</summary>
    [Serializable]
    public sealed class SkillEffect
    {
        [SerializeField] private SkillEffectType _type = SkillEffectType.MeleeDamage;
        [Tooltip("Added per rank. Shares are fractions: 0.15 reads as 15%.")]
        [SerializeField] private float _valuePerRank = 0f;

        public SkillEffect() { }

        public SkillEffect(SkillEffectType type, float valuePerRank)
        {
            _type = type;
            _valuePerRank = valuePerRank;
        }

        public SkillEffectType Type => _type;
        public float ValuePerRank => _valuePerRank;

        /// <summary>The line the details panel shows, generated from the value it really uses.</summary>
        public string Describe(int rank)
        {
            var total = _valuePerRank * Mathf.Max(1, rank);
            var sign = total >= 0f ? "+" : string.Empty;
            switch (_type)
            {
                case SkillEffectType.MaxMana:
                    return $"Максимум мани {sign}{total:0.#}";
                case SkillEffectType.SpellKillMana:
                    return $"Вбивство закляттям повертає {total:0.#} мани";
                case SkillEffectType.SpellDamageReduction:
                    return $"Щит поглинає {total * 100f:0}% шкоди";
                case SkillEffectType.GuardRecovery:
                    return $"Пауза регенерації після блоку {total:0.##} с";
                case SkillEffectType.MomentumDamage:
                    return $"Після ідеального ухилу: урон {sign}{total * 100f:0}%";
                case SkillEffectType.AdrenalineRegen:
                    return $"Після вбивства: регенерація витривалості {sign}{total * 100f:0}%";
                case SkillEffectType.SecondWindResistance:
                    return $"На малому здоров'ї: опір оглушенню {sign}{total * 100f:0}%";
                default:
                    return $"{EffectName(_type)} {sign}{total * 100f:0}%";
            }
        }

        public static string EffectName(SkillEffectType type)
        {
            switch (type)
            {
                case SkillEffectType.MeleeDamage: return "Урон у ближньому бою";
                case SkillEffectType.HeavyDamage: return "Урон важкої атаки";
                case SkillEffectType.HeavyStagger: return "Оглушення важкою атакою";
                case SkillEffectType.StaggerResistance: return "Опір оглушенню";
                case SkillEffectType.GuardCost: return "Вартість блоку";
                case SkillEffectType.ParryStagger: return "Оглушення від парирування";
                case SkillEffectType.StunnedDamage: return "Урон по оглушених";
                case SkillEffectType.BackAttackDamage: return "Урон зі спини";
                case SkillEffectType.DashStaminaCost: return "Вартість ривка";
                case SkillEffectType.WallRunStaminaCost: return "Вартість бігу по стіні";
                case SkillEffectType.DashCooldown: return "Відкат ривка";
                case SkillEffectType.CritChance: return "Шанс критичного удару";
                case SkillEffectType.DexterityWeaponDamage: return "Урон легкою зброєю";
                case SkillEffectType.SpellDamage: return "Шкода заклять";
                case SkillEffectType.FireDamage: return "Шкода вогнем";
                case SkillEffectType.FrostDamage: return "Шкода льодом";
                case SkillEffectType.SpellManaCost: return "Вартість заклять";
                case SkillEffectType.SpellCastTime: return "Час читання";
                case SkillEffectType.SpellCritChance: return "Крит заклять";
                default: return type.ToString();
            }
        }
    }

    /// <summary>
    /// The swing an active skill performs. The attack itself is an ordinary
    /// <see cref="AttackDefinition"/>, so skills go through the same hitbox, the same damage
    /// pipeline and the same defences as every other attack in the game.
    /// </summary>
    [Serializable]
    public sealed class ActiveSkillData
    {
        [SerializeField] private AttackDefinition _attack = new AttackDefinition();
        [SerializeField, Min(0f)] private float _cooldown = 8f;
        [Tooltip("Refused while a ranged weapon is in hand, once those exist.")]
        [SerializeField] private bool _requiresMeleeWeapon = true;
        [Tooltip("Forward speed carried during the skill; 0 leaves the player planted.")]
        [SerializeField, Min(0f)] private float _lungeSpeed = 0f;
        [Tooltip("Invulnerability from the start of the skill. Short on purpose.")]
        [SerializeField, Min(0f)] private float _invulnerability = 0f;
        [SerializeField] private string[] _tags = Array.Empty<string>();

        [SerializeField] private ShadowAction _shadowAction;
        [SerializeField, Min(0f)] private float _manaCost;
        [Tooltip("How this skill looks. Left on ActiveSkill, it uses the shared skill animation, "
            + "so a new skill needs art only when it deserves its own.")]
        [SerializeField] private PlayerAnimationState _animationState;
        public ShadowAction ShadowAction => _shadowAction;

        /// <summary>The visual state <see cref="PlayerAnimationController"/> plays while this runs.</summary>
        public PlayerAnimationState AnimationState => _animationState;
        public float ManaCost => _manaCost;
        public AttackDefinition Attack => _attack;
        public float Cooldown => _cooldown;
        public bool RequiresMeleeWeapon => _requiresMeleeWeapon;
        public float LungeSpeed => _lungeSpeed;
        public float Invulnerability => _invulnerability;
        public IReadOnlyList<string> Tags => _tags;

        public bool HasTag(string tag)
        {
            return Array.IndexOf(_tags, tag) >= 0;
        }
    }

    /// <summary>
    /// One node of a skill tree. Everything about a skill — what it costs, what it needs, what it
    /// does and how it reads in the panel — lives in this asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Forgotten Kingdom/Skill", fileName = "Skill")]
    public sealed class SkillDefinition : ScriptableObject
    {
        [Tooltip("Stable, lowercase, never renamed: this is what a save file records.")]
        [SerializeField] private string _skillId = string.Empty;
        [SerializeField] private string _displayName = "Skill";
        [SerializeField, TextArea] private string _description = string.Empty;
        [SerializeField] private SkillBranch _branch = SkillBranch.Warrior;
        [SerializeField] private SkillType _type = SkillType.Passive;
        [SerializeField, Min(1)] private int _cost = 1;
        [SerializeField, Min(1)] private int _maxRank = 1;
        [Tooltip("Character level needed before this can be bought at all.")]
        [SerializeField, Min(1)] private int _requiredLevel = 1;
        [Tooltip("Every one of these must be owned first.")]
        [SerializeField] private string[] _prerequisites = Array.Empty<string>();
        [SerializeField] private SkillEffect[] _effects = Array.Empty<SkillEffect>();
        [SerializeField] private ActiveSkillData _active;
        [Tooltip("Spell this skill teaches, if any; the spellbook grants it on unlock.")]
        [SerializeField] private string _grantedSpellId = string.Empty;
        [SerializeField] private Sprite _icon;
        [Tooltip("Where the node sits on the tree; column then row.")]
        [SerializeField] private Vector2Int _position = Vector2Int.zero;

        public string SkillId => _skillId;
        public string DisplayName => _displayName;
        public string Description => _description;
        public SkillBranch Branch => _branch;
        public SkillType Type => _type;
        public int Cost => _cost;
        public int MaxRank => _maxRank;
        public int RequiredLevel => _requiredLevel;
        public IReadOnlyList<string> Prerequisites => _prerequisites;
        public IReadOnlyList<SkillEffect> Effects => _effects;
        public ActiveSkillData Active => _active;
        public string GrantedSpellId => _grantedSpellId;
        public Sprite Icon => _icon;
        public Vector2Int Position => _position;
        public bool IsActive => _type == SkillType.Active;

        public float EffectValue(SkillEffectType type, int rank)
        {
            var total = 0f;
            foreach (var effect in _effects)
            {
                if (effect != null && effect.Type == type)
                {
                    total += effect.ValuePerRank * Mathf.Max(0, rank);
                }
            }

            return total;
        }

        /// <summary>
        /// The effect lines for the details panel, generated from the same values the game uses,
        /// so the description can never promise 15% while combat delivers 20%.
        /// </summary>
        public void DescribeInto(List<string> lines, int rank)
        {
            foreach (var effect in _effects)
            {
                if (effect != null)
                {
                    lines.Add(effect.Describe(rank));
                }
            }

            if (!string.IsNullOrEmpty(_grantedSpellId))
            {
                lines.Add("Відкриває закляття");
            }

            if (_active == null || _type != SkillType.Active)
            {
                return;
            }

            lines.Add($"Витривалість {_active.Attack.StaminaCost:0.#}");
            lines.Add($"Відкат {_active.Cooldown:0.#} с");
            if (_active.RequiresMeleeWeapon)
            {
                lines.Add("Потребує зброї ближнього бою");
            }
        }
    }
}
