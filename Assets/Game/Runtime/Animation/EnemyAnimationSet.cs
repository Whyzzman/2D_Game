using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Every visual state an ordinary enemy can be in. A presentation vocabulary, not a gameplay
    /// one: <see cref="EnemyAnimationController"/> derives these from <see cref="EnemyState"/> and
    /// the attack that is actually running, and nothing here decides what an enemy does.
    ///
    /// Zero is <see cref="AttackLight"/> on purpose. Unity's serializer skips field initializers,
    /// so anything left unset lands on the zero value; an attack with no art chosen should look
    /// like a swing, never like standing still.
    /// </summary>
    public enum EnemyAnimationState
    {
        AttackLight,
        Idle,
        Patrol,
        Alert,
        Chase,
        /// <summary>Raising the shield: a separate, readable transition into guarding.</summary>
        GuardRaise,
        /// <summary>Shield up and holding.</summary>
        GuardIdle,
        /// <summary>The shield ate a blow: recoil, without leaving the guard.</summary>
        GuardImpact,
        /// <summary>Guard shattered: wide open, the punish window.</summary>
        GuardBreak,
        AttackHeavy,
        ShieldBash,
        Hurt,
        Stunned,
        Death
    }

    /// <summary>
    /// Every clip one kind of enemy has, keyed by visual state. AI code never names a clip: it
    /// changes state, and this asset says what that looks like. A new enemy kind is a new asset.
    ///
    /// It holds visuals only. Damage, ranges, cooldowns and timings stay in
    /// <see cref="EnemyDefinition"/> and <see cref="AttackDefinition"/>, which remain the single
    /// authority on what an enemy actually does.
    /// </summary>
    [CreateAssetMenu(menuName = "Forgotten Kingdom/Enemy Animation Set", fileName = "EnemyAnimations")]
    public sealed class EnemyAnimationSet : ScriptableObject
    {
        [Serializable]
        private sealed class StateClip
        {
            public EnemyAnimationState state;
            public AnimationClipDefinition clip = new AnimationClipDefinition();
        }

        [SerializeField] private StateClip[] _clips = Array.Empty<StateClip>();
        [Tooltip("Shown whenever a state has no clip of its own; never left empty.")]
        [SerializeField] private AnimationClipDefinition _fallback = new AnimationClipDefinition();
        [Tooltip("Which states this enemy kind is required to have; guard states only for shields.")]
        [SerializeField] private bool _carriesShield;
        private Dictionary<EnemyAnimationState, AnimationClipDefinition> _byState;

        public AnimationClipDefinition Fallback => _fallback;
        public bool CarriesShield => _carriesShield;

        /// <summary>
        /// The clip for a state. Never returns null while a fallback exists, so a missing clip is a
        /// visual gap and never a crash.
        /// </summary>
        public AnimationClipDefinition Clip(EnemyAnimationState state)
        {
            EnsureIndex();
            return _byState.TryGetValue(state, out var clip) && clip.HasFrames ? clip : _fallback;
        }

        public bool Has(EnemyAnimationState state)
        {
            EnsureIndex();
            return _byState.TryGetValue(state, out var clip) && clip.HasFrames;
        }

        /// <summary>
        /// The states this enemy must have art for. A shield carrier owes four more, and a zombie
        /// is not failed for lacking a guard it never raises.
        /// </summary>
        public IEnumerable<EnemyAnimationState> RequiredStates()
        {
            yield return EnemyAnimationState.Idle;
            yield return EnemyAnimationState.Patrol;
            yield return EnemyAnimationState.Alert;
            yield return EnemyAnimationState.Chase;
            yield return EnemyAnimationState.AttackLight;
            yield return EnemyAnimationState.Hurt;
            yield return EnemyAnimationState.Stunned;
            yield return EnemyAnimationState.Death;
            if (!_carriesShield)
            {
                yield break;
            }

            yield return EnemyAnimationState.GuardRaise;
            yield return EnemyAnimationState.GuardIdle;
            yield return EnemyAnimationState.GuardImpact;
            yield return EnemyAnimationState.GuardBreak;
            yield return EnemyAnimationState.ShieldBash;
            yield return EnemyAnimationState.AttackHeavy;
        }

        private void EnsureIndex()
        {
            if (_byState != null)
            {
                return;
            }

            _byState = new Dictionary<EnemyAnimationState, AnimationClipDefinition>();
            foreach (var entry in _clips)
            {
                if (entry?.clip == null)
                {
                    continue;
                }

                if (!_byState.TryAdd(entry.state, entry.clip))
                {
                    Debug.LogError($"Enemy animation set '{name}' maps {entry.state} twice.", this);
                }
            }
        }

        /// <summary>
        /// Reports gaps at authoring time rather than per frame in play. A missing combat-critical
        /// state is an error, because an unreadable attack is a broken fight, not a rough edge.
        /// </summary>
        public bool Validate()
        {
            EnsureIndex();
            var isValid = true;
            if (!_fallback.HasFrames)
            {
                Debug.LogError($"Enemy animation set '{name}' has no fallback clip; a missing "
                    + "state would leave the enemy invisible.", this);
                isValid = false;
            }

            foreach (var state in RequiredStates())
            {
                if (!Has(state))
                {
                    Debug.LogError($"Enemy animation set '{name}' is missing the required state "
                        + $"{state}.", this);
                    isValid = false;
                }
            }

            return isValid;
        }

        private void OnValidate() => _byState = null;

        private void OnEnable() => _byState = null;
    }
}
