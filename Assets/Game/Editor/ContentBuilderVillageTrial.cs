using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// «Нічний дзвін» (item 5c of the 15 zones, the user's choices of 2026-10-19): a small bell on a
    /// frame in the square by the well, silent while the Bellringer lives. Rung after him, it closes
    /// the square's two ways out and calls three waves of the village's own — villagers, watchdogs,
    /// scarecrows with their ravens. The first win lays out a health shard and gold, like the trial
    /// of arms; the machinery is the same <see cref="TrialArena"/>. Its places are the square's text.
    /// </summary>
    public static partial class ContentBuilder
    {
        public const string NightBellTrialId = ExplorationIdPrefix + "trial-night-bell";

        private static void BuildNightBellTrial(Transform square, GameProgress progress)
        {
            var bellAt = VillageSpot("night-bell");
            var bell = new GameObject("NightBell");
            bell.transform.SetParent(square);
            bell.transform.position = bellAt;
            var trigger = bell.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(1.8f, 2.6f);
            trigger.offset = new Vector2(0f, 1.3f);
            var timber = new Color(0.26f, 0.2f, 0.15f);
            GongPart(bell, "PostWest", new Vector2(-0.6f, 1.1f), new Vector2(0.16f, 2.2f), timber);
            GongPart(bell, "PostEast", new Vector2(0.6f, 1.1f), new Vector2(0.16f, 2.2f), timber);
            GongPart(bell, "Beam", new Vector2(0f, 2.2f), new Vector2(1.5f, 0.16f), timber);
            GongPart(bell, "Bell", new Vector2(0f, 1.6f), new Vector2(0.7f, 0.8f), new Color(0.55f, 0.42f, 0.22f));

            // The square's ways out at floor level, each a passage four units high.
            var barriers = new List<GameObject>();
            foreach (var spot in new[] { "night-bell-west", "night-bell-east" })
            {
                var at = VillageSpot(spot);
                var wall = VillageBlock(square, spot == "night-bell-west" ? "NightBellWallWest" : "NightBellWallEast",
                    at + new Vector2(0f, 2.75f), new Vector2(0.5f, 5.5f), VillageArt("env_village_planks_01"));
                Ensure<NonClimbableSurface>(wall);
                wall.SetActive(false);
                barriers.Add(wall);
            }

            var trial = Ensure<TrialArena>(bell);
            SetString(trial, "_trialId", NightBellTrialId);
            SetString(trial, "_displayName", "Нічний дзвін");
            SetString(trial, "_requiresBoss", BellringerBossId);
            SetReference(trial, "_progress", progress);
            SetReference(trial, "_registry", Object.FindAnyObjectByType<EnemyRegistry>());

            GameObject Enemy(string name) => AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/{name}.prefab");
            Vector2 Floor(float dx) => bellAt + new Vector2(dx, 0.9f);
            var waves = new[]
            {
                (new[] { Enemy(VillagerPrefabName), Enemy(VillagerPrefabName), Enemy(VillagerPrefabName) },
                    new[] { Floor(-9f), Floor(8f), Floor(11f) }),
                (new[] { Enemy(VillagerPrefabName), Enemy(WatchdogPrefabName), Enemy(ScarecrowPrefabName) },
                    new[] { Floor(-10f), Floor(9f), Floor(5f) }),
                (new[] { Enemy(ScarecrowPrefabName), Enemy(ScarecrowPrefabName), Enemy(VillagerPrefabName),
                        Enemy(VillagerPrefabName) },
                    new[] { Floor(-6f), Floor(7f), Floor(-11f), Floor(12f) })
            };
            var serialized = new SerializedObject(trial);
            var list = serialized.FindProperty("_waves");
            list.arraySize = waves.Length;
            for (var w = 0; w < waves.Length; w++)
            {
                var element = list.GetArrayElementAtIndex(w);
                var enemies = element.FindPropertyRelative("Enemies");
                var positions = element.FindPropertyRelative("Positions");
                enemies.arraySize = waves[w].Item1.Length;
                positions.arraySize = waves[w].Item2.Length;
                for (var i = 0; i < waves[w].Item1.Length; i++)
                {
                    enemies.GetArrayElementAtIndex(i).objectReferenceValue = waves[w].Item1[i];
                    positions.GetArrayElementAtIndex(i).vector2Value = waves[w].Item2[i];
                }
            }

            SetObjects(serialized.FindProperty("_barriers"), barriers.ToArray());

            var pickup = AssetDatabase.LoadAssetAtPath<GameObject>(PickupPrefabPath);
            PlacePickup(pickup, bellAt + new Vector2(-1.5f, 0.7f), progress, VesselShards.HealthPrefix + "trial-night-bell",
                false, 1, ExplorationIdPrefix + "health-trial-night-bell", false);
            PlacePickup(pickup, bellAt + new Vector2(1.5f, 0.6f), progress, string.Empty, true, 400,
                ExplorationIdPrefix + "trial-night-bell-purse", true);
            var rewards = new List<GameObject>();
            foreach (var find in Object.FindObjectsByType<WorldPickup>(FindObjectsInactive.Include))
            {
                if (find.PersistentId != null && find.PersistentId.Contains("trial-night-bell"))
                {
                    find.transform.SetParent(square, true);
                    find.gameObject.SetActive(false);
                    rewards.Add(find.gameObject);
                }
            }

            SetObjects(serialized.FindProperty("_rewards"), rewards.ToArray());
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
