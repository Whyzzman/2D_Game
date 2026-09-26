using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace ForgottenKingdom
{
    /// <summary>
    /// Poise. The meter starts at 0 and fills toward <see cref="Threshold"/>; crossing it stuns the
    /// owner and empties the meter. After a quiet spell the meter drains back toward 0. The same
    /// component is used by the player, ordinary enemies and the boss, which differ only in their
    /// threshold and resistance.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StaggerMeter : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float _threshold = 40f;
        [Tooltip("Share of incoming poise damage ignored; 0.3 means heavy armour takes 70%.")]
        [SerializeField, Range(0f, 0.95f)] private float _resistance = 0f;
        [FormerlySerializedAs("_decayPerSecond")]
        [SerializeField, Min(0f)] private float _recoveryPerSecond = 20f;
        [FormerlySerializedAs("_decayDelay")]
        [SerializeField, Min(0f)] private float _recoveryDelay = 2f;
        [SerializeField, Min(0f)] private float _stunDuration = 1.1f;
        private float _current;
        private float _recoveryBlockedUntil;
        private float _stunnedUntil;
        private float _immuneUntil;

        /// <summary>Raised with the stun duration when the threshold is crossed or a break is forced.</summary>
        public event Action<float> Staggered;

        /// <summary>Raised with current and threshold whenever the meter moves; for UI and debug.</summary>
        public event Action<float, float> StaggerChanged;

        public event Action StunEnded;

        public float Threshold => _threshold;
        public float Current => _current;
        public float Normalized => _threshold > 0f ? Mathf.Clamp01(_current / _threshold) : 0f;
        public float Resistance => _resistance;
        public float StunDuration => _stunDuration;
        public bool IsStunned => Time.time < _stunnedUntil;

        /// <summary>
        /// True while poise damage is being ignored. Used right after a stun so a heavy hitter
        /// cannot chain one break straight into the next.
        /// </summary>
        public bool IsImmune => Time.time < _immuneUntil;

        /// <summary>
        /// Sets the resistance from outside; the player's derived stats do this when armour
        /// changes. The authored value is the base and is simply replaced, never added to.
        /// </summary>
        public void SetResistance(float resistance)
        {
            _resistance = Mathf.Clamp(resistance, 0f, 0.95f);
        }

        /// <summary>Ignores poise damage for a while; the anti stun-lock window.</summary>
        public void SuppressFor(float duration)
        {
            _immuneUntil = Mathf.Max(_immuneUntil, Time.time + Mathf.Max(0f, duration));
        }

        private void Update()
        {
            if (_stunnedUntil > 0f && Time.time >= _stunnedUntil)
            {
                _stunnedUntil = 0f;
                StunEnded?.Invoke();
            }

            if (_current <= 0f || Time.time < _recoveryBlockedUntil)
            {
                return;
            }

            _current = Mathf.Max(0f, _current - _recoveryPerSecond * Time.deltaTime);
            StaggerChanged?.Invoke(_current, _threshold);
        }

        /// <summary>Adds poise damage after resistance; returns true when it caused a stun.</summary>
        public bool Add(float staggerDamage)
        {
            if (staggerDamage <= 0f || IsStunned || IsImmune)
            {
                return false;
            }

            _current += staggerDamage * (1f - _resistance);
            _recoveryBlockedUntil = Time.time + _recoveryDelay;
            StaggerChanged?.Invoke(_current, _threshold);
            if (_current < _threshold)
            {
                return false;
            }

            Break(_stunDuration);
            return true;
        }

        /// <summary>Guard break and scripted breaks skip the meter and stun directly.</summary>
        public void ForceStun(float duration)
        {
            Break(duration);
        }

        public void ResetStagger()
        {
            _current = 0f;
            _stunnedUntil = 0f;
            _immuneUntil = 0f;
            _recoveryBlockedUntil = 0f;
            StaggerChanged?.Invoke(_current, _threshold);
        }

        private void Break(float duration)
        {
            _current = 0f;
            _stunnedUntil = Time.time + Mathf.Max(0f, duration);
            StaggerChanged?.Invoke(_current, _threshold);
            Staggered?.Invoke(duration);
        }
    }
}
