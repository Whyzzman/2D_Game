using System;
using UnityEngine;

namespace ForgottenKingdom
{
    public enum EnemyState
    {
        Idle,
        Patrol,
        Alert,
        Chase,
        /// <summary>Shield up, waiting behind it.</summary>
        Guard,
        Attack,
        /// <summary>A short recoil after the shield ate a hit; not a full flinch.</summary>
        GuardImpact,
        /// <summary>The shield ran out of Guard: wide open until it comes back.</summary>
        GuardBroken,
        Search,
        ReturnToPatrol,
        Hurt,
        Stunned,
        Dead
    }

    /// <summary>
    /// The decision layer shared by every walking enemy. It owns the state machine and the target,
    /// and delegates: <see cref="EnemyMotor2D"/> moves, <see cref="EnemyCombat"/> swings and
    /// <see cref="CombatActor"/> resolves incoming hits. A new enemy kind is a new
    /// <see cref="EnemyDefinition"/> and prefab.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyMotor2D), typeof(CombatActor), typeof(EnemyCombat))]
    public sealed class EnemyController : MonoBehaviour
    {
        [SerializeField] private EnemyDefinition _definition;
        [SerializeField] private EnemyMotor2D _motor;
        [SerializeField] private CombatActor _actor;
        [SerializeField] private EnemyCombat _combat;
        [SerializeField] private EnemyGuard _guard;
        [SerializeField] private Collider2D _bodyCollider;
        [SerializeField] private GameObject _visualRoot;
        [SerializeField] private EnemyResetPolicy _resetPolicy =
            EnemyResetPolicy.RespawnOnRestOrDeath;
        [Tooltip("Leave empty to find the registry and the player once on start.")]
        [SerializeField] private EnemyRegistry _registry;
        [SerializeField] private PlayerCharacter _target;
        private float _corpseHiddenAt = -1f;
        private Vector2 _spawnPosition;
        private int _spawnFacing = 1;
        private Vector2 _lastKnownTargetPosition;
        private EnemyState _state = EnemyState.Idle;
        private float _stateTimer;
        private float _decisionTimer;
        private float _attackReadyAt;
        private float _hurtReadyAt;
        private float _idleDuration;
        private float _chaseTime;
        private float _guardReadyAt;
        private float _guardUntil;
        private float _guardImpactUntil;
        private float _decisionReadyAt;
        private float[] _attackReadyPerOption = Array.Empty<float>();
        private EnemySenses _senses;

        public event Action<EnemyState> StateChanged;
        public event Action Alerted;
        public event Action<AttackDefinition> AttackTelegraphed;
        public event Action<DamageInfo> Hurt;
        public event Action<DamageInfo> Parried;
        public event Action<float> StaggerBroken;

        /// <summary>The shield stopped a blow; the hook for a spark and its own sound.</summary>
        public event Action<DamageInfo> GuardImpacted;
        public event Action GuardBrokenOpen;
        public event Action GuardRecovered;
        public event Action<EnemyController> Died;

        /// <summary>Raised after a world reset put this enemy back; a hook for animation and VFX.</summary>
        public event Action<EnemyController> EnemyReset;

        public EnemyState State => _state;
        public EnemyDefinition Definition => _definition;
        public CombatActor Actor => _actor;
        public EnemyCombat Combat => _combat;
        public PlayerCharacter Target => _target;
        public int FacingDirection => _motor != null ? _motor.FacingDirection : 1;
        public bool IsAlive => _state != EnemyState.Dead && _actor.IsAlive;
        public EnemyGuard Guard => _guard;
        public EnemyResetPolicy ResetPolicy => _resetPolicy;
        public bool IsAttackReady => Time.time >= _attackReadyAt;
        public float AttackCooldownRemaining => Mathf.Max(0f, _attackReadyAt - Time.time);

        /// <summary>
        /// Which move the running swing is. The AI already knows this when it picks an option; it
        /// simply had nowhere to say it, and the animation cannot tell a bash from a heavy by the
        /// attack data alone. Read-only, and meaningful only while attacking.
        /// </summary>
        public EnemyAttackKind CurrentAttackKind { get; private set; } = EnemyAttackKind.Normal;

        /// <summary>Remaining cooldown of one attack option, for the debug readout.</summary>
        public float OptionCooldownRemaining(int index)
        {
            return index >= 0 && index < _attackReadyPerOption.Length
                ? Mathf.Max(0f, _attackReadyPerOption[index] - Time.time)
                : 0f;
        }

        private void Awake()
        {
            if (_motor == null) { _motor = GetComponent<EnemyMotor2D>(); }
            if (_actor == null) { _actor = GetComponent<CombatActor>(); }
            if (_combat == null) { _combat = GetComponent<EnemyCombat>(); }
            if (_guard == null) { _guard = GetComponent<EnemyGuard>(); }
            if (_bodyCollider == null) { _bodyCollider = GetComponent<Collider2D>(); }
            if (_definition == null)
            {
                Debug.LogError("EnemyController requires an EnemyDefinition.", this);
                enabled = false;
                return;
            }

            _senses = new EnemySenses(_definition, _motor, transform);
            _spawnPosition = transform.position;
            _spawnFacing = _motor.FacingDirection;
            _idleDuration = RollIdleTime();
            _attackReadyPerOption = new float[_definition.AttackCount];
        }

        private void OnEnable()
        {
            _actor.DamageTaken += OnDamageTaken;
            _actor.AttackParried += OnAttackParried;
            _actor.Killed += OnKilled;
            _combat.AttackTelegraphed += OnAttackTelegraphed;
            _combat.AttackFinished += OnAttackFinished;
            if (_actor.Stagger != null)
            {
                _actor.Stagger.Staggered += OnStaggered;
            }
            if (_guard != null)
            {
                _guard.GuardBroken += OnGuardBroken;
                _guard.GuardHit += OnGuardHit;
            }
        }

        private void OnDisable()
        {
            _actor.DamageTaken -= OnDamageTaken;
            _actor.AttackParried -= OnAttackParried;
            _actor.Killed -= OnKilled;
            _combat.AttackTelegraphed -= OnAttackTelegraphed;
            _combat.AttackFinished -= OnAttackFinished;
            if (_actor.Stagger != null)
            {
                _actor.Stagger.Staggered -= OnStaggered;
            }
            if (_guard != null)
            {
                _guard.GuardBroken -= OnGuardBroken;
                _guard.GuardHit -= OnGuardHit;
                _guard.LowerGuard();
            }
            _combat.Cancel();
        }

        private void Start()
        {
            // Resolved once instead of searching the scene while the enemy is running.
            if (_target == null)
            {
                _target = FindAnyObjectByType<PlayerCharacter>();
            }

            if (_registry == null)
            {
                _registry = FindAnyObjectByType<EnemyRegistry>();
            }

            _registry?.Register(this);
        }

        private void OnDestroy()
        {
            _registry?.Unregister(this);
        }

        private void FixedUpdate()
        {
            var deltaTime = Time.fixedDeltaTime;
            _stateTimer += deltaTime;
            _combat.Tick(deltaTime);
            if (_state == EnemyState.Dead)
            {
                // The corpse lingers, then hides. The object stays alive so a rest can revive it
                // instead of spawning a duplicate.
                if (_corpseHiddenAt > 0f && Time.time >= _corpseHiddenAt)
                {
                    _corpseHiddenAt = -1f;
                    SetVisible(false);
                }
                return;
            }

            // Swings keep tracking the target only until they commit to a direction.
            if (_combat.IsAttacking && !_combat.IsCommitted && HasTarget)
            {
                FaceTarget();
            }

            _decisionTimer += deltaTime;
            if (_decisionTimer < _definition.AiTickInterval && _state != EnemyState.Attack)
            {
                ContinueMovement();
                return;
            }

            _decisionTimer = 0f;
            Decide();
        }

        // ---------- public API used by other systems ----------

        public void SetTarget(PlayerCharacter target)
        {
            _target = target;
        }

        public void ClearTarget()
        {
            _lastKnownTargetPosition = transform.position;
            if (_state == EnemyState.Chase || _state == EnemyState.Attack
                || _state == EnemyState.Alert || _state == EnemyState.Search)
            {
                Enter(EnemyState.ReturnToPatrol);
            }
        }

        /// <summary>
        /// Puts the enemy back the way it spawned. A checkpoint rest will call this once
        /// respawning ordinary enemies is wired up.
        /// </summary>
        public void ResetEnemy()
        {
            _actor.Statuses?.Clear();
            _combat.Cancel();
            _actor.ClearInvulnerability();
            _actor.Health.RestoreToFull();
            _actor.Stagger?.ResetStagger();
            _motor.enabled = true;
            _motor.Teleport(_spawnPosition);
            _motor.Face(_spawnFacing);
            if (_bodyCollider != null)
            {
                _bodyCollider.enabled = true;
            }

            _corpseHiddenAt = -1f;
            SetVisible(true);
            _guard?.ResetGuard();
            // A revived enemy never gets a free opening swing.
            _attackReadyAt = Time.time + _definition.AlertTime;
            _hurtReadyAt = 0f;
            _chaseTime = 0f;
            _guardReadyAt = 0f;
            _guardUntil = 0f;
            _guardImpactUntil = 0f;
            _decisionReadyAt = 0f;
            for (var i = 0; i < _attackReadyPerOption.Length; i++)
            {
                _attackReadyPerOption[i] = 0f;
            }
            _idleDuration = RollIdleTime();
            _lastKnownTargetPosition = _spawnPosition;
            Enter(EnemyState.Idle, force: true);
            EnemyReset?.Invoke(this);
        }

        public bool CanAttack()
        {
            return IsAlive && !_combat.IsAttacking && IsAttackReady && IsTargetInStrikingRange()
                && _state != EnemyState.GuardBroken && _state != EnemyState.Stunned;
        }

        // ---------- state machine ----------

        private void Decide()
        {
            switch (_state)
            {
                case EnemyState.Idle:
                    TickIdle();
                    break;
                case EnemyState.Patrol:
                    TickPatrol();
                    break;
                case EnemyState.Alert:
                    TickAlert();
                    break;
                case EnemyState.Chase:
                    TickChase();
                    break;
                case EnemyState.Guard:
                    TickGuard();
                    break;
                case EnemyState.GuardImpact:
                    TickGuardImpact();
                    break;
                case EnemyState.GuardBroken:
                    TickGuardBroken();
                    break;
                case EnemyState.Attack:
                    TickAttack();
                    break;
                case EnemyState.Search:
                    TickSearch();
                    break;
                case EnemyState.ReturnToPatrol:
                    TickReturn();
                    break;
                case EnemyState.Hurt:
                case EnemyState.Stunned:
                    TickRecovering();
                    break;
            }
        }

        /// <summary>Keeps the current motion between decision ticks so movement stays smooth.</summary>
        private void ContinueMovement()
        {
            switch (_state)
            {
                case EnemyState.Patrol:
                    _motor.Drive(_definition.PatrolSpeed);
                    break;
                case EnemyState.ReturnToPatrol:
                    _motor.Drive(_definition.ReturnSpeed);
                    break;
                case EnemyState.GuardBroken:
                case EnemyState.GuardImpact:
                case EnemyState.Stunned:
                    _motor.Stop();
                    break;
                default:
                    break;
            }
        }

        private void TickIdle()
        {
            _motor.Stop();
            _guard?.LowerGuard();
            if (CanSeeTarget())
            {
                Enter(EnemyState.Alert);
                return;
            }

            if (_stateTimer >= _idleDuration)
            {
                Enter(EnemyState.Patrol);
            }
        }

        private void TickPatrol()
        {
            if (CanSeeTarget())
            {
                Enter(EnemyState.Alert);
                return;
            }

            if (IsBlockedAhead() || IsOutsidePatrolArea())
            {
                _motor.Face(-_motor.FacingDirection);
                _idleDuration = RollIdleTime();
                Enter(EnemyState.Idle);
                return;
            }

            _motor.Drive(_definition.PatrolSpeed);
        }

        private void TickAlert()
        {
            _motor.Stop();
            if (HasTarget)
            {
                FaceTarget();
            }

            if (_stateTimer >= _definition.AlertTime)
            {
                _chaseTime = 0f;
                Enter(EnemyState.Chase);
            }
        }

        private void TickChase()
        {
            if (!HasTarget || !_target.IsAlive)
            {
                // Nothing left to fight: go home instead of beating a corpse.
                Enter(EnemyState.ReturnToPatrol);
                return;
            }

            _chaseTime += _definition.AiTickInterval;
            var horizontal = Mathf.Abs(_target.transform.position.x - transform.position.x);
            if (horizontal > _definition.LoseSightRange || !HasLineOfSightToTarget()
                || _chaseTime > _definition.MaxChaseTime)
            {
                Enter(EnemyState.Search);
                return;
            }

            _lastKnownTargetPosition = _target.transform.position;
            if (IsTargetInStrikingRange())
            {
                // The limit is for a chase that never reaches its target. Once the target is in
                // reach this is a fight, and a fight must not run out of time and hand the
                // player a free opening while the enemy stands searching for them.
                _chaseTime = 0f;
            }

            FaceTarget();
            if (CanAttack() && TryBeginAttack(horizontal))
            {
                return;
            }

            if (ShouldRaiseGuard(horizontal))
            {
                _guard.RaiseGuard();
                _guardUntil = Time.time + UnityEngine.Random.Range(
                    _guard.MinGuardDuration, Mathf.Max(_guard.MinGuardDuration,
                        _guard.MaxGuardDuration));
                Enter(EnemyState.Guard);
                return;
            }

            if (TryKeepDistance(horizontal))
            {
                return;
            }

            // Stop at the edge of striking distance, at a ledge, or against a wall.
            var holdAt = _definition.KeepsDistance
                ? _definition.PreferredRange
                : _definition.AttackRange * 0.85f;
            if (horizontal <= holdAt || IsBlockedAhead())
            {
                _motor.Stop();
                return;
            }

            _motor.Drive(_definition.ChaseSpeed);
        }

        /// <summary>
        /// The archer's footwork. A ranged enemy that stood still while being hit in the face would
        /// be a free kill and would make the bow pointless, so inside its retreat range it backs
        /// away — still facing the player, and never over a ledge or into a wall. When it has
        /// nowhere left to go it stops retreating and fights, rather than grinding against the
        /// scenery until it dies.
        /// </summary>
        private bool TryKeepDistance(float horizontal)
        {
            if (!_definition.KeepsDistance || horizontal > _definition.RetreatRange
                || _target == null)
            {
                return false;
            }

            var away = _target.transform.position.x > transform.position.x ? -1 : 1;
            if (!_motor.HasGroundToward(away) || _motor.HasWallToward(away))
            {
                return false;
            }

            _motor.DriveToward(away, _definition.RetreatSpeed);
            return true;
        }

        /// <summary>
        /// Behind the shield: it closes slowly, swings when an opening appears, and drops the
        /// guard when its window expires so the player always gets a turn.
        /// </summary>
        private void TickGuard()
        {
            if (!HasTarget || !_target.IsAlive || _guard == null)
            {
                LowerGuardAndChase();
                return;
            }

            var horizontal = Mathf.Abs(_target.transform.position.x - transform.position.x);
            if (horizontal > _definition.LoseSightRange || !HasLineOfSightToTarget())
            {
                LowerGuardAndChase();
                Enter(EnemyState.Search);
                return;
            }

            FaceTarget();
            if (Time.time >= _guardUntil || !_guard.CanRaiseGuard)
            {
                LowerGuardAndChase();
                return;
            }

            if (Time.time >= _decisionReadyAt && CanAttack() && TryBeginAttack(horizontal))
            {
                return;
            }

            // Advance slowly behind the shield rather than standing still.
            if (horizontal <= _definition.AttackRange || IsBlockedAhead())
            {
                _motor.Stop();
                return;
            }

            _motor.Drive(_definition.ChaseSpeed * _definition.GuardMovementScale);
        }

        private void TickGuardImpact()
        {
            _motor.Stop();
            if (Time.time >= _guardImpactUntil)
            {
                Enter(_guard != null && _guard.IsRaised ? EnemyState.Guard : EnemyState.Chase);
            }
        }

        private void TickGuardBroken()
        {
            _motor.Stop();
            if (_guard == null || _stateTimer < _guard.BreakDuration)
            {
                return;
            }

            _guard.RecoverFromBreak();
            _guardReadyAt = Time.time + _guard.GuardCooldown;
            GuardRecovered?.Invoke();
            Enter(CanSeeTarget() ? EnemyState.Chase : EnemyState.ReturnToPatrol);
        }

        private void LowerGuardAndChase()
        {
            if (_guard != null)
            {
                _guard.LowerGuard();
                _guardReadyAt = Time.time + _guard.GuardCooldown;
            }

            Enter(EnemyState.Chase);
        }

        /// <summary>
        /// The shield goes up in reaction to an observable threat: the target is close, facing this
        /// way, and the cooldown has elapsed. There is no peeking at what button will be pressed.
        /// </summary>
        private bool ShouldRaiseGuard(float horizontal)
        {
            if (_guard == null || !_guard.CanRaiseGuard || Time.time < _guardReadyAt
                || horizontal > _definition.AttackRange * 2.2f)
            {
                return false;
            }

            var towardEnemy = transform.position.x - _target.transform.position.x;
            var isTargetFacingUs = _target.GetComponent<IFacing>() is { } facing
                && Mathf.Sign(towardEnemy) == facing.FacingDirection;
            return isTargetFacingUs;
        }

        private void TickAttack()
        {
            _motor.Stop();
            if (!_combat.IsAttacking)
            {
                Enter(EnemyState.Chase);
            }
        }

        private void TickSearch()
        {
            _guard?.LowerGuard();
            if (CanSeeTarget())
            {
                Enter(EnemyState.Alert);
                return;
            }

            if (_stateTimer >= _definition.SearchTime)
            {
                Enter(EnemyState.ReturnToPatrol);
                return;
            }

            var delta = _lastKnownTargetPosition.x - transform.position.x;
            if (Mathf.Abs(delta) < 0.4f || IsBlockedAhead())
            {
                _motor.Stop();
                return;
            }

            _motor.Face(delta > 0f ? 1 : -1);
            _motor.Drive(_definition.PatrolSpeed);
        }

        private void TickReturn()
        {
            _guard?.LowerGuard();
            if (CanSeeTarget())
            {
                Enter(EnemyState.Alert);
                return;
            }

            var delta = _spawnPosition.x - transform.position.x;
            if (Mathf.Abs(delta) < 0.5f || IsBlockedAhead())
            {
                _motor.Stop();
                _idleDuration = RollIdleTime();
                Enter(EnemyState.Idle);
                return;
            }

            _motor.Face(delta > 0f ? 1 : -1);
            _motor.Drive(_definition.ReturnSpeed);
        }

        private void TickRecovering()
        {
            _motor.Stop();
            _guard?.LowerGuard();
            var isStunned = _actor.Stagger != null && _actor.Stagger.IsStunned;
            var window = _state == EnemyState.Stunned ? 0f : _definition.HurtTime;
            if (_stateTimer < window || isStunned)
            {
                return;
            }

            // Leaving a stun never comes with a free swing.
            _attackReadyAt = Mathf.Max(_attackReadyAt, Time.time + _definition.AlertTime);
            Enter(CanSeeTarget() ? EnemyState.Chase : EnemyState.ReturnToPatrol);
        }

        /// <summary>
        /// Picks a move by context rather than in turn: range decides what is reachable, pressure
        /// on the shield unlocks the bash, and a guarding target makes a heavy blow more likely.
        /// </summary>
        private bool TryBeginAttack(float distance)
        {
            var pressure = _guard != null ? _guard.Pressure : 0f;
            var isTargetGuarding = _target != null && _target.Defense != null
                && _target.Defense.IsBlocking;
            var totalWeight = 0f;
            for (var i = 0; i < _definition.AttackCount; i++)
            {
                if (IsOptionAvailable(i, distance, pressure))
                {
                    totalWeight += _definition.Attack(i).ScoreFor(isTargetGuarding);
                }
            }

            if (totalWeight <= 0f)
            {
                return false;
            }

            var roll = UnityEngine.Random.Range(0f, totalWeight);
            for (var i = 0; i < _definition.AttackCount; i++)
            {
                if (!IsOptionAvailable(i, distance, pressure))
                {
                    continue;
                }

                var option = _definition.Attack(i);
                roll -= option.ScoreFor(isTargetGuarding);
                if (roll > 0f)
                {
                    continue;
                }

                return StartOption(i, option);
            }

            return false;
        }

        private bool IsOptionAvailable(int index, float distance, float pressure)
        {
            var option = _definition.Attack(index);
            return option != null
                && option.IsUsable(distance, pressure)
                && Time.time >= _attackReadyPerOption[index];
        }

        private bool StartOption(int index, EnemyAttackOption option)
        {
            // A group takes turns: with every token out, this enemy keeps its guard and its
            // ground, and asks again on its next decision instead of piling on another attack.
            if (_registry != null && !_registry.TryTakeAttackToken(this))
            {
                return false;
            }

            _motor.Stop();
            _guard?.LowerGuard();
            var lockTime = option.FacingLockTime > 0f
                ? option.FacingLockTime
                : _definition.FacingLockTime;
            var target = _target != null ? _target.transform : null;
            if (!_combat.TryStartAttack(option.Attack, lockTime, option.Spawn, target,
                    option.Lunge))
            {
                return false;
            }

            _chaseTime = 0f;
            _attackReadyPerOption[index] = Time.time + option.Cooldown;
            _decisionReadyAt = Time.time + _definition.DecisionCooldown;
            CurrentAttackKind = option.Kind;
            if (option.Kind == EnemyAttackKind.ShieldBash)
            {
                // The bash is the answer to pressure, so it clears the meter that summoned it.
                _guard?.ResetPressure();
            }

            Enter(EnemyState.Attack);
            return true;
        }

        // ---------- senses ----------

        private bool HasTarget => _target != null;

        /// <summary>Sight, and a sighting refreshes where the target was last seen.</summary>
        private bool CanSeeTarget()
        {
            if (!_senses.CanSee(_target))
            {
                return false;
            }

            _lastKnownTargetPosition = _target.transform.position;
            return true;
        }

        private bool HasLineOfSightToTarget() => _senses.HasLineOfSight(_target);

        private bool IsTargetInStrikingRange() => _senses.IsInStrikingRange(_target);

        private bool IsBlockedAhead() => _senses.IsBlockedAhead();

        private bool IsOutsidePatrolArea()
        {
            var offset = transform.position.x - _spawnPosition.x;
            return Mathf.Abs(offset) >= _definition.PatrolRange
                && Mathf.Sign(offset) == _motor.FacingDirection;
        }

        private void FaceTarget()
        {
            var delta = _target.transform.position.x - transform.position.x;
            if (Mathf.Abs(delta) >= _definition.FacingDeadZone)
            {
                _motor.Face(delta > 0f ? 1 : -1);
            }
        }

        private float RollIdleTime()
        {
            return _definition == null
                ? 1f
                : _definition.IdleTime + UnityEngine.Random.Range(0f, _definition.IdleTimeVariance);
        }

        // ---------- reactions ----------

        private void OnAttackTelegraphed(AttackDefinition attack)
        {
            AttackTelegraphed?.Invoke(attack);
        }

        private void OnAttackFinished(AttackDefinition attack)
        {
            _attackReadyAt = Time.time + _definition.AttackCooldown;
        }

        private void OnDamageTaken(DamageInfo info, DamageResult result)
        {
            if (info.IsStatusDamage) { return; }
            if (_state == EnemyState.Dead)
            {
                return;
            }

            if (result.Outcome != DamageOutcome.Hit || result.IsLethal)
            {
                return;
            }

            // Being hit reveals the attacker even from behind.
            if (info.Attacker != null)
            {
                _lastKnownTargetPosition = info.Attacker.transform.position;
            }

            // Being hit is being fought, not chasing in vain.
            _chaseTime = 0f;
            Hurt?.Invoke(info);
            if (_state == EnemyState.Stunned || _state == EnemyState.GuardBroken
                || Time.time < _hurtReadyAt)
            {
                // Flinch has a cooldown, so a flurry of light hits cannot freeze the enemy, and a
                // broken guard is already a long opening that must not be extended.
                return;
            }

            _hurtReadyAt = Time.time + _definition.HitReactionCooldown;
            _combat.Cancel();
            Enter(EnemyState.Hurt, force: true);
        }

        /// <summary>A perfect parry always breaks the swing, whatever the poise meter says.</summary>
        private void OnAttackParried(DamageInfo info)
        {
            if (_state == EnemyState.Dead)
            {
                return;
            }

            _combat.Cancel();
            _attackReadyAt = Time.time + _definition.AttackCooldown;
            // A parry rattles the shield too, so skilled timing breaks a guard without heavy spam.
            _guard?.ApplyParryDamage();
            Parried?.Invoke(info);
            if (_state != EnemyState.Stunned && _state != EnemyState.GuardBroken)
            {
                Enter(EnemyState.Hurt, force: true);
            }
        }

        /// <summary>The shield ate a blow: a short recoil, not the full flinch a body hit causes.</summary>
        private void OnGuardHit(DamageInfo info)
        {
            GuardImpacted?.Invoke(info);
            if (_state == EnemyState.Dead || _state == EnemyState.GuardBroken)
            {
                return;
            }

            // A heavier blow rocks the shield for longer.
            _guardImpactUntil = Time.time
                + Mathf.Clamp(info.EffectiveGuardDamage * 0.004f, 0.1f, 0.35f);
            Enter(EnemyState.GuardImpact, force: true);
        }

        private void OnGuardBroken()
        {
            if (_state == EnemyState.Dead)
            {
                return;
            }

            _combat.Cancel();
            _motor.Stop();
            GuardBrokenOpen?.Invoke();
            Enter(EnemyState.GuardBroken, force: true);
        }

        private void OnStaggered(float duration)
        {
            if (_state == EnemyState.Dead)
            {
                return;
            }

            _combat.Cancel();
            StaggerBroken?.Invoke(duration);
            Enter(EnemyState.Stunned, force: true);
        }

        private void OnKilled(DamageInfo info)
        {
            _combat.Cancel();
            _motor.Stop();
            _motor.enabled = false;
            _guard?.LowerGuard();
            if (_bodyCollider != null)
            {
                _bodyCollider.enabled = false;
            }
            Enter(EnemyState.Dead, force: true);
            _corpseHiddenAt = Time.time + _definition.DeathTime;
            Died?.Invoke(this);
        }

        private void SetVisible(bool isVisible)
        {
            if (_visualRoot != null)
            {
                _visualRoot.SetActive(isVisible);
            }
        }

        private void Enter(EnemyState next, bool force = false)
        {
            if (_state == next && !force)
            {
                return;
            }

            if (_state == EnemyState.Dead && !force)
            {
                return;
            }

            _state = next;
            _stateTimer = 0f;
            _decisionTimer = 0f;
            if (next == EnemyState.Alert)
            {
                Alerted?.Invoke();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_definition == null)
            {
                return;
            }

            Gizmos.color = new Color(1f, 0.7f, 0.1f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, _definition.DetectionRange);
            Gizmos.color = new Color(1f, 0.2f, 0.1f, 0.6f);
            Gizmos.DrawWireCube(transform.position,
                new Vector3(_definition.AttackRange * 2f, _definition.VerticalAttackRange * 2f, 0f));
            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.45f);
            Gizmos.DrawWireCube(new Vector3(_spawnPosition.x, transform.position.y, 0f),
                new Vector3(_definition.PatrolRange * 2f, 0.4f, 0f));
        }
    }
}
