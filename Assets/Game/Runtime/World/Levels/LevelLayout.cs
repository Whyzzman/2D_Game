using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>What a cell of a room is made of.</summary>
    public enum LevelCell
    {
        Empty,
        /// <summary>Stone, wood, ice: whatever the zone builds with. <c>#</c></summary>
        Solid,
        /// <summary>Solid, but smooth: no wall slide, run or jump. <c>%</c></summary>
        Smooth,
        /// <summary>Solid, drawn as a thin ledge or plank. <c>=</c></summary>
        Ledge,
        /// <summary>Spikes: a hazard, not ground. <c>^</c></summary>
        Spikes
    }

    /// <summary>A lettered thing in a room's grid, described by the room's <c>key</c> line.</summary>
    public sealed class LevelMarker
    {
        public char Letter;
        /// <summary>altar, enemy, gold, item, ring, wall, hint, start, npc, boss, trigger…</summary>
        public string Kind;
        /// <summary>The words after the kind, quotes removed.</summary>
        public string[] Args;
        /// <summary>Cell in zone coordinates (x right, y up).</summary>
        public Vector2Int Cell;
        public string RoomId;

        public string Arg(int index) => Args != null && index < Args.Length ? Args[index] : string.Empty;
    }

    /// <summary>One room: a rectangle of cells with its own name and flags.</summary>
    public sealed class LevelRoom
    {
        public string Id;
        public string Name;
        /// <summary>Bottom-left cell in zone coordinates.</summary>
        public Vector2Int Origin;
        public int Width;
        public int Height;
        public bool IsSecret;
        public bool IsOptional;
        public string BossId = string.Empty;
        /// <summary>For an optional room: the ability its way in is built for.</summary>
        public string NeedsAbility = string.Empty;
        /// <summary>Rows bottom-up: Cells[y, x], room-local.</summary>
        public LevelCell[,] Cells;
        public readonly List<LevelMarker> Markers = new List<LevelMarker>();
        /// <summary>Sides that may open onto another zone: N, S, E, W.</summary>
        public string ZoneExits = string.Empty;

        public RectInt Bounds => new RectInt(Origin.x, Origin.y, Width, Height);

        /// <summary>«flags secret optional boss=id needs=ability exit=NE».</summary>
        internal void ReadFlags(List<string> words)
        {
            for (var i = 1; i < words.Count; i++)
            {
                var word = words[i];
                var value = word.Contains("=") ? word.Substring(word.IndexOf('=') + 1) : string.Empty;
                switch (word.Contains("=") ? word.Substring(0, word.IndexOf('=')) : word)
                {
                    case "secret": IsSecret = true; break;
                    case "optional": IsOptional = true; break;
                    case "boss": BossId = value; break;
                    case "needs": NeedsAbility = value; break;
                    case "exit": ZoneExits = value; break;
                    default: UnknownFlags.Add(word); break;
                }
            }
        }

        public readonly List<string> UnknownFlags = new List<string>();
        public bool Contains(Vector2Int cell) => Bounds.Contains(cell);
        public LevelCell Local(int x, int y) => Cells[y, x];
    }

    /// <summary>How two rooms are joined; the graph's connection, read from the layout.</summary>
    public sealed class LevelLink
    {
        public string From;
        public string To;
        public MapDirection Direction;
        public ConnectionType Type = ConnectionType.Normal;
        public bool OneWay;
        public string Ability = string.Empty;
        public string Boss = string.Empty;
        public string Shortcut = string.Empty;
        public string OpenedFrom = string.Empty;
        /// <summary>True when the rooms really share an opening (false: a link line only).</summary>
        public bool HasOpening;
    }

    /// <summary>
    /// A zone written as text (the level tool of the 15-zone plan, `WORLD_DESIGN.md`). One file per
    /// zone: a few header lines, then rooms, each a grid of characters with its own key, then the
    /// links that are more than an opening. The grid is the level: the builder turns it into blocks,
    /// the graph into areas and connections, the reach check proves it is walkable. Format —
    /// `gamedesign/LEVEL_TOOL.md`.
    /// </summary>
    public sealed class LevelZone
    {
        public string ZoneId = string.Empty;
        public float CellSize = 0.5f;
        /// <summary>World position of the bottom-left corner of cell (0, 0).</summary>
        public Vector2 Origin;
        /// <summary>What every player entering the zone is sure to have: the main path's toolkit.</summary>
        public readonly List<string> Abilities = new List<string>();
        public readonly List<LevelRoom> Rooms = new List<LevelRoom>();
        public readonly List<LevelLink> Links = new List<LevelLink>();
        public readonly List<string> Errors = new List<string>();

        private Dictionary<Vector2Int, LevelRoom> _byCell;

        public bool IsValid => Errors.Count == 0;

        public LevelRoom Room(string id) => Rooms.Find(r => r.Id == id);

        public IEnumerable<LevelMarker> Markers()
        {
            foreach (var room in Rooms)
            {
                foreach (var marker in room.Markers)
                {
                    yield return marker;
                }
            }
        }

        /// <summary>The room a zone cell belongs to, or null outside every room.</summary>
        public LevelRoom RoomAt(Vector2Int cell)
        {
            if (_byCell == null)
            {
                _byCell = new Dictionary<Vector2Int, LevelRoom>();
                foreach (var room in Rooms)
                {
                    for (var y = 0; y < room.Height; y++)
                    {
                        for (var x = 0; x < room.Width; x++)
                        {
                            _byCell[new Vector2Int(room.Origin.x + x, room.Origin.y + y)] = room;
                        }
                    }
                }
            }

            return _byCell.TryGetValue(cell, out var found) ? found : null;
        }

        /// <summary>What a zone cell is; outside every room it is solid rock.</summary>
        public LevelCell At(Vector2Int cell)
        {
            var room = RoomAt(cell);
            return room == null ? LevelCell.Solid : room.Local(cell.x - room.Origin.x, cell.y - room.Origin.y);
        }

        public bool IsSolid(Vector2Int cell)
        {
            var kind = At(cell);
            return kind == LevelCell.Solid || kind == LevelCell.Smooth || kind == LevelCell.Ledge;
        }

        public Vector2 CellCorner(Vector2Int cell) => Origin + (Vector2)cell * CellSize;
        public Vector2 CellCentre(Vector2Int cell) => CellCorner(cell) + Vector2.one * (CellSize * 0.5f);

        public Vector2Int CellOf(Vector2 world)
        {
            var local = (world - Origin) / CellSize;
            return new Vector2Int(Mathf.FloorToInt(local.x), Mathf.FloorToInt(local.y));
        }

        /// <summary>A room's rectangle in world units.</summary>
        public Rect WorldBounds(LevelRoom room) =>
            new Rect(CellCorner(room.Origin), new Vector2(room.Width, room.Height) * CellSize);

        // ---------- parsing ----------

        public static LevelZone Parse(string text)
        {
            var zone = new LevelZone();
            var lines = (text ?? string.Empty).Replace("\r", string.Empty).Split('\n');
            LevelRoom room = null;
            var keys = new Dictionary<char, string[]>();
            List<string> grid = null;
            var gridStart = 0;
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var number = i + 1;
                if (grid != null)
                {
                    if (line.Trim() == "end")
                    {
                        zone.FillRoom(room, grid, keys, gridStart);
                        grid = null;
                        continue;
                    }

                    grid.Add(line.TrimEnd());
                    continue;
                }

                var comment = line.IndexOf('#');
                // '#' starts a comment only at the start of a line or after a space: in a grid it is stone.
                if (comment == 0 || (comment > 0 && line[comment - 1] == ' '))
                {
                    line = line.Substring(0, comment);
                }

                var words = Words(line);
                if (words.Count == 0)
                {
                    continue;
                }

                try
                {
                    switch (words[0])
                    {
                        case "zone":
                            zone.ZoneId = words[1];
                            break;
                        case "cell":
                            zone.CellSize = Number(words[1]);
                            break;
                        case "origin":
                            zone.Origin = new Vector2(Number(words[1]), Number(words[2]));
                            break;
                        case "abilities":
                            zone.Abilities.AddRange(words.GetRange(1, words.Count - 1));
                            break;
                        case "room":
                            room = new LevelRoom
                            {
                                Id = words[1],
                                Name = words.Count > 2 && words[2] != "at" ? words[2] : words[1]
                            };
                            var at = words.IndexOf("at");
                            if (at < 0 || at + 2 >= words.Count)
                            {
                                zone.Errors.Add($"line {number}: room {room.Id} has no «at x y»");
                            }
                            else
                            {
                                room.Origin = new Vector2Int((int)Number(words[at + 1]), (int)Number(words[at + 2]));
                            }

                            keys = new Dictionary<char, string[]>();
                            zone.Rooms.Add(room);
                            break;
                        case "flags":
                            RequireRoom(zone, room, number);
                            room?.ReadFlags(words);
                            break;
                        case "key":
                            RequireRoom(zone, room, number);
                            if (words[1].Length != 1 || !char.IsLower(words[1][0]))
                            {
                                zone.Errors.Add($"line {number}: a key is one lowercase letter, not «{words[1]}»");
                                break;
                            }

                            keys[words[1][0]] = words.GetRange(2, words.Count - 2).ToArray();
                            break;
                        case "grid":
                            RequireRoom(zone, room, number);
                            grid = new List<string>();
                            gridStart = number + 1;
                            break;
                        case "link":
                            zone.Links.Add(ReadLink(words, number, zone));
                            break;
                        default:
                            zone.Errors.Add($"line {number}: unknown «{words[0]}»");
                            break;
                    }
                }
                catch (ArgumentOutOfRangeException)
                {
                    zone.Errors.Add($"line {number}: «{words[0]}» is missing a value");
                }
            }

            if (grid != null)
            {
                zone.Errors.Add($"room {room?.Id}: grid has no «end»");
            }

            zone.Check();
            zone.FindOpenings();
            return zone;
        }

        private static void RequireRoom(LevelZone zone, LevelRoom room, int number)
        {
            if (room == null)
            {
                zone.Errors.Add($"line {number}: before any room");
            }
        }

        private void FillRoom(LevelRoom room, List<string> rows, Dictionary<char, string[]> keys, int firstLine)
        {
            if (room == null)
            {
                return;
            }

            while (rows.Count > 0 && rows[rows.Count - 1].Length == 0)
            {
                rows.RemoveAt(rows.Count - 1);
            }

            room.Height = rows.Count;
            room.Width = 0;
            foreach (var row in rows)
            {
                room.Width = Mathf.Max(room.Width, row.Length);
            }

            for (var r = 0; r < rows.Count; r++)
            {
                if (rows[r].Length != room.Width)
                {
                    Errors.Add($"room {room.Id}, line {firstLine + r}: row is {rows[r].Length} wide, the room {room.Width}");
                }
            }

            room.Cells = new LevelCell[room.Height, room.Width];
            var used = new HashSet<char>();
            for (var r = 0; r < rows.Count; r++)
            {
                var y = room.Height - 1 - r;
                for (var x = 0; x < room.Width; x++)
                {
                    var c = x < rows[r].Length ? rows[r][x] : '.';
                    switch (c)
                    {
                        case '#': room.Cells[y, x] = LevelCell.Solid; break;
                        case '%': room.Cells[y, x] = LevelCell.Smooth; break;
                        case '=': room.Cells[y, x] = LevelCell.Ledge; break;
                        case '^': room.Cells[y, x] = LevelCell.Spikes; break;
                        case '.':
                        case ' ': room.Cells[y, x] = LevelCell.Empty; break;
                        default:
                            room.Cells[y, x] = LevelCell.Empty;
                            if (char.IsLower(c) && keys.TryGetValue(c, out var args) && args.Length > 0)
                            {
                                used.Add(c);
                                room.Markers.Add(new LevelMarker
                                {
                                    Letter = c,
                                    Kind = args[0],
                                    Args = args.Length > 1 ? SubArray(args, 1) : Array.Empty<string>(),
                                    Cell = new Vector2Int(room.Origin.x + x, room.Origin.y + y),
                                    RoomId = room.Id
                                });
                            }
                            else
                            {
                                Errors.Add($"room {room.Id}, line {firstLine + r}: «{c}» is neither a cell nor a key");
                            }

                            break;
                    }
                }
            }

            foreach (var key in keys.Keys)
            {
                if (!used.Contains(key))
                {
                    Errors.Add($"room {room.Id}: key «{key}» is never placed");
                }
            }
        }

        private static string[] SubArray(string[] source, int from)
        {
            var result = new string[source.Length - from];
            Array.Copy(source, from, result, 0, result.Length);
            return result;
        }

        private static LevelLink ReadLink(List<string> words, int number, LevelZone zone)
        {
            var link = new LevelLink { From = words[1], To = words[2] };
            for (var i = 3; i < words.Count; i++)
            {
                var word = words[i];
                var value = word.Contains("=") ? word.Substring(word.IndexOf('=') + 1) : string.Empty;
                var name = word.Contains("=") ? word.Substring(0, word.IndexOf('=')) : word;
                switch (name)
                {
                    case "oneway": link.OneWay = true; link.Type = ConnectionType.Drop; break;
                    case "door": link.Type = ConnectionType.Door; break;
                    case "ability": link.Ability = value; link.Type = ConnectionType.AbilityGate; break;
                    case "boss": link.Boss = value; link.Type = ConnectionType.BossGate; break;
                    case "shortcut": link.Shortcut = value; link.Type = ConnectionType.Shortcut; break;
                    case "from": link.OpenedFrom = value; break;
                    case "zone": link.Type = ConnectionType.ZoneTransition; break;
                    default: zone.Errors.Add($"line {number}: unknown link word «{word}»"); break;
                }
            }

            return link;
        }

        // ---------- checks ----------

        private void Check()
        {
            if (string.IsNullOrEmpty(ZoneId))
            {
                Errors.Add("no «zone» line");
            }

            if (CellSize <= 0f)
            {
                Errors.Add("cell size must be above zero");
            }

            var ids = new HashSet<string>();
            foreach (var room in Rooms)
            {
                if (!ids.Add(room.Id))
                {
                    Errors.Add($"room {room.Id} is defined twice");
                }

                foreach (var flag in room.UnknownFlags)
                {
                    Errors.Add($"room {room.Id}: unknown flag «{flag}»");
                }

                if (room.Cells == null)
                {
                    Errors.Add($"room {room.Id} has no grid");
                    room.Cells = new LevelCell[0, 0];
                }
            }

            for (var a = 0; a < Rooms.Count; a++)
            {
                for (var b = a + 1; b < Rooms.Count; b++)
                {
                    if (Rooms[a].Bounds.Overlaps(Rooms[b].Bounds))
                    {
                        Errors.Add($"rooms {Rooms[a].Id} and {Rooms[b].Id} overlap");
                    }
                }
            }

            foreach (var link in Links)
            {
                if (Room(link.From) == null || Room(link.To) == null)
                {
                    Errors.Add($"link {link.From} → {link.To}: no such room");
                }
            }
        }

        /// <summary>
        /// Every open cell on a room's edge must face an open cell of the next room; that pair is
        /// a connection. An opening onto rock or onto nothing is an error — the way a forgotten
        /// wall or a misplaced room is caught before anything is built.
        /// </summary>
        private void FindOpenings()
        {
            _byCell = null;
            var found = new Dictionary<string, LevelLink>();
            foreach (var room in Rooms)
            {
                for (var y = 0; y < room.Height; y++)
                {
                    for (var x = 0; x < room.Width; x++)
                    {
                        if (IsSolidKind(room.Local(x, y)))
                        {
                            continue;
                        }

                        Edge(room, x, y, x == room.Width - 1, Vector2Int.right, MapDirection.East, 'E', found);
                        Edge(room, x, y, x == 0, Vector2Int.left, MapDirection.West, 'W', found);
                        Edge(room, x, y, y == room.Height - 1, Vector2Int.up, MapDirection.North, 'N', found);
                        Edge(room, x, y, y == 0, Vector2Int.down, MapDirection.South, 'S', found);
                    }
                }
            }

            foreach (var opening in found.Values)
            {
                var declared = Links.Find(l => (l.From == opening.From && l.To == opening.To)
                    || (l.From == opening.To && l.To == opening.From));
                if (declared != null)
                {
                    declared.HasOpening = true;
                    declared.Direction = declared.From == opening.From ? opening.Direction : Opposite(opening.Direction);
                }
                else
                {
                    Links.Add(opening);
                }
            }
        }

        private void Edge(LevelRoom room, int x, int y, bool onEdge, Vector2Int step, MapDirection direction,
            char side, Dictionary<string, LevelLink> found)
        {
            if (!onEdge)
            {
                return;
            }

            var beyond = new Vector2Int(room.Origin.x + x, room.Origin.y + y) + step;
            var other = RoomAt(beyond);
            if (other == null)
            {
                if (room.ZoneExits.IndexOf(side) < 0)
                {
                    Errors.Add($"room {room.Id}: opening at ({x}, {y}) leads nowhere ({direction})");
                }

                return;
            }

            if (IsSolidKind(At(beyond)))
            {
                Errors.Add($"room {room.Id}: opening at ({x}, {y}) faces the wall of {other.Id}");
                return;
            }

            var first = string.CompareOrdinal(room.Id, other.Id) < 0;
            var key = first ? room.Id + "|" + other.Id : other.Id + "|" + room.Id;
            if (!found.ContainsKey(key))
            {
                found[key] = new LevelLink
                {
                    From = first ? room.Id : other.Id,
                    To = first ? other.Id : room.Id,
                    Direction = first ? direction : Opposite(direction),
                    HasOpening = true
                };
            }
        }

        public static MapDirection Opposite(MapDirection direction)
        {
            switch (direction)
            {
                case MapDirection.East: return MapDirection.West;
                case MapDirection.West: return MapDirection.East;
                case MapDirection.North: return MapDirection.South;
                default: return MapDirection.North;
            }
        }

        private static bool IsSolidKind(LevelCell kind) =>
            kind == LevelCell.Solid || kind == LevelCell.Smooth || kind == LevelCell.Ledge;

        // ---------- words ----------

        /// <summary>Splits on spaces; «quoted text» (or "quoted") stays one word, without the quotes.</summary>
        private static List<string> Words(string line)
        {
            var words = new List<string>();
            var i = 0;
            while (i < line.Length)
            {
                if (char.IsWhiteSpace(line[i]))
                {
                    i++;
                    continue;
                }

                var close = line[i] == '«' ? '»' : line[i] == '"' ? '"' : '\0';
                if (close != '\0')
                {
                    var end = line.IndexOf(close, i + 1);
                    if (end < 0) { end = line.Length; }
                    words.Add(line.Substring(i + 1, end - i - 1));
                    i = end + 1;
                    continue;
                }

                var start = i;
                while (i < line.Length && !char.IsWhiteSpace(line[i]))
                {
                    i++;
                }

                words.Add(line.Substring(start, i - start));
            }

            return words;
        }

        private static float Number(string word) => float.Parse(word, NumberStyles.Float, CultureInfo.InvariantCulture);
    }
}
