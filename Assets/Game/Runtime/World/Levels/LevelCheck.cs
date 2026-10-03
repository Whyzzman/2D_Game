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

            foreach (var ability in Gates(zone, abilities))
            {
                var more = new HashSet<string>(abilities) { ability };
                var wider = new LevelReach(zone, more);
                var rooms = wider.Rooms(wider.Forward(wider.SegmentUnder(start.Cell)));
                foreach (var room in zone.Rooms)
                {
                    if (room.NeedsAbility == ability && !rooms.Contains(room.Id))
                    {
                        problems.Add($"room {room.Id} is not reached even with {ability}");
                    }
                }
            }

            return problems;
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
