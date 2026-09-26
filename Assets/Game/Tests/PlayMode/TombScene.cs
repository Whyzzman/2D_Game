using System.Linq;
using UnityEngine;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// Finds the Tomb's own pieces in a scene that now also holds the Withered Forest.
    ///
    /// Before PHASE 24 the level had one boss, two altars and five enemies, and tests could ask
    /// for "the" encounter. With a second region in the same scene that question has two answers
    /// and Unity picks one arbitrarily, so the Tomb's tests ask for the Tomb by name and position.
    /// </summary>
    internal static class TombScene
    {
        public const string GuardianId = "guardian-of-the-tomb";
        public const string WardenId = "hollow-warden";

        /// <summary>The Tomb ends at its exit wall; everything east of it is the forest.</summary>
        public const float TombEastEdge = 62.5f;

        public static BossEncounter GuardianEncounter() => Encounter(GuardianId);

        public static BossEncounter WardenEncounter() => Encounter(WardenId);

        public static BossController Guardian() => Boss(GuardianId);

        public static BossController Warden() => Boss(WardenId);

        public static bool InTomb(Component component) =>
            component != null && component.transform.position.x < TombEastEdge;

        public static EnemyController[] TombEnemies() =>
            Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Include)
                .Where(InTomb)
                .ToArray();

        public static Checkpoint[] TombAltars() =>
            Object.FindObjectsByType<Checkpoint>(FindObjectsInactive.Include)
                .Where(InTomb)
                .OrderBy(checkpoint => checkpoint.transform.position.x)
                .ToArray();

        private static BossEncounter Encounter(string bossId) =>
            Object.FindObjectsByType<BossEncounter>(FindObjectsInactive.Include)
                .FirstOrDefault(encounter => encounter.Boss != null
                    && encounter.Boss.BossId == bossId);

        private static BossController Boss(string bossId) =>
            Object.FindObjectsByType<BossController>(FindObjectsInactive.Include)
                .FirstOrDefault(boss => boss.BossId == bossId);
    }
}
