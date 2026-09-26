using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// AI tuning and attack list for one enemy kind. Health and poise stay on the prefab's
    /// <see cref="Health"/> and <see cref="StaggerMeter"/> so there is one source of truth for each.
    /// A new enemy kind is a new asset of this type plus a prefab, not new AI code.
    /// </summary>
    [CreateAssetMenu(menuName = "Forgotten Kingdom/Enemy", fileName = "Enemy")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName = "Enemy";
        [Tooltip("Experience granted once, when this enemy really dies.")]
        [SerializeField, Min(0)] private int _experienceReward = 0;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float _patrolSpeed = 1.6f;
        [SerializeField, Min(0f)] private float _chaseSpeed = 3.4f;
        [SerializeField, Min(0f)] private float _returnSpeed = 2.2f;
        [Tooltip("Half width of the patrol area around the spawn position.")]
        [SerializeField, Min(0f)] private float _patrolRange = 4f;
        [SerializeField, Min(0f)] private float _idleTime = 1.2f;
        [Tooltip("Random spread added to the idle pause, so a group does not march in lockstep.")]
        [SerializeField, Min(0f)] private float _idleTimeVariance = 0.6f;

        [Header("Senses")]
        [SerializeField, Min(0f)] private float _detectionRange = 7f;
        [SerializeField, Min(0f)] private float _loseSightRange = 11f;
        [Tooltip("Vertical reach of the senses; a target far above is not noticed.")]
        [SerializeField, Min(0f)] private float _verticalDetectionRange = 3.5f;
        [SerializeField, Min(0f)] private float _alertTime = 0.35f;
        [SerializeField, Min(0f)] private float _searchTime = 2.5f;
        [Tooltip("The player is noticed from behind only inside this distance.")]
        [SerializeField, Min(0f)] private float _behindDetectionRange = 2f;
        [Tooltip("Horizontal gap below which the enemy stops flipping, so it cannot jitter.")]
        [SerializeField, Min(0f)] private float _facingDeadZone = 0.35f;
        [Tooltip("How long the decision logic may run between full re-evaluations.")]
        [SerializeField, Min(0f)] private float _aiTickInterval = 0.08f;

        [Header("Attack")]
        [SerializeField, Min(0f)] private float _attackRange = 1.5f;
        [Tooltip("Largest height difference that still allows a swing.")]
        [SerializeField, Min(0f)] private float _verticalAttackRange = 1.3f;
        [SerializeField, Min(0f)] private float _attackCooldown = 1.3f;
        [Tooltip("Time from the attack start after which the swing stops tracking the target.")]
        [SerializeField, Min(0f)] private float _facingLockTime = 0.12f;
        [Tooltip("Shortest gap between two AI decisions, so states cannot flicker.")]
        [SerializeField, Min(0f)] private float _decisionCooldown = 0.25f;
        [Tooltip("Share of chase speed kept while advancing behind a raised shield.")]
        [SerializeField, Range(0f, 1f)] private float _guardMovementScale = 0.3f;
        [SerializeField] private EnemyAttackOption[] _attacks = { new EnemyAttackOption() };

        [Header("Spacing")]
        [Tooltip("Distance a ranged enemy tries to keep. 0 makes it close in like a melee enemy.")]
        [SerializeField, Min(0f)] private float _preferredRange = 0f;
        [Tooltip("Closer than this it gives ground instead of standing there being hit.")]
        [SerializeField, Min(0f)] private float _retreatRange = 0f;
        [SerializeField, Min(0f)] private float _retreatSpeed = 3f;

        [Header("Reactions")]
        [SerializeField, Min(0f)] private float _hurtTime = 0.3f;
        [Tooltip("Minimum gap between two flinches, so a flurry cannot stun-lock the enemy.")]
        [SerializeField, Min(0f)] private float _hitReactionCooldown = 0.55f;
        [SerializeField, Min(0f)] private float _deathTime = 1.2f;
        [Tooltip("Chase time without reaching the target before giving up and going home.")]
        [SerializeField, Min(0f)] private float _maxChaseTime = 12f;

        public string DisplayName => _displayName;
        public int ExperienceReward => _experienceReward;
        public float PatrolSpeed => _patrolSpeed;
        public float ChaseSpeed => _chaseSpeed;
        public float ReturnSpeed => _returnSpeed;
        public float PatrolRange => _patrolRange;
        public float IdleTime => _idleTime;
        public float IdleTimeVariance => _idleTimeVariance;
        public float DetectionRange => _detectionRange;
        public float LoseSightRange => _loseSightRange;
        public float VerticalDetectionRange => _verticalDetectionRange;
        public float AlertTime => _alertTime;
        public float SearchTime => _searchTime;
        public float BehindDetectionRange => _behindDetectionRange;
        public float FacingDeadZone => _facingDeadZone;
        public float AiTickInterval => _aiTickInterval;
        public float AttackRange => _attackRange;
        public float VerticalAttackRange => _verticalAttackRange;
        public float AttackCooldown => _attackCooldown;
        public float FacingLockTime => _facingLockTime;
        public float DecisionCooldown => _decisionCooldown;
        public float GuardMovementScale => _guardMovementScale;
        public float PreferredRange => _preferredRange;
        public float RetreatRange => _retreatRange;
        public float RetreatSpeed => _retreatSpeed;

        /// <summary>True for an enemy that fights at range and gives ground rather than closing.</summary>
        public bool KeepsDistance => _preferredRange > 0f;
        public float HurtTime => _hurtTime;
        public float HitReactionCooldown => _hitReactionCooldown;
        public float DeathTime => _deathTime;
        public float MaxChaseTime => _maxChaseTime;
        public int AttackCount => _attacks != null ? _attacks.Length : 0;

        public EnemyAttackOption Attack(int index)
        {
            if (_attacks == null || _attacks.Length == 0)
            {
                return null;
            }

            return _attacks[Mathf.Clamp(index, 0, _attacks.Length - 1)];
        }
    }
}
