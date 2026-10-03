using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>How a region shows on the world map.</summary>
    public enum ZoneMapState
    {
        /// <summary>Not drawn: lost in the fog.</summary>
        Hidden,
        /// <summary>A dark island with «?»: a road from a known region leads there.</summary>
        Silhouette,
        /// <summary>Lit, named, with its icons: the hero has been inside.</summary>
        Known
    }

    /// <summary>One island of the world map and what its card says.</summary>
    public sealed class ZoneMapEntry
    {
        public ZoneDefinition Zone;
        public ZoneMapState State;
        public bool IsCurrent;
        public ZoneCompletion Completion;
        public int ShrinesLit;
        public int ShrinesTotal;
        public int BossesBeaten;
        public int BossesTotal;

        public string ZoneId => Zone.ZoneId;
        public bool IsKnown => State == ZoneMapState.Known;
        /// <summary>Its own map can be opened: known and built.</summary>
        public bool CanEnter => IsKnown && Zone.IsImplemented;
        public bool HasBossAlive => BossesTotal > BossesBeaten;
        /// <summary>Explored share in whole percent, as the card shows it.</summary>
        public int ExploredPercent => Mathf.RoundToInt(Completion.Fraction * 100f);
    }

    /// <summary>A road between two islands: drawn when both ends are known, hinted from one.</summary>
    public sealed class RoadMapEntry
    {
        public WorldRoad Road;
        public bool IsShown;
        public bool IsHinted;
    }

    /// <summary>
    /// The world map's level above the rooms (item 2 of the 15-zone list, design/ui/world-map):
    /// the regions as islands, their roads and cards. Built from the same room view the zone maps
    /// draw, so a region counts as known exactly when one of its rooms is.
    /// </summary>
    public sealed class WorldAtlas
    {
        public readonly List<ZoneMapEntry> Zones = new List<ZoneMapEntry>();
        public readonly List<RoadMapEntry> Roads = new List<RoadMapEntry>();

        public ZoneMapEntry Zone(string zoneId) => Zones.Find(z => z.ZoneId == zoneId);

        public static WorldAtlas Build(WorldGraph graph, IWorldQuery world, WorldMapView view, string currentZoneId)
        {
            var atlas = new WorldAtlas();
            if (graph == null || world == null)
            {
                return atlas;
            }

            var known = new HashSet<string>();
            if (!string.IsNullOrEmpty(currentZoneId)) { known.Add(currentZoneId); }
            if (view != null)
            {
                foreach (var node in view.Nodes)
                {
                    if (node.State == MapNodeState.Discovered) { known.Add(node.Area.ZoneId); }
                }
            }

            foreach (var zone in graph.Zones)
            {
                if (!zone.IsOnWorldMap)
                {
                    continue;
                }

                var entry = new ZoneMapEntry
                {
                    Zone = zone,
                    State = known.Contains(zone.ZoneId) ? ZoneMapState.Known : ZoneMapState.Hidden,
                    IsCurrent = zone.ZoneId == currentZoneId,
                    Completion = WorldMapBuilder.Completion(graph, world, zone.ZoneId)
                };
                foreach (var checkpoint in graph.Checkpoints)
                {
                    if (checkpoint.ZoneId != zone.ZoneId) { continue; }
                    entry.ShrinesTotal++;
                    entry.ShrinesLit += world.IsCheckpointActivated(checkpoint.CheckpointId) ? 1 : 0;
                }

                foreach (var area in graph.AreasIn(zone.ZoneId))
                {
                    if (!area.IsBossArena) { continue; }
                    entry.BossesTotal++;
                    entry.BossesBeaten += world.IsBossDefeated(area.BossId) ? 1 : 0;
                }

                atlas.Zones.Add(entry);
            }

            foreach (var road in graph.Roads)
            {
                var from = atlas.Zone(road.FromZoneId);
                var to = atlas.Zone(road.ToZoneId);
                if (from == null || to == null)
                {
                    continue;
                }

                var entry = new RoadMapEntry { Road = road, IsShown = from.IsKnown && to.IsKnown };
                entry.IsHinted = !entry.IsShown && (from.IsKnown || to.IsKnown);
                if (entry.IsHinted)
                {
                    // The far end of a road from a known region shows as a silhouette.
                    var far = from.IsKnown ? to : from;
                    far.State = ZoneMapState.Silhouette;
                }

                atlas.Roads.Add(entry);
            }

            return atlas;
        }

        /// <summary>
        /// The next known region in a stick direction, the way the room cursor moves: nearest within
        /// a cone. The world map's y grows downward, so «up» is negative there.
        /// </summary>
        public ZoneMapEntry Step(string fromZoneId, Vector2 direction)
        {
            var from = Zone(fromZoneId);
            if (from == null || direction.sqrMagnitude < 0.01f)
            {
                return from;
            }

            var dir = new Vector2(direction.x, -direction.y).normalized;
            ZoneMapEntry best = null;
            var bestScore = float.MaxValue;
            foreach (var zone in Zones)
            {
                if (zone == from || !zone.IsKnown)
                {
                    continue;
                }

                var delta = zone.Zone.WorldPosition - from.Zone.WorldPosition;
                var along = Vector2.Dot(delta, dir);
                if (along <= 1f)
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
                    best = zone;
                    bestScore = score;
                }
            }

            return best ?? from;
        }
    }
}
