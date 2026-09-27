using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The rooms the camera keeps to, as in Hollow Knight: the view stays inside the room the
    /// player is in and never shows the next one until the player crosses into it.
    ///
    /// A room is an area of the world graph that no other area of the scene contains (a secret
    /// nook inside a hall belongs to the hall's view). Its sides are the area's; its floor and top
    /// come from the level itself — the ground under the lowest footing and some headroom over
    /// the highest — so reshaping the level reshapes the view without anyone retyping numbers.
    /// </summary>
    public sealed class CameraRooms
    {
        /// <summary>Ground shown under the lowest solid thing in a room.</summary>
        public const float FloorMargin = 1f;

        /// <summary>
        /// Room over the highest footing: a double jump (~3.4) plus the player's height, so a jump
        /// off the top ledge is still on screen.
        /// </summary>
        public const float Headroom = 5.5f;

        /// <summary>How far past a shared edge the player goes before the view changes rooms.</summary>
        public const float SwitchMargin = 0.5f;

        private readonly List<Room> _rooms = new List<Room>();

        public int Count => _rooms.Count;

        public IReadOnlyList<Room> Rooms => _rooms;

        /// <summary>A room: the area it is known by in the graph and the rectangle the view keeps to.</summary>
        public readonly struct Room
        {
            public Room(string areaId, Rect area, Rect view)
            {
                AreaId = areaId;
                Area = area;
                View = view;
            }

            public string AreaId { get; }

            /// <summary>The graph area: whether the player is in this room.</summary>
            public Rect Area { get; }

            /// <summary>What the camera may show while the player is in it.</summary>
            public Rect View { get; }
        }

        /// <summary>
        /// The rooms of one scene. Areas are given as the graph has them; <paramref name="solids"/>
        /// answers what level geometry lies inside a rectangle.
        /// </summary>
        public static CameraRooms Build(IReadOnlyList<(string id, Rect area)> areas,
            System.Func<Rect, IEnumerable<Bounds>> solids)
        {
            var rooms = new CameraRooms();
            for (var i = 0; i < areas.Count; i++)
            {
                var (id, area) = areas[i];
                if (IsInsideAnother(areas, i))
                {
                    continue;
                }

                rooms._rooms.Add(new Room(id, area, ViewOf(area, solids(area))));
            }

            return rooms;
        }

        /// <summary>
        /// The view rectangle of one area: its sides, the floor a little under the lowest solid and
        /// the top with headroom over the highest. An area with no geometry keeps its own height.
        /// </summary>
        public static Rect ViewOf(Rect area, IEnumerable<Bounds> solids)
        {
            var bottom = float.MaxValue;
            var top = float.MinValue;
            foreach (var solid in solids)
            {
                bottom = Mathf.Min(bottom, Mathf.Max(solid.min.y, area.yMin));
                top = Mathf.Max(top, Mathf.Min(solid.max.y, area.yMax));
            }

            if (bottom > top)
            {
                return area;
            }

            return Rect.MinMaxRect(area.xMin, Mathf.Max(area.yMin, bottom - FloorMargin),
                area.xMax, Mathf.Min(area.yMax, top + Headroom));
        }

        /// <summary>
        /// The room at a position. The one the player is already in wins until they are clearly
        /// out of it, so standing on a shared edge does not swing the view back and forth.
        /// </summary>
        public int RoomAt(Vector2 position, int current)
        {
            if (current >= 0 && current < _rooms.Count)
            {
                var area = _rooms[current].Area;
                var held = Rect.MinMaxRect(area.xMin - SwitchMargin, area.yMin - SwitchMargin,
                    area.xMax + SwitchMargin, area.yMax + SwitchMargin);
                if (held.Contains(position))
                {
                    return current;
                }
            }

            for (var i = 0; i < _rooms.Count; i++)
            {
                if (_rooms[i].Area.Contains(position))
                {
                    return i;
                }
            }

            return current;
        }

        public int IndexOf(string areaId)
        {
            for (var i = 0; i < _rooms.Count; i++)
            {
                if (_rooms[i].AreaId == areaId)
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool IsInsideAnother(IReadOnlyList<(string id, Rect area)> areas, int index)
        {
            var area = areas[index].area;
            for (var i = 0; i < areas.Count; i++)
            {
                var other = areas[i].area;
                if (i != index && other.xMin <= area.xMin && other.xMax >= area.xMax
                    && other.yMin <= area.yMin && other.yMax >= area.yMax
                    && (other.width * other.height > area.width * area.height || i < index))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
