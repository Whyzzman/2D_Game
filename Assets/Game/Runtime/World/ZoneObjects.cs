using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// For services of the world scene that hook into objects of the level: the level now comes and
    /// goes by zone (<see cref="ZoneStreamer"/>), so they look again on every
    /// <see cref="ZoneStreamer.ZonesChanged"/> and hook only what they have not hooked yet.
    /// </summary>
    public static class ZoneObjects
    {
        /// <summary>Objects of this type not in <paramref name="seen"/>; adds them to it and forgets the unloaded.</summary>
        public static List<T> New<T>(HashSet<T> seen, bool includeInactive = false) where T : Object
        {
            seen.RemoveWhere(item => item == null);
            var fresh = new List<T>();
            var found = Object.FindObjectsByType<T>(includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude);
            foreach (var item in found)
            {
                if (seen.Add(item))
                {
                    fresh.Add(item);
                }
            }

            return fresh;
        }

        /// <summary>Every object of this type now in the world, for a list simply replaced on each change.</summary>
        public static T[] All<T>(bool includeInactive = false) where T : Object =>
            Object.FindObjectsByType<T>(includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude);
    }
}
