using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The Ashen Capital's four enemies (item 2 of the big list, the user's choices of 2026-09-28).
    ///
    /// Three are soldiers of the burnt court and share the humanoid rig with the forest's: the
    /// crossbowman is drawn on the archer's poses (a heavier, helmed build), the palace priest on
    /// the shaman's (a pale robe and gilded staff), the gargoyle on the spearman's (a broad stone
    /// body with horns and a short claw for a weapon). Only their builds are new, so a pose tuned
    /// for one fight reads the same in the other. The raven is a bird, on a small rig of its own:
    /// a body, two wings, a head and a beak, flapping, swooping and falling.
    /// </summary>
    public static partial class PlaceholderEnemyArt
    {
        public const string CrossbowmanFolder = "Assets/Game/Art/Characters/Enemies/RoyalCrossbowman";
        public const string PriestFolder = "Assets/Game/Art/Characters/Enemies/PalacePriest";
        public const string GargoyleFolder = "Assets/Game/Art/Characters/Enemies/StoneGargoyle";
        public const string RavenFolder = "Assets/Game/Art/Characters/Enemies/AshRaven";

        private static readonly Color32 AshGrey = new Color32(0x4A, 0x45, 0x44, 0xFF);
        private static readonly Color32 AshDark = new Color32(0x2A, 0x26, 0x27, 0xFF);
        private static readonly Color32 CourtRed = new Color32(0x4E, 0x1C, 0x20, 0xFF);
        private static readonly Color32 PriestWhite = new Color32(0x8E, 0x86, 0x7C, 0xFF);
        private static readonly Color32 StoneBody = new Color32(0x55, 0x52, 0x50, 0xFF);
        private static readonly Color32 StoneDark = new Color32(0x36, 0x34, 0x34, 0xFF);
        private static readonly Color32 EmberGlow = new Color32(0xE0, 0x7A, 0x3A, 0xFF);

        // Properties, not fields: the forest's arrays live in another file of this partial class,
        // and static fields across files are not initialised in an order that can be relied on.
        public static Spec<EnemyAnimationState>[] CrossbowmanStates => ArcherStates;

        public static Spec<EnemyAnimationState>[] PriestStates => ShamanStates;

        public static readonly Spec<EnemyAnimationState>[] GargoyleStates =
        {
            // Idle is the statue: held still, as it stands until someone comes close.
            new Spec<EnemyAnimationState>(EnemyAnimationState.Idle, 2, 2f, true),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Patrol, 8, 10f, true),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Alert, 8, 10f, false),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Chase, 8, 10f, true),
            new Spec<EnemyAnimationState>(EnemyAnimationState.AttackLight, 20, 24f, false),
            new Spec<EnemyAnimationState>(EnemyAnimationState.AttackHeavy, 24, 24f, false),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Hurt, 5, 24f, false),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Stunned, 6, 12f, true),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Death, 12, 12f, false)
        };

        public static readonly Spec<EnemyAnimationState>[] RavenStates =
        {
            new Spec<EnemyAnimationState>(EnemyAnimationState.Idle, 6, 12f, true),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Patrol, 6, 12f, true),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Alert, 5, 14f, false),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Chase, 6, 16f, true),
            // Wings drawn back and up for the tell, then folded for the dive.
            new Spec<EnemyAnimationState>(EnemyAnimationState.AttackLight, 16, 24f, false),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Hurt, 5, 24f, false),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Stunned, 6, 12f, true),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Death, 10, 12f, false)
        };

        // ---------- generation ----------

        public static Dictionary<EnemyAnimationState, Sprite[]> GenerateCrossbowman() =>
            Generate(CrossbowmanFolder, "enemy_crossbowman", CrossbowmanStates,
                (state, t) => Render(Actor.Archer, CrossbowmanBuild(),
                    Telegraphed(Actor.Archer, state, t, ForestPose(Actor.Archer, state, t))));

        public static Dictionary<EnemyAnimationState, Sprite[]> GeneratePriest() =>
            Generate(PriestFolder, "enemy_priest", PriestStates,
                (state, t) => Render(Actor.Shaman, PriestBuild(),
                    Telegraphed(Actor.Shaman, state, t, ForestPose(Actor.Shaman, state, t))));

        public static Dictionary<EnemyAnimationState, Sprite[]> GenerateGargoyle() =>
            Generate(GargoyleFolder, "enemy_gargoyle", GargoyleStates,
                (state, t) => Render(Actor.Spearman, GargoyleBuild(),
                    Telegraphed(Actor.Spearman, state, t, GargoylePose(state, t))));

        public static Dictionary<EnemyAnimationState, Sprite[]> GenerateRaven() =>
            Generate(RavenFolder, "enemy_raven", RavenStates, RenderRaven);

        // ---------- builds ----------

        /// <summary>0.9 × 1.75 units: the archer's body in the court's red and plate, helmed.</summary>
        private static Build CrossbowmanBuild()
        {
            var build = ForestBuildFor(Actor.Archer);
            build.Width = 160;
            build.TorsoWidth = 28f;
            build.ArmWidth = 7f;
            build.Armor = AshGrey;
            build.Cloth = CourtRed;
            build.Metal = DarkSteel;
            build.Trim = FadedGold;
            build.Head = HeadGear.Helm;
            build.Growth = false;
            return build;
        }

        /// <summary>A pale robe over ash, a gilded staff, the hood of the palace chapel.</summary>
        private static Build PriestBuild()
        {
            var build = ForestBuildFor(Actor.Shaman);
            build.Armor = PriestWhite;
            build.Cloth = PriestWhite;
            build.Metal = FadedGold;
            build.Trim = FadedGold;
            build.Skin = BoneWhite;
            build.Rot = EmberGlow;
            build.Head = HeadGear.Hood;
            build.Growth = false;
            return build;
        }

        /// <summary>1.1 × 1.8 units of stone: broad, horned, a short claw where a soldier holds a blade.</summary>
        private static Build GargoyleBuild()
        {
            var build = ForestBuildFor(Actor.Spearman);
            build.Width = 220;
            build.Height = 186;
            build.TorsoWidth = 40f;
            build.ArmWidth = 11f;
            build.LegWidth = 13f;
            build.HeadRx = 15f;
            build.Armor = StoneBody;
            build.Cloth = StoneDark;
            build.Metal = StoneDark;
            build.Trim = AshGrey;
            build.Skin = StoneBody;
            build.Rot = EmberGlow;
            build.Weapon = WeaponShape.Blade;
            build.SwordLength = 34f;
            build.SwordWidth = 7f;
            build.Head = HeadGear.Antlers;
            build.Growth = false;
            return build;
        }

        /// <summary>
        /// The spearman's poses with the gargoyle's own idle: crouched on its plinth, head down,
        /// not a breath of movement — the thing it is waiting for is the player walking past.
        /// </summary>
        private static Pose GargoylePose(EnemyAnimationState state, float t)
        {
            if (state != EnemyAnimationState.Idle)
            {
                return ForestPose(Actor.Spearman, state, t);
            }

            var pose = ForestPose(Actor.Spearman, EnemyAnimationState.Idle, 0f);
            pose.HipY -= 16f;
            pose.Lean = 16f;
            pose.HeadTilt = 18f;
            pose.FrontThigh = 40f;
            pose.FrontShin = -60f;
            pose.BackThigh = -30f;
            pose.BackShin = 70f;
            pose.FrontArm = 20f;
            pose.FrontForearm = 30f;
            pose.SwordAngle = 150f;
            return pose;
        }

        /// <summary>The humanoid draw with a build of the caller's, for bodies that share poses.</summary>
        private static byte[] Render(Actor actor, Build build, Pose pose)
        {
            var canvas = new Canvas(build.Width, build.Height);
            Draw(canvas, build, pose, actor);
            canvas.Silhouette(new Color32(6, 5, 5, 225), EnemyRim, 0.42f);
            return canvas.EncodePng();
        }

        // ---------- the raven ----------

        private const int RavenWidth = 140;
        private const int RavenHeight = 110;

        /// <summary>
        /// A black bird the size of a dog, drawn from its middle: the pivot sits at the bottom of
        /// the canvas like every enemy's, the body 45 px above it, so the flight code can treat
        /// the transform like anyone's feet.
        /// </summary>
        private static byte[] RenderRaven(EnemyAnimationState state, float t)
        {
            var canvas = new Canvas(RavenWidth, RavenHeight);
            var body = new Vector2(RavenWidth * 0.5f, 45f);
            var flap = Mathf.Sin(t * Mathf.PI * 2f);
            float wing;
            var tilt = 0f;
            var folded = false;
            var eye = 0f;
            switch (state)
            {
                case EnemyAnimationState.AttackLight:
                    // Rearing back with the wings high for the tell, then folded into the dive.
                    var tell = Mathf.Clamp01(t / 0.55f);
                    if (t < 0.55f)
                    {
                        wing = Mathf.Lerp(20f, 70f, tell);
                        tilt = -18f * tell;
                        eye = tell;
                    }
                    else
                    {
                        wing = -10f;
                        tilt = 32f;
                        folded = true;
                        eye = 1f;
                    }

                    break;
                case EnemyAnimationState.Hurt:
                    wing = -30f + 20f * t;
                    tilt = -24f;
                    break;
                case EnemyAnimationState.Stunned:
                    wing = -20f + 10f * flap;
                    tilt = 20f;
                    break;
                case EnemyAnimationState.Death:
                    wing = -50f;
                    tilt = Mathf.Lerp(0f, 150f, t);
                    body.y = Mathf.Lerp(45f, 18f, t);
                    break;
                case EnemyAnimationState.Alert:
                    wing = 40f * (1f - t) + 10f;
                    eye = t;
                    break;
                default:
                    // Wings beat; the chase beats faster (its clip is at a higher rate).
                    wing = 35f * flap;
                    body.y += 3f * flap;
                    break;
            }

            var ink = new Color32(0x16, 0x14, 0x17, 0xFF);
            var feather = new Color32(0x2A, 0x26, 0x2C, 0xFF);
            // The far wing first, dimmer; then the body, head and beak; then the near wing.
            Wing(canvas, body + new Vector2(-4f, 6f), wing * 0.8f + 8f, feather, folded);
            canvas.Ellipse(body, 22f, 13f, ink);
            var head = body + Canvas.Rotate(new Vector2(20f, 6f), tilt);
            canvas.Ellipse(head, 10f, 9f, ink);
            var beakBase = head + Canvas.Rotate(new Vector2(8f, -1f), tilt);
            canvas.Triangle(beakBase + Canvas.Rotate(new Vector2(0f, 4f), tilt),
                beakBase + Canvas.Rotate(new Vector2(0f, -3f), tilt),
                beakBase + Canvas.Rotate(new Vector2(14f, -2f), tilt), AshGrey);
            // Tail feathers.
            var tail = body + Canvas.Rotate(new Vector2(-20f, 0f), tilt);
            canvas.Triangle(tail + new Vector2(0f, 5f), tail + new Vector2(0f, -5f),
                tail + Canvas.Rotate(new Vector2(-20f, -4f), tilt), ink);
            // The eye: an ember, flaring through the tell.
            canvas.Ellipse(head + Canvas.Rotate(new Vector2(3f, 2f), tilt), 2.2f + eye, 2.2f + eye,
                Color32.Lerp(SicklyEye, EmberGlow, eye));
            Wing(canvas, body + new Vector2(2f, 4f), wing, ink, folded);
            canvas.Silhouette(new Color32(6, 5, 5, 225), EnemyRim, 0.42f);
            return canvas.EncodePng();
        }

        /// <summary>A wing from the shoulder: three feathered segments, spread or folded along the back.</summary>
        private static void Wing(Canvas canvas, Vector2 shoulder, float angle, Color32 color, bool folded)
        {
            var span = folded ? 26f : 48f;
            var tip = shoulder + Canvas.Rotate(new Vector2(-span * 0.4f, span), -angle * 0.9f);
            canvas.Thick(shoulder, tip, 12f, 3f, color);
            for (var i = 0; i < 4; i++)
            {
                var along = Vector2.Lerp(shoulder, tip, 0.35f + i * 0.18f);
                var back = along + Canvas.Rotate(new Vector2(-14f, -4f), -angle * 0.5f);
                canvas.Thick(along, back, 6f, 1.5f, color);
            }
        }
    }
}
