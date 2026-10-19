using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The Ash Commander (item 6b of the 15-zone list): on the King's poses, a knight in burnt-black
    /// plate with a crimson banner hung on his shield and a halberd. Phase two: the plate's seams glow
    /// like a coal.
    /// </summary>
    public static partial class PlaceholderEnemyArt
    {
        public const string CommanderFolder = "Assets/Game/Art/Characters/Bosses/AshCommander";

        private static readonly Color32 BurntPlate = new Color32(0x2C, 0x2A, 0x2A, 0xFF);
        private static readonly Color32 BurntPlateLit = new Color32(0x4A, 0x46, 0x44, 0xFF);
        private static readonly Color32 BannerRed = new Color32(0x5E, 0x1A, 0x1E, 0xFF);
        private static readonly Color32 CoalGlow = new Color32(0xE8, 0x6A, 0x30, 0xFF);

        public static Dictionary<BossAnimationState, Sprite[]> GenerateCommander() =>
            Generate(CommanderFolder, "boss_commander", KingStates, (state, t) =>
            {
                var actor = ActorForBossState(state);
                return Render(actor, CommanderBuild(actor == Actor.BossPhaseTwo), KingPose(state, t));
            });

        /// <summary>1.4 × 2.6 units: upright and armoured, the halberd longer than he is tall.</summary>
        private static Build CommanderBuild(bool burning)
        {
            var build = BuildFor(Actor.BossPhaseOne);
            build.Width = 320;
            build.Height = 300;
            build.TorsoWidth = 60f;
            build.SwordLength = 150f;
            build.SwordWidth = 7f;
            build.Armor = BurntPlate;
            build.Metal = BurntPlateLit;
            build.Cloth = BannerRed;
            build.Trim = burning ? CoalGlow : BurntPlateLit;
            build.Rot = burning ? CoalGlow : BannerRed;
            build.Shield = true;
            build.Cape = true;
            build.Crown = false;
            build.Runes = burning;
            build.Head = HeadGear.Helm;
            build.Weapon = WeaponShape.Glaive;
            return build;
        }
    }
}
