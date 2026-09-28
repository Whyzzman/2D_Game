using System;
using System.Collections.Generic;

namespace ForgottenKingdom
{
    /// <summary>
    /// Which zone scenes have to be in memory, as a question about the world graph alone: the zone
    /// the player is in and every zone within <c>depth</c> crossings of it. Two zones are a crossing
    /// apart when a connection runs from an area of one to an area of the other. Zones with no
    /// scene of their own (not built yet) are never asked for.
    /// </summary>
    public static class ZoneStreamingPlan
    {
        public static HashSet<string> Needed(WorldGraph graph, string zoneId, int depth)
        {
            var needed = new HashSet<string>(StringComparer.Ordinal);
            if (graph == null || !IsStreamed(graph, zoneId))
            {
                return needed;
            }

            needed.Add(zoneId);
            var frontier = new List<string> { zoneId };
            for (var step = 0; step < depth && frontier.Count > 0; step++)
            {
                var next = new List<string>();
                foreach (var zone in frontier)
                {
                    foreach (var neighbour in Neighbours(graph, zone))
                    {
                        if (needed.Add(neighbour))
                        {
                            next.Add(neighbour);
                        }
                    }
                }

                frontier = next;
            }

            return needed;
        }

        /// <summary>Zones one crossing away that have a scene to load.</summary>
        public static IEnumerable<string> Neighbours(WorldGraph graph, string zoneId)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var connection in graph.Connections)
            {
                var from = connection != null ? graph.Area(connection.FromAreaId) : null;
                var to = connection != null ? graph.Area(connection.ToAreaId) : null;
                if (from == null || to == null || from.ZoneId == to.ZoneId)
                {
                    continue;
                }

                var other = from.ZoneId == zoneId ? to.ZoneId : to.ZoneId == zoneId ? from.ZoneId : null;
                if (other != null && IsStreamed(graph, other) && seen.Add(other))
                {
                    yield return other;
                }
            }
        }

        public static bool IsStreamed(WorldGraph graph, string zoneId)
        {
            var zone = graph != null ? graph.Zone(zoneId) : null;
            return zone != null && zone.IsImplemented && !string.IsNullOrEmpty(zone.ContentScene);
        }
    }
}
