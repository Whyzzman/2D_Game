using System.Linq;
using UnityEngine;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// Finds the Tomb's own pieces in a scene that now also holds the Withered Forest, and the
    /// places tests stand the hero at. Since item 3 of the 15-zone list the Tomb is built from its
    /// text (`Levels/tomb.txt`), so places are asked of the scene (the spawn, the Guardian's door)
    /// or of the text's named spots, never written as numbers.
    /// </summary>
    internal static class TombScene
    {
        public const string GuardianId = "guardian-of-the-tomb";
        public const string WardenId = "hollow-warden";

        /// <summary>The Tomb ends at its exit wall (x 62.5); everything east of it is the forest.</summary>
        public const float TombEastEdge = 62.5f;

        public static BossEncounter GuardianEncounter() => Encounter(GuardianId);

        private static LevelZone _layout;

        /// <summary>The Tomb's text, read once.</summary>
        public static LevelZone Layout => _layout ?? (_layout = LevelZone.Parse(
            System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath, "Game/Levels/tomb.txt"))));

        /// <summary>A named spot of the text (<c>spot name</c>, or a thing's kind): its floor, mid-cell.</summary>
        public static Vector2 Spot(string name)
        {
            foreach (var marker in Layout.Markers())
            {
                if ((marker.Kind == "spot" && marker.Arg(0) == name) || (marker.Kind == name))
                {
                    return Layout.CellCorner(marker.Cell) + new Vector2(Layout.CellSize * 0.5f, 0f);
                }
            }

            throw new System.ArgumentException($"The Tomb's text has no spot «{name}».");
        }

        /// <summary>Where the hero wakes: the feet on the entrance's floor.</summary>
        public static Vector2 StartFeet => Spot("start");

        /// <summary>
        /// A place on the entrance's floor, <paramref name="dx"/> from the start (the training
        /// figures stand 3.5, 6.5 and 9.5 east of it; flat from 1.5 west to 14 east), as a motor
        /// reset point (0.85 over the floor).
        /// </summary>
        public static Vector2 Ground(float dx) => StartFeet + new Vector2(dx, 0.85f);

        /// <summary>Just inside the Guardian's door, on the trigger that starts the fight.</summary>
        public static Vector2 ArenaEntry() =>
            (Vector2)GuardianEncounter().transform.position + new Vector2(0f, -1f);

        /// <summary>A point of the Guardian's floor, <paramref name="dx"/> from his spot.</summary>
        public static Vector2 ArenaFloor(float dx) => Spot("guardian") + new Vector2(dx, 1f);

        public static BossEncounter WardenEncounter() => Encounter(WardenId);

        public static BossController Guardian() => Boss(GuardianId);

        public static BossController Warden() => Boss(WardenId);

        /// <summary>Whether a thing stands inside a room of the Tomb's text.</summary>
        public static bool InRoom(Component component, string roomId) =>
            component != null && Layout.WorldBounds(Layout.Room(roomId)).Contains(component.transform.position);

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
