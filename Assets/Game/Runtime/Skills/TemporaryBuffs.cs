using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Short-lived bonuses such as the one a perfect dodge grants. Each source keeps at most one
    /// buff: triggering it again refreshes the timer rather than stacking another copy, so no
    /// amount of dodging can pile a bonus up on itself.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TemporaryBuffs : MonoBehaviour
    {
        private readonly Dictionary<string, Buff> _buffs = new Dictionary<string, Buff>();
        private readonly List<string> _expired = new List<string>();

        /// <summary>Raised when a buff starts, refreshes or ends; the HUD's status-icon hook.</summary>
        public event Action BuffsChanged;

        private struct Buff
        {
            public SkillEffectType Type;
            public float Value;
            public float ExpiresAt;
            public float Duration;
        }

        public int ActiveCount => _buffs.Count;

        /// <summary>Starts or refreshes a buff. Duration counts in unscaled-free scaled time.</summary>
        public void Apply(string sourceId, SkillEffectType type, float value, float duration)
        {
            if (string.IsNullOrEmpty(sourceId) || duration <= 0f || Mathf.Approximately(value, 0f))
            {
                return;
            }

            _buffs[sourceId] = new Buff
            {
                Type = type,
                Value = value,
                ExpiresAt = Time.time + duration,
                Duration = duration
            };
            BuffsChanged?.Invoke();
        }

        public bool IsActive(string sourceId)
        {
            return _buffs.TryGetValue(sourceId, out var buff) && Time.time < buff.ExpiresAt;
        }

        public float RemainingTime(string sourceId)
        {
            return _buffs.TryGetValue(sourceId, out var buff)
                ? Mathf.Max(0f, buff.ExpiresAt - Time.time)
                : 0f;
        }

        /// <summary>The total a particular kind of buff is currently worth.</summary>
        public float Value(SkillEffectType type)
        {
            var total = 0f;
            foreach (var pair in _buffs)
            {
                if (pair.Value.Type == type && Time.time < pair.Value.ExpiresAt)
                {
                    total += pair.Value.Value;
                }
            }

            return total;
        }

        /// <summary>Everything currently running, for the HUD and the debug overlay.</summary>
        public void CollectActive(List<string> into)
        {
            if (into == null)
            {
                return;
            }

            foreach (var pair in _buffs)
            {
                if (Time.time < pair.Value.ExpiresAt)
                {
                    into.Add($"{pair.Key} {RemainingTime(pair.Key):0.0}s");
                }
            }
        }

        /// <summary>Drops every buff. Death and resting both go through here.</summary>
        public void Clear()
        {
            if (_buffs.Count == 0)
            {
                return;
            }

            _buffs.Clear();
            BuffsChanged?.Invoke();
        }

        private void Update()
        {
            if (_buffs.Count == 0)
            {
                return;
            }

            _expired.Clear();
            foreach (var pair in _buffs)
            {
                if (Time.time >= pair.Value.ExpiresAt)
                {
                    _expired.Add(pair.Key);
                }
            }

            if (_expired.Count == 0)
            {
                return;
            }

            foreach (var id in _expired)
            {
                _buffs.Remove(id);
            }

            BuffsChanged?.Invoke();
        }
    }

    /// <summary>Buff source names, kept in one place rather than spelled inline.</summary>
    public static class BuffIds
    {
        public const string Momentum = "momentum";
        public const string Adrenaline = "adrenaline";
        public const string SecondWind = "second-wind";
    }
}
