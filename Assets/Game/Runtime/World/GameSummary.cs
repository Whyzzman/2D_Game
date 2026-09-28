using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The numbers of the summary after the ending (item 5 of the big list, design/ui/ending
    /// frame 5): time, deaths, the share of the map's rooms discovered, kills, secrets found of
    /// all, the hero's level. Read from the save when asked; nothing here is stored.
    /// </summary>
    public readonly struct GameSummary
    {
        public readonly float Seconds;
        public readonly int Deaths;
        public readonly int MapPercent;
        public readonly int Kills;
        public readonly int SecretsFound;
        public readonly int SecretsTotal;
        public readonly int Level;

        private GameSummary(float seconds, int deaths, int mapPercent, int kills, int secretsFound,
            int secretsTotal, int level)
        {
            Seconds = seconds;
            Deaths = deaths;
            MapPercent = mapPercent;
            Kills = kills;
            SecretsFound = secretsFound;
            SecretsTotal = secretsTotal;
            Level = level;
        }

        public static GameSummary Of(GameProgress progress)
        {
            if (progress == null || progress.Data == null)
            {
                return default;
            }

            int discovered = 0, total = 0, secretsFound = 0, secretsTotal = 0;
            if (progress.World != null)
            {
                foreach (var zone in progress.World.Zones)
                {
                    if (zone == null)
                    {
                        continue;
                    }

                    var completion = WorldMapBuilder.Completion(progress.World, progress, zone.ZoneId);
                    discovered += completion.Discovered;
                    total += completion.Total;
                    secretsFound += completion.SecretsFound;
                    secretsTotal += completion.SecretsTotal;
                }
            }

            var percent = total > 0 ? Mathf.FloorToInt(100f * discovered / total) : 0;
            var data = progress.Data;
            return new GameSummary(data.totalPlayTimeSeconds, data.deaths, percent, Bestiary.TotalKills(progress),
                secretsFound, secretsTotal, data.player != null ? data.player.level : 1);
        }

        /// <summary>«4 год 12 хв», or «12 хв» under an hour.</summary>
        public static string FormatTime(float seconds)
        {
            var minutes = Mathf.FloorToInt(Mathf.Max(0f, seconds) / 60f);
            return minutes >= 60 ? $"{minutes / 60} год {minutes % 60:00} хв" : $"{minutes} хв";
        }
    }
}
