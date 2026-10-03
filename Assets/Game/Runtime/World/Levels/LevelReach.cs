using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// A stretch of floor the hero can stand anywhere along: one row, a run of positions. Positions
    /// are cell corners (the body, 0.8 wide, is centred on a corner), so a corridor two cells wide
    /// still has somewhere to stand. A grapple ring is a node too, with no floor.
    /// </summary>
    public sealed class ReachSegment
    {
        public int Index;
        public int Row;
        public int XMin;
        public int XMax;
        public string RoomId;
        public bool IsRing;
        public Vector2 RingPoint;

        public bool Holds(int x) => x >= XMin && x <= XMax;
        public override string ToString() => IsRing ? $"ring at {RingPoint}" : $"{RoomId} row {Row} x {XMin}…{XMax}";
    }

    /// <summary>
    /// Where the hero can go in a <see cref="LevelZone"/>, worked out by moving him: every jump,
    /// fall, second jump and dash in the air is stepped through the real
    /// <see cref="PlayerMovementLogic"/> at the motor's 50 Hz against the zone's cells. The result is
    /// a graph of floors (<see cref="ReachSegment"/>) and the moves between them.
    ///
    /// It is a model, not the physics: the body is a 0.8 × 1.6 box standing on the middle half of
    /// its feet, walls give nothing back (no wall jumps or runs) and a running start is assumed
    /// wherever there is floor behind. It comes strict or generous (<see cref="StrictJump"/>);
    /// PlayMode tests hold the real hero between the two (`LevelReachTests`).
    /// </summary>
    public sealed class LevelReach
    {
        private const float Dt = 0.02f;
        private const int MaxSteps = 160;
        private const float BodyWidth = 0.8f;
        private const float BodyHeight = 1.6f;
        private const float SubStep = 0.1f;
        private const float GroundProbe = 0.03f;
        private const float SupportHalfWidth = 0.25f;
        /// <summary>The grapple's reach and how it lets go: 1.9 under the ring, thrown up at 7.</summary>
        private const float RingRange = 7.5f;
        private const float RingRelease = 1.9f;
        private const float RingPop = 7f;

        private readonly LevelZone _zone;
        private readonly MovementSettings _move;
        private readonly DashSettings _dash;
        private readonly bool _doubleJump;
        private readonly bool _airDash;
        private readonly bool _grapple;
        private readonly List<ReachSegment> _segments = new List<ReachSegment>();
        private readonly List<HashSet<int>> _edges = new List<HashSet<int>>();
        private readonly Dictionary<int, List<ReachSegment>> _byRow = new Dictionary<int, List<ReachSegment>>();
        private RectInt _grid;
        private LevelCell[,] _cells;

        /// <summary>
        /// The two models. The real hero sits between them: the strict one jumps a little lower and
        /// runs a little slower than he does (what it allows, he surely does — the main path counts on
        /// it); the generous one a little stronger (what it forbids, he surely cannot — a gate counts
        /// on that). Measured 2026-10-03: with the second jump the hero stood on a shelf 3.5 up, and
        /// fell short of a gap 5.0 across that the plain numbers allowed.
        /// </summary>
        public const float StrictJump = 0.97f;
        public const float StrictRun = 0.95f;
        public const float GenerousJump = 1.04f;
        public const float GenerousRun = 1.05f;

        public LevelReach(LevelZone zone, ICollection<string> abilities,
            MovementSettings move = null, DashSettings dash = null, bool generous = false)
        {
            _zone = zone;
            _move = Scaled(move ?? new MovementSettings(), generous ? GenerousJump : StrictJump,
                generous ? GenerousRun : StrictRun);
            _dash = dash ?? new DashSettings();
            _doubleJump = abilities != null && abilities.Contains(AbilityIds.DoubleJump);
            _airDash = abilities != null && abilities.Contains(AbilityIds.AirDash);
            _grapple = abilities != null && abilities.Contains(AbilityIds.Grapple);
            Rasterise();
            FindSegments();
            AddRings();
            foreach (var segment in _segments.ToArray())
            {
                Moves(segment);
            }
        }

        public IReadOnlyList<ReachSegment> Segments => _segments;

        private static MovementSettings Scaled(MovementSettings source, float jump, float run)
        {
            var scaled = new MovementSettings();
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(source), scaled);
            JsonUtility.FromJsonOverwrite("{\"_jumpSpeed\":" + Inv(source.JumpSpeed * jump)
                + ",\"_airJumpSpeed\":" + Inv(source.AirJumpSpeed * jump)
                + ",\"_runSpeed\":" + Inv(source.RunSpeed * run) + "}", scaled);
            return scaled;
        }

        private static string Inv(float value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>Every segment reachable from <paramref name="from"/>, itself included.</summary>
        public HashSet<int> Forward(int from) => Search(from, true);

        /// <summary>Every segment from which <paramref name="to"/> can be reached.</summary>
        public HashSet<int> Backward(int to) => Search(to, false);

        public bool HasMove(int from, int to) => _edges[from].Contains(to);

        /// <summary>The floor under a cell: the segment on its row, or the first below it.</summary>
        public int SegmentUnder(Vector2Int cell)
        {
            for (var row = cell.y; row >= cell.y - 8; row--)
            {
                if (!_byRow.TryGetValue(row, out var list))
                {
                    continue;
                }

                foreach (var segment in list)
                {
                    if (segment.Holds(cell.x) || segment.Holds(cell.x + 1))
                    {
                        return segment.Index;
                    }
                }
            }

            return -1;
        }

        public HashSet<string> Rooms(IEnumerable<int> segments)
        {
            var rooms = new HashSet<string>();
            foreach (var index in segments)
            {
                if (_segments[index].RoomId != null)
                {
                    rooms.Add(_segments[index].RoomId);
                }
            }

            return rooms;
        }

        // ---------- the grid ----------

        private void Rasterise()
        {
            if (_zone.Rooms.Count == 0)
            {
                _grid = new RectInt(0, 0, 1, 1);
                _cells = new LevelCell[1, 1];
                return;
            }

            var bounds = _zone.Rooms[0].Bounds;
            foreach (var room in _zone.Rooms)
            {
                bounds.xMin = Mathf.Min(bounds.xMin, room.Bounds.xMin);
                bounds.yMin = Mathf.Min(bounds.yMin, room.Bounds.yMin);
                bounds.xMax = Mathf.Max(bounds.xMax, room.Bounds.xMax);
                bounds.yMax = Mathf.Max(bounds.yMax, room.Bounds.yMax);
            }

            _grid = bounds;
            _cells = new LevelCell[bounds.height, bounds.width];
            for (var y = 0; y < bounds.height; y++)
            {
                for (var x = 0; x < bounds.width; x++)
                {
                    _cells[y, x] = _zone.At(new Vector2Int(bounds.x + x, bounds.y + y));
                }
            }
        }

        private LevelCell Cell(int x, int y)
        {
            x -= _grid.x;
            y -= _grid.y;
            return x < 0 || y < 0 || x >= _grid.width || y >= _grid.height ? LevelCell.Solid : _cells[y, x];
        }

        private static bool IsSolid(LevelCell cell) =>
            cell == LevelCell.Solid || cell == LevelCell.Smooth || cell == LevelCell.Ledge;

        /// <summary>Cells under a world box, shrunk a hair so touching is not overlapping.</summary>
        private bool Overlaps(Vector2 min, Vector2 max, bool spikes)
        {
            const float Eps = 0.001f;
            var size = _zone.CellSize;
            var x0 = Mathf.FloorToInt((min.x + Eps - _zone.Origin.x) / size);
            var x1 = Mathf.FloorToInt((max.x - Eps - _zone.Origin.x) / size);
            var y0 = Mathf.FloorToInt((min.y + Eps - _zone.Origin.y) / size);
            var y1 = Mathf.FloorToInt((max.y - Eps - _zone.Origin.y) / size);
            for (var y = y0; y <= y1; y++)
            {
                for (var x = x0; x <= x1; x++)
                {
                    var cell = Cell(x, y);
                    if (spikes ? cell == LevelCell.Spikes : IsSolid(cell))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>The body, by its feet (bottom centre).</summary>
        private bool Blocked(Vector2 feet) =>
            Overlaps(new Vector2(feet.x - BodyWidth * 0.5f, feet.y),
                new Vector2(feet.x + BodyWidth * 0.5f, feet.y + BodyHeight), false);

        private bool OnSpikes(Vector2 feet) =>
            Overlaps(new Vector2(feet.x - BodyWidth * 0.4f, feet.y),
                new Vector2(feet.x + BodyWidth * 0.4f, feet.y + BodyHeight), true);

        /// <summary>
        /// Ground under the middle of the feet only: the capsule's bottom is round, and on a corner
        /// further out than this it slides off rather than stands.
        /// </summary>
        private bool Supported(Vector2 feet) =>
            Overlaps(new Vector2(feet.x - SupportHalfWidth, feet.y - GroundProbe),
                new Vector2(feet.x + SupportHalfWidth, feet.y), false);

        private Vector2 Feet(int x, int row) => _zone.CellCorner(new Vector2Int(x, row));

        // ---------- floors ----------

        private void FindSegments()
        {
            for (var row = _grid.yMin; row < _grid.yMax; row++)
            {
                ReachSegment open = null;
                for (var x = _grid.xMin; x <= _grid.xMax; x++)
                {
                    var feet = Feet(x, row);
                    var stands = x < _grid.xMax && !Blocked(feet) && Supported(feet) && !OnSpikes(feet)
                        && _zone.RoomAt(new Vector2Int(x, row)) != null;
                    if (stands)
                    {
                        // A floor running on into the next room is two floors: each room is reached on its own.
                        var room = _zone.RoomAt(new Vector2Int(x, row)).Id;
                        if (open != null && open.RoomId != room)
                        {
                            var previous = open;
                            open = NewSegment(row, x, room);
                            _edges[previous.Index].Add(open.Index);
                            _edges[open.Index].Add(previous.Index);
                        }

                        if (open == null)
                        {
                            open = NewSegment(row, x, room);
                        }

                        open.XMax = x;
                    }
                    else
                    {
                        open = null;
                    }
                }
            }
        }

        private ReachSegment NewSegment(int row, int x, string roomId)
        {
            var segment = new ReachSegment { Index = _segments.Count, Row = row, XMin = x, XMax = x, RoomId = roomId };
            _segments.Add(segment);
            _edges.Add(new HashSet<int>());
            if (!_byRow.TryGetValue(row, out var list))
            {
                _byRow[row] = list = new List<ReachSegment>();
            }

            list.Add(segment);
            return segment;
        }

        private void AddRings()
        {
            if (!_grapple)
            {
                return;
            }

            foreach (var marker in _zone.Markers())
            {
                if (marker.Kind != "ring")
                {
                    continue;
                }

                var ring = new ReachSegment
                {
                    Index = _segments.Count, Row = int.MinValue, IsRing = true,
                    RingPoint = _zone.CellCentre(marker.Cell), RoomId = marker.RoomId
                };
                _segments.Add(ring);
                _edges.Add(new HashSet<int>());
            }
        }

        // ---------- moves ----------

        private void Moves(ReachSegment from)
        {
            if (from.IsRing)
            {
                var release = from.RingPoint - new Vector2(0f, RingRelease + BodyHeight * 0.5f);
                foreach (var plan in Plans(+1, fromRing: true))
                {
                    Land(from, Fly(release, new Vector2(0f, RingPop), plan, airborne: true));
                }

                foreach (var plan in Plans(-1, fromRing: true))
                {
                    Land(from, Fly(release, new Vector2(0f, RingPop), plan, airborne: true));
                }

                return;
            }

            // Along the floor, both ways.
            foreach (var x in Starts(from))
            {
                for (var d = -1; d <= 1; d += 2)
                {
                    var feet = Feet(x, from.Row);
                    foreach (var plan in Plans(d, fromRing: false))
                    {
                        if (plan.WalkOff && x != (d > 0 ? from.XMax : from.XMin))
                        {
                            continue;
                        }

                        var start = plan.WalkOff || plan.Running ? new Vector2(d * _move.RunSpeed, 0f) : Vector2.zero;
                        Land(from, Fly(feet, start, plan, airborne: false));
                    }
                }

                if (_grapple)
                {
                    Grapple(from, x);
                }
            }
        }

        private IEnumerable<int> Starts(ReachSegment segment)
        {
            yield return segment.XMin;
            if (segment.XMax != segment.XMin)
            {
                yield return segment.XMax;
            }

            for (var x = segment.XMin + 3; x < segment.XMax - 1; x += 3)
            {
                yield return x;
            }
        }

        private void Grapple(ReachSegment from, int x)
        {
            var hand = Feet(x, from.Row) + new Vector2(0f, BodyHeight * 0.75f);
            for (var i = 0; i < _segments.Count; i++)
            {
                var ring = _segments[i];
                if (ring.IsRing && Vector2.Distance(hand, ring.RingPoint) <= RingRange && Clear(hand, ring.RingPoint))
                {
                    _edges[from.Index].Add(i);
                }
            }
        }

        private bool Clear(Vector2 a, Vector2 b)
        {
            var steps = Mathf.CeilToInt(Vector2.Distance(a, b) / (_zone.CellSize * 0.5f));
            for (var i = 1; i < steps; i++)
            {
                var p = Vector2.Lerp(a, b, i / (float)steps);
                if (IsSolid(Cell(_zone.CellOf(p).x, _zone.CellOf(p).y)))
                {
                    return false;
                }
            }

            return true;
        }

        private void Land(ReachSegment from, Vector2? landing)
        {
            if (landing == null)
            {
                return;
            }

            var feet = landing.Value;
            var x = Mathf.RoundToInt((feet.x - _zone.Origin.x) / _zone.CellSize);
            var row = Mathf.RoundToInt((feet.y - _zone.Origin.y) / _zone.CellSize);
            if (!_byRow.TryGetValue(row, out var list))
            {
                return;
            }

            foreach (var segment in list)
            {
                if (segment.Index != from.Index && x >= segment.XMin - 1 && x <= segment.XMax + 1)
                {
                    _edges[from.Index].Add(segment.Index);
                    return;
                }
            }
        }

        /// <summary>
        /// One way of moving through the air: which way the stick is held before and after the top
        /// of the arc, and what is spent there.
        /// </summary>
        private struct Plan
        {
            public bool WalkOff;
            public bool Jump;
            public bool Running;
            public bool ShortHop;
            public float Before;
            public float After;
            public bool SecondJump;
            public bool AirDash;
            public int Direction;
        }

        private IEnumerable<Plan> Plans(int d, bool fromRing)
        {
            if (!fromRing)
            {
                yield return new Plan { WalkOff = true, Before = d, After = d, Direction = d };
                yield return new Plan { WalkOff = true, Before = d, After = 0f, Direction = d };
                yield return new Plan { WalkOff = true, Before = d, After = -d, Direction = d };
            }

            var starts = fromRing ? new[] { false } : new[] { true, false };
            foreach (var running in starts)
            {
                var jump = !fromRing;
                yield return new Plan { Jump = jump, Running = running, Before = d, After = d, Direction = d };
                yield return new Plan { Jump = jump, Running = running, Before = 0f, After = d, Direction = d };
                yield return new Plan { Jump = jump, Running = running, Before = d, After = 0f, Direction = d };
                if (!fromRing)
                {
                    yield return new Plan { Jump = true, Running = running, ShortHop = true, Before = d, After = d, Direction = d };
                }

                if (_doubleJump)
                {
                    yield return new Plan { Jump = jump, Running = running, Before = d, After = d, SecondJump = true, Direction = d };
                    yield return new Plan { Jump = jump, Running = running, Before = 0f, After = d, SecondJump = true, Direction = d };
                    yield return new Plan { Jump = jump, Running = running, Before = d, After = 0f, SecondJump = true, Direction = d };
                }

                if (_airDash)
                {
                    yield return new Plan { Jump = jump, Running = running, Before = d, After = d, AirDash = true, Direction = d };
                    if (_doubleJump)
                    {
                        yield return new Plan { Jump = jump, Running = running, Before = d, After = d, SecondJump = true, AirDash = true, Direction = d };
                    }
                }
            }
        }

        /// <summary>Steps one move to its landing; null if it ends on spikes, in the void or never lands.</summary>
        private Vector2? Fly(Vector2 feet, Vector2 velocity, Plan plan, bool airborne)
        {
            var logic = new PlayerMovementLogic(_move, _dash) { AirJumpsAllowed = _doubleJump ? 1 : 0 };
            if (Blocked(feet))
            {
                return null;
            }

            var leftGround = airborne;
            var pastApex = false;
            var secondDone = false;
            var dashDone = false;
            var secondApex = false;
            for (var step = 0; step < MaxSteps; step++)
            {
                var ground = Supported(feet) && velocity.y <= 0.01f;
                if (!ground)
                {
                    leftGround = true;
                }

                if (leftGround && !pastApex && velocity.y <= 0f)
                {
                    pastApex = true;
                }

                var press = plan.Jump && step == 0;
                if (plan.SecondJump && pastApex && !secondDone)
                {
                    press = true;
                    secondDone = true;
                }
                else if (secondDone && !secondApex && velocity.y <= 0f && step > 0)
                {
                    secondApex = true;
                }

                var dashTime = plan.SecondJump ? secondApex : pastApex;
                if (plan.AirDash && dashTime && !dashDone && !ground)
                {
                    dashDone = logic.TryBeginDash(plan.Direction);
                }

                var held = (plan.Jump || plan.SecondJump || airborne) && !(plan.ShortHop && step >= 2);
                var horizontal = pastApex ? plan.After : plan.Before;
                velocity = logic.Step(velocity, ground, new MovementInput(horizontal, press, held), Dt);
                feet = Move(feet, ref velocity);
                if (OnSpikes(feet) || feet.y < _zone.CellCorner(new Vector2Int(0, _grid.yMin)).y)
                {
                    return null;
                }

                if (leftGround && step > 0 && velocity.y <= 0f && Supported(feet))
                {
                    return feet;
                }

                if (!leftGround && step > 15)
                {
                    return null;
                }
            }

            return null;
        }

        private Vector2 Move(Vector2 feet, ref Vector2 velocity)
        {
            feet = Axis(feet, ref velocity.x, Vector2.right);
            feet = Axis(feet, ref velocity.y, Vector2.up);
            return feet;
        }

        private Vector2 Axis(Vector2 feet, ref float speed, Vector2 axis)
        {
            var delta = speed * Dt;
            if (Mathf.Abs(delta) < 1e-6f)
            {
                return feet;
            }

            var count = Mathf.CeilToInt(Mathf.Abs(delta) / SubStep);
            var piece = axis * (delta / count);
            for (var i = 0; i < count; i++)
            {
                var next = feet + piece;
                if (!Blocked(next))
                {
                    feet = next;
                    continue;
                }

                // Close the gap to the wall or floor by halving, so standing reads as standing.
                var low = 0f;
                var high = 1f;
                for (var k = 0; k < 6; k++)
                {
                    var mid = (low + high) * 0.5f;
                    if (Blocked(feet + piece * mid)) { high = mid; } else { low = mid; }
                }

                feet += piece * low;
                speed = 0f;
                break;
            }

            return feet;
        }

        // ---------- search ----------

        private HashSet<int> Search(int start, bool forward)
        {
            var seen = new HashSet<int>();
            if (start < 0 || start >= _segments.Count)
            {
                return seen;
            }

            List<int>[] reverse = null;
            if (!forward)
            {
                reverse = new List<int>[_segments.Count];
                for (var a = 0; a < _segments.Count; a++)
                {
                    foreach (var b in _edges[a])
                    {
                        (reverse[b] ??= new List<int>()).Add(a);
                    }
                }
            }

            var queue = new Queue<int>();
            queue.Enqueue(start);
            seen.Add(start);
            while (queue.Count > 0)
            {
                var at = queue.Dequeue();
                IEnumerable<int> next = forward ? _edges[at] : (IEnumerable<int>)reverse[at] ?? System.Array.Empty<int>();
                foreach (var to in next)
                {
                    if (seen.Add(to))
                    {
                        queue.Enqueue(to);
                    }
                }
            }

            return seen;
        }
    }
}
