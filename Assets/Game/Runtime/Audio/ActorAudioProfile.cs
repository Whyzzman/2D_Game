using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// What one creature sounds like.
    ///
    /// Before the forest, <see cref="CombatAudioBinder"/> asked a single question — does it carry a
    /// shield? — and picked between two sets of sounds. That worked for two enemies and falls apart
    /// at six: a hound is not a quiet zombie, and a shaman is not a loud one.
    ///
    /// The sounds live here, on the prefab, rather than in <see cref="EnemyDefinition"/> or
    /// <see cref="BossController"/>. That keeps the rule the audio pass was built on: gameplay code
    /// names a moment and never a sound, and deleting every one of these still leaves a game that
    /// plays, silently. The binder reads this when it finds one and falls back to the old pairing
    /// when it does not.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ActorAudioProfile : MonoBehaviour
    {
        [Header("Reactions")]
        [SerializeField] private AudioId _alert = AudioId.None;
        [SerializeField] private AudioId _hurt = AudioId.None;
        [SerializeField] private AudioId _death = AudioId.None;
        [SerializeField] private AudioId _step = AudioId.None;

        [Header("Attacks")]
        [Tooltip("The ordinary swing, and the fallback for anything not named below.")]
        [SerializeField] private AudioId _lightAttack = AudioId.None;
        [SerializeField] private AudioId _heavyAttack = AudioId.None;
        [Tooltip("The third move: a shield bash, a kick, a ground spell.")]
        [SerializeField] private AudioId _specialAttack = AudioId.None;

        public AudioId Alert => _alert;
        public AudioId Hurt => _hurt;
        public AudioId Death => _death;
        public AudioId Step => _step;
        public AudioId LightAttack => _lightAttack;
        public AudioId HeavyAttack => _heavyAttack;
        public AudioId SpecialAttack => _specialAttack;

        /// <summary>The sound for one kind of swing, falling back to the light attack.</summary>
        public AudioId AttackFor(EnemyAttackKind kind)
        {
            switch (kind)
            {
                case EnemyAttackKind.Heavy:
                    return _heavyAttack != AudioId.None ? _heavyAttack : _lightAttack;
                case EnemyAttackKind.ShieldBash:
                    return _specialAttack != AudioId.None ? _specialAttack : _lightAttack;
                default:
                    return _lightAttack;
            }
        }
    }
}
