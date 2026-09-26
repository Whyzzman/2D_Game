using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Every visual state the Guardian can be in. Phase one and phase two are separate states
    /// rather than one state with a flag, because the brief asks the player to read the phase
    /// change with no interface at all — which only works if the poses genuinely differ.
    ///
    /// Zero is <see cref="SlashP1"/> for the same reason the other sets put an attack at zero: an
    /// attack with no art chosen must look like a swing, never like standing still.
    /// </summary>
    public enum BossAnimationState
    {
        SlashP1,
        /// <summary>Asleep before the fight: a statue that turns out not to be one.</summary>
        Dormant,
        Intro,
        IdleP1,
        WalkP1,
        ShieldBash,
        ChargeStart,
        Charge,
        ChargeRecover,
        Combo1,
        Combo2,
        Combo3,
        Stunned,
        Recoil,
        PhaseTransition,
        IdleP2,
        MoveP2,
        SlashP2,
        ComboP2,
        JumpStart,
        Jump,
        Slam,
        SlamRecover,
        Death,
        // The Hollow Warden's moves. Appended rather than inserted: these are serialized as
        // numbers, and renumbering would silently repaint the Guardian's whole fight.
        Sweep,
        RootShot,
        RootSlam,
        PoisonFieldCast
    }

    /// <summary>
    /// Every clip the Guardian has. The boss controller never names a clip; it changes state and
    /// picks attacks, and this asset says what those look like.
    ///
    /// Visuals only. Phase threshold, attack damage, ranges, cooldowns and timings stay in
    /// <see cref="BossAttack"/> and <see cref="AttackDefinition"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Forgotten Kingdom/Boss Animation Set", fileName = "BossAnimations")]
    public sealed class BossAnimationSet : ScriptableObject
    {
        [Serializable]
        private sealed class StateClip
        {
            public BossAnimationState state;
            public AnimationClipDefinition clip = new AnimationClipDefinition();
        }

        [SerializeField] private StateClip[] _clips = Array.Empty<StateClip>();
        [Tooltip("Shown whenever a state has no clip of its own; never left empty.")]
        [SerializeField] private AnimationClipDefinition _fallback = new AnimationClipDefinition();
        [Tooltip("States this boss actually enters. Empty means the Guardian's moveset — every "
            + "state up to and including Death — so the original asset keeps its meaning.")]
        [SerializeField] private BossAnimationState[] _requiredStates =
            Array.Empty<BossAnimationState>();
        private Dictionary<BossAnimationState, AnimationClipDefinition> _byState;

        public AnimationClipDefinition Fallback => _fallback;

        /// <summary>
        /// The states that must have real art. The enum is shared by every boss, so it grows with
        /// each new moveset; the Guardian never sweeps a glaive and must not be failed for having
        /// no art for it, while the Warden must be failed for missing its own.
        /// </summary>
        public IEnumerable<BossAnimationState> RequiredStates()
        {
            if (_requiredStates != null && _requiredStates.Length > 0)
            {
                return _requiredStates;
            }

            return GuardianStates();
        }

        private static IEnumerable<BossAnimationState> GuardianStates()
        {
            foreach (BossAnimationState state in Enum.GetValues(typeof(BossAnimationState)))
            {
                if (state <= BossAnimationState.Death)
                {
                    yield return state;
                }
            }
        }

        public AnimationClipDefinition Clip(BossAnimationState state)
        {
            EnsureIndex();
            return _byState.TryGetValue(state, out var clip) && clip.HasFrames ? clip : _fallback;
        }

        public bool Has(BossAnimationState state)
        {
            EnsureIndex();
            return _byState.TryGetValue(state, out var clip) && clip.HasFrames;
        }

        /// <summary>
        /// The phase-one states. Split from phase two so validation can say which half is missing
        /// rather than only that something is.
        /// </summary>
        public static IEnumerable<BossAnimationState> PhaseOneStates()
        {
            yield return BossAnimationState.Dormant;
            yield return BossAnimationState.Intro;
            yield return BossAnimationState.IdleP1;
            yield return BossAnimationState.WalkP1;
            yield return BossAnimationState.SlashP1;
            yield return BossAnimationState.ShieldBash;
            yield return BossAnimationState.ChargeStart;
            yield return BossAnimationState.Charge;
            yield return BossAnimationState.ChargeRecover;
            yield return BossAnimationState.Combo1;
            yield return BossAnimationState.Combo2;
            yield return BossAnimationState.Combo3;
        }

        public static IEnumerable<BossAnimationState> PhaseTwoStates()
        {
            yield return BossAnimationState.PhaseTransition;
            yield return BossAnimationState.IdleP2;
            yield return BossAnimationState.MoveP2;
            yield return BossAnimationState.SlashP2;
            yield return BossAnimationState.ComboP2;
            yield return BossAnimationState.JumpStart;
            yield return BossAnimationState.Jump;
            yield return BossAnimationState.Slam;
            yield return BossAnimationState.SlamRecover;
        }

        private void EnsureIndex()
        {
            if (_byState != null)
            {
                return;
            }

            _byState = new Dictionary<BossAnimationState, AnimationClipDefinition>();
            foreach (var entry in _clips)
            {
                if (entry?.clip == null)
                {
                    continue;
                }

                if (!_byState.TryAdd(entry.state, entry.clip))
                {
                    Debug.LogError($"Boss animation set '{name}' maps {entry.state} twice.", this);
                }
            }
        }

        /// <summary>
        /// A boss whose charge or phase transition is a generic placeholder is a broken fight, so
        /// every state here is required rather than merely warned about.
        /// </summary>
        public bool Validate()
        {
            EnsureIndex();
            var isValid = true;
            if (!_fallback.HasFrames)
            {
                Debug.LogError($"Boss animation set '{name}' has no fallback clip.", this);
                isValid = false;
            }

            foreach (var state in RequiredStates())
            {
                if (!Has(state))
                {
                    Debug.LogError($"Boss animation set '{name}' is missing {state}.", this);
                    isValid = false;
                }
            }

            return isValid;
        }

        private void OnValidate() => _byState = null;

        private void OnEnable() => _byState = null;
    }
}
