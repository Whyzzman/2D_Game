using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The clips one weapon brings with it. Swinging a greatsword is not a slowed-down sword
    /// swing, so each weapon type supplies its own attacks; anything left empty falls back to the
    /// player's shared set rather than showing nothing.
    ///
    /// This asset holds visuals only. Damage, stamina and timing stay in
    /// <see cref="WeaponDefinition"/> and <see cref="AttackDefinition"/>, which remain the single
    /// authority on what an attack actually does.
    ///
    /// It lives in a file of its own because Unity resolves a saved ScriptableObject's script by
    /// filename: sharing a file with another type leaves the asset with no script at all, and it
    /// then loads silently as null rather than failing loudly.
    /// </summary>
    [CreateAssetMenu(menuName = "Forgotten Kingdom/Weapon Animation Set",
        fileName = "WeaponAnimations")]
    public sealed class WeaponAnimationSet : ScriptableObject
    {
        [Tooltip("The weapon type these clips belong to; used to pick a set at runtime.")]
        [SerializeField] private WeaponType _weaponType = WeaponType.Sword;
        [SerializeField] private AnimationClipDefinition _idle = new AnimationClipDefinition();
        [SerializeField] private AnimationClipDefinition _attack1 = new AnimationClipDefinition();
        [SerializeField] private AnimationClipDefinition _attack2 = new AnimationClipDefinition();
        [SerializeField] private AnimationClipDefinition _attack3 = new AnimationClipDefinition();
        [SerializeField] private AnimationClipDefinition _heavy = new AnimationClipDefinition();
        [SerializeField] private AnimationClipDefinition _attackUp = new AnimationClipDefinition();
        [SerializeField] private AnimationClipDefinition _attackDown = new AnimationClipDefinition();
        [SerializeField] private AnimationClipDefinition _block = new AnimationClipDefinition();
        [SerializeField] private AnimationClipDefinition _parry = new AnimationClipDefinition();

        public WeaponType WeaponType => _weaponType;

        /// <summary>The clip for a state, or null when this weapon does not override it.</summary>
        public AnimationClipDefinition Clip(PlayerAnimationState state)
        {
            switch (state)
            {
                case PlayerAnimationState.Idle: return Valid(_idle);
                case PlayerAnimationState.Attack1: return Valid(_attack1);
                case PlayerAnimationState.Attack2: return Valid(_attack2);
                case PlayerAnimationState.Attack3: return Valid(_attack3);
                case PlayerAnimationState.Heavy: return Valid(_heavy);
                case PlayerAnimationState.AttackUp: return Valid(_attackUp);
                case PlayerAnimationState.AttackDown: return Valid(_attackDown);
                case PlayerAnimationState.Block: return Valid(_block);
                case PlayerAnimationState.Parry: return Valid(_parry);
                default: return null;
            }
        }

        private static AnimationClipDefinition Valid(AnimationClipDefinition clip)
        {
            return clip != null && clip.HasFrames ? clip : null;
        }

        /// <summary>True when the weapon carries a full attack set of its own.</summary>
        public bool HasCompleteAttackSet =>
            Valid(_attack1) != null && Valid(_attack2) != null
            && Valid(_attack3) != null && Valid(_heavy) != null;
    }
}
