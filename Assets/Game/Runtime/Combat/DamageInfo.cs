using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Who an actor fights for. Hitboxes skip their own side, so the player never hits himself and
    /// projectiles reuse the same rule without knowing who fired them.
    /// </summary>
    public enum Faction
    {
        Player,
        Enemy,
        /// <summary>Training dummies and destructibles: anyone may damage them.</summary>
        Neutral
    }

    public enum DamageType
    {
        Physical,
        Fire,
        Frost,
        Poison,
        Bleed,
        Lightning,
        Dark,
        Magic
    }

    /// <summary>
    /// What kind of thing produced a hit. Passives and future synergies read these instead of
    /// asking what class created the damage.
    /// </summary>
    [System.Flags]
    public enum DamageTags
    {
        None = 0,
        Melee = 1 << 0,
        Spell = 1 << 1,
        Projectile = 1 << 2,
        Area = 1 << 3,
        Status = 1 << 4,
        Shadow = 1 << 5
    }

    /// <summary>
    /// Everything a single hit carries. Attacks, traps, projectiles and spells all build one of
    /// these, so receivers never need to know which class produced the hit.
    /// </summary>
    public readonly struct DamageInfo
    {
        public readonly DamageTags Tags;
        public readonly StatusBuildup[] Statuses;
        public bool IsStatusDamage => HasTag(DamageTags.Status);
        public readonly float Amount;
        public readonly DamageType Type;
        public readonly GameObject Attacker;
        public readonly Object Source;
        public readonly Vector2 HitPoint;
        public readonly Vector2 HitDirection;
        public readonly float Knockback;
        public readonly float StaggerDamage;
        /// <summary>Stamina a successful block pays. Falls back to poise damage when left at 0.</summary>
        public readonly float GuardDamage;
        public readonly float HitStop;
        public readonly bool IsCritical;
        public readonly bool IsUnblockable;
        public readonly bool IsParryable;

        public DamageInfo(float amount, Vector2 hitDirection, DamageType type = DamageType.Physical,
            GameObject attacker = null, Object source = null, Vector2 hitPoint = default,
            float knockback = 0f, float staggerDamage = 0f, float hitStop = 0f,
            bool isCritical = false, bool isUnblockable = false, float guardDamage = 0f,
            bool isParryable = true, DamageTags tags = DamageTags.None,
            StatusBuildup[] statuses = null)
        {
            Tags = tags;
            Statuses = statuses;
            Amount = amount;
            Type = type;
            Attacker = attacker;
            Source = source;
            HitPoint = hitPoint;
            HitDirection = hitDirection.sqrMagnitude > 0.0001f ? hitDirection.normalized : Vector2.right;
            Knockback = knockback;
            StaggerDamage = staggerDamage;
            GuardDamage = guardDamage;
            HitStop = hitStop;
            IsCritical = isCritical;
            IsUnblockable = isUnblockable;
            IsParryable = isParryable;
        }

        /// <summary>Stamina this hit costs to block.</summary>
        public float EffectiveGuardDamage => GuardDamage > 0f ? GuardDamage : StaggerDamage;

        public DamageInfo AimedAt(Vector2 hitPoint, Vector2 hitDirection)
        {
            return new DamageInfo(Amount, hitDirection, Type, Attacker, Source, hitPoint,
                Knockback, StaggerDamage, HitStop, IsCritical, IsUnblockable, GuardDamage,
                IsParryable, Tags, Statuses);
        }

        /// <summary>
        /// The same hit, re-labelled. An attack's data says how hard it hits; whether it arrives as
        /// a swing or as an arrow is decided where it is emitted, and passives read the tag.
        /// </summary>
        public DamageInfo WithTags(DamageTags tags)
        {
            return new DamageInfo(Amount, HitDirection, Type, Attacker, Source, HitPoint,
                Knockback, StaggerDamage, HitStop, IsCritical, IsUnblockable, GuardDamage,
                IsParryable, tags, Statuses);
        }

        public DamageInfo WithStatuses(StatusBuildup[] statuses)
        {
            return new DamageInfo(Amount, HitDirection, Type, Attacker, Source, HitPoint,
                Knockback, StaggerDamage, HitStop, IsCritical, IsUnblockable, GuardDamage,
                IsParryable, Tags, statuses);
        }

        public bool HasTag(DamageTags tag) => (Tags & tag) != 0;

        /// <summary>The same hit with different numbers; how the damage pipeline scales a blow.</summary>
        public DamageInfo Scaled(float amount, float staggerDamage, float guardDamage,
            bool isCritical)
        {
            return new DamageInfo(amount, HitDirection, Type, Attacker, Source, HitPoint,
                Knockback, staggerDamage, HitStop, isCritical, IsUnblockable, guardDamage,
                IsParryable, Tags, Statuses);
        }
    }

    public enum DamageOutcome
    {
        /// <summary>The target ignored the hit, for example during invincibility frames.</summary>
        Ignored,
        Hit,
        Blocked,
        Parried
    }

    public readonly struct DamageResult
    {
        public readonly DamageOutcome Outcome;
        public readonly float AppliedDamage;
        public readonly bool IsLethal;

        public DamageResult(DamageOutcome outcome, float appliedDamage = 0f, bool isLethal = false)
        {
            Outcome = outcome;
            AppliedDamage = appliedDamage;
            IsLethal = isLethal;
        }

        public bool HasConnected => Outcome != DamageOutcome.Ignored;
    }

    public interface IDamageable
    {
        DamageResult ApplyDamage(in DamageInfo info);
    }

    /// <summary>Anything that has a left/right orientation used by combat and guard checks.</summary>
    public interface IFacing
    {
        int FacingDirection { get; }
    }

    public enum GuardResponse
    {
        None,
        Blocked,
        Parried
    }

    public readonly struct GuardResult
    {
        public readonly GuardResponse Response;
        /// <summary>Share of the damage that still gets through a block; 0 blocks everything.</summary>
        public readonly float DamageMultiplier;
        /// <summary>Share of poise damage a blocked hit still delivers; 0 absorbs it all.</summary>
        public readonly float StaggerMultiplier;

        public GuardResult(GuardResponse response, float damageMultiplier = 0f,
            float staggerMultiplier = 0f)
        {
            Response = response;
            DamageMultiplier = Mathf.Clamp01(damageMultiplier);
            StaggerMultiplier = Mathf.Clamp01(staggerMultiplier);
        }

        public static readonly GuardResult None = new GuardResult(GuardResponse.None, 1f, 1f);
    }

    /// <summary>
    /// Implemented by the player shield and by enemies that can block, so
    /// <see cref="CombatActor"/> resolves guarding the same way for both.
    /// </summary>
    public interface IDamageGuard
    {
        GuardResult Evaluate(in DamageInfo info);
    }

    /// <summary>
    /// Armour and anything else that softens a hit before it reaches health. Implemented by the
    /// player's derived stats; <see cref="CombatActor"/> asks for it without knowing about
    /// equipment at all.
    /// </summary>
    public interface IDamageMitigation
    {
        /// <summary>Returns the damage that should actually be dealt. Never negative.</summary>
        float Mitigate(in DamageInfo info);
    }

    /// <summary>
    /// Last stop before a hit is delivered, with the target finally known. It exists for bonuses
    /// that depend on who is being hit and from where — a blow to a stunned enemy, a strike in the
    /// back — which cannot be decided when the swing starts. Implemented on the attacker, so the
    /// enemy side of combat never learns that skills exist.
    /// </summary>
    public interface IDamageAugment
    {
        DamageInfo Augment(in DamageInfo info, CombatActor target);
    }
}
