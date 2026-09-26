using System;
using UnityEngine;

namespace ForgottenKingdom
{
    public enum StatusType { Bleed, Poison, Burn, Frost }

    [Serializable]
    public struct StatusBuildup
    {
        public StatusType type;
        [Min(0f)] public float amount;
        public StatusBuildup(StatusType type, float amount)
        {
            this.type = type;
            this.amount = amount;
        }
    }

    [Serializable]
    public sealed class StatusRule
    {
        public StatusType type;
        [Min(1f)] public float threshold = 100f;
        [Min(0f)] public float decayDelay = 3f;
        [Min(0f)] public float decayRate = 15f;
        [Min(0f)] public float duration = 10f;
        [Min(0.05f)] public float tickInterval = 1f;
        [Min(0f)] public float damage = 4f;
        [Range(0f, 0.8f)] public float slow = 0.25f;
        [Range(0f, 0.95f)] public float resistance;
        public bool immune;
        [Min(0f)] public float reprocDelay = 2f;
    }

    /// <summary>Independent buildup and timers per effect; no combat state is persisted.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatActor))]
    public sealed class StatusEffects : MonoBehaviour
    {
        [SerializeField] private StatusRule[] _rules =
        {
            new StatusRule { type = StatusType.Bleed, damage = 35f, duration = 0f },
            new StatusRule { type = StatusType.Poison, damage = 4f, duration = 12f },
            new StatusRule { type = StatusType.Burn, damage = 5f, duration = 5f, tickInterval = 0.5f },
            new StatusRule { type = StatusType.Frost, damage = 0f, duration = 4f }
        };
        [SerializeField, Range(0f, 1f)] private float _blockedBuildupMultiplier = 0.25f;
        [SerializeField, Range(0f, 1f)] private float _buildupMultiplier = 1f;
        [SerializeField, Range(0f, 1f)] private float _potencyMultiplier = 1f;
        [SerializeField, Range(0f, 1f)] private float _frostDurationMultiplier = 1f;
        private readonly float[] _buildup = new float[4];
        private readonly float[] _remaining = new float[4];
        private readonly float[] _tick = new float[4];
        private readonly float[] _decay = new float[4];
        private readonly float[] _protected = new float[4];
        private readonly GameObject[] _sources = new GameObject[4];
        private CombatActor _actor;
        private PlayerStats _stats;

        public event Action<StatusType> BuildupChanged;
        public event Action<StatusType> Applied;
        public event Action<StatusType> Ticked;
        public event Action<StatusType> Removed;
        public float Buildup(StatusType type) => _buildup[(int)type];
        public float Remaining(StatusType type) => _remaining[(int)type];
        public bool HasActiveStatus => _remaining[1] > 0f || _remaining[2] > 0f || _remaining[3] > 0f;
        public float MovementMultiplier => _remaining[3] > 0f
            ? 1f - Rule(StatusType.Frost).slow * _potencyMultiplier : 1f;
        public StatusRule Rule(StatusType type)
        {
            foreach (var rule in _rules)
            {
                if (rule.type == type) { return rule; }
            }
            return null;
        }

        private void Awake()
        {
            _actor = GetComponent<CombatActor>();
            _stats = GetComponent<PlayerStats>();
        }

        private void OnEnable() { GetComponent<Health>().Died += Clear; }
        private void OnDisable()
        {
            GetComponent<Health>().Died -= Clear;
            Clear();
        }

        public void Receive(in DamageInfo info, bool blocked)
        {
            if (info.IsStatusDamage || info.Statuses == null) { return; }
            foreach (var entry in info.Statuses)
            {
                Add(entry.type, entry.amount * (blocked ? _blockedBuildupMultiplier : 1f),
                    info.Attacker);
            }
        }

        public void Add(StatusType type, float amount, GameObject attacker = null)
        {
            var rule = Rule(type);
            var i = (int)type;
            if (rule == null || !_actor.IsAlive || rule.immune || _protected[i] > 0f
                || (type == StatusType.Frost && _remaining[i] > 0f)) { return; }
            var resistance = Mathf.Clamp(rule.resistance
                + (_stats != null ? _stats.StatusResistance(type) : 0f), 0f, 0.95f);
            _buildup[i] += Mathf.Max(0f, amount) * (1f - resistance) * _buildupMultiplier;
            _decay[i] = rule.decayDelay;
            if (_buildup[i] >= Mathf.Max(1f, rule.threshold))
            {
                _buildup[i] = 0f;
                _sources[i] = attacker;
                if (type == StatusType.Bleed)
                {
                    Applied?.Invoke(type);
                    DealTick(rule);
                }
                else
                {
                    // Refreshing never advances the next tick or adds a second ticking instance.
                    if (_remaining[i] <= 0f) { _tick[i] = Mathf.Max(0.05f, rule.tickInterval); }
                    _remaining[i] = rule.duration
                        * (type == StatusType.Frost ? _frostDurationMultiplier : 1f);
                    Applied?.Invoke(type);
                }
            }
            BuildupChanged?.Invoke(type);
        }

        private void Update() { Tick(Time.deltaTime); }

        public void Tick(float deltaTime)
        {
            if (_actor == null || !_actor.IsAlive || deltaTime <= 0f) { return; }
            foreach (var rule in _rules)
            {
                var i = (int)rule.type;
                _protected[i] = Mathf.Max(0f, _protected[i] - deltaTime);
                var decayTime = Mathf.Max(0f, deltaTime - _decay[i]);
                _decay[i] = Mathf.Max(0f, _decay[i] - deltaTime);
                if (_buildup[i] > 0f && decayTime > 0f)
                {
                    _buildup[i] = Mathf.Max(0f, _buildup[i] - rule.decayRate * decayTime);
                    BuildupChanged?.Invoke(rule.type);
                }
                if (_remaining[i] <= 0f) { continue; }
                var activeTime = Mathf.Min(deltaTime, _remaining[i]);
                _remaining[i] = Mathf.Max(0f, _remaining[i] - deltaTime);
                _tick[i] -= activeTime;
                if (rule.type != StatusType.Frost)
                {
                    while (_tick[i] <= 0.00001f && _actor.IsAlive)
                    {
                        _tick[i] += Mathf.Max(0.05f, rule.tickInterval);
                        DealTick(rule);
                    }
                }
                if (!_actor.IsAlive) { return; }
                if (_remaining[i] <= 0f)
                {
                    if (rule.type == StatusType.Frost) { _protected[i] = rule.reprocDelay; }
                    Removed?.Invoke(rule.type);
                }
            }
        }

        private void DealTick(StatusRule rule)
        {
            var type = rule.type == StatusType.Burn ? DamageType.Fire
                : rule.type == StatusType.Poison ? DamageType.Poison : DamageType.Bleed;
            _actor.ApplyDamage(new DamageInfo(rule.damage * _potencyMultiplier, Vector2.zero,
                type, _sources[(int)rule.type], this, isParryable: false,
                tags: DamageTags.Status));
            Ticked?.Invoke(rule.type);
        }

        public void Clear()
        {
            for (var i = 0; i < 4; i++)
            {
                var hadEffect = _remaining[i] > 0f;
                _buildup[i] = _remaining[i] = _tick[i] = _decay[i] = _protected[i] = 0f;
                _sources[i] = null;
                BuildupChanged?.Invoke((StatusType)i);
                if (hadEffect) { Removed?.Invoke((StatusType)i); }
            }
        }
    }
}
