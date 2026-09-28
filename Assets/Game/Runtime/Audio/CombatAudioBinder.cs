using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Turns the events the game already raises into sounds.
    ///
    /// It exists so that no gameplay script has to know audio exists. Combat, the enemies and the
    /// boss keep raising exactly the events they raised before this phase; this listens and asks
    /// the manager for a sound. Delete this component and the game still plays, silently.
    ///
    /// Most of combat arrives through <see cref="CombatFeedbackService.Played"/>, which PHASE 20
    /// built as the single authoritative point where a combat moment becomes presentation. Reusing
    /// it means audio and effects can never disagree about what just happened — a blocked hit is a
    /// block in both, and a hit absorbed by invincibility is silent in both.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatAudioBinder : MonoBehaviour
    {
        [SerializeField] private CombatFeedbackService _feedback;
        [SerializeField] private PlayerCharacter _player;
        [Tooltip("Frames of the run cycle where a foot lands. Taken from the animation, not a timer.")]
        [SerializeField] private int[] _footstepFrames = { 0, 3 };

        private PlayerCombat _combat;
        private PlayerAnimationController _animation;
        private PlayerRespawner _respawner;
        private AudioSource _checkpointLoop;
        private readonly HashSet<EnemyController> _enemies = new HashSet<EnemyController>();
        private readonly HashSet<BossController> _bosses = new HashSet<BossController>();
        private readonly HashSet<PersistentDoor> _doors = new HashSet<PersistentDoor>();
        private readonly HashSet<Shortcut> _shortcuts = new HashSet<Shortcut>();
        private readonly HashSet<BossEncounter> _encounters = new HashSet<BossEncounter>();
        private readonly HashSet<EnemyDormancy> _sleepers = new HashSet<EnemyDormancy>();
        private readonly HashSet<EnemySupport> _priests = new HashSet<EnemySupport>();

        private void Start()
        {
            if (_feedback == null) { _feedback = FindAnyObjectByType<CombatFeedbackService>(); }
            if (_player == null) { _player = FindAnyObjectByType<PlayerCharacter>(); }

            if (_feedback != null)
            {
                _feedback.Played += OnFeedback;
            }

            BindPlayer();
            BindLevel();
            BindWorld();
            ZoneStreamer.ZonesChanged += BindLevel;
        }

        /// <summary>What the zones loaded since the last look brought; each thing is hooked once.</summary>
        private void BindLevel()
        {
            BindEnemies();
            BindBoss();
            BindGates();
            BindCapitalEnemies();
        }

        /// <summary>The capital's own moments: stone tearing loose, a wound closed, a step through ash.</summary>
        private void BindCapitalEnemies()
        {
            foreach (var sleeper in ZoneObjects.New(_sleepers, includeInactive: true))
            {
                var captured = sleeper;
                sleeper.Woke += () => AudioManager.Play(AudioId.GargoyleWake, captured.transform.position);
            }

            foreach (var priest in ZoneObjects.New(_priests, includeInactive: true))
            {
                priest.Healed += ally => AudioManager.Play(AudioId.PriestHeal, ally.transform.position);
                priest.Blinked += (from, to) => AudioManager.Play(AudioId.PriestBlink, to);
            }
        }

        private void OnDestroy()
        {
            ZoneStreamer.ZonesChanged -= BindLevel;
            if (_feedback != null)
            {
                _feedback.Played -= OnFeedback;
            }

            // Static events outlive the scene, so leaving these attached would make a reloaded
            // level play every hazard twice.
            HazardZone.AnyTelegraphed -= OnHazardTelegraphed;
            HazardZone.AnyActivated -= OnHazardActivated;
        }

        // ---------- combat, through the feedback service ----------

        /// <summary>
        /// One combat moment, one sound. The mapping is deliberately flat and readable: this is
        /// the table someone will want to read when a fight sounds wrong.
        /// </summary>
        public static AudioId ForFeedback(FeedbackKind kind)
        {
            switch (kind)
            {
                case FeedbackKind.LightHit: return AudioId.HitFlesh;
                case FeedbackKind.HeavyHit: return AudioId.HitHeavy;
                case FeedbackKind.CriticalHit: return AudioId.HitCritical;
                case FeedbackKind.Block: return AudioId.Block;
                case FeedbackKind.Parry: return AudioId.Parry;
                case FeedbackKind.GuardBreak: return AudioId.GuardBreak;
                case FeedbackKind.StaggerBreak: return AudioId.StaggerBreak;
                case FeedbackKind.PlayerHurt: return AudioId.PlayerHurt;
                case FeedbackKind.FireImpact: return AudioId.FireImpact;
                case FeedbackKind.FrostImpact: return AudioId.FrostImpact;
                case FeedbackKind.ArcaneBurst: return AudioId.ArcaneBurst;
                case FeedbackKind.ShadowImpact: return AudioId.ShadowImpact;
                case FeedbackKind.ShadowDashStart: return AudioId.ShadowDashStart;
                case FeedbackKind.ShadowDashEnd: return AudioId.ShadowDashEnd;
                case FeedbackKind.BleedProc: return AudioId.BleedProc;
                case FeedbackKind.PoisonProc: return AudioId.PoisonProc;
                case FeedbackKind.BurnProc: return AudioId.BurnProc;
                case FeedbackKind.FrostProc: return AudioId.FrostProc;
                case FeedbackKind.Dash: return AudioId.DashGround;
                case FeedbackKind.AirDash: return AudioId.DashAir;
                case FeedbackKind.WallContact: return AudioId.WallContact;
                case FeedbackKind.GroundBreaker: return AudioId.GroundBreaker;
                case FeedbackKind.DashStrike: return AudioId.DashStrike;
                case FeedbackKind.BossSlam: return AudioId.BossSlam;
                case FeedbackKind.BossCharge: return AudioId.BossCharge;
                case FeedbackKind.BossPhase: return AudioId.BossPhase;
                case FeedbackKind.BossDeath: return AudioId.BossDeath;
                case FeedbackKind.EnemyDeath: return AudioId.ZombieDeath;
                case FeedbackKind.Pickup: return AudioId.PickupCommon;
                case FeedbackKind.Pogo: return AudioId.Pogo;
                // The seal shutting is covered by the boss waking; its breaking is a gate opening.
                case FeedbackKind.ArenaOpen: return AudioId.GateOpen;
                // A lit altar is already heard (CheckpointActivate); a lost spell has its own hiss.
                case FeedbackKind.CastFizzle: return AudioId.CastFizzle;
                case FeedbackKind.LostGoldRecovered: return AudioId.PickupCurrency;
                default: return AudioId.None;
            }
        }

        private void OnFeedback(FeedbackKind kind, Vector2 position)
        {
            var id = ForFeedback(kind);
            if (id == AudioId.None)
            {
                return;
            }

            AudioManager.Play(id, position);

            // The loudest moments make room for themselves instead of shouting over the room.
            var manager = AudioManager.Instance;
            if (manager == null)
            {
                return;
            }

            switch (kind)
            {
                case FeedbackKind.BossPhase:
                case FeedbackKind.BossDeath:
                    manager.Duck(AudioBus.Ambience, 0.7f);
                    manager.Duck(AudioBus.Music, 0.5f);
                    break;
                case FeedbackKind.BossSlam:
                case FeedbackKind.Parry:
                    manager.Duck(AudioBus.Ambience, 0.35f);
                    break;
            }
        }

        // ---------- the player ----------

        private void BindPlayer()
        {
            if (_player == null)
            {
                return;
            }

            _combat = _player.GetComponent<PlayerCombat>();
            _animation = _player.GetComponent<PlayerAnimationController>();

            if (_combat != null)
            {
                // The swing plays whether or not it connects; the impact is a separate sound.
                _combat.AttackStarted += OnPlayerAttack;
            }

            if (_animation != null)
            {
                _animation.FrameChanged += OnPlayerFrame;
                _animation.StateChanged += OnPlayerState;
                // Every landing is heard, including the running ones that skip the Land pose.
                _animation.Landed += () => AudioManager.Play(AudioId.Land, _player.transform.position);
            }

            var equipment = _player.GetComponent<PlayerEquipment>();
            if (equipment != null)
            {
                equipment.ActiveWeaponChanged += _ =>
                    AudioManager.Play(AudioId.WeaponSwitch, _player.transform.position);
            }

            _player.Died += () => AudioManager.Play(AudioId.PlayerDeath, _player.transform.position);
            _player.Flasks.DrinkStarted += () => AudioManager.Play(
                _player.Flasks.IsDrinkingFlask ? AudioId.FlaskDrink : AudioId.HerbEat,
                _player.transform.position);
        }

        /// <summary>
        /// Which swing. Weight comes from the weapon, so a greatsword reads heavier without the
        /// combat code knowing anything about it.
        /// </summary>
        private void OnPlayerAttack(AttackDefinition attack)
        {
            if (_player == null || attack == null)
            {
                return;
            }

            var weapon = _combat != null ? _combat.Weapon : null;
            var isTwoHanded = weapon != null && weapon.WeaponType == WeaponType.Greatsword;
            var isHeavy = _player.Action == PlayerAction.HeavyAttack;
            var id = isTwoHanded
                ? (isHeavy ? AudioId.GreatswordSwingHeavy : AudioId.GreatswordSwingLight)
                : (isHeavy ? AudioId.SwordSwingHeavy : AudioId.SwordSwingLight);
            AudioManager.Play(id, _player.transform.position);
        }

        /// <summary>
        /// Footsteps come from the animation's own frames, not from a timer, so they land when the
        /// foot does however the run is retimed later.
        /// </summary>
        private void OnPlayerFrame(PlayerAnimationState state, int frame)
        {
            if (state != PlayerAnimationState.Run || _player == null)
            {
                return;
            }

            foreach (var contact in _footstepFrames)
            {
                if (contact == frame)
                {
                    AudioManager.Play(AudioId.FootstepStone, _player.transform.position, 0.7f);
                    return;
                }
            }
        }

        private void OnPlayerState(PlayerAnimationState from, PlayerAnimationState to)
        {
            if (_player == null)
            {
                return;
            }

            var at = (Vector2)_player.transform.position;
            switch (to)
            {
                case PlayerAnimationState.JumpStart:
                    AudioManager.Play(AudioId.Jump, at, 0.6f);
                    break;
                case PlayerAnimationState.WallSlide:
                    AudioManager.Play(AudioId.WallSlide, at, 0.6f);
                    break;
                case PlayerAnimationState.CastStart:
                    AudioManager.Play(CastSound(), at);
                    break;
                case PlayerAnimationState.ShadowStrike:
                    AudioManager.Play(AudioId.ShadowStrike, at);
                    break;
            }
        }

        private AudioId CastSound()
        {
            var caster = _player != null ? _player.GetComponent<SpellCaster>() : null;
            var spell = caster != null ? caster.CastingSpell : null;
            if (spell == null)
            {
                return AudioId.ArcaneCast;
            }

            switch (spell.Element)
            {
                case DamageType.Fire: return AudioId.FireCast;
                case DamageType.Frost: return AudioId.FrostCast;
                default: return AudioId.ArcaneCast;
            }
        }

        // ---------- enemies ----------

        private void BindEnemies()
        {
            foreach (var enemy in ZoneObjects.New(_enemies))
            {
                var captured = enemy;
                var isGuard = enemy.Guard != null;
                // The forest's enemies carry their own voice; the Tomb's two are still told apart
                // by whether they have a shield, exactly as before.
                var profile = enemy.GetComponent<ActorAudioProfile>();

                enemy.Alerted += () => AudioManager.Play(
                    Pick(profile != null ? profile.Alert : AudioId.None,
                        isGuard ? AudioId.GuardRaise : AudioId.ZombieAlert),
                    captured.transform.position);

                enemy.AttackTelegraphed += _ => AudioManager.Play(
                    Pick(profile != null ? profile.AttackFor(captured.CurrentAttackKind)
                        : AudioId.None, SwingFor(captured, isGuard)),
                    captured.transform.position);

                enemy.Hurt += _ => AudioManager.Play(
                    Pick(profile != null ? profile.Hurt : AudioId.None,
                        isGuard ? AudioId.GuardHurt : AudioId.ZombieHurt),
                    captured.transform.position);

                enemy.Died += _ => AudioManager.Play(
                    Pick(profile != null ? profile.Death : AudioId.None,
                        isGuard ? AudioId.GuardDeath : AudioId.ZombieDeath),
                    captured.transform.position);

                enemy.GuardImpacted += _ => AudioManager.Play(
                    AudioId.BlockShield, captured.transform.position);

                // A reset must not leave a death or a status still sounding.
                enemy.EnemyReset += _ => AudioManager.Instance?.StopLoops();
            }
        }

        /// <summary>The profile's sound when it has one, otherwise the old pairing.</summary>
        private static AudioId Pick(AudioId preferred, AudioId fallback) =>
            preferred != AudioId.None ? preferred : fallback;

        private static AudioId SwingFor(EnemyController enemy, bool isGuard)
        {
            if (!isGuard)
            {
                return AudioId.ZombieSwing;
            }

            switch (enemy.CurrentAttackKind)
            {
                case EnemyAttackKind.ShieldBash: return AudioId.GuardBash;
                case EnemyAttackKind.Heavy: return AudioId.GuardHeavy;
                default: return AudioId.GuardBash;
            }
        }

        // ---------- the guardian ----------

        /// <summary>
        /// Every boss in the level, not the first one found. With a second encounter in the same
        /// scene the old single lookup silently left one of them mute, which is the kind of bug
        /// that only shows up the first time someone reaches it.
        /// </summary>
        private void BindBoss()
        {
            foreach (var found in ZoneObjects.New(_bosses))
            {
                var boss = found;
                var profile = boss.GetComponent<ActorAudioProfile>();

                boss.FightStarted += () => AudioManager.Play(
                    Pick(profile != null ? profile.Alert : AudioId.None, AudioId.BossAwaken),
                    boss.transform.position);
                boss.Staggered += _ => AudioManager.Play(
                    Pick(profile != null ? profile.Hurt : AudioId.None, AudioId.BossStunned),
                    boss.transform.position);
                boss.AttackStarted += kind => OnBossAttack(boss, kind);

                var animation = boss.GetComponent<BossAnimationController>();
                if (animation != null)
                {
                    animation.StateChanged += (from, to) => OnBossState(boss, profile, to);
                }
            }
        }

        /// <summary>
        /// The charge telegraph is the one sound in the game that has to cut through everything.
        /// It gets the loudest scale and ducks the room, because a player who misses it eats the
        /// charge — and the brief is explicit that it must be recognisable without looking.
        /// </summary>
        private void OnBossAttack(BossController boss, BossAttackKind kind)
        {
            var at = (Vector2)boss.transform.position;
            switch (kind)
            {
                case BossAttackKind.Charge:
                    AudioManager.Play(AudioId.BossChargeStart, at, 1.2f);
                    AudioManager.Instance?.Duck(AudioBus.Ambience, 0.6f);
                    break;
                case BossAttackKind.JumpSlam:
                    AudioManager.Play(AudioId.BossJump, at);
                    break;
                case BossAttackKind.ShieldBash:
                    AudioManager.Play(AudioId.BossBash, at);
                    break;
                case BossAttackKind.TripleCombo:
                    AudioManager.Play(AudioId.BossCombo, at);
                    break;

                // The Warden's moves. The thrust and the leap duck the room the way the Guardian's
                // charge does: they are the two that punish a player who was not listening.
                case BossAttackKind.GlaiveSlash:
                    AudioManager.Play(AudioId.WardenGlaive, at);
                    break;
                case BossAttackKind.LongThrust:
                    AudioManager.Play(AudioId.WardenThrust, at, 1.2f);
                    AudioManager.Instance?.Duck(AudioBus.Ambience, 0.6f);
                    break;
                case BossAttackKind.Sweep:
                    AudioManager.Play(AudioId.WardenSweep, at);
                    break;
                case BossAttackKind.RootShot:
                    AudioManager.Play(AudioId.WardenRootShot, at);
                    break;
                case BossAttackKind.RootSlam:
                    AudioManager.Play(AudioId.WardenRootSlam, at);
                    AudioManager.Instance?.Duck(AudioBus.Ambience, 0.5f);
                    break;
                case BossAttackKind.PoisonField:
                    AudioManager.Play(AudioId.WardenPoisonField, at);
                    break;
                case BossAttackKind.LeapStrike:
                    AudioManager.Play(AudioId.WardenLeap, at, 1.2f);
                    AudioManager.Instance?.Duck(AudioBus.Ambience, 0.6f);
                    break;

                default:
                    AudioManager.Play(AudioId.BossSlash, at);
                    break;
            }
        }

        private void OnBossState(BossController boss, ActorAudioProfile profile,
            BossAnimationState state)
        {
            var at = (Vector2)boss.transform.position;
            switch (state)
            {
                case BossAnimationState.Intro:
                    AudioManager.Play(
                        Pick(profile != null ? profile.Alert : AudioId.None, AudioId.BossAwaken),
                        at);
                    break;
                case BossAnimationState.WalkP1:
                case BossAnimationState.MoveP2:
                    AudioManager.Play(
                        Pick(profile != null ? profile.Step : AudioId.None, AudioId.BossStep),
                        at, 0.8f);
                    break;
                case BossAnimationState.PhaseTransition:
                    AudioManager.Play(
                        Pick(profile != null ? profile.SpecialAttack : AudioId.None,
                            AudioId.BossPhase), at);
                    break;
            }
        }

        // ---------- the world ----------

        private void BindWorld()
        {
            _respawner = FindAnyObjectByType<PlayerRespawner>();
            if (_respawner != null)
            {
                _respawner.CheckpointActivated += _ =>
                    AudioManager.Play2D(AudioId.CheckpointActivate);
                _respawner.CheckpointRested += _ => AudioManager.Play2D(AudioId.CheckpointRest);
                _respawner.PlayerRespawned += OnRespawn;
                // Death silences the world so the moment lands; it never mutes outright.
                _respawner.PlayerDied += OnDeath;
            }

            var progression = FindAnyObjectByType<PlayerProgressionService>();
            if (progression != null)
            {
                progression.LevelChanged += (from, to) =>
                {
                    if (to > from)
                    {
                        AudioManager.Play2D(AudioId.LevelUp);
                    }
                };
            }

            var skills = FindAnyObjectByType<PlayerSkills>();
            if (skills != null)
            {
                skills.SkillUnlocked += (skill, rank) => AudioManager.Play2D(AudioId.SkillUnlock);
            }

            // A failed save is the one thing the interface must never let pass quietly.
            SaveSystem.SaveFailed += _ => AudioManager.Play2D(AudioId.UiSaveFailed);

            BindForest();
        }

        /// <summary>
        /// The Withered Forest's world sounds. Hazards are heard through static events rather than
        /// subscribed one by one, because the ones that matter most — a shaman's pool, the Warden's
        /// roots — do not exist yet when this runs.
        /// </summary>
        private void BindForest()
        {
            HazardZone.AnyTelegraphed += OnHazardTelegraphed;
            HazardZone.AnyActivated += OnHazardActivated;

            var motor = _player != null ? _player.GetComponent<PlayerMotor2D>() : null;
            if (motor != null)
            {
                motor.AirJumped += () =>
                    AudioManager.Play(AudioId.DoubleJump, _player.transform.position);
            }
        }

        private void BindGates()
        {
            // Doors opening and shortcuts coming down share one sound: stone and iron giving way.
            // Neither fires for something a load finds already open.
            foreach (var door in ZoneObjects.New(_doors, includeInactive: true))
            {
                door.Opened += opened => AudioManager.Play(AudioId.GateOpen,
                    opened.transform.position);
            }

            foreach (var shortcut in ZoneObjects.New(_shortcuts, includeInactive: true))
            {
                shortcut.Unlocked += unlocked => AudioManager.Play(AudioId.GateOpen,
                    unlocked.transform.position);
            }

            foreach (var encounter in ZoneObjects.New(_encounters))
            {
                encounter.AbilityUnlocked += _ => AudioManager.Play2D(AudioId.AbilityUnlocked);
            }
        }

        private void OnHazardTelegraphed(HazardZone zone)
        {
            if (IsWithinEarshot(zone))
            {
                AudioManager.Play(AudioId.HazardTelegraph, zone.transform.position, 0.8f);
            }
        }

        private void OnHazardActivated(HazardZone zone)
        {
            if (IsWithinEarshot(zone))
            {
                AudioManager.Play(AudioId.HazardBurst, zone.transform.position);
            }
        }

        /// <summary>
        /// Rooted plants cycle for the whole session, wherever the player is. One in another region
        /// is inaudible anyway, but it would still take a voice from something the player can hear.
        /// </summary>
        private bool IsWithinEarshot(HazardZone zone)
        {
            const float HearingRange = 24f;
            return zone != null && (_player == null
                || Mathf.Abs(zone.transform.position.x - _player.transform.position.x)
                    <= HearingRange);
        }

        private void OnDeath()
        {
            var manager = AudioManager.Instance;
            if (manager == null)
            {
                return;
            }

            manager.StopLoops();
            manager.Duck(AudioBus.Ambience, 0.75f);
            manager.Duck(AudioBus.Music, 0.6f);
        }

        /// <summary>
        /// A respawn starts clean. Every loop from the attempt that just ended is stopped, so no
        /// burning, no shadow trail and no phase-two layer survives into the next try.
        /// </summary>
        private void OnRespawn()
        {
            AudioManager.Instance?.StopAll();
            if (_player != null)
            {
                AudioManager.Play(AudioId.Respawn, _player.transform.position);
            }
        }
    }
}
