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
            new ShardSpot(new Vector2(14.1f, 7.7f), VesselKind.Health, "tomb-ossuary"),
            new ShardSpot(new Vector2(-6.2f, 10.9f), VesselKind.Mana, "tomb-bell-loft"),
            new ShardSpot(new Vector2(4.5f, 4.5f), VesselKind.Mana, "tomb-reliquary"),
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
            // West of the bell loft (-6.8..-2.8, 10.4), under the mason's shelf.
            new SecretRoom(TombWalledCell, 0, -11.3f, -7.3f, 10.4f, RoomSide.East, VesselKind.Health),
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

            foreach (var find in Object.FindObjectsByType<WorldPickup>(FindObjectsInactive.Include))
            {
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
            BuildBreakableWall(wallObject, room, progress, root);

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
        private static void BuildBreakableWall(GameObject wallObject, SecretRoom room, GameProgress progress,
            Transform roomRoot)
        {
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
            crack.transform.localScale = new Vector3(0.05f, RoomHeight * 0.55f, 1f);
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
            hurtCollider.size = new Vector2(RoomShell + 0.3f, RoomHeight);
            SetReference(hurtObject.AddComponent<Hurtbox>(), "_owner", actor);

            var veil = new GameObject("Veil");
            veil.transform.SetParent(roomRoot, false);
            veil.transform.position = new Vector3((room.X0 + room.X1) * 0.5f, room.FloorY + RoomHeight * 0.5f, 0f);
            veil.transform.localScale = new Vector3(room.X1 - room.X0 + RoomShell * 2f, RoomHeight + RoomShell * 2f, 1f);
            var veilRenderer = veil.AddComponent<SpriteRenderer>();
            veilRenderer.sprite = _sprite;
            veilRenderer.sharedMaterial = _material;
            veilRenderer.color = new Color(0.03f, 0.03f, 0.04f, 0.97f);
            SetSorting(veilRenderer, "Foreground", 20);

            var wall = Ensure<BreakableWall>(wallObject);
            SetString(wall, "_wallId", ExplorationIdPrefix + "wall-" + room.AreaId.Replace("room_", string.Empty));
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
