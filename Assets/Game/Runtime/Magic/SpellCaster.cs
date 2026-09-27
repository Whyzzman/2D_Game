using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    public enum CastResult
    {
        Casting,
        EmptySlot,
        NotKnown,
        OnCooldown,
        NotEnoughMana,
        /// <summary>Dead, stunned, guard-broken, mid-swing or already casting.</summary>
        BusyOrIncapacitated,
        /// <summary>A spell that needs both feet on the ground, cast in the air.</summary>
        GroundOnly
    }

    /// <summary>
    /// Casting, from the first gesture to the recovery. A spell is shaped over its cast time, is
    /// paid for and released at the end of it, and can be lost to a real hit before that — which
    /// is the risk that pays for reaching across the room.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpellCaster : MonoBehaviour
    {
        [SerializeField] private PlayerSpellbook _spellbook;
        [SerializeField] private PlayerStats _stats;
        [SerializeField] private PlayerSkills _skills;
        [SerializeField] private PlayerOffense _offense;
        [SerializeField] private TemporaryBuffs _buffs;
        [SerializeField] private Mana _mana;
        [SerializeField] private CombatActor _actor;
        [SerializeField] private PlayerMotor2D _motor;
        [SerializeField] private Transform _castPoint;
        [SerializeField] private SpellProjectile _projectilePrefab;
        [SerializeField] private LayerMask _areaTargetMask;
        [Tooltip("Shortest gap between two mana refunds from spell kills.")]
        [SerializeField, Min(0f)] private float _killManaCooldown = 1.5f;
        private readonly float[] _readyAt = new float[PlayerSpellbook.SlotCount];
        private readonly Collider2D[] _overlaps = new Collider2D[24];
        private readonly HashSet<CombatActor> _areaHits = new HashSet<CombatActor>();
        private IFacing _facing;
        private SpellDefinition _casting;
        private int _castingSlot = -1;
        private float _releaseAt;
        private float _castTime;
        private float _endsAt;
        private float _killManaReadyAt;
        private bool _hasReleased;

        public event Action<SpellDefinition> CastStarted;
        public event Action<SpellDefinition> SpellReleased;
        public event Action<SpellDefinition> CastInterrupted;

        public bool IsCasting => _casting != null;
        public SpellDefinition CastingSpell => _casting;

        /// <summary>
        /// How far the shaping is towards release, 0 to 1; 1 once released. What the charge in the
        /// hand grows by, so it can never promise a release that is not coming.
        /// </summary>
        public float CastProgress => _casting == null ? 0f
            : _hasReleased || _castTime <= 0f ? 1f
            : Mathf.Clamp01(1f - (_releaseAt - Time.time) / _castTime);

        /// <summary>Share of normal speed allowed while shaping the current spell.</summary>
        public float MovementScale => _casting != null ? _casting.MovementScale : 1f;

        public float CooldownRemaining(int slot)
        {
            return slot >= 0 && slot < PlayerSpellbook.SlotCount
                ? Mathf.Max(0f, _readyAt[slot] - Time.time)
                : 0f;
        }

        public float CooldownNormalized(int slot)
        {
            var spell = _spellbook != null ? _spellbook.SlotSpell(slot) : null;
            var length = spell != null ? spell.Cooldown : 0f;
            return length > 0f ? Mathf.Clamp01(CooldownRemaining(slot) / length) : 0f;
        }

        public bool IsReady(int slot) => CooldownRemaining(slot) <= 0f;

        private void OnEnable()
        {
            SpellProjectile.Hit += OnProjectileHit;
        }

        private void OnDisable()
        {
            SpellProjectile.Hit -= OnProjectileHit;
            ResetCasting();
        }

        private void OnProjectileHit(SpellProjectile projectile, CombatActor target,
            DamageResult result)
        {
            if (projectile != null && projectile.Faction == Faction.Player && result.IsLethal)
            {
                GrantKillMana();
            }
        }

        /// <summary>
        /// Mana back for a kill made with magic. Rate-limited, and worth less than a spell costs,
        /// so clearing weak enemies cannot become an endless source of casting.
        /// </summary>
        private void GrantKillMana()
        {
            var amount = _skills != null ? _skills.Effect(SkillEffectType.SpellKillMana) : 0f;
            if (amount <= 0f || _mana == null || Time.time < _killManaReadyAt)
            {
                return;
            }

            _killManaReadyAt = Time.time + _killManaCooldown;
            _mana.Restore(amount);
        }

        private void Awake()
        {
            if (_spellbook == null) { _spellbook = FindAnyObjectByType<PlayerSpellbook>(); }
            if (_skills == null) { _skills = FindAnyObjectByType<PlayerSkills>(); }
            if (_stats == null) { _stats = GetComponent<PlayerStats>(); }
            if (_offense == null) { _offense = GetComponent<PlayerOffense>(); }
            if (_buffs == null) { _buffs = GetComponent<TemporaryBuffs>(); }
            if (_mana == null) { _mana = GetComponent<Mana>(); }
            if (_actor == null) { _actor = GetComponent<CombatActor>(); }
            if (_motor == null) { _motor = GetComponent<PlayerMotor2D>(); }
            if (_castPoint == null) { _castPoint = transform; }
            _facing = GetComponent<IFacing>();
        }

        // ---------- the numbers a spell costs and deals right now ----------

        /// <summary>Mana this spell costs with the current skills; never free.</summary>
        public float ManaCostOf(SpellDefinition spell)
        {
            if (spell == null)
            {
                return 0f;
            }

            var multiplier = _skills != null
                ? _skills.Multiplier(SkillEffectType.SpellManaCost)
                : 1f;
            return Mathf.Max(spell.ManaCost * 0.25f, spell.ManaCost * multiplier);
        }

        /// <summary>Cast time with the current skills; never instant.</summary>
        public float CastTimeOf(SpellDefinition spell)
        {
            if (spell == null)
            {
                return 0f;
            }

            var multiplier = _skills != null
                ? _skills.Multiplier(SkillEffectType.SpellCastTime)
                : 1f;
            return Mathf.Max(spell.CastTime * 0.4f, spell.CastTime * multiplier);
        }

        /// <summary>Everything the tree adds to this spell's damage, as one multiplier.</summary>
        public float SkillMultiplierFor(SpellDefinition spell)
        {
            if (_skills == null || spell == null)
            {
                return 1f;
            }

            var multiplier = _skills.Multiplier(SkillEffectType.SpellDamage);
            switch (spell.Element)
            {
                case DamageType.Fire:
                    multiplier *= _skills.Multiplier(SkillEffectType.FireDamage);
                    break;
                case DamageType.Frost:
                    multiplier *= _skills.Multiplier(SkillEffectType.FrostDamage);
                    break;
            }

            return multiplier;
        }

        public SpellDamageBreakdown Evaluate(SpellDefinition spell)
        {
            return MagicDamageCalculator.Evaluate(spell,
                _stats != null ? _stats.Curve : null,
                _stats != null ? _stats.GetFinalStat(CharacterStat.Intelligence) : 0,
                SkillMultiplierFor(spell));
        }

        // ---------- casting ----------

        public CastResult CanCast(int slot)
        {
            var spell = _spellbook != null ? _spellbook.SlotSpell(slot) : null;
            if (spell == null)
            {
                return string.IsNullOrEmpty(_spellbook?.SlotSpellId(slot))
                    ? CastResult.EmptySlot
                    : CastResult.NotKnown;
            }

            if (!_spellbook.Knows(spell.SpellId))
            {
                return CastResult.NotKnown;
            }

            if (IsCasting)
            {
                return CastResult.BusyOrIncapacitated;
            }

            if (!IsReady(slot))
            {
                return CastResult.OnCooldown;
            }

            if (!spell.CanCastInAir && _motor != null && !_motor.IsGrounded)
            {
                return CastResult.GroundOnly;
            }

            if (_mana != null && !_mana.Has(ManaCostOf(spell)))
            {
                return CastResult.NotEnoughMana;
            }

            return CastResult.Casting;
        }

        /// <summary>
        /// Begins shaping a spell. Nothing is spent yet: mana is paid at release, so a cast that
        /// is interrupted costs the player only the time.
        /// </summary>
        public CastResult BeginCast(int slot)
        {
            var verdict = CanCast(slot);
            if (verdict != CastResult.Casting)
            {
                if (verdict == CastResult.NotEnoughMana)
                {
                    _mana?.TrySpend(ManaCostOf(_spellbook.SlotSpell(slot)));
                }

                return verdict;
            }

            var spell = _spellbook.SlotSpell(slot);
            _casting = spell;
            _castingSlot = slot;
            _hasReleased = false;
            var castTime = CastTimeOf(spell);
            _castTime = castTime;
            _releaseAt = Time.time + castTime;
            _endsAt = _releaseAt + spell.RecoveryTime;
            _mana?.SetCasting(true);
            CastStarted?.Invoke(spell);
            return CastResult.Casting;
        }

        public void Tick(float deltaTime)
        {
            if (_casting == null)
            {
                return;
            }

            if (!_hasReleased && Time.time >= _releaseAt)
            {
                Release();
            }

            if (_hasReleased && Time.time >= _endsAt)
            {
                Finish();
            }
        }

        /// <summary>
        /// Pays for the spell and makes it happen. If the mana went somewhere else during the
        /// cast, nothing is released and nothing goes on cooldown.
        /// </summary>
        private void Release()
        {
            _hasReleased = true;
            var spell = _casting;
            if (_mana != null && !_mana.TrySpend(ManaCostOf(spell)))
            {
                Interrupt();
                return;
            }

            _readyAt[_castingSlot] = Time.time + spell.Cooldown;
            switch (spell.Type)
            {
                case SpellType.Area:
                    CastArea(spell);
                    break;
                case SpellType.SelfBuff:
                    CastSelfBuff(spell);
                    break;
                default:
                    CastProjectile(spell);
                    break;
            }

            SpellReleased?.Invoke(spell);
        }

        private void CastProjectile(SpellDefinition spell)
        {
            if (_projectilePrefab == null)
            {
                Debug.LogError("The spell caster has no projectile prefab.", this);
                return;
            }

            var facing = _facing?.FacingDirection ?? 1;
            var direction = new Vector2(facing, 0f);
            var origin = (Vector2)_castPoint.position + new Vector2(0.5f * facing, 0f);
            var projectile = Instantiate(_projectilePrefab, origin, Quaternion.identity);
            projectile.Launch(BuildDamage(spell, direction), Faction.Player, direction,
                spell.ProjectileSpeed, spell.Range, spell.PierceCount, spell.Color);
        }

        /// <summary>
        /// A burst around the caster. Every enemy inside is struck once — the set makes a second
        /// hit in the same cast impossible however many colliders one creature has.
        /// </summary>
        private void CastArea(SpellDefinition spell)
        {
            _areaHits.Clear();
            var origin = (Vector2)_castPoint.position;
            var filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(_areaTargetMask);
            var count = Physics2D.OverlapCircle(origin, spell.AreaRadius, filter, _overlaps);
            for (var i = 0; i < count; i++)
            {
                var hurtbox = _overlaps[i].GetComponentInParent<Hurtbox>();
                if (hurtbox?.Owner == null || hurtbox.Owner == _actor
                    || hurtbox.Owner.Faction == Faction.Player || !_areaHits.Add(hurtbox.Owner))
                {
                    continue;
                }

                var toTarget = (Vector2)hurtbox.Owner.transform.position - origin;
                var direction = new Vector2(toTarget.x >= 0f ? 1f : -1f, 0f);
                var result = hurtbox.Owner.ApplyDamage(
                    BuildDamage(spell, direction).AimedAt(
                        _overlaps[i].ClosestPoint(origin), direction));
                if (result.HasConnected)
                {
                    HitStop.Request(spell.HitStop);
                }

                if (result.IsLethal)
                {
                    GrantKillMana();
                }
            }
        }

        private void CastSelfBuff(SpellDefinition spell)
        {
            _buffs?.Apply(spell.SpellId, SkillEffectType.SpellDamageReduction,
                spell.DamageReduction, spell.BuffDuration);
        }

        /// <summary>
        /// Builds the hit, rolling the critical here so one cast cannot reroll per target and a
        /// burst treats everyone it catches the same.
        /// </summary>
        private DamageInfo BuildDamage(SpellDefinition spell, Vector2 direction)
        {
            var breakdown = Evaluate(spell);
            var isCritical = spell.CanCrit && _offense != null
                && UnityEngine.Random.value < SpellCritChance;
            var damage = breakdown.Total;
            if (isCritical && _offense != null)
            {
                damage *= _offense.CritMultiplier;
            }

            return MagicDamageCalculator.BuildDamage(spell, damage, gameObject, this, direction,
                isCritical);
        }

        public float SpellCritChance => Mathf.Clamp01(
            _skills != null ? _skills.Effect(SkillEffectType.SpellCritChance) : 0f);

        /// <summary>A real hit during the shaping loses the spell — and costs nothing.</summary>
        public bool Interrupt()
        {
            if (_casting == null)
            {
                return false;
            }

            var spell = _casting;
            var wasReleased = _hasReleased;
            Finish();
            if (!wasReleased)
            {
                CastInterrupted?.Invoke(spell);
            }

            return !wasReleased;
        }

        private void Finish()
        {
            _casting = null;
            _castingSlot = -1;
            _hasReleased = false;
            _mana?.SetCasting(false);
        }

        /// <summary>Death and resting clear cooldowns and anything still in the air.</summary>
        public void ResetCasting()
        {
            Finish();
            for (var i = 0; i < _readyAt.Length; i++)
            {
                _readyAt[i] = 0f;
            }

            SpellProjectile.ClearAll(Faction.Player);
        }
    }
}
