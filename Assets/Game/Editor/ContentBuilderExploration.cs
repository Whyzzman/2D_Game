using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// What exploring pays (item 4 of the big list, the user's choices of 2026-09-28): shards of
    /// the health and mana vessels. Stage A puts seven of the fourteen in the secret pockets the
    /// world already has, beside what lies there, so each is exactly as hard to reach as its
    /// pocket. Stage B walls five more into rooms behind breakable walls, each hung off the far
    /// end of a dead-end ledge, so nothing on the main path runs into one.
    /// Built into the whole world under <c>Level/Exploration</c>, replaced whole on every run;
    /// the split hands its children to the zones by position.
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
            new ShardSpot(new Vector2(194.5f, 12.3f), VesselKind.Health, "forest-ruin-top"),
            new ShardSpot(new Vector2(204.2f, 15.4f), VesselKind.Health, "forest-hunters-perch"),
            new ShardSpot(new Vector2(305.8f, 3.3f), VesselKind.Mana, "forest-overlook"),
            new ShardSpot(new Vector2(423.3f, 8.8f), VesselKind.Health, "capital-bell-nest")
        };

        private enum RoomSide { West, East }

        /// <summary>
        /// A room behind a wall: its inside from <c>X0</c> to <c>X1</c> over a floor at
        /// <c>FloorY</c>, the breakable wall on the side the hero comes from, and one shard.
        /// </summary>
        private readonly struct SecretRoom
        {
            public readonly string AreaId;
            public readonly int Zone;
            public readonly float X0;
            public readonly float X1;
            public readonly float FloorY;
            public readonly RoomSide Wall;
            public readonly VesselKind Shard;

            public SecretRoom(string areaId, int zone, float x0, float x1, float floorY, RoomSide wall,
                VesselKind shard)
            {
                AreaId = areaId;
                Zone = zone;
                X0 = x0;
                X1 = x1;
                FloorY = floorY;
                Wall = wall;
                Shard = shard;
            }
        }

        private const float RoomHeight = 2.8f;
        private const float RoomShell = 0.5f;

        /// <summary>
        /// Where the ledge ends, the wall begins: the hero standing at its end strikes it. Each
        /// room's floor is at least four units over the ground below, so no jump from the main
        /// path meets its underside. Zones: 0 the Tomb, 1 the Forest, 2 the Capital.
        /// </summary>
        private static readonly SecretRoom[] SecretRooms =
        {
            // East of the grove's ledge (169.5..174.5, 5.9).
            new SecretRoom(ForestGroveStash, 1, 175f, 179f, 5.9f, RoomSide.West, VesselKind.Mana),
            // East of the hunters' perch (201..205, 14.9).
            new SecretRoom(ForestFalconerNest, 1, 205.5f, 209.5f, 14.9f, RoomSide.West, VesselKind.Health),
            // East of the bell nest (421..424, 8.2).
            new SecretRoom(CapitalRoofCache, 2, 424.5f, 428.5f, 8.2f, RoomSide.West, VesselKind.Mana),
            // West of the royal gallery (495..509, 6.8).
            new SecretRoom(CapitalHiddenTreasury, 2, 490.5f, 494.5f, 6.8f, RoomSide.East, VesselKind.Health)
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

            // The Tomb's shards are its text's (Levels/tomb.txt) and stay with its rooms.
            var tomb = level.transform.Find(TombRootName);
            foreach (var find in Object.FindObjectsByType<WorldPickup>(FindObjectsInactive.Include))
            {
                if (tomb != null && find.transform.IsChildOf(tomb))
                {
                    continue;
                }

                if (find.PersistentId != null && find.PersistentId.StartsWith(ExplorationIdPrefix))
                {
                    Object.DestroyImmediate(find.gameObject);
                }
            }

            if (_sprite == null) { _sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath); }
            if (_material == null) { _material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath); }
            if (_shapes == null) { _shapes = ProductionSpriteArt.Generate(); }
            if (_tomb == null) { _tomb = PlaceholderTombArt.Generate(); }
            if (_forest == null) { _forest = PlaceholderForestArt.Generate(); }
            if (_capital == null) { _capital = PlaceholderCapitalArt.Generate(); }
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

            var rooms = Section(root, "SecretRooms");
            foreach (var room in SecretRooms)
            {
                BuildSecretRoom(rooms, room, pickup, progress);
            }

            BuildArmsTrial(Section(root, "ArmsTrial"), pickup, progress);
            BuildThornCourse(Section(root, "ThornCourse"), pickup, progress);
            BuildInscriptions(Section(root, "Inscriptions"));
            if (progress != null)
            {
                SetReference(progress, "_bestiary", BuildBestiaryCatalog());
                BuildEnding(progress);
            }
        }

        // ---------- the bestiary (stage D) ----------

        private const string BestiaryCatalogPath = "Assets/Game/Data/BestiaryCatalog.asset";

        /// <summary>Every kind by zone, in the order the road meets them, and the three bosses.</summary>
        private static BestiaryCatalog BuildBestiaryCatalog()
        {
            var catalog = CreateAsset<BestiaryCatalog>(BestiaryCatalogPath);
            var kinds = new[]
            {
                ("ZombieSoldier", TombZoneId), ("ShieldGuard", TombZoneId), ("Tomb/BoneArcher", TombZoneId),
                ("Forest/CorruptedHound", ForestZoneId), ("Forest/ForestArcher", ForestZoneId),
                ("Forest/RootShaman", ForestZoneId), ("Forest/RoyalSpearman", ForestZoneId),
                ("Capital/AshRaven", CapitalZoneId), ("Capital/StoneGargoyle", CapitalZoneId),
                ("Capital/PalacePriest", CapitalZoneId), ("Capital/RoyalCrossbowman", CapitalZoneId)
            };
            var serialized = new SerializedObject(catalog);
            var enemies = serialized.FindProperty("_enemies");
            enemies.arraySize = kinds.Length;
            for (var i = 0; i < kinds.Length; i++)
            {
                var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"Assets/Game/Data/{kinds[i].Item1}.asset");
                if (definition == null)
                {
                    Debug.LogError($"Bestiary: no enemy definition {kinds[i].Item1}.");
                }

                var element = enemies.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Enemy").objectReferenceValue = definition;
                element.FindPropertyRelative("ZoneId").stringValue = kinds[i].Item2;
            }

            var bossList = new[]
            {
                (GuardianBossId, "Страж Гробниці", TombZoneId),
                (WardenBossId, "Порожній Вартовий", ForestZoneId),
                (CapitalKingBossId, "Король-Заклятий", CapitalZoneId)
            };
            var bosses = serialized.FindProperty("_bosses");
            bosses.arraySize = bossList.Length;
            for (var i = 0; i < bossList.Length; i++)
            {
                var element = bosses.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("BossId").stringValue = bossList[i].Item1;
                element.FindPropertyRelative("DisplayName").stringValue = bossList[i].Item2;
                element.FindPropertyRelative("ZoneId").stringValue = bossList[i].Item3;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        // ---------- trials (stage C) ----------

        public const string ArmsTrialId = ExplorationIdPrefix + "trial-ash-road";

        /// <summary>
        /// The trial of arms on the Ash Road, the capital's empty first stretch: a gong at its
        /// middle, walls that close it at 321 and 343 while it runs, and three waves of enemies the
        /// hero has met on the way — the Tomb's, the Forest's, then the Capital's own. The first
        /// win lays out a health shard and a purse.
        /// </summary>
        private static void BuildArmsTrial(Transform root, GameObject pickup, GameProgress progress)
        {
            var gong = new GameObject("TrialGong");
            gong.transform.SetParent(root);
            gong.transform.position = new Vector2(331.5f, 0f);
            var trigger = gong.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(1.8f, 2.6f);
            trigger.offset = new Vector2(0f, 1.3f);
            GongPart(gong, "Post", new Vector2(0f, 1.1f), new Vector2(0.18f, 2.2f), new Color(0.24f, 0.2f, 0.18f));
            GongPart(gong, "Beam", new Vector2(0f, 2.15f), new Vector2(1.5f, 0.16f), new Color(0.24f, 0.2f, 0.18f));
            GongPart(gong, "Disc", new Vector2(0f, 1.45f), new Vector2(1.05f, 1.05f), new Color(0.72f, 0.5f, 0.24f));

            var barriers = new[]
            {
                CapitalBlock(root, "TrialWallWest", new Vector2(321f, 4f), new Vector2(1f, 8f), CapitalSurface.Masonry),
                CapitalBlock(root, "TrialWallEast", new Vector2(343f, 4f), new Vector2(1f, 8f), CapitalSurface.Masonry)
            };
            foreach (var barrier in barriers)
            {
                Ensure<NonClimbableSurface>(barrier);
                barrier.SetActive(false);
            }

            var trial = Ensure<TrialArena>(gong);
            SetString(trial, "_trialId", ArmsTrialId);
            SetString(trial, "_displayName", "Випробування зброї");
            SetReference(trial, "_progress", progress);
            SetReference(trial, "_registry", Object.FindAnyObjectByType<EnemyRegistry>());

            GameObject Enemy(string name) => AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/{name}.prefab");
            var waves = new[]
            {
                (new[] { Enemy("ZombieSoldier"), Enemy("ZombieSoldier"), Enemy("CorruptedHound") },
                    new[] { new Vector2(325f, 0.9f), new Vector2(338f, 0.9f), new Vector2(340f, 0.9f) }),
                (new[] { Enemy("ShieldGuard"), Enemy("RoyalSpearman"), Enemy("ForestArcher") },
                    new[] { new Vector2(324f, 0.9f), new Vector2(339f, 0.9f), new Vector2(341f, 0.9f) }),
                (new[] { Enemy(GargoylePrefabName), Enemy(RavenPrefabName), Enemy(RavenPrefabName), Enemy(PriestPrefabName) },
                    new[] { new Vector2(333f, 0.9f), new Vector2(325f, 6f), new Vector2(339f, 6.5f), new Vector2(337f, 0.9f) })
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

            SetObjects(serialized.FindProperty("_barriers"), barriers);
            PlacePickup(pickup, new Vector2(330f, 0.7f), progress, VesselShards.HealthPrefix + "trial-ash-road",
                false, 1, ExplorationIdPrefix + "health-trial-ash-road", false);
            PlacePickup(pickup, new Vector2(333f, 0.6f), progress, string.Empty, true, 600,
                ExplorationIdPrefix + "trial-ash-road-purse", true);
            var rewards = new System.Collections.Generic.List<GameObject>();
            foreach (var find in Object.FindObjectsByType<WorldPickup>(FindObjectsInactive.Include))
            {
                if (find.PersistentId != null && find.PersistentId.Contains("trial-ash-road"))
                {
                    find.transform.SetParent(root, true);
                    find.gameObject.SetActive(false);
                    rewards.Add(find.gameObject);
                }
            }

            SetObjects(serialized.FindProperty("_rewards"), rewards.ToArray());
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjects(SerializedProperty property, GameObject[] objects)
        {
            property.arraySize = objects.Length;
            for (var i = 0; i < objects.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
            }
        }

        private static void GongPart(GameObject gong, string name, Vector2 at, Vector2 size, Color color)
        {
            var part = new GameObject(name);
            part.transform.SetParent(gong.transform, false);
            part.transform.localPosition = at;
            part.transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = name == "Disc" && Shape("shape_pickup_relic") != null ? Shape("shape_pickup_relic") : _sprite;
            renderer.sharedMaterial = _material;
            renderer.color = color;
            SetSorting(renderer, "EnvironmentBack", 6);
        }

        /// <summary>A platform of the Thorn Path: its span and its top.</summary>
        private readonly struct Perch
        {
            public readonly float X0;
            public readonly float X1;
            public readonly float Top;

            public Perch(float x0, float x1, float top)
            {
                X0 = x0;
                X1 = x1;
                Top = top;
            }
        }

        /// <summary>
        /// The Thorn Path over the Deep Forest: six perches, each hop asking one thing — a jump,
        /// a second jump, a dash in the air across nine units no jump spans (a jump and a second
        /// jump carry about 7.4), then two more climbs. Under every gap a tray of thorns from
        /// the second perch on (over 5 units up, out of a jump's reach from the road): falling short
        /// puts the hero back on the perch they left, as spikes do in Hollow Knight. The
        /// mana shard waits on the last.
        /// </summary>
        public static readonly Vector4[] ThornPerches =
        {
            new Vector4(214f, 216.5f, 1.6f, 0f),
            new Vector4(219f, 220.5f, 3.2f, 0f),
            new Vector4(223.5f, 225f, 6.2f, 0f),
            new Vector4(234f, 235.5f, 6.2f, 0f),
            new Vector4(238f, 239.5f, 8.6f, 0f),
            new Vector4(242f, 245f, 10.4f, 0f)
        };

        private static void BuildThornCourse(Transform root, GameObject pickup, GameProgress progress)
        {
            for (var i = 0; i < ThornPerches.Length; i++)
            {
                var perch = new Perch(ThornPerches[i].x, ThornPerches[i].y, ThornPerches[i].z);
                var block = ForestBlock(root, $"ThornPerch{i}", new Vector2((perch.X0 + perch.X1) * 0.5f, perch.Top - 0.25f),
                    new Vector2(perch.X1 - perch.X0, 0.5f), ForestSurface.Plank);
                Ensure<NonClimbableSurface>(block);
                if (i == 0)
                {
                    continue;
                }

                // The thorns under the gap before this perch, a little below the lower of the two.
                var previous = ThornPerches[i - 1];
                var from = previous.y;
                var to = perch.X0;
                var trayY = Mathf.Min(previous.z, perch.Top) - 1.2f;
                // Under the first two hops a fall only lands on the forest floor: thorns there
                // would be in reach of an ordinary jump on the main path below.
                if (i <= 2)
                {
                    continue;
                }

                var thorns = new GameObject($"Thorns{i}");
                thorns.transform.SetParent(root);
                thorns.transform.position = new Vector2((from + to) * 0.5f, trayY);
                var spikes = thorns.AddComponent<Spikes>();
                SetVector(spikes, "_size", new Vector2(to - from, 0.4f));
                SetColor(spikes, "_color", new Color(0.36f, 0.3f, 0.22f));
            }

            var last = ThornPerches[ThornPerches.Length - 1];
            PlacePickup(pickup, new Vector2((last.x + last.y) * 0.5f, last.z + 0.7f), progress,
                VesselShards.ManaPrefix + "thorn-course", false, 1, ExplorationIdPrefix + "mana-thorn-course", false);
            foreach (var find in Object.FindObjectsByType<WorldPickup>(FindObjectsInactive.Include))
            {
                if (find.PersistentId == ExplorationIdPrefix + "mana-thorn-course")
                {
                    find.transform.SetParent(root, true);
                }
            }

            AreaTrigger(root, ForestThornCourse, new Vector2((last.x + last.y) * 0.5f, last.z + 1f),
                new Vector2(last.y - last.x - 0.4f, 1.6f));
        }

        /// <summary>Floor, roof, far wall, the breakable wall, the veil over the inside, the shard.</summary>
        private static void BuildSecretRoom(Transform parent, SecretRoom room, GameObject pickup,
            GameProgress progress)
        {
            var name = room.AreaId.Replace("room_", string.Empty);
            var root = Section(parent, name);
            var left = room.X0 - RoomShell;
            var right = room.X1 + RoomShell;
            var width = right - left;
            var middle = (left + right) * 0.5f;
            var top = room.FloorY + RoomHeight;

            var floor = RoomBlock(root, room.Zone, "Floor", new Vector2(middle, room.FloorY - RoomShell * 0.5f),
                new Vector2(width, RoomShell), false);
            var roof = RoomBlock(root, room.Zone, "Roof", new Vector2(middle, top + RoomShell * 0.5f),
                new Vector2(width, RoomShell), false);
            var farX = room.Wall == RoomSide.East ? room.X0 - RoomShell * 0.5f : room.X1 + RoomShell * 0.5f;
            var far = RoomBlock(root, room.Zone, "FarWall", new Vector2(farX, room.FloorY + RoomHeight * 0.5f),
                new Vector2(RoomShell, RoomHeight), true);
            foreach (var shell in new[] { floor, roof, far })
            {
                Ensure<NonClimbableSurface>(shell);
            }

            var nearX = room.Wall == RoomSide.East ? room.X1 + RoomShell * 0.5f : room.X0 - RoomShell * 0.5f;
            var wallObject = RoomBlock(root, room.Zone, "BreakableWall",
                new Vector2(nearX, room.FloorY + RoomHeight * 0.5f), new Vector2(RoomShell, RoomHeight), true);
            Ensure<NonClimbableSurface>(wallObject);
            BuildBreakableWall(wallObject, ExplorationIdPrefix + "wall-" + room.AreaId.Replace("room_", string.Empty),
                Rect.MinMaxRect(room.X0, room.FloorY, room.X1, room.FloorY + RoomHeight), progress, root);

            AreaTrigger(root, room.AreaId, new Vector2((room.X0 + room.X1) * 0.5f, room.FloorY + 1f),
                new Vector2(room.X1 - room.X0 - 0.6f, 1.6f));
            PlaceShard(pickup, root, new ShardSpot(new Vector2((room.X0 + room.X1) * 0.5f, room.FloorY + 0.7f),
                room.Shard, name), progress);
        }

        private static GameObject RoomBlock(Transform parent, int zone, string name, Vector2 centre,
            Vector2 size, bool upright)
        {
            switch (zone)
            {
                case 0: return Block(parent, name, centre, size, upright ? TombSurface.Wall : TombSurface.Platform);
                case 1: return ForestBlock(parent, name, centre, size, upright ? ForestSurface.RootWall : ForestSurface.Ground);
                default: return CapitalBlock(parent, name, centre, size, upright ? CapitalSurface.Masonry : CapitalSurface.Ledge);
            }
        }

        /// <summary>
        /// The block becomes a wall that takes the hero's blows: an actor that cannot die, a
        /// hurtbox on the enemy layer a touch wider than the stone, a faint crack, and the veil.
        /// </summary>
        private static void BuildBreakableWall(GameObject wallObject, string wallId, Rect inside, GameProgress progress,
            Transform roomRoot)
        {
            var height = wallObject.GetComponent<BoxCollider2D>().size.y;
            var actor = Ensure<CombatActor>(wallObject);
            SetFloat(Ensure<Health>(wallObject), "_maxHealth", 100000f);
            SetFloat(actor, "_hitInvulnerability", 0.05f);
            var visual = wallObject.transform.Find("Visual").GetComponent<SpriteRenderer>();
            // A shade warmer and lighter than the stone around it: there, if one looks.
            visual.color = new Color(0.9f, 0.84f, 0.78f);

            var crack = new GameObject("Crack");
            crack.transform.SetParent(wallObject.transform, false);
            crack.transform.localPosition = new Vector3(0.02f, 0.25f, 0f);
            crack.transform.localRotation = Quaternion.Euler(0f, 0f, 18f);
            crack.transform.localScale = new Vector3(0.05f, height * 0.55f, 1f);
            var crackRenderer = crack.AddComponent<SpriteRenderer>();
            crackRenderer.sprite = _sprite;
            crackRenderer.sharedMaterial = _material;
            crackRenderer.color = new Color(0.05f, 0.04f, 0.04f, 0.45f);
            SetSorting(crackRenderer, "GameplayEnvironment", 1);
            crack.transform.SetParent(visual.transform, true);

            var hurtObject = new GameObject("Hurtbox");
            hurtObject.transform.SetParent(wallObject.transform, false);
            hurtObject.layer = EnemyLayer;
            var hurtCollider = hurtObject.AddComponent<BoxCollider2D>();
            hurtCollider.isTrigger = true;
            hurtCollider.size = new Vector2(wallObject.GetComponent<BoxCollider2D>().size.x + 0.3f, height);
            SetReference(hurtObject.AddComponent<Hurtbox>(), "_owner", actor);

            var veil = new GameObject("Veil");
            veil.transform.SetParent(roomRoot, false);
            veil.transform.position = inside.center;
            veil.transform.localScale = new Vector3(inside.width + RoomShell * 2f, inside.height + RoomShell * 2f, 1f);
            var veilRenderer = veil.AddComponent<SpriteRenderer>();
            veilRenderer.sprite = _sprite;
            veilRenderer.sharedMaterial = _material;
            veilRenderer.color = new Color(0.03f, 0.03f, 0.04f, 0.97f);
            SetSorting(veilRenderer, "Foreground", 20);

            var wall = Ensure<BreakableWall>(wallObject);
            SetString(wall, "_wallId", wallId);
            SetReference(wall, "_progress", progress);
            SetReference(wall, "_actor", actor);
            SetReference(wall, "_solid", wallObject.GetComponent<BoxCollider2D>());
            SetReference(wall, "_hurtbox", hurtCollider);
            SetReference(wall, "_visual", visual);
            SetReference(wall, "_veil", veilRenderer);
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
