namespace ForgottenKingdom
{
    /// <summary>
    /// «Нова гра+» (item 5 of the big list, stage C; the user's rules of 2026-09-28): started from
    /// the title once the game is completed, it keeps the level, skills, equipment, items and
    /// gold, and starts the world again — movement abilities, map, bosses, secrets, bestiary.
    /// Enemies and bosses have half again the health and deal half again the damage. The cycle
    /// is kept in the save; <see cref="GameProgress"/> tells this class which one is played.
    /// </summary>
    public static class NewGamePlus
    {
        public const float EnemyScale = 1.5f;

        /// <summary>0 for a first run, 1 for the first «Нова гра+», and so on.</summary>
        public static int Cycle { get; private set; }

        /// <summary>What enemy health and enemy damage are multiplied by now.</summary>
        public static float Scale => Cycle > 0 ? EnemyScale : 1f;

        internal static void Adopt(GameProgressData data) => Cycle = data != null ? data.newGamePlus : 0;

        /// <summary>The save a finished run hands to the next one.</summary>
        public static GameProgressData From(GameProgressData finished)
        {
            var next = GameProgressData.CreateNew();
            if (finished == null)
            {
                return next;
            }

            next.newGamePlus = finished.newGamePlus + 1;
            if (finished.player != null)
            {
                next.player = finished.player;
                next.player.unlockedAbilities = PlayerProgressionData.CreateDefault().unlockedAbilities;
            }

            if (finished.inventory != null) { next.inventory = finished.inventory; }
            if (finished.equipment != null) { next.equipment = finished.equipment; }
            return next;
        }
    }
}
