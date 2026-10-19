using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The one place a combat event becomes something you can see and feel.
    ///
    /// Every impact goes through here: burst, hit-stop, camera kick, screen flash, light pulse,
    /// and the audio hook a later phase will use. Combat code raises the events it always raised
    /// and knows nothing about any of this, which is what keeps presentation out of gameplay.
    ///
    /// Three rules keep it from becoming the thing that ruins a fight:
    ///
    /// One event per hit. <see cref="CombatActor.DamageTaken"/> is the authoritative signal, so a
    /// blocked hit produces a block and never also a hit, and a hit absorbed by invincibility
    /// frames produces nothing at all.
    ///
    /// The screen belongs to one moment at a time. Freeze, shake and flash are collected per frame
    /// and only the highest-priority one is applied, so a spell landing on five enemies does not
    /// freeze the game five times.
    ///
    /// Everything can be turned off. Shake, flash and freeze all scale through
    /// <see cref="FeedbackSettings"/>, and combat still reads with all three at zero.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatFeedbackService : MonoBehaviour
    {
        [SerializeField] private FeedbackLibrary _library;
        [SerializeField] private CameraShaker _shaker;
        [SerializeField] private ScreenFlash _flash;
        [Tooltip("Ceiling on simultaneous bursts. More than this is noise, not information.")]
        [SerializeField, Min(4)] private int _burstCapacity = 48;
        [Tooltip("Ceiling on simultaneous light pulses; they are the most expensive effect here.")]
        [SerializeField, Min(2)] private int _lightCapacity = 10;
        [SerializeField] private Sprite _lightSprite;
        [Tooltip("Unlit, so an impact glows in a dark room instead of being dimmed by it.")]
        [SerializeField] private Material _effectMaterial;
        [Tooltip("A real 2D light cloned into each light pulse, so a parry lights the stone around "
            + "it rather than only drawing a halo. Built in the editor so it keeps its layers.")]
        [SerializeField] private UnityEngine.Rendering.Universal.Light2D _lightTemplate;

        private VfxPool _bursts;
        private VfxPool _lights;
        private BossDeathMoment _bossDeath;
        private TelegraphCues _telegraphs;
        private Transform _root;

        // Collected during a frame and applied once at the end of it.
        private FeedbackProfile _frameOwner;
        private Vector2 _frameDirection;
        private readonly HashSet<CombatActor> _actors = new HashSet<CombatActor>();
        private readonly HashSet<StatusEffects> _statuses = new HashSet<StatusEffects>();
        private readonly HashSet<EnemyController> _enemies = new HashSet<EnemyController>();
        private readonly HashSet<BossController> _bosses = new HashSet<BossController>();
        private readonly HashSet<BossEncounter> _encounters = new HashSet<BossEncounter>();
        private readonly HashSet<Checkpoint> _checkpoints = new HashSet<Checkpoint>();

        public static CombatFeedbackService Instance { get; private set; }

        /// <summary>Raised for every moment played, so audio can attach without touching combat.</summary>
        public event System.Action<FeedbackKind, Vector2> Played;

        public int LiveBursts => _bursts?.Live ?? 0;
        public int LiveLights => _lights?.Live ?? 0;
        public FeedbackLibrary Library => _library;

        /// <summary>True while a boss's death holds the world slow (GAME_FEEL_IDEAS item 11).</summary>
        public bool IsBossDeathRunning => _bossDeath != null && _bossDeath.IsRunning;

        /// <summary>Enemy windups being breathed through right now (GAME_FEEL_IDEAS item 12).</summary>
        public int TelegraphsRunning => _telegraphs?.Count ?? 0;

        private void Awake()
        {
            Instance = this;
            _root = new GameObject("VFX").transform;
            _root.SetParent(transform, false);
            _bursts = new VfxPool(_root, _burstCapacity, _effectMaterial);
            _lights = new VfxPool(_root, _lightCapacity, _effectMaterial, _lightTemplate);
            _bossDeath = new BossDeathMoment(PlayCrackLight);
            _telegraphs = new TelegraphCues(point =>
                PlayInternal(FeedbackKind.EnemyTelegraph, point, Vector2.right, 0.8f, Color.white));
            if (_shaker == null && Camera.main != null)
            {
                _shaker = Camera.main.GetComponent<CameraShaker>();
            }

            if (_flash == null && Camera.main != null)
            {
                _flash = Camera.main.GetComponent<ScreenFlash>();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            ZoneStreamer.ZonesChanged -= SubscribeLevel;
            Unsubscribe();
            _bossDeath?.Stop();
            _telegraphs?.Clear();
        }

        private void Start()
        {
            Subscribe();
            ZoneStreamer.ZonesChanged += SubscribeLevel;
        }

        // ---------- the public entry point ----------

        /// <summary>
        /// Plays one combat moment. Safe to call when there is no service, no library and no
        /// profile: feedback failing must never take the hit that caused it with it.
        /// </summary>
        public static void Play(FeedbackKind kind, Vector2 position, Vector2 direction = default,
            float scale = 1f)
        {
            Instance?.PlayInternal(kind, position, direction, scale, Color.white);
        }

        /// <summary>The same moment, its burst tinted — a spell's colour on a pale effect.</summary>
        public static void Play(FeedbackKind kind, Vector2 position, Color tint, float scale = 1f)
        {
            Instance?.PlayInternal(kind, position, Vector2.right, scale, tint);
        }

        /// <summary>A directed moment, tinted: movement dust in the colour of the ground it rose from.</summary>
        public static void Play(FeedbackKind kind, Vector2 position, Vector2 direction, float scale, Color tint)
        {
            Instance?.PlayInternal(kind, position, direction, scale, tint);
        }

        private void PlayInternal(FeedbackKind kind, Vector2 position, Vector2 direction,
            float scale) => PlayInternal(kind, position, direction, scale, Color.white);

        private void PlayInternal(FeedbackKind kind, Vector2 position, Vector2 direction,
            float scale, Color tint)
        {
            if (_library == null)
            {
                return;
            }

            var profile = _library.Profile(kind);
            if (profile == null)
            {
                return;
            }

            // The burst is per-impact and always plays: it is what tells the player where the hit
            // landed, and five hits should produce five sparks.
            if (profile.HasBurst)
            {
                _bursts.Take().Play(profile, position, direction, scale, _bursts, tint);
            }

            if (profile.HasLight && _lightSprite != null)
            {
                PlayLight(profile, position);
            }

            // Freeze, shake and flash are screen-wide, so they are claimed rather than repeated.
            if (_frameOwner == null || profile.Priority > _frameOwner.Priority)
            {
                _frameOwner = profile;
                _frameDirection = direction;
            }

            Played?.Invoke(kind, position);
        }

        private void PlayLight(FeedbackProfile profile, Vector2 position)
        {
            _lights.Take().PlayLight(_lightSprite, profile.LightColor, profile.LightRadius,
                profile.LightDuration, position, _lights);
        }

        private void LateUpdate()
        {
            _bossDeath?.Tick(Time.unscaledDeltaTime);
            _telegraphs?.Tick();
            if (_frameOwner == null)
            {
                return;
            }

            var profile = _frameOwner;
            _frameOwner = null;

            HitStop.Request(profile.HitStop);
            if (_shaker != null)
            {
                // The direction of the moment that owns the frame: the camera is pushed along the blow.
                _shaker.Shake(profile.Shake, profile.ShakeScale, profile.ShakeVerticalBias,
                    _frameDirection, profile.ShakeKick);
            }

            if (_flash != null && profile.ScreenFlash > 0f)
            {
                _flash.Flash(profile.FlashColor, profile.ScreenFlash);
            }
        }

        /// <summary>Drops every running effect. Called on death, respawn and world reset.</summary>
        public void ClearAll()
        {
            _bursts?.ClearAll();
            _lights?.ClearAll();
            _shaker?.Clear();
            _flash?.Clear();
            _frameOwner = null;
            _telegraphs?.Clear();
        }

        private void PlayCrackLight(Vector2 position, Color color, float radius, float duration)
        {
            if (_lightSprite == null) { return; }
            _lights.Take().PlayLight(_lightSprite, color, radius, duration, position, _lights);
        }

        /// <summary>The body's drawn extent, so the light breaks out of the figure and not its feet.</summary>
        private static Bounds BodyBounds(BossController boss)
        {
            var renderers = boss.GetComponentsInChildren<SpriteRenderer>();
            if (renderers.Length == 0) { return new Bounds(boss.transform.position + Vector3.up, new Vector3(1.5f, 2f, 0f)); }

            Bounds? bounds = null;
            foreach (var renderer in renderers)
            {
                if (!renderer.enabled || renderer.sprite == null) { continue; }
                if (bounds == null) { bounds = renderer.bounds; }
                else { var grown = bounds.Value; grown.Encapsulate(renderer.bounds); bounds = grown; }
            }

            return bounds ?? new Bounds(boss.transform.position + Vector3.up, new Vector3(1.5f, 2f, 0f));
        }

        // ---------- listening to combat ----------

        private void Subscribe()
        {
            SubscribeLevel();
            var player = FindAnyObjectByType<PlayerCharacter>();
            if (player != null)
            {
                var defense = player.GetComponent<PlayerDefense>();
                if (defense != null)
                {
                    defense.GuardBroken += () => PlayAt(FeedbackKind.GuardBreak, player.transform);
                }

                player.DashStarted += () => OnPlayerDash(player);
                var combat = player.GetComponent<PlayerCombat>();
                if (combat != null)
                {
                    combat.Pogoed += () => PlayInternal(FeedbackKind.Pogo,
                        (Vector2)player.transform.position + PogoSparkOffset, Vector2.down, 1f);
                }
            }

            // Respawning has to leave nothing behind: no shake, no freeze, no burst still playing
            // from the death that caused it. The respawner is the authoritative signal — the
            // motor's own is about being moved, which is not the same thing.
            var respawner = FindAnyObjectByType<PlayerRespawner>();
            if (respawner != null)
            {
                respawner.PlayerRespawned += ClearAll;
                respawner.PlayerDied += ClearAll;
            }

            var shadow = FindAnyObjectByType<ShadowAbilities>();
            if (shadow != null)
            {
                shadow.Started += _ => PlayAt(FeedbackKind.ShadowDashStart, shadow.transform);
                shadow.Ended += () => PlayAt(FeedbackKind.ShadowDashEnd, shadow.transform);
            }
        }

        /// <summary>
        /// Everything of the level: the zones loaded since the last look bring their fighters,
        /// arenas and altars, and each is hooked once (<see cref="ZoneStreamer.ZonesChanged"/>).
        /// </summary>
        private void SubscribeLevel()
        {
            foreach (var actor in ZoneObjects.New(_actors))
            {
                actor.AttackParried += OnParried;
                if (actor.GetComponent<PlayerCharacter>() != null)
                {
                    // The hero's own hurt (COMBAT_FEEDBACK.md §6а). Its death has its own
                    // presentation; the enemy-death burst and the zombie's death cry are not it.
                    actor.DamageTaken += OnPlayerDamaged;
                    continue;
                }

                actor.DamageTaken += OnDamageTaken;
                actor.Killed += OnKilled;
            }

            foreach (var status in ZoneObjects.New(_statuses))
            {
                status.Applied += type => OnStatusApplied(status, type);
            }

            foreach (var enemy in ZoneObjects.New(_enemies))
            {
                var captured = enemy;
                enemy.GuardBrokenOpen += () => PlayAt(FeedbackKind.GuardBreak, captured.transform);
                enemy.StaggerBroken += _ => PlayAt(FeedbackKind.StaggerBreak, captured.transform);
                var sockets = enemy.GetComponentInChildren<VisualSockets>(true);
                enemy.AttackTelegraphed += attack =>
                {
                    OnEnemyTelegraph(captured, sockets);
                    _telegraphs.Begin(captured, attack, sockets);
                };
            }

            // Every boss in the scene: the forest's Warden deserves the same weight as the Guardian.
            foreach (var boss in ZoneObjects.New(_bosses, includeInactive: true))
            {
                var bossSockets = boss.GetComponentInChildren<VisualSockets>(true);
                var bossFlash = boss.GetComponent<HitFlash>();
                boss.SwingWindup += (movement, swing) => OnBossWindup(boss, bossSockets, bossFlash, movement);
                boss.MovementBegan += movement => OnBossMovement(boss, movement, true);
                boss.MovementEnded += movement => OnBossMovement(boss, movement, false);
                boss.PhaseChanged += phase => OnBossPhase(boss, phase);
                boss.Defeated += defeated =>
                {
                    PlayAt(FeedbackKind.BossDeath, defeated.transform, 2f);
                    _bossDeath.Begin(BodyBounds(defeated));
                };
                boss.Staggered += _ => PlayAt(FeedbackKind.StaggerBreak, boss.transform, 1.5f);
            }

            foreach (var encounter in ZoneObjects.New(_encounters, includeInactive: true))
            {
                ArenaSealGlow.Attach(encounter, _library, _effectMaterial, _lightTemplate);
            }

            // Lighting an altar: a warm flash at the flame. The altar's own light wave and the
            // flame catching are CheckpointVisual's; this is the burst over them.
            foreach (var checkpoint in ZoneObjects.New(_checkpoints, includeInactive: true))
            {
                var visual = checkpoint.GetComponent<CheckpointVisual>();
                checkpoint.Activated += lit => PlayInternal(FeedbackKind.AltarKindle,
                    visual != null ? visual.FlamePosition : (Vector2)lit.transform.position,
                    Vector2.right, 1f);
            }
        }

        private void Unsubscribe()
        {
            foreach (var actor in _actors)
            {
                if (actor == null)
                {
                    continue;
                }

                actor.DamageTaken -= OnDamageTaken;
                actor.DamageTaken -= OnPlayerDamaged;
                actor.AttackParried -= OnParried;
                actor.Killed -= OnKilled;
            }

            _actors.Clear();
            _statuses.Clear();
        }

        /// <summary>
        /// The whole physical-combat routing, in one readable decision. Outcome first, because a
        /// blocked or parried hit is a different moment from a hit that landed — not a quieter
        /// version of it.
        /// </summary>
        private void OnDamageTaken(DamageInfo info, DamageResult result)
        {
            var position = ImpactPoint(info);

            switch (result.Outcome)
            {
                case DamageOutcome.Ignored:
                    // Invincibility frames ate it. Showing an impact here would tell the player
                    // they were hit when they were not.
                    return;
                case DamageOutcome.Blocked:
                    PlayInternal(FeedbackKind.Block, position, info.HitDirection, 1f);
                    return;
                case DamageOutcome.Parried:
                    PlayInternal(FeedbackKind.Parry, position, info.HitDirection, 1f);
                    return;
            }

            PlayInternal(KindForHit(info), position, BurstDirection(info), ScaleForHit(info));
        }

        /// <summary>
        /// A blow that landed on the hero is the hero being hurt: its own profile — a longer stop
        /// and a shove of the camera the way the blow came — rather than the spark the hero's own
        /// sword makes on an enemy. Elements and statuses keep their impacts, which say what hit;
        /// blocks, parries and invincibility go the usual way.
        /// </summary>
        private void OnPlayerDamaged(DamageInfo info, DamageResult result)
        {
            if (result.Outcome == DamageOutcome.Hit && !info.IsStatusDamage
                && info.Type == DamageType.Physical)
            {
                PlayInternal(FeedbackKind.PlayerHurt, ImpactPoint(info), info.HitDirection, 1f);
                return;
            }

            OnDamageTaken(info, result);
        }

        private static Vector2 ImpactPoint(in DamageInfo info) =>
            info.HitPoint.sqrMagnitude > 0.0001f
                ? info.HitPoint
                : (Vector2)(info.Attacker != null
                    ? info.Attacker.transform.position
                    : Vector3.zero);

        /// <summary>Where the pogo's spark sits: under the feet, where the blade struck.</summary>
        private static readonly Vector2 PogoSparkOffset = new Vector2(0f, -1.1f);

        /// <summary>
        /// Which way the impact bursts. A swing up or down still knocks back sideways — that is
        /// combat's business and unchanged — but its spark follows the blade, not the facing.
        /// </summary>
        private static Vector2 BurstDirection(in DamageInfo info)
        {
            if (info.Source is PlayerCombat combat)
            {
                switch (combat.CurrentDirection)
                {
                    case AttackDirection.Up: return Vector2.up;
                    case AttackDirection.Down: return Vector2.down;
                }
            }

            return info.HitDirection;
        }

        /// <summary>
        /// Which impact a landed hit is. Damage type wins over weight, because knowing that you
        /// were burned matters more than knowing it was a heavy blow.
        /// </summary>
        private static FeedbackKind KindForHit(in DamageInfo info)
        {
            if (info.HasTag(DamageTags.Status))
            {
                return StatusKind(info.Type);
            }

            switch (info.Type)
            {
                case DamageType.Fire: return FeedbackKind.FireImpact;
                case DamageType.Frost: return FeedbackKind.FrostImpact;
                case DamageType.Dark: return FeedbackKind.ShadowImpact;
                case DamageType.Magic: return FeedbackKind.ArcaneBurst;
            }

            if (info.IsCritical)
            {
                return FeedbackKind.CriticalHit;
            }

            // Heavy blows carry far more poise damage than light ones; that is already the
            // difference between them, so it is what decides which impact is shown.
            return info.StaggerDamage >= 25f ? FeedbackKind.HeavyHit : FeedbackKind.LightHit;
        }

        private static FeedbackKind StatusKind(DamageType type)
        {
            switch (type)
            {
                case DamageType.Fire: return FeedbackKind.BurnProc;
                case DamageType.Frost: return FeedbackKind.FrostProc;
                case DamageType.Poison: return FeedbackKind.PoisonProc;
                default: return FeedbackKind.BleedProc;
            }
        }

        private static float ScaleForHit(in DamageInfo info)
        {
            // A greatsword reads heavier than a sword without a second profile: the same impact,
            // grown by how much poise the blow actually carries.
            var weight = Mathf.InverseLerp(10f, 80f, info.StaggerDamage);
            return Mathf.Lerp(0.85f, 1.45f, weight);
        }

        private void OnParried(DamageInfo info) =>
            PlayInternal(FeedbackKind.Parry, info.HitPoint, info.HitDirection, 1f);

        private void OnKilled(DamageInfo info)
        {
            var position = info.HitPoint.sqrMagnitude > 0.0001f ? info.HitPoint : Vector2.zero;
            PlayInternal(FeedbackKind.EnemyDeath, position, info.HitDirection, 1f);
        }

        private void OnStatusApplied(StatusEffects source, StatusType type)
        {
            if (source == null)
            {
                return;
            }

            var kind = type switch
            {
                StatusType.Bleed => FeedbackKind.BleedProc,
                StatusType.Poison => FeedbackKind.PoisonProc,
                StatusType.Burn => FeedbackKind.BurnProc,
                StatusType.Frost => FeedbackKind.FrostProc,
                _ => FeedbackKind.BleedProc
            };
            PlayAt(kind, source.transform);
        }

        private void OnPlayerDash(PlayerCharacter player)
        {
            var motor = player.GetComponent<PlayerMotor2D>();
            var grounded = motor == null || motor.IsGrounded;
            // The streak trails behind the dash, so it follows the way the dash went.
            var direction = new Vector2(motor != null ? motor.FacingDirection : 1, 0f);
            PlayInternal(grounded ? FeedbackKind.Dash : FeedbackKind.AirDash,
                player.transform.position, direction, 1f);
        }

        /// <summary>
        /// A glint where the weapon is as the swing winds up: in a dark room the pose alone can be
        /// lost, and the glint is what the eye catches first.
        /// </summary>
        private void OnEnemyTelegraph(EnemyController enemy, VisualSockets sockets)
        {
            if (enemy == null)
            {
                return;
            }

            var position = sockets != null && sockets.Has(VisualSocket.Weapon)
                ? sockets.Position(VisualSocket.Weapon)
                : enemy.transform.position;
            PlayInternal(FeedbackKind.EnemyTelegraph, position, Vector2.right, 1f);
        }

        /// <summary>The red of a boss about to throw itself at the player: danger, not damage.</summary>
        public static readonly Color BossDangerColor = new Color(1f, 0.32f, 0.18f, 0.7f);

        /// <summary>How long the red warning swells and fades; about the windup of a charge.</summary>
        public const float BossDangerTime = 0.45f;

        /// <summary>
        /// Every boss swing glints on the weapon as the enemies' do, larger; a charge or a leap —
        /// the moves that cover the arena — also glows the body red, so "it is about to come at
        /// me" reads apart from "it is about to swing".
        /// </summary>
        private void OnBossWindup(BossController boss, VisualSockets sockets, HitFlash flash,
            BossAttackMovement movement)
        {
            if (boss == null)
            {
                return;
            }

            var weapon = sockets != null && sockets.Has(VisualSocket.Weapon)
                ? sockets.Position(VisualSocket.Weapon)
                : boss.transform.position;
            PlayInternal(FeedbackKind.EnemyTelegraph, weapon, Vector2.right, 1.7f);
            if (movement == BossAttackMovement.None)
            {
                return;
            }

            flash?.Warn(BossDangerColor, BossDangerTime);
            PlayInternal(FeedbackKind.BossDanger, boss.transform.position, Vector2.right, 1f);
        }

        /// <summary>
        /// Dust when a charge starts running, and the slam when a leap comes down — at the landing,
        /// where the blow is, not at the take-off as it used to play.
        /// </summary>
        private void OnBossMovement(BossController boss, BossAttackMovement movement, bool began)
        {
            if (boss == null)
            {
                return;
            }

            if (began && movement == BossAttackMovement.Charge)
            {
                PlayAt(FeedbackKind.BossCharge, boss.transform, 1.4f);
            }
            else if (!began && movement == BossAttackMovement.Leap)
            {
                PlayAt(FeedbackKind.BossSlam, boss.transform, 2f);
            }
        }

        private void OnBossPhase(BossController boss, int phase)
        {
            // Phase one also fires on reset, when nothing should be shown.
            if (phase >= 2)
            {
                PlayAt(FeedbackKind.BossPhase, boss.transform, 2f);
            }
        }

        private void PlayAt(FeedbackKind kind, Transform where, float scale = 1f)
        {
            if (where != null)
            {
                PlayInternal(kind, where.position, Vector2.right, scale);
            }
        }
    }
}
