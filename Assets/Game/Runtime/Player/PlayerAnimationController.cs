using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The one place that decides what the player looks like. Every other system keeps doing its
    /// job and this reads the result: nothing here starts an attack, moves the character or ends a
    /// state, and no gameplay script names a clip.
    ///
    /// The order below is the whole design. A state that matters more must not be hidden by one
    /// that matters less — dying must never look like running — so resolution walks from the most
    /// important state down and takes the first that applies.
    ///
    /// Timings stay where they already are. An attack's clip is stretched to the attack's real
    /// duration rather than the clip deciding how long the swing lasts, so retuning a weapon never
    /// desynchronises the art and the art can never lie about the active window.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerAnimationController : MonoBehaviour, ITrailAnchorSource
    {
        /// <summary>Faster than this sideways at touchdown counts as landing on the run.</summary>
        private const float LandSkipSpeed = 0.5f;
        /// <summary>The slowest the run clip is played, as a share of its authored pace.</summary>
        private const float MinRunRate = 0.35f;
        /// <summary>Standing still this long before the first idle variant plays.</summary>
        public const float IdleVariantDelay = 6f;
        /// <summary>After one variant, the next waits somewhere in this range (the user's choice).</summary>
        public const float IdleVariantGapMin = 8f;
        public const float IdleVariantGapMax = 12f;
        [SerializeField] private PlayerAnimationSet _set;
        [Tooltip("The renderer showing the character; frames are written here.")]
        [SerializeField] private SpriteRenderer _body;
        [Tooltip("Hidden while a real animation set is present, since the frames draw the face.")]
        [SerializeField] private SpriteRenderer _face;
        [SerializeField] private PlayerCharacter _character;
        [SerializeField] private PlayerMotor2D _motor;
        [SerializeField] private PlayerCombat _combat;
        [SerializeField] private PlayerDefense _defense;
        [SerializeField] private PlayerEquipment _equipment;
        [SerializeField] private WallMovement _wall;
        [SerializeField] private ShadowAbilities _shadow;
        [SerializeField] private SpellCaster _caster;
        [SerializeField] private ActiveSkillController _skills;
        [Tooltip("How long the landing and jump-launch flourishes hold before locomotion resumes.")]
        [SerializeField, Min(0f)] private float _transitionHold = 0.12f;

        private PlayerAnimationState _state = PlayerAnimationState.Idle;
        private AnimationClipDefinition _clip;
        private WeaponAnimationSet _weaponSet;
        private float _elapsed;
        private float _speed = 1f;
        private float _stateDuration;
        private bool _completed;
        private PlayerAnimationState _oneShot;
        private float _oneShotUntil = float.NegativeInfinity;
        private bool _wasGrounded = true;
        private int _lastFrame = -1;
        private int _lastFacing;
        private float _calm;
        private float _nextVariant = IdleVariantDelay;
        private bool _variantPlaying;

        /// <summary>Raised on every real change, for VFX and audio that follow the visual.</summary>
        public event Action<PlayerAnimationState, PlayerAnimationState> StateChanged;

        /// <summary>Raised once when a non-looping clip reaches its final frame.</summary>
        public event Action<PlayerAnimationState> ClipCompleted;

        /// <summary>Raised as each new frame is shown; footstep and trail effects hang off this.</summary>
        public event Action<PlayerAnimationState, int> FrameChanged;

        /// <summary>
        /// The feet touched down. Raised for every landing, including the running ones that go
        /// straight into the run without the Land pose, so the landing is always heard.
        /// </summary>
        public event Action Landed;

        public PlayerAnimationState State => _state;
        public AnimationClipDefinition Clip => _clip;
        public WeaponAnimationSet ActiveWeaponSet => _weaponSet;

        /// <summary>The renderer the frames are shown on; cloth that hangs off the body follows it.</summary>
        public SpriteRenderer Body => _body;

        /// <summary>
        /// Where the cloak's loose edge hangs from on the frame now shown, from the clip's per-frame
        /// anchors (x, y in units from the body's pivot, facing right; z the hang angle). False
        /// when the clip carries none, and the cloth then keeps its own resting point.
        /// </summary>
        public bool TryGetTrailAnchor(out Vector3 anchor)
        {
            anchor = default;
            return _clip != null && _lastFrame >= 0 && _clip.TryGetAnchor(_lastFrame, out anchor);
        }

        /// <summary>How far through the current clip playback is, in [0, 1].</summary>
        public float NormalizedTime => _clip != null && _clip.Duration > 0f
            ? Mathf.Clamp01(_elapsed / _clip.Duration)
            : 0f;

        /// <summary>
        /// How much the clip is being stretched or squeezed to match gameplay. Far from 1 means the
        /// authored length disagrees with the real action; the debug tools surface it so an
        /// animator can see it rather than guess.
        /// </summary>
        public float PlaybackSpeed => _speed * LocomotionRate();

        /// <summary>Set by the debug tools to pin a state; ignored in a normal build path.</summary>
        public PlayerAnimationState? ForcedState { get; set; }

        private void Awake()
        {
            if (_character == null) { _character = GetComponent<PlayerCharacter>(); }
            if (_motor == null) { _motor = GetComponent<PlayerMotor2D>(); }
            if (_combat == null) { _combat = GetComponent<PlayerCombat>(); }
            if (_defense == null) { _defense = GetComponent<PlayerDefense>(); }
            if (_equipment == null) { _equipment = GetComponent<PlayerEquipment>(); }
            if (_wall == null) { _wall = GetComponent<WallMovement>(); }
            if (_shadow == null) { _shadow = GetComponent<ShadowAbilities>(); }
            if (_caster == null) { _caster = GetComponent<SpellCaster>(); }
            if (_skills == null) { _skills = GetComponent<ActiveSkillController>(); }
            if (_set == null)
            {
                // Said once, at startup, where it is actionable — not on a state change, which may
                // never come and would bury it in the middle of play.
                Debug.LogWarning("Player has no animation set; the placeholder body stays as it is "
                    + "and gameplay is unaffected.", this);
            }

            AdoptBodyTransform();
            RefreshWeaponSet();
            // Entering the starting state rather than waiting for a change: standing still from
            // the first frame would otherwise hold no clip at all.
            Enter(Resolve());
            Draw();
        }

        /// <summary>
        /// The placeholder body is a unit square stretched to the capsule. Real frames carry their
        /// own size through pixels-per-unit, so stretching them again would distort them: once a
        /// set is present the renderer goes back to unit scale and the sprite decides.
        /// </summary>
        private void AdoptBodyTransform()
        {
            if (_set == null || _body == null)
            {
                return;
            }

            _body.transform.localScale = Vector3.one;
            if (_face != null)
            {
                _face.enabled = false;
            }
        }

        private void OnEnable()
        {
            if (_equipment != null) { _equipment.ActiveWeaponChanged += OnWeaponChanged; }
            if (_defense != null) { _defense.BlockedHit += OnBlockedHit; }
            if (_caster != null) { _caster.SpellReleased += OnSpellReleased; }
            if (_shadow != null)
            {
                _shadow.Started += OnShadowStarted;
                _shadow.Ended += OnShadowEnded;
            }
        }

        private void OnDisable()
        {
            if (_equipment != null) { _equipment.ActiveWeaponChanged -= OnWeaponChanged; }
            if (_defense != null) { _defense.BlockedHit -= OnBlockedHit; }
            if (_caster != null) { _caster.SpellReleased -= OnSpellReleased; }
            if (_shadow != null)
            {
                _shadow.Started -= OnShadowStarted;
                _shadow.Ended -= OnShadowEnded;
            }
        }

        private void OnWeaponChanged(EquippedWeapon weapon) => RefreshWeaponSet();

        private void OnBlockedHit(DamageInfo info) => Flash(PlayerAnimationState.BlockImpact);

        private void OnSpellReleased(SpellDefinition spell) => Flash(PlayerAnimationState.CastRelease);

        private void OnShadowStarted(ShadowAction action)
        {
            if (action == ShadowAction.Dash)
            {
                Flash(PlayerAnimationState.ShadowDashStart);
            }
        }

        private void OnShadowEnded() => Flash(PlayerAnimationState.ShadowDashEnd);

        /// <summary>
        /// Holds a brief state that has no gameplay state of its own — a landing, a blocked hit,
        /// the moment a spell leaves the hand. It expires on its own, so a missed event can never
        /// leave the character stuck.
        /// </summary>
        private void Flash(PlayerAnimationState state)
        {
            var clip = _set != null ? _set.Clip(state, _weaponSet) : null;
            var hold = clip != null && clip.Duration > 0f ? clip.Duration : _transitionHold;
            _oneShot = state;
            _oneShotUntil = Time.time + hold;
        }

        private bool Held(PlayerAnimationState state) =>
            _oneShotUntil > Time.time && _oneShot == state;

        private void RefreshWeaponSet()
        {
            var weapon = _combat != null ? _combat.Weapon : null;
            _weaponSet = _set != null && weapon != null ? _set.WeaponSet(weapon.WeaponType) : null;
        }

        private void LateUpdate() => Tick(Time.deltaTime);

        /// <summary>
        /// Advances the animation. Separate from <c>LateUpdate</c> so tests can step it by an exact
        /// amount rather than waiting on real frames.
        /// </summary>
        public void Tick(float deltaTime)
        {
            TrackLanding();
            TrackTurn();
            var next = ForcedState ?? Resolve();
            if (next != _state)
            {
                Enter(next);
            }

            _elapsed += Mathf.Max(0f, deltaTime) * _speed * LocomotionRate();
            Draw();
            TrackCalm(Mathf.Max(0f, deltaTime));
        }

        /// <summary>
        /// The facing flipped on the ground: two frames of the turn. The motor has already turned
        /// the body and control answers the same frame — the pose only dresses the moment, so it
        /// is never a reason for the turn to feel slower than Hollow Knight's.
        /// </summary>
        private void TrackTurn()
        {
            var facing = _motor != null ? _motor.FacingDirection : 1;
            var grounded = _motor == null || _motor.IsGrounded;
            if (_lastFacing != 0 && facing != _lastFacing && grounded && IsStanding(_state))
            {
                Flash(PlayerAnimationState.Turn);
            }

            _lastFacing = facing;
        }

        private static bool IsStanding(PlayerAnimationState state) =>
            state == PlayerAnimationState.Idle || state == PlayerAnimationState.IdleVariant
            || state == PlayerAnimationState.Run || state == PlayerAnimationState.Turn
            || state == PlayerAnimationState.Land;

        /// <summary>
        /// Counts how long the hero has stood still, and now and then lets the idle variant play
        /// once (the user's choice, 2026-09-27: after about six seconds, then every eight to
        /// twelve). Anything but standing ends it at once and starts the count over.
        /// </summary>
        private void TrackCalm(float deltaTime)
        {
            if (_state == PlayerAnimationState.IdleVariant)
            {
                if (_completed)
                {
                    _variantPlaying = false;
                }

                return;
            }

            if (_state != PlayerAnimationState.Idle)
            {
                _variantPlaying = false;
                _calm = 0f;
                _nextVariant = IdleVariantDelay;
                return;
            }

            _calm += deltaTime;
            if (_calm >= _nextVariant && _set != null && _set.Has(PlayerAnimationState.IdleVariant))
            {
                _variantPlaying = true;
                _calm = 0f;
                _nextVariant = UnityEngine.Random.Range(IdleVariantGapMin, IdleVariantGapMax);
            }
        }

        private void Enter(PlayerAnimationState next)
        {
            var previous = _state;
            _state = next;
            _elapsed = 0f;
            _completed = false;
            _lastFrame = -1;
            _clip = _set != null ? _set.Clip(next, _weaponSet) : null;
            _stateDuration = GameplayDurationOf(next);
            _speed = _clip != null && _clip.Duration > 0f && _stateDuration > 0f
                ? _clip.Duration / _stateDuration
                : 1f;
            StateChanged?.Invoke(previous, next);
        }

        /// <summary>
        /// How long the gameplay action really lasts, when it is known. This is what keeps the
        /// data authoritative: the clip is fitted to the action, never the other way round.
        /// Zero means "play at the authored rate".
        /// </summary>
        private float GameplayDurationOf(PlayerAnimationState state)
        {
            switch (state)
            {
                case PlayerAnimationState.Attack1:
                case PlayerAnimationState.Attack2:
                case PlayerAnimationState.Attack3:
                case PlayerAnimationState.AttackUp:
                case PlayerAnimationState.AttackDown:
                case PlayerAnimationState.Heavy:
                    var attack = _combat != null ? _combat.CurrentAttack : null;
                    return attack != null ? attack.Duration : 0f;
                case PlayerAnimationState.Heal:
                    return _character != null ? _character.Flasks.DrinkTime : 0f;
                case PlayerAnimationState.CastStart:
                    return _caster != null && _caster.CastingSpell != null
                        ? _caster.CastTimeOf(_caster.CastingSpell)
                        : 0f;
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

            var index = Mathf.FloorToInt(_elapsed * _clip.FramesPerSecond);
            index = _clip.IsLooping
                ? index % _clip.FrameCount
                : Mathf.Min(index, _clip.FrameCount - 1);
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
        /// The priority chain. Read it top to bottom: the first branch that applies wins, so a more
        /// important state can never be masked by a less important one further down.
        /// </summary>
        public PlayerAnimationState Resolve()
        {
            var action = _character != null ? _character.Action : PlayerAction.None;

            // Death outranks everything, including anything that was mid-swing.
            if (action == PlayerAction.Dead)
            {
                return PlayerAnimationState.Death;
            }

            if (action == PlayerAction.Stunned) { return PlayerAnimationState.Stunned; }
            if (action == PlayerAction.GuardBroken) { return PlayerAnimationState.GuardBroken; }
            if (action == PlayerAction.Hurt) { return PlayerAnimationState.Hurt; }

            // Shadow movement replaces the body entirely, so it sits above ordinary skills.
            if (_shadow != null && _shadow.Action != ShadowAction.None)
            {
                if (_shadow.Action == ShadowAction.Strike)
                {
                    return PlayerAnimationState.ShadowStrike;
                }

                return Held(PlayerAnimationState.ShadowDashStart)
                    ? PlayerAnimationState.ShadowDashStart
                    : PlayerAnimationState.ShadowDashLoop;
            }

            if (Held(PlayerAnimationState.ShadowDashEnd))
            {
                return PlayerAnimationState.ShadowDashEnd;
            }

            if (action == PlayerAction.ActiveSkill)
            {
                return SkillState();
            }

            if (action == PlayerAction.LightAttack || action == PlayerAction.HeavyAttack)
            {
                return AttackState(action);
            }

            if (action == PlayerAction.Casting)
            {
                return Held(PlayerAnimationState.CastRelease)
                    ? PlayerAnimationState.CastRelease
                    : PlayerAnimationState.CastStart;
            }

            if (Held(PlayerAnimationState.CastRelease))
            {
                return PlayerAnimationState.CastRelease;
            }

            if (action == PlayerAction.Heal) { return PlayerAnimationState.Heal; }

            if (action == PlayerAction.Parry) { return PlayerAnimationState.Parry; }
            if (action == PlayerAction.Block)
            {
                return Held(PlayerAnimationState.BlockImpact)
                    ? PlayerAnimationState.BlockImpact
                    : PlayerAnimationState.Block;
            }

            if (action == PlayerAction.WeaponSwitch) { return PlayerAnimationState.WeaponSwitch; }

            var grounded = _motor == null || _motor.IsGrounded;
            if (action == PlayerAction.Dash || (_motor != null && _motor.IsDashing))
            {
                return grounded ? PlayerAnimationState.Dash : PlayerAnimationState.AirDash;
            }

            if (_wall != null)
            {
                switch (_wall.State)
                {
                    case WallMovementState.Slide: return PlayerAnimationState.WallSlide;
                    case WallMovementState.Jump: return PlayerAnimationState.WallJump;
                    case WallMovementState.Run: return PlayerAnimationState.WallRun;
                }
            }

            return LocomotionState(grounded);
        }

        /// <summary>
        /// Landing is a moment, not a state the motor reports, so it is caught on the edge — every
        /// frame, whatever the body is doing. Catching it only while in locomotion left the edge
        /// stale through an attack or a dash, and the Land pose then played late, on the ground.
        /// The pose is held only for a landing on the spot that nothing else is using the body
        /// for: landing on the run goes straight into the run instead of sliding in the pose.
        /// </summary>
        private void TrackLanding()
        {
            var grounded = _motor == null || _motor.IsGrounded;
            if (grounded && !_wasGrounded)
            {
                Landed?.Invoke();
                var isRunning = _motor != null && Mathf.Abs(_motor.Velocity.x) > LandSkipSpeed;
                var isFree = _character == null || _character.Action == PlayerAction.None;
                if (!isRunning && isFree)
                {
                    Flash(PlayerAnimationState.Land);
                }
            }

            _wasGrounded = grounded;
        }

        /// <summary>
        /// The run clip's pace follows the real ground speed, so a half-tilted stick or a slow
        /// drift does not slide the feet under a full-speed stride. Never below a third: a crawl
        /// at a tenth of the stride reads as frozen rather than slow.
        /// </summary>
        private float LocomotionRate()
        {
            if (_state != PlayerAnimationState.Run || _motor == null || _motor.RunSpeed <= 0f)
            {
                return 1f;
            }

            return Mathf.Clamp(Mathf.Abs(_motor.Velocity.x) / _motor.RunSpeed, MinRunRate, 1f);
        }

        private PlayerAnimationState LocomotionState(bool grounded)
        {
            if (Held(PlayerAnimationState.Land))
            {
                return PlayerAnimationState.Land;
            }

            var motion = _motor != null ? _motor.State : MovementState.Idle;
            switch (motion)
            {
                case MovementState.Jump:
                    return Held(PlayerAnimationState.JumpStart)
                        ? PlayerAnimationState.JumpStart
                        : PlayerAnimationState.JumpRise;
                case MovementState.Fall:
                    return PlayerAnimationState.Fall;
                case MovementState.Walk:
                case MovementState.Run:
                    return Held(PlayerAnimationState.Turn)
                        ? PlayerAnimationState.Turn
                        : PlayerAnimationState.Run;
                case MovementState.Dash:
                    return grounded ? PlayerAnimationState.Dash : PlayerAnimationState.AirDash;
                default:
                    if (Held(PlayerAnimationState.Turn))
                    {
                        return PlayerAnimationState.Turn;
                    }

                    return _variantPlaying
                        ? PlayerAnimationState.IdleVariant
                        : PlayerAnimationState.Idle;
            }
        }

        private PlayerAnimationState SkillState()
        {
            var skill = _skills != null ? _skills.RunningSkill : null;
            return skill != null && skill.Active != null
                ? skill.Active.AnimationState
                : PlayerAnimationState.ActiveSkill;
        }

        /// <summary>
        /// Which swing of the chain this is. The combo index is the combat system's own counter, so
        /// the art follows the real chain instead of keeping a second one that could drift.
        /// </summary>
        private PlayerAnimationState AttackState(PlayerAction action)
        {
            if (action == PlayerAction.HeavyAttack)
            {
                return PlayerAnimationState.Heavy;
            }

            if (_combat != null && _combat.CurrentDirection == AttackDirection.Up)
            {
                return PlayerAnimationState.AttackUp;
            }

            if (_combat != null && _combat.CurrentDirection == AttackDirection.Down)
            {
                return PlayerAnimationState.AttackDown;
            }

            var index = _combat != null ? _combat.ComboIndex : 0;
            switch (Mathf.Clamp(index, 0, 2))
            {
                case 1: return PlayerAnimationState.Attack2;
                case 2: return PlayerAnimationState.Attack3;
                default: return PlayerAnimationState.Attack1;
            }
        }
    }
}
