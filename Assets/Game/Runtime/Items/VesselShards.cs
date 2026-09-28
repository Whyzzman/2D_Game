using UnityEngine;

namespace ForgottenKingdom
{
    public enum VesselKind { Health, Mana }

    /// <summary>
    /// Shards of the hero's vessels found in the world (item 4 of the big list, the user's choice
    /// of 2026-09-28): four shards of health or three of mana make one whole vessel, and each whole
    /// vessel adds 15% to its bar, as a mask or a soul vessel does in Hollow Knight. The save holds
    /// the shards as granted rewards, never the bonus, so the two cannot disagree.
    /// </summary>
    public static class VesselShards
    {
        public const string HealthPrefix = "health-shard:";
        public const string ManaPrefix = "mana-shard:";
        public const int HealthPerVessel = 4;
        public const int ManaPerVessel = 3;
        public const float BonusPerVessel = 0.15f;

        public static string Prefix(VesselKind kind) =>
            kind == VesselKind.Health ? HealthPrefix : ManaPrefix;

        public static int PerVessel(VesselKind kind) =>
            kind == VesselKind.Health ? HealthPerVessel : ManaPerVessel;

        public static string ShardName(VesselKind kind) =>
            kind == VesselKind.Health ? "Осколок здоров'я" : "Осколок мани";

        /// <summary>Whether a reward id is one of these shards, and of which vessel.</summary>
        public static bool TryKind(string id, out VesselKind kind)
        {
            kind = VesselKind.Health;
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            if (id.StartsWith(HealthPrefix)) { return true; }
            kind = VesselKind.Mana;
            return id.StartsWith(ManaPrefix);
        }

        public static int Count(GameProgress progress, VesselKind kind)
        {
            var count = 0;
            if (progress == null)
            {
                return count;
            }

            var prefix = Prefix(kind);
            foreach (var reward in progress.AcquiredRewards)
            {
                count += reward.StartsWith(prefix) ? 1 : 0;
            }

            return count;
        }

        public static int Vessels(GameProgress progress, VesselKind kind) =>
            Count(progress, kind) / PerVessel(kind);

        /// <summary>Shards toward the next vessel: 0 right after one is completed.</summary>
        public static int Partial(GameProgress progress, VesselKind kind) =>
            Count(progress, kind) % PerVessel(kind);

        public static float Multiplier(GameProgress progress, VesselKind kind) =>
            1f + BonusPerVessel * Vessels(progress, kind);

        /// <summary>The line the HUD shows when one is picked up.</summary>
        public static string Notice(GameProgress progress, VesselKind kind)
        {
            var partial = Partial(progress, kind);
            if (partial == 0 && Count(progress, kind) > 0)
            {
                return kind == VesselKind.Health ? "Здоров'я зросло" : "Мана зросла";
            }

            return $"{ShardName(kind)} · {partial}/{PerVessel(kind)}";
        }

        public static Color PickupColor(VesselKind kind) => kind == VesselKind.Health
            ? new Color(0.9f, 0.34f, 0.3f)
            : new Color(0.42f, 0.62f, 0.98f);
    }
}
