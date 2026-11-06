using System.Collections.Generic;

namespace ForgottenKingdom
{
    /// <summary>
    /// What must hold for a zone layout before it is built, in plain words a designer can act on:
    /// the text parses; from the <c>start</c> marker every main room is reachable with the abilities
    /// the player is sure to have there and nothing strands him (every floor reached leads back to
    /// the start); a room built for an ability is out of reach without it and in reach with it.
    /// </summary>
    public static class LevelCheck
    {
        /// <summary>Checked with the abilities the zone's own «abilities» line promises.</summary>
        public static List<string> Problems(LevelZone zone) => Problems(zone, new HashSet<string>(zone.Abilities));

        public static List<string> Problems(LevelZone zone, ICollection<string> abilities)
        {
            var problems = new List<string>(zone.Errors);
            if (!zone.IsValid)
            {
                return problems;
            }

            var start = Start(zone);
            if (start == null)
            {
                problems.Add("no «start» marker");
                return problems;
            }

            var reach = new LevelReach(zone, abilities);
            var from = reach.SegmentUnder(start.Cell);
            if (from < 0)
            {
                problems.Add($"the start in {start.RoomId} has no floor under it");
                return problems;
            }

            var forward = reach.Forward(from);
            var back = reach.Backward(from);
            var reached = reach.Rooms(forward);
            // Whether a gate is shut is asked of the generous model: at the edge the hero does a
            // little more than the strict one, and a gate he slips through is a broken gate.
            var loose = new LevelReach(zone, abilities, generous: true);
            var nearly = loose.Rooms(loose.Forward(loose.SegmentUnder(start.Cell)));
            foreach (var room in zone.Rooms)
            {
                var gated = !string.IsNullOrEmpty(room.NeedsAbility) && !abilities.Contains(room.NeedsAbility);
                if (!room.IsOptional && !room.IsSecret && !reached.Contains(room.Id))
                {
                    problems.Add($"main room {room.Id} cannot be reached from the start");
                }
                else if ((room.IsOptional || room.IsSecret) && !gated && !reached.Contains(room.Id))
                {
                    problems.Add($"room {room.Id} needs no ability but cannot be reached");
                }
                else if (gated && nearly.Contains(room.Id))
                {
                    problems.Add(reached.Contains(room.Id)
                        ? $"room {room.Id} is reached without {room.NeedsAbility}"
                        : $"room {room.Id} is nearly reached without {room.NeedsAbility}: give the gate one more cell");
                }
            }

            foreach (var index in forward)
            {
                if (!back.Contains(index))
                {
                    problems.Add($"stranded: no way back to the start from {reach.Segments[index]}");
                }
            }

            var gates = Gates(zone, abilities);
            foreach (var ability in gates)
            {
                var more = new HashSet<string>(abilities) { ability };
                var wider = new LevelReach(zone, more);
                var widerFrom = wider.SegmentUnder(start.Cell);
                var widerForward = wider.Forward(widerFrom);
                var rooms = wider.Rooms(widerForward);
                foreach (var room in zone.Rooms)
                {
                    if (room.NeedsAbility == ability && !rooms.Contains(room.Id))
                    {
                        problems.Add($"room {room.Id} is not reached even with {ability}");
                    }
                }

                Strands(wider, widerFrom, widerForward, $" with {ability}", problems);
            }

            // A gate opened is a way in; it must be a way out too (2026-11-06, user: «bugs everywhere»): the
            // Forest's deep roots took the hero down on the grapple and had no ring to climb back by. Asked with
            // each gate's ability and with all of them, the kit a returning hero carries.
            if (gates.Count > 1)
            {
                var all = new HashSet<string>(abilities);
                all.UnionWith(gates);
                var full = new LevelReach(zone, all);
                var fullFrom = full.SegmentUnder(start.Cell);
                Strands(full, fullFrom, full.Forward(fullFrom), " with every gate's ability", problems);
            }

            return problems;
        }

        /// <summary>
        /// Floors that lead back to the start no more. With a gate's ability the hero has long passed the
        /// Guardian, so a lit altar he can still walk to takes him away (FAST_TRAVEL.md): that floor is no trap;
        /// nor is one from which he drops out of the zone into the next (<see cref="LevelReach.LeavesZone"/>).
        /// </summary>
        private static void Strands(LevelReach reach, int from, HashSet<int> forward, string with, List<string> problems)
        {
            var back = reach.Backward(from);
            var away = new HashSet<int>();
            foreach (var marker in reach.Zone.Markers())
            {
                var under = marker.Kind == "altar" ? reach.SegmentUnder(marker.Cell) : -1;
                if (under >= 0)
                {
                    away.Add(under);
                }
            }

            for (var i = 0; i < reach.Segments.Count; i++)
            {
                if (reach.LeavesZone(i))
                {
                    away.Add(i);
                }
            }

            foreach (var index in forward)
            {
                if (!back.Contains(index) && !reach.Forward(index).Overlaps(away))
                {
                    var problem = $"stranded{with}: no way back to the start from {reach.Segments[index]}";
                    if (!problems.Contains(problem))
                    {
                        problems.Add(problem);
                    }
                }
            }
        }

        public static LevelMarker Start(LevelZone zone)
        {
            foreach (var marker in zone.Markers())
            {
                if (marker.Kind == "start")
                {
                    return marker;
                }
            }

            return null;
        }

        private static HashSet<string> Gates(LevelZone zone, ICollection<string> abilities)
        {
            var gates = new HashSet<string>();
            foreach (var room in zone.Rooms)
            {
                if (!string.IsNullOrEmpty(room.NeedsAbility) && !abilities.Contains(room.NeedsAbility))
                {
                    gates.Add(room.NeedsAbility);
                }
            }

            return gates;
        }
    }
}
