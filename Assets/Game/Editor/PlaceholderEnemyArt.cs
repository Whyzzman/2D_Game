using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// Draws placeholder silhouettes for the Zombie Soldier, the Shield Guard and the Guardian of
    /// the Tomb.
    ///
    /// This is not final art and does not pretend to be. It exists because the questions the
    /// animation pipeline has to answer cannot wait on a hand-drawn set: does the zombie's 0.42 s
    /// windup read, does the shield dominate the guard's front, does the Guardian's phase change
    /// register without a health bar. Proportions, palette and frame counts follow
    /// `gamedesign/CONCEPT_BRIEFS.md` and `gamedesign/ANIMATION_LIST.md`, so real frames drop in
    /// against the same contract.
    ///
    /// All three are drawn by one routine with different builds, because the brief asks them to
    /// look like three ranks of one fallen military order rather than three unrelated monsters.
    /// Hierarchy is carried by weathering and by what each one wears, not by different art rules.
    /// </summary>
    public static partial class PlaceholderEnemyArt
    {
        public const string ZombieFolder = "Assets/Game/Art/Characters/Enemies/ZombieSoldier";
        public const string GuardFolder = "Assets/Game/Art/Characters/Enemies/ShieldGuard";
        public const string BossFolder = "Assets/Game/Art/Characters/Bosses/GuardianOfTheTomb";

        // The shared royal-undead palette from the Visual Bible.
        private static readonly Color32 Charcoal = new Color32(0x14, 0x11, 0x0F, 0xFF);
        private static readonly Color32 EnemyRim = new Color32(206, 150, 100, 255);
        private static readonly Color32 SicklyEye = new Color32(236, 206, 112, 255);
        private static readonly Color32 AmberEye = new Color32(246, 172, 82, 255);
        private static readonly Color32 PaleGreenEye = new Color32(194, 236, 146, 255);
        private static readonly Color32 PoisonEye = new Color32(172, 242, 112, 255);
        private static readonly Color32 VioletEye = new Color32(236, 172, 255, 255);

        /// <summary>
        /// An eye that glows dimly: a darker ring, the bright core and a pin of light. The eyes are
        /// where the player looks to read which way an enemy faces and whether it has seen him.
        /// </summary>
        private static void Eye(Canvas canvas, Vector2 at, float radius, Color32 core, float flare = 0f)
        {
            // Just before a blow lands the eyes flare (visual item 4): bigger, whiter, a glint.
            flare = Mathf.Clamp01(flare);
            var white = new Color32(255, 255, 255, 255);
            var r = radius * (1f + 0.55f * flare);
            var lit = Color32.Lerp(core, white, 0.45f * flare);
            canvas.Ellipse(at, r * 1.7f, r * 1.4f, Color32.Lerp(core, Charcoal, 0.6f - 0.25f * flare));
            canvas.Ellipse(at, r, r * 0.8f, lit);
            canvas.Ellipse(at + new Vector2(r * 0.3f, r * 0.25f), r * 0.4f, r * 0.4f,
                Color32.Lerp(core, white, 0.6f + 0.4f * flare));
            if (flare > 0.3f)
            {
                var reach = r * (1.2f + 2.2f * flare);
                canvas.Thick(at - new Vector2(reach, 0f), at + new Vector2(reach, 0f), 1.2f, 1.2f, lit);
                canvas.Thick(at - new Vector2(0f, reach * 0.6f), at + new Vector2(0f, reach * 0.6f), 1f, 1f, lit);
            }
        }

        /// <summary>
        /// Where the loose piece hangs on the frame just drawn (x, y in pixels on the canvas, z the
        /// angle), or null for an actor with nothing loose. Set by the draw, read by Generate.
        /// </summary>
        private static Vector3? _frameAnchor;

        /// <summary>Per-frame hanging points of every generated clip, by "prefix_state".</summary>
        private static readonly Dictionary<string, Vector3[]> FrameAnchors = new Dictionary<string, Vector3[]>();

        /// <summary>The live piece's hanging points for one clip, or null when the actor has none.</summary>
        public static Vector3[] AnchorsFor<TState>(string prefix, TState state) =>
            FrameAnchors.TryGetValue($"{prefix}_{state.ToString().ToLowerInvariant()}", out var anchors)
                ? anchors
                : null;

        /// <summary>
        /// The windup made unmistakable (visual item 4): up to the moment the blow commits the body
        /// sinks and leans back, the weapon goes further back and the eyes flare; it all lets go
        /// within a few frames of the strike. <paramref name="peak"/> is where in the clip that is.
        /// </summary>
        private static Pose Anticipate(Pose pose, float t, float peak, bool moveArms = true)
        {
            var w = Windup(t, peak);
            pose.HipY -= 5f * w;
            pose.Lean -= 5f * w;
            pose.FrontThigh -= 4f * w;
            pose.BackThigh += 6f * w;
            if (moveArms)
            {
                pose.FrontArm -= 12f * w;
                pose.SwordAngle -= 14f * w;
            }

            pose.EyeFlare = w;
            return pose;
        }

        private static float Windup(float t, float peak) =>
            t <= peak ? Ease(Mathf.Clamp01(t / Mathf.Max(0.01f, peak)))
                : 1f - Ease(Mathf.Clamp01((t - peak) / 0.12f));

        /// <summary>Where each ordinary attack commits, as a share of its clip (from its pose curves).</summary>
        private static float PeakOf(Actor actor, EnemyAnimationState state)
        {
            switch (state)
            {
                case EnemyAnimationState.AttackLight:
                    return actor == Actor.Archer ? 0.72f : actor == Actor.Shaman ? 0.7f
                        : actor == Actor.Spearman ? 0.42f : 0.3f;
                case EnemyAnimationState.AttackHeavy:
                    return actor == Actor.Shaman ? 0.78f : 0.3f;
                case EnemyAnimationState.ShieldBash:
                    return actor == Actor.Archer ? 0.28f : actor == Actor.Spearman ? 0.42f : 0.25f;
                default:
                    return -1f;
            }
        }

        /// <summary>The pose, with the windup made clearer on attacks.</summary>
        private static Pose Telegraphed(Actor actor, EnemyAnimationState state, float t, Pose pose)
        {
            var peak = PeakOf(actor, state);
            if (peak < 0f)
            {
                return pose;
            }

            // A bow or a staff is held where it is aimed: only the body and the eyes tell.
            var armed = actor != Actor.Archer && actor != Actor.Shaman;
            return Anticipate(pose, t, peak, armed || state == EnemyAnimationState.ShieldBash);
        }
        private static readonly Color32 Steel = new Color32(0x3E, 0x42, 0x47, 0xFF);
        private static readonly Color32 DarkSteel = new Color32(0x2A, 0x2D, 0x31, 0xFF);
        private static readonly Color32 Navy = new Color32(0x2F, 0x3E, 0x52, 0xFF);
        private static readonly Color32 Rust = new Color32(0x4A, 0x3B, 0x2E, 0xFF);
        private static readonly Color32 Flesh = new Color32(0x33, 0x45, 0x3B, 0xFF);
        private static readonly Color32 FadedGold = new Color32(0x8A, 0x74, 0x34, 0xFF);
        private static readonly Color32 Crimson = new Color32(0x3A, 0x16, 0x20, 0xFF);
        private static readonly Color32 RuneCold = new Color32(0x6F, 0x8E, 0xC4, 0xFF);
        private static readonly Color32 RuneDark = new Color32(0x7B, 0x3D, 0xC4, 0xFF);

        // The Withered Forest's palette, from the Visual Bible's zone two entry. Deliberately
        // desaturated: the forest is wet and dying, not a spring meadow.
        private static readonly Color32 BarkDark = new Color32(0x2A, 0x24, 0x1E, 0xFF);
        private static readonly Color32 MossGreen = new Color32(0x36, 0x47, 0x33, 0xFF);
        private static readonly Color32 WetBrown = new Color32(0x3B, 0x30, 0x25, 0xFF);
        private static readonly Color32 PoisonGreen = new Color32(0x7B, 0xB0, 0x45, 0xFF);
        private static readonly Color32 Teal = new Color32(0x2C, 0x43, 0x45, 0xFF);
        private static readonly Color32 HuntersBlue = new Color32(0x36, 0x44, 0x55, 0xFF);
        private static readonly Color32 BoneWhite = new Color32(0x9A, 0x96, 0x86, 0xFF);
        private static readonly Color32 CorruptViolet = new Color32(0x6B, 0x3A, 0x8E, 0xFF);

        /// <summary>Which actor is being drawn; selects the build and the pose table.</summary>
        private enum Actor
        {
            Zombie,
            Guard,
            BossPhaseOne,
            BossPhaseTwo,
            // Withered Forest. Appended so the Tomb's four keep their meaning.
            Hound,
            Archer,
            Shaman,
            Spearman,
            WardenPhaseOne,
            WardenPhaseTwo
        }

        /// <summary>
        /// What hangs from the front hand. The Tomb only ever needed a blade; the forest needs a
        /// bow, a staff and a polearm, and a spear drawn as a long thin sword reads as a mistake.
        /// </summary>
        private enum WeaponShape { Blade, Bow, Staff, Spear, Glaive, Maul }

        /// <summary>What the actor has instead of a visored helm.</summary>
        private enum HeadGear { Helm, Hood, Mask, Antlers }

        /// <summary>
        /// Proportions and wardrobe. Scale comes from the real capsules, which are gameplay and are
        /// not touched: zombie and guard share 0.85 × 1.7 units and differ by silhouette alone,
        /// exactly as the Visual Bible decided.
        /// </summary>
        private sealed class Build
        {
            public int Width;
            public int Height;
            public float HipY;
            public float Torso;
            public float Neck;
            public float Thigh;
            public float Shin;
            public float LegWidth;
            public float Arm;
            public float Forearm;
            public float ArmWidth;
            public float TorsoWidth;
            public float HeadRx;
            public float HeadRy;
            public float SwordLength;
            public float SwordWidth;
            public Color32 Armor;
            public Color32 Cloth;
            public Color32 Metal;
            public Color32 Trim;
            public Color32 Skin;
            public Color32 Rot;
            public bool Shield;
            public float ShieldWidth;
            public float ShieldHeight;
            public bool Cape;
            public bool Crown;
            public bool Runes;
            public WeaponShape Weapon;
            public HeadGear Head;
            /// <summary>Flared lower torso: what turns a soldier's outline into a robed one.</summary>
            public float RobeFlare;
            /// <summary>Corruption growth drawn over the body; the forest's mark on everything.</summary>
            public bool Growth;
            public Color32 GrowthColor;
        }

        private static Build BuildFor(Actor actor)
        {
            switch (actor)
            {
                case Actor.Zombie:
                    // 0.85 × 1.7 units = 85 × 170 px. Narrow and simple: at a distance the player
                    // must be able to tell it from the guard by width alone.
                    return new Build
                    {
                        // Wide enough that the deep windup keeps the whole sword on the canvas:
                        // the frame the telegraph depends on is the one that must not be clipped.
                        Width = 140, Height = 176, HipY = 82f, Torso = 46f, Neck = 15f,
                        Thigh = 26f, Shin = 26f, LegWidth = 9f,
                        Arm = 21f, Forearm = 19f, ArmWidth = 6.5f,
                        TorsoWidth = 25f, HeadRx = 12f, HeadRy = 14f,
                        SwordLength = 58f, SwordWidth = 5f,
                        Armor = Rust, Cloth = Crimson, Metal = Steel, Trim = FadedGold,
                        Skin = Flesh, Rot = Crimson
                    };

                case Actor.Guard:
                    // The same capsule as the zombie. Every bit of extra presence is silhouette:
                    // broader shoulders, heavier plate, and a shield that leaves the capsule.
                    return new Build
                    {
                        Width = 168, Height = 176, HipY = 84f, Torso = 48f, Neck = 13f,
                        Thigh = 27f, Shin = 27f, LegWidth = 11f,
                        Arm = 22f, Forearm = 20f, ArmWidth = 8.5f,
                        TorsoWidth = 34f, HeadRx = 13f, HeadRy = 14f,
                        SwordLength = 62f, SwordWidth = 6f,
                        Armor = Steel, Cloth = Navy, Metal = DarkSteel, Trim = FadedGold,
                        Skin = Flesh, Rot = Crimson,
                        Shield = true, ShieldWidth = 44f, ShieldHeight = 70f
                    };

                case Actor.BossPhaseOne:
                    // 1.4 × 2.6 units = 140 × 260 px. Height above the capsule comes from the
                    // crown and the cape, as the Visual Bible ruled — the capsule is gameplay.
                    return new Build
                    {
                        Width = 272, Height = 300, HipY = 126f, Torso = 76f, Neck = 20f,
                        Thigh = 44f, Shin = 44f, LegWidth = 18f,
                        Arm = 36f, Forearm = 32f, ArmWidth = 14f,
                        TorsoWidth = 54f, HeadRx = 20f, HeadRy = 22f,
                        SwordLength = 112f, SwordWidth = 11f,
                        Armor = Steel, Cloth = Navy, Metal = DarkSteel, Trim = FadedGold,
                        Skin = Charcoal, Rot = RuneCold,
                        Shield = true, ShieldWidth = 68f, ShieldHeight = 112f,
                        Cape = true, Crown = true, Runes = true
                    };

                case Actor.BossPhaseTwo:
                    // Phase two: the shield is gone, the stance opens, the runes turn from cold
                    // blue to corrupt violet. Same body, visibly different creature.
                    var two = BuildFor(Actor.BossPhaseOne);
                    two.Shield = false;
                    two.Rot = RuneDark;
                    two.Cloth = Crimson;
                    return two;

                default:
                    return ForestBuildFor(actor);
            }
        }

        /// <summary>One authored state: how many frames and how fast.</summary>
        public readonly struct Spec<TState>
        {
            public readonly TState State;
            public readonly int Frames;
            public readonly float Fps;
            public readonly bool Loops;

            public Spec(TState state, int frames, float fps, bool loops)
            {
                State = state;
                Frames = frames;
                Fps = fps;
                Loops = loops;
            }
        }

        /// <summary>
        /// Frame budgets taken straight from `ANIMATION_LIST.md`, which derived them from the real
        /// attack data. Using the artist's own budget means the placeholder and the final set are
        /// the same shape, and a replacement drops in file for file.
        /// </summary>
        public static readonly Spec<EnemyAnimationState>[] ZombieStates =
        {
            new Spec<EnemyAnimationState>(EnemyAnimationState.Idle, 6, 8f, true),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Patrol, 8, 12f, true),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Alert, 5, 12f, false),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Chase, 8, 12f, true),
            new Spec<EnemyAnimationState>(EnemyAnimationState.AttackLight, 18, 24f, false),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Hurt, 5, 24f, false),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Stunned, 6, 12f, true),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Death, 10, 12f, false)
        };

        public static readonly Spec<EnemyAnimationState>[] GuardStates =
        {
            new Spec<EnemyAnimationState>(EnemyAnimationState.Idle, 6, 8f, true),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Patrol, 8, 12f, true),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Alert, 5, 12f, false),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Chase, 8, 12f, true),
            new Spec<EnemyAnimationState>(EnemyAnimationState.GuardRaise, 5, 24f, false),
            new Spec<EnemyAnimationState>(EnemyAnimationState.GuardIdle, 4, 12f, true),
            new Spec<EnemyAnimationState>(EnemyAnimationState.GuardImpact, 5, 24f, false),
            new Spec<EnemyAnimationState>(EnemyAnimationState.GuardBreak, 12, 12f, false),
            new Spec<EnemyAnimationState>(EnemyAnimationState.AttackLight, 20, 24f, false),
            new Spec<EnemyAnimationState>(EnemyAnimationState.ShieldBash, 16, 24f, false),
            new Spec<EnemyAnimationState>(EnemyAnimationState.AttackHeavy, 26, 24f, false),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Hurt, 5, 24f, false),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Stunned, 6, 12f, true),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Death, 12, 12f, false)
        };

        public static readonly Spec<BossAnimationState>[] BossStates =
        {
            new Spec<BossAnimationState>(BossAnimationState.Dormant, 4, 8f, true),
            new Spec<BossAnimationState>(BossAnimationState.Intro, 16, 12f, false),
            new Spec<BossAnimationState>(BossAnimationState.IdleP1, 6, 8f, true),
            new Spec<BossAnimationState>(BossAnimationState.WalkP1, 8, 12f, true),
            new Spec<BossAnimationState>(BossAnimationState.SlashP1, 20, 24f, false),
            new Spec<BossAnimationState>(BossAnimationState.ShieldBash, 16, 24f, false),
            new Spec<BossAnimationState>(BossAnimationState.ChargeStart, 14, 24f, false),
            new Spec<BossAnimationState>(BossAnimationState.Charge, 6, 24f, true),
            new Spec<BossAnimationState>(BossAnimationState.ChargeRecover, 10, 24f, false),
            new Spec<BossAnimationState>(BossAnimationState.Combo1, 10, 24f, false),
            new Spec<BossAnimationState>(BossAnimationState.Combo2, 10, 24f, false),
            new Spec<BossAnimationState>(BossAnimationState.Combo3, 14, 24f, false),
            new Spec<BossAnimationState>(BossAnimationState.Stunned, 8, 12f, true),
            new Spec<BossAnimationState>(BossAnimationState.Recoil, 6, 24f, false),
            new Spec<BossAnimationState>(BossAnimationState.PhaseTransition, 24, 12f, false),
            new Spec<BossAnimationState>(BossAnimationState.IdleP2, 6, 8f, true),
            new Spec<BossAnimationState>(BossAnimationState.MoveP2, 8, 12f, true),
            new Spec<BossAnimationState>(BossAnimationState.SlashP2, 18, 24f, false),
            new Spec<BossAnimationState>(BossAnimationState.ComboP2, 16, 24f, false),
            new Spec<BossAnimationState>(BossAnimationState.JumpStart, 12, 24f, false),
            new Spec<BossAnimationState>(BossAnimationState.Jump, 6, 24f, true),
            new Spec<BossAnimationState>(BossAnimationState.Slam, 4, 24f, false),
            new Spec<BossAnimationState>(BossAnimationState.SlamRecover, 12, 24f, false),
            new Spec<BossAnimationState>(BossAnimationState.Death, 24, 12f, false)
        };

        // ---------- generation ----------

        public static Dictionary<EnemyAnimationState, Sprite[]> GenerateZombie() =>
            Generate(ZombieFolder, "enemy_zombie", ZombieStates,
                (state, t) => Render(Actor.Zombie,
                    Telegraphed(Actor.Zombie, state, t, EnemyPose(Actor.Zombie, state, t))));

        public static Dictionary<EnemyAnimationState, Sprite[]> GenerateGuard() =>
            Generate(GuardFolder, "enemy_shieldguard", GuardStates,
                (state, t) => Render(Actor.Guard,
                    Telegraphed(Actor.Guard, state, t, EnemyPose(Actor.Guard, state, t))));

        public static Dictionary<BossAnimationState, Sprite[]> GenerateBoss() =>
            Generate(BossFolder, "boss_guardian", BossStates,
                (state, t) => Render(ActorForBossState(state), BossPose(state, t)));

        /// <summary>
        /// Which build a boss state is drawn with. Everything from the transition onward loses the
        /// shield, which is what makes phase two readable at a glance.
        /// </summary>
        private static Actor ActorForBossState(BossAnimationState state)
        {
            switch (state)
            {
                case BossAnimationState.IdleP2:
                case BossAnimationState.MoveP2:
                case BossAnimationState.SlashP2:
                case BossAnimationState.ComboP2:
                case BossAnimationState.JumpStart:
                case BossAnimationState.Jump:
                case BossAnimationState.Slam:
                case BossAnimationState.SlamRecover:
                case BossAnimationState.Death:
                    return Actor.BossPhaseTwo;
                default:
                    return Actor.BossPhaseOne;
            }
        }

        private static Dictionary<TState, Sprite[]> Generate<TState>(string rootFolder,
            string prefix, Spec<TState>[] specs, System.Func<TState, float, byte[]> render)
        {
            PlaceholderImport.EnsureFolder(rootFolder);
            var result = new Dictionary<TState, Sprite[]>();
            foreach (var spec in specs)
            {
                var folder = $"{rootFolder}/{spec.State}";
                PlaceholderImport.EnsureFolder(folder);
                var name = $"{prefix}_{spec.State.ToString().ToLowerInvariant()}";
                var sprites = new Sprite[spec.Frames];
                var anchors = new Vector3[spec.Frames];
                var anchored = true;
                for (var i = 0; i < spec.Frames; i++)
                {
                    var path = $"{folder}/{name}_{i:00}.png";
                    // A loop divides the full turn so the last frame leads back to the first; a
                    // one-shot spans end to end so its final pose is actually reached.
                    var t = spec.Loops
                        ? i / (float)spec.Frames
                        : (spec.Frames <= 1 ? 0f : i / (float)(spec.Frames - 1));
                    _frameAnchor = null;
                    var png = render(spec.State, t);
                    if (_frameAnchor.HasValue)
                    {
                        // Canvas pixels to units from the pivot, which sits at the feet, centred.
                        var a = _frameAnchor.Value;
                        anchors[i] = new Vector3((a.x - _frameWidth * 0.5f) / PlaceholderImport.PixelsPerUnit,
                            a.y / PlaceholderImport.PixelsPerUnit, a.z);
                    }
                    else
                    {
                        anchored = false;
                    }

                    PlaceholderImport.WriteIfChanged(path, png);
                    PlaceholderImport.ConfigureImporter(path);
                    sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (sprites[i] == null)
                    {
                        Debug.LogError($"Placeholder frame failed to import: {path}");
                    }
                }

                result[spec.State] = sprites;
                if (anchored)
                {
                    FrameAnchors[name] = anchors;
                }
                else
                {
                    FrameAnchors.Remove(name);
                }
            }

            return result;
        }

        private static float _frameWidth;

        /// <summary>Records the hanging point for the frame being drawn (see <see cref="_frameAnchor"/>).</summary>
        private static void Anchor(Canvas canvas, Vector2 at, float angle)
        {
            _frameWidth = canvas.Width;
            _frameAnchor = new Vector3(at.x, at.y, angle);
        }

        /// <summary>The hang angle (degrees from straight down, positive towards the back) of a direction.</summary>
        private static float HangAngle(Vector2 direction) =>
            Mathf.Atan2(-direction.x, -direction.y) * Mathf.Rad2Deg;

        // ---------- posing ----------

        /// <summary>
        /// Everything that separates one frame from another. Limb angles are degrees, zero hanging
        /// straight down, positive swinging forward towards the facing direction.
        /// </summary>
        private struct Pose
        {
            public float Lean;
            public float HipY;
            public float HipX;
            public float FrontThigh;
            public float FrontShin;
            public float BackThigh;
            public float BackShin;
            public float FrontArm;
            public float FrontForearm;
            public float BackArm;
            public float BackForearm;
            public float HeadTilt;
            public float SwordAngle;
            public float ShieldAngle;
            public float ShieldDrop;
            public float CapeSway;
            public float Squash;
            public float Alpha;
            public float Glow;
            public float EyeFlare;

            public static Pose Stand => new Pose
            {
                Lean = 0f, HipY = 0f, HipX = 0f,
                FrontThigh = 4f, FrontShin = -5f, BackThigh = -5f, BackShin = -6f,
                FrontArm = 14f, FrontForearm = 22f, BackArm = -10f, BackForearm = 16f,
                HeadTilt = 0f, SwordAngle = 20f, ShieldAngle = 0f, ShieldDrop = 0f,
                CapeSway = 0f, Squash = 1f, Alpha = 1f, Glow = 0f
            };
        }

        private static float Wave(float t) => Mathf.Sin(t * Mathf.PI * 2f);

        private static float Ease(float t) => t * t * (3f - 2f * t);

        /// <summary>
        /// A swing in three beats: wind up, commit, follow through. The strike lands around a third
        /// of the way in, near where the active window sits in the attack data; the debug tools
        /// show the real window so this is checked rather than assumed.
        /// </summary>
        private static Pose Swing(float t, float windupArm, float strikeArm, float lean,
            float windupSword, float strikeSword)
        {
            var pose = Pose.Stand;
            var commit = Ease(Mathf.Clamp01((t - 0.3f) / 0.4f));
            pose.Lean = Mathf.Lerp(-lean * 0.55f, lean, commit);
            pose.HipX = Mathf.Lerp(-3f, 5f, commit);
            pose.FrontArm = Mathf.Lerp(windupArm, strikeArm, commit);
            pose.FrontForearm = Mathf.Lerp(30f, -8f, commit);
            pose.BackArm = Mathf.Lerp(-windupArm * 0.2f, -strikeArm * 0.25f, commit);
            pose.SwordAngle = Mathf.Lerp(windupSword, strikeSword, commit);
            pose.FrontThigh = Mathf.Lerp(-8f, 22f, commit);
            pose.FrontShin = Mathf.Lerp(-10f, -24f, commit);
            pose.BackThigh = Mathf.Lerp(10f, -26f, commit);
            pose.BackShin = Mathf.Lerp(-14f, -22f, commit);
            pose.HeadTilt = Mathf.Lerp(-4f, 7f, commit);
            pose.CapeSway = Mathf.Lerp(16f, -24f, commit);
            return pose;
        }

        private static Pose EnemyPose(Actor actor, EnemyAnimationState state, float t)
        {
            var pose = Pose.Stand;
            var isZombie = actor == Actor.Zombie;
            switch (state)
            {
                case EnemyAnimationState.Idle:
                    // The zombie's idle has to say "dead but upright": a slack arm and an uneven
                    // sway, against the guard's disciplined, near-still stance.
                    if (isZombie)
                    {
                        pose.Lean = 9f + Wave(t) * 2.5f;
                        pose.HipY = Wave(t) * 1.2f;
                        pose.FrontArm = 22f + Wave(t) * 7f;
                        pose.FrontForearm = 34f;
                        pose.BackArm = -4f + Wave(t * 0.5f) * 5f;
                        pose.HeadTilt = 11f + Wave(t) * 4f;
                        pose.SwordAngle = 68f;
                    }
                    else
                    {
                        pose.Lean = 3f;
                        pose.HipY = Wave(t) * 0.8f;
                        pose.FrontArm = 10f;
                        pose.ShieldAngle = -18f;
                        pose.HeadTilt = Wave(t) * 1.5f;
                    }

                    break;

                case EnemyAnimationState.Patrol:
                    // A dead soldier's march: stiffer and shorter-strided than a living walk. The
                    // guard's is heavier and more deliberate instead.
                    pose.Lean = isZombie ? 11f : 5f;
                    pose.HipY = Mathf.Abs(Wave(t)) * (isZombie ? 2f : 2.6f);
                    pose.FrontThigh = Wave(t) * (isZombie ? 22f : 20f);
                    pose.BackThigh = -Wave(t) * (isZombie ? 22f : 20f);
                    pose.FrontShin = -Mathf.Max(0f, -Wave(t)) * 26f - 6f;
                    pose.BackShin = -Mathf.Max(0f, Wave(t)) * 26f - 6f;
                    pose.FrontArm = isZombie ? 26f + Wave(t) * 5f : 12f;
                    pose.BackArm = isZombie ? -Wave(t) * 10f : -Wave(t) * 16f;
                    pose.HeadTilt = isZombie ? 12f : 0f;
                    pose.SwordAngle = isZombie ? 66f : 40f;
                    pose.ShieldAngle = isZombie ? 0f : -26f;
                    break;

                case EnemyAnimationState.Alert:
                    // The one beat that tells the player it has been noticed: head up, weapon up,
                    // body turned in. It must read in well under half a second.
                    pose.Lean = Mathf.Lerp(isZombie ? 11f : 5f, isZombie ? 4f : 8f, Ease(t));
                    pose.HeadTilt = Mathf.Lerp(isZombie ? 12f : 0f, -8f, Ease(t));
                    pose.FrontArm = Mathf.Lerp(20f, -26f, Ease(t));
                    pose.FrontForearm = Mathf.Lerp(30f, 44f, Ease(t));
                    pose.SwordAngle = Mathf.Lerp(66f, 8f, Ease(t));
                    pose.ShieldAngle = Mathf.Lerp(-20f, -52f, Ease(t));
                    pose.BackArm = Mathf.Lerp(-8f, -20f, Ease(t));
                    break;

                case EnemyAnimationState.Chase:
                    pose.Lean = isZombie ? 17f : 12f;
                    pose.HipY = Mathf.Abs(Wave(t)) * 3f;
                    pose.FrontThigh = Wave(t) * (isZombie ? 32f : 28f);
                    pose.BackThigh = -Wave(t) * (isZombie ? 32f : 28f);
                    pose.FrontShin = -Mathf.Max(0f, -Wave(t)) * 38f - 6f;
                    pose.BackShin = -Mathf.Max(0f, Wave(t)) * 38f - 6f;
                    pose.FrontArm = isZombie ? -14f - Wave(t) * 8f : -6f;
                    pose.BackArm = isZombie ? Wave(t) * 22f : Wave(t) * 18f;
                    pose.SwordAngle = isZombie ? 26f : 16f;
                    pose.ShieldAngle = isZombie ? 0f : -44f;
                    pose.HeadTilt = isZombie ? 6f : -2f;
                    break;

                case EnemyAnimationState.AttackLight:
                    // The zombie pulls the sword right back. This is the game's longest read and
                    // the brief is explicit that a thin windup would break the fight.
                    return isZombie
                        ? Swing(t, -96f, 64f, 16f, -104f, 74f)
                        : Swing(t, -74f, 56f, 12f, -80f, 62f);

                case EnemyAnimationState.AttackHeavy:
                    // The longest telegraph an ordinary enemy has: it must look dangerous before
                    // it lands, not as it lands.
                    return Swing(t, -116f, 86f, 26f, -124f, 96f);

                case EnemyAnimationState.ShieldBash:
                    // Driven by the shield, never by the sword, so it cannot be mistaken for the
                    // guard simply shifting its stance.
                    pose.Lean = Mathf.Lerp(-10f, 24f, Ease(Mathf.Clamp01((t - 0.25f) / 0.35f)));
                    pose.HipX = Mathf.Lerp(-6f, 9f, Ease(Mathf.Clamp01((t - 0.25f) / 0.35f)));
                    pose.ShieldAngle = Mathf.Lerp(28f, -78f,
                        Ease(Mathf.Clamp01((t - 0.25f) / 0.35f)));
                    pose.FrontArm = 6f;
                    pose.BackArm = Mathf.Lerp(24f, -34f, Ease(t));
                    pose.SwordAngle = 48f;
                    pose.FrontThigh = 20f;
                    pose.FrontShin = -24f;
                    pose.BackThigh = -24f;
                    pose.BackShin = -20f;
                    break;

                case EnemyAnimationState.GuardRaise:
                    pose.Lean = Mathf.Lerp(4f, 10f, Ease(t));
                    pose.ShieldAngle = Mathf.Lerp(-20f, -62f, Ease(t));
                    pose.FrontArm = Mathf.Lerp(12f, 2f, Ease(t));
                    pose.SwordAngle = Mathf.Lerp(40f, 58f, Ease(t));
                    pose.FrontThigh = 16f;
                    pose.FrontShin = -20f;
                    pose.BackThigh = -18f;
                    pose.BackShin = -18f;
                    break;

                case EnemyAnimationState.GuardIdle:
                    pose.Lean = 10f;
                    pose.HipY = Wave(t) * 0.6f;
                    pose.ShieldAngle = -62f;
                    pose.FrontArm = 2f;
                    pose.SwordAngle = 58f;
                    pose.FrontThigh = 16f;
                    pose.FrontShin = -20f;
                    pose.BackThigh = -18f;
                    pose.BackShin = -18f;
                    break;

                case EnemyAnimationState.GuardImpact:
                    // The shield absorbs and returns. The guard never leaves its stance here, and
                    // the pose must not suggest otherwise.
                    var recoil = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
                    pose.Lean = 10f - recoil * 9f;
                    pose.HipX = -recoil * 6f;
                    pose.ShieldAngle = -62f + recoil * 16f;
                    pose.FrontArm = 2f + recoil * 8f;
                    pose.SwordAngle = 58f;
                    pose.FrontThigh = 18f + recoil * 6f;
                    pose.FrontShin = -22f;
                    pose.BackThigh = -20f - recoil * 8f;
                    pose.BackShin = -18f;
                    break;

                case EnemyAnimationState.GuardBreak:
                    // The punish window. The shield is knocked aside and down, the chest opens and
                    // the sword arm is thrown out: the player has to recognise this instantly.
                    pose.Lean = Mathf.Lerp(8f, -26f, Ease(t));
                    pose.HipX = -Mathf.Lerp(0f, 12f, Ease(t));
                    pose.HipY = -Mathf.Lerp(0f, 8f, Ease(t));
                    pose.ShieldAngle = Mathf.Lerp(-58f, 48f, Ease(t));
                    pose.ShieldDrop = Mathf.Lerp(0f, 26f, Ease(t));
                    pose.FrontArm = Mathf.Lerp(4f, -46f, Ease(t));
                    pose.BackArm = Mathf.Lerp(-10f, -56f, Ease(t));
                    pose.SwordAngle = Mathf.Lerp(58f, 116f, Ease(t));
                    pose.HeadTilt = Mathf.Lerp(0f, 22f, Ease(t));
                    pose.FrontThigh = 30f;
                    pose.FrontShin = -36f;
                    pose.BackThigh = -26f;
                    pose.BackShin = -30f;
                    break;

                case EnemyAnimationState.Hurt:
                    // Short. A dramatic flinch on every light hit would make the fight read as if
                    // the enemy were staggered when it is not.
                    var flinch = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
                    pose.Lean = -flinch * 16f;
                    pose.HipX = -flinch * 5f;
                    pose.HeadTilt = flinch * 12f;
                    pose.FrontArm = 16f - flinch * 30f;
                    pose.BackArm = -10f - flinch * 24f;
                    pose.SwordAngle = 40f + flinch * 20f;
                    pose.ShieldAngle = -14f;
                    break;

                case EnemyAnimationState.Stunned:
                    // Balance gone, weapon down, chest open: vulnerability has to be obvious.
                    pose.Lean = -22f + Wave(t) * 6f;
                    pose.HipY = -7f;
                    pose.HipX = Wave(t) * 4f;
                    pose.HeadTilt = 24f + Wave(t) * 5f;
                    pose.FrontArm = -34f + Wave(t) * 9f;
                    pose.BackArm = -40f - Wave(t) * 9f;
                    pose.SwordAngle = 104f;
                    pose.ShieldAngle = 34f;
                    pose.ShieldDrop = 20f;
                    pose.FrontThigh = 22f;
                    pose.FrontShin = -30f;
                    pose.BackThigh = -22f;
                    pose.BackShin = -28f;
                    break;

                case EnemyAnimationState.Death:
                    // Down to the knees, then over. Armour makes the guard's collapse heavier and
                    // its weapon fall away sooner.
                    var fall = Ease(t);
                    pose.Squash = Mathf.Lerp(1f, 0.52f, fall);
                    pose.HipY = -Mathf.Lerp(0f, 62f, t * t);
                    pose.HipX = -Mathf.Lerp(0f, isZombie ? 16f : 22f, fall);
                    pose.Lean = Mathf.Lerp(-8f, -78f, fall);
                    pose.FrontThigh = Mathf.Lerp(18f, 66f, fall);
                    pose.FrontShin = Mathf.Lerp(-22f, -74f, fall);
                    pose.BackThigh = Mathf.Lerp(-16f, 44f, fall);
                    pose.BackShin = Mathf.Lerp(-22f, -70f, fall);
                    pose.FrontArm = Mathf.Lerp(-18f, -78f, fall);
                    pose.BackArm = Mathf.Lerp(-24f, -84f, fall);
                    pose.HeadTilt = Mathf.Lerp(8f, 44f, fall);
                    pose.SwordAngle = Mathf.Lerp(60f, 140f, Ease(Mathf.Clamp01(t * 1.8f)));
                    pose.ShieldAngle = Mathf.Lerp(-10f, 62f, Ease(Mathf.Clamp01(t * 1.6f)));
                    pose.ShieldDrop = Mathf.Lerp(0f, 34f, fall);
                    break;
            }

            return pose;
        }

        private static Pose BossPose(BossAnimationState state, float t)
        {
            var pose = Pose.Stand;
            switch (state)
            {
                case BossAnimationState.Dormant:
                    // A statue that turns out not to be one: almost no motion, weapon grounded,
                    // head down. The only tell is the faintest rune pulse.
                    pose.Lean = 4f;
                    pose.HeadTilt = 20f;
                    pose.FrontArm = 4f;
                    pose.BackArm = 2f;
                    pose.SwordAngle = 88f;
                    pose.ShieldAngle = -6f;
                    pose.Glow = 0.12f + Wave(t) * 0.05f;
                    pose.FrontThigh = 3f;
                    pose.FrontShin = -4f;
                    pose.BackThigh = -3f;
                    pose.BackShin = -4f;
                    break;

                case BossAnimationState.Intro:
                    // Head rises, runes light, weapon and shield come up. No cinematic — the
                    // encounter's own timing owns this, the animation only fills it.
                    pose.Lean = Mathf.Lerp(4f, 6f, Ease(t));
                    pose.HeadTilt = Mathf.Lerp(20f, -6f, Ease(t));
                    pose.SwordAngle = Mathf.Lerp(88f, 34f, Ease(t));
                    pose.ShieldAngle = Mathf.Lerp(-6f, -48f, Ease(t));
                    pose.FrontArm = Mathf.Lerp(4f, 8f, Ease(t));
                    pose.BackArm = Mathf.Lerp(2f, -16f, Ease(t));
                    pose.Glow = Mathf.Lerp(0.12f, 1f, Ease(t));
                    pose.CapeSway = Mathf.Lerp(0f, 10f, Ease(t));
                    break;

                case BossAnimationState.IdleP1:
                    // Disciplined and experienced, not tense. Deliberately unlike the Shield
                    // Guard's braced crouch: this one has nothing to prove.
                    pose.Lean = 5f;
                    pose.HipY = Wave(t) * 1f;
                    pose.ShieldAngle = -46f;
                    pose.SwordAngle = 34f;
                    pose.FrontArm = 8f;
                    pose.CapeSway = Wave(t) * 4f;
                    pose.Glow = 0.85f;
                    break;

                case BossAnimationState.WalkP1:
                    pose.Lean = 7f;
                    pose.HipY = Mathf.Abs(Wave(t)) * 3.4f;
                    pose.FrontThigh = Wave(t) * 20f;
                    pose.BackThigh = -Wave(t) * 20f;
                    pose.FrontShin = -Mathf.Max(0f, -Wave(t)) * 24f - 6f;
                    pose.BackShin = -Mathf.Max(0f, Wave(t)) * 24f - 6f;
                    pose.ShieldAngle = -44f;
                    pose.SwordAngle = 36f;
                    pose.CapeSway = -8f + Wave(t) * 4f;
                    pose.Glow = 0.85f;
                    break;

                case BossAnimationState.SlashP1:
                    pose = Swing(t, -92f, 70f, 18f, -100f, 80f);
                    pose.ShieldAngle = -34f;
                    pose.Glow = 0.85f;
                    break;

                case BossAnimationState.ShieldBash:
                    pose.Lean = Mathf.Lerp(-12f, 26f, Ease(Mathf.Clamp01((t - 0.3f) / 0.35f)));
                    pose.HipX = Mathf.Lerp(-8f, 12f, Ease(Mathf.Clamp01((t - 0.3f) / 0.35f)));
                    pose.ShieldAngle = Mathf.Lerp(24f, -84f,
                        Ease(Mathf.Clamp01((t - 0.3f) / 0.35f)));
                    pose.SwordAngle = 52f;
                    pose.BackArm = Mathf.Lerp(20f, -32f, Ease(t));
                    pose.FrontThigh = 22f;
                    pose.FrontShin = -26f;
                    pose.BackThigh = -26f;
                    pose.BackShin = -22f;
                    pose.Glow = 0.85f;
                    break;

                case BossAnimationState.ChargeStart:
                    // The most important telegraph in the game. The stance drops, shield and sword
                    // line up, and the whole body aims: the player must know this is coming.
                    pose.Lean = Mathf.Lerp(2f, 32f, Ease(t));
                    pose.HipY = -Mathf.Lerp(0f, 14f, Ease(t));
                    pose.ShieldAngle = Mathf.Lerp(-40f, -86f, Ease(t));
                    pose.SwordAngle = Mathf.Lerp(34f, 4f, Ease(t));
                    pose.FrontThigh = Mathf.Lerp(8f, 34f, Ease(t));
                    pose.FrontShin = Mathf.Lerp(-10f, -40f, Ease(t));
                    pose.BackThigh = Mathf.Lerp(-8f, -40f, Ease(t));
                    pose.BackShin = Mathf.Lerp(-10f, -26f, Ease(t));
                    pose.HeadTilt = Mathf.Lerp(0f, -10f, Ease(t));
                    pose.CapeSway = Mathf.Lerp(0f, 22f, Ease(t));
                    pose.Glow = Mathf.Lerp(0.85f, 1.3f, Ease(t));
                    break;

                case BossAnimationState.Charge:
                    pose.Lean = 40f;
                    pose.HipY = -10f + Mathf.Abs(Wave(t)) * 3f;
                    pose.ShieldAngle = -88f;
                    pose.SwordAngle = 2f;
                    pose.FrontThigh = Wave(t) * 34f;
                    pose.BackThigh = -Wave(t) * 34f;
                    pose.FrontShin = -22f;
                    pose.BackShin = -22f;
                    pose.HeadTilt = -12f;
                    pose.CapeSway = -42f;
                    pose.Glow = 1.3f;
                    break;

                case BossAnimationState.ChargeRecover:
                    // The punish window, and it has to look like one: overextended, shield out of
                    // position, slow to gather itself.
                    pose.Lean = Mathf.Lerp(38f, 6f, Ease(t));
                    pose.HipY = Mathf.Lerp(-16f, 0f, Ease(t));
                    pose.HipX = Mathf.Lerp(10f, 0f, Ease(t));
                    pose.ShieldAngle = Mathf.Lerp(-30f, -44f, Ease(t));
                    pose.SwordAngle = Mathf.Lerp(14f, 34f, Ease(t));
                    pose.FrontThigh = Mathf.Lerp(40f, 6f, Ease(t));
                    pose.FrontShin = Mathf.Lerp(-46f, -8f, Ease(t));
                    pose.BackThigh = Mathf.Lerp(-42f, -6f, Ease(t));
                    pose.BackShin = Mathf.Lerp(-30f, -8f, Ease(t));
                    pose.HeadTilt = Mathf.Lerp(16f, 0f, Ease(t));
                    pose.CapeSway = Mathf.Lerp(30f, 0f, Ease(t));
                    pose.Glow = 0.85f;
                    break;

                // Three visibly different swings, so the player can count the sequence: a flat
                // horizontal, a reversed diagonal, then a committed finisher.
                case BossAnimationState.Combo1:
                    pose = Swing(t, -70f, 58f, 12f, -76f, 64f);
                    pose.ShieldAngle = -40f;
                    pose.Glow = 0.85f;
                    break;
                case BossAnimationState.Combo2:
                    pose = Swing(t, 78f, -58f, 16f, 86f, -64f);
                    pose.ShieldAngle = -30f;
                    pose.Glow = 0.9f;
                    break;
                case BossAnimationState.Combo3:
                    pose = Swing(t, -112f, 88f, 28f, -120f, 98f);
                    pose.ShieldAngle = -22f;
                    pose.Glow = 1f;
                    break;

                case BossAnimationState.Recoil:
                    // Parried. A controlled recoil, not a giant knockback — it is still the boss.
                    var jolt = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
                    pose.Lean = -jolt * 18f;
                    pose.HipX = -jolt * 7f;
                    pose.SwordAngle = 34f + jolt * 42f;
                    pose.ShieldAngle = -40f + jolt * 22f;
                    pose.FrontArm = 8f - jolt * 26f;
                    pose.HeadTilt = jolt * 10f;
                    pose.Glow = 0.85f;
                    break;

                case BossAnimationState.Stunned:
                    pose.Lean = -24f + Wave(t) * 6f;
                    pose.HipY = -14f;
                    pose.HeadTilt = 26f + Wave(t) * 5f;
                    pose.FrontArm = -36f;
                    pose.BackArm = -42f;
                    pose.SwordAngle = 112f;
                    pose.ShieldAngle = 30f;
                    pose.ShieldDrop = 26f;
                    pose.FrontThigh = 26f;
                    pose.FrontShin = -34f;
                    pose.BackThigh = -24f;
                    pose.BackShin = -30f;
                    pose.Glow = 0.4f;
                    break;

                case BossAnimationState.PhaseTransition:
                    // The moment the fight turns. It stops, the shield falls away, the armour
                    // cracks and the core lights: readable with no interface at all.
                    var drop = Ease(Mathf.Clamp01(t / 0.45f));
                    var rise = Ease(Mathf.Clamp01((t - 0.45f) / 0.55f));
                    pose.Lean = Mathf.Lerp(Mathf.Lerp(4f, -18f, drop), 14f, rise);
                    pose.HipY = Mathf.Lerp(-Mathf.Lerp(0f, 16f, drop), -4f, rise);
                    pose.ShieldAngle = Mathf.Lerp(-44f, 76f, drop);
                    pose.ShieldDrop = Mathf.Lerp(0f, 90f, drop);
                    pose.SwordAngle = Mathf.Lerp(Mathf.Lerp(34f, 96f, drop), -18f, rise);
                    pose.FrontArm = Mathf.Lerp(Mathf.Lerp(8f, -30f, drop), -48f, rise);
                    pose.BackArm = Mathf.Lerp(Mathf.Lerp(-12f, -36f, drop), -52f, rise);
                    pose.HeadTilt = Mathf.Lerp(Mathf.Lerp(0f, 26f, drop), -14f, rise);
                    pose.CapeSway = Mathf.Lerp(8f, 30f, drop);
                    pose.Glow = Mathf.Lerp(Mathf.Lerp(0.85f, 0.2f, drop), 1.6f, rise);
                    break;

                case BossAnimationState.IdleP2:
                    // Open and forward, where phase one was closed and braced. Without the shield
                    // the whole shape of the body has changed.
                    pose.Lean = 16f;
                    pose.HipY = Wave(t) * 1.2f;
                    pose.SwordAngle = -14f;
                    pose.FrontArm = -22f;
                    pose.BackArm = -28f;
                    pose.HeadTilt = -8f;
                    pose.CapeSway = Wave(t) * 6f;
                    pose.Glow = 1.5f;
                    break;

                case BossAnimationState.MoveP2:
                    pose.Lean = 22f;
                    pose.HipY = Mathf.Abs(Wave(t)) * 3.6f;
                    pose.FrontThigh = Wave(t) * 28f;
                    pose.BackThigh = -Wave(t) * 28f;
                    pose.FrontShin = -Mathf.Max(0f, -Wave(t)) * 30f - 6f;
                    pose.BackShin = -Mathf.Max(0f, Wave(t)) * 30f - 6f;
                    pose.SwordAngle = -10f;
                    pose.FrontArm = -20f;
                    pose.CapeSway = -14f;
                    pose.Glow = 1.5f;
                    break;

                case BossAnimationState.SlashP2:
                    pose = Swing(t, -104f, 82f, 24f, -112f, 92f);
                    pose.Glow = 1.5f;
                    pose.CapeSway = Mathf.Lerp(20f, -32f, Ease(t));
                    break;

                case BossAnimationState.ComboP2:
                    pose = Swing(t, -118f, 94f, 30f, -126f, 104f);
                    pose.Glow = 1.6f;
                    pose.CapeSway = Mathf.Lerp(24f, -38f, Ease(t));
                    break;

                case BossAnimationState.JumpStart:
                    // A deep, unmistakable crouch. The leap locks its landing spot at take-off, so
                    // this is the only warning the player gets.
                    pose.Squash = Mathf.Lerp(1f, 0.84f, Ease(t));
                    pose.HipY = -Mathf.Lerp(0f, 30f, Ease(t));
                    pose.Lean = Mathf.Lerp(14f, 30f, Ease(t));
                    pose.FrontThigh = Mathf.Lerp(10f, 44f, Ease(t));
                    pose.FrontShin = Mathf.Lerp(-12f, -66f, Ease(t));
                    pose.BackThigh = Mathf.Lerp(-10f, -40f, Ease(t));
                    pose.BackShin = Mathf.Lerp(-12f, -62f, Ease(t));
                    pose.SwordAngle = Mathf.Lerp(-10f, -86f, Ease(t));
                    pose.FrontArm = Mathf.Lerp(-20f, -64f, Ease(t));
                    pose.Glow = Mathf.Lerp(1.5f, 1.9f, Ease(t));
                    break;

                case BossAnimationState.Jump:
                    pose.Squash = 1.06f;
                    pose.HipY = 6f;
                    pose.Lean = 12f;
                    pose.FrontThigh = 34f;
                    pose.FrontShin = -44f;
                    pose.BackThigh = -20f;
                    pose.BackShin = -40f;
                    pose.SwordAngle = -96f + Wave(t) * 4f;
                    pose.FrontArm = -76f;
                    pose.BackArm = -60f;
                    pose.CapeSway = 34f;
                    pose.Glow = 1.9f;
                    break;

                case BossAnimationState.Slam:
                    // The impact frame. Everything drives downward, and the clip is fitted to the
                    // real active window so the visible impact and the damage cannot drift.
                    pose.Squash = Mathf.Lerp(0.94f, 0.86f, t);
                    pose.HipY = -Mathf.Lerp(18f, 26f, t);
                    pose.Lean = Mathf.Lerp(26f, 34f, t);
                    pose.FrontThigh = 46f;
                    pose.FrontShin = -62f;
                    pose.BackThigh = -40f;
                    pose.BackShin = -58f;
                    pose.SwordAngle = Mathf.Lerp(72f, 104f, t);
                    pose.FrontArm = Mathf.Lerp(52f, 74f, t);
                    pose.BackArm = 40f;
                    pose.CapeSway = -30f;
                    pose.Glow = 2f;
                    break;

                case BossAnimationState.SlamRecover:
                    // The largest opening in the fight, and it must look like it.
                    pose.Squash = Mathf.Lerp(0.86f, 1f, Ease(t));
                    pose.HipY = Mathf.Lerp(-26f, 0f, Ease(t));
                    pose.Lean = Mathf.Lerp(34f, 16f, Ease(t));
                    pose.FrontThigh = Mathf.Lerp(46f, 8f, Ease(t));
                    pose.FrontShin = Mathf.Lerp(-62f, -10f, Ease(t));
                    pose.BackThigh = Mathf.Lerp(-40f, -8f, Ease(t));
                    pose.BackShin = Mathf.Lerp(-58f, -10f, Ease(t));
                    pose.SwordAngle = Mathf.Lerp(104f, -12f, Ease(t));
                    pose.FrontArm = Mathf.Lerp(74f, -20f, Ease(t));
                    pose.HeadTilt = Mathf.Lerp(24f, -6f, Ease(t));
                    pose.Glow = Mathf.Lerp(1.2f, 1.5f, Ease(t));
                    break;

                case BossAnimationState.Death:
                    // Longer and heavier than an ordinary enemy's, but still a death and not a
                    // cutscene: the sword goes down, the knee follows, the runes go out.
                    var end = Ease(t);
                    pose.Squash = Mathf.Lerp(1f, 0.5f, end);
                    pose.HipY = -Mathf.Lerp(0f, 92f, t * t);
                    pose.HipX = -Mathf.Lerp(0f, 26f, end);
                    pose.Lean = Mathf.Lerp(10f, -74f, end);
                    pose.FrontThigh = Mathf.Lerp(14f, 70f, end);
                    pose.FrontShin = Mathf.Lerp(-18f, -78f, end);
                    pose.BackThigh = Mathf.Lerp(-14f, 48f, end);
                    pose.BackShin = Mathf.Lerp(-18f, -74f, end);
                    pose.FrontArm = Mathf.Lerp(-16f, -80f, end);
                    pose.BackArm = Mathf.Lerp(-22f, -86f, end);
                    pose.HeadTilt = Mathf.Lerp(-4f, 46f, end);
                    pose.SwordAngle = Mathf.Lerp(-12f, 134f, Ease(Mathf.Clamp01(t * 1.6f)));
                    pose.CapeSway = Mathf.Lerp(10f, 42f, end);
                    pose.Glow = Mathf.Lerp(1.5f, 0f, Ease(Mathf.Clamp01(t * 1.3f)));
                    break;
            }

            return pose;
        }

        // ---------- drawing ----------

        private static byte[] Render(Actor actor, Pose pose)
        {
            var build = BuildFor(actor);
            var canvas = new Canvas(build.Width, build.Height);
            Draw(canvas, build, pose, actor);
            // Enemies get a lit edge too — never lost in the dark — but a warm, sickly one and
            // weaker than the player's: cold is the hero, warm is everything that wants him dead.
            canvas.Silhouette(new Color32(6, 5, 5, 225), EnemyRim, 0.42f);
            return canvas.EncodePng();
        }

        private static void Draw(Canvas canvas, Build build, Pose pose, Actor actor)
        {
            var hip = new Vector2(canvas.Width * 0.5f + pose.HipX,
                (build.HipY + pose.HipY) * pose.Squash);
            var lean = pose.Lean;
            var shoulder = hip + Canvas.Rotate(new Vector2(0f, build.Torso * pose.Squash), lean);
            var head = shoulder + Canvas.Rotate(new Vector2(0f, build.Neck + build.HeadRy),
                lean + pose.HeadTilt);

            LooseAnchor(canvas, build, actor, hip, shoulder, head, lean, lean + pose.HeadTilt);

            // Back limbs first: depth comes from draw order, not from a z buffer.
            canvas.Limb(shoulder, pose.BackArm + lean, build.Arm, pose.BackForearm, build.Forearm,
                build.ArmWidth, build.Metal);
            canvas.Limb(hip, pose.BackThigh, build.Thigh, pose.BackShin, build.Shin,
                build.LegWidth, build.Metal);

            if (build.Cape)
            {
                Cape(canvas, build, shoulder, hip, lean, pose.CapeSway);
            }

            Torso(canvas, build, hip, shoulder, lean, actor);

            canvas.Limb(hip, pose.FrontThigh, build.Thigh, pose.FrontShin, build.Shin,
                build.LegWidth, build.Armor);

            // The weapon hangs from the front hand, so it follows the arm rather than floating.
            var hand = canvas.Limb(shoulder, pose.FrontArm + lean, build.Arm, pose.FrontForearm,
                build.Forearm, build.ArmWidth, build.Armor);
            Sword(canvas, build, hand, pose.SwordAngle + lean, actor);

            // Without a neck the head reads as a separate object floating above the shoulders.
            canvas.Thick(shoulder, head, build.TorsoWidth * 0.34f, build.HeadRx * 0.85f,
                build.Metal);
            Head(canvas, build, head, lean + pose.HeadTilt, pose, actor);

            if (build.Runes)
            {
                Runes(canvas, build, hip, shoulder, lean, pose.Glow);
            }

            // The shield is drawn last and in front: the brief asks it to dominate the frontal
            // silhouette, which only happens if nothing is painted over it.
            if (build.Shield)
            {
                Shield(canvas, build, shoulder, hip, lean, pose.ShieldAngle, pose.ShieldDrop,
                    actor);
            }

            if (pose.Alpha < 0.999f)
            {
                canvas.Fade(pose.Alpha);
            }
        }

        /// <summary>
        /// Where each kind hangs its loose piece from (visual item 4): rags at the zombie's back,
        /// the archer's hood tail, the hem of the shaman's robe, the spearman's scarf, the
        /// Guardian's cape and the Warden's cloak of roots. The guard has nothing loose.
        /// </summary>
        private static void LooseAnchor(Canvas canvas, Build build, Actor actor, Vector2 hip,
            Vector2 shoulder, Vector2 head, float lean, float headTilt)
        {
            var w = build.TorsoWidth;
            switch (actor)
            {
                case Actor.Zombie:
                    Anchor(canvas, hip + Canvas.Rotate(new Vector2(-w * 0.4f, build.Torso * 0.15f), lean), 10f + lean * 0.3f);
                    break;
                case Actor.Archer:
                    Anchor(canvas, head + Canvas.Rotate(new Vector2(-build.HeadRx * 1.05f, -build.HeadRy * 0.35f), headTilt),
                        28f + headTilt * 0.3f);
                    break;
                case Actor.Shaman:
                    Anchor(canvas, hip + Canvas.Rotate(new Vector2(-w * 0.5f, -build.Torso * 0.3f), lean), 14f + lean * 0.3f);
                    break;
                case Actor.Spearman:
                    Anchor(canvas, shoulder + Canvas.Rotate(new Vector2(-w * 0.3f, -2f), lean), 34f + lean * 0.3f);
                    break;
                case Actor.BossPhaseOne:
                case Actor.BossPhaseTwo:
                    Anchor(canvas, shoulder + Canvas.Rotate(new Vector2(-w * 0.55f, -build.Torso * 0.85f), lean),
                        12f + lean * 0.3f);
                    break;
                case Actor.WardenPhaseOne:
                case Actor.WardenPhaseTwo:
                    Anchor(canvas, shoulder + Canvas.Rotate(new Vector2(-w * 0.45f, -4f), lean), 18f + lean * 0.3f);
                    break;
            }
        }

        private static void Torso(Canvas canvas, Build build, Vector2 hip, Vector2 shoulder,
            float lean, Actor actor)
        {
            canvas.Thick(hip, shoulder, build.TorsoWidth * 0.78f, build.TorsoWidth * 0.62f,
                build.Armor);
            // A tabard, torn and faded: the one piece of royal cloth every rank still wears.
            var chest = Vector2.Lerp(hip, shoulder, 0.55f);
            canvas.Thick(chest, hip + Canvas.Rotate(new Vector2(0f, -build.Torso * 0.28f), lean),
                build.TorsoWidth * 0.34f, build.TorsoWidth * 0.26f, build.Cloth);

            // Pauldrons. The guard's and the Guardian's are what make them read as heavier than
            // the zombie at the same capsule size.
            if (actor != Actor.Zombie)
            {
                var span = build.TorsoWidth * 0.52f;
                canvas.Ellipse(shoulder + Canvas.Rotate(new Vector2(-span * 0.5f, 2f), lean),
                    span * 0.55f, span * 0.42f, build.Metal);
                canvas.Ellipse(shoulder + Canvas.Rotate(new Vector2(span * 0.45f, 1f), lean),
                    span * 0.6f, span * 0.45f, build.Metal);
            }

            // A robe instead of a soldier's waist: the shaman's outline widens towards the ground
            // where everyone else's narrows, which is the whole difference at a glance.
            if (build.RobeFlare > 0f)
            {
                var hem = hip + Canvas.Rotate(new Vector2(0f, -build.Torso * 0.42f), lean);
                canvas.Triangle(
                    chest + Canvas.Rotate(new Vector2(-build.TorsoWidth * 0.3f, 0f), lean),
                    hem + Canvas.Rotate(new Vector2(-build.RobeFlare, 0f), lean),
                    hem + Canvas.Rotate(new Vector2(build.RobeFlare, 0f), lean), build.Cloth);
                canvas.Triangle(
                    chest + Canvas.Rotate(new Vector2(build.TorsoWidth * 0.3f, 0f), lean),
                    chest + Canvas.Rotate(new Vector2(-build.TorsoWidth * 0.3f, 0f), lean),
                    hem + Canvas.Rotate(new Vector2(build.RobeFlare, 0f), lean), build.Cloth);
            }

            // The royal crest, faded gold, on every rank.
            canvas.Ellipse(chest + Canvas.Rotate(new Vector2(2f, 0f), lean),
                build.TorsoWidth * 0.13f, build.TorsoWidth * 0.17f, build.Trim);

            // What the forest has done to it. Painted last and only over the body, so the shape
            // stays readable and the corruption looks like it grew there rather than beside it.
            if (build.Growth)
            {
                ForestGrowth(canvas, build, hip, shoulder, lean);
            }
        }

        private static void Head(Canvas canvas, Build build, Vector2 head, float tilt, Pose pose,
            Actor actor)
        {
            if (actor == Actor.Zombie)
            {
                // Exposed dead flesh, then a broken helm that only covers part of it. The damage
                // is asymmetric on purpose: this one has been out here the longest.
                canvas.Ellipse(head, build.HeadRx, build.HeadRy, build.Skin);
                canvas.Triangle(head + Canvas.Rotate(new Vector2(-build.HeadRx, 2f), tilt),
                    head + Canvas.Rotate(new Vector2(build.HeadRx * 0.5f, build.HeadRy), tilt),
                    head + Canvas.Rotate(new Vector2(-build.HeadRx * 0.9f, -build.HeadRy * 0.4f),
                        tilt), build.Metal);
                Eye(canvas, head + Canvas.Rotate(new Vector2(build.HeadRx * 0.45f, 0f), tilt), 2f,
                    SicklyEye, pose.EyeFlare);
                return;
            }

            if (build.Head != HeadGear.Helm)
            {
                ForestHead(canvas, build, head, tilt, pose.EyeFlare);
                return;
            }

            canvas.Ellipse(head, build.HeadRx, build.HeadRy, build.Metal);
            // A closed visor: a dark slit rather than a face, on both the guard and the Guardian.
            canvas.Thick(head + Canvas.Rotate(new Vector2(build.HeadRx * 0.2f, 2f), tilt),
                head + Canvas.Rotate(new Vector2(build.HeadRx * 0.95f, 0f), tilt),
                build.HeadRy * 0.3f, build.HeadRy * 0.24f, Charcoal);

            if (!build.Crown)
            {
                // Two glints deep in the visor.
                Eye(canvas, head + Canvas.Rotate(new Vector2(build.HeadRx * 0.72f, 1.5f), tilt), 1.4f,
                    AmberEye, pose.EyeFlare);
                return;
            }

            // The crown is what stops the Guardian reading as a large Shield Guard, and it is also
            // where the extra height the Visual Bible asked for comes from.
            for (var i = -2; i <= 2; i++)
            {
                var spikeBase = head + Canvas.Rotate(
                    new Vector2(i * build.HeadRx * 0.36f, build.HeadRy * 0.8f), tilt);
                var spikeTip = head + Canvas.Rotate(
                    new Vector2(i * build.HeadRx * 0.42f,
                        build.HeadRy + (i == 0 ? 22f : 14f - Mathf.Abs(i) * 3f)), tilt);
                canvas.Thick(spikeBase, spikeTip, 6f, 1.5f, build.Trim);
            }

            // Narrow eyes, always lit and brightening with the phase.
            var eyeTone = Color32.Lerp(Color32.Lerp(AmberEye, Charcoal, 0.35f), build.Rot,
                Mathf.Clamp01(pose.Glow));
            canvas.Ellipse(head + Canvas.Rotate(new Vector2(build.HeadRx * 0.62f, 1f), tilt),
                build.HeadRx * 0.26f, build.HeadRy * 0.14f, Color32.Lerp(eyeTone, Charcoal, 0.5f));
            canvas.Ellipse(head + Canvas.Rotate(new Vector2(build.HeadRx * 0.62f, 1f), tilt),
                build.HeadRx * 0.2f, build.HeadRy * 0.09f, eyeTone);
        }

        /// <summary>
        /// The sword, drawn from the hand outward so it always sits in the grip. Its length is the
        /// authored visual length and has nothing to do with the hitbox, which comes from the
        /// attack data — the Visual Bible's rule that art never defines reach.
        /// </summary>
        private static void Sword(Canvas canvas, Build build, Vector2 hand, float angle,
            Actor actor)
        {
            if (build.Weapon != WeaponShape.Blade)
            {
                ForestWeapon(canvas, build, hand, angle);
                return;
            }

            var tip = hand + Canvas.Rotate(new Vector2(0f, -build.SwordLength), -angle);
            var guardEnd = hand + Canvas.Rotate(new Vector2(0f, -build.SwordLength * 0.16f),
                -angle);
            canvas.Thick(hand, tip, build.SwordWidth, build.SwordWidth * 0.5f,
                actor == Actor.Zombie ? Rust : build.Metal);
            // Crossguard, across the blade.
            var across = Canvas.Rotate(new Vector2(build.SwordWidth * 1.9f, 0f), -angle);
            canvas.Thick(guardEnd - across, guardEnd + across, build.SwordWidth * 0.8f,
                build.SwordWidth * 0.8f, build.Trim);
        }

        private static void Shield(Canvas canvas, Build build, Vector2 shoulder, Vector2 hip,
            float lean, float angle, float drop, Actor actor)
        {
            // Carried on the front arm, at chest height, and dropped when the guard breaks. It has
            // to dominate the frontal silhouette without swallowing the body: the player needs to
            // read "shield" and "armoured soldier behind it" in the same glance.
            var anchor = Vector2.Lerp(hip, shoulder, 0.68f)
                + Canvas.Rotate(new Vector2(build.ShieldWidth * 0.3f, -drop), lean);
            var up = Canvas.Rotate(new Vector2(0f, build.ShieldHeight * 0.5f), lean + angle * 0.35f);
            var side = Canvas.Rotate(new Vector2(build.ShieldWidth * 0.5f, 0f),
                lean + angle * 0.35f);
            // A kite: square shoulders, tapering to a point. Never a round wooden buckler.
            var topBack = anchor + up - side;
            var topFront = anchor + up + side;
            var midBack = anchor - side * 0.92f;
            var midFront = anchor + side * 0.92f;
            var point = anchor - up * 1.25f;
            canvas.Quad(topBack, topFront, midFront, midBack, build.Metal);
            canvas.Triangle(midBack, midFront, point, build.Metal);
            // Rim and crest, so it reads as kingdom-made rather than as a grey slab.
            canvas.Thick(topBack, topFront, 4f, 4f, build.Trim);
            canvas.Ellipse(anchor + up * 0.15f, build.ShieldWidth * 0.2f,
                build.ShieldWidth * 0.24f, build.Trim);
            canvas.Ellipse(anchor + up * 0.15f, build.ShieldWidth * 0.1f,
                build.ShieldWidth * 0.13f, build.Cloth);
            if (actor == Actor.BossPhaseOne)
            {
                // The boss shield is ceremonial: a second inner rim the guard's does not have.
                canvas.Thick(midBack, point, 3f, 3f, build.Trim);
                canvas.Thick(midFront, point, 3f, 3f, build.Trim);
            }
        }

        private static void Cape(Canvas canvas, Build build, Vector2 shoulder, Vector2 hip,
            float lean, float sway)
        {
            var topBack = shoulder + Canvas.Rotate(new Vector2(-build.TorsoWidth * 0.5f, 6f), lean);
            var topFront = shoulder + Canvas.Rotate(new Vector2(build.TorsoWidth * 0.2f, 4f), lean);
            // The hem hangs from the hips and follows the sway; the anchors above already moved
            // with the lean. Simple secondary motion, no simulation.
            var hemBack = hip + Canvas.Rotate(new Vector2(-build.TorsoWidth * 0.75f, -84f), -sway);
            var hemFront = hip + Canvas.Rotate(new Vector2(build.TorsoWidth * 0.1f, -66f),
                -sway * 0.7f);
            canvas.Quad(topBack, topFront, hemFront, hemBack, build.Cloth);
        }

        private static void Runes(Canvas canvas, Build build, Vector2 hip, Vector2 shoulder,
            float lean, float glow)
        {
            var intensity = Mathf.Clamp01(glow);
            if (intensity <= 0.01f)
            {
                return;
            }

            var tone = Color32.Lerp(build.Armor, build.Rot, intensity);
            // The corrupted core, in the cracks of the breastplate. In phase two it is exposed and
            // brighter; the glow value carries that without repainting the whole boss.
            var core = Vector2.Lerp(hip, shoulder, 0.62f);
            canvas.Ellipse(core, build.TorsoWidth * (0.1f + intensity * 0.06f),
                build.TorsoWidth * (0.13f + intensity * 0.08f), tone);
            for (var i = 0; i < 3; i++)
            {
                var from = core + Canvas.Rotate(new Vector2(-10f + i * 10f, 6f), lean);
                var to = core + Canvas.Rotate(new Vector2(-16f + i * 14f, -22f - i * 4f), lean);
                canvas.Thick(from, to, 3f, 1.5f, tone);
            }
        }
    }
}
