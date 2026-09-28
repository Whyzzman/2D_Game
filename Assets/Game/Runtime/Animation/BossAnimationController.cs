using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The one place that decides what the Guardian looks like. The fight keeps running exactly as
    /// it did; this reads it. Nothing here starts an attack, changes a phase, moves the boss or
    /// grants invulnerability, and the boss controller never names a clip.
    ///
    /// Two requirements shape the whole class. The phase change has to read with no interface at
    /// all, so phase one and phase two are separate poses rather than a tint. And the charge and
    /// the leap have to be readable before they land, so their telegraph, travel, impact and
    /// recovery are four distinct clips driven by the routine's own
    /// <see cref="BossSwingPhase"/> — not by guesswork about elapsed time.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BossAnimationController : MonoBehaviour, ITrailAnchorSource
    {
        [SerializeField] private BossAnimationSet _set;
        [SerializeField] private SpriteRenderer _body;
        [Tooltip("Hidden while a real animation set is present, since the frames draw the face.")]
        [SerializeField] private SpriteRenderer _face;
        [SerializeField] private BossController _boss;
        [SerializeField] private EnemyMotor2D _motor;

        private BossAnimationState _state = BossAnimationState.Dormant;
        private AnimationClipDefinition _clip;
        private float _elapsed;
        private float _speed = 1f;
        private bool _completed;
        private int _lastFrame = -1;
        private int _transitionsSeen;

        public event Action<BossAnimationState, BossAnimationState> StateChanged;
        public event Action<BossAnimationState> ClipCompleted;
        public event Action<BossAnimationState, int> FrameChanged;

        /// <summary>
        /// The visual half of the phase change, for VFX and audio. Separate from the boss's own
        /// <c>PhaseChanged</c> because that one fires on reset too, when nothing should be shown.
        /// </summary>
        public event Action PhaseTransitionStarted;

        public BossAnimationState State => _state;
        public AnimationClipDefinition Clip => _clip;
        public float PlaybackSpeed => _speed;

        /// <summary>How many times the transition has played; it must never play twice a fight.</summary>
        public int TransitionsPlayed => _transitionsSeen;

        public float NormalizedTime => _clip != null && _clip.Duration > 0f
            ? Mathf.Clamp01(_elapsed / _clip.Duration)
            : 0f;

        public BossAnimationState? ForcedState { get; set; }

        public SpriteRenderer Body => _body;

        /// <summary>Where the frame now shown hangs its loose piece (see <see cref="ITrailAnchorSource"/>).</summary>
        public bool TryGetTrailAnchor(out Vector3 anchor)
        {
            anchor = default;
            return _clip != null && _lastFrame >= 0 && _clip.TryGetAnchor(_lastFrame, out anchor);
        }

        private void Awake()
        {
            if (_boss == null) { _boss = GetComponent<BossController>(); }
            if (_motor == null) { _motor = GetComponent<EnemyMotor2D>(); }
            if (_set == null)
            {
                Debug.LogWarning($"{name} has no boss animation set; the placeholder body stays as "
                    + "it is and the fight is unaffected.", this);
                return;
            }

            if (_body != null)
            {
                _body.transform.localScale = Vector3.one;
            }

            if (_face != null)
            {
                _face.enabled = false;
            }

            // The boss sleeps for a long time before its first state change, so the starting clip
            // is entered explicitly instead of waiting for one.
            Enter(Resolve());
            Draw();
        }

        private void OnEnable()
        {
            if (_boss != null)
            {
                _boss.PhaseChanged += OnPhaseChanged;
            }
        }

        private void OnDisable()
        {
            if (_boss != null)
            {
                _boss.PhaseChanged -= OnPhaseChanged;
            }
        }

        /// <summary>
        /// A reset also raises this, back to phase one. Only a move forward is a transition, so a
        /// player dying in phase two does not leave a transition counted or half-played.
        /// </summary>
        private void OnPhaseChanged(int phase)
        {
            if (phase <= 1)
            {
                _transitionsSeen = 0;
                Enter(BossAnimationState.Dormant);
                Draw();
            }
        }

        private void LateUpdate() => Tick(Time.deltaTime);

        public void Tick(float deltaTime)
        {
            var next = ForcedState ?? Resolve();
            if (next != _state)
            {
                if (next == BossAnimationState.PhaseTransition)
                {
                    _transitionsSeen++;
                    PhaseTransitionStarted?.Invoke();
                }

                Enter(next);
            }

            _elapsed += Mathf.Max(0f, deltaTime) * _speed;
            Draw();
        }

        private void Enter(BossAnimationState next)
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
        /// Fits a clip to the part of the swing it covers. The slam's impact clip is stretched to
        /// the real active window, so the visible impact frame and the damage cannot drift apart.
        /// </summary>
        private float GameplayDurationOf(BossAnimationState state)
        {
            var swing = _boss != null ? _boss.CurrentSwing : null;
            if (swing == null)
            {
                return 0f;
            }

            switch (state)
            {
                case BossAnimationState.ChargeStart:
                case BossAnimationState.JumpStart:
                    return swing.Windup;
                case BossAnimationState.Slam:
                    return swing.ActiveTime;
                case BossAnimationState.SlamRecover:
                case BossAnimationState.ChargeRecover:
                    return swing.Recovery;
                case BossAnimationState.SlashP1:
                case BossAnimationState.SlashP2:
                case BossAnimationState.ShieldBash:
                case BossAnimationState.Combo1:
                case BossAnimationState.Combo2:
                case BossAnimationState.Combo3:
                case BossAnimationState.ComboP2:
                    return swing.Duration;
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
        /// Death, then the phase change, then the stun, then attacks, then movement. The phase
        /// change sits second because it interrupts everything below it and must never be masked.
        /// </summary>
        public BossAnimationState Resolve()
        {
            if (_boss == null)
            {
                return BossAnimationState.Dormant;
            }

            var isPhaseTwo = _boss.Phase >= 2;
            switch (_boss.State)
            {
                case BossState.Dead:
                    return BossAnimationState.Death;
                case BossState.PhaseTransition:
                    return BossAnimationState.PhaseTransition;
                case BossState.Stunned:
                    return BossAnimationState.Stunned;
                case BossState.Recoil:
                    return BossAnimationState.Recoil;
                case BossState.Attacking:
                    return AttackState(isPhaseTwo);
                case BossState.Reposition:
                    return isPhaseTwo ? BossAnimationState.MoveP2 : BossAnimationState.WalkP1;
                case BossState.Intro:
                    return BossAnimationState.Intro;
                case BossState.Dormant:
                    return BossAnimationState.Dormant;
                default:
                    return isPhaseTwo ? BossAnimationState.IdleP2 : BossAnimationState.IdleP1;
            }
        }

        private BossAnimationState AttackState(bool isPhaseTwo)
        {
            var phase = _boss.SwingPhase;
            switch (_boss.CurrentAttackKind)
            {
                case BossAttackKind.ShieldBash:
                    return BossAnimationState.ShieldBash;

                case BossAttackKind.Charge:
                    // Four beats, because the charge is the move the player most has to read.
                    switch (phase)
                    {
                        case BossSwingPhase.Travel: return BossAnimationState.Charge;
                        case BossSwingPhase.Active: return BossAnimationState.Charge;
                        case BossSwingPhase.Recovery: return BossAnimationState.ChargeRecover;
                        default: return BossAnimationState.ChargeStart;
                    }

                case BossAttackKind.JumpSlam:
                    switch (phase)
                    {
                        case BossSwingPhase.Travel: return BossAnimationState.Jump;
                        case BossSwingPhase.Active: return BossAnimationState.Slam;
                        case BossSwingPhase.Recovery: return BossAnimationState.SlamRecover;
                        default: return BossAnimationState.JumpStart;
                    }

                // The Warden's polearm reuses the charge and leap beats where the movement is the
                // same, and has its own state where the shape of the move is different. A sweep
                // that looked like a slash would be a lie about where the danger is.
                case BossAttackKind.LongThrust:
                    switch (phase)
                    {
                        case BossSwingPhase.Travel:
                        case BossSwingPhase.Active: return BossAnimationState.Charge;
                        case BossSwingPhase.Recovery: return BossAnimationState.ChargeRecover;
                        default: return BossAnimationState.ChargeStart;
                    }

                case BossAttackKind.LeapStrike:
                    switch (phase)
                    {
                        case BossSwingPhase.Travel: return BossAnimationState.Jump;
                        case BossSwingPhase.Active: return BossAnimationState.Slam;
                        case BossSwingPhase.Recovery: return BossAnimationState.SlamRecover;
                        default: return BossAnimationState.JumpStart;
                    }

                case BossAttackKind.Sweep:
                    return BossAnimationState.Sweep;

                case BossAttackKind.RootShot:
                    return BossAnimationState.RootShot;

                case BossAttackKind.RootSlam:
                    return BossAnimationState.RootSlam;

                case BossAttackKind.PoisonField:
                    return BossAnimationState.PoisonFieldCast;

                case BossAttackKind.GlaiveSlash:
                    return isPhaseTwo ? BossAnimationState.SlashP2 : BossAnimationState.SlashP1;

                case BossAttackKind.TripleCombo:
                    if (isPhaseTwo)
                    {
                        return BossAnimationState.ComboP2;
                    }

                    // Three visibly different swings, so the sequence can be counted.
                    switch (_boss.CurrentSwingIndex)
                    {
                        case 1: return BossAnimationState.Combo2;
                        case 2: return BossAnimationState.Combo3;
                        default: return BossAnimationState.Combo1;
                    }

                default:
                    return isPhaseTwo ? BossAnimationState.SlashP2 : BossAnimationState.SlashP1;
            }
        }
    }
}
