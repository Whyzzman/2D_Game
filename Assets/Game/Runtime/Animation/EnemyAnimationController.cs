using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The one place that decides what an ordinary enemy looks like. The AI keeps making decisions
    /// and this reads the result: nothing here starts an attack, moves an enemy or ends a state,
    /// and no AI script names a clip.
    ///
    /// The priority order is the whole design, and it matters most for the shield carrier: a guard
    /// break that kept playing the guard pose would hide the punish window the fight is built
    /// around. Resolution walks from the most important state down and takes the first that
    /// applies.
    ///
    /// Attack clips are fitted to the real swing from <see cref="AttackDefinition"/>, so retuning
    /// an enemy can never desynchronise the telegraph from the active window.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyAnimationController : MonoBehaviour, ITrailAnchorSource
    {
        [SerializeField] private EnemyAnimationSet _set;
        [Tooltip("The renderer showing the body; frames are written here.")]
        [SerializeField] private SpriteRenderer _body;
        [Tooltip("Hidden while a real animation set is present, since the frames draw the face.")]
        [SerializeField] private SpriteRenderer _face;
        [SerializeField] private EnemyController _enemy;
        [SerializeField] private EnemyCombat _combat;
        [SerializeField] private EnemyGuard _guard;
        [Tooltip("How long the guard-raise and guard-impact flourishes hold.")]
        [SerializeField, Min(0f)] private float _transitionHold = 0.18f;

        private EnemyAnimationState _state = EnemyAnimationState.Idle;
        private AnimationClipDefinition _clip;
        private float _elapsed;
        private float _speed = 1f;
        private bool _completed;
        private EnemyAnimationState _oneShot;
        private float _oneShotUntil = float.NegativeInfinity;
        private bool _wasGuarding;
        private int _lastFrame = -1;

        public event Action<EnemyAnimationState, EnemyAnimationState> StateChanged;
        public event Action<EnemyAnimationState> ClipCompleted;
        public event Action<EnemyAnimationState, int> FrameChanged;

        public EnemyAnimationState State => _state;
        public AnimationClipDefinition Clip => _clip;
        public float PlaybackSpeed => _speed;

        public float NormalizedTime => _clip != null && _clip.Duration > 0f
            ? Mathf.Clamp01(_elapsed / _clip.Duration)
            : 0f;

        /// <summary>Set by the debug tools to pin a state; never used by the game itself.</summary>
        public EnemyAnimationState? ForcedState { get; set; }

        public SpriteRenderer Body => _body;

        /// <summary>Where the frame now shown hangs its loose piece (see <see cref="ITrailAnchorSource"/>).</summary>
        public bool TryGetTrailAnchor(out Vector3 anchor)
        {
            anchor = default;
            return _clip != null && _lastFrame >= 0 && _clip.TryGetAnchor(_lastFrame, out anchor);
        }

        private void Awake()
        {
            if (_enemy == null) { _enemy = GetComponent<EnemyController>(); }
            if (_combat == null) { _combat = GetComponent<EnemyCombat>(); }
            if (_guard == null) { _guard = GetComponent<EnemyGuard>(); }
            if (_set == null)
            {
                Debug.LogWarning($"{name} has no enemy animation set; the placeholder body stays "
                    + "as it is and gameplay is unaffected.", this);
                return;
            }

            // The placeholder body was a unit square stretched to the capsule and centred on it.
            // Real frames carry their own size through pixels-per-unit and are pivoted at the feet.
            if (_body != null)
            {
                _body.transform.localScale = Vector3.one;
            }

            if (_face != null)
            {
                _face.enabled = false;
            }

            // Entering the starting state rather than waiting for a change: an enemy that spawns
            // idle and stays idle would otherwise hold no clip at all and show nothing.
            Enter(Resolve());
            Draw();
        }

        private void OnEnable()
        {
            if (_enemy != null)
            {
                _enemy.GuardImpacted += OnGuardImpacted;
                _enemy.EnemyReset += OnEnemyReset;
            }
        }

        private void OnDisable()
        {
            if (_enemy != null)
            {
                _enemy.GuardImpacted -= OnGuardImpacted;
                _enemy.EnemyReset -= OnEnemyReset;
            }
        }

        private void OnGuardImpacted(DamageInfo info) => Flash(EnemyAnimationState.GuardImpact);

        /// <summary>
        /// A reset puts the enemy back as it was found, so the visual has to come back too: no
        /// held death frame, no leftover guard-break pose, nothing latched.
        /// </summary>
        private void OnEnemyReset(EnemyController enemy)
        {
            _oneShotUntil = float.NegativeInfinity;
            _wasGuarding = false;
            Enter(EnemyAnimationState.Idle);
            Draw();
        }

        private void Flash(EnemyAnimationState state)
        {
            var clip = _set != null ? _set.Clip(state) : null;
            var hold = clip != null && clip.Duration > 0f ? clip.Duration : _transitionHold;
            _oneShot = state;
            _oneShotUntil = Time.time + hold;
        }

        private bool Held(EnemyAnimationState state) =>
            _oneShotUntil > Time.time && _oneShot == state;

        private void LateUpdate() => Tick(Time.deltaTime);

        /// <summary>Advances the animation; separate from LateUpdate so tests can step it.</summary>
        public void Tick(float deltaTime)
        {
            var next = ForcedState ?? Resolve();
            if (next != _state)
            {
                Enter(next);
            }

            _elapsed += Mathf.Max(0f, deltaTime) * _speed;
            Draw();
        }

        private void Enter(EnemyAnimationState next)
        {
            var previous = _state;
            _state = next;
            _elapsed = 0f;
            _completed = false;
            _lastFrame = -1;
            _clip = _set != null ? _set.Clip(next) : null;
            var gameplay = GameplayDurationOf(next);
            _speed = _clip != null && _clip.Duration > 0f && gameplay > 0f
                ? _clip.Duration / gameplay
                : 1f;
            StateChanged?.Invoke(previous, next);
        }

        /// <summary>
        /// How long the real action lasts, when it is known. Zero means "play at the authored
        /// rate". This is what keeps the telegraph honest: the zombie's 0.42 s windup is the
        /// longest read in the game, and the clip is stretched to it rather than the reverse.
        /// </summary>
        private float GameplayDurationOf(EnemyAnimationState state)
        {
            switch (state)
            {
                case EnemyAnimationState.AttackLight:
                case EnemyAnimationState.AttackHeavy:
                case EnemyAnimationState.ShieldBash:
                    var attack = _combat != null ? _combat.CurrentAttack : null;
                    return attack != null ? attack.Duration : 0f;
                default:
                    return 0f;
            }
        }

        private void Draw()
        {
            if (_body == null || _clip == null || !_clip.HasFrames)
            {
                return;
            }

            var frame = _clip.FrameAt(_elapsed);
            if (frame != null)
            {
                _body.sprite = frame;
            }

            var index = _clip.FrameIndexAt(_elapsed);
            if (index != _lastFrame)
            {
                _lastFrame = index;
                FrameChanged?.Invoke(_state, index);
            }

            if (!_clip.IsLooping && !_completed && _elapsed >= _clip.Duration)
            {
                _completed = true;
                ClipCompleted?.Invoke(_state);
            }
        }

        /// <summary>
        /// The priority chain: Dead, then the helpless states, then hurt, then attacking, then
        /// guarding, then movement. Read it top to bottom — the first branch that applies wins.
        /// </summary>
        public EnemyAnimationState Resolve()
        {
            if (_enemy == null)
            {
                return EnemyAnimationState.Idle;
            }

            switch (_enemy.State)
            {
                case EnemyState.Dead:
                    return EnemyAnimationState.Death;
                case EnemyState.GuardBroken:
                    return EnemyAnimationState.GuardBreak;
                case EnemyState.Stunned:
                    return EnemyAnimationState.Stunned;
                case EnemyState.Hurt:
                    return EnemyAnimationState.Hurt;
                case EnemyState.Attack:
                    return AttackState();
                case EnemyState.GuardImpact:
                    return EnemyAnimationState.GuardImpact;
                case EnemyState.Guard:
                    return GuardState();
                case EnemyState.Alert:
                    return EnemyAnimationState.Alert;
                case EnemyState.Chase:
                case EnemyState.Search:
                    return EnemyAnimationState.Chase;
                case EnemyState.Patrol:
                case EnemyState.ReturnToPatrol:
                    return EnemyAnimationState.Patrol;
                default:
                    return EnemyAnimationState.Idle;
            }
        }

        /// <summary>
        /// Raising the shield is its own readable beat before holding it. A blocked blow recoils
        /// without leaving the guard, exactly as the gameplay does.
        /// </summary>
        private EnemyAnimationState GuardState()
        {
            if (Held(EnemyAnimationState.GuardImpact))
            {
                return EnemyAnimationState.GuardImpact;
            }

            var isGuarding = _guard == null || _guard.IsRaised;
            if (isGuarding && !_wasGuarding)
            {
                Flash(EnemyAnimationState.GuardRaise);
            }

            _wasGuarding = isGuarding;
            return Held(EnemyAnimationState.GuardRaise)
                ? EnemyAnimationState.GuardRaise
                : EnemyAnimationState.GuardIdle;
        }

        /// <summary>
        /// Which swing this is. The AI already chose a move; the animation follows that choice
        /// rather than guessing from the attack's numbers, so a bash never plays as a heavy.
        /// </summary>
        private EnemyAnimationState AttackState()
        {
            switch (_enemy.CurrentAttackKind)
            {
                case EnemyAttackKind.Heavy: return EnemyAnimationState.AttackHeavy;
                case EnemyAttackKind.ShieldBash: return EnemyAnimationState.ShieldBash;
                default: return EnemyAnimationState.AttackLight;
            }
        }
    }
}
