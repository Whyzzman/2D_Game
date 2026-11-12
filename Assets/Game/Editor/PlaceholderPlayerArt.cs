using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// Draws a placeholder silhouette for every player animation state.
    ///
    /// This is not final art and does not pretend to be. It exists because a coloured rectangle
    /// cannot answer the questions the animation pipeline has to answer — does the pose read at
    /// 144 pixels, does the contact frame land inside the active window, does the hood and
    /// half-cloak silhouette stay recognisable while running — and those answers should not wait
    /// on a hand-drawn set. The proportions, palette and frame counts follow
    /// `gamedesign/CONCEPT_BRIEFS.md` and `gamedesign/ANIMATION_LIST.md`, so real frames drop in
    /// against the same contract.
    ///
    /// Each frame is its own PNG under `Assets/Game/Art/Characters/Player/&lt;state&gt;/`. These files
    /// are regenerated on every build; an artist's frame replaces the file in place and is listed in
    /// `Art/Painted.txt`, and <see cref="PlaceholderImport.WriteIfChanged"/> leaves it alone
    /// (gamedesign/ART_ASSET_LIST.md). Nothing in code changes.
    /// </summary>
    public static class PlaceholderPlayerArt
    {
        public const string ArtFolder = "Assets/Game/Art/Characters/Player";
        // Wide and tall enough for the sword at full extension; the pivot stays at the feet.
        private const int Width = 256;
        private const int Height = 224;

        // Palette from the concept brief. Kept to a few tones on purpose: a placeholder that is
        // more colourful than the real character would mislead everyone looking at it.
        private static readonly Color32 Charcoal = new Color32(0x14, 0x11, 0x0F, 0xFF);
        private static readonly Color32 CloakTone = new Color32(0x22, 0x1C, 0x18, 0xFF);
        private static readonly Color32 Leather = new Color32(0x4A, 0x3B, 0x2E, 0xFF);
        private static readonly Color32 Steel = new Color32(0x3E, 0x42, 0x47, 0xFF);
        private static readonly Color32 Sash = new Color32(0x6B, 0x77, 0x84, 0xFF);
        /// <summary>The cloak's grey-blue lining (Visual Bible §13): the hero's own accent, dimmer than any magic.</summary>
        private static readonly Color32 Lining = new Color32(0x50, 0x60, 0x70, 0xFF);
        private static readonly Color32 Face = new Color32(0x7A, 0x6F, 0x66, 0xFF);
        /// <summary>The player's lit edge: silver, strong, the brightest outline on screen.</summary>
        private static readonly Color32 Rim = new Color32(196, 218, 246, 255);
        private static readonly Color32 Clear = new Color32(0, 0, 0, 0);

        /// <summary>One authored state: how many frames it has and how fast it plays.</summary>
        public readonly struct StateSpec
        {
            public readonly PlayerAnimationState State;
            public readonly int Frames;
            public readonly float Fps;
            public readonly bool Loops;

            public StateSpec(PlayerAnimationState state, int frames, float fps, bool loops)
            {
                State = state;
                Frames = frames;
                Fps = fps;
                Loops = loops;
            }
        }

        /// <summary>
        /// The frame budget per state. Stances run at 12 FPS and combat at 24, as the Visual Bible
        /// fixed; the counts are the ones an artist is being asked to deliver, so the placeholder
        /// and the real set are the same shape.
        /// </summary>
        public static readonly StateSpec[] States =
        {
            // A slow breath: two seconds a cycle (hero animation 2.0, 2026-09-28). 2026-10-28 (new hero, user:
            // «more movement»): more frames in the moves seen most — idle, run, jumps, swings, wall, dash.
            new StateSpec(PlayerAnimationState.Idle, 16, 8f, true),
            // Smoother motion (2026-10-19, decided by Claude under the standing approval): the
            // frame counts move to the budgets of ANIMATION_LIST.md. The run keeps its half-second
            // stride, so ten frames play at 20 FPS; the feet strike on frames 0 and 5.
            new StateSpec(PlayerAnimationState.Run, 14, 28f, true),
            new StateSpec(PlayerAnimationState.JumpStart, 4, 24f, false),
            new StateSpec(PlayerAnimationState.JumpRise, 5, 12f, true),
            new StateSpec(PlayerAnimationState.Fall, 5, 12f, true),
            new StateSpec(PlayerAnimationState.Land, 5, 24f, false),
            new StateSpec(PlayerAnimationState.Dash, 7, 32f, false),
            new StateSpec(PlayerAnimationState.AirDash, 7, 32f, false),
            new StateSpec(PlayerAnimationState.WallSlide, 6, 12f, true),
            new StateSpec(PlayerAnimationState.WallJump, 4, 24f, false),
            new StateSpec(PlayerAnimationState.WallRun, 6, 16f, true),
            // Swings are stretched to the attack's real duration, so more frames only add
            // in-betweens; the strike still lands where AttackDefinition puts it.
            new StateSpec(PlayerAnimationState.Attack1, 10, 24f, false),
            new StateSpec(PlayerAnimationState.Attack2, 10, 24f, false),
            new StateSpec(PlayerAnimationState.Attack3, 12, 24f, false),
            new StateSpec(PlayerAnimationState.Heavy, 16, 24f, false),
            new StateSpec(PlayerAnimationState.Block, 2, 12f, true),
            new StateSpec(PlayerAnimationState.BlockImpact, 2, 24f, false),
            new StateSpec(PlayerAnimationState.Parry, 3, 24f, false),
            new StateSpec(PlayerAnimationState.GuardBroken, 3, 12f, false),
            new StateSpec(PlayerAnimationState.Hurt, 4, 24f, false),
            new StateSpec(PlayerAnimationState.Stunned, 4, 12f, true),
            new StateSpec(PlayerAnimationState.Death, 6, 12f, false),
            new StateSpec(PlayerAnimationState.CastStart, 4, 12f, true),
            new StateSpec(PlayerAnimationState.CastRelease, 3, 24f, false),
            new StateSpec(PlayerAnimationState.GroundBreaker, 10, 24f, false),
            new StateSpec(PlayerAnimationState.DashStrike, 7, 24f, false),
            new StateSpec(PlayerAnimationState.ShadowDashStart, 3, 24f, false),
            new StateSpec(PlayerAnimationState.ShadowDashLoop, 2, 24f, true),
            new StateSpec(PlayerAnimationState.ShadowDashEnd, 3, 24f, false),
            new StateSpec(PlayerAnimationState.ShadowStrike, 5, 24f, false),
            new StateSpec(PlayerAnimationState.WeaponSwitch, 3, 24f, false),
            new StateSpec(PlayerAnimationState.ActiveSkill, 4, 24f, false),
            new StateSpec(PlayerAnimationState.AttackUp, 9, 24f, false),
            new StateSpec(PlayerAnimationState.AttackDown, 9, 24f, false),
            new StateSpec(PlayerAnimationState.Heal, 6, 12f, false),
            new StateSpec(PlayerAnimationState.IdleVariant, 10, 12f, false),
            new StateSpec(PlayerAnimationState.Turn, 3, 36f, false), // 0,083 s: a Hollow Knight turn
            new StateSpec(PlayerAnimationState.HardLand, 5, 12f, false),
            new StateSpec(PlayerAnimationState.IdleGlance, 10, 12f, false)
        };

        /// <summary>
        /// The swings a two-handed weapon replaces. Fewer frames per second and more reach: a
        /// greatsword is not a sword played slowly, and having the placeholder already differ
        /// proves the per-weapon override path works rather than merely existing.
        /// </summary>
        public static readonly StateSpec[] GreatswordStates =
        {
            new StateSpec(PlayerAnimationState.Attack1, 10, 18f, false),
            new StateSpec(PlayerAnimationState.Attack2, 10, 18f, false),
            new StateSpec(PlayerAnimationState.Attack3, 12, 18f, false),
            new StateSpec(PlayerAnimationState.Heavy, 16, 18f, false),
            new StateSpec(PlayerAnimationState.AttackUp, 9, 18f, false),
            new StateSpec(PlayerAnimationState.AttackDown, 9, 18f, false)
        };

        /// <summary>Writes every missing or changed frame and returns the sprites, per state.</summary>
        public static Dictionary<PlayerAnimationState, Sprite[]> Generate() =>
            Generate(ArtFolder, States, 0f);

        public static Dictionary<PlayerAnimationState, Sprite[]> Generate(string rootFolder,
            StateSpec[] specs, float reachBonus)
        {
            PlaceholderImport.EnsureFolder(rootFolder);
            var result = new Dictionary<PlayerAnimationState, Sprite[]>();
            foreach (var spec in specs)
            {
                var folder = $"{rootFolder}/{spec.State}";
                PlaceholderImport.EnsureFolder(folder);
                var sprites = new Sprite[spec.Frames];
                for (var i = 0; i < spec.Frames; i++)
                {
                    var path = $"{folder}/{Name(spec.State)}_{i:00}.png";
                    // Where the blade was a fixed share of the clip ago, so a swing's fast frames
                    // are drawn as a smear however many in-betweens the clip has.
                    var phase = FramePhase(spec, i);
                    var previous = i > 0 ? Mathf.Max(0f, phase - SmearSpan) : (float?)null;
                    PlaceholderImport.WriteIfChanged(path, Render(spec.State, phase,
                        reachBonus, previous));
                    PlaceholderImport.ConfigureImporter(path);
                    sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (sprites[i] == null)
                    {
                        Debug.LogError($"Placeholder frame failed to import: {path}");
                    }
                }

                result[spec.State] = sprites;
            }

            return result;
        }

        /// <summary>
        /// How much of the clip a smear looks back over: the arc the eye still holds from the last
        /// third of a swing. It used to be one frame of four; with denser frames it stays the same arc.
        /// </summary>
        private const float SmearSpan = 0.3f;

        /// <summary>How far through its clip a frame is: a loop never repeats its first frame.</summary>
        private static float FramePhase(StateSpec spec, int frame)
        {
            if (spec.Loops)
            {
                return frame / (float)spec.Frames;
            }

            return spec.Frames <= 1 ? 0f : frame / (float)(spec.Frames - 1);
        }

        /// <summary>
        /// Where the cloak's loose edge hangs from on each frame, for <see cref="CloakTail"/>: the
        /// back corner of the drawn hem and the line of the cloak's back edge, from the same pose
        /// the frame is drawn from, so the live edge never comes off the cloak.
        /// </summary>
        public static Vector3[] Anchors(StateSpec spec, float reachBonus)
        {
            var anchors = new Vector3[spec.Frames];
            for (var i = 0; i < spec.Frames; i++)
            {
                anchors[i] = TrailAnchor(PoseFor(spec.State, FramePhase(spec, i), reachBonus));
            }

            return anchors;
        }

        // How far above the hem's back corner the live edge starts, so it grows out of the cloak.
        private const float TrailInset = 6f;
        private const float HeroCloakNarrow = 0.76f;
        private const float HeroHemLift = 0.2f;

        private static Vector3 TrailAnchor(Pose pose)
        {
            Skeleton(pose, out var hip, out var shoulder);
            CloakBackEdge(shoulder, hip, pose.Lean, pose.CloakSway, out var top, out var hem);
            var along = (hem - top).normalized;
            var point = hem - along * TrailInset;
            var angle = Mathf.Atan2(-along.x, -along.y) * Mathf.Rad2Deg;
            // Low poses — a crouch, a body lying in death — would hang the edge through the
            // floor. The point stays on it and the edge swings back until its end clears it.
            var height = Mathf.Max(point.y, TrailInset) / PlaceholderImport.PixelsPerUnit;
            var level = Mathf.Acos(Mathf.Clamp01(height / CloakTail.DefaultLength)) * Mathf.Rad2Deg;
            if (angle < level)
            {
                angle = level;
            }

            // The drawn hero (design/characters/hero) is slimmer and stands on longer legs than this skeleton
            // (2026-11-12): his cloak's back edge is nearer the middle and its hem higher.
            return new Vector3((point.x - Width * 0.5f) * HeroCloakNarrow / PlaceholderImport.PixelsPerUnit,
                height + HeroHemLift, angle);
        }

        /// <summary>
        /// Where the casting hand holds the gathering spell, in units from the feet, facing right:
        /// the front hand of the cast pose half-way through its loop, a little past the fingers.
        /// </summary>
        public static Vector2 CastHand()
        {
            var pose = PoseFor(PlayerAnimationState.CastStart, 0.5f, 0f);
            Skeleton(pose, out _, out var shoulder);
            var upper = pose.FrontArm + pose.Lean;
            var elbow = shoulder + Canvas.Rotate(new Vector2(0f, -20f), -upper);
            var along = Canvas.Rotate(new Vector2(0f, -1f), -(upper + pose.FrontForearm));
            var hand = elbow + along * (18f + 6f);
            return new Vector2((hand.x - Width * 0.5f) / PlaceholderImport.PixelsPerUnit,
                hand.y / PlaceholderImport.PixelsPerUnit);
        }

        public const string CloakTailPath = ArtFolder + "/Cloak/player_cloak_tail.png";

        /// <summary>
        /// The strip the live edge is drawn with: root on the left, where it hides under the drawn
        /// cloak, and a torn end on the right. Same cloth, rim and outline as the frames, so the
        /// eye reads it as the cloak going on rather than as something attached.
        /// </summary>
        public static Texture2D GenerateCloakTail()
        {
            PlaceholderImport.EnsureFolder(ArtFolder + "/Cloak");
            var canvas = new Canvas(64, 24);
            canvas.Thick(new Vector2(-6f, 12f), new Vector2(50f, 12f), 14f, 11f, CloakTone);
            // Torn, not cut: two uneven tongues.
            canvas.Triangle(new Vector2(48f, 17.5f), new Vector2(48f, 11f), new Vector2(61f, 14.5f),
                CloakTone);
            canvas.Triangle(new Vector2(48f, 13f), new Vector2(48f, 6.5f), new Vector2(57f, 8f),
                CloakTone);
            // The lining shows along the lower edge, as on the drawn cloak.
            canvas.Thick(new Vector2(-6f, 7f), new Vector2(48f, 7.5f), 3f, 2.5f, Lining);
            canvas.Silhouette(new Color32(8, 7, 6, 235), Rim, 0.8f, 0.12f);
            PlaceholderImport.WriteIfChanged(CloakTailPath, canvas.EncodePng());
            PlaceholderImport.ConfigureImporter(CloakTailPath);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(CloakTailPath);
        }

        private static string Name(PlayerAnimationState state) =>
            "player_" + state.ToString().ToLowerInvariant();

        // ---------- posing ----------

        /// <summary>
        /// Everything that distinguishes one frame from another. Angles are degrees, 0 pointing
        /// straight down for limbs, and positive swings the limb forward (towards +x, the facing
        /// direction); the motor mirrors the whole visual, so every pose is drawn facing right.
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
            public float CloakSway;
            public float Squash;
            /// <summary>Extra torso length in pixels: the chest rising with a breath.</summary>
            public float Chest;
            public float Alpha;
            /// <summary>Drinking: the front hand holds a flask and the sword moves to the back hand.</summary>
            public bool Flask;
            /// <summary>
            /// The cloak's hem catching the air, -1..1: a tongue of cloth at the back corner that
            /// flicks to one side or the other, a beat behind the body (overlapping action).
            /// </summary>
            public float CloakFlutter;
            /// <summary>Speed streaks behind the body, 0..1: the dash's trail drawn into the frame.</summary>
            public float Streak;
            /// <summary>The streaks are the Shadow's violet rather than the hero's cold grey-blue.</summary>
            public bool ShadowStreak;

            public static Pose Stand => new Pose
            {
                Lean = 3f,
                HipY = 78f,
                HipX = 0f,
                FrontThigh = 4f,
                FrontShin = -4f,
                BackThigh = -4f,
                BackShin = -6f,
                FrontArm = 12f,
                FrontForearm = 18f,
                BackArm = -10f,
                BackForearm = 14f,
                HeadTilt = 0f,
                CloakSway = 0f,
                Squash = 1f,
                Alpha = 1f
            };
        }

        private static float Sin01(float t) => Mathf.Sin(t * Mathf.PI * 2f);

        /// <summary>0 outside [from, to]; eases up over its first third, holds, eases back down.</summary>
        private static float Plateau(float t, float from, float to)
        {
            var u = Mathf.Clamp01((t - from) / Mathf.Max(0.0001f, to - from));
            var edge = u < 0.5f ? u : 1f - u;
            return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(edge / 0.3f));
        }

        private static Pose PoseFor(PlayerAnimationState state, float t, float reachBonus)
        {
            var reachScale = 1f + Mathf.Max(0f, reachBonus);
            var pose = Pose.Stand;
            switch (state)
            {
                case PlayerAnimationState.Idle:
                {
                    // A breath: the chest and shoulders rise, the arms ride up with them, and the
                    // head and cloak follow a beat later.
                    var breath = 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI * 2f);
                    var after = 0.5f - 0.5f * Mathf.Cos((t - 0.12f) * Mathf.PI * 2f);
                    pose.Chest = breath * 2.5f;
                    pose.HipY += breath * 0.8f;
                    pose.FrontArm -= breath * 3f;
                    pose.BackArm += breath * 3f;
                    pose.HeadTilt = -after * 2.5f;
                    pose.CloakSway = (after - 0.5f) * 5f;
                    break;
                }

                case PlayerAnimationState.IdleGlance:
                {
                    // Down at the blade, a turn of the wrist to look along it, then up ahead and
                    // back into the stance, which it ends on so returning to Idle never jumps.
                    var down = Plateau(t, 0.04f, 0.56f);
                    var ahead = Plateau(t, 0.5f, 0.96f);
                    pose.FrontArm += 22f * down;
                    pose.FrontForearm += 46f * down;
                    pose.HeadTilt = 16f * down - 9f * ahead;
                    pose.Lean = 3f + 4f * down - 3f * ahead;
                    pose.Chest = 1.5f * ahead;
                    pose.CloakSway = -3f * down + 4f * ahead;
                    break;
                }

                case PlayerAnimationState.HardLand:
                    return HardLandPose(t);

                case PlayerAnimationState.Run:
                {
                    // Counter-swing: the arm opposite the leading leg comes forward, which is what
                    // makes a run read as a run rather than a shuffle. A foot strikes at t = 0 and
                    // 0.5: the body is lowest and a little squashed there, highest and stretched as
                    // the legs pass. The leg swinging forward folds at the knee. The cloak flaps
                    // once a step, a beat behind the bob.
                    var stride = Mathf.Cos(t * Mathf.PI * 2f);
                    var swing = Mathf.Sin(t * Mathf.PI * 2f);
                    var lift = Mathf.Abs(swing);
                    pose.Lean = 13f + (1f - lift) * 2f;
                    pose.HipY = 75f + lift * 4f;
                    pose.Squash = 0.96f + lift * 0.07f;
                    pose.FrontThigh = stride * 38f;
                    pose.BackThigh = -stride * 38f;
                    pose.FrontShin = -Mathf.Max(0f, -swing) * 58f - 6f;
                    pose.BackShin = -Mathf.Max(0f, swing) * 58f - 6f;
                    pose.FrontArm = -stride * 34f;
                    pose.BackArm = stride * 34f;
                    pose.FrontForearm = 40f + lift * 6f;
                    pose.BackForearm = 35f + lift * 6f;
                    pose.HeadTilt = 1f - lift * 2f;
                    pose.CloakSway = -16f + Mathf.Cos((t - 0.08f) * Mathf.PI * 4f) * 5f;
                    pose.CloakFlutter = Mathf.Sin((t - 0.15f) * Mathf.PI * 4f);
                    break;
                }

                case PlayerAnimationState.JumpStart:
                    // Anticipation, then launch: crouched and wide, then long and thin; the cloak
                    // is still hanging when the body has already left.
                    pose.Squash = Mathf.Lerp(0.84f, 1.08f, Mathf.SmoothStep(0f, 1f, t));
                    pose.CloakSway = Mathf.Lerp(10f, -4f, t * t);
                    pose.CloakFlutter = Mathf.Lerp(0f, -0.6f, t);
                    pose.HipY = Mathf.Lerp(62f, 84f, t);
                    pose.FrontThigh = Mathf.Lerp(34f, 12f, t);
                    pose.BackThigh = Mathf.Lerp(-30f, -10f, t);
                    pose.FrontShin = Mathf.Lerp(-56f, -10f, t);
                    pose.BackShin = Mathf.Lerp(-52f, -12f, t);
                    pose.FrontArm = Mathf.Lerp(-40f, 34f, t);
                    pose.BackArm = Mathf.Lerp(-46f, 26f, t);
                    break;

                case PlayerAnimationState.JumpRise:
                    pose.Squash = 1.05f - Sin01(t) * 0.01f;
                    pose.CloakFlutter = Sin01(t + 0.2f) * 0.8f;
                    pose.Lean = 8f;
                    pose.HipY = 80f + Sin01(t) * 1f;
                    pose.FrontThigh = 26f;
                    pose.FrontShin = -34f;
                    pose.BackThigh = -14f;
                    pose.BackShin = -30f;
                    pose.FrontArm = 30f;
                    pose.BackArm = 24f;
                    pose.CloakSway = -22f + Sin01(t) * 3f;
                    break;

                case PlayerAnimationState.Fall:
                    pose.Lean = -4f;
                    pose.HipY = 80f;
                    pose.FrontThigh = 16f;
                    pose.FrontShin = -20f;
                    pose.BackThigh = -20f;
                    pose.BackShin = -16f;
                    // The air pushes the cloak up and makes it flutter; the arms ride the air too.
                    pose.Squash = 1.03f;
                    pose.FrontArm = 42f + Sin01(t) * 5f;
                    pose.BackArm = 36f - Sin01(t) * 4f;
                    pose.CloakSway = 26f + Sin01(t) * 6f;
                    pose.CloakFlutter = Sin01(t + 0.25f);
                    break;

                case PlayerAnimationState.Land:
                    // Squashed flat on impact and widened, then springing back. The cloak keeps
                    // falling after the body has stopped and settles last. MovementFeedback adds
                    // its own small squash on top, so the frames stay a little short of the old 0.82.
                    pose.Squash = Mathf.Lerp(0.84f, 1f, 1f - (1f - t) * (1f - t));
                    pose.HipY = Mathf.Lerp(58f, 74f, t);
                    pose.CloakFlutter = Mathf.Lerp(0.8f, 0f, t);
                    pose.Lean = 16f;
                    pose.FrontThigh = 40f;
                    pose.FrontShin = -62f;
                    pose.BackThigh = -34f;
                    pose.BackShin = -58f;
                    pose.FrontArm = Mathf.Lerp(-34f, -14f, t);
                    pose.BackArm = Mathf.Lerp(-28f, -12f, t);
                    pose.CloakSway = Mathf.Lerp(30f, 8f, t);
                    break;

                case PlayerAnimationState.Dash:
                case PlayerAnimationState.AirDash:
                {
                    // A burst and a recovery: a quick gather, the body thrown long at a quarter of
                    // the clip with the streaks at their brightest, then righting itself while the
                    // cloak, left behind, whips forward over it.
                    var burst = Mathf.Sin(Mathf.Clamp01(t / 0.35f) * Mathf.PI * 0.5f);
                    var settle = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.45f) / 0.55f));
                    pose.Lean = Mathf.Lerp(24f, 38f, burst) - 14f * settle;
                    pose.HipY = 68f + 3f * settle;
                    pose.Squash = Mathf.Lerp(0.94f, 0.92f, burst) + 0.07f * settle;
                    pose.FrontThigh = 30f;
                    pose.FrontShin = -20f;
                    pose.BackThigh = Mathf.Lerp(-30f, -48f, burst) + 16f * settle;
                    pose.BackShin = -26f;
                    pose.FrontArm = Mathf.Lerp(-30f, -52f, burst) + 26f * settle;
                    pose.BackArm = Mathf.Lerp(-40f, -62f, burst) + 24f * settle;
                    pose.FrontForearm = 12f;
                    pose.CloakSway = Mathf.Lerp(-28f, -46f, burst) + 30f * settle;
                    pose.CloakFlutter = Mathf.Sin(t * Mathf.PI * 3f);
                    pose.Streak = burst * (1f - settle * 0.85f);
                    pose.Alpha = state == PlayerAnimationState.AirDash ? 0.9f : 1f;
                    break;
                }

                case PlayerAnimationState.WallSlide:
                    pose.Lean = -8f;
                    pose.HipY = 74f;
                    pose.FrontThigh = 22f;
                    pose.FrontShin = -28f;
                    pose.BackThigh = -8f;
                    pose.BackShin = -34f;
                    pose.FrontArm = 58f;
                    pose.FrontForearm = -20f;
                    pose.BackArm = 20f;
                    pose.CloakSway = 20f + Sin01(t) * 3f;
                    break;

                case PlayerAnimationState.WallJump:
                    pose.Lean = 18f;
                    pose.HipY = Mathf.Lerp(70f, 84f, t);
                    pose.FrontThigh = Mathf.Lerp(40f, 18f, t);
                    pose.FrontShin = Mathf.Lerp(-54f, -16f, t);
                    pose.BackThigh = -26f;
                    pose.BackShin = -20f;
                    pose.FrontArm = Mathf.Lerp(52f, -10f, t);
                    pose.BackArm = 30f;
                    pose.CloakSway = -26f;
                    break;

                case PlayerAnimationState.WallRun:
                    pose.Lean = 22f;
                    pose.HipY = 78f;
                    pose.FrontThigh = Sin01(t) * 42f;
                    pose.BackThigh = -Sin01(t) * 42f;
                    pose.FrontShin = -26f;
                    pose.BackShin = -26f;
                    pose.FrontArm = 46f;
                    pose.BackArm = -Sin01(t) * 30f;
                    pose.CloakSway = 22f;
                    break;

                // The three light swings share a shape and differ in reach and lean, so the chain
                // reads as one continuous escalation rather than three unrelated moves.
                case PlayerAnimationState.Attack1:
                    return SwingPose(t, -70f, 58f, 10f, 1f * reachScale);
                case PlayerAnimationState.Attack2:
                    return SwingPose(t, 76f, -52f, 14f, 1.05f * reachScale);
                case PlayerAnimationState.Attack3:
                    return SwingPose(t, -86f, 70f, 20f, 1.15f * reachScale);
                case PlayerAnimationState.Heavy:
                    return SwingPose(t, -104f, 82f, 26f, 1.25f * reachScale);
                case PlayerAnimationState.GroundBreaker:
                    return SwingPose(t, -118f, 96f, 34f, 1.2f * reachScale);
                case PlayerAnimationState.DashStrike:
                    return SwingPose(t, -46f, 64f, 32f, 1.2f * reachScale);
                case PlayerAnimationState.ShadowStrike:
                    return SwingPose(t, -92f, 74f, 24f, 1.1f * reachScale,
                        alpha: Mathf.Lerp(0.55f, 1f, Mathf.Min(1f, t * 2f)));
                case PlayerAnimationState.ActiveSkill:
                    return SwingPose(t, -60f, 50f, 16f, 1f * reachScale);
                case PlayerAnimationState.AttackUp:
                    return UpSwingPose(t, reachScale);
                case PlayerAnimationState.AttackDown:
                    return DownSwingPose(t, reachScale);
                case PlayerAnimationState.Heal:
                    return DrinkPose(t);

                case PlayerAnimationState.IdleVariant:
                {
                    // Settle the sword, then a look back over the shoulder, then the stance again:
                    // it ends on the standing pose, so returning to Idle never jumps.
                    var settle = Plateau(t, 0.04f, 0.42f);
                    var look = Plateau(t, 0.34f, 0.94f);
                    pose.FrontArm += 24f * settle;
                    pose.FrontForearm += 34f * settle;
                    pose.HipY -= 1.5f * settle;
                    pose.Lean = 3f - 6f * look;
                    pose.HeadTilt = -12f * look;
                    pose.BackArm -= 8f * look;
                    pose.CloakSway = 5f * look - 3f * settle;
                    break;
                }

                case PlayerAnimationState.Turn:
                    // Drawn facing the new way: braking back, arms drawn in, the cloak still
                    // swinging round in front from the old side.
                    pose.Lean = Mathf.Lerp(-7f, 1f, t);
                    pose.HipY = Mathf.Lerp(74f, 77f, t);
                    pose.FrontThigh = Mathf.Lerp(16f, 8f, t);
                    pose.FrontShin = Mathf.Lerp(-20f, -8f, t);
                    pose.BackThigh = Mathf.Lerp(-20f, -8f, t);
                    pose.BackShin = Mathf.Lerp(-12f, -8f, t);
                    pose.FrontArm = Mathf.Lerp(-12f, 6f, t);
                    pose.BackArm = Mathf.Lerp(22f, -4f, t);
                    pose.HeadTilt = Mathf.Lerp(-5f, 0f, t);
                    pose.CloakSway = Mathf.Lerp(34f, 14f, t);
                    break;

                case PlayerAnimationState.Block:
                    pose.Lean = 12f;
                    pose.HipY = 72f;
                    pose.FrontThigh = 18f;
                    pose.FrontShin = -22f;
                    pose.BackThigh = -20f;
                    pose.BackShin = -18f;
                    pose.FrontArm = 74f;
                    pose.FrontForearm = -56f;
                    pose.BackArm = 30f;
                    pose.HeadTilt = -4f;
                    pose.CloakSway = Sin01(t) * 2f;
                    break;

                case PlayerAnimationState.BlockImpact:
                    pose.Lean = 4f;
                    pose.HipY = Mathf.Lerp(66f, 72f, t);
                    pose.HipX = Mathf.Lerp(-5f, -1f, t);
                    pose.FrontThigh = 24f;
                    pose.FrontShin = -30f;
                    pose.BackThigh = -28f;
                    pose.BackShin = -22f;
                    pose.FrontArm = 82f;
                    pose.FrontForearm = -64f;
                    pose.BackArm = 24f;
                    pose.CloakSway = 16f;
                    break;

                case PlayerAnimationState.Parry:
                    pose.Lean = 16f;
                    pose.HipY = 74f;
                    pose.FrontArm = Mathf.Lerp(40f, 108f, t);
                    pose.FrontForearm = Mathf.Lerp(-30f, -70f, t);
                    pose.BackArm = 26f;
                    pose.FrontThigh = 20f;
                    pose.FrontShin = -24f;
                    pose.BackThigh = -22f;
                    pose.BackShin = -20f;
                    pose.CloakSway = -12f;
                    break;

                case PlayerAnimationState.GuardBroken:
                    pose.Lean = -22f;
                    pose.HipY = Mathf.Lerp(74f, 64f, t);
                    pose.HipX = -Mathf.Lerp(2f, 9f, t);
                    pose.FrontArm = Mathf.Lerp(60f, -36f, t);
                    pose.FrontForearm = Mathf.Lerp(-40f, 30f, t);
                    pose.BackArm = Mathf.Lerp(20f, -44f, t);
                    pose.FrontThigh = 30f;
                    pose.FrontShin = -36f;
                    pose.BackThigh = -24f;
                    pose.BackShin = -30f;
                    pose.HeadTilt = Mathf.Lerp(0f, 18f, t);
                    pose.CloakSway = 26f;
                    break;

                case PlayerAnimationState.Hurt:
                    pose.Lean = -18f;
                    pose.HipX = -Mathf.Lerp(6f, 2f, t);
                    pose.HipY = 76f;
                    pose.FrontArm = -30f;
                    pose.BackArm = -40f;
                    pose.FrontThigh = 22f;
                    pose.FrontShin = -20f;
                    pose.BackThigh = -18f;
                    pose.BackShin = -24f;
                    pose.HeadTilt = 12f;
                    pose.CloakSway = 22f;
                    break;

                case PlayerAnimationState.Stunned:
                    pose.Lean = -12f + Sin01(t) * 5f;
                    pose.HipY = 70f;
                    pose.HipX = Sin01(t) * 4f;
                    pose.FrontArm = -24f + Sin01(t) * 10f;
                    pose.BackArm = -30f - Sin01(t) * 10f;
                    pose.FrontThigh = 18f;
                    pose.FrontShin = -26f;
                    pose.BackThigh = -20f;
                    pose.BackShin = -26f;
                    pose.HeadTilt = 14f + Sin01(t) * 6f;
                    pose.CloakSway = Sin01(t) * 10f;
                    break;

                case PlayerAnimationState.Death:
                    // Collapse: the hips drop and the torso folds, ending low and still.
                    pose.Squash = Mathf.Lerp(1f, 0.55f, t);
                    pose.HipY = Mathf.Lerp(76f, 16f, t * t);
                    pose.HipX = -Mathf.Lerp(0f, 16f, t);
                    pose.Lean = Mathf.Lerp(-10f, -74f, t);
                    pose.FrontThigh = Mathf.Lerp(20f, 62f, t);
                    pose.FrontShin = Mathf.Lerp(-24f, -70f, t);
                    pose.BackThigh = Mathf.Lerp(-18f, 40f, t);
                    pose.BackShin = Mathf.Lerp(-24f, -66f, t);
                    pose.FrontArm = Mathf.Lerp(-20f, -74f, t);
                    pose.BackArm = Mathf.Lerp(-28f, -80f, t);
                    pose.HeadTilt = Mathf.Lerp(8f, 40f, t);
                    pose.CloakSway = Mathf.Lerp(10f, 40f, t);
                    break;

                case PlayerAnimationState.CastStart:
                    pose.Lean = -6f;
                    pose.HipY = 76f;
                    pose.FrontArm = 84f + Sin01(t) * 6f;
                    pose.FrontForearm = -40f;
                    pose.BackArm = -18f;
                    pose.FrontThigh = 14f;
                    pose.FrontShin = -16f;
                    pose.BackThigh = -16f;
                    pose.BackShin = -18f;
                    pose.CloakSway = Sin01(t) * 6f;
                    break;

                case PlayerAnimationState.CastRelease:
                    pose.Lean = 14f;
                    pose.HipY = 74f;
                    pose.FrontArm = Mathf.Lerp(96f, 52f, t);
                    pose.FrontForearm = Mathf.Lerp(-56f, 6f, t);
                    pose.BackArm = -22f;
                    pose.FrontThigh = 22f;
                    pose.FrontShin = -22f;
                    pose.BackThigh = -24f;
                    pose.BackShin = -20f;
                    pose.CloakSway = -18f;
                    break;

                case PlayerAnimationState.ShadowDashStart:
                    pose.Lean = 28f;
                    pose.HipY = 70f;
                    pose.FrontThigh = 34f;
                    pose.FrontShin = -40f;
                    pose.BackThigh = -36f;
                    pose.BackShin = -30f;
                    pose.FrontArm = -50f;
                    pose.BackArm = -60f;
                    pose.CloakSway = -34f;
                    pose.Alpha = Mathf.Lerp(1f, 0.35f, t);
                    break;

                case PlayerAnimationState.ShadowDashLoop:
                    pose.Lean = 40f;
                    pose.HipY = 66f;
                    pose.FrontThigh = 26f;
                    pose.FrontShin = -18f;
                    pose.BackThigh = -48f;
                    pose.BackShin = -22f;
                    pose.FrontArm = -56f;
                    pose.BackArm = -66f;
                    pose.CloakSway = -48f;
                    pose.CloakFlutter = Sin01(t);
                    pose.Streak = 0.8f;
                    pose.ShadowStreak = true;
                    pose.Alpha = 0.3f + Sin01(t) * 0.05f;
                    break;

                case PlayerAnimationState.ShadowDashEnd:
                    pose.Lean = 18f;
                    pose.HipY = 72f;
                    pose.FrontThigh = 28f;
                    pose.FrontShin = -30f;
                    pose.BackThigh = -30f;
                    pose.BackShin = -26f;
                    pose.FrontArm = -20f;
                    pose.BackArm = -34f;
                    pose.CloakSway = -20f;
                    pose.Alpha = Mathf.Lerp(0.35f, 1f, t);
                    break;

                case PlayerAnimationState.WeaponSwitch:
                    pose.Lean = 6f;
                    pose.HipY = 76f;
                    pose.FrontArm = Mathf.Lerp(-58f, 34f, t);
                    pose.FrontForearm = Mathf.Lerp(50f, -10f, t);
                    pose.BackArm = Mathf.Lerp(-30f, 10f, t);
                    pose.FrontThigh = 10f;
                    pose.FrontShin = -12f;
                    pose.BackThigh = -12f;
                    pose.BackShin = -14f;
                    pose.CloakSway = 6f;
                    break;
            }

            return pose;
        }

        /// <summary>
        /// A swing, as three beats: wind up, commit, follow through. The strike lands at a third of
        /// the clip because that is roughly where the active window sits in the attack data — the
        /// animation debug tools show the exact window so this can be checked rather than assumed.
        /// </summary>
        private static Pose SwingPose(float t, float windupArm, float strikeArm, float lean,
            float reach, float alpha = 1f)
        {
            var pose = Pose.Stand;
            var commit = Mathf.Clamp01((t - 0.25f) / 0.45f);
            var eased = commit * commit * (3f - 2f * commit);
            pose.Lean = Mathf.Lerp(-lean * 0.6f, lean, eased);
            pose.HipY = 76f - Mathf.Abs(lean) * 0.12f;
            pose.HipX = Mathf.Lerp(-3f, 5f * reach, eased);
            pose.FrontArm = Mathf.Lerp(windupArm, strikeArm, eased);
            pose.FrontForearm = Mathf.Lerp(30f, -10f, eased) * reach;
            pose.BackArm = Mathf.Lerp(-windupArm * 0.25f, -strikeArm * 0.3f, eased);
            pose.BackForearm = 20f;
            pose.FrontThigh = Mathf.Lerp(-10f, 26f * reach, eased);
            pose.FrontShin = Mathf.Lerp(-12f, -28f, eased);
            pose.BackThigh = Mathf.Lerp(12f, -30f * reach, eased);
            pose.BackShin = Mathf.Lerp(-16f, -24f, eased);
            pose.HeadTilt = Mathf.Lerp(-4f, 6f, eased);
            // The cloak follows the body a beat late and overshoots before it settles.
            var late = Commit(t - 0.12f);
            pose.CloakSway = Mathf.Lerp(18f, -26f, late) - Mathf.Sin(late * Mathf.PI) * 6f;
            pose.CloakFlutter = Mathf.Sin(late * Mathf.PI) * 0.8f;
            // Gathered on the wind-up, long through the strike, back to the stance after.
            pose.Squash = 1f - 0.035f * Plateau(t, 0f, 0.3f) + 0.03f * Plateau(t, 0.3f, 0.8f);
            pose.Alpha = alpha;
            return pose;
        }

        /// <summary>
        /// The swing up: the blade starts low and forward and finishes pointing straight overhead,
        /// the body leaning back under it, so the arc reads as "up" even without the effect.
        /// </summary>
        private static Pose UpSwingPose(float t, float reach)
        {
            var pose = Pose.Stand;
            var eased = Commit(t);
            pose.Lean = Mathf.Lerp(10f, -10f, eased);
            pose.HipY = Mathf.Lerp(72f, 80f, eased);
            pose.FrontArm = Mathf.Lerp(-34f, 158f, eased);
            pose.FrontForearm = Mathf.Lerp(34f, 18f, eased) * reach;
            pose.BackArm = Mathf.Lerp(-20f, -40f, eased);
            pose.BackForearm = 24f;
            pose.FrontThigh = Mathf.Lerp(16f, 8f, eased);
            pose.FrontShin = Mathf.Lerp(-26f, -6f, eased);
            pose.BackThigh = Mathf.Lerp(-18f, -8f, eased);
            pose.BackShin = Mathf.Lerp(-22f, -8f, eased);
            pose.HeadTilt = Mathf.Lerp(0f, -14f, eased);
            pose.CloakSway = Mathf.Lerp(-8f, 20f, eased);
            return pose;
        }

        /// <summary>
        /// The swing down, only ever in the air: knees tucked, the blade raised overhead and driven
        /// straight down past the feet — the pose of a pogo, which has to be readable at a glance.
        /// </summary>
        private static Pose DownSwingPose(float t, float reach)
        {
            var pose = Pose.Stand;
            var eased = Commit(t);
            pose.Lean = Mathf.Lerp(-6f, 14f, eased);
            pose.HipY = 84f;
            pose.FrontArm = Mathf.Lerp(132f, -18f, eased);
            pose.FrontForearm = Mathf.Lerp(24f, -34f, eased) * reach;
            pose.BackArm = Mathf.Lerp(40f, 60f, eased);
            pose.BackForearm = 20f;
            pose.FrontThigh = 48f;
            pose.FrontShin = -70f;
            pose.BackThigh = 26f;
            pose.BackShin = -62f;
            pose.HeadTilt = Mathf.Lerp(-6f, 12f, eased);
            pose.CloakSway = Mathf.Lerp(-18f, -34f, eased);
            return pose;
        }

        /// <summary>
        /// Drinking a flask: raised to the mouth, the head thrown back while it is drunk (the heal
        /// lands at half the clip), then lowered. The sword hangs from the other hand, point down,
        /// so the whole pose says "not fighting now" — the window an enemy can punish.
        /// </summary>
        private static Pose DrinkPose(float t)
        {
            var pose = Pose.Stand;
            var raise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.35f));
            var lower = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.75f) / 0.25f));
            var up = raise * (1f - lower);
            pose.Flask = true;
            pose.Lean = Mathf.Lerp(4f, -8f, up);
            pose.HipY = 77f;
            pose.FrontArm = Mathf.Lerp(20f, 108f, up) - pose.Lean;
            pose.FrontForearm = Mathf.Lerp(40f, 132f, up);
            pose.BackArm = -8f;
            pose.BackForearm = 10f;
            pose.HeadTilt = Mathf.Lerp(0f, -22f, up);
            pose.FrontThigh = 8f;
            pose.FrontShin = -8f;
            pose.BackThigh = -8f;
            pose.BackShin = -10f;
            pose.CloakSway = Mathf.Lerp(0f, 8f, up);
            return pose;
        }

        /// <summary>
        /// Down on one knee after a long drop (the user's choice, 2026-09-28: visual only): the
        /// impact presses the body flat on the first frame, the pose holds, then the hero stands.
        /// The last frame is the stance, so returning to Idle never jumps.
        /// </summary>
        private static Pose HardLandPose(float t)
        {
            var stand = Pose.Stand;
            var rise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.3f) / 0.7f));
            float Blend(float kneel, float standing) => Mathf.Lerp(kneel, standing, rise);
            var pose = stand;
            pose.Squash = 1f - 0.07f * (1f - Mathf.Clamp01(t / 0.25f));
            pose.Lean = Blend(25f, stand.Lean);
            pose.HipX = Blend(-3f, stand.HipX);
            pose.HipY = Blend(33f, stand.HipY);
            pose.FrontThigh = Blend(78f, stand.FrontThigh);
            pose.FrontShin = Blend(-80f, stand.FrontShin);
            pose.BackThigh = Blend(-8f, stand.BackThigh);
            pose.BackShin = Blend(-82f, stand.BackShin);
            pose.FrontArm = Blend(-12f, stand.FrontArm);
            pose.FrontForearm = Blend(44f, stand.FrontForearm);
            pose.BackArm = Blend(-38f, stand.BackArm);
            pose.BackForearm = Blend(8f, stand.BackForearm);
            pose.HeadTilt = Blend(16f, stand.HeadTilt);
            pose.Chest = Blend(-2f, 0f);
            pose.CloakSway = Blend(32f, stand.CloakSway);
            return pose;
        }

        /// <summary>Wind up, commit, follow through — the same beat as <see cref="SwingPose"/>.</summary>
        private static float Commit(float t)
        {
            var commit = Mathf.Clamp01((t - 0.25f) / 0.45f);
            return commit * commit * (3f - 2f * commit);
        }

        // ---------- rasterising ----------

        private static byte[] Render(PlayerAnimationState state, float t, float reachBonus,
            float? previousT = null)
        {
            var canvas = new Canvas(Width, Height);
            float? smearFrom = null;
            if (previousT.HasValue && IsSwing(state))
            {
                smearFrom = ForearmOf(PoseFor(state, previousT.Value, reachBonus));
            }

            var pose = PoseFor(state, t, reachBonus);
            Draw(canvas, pose, reachBonus > 0f, smearFrom);
            // The player is the brightest, most outlined figure on screen (Visual Bible §13).
            canvas.Silhouette(new Color32(8, 7, 6, 235), Rim, 0.8f, 0.12f);
            // After the outline, so the trail stays light and never reads as part of the body.
            Streaks(canvas, pose);
            return canvas.EncodePng();
        }

        private static void Skeleton(Pose pose, out Vector2 hip, out Vector2 shoulder)
        {
            hip = new Vector2(Width * 0.5f + pose.HipX, pose.HipY * pose.Squash);
            shoulder = hip + Canvas.Rotate(new Vector2(0f, (46f + pose.Chest) * pose.Squash), pose.Lean);
        }

        private static void Draw(Canvas canvas, Pose pose, bool greatsword = false, float? smearFrom = null)
        {
            Skeleton(pose, out var hip, out var shoulder);
            var lean = pose.Lean;
            var head = shoulder + Canvas.Rotate(new Vector2(0f, 17f), lean + pose.HeadTilt);

            // Back limbs first: depth comes from draw order, not from a z buffer.
            var backHand = canvas.Limb(shoulder, pose.BackArm + lean, 20f, pose.BackForearm, 18f, 6f,
                Charcoal);
            if (pose.Flask)
            {
                Sword(canvas, backHand, pose.BackArm + lean + pose.BackForearm - 80f, greatsword);
            }

            canvas.Limb(hip, pose.BackThigh, 24f, pose.BackShin, 24f, 8f, Charcoal);

            Cloak(canvas, shoulder, hip, lean, pose.CloakSway, pose.CloakFlutter);

            // Torso, tapering from shoulders to hips. A squashed body widens and a stretched one
            // thins, so the volume reads as kept (squash and stretch).
            var girth = Mathf.Clamp(1f / Mathf.Sqrt(Mathf.Max(0.5f, pose.Squash)), 0.92f, 1.12f);
            canvas.Thick(hip, shoulder, 13f * girth, 10f * girth, Charcoal);
            canvas.Thick(hip, hip + Canvas.Rotate(new Vector2(0f, 8f), lean), 14f * girth, 14f * girth, Leather);

            canvas.Limb(hip, pose.FrontThigh, 24f, pose.FrontShin, 24f, 9f, CloakTone);
            canvas.Thick(shoulder + Canvas.Rotate(new Vector2(0f, -2f), lean),
                shoulder + Canvas.Rotate(new Vector2(3f, 2f), lean), 9f, 8f, Steel);
            var hand = canvas.Limb(shoulder, pose.FrontArm + lean, 20f, pose.FrontForearm, 18f, 6f,
                CloakTone);
            if (pose.Flask)
            {
                Flask(canvas, hand, pose.FrontArm + lean + pose.FrontForearm);
            }
            else
            {
                if (smearFrom.HasValue)
                {
                    Smear(canvas, hand, smearFrom.Value, ForearmOf(pose), greatsword);
                }

                Sword(canvas, hand, ForearmOf(pose), greatsword);
            }

            // Without a neck the head reads as a separate object floating above the shoulders.
            canvas.Thick(shoulder, head, 8f, 10f, Charcoal);
            Hood(canvas, head, lean + pose.HeadTilt);

            // The one readability accent the brief allows: a pale sash across the chest.
            var sashA = shoulder + Canvas.Rotate(new Vector2(-7f, -4f), lean);
            var sashB = hip + Canvas.Rotate(new Vector2(7f, 14f), lean);
            canvas.Thick(sashA, sashB, 3f, 3f, Sash);

            if (pose.Alpha < 0.999f)
            {
                canvas.Fade(pose.Alpha);
            }
        }

        /// <summary>
        /// The sword in the front hand. It used to be a separate white bar on a mount beside the
        /// figure; drawn into the frame it follows every pose of the swing, and the blade is the
        /// brightest line on the character — the one thing the eye has to find in a fight.
        /// </summary>
        private static void Sword(Canvas canvas, Vector2 hand, float forearmAngle, bool greatsword)
        {
            // Angles are measured from straight down, turning forward. A lowered arm holds the
            // blade forward and low; a raised one carries it along the arm and over the head, so
            // the swing's arc is the blade's arc.
            var bladeAngle = BladeAngle(forearmAngle);
            var blade = Canvas.Rotate(new Vector2(0f, -1f), -bladeAngle).normalized;
            var across = new Vector2(-blade.y, blade.x);
            var length = greatsword ? 84f : 62f;
            var width = greatsword ? 7f : 4.5f;
            var guard = hand + blade * 4f;
            var tip = guard + blade * length;

            canvas.Thick(hand - blade * (greatsword ? 13f : 9f), hand + blade * 2f, 3.5f, 3.5f, Leather);
            canvas.Thick(guard, tip - blade * 6f, width, width * 0.8f, BladeSteel);
            canvas.Triangle(tip - blade * 6f + across * width * 0.4f,
                tip - blade * 6f - across * width * 0.4f, tip, BladeSteel);
            // A bright edge line on the leading side: reads as polished metal catching the light.
            canvas.Thick(guard + across * width * 0.25f + blade * 3f, tip - blade * 8f + across * width * 0.2f,
                1.4f, 1f, BladeEdge);
            canvas.Thick(guard - across * (greatsword ? 11f : 8f), guard + across * (greatsword ? 11f : 8f),
                3.5f, 3.5f, Guard);
            canvas.Ellipse(hand - blade * (greatsword ? 14f : 10f), 2.8f, 2.8f, Guard);
        }

        private static float BladeAngle(float forearmAngle) => forearmAngle * 0.8f + 40f;

        private static float ForearmOf(Pose pose) => pose.FrontArm + pose.Lean + pose.FrontForearm;

        private static bool IsSwing(PlayerAnimationState state) =>
            state == PlayerAnimationState.Attack1 || state == PlayerAnimationState.Attack2
            || state == PlayerAnimationState.Attack3 || state == PlayerAnimationState.Heavy
            || state == PlayerAnimationState.AttackUp || state == PlayerAnimationState.AttackDown
            || state == PlayerAnimationState.GroundBreaker || state == PlayerAnimationState.DashStrike
            || state == PlayerAnimationState.ShadowStrike || state == PlayerAnimationState.ActiveSkill;

        /// <summary>A blade that turned at least this far since the frame before is drawn smeared.</summary>
        public const float SmearMinSweep = 40f;

        /// <summary>The smear's pale trailing end: the hero's cold edge, dimmed.</summary>
        private static readonly Color32 SmearPale = new Color32(150, 168, 194, 0xFF);

        /// <summary>
        /// The swing's fastest frame (the user's choice, 2026-09-28): the blade drawn as a crescent
        /// over the arc it covered since the frame before, full and bright at the blade, thinning
        /// and paling towards where it came from. The slash effect still plays over it.
        /// </summary>
        private static void Smear(Canvas canvas, Vector2 hand, float fromForearm, float toForearm,
            bool greatsword)
        {
            var from = BladeAngle(fromForearm);
            var sweep = BladeAngle(toForearm) - from;
            if (Mathf.Abs(sweep) < SmearMinSweep)
            {
                return;
            }

            var length = (greatsword ? 84f : 62f) + 4f;
            var steps = Mathf.CeilToInt(Mathf.Abs(sweep) / 5f);
            Vector2 Direction(float u) => Canvas.Rotate(new Vector2(0f, -1f), -(from + sweep * u)).normalized;
            float Outer(float u) => length * (0.62f + 0.38f * u);
            float Inner(float u) => Mathf.Min(length * (0.22f + 0.5f * (1f - u)), Outer(u) - 1.5f);
            for (var k = 0; k < steps; k++)
            {
                var u0 = k / (float)steps;
                var u1 = (k + 1) / (float)steps;
                var inner0 = hand + Direction(u0) * Inner(u0);
                var outer0 = hand + Direction(u0) * Outer(u0);
                var inner1 = hand + Direction(u1) * Inner(u1);
                var outer1 = hand + Direction(u1) * Outer(u1);
                var tone = Color32.Lerp(SmearPale, BladeEdge, (u0 + u1) * 0.5f);
                canvas.Triangle(inner0, outer0, outer1, tone);
                canvas.Triangle(inner0, outer1, inner1, tone);
            }
        }

        /// <summary>
        /// A small flask in the hand, its neck along the forearm. The liquid is the one warm,
        /// bright spot on the figure while drinking — the same amber as the HUD's flasks.
        /// </summary>
        private static void Flask(Canvas canvas, Vector2 hand, float forearmAngle)
        {
            var along = Canvas.Rotate(new Vector2(0f, -1f), -forearmAngle).normalized;
            var body = hand - along * 3f;
            canvas.Ellipse(body, 6.5f, 6.5f, FlaskGlass);
            canvas.Ellipse(body, 4.8f, 4.8f, FlaskLiquid);
            canvas.Thick(body + along * 5f, body + along * 11f, 3.5f, 3f, FlaskGlass);
            canvas.Thick(body + along * 10f, body + along * 12.5f, 4f, 4f, Leather);
            canvas.Ellipse(body + new Vector2(-1.8f, 1.8f), 1.4f, 1.4f, BladeEdge);
        }

        private static readonly Color32 FlaskGlass = new Color32(107, 90, 66, 0xFF);
        private static readonly Color32 FlaskLiquid = new Color32(201, 161, 74, 0xFF);
        private static readonly Color32 BladeSteel = new Color32(0x9A, 0xA3, 0xAE, 0xFF);
        private static readonly Color32 BladeEdge = new Color32(0xE6, 0xEC, 0xF2, 0xFF);
        private static readonly Color32 Guard = new Color32(0xB0, 0x8A, 0x3E, 0xFF);

        private static void Hood(Canvas canvas, Vector2 head, float tilt)
        {
            canvas.Ellipse(head, 12f, 14f, Charcoal);
            // The peak has to clear the skull to be seen at all; inside it, the head just reads as
            // a helmet, and the brief asks for a hood to be the first thing recognised.
            var baseBack = head + Canvas.Rotate(new Vector2(-12f, 1f), tilt);
            var baseFront = head + Canvas.Rotate(new Vector2(1f, 11f), tilt);
            var peak = head + Canvas.Rotate(new Vector2(-13f, 25f), tilt);
            canvas.Triangle(baseBack, baseFront, peak, Charcoal);
            // The face, half hidden: a pale sliver of it under the brim, the brow in the hood's
            // shadow. Muted skin, never white — the hero must not read as a masked figure.
            canvas.Ellipse(head + Canvas.Rotate(new Vector2(5f, -3f), tilt), 4.5f, 5.5f, Face);
            canvas.Ellipse(head + Canvas.Rotate(new Vector2(4f, 1.5f), tilt), 5.5f, 3.5f, CloakTone);
        }

        /// <summary>
        /// The half-cloak, cut asymmetrically. It is the strongest line in the silhouette, so it is
        /// drawn as one solid shape rather than as trailing strips that would break up at this size.
        /// </summary>
        private static void Cloak(Canvas canvas, Vector2 shoulder, Vector2 hip, float lean,
            float sway, float flutter = 0f)
        {
            CloakBackEdge(shoulder, hip, lean, sway, out var topBack, out var hemBack);
            var topFront = shoulder + Canvas.Rotate(new Vector2(3f, 4f), lean);
            // The hem hangs from the hips, so it follows the sway rather than the torso lean; the
            // shoulder anchors above have already moved with the lean. It is cut short at the front
            // and long at the back, which is the asymmetry the concept brief asks for.
            var hemFront = hip + Canvas.Rotate(new Vector2(5f, -34f), -sway * 0.7f);
            canvas.Triangle(topBack, topFront, hemFront, CloakTone);
            canvas.Triangle(topBack, hemFront, hemBack, CloakTone);
            // The lining turned out along the back edge, where the cloak swings open.
            var inward = (hemFront - hemBack).normalized * 2.5f;
            canvas.Thick(topBack + inward, hemBack + inward, 2.5f, 4f, Lining);
            if (Mathf.Abs(flutter) > 0.02f)
            {
                // A tongue of cloth at the back corner, flicked by the air: one smooth triangle
                // that grows out of the hem, so the shape stays one piece.
                var down = (hemBack - topBack).normalized;
                var side = new Vector2(-down.y, down.x);
                var hemMid = Vector2.Lerp(hemBack, hemFront, 0.45f);
                var tip = hemBack + down * (4f + 5f * Mathf.Abs(flutter)) + side * flutter * 8f;
                canvas.Triangle(hemBack - down * 6f, hemMid, tip, CloakTone);
                canvas.Thick(hemBack + inward, tip + inward * 0.5f, 3f, 1.5f, Lining);
            }
        }

        private static readonly Color32 StreakCold = new Color32(176, 196, 222, 0xFF);
        private static readonly Color32 StreakShadow = new Color32(150, 92, 220, 0xFF);

        /// <summary>
        /// The dash's trail drawn into the frame: five tapered streaks reaching back from behind
        /// the body, brightest at the body and gone at their ends. Painted only on empty pixels,
        /// after the outline, so they sit behind the figure and never thicken its silhouette.
        /// The runtime afterimages (DashAfterimages) still play over it.
        /// </summary>
        private static void Streaks(Canvas canvas, Pose pose)
        {
            if (pose.Streak <= 0.02f)
            {
                return;
            }

            Skeleton(pose, out var hip, out var shoulder);
            var tone = pose.ShadowStreak ? StreakShadow : StreakCold;
            var lines = new[]
            {
                // height between hip (0) and shoulder (1), length, width
                (-0.35f, 70f, 3.2f), (0.05f, 96f, 3.8f), (0.4f, 82f, 3.4f), (0.75f, 60f, 2.6f), (1.05f, 44f, 2f)
            };
            foreach (var (along, length, width) in lines)
            {
                var start = Vector2.Lerp(hip, shoulder, along) + new Vector2(-14f, 0f);
                var reach = length * pose.Streak;
                var steps = Mathf.CeilToInt(reach);
                for (var k = 0; k <= steps; k++)
                {
                    var u = k / (float)Mathf.Max(1, steps);
                    var centre = start + new Vector2(-reach * u, 0f);
                    var radius = Mathf.Lerp(width, 0.5f, u) * 0.5f;
                    var alpha = (byte)Mathf.RoundToInt(170f * pose.Streak * (1f - u) * (1f - u));
                    for (var y = Mathf.FloorToInt(centre.y - radius); y <= Mathf.CeilToInt(centre.y + radius); y++)
                    {
                        for (var x = Mathf.FloorToInt(centre.x - radius); x <= Mathf.CeilToInt(centre.x + radius); x++)
                        {
                            if (canvas.Alpha(x, y) == 0 && alpha > 0)
                            {
                                canvas.Set(x, y, new Color32(tone.r, tone.g, tone.b, alpha));
                            }
                        }
                    }
                }
            }
        }

        /// <summary>The cloak's back edge, from the shoulder to the long back corner of the hem.</summary>
        private static void CloakBackEdge(Vector2 shoulder, Vector2 hip, float lean, float sway,
            out Vector2 top, out Vector2 hem)
        {
            top = shoulder + Canvas.Rotate(new Vector2(-13f, 6f), lean);
            hem = hip + Canvas.Rotate(new Vector2(-17f, -46f), -sway);
        }
    }
}
