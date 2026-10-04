using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// Placeholder silhouettes for the Withered Forest: the Corrupted Hound, the Forest Archer, the
    /// Root Shaman, the Royal Spearman and the Hollow Warden.
    ///
    /// Same rig, same palette rules and same honesty as the Tomb's set — this is not final art. The
    /// three humanoids go through the existing body, because the brief asks the forest's dead to
    /// still read as the same fallen kingdom: the archer and the spearman were royal soldiers, and
    /// they wear what is left of it under the moss. What separates them is silhouette — a hood and
    /// a bow, a robe and a staff, a spear held long — exactly as the Visual Bible requires.
    ///
    /// The hound is the one thing that could not reuse the humanoid rig, so it has its own: four
    /// legs, a low back line, and a head that leads rather than sits on top. It is drawn here
    /// rather than bent out of the soldier build, because a quadruped pretending to be a man on all
    /// fours reads as a bug.
    /// </summary>
    public static partial class PlaceholderEnemyArt
    {
        public const string HoundFolder = "Assets/Game/Art/Characters/Enemies/CorruptedHound";
        public const string ArcherFolder = "Assets/Game/Art/Characters/Enemies/ForestArcher";
        public const string ShamanFolder = "Assets/Game/Art/Characters/Enemies/RootShaman";
        public const string SpearmanFolder = "Assets/Game/Art/Characters/Enemies/RoyalSpearman";
        public const string WardenFolder = "Assets/Game/Art/Characters/Bosses/HollowWarden";

        // ---------- builds ----------

        private static Build ForestBuildFor(Actor actor)
        {
            switch (actor)
            {
                case Actor.Archer:
                    // 0.8 × 1.7 units. Lighter than the Shield Guard at the same height: narrow
                    // shoulders, no plate, and a bow that widens the outline only while aiming.
                    return new Build
                    {
                        Width = 150, Height = 176, HipY = 84f, Torso = 47f, Neck = 14f,
                        Thigh = 27f, Shin = 27f, LegWidth = 8f,
                        Arm = 22f, Forearm = 20f, ArmWidth = 6f,
                        TorsoWidth = 24f, HeadRx = 12f, HeadRy = 14f,
                        SwordLength = 54f, SwordWidth = 4f,
                        Armor = HuntersBlue, Cloth = HuntersBlue, Metal = Teal, Trim = FadedGold,
                        Skin = Flesh, Rot = PoisonGreen,
                        Weapon = WeaponShape.Bow, Head = HeadGear.Hood,
                        Growth = true, GrowthColor = MossGreen
                    };

                case Actor.Shaman:
                    // Robed: the outline widens towards the floor where every soldier's narrows.
                    // Almost no armour, because the counter to it is reaching it.
                    return new Build
                    {
                        Width = 156, Height = 178, HipY = 80f, Torso = 50f, Neck = 12f,
                        Thigh = 25f, Shin = 25f, LegWidth = 8f,
                        Arm = 21f, Forearm = 20f, ArmWidth = 6f,
                        TorsoWidth = 26f, HeadRx = 12f, HeadRy = 13f,
                        SwordLength = 74f, SwordWidth = 5f,
                        Armor = MossGreen, Cloth = MossGreen, Metal = WetBrown, Trim = PoisonGreen,
                        Skin = BoneWhite, Rot = PoisonGreen,
                        Weapon = WeaponShape.Staff, Head = HeadGear.Mask,
                        RobeFlare = 28f, Growth = true, GrowthColor = PoisonGreen
                    };

                case Actor.Spearman:
                    // Still a soldier: plate, a helm, and the discipline of the Tomb's guard. The
                    // spear is what makes it read at a distance, so it is drawn long.
                    return new Build
                    {
                        Width = 214, Height = 178, HipY = 84f, Torso = 48f, Neck = 13f,
                        Thigh = 27f, Shin = 27f, LegWidth = 10f,
                        Arm = 22f, Forearm = 20f, ArmWidth = 8f,
                        TorsoWidth = 31f, HeadRx = 13f, HeadRy = 14f,
                        SwordLength = 96f, SwordWidth = 4.5f,
                        Armor = Teal, Cloth = Navy, Metal = DarkSteel, Trim = FadedGold,
                        Skin = Flesh, Rot = MossGreen,
                        Weapon = WeaponShape.Spear, Head = HeadGear.Helm,
                        Growth = true, GrowthColor = MossGreen
                    };

                case Actor.WardenPhaseOne:
                    // 1.1 × 2.2 units. Deliberately narrower and taller-legged than the Guardian's
                    // 1.4 × 2.6: this one is a hunter, and it should look like it moves.
                    return new Build
                    {
                        Width = 268, Height = 260, HipY = 112f, Torso = 64f, Neck = 17f,
                        Thigh = 42f, Shin = 42f, LegWidth = 14f,
                        Arm = 32f, Forearm = 30f, ArmWidth = 11f,
                        TorsoWidth = 42f, HeadRx = 16f, HeadRy = 18f,
                        SwordLength = 122f, SwordWidth = 8f,
                        Armor = MossGreen, Cloth = WetBrown, Metal = BarkDark, Trim = FadedGold,
                        Skin = BarkDark, Rot = PoisonGreen,
                        Weapon = WeaponShape.Glaive, Head = HeadGear.Antlers,
                        Cape = true, Growth = true, GrowthColor = PoisonGreen
                    };

                case Actor.WardenPhaseTwo:
                    // Phase two: the growth wins. The green turns violet, the cloth darkens, and
                    // the runes light — the same body, visibly further gone.
                    var two = ForestBuildFor(Actor.WardenPhaseOne);
                    two.GrowthColor = CorruptViolet;
                    two.Rot = CorruptViolet;
                    two.Cloth = Charcoal;
                    two.Trim = CorruptViolet;
                    two.Runes = true;
                    return two;

                default:
                    // The hound never goes through the humanoid draw; this only sizes its canvas.
                    // 1.5 × 0.9 units, and wide, because the leap frames put it fully extended.
                    return new Build
                    {
                        Width = 200, Height = 120, HipY = 46f,
                        Armor = BarkDark, Cloth = CorruptViolet, Metal = WetBrown,
                        Trim = BoneWhite, Skin = BarkDark, Rot = CorruptViolet
                    };
            }
        }

        // ---------- parts the Tomb never needed ----------

        /// <summary>
        /// Everything held in the front hand that is not a sword. All four are drawn from the hand
        /// outward like the blade is, so they follow the arm instead of floating beside it, and
        /// none of them has anything to do with reach — the hitbox comes from the attack data.
        /// </summary>
        private static void ForestWeapon(Canvas canvas, Build build, Vector2 hand, float angle)
        {
            var direction = Canvas.Rotate(new Vector2(0f, -1f), -angle);
            var tip = hand + direction * build.SwordLength;

            switch (build.Weapon)
            {
                case WeaponShape.Bow:
                    // A limb and a string: the belly of the bow bulges away from the hand, and the
                    // string cuts straight across it, so a drawn bow reads even in silhouette.
                    var back = hand - direction * build.SwordLength * 0.5f;
                    var front = hand + direction * build.SwordLength * 0.5f;
                    var belly = Canvas.Rotate(new Vector2(build.SwordLength * 0.22f, 0f), -angle);
                    canvas.Thick(back, back + belly * 0.7f, build.SwordWidth,
                        build.SwordWidth * 0.8f, build.Metal);
                    canvas.Thick(back + belly * 0.7f, front + belly * 0.7f, build.SwordWidth * 0.9f,
                        build.SwordWidth * 0.9f, build.Metal);
                    canvas.Thick(front + belly * 0.7f, front, build.SwordWidth * 0.8f,
                        build.SwordWidth, build.Metal);
                    canvas.Thick(back, front, 1.4f, 1.4f, build.Trim);
                    break;

                case WeaponShape.Staff:
                    // A shaft with a knot of root at the head, lit from inside. The glow is where
                    // the player's eye should go during the long cast.
                    canvas.Thick(hand - direction * build.SwordLength * 0.25f, tip,
                        build.SwordWidth, build.SwordWidth * 0.7f, build.Metal);
                    canvas.Ellipse(tip, build.SwordWidth * 2.2f, build.SwordWidth * 2.6f,
                        build.Armor);
                    canvas.Ellipse(tip, build.SwordWidth * 1.2f, build.SwordWidth * 1.4f,
                        build.Trim);
                    break;

                case WeaponShape.Spear:
                    // Held well back along the shaft, so most of its length is in front and the
                    // thrust has somewhere to come from.
                    var butt = hand - direction * build.SwordLength * 0.35f;
                    canvas.Thick(butt, tip, build.SwordWidth, build.SwordWidth * 0.8f, WetBrown);
                    var headBase = hand + direction * build.SwordLength * 0.82f;
                    var across = Canvas.Rotate(new Vector2(build.SwordWidth * 1.8f, 0f), -angle);
                    canvas.Triangle(headBase - across, headBase + across, tip, build.Metal);
                    break;

                case WeaponShape.Maul:
                    // The Bellringer's hammer: a long haft and a heavy round bronze head across
                    // its end, the weight all at the tip like the bell's own clapper.
                    var neck = hand + direction * build.SwordLength * 0.84f;
                    canvas.Thick(hand - direction * build.SwordLength * 0.2f, neck,
                        build.SwordWidth * 0.7f, build.SwordWidth * 0.6f, build.Metal);
                    var span = Canvas.Rotate(new Vector2(build.SwordWidth * 2.6f, 0f), -angle);
                    canvas.Thick(neck - span, neck + span, build.SwordWidth * 2.2f, build.SwordWidth * 2.2f, build.Trim);
                    canvas.Ellipse(neck - span, build.SwordWidth * 1.3f, build.SwordWidth * 1.3f, build.Trim);
                    canvas.Ellipse(neck + span, build.SwordWidth * 1.3f, build.SwordWidth * 1.3f, build.Trim);
                    break;

                default:
                    // The glaive: a long haft with a heavy curved blade on the end. The weight is
                    // all at the tip, which is what the long windups are meant to sell.
                    var haftEnd = hand + direction * build.SwordLength * 0.72f;
                    canvas.Thick(hand - direction * build.SwordLength * 0.3f, haftEnd,
                        build.SwordWidth * 0.9f, build.SwordWidth * 0.8f, build.Metal);
                    var side = Canvas.Rotate(new Vector2(build.SwordWidth * 3.2f, 0f), -angle);
                    canvas.Triangle(haftEnd - side * 0.3f, haftEnd + side, tip, build.Armor);
                    canvas.Triangle(haftEnd - side * 0.3f, tip,
                        tip - direction * build.SwordLength * 0.12f + side * 0.2f, build.Trim);
                    break;
            }
        }

        /// <summary>Heads that are not a closed royal helm.</summary>
        private static void ForestHead(Canvas canvas, Build build, Vector2 head, float tilt, float flare)
        {
            switch (build.Head)
            {
                case HeadGear.Hood:
                    // A hood with nothing readable inside it. The point of the archer is that it
                    // used to be a person and the forest has finished with that.
                    canvas.Ellipse(head, build.HeadRx, build.HeadRy, build.Skin);
                    canvas.Triangle(
                        head + Canvas.Rotate(new Vector2(-build.HeadRx * 1.3f, -build.HeadRy), tilt),
                        head + Canvas.Rotate(new Vector2(build.HeadRx * 0.9f, -build.HeadRy * 0.2f),
                            tilt),
                        head + Canvas.Rotate(new Vector2(-build.HeadRx * 0.2f, build.HeadRy * 1.5f),
                            tilt), build.Cloth);
                    Eye(canvas, head + Canvas.Rotate(new Vector2(build.HeadRx * 0.5f, -1f), tilt), 1.8f,
                        PaleGreenEye, flare);
                    break;

                case HeadGear.Mask:
                    // A pale bone mask, the one bright thing on the shaman. It is the aiming point
                    // the player learns to go for.
                    canvas.Ellipse(head, build.HeadRx * 1.05f, build.HeadRy * 1.1f, build.Skin);
                    canvas.Triangle(
                        head + Canvas.Rotate(new Vector2(-build.HeadRx * 0.7f, build.HeadRy), tilt),
                        head + Canvas.Rotate(new Vector2(build.HeadRx * 0.7f, build.HeadRy), tilt),
                        head + Canvas.Rotate(new Vector2(0f, -build.HeadRy * 1.3f), tilt),
                        build.Skin);
                    canvas.Ellipse(head + Canvas.Rotate(new Vector2(-3f, 2f), tilt), 2f, 2.6f,
                        Charcoal);
                    canvas.Ellipse(head + Canvas.Rotate(new Vector2(4f, 2f), tilt), 2f, 2.6f,
                        Charcoal);
                    // Something alive behind the bone.
                    canvas.Ellipse(head + Canvas.Rotate(new Vector2(-3f, 2f), tilt), 1f + flare, 1.2f + flare, PoisonEye);
                    canvas.Ellipse(head + Canvas.Rotate(new Vector2(4f, 2f), tilt), 1f + flare, 1.2f + flare, PoisonEye);
                    break;

                default:
                    // The Warden: a bare wooden skull with the forest growing out of it. The
                    // antlers are where its extra height comes from, above the capsule.
                    canvas.Ellipse(head, build.HeadRx, build.HeadRy, build.Skin);
                    canvas.Thick(head + Canvas.Rotate(new Vector2(build.HeadRx * 0.3f, 1f), tilt),
                        head + Canvas.Rotate(new Vector2(build.HeadRx * 1.0f, -1f), tilt),
                        build.HeadRy * 0.34f, build.HeadRy * 0.2f, Charcoal);
                    Eye(canvas, head + Canvas.Rotate(new Vector2(build.HeadRx * 0.75f, 0f), tilt), 1.8f,
                        PoisonEye, flare);
                    for (var i = -1; i <= 1; i += 2)
                    {
                        var root = head + Canvas.Rotate(
                            new Vector2(build.HeadRx * 0.5f * i, build.HeadRy * 0.7f), tilt);
                        var mid = root + Canvas.Rotate(
                            new Vector2(build.HeadRx * 0.9f * i, build.HeadRy * 1.1f), tilt);
                        var end = mid + Canvas.Rotate(
                            new Vector2(build.HeadRx * 0.5f * i, build.HeadRy * 0.9f), tilt);
                        canvas.Thick(root, mid, 4.5f, 3f, build.Metal);
                        canvas.Thick(mid, end, 3f, 1.6f, build.Metal);
                        canvas.Thick(mid, mid + Canvas.Rotate(
                                new Vector2(build.HeadRx * 0.8f * i, build.HeadRy * 0.2f), tilt),
                            2.6f, 1.4f, build.Metal);
                    }

                    break;
            }
        }

        /// <summary>
        /// Corruption growing through whatever the body is wearing. Drawn only over pixels that are
        /// already opaque, so it never leaks past the silhouette and never adds reach.
        /// </summary>
        private static void ForestGrowth(Canvas canvas, Build build, Vector2 hip, Vector2 shoulder,
            float lean)
        {
            for (var i = 0; i < 5; i++)
            {
                var along = (i + 0.5f) / 5f;
                var anchor = Vector2.Lerp(hip, shoulder, along);
                var side = (i % 2 == 0 ? 1f : -1f) * build.TorsoWidth * (0.2f + 0.16f * (i % 3));
                var a = anchor + Canvas.Rotate(new Vector2(side, 0f), lean);
                var b = anchor + Canvas.Rotate(new Vector2(side * 0.3f, build.Torso * 0.16f), lean);
                canvas.RectOver(Mathf.Min(a.x, b.x) - 1f, Mathf.Min(a.y, b.y),
                    Mathf.Max(a.x, b.x) + 1f, Mathf.Max(a.y, b.y), build.GrowthColor);
            }
        }

        // ---------- state tables ----------

        /// <summary>
        /// Frame budgets derived from each enemy's real attack timings, the same way the Tomb's
        /// were. The hound's leap is the longest clip it has because its windup is the longest
        /// thing it does, and that is the frame the player's dash is timed against.
        /// </summary>
        public static readonly Spec<EnemyAnimationState>[] HoundStates =
        {
            new Spec<EnemyAnimationState>(EnemyAnimationState.Idle, 6, 8f, true),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Patrol, 8, 14f, true),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Alert, 5, 14f, false),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Chase, 8, 18f, true),
            new Spec<EnemyAnimationState>(EnemyAnimationState.AttackLight, 16, 24f, false),
            new Spec<EnemyAnimationState>(EnemyAnimationState.AttackHeavy, 22, 24f, false),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Hurt, 5, 24f, false),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Stunned, 6, 12f, true),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Death, 10, 12f, false)
        };

        public static readonly Spec<EnemyAnimationState>[] ArcherStates =
        {
            new Spec<EnemyAnimationState>(EnemyAnimationState.Idle, 6, 8f, true),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Patrol, 8, 12f, true),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Alert, 5, 12f, false),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Chase, 8, 14f, true),
            // The draw is long and the release is short, which is the whole tell.
            new Spec<EnemyAnimationState>(EnemyAnimationState.AttackLight, 22, 24f, false),
            new Spec<EnemyAnimationState>(EnemyAnimationState.ShieldBash, 14, 24f, false),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Hurt, 5, 24f, false),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Stunned, 6, 12f, true),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Death, 10, 12f, false)
        };

        public static readonly Spec<EnemyAnimationState>[] ShamanStates =
        {
            new Spec<EnemyAnimationState>(EnemyAnimationState.Idle, 6, 8f, true),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Patrol, 8, 10f, true),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Alert, 5, 12f, false),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Chase, 8, 12f, true),
            new Spec<EnemyAnimationState>(EnemyAnimationState.AttackLight, 24, 24f, false),
            new Spec<EnemyAnimationState>(EnemyAnimationState.AttackHeavy, 28, 24f, false),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Hurt, 5, 24f, false),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Stunned, 6, 12f, true),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Death, 11, 12f, false)
        };

        public static readonly Spec<EnemyAnimationState>[] SpearmanStates =
        {
            new Spec<EnemyAnimationState>(EnemyAnimationState.Idle, 6, 8f, true),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Patrol, 8, 12f, true),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Alert, 5, 12f, false),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Chase, 8, 12f, true),
            new Spec<EnemyAnimationState>(EnemyAnimationState.AttackLight, 20, 24f, false),
            new Spec<EnemyAnimationState>(EnemyAnimationState.AttackHeavy, 24, 24f, false),
            new Spec<EnemyAnimationState>(EnemyAnimationState.ShieldBash, 22, 24f, false),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Hurt, 5, 24f, false),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Stunned, 6, 12f, true),
            new Spec<EnemyAnimationState>(EnemyAnimationState.Death, 12, 12f, false)
        };

        public static readonly Spec<BossAnimationState>[] WardenStates =
        {
            new Spec<BossAnimationState>(BossAnimationState.Dormant, 4, 8f, true),
            new Spec<BossAnimationState>(BossAnimationState.Intro, 18, 12f, false),
            new Spec<BossAnimationState>(BossAnimationState.IdleP1, 6, 8f, true),
            new Spec<BossAnimationState>(BossAnimationState.WalkP1, 8, 14f, true),
            new Spec<BossAnimationState>(BossAnimationState.SlashP1, 18, 24f, false),
            new Spec<BossAnimationState>(BossAnimationState.Sweep, 22, 24f, false),
            new Spec<BossAnimationState>(BossAnimationState.ChargeStart, 16, 24f, false),
            new Spec<BossAnimationState>(BossAnimationState.Charge, 6, 24f, true),
            new Spec<BossAnimationState>(BossAnimationState.ChargeRecover, 12, 24f, false),
            new Spec<BossAnimationState>(BossAnimationState.RootShot, 20, 24f, false),
            new Spec<BossAnimationState>(BossAnimationState.Stunned, 8, 12f, true),
            new Spec<BossAnimationState>(BossAnimationState.Recoil, 6, 24f, false),
            new Spec<BossAnimationState>(BossAnimationState.PhaseTransition, 26, 12f, false),
            new Spec<BossAnimationState>(BossAnimationState.IdleP2, 6, 8f, true),
            new Spec<BossAnimationState>(BossAnimationState.MoveP2, 8, 14f, true),
            new Spec<BossAnimationState>(BossAnimationState.SlashP2, 16, 24f, false),
            new Spec<BossAnimationState>(BossAnimationState.ComboP2, 18, 24f, false),
            new Spec<BossAnimationState>(BossAnimationState.Combo1, 10, 24f, false),
            new Spec<BossAnimationState>(BossAnimationState.Combo2, 10, 24f, false),
            new Spec<BossAnimationState>(BossAnimationState.Combo3, 14, 24f, false),
            new Spec<BossAnimationState>(BossAnimationState.RootSlam, 24, 24f, false),
            new Spec<BossAnimationState>(BossAnimationState.PoisonFieldCast, 22, 24f, false),
            new Spec<BossAnimationState>(BossAnimationState.JumpStart, 14, 24f, false),
            new Spec<BossAnimationState>(BossAnimationState.Jump, 6, 24f, true),
            new Spec<BossAnimationState>(BossAnimationState.Slam, 4, 24f, false),
            new Spec<BossAnimationState>(BossAnimationState.SlamRecover, 14, 24f, false),
            new Spec<BossAnimationState>(BossAnimationState.Death, 26, 12f, false)
        };

        // ---------- generation ----------

        public static Dictionary<EnemyAnimationState, Sprite[]> GenerateHound() =>
            Generate(HoundFolder, "enemy_hound", HoundStates,
                (state, t) => RenderHound(state, t));

        public static Dictionary<EnemyAnimationState, Sprite[]> GenerateArcher() =>
            Generate(ArcherFolder, "enemy_archer", ArcherStates,
                (state, t) => Render(Actor.Archer,
                    Telegraphed(Actor.Archer, state, t, ForestPose(Actor.Archer, state, t))));

        public static Dictionary<EnemyAnimationState, Sprite[]> GenerateShaman() =>
            Generate(ShamanFolder, "enemy_shaman", ShamanStates,
                (state, t) => Render(Actor.Shaman,
                    Telegraphed(Actor.Shaman, state, t, ForestPose(Actor.Shaman, state, t))));

        public static Dictionary<EnemyAnimationState, Sprite[]> GenerateSpearman() =>
            Generate(SpearmanFolder, "enemy_spearman", SpearmanStates,
                (state, t) => Render(Actor.Spearman,
                    Telegraphed(Actor.Spearman, state, t, ForestPose(Actor.Spearman, state, t))));

        public static Dictionary<BossAnimationState, Sprite[]> GenerateWarden() =>
            Generate(WardenFolder, "boss_warden", WardenStates,
                (state, t) => Render(ActorForWardenState(state), WardenPose(state, t)));

        /// <summary>
        /// Which build a Warden state is drawn with. Everything from the transition onward is the
        /// corrupted one, so the phase change is visible in a single frame.
        /// </summary>
        private static Actor ActorForWardenState(BossAnimationState state)
        {
            switch (state)
            {
                case BossAnimationState.IdleP2:
                case BossAnimationState.MoveP2:
                case BossAnimationState.SlashP2:
                case BossAnimationState.ComboP2:
                case BossAnimationState.RootSlam:
                case BossAnimationState.PoisonFieldCast:
                case BossAnimationState.JumpStart:
                case BossAnimationState.Jump:
                case BossAnimationState.Slam:
                case BossAnimationState.SlamRecover:
                case BossAnimationState.Death:
                    return Actor.WardenPhaseTwo;
                default:
                    return Actor.WardenPhaseOne;
            }
        }

        // ---------- humanoid poses ----------

        /// <summary>
        /// The forest humanoids' poses. Locomotion and reactions are the shared soldier's, because
        /// they are soldiers; only the states where the weapon changes what the body does are
        /// written out here — the archer's draw, the shaman's two casts, the spearman's reach.
        /// </summary>
        private static Pose ForestPose(Actor actor, EnemyAnimationState state, float t)
        {
            switch (state)
            {
                case EnemyAnimationState.AttackLight when actor == Actor.Archer:
                    return ArcherDraw(t);
                case EnemyAnimationState.AttackLight when actor == Actor.Shaman:
                    return ShamanCast(t, isZone: false);
                case EnemyAnimationState.AttackHeavy when actor == Actor.Shaman:
                    return ShamanCast(t, isZone: true);
                case EnemyAnimationState.AttackLight when actor == Actor.Spearman:
                    return SpearThrust(t);
                case EnemyAnimationState.ShieldBash when actor == Actor.Spearman:
                    return SpearThrust(t, advancing: true);
                case EnemyAnimationState.ShieldBash when actor == Actor.Archer:
                    return ArcherKick(t);
                default:
                    var pose = EnemyPose(actor, state, t);
                    if (actor == Actor.Shaman && state == EnemyAnimationState.Idle)
                    {
                        // The staff head is held out and low, and it never stops glowing: the
                        // shaman should be identifiable across a clearing.
                        pose.SwordAngle = 78f;
                        pose.Glow = 0.5f + 0.25f * Wave(t);
                    }

                    return pose;
            }
        }

        /// <summary>
        /// The draw: a long, still pull with the bow arm locked forward, then a snap. Nearly the
        /// whole clip is the pull, because nearly the whole attack is the windup — the player has
        /// well over half a second to get behind something, and has to be able to see that.
        /// </summary>
        private static Pose ArcherDraw(float t)
        {
            var pose = Pose.Stand;
            var pull = Ease(Mathf.Clamp01(t / 0.72f));
            var release = Ease(Mathf.Clamp01((t - 0.74f) / 0.26f));
            pose.Lean = Mathf.Lerp(2f, -4f, pull);
            pose.FrontArm = -78f;
            pose.FrontForearm = Mathf.Lerp(8f, 0f, pull);
            pose.BackArm = Mathf.Lerp(-40f, -96f, pull) + release * 46f;
            pose.BackForearm = Mathf.Lerp(30f, 74f, pull) - release * 60f;
            pose.SwordAngle = -78f;
            pose.HeadTilt = Mathf.Lerp(0f, -6f, pull);
            pose.FrontThigh = 12f;
            pose.BackThigh = -14f;
            pose.HipX = Mathf.Lerp(0f, -2.5f, pull) + release * 3f;
            return pose;
        }

        /// <summary>A shove to buy back the distance the bow needs.</summary>
        private static Pose ArcherKick(float t)
        {
            var pose = Pose.Stand;
            var commit = Ease(Mathf.Clamp01((t - 0.28f) / 0.34f));
            pose.Lean = Mathf.Lerp(-8f, 12f, commit);
            pose.FrontThigh = Mathf.Lerp(-18f, 62f, commit);
            pose.FrontShin = Mathf.Lerp(-30f, -4f, commit);
            pose.BackThigh = Mathf.Lerp(8f, -18f, commit);
            pose.FrontArm = Mathf.Lerp(-40f, -20f, commit);
            pose.BackArm = Mathf.Lerp(-20f, -52f, commit);
            pose.SwordAngle = -50f;
            pose.HipX = Mathf.Lerp(-2f, 4f, commit);
            return pose;
        }

        /// <summary>
        /// Both casts: the staff comes up, the head brightens, and the release is a push. The
        /// ground version reaches further over and holds longer, so the two are not the same
        /// animation with a different projectile on the end.
        /// </summary>
        private static Pose ShamanCast(float t, bool isZone)
        {
            var pose = Pose.Stand;
            var gather = Ease(Mathf.Clamp01(t / (isZone ? 0.78f : 0.7f)));
            var push = Ease(Mathf.Clamp01((t - (isZone ? 0.8f : 0.72f)) / 0.2f));
            pose.Lean = Mathf.Lerp(-6f, isZone ? 16f : 8f, push);
            pose.FrontArm = Mathf.Lerp(10f, isZone ? -34f : -62f, gather) + push * 24f;
            pose.FrontForearm = Mathf.Lerp(24f, isZone ? 52f : 14f, gather);
            pose.BackArm = Mathf.Lerp(-8f, -34f, gather);
            pose.SwordAngle = Mathf.Lerp(72f, isZone ? 128f : 4f, gather);
            pose.HeadTilt = Mathf.Lerp(0f, isZone ? 12f : -6f, gather);
            pose.FrontThigh = Mathf.Lerp(4f, 14f, push);
            pose.BackThigh = Mathf.Lerp(-6f, -18f, push);
            // The glow peaks at the end of the gather, which is the moment to interrupt it.
            pose.Glow = Mathf.Clamp01(gather * 1.2f) * (1f - push * 0.6f);
            return pose;
        }

        /// <summary>
        /// The thrust: the spear goes out along its own line rather than arcing. The advancing
        /// version leans hard into it, because the body follows the point.
        /// </summary>
        private static Pose SpearThrust(float t, bool advancing = false)
        {
            var pose = Pose.Stand;
            var commit = Ease(Mathf.Clamp01((t - 0.42f) / 0.26f));
            pose.Lean = Mathf.Lerp(-10f, advancing ? 22f : 14f, commit);
            pose.HipX = Mathf.Lerp(-4f, advancing ? 9f : 6f, commit);
            pose.FrontArm = Mathf.Lerp(-14f, -86f, commit);
            pose.FrontForearm = Mathf.Lerp(46f, -2f, commit);
            pose.BackArm = Mathf.Lerp(-26f, -70f, commit);
            pose.BackForearm = Mathf.Lerp(40f, 6f, commit);
            pose.SwordAngle = Mathf.Lerp(-40f, -90f, commit);
            pose.FrontThigh = Mathf.Lerp(-14f, advancing ? 34f : 26f, commit);
            pose.FrontShin = Mathf.Lerp(-8f, -26f, commit);
            pose.BackThigh = Mathf.Lerp(14f, -30f, commit);
            pose.HeadTilt = Mathf.Lerp(-4f, 6f, commit);
            return pose;
        }

        // ---------- the Warden's poses ----------

        private static Pose WardenPose(BossAnimationState state, float t)
        {
            var pose = Pose.Stand;
            switch (state)
            {
                case BossAnimationState.Dormant:
                    // Kneeling in the roots, head down. It is scenery until it is not.
                    pose.Lean = 26f;
                    pose.HipY = -30f + Wave(t) * 0.6f;
                    pose.FrontThigh = 68f;
                    pose.FrontShin = -74f;
                    pose.BackThigh = -58f;
                    pose.BackShin = -72f;
                    pose.HeadTilt = 30f;
                    pose.FrontArm = 46f;
                    pose.SwordAngle = 96f;
                    pose.Glow = 0f;
                    break;

                case BossAnimationState.Intro:
                    // Standing up out of the roots. The glaive comes off the ground last.
                    var rise = Ease(t);
                    pose.Lean = Mathf.Lerp(26f, 4f, rise);
                    pose.HipY = Mathf.Lerp(-30f, 0f, rise);
                    pose.FrontThigh = Mathf.Lerp(68f, 6f, rise);
                    pose.FrontShin = Mathf.Lerp(-74f, -6f, rise);
                    pose.BackThigh = Mathf.Lerp(-58f, -6f, rise);
                    pose.BackShin = Mathf.Lerp(-72f, -8f, rise);
                    pose.HeadTilt = Mathf.Lerp(30f, -10f, rise);
                    pose.FrontArm = Mathf.Lerp(46f, -8f, rise);
                    pose.SwordAngle = Mathf.Lerp(96f, 26f, Ease(Mathf.Clamp01((t - 0.5f) / 0.5f)));
                    pose.Glow = rise;
                    break;

                case BossAnimationState.IdleP1:
                    pose.Lean = 3f;
                    pose.HipY = Wave(t) * 1.1f;
                    pose.FrontArm = 6f;
                    pose.SwordAngle = 28f;
                    pose.HeadTilt = Wave(t * 0.5f) * 2f;
                    break;

                case BossAnimationState.IdleP2:
                    // Lower, wider, leaning in. Phase two never stops threatening.
                    pose.Lean = 10f;
                    pose.HipY = -3f + Wave(t) * 1.4f;
                    pose.FrontArm = -6f;
                    pose.FrontThigh = 12f;
                    pose.BackThigh = -14f;
                    pose.SwordAngle = 12f;
                    pose.HeadTilt = -6f;
                    pose.Glow = 0.6f + 0.2f * Wave(t);
                    break;

                case BossAnimationState.WalkP1:
                case BossAnimationState.MoveP2:
                    var fast = state == BossAnimationState.MoveP2;
                    pose.Lean = fast ? 12f : 6f;
                    pose.HipY = Mathf.Abs(Wave(t)) * 3f;
                    pose.FrontThigh = Wave(t) * (fast ? 30f : 24f);
                    pose.BackThigh = -Wave(t) * (fast ? 30f : 24f);
                    pose.FrontShin = -Mathf.Max(0f, -Wave(t)) * 30f - 6f;
                    pose.BackShin = -Mathf.Max(0f, Wave(t)) * 30f - 6f;
                    pose.FrontArm = -4f + Wave(t) * 8f;
                    pose.BackArm = -Wave(t) * 18f;
                    pose.SwordAngle = fast ? 14f : 30f;
                    pose.CapeSway = Wave(t) * 14f;
                    pose.Glow = fast ? 0.6f : 0f;
                    break;

                case BossAnimationState.SlashP1:
                    return WardenSwing(t, 1f);

                case BossAnimationState.SlashP2:
                    return WardenSwing(t, 1.15f, glow: 0.7f);

                case BossAnimationState.Sweep:
                    // Low and wide. The glaive drops below the waist and comes across the floor,
                    // which is what says "this one goes under a block and not over a jump".
                    var sweep = Ease(Mathf.Clamp01((t - 0.36f) / 0.3f));
                    pose.Lean = Mathf.Lerp(-16f, 24f, sweep);
                    pose.HipY = Mathf.Lerp(0f, -10f, sweep);
                    pose.FrontArm = Mathf.Lerp(-52f, 74f, sweep);
                    pose.FrontForearm = Mathf.Lerp(20f, -14f, sweep);
                    pose.BackArm = Mathf.Lerp(-30f, 34f, sweep);
                    pose.SwordAngle = Mathf.Lerp(-56f, 118f, sweep);
                    pose.FrontThigh = Mathf.Lerp(-16f, 30f, sweep);
                    pose.BackThigh = Mathf.Lerp(18f, -34f, sweep);
                    pose.FrontShin = -22f;
                    pose.HeadTilt = Mathf.Lerp(-8f, 14f, sweep);
                    pose.CapeSway = Mathf.Lerp(20f, -28f, sweep);
                    break;

                case BossAnimationState.ChargeStart:
                    // The thrust's windup: the glaive is pulled all the way back and levelled.
                    pose.Lean = Mathf.Lerp(0f, -22f, Ease(t));
                    pose.FrontArm = Mathf.Lerp(0f, -58f, Ease(t));
                    pose.FrontForearm = Mathf.Lerp(22f, 60f, Ease(t));
                    pose.BackArm = Mathf.Lerp(-10f, -74f, Ease(t));
                    pose.SwordAngle = Mathf.Lerp(26f, -84f, Ease(t));
                    pose.FrontThigh = Mathf.Lerp(4f, -20f, Ease(t));
                    pose.BackThigh = Mathf.Lerp(-6f, 22f, Ease(t));
                    pose.HeadTilt = -10f;
                    pose.Glow = Ease(t) * 0.5f;
                    break;

                case BossAnimationState.Charge:
                    pose.Lean = 26f;
                    pose.HipX = 6f;
                    pose.FrontArm = -88f;
                    pose.FrontForearm = -4f;
                    pose.BackArm = -76f;
                    pose.SwordAngle = -92f;
                    pose.FrontThigh = Wave(t) * 34f + 14f;
                    pose.BackThigh = -Wave(t) * 34f - 10f;
                    pose.CapeSway = -30f;
                    break;

                case BossAnimationState.ChargeRecover:
                    pose.Lean = Mathf.Lerp(26f, 4f, Ease(t));
                    pose.HipX = Mathf.Lerp(6f, 0f, Ease(t));
                    pose.FrontArm = Mathf.Lerp(-88f, 4f, Ease(t));
                    pose.SwordAngle = Mathf.Lerp(-92f, 26f, Ease(t));
                    pose.FrontThigh = Mathf.Lerp(26f, 4f, Ease(t));
                    pose.BackThigh = Mathf.Lerp(-24f, -6f, Ease(t));
                    break;

                case BossAnimationState.RootShot:
                    // The off hand does the work: it comes up, the palm lights, and the shot
                    // leaves it. The glaive stays down, so this never reads as a melee windup.
                    var gather = Ease(Mathf.Clamp01(t / 0.74f));
                    var throwOut = Ease(Mathf.Clamp01((t - 0.76f) / 0.24f));
                    pose.Lean = Mathf.Lerp(-6f, 10f, throwOut);
                    pose.BackArm = Mathf.Lerp(-16f, -102f, gather) + throwOut * 30f;
                    pose.BackForearm = Mathf.Lerp(20f, 4f, gather);
                    pose.FrontArm = 22f;
                    pose.SwordAngle = 62f;
                    pose.HeadTilt = Mathf.Lerp(0f, -8f, gather);
                    pose.Glow = gather * (1f - throwOut * 0.7f);
                    break;

                case BossAnimationState.RootSlam:
                    // Both hands up, then the haft is driven into the ground. The gather is long
                    // and lit, which is the window to leave the lane in front of it.
                    var lift = Ease(Mathf.Clamp01(t / 0.62f));
                    var drive = Ease(Mathf.Clamp01((t - 0.64f) / 0.22f));
                    pose.Lean = Mathf.Lerp(-18f, 26f, drive);
                    pose.HipY = Mathf.Lerp(4f, -12f, drive);
                    pose.FrontArm = Mathf.Lerp(-40f, -96f, lift) + drive * 120f;
                    pose.BackArm = Mathf.Lerp(-30f, -88f, lift) + drive * 110f;
                    pose.FrontForearm = Mathf.Lerp(30f, -6f, lift);
                    pose.SwordAngle = Mathf.Lerp(-30f, -96f, lift) + drive * 190f;
                    pose.FrontThigh = Mathf.Lerp(-10f, 30f, drive);
                    pose.BackThigh = Mathf.Lerp(10f, -30f, drive);
                    pose.HeadTilt = Mathf.Lerp(-14f, 16f, drive);
                    pose.Glow = lift * (1f - drive * 0.5f);
                    break;

                case BossAnimationState.PoisonFieldCast:
                    // An open hand held out over where the pool will be, for most of a second.
                    var open = Ease(Mathf.Clamp01(t / 0.8f));
                    pose.Lean = Mathf.Lerp(-4f, 14f, open);
                    pose.BackArm = Mathf.Lerp(-10f, 54f, open);
                    pose.BackForearm = Mathf.Lerp(20f, 48f, open);
                    pose.FrontArm = 18f;
                    pose.SwordAngle = 70f;
                    pose.HeadTilt = Mathf.Lerp(2f, 20f, open);
                    pose.Glow = open;
                    break;

                case BossAnimationState.Combo1:
                    return WardenSwing(t, 0.85f);
                case BossAnimationState.Combo2:
                    return WardenSwing(t, 0.9f, mirrored: true);
                case BossAnimationState.Combo3:
                case BossAnimationState.ComboP2:
                    return WardenSwing(t, 1.3f, glow: 0.7f);

                case BossAnimationState.JumpStart:
                    var crouch = Ease(t);
                    pose.Lean = Mathf.Lerp(0f, -14f, crouch);
                    pose.HipY = Mathf.Lerp(0f, -22f, crouch);
                    pose.FrontThigh = Mathf.Lerp(4f, -42f, crouch);
                    pose.FrontShin = Mathf.Lerp(-5f, -52f, crouch);
                    pose.BackThigh = Mathf.Lerp(-5f, -38f, crouch);
                    pose.BackShin = Mathf.Lerp(-6f, -50f, crouch);
                    pose.FrontArm = Mathf.Lerp(0f, -70f, crouch);
                    pose.SwordAngle = Mathf.Lerp(26f, -66f, crouch);
                    pose.Glow = crouch;
                    break;

                case BossAnimationState.Jump:
                    pose.Lean = -6f;
                    pose.HipY = 6f;
                    pose.FrontThigh = -34f;
                    pose.FrontShin = -40f;
                    pose.BackThigh = -20f;
                    pose.FrontArm = -104f;
                    pose.SwordAngle = -104f;
                    pose.CapeSway = 34f;
                    pose.Glow = 1f;
                    break;

                case BossAnimationState.Slam:
                    pose.Lean = 28f;
                    pose.HipY = -14f;
                    pose.FrontThigh = 38f;
                    pose.FrontShin = -30f;
                    pose.BackThigh = -36f;
                    pose.FrontArm = 86f;
                    pose.SwordAngle = 128f;
                    pose.Squash = 0.94f;
                    pose.Glow = 1f;
                    break;

                case BossAnimationState.SlamRecover:
                    pose.Lean = Mathf.Lerp(28f, 10f, Ease(t));
                    pose.HipY = Mathf.Lerp(-14f, -3f, Ease(t));
                    pose.FrontArm = Mathf.Lerp(86f, -6f, Ease(t));
                    pose.SwordAngle = Mathf.Lerp(128f, 12f, Ease(t));
                    pose.FrontThigh = Mathf.Lerp(38f, 12f, Ease(t));
                    pose.BackThigh = Mathf.Lerp(-36f, -14f, Ease(t));
                    pose.Glow = 1f - Ease(t) * 0.4f;
                    break;

                case BossAnimationState.Stunned:
                    pose.Lean = 20f + Wave(t) * 5f;
                    pose.HipY = -7f;
                    pose.HeadTilt = 26f + Wave(t * 0.5f) * 5f;
                    pose.FrontArm = 40f;
                    pose.BackArm = 26f;
                    pose.SwordAngle = 104f;
                    pose.FrontThigh = 16f;
                    pose.BackThigh = -20f;
                    break;

                case BossAnimationState.Recoil:
                    pose.Lean = Mathf.Lerp(-24f, -4f, Ease(t));
                    pose.FrontArm = Mathf.Lerp(-64f, 0f, Ease(t));
                    pose.SwordAngle = Mathf.Lerp(-70f, 24f, Ease(t));
                    pose.HipX = Mathf.Lerp(-6f, 0f, Ease(t));
                    pose.HeadTilt = Mathf.Lerp(-16f, 0f, Ease(t));
                    break;

                case BossAnimationState.PhaseTransition:
                    // It is torn open and the forest fills the gap. Arms wide, head back, and the
                    // glow climbs the whole way: the one moment the fight stops for.
                    var open2 = Ease(Mathf.Clamp01(t / 0.55f));
                    var settle = Ease(Mathf.Clamp01((t - 0.6f) / 0.4f));
                    pose.Lean = Mathf.Lerp(-30f, 10f, settle);
                    pose.HipY = Mathf.Lerp(-16f, -3f, settle);
                    pose.FrontArm = Mathf.Lerp(0f, -118f, open2) + settle * 112f;
                    pose.BackArm = Mathf.Lerp(0f, -110f, open2) + settle * 104f;
                    pose.HeadTilt = Mathf.Lerp(0f, -38f, open2) + settle * 32f;
                    pose.SwordAngle = Mathf.Lerp(26f, -96f, open2) + settle * 108f;
                    pose.CapeSway = Mathf.Lerp(0f, 40f, open2);
                    pose.Glow = Mathf.Max(open2, settle);
                    break;

                default:
                    // Death: it comes apart where the roots went in, and folds rather than falls.
                    var fall = Ease(t);
                    pose.Lean = Mathf.Lerp(10f, 76f, fall);
                    pose.HipY = Mathf.Lerp(-3f, -44f, fall);
                    pose.FrontThigh = Mathf.Lerp(12f, 70f, fall);
                    pose.FrontShin = Mathf.Lerp(-5f, -78f, fall);
                    pose.BackThigh = Mathf.Lerp(-14f, -62f, fall);
                    pose.BackShin = Mathf.Lerp(-6f, -74f, fall);
                    pose.FrontArm = Mathf.Lerp(-6f, 62f, fall);
                    pose.BackArm = Mathf.Lerp(-8f, 48f, fall);
                    pose.HeadTilt = Mathf.Lerp(-6f, 44f, fall);
                    pose.SwordAngle = Mathf.Lerp(12f, 128f, Ease(Mathf.Clamp01(t / 0.4f)));
                    pose.Glow = Mathf.Max(0f, 1f - fall * 1.6f);
                    pose.Alpha = Mathf.Lerp(1f, 0.45f, Mathf.Clamp01((t - 0.7f) / 0.3f));
                    break;
            }

            return pose;
        }

        /// <summary>
        /// A glaive swing. Heavier and slower to commit than a sword's, and it travels further,
        /// because the weight is at the end of a long haft.
        /// </summary>
        private static Pose WardenSwing(float t, float power, bool mirrored = false,
            float glow = 0f)
        {
            var pose = Pose.Stand;
            var commit = Ease(Mathf.Clamp01((t - 0.32f) / 0.36f));
            var arc = mirrored ? -1f : 1f;
            pose.Lean = Mathf.Lerp(-14f * power, 20f * power, commit);
            pose.HipX = Mathf.Lerp(-4f, 6f, commit);
            pose.FrontArm = Mathf.Lerp(-74f * arc, 68f * arc, commit);
            pose.FrontForearm = Mathf.Lerp(26f, -10f, commit);
            pose.BackArm = Mathf.Lerp(-44f, 26f, commit);
            pose.BackForearm = Mathf.Lerp(24f, -4f, commit);
            pose.SwordAngle = Mathf.Lerp(-72f * arc, 104f * arc, commit);
            pose.FrontThigh = Mathf.Lerp(-12f, 26f, commit);
            pose.FrontShin = Mathf.Lerp(-10f, -26f, commit);
            pose.BackThigh = Mathf.Lerp(14f, -28f, commit);
            pose.BackShin = Mathf.Lerp(-14f, -22f, commit);
            pose.HeadTilt = Mathf.Lerp(-6f, 10f, commit);
            pose.CapeSway = Mathf.Lerp(18f, -26f, commit);
            pose.Glow = glow;
            return pose;
        }
    }
}
