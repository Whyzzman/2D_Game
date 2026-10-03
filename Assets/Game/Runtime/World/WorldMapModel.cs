using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>How much of an area the map is allowed to show.</summary>
    public enum MapNodeState
    {
        /// <summary>Not drawn at all.</summary>
        Hidden,

        /// <summary>
        /// A vague silhouette next to a known room: there is something through that exit. No name,
        /// no icons — in particular no boss before the arena has been seen.
        /// </summary>
        Unexplored,

        Discovered,

        /// <summary>
        /// Not visited, but on a map bought from the merchant, or pointed to by a rumour (item
        /// 70): drawn as a pale outline with its name, its altars and the rumour's mark.
        /// </summary>
        Charted
    }

    /// <summary>How a line between two rooms is drawn.</summary>
    public enum MapLinkState
    {
        Hidden,
        Open,
        /// <summary>Known to exist, not passable yet: a boss door, a ledge out of reach.</summary>
        Blocked,
        /// <summary>A shortcut seen from one side, still closed. Drawn broken.</summary>
        ShortcutLocked,
        /// <summary>Leads somewhere not built. Drawn sealed, never selectable.</summary>
        Sealed
    }

    /// <summary>One symbol on a room.</summary>
    public readonly struct MapIcon
    {
        public MapIcon(MapIconType type, bool isActive, string id)
        {
            Type = type;
            IsActive = isActive;
            Id = id ?? string.Empty;
        }

        public MapIconType Type { get; }

        /// <summary>A lit altar, a living boss. False for an unlit altar or a beaten boss.</summary>
        public bool IsActive { get; }

        /// <summary>Checkpoint or boss id the symbol stands for.</summary>
        public string Id { get; }
    }

    public sealed class MapNode
    {
        public WorldAreaDefinition Area;
        public MapNodeState State;
        public bool IsCurrent;
        public readonly List<MapIcon> Icons = new List<MapIcon>();
        /// <summary>The quill's mark on this room, a colour index; -1 for none.</summary>
        public int MarkColor = -1;

        /// <summary>Known well enough to be picked on the map: visited, or charted.</summary>
        public bool IsKnown => State == MapNodeState.Discovered || State == MapNodeState.Charted;

        public string AreaId => Area.AreaId;
        public Rect Rect => Area.MapRect;

        /// <summary>Empty for a silhouette: an unexplored room has no name yet.</summary>
        public string Label => IsKnown ? Area.DisplayName : string.Empty;
    }

    public sealed class MapLink
    {
        public WorldConnection Connection;
        public MapLinkState State;
        public bool IsZoneTransition;
        public bool IsOneWay;
        /// <summary>Why it is shut, in words that never name an ability the player has not met.</summary>
        public string Hint = string.Empty;
        /// <summary>A small gate symbol on the line; used sparingly, only where the way is shut.</summary>
        public bool ShowsGateIcon;
    }

    /// <summary>Explored share of one region.</summary>
    public readonly struct ZoneCompletion
    {
        public ZoneCompletion(int discovered, int total, int secretsFound, int secretsTotal)
        {
            Discovered = discovered;
            Total = total;
            SecretsFound = secretsFound;
            SecretsTotal = secretsTotal;
        }

        public int Discovered { get; }
        public int Total { get; }
        public int SecretsFound { get; }
        public int SecretsTotal { get; }

        /// <summary>Discovered ordinary areas over all ordinary areas; secrets are counted apart.</summary>
        public float Fraction => Total > 0 ? (float)Discovered / Total : 0f;
    }

    /// <summary>Everything the map screen draws, built fresh from the graph and the save.</summary>
    public sealed class WorldMapView
    {
        public string ZoneHeader = string.Empty;
        public string CurrentAreaId = string.Empty;
        public readonly List<MapNode> Nodes = new List<MapNode>();
        public readonly List<MapLink> Links = new List<MapLink>();
        public readonly Dictionary<string, ZoneCompletion> Completion =
            new Dictionary<string, ZoneCompletion>();

        /// <summary>The box around everything drawn; pan limits are measured against it.</summary>
        public Rect ContentBounds;

        public MapNode Node(string areaId)
        {
            foreach (var node in Nodes)
            {
                if (node.AreaId == areaId)
                {
                    return node;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Turns the world graph and what the player has done into a map. Pure: no scene, no screen,
    /// so every reveal rule is tested without starting the game.
    /// </summary>
    public static class WorldMapBuilder
    {
        public const string UnreachableHint = "Поки що не дістатися";
        public const string ClosedHint = "Зачинено";
        public const string ShortcutHint = "Відчиняється з іншого боку";

        /// <param name="revealAll">Developer view: every room and every id, hidden or not.</param>
        public static WorldMapView Build(WorldGraph graph, IWorldQuery world, string currentAreaId,
            string currentZoneId, bool fastTravelUnlocked, bool revealAll = false,
            IReadOnlyList<MapMark> marks = null)
        {
            var view = new WorldMapView { CurrentAreaId = currentAreaId ?? string.Empty };
            if (graph == null || world == null)
            {
                return view;
            }

            var zone = graph.Zone(currentZoneId) ?? graph.DefaultZone;
            view.ZoneHeader = zone != null ? zone.DisplayName : string.Empty;

            var states = new Dictionary<string, MapNodeState>(StringComparer.Ordinal);
            foreach (var area in graph.Areas)
            {
                if (area != null)
                {
                    states[area.AreaId] = revealAll || world.IsAreaDiscovered(area.AreaId)
                        ? MapNodeState.Discovered
                        : MapNodeState.Hidden;
                }
            }

            // Silhouettes: an ordinary room next to a known one. A secret never gets one — it
            // appears only once the player has been inside it.
            foreach (var connection in graph.Connections)
            {
                if (connection == null)
                {
                    continue;
                }

                MarkSilhouette(graph, states, connection.FromAreaId, connection.ToAreaId);
                MarkSilhouette(graph, states, connection.ToAreaId, connection.FromAreaId);
            }

            // A bought map charts its zone's ordinary rooms; a rumour charts its one room, secret
            // or not. Neither hides what has really been seen.
            foreach (var area in graph.Areas)
            {
                if (area == null || states[area.AreaId] == MapNodeState.Discovered)
                {
                    continue;
                }

                var charted = IsRumoured(world, area)
                    || (!area.IsSecret && world.HasProgression(MerchantStock.MapRewardId(area.ZoneId)));
                if (charted)
                {
                    states[area.AreaId] = MapNodeState.Charted;
                }
            }

            var bounds = default(Rect);
            var hasBounds = false;
            foreach (var area in graph.Areas)
            {
                if (area == null || states[area.AreaId] == MapNodeState.Hidden)
                {
                    continue;
                }

                var node = new MapNode
                {
                    Area = area,
                    State = states[area.AreaId],
                    IsCurrent = area.AreaId == view.CurrentAreaId
                };
                if (node.State == MapNodeState.Discovered)
                {
                    AddIcons(graph, world, node, fastTravelUnlocked);
                }
                else if (node.State == MapNodeState.Charted)
                {
                    // Altars as the map shows them, never lit; a boss is not given away.
                    foreach (var checkpoint in graph.CheckpointsIn(node.AreaId))
                    {
                        node.Icons.Add(new MapIcon(MapIconType.Checkpoint, false, checkpoint.CheckpointId));
                    }
                }

                if (node.State != MapNodeState.Discovered && IsRumoured(world, area))
                {
                    node.Icons.Add(new MapIcon(MapIconType.Rumour, true, area.AreaId));
                }

                if (marks != null && node.IsKnown)
                {
                    foreach (var mark in marks)
                    {
                        if (mark != null && mark.areaId == area.AreaId)
                        {
                            node.MarkColor = mark.color;
                        }
                    }
                }

                view.Nodes.Add(node);
                bounds = hasBounds ? Encapsulate(bounds, area.MapRect) : area.MapRect;
                hasBounds = true;
            }

            view.ContentBounds = bounds;
            foreach (var connection in graph.Connections)
            {
                var link = BuildLink(graph, world, states, connection);
                if (link != null)
                {
                    view.Links.Add(link);
                }
            }

            foreach (var zoneDefinition in graph.Zones)
            {
                if (zoneDefinition != null && zoneDefinition.IsImplemented)
                {
                    view.Completion[zoneDefinition.ZoneId] =
                        Completion(graph, world, zoneDefinition.ZoneId);
                }
            }

            return view;
        }

        public static ZoneCompletion Completion(WorldGraph graph, IWorldQuery world, string zoneId)
        {
            int discovered = 0, total = 0, secretsFound = 0, secretsTotal = 0;
            foreach (var area in graph.AreasIn(zoneId))
            {
                var isKnown = world.IsAreaDiscovered(area.AreaId);
                if (area.IsSecret)
                {
                    secretsTotal++;
                    secretsFound += isKnown ? 1 : 0;
                }
                else
                {
                    total++;
                    discovered += isKnown ? 1 : 0;
                }
            }

            return new ZoneCompletion(discovered, total, secretsFound, secretsTotal);
        }

        private static bool IsRumoured(IWorldQuery world, WorldAreaDefinition area) =>
            world.HasProgression(MerchantStock.RumourRewardId(area.AreaId));

        private static void MarkSilhouette(WorldGraph graph, Dictionary<string, MapNodeState> states,
            string knownId, string otherId)
        {
            if (!states.TryGetValue(knownId, out var known) || known != MapNodeState.Discovered
                || !states.TryGetValue(otherId, out var other) || other != MapNodeState.Hidden)
            {
                return;
            }

            var area = graph.Area(otherId);
            if (area != null && !area.IsSecret)
            {
                states[otherId] = MapNodeState.Unexplored;
            }
        }

        private static void AddIcons(WorldGraph graph, IWorldQuery world, MapNode node,
            bool fastTravelUnlocked)
        {
            foreach (var checkpoint in graph.CheckpointsIn(node.AreaId))
            {
                var isLit = world.IsCheckpointActivated(checkpoint.CheckpointId);
                var type = isLit && fastTravelUnlocked && checkpoint.CanFastTravel
                    ? MapIconType.FastTravel
                    : MapIconType.Checkpoint;
                node.Icons.Add(new MapIcon(type, isLit, checkpoint.CheckpointId));
            }

            if (node.Area.IsBossArena)
            {
                // Only reached here for a discovered arena, so the boss is never given away early.
                node.Icons.Add(new MapIcon(MapIconType.Boss,
                    !world.IsBossDefeated(node.Area.BossId), node.Area.BossId));
            }

            if (node.Area.IsSecret)
            {
                node.Icons.Add(new MapIcon(MapIconType.Secret, true, node.AreaId));
            }

            var extra = node.Area.IconType;
            if (extra != MapIconType.None && extra != MapIconType.Boss
                && extra != MapIconType.Checkpoint && extra != MapIconType.Secret)
            {
                node.Icons.Add(new MapIcon(extra, true, node.AreaId));
            }
        }

        private static MapLink BuildLink(WorldGraph graph, IWorldQuery world,
            Dictionary<string, MapNodeState> states, WorldConnection connection)
        {
            if (connection == null
                || !states.TryGetValue(connection.FromAreaId, out var from)
                || !states.TryGetValue(connection.ToAreaId, out var to))
            {
                return null;
            }

            // A line is drawn when one end is known and the other is at least a silhouette.
            var fromKnown = from == MapNodeState.Discovered || from == MapNodeState.Charted;
            var toKnown = to == MapNodeState.Discovered || to == MapNodeState.Charted;
            if ((!fromKnown && !toKnown) || from == MapNodeState.Hidden || to == MapNodeState.Hidden)
            {
                return null;
            }

            var link = new MapLink
            {
                Connection = connection,
                IsZoneTransition = connection.IsZoneTransition,
                IsOneWay = !connection.IsBidirectional
            };

            if (connection.IsFuture)
            {
                link.State = MapLinkState.Sealed;
                link.Hint = ClosedHint;
                return link;
            }

            if (WorldRules.IsOpen(connection, world))
            {
                link.State = MapLinkState.Open;
                return link;
            }

            var shortcutClosed = connection.IsShortcut
                && !world.IsShortcutUnlocked(connection.PersistentUnlockId);
            if (shortcutClosed && OnlyShortcutIsMissing(connection, world))
            {
                link.State = MapLinkState.ShortcutLocked;
                link.Hint = ShortcutHint;
                return link;
            }

            link.State = MapLinkState.Blocked;
            link.ShowsGateIcon = true;
            link.Hint = !string.IsNullOrEmpty(connection.RequiredAbilityId)
                && !world.HasAbility(connection.RequiredAbilityId)
                ? UnreachableHint
                : ClosedHint;
            return link;
        }

        private static bool OnlyShortcutIsMissing(WorldConnection connection, IWorldQuery world)
        {
            foreach (var requirement in connection.Requirements())
            {
                if (requirement.Kind != RequirementKind.Shortcut && !WorldRules.IsMet(requirement, world))
                {
                    return false;
                }
            }

            return true;
        }

        private static Rect Encapsulate(Rect a, Rect b)
        {
            return Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin),
                Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax));
        }
    }

    /// <summary>
    /// Zoom, pan and the controller cursor for the map, with their limits. Kept apart from the
    /// screen so the limits are tested as numbers.
    /// </summary>
    public sealed class WorldMapViewport
    {
        public WorldMapViewport(float minZoom = 0.5f, float maxZoom = 3f)
        {
            MinZoom = minZoom;
            MaxZoom = Mathf.Max(minZoom, maxZoom);
        }

        public float MinZoom { get; }
        public float MaxZoom { get; }
        public float Zoom { get; private set; } = 1f;

        /// <summary>The map point shown at the centre of the screen.</summary>
        public Vector2 Center { get; private set; }

        /// <summary>Screen size in map units at zoom 1.</summary>
        public Vector2 ViewSize { get; private set; } = new Vector2(40f, 22f);

        public Rect Content { get; private set; }

        public void Configure(Rect content, Vector2 viewSize)
        {
            Content = content;
            ViewSize = new Vector2(Mathf.Max(1f, viewSize.x), Mathf.Max(1f, viewSize.y));
            Clamp();
        }

        public void CenterOn(Vector2 point)
        {
            Center = point;
            Clamp();
        }

        public void Pan(Vector2 delta)
        {
            // Measured in screens rather than units, so panning feels the same at every zoom.
            Center += delta / Zoom;
            Clamp();
        }

        public void ZoomBy(float factor)
        {
            if (factor > 0f)
            {
                Zoom = Mathf.Clamp(Zoom * factor, MinZoom, MaxZoom);
                Clamp();
            }
        }

        /// <summary>What the screen shows right now, in map units.</summary>
        public Rect Visible => new Rect(Center - ViewSize / Zoom * 0.5f, ViewSize / Zoom);

        /// <summary>
        /// Keeps the content from leaving the screen: the centre may wander only as far as the
        /// content's own edges, so some of the map is always in view.
        /// </summary>
        private void Clamp()
        {
            if (Content.width <= 0f && Content.height <= 0f)
            {
                return;
            }

            Center = new Vector2(Mathf.Clamp(Center.x, Content.xMin, Content.xMax),
                Mathf.Clamp(Center.y, Content.yMin, Content.yMax));
        }

        /// <summary>
        /// The next discovered room in a stick direction: the nearest one within a cone, so a push
        /// right never jumps to a room far above.
        /// </summary>
        public static MapNode Step(WorldMapView view, string fromAreaId, Vector2 direction) =>
            Step(view, fromAreaId, direction, null);

        /// <summary>The same, among the rooms <paramref name="among"/> accepts (one zone's, on its map).</summary>
        public static MapNode Step(WorldMapView view, string fromAreaId, Vector2 direction, Func<MapNode, bool> among)
        {
            var from = view?.Node(fromAreaId);
            if (from == null || direction.sqrMagnitude < 0.01f)
            {
                return from;
            }

            var dir = direction.normalized;
            MapNode best = null;
            var bestScore = float.MaxValue;
            foreach (var node in view.Nodes)
            {
                if (node == from || !node.IsKnown || (among != null && !among(node)))
                {
                    continue;
                }

                var delta = node.Area.MapPosition - from.Area.MapPosition;
                var along = Vector2.Dot(delta, dir);
                if (along <= 0.01f)
                {
                    continue;
                }

                var across = Mathf.Abs(delta.x * dir.y - delta.y * dir.x);
                if (across > along * 1.5f)
                {
                    continue;
                }

                var score = along + across * 2f;
                if (score < bestScore)
                {
                    best = node;
                    bestScore = score;
                }
            }

            return best ?? from;
        }
    }
}
