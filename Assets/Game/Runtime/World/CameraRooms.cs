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
    ///
    /// Hollow Knight never has a room narrower than the screen. Ours can be, and a camera held
    /// still in each of them would slide at every seam, so a room narrower than the view is
    /// joined to its narrower neighbour until it is wide enough. A boss arena is never joined:
    /// its fight is framed on the arena alone.
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
            public Room(string areaId, Rect area, Rect view, bool keepApart = false)
                : this(new[] { areaId }, area, view, keepApart)
            {
            }

            private Room(string[] areaIds, Rect area, Rect view, bool keepApart)
            {
                AreaIds = areaIds;
                Area = area;
                View = view;
                KeepApart = keepApart;
            }

            /// <summary>The graph areas this room is made of; more than one once joined.</summary>
            public IReadOnlyList<string> AreaIds { get; }

            /// <summary>Never joined to a neighbour (a boss arena).</summary>
            public bool KeepApart { get; }

            /// <summary>The graph area: whether the player is in this room.</summary>
            public Rect Area { get; }

            /// <summary>What the camera may show while the player is in it.</summary>
            public Rect View { get; }

            public Room JoinedWith(Room other)
            {
                var ids = new string[AreaIds.Count + other.AreaIds.Count];
                for (var i = 0; i < AreaIds.Count; i++) { ids[i] = AreaIds[i]; }
                for (var i = 0; i < other.AreaIds.Count; i++) { ids[AreaIds.Count + i] = other.AreaIds[i]; }
                return new Room(ids, Union(Area, other.Area), Union(View, other.View), false);
            }

            private static Rect Union(Rect a, Rect b) => Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin),
                Mathf.Min(a.yMin, b.yMin), Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax));
        }

        /// <summary>
        /// The rooms of one scene. Areas are given as the graph has them; <paramref name="solids"/>
        /// answers what level geometry lies inside a rectangle. Rooms narrower than
        /// <paramref name="minWidth"/> (the view's width) are joined, except those
        /// <paramref name="keepApart"/> names.
        /// </summary>
        public static CameraRooms Build(IReadOnlyList<(string id, Rect area)> areas,
            System.Func<Rect, IEnumerable<Bounds>> solids, float minWidth = 0f,
            System.Func<string, bool> keepApart = null)
        {
            var rooms = new CameraRooms();
            for (var i = 0; i < areas.Count; i++)
            {
                var (id, area) = areas[i];
                if (IsInsideAnother(areas, i))
                {
                    continue;
                }

                rooms._rooms.Add(new Room(id, area, ViewOf(area, solids(area)),
                    keepApart != null && keepApart(id)));
            }

            rooms.JoinNarrowRooms(minWidth);
            return rooms;
        }

        /// <summary>
        /// Joins the narrowest room that is still narrower than the view to its narrower side
        /// neighbour, and again, until every room is wide enough or has no one left to join.
        /// </summary>
        private void JoinNarrowRooms(float minWidth)
        {
            while (true)
            {
                var narrowest = -1;
                var partner = -1;
                for (var i = 0; i < _rooms.Count; i++)
                {
                    var room = _rooms[i];
                    if (room.KeepApart || room.View.width >= minWidth
                        || (narrowest >= 0 && room.View.width >= _rooms[narrowest].View.width))
                    {
                        continue;
                    }

                    var neighbour = NarrowerNeighbour(i);
                    if (neighbour >= 0)
                    {
                        narrowest = i;
                        partner = neighbour;
                    }
                }

                if (narrowest < 0)
                {
                    return;
                }

                var joined = _rooms[narrowest].JoinedWith(_rooms[partner]);
                _rooms[Mathf.Min(narrowest, partner)] = joined;
                _rooms.RemoveAt(Mathf.Max(narrowest, partner));
            }
        }

        private int NarrowerNeighbour(int index)
        {
            var room = _rooms[index].Area;
            var best = -1;
            for (var i = 0; i < _rooms.Count; i++)
            {
                var other = _rooms[i];
                if (i == index || other.KeepApart)
                {
                    continue;
                }

                var sideBySide = Mathf.Abs(other.Area.xMin - room.xMax) < 0.01f
                    || Mathf.Abs(other.Area.xMax - room.xMin) < 0.01f;
                var overlap = Mathf.Min(other.Area.yMax, room.yMax) - Mathf.Max(other.Area.yMin, room.yMin);
                if (sideBySide && overlap > Mathf.Min(other.Area.height, room.height) * 0.5f
                    && (best < 0 || other.View.width < _rooms[best].View.width))
                {
                    best = i;
                }
            }

            return best;
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
                foreach (var id in _rooms[i].AreaIds)
                {
                    if (id == areaId)
                    {
                        return i;
                    }
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
