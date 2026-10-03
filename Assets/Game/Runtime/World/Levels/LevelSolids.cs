using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>One block of the level: a rectangle of cells of one kind.</summary>
    public readonly struct LevelBlock
    {
        public readonly LevelCell Kind;
        /// <summary>In zone cells.</summary>
        public readonly RectInt Cells;
        public readonly string RoomId;

        public LevelBlock(LevelCell kind, RectInt cells, string roomId)
        {
            Kind = kind;
            Cells = cells;
            RoomId = roomId;
        }
    }

    /// <summary>
    /// Turns a room's cells into as few rectangles as it reasonably can: runs along a row, grown
    /// upward while the rows above hold the same run. A wall of forty cells is one collider and one
    /// tiled sprite, as the hand-built zones had.
    /// </summary>
    public static class LevelSolids
    {
        public static List<LevelBlock> Blocks(LevelZone zone)
        {
            var blocks = new List<LevelBlock>();
            foreach (var room in zone.Rooms)
            {
                Merge(room, blocks);
            }

            return blocks;
        }

        private static void Merge(LevelRoom room, List<LevelBlock> blocks)
        {
            var taken = new bool[room.Height, room.Width];
            for (var y = 0; y < room.Height; y++)
            {
                var x = 0;
                while (x < room.Width)
                {
                    var kind = room.Local(x, y);
                    if (kind == LevelCell.Empty || taken[y, x])
                    {
                        x++;
                        continue;
                    }

                    var end = x;
                    while (end + 1 < room.Width && room.Local(end + 1, y) == kind && !taken[y, end + 1])
                    {
                        end++;
                    }

                    var top = y;
                    while (top + 1 < room.Height && RowMatches(room, taken, kind, x, end, top + 1))
                    {
                        top++;
                    }

                    for (var yy = y; yy <= top; yy++)
                    {
                        for (var xx = x; xx <= end; xx++)
                        {
                            taken[yy, xx] = true;
                        }
                    }

                    blocks.Add(new LevelBlock(kind,
                        new RectInt(room.Origin.x + x, room.Origin.y + y, end - x + 1, top - y + 1), room.Id));
                    x = end + 1;
                }
            }
        }

        /// <summary>The same kind from x to end, and not one cell more on either side.</summary>
        private static bool RowMatches(LevelRoom room, bool[,] taken, LevelCell kind, int x, int end, int y)
        {
            for (var xx = x; xx <= end; xx++)
            {
                if (room.Local(xx, y) != kind || taken[y, xx])
                {
                    return false;
                }
            }

            var left = x > 0 && room.Local(x - 1, y) == kind && !taken[y, x - 1];
            var right = end + 1 < room.Width && room.Local(end + 1, y) == kind && !taken[y, end + 1];
            return !left && !right;
        }

        /// <summary>The block's rectangle in world units.</summary>
        public static Rect World(LevelZone zone, LevelBlock block) =>
            new Rect(zone.CellCorner(block.Cells.position), (Vector2)block.Cells.size * zone.CellSize);

        /// <summary>
        /// Colliders only, under <paramref name="parent"/>: enough for the motor to stand on. The
        /// editor builder dresses the same blocks with art; tests use this to prove that what the
        /// reach check says about a layout is what the real hero can do in it.
        /// </summary>
        public static void BuildColliders(LevelZone zone, Transform parent, int groundLayer)
        {
            foreach (var block in Blocks(zone))
            {
                if (block.Kind == LevelCell.Spikes)
                {
                    continue;
                }

                var rect = World(zone, block);
                var piece = new GameObject($"{block.RoomId}_{block.Kind}_{block.Cells.x}_{block.Cells.y}");
                piece.transform.SetParent(parent, false);
                piece.transform.position = rect.center;
                piece.layer = groundLayer;
                piece.AddComponent<BoxCollider2D>().size = rect.size;
                if (block.Kind == LevelCell.Smooth)
                {
                    piece.AddComponent<NonClimbableSurface>();
                }
            }
        }
    }
}
