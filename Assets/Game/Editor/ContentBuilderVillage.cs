using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The Fallen Village built from its text (item 5 of the 15-zone list, the user's map
    /// `reference/maps/03_fallen_village.webp`). It stands over the Ashen Capital: the hill climbs
    /// from the forest's edge, the feather's drop falls into the capital. The layout gives the
    /// timber, the finds, the enemies, the altars and the graph; this file adds what only the
    /// Village has — the Bellringer under his bell, the sealed road to the marshes, the hints at
    /// the grapple's rings, and each hall's own dressing, sky or walls and warm lights. One root,
    /// <c>Level/FallenVillage</c>, replaced whole on every build.
    /// </summary>
    public static partial class ContentBuilder
    {
        public const string VillageLayoutFile = "village.txt";
        public const string VillageRootName = "FallenVillage";
        public const string VillageBellRoom = "room_village_bell";
        public const string BellringerRewardItemId = "bell_clapper";

        private static Dictionary<string, Sprite> _village;
        private static LevelZone _villageLayout;

        /// <summary>The Village's layout, read once per build.</summary>
        public static LevelZone VillageLayout => _villageLayout ?? (_villageLayout = LoadLayout(VillageLayoutFile));

        /// <summary>Where a named spot of the Village's layout stands, on its floor.</summary>
        public static Vector2 VillageSpot(string name) => SpotFeet(VillageLayout, name);

        private static Sprite VillageArt(string name) =>
            _village != null && _village.TryGetValue(name, out var sprite) ? sprite : null;

        /// <summary>The rooms under a sky: the rest are inside a house, a cellar or the church.</summary>
        public static readonly HashSet<string> VillageOpenRooms = new HashSet<string>
        {
            "room_village_hill", "room_village_lower_street", "room_village_feather", "room_village_square",
            "room_village_stilt_houses", "room_village_upper_houses", "room_village_upper_street",
            "room_village_marsh_road", VillageBellRoom
        };

        /// <summary>The rooms of stone: the church, its crypt, the belfry, the cellars.</summary>
        private static readonly HashSet<string> VillageStoneRooms = new HashSet<string>
        {
            "room_village_church", "room_village_crypt_cache", "room_village_belfry_base", "room_village_bell_shaft",
            "room_village_cellar", "room_village_well_cache", "room_village_bridge", "room_village_stair"
        };

        /// <summary>
        /// The whole Village under <paramref name="level"/>. Runs in the generator's new scene and in
        /// a world opened whole (<see cref="SceneVillage"/>); everything it hooks up to is found there.
        /// </summary>
        public static BossEncounter BuildVillage(Transform level)
        {
            _villageLayout = null;
            _village = PlaceholderVillageArt.Generate();
            if (_shapes == null) { _shapes = ProductionSpriteArt.Generate(); }
            if (_sprite == null) { _sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath); }
            if (_material == null) { _material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath); }

            var old = level.Find(VillageRootName);
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            var zone = VillageLayout;
            var root = Section(level, VillageRootName);
            var context = LevelContext.FromScene();
            foreach (var problem in BuildLayout(zone, root, VillageLayoutBlock, context))
            {
                Debug.LogError($"Village layout: {problem}");
            }

            var rooms = new Dictionary<string, Transform>();
            foreach (var room in zone.Rooms)
            {
                rooms[room.Id] = root.Find(room.Id);
            }

            var encounter = BuildBellringerFloor(zone, rooms[VillageBellRoom], context);
            BuildMarshGate(zone, rooms["room_village_marsh_road"], context.Progress);
            BuildVillageGateHints(zone, rooms, context.Progress);
            BuildNightBellTrial(rooms["room_village_square"], context.Progress);
            foreach (var room in zone.Rooms)
            {
                DressVillageRoom(zone, room, rooms[room.Id]);
            }

            return encounter;
        }

        /// <summary>How the Village's ground looks: planks on the ledges, cobbles on the square, timber or stone walls.</summary>
        private static GameObject VillageLayoutBlock(Transform parent, string name, Rect world, LevelCell kind)
        {
            var stone = VillageStoneRooms.Contains(parent.name);
            Sprite tile;
            if (kind == LevelCell.Ledge)
            {
                var seed = Mathf.Abs(Mathf.RoundToInt(world.x * 7f + world.y * 13f));
                tile = VillageArt(seed % 3 == 0 ? "env_village_ledge_broken_01" : "env_village_ledge_01");
            }
            else if (kind == LevelCell.Smooth || world.height > world.width)
            {
                tile = VillageArt(stone ? "env_village_stone_01" : "env_village_timber_01");
            }
            else
            {
                tile = VillageArt(stone ? "env_village_stone_01"
                    : parent.name == "room_village_square" ? "env_village_cobble_01" : "env_village_planks_01");
            }

            return VillageBlock(parent, name, world.center, world.size, tile);
        }

        /// <summary>A solid piece of village: the collider from the gameplay size, the art fitted to it.</summary>
        private static GameObject VillageBlock(Transform parent, string name, Vector2 centre, Vector2 size, Sprite tile)
        {
            var block = new GameObject(name);
            block.transform.SetParent(parent);
            block.transform.position = centre;
            block.layer = GroundLayer;
            var collider = block.AddComponent<BoxCollider2D>();
            collider.size = size;
            var visual = FindOrCreateChild(block, "Visual");
            var renderer = visual.AddComponent<SpriteRenderer>();
            if (tile != null)
            {
                renderer.sprite = tile;
                renderer.drawMode = SpriteDrawMode.Tiled;
                renderer.tileMode = SpriteTileMode.Continuous;
                renderer.size = size;
            }
            else
            {
                visual.transform.localScale = new Vector3(size.x, size.y, 1f);
                renderer.sprite = _sprite;
                renderer.sharedMaterial = _material;
                renderer.color = new Color(0.28f, 0.2f, 0.15f);
            }

            SetSorting(renderer, "GameplayEnvironment", 0);
            return block;
        }

        private static GameObject VillageProp(Transform parent, string spriteName, Vector2 position, string layer,
            int order, float scale = 1f, bool flip = false, float alpha = 1f)
        {
            var sprite = VillageArt(spriteName);
            if (sprite == null)
            {
                return null;
            }

            var prop = new GameObject(spriteName);
            prop.transform.SetParent(parent);
            prop.transform.position = position;
            prop.transform.localScale = new Vector3(flip ? -scale : scale, scale, 1f);
            var renderer = prop.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(1f, 1f, 1f, alpha);
            SetSorting(renderer, layer, order);
            return prop;
        }

        // ---------- the Bellringer ----------

        /// <summary>
        /// The Bellringer under his bell; the floor's hatch, the way up the belfry, is shut behind the
        /// hero when the fight starts and opens again when it ends. His death hands over the grapple.
        /// </summary>
        private static BossEncounter BuildBellringerFloor(LevelZone zone, Transform floor, LevelContext context)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/{BellringerPrefabName}.prefab");
            if (prefab == null)
            {
                Debug.LogError("No Bellringer prefab; the Village has no boss.");
                return null;
            }

            var boss = (GameObject)PrefabUtility.InstantiatePrefab(prefab, floor);
            boss.transform.position = VillageSpot("bellringer") + new Vector2(0f, 1.3f);

            // The hatch: from the bell's room's west wall to the door spot, two cells of floor.
            var door = VillageSpot("bell-door");
            var room = zone.WorldBounds(zone.Room(VillageBellRoom));
            var cell = zone.CellSize;
            var hatch = Rect.MinMaxRect(room.xMin + cell * 2f, door.y - cell * 2f, door.x - cell * 0.5f, door.y);
            var barrier = VillageBlock(floor, "BellHatchSeal", hatch.center, hatch.size, VillageArt("env_village_planks_01"));
            barrier.SetActive(false);

            var encounterObject = new GameObject("BellringerEncounter");
            encounterObject.transform.SetParent(floor);
            encounterObject.transform.position = door + new Vector2(4f, 2f);
            var trigger = encounterObject.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(3f, 6f);
            var encounter = encounterObject.AddComponent<BossEncounter>();
            SetReference(encounter, "_boss", boss.GetComponent<BossController>());
            SetReference(encounter, "_barrier", barrier);
            SetReference(encounter, "_progress", context.Progress);
            SetReference(encounter, "_respawner", context.Respawner);
            SetReference(encounter, "_progression", Object.FindAnyObjectByType<PlayerProgressionService>());
            SetString(encounter, "_rewardId", BellringerRewardItemId);
            SetString(encounter, "_rewardName", "Язик дзвона");
            SetInt(encounter, "_rewardCurrency", 1000);
            SetInt(encounter, "_rewardExperience", 1700);
            // The grapple: the way on to the capital, granted inside the kill's own guard.
            SetString(encounter, "_rewardAbilityId", AbilityIds.Grapple);

            var inside = Inside(zone, zone.Room(VillageBellRoom));
            var bell = VillageProp(floor, "env_village_bell_01", new Vector2(VillageSpot("bell").x, inside.yMax - 4.2f),
                "EnvironmentBack", 4, 1.1f);
            if (bell != null)
            {
                bell.name = "Bell";
            }

            var lights = Section(floor, "BellLights");
            foreach (var share in new[] { 0.3f, 0.7f })
            {
                var host = LightChild(lights, "BellLight", new Vector2(Mathf.Lerp(inside.xMin, inside.xMax, share), inside.yMin + 8f));
                AddLight(host, Light2D.LightType.Point, new Color(1f, 0.78f, 0.52f), 0.95f, 11f, 1.5f, 0.45f);
            }

            return encounter;
        }

        /// <summary>The road to the Plague Marsh (item 7): barred until that zone exists.</summary>
        private static void BuildMarshGate(LevelZone zone, Transform road, GameProgress progress)
        {
            var cell = zone.CellSize;
            var feet = VillageSpot("marsh-gate");
            var wall = VillageBlock(road, "MarshRoadSeal", feet + new Vector2(cell * 0.5f, cell * 5f),
                new Vector2(cell * 2f, cell * 10f), VillageArt("env_village_timber_01"));
            Ensure<NonClimbableSurface>(wall);
            PlaceDoor(road, "MarshRoadGate", wall.transform.position, RequirementKind.Unavailable, string.Empty, wall, progress);
        }

        private static readonly Dictionary<string, string> VillageHintSpots = new Dictionary<string, string>
        {
            ["room_village_feather"] = "feather-hint",
            ["room_village_church"] = "church-hint",
            ["room_village_forge_loft"] = "forge-hint"
        };

        private static void BuildVillageGateHints(LevelZone zone, Dictionary<string, Transform> rooms, GameProgress progress)
        {
            foreach (var link in zone.Links)
            {
                if (string.IsNullOrEmpty(link.Ability) || !VillageHintSpots.TryGetValue(link.To, out var spot))
                {
                    continue;
                }

                PlaceAbilityGate(rooms[link.From], $"{Short(link.To)}_Hint", VillageSpot(spot) + new Vector2(0f, 0.8f),
                    link.Ability, LinkId(link), Inside(zone, zone.Room(link.To)).center, progress);
            }
        }

        // ---------- dressing ----------

        private static Dress VF(string sprite, float x, float scale = 1f, bool flip = false) =>
            new Dress("env_village_" + sprite, x, -1f, scale, flip);

        private static Dress VH(string sprite, float x, float y, float scale = 1f, bool flip = false) =>
            new Dress("env_village_" + sprite, x, y, scale, flip);

        /// <summary>Each hall its own set: the same pieces never in the same arrangement, size or tone.</summary>
        private static readonly Dictionary<string, Dress[]> VillageDressing = new Dictionary<string, Dress[]>
        {
            ["room_village_hill"] = new[] { VF("tree_01", 0.15f, 1.2f), VF("fence_01", 0.5f, 0.9f), VF("cross_01", 0.82f), VF("lantern_01", 0.92f) },
            ["room_village_hay_barn"] = new[] { VF("hay_01", 0.3f, 0.9f), VF("cart_01", 0.75f, 0.8f) },
            ["room_village_lower_street"] = new[]
            {
                VF("house_01", 0.24f, 1.1f), VF("fence_01", 0.55f), VF("lantern_01", 0.68f), VF("barrel_01", 0.86f, 0.8f),
                VH("garland_01", 0.45f, 0.85f)
            },
            ["room_village_feather"] = new[] { VF("cross_01", 0.25f, 0.8f, true) },
            ["room_village_market"] = new[]
            {
                VF("stall_01", 0.12f), VF("stall_01", 0.38f, 0.9f, true), VF("barrel_01", 0.52f, 0.9f), VF("cart_01", 0.66f),
                VF("lantern_01", 0.8f, 0.9f), VH("garland_01", 0.3f, 0.8f, 1.1f)
            },
            ["room_village_well_cache"] = new[] { VF("barrel_01", 0.6f, 0.7f) },
            ["room_village_cellar"] = new[] { VF("barrel_01", 0.1f), VF("barrel_01", 0.46f, 1.2f, true), VF("cart_01", 0.78f, 0.9f) },
            ["room_village_bridge"] = new[] { VF("fence_01", 0.3f, 1.1f), VF("lantern_01", 0.62f, 1.1f), VF("fence_01", 0.8f, 0.8f, true) },
            ["room_village_stair"] = new[] { VH("garland_01", 0.5f, 0.6f, 0.4f) },
            ["room_village_church"] = new[] { VF("church_01", 0.62f, 1.3f), VF("cross_01", 0.2f), VF("cross_01", 0.88f, 0.9f, true) },
            ["room_village_crypt_cache"] = new[] { VF("cross_01", 0.5f, 0.7f) },
            ["room_village_forge"] = new[] { VF("forge_01", 0.62f, 1.1f), VF("barrel_01", 0.3f, 0.8f), VF("cart_01", 0.86f, 0.7f, true) },
            ["room_village_forge_loft"] = new[] { VF("hay_01", 0.7f, 0.7f, true), VF("barrel_01", 0.25f, 0.6f) },
            ["room_village_square"] = new[]
            {
                VF("house_01", 0.08f, 1.3f, true), VF("tree_01", 0.26f, 1.1f), VF("well_01", 0.52f, 1.2f), VF("tree_01", 0.7f, 0.9f, true),
                VF("house_02", 0.9f, 1.2f), VF("lantern_01", 0.4f), VH("garland_01", 0.6f, 0.62f, 1.3f)
            },
            ["room_village_stilt_houses"] = new[]
            {
                VF("house_02", 0.2f, 1.2f, true), VF("house_02", 0.62f, 1.05f), VF("lantern_01", 0.86f), VF("fence_01", 0.42f, 0.8f)
            },
            ["room_village_belfry_base"] = new[] { VF("lantern_01", 0.15f), VF("cross_01", 0.8f, 0.8f) },
            ["room_village_east_lane"] = new[] { VF("barrel_01", 0.3f, 0.7f), VF("fence_01", 0.7f, 0.7f) },
            ["room_village_east_cache"] = new[] { VF("hay_01", 0.5f, 0.6f) },
            ["room_village_marsh_road"] = new[] { VF("tree_01", 0.3f, 1f, true), VF("cross_01", 0.6f, 0.9f) },
            ["room_village_bell_shaft"] = new[] { VH("garland_01", 0.5f, 0.9f, 0.4f) },
            ["room_village_bell"] = new[] { VF("lantern_01", 0.08f, 1.2f), VF("lantern_01", 0.92f, 1.2f, true) },
            ["room_village_upper_houses"] = new[]
            {
                VF("house_01", 0.2f, 1.1f), VF("house_02", 0.7f, 1f, true), VF("lantern_01", 0.45f, 0.9f), VH("garland_01", 0.5f, 0.85f)
            },
            ["room_village_attic"] = new[] { VF("hay_01", 0.4f, 0.6f), VF("barrel_01", 0.78f, 0.5f) },
            ["room_village_upper_street"] = new[]
            {
                VF("house_01", 0.12f, 0.95f, true), VF("tree_01", 0.35f, 0.8f), VF("fence_01", 0.55f), VF("house_02", 0.78f, 0.9f),
                VF("lantern_01", 0.95f, 0.9f)
            }
        };

        /// <summary>A tone per room, so the same timber never reads as the same room twice.</summary>
        private static Color VillageRoomTone(string roomId)
        {
            var hash = 0;
            foreach (var c in roomId)
            {
                hash = hash * 31 + c;
            }

            var r = (hash & 0xff) / 255f;
            var g = ((hash >> 8) & 0xff) / 255f;
            return new Color(0.64f + r * 0.14f, 0.54f + g * 0.12f, 0.46f + (1f - r) * 0.08f);
        }

        private static void DressVillageRoom(LevelZone zone, LevelRoom room, Transform parent)
        {
            var inside = Inside(zone, room);
            var dressing = Section(parent, "Dressing");
            BuildVillageBackdrop(room, inside, dressing);
            if (VillageDressing.TryGetValue(room.Id, out var pieces))
            {
                foreach (var piece in pieces)
                {
                    var x = Mathf.Lerp(inside.xMin + 0.6f, inside.xMax - 0.6f, piece.X);
                    var at = piece.Y < 0f
                        ? new Vector2(x, FloorUnder(zone, room, x))
                        : new Vector2(x, Mathf.Lerp(inside.yMin, inside.yMax, piece.Y));
                    var front = piece.Sprite.Contains("lantern") || piece.Sprite.Contains("barrel")
                        || piece.Sprite.Contains("fence") || piece.Sprite.Contains("cross");
                    var prop = VillageProp(dressing, piece.Sprite, at, front ? "GameplayEnvironment" : "EnvironmentBack",
                        front ? 1 : 2, piece.Scale, piece.Flip);
                    if (prop == null)
                    {
                        continue;
                    }

                    if (!front)
                    {
                        prop.GetComponent<SpriteRenderer>().color = Color.Lerp(Color.white, VillageRoomTone(room.Id), 0.35f);
                    }

                    var glow = piece.Sprite.Contains("lantern") ? (Vector2?)new Vector2(0.38f * piece.Scale, 2.2f * piece.Scale)
                        : piece.Sprite.Contains("forge") ? new Vector2(-0.9f * piece.Scale, 1.1f * piece.Scale)
                        : piece.Sprite.Contains("church") ? new Vector2(0f, 3.8f * piece.Scale)
                        : (Vector2?)null;
                    if (glow.HasValue)
                    {
                        var host = LightChild(prop.transform, "WarmLight", at + glow.Value);
                        host.transform.localScale = Vector3.one;
                        AddLight(host, Light2D.LightType.Point, new Color(1f, 0.66f, 0.34f), 0.85f, 4.2f, 0.3f, 0.6f);
                        host.AddComponent<LightFlicker>();
                    }
                }
            }
        }

        /// <summary>The sky over the streets, the walls of the houses, the stone of the church and the cellars.</summary>
        private static void BuildVillageBackdrop(LevelRoom room, Rect inside, Transform parent)
        {
            var open = VillageOpenRooms.Contains(room.Id);
            var art = VillageArt(open ? "env_village_sky_01"
                : VillageStoneRooms.Contains(room.Id) ? "env_village_cellar_01" : "env_village_wall_01");
            if (art == null)
            {
                return;
            }

            var tone = VillageRoomTone(room.Id) * (open ? 0.9f : 0.6f);
            tone.a = 1f;
            // The sky tiles only sideways: one row of far roofs along the floor, and above it the dark
            // the sky fades into, so a tall room never shows the roofs again in mid-air.
            var height = open ? Mathf.Min(inside.height, art.bounds.size.y) : inside.height;
            var back = new GameObject("Backdrop");
            back.transform.SetParent(parent, false);
            back.transform.position = new Vector2(inside.center.x, inside.yMin + height * 0.5f);
            var renderer = back.AddComponent<SpriteRenderer>();
            renderer.sprite = art;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = new Vector2(inside.width, height);
            renderer.color = tone;
            renderer.sharedMaterial = _material;
            SetSorting(renderer, "BackgroundMid", 0);
            if (open && inside.height > height)
            {
                var above = new GameObject("SkyAbove");
                above.transform.SetParent(parent, false);
                above.transform.position = new Vector2(inside.center.x, inside.yMin + height + (inside.height - height) * 0.5f);
                above.transform.localScale = new Vector3(inside.width, inside.height - height, 1f);
                var fill = above.AddComponent<SpriteRenderer>();
                fill.sprite = _sprite;
                fill.sharedMaterial = _material;
                fill.color = new Color(0.078f * tone.r, 0.063f * tone.g, 0.063f * tone.b, 1f);
                SetSorting(fill, "BackgroundMid", 0);
            }
        }

        // ---------- light and air ----------

        /// <summary>The Village's own lights when it is rebuilt in a finished world: altars and the Bellringer.</summary>
        public static void LightVillage(Transform village)
        {
            BuildCheckpointLights(village);
            BuildBossLights(village);
        }

        /// <summary>The Village's ambient light and its sounds: warm, dim, a dog somewhere, the wind.</summary>
        public static void ConfigureVillageAtmosphere()
        {
            var lighting = Object.FindAnyObjectByType<ZoneLighting>(FindObjectsInactive.Include);
            if (lighting != null)
            {
                var serialized = new SerializedObject(lighting);
                var entry = UpsertZone(serialized.FindProperty("_zones"), "_zoneId", VillageZoneId);
                WriteZoneLight(entry, VillageZoneId, new Color(0.86f, 0.7f, 0.56f), 0.8f);
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            var ambience = Object.FindAnyObjectByType<AmbienceController>(FindObjectsInactive.Include);
            if (ambience != null)
            {
                var serialized = new SerializedObject(ambience);
                var palette = UpsertZone(serialized.FindProperty("_palettes"), "_zoneId", VillageZoneId);
                palette.FindPropertyRelative("_zoneId").stringValue = VillageZoneId;
                palette.FindPropertyRelative("_bed").enumValueIndex = (int)AudioId.AmbienceForestBase;
                var details = palette.FindPropertyRelative("_details");
                var sounds = new[] { AudioId.AmbienceBellToll, AudioId.AmbienceEmbers, AudioId.AmbienceDistantBeast,
                    AudioId.AmbienceBranch, AudioId.AmbienceWind };
                details.arraySize = sounds.Length;
                for (var i = 0; i < sounds.Length; i++)
                {
                    details.GetArrayElementAtIndex(i).enumValueIndex = (int)sounds[i];
                }

                palette.FindPropertyRelative("_minimumGap").floatValue = 6f;
                palette.FindPropertyRelative("_maximumGap").floatValue = 17f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>A zone's entry of a zone list, made if it is not there yet.</summary>
        private static SerializedProperty UpsertZone(SerializedProperty list, string idField, string zoneId)
        {
            for (var i = 0; i < list.arraySize; i++)
            {
                var entry = list.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative(idField).stringValue == zoneId)
                {
                    return entry;
                }
            }

            list.arraySize++;
            var added = list.GetArrayElementAtIndex(list.arraySize - 1);
            added.FindPropertyRelative(idField).stringValue = zoneId;
            return added;
        }
    }
}
