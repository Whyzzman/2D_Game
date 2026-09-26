using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Executes an enemy's swings: telegraph, active hitbox window and recovery. It never decides
    /// whether to attack — that is the state machine's job — so every enemy kind can reuse it with
    /// its own attack list.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyCombat : MonoBehaviour
    {
        [SerializeField] private Hitbox _hitbox;
        [SerializeField] private SpriteRenderer _telegraph;
        [SerializeField] private Color _telegraphColor = new Color(1f, 0.82f, 0.28f);
        [Tooltip("Side this enemy fights for; projectiles it fires never hit its own kind.")]
        [SerializeField] private Faction _faction = Faction.Enemy;
        private readonly AttackRunner _runner = new AttackRunner();
        private IFacing _facing;
        private float _facingLockTime;
        private AttackSpawn _spawn;
        private Transform _target;
        private Vector2 _lunge;
        private EnemyMotor2D _motor;

        /// <summary>Raised when the windup begins, so audio and animation can play the tell.</summary>
        public event Action<AttackDefinition> AttackTelegraphed;
        public event Action<AttackDefinition> AttackLanded;
        public event Action<AttackDefinition> AttackFinished;
        public event Action<CombatActor, DamageResult> HitLanded;

        public bool IsAttacking => _runner.IsBusy;
        public AttackDefinition CurrentAttack => _runner.Current;
        public AttackPhase Phase => _runner.Phase;

        /// <summary>Time since the swing started; animation and tests read the windup from it.</summary>
        public float Elapsed => _runner.Elapsed;

        /// <summary>
        /// True once the swing has committed to a direction. Before this the enemy may still turn
        /// with its target; afterwards a dodge behind it is rewarded.
        /// </summary>
        public bool IsCommitted => _runner.IsBusy && _runner.Elapsed >= _facingLockTime;

        private void Awake()
        {
            _facing = GetComponent<IFacing>();
            _motor = GetComponent<EnemyMotor2D>();
            if (_hitbox == null)
            {
                Debug.LogError("EnemyCombat requires a hitbox.", this);
                enabled = false;
                return;
            }

            SetTelegraph(false);
        }

        private void OnEnable()
        {
            _runner.ActiveStarted += OnActiveStarted;
            _runner.ActiveEnded += OnActiveEnded;
            _runner.Finished += OnFinished;
            _hitbox.Hit += OnHit;
        }

        private void OnDisable()
        {
            _runner.ActiveStarted -= OnActiveStarted;
            _runner.ActiveEnded -= OnActiveEnded;
            _runner.Finished -= OnFinished;
            if (_hitbox != null)
            {
                _hitbox.Hit -= OnHit;
            }
            Cancel();
        }

        /// <summary>
        /// Starts a swing. The optional spawn is what the attack sends out when it turns active —
        /// an arrow, an orb, a pool — and the target is only used to aim it.
        /// </summary>
        public bool TryStartAttack(AttackDefinition attack, float facingLockTime,
            AttackSpawn spawn = null, Transform target = null, Vector2 lunge = default)
        {
            if (attack == null || !enabled || _runner.IsBusy)
            {
                return false;
            }

            _spawn = spawn;
            _target = target;
            _lunge = lunge;
            _facingLockTime = Mathf.Max(0f, facingLockTime);
            _hitbox.End();
            _runner.Begin(attack, 1f);
            SetTelegraph(true);
            AttackTelegraphed?.Invoke(attack);
            return true;
        }

        public void Tick(float deltaTime)
        {
            _runner.Tick(deltaTime);
        }

        /// <summary>Stops the swing and closes the hitbox in the same frame.</summary>
        public void Cancel()
        {
            _runner.Cancel();
            _hitbox?.End();
            SetTelegraph(false);
        }

        private void OnActiveStarted(AttackDefinition attack)
        {
            var facing = _facing?.FacingDirection ?? 1;
            SetTelegraph(false);
            var damage = attack.BuildDamage(gameObject, this, facing);

            // A ranged attack still opens its hitbox: the shot is what threatens, but an archer
            // loosing an arrow into someone standing on top of it should not be harmless.
            _hitbox.Begin(damage, attack.HitboxOffset, attack.HitboxSize, facing);
            _spawn?.Emit(damage, _faction, transform, facing, _target);

            // The leap commits here, at the end of the windup the player watched — not when the
            // attack was chosen. Direction is taken from facing, which is already locked by now,
            // so a hound cannot steer into a dodge after leaving the ground.
            if (_motor != null && _lunge.sqrMagnitude > 0.0001f)
            {
                _motor.Launch(new Vector2(_lunge.x * facing, _lunge.y));
            }

            AttackLanded?.Invoke(attack);
        }

        private void OnActiveEnded()
        {
            _hitbox.End();
        }

        private void OnFinished(AttackDefinition attack)
        {
            SetTelegraph(false);
            AttackFinished?.Invoke(attack);
        }

        private void OnHit(CombatActor target, DamageResult result)
        {
            HitLanded?.Invoke(target, result);
        }

        private void SetTelegraph(bool isVisible)
        {
            if (_telegraph == null)
            {
                return;
            }

            _telegraph.enabled = isVisible;
            _telegraph.color = _telegraphColor;
        }
    }
}
