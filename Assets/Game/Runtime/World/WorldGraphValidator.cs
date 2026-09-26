using System;
using System.Collections.Generic;

namespace ForgottenKingdom
{
    /// <summary>
    /// Everything that can be wrong with a world graph, checked in one pass and reported in plain
    /// words. The content builder refuses to build a scene on top of a graph with problems, and the
    /// tests hold the shipped graph to an empty report.
    ///
    /// Beyond typos — duplicate ids, links to rooms that do not exist — it proves three things about
    /// progression: every room on the main path can be reached with guaranteed movement alone, no
    /// build choice is ever needed to get there, and nothing the player can reach is a trap with no
    /// way back to an altar.
    /// </summary>
    public static class WorldGraphValidator
    {
        public static List<string> Validate(WorldGraph graph)
        {
            var problems = new List<string>();
            if (graph == null)
            {
                problems.Add("There is no world graph.");
                return problems;
            }

            CheckIds(graph, problems);
            CheckReferences(graph, problems);
            if (problems.Count == 0)
            {
                // Walking a graph with broken links would only report the same breakage again.
                CheckProgression(graph, problems);
                CheckReturnRoutes(graph, problems);
            }

            return problems;
        }

        // ---------- ids ----------

        private static void CheckIds(WorldGraph graph, List<string> problems)
        {
            Unique(graph.Zones, z => z.ZoneId, "zone", problems);
            Unique(graph.Areas, a => a.AreaId, "area", problems);
            Unique(graph.Connections, c => c.ConnectionId, "connection", problems);
            Unique(graph.Checkpoints, c => c.CheckpointId, "checkpoint", problems);
            Unique(graph.Abilities, a => a.AbilityId, "ability", problems);
        }

        private static void Unique<T>(IReadOnlyList<T> items, Func<T, string> key, string kind,
            List<string> problems) where T : class
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in items)
            {
                var id = item != null ? key(item) : null;
                if (string.IsNullOrEmpty(id))
                {
                    problems.Add($"A {kind} has no id.");
                }
                else if (!seen.Add(id))
                {
                    problems.Add($"Duplicate {kind} id '{id}'.");
                }
            }
        }

        // ---------- references ----------

        private static void CheckReferences(WorldGraph graph, List<string> problems)
        {
            if (graph.Area(graph.StartAreaId) == null)
            {
                problems.Add($"Start area '{graph.StartAreaId}' does not exist.");
            }

            var bosses = new HashSet<string>(StringComparer.Ordinal);
            foreach (var area in graph.Areas)
            {
                if (graph.Zone(area.ZoneId) == null)
                {
                    problems.Add($"Area '{area.AreaId}' is in unknown zone '{area.ZoneId}'.");
                }

                if (!string.IsNullOrEmpty(area.RequiredAbilityId)
                    && graph.Ability(area.RequiredAbilityId) == null)
                {
                    problems.Add($"Area '{area.AreaId}' names unknown ability "
                        + $"'{area.RequiredAbilityId}'.");
                }

                if (area.IsBossArena)
                {
                    bosses.Add(area.BossId);
                }

                var areaZone = graph.Zone(area.ZoneId);
                if (areaZone != null && areaZone.IsImplemented
                    && area.DiscoveryRule == AreaDiscoveryRule.OnEnter
                    && (area.WorldBounds.width <= 0f || area.WorldBounds.height <= 0f))
                {
                    problems.Add($"Area '{area.AreaId}' is found by position but has no world "
                        + "bounds; the player could never be in it.");
                }
            }

            foreach (var connection in graph.Connections)
            {
                var id = connection.ConnectionId;
                if (graph.Area(connection.FromAreaId) == null)
                {
                    problems.Add($"Connection '{id}' starts in missing area '{connection.FromAreaId}'.");
                }

                if (graph.Area(connection.ToAreaId) == null)
                {
                    problems.Add($"Connection '{id}' leads to missing area '{connection.ToAreaId}'.");
                }

                if (connection.FromAreaId == connection.ToAreaId)
                {
                    problems.Add($"Connection '{id}' leads from an area to itself.");
                }

                if (!string.IsNullOrEmpty(connection.RequiredAbilityId)
                    && graph.Ability(connection.RequiredAbilityId) == null)
                {
                    problems.Add($"Connection '{id}' requires unknown ability "
                        + $"'{connection.RequiredAbilityId}'.");
                }

                if (!string.IsNullOrEmpty(connection.RequiredBossId)
                    && !bosses.Contains(connection.RequiredBossId))
                {
                    problems.Add($"Connection '{id}' waits for boss '{connection.RequiredBossId}', "
                        + "who has no arena in the graph.");
                }

                if (connection.IsShortcut)
                {
                    if (string.IsNullOrEmpty(connection.PersistentUnlockId))
                    {
                        problems.Add($"Shortcut '{id}' has no persistent id; it could never be saved.");
                    }

                    if (!connection.Touches(connection.OpenedFromAreaId))
                    {
                        problems.Add($"Shortcut '{id}' must be opened from one of its own ends.");
                    }
                }

                var to = graph.Area(connection.ToAreaId);
                var toZone = to != null ? graph.Zone(to.ZoneId) : null;
                if (toZone != null && !toZone.IsImplemented && !connection.IsFuture)
                {
                    problems.Add($"Connection '{id}' leads into unbuilt zone '{toZone.ZoneId}' "
                        + "without being marked as a future path.");
                }
            }

            foreach (var checkpoint in graph.Checkpoints)
            {
                var area = graph.Area(checkpoint.AreaId);
                if (area == null)
                {
                    problems.Add($"Checkpoint '{checkpoint.CheckpointId}' is in missing area "
                        + $"'{checkpoint.AreaId}'.");
                    continue;
                }

                if (area.ZoneId != checkpoint.ZoneId)
                {
                    problems.Add($"Checkpoint '{checkpoint.CheckpointId}' says zone "
                        + $"'{checkpoint.ZoneId}' but its area is in '{area.ZoneId}'.");
                }

                var zone = graph.Zone(checkpoint.ZoneId);
                if (zone != null && !zone.IsImplemented)
                {
                    problems.Add($"Checkpoint '{checkpoint.CheckpointId}' stands in unbuilt zone "
                        + $"'{zone.ZoneId}'; nobody could ever light or reach it.");
                }
            }

            if (graph.FastTravelRequirement.Kind == RequirementKind.Ability
                && graph.Ability(graph.FastTravelRequirement.Id) == null)
            {
                problems.Add("Fast travel waits for an unknown ability.");
            }
        }

        // ---------- progression ----------

        /// <summary>
        /// Main-path rooms must be reachable with nothing but guaranteed movement and beaten
        /// bosses. A room that needs an optional ability or a skill-tree choice to reach cannot
        /// be on the main path — it has to be marked optional, or the gate has to change.
        /// </summary>
        private static void CheckProgression(WorldGraph graph, List<string> problems)
        {
            var reached = WorldReachability.Reachable(graph, graph.StartAreaId,
                WorldReachability.AbilityAssumption.GuaranteedOnly);
            foreach (var area in graph.Areas)
            {
                var zone = graph.Zone(area.ZoneId);
                if (zone == null || !zone.IsImplemented || area.IsOptional)
                {
                    continue;
                }

                if (!reached.Contains(area.AreaId))
                {
                    problems.Add($"Main-path area '{area.AreaId}' cannot be reached without "
                        + "optional abilities, build choices or unbuilt content.");
                }
            }

            foreach (var connection in graph.Connections)
            {
                var ability = graph.Ability(connection.RequiredAbilityId);
                if (ability == null || ability.Kind != AbilityKind.CombatSkill)
                {
                    continue;
                }

                var from = graph.Area(connection.FromAreaId);
                var to = graph.Area(connection.ToAreaId);
                if (from != null && to != null && !from.IsOptional && !to.IsOptional)
                {
                    problems.Add($"Connection '{connection.ConnectionId}' puts combat skill "
                        + $"'{ability.AbilityId}' between two main-path areas.");
                }
            }

            var everything = WorldReachability.Reachable(graph, graph.StartAreaId,
                WorldReachability.AbilityAssumption.Everything, shortcutsOpen: true);
            foreach (var area in graph.Areas)
            {
                var zone = graph.Zone(area.ZoneId);
                if (zone != null && zone.IsImplemented && !everything.Contains(area.AreaId))
                {
                    problems.Add($"Area '{area.AreaId}' cannot be reached even with every "
                        + "ability; it is disconnected.");
                }
            }
        }

        /// <summary>
        /// From every room the player can get into, some altar has to be reachable again. A drop
        /// with no climb back and no altar at the bottom is how a save becomes a trap.
        /// </summary>
        private static void CheckReturnRoutes(WorldGraph graph, List<string> problems)
        {
            var reachable = WorldReachability.Reachable(graph, graph.StartAreaId,
                WorldReachability.AbilityAssumption.Everything, shortcutsOpen: false);
            var defeated = new HashSet<string>(StringComparer.Ordinal);
            foreach (var area in graph.Areas)
            {
                if (area.IsBossArena && reachable.Contains(area.AreaId))
                {
                    defeated.Add(area.BossId);
                }
            }

            foreach (var areaId in reachable)
            {
                var back = WorldReachability.Reachable(graph, areaId,
                    WorldReachability.AbilityAssumption.Everything, shortcutsOpen: false,
                    defeatedAtStart: defeated);
                var hasAltar = false;
                foreach (var id in back)
                {
                    if (graph.CheckpointsIn(id).Count > 0)
                    {
                        hasAltar = true;
                        break;
                    }
                }

                if (!hasAltar)
                {
                    problems.Add($"From '{areaId}' no altar can be reached again: a softlock.");
                }
            }
        }
    }
}
