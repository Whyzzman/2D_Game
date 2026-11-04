using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The level tool's builder (item 1 of the 15-zone list): a zone written as text
    /// (<see cref="LevelZone"/>, `Assets/Game/Levels/*.txt`) becomes blocks, things and graph data.
    ///
    /// The zone's own builder says how a block looks (its tiles); this file does everything that
    /// is the same in every zone: merging cells into blocks, spikes, altars, finds, rings, enemies,
    /// the discovery triggers of secret rooms, and the areas, connections and altars of the graph,
    /// read from the same text the geometry came from — so the map cannot disagree with the level.
    /// Format and rules — `gamedesign/LEVEL_TOOL.md`.
    /// </summary>
    public static partial class ContentBuilder
    {
        public const string LevelFolder = "Assets/Game/Levels";

        /// <summary>How one zone dresses a block of its level.</summary>
        public delegate GameObject LevelBlockMaker(Transform parent, string name, Rect world, LevelCell kind);

        /// <summary>The scene objects a layout's things hook up to.</summary>
        public sealed class LevelContext
        {
            public GameObject Checkpoint;
            public GameObject Pickup;
            public PlayerRespawner Respawner;
            public GameProgress Progress;
            public GameObject Player;

            public static LevelContext FromScene() => new LevelContext
            {
                Checkpoint = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/Checkpoint.prefab"),
                Pickup = AssetDatabase.LoadAssetAtPath<GameObject>(PickupPrefabPath),
                Respawner = UnityEngine.Object.FindAnyObjectByType<PlayerRespawner>(),
                Progress = UnityEngine.Object.FindAnyObjectByType<GameProgress>(),
                Player = UnityEngine.Object.FindAnyObjectByType<PlayerCharacter>()?.gameObject
            };
        }

        /// <summary>
        /// What a block of a written zone is made of, for the feet (GAME_FEEL_IDEAS item 4): read from
        /// the room's zone and the cell, as the zones' own builders already choose its tiles. Ledges
        /// are planks where the zone builds with timber; the rest is the zone's ground.
        /// </summary>
        public static SurfaceKind SurfaceFor(string roomId, LevelCell kind)
        {
            var id = roomId ?? string.Empty;
            var ledge = kind == LevelCell.Ledge;
            if (id.StartsWith("room_forest") || id.StartsWith("room_marsh"))
            {
                return ledge ? SurfaceKind.Wood : SurfaceKind.Earth;
            }

            if (id.StartsWith("room_village"))
            {
                return ledge ? SurfaceKind.Wood
                    : VillageStoneRooms.Contains(id) ? SurfaceKind.Stone : SurfaceKind.Earth;
            }

            if (id.StartsWith("room_capital"))
            {
                // The Capital's ledges are carved stone; its streets are deep in ash.
                return ledge ? SurfaceKind.Stone : SurfaceKind.Ash;
            }

            return SurfaceKind.Stone;
        }

        public static LevelZone LoadLayout(string fileName)
        {
            var path = Path.Combine(LevelFolder, fileName);
            return LevelZone.Parse(File.Exists(path) ? File.ReadAllText(path) : string.Empty);
        }

        /// <summary>
        /// Builds the zone under <paramref name="parent"/>, one child per room. Returns what could
        /// not be built (an unknown thing, a missing prefab); an empty list means all of it was.
        /// </summary>
        /// <param name="paintedSpikes">The zone's painter draws its spikes (a painted zone): no placeholder teeth.</param>
        public static List<string> BuildLayout(LevelZone zone, Transform parent, LevelBlockMaker block,
            LevelContext context, bool paintedSpikes = false)
        {
            var problems = new List<string>(zone.Errors);
            // Loaded fresh for each zone: asset references taken before a new scene are dead.
            _breakables = null;
            var rooms = new Dictionary<string, Transform>();
            foreach (var room in zone.Rooms)
            {
                rooms[room.Id] = Section(parent, room.Id);
                if (room.IsSecret)
                {
                    // Found by going in, not by brushing its edge: the trigger is the room less its walls.
                    var inner = zone.WorldBounds(room);
                    var wall = zone.CellSize;
                    AreaTrigger(rooms[room.Id], room.Id, inner.center,
                        new Vector2(Mathf.Max(wall, inner.width - wall * 4f), Mathf.Max(wall, inner.height - wall * 4f)));
                }
            }

            foreach (var piece in LevelSolids.Blocks(zone))
            {
                var world = LevelSolids.World(zone, piece);
                var name = $"{piece.Kind}_{piece.Cells.x}_{piece.Cells.y}";
                if (piece.Kind == LevelCell.Spikes)
                {
                    LayoutSpikes(rooms[piece.RoomId], name, world, zone.CellSize, paintedSpikes);
                    continue;
                }

                var made = block(rooms[piece.RoomId], name, world, piece.Kind);
                if (made != null && piece.Kind == LevelCell.Smooth)
                {
                    Ensure<NonClimbableSurface>(made);
                }

                if (made != null && piece.Kind == LevelCell.Ledge)
                {
                    // A jump from under a ledge lands on top of it (SceneOneWayLedges).
                    Ensure<OneWayPlatform>(made);
                }

                if (made != null)
                {
                    Ensure<GroundSurface>(made).Set(SurfaceFor(piece.RoomId, piece.Kind));
                }
            }

            foreach (var marker in zone.Markers())
            {
                var problem = marker.Kind == "breakable"
                    ? LayoutBreakableWall(zone, marker, rooms[marker.RoomId], block, context)
                    : PlaceMarker(zone, marker, rooms[marker.RoomId], context);
                if (problem != null)
                {
                    problems.Add($"room {marker.RoomId}, «{marker.Letter}»: {problem}");
                }
            }

            return problems;
        }

        /// <summary>Spikes fill the bottom of their cells: a row of them reads as a row of teeth.</summary>
        private static void LayoutSpikes(Transform parent, string name, Rect world, float cell, bool painted)
        {
            for (var row = 0; row < Mathf.RoundToInt(world.height / cell); row++)
            {
                var spikes = new GameObject(row == 0 ? name : $"{name}_{row}");
                spikes.transform.SetParent(parent);
                spikes.transform.position = new Vector2(world.center.x, world.yMin + row * cell + cell * 0.4f);
                var component = spikes.AddComponent<Spikes>();
                SetVector(component, "_size", new Vector2(world.width, cell * 0.8f));
                SetBool(component, "_painted", painted);
            }
        }

        /// <summary>Where a lettered thing stands: the middle of its cell, on the cell's floor.</summary>
        public static Vector2 MarkerFeet(LevelZone zone, LevelMarker marker) =>
            zone.CellCorner(marker.Cell) + new Vector2(zone.CellSize * 0.5f, 0f);

        /// <summary>
        /// A named point of the zone for the zone's own builder: a <c>spot name</c>, or the first
        /// thing of that kind (<c>start</c>).
        /// </summary>
        public static Vector2 SpotFeet(LevelZone zone, string name)
        {
            foreach (var marker in zone.Markers())
            {
                if ((marker.Kind == "spot" && marker.Arg(0) == name) || marker.Kind == name)
                {
                    return MarkerFeet(zone, marker);
                }
            }

            Debug.LogError($"Layout {zone.ZoneId} has no spot «{name}».");
            return Vector2.zero;
        }

        /// <summary>
        /// «breakable id height»: the secret room's wall, two cells wide from the marker to its
        /// right and <c>height</c> cells up from its floor, built in the zone's stone and made to
        /// break (<see cref="BreakableWall"/>), with a veil over the room until it does.
        /// </summary>
        private static string LayoutBreakableWall(LevelZone zone, LevelMarker marker, Transform parent,
            LevelBlockMaker block, LevelContext context)
        {
            if (!int.TryParse(marker.Arg(1), out var cells) || cells < 4)
            {
                return "a breakable wall needs a height of at least four cells";
            }

            var corner = zone.CellCorner(marker.Cell);
            var world = new Rect(corner, new Vector2(zone.CellSize * 2f, zone.CellSize * cells));
            var made = block(parent, "BreakableWall", world, LevelCell.Solid);
            if (made == null)
            {
                return "the zone made no block for the wall";
            }

            Ensure<NonClimbableSurface>(made);
            var room = zone.Room(marker.RoomId);
            var inside = zone.WorldBounds(room);
            var shell = zone.CellSize * 2f;
            inside = Rect.MinMaxRect(inside.xMin + shell, inside.yMin + shell, inside.xMax - shell, inside.yMax - shell);
            BuildBreakableWall(made, marker.Arg(0), inside, context.Progress, parent);
            return null;
        }

        private static string PlaceMarker(LevelZone zone, LevelMarker marker, Transform parent, LevelContext context)
        {
            var feet = MarkerFeet(zone, marker);
            switch (marker.Kind)
            {
                case "start":
                case "spot":
                    return null;
                case "shard":
                    if (context.Pickup == null)
                    {
                        return "no pickup prefab";
                    }

                    var kind = marker.Arg(0) == "mana" ? VesselKind.Mana : VesselKind.Health;
                    var shardId = ExplorationIdPrefix + (kind == VesselKind.Health ? "health-" : "mana-") + marker.Arg(1);
                    PlacePickup(context.Pickup, feet + new Vector2(0f, 0.7f), context.Progress,
                        VesselShards.Prefix(kind) + marker.Arg(1), false, 1, shardId, false);
                    Adopt(parent, c => c.GetComponent<WorldPickup>()?.PersistentId == shardId);
                    return null;
                case "reward":
                    PlaceTraversalReward(parent, feet + new Vector2(0f, 1f), marker.Arg(0), marker.Arg(1));
                    return null;
                case "altar":
                    if (context.Checkpoint == null || context.Player == null)
                    {
                        return "no altar prefab or player in the scene";
                    }

                    PlaceCheckpoint(context.Checkpoint, feet + new Vector2(0f, 0.8f), marker.Arg(0), marker.Arg(1),
                        context.Respawner, context.Progress, context.Player);
                    Adopt(parent, c => c.GetComponent<Checkpoint>()?.CheckpointId == marker.Arg(0));
                    return null;
                case "gold":
                case "item":
                    if (context.Pickup == null)
                    {
                        return "no pickup prefab";
                    }

                    var gold = marker.Kind == "gold";
                    PlacePickup(context.Pickup, feet + new Vector2(0f, 0.6f), context.Progress,
                        gold ? string.Empty : marker.Arg(0), gold, gold ? int.Parse(marker.Arg(0)) : 1,
                        marker.Arg(1), gold);
                    Adopt(parent, c => c.GetComponent<WorldPickup>()?.PersistentId == marker.Arg(1));
                    return null;
                case "urn":
                    return PlaceBreakable(parent, marker, feet);
                case "ring":
                    PlaceGrappleRing(parent, $"Ring_{marker.Cell.x}_{marker.Cell.y}", zone.CellCentre(marker.Cell));
                    return null;
                case "enemy":
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/{marker.Arg(0)}.prefab");
                    if (prefab == null)
                    {
                        return $"no enemy prefab «{marker.Arg(0)}»";
                    }

                    SpawnEnemy(prefab, feet + new Vector2(0f, 0.9f)).transform.SetParent(parent, true);
                    return null;
                default:
                    return $"unknown thing «{marker.Kind}»";
            }
        }

        /// <summary>The helpers place at the scene root; the newest match moves into its room.</summary>
        private static void Adopt(Transform parent, Func<GameObject, bool> match)
        {
            foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (match(root))
                {
                    root.transform.SetParent(parent, true);
                }
            }
        }

        // ---------- the graph ----------

        /// <summary>The zone's areas, connections and altars as the graph's JSON entries.</summary>
        public static (List<string> areas, List<string> links, List<string> altars) LayoutGraphJson(LevelZone zone)
        {
            var areas = new List<string>();
            foreach (var room in zone.Rooms)
            {
                var bounds = zone.WorldBounds(room);
                var icon = !string.IsNullOrEmpty(room.BossId) ? MapIconType.Boss
                    : room.IsSecret ? MapIconType.Secret : MapIconType.None;
                areas.Add(AreaJson(room.Id, zone.ZoneId, room.Name, bounds.center * MapScale,
                    new Vector2(Mathf.Max(0.3f, bounds.width * MapScale - 0.1f), Mathf.Max(0.3f, bounds.height * MapScale - 0.1f)),
                    bounds, secret: room.IsSecret, optional: room.IsOptional, trigger: room.IsSecret,
                    ability: room.NeedsAbility, icon: icon, boss: room.BossId));
            }

            var links = new List<string>();
            foreach (var link in zone.Links)
            {
                links.Add(LinkJson(LinkId(link), link.From, link.To, link.Direction, link.Type, !link.OneWay,
                    link.Ability, link.Boss, link.Shortcut, link.OpenedFrom));
            }

            var altars = new List<string>();
            foreach (var marker in zone.Markers())
            {
                if (marker.Kind == "altar")
                {
                    altars.Add(AltarJson(marker.Arg(0), marker.Arg(1), zone.ZoneId, marker.RoomId));
                }
            }

            return (areas, links, altars);
        }

        /// <summary>«conn_tomb_entrance_guard_passage»: the zone's word once, as the graph's older ids have it.</summary>
        public static string LinkId(LevelLink link)
        {
            var from = Short(link.From);
            var to = Short(link.To);
            var zone = from.Split('_')[0] + "_";
            return "conn_" + from + "_" + (to.StartsWith(zone) ? to.Substring(zone.Length) : to);
        }

        private static string Short(string roomId) => roomId.StartsWith("room_") ? roomId.Substring(5) : roomId;
    }
}
