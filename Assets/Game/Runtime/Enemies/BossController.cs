using System;
using System.Collections;
using UnityEngine;

namespace ForgottenKingdom
{
    public enum BossState
    {
        /// <summary>Asleep until the arena wakes it.</summary>
        Dormant,
        /// <summary>The short wake-up before the first swing; untouchable and harmless.</summary>
        Intro,
        Idle,
        Reposition,
        Attacking,
        /// <summary>A brief recoil after its swing was parried.</summary>
        Recoil,
        PhaseTransition,
        Stunned,
        Dead
    }

    /// <summary>Where a boss swing currently is, for animation and for the debug readout.</summary>
    public enum BossSwingPhase
    {
        None,
        Windup,
        /// <summary>The committed run of a charge, or the airborne part of a leap.</summary>
        Travel,
        Active,
        Recovery
    }

    /// <summary>
    /// Guardian of the Tomb. It sleeps until the arena wakes it, picks moves by distance, phase and
    /// what it did last, and drops its shield for a faster, meaner second phase. The fight is driven
    /// by one coroutine so a multi-swing combo or a leap reads as a single committed move; hits
    /// themselves still go through the shared <see cref="Hitbox"/> and <see cref="DamageInfo"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyMotor2D), typeof(CombatActor))]
    public sealed class BossController : MonoBehaviour
    {
        [SerializeField] private string _bossId = "guardian-of-the-tomb";
        [SerializeField] private string _displayName = "Страж Гробниці";
        [Tooltip("The line over the name on the boss's title card.")]
        [SerializeField] private string _epithet = string.Empty;
        [SerializeField] private EnemyMotor2D _motor;
        [SerializeField] private CombatActor _actor;
        [SerializeField] private Hitbox _hitbox;
        [SerializeField] private Shockwave _shockwave;
        [SerializeField] private Collider2D _bodyCollider;
        [Tooltip("Carried in phase one and thrown away for phase two.")]
        [SerializeField] private GameObject _shieldVisual;
        [SerializeField] private BossAttack[] _attacks = { new BossAttack() };

        [Header("Phases")]
        [SerializeField, Min(1)] private int _totalPhases = 2;
        [SerializeField, Range(0.05f, 0.95f)] private float _phaseTwoHealthFraction = 0.5f;
        [SerializeField, Min(0f)] private float _phaseTransitionTime = 2f;
        [Tooltip("Attack speed multiplier in phase two.")]
        [SerializeField, Min(0.1f)] private float _phaseTwoAttackSpeed = 1.2f;
        [Tooltip("Pause multiplier in phase two; below 1 means shorter pauses.")]
        [SerializeField, Range(0.1f, 1f)] private float _phaseTwoPauseScale = 0.7f;
        [Tooltip("Move speed multiplier in phase two.")]
        [SerializeField, Min(0.1f)] private float _phaseTwoMoveScale = 1.15f;

        [Header("Pacing")]
        [SerializeField, Min(0f)] private float _introDuration = 1.4f;
        [SerializeField, Min(0f)] private float _idlePause = 1.1f;
        [SerializeField, Min(0f)] private float _repositionSpeed = 3.2f;
        [SerializeField, Min(0f)] private float _repositionTimeout = 1.6f;
        [SerializeField, Min(0f)] private float _preferredRange = 2f;
        [Tooltip("Closer than this the boss backs off instead of being stood inside.")]
        [SerializeField, Min(0f)] private float _tooCloseRange = 0.9f;

        [Header("Reactions")]
        [Tooltip("Poise damage is ignored for this long after a stun, to stop a stun loop.")]
        [SerializeField, Min(0f)] private float _staggerImmunityAfterStun = 1.5f;
        [SerializeField, Min(0f)] private float _parryRecoilTime = 0.7f;
        private float[] _attackReadyAt = Array.Empty<float>();
        private Coroutine _routine;
        private Transform _player;
        private Vector2 _spawnPosition;
        private int _spawnFacing = -1;
        private int _lastAttackIndex = -1;

        public event Action<float, float> HealthChanged;
        public event Action<int> PhaseChanged;
        public event Action<BossController> Defeated;
        public event Action<BossState> StateChanged;

        // Hooks for audio, music and VFX; they fire with no assets attached.
        public event Action FightStarted;
        public event Action<BossAttackKind> AttackStarted;

        /// <summary>A swing begins its windup: which movement the attack carries and which swing it is.</summary>
        public event Action<BossAttackMovement, int> SwingWindup;

        /// <summary>A charge starts running or a leap leaves the ground.</summary>
        public event Action<BossAttackMovement> MovementBegan;

        /// <summary>A charge stops or a leap lands.</summary>
        public event Action<BossAttackMovement> MovementEnded;
        public event Action<float> Staggered;
        public event Action<DamageInfo> AttackParried;
        public event Action ShieldDropped;

        public string BossId => _bossId;
        public string DisplayName => _displayName;
        public string Epithet => _epithet;

        /// <summary>Health share at which phase two begins; the boss bar marks it.</summary>
        public float PhaseTwoHealthFraction => _phaseTwoHealthFraction;
        public BossState State { get; private set; } = BossState.Dormant;
        public int Phase { get; private set; } = 1;
        public int TotalPhases => _totalPhases;
        public CombatActor Actor => _actor;
        public bool IsFighting => State != BossState.Dormant && State != BossState.Dead;
        public BossAttackKind? LastAttack => _lastAttackIndex >= 0 && _attacks != null
            && _lastAttackIndex < _attacks.Length
            ? _attacks[_lastAttackIndex].Kind
            : null;

        /// <summary>
        /// The move being performed right now, and which swing of it. The routine already knows
        /// both; it simply had nowhere to say so, and the three swings of the combo have to look
        /// different for the player to be able to count them. Read-only, null when not attacking.
        /// </summary>
        public BossAttackKind? CurrentAttackKind { get; private set; }

        public int CurrentSwingIndex { get; private set; }

        /// <summary>The swing in flight, so animation can be fitted to its real windup.</summary>
        public AttackDefinition CurrentSwing { get; private set; }

        /// <summary>
        /// Where in the swing the routine currently is. The coroutine is the only thing that knows
        /// the difference between winding up and travelling, and a charge that looked identical
        /// while telegraphing and while already committed would be unreadable.
        /// </summary>
        public BossSwingPhase SwingPhase { get; private set; }

        public float CooldownRemaining(int index)
        {
            return index >= 0 && index < _attackReadyAt.Length
                ? Mathf.Max(0f, _attackReadyAt[index] - Time.time)
                : 0f;
        }

        private void Awake()
        {
            if (_motor == null) { _motor = GetComponent<EnemyMotor2D>(); }
            if (_actor == null) { _actor = GetComponent<CombatActor>(); }
            if (_hitbox == null)
            {
                Debug.LogError("BossController requires a hitbox.", this);
                enabled = false;
                return;
            }

            _attackReadyAt = new float[_attacks != null ? _attacks.Length : 0];
            _spawnPosition = transform.position;
            _spawnFacing = _motor.FacingDirection;
        }

        private void Start() => _actor.Toughen(NewGamePlus.Scale);

        private void OnEnable()
        {
            _actor.DamageTaken += OnDamageTaken;
            _actor.AttackParried += OnAttackParried;
            _actor.Killed += OnKilled;
            _actor.Health.HealthChanged += OnHealthChanged;
            if (_actor.Stagger != null)
            {
                _actor.Stagger.Staggered += OnStaggered;
            }
        }

        private void OnDisable()
        {
            _actor.DamageTaken -= OnDamageTaken;
            _actor.AttackParried -= OnAttackParried;
            _actor.Killed -= OnKilled;
            _actor.Health.HealthChanged -= OnHealthChanged;
            if (_actor.Stagger != null)
            {
                _actor.Stagger.Staggered -= OnStaggered;
            }
            StopRoutine();
        }

        public void BeginFight(PlayerCharacter player)
        {
            if (player == null || State != BossState.Dormant || !_actor.IsAlive)
            {
                return;
            }

            _player = player.transform;
            HealthChanged?.Invoke(_actor.Health.Current, _actor.Health.Max);
            PhaseChanged?.Invoke(Phase);
            FightStarted?.Invoke();
            _routine = StartCoroutine(EncounterRoutine());
        }

        /// <summary>
        /// Puts the encounter back exactly as it was found: full health, phase one, shield back on
        /// its arm, nothing left running. Called when the player dies before winning.
        /// </summary>
        public void ResetBoss()
        {
            _actor.Statuses?.Clear();
            StopRoutine();
            ClearBattlefield();
            _player = null;
            Phase = 1;
            _lastAttackIndex = -1;
            for (var i = 0; i < _attackReadyAt.Length; i++)
            {
                _attackReadyAt[i] = 0f;
            }

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

            SetShieldVisible(true);
            SetState(BossState.Dormant);
            PhaseChanged?.Invoke(Phase);
        }

        private IEnumerator EncounterRoutine()
        {
            // A short wake-up: untouchable and harmless, so nobody is hit by a cutscene.
            SetState(BossState.Intro);
            _motor.Stop();
            _actor.SetInvulnerable(ImmunitySource.Script, _introDuration);
            if (_player != null)
            {
                _motor.FaceTowards(_player.position.x);
            }
            yield return new WaitForSeconds(_introDuration);

            yield return FightRoutine();
        }

        private IEnumerator FightRoutine()
        {
            SetState(BossState.Idle);
            while (_actor.IsAlive && _player != null)
            {
                if (Phase == 1 && _actor.Health.Normalized <= _phaseTwoHealthFraction)
                {
                    yield return EnterPhaseTwo();
                }

                SetState(BossState.Idle);
                _motor.Stop();
                _motor.FaceTowards(_player.position.x);
                yield return new WaitForSeconds(_idlePause * PauseScale);

                var distance = DistanceToPlayer;
                if (distance < _tooCloseRange)
                {
                    // Standing inside the boss is not a safe spot; it steps back and swings.
                    yield return BackOff();
                }

                var index = PickAttack();
                if (index < 0)
                {
                    yield return Reposition();
                    continue;
                }

                _attackReadyAt[index] = Time.time + _attacks[index].Cooldown * PauseScale;
                _lastAttackIndex = index;
                yield return RunAttack(_attacks[index]);
            }
        }

        private IEnumerator EnterPhaseTwo()
        {
            SetState(BossState.PhaseTransition);
            StopHitboxes();
            _motor.Stop();
            _actor.SetInvulnerable(ImmunitySource.Script, _phaseTransitionTime);
            _actor.Stagger?.ResetStagger();
            Phase = 2;
            SetShieldVisible(false);
            ShieldDropped?.Invoke();
            PhaseChanged?.Invoke(Phase);
            yield return new WaitForSeconds(_phaseTransitionTime);
        }

        private IEnumerator BackOff()
        {
            SetState(BossState.Reposition);
            var elapsed = 0f;
            var away = _player != null && _player.position.x > transform.position.x ? -1 : 1;
            while (elapsed < 0.4f)
            {
                _motor.Face(-away);
                if (!_motor.HasGroundAhead() || _motor.HasWallAhead())
                {
                    break;
                }

                // Face the player while stepping backwards.
                _motor.Drive(-_repositionSpeed * 0.6f * MoveScale);
                elapsed += Time.deltaTime;
                yield return null;
            }

            _motor.Stop();
        }

        private IEnumerator Reposition()
        {
            SetState(BossState.Reposition);
            var elapsed = 0f;
            while (elapsed < _repositionTimeout && _player != null)
            {
                var delta = _player.position.x - transform.position.x;
                if (Mathf.Abs(delta) <= _preferredRange)
                {
                    break;
                }

                _motor.FaceTowards(_player.position.x);
                if (!_motor.HasGroundAhead() || _motor.HasWallAhead())
                {
                    break;
                }

                _motor.Drive(_repositionSpeed * MoveScale);
                elapsed += Time.deltaTime;
                yield return null;
            }

            _motor.Stop();
        }

        private IEnumerator RunAttack(BossAttack attack)
        {
            SetState(BossState.Attacking);
            _motor.FaceTowards(_player.position.x);
            CurrentAttackKind = attack.Kind;
            AttackStarted?.Invoke(attack.Kind);
            for (var i = 0; i < attack.SwingCount; i++)
            {
                var swing = attack.Swing(i);
                if (swing == null)
                {
                    continue;
                }

                CurrentSwingIndex = i;
                CurrentSwing = swing;
                SwingPhase = BossSwingPhase.Windup;
                SwingWindup?.Invoke(i == 0 ? attack.Movement : BossAttackMovement.None, i);

                if (i == 0)
                {
                    yield return RunApproach(attack, swing);
                }
                else
                {
                    yield return TrackThenCommit(swing.Windup / AttackSpeed, attack.TrackingTime);
                }

                yield return RunActiveWindow(swing, i == 0 ? attack.Spawn : null);
                SwingPhase = BossSwingPhase.Recovery;
                yield return new WaitForSeconds(swing.Recovery / AttackSpeed);
                if (i + 1 < attack.SwingCount)
                {
                    yield return new WaitForSeconds(attack.SwingInterval * PauseScale);
                }
            }

            if (attack.EndsWithShockwave && _shockwave != null)
            {
                _shockwave.Play(gameObject, GroundPoint, _motor.FacingDirection);
            }

            CurrentAttackKind = null;
            CurrentSwing = null;
            CurrentSwingIndex = 0;
            _motor.Stop();
        }

        /// <summary>
        /// The windup: the boss turns with its target for the first slice of it, then commits. A
        /// dodge behind it after that point is rewarded instead of tracked.
        /// </summary>
        private IEnumerator TrackThenCommit(float windup, float trackingTime)
        {
            var tracking = Mathf.Min(trackingTime, windup);
            var elapsed = 0f;
            while (elapsed < tracking)
            {
                if (_player != null)
                {
                    _motor.FaceTowards(_player.position.x);
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            var remaining = windup - elapsed;
            if (remaining > 0f)
            {
                yield return new WaitForSeconds(remaining);
            }
        }

        /// <summary>Plays the windup, moving the boss when the attack is a charge or a leap.</summary>
        private IEnumerator RunApproach(BossAttack attack, AttackDefinition swing)
        {
            var windup = swing.Windup / AttackSpeed;
            switch (attack.Movement)
            {
                case BossAttackMovement.Charge:
                    // Telegraph first, then commit to a straight run: no homing.
                    yield return TrackThenCommit(windup * 0.55f, attack.TrackingTime);
                    SwingPhase = BossSwingPhase.Travel;
                    MovementBegan?.Invoke(BossAttackMovement.Charge);
                    var elapsed = 0f;
                    while (elapsed < windup * 0.45f)
                    {
                        if (!_motor.HasGroundAhead() || _motor.HasWallAhead())
                        {
                            // Hitting the arena wall ends the charge and leaves a longer opening.
                            break;
                        }

                        _motor.Drive(attack.MovementSpeed * MoveScale);
                        elapsed += Time.deltaTime;
                        yield return null;
                    }
                    _motor.Stop();
                    MovementEnded?.Invoke(BossAttackMovement.Charge);
                    break;
                case BossAttackMovement.Leap:
                    yield return TrackThenCommit(windup, attack.TrackingTime);
                    SwingPhase = BossSwingPhase.Travel;
                    MovementBegan?.Invoke(BossAttackMovement.Leap);
                    yield return Leap(attack.MovementSpeed);
                    MovementEnded?.Invoke(BossAttackMovement.Leap);
                    break;
                default:
                    _motor.Stop();
                    yield return TrackThenCommit(windup, attack.TrackingTime);
                    break;
            }
        }

        private IEnumerator Leap(float verticalSpeed)
        {
            var airTime = 2f * verticalSpeed / _motor.Gravity;
            // The landing spot is locked at take-off; dashing out of it is the counter.
            var horizontal = _player != null
                ? (_player.position.x - transform.position.x) / Mathf.Max(0.1f, airTime)
                : 0f;
            _motor.Face(horizontal >= 0f ? 1 : -1);
            _motor.Launch(new Vector2(horizontal, verticalSpeed));
            yield return new WaitForSeconds(Mathf.Min(0.15f, airTime * 0.4f));
            var timeout = airTime * 2f + 0.5f;
            var elapsed = 0f;
            while (!_motor.IsGrounded && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            _motor.Stop();
        }

        private IEnumerator RunActiveWindow(AttackDefinition swing, AttackSpawn spawn)
        {
            SwingPhase = BossSwingPhase.Active;
            var facing = _motor.FacingDirection;
            var damage = swing.BuildDamage(gameObject, this, facing);
            _hitbox.Begin(damage, swing.HitboxOffset, swing.HitboxSize, facing);

            // Emitted on the first swing only, so a combo does not fire one shot per hit.
            spawn?.Emit(damage, _actor.Faction, transform, facing, _player);
            yield return new WaitForSeconds(swing.ActiveTime / AttackSpeed);
            _hitbox.End();
        }

        /// <summary>
        /// Chooses by range, cooldown and phase, and avoids repeating the last move unless it is
        /// the only thing available.
        /// </summary>
        private int PickAttack()
        {
            if (_attacks == null || _player == null)
            {
                return -1;
            }

            var distance = DistanceToPlayer;
            var best = PickBest(distance, skipLast: true);
            return best >= 0 ? best : PickBest(distance, skipLast: false);
        }

        private int PickBest(float distance, bool skipLast)
        {
            var best = -1;
            var bestReadyAt = float.MaxValue;
            for (var i = 0; i < _attacks.Length; i++)
            {
                if (skipLast && i == _lastAttackIndex)
                {
                    continue;
                }

                if (!_attacks[i].IsUsable(Phase, distance) || Time.time < _attackReadyAt[i])
                {
                    continue;
                }

                // Prefer the move that has been available the longest, so the boss varies its
                // openers instead of repeating the first entry in the list.
                if (_attackReadyAt[i] < bestReadyAt)
                {
                    bestReadyAt = _attackReadyAt[i];
                    best = i;
                }
            }

            return best;
        }

        public float DistanceToPlayer => _player != null
            ? Mathf.Abs(_player.position.x - transform.position.x)
            : float.MaxValue;

        private float AttackSpeed => Phase >= 2 ? _phaseTwoAttackSpeed : 1f;
        private float PauseScale => Phase >= 2 ? _phaseTwoPauseScale : 1f;
        private float MoveScale => Phase >= 2 ? _phaseTwoMoveScale : 1f;
        private Vector2 GroundPoint => new Vector2(transform.position.x, transform.position.y - 0.9f);

        private void OnHealthChanged(float current, float max)
        {
            HealthChanged?.Invoke(current, max);
        }

        private void OnDamageTaken(DamageInfo info, DamageResult result)
        {
            // The boss does not flinch from ordinary hits; only poise or a parry interrupts it.
        }

        private void OnAttackParried(DamageInfo info)
        {
            if (State == BossState.Dead || State == BossState.Dormant
                || State == BossState.PhaseTransition)
            {
                return;
            }

            AttackParried?.Invoke(info);
            StopRoutine();
            _routine = StartCoroutine(RecoilRoutine());
        }

        private IEnumerator RecoilRoutine()
        {
            SetState(BossState.Recoil);
            _motor.Stop();
            yield return new WaitForSeconds(_parryRecoilTime);
            _routine = StartCoroutine(FightRoutine());
        }

        private void OnStaggered(float duration)
        {
            if (State == BossState.Dead || State == BossState.Dormant
                || State == BossState.PhaseTransition)
            {
                return;
            }

            Staggered?.Invoke(duration);
            StopRoutine();
            _routine = StartCoroutine(StunRoutine(duration));
        }

        private IEnumerator StunRoutine(float duration)
        {
            SetState(BossState.Stunned);
            _motor.Stop();
            yield return new WaitForSeconds(duration);
            // A breather before poise can be broken again, so a stun cannot be chained forever.
            _actor.Stagger?.SuppressFor(_staggerImmunityAfterStun);
            _routine = StartCoroutine(FightRoutine());
        }

        /// <summary>
        /// Sweeps up what the fight left lying around. A root shot in flight and a poison field
        /// still burning must not outlive the fight that made them — on a reset they would greet
        /// the next attempt, and on a death they would keep hurting the winner.
        /// </summary>
        private void ClearBattlefield()
        {
            SpellProjectile.ClearAll(_actor.Faction);
            HazardZone.ClearSpawned(_actor.Faction);
        }

        private void OnKilled(DamageInfo info)
        {
            StopRoutine();
            ClearBattlefield();
            _motor.Stop();
            _motor.enabled = false;
            if (_bodyCollider != null)
            {
                _bodyCollider.enabled = false;
            }
            SetState(BossState.Dead);
            Defeated?.Invoke(this);
        }

        private void StopRoutine()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            StopHitboxes();
        }

        private void StopHitboxes()
        {
            _hitbox?.End();
            _shockwave?.Stop();
        }

        private void SetShieldVisible(bool isVisible)
        {
            if (_shieldVisual != null)
            {
                _shieldVisual.SetActive(isVisible);
            }
        }

        private void SetState(BossState next)
        {
            if (State == next)
            {
                return;
            }

            State = next;
            if (next != BossState.Attacking)
            {
                // A stun, a parry recoil, the phase change or death can stop the routine mid-swing.
                // Clearing here rather than only at the end of the attack means the readout can
                // never claim a swing is still running after it was taken away.
                CurrentAttackKind = null;
                CurrentSwing = null;
                CurrentSwingIndex = 0;
                SwingPhase = BossSwingPhase.None;
            }

            StateChanged?.Invoke(next);
        }
    }
}
