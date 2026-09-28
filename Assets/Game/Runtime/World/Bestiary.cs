using System;
using System.Collections.Generic;

namespace ForgottenKingdom
{
    /// <summary>How many of one kind the hero has killed; kept in the save by kind.</summary>
    [Serializable]
    public sealed class KillCount
    {
        public string id = string.Empty;
        public int count;
    }

    /// <summary>
    /// The bestiary's record (item 4 of the big list, stage D): kills counted by enemy kind (the
    /// definition's asset name) in the save, and which kinds the page has already shown, for its
    /// «new» mark. A new game starts it empty with the rest of the save.
    /// </summary>
    public static class Bestiary
    {
        /// <summary>Raised with the kind and its new count.</summary>
        public static event Action<string, int> KillRecorded;

        public static string KindOf(EnemyDefinition definition) => definition != null ? definition.name : string.Empty;

        public static void Record(GameProgress progress, EnemyDefinition definition)
        {
            var kind = KindOf(definition);
            if (progress == null || string.IsNullOrEmpty(kind))
            {
                return;
            }

            var kills = progress.Data.kills;
            var entry = kills.Find(k => k.id == kind);
            if (entry == null)
            {
                entry = new KillCount { id = kind };
                kills.Add(entry);
            }

            entry.count++;
            KillRecorded?.Invoke(kind, entry.count);
        }

        public static int Kills(GameProgress progress, EnemyDefinition definition)
        {
            var kind = KindOf(definition);
            var entry = progress != null ? progress.Data.kills.Find(k => k.id == kind) : null;
            return entry != null ? entry.count : 0;
        }

        public static int TotalKills(GameProgress progress)
        {
            var total = 0;
            if (progress != null)
            {
                foreach (var entry in progress.Data.kills)
                {
                    total += entry.count;
                }
            }

            return total;
        }

        public static bool IsSeen(GameProgress progress, EnemyDefinition definition) =>
            progress != null && progress.Data.bestiarySeen.Contains(KindOf(definition));

        public static void MarkSeen(GameProgress progress, EnemyDefinition definition)
        {
            var kind = KindOf(definition);
            if (progress != null && !string.IsNullOrEmpty(kind) && !progress.Data.bestiarySeen.Contains(kind))
            {
                progress.Data.bestiarySeen.Add(kind);
            }
        }

        /// <summary>Whether the hero has set foot in a zone: any of its areas discovered.</summary>
        public static bool HasVisited(GameProgress progress, string zoneId)
        {
            if (progress == null || progress.World == null)
            {
                return false;
            }

            foreach (var area in progress.World.AreasIn(zoneId))
            {
                if (area != null && progress.IsAreaDiscovered(area.AreaId))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
