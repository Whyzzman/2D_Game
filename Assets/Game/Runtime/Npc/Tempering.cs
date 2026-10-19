namespace ForgottenKingdom
{
    /// <summary>
    /// The smith Ostap's tempering (item 6c of the 15 zones; decided by Claude under the user's
    /// standing approval): three levels bought in turn for gold and iron scrap, each adding a tenth of
    /// the weapon's own base to whatever weapon is in hand — the smith hardens the hand and the
    /// craft, so a new blade keeps it. The save holds «temper:1»…«temper:3».
    /// </summary>
    public static class Tempering
    {
        public const int MaxLevel = 3;
        public const float BonusPerLevel = 0.1f;
        public const string IronItemId = "iron_scrap";

        public static string RewardId(int level) => "temper:" + level;

        /// <summary>How far the hero has been tempered: the highest level bought.</summary>
        public static int Level(GameProgress progress)
        {
            if (progress == null)
            {
                return 0;
            }

            var level = 0;
            while (level < MaxLevel && progress.HasReward(RewardId(level + 1)))
            {
                level++;
            }

            return level;
        }

        /// <summary>The attack power tempering adds to a weapon of this base.</summary>
        public static float Bonus(float weaponBase, int level) => weaponBase * BonusPerLevel * level;
    }
}
