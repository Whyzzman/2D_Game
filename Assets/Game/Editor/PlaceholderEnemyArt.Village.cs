using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The Fallen Village's own three (item 5b of the 15 zones, the user's choices of 2026-10-19).
    /// The possessed villager and the scarecrow share the spearman's poses — a pitchfork held like a
    /// spear, a scythe-blade like the gargoyle's claw — and differ by build alone; the scarecrow
    /// keeps the gargoyle's trick of a still idle, hung on its pole with its arms out. The watchdog
    /// is the forest hound's rig in a farm dog's coat with an iron collar.
    /// </summary>
    public static partial class PlaceholderEnemyArt
    {
        public const string VillagerFolder = "Assets/Game/Art/Characters/Enemies/PossessedVillager";
        public const string ScarecrowFolder = "Assets/Game/Art/Characters/Enemies/Scarecrow";
        public const string WatchdogFolder = "Assets/Game/Art/Characters/Enemies/Watchdog";

        private static readonly Color32 Homespun = new Color32(0x4C, 0x40, 0x31, 0xFF);
        private static readonly Color32 HomespunDark = new Color32(0x2F, 0x28, 0x20, 0xFF);
        private static readonly Color32 Straw = new Color32(0x7A, 0x66, 0x3A, 0xFF);
        private static readonly Color32 Sackcloth = new Color32(0x5A, 0x4B, 0x36, 0xFF);
        private static readonly Color32 DogCoat = new Color32(0x3E, 0x33, 0x2A, 0xFF);
        private static readonly Color32 PossessedGlow = new Color32(0xC8, 0x5A, 0x3C, 0xFF);

        public static Spec<EnemyAnimationState>[] VillagerStates => SpearmanStates;

        public static Spec<EnemyAnimationState>[] ScarecrowStates => GargoyleStates;

        public static Spec<EnemyAnimationState>[] WatchdogStates => HoundStates;

        public static Dictionary<EnemyAnimationState, Sprite[]> GenerateVillager() =>
            Generate(VillagerFolder, "enemy_villager", VillagerStates,
                (state, t) => Render(Actor.Spearman, VillagerBuild(),
                    Telegraphed(Actor.Spearman, state, t, ForestPose(Actor.Spearman, state, t))));

        public static Dictionary<EnemyAnimationState, Sprite[]> GenerateScarecrow() =>
            Generate(ScarecrowFolder, "enemy_scarecrow", ScarecrowStates,
                (state, t) => Render(Actor.Spearman, ScarecrowBuild(),
                    Telegraphed(Actor.Spearman, state, t, ScarecrowPose(state, t))));

        public static Dictionary<EnemyAnimationState, Sprite[]> GenerateWatchdog() =>
            Generate(WatchdogFolder, "enemy_watchdog", WatchdogStates,
                (state, t) => RenderHound(state, t, WatchdogBuild()));

        /// <summary>The spearman's frame, unarmoured: a smock, bare head, a long-tined fork.</summary>
        private static Build VillagerBuild()
        {
            var build = ForestBuildFor(Actor.Spearman);
            build.TorsoWidth = 26f;
            build.ArmWidth = 6.5f;
            build.LegWidth = 8.5f;
            build.Armor = Homespun;
            build.Cloth = HomespunDark;
            build.Metal = Rust;
            build.Trim = HomespunDark;
            build.Skin = Flesh;
            build.Rot = PossessedGlow;
            build.SwordLength = 84f;
            build.SwordWidth = 4f;
            build.Head = HeadGear.Hood;
            build.Growth = false;
            return build;
        }

        /// <summary>A sack for a head, a straw coat flaring to the ground, a rusted scythe-blade.</summary>
        private static Build ScarecrowBuild()
        {
            var build = ForestBuildFor(Actor.Spearman);
            build.TorsoWidth = 30f;
            build.ArmWidth = 7f;
            build.LegWidth = 7f;
            build.Armor = Sackcloth;
            build.Cloth = Straw;
            build.Metal = Rust;
            build.Trim = Straw;
            build.Skin = Sackcloth;
            build.Rot = PossessedGlow;
            build.Weapon = WeaponShape.Glaive;
            build.SwordLength = 60f;
            build.SwordWidth = 7f;
            build.Head = HeadGear.Hood;
            build.RobeFlare = 22f;
            build.Growth = false;
            return build;
        }

        /// <summary>The hound's canvas and rig, a farm dog's coat and an iron collar for trim.</summary>
        private static Build WatchdogBuild()
        {
            var build = ForestBuildFor(Actor.Hound);
            build.Armor = DogCoat;
            build.Cloth = HomespunDark;
            build.Metal = DarkSteel;
            build.Trim = Steel;
            build.Skin = DogCoat;
            build.Rot = PossessedGlow;
            return build;
        }

        /// <summary>
        /// The spearman's poses with the scarecrow's idle: hung on its pole, arms out along the
        /// crossbar, head dropped — nothing a passer-by would look at twice.
        /// </summary>
        private static Pose ScarecrowPose(EnemyAnimationState state, float t)
        {
            if (state != EnemyAnimationState.Idle)
            {
                return ForestPose(Actor.Spearman, state, t);
            }

            var pose = ForestPose(Actor.Spearman, EnemyAnimationState.Idle, 0f);
            pose.HipY += 6f;
            pose.Lean = 0f;
            pose.HeadTilt = 22f;
            pose.FrontThigh = 2f;
            pose.FrontShin = 4f;
            pose.BackThigh = -2f;
            pose.BackShin = 4f;
            pose.FrontArm = 88f;
            pose.FrontForearm = 6f;
            pose.BackArm = -88f;
            pose.BackForearm = -6f;
            pose.SwordAngle = 170f;
            return pose;
        }
    }
}
