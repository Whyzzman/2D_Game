using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>What kind of swing produced a hit; some skills only apply to some of them.</summary>
    public enum SwingKind
    {
        Light,
        Heavy,
        ActiveSkill
    }

    /// <summary>
    /// The one pipeline every player hit passes through on its way out. Weapon and attribute
    /// scaling have already produced the base; this adds what the skill tree is worth, decides the
    /// bonuses that depend on the target, and rolls the critical — in that order, once per target.
    ///
    /// It lives on the attacker, so enemies and the shared damage code never learn that skills
    /// exist: they receive an ordinary <see cref="DamageInfo"/> and resolve it as always.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerOffense : MonoBehaviour, IDamageAugment
    {
        [SerializeField] private PlayerSkills _skills;
        [SerializeField] private PlayerEquipment _equipment;
        [SerializeField] private TemporaryBuffs _buffs;
        [Tooltip("Critical chance before any skill adds to it.")]
        [SerializeField, Range(0f, 1f)] private float _baseCritChance = 0f;
        [SerializeField, Min(1f)] private float _baseCritMultiplier = 1.5f;
        private SwingKind _swing = SwingKind.Light;

        /// <summary>Raised for every critical hit; the hook for a louder number and a sound.</summary>
        public event Action<CombatActor, DamageInfo> CriticalLanded;

        public float CritChance => Mathf.Clamp01(_baseCritChance
            + (_skills != null ? _skills.Effect(SkillEffectType.CritChance) : 0f));

        public float CritMultiplier
        {
            get
            {
                var weapon = _equipment != null ? _equipment.ActiveWeaponDefinition : null;
                return weapon != null && weapon.CritMultiplier > 1f
                    ? weapon.CritMultiplier
                    : _baseCritMultiplier;
            }
        }

        private void Awake()
        {
            if (_skills == null) { _skills = FindAnyObjectByType<PlayerSkills>(); }
            if (_equipment == null) { _equipment = GetComponent<PlayerEquipment>(); }
            if (_buffs == null) { _buffs = GetComponent<TemporaryBuffs>(); }
        }

        /// <summary>Told by combat which kind of swing is starting, before the hitbox opens.</summary>
        public void BeginSwing(SwingKind kind)
        {
            _swing = kind;
        }

        public DamageInfo Augment(in DamageInfo info, CombatActor target)
        {
            if (info.IsStatusDamage) { return info; }
            var damage = info.Amount;
            var stagger = info.StaggerDamage;
            var guard = info.GuardDamage;

            if (_skills != null)
            {
                // Passive bonuses that apply to any melee blow.
                var multiplier = info.HasTag(DamageTags.Shadow)
                    ? _skills.Multiplier(SkillEffectType.DarkDamage)
                    : _skills.Multiplier(SkillEffectType.MeleeDamage);
                if (target.Statuses != null && target.Statuses.HasActiveStatus)
                {
                    multiplier *= _skills.Multiplier(SkillEffectType.StatusTargetDamage);
                }
                if (_swing == SwingKind.Heavy)
                {
                    multiplier *= _skills.Multiplier(SkillEffectType.HeavyDamage);
                    stagger *= _skills.Multiplier(SkillEffectType.HeavyStagger);
                    guard *= _skills.Multiplier(SkillEffectType.HeavyStagger);
                }

                if (IsDexterityWeapon())
                {
                    multiplier *= _skills.Multiplier(SkillEffectType.DexterityWeaponDamage);
                }

                // Bonuses that need the target: only decidable here, with it in front of us.
                if (IsTargetHelpless(target))
                {
                    multiplier *= _skills.Multiplier(SkillEffectType.StunnedDamage);
                }

                if (IsBehind(target))
                {
                    multiplier *= _skills.Multiplier(SkillEffectType.BackAttackDamage);
                }

                damage *= multiplier;
            }

            if (_buffs != null)
            {
                damage *= Mathf.Max(0f, 1f + _buffs.Value(SkillEffectType.MomentumDamage));
            }

            // Rolled once, here, so a swing cannot reroll its critical against the same target.
            var isCritical = info.IsCritical
                || (CritChance > 0f && UnityEngine.Random.value < CritChance);
            if (isCritical)
            {
                damage *= CritMultiplier;
            }

            var result = info.Scaled(damage, stagger, guard, isCritical);
            var bleed = _skills != null ? _skills.Effect(SkillEffectType.BloodBuildup) : 0f;
            if (bleed > 0f && info.HasTag(DamageTags.Melee))
            {
                var entries = new StatusBuildup[(info.Statuses?.Length ?? 0) + 1];
                if (info.Statuses != null) { Array.Copy(info.Statuses, entries, info.Statuses.Length); }
                entries[entries.Length - 1] = new StatusBuildup(StatusType.Bleed,
                    bleed * (_swing == SwingKind.Heavy ? 1.8f : 1f));
                result = result.WithStatuses(entries);
            }
            if (isCritical)
            {
                CriticalLanded?.Invoke(target, result);
            }

            return result;
        }

        /// <summary>
        /// A stunned or guard-broken enemy is the punish window the tree rewards. Read off the
        /// enemy's own state rather than guessed, so it can never disagree with the fight.
        /// </summary>
        private static bool IsTargetHelpless(CombatActor target)
        {
            if (target == null)
            {
                return false;
            }

            if (target.Stagger != null && target.Stagger.IsStunned)
            {
                return true;
            }

            var enemy = target.GetComponent<EnemyController>();
            return enemy != null
                && (enemy.State == EnemyState.Stunned || enemy.State == EnemyState.GuardBroken);
        }

        /// <summary>
        /// A hit lands behind a target when the target is facing away from the attacker. This is
        /// the same reading the shield uses to refuse a block, so a back attack cannot count as
        /// one thing for damage and another for the shield.
        /// </summary>
        private bool IsBehind(CombatActor target)
        {
            if (target == null)
            {
                return false;
            }

            var targetFacing = target.GetComponent<IFacing>();
            if (targetFacing == null)
            {
                return false;
            }

            var toAttacker = transform.position.x - target.transform.position.x;
            return targetFacing.FacingDirection * toAttacker < 0f;
        }

        /// <summary>A weapon is "light" when dexterity, not strength, is what makes it hit.</summary>
        private bool IsDexterityWeapon()
        {
            var weapon = _equipment != null ? _equipment.ActiveWeaponDefinition : null;
            return weapon != null && weapon.DexterityScaling >= weapon.StrengthScaling;
        }
    }
}
