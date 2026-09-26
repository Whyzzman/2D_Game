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
    /// are regenerated on every build; an artist's frame goes to the mirrored path under
    /// `Art/Source/Final/`, and <see cref="PlaceholderImport.WriteIfChanged"/> writes it in place
    /// of the drawn one (gamedesign/ART_ASSET_LIST.md). Nothing in code changes.
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
            new StateSpec(PlayerAnimationState.Idle, 4, 8f, true),
            new StateSpec(PlayerAnimationState.Run, 6, 12f, true),
            new StateSpec(PlayerAnimationState.JumpStart, 2, 24f, false),
            new StateSpec(PlayerAnimationState.JumpRise, 2, 12f, true),
            new StateSpec(PlayerAnimationState.Fall, 2, 12f, true),
            new StateSpec(PlayerAnimationState.Land, 2, 24f, false),
            new StateSpec(PlayerAnimationState.Dash, 3, 24f, false),
            new StateSpec(PlayerAnimationState.AirDash, 3, 24f, false),
            new StateSpec(PlayerAnimationState.WallSlide, 2, 12f, true),
            new StateSpec(PlayerAnimationState.WallJump, 2, 24f, false),
            new StateSpec(PlayerAnimationState.WallRun, 4, 12f, true),
            new StateSpec(PlayerAnimationState.Attack1, 4, 24f, false),
            new StateSpec(PlayerAnimationState.Attack2, 4, 24f, false),
            new StateSpec(PlayerAnimationState.Attack3, 5, 24f, false),
            new StateSpec(PlayerAnimationState.Heavy, 6, 24f, false),
            new StateSpec(PlayerAnimationState.Block, 2, 12f, true),
            new StateSpec(PlayerAnimationState.BlockImpact, 2, 24f, false),
            new StateSpec(PlayerAnimationState.Parry, 3, 24f, false),
            new StateSpec(PlayerAnimationState.GuardBroken, 3, 12f, false),
            new StateSpec(PlayerAnimationState.Hurt, 2, 24f, false),
            new StateSpec(PlayerAnimationState.Stunned, 4, 12f, true),
            new StateSpec(PlayerAnimationState.Death, 6, 12f, false),
            new StateSpec(PlayerAnimationState.CastStart, 4, 12f, true),
            new StateSpec(PlayerAnimationState.CastRelease, 3, 24f, false),
            new StateSpec(PlayerAnimationState.GroundBreaker, 6, 24f, false),
            new StateSpec(PlayerAnimationState.DashStrike, 5, 24f, false),
            new StateSpec(PlayerAnimationState.ShadowDashStart, 3, 24f, false),
            new StateSpec(PlayerAnimationState.ShadowDashLoop, 2, 24f, true),
            new StateSpec(PlayerAnimationState.ShadowDashEnd, 3, 24f, false),
            new StateSpec(PlayerAnimationState.ShadowStrike, 5, 24f, false),
            new StateSpec(PlayerAnimationState.WeaponSwitch, 3, 24f, false),
            new StateSpec(PlayerAnimationState.ActiveSkill, 4, 24f, false)
        };

        /// <summary>
        /// The swings a two-handed weapon replaces. Fewer frames per second and more reach: a
        /// greatsword is not a sword played slowly, and having the placeholder already differ
        /// proves the per-weapon override path works rather than merely existing.
        /// </summary>
        public static readonly StateSpec[] GreatswordStates =
        {
            new StateSpec(PlayerAnimationState.Attack1, 5, 18f, false),
            new StateSpec(PlayerAnimationState.Attack2, 5, 18f, false),
            new StateSpec(PlayerAnimationState.Attack3, 6, 18f, false),
            new StateSpec(PlayerAnimationState.Heavy, 7, 18f, false)
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
                    var phase = spec.Frames <= 1 ? 0f : i / (float)(spec.Frames - 1);
                    var loopPhase = spec.Loops ? i / (float)spec.Frames : phase;
                    PlaceholderImport.WriteIfChanged(path, Render(spec.State, spec.Loops ? loopPhase : phase,
                        reachBonus));
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
            public float Alpha;

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

        private static Pose PoseFor(PlayerAnimationState state, float t, float reachBonus)
        {
            var reachScale = 1f + Mathf.Max(0f, reachBonus);
            var pose = Pose.Stand;
            switch (state)
            {
                case PlayerAnimationState.Idle:
                    pose.HipY += Sin01(t) * 1.5f;
                    pose.HeadTilt = Sin01(t) * 2f;
                    pose.CloakSway = Sin01(t) * 3f;
                    break;

                case PlayerAnimationState.Run:
                    // Counter-swing: the arm opposite the leading leg comes forward, which is what
                    // makes a run read as a run rather than a shuffle.
                    pose.Lean = 14f;
                    pose.HipY = 76f + Mathf.Abs(Sin01(t)) * 3f;
                    pose.FrontThigh = Sin01(t) * 38f;
                    pose.BackThigh = -Sin01(t) * 38f;
                    pose.FrontShin = -Mathf.Max(0f, -Sin01(t)) * 45f - 6f;
                    pose.BackShin = -Mathf.Max(0f, Sin01(t)) * 45f - 6f;
                    pose.FrontArm = -Sin01(t) * 32f;
                    pose.BackArm = Sin01(t) * 32f;
                    pose.FrontForearm = 40f;
                    pose.BackForearm = 35f;
                    pose.CloakSway = -14f;
                    break;

                case PlayerAnimationState.JumpStart:
                    pose.Squash = Mathf.Lerp(0.86f, 1.05f, t);
                    pose.HipY = Mathf.Lerp(62f, 84f, t);
                    pose.FrontThigh = Mathf.Lerp(34f, 12f, t);
                    pose.BackThigh = Mathf.Lerp(-30f, -10f, t);
                    pose.FrontShin = Mathf.Lerp(-56f, -10f, t);
                    pose.BackShin = Mathf.Lerp(-52f, -12f, t);
                    pose.FrontArm = Mathf.Lerp(-40f, 34f, t);
                    pose.BackArm = Mathf.Lerp(-46f, 26f, t);
                    break;

                case PlayerAnimationState.JumpRise:
                    pose.Lean = 8f;
                    pose.HipY = 80f + Sin01(t) * 1f;
                    pose.FrontThigh = 26f;
                    pose.FrontShin = -34f;
                    pose.BackThigh = -14f;
                    pose.BackShin = -30f;
                    pose.FrontArm = 30f;
                    pose.BackArm = 24f;
                    pose.CloakSway = -22f;
                    break;

                case PlayerAnimationState.Fall:
                    pose.Lean = -4f;
                    pose.HipY = 80f;
                    pose.FrontThigh = 16f;
                    pose.FrontShin = -20f;
                    pose.BackThigh = -20f;
                    pose.BackShin = -16f;
                    pose.FrontArm = 42f;
                    pose.BackArm = 36f;
                    pose.CloakSway = 24f + Sin01(t) * 4f;
                    break;

                case PlayerAnimationState.Land:
                    pose.Squash = Mathf.Lerp(0.82f, 0.96f, t);
                    pose.HipY = Mathf.Lerp(58f, 72f, t);
                    pose.Lean = 16f;
                    pose.FrontThigh = 40f;
                    pose.FrontShin = -62f;
                    pose.BackThigh = -34f;
                    pose.BackShin = -58f;
                    pose.FrontArm = -34f;
                    pose.BackArm = -28f;
                    pose.CloakSway = 18f;
                    break;

                case PlayerAnimationState.Dash:
                case PlayerAnimationState.AirDash:
                    pose.Lean = 34f;
                    pose.HipY = 68f;
                    pose.FrontThigh = 30f;
                    pose.FrontShin = -20f;
                    pose.BackThigh = -44f;
                    pose.BackShin = -26f;
                    pose.FrontArm = -48f;
                    pose.BackArm = -58f;
                    pose.FrontForearm = 12f;
                    pose.CloakSway = -40f;
                    pose.Alpha = state == PlayerAnimationState.AirDash ? 0.9f : 1f;
                    break;

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
            pose.CloakSway = Mathf.Lerp(18f, -26f, eased);
            pose.Squash = 1f;
            pose.Alpha = alpha;
            return pose;
        }

        // ---------- rasterising ----------

        private static byte[] Render(PlayerAnimationState state, float t, float reachBonus)
        {
            var canvas = new Canvas(Width, Height);
            Draw(canvas, PoseFor(state, t, reachBonus), reachBonus > 0f);
            // The player is the brightest, most outlined figure on screen (Visual Bible §13).
            canvas.Silhouette(new Color32(8, 7, 6, 235), new Color32(170, 198, 235, 255), 0.55f,
                0.1f);
            return canvas.EncodePng();
        }

        private static void Draw(Canvas canvas, Pose pose, bool greatsword = false)
        {
            var hip = new Vector2(Width * 0.5f + pose.HipX, pose.HipY * pose.Squash);
            var lean = pose.Lean;
            var shoulder = hip + Canvas.Rotate(new Vector2(0f, 46f * pose.Squash), lean);
            var head = shoulder + Canvas.Rotate(new Vector2(0f, 17f), lean + pose.HeadTilt);

            // Back limbs first: depth comes from draw order, not from a z buffer.
            canvas.Limb(shoulder, pose.BackArm + lean, 20f, pose.BackForearm, 18f, 6f, Charcoal);
            canvas.Limb(hip, pose.BackThigh, 24f, pose.BackShin, 24f, 8f, Charcoal);

            Cloak(canvas, shoulder, hip, lean, pose.CloakSway);

            // Torso, tapering from shoulders to hips.
            canvas.Thick(hip, shoulder, 13f, 10f, Charcoal);
            canvas.Thick(hip, hip + Canvas.Rotate(new Vector2(0f, 8f), lean), 14f, 14f, Leather);

            canvas.Limb(hip, pose.FrontThigh, 24f, pose.FrontShin, 24f, 9f, CloakTone);
            canvas.Thick(shoulder + Canvas.Rotate(new Vector2(0f, -2f), lean),
                shoulder + Canvas.Rotate(new Vector2(3f, 2f), lean), 9f, 8f, Steel);
            var hand = canvas.Limb(shoulder, pose.FrontArm + lean, 20f, pose.FrontForearm, 18f, 6f,
                CloakTone);
            Sword(canvas, hand, pose.FrontArm + lean + pose.FrontForearm, greatsword);

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
            var bladeAngle = forearmAngle * 0.8f + 40f;
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
            // A sliver of shadow where the face would be: the brief keeps it half hidden.
            canvas.Ellipse(head + Canvas.Rotate(new Vector2(5f, -2f), tilt), 5f, 6f, CloakTone);
        }

        /// <summary>
        /// The half-cloak, cut asymmetrically. It is the strongest line in the silhouette, so it is
        /// drawn as one solid shape rather than as trailing strips that would break up at this size.
        /// </summary>
        private static void Cloak(Canvas canvas, Vector2 shoulder, Vector2 hip, float lean,
            float sway)
        {
            var topBack = shoulder + Canvas.Rotate(new Vector2(-13f, 6f), lean);
            var topFront = shoulder + Canvas.Rotate(new Vector2(3f, 4f), lean);
            // The hem hangs from the hips, so it follows the sway rather than the torso lean; the
            // shoulder anchors above have already moved with the lean. It is cut short at the front
            // and long at the back, which is the asymmetry the concept brief asks for.
            var hemBack = hip + Canvas.Rotate(new Vector2(-17f, -46f), -sway);
            var hemFront = hip + Canvas.Rotate(new Vector2(5f, -34f), -sway * 0.7f);
            canvas.Triangle(topBack, topFront, hemFront, CloakTone);
            canvas.Triangle(topBack, hemFront, hemBack, CloakTone);
        }
    }
}
