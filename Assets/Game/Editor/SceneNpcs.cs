using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// Places the people of the world and the merchant's chests in TombTrial (item 70, the user's
    /// choices of 2026-09-28): Hnat by the first altar, then by the forest's first once the
    /// Guardian falls and by the capital's gate once the Warden does; Myroslava by the crypt's altar, the royal road's, then the deep forest's;
    /// Severyn's ghost before the Guardian's door; one locked chest in the Tomb and two in the
    /// Forest. Everything goes under one root, <c>Npcs</c>, which is replaced whole, so running it
    /// again is safe and nothing else in the scene is touched.
    /// Run: -executeMethod ForgottenKingdom.EditorTools.SceneNpcs.Apply
    /// </summary>
    public static class SceneNpcs
    {
        public const string RootName = "Npcs";
        private const string GuardianBossId = "guardian-of-the-tomb";
        private const int GroundMask = 1 << 9;

        [MenuItem("Forgotten Kingdom/Scene/Apply Npcs")]
        public static void Apply()
        {
            WorldScenes.Edit(Build);
            Debug.Log("Npcs placed in the world.");
        }

        public static void Build()
        {
            var old = GameObject.Find(RootName);
            if (old != null)
            {
                Object.DestroyImmediate(old);
            }

            Physics2D.SyncTransforms();
            var root = new GameObject(RootName);
            var services = new GameObject("Services");
            services.transform.SetParent(root.transform, false);
            var dialogue = services.AddComponent<DialogueService>();
            var shop = services.AddComponent<ShopService>();
            Set(dialogue, "_shop", shop);

            var gnat = Load(ContentBuilder.GnatPrefabPath);
            var myroslava = Load(ContentBuilder.MyroslavaPrefabPath);
            var severyn = Load(ContentBuilder.SeverynPrefabPath);
            var chest = Load(ContentBuilder.LockedChestPrefabPath);
            var warden = ContentBuilder.WardenBossId;

            Place(root, gnat, "tomb-altar-01", 3.2f, null, GuardianBossId, "Гробниця · Поховальна зала", dialogue);
            Place(root, gnat, "forest-entrance", 3.2f, GuardianBossId, warden, "Ліс · Узлісся", dialogue);
            Place(root, gnat, "capital-gate", 3.2f, warden, null, "Столиця · Міська брама", dialogue);
            Place(root, myroslava, "tomb-altar-02", 2.2f, null, GuardianBossId, "Гробниця · Східна каплиця", dialogue);
            Place(root, myroslava, "royal-road-shrine", -2.6f, GuardianBossId, warden, "Ліс · Королівський шлях", dialogue);
            Place(root, myroslava, "deep-forest-shrine", -2.6f, warden, null, "Ліс · Глибокий ліс", dialogue);
            // Before the Guardian's door, where the Tomb's text puts him.
            PlaceAt(root, severyn, "tomb-approach", ContentBuilder.TombSpot("severyn"), null, null,
                "Гробниця · Підступ до Стража", dialogue, flip: true);

            Chest(root, chest, "tomb-altar-02", 4.6f, "chest-tomb-crypt", 180, "healing_herb");
            Chest(root, chest, "royal-road-shrine", 4.2f, "chest-forest-road", 260, "iron_scrap");
            Chest(root, chest, "deep-forest-shrine", 4.2f, "chest-forest-deep", 320, "healing_herb");
        }

        private static GameObject Load(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError($"Missing prefab {path}; run ContentBuilder.Build first.");
            }

            return prefab;
        }

        private static void Place(GameObject root, GameObject prefab, string altarId, float offset,
            string appears, string leaves, string place, DialogueService dialogue, bool flip = false)
        {
            if (prefab != null && Near(altarId, offset, out var position))
            {
                PlaceAt(root, prefab, altarId, position, appears, leaves, place, dialogue, flip);
            }
        }

        private static void PlaceAt(GameObject root, GameObject prefab, string tag, Vector3 position,
            string appears, string leaves, string place, DialogueService dialogue, bool flip = false)
        {
            if (prefab == null)
            {
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
            instance.name = $"{prefab.name}_{tag}";
            instance.transform.position = position;
            if (flip)
            {
                // Drawn facing right; this one faces back towards where the hero comes from.
                instance.transform.localScale = new Vector3(-1f, 1f, 1f);
            }

            var npc = instance.GetComponent<Npc>();
            var serialized = new SerializedObject(npc);
            serialized.FindProperty("_appearsAfterBoss").stringValue = appears ?? string.Empty;
            serialized.FindProperty("_leavesAfterBoss").stringValue = leaves ?? string.Empty;
            serialized.FindProperty("_placeName").stringValue = place;
            serialized.FindProperty("_dialogue").objectReferenceValue = dialogue;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Chest(GameObject root, GameObject prefab, string altarId, float offset, string id,
            int gold, string itemId)
        {
            if (prefab == null || !Near(altarId, offset, out var position))
            {
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
            instance.name = $"LockedChest_{id}";
            instance.transform.position = position;
            var serialized = new SerializedObject(instance.GetComponent<LockedChest>());
            serialized.FindProperty("_chestId").stringValue = id;
            serialized.FindProperty("_gold").intValue = gold;
            serialized.FindProperty("_itemId").stringValue = itemId ?? string.Empty;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// A spot on the floor beside an altar: the altar's own floor line, moved along it and
        /// dropped onto whatever ground is there, so a placement never floats or sinks.
        /// </summary>
        private static bool Near(string altarId, float offset, out Vector3 position)
        {
            position = default;
            Checkpoint altar = null;
            foreach (var checkpoint in Object.FindObjectsByType<Checkpoint>(FindObjectsInactive.Include))
            {
                if (checkpoint.CheckpointId == altarId)
                {
                    altar = checkpoint;
                }
            }

            if (altar == null)
            {
                Debug.LogError($"No altar {altarId} in the scene to place beside.");
                return false;
            }

            var floor = altar.transform.position.y - 0.8f;
            var x = altar.transform.position.x + offset;
            var hit = Physics2D.Raycast(new Vector2(x, floor + 1.5f), Vector2.down, 4f, GroundMask);
            position = new Vector3(x, hit ? hit.point.y : floor, 0f);
            return true;
        }

        private static void Set(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
