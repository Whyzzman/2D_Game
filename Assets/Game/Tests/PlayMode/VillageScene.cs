using UnityEngine;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The places tests stand the hero at in the Fallen Village. Since item 5 of the 15-zone list
    /// the Village is built from its text (`Levels/village.txt`), so places are asked of the text's
    /// named spots and rooms, never written as numbers.
    /// </summary>
    internal static class VillageScene
    {
        private static LevelZone _layout;

        /// <summary>The Village's text, read once.</summary>
        public static LevelZone Layout => _layout ?? (_layout = LevelZone.Parse(
            System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath, "Game/Levels/village.txt"))));

        /// <summary>A named spot of the text (<c>spot name</c>, or a thing's kind, or an altar's id): its floor, mid-cell.</summary>
        public static Vector2 Spot(string name)
        {
            foreach (var marker in Layout.Markers())
            {
                if ((marker.Kind == "spot" && marker.Arg(0) == name) || marker.Kind == name
                    || (marker.Kind == "altar" && marker.Arg(0) == name))
                {
                    return Feet(marker);
                }
            }

            throw new System.ArgumentException($"The Village's text has no spot «{name}».");
        }

        /// <summary>Where a lettered thing stands: the middle of its cell, on the cell's floor.</summary>
        public static Vector2 Feet(LevelMarker marker) =>
            Layout.CellCorner(marker.Cell) + new Vector2(Layout.CellSize * 0.5f, 0f);

        /// <summary>A room of the text in the world, walls included.</summary>
        public static Rect Room(string roomId) => Layout.WorldBounds(Layout.Room(roomId));

        /// <summary>Whether a thing stands inside a room of the Village's text.</summary>
        public static bool InRoom(Component component, string roomId) =>
            component != null && Room(roomId).Contains(component.transform.position);

        /// <summary>A motor reset point (0.85 over the floor) <paramref name="dx"/> from a spot.</summary>
        public static Vector2 Ground(string spot, float dx = 0f) => Spot(spot) + new Vector2(dx, 0.85f);
    }
}
