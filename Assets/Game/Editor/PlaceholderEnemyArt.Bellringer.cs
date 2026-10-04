using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The Bellringer, keeper of the Fallen Village's bell (item 5 of the 15-zone list, the plan's
    /// «бой на площадке у колокола; удары колокола — волны по полу»).
    ///
    /// Drawn on the King's poses (themselves the Guardian's), because it is the same kind of fight
    /// — a big body and one heavy two-handed weapon. The build is its own: a hunched frame in a
    /// rope-belted smock and a deep hood instead of plate and crown, and a bell-hammer instead of a
    /// blade. Phase two is the bell's toll taking him: the hood's shadow and the hammer's head
    /// glow like a struck bell.
    /// </summary>
    public static partial class PlaceholderEnemyArt
    {
        public const string BellringerFolder = "Assets/Game/Art/Characters/Bosses/Bellringer";

        private static readonly Color32 Smock = new Color32(0x4A, 0x3A, 0x2C, 0xFF);
        private static readonly Color32 SmockDark = new Color32(0x2E, 0x24, 0x1C, 0xFF);
        private static readonly Color32 BellBronze = new Color32(0x8C, 0x6A, 0x36, 0xFF);
        private static readonly Color32 TollGlow = new Color32(0xF2, 0xB0, 0x5A, 0xFF);

        public static Dictionary<BossAnimationState, Sprite[]> GenerateBellringer() =>
            Generate(BellringerFolder, "boss_bellringer", KingStates, (state, t) =>
            {
                var actor = ActorForBossState(state);
                return Render(actor, BellringerBuild(actor == Actor.BossPhaseTwo), KingPose(state, t));
            });

        /// <summary>1.4 × 2.5 units: broad and stooped, smaller than the King, all of it in the hammer.</summary>
        private static Build BellringerBuild(bool tolling)
        {
            var build = BuildFor(Actor.BossPhaseOne);
            build.Width = 300;
            build.Height = 300;
            build.TorsoWidth = 62f;
            build.SwordLength = 120f;
            build.SwordWidth = 9f;
            build.Armor = Smock;
            build.Metal = SmockDark;
            build.Cloth = SmockDark;
            build.Trim = BellBronze;
            build.Rot = tolling ? TollGlow : BellBronze;
            build.Shield = false;
            build.Cape = false;
            build.Crown = false;
            build.Runes = false;
            build.Head = HeadGear.Hood;
            build.Weapon = WeaponShape.Maul;
            build.RobeFlare = 44f;
            if (tolling)
            {
                build.Trim = TollGlow;
            }

            return build;
        }
    }
}
