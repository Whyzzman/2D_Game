using System;
using System.Collections.Generic;

namespace ForgottenKingdom
{
    /// <summary>
    /// The questions the world asks about progress. Doors, gates, the map and fast travel all go
    /// through this; none of them reads the save file.
    /// </summary>
    public interface IWorldQuery
    {
        bool IsAreaDiscovered(string areaId);
        bool IsShortcutUnlocked(string shortcutId);
        bool IsBossDefeated(string bossId);
        bool IsCheckpointActivated(string checkpointId);
        bool HasAbility(string abilityId);
        /// <summary>A progression reward or a key item the player holds.</summary>
        bool HasProgression(string progressionId);
        bool IsWorldPickupCollected(string pickupId);
    }

    /// <summary>How requirements are judged. One place, so a door and the map never disagree.</summary>
    public static class WorldRules
    {
        public static bool IsMet(WorldRequirement requirement, IWorldQuery world)
        {
            if (world == null)
            {
                return requirement.IsNone;
            }

            switch (requirement.Kind)
            {
                case RequirementKind.None:
                    return true;
                case RequirementKind.Ability:
                    return world.HasAbility(requirement.Id);
                case RequirementKind.BossDefeated:
                    return world.IsBossDefeated(requirement.Id);
                case RequirementKind.Progression:
                    return world.HasProgression(requirement.Id);
                case RequirementKind.Shortcut:
                    return world.IsShortcutUnlocked(requirement.Id);
                default:
                    return false;
            }
        }

        public static bool IsOpen(WorldConnection connection, IWorldQuery world)
        {
            if (connection == null)
            {
                return false;
            }

            foreach (var requirement in connection.Requirements())
            {
                if (!IsMet(requirement, world))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Fast travel opens with the graph's own rule, or with the override id the debug command
        /// grants. Nothing is stored for it: an old save that meets the rule simply has it.
        /// </summary>
        public static bool IsFastTravelUnlocked(WorldGraph graph, IWorldQuery world)
        {
            if (graph == null || world == null)
            {
                return false;
            }

            return IsMet(graph.FastTravelRequirement, world)
                || (!string.IsNullOrEmpty(graph.FastTravelOverrideId)
                    && world.HasProgression(graph.FastTravelOverrideId));
        }
    }

    /// <summary>The world questions answered straight from a save's data; for migration and tests.</summary>
    public sealed class ProgressDataWorldQuery : IWorldQuery
    {
        private readonly GameProgressData _data;

        public ProgressDataWorldQuery(GameProgressData data)
        {
            _data = data ?? GameProgressData.CreateNew();
        }

        public bool IsAreaDiscovered(string areaId) => _data.IsAreaDiscovered(areaId);
        public bool IsShortcutUnlocked(string shortcutId) => _data.IsShortcutUnlocked(shortcutId);
        public bool IsBossDefeated(string bossId) => _data.IsBossDefeated(bossId);
        public bool IsCheckpointActivated(string checkpointId) =>
            _data.IsCheckpointActivated(checkpointId);
        public bool HasAbility(string abilityId) =>
            !string.IsNullOrEmpty(abilityId) && _data.player.unlockedAbilities.Contains(abilityId);
        public bool HasProgression(string progressionId) =>
            _data.HasReward(progressionId) || _data.inventory.HasItem(progressionId);
        public bool IsWorldPickupCollected(string pickupId) => _data.IsPickupCollected(pickupId);
    }

    /// <summary>
    /// Walks the graph. Used by the validator to prove the main path never depends on something
    /// optional, and by save migration to recover the route an old save must have walked.
    /// </summary>
    public static class WorldReachability
    {
        /// <summary>Which abilities a reachability pass may assume.</summary>
        public enum AbilityAssumption
        {
            /// <summary>Only movement progression guarantees, and only after its boss is beaten.</summary>
            GuaranteedOnly,
            /// <summary>Everything, as a fully built player would have.</summary>
            Everything
        }

        /// <summary>
        /// Every area reachable from <paramref name="startAreaId"/>, beating bosses as their arenas
        /// are reached. Shortcuts count as locked unless <paramref name="shortcutsOpen"/>: they
        /// improve a route, they are never the route. Areas in unbuilt zones are never entered.
        /// </summary>
        public static HashSet<string> Reachable(WorldGraph graph, string startAreaId,
            AbilityAssumption abilities, bool shortcutsOpen = false,
            ISet<string> defeatedAtStart = null)
        {
            var defeated = new HashSet<string>(defeatedAtStart ?? new HashSet<string>(),
                StringComparer.Ordinal);
            var reached = new HashSet<string>(StringComparer.Ordinal);
            if (graph == null || graph.Area(startAreaId) == null)
            {
                return reached;
            }

            // Fixpoint: beating a boss can open doors behind the player, so keep going until a
            // full pass changes nothing.
            var changed = true;
            while (changed)
            {
                changed = false;
                var frontier = new Queue<string>();
                if (reached.Add(startAreaId))
                {
                    changed = true;
                }

                foreach (var id in reached)
                {
                    frontier.Enqueue(id);
                }

                while (frontier.Count > 0)
                {
                    var areaId = frontier.Dequeue();
                    var area = graph.Area(areaId);
                    if (area != null && area.IsBossArena && defeated.Add(area.BossId))
                    {
                        changed = true;
                    }

                    foreach (var connection in graph.ConnectionsOf(areaId))
                    {
                        if (!connection.LeadsFrom(areaId))
                        {
                            continue;
                        }

                        var next = connection.Other(areaId);
                        if (reached.Contains(next) || !CanEnter(graph, next)
                            || !Passable(graph, connection, abilities, shortcutsOpen, defeated,
                                reached))
                        {
                            continue;
                        }

                        reached.Add(next);
                        frontier.Enqueue(next);
                        changed = true;
                    }
                }
            }

            return reached;
        }

        /// <summary>
        /// The shortest walk between two areas through the main path only — no secret or optional
        /// area in between — under what <paramref name="world"/> says the player has. Empty when
        /// there is none.
        /// </summary>
        public static List<string> MainPath(WorldGraph graph, string fromAreaId, string toAreaId,
            IWorldQuery world)
        {
            var path = new List<string>();
            if (graph == null || graph.Area(fromAreaId) == null || graph.Area(toAreaId) == null)
            {
                return path;
            }

            var parents = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [fromAreaId] = null
            };
            var queue = new Queue<string>();
            queue.Enqueue(fromAreaId);
            while (queue.Count > 0)
            {
                var areaId = queue.Dequeue();
                if (string.Equals(areaId, toAreaId, StringComparison.Ordinal))
                {
                    break;
                }

                foreach (var connection in graph.ConnectionsOf(areaId))
                {
                    if (!connection.LeadsFrom(areaId))
                    {
                        continue;
                    }

                    var next = connection.Other(areaId);
                    var nextArea = graph.Area(next);
                    if (parents.ContainsKey(next) || nextArea == null || !CanEnter(graph, next)
                        || (nextArea.IsOptional && next != toAreaId)
                        || !WorldRules.IsOpen(connection, world))
                    {
                        continue;
                    }

                    parents[next] = areaId;
                    queue.Enqueue(next);
                }
            }

            if (!parents.ContainsKey(toAreaId))
            {
                return path;
            }

            for (var at = toAreaId; at != null; at = parents[at])
            {
                path.Add(at);
            }

            path.Reverse();
            return path;
        }

        private static bool CanEnter(WorldGraph graph, string areaId)
        {
            var area = graph.Area(areaId);
            var zone = area != null ? graph.Zone(area.ZoneId) : null;
            return area != null && (zone == null || zone.IsImplemented);
        }

        private static bool Passable(WorldGraph graph, WorldConnection connection,
            AbilityAssumption abilities, bool shortcutsOpen, HashSet<string> defeated,
            HashSet<string> reached)
        {
            foreach (var requirement in connection.Requirements())
            {
                switch (requirement.Kind)
                {
                    case RequirementKind.None:
                        continue;
                    case RequirementKind.BossDefeated:
                        if (!defeated.Contains(requirement.Id))
                        {
                            return false;
                        }

                        continue;
                    case RequirementKind.Shortcut:
                        // A lever the player has reached is a lever the player can pull.
                        if (!shortcutsOpen && !(reached != null
                                && reached.Contains(connection.OpenedFromAreaId)))
                        {
                            return false;
                        }

                        continue;
                    case RequirementKind.Ability:
                        if (abilities == AbilityAssumption.Everything)
                        {
                            continue;
                        }

                        var ability = graph.Ability(requirement.Id);
                        if (ability == null || ability.Kind != AbilityKind.TraversalProgression
                            || (!string.IsNullOrEmpty(ability.GuaranteedByBossId)
                                && !defeated.Contains(ability.GuaranteedByBossId)))
                        {
                            return false;
                        }

                        continue;
                    default:
                        // Progression items and unbuilt content: nothing in the graph says where
                        // they come from, so the main path is not allowed to rely on them.
                        return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Fills in the map for a save written before the map existed.
    ///
    /// Only what the save proves: the areas holding altars it lit, the area it was saved in, the
    /// arenas of bosses it beat — and the main-path walk from the start to each of those, since the
    /// player cannot have reached an altar without walking there. Secrets and side rooms are never
    /// inferred; a revealed map the player never saw would be a lie.
    /// </summary>
    public static class WorldDiscoveryMigration
    {
        public static void Apply(GameProgressData data, WorldGraph graph)
        {
            if (data == null || graph == null)
            {
                return;
            }

            var known = new List<string>();
            foreach (var checkpointId in data.activatedCheckpoints)
            {
                AddArea(known, graph.AreaForCheckpoint(checkpointId));
            }

            AddArea(known, graph.AreaForCheckpoint(data.checkpointId));
            foreach (var area in graph.Areas)
            {
                if (area != null && area.IsBossArena && data.IsBossDefeated(area.BossId))
                {
                    AddArea(known, area);
                }
            }

            var world = new ProgressDataWorldQuery(data);
            var start = graph.StartAreaId;
            data.MarkAreaDiscovered(start);
            foreach (var areaId in known)
            {
                foreach (var step in WorldReachability.MainPath(graph, start, areaId, world))
                {
                    data.MarkAreaDiscovered(step);
                }

                data.MarkAreaDiscovered(areaId);
            }

            // A region the old save had already walked into is not new; its title card would
            // otherwise greet the player on the first load after updating.
            foreach (var areaId in data.discoveredAreas)
            {
                var area = graph.Area(areaId);
                if (area != null)
                {
                    data.MarkZoneIntroSeen(area.ZoneId);
                }
            }
        }

        private static void AddArea(List<string> known, WorldAreaDefinition area)
        {
            if (area != null && !known.Contains(area.AreaId))
            {
                known.Add(area.AreaId);
            }
        }
    }
}
