using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// What exploring pays (item 4 of the big list, the user's choices of 2026-09-28): shards of
    /// the health and mana vessels. Stage A puts seven of the fourteen in the secret pockets the
    /// world already has, beside what lies there, so each is exactly as hard to reach as its
    /// pocket. Built into the whole world under <c>Level/Exploration</c>, replaced whole on every
    /// run; the split hands its children to the zones by position.
    /// </summary>
    public static partial class ContentBuilder
    {
        public const string ExplorationRootName = "Exploration";
        private const string ExplorationIdPrefix = "explore-";

        /// <summary>A shard: where it lies, which vessel, and its id in the save.</summary>
        private readonly struct ShardSpot
        {
            public readonly Vector2 Position;
            public readonly VesselKind Kind;
            public readonly string Id;

            public ShardSpot(Vector2 position, VesselKind kind, string id)
            {
                Position = position;
                Kind = kind;
                Id = id;
            }
        }

        /// <summary>Each lies on the same shelf as the pocket's find, a step to its side.</summary>
        private static readonly ShardSpot[] PocketShards =
        {
            new ShardSpot(new Vector2(14.1f, 7.7f), VesselKind.Health, "tomb-ossuary"),
            new ShardSpot(new Vector2(-6.2f, 10.9f), VesselKind.Mana, "tomb-bell-loft"),
            new ShardSpot(new Vector2(4.5f, 4.5f), VesselKind.Mana, "tomb-reliquary"),
            new ShardSpot(new Vector2(194.5f, 12.3f), VesselKind.Health, "forest-ruin-top"),
            new ShardSpot(new Vector2(204.2f, 15.4f), VesselKind.Health, "forest-hunters-perch"),
            new ShardSpot(new Vector2(305.8f, 3.3f), VesselKind.Mana, "forest-overlook"),
            new ShardSpot(new Vector2(423.3f, 8.8f), VesselKind.Health, "capital-bell-nest")
        };

        /// <summary>The whole layer, from nothing; run inside a world opened whole.</summary>
        public static void BuildExploration()
        {
            var level = GameObject.Find("Level");
            if (level == null)
            {
                Debug.LogError("The world has no Level to hide its rewards in.");
                return;
            }

            var old = level.transform.Find(ExplorationRootName);
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            foreach (var find in Object.FindObjectsByType<WorldPickup>(FindObjectsInactive.Include))
            {
                if (find.PersistentId != null && find.PersistentId.StartsWith(ExplorationIdPrefix))
                {
                    Object.DestroyImmediate(find.gameObject);
                }
            }

            var progress = Object.FindAnyObjectByType<GameProgress>();
            var pickup = AssetDatabase.LoadAssetAtPath<GameObject>(PickupPrefabPath);
            var root = Section(level.transform, ExplorationRootName);
            var shards = Section(root, "Shards");
            if (pickup == null)
            {
                Debug.LogError("No pickup prefab: build the content first.");
                return;
            }

            foreach (var spot in PocketShards)
            {
                PlaceShard(pickup, shards, spot, progress);
            }
        }

        private static void PlaceShard(GameObject pickup, Transform parent, ShardSpot spot,
            GameProgress progress)
        {
            var id = ExplorationIdPrefix + (spot.Kind == VesselKind.Health ? "health-" : "mana-") + spot.Id;
            PlacePickup(pickup, spot.Position, progress, VesselShards.Prefix(spot.Kind) + spot.Id,
                false, 1, id, false);
            foreach (var find in Object.FindObjectsByType<WorldPickup>(FindObjectsInactive.Include))
            {
                if (find.PersistentId == id)
                {
                    find.name = $"{(spot.Kind == VesselKind.Health ? "Health" : "Mana")}Shard_{spot.Id}";
                    find.transform.SetParent(parent, true);
                }
            }
        }
    }
}
