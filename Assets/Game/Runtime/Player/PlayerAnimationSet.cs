using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Every visual state the player can be in. This is a presentation vocabulary, not a
    /// gameplay one: <see cref="PlayerAnimationController"/> derives these from the real
    /// gameplay state, and nothing here ever decides what the character does.
    /// </summary>
    public enum PlayerAnimationState
    {
        /// <summary>
        /// The shared skill animation, and deliberately the zero value: Unity's serializer skips
        /// field initializers, so anything that forgets to choose a state lands here rather than on
        /// a specific one. A skill that has not been given art should look generic, never idle.
        /// </summary>
        ActiveSkill,
        Idle,
        Run,
        JumpStart,
        JumpRise,
        Fall,
        Land,
        Dash,
        AirDash,
        WallSlide,
        WallJump,
        WallRun,
        Attack1,
        Attack2,
        Attack3,
        Heavy,
        Block,
        BlockImpact,
        Parry,
        GuardBroken,
        Hurt,
        Stunned,
        Death,
        CastStart,
        CastRelease,
        GroundBreaker,
        DashStrike,
        ShadowDashStart,
        ShadowDashLoop,
        ShadowDashEnd,
        ShadowStrike,
        WeaponSwitch,
        /// <summary>
        /// Drinking a flask. Added last so saved states keep their numbers; it has frames of its
        /// own, and a set without them borrows the cast's opening rather than showing nothing.
        /// </summary>
        Heal,
        /// <summary>
        /// The swings up and down (pogo). Added last, like Heal; they have their own frames, and
        /// should a set lack them they borrow the first swing rather than showing nothing.
        /// </summary>
        AttackUp,
        AttackDown,
        /// <summary>
        /// A rare flourish while standing still: the sword is settled and the hero looks back over
        /// the shoulder. Added last to keep the numbers; a set without it simply stays in Idle.
        /// </summary>
        IdleVariant,
        /// <summary>
        /// The two frames after the facing flips on the ground. Visual only — the turn itself is
        /// instant, as in Hollow Knight — and a set without it shows Idle for that instant.
        /// </summary>
        Turn,
        /// <summary>
        /// Touching down after a long drop: down on one knee, then up again. Visual only (the
        /// user's choice, 2026-09-28) — any move cuts it. A set without it shows Land.
        /// </summary>
        HardLand,
        /// <summary>
        /// The second idle flourish, taking turns with <see cref="IdleVariant"/>: a look down at
        /// the blade, then ahead. A set without it shows Idle.
        /// </summary>
        IdleGlance
    }

    /// <summary>
    /// Every clip the player has, keyed by visual state. Gameplay code never names a clip: it
    /// changes state, and this asset says what that looks like. Swapping this asset is all a
    /// future skin or second character would need.
    /// </summary>
    [CreateAssetMenu(menuName = "Forgotten Kingdom/Player Animation Set", fileName = "PlayerAnimations")]
    public sealed class PlayerAnimationSet : ScriptableObject
    {
        [Serializable]
        private sealed class StateClip
        {
            public PlayerAnimationState state;
            public AnimationClipDefinition clip = new AnimationClipDefinition();
        }

        [SerializeField] private StateClip[] _clips = Array.Empty<StateClip>();
        [Tooltip("Shown whenever a state has no clip of its own; never left empty.")]
        [SerializeField] private AnimationClipDefinition _fallback = new AnimationClipDefinition();
        [SerializeField] private WeaponAnimationSet[] _weaponSets = Array.Empty<WeaponAnimationSet>();
        private Dictionary<PlayerAnimationState, AnimationClipDefinition> _byState;

        public AnimationClipDefinition Fallback => _fallback;

        /// <summary>
        /// The clip for a state, preferring the weapon's own when it has one. Never returns null
        /// while a fallback exists, so a missing clip is a visual gap and never a crash.
        /// </summary>
        public AnimationClipDefinition Clip(PlayerAnimationState state, WeaponAnimationSet weapon)
        {
            if (state == PlayerAnimationState.Heal && !Has(state, weapon))
            {
                return Clip(PlayerAnimationState.CastStart, weapon);
            }

            if ((state == PlayerAnimationState.AttackUp || state == PlayerAnimationState.AttackDown)
                && !Has(state, weapon))
            {
                return Clip(PlayerAnimationState.Attack1, weapon);
            }

            if (state == PlayerAnimationState.HardLand && !Has(state, weapon))
            {
                return Clip(PlayerAnimationState.Land, weapon);
            }

            if ((state == PlayerAnimationState.IdleVariant || state == PlayerAnimationState.Turn
                    || state == PlayerAnimationState.IdleGlance)
                && !Has(state, weapon))
            {
                return Clip(PlayerAnimationState.Idle, weapon);
            }

            var fromWeapon = weapon != null ? weapon.Clip(state) : null;
            if (fromWeapon != null)
            {
                return fromWeapon;
            }

            EnsureIndex();
            return _byState.TryGetValue(state, out var clip) && clip.HasFrames ? clip : _fallback;
        }

        /// <summary>True when this state has real frames rather than the fallback.</summary>
        public bool Has(PlayerAnimationState state, WeaponAnimationSet weapon = null)
        {
            if (weapon != null && weapon.Clip(state) != null)
            {
                return true;
            }

            EnsureIndex();
            return _byState.TryGetValue(state, out var clip) && clip.HasFrames;
        }

        public WeaponAnimationSet WeaponSet(WeaponType type)
        {
            foreach (var set in _weaponSets)
            {
                if (set != null && set.WeaponType == type)
                {
                    return set;
                }
            }

            return null;
        }

        private void EnsureIndex()
        {
            if (_byState != null)
            {
                return;
            }

            _byState = new Dictionary<PlayerAnimationState, AnimationClipDefinition>();
            foreach (var entry in _clips)
            {
                if (entry?.clip == null)
                {
                    continue;
                }

                if (!_byState.TryAdd(entry.state, entry.clip))
                {
                    Debug.LogError($"Animation set '{name}' maps {entry.state} twice.", this);
                }
            }
        }

        /// <summary>Reports gaps once, at authoring time, rather than per frame in play.</summary>
        public bool Validate()
        {
            EnsureIndex();
            var isValid = true;
            if (!_fallback.HasFrames)
            {
                Debug.LogError($"Animation set '{name}' has no fallback clip; a missing state "
                    + "would leave the character invisible.", this);
                isValid = false;
            }

            for (var i = 0; i < _weaponSets.Length; i++)
            {
                if (_weaponSets[i] == null)
                {
                    // Usually a weapon set whose script failed to resolve, which otherwise shows up
                    // only as that weapon quietly using the shared swings.
                    Debug.LogError($"Animation set '{name}' has an empty weapon set at {i}.", this);
                    isValid = false;
                }
            }

            foreach (PlayerAnimationState state in Enum.GetValues(typeof(PlayerAnimationState)))
            {
                if (!Has(state))
                {
                    Debug.LogWarning($"Animation set '{name}' has no clip for {state}; "
                        + "the fallback will be shown.", this);
                }
            }

            return isValid;
        }

        private void OnValidate() => _byState = null;

        private void OnEnable() => _byState = null;
    }
}
