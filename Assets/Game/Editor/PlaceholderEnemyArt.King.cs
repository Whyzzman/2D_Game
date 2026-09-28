using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The Oath-Bound King, lord of the Ashen Capital (item 2 of the big list, the user's choice
    /// of 2026-09-28: a heavy knight-ruler with a rage phase).
    ///
    /// Drawn on the Guardian's poses, because it is the same kind of fight — a big armoured body
    /// with a sword — and a pose tuned for reading one heavy blow reads the same on the other. The
    /// build is its own: no shield, a longer two-handed blade, ash plate and a court-red mantle
    /// under a crown. Phase two is the curse taking hold: the mantle burns and the runes turn to
    /// embers, so the rage is visible in one frame.
    /// </summary>
    public static partial class PlaceholderEnemyArt
    {
        public const string KingFolder = "Assets/Game/Art/Characters/Bosses/OathBoundKing";

        /// <summary>The Guardian's moveset without the shield bash: the King has no shield.</summary>
        public static readonly Spec<BossAnimationState>[] KingStates =
        {
            new Spec<BossAnimationState>(BossAnimationState.Dormant, 4, 8f, true),
            new Spec<BossAnimationState>(BossAnimationState.Intro, 18, 12f, false),
            new Spec<BossAnimationState>(BossAnimationState.IdleP1, 6, 8f, true),
            new Spec<BossAnimationState>(BossAnimationState.WalkP1, 8, 10f, true),
            new Spec<BossAnimationState>(BossAnimationState.SlashP1, 22, 24f, false),
            new Spec<BossAnimationState>(BossAnimationState.ChargeStart, 16, 24f, false),
            new Spec<BossAnimationState>(BossAnimationState.Charge, 6, 24f, true),
            new Spec<BossAnimationState>(BossAnimationState.ChargeRecover, 12, 24f, false),
            new Spec<BossAnimationState>(BossAnimationState.Combo1, 10, 24f, false),
            new Spec<BossAnimationState>(BossAnimationState.Combo2, 10, 24f, false),
            new Spec<BossAnimationState>(BossAnimationState.Combo3, 14, 24f, false),
            new Spec<BossAnimationState>(BossAnimationState.Stunned, 8, 12f, true),
            new Spec<BossAnimationState>(BossAnimationState.Recoil, 6, 24f, false),
            new Spec<BossAnimationState>(BossAnimationState.PhaseTransition, 26, 12f, false),
            new Spec<BossAnimationState>(BossAnimationState.IdleP2, 6, 10f, true),
            new Spec<BossAnimationState>(BossAnimationState.MoveP2, 8, 14f, true),
            new Spec<BossAnimationState>(BossAnimationState.SlashP2, 18, 24f, false),
            new Spec<BossAnimationState>(BossAnimationState.ComboP2, 16, 24f, false),
            new Spec<BossAnimationState>(BossAnimationState.JumpStart, 14, 24f, false),
            new Spec<BossAnimationState>(BossAnimationState.Jump, 6, 24f, true),
            new Spec<BossAnimationState>(BossAnimationState.Slam, 4, 24f, false),
            new Spec<BossAnimationState>(BossAnimationState.SlamRecover, 14, 24f, false),
            new Spec<BossAnimationState>(BossAnimationState.Death, 26, 12f, false)
        };

        public static Dictionary<BossAnimationState, Sprite[]> GenerateKing() =>
            Generate(KingFolder, "boss_king", KingStates, (state, t) =>
            {
                var actor = ActorForBossState(state);
                return Render(actor, KingBuild(actor == Actor.BossPhaseTwo), KingPose(state, t));
            });

        /// <summary>
        /// The Guardian's pose with the shield arm brought to the hilt: the King swings with both
        /// hands, so the back arm follows the sword instead of holding a guard out.
        /// </summary>
        private static Pose KingPose(BossAnimationState state, float t)
        {
            var pose = BossPose(state, t);
            pose.BackArm = pose.FrontArm - 6f;
            return pose;
        }

        /// <summary>
        /// 1.5 × 2.7 units: a head taller than the hero's fights so far, broad in ash plate. Phase
        /// two burns — the mantle smoulders and the runes are embers, not cold light.
        /// </summary>
        private static Build KingBuild(bool enraged)
        {
            var build = BuildFor(Actor.BossPhaseOne);
            build.Width = 300;
            build.Height = 312;
            build.TorsoWidth = 58f;
            build.SwordLength = 128f;
            build.SwordWidth = 13f;
            build.Armor = AshGrey;
            build.Metal = AshDark;
            build.Cloth = CourtRed;
            build.Trim = FadedGold;
            build.Rot = enraged ? EmberGlow : RuneCold;
            build.Shield = false;
            build.Cape = true;
            build.Crown = true;
            build.Runes = true;
            if (enraged)
            {
                build.Cloth = new Color32(0x6A, 0x22, 0x14, 0xFF);
            }

            return build;
        }
    }
}
