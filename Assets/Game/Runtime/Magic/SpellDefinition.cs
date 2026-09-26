using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// How a spell reaches its target. Beam, Summon and Mobility are declared so the data and the
    /// screen already handle them; nothing authors them yet.
    /// </summary>
    public enum SpellType
    {
        Projectile,
        /// <summary>A burst centred on the caster.</summary>
        Area,
        /// <summary>Changes the caster rather than anyone else.</summary>
        SelfBuff,
        Beam,
        Summon,
        Mobility
    }

    /// <summary>
    /// One authored spell. Its damage, cost, timings and shape all live here, so a new spell is
    /// an asset — the casting code never learns any spell's name.
    /// </summary>
    [CreateAssetMenu(menuName = "Forgotten Kingdom/Spell", fileName = "Spell")]
    public sealed class SpellDefinition : ScriptableObject
    {
        [Tooltip("Stable, lowercase, never renamed: this is what a save file records.")]
        [SerializeField] private string _spellId = string.Empty;
        [SerializeField] private string _displayName = "Spell";
        [SerializeField, TextArea] private string _description = string.Empty;
        [SerializeField] private SpellType _type = SpellType.Projectile;
        [SerializeField] private DamageType _element = DamageType.Fire;

        [Header("Cost and timing")]
        [SerializeField, Min(0f)] private float _manaCost = 15f;
        [SerializeField, Min(0f)] private float _castTime = 0.3f;
        [SerializeField, Min(0f)] private float _recoveryTime = 0.2f;
        [SerializeField, Min(0f)] private float _cooldown = 0.4f;
        [Tooltip("Share of normal speed the caster keeps while shaping the spell.")]
        [SerializeField, Range(0f, 1f)] private float _movementScale = 0.3f;
        [Tooltip("Off for spells that need both feet on the ground.")]
        [SerializeField] private bool _canCastInAir = true;

        [Header("Damage")]
        [SerializeField, Min(0f)] private float _baseDamage = 30f;
        [Tooltip("Share of the base that one point of intelligence adds, before the global factor.")]
        [SerializeField, Min(0f)] private float _intelligenceScaling = 0.8f;
        [SerializeField, Min(0f)] private float _staggerDamage = 10f;
        [SerializeField, Min(0f)] private float _guardDamage = 0f;
        [SerializeField, Min(0f)] private float _knockback = 3f;
        [SerializeField, Min(0f)] private float _hitStop = 0.05f;
        [SerializeField] private bool _canCrit = true;
        [Tooltip("Off for spells no shield can stop.")]
        [SerializeField] private bool _isBlockable = true;

        [Header("Shape")]
        [SerializeField, Min(0f)] private float _projectileSpeed = 12f;
        [Tooltip("How far a projectile travels before it fades, or how wide a burst reaches.")]
        [SerializeField, Min(0.5f)] private float _range = 12f;
        [SerializeField, Min(0f)] private float _areaRadius = 3f;
        [SerializeField, Min(1)] private int _pierceCount = 1;

        [Header("Self buff")]
        [Tooltip("Share of incoming damage removed while the buff lasts.")]
        [SerializeField, Range(0f, 0.8f)] private float _damageReduction = 0f;
        [SerializeField, Min(0f)] private float _buffDuration = 0f;

        [Header("Presentation")]
        [SerializeField] private Sprite _icon;
        [SerializeField] private Color _color = new Color(1f, 0.55f, 0.2f);

        public string SpellId => _spellId;
        public string DisplayName => _displayName;
        public string Description => _description;
        public SpellType Type => _type;
        public DamageType Element => _element;
        public float ManaCost => _manaCost;
        public float CastTime => _castTime;
        public float RecoveryTime => _recoveryTime;
        public float Cooldown => _cooldown;
        public float MovementScale => _movementScale;
        public bool CanCastInAir => _canCastInAir;
        [SerializeField] private StatusBuildup[] _statusBuildup = System.Array.Empty<StatusBuildup>();
        public StatusBuildup[] StatusBuildup => _statusBuildup;
        public float BaseDamage => _baseDamage;
        public float IntelligenceScaling => _intelligenceScaling;
        public float StaggerDamage => _staggerDamage;
        public float GuardDamage => _guardDamage;
        public float Knockback => _knockback;
        public float HitStop => _hitStop;
        public bool CanCrit => _canCrit;
        public bool IsBlockable => _isBlockable;
        public float ProjectileSpeed => _projectileSpeed;
        public float Range => _range;
        public float AreaRadius => _areaRadius;
        public int PierceCount => _pierceCount;
        public float DamageReduction => _damageReduction;
        public float BuffDuration => _buffDuration;
        public Sprite Icon => _icon;
        public Color Color => _color;

        public DamageTags Tags
        {
            get
            {
                var tags = DamageTags.Spell;
                if (_type == SpellType.Projectile) { tags |= DamageTags.Projectile; }
                if (_type == SpellType.Area) { tags |= DamageTags.Area; }
                return tags;
            }
        }

        /// <summary>Lines for the spellbook, generated from the values the game really uses.</summary>
        public void DescribeInto(List<string> lines, float currentDamage, float currentCost,
            float currentCastTime)
        {
            lines.Add($"Стихія: {ResistanceProfile.Display(_element)}");
            if (_type == SpellType.SelfBuff)
            {
                lines.Add($"Знижує вхідну шкоду на {_damageReduction * 100f:0}% "
                    + $"протягом {_buffDuration:0.#} с");
            }
            else
            {
                lines.Add($"Шкода {_baseDamage:0.#} (зараз {currentDamage:0.#})");
                lines.Add($"Оглушення {_staggerDamage:0.#}");
            }

            lines.Add($"Мана {_manaCost:0.#}"
                + (Mathf.Abs(currentCost - _manaCost) > 0.05f ? $" (зараз {currentCost:0.#})" : string.Empty));
            lines.Add($"Читання {_castTime:0.##} с"
                + (Mathf.Abs(currentCastTime - _castTime) > 0.005f
                    ? $" (зараз {currentCastTime:0.##} с)"
                    : string.Empty));
            lines.Add($"Відкат {_cooldown:0.##} с");
        }

        /// <summary>Reports authoring mistakes that would show up as a spell doing nothing.</summary>
        public bool Validate()
        {
            var isValid = true;
            if (string.IsNullOrWhiteSpace(_spellId))
            {
                Debug.LogError($"Spell '{name}' has no id; a save could not record it.", this);
                isValid = false;
            }

            if (_type == SpellType.Projectile && _projectileSpeed <= 0f)
            {
                Debug.LogError($"Spell '{_spellId}' is a projectile that would never move.", this);
                isValid = false;
            }

            if (_type == SpellType.SelfBuff && _buffDuration <= 0f)
            {
                Debug.LogError($"Spell '{_spellId}' is a buff that would end instantly.", this);
                isValid = false;
            }

            if (_type != SpellType.SelfBuff && _baseDamage <= 0f)
            {
                Debug.LogWarning($"Spell '{_spellId}' deals no damage.", this);
            }

            return isValid;
        }

        private void OnValidate()
        {
            if (!string.IsNullOrWhiteSpace(_spellId)
                && _spellId != _spellId.Trim().ToLowerInvariant())
            {
                Debug.LogWarning($"Spell id '{_spellId}' should be lowercase without spaces.", this);
            }
        }
    }
}
