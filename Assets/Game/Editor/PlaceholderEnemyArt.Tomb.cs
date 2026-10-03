using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The Tomb's bone archer (item 3 of the 15-zone list, `WORLD_DESIGN.md`): the forest archer's
    /// body and draw, but what is left of a royal bowman after the Tomb kept him — bone where the
    /// hunter has flesh, a grave-dust rag for the hood, rusted fittings, no moss.
    /// </summary>
    public static partial class PlaceholderEnemyArt
    {
        public const string BoneArcherFolder = "Assets/Game/Art/Characters/Enemies/BoneArcher";
        private static readonly Color32 GraveDust = new Color32(0x4A, 0x4C, 0x52, 0xFF);

        public static Spec<EnemyAnimationState>[] BoneArcherStates => ArcherStates;

        public static Dictionary<EnemyAnimationState, Sprite[]> GenerateBoneArcher() =>
            Generate(BoneArcherFolder, "enemy_bone_archer", BoneArcherStates,
                (state, t) => Render(Actor.Archer, BoneArcherBuild(),
                    Telegraphed(Actor.Archer, state, t, ForestPose(Actor.Archer, state, t))));

        /// <summary>0.8 × 1.7 units, narrower than the hunter: ribs and a rag, not a jerkin.</summary>
        private static Build BoneArcherBuild()
        {
            var build = ForestBuildFor(Actor.Archer);
            build.TorsoWidth = 20f;
            build.ArmWidth = 5f;
            build.LegWidth = 6f;
            build.Armor = BoneWhite;
            build.Cloth = GraveDust;
            build.Metal = Rust;
            build.Trim = Rust;
            build.Skin = BoneWhite;
            build.Rot = RuneCold;
            build.Growth = false;
            return build;
        }
    }
}
