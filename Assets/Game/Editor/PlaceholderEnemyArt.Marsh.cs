using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The Plague Marsh's own (the fifth level, MARSH.md §4–5; decided by Claude under the user's standing
    /// approval, 2026-11-05). The corpse-eater is the hound's rig in drowned grey-green skin; the plague doctor
    /// the shaman's, in a black coat and a pale beaked mask; the leech flies on the raven's rig; the Plague Stag
    /// stands on the King's poses with a deer's skull and antlers. Drawn art (design/characters/enemies) replaces
    /// these frames by file name.
    /// </summary>
    public static partial class PlaceholderEnemyArt
    {
        public const string CorpseEaterFolder = "Assets/Game/Art/Characters/Enemies/MarshCorpseEater";
        public const string PlagueDoctorFolder = "Assets/Game/Art/Characters/Enemies/PlagueDoctor";
        public const string LeechFolder = "Assets/Game/Art/Characters/Enemies/MarshLeech";
        public const string StagFolder = "Assets/Game/Art/Characters/Bosses/PlagueStag";

        private static readonly Color32 DrownedSkin = new Color32(0x3E, 0x4A, 0x38, 0xFF);
        private static readonly Color32 DrownedDark = new Color32(0x24, 0x2C, 0x22, 0xFF);
        private static readonly Color32 PlagueBlack = new Color32(0x22, 0x20, 0x22, 0xFF);
        private static readonly Color32 MaskPale = new Color32(0x9A, 0x92, 0x7E, 0xFF);
        private static readonly Color32 PoisonGlow = new Color32(0x9C, 0xE0, 0x4A, 0xFF);
        private static readonly Color32 StagHide = new Color32(0x4A, 0x44, 0x38, 0xFF);

        public static Spec<EnemyAnimationState>[] CorpseEaterStates => HoundStates;

        public static Spec<EnemyAnimationState>[] PlagueDoctorStates => ShamanStates;

        public static Spec<EnemyAnimationState>[] LeechStates => RavenStates;

        public static Dictionary<EnemyAnimationState, Sprite[]> GenerateCorpseEater() =>
            Generate(CorpseEaterFolder, "enemy_corpse_eater", CorpseEaterStates,
                (state, t) => RenderHound(state, t, CorpseEaterBuild()));

        public static Dictionary<EnemyAnimationState, Sprite[]> GeneratePlagueDoctor() =>
            Generate(PlagueDoctorFolder, "enemy_plague_doctor", PlagueDoctorStates,
                (state, t) => Render(Actor.Shaman, PlagueDoctorBuild(),
                    Telegraphed(Actor.Shaman, state, t, ForestPose(Actor.Shaman, state, t))));

        public static Dictionary<EnemyAnimationState, Sprite[]> GenerateLeech() =>
            Generate(LeechFolder, "enemy_leech", LeechStates, RenderRaven);

        public static Dictionary<BossAnimationState, Sprite[]> GeneratePlagueStag() =>
            Generate(StagFolder, "boss_stag", KingStates, (state, t) =>
            {
                var actor = ActorForBossState(state);
                return Render(actor, StagBuild(actor == Actor.BossPhaseTwo), KingPose(state, t));
            });

        private static Build CorpseEaterBuild()
        {
            var build = ForestBuildFor(Actor.Hound);
            build.Armor = DrownedSkin;
            build.Cloth = DrownedDark;
            build.Metal = DrownedDark;
            build.Trim = DrownedDark;
            build.Skin = DrownedSkin;
            build.Rot = PoisonGlow;
            return build;
        }

        private static Build PlagueDoctorBuild()
        {
            var build = ForestBuildFor(Actor.Shaman);
            build.Armor = PlagueBlack;
            build.Cloth = PlagueBlack;
            build.Metal = MaskPale;
            build.Trim = MaskPale;
            build.Skin = MaskPale;
            build.Rot = PoisonGlow;
            build.Head = HeadGear.Mask;
            build.Growth = false;
            return build;
        }

        /// <summary>Tall and gaunt under a deer's skull: antlers wider than his shoulders, a rotten cloak.</summary>
        private static Build StagBuild(bool rotting)
        {
            var build = BuildFor(Actor.BossPhaseOne);
            build.Width = 300;
            build.Height = 312;
            build.TorsoWidth = 54f;
            build.SwordLength = 120f;
            build.SwordWidth = 6f;
            build.Armor = StagHide;
            build.Metal = BoneWhite;
            build.Cloth = DrownedDark;
            build.Trim = rotting ? PoisonGlow : DrownedSkin;
            build.Rot = PoisonGlow;
            build.Shield = false;
            build.Cape = true;
            build.Crown = false;
            build.Runes = rotting;
            build.Head = HeadGear.Antlers;
            build.Weapon = WeaponShape.Glaive;
            return build;
        }
    }
}
