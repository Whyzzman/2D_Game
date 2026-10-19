using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The Withered Forest built from its text (item 4 of the 15-zone list, the user's map
    /// `reference/maps/02_withered_forest.webp`): the layout gives the ground, the finds, the
    /// enemies, the altars and the graph; this file adds what only the Forest has — the Tomb's
    /// door, the Warden and his gates, the barred arch under the old road, the poison plants, the
    /// hints at the ability gates and each hall's own dressing. One root, <c>Level/WitheredForest</c>,
    /// replaced whole on every build.
    /// </summary>
    public static partial class ContentBuilder
    {
        public const string ForestLayoutFile = "forest.txt";
        public const string ForestRootName = "WitheredForest";
        public const string ForestArenaRoom = "room_forest_warden_arena";
        private const string WardenPrefabPath = PrefabFolder + "/HollowWarden.prefab";

        private static LevelZone _forestLayout;

        /// <summary>The Forest's layout, read once per build.</summary>
        public static LevelZone ForestLayout => _forestLayout ?? (_forestLayout = LoadLayout(ForestLayoutFile));

        /// <summary>Where a named spot of the Forest's layout stands, on its floor.</summary>
        public static Vector2 ForestSpot(string name) => SpotFeet(ForestLayout, name);

        /// <summary>A room of the Forest in the world, walls included.</summary>
        public static Rect ForestRoomBounds(string roomId) => ForestLayout.WorldBounds(ForestLayout.Room(roomId));

        /// <summary>
        /// The rooms under the ground or shut in: they get a wall of their own behind them. The
        /// rest are open to the sky, and the forest's parallax shows through them.
        /// </summary>
        public static readonly HashSet<string> ForestEnclosedRooms = new HashSet<string>
        {
            "room_forest_root_tunnel", "room_forest_gate_arch", "room_forest_old_graves",
            "room_forest_stone_bridge", "room_forest_grove_stash", "room_forest_hunter_hollow",
            "room_forest_hollow_trunk", "room_forest_falconer_nest", "room_forest_under_walkway",
            "room_forest_root_nest", "room_forest_ravine", "room_forest_ravine_foot", "room_forest_deep_roots",
            "room_forest_tree_grave", "room_forest_lower_gallery", "room_forest_thorn_course",
            "room_forest_mushroom_pool", "room_forest_mushroom_cache", "room_forest_falconer_ladder",
            "room_forest_poison_grove"
        };

        /// <summary>The Forest's painted art (item 4 of the zone rework, approved look — design/levels/forest-look).</summary>
        public const string ForestArtFolder = "Forest";

        private static bool _forestPainted;

        /// <summary>Below this the forest is underground: earth stands behind it, not the sky.</summary>
        public const float ForestGroundLine = -5.5f;

        /// <summary>
        /// The whole Forest under <paramref name="level"/>. Runs in the generator's new scene and in a
        /// world opened whole (<see cref="SceneForest"/>); everything it hooks up to is found there.
        /// </summary>
        public static BossEncounter BuildForest(Transform level)
        {
            _forestLayout = null;
            if (_forest == null) { _forest = PlaceholderForestArt.Generate(); }
            if (_tomb == null) { _tomb = PlaceholderTombArt.Generate(); }
            if (_shapes == null) { _shapes = ProductionSpriteArt.Generate(); }
            if (_sprite == null) { _sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath); }
            if (_material == null) { _material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath); }

            var old = level.Find(ForestRootName);
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            var zone = ForestLayout;
            var root = Section(level, ForestRootName);
            var context = LevelContext.FromScene();
            _forestPainted = HasPaintedArt(ForestArtFolder);
            foreach (var problem in BuildLayout(zone, root, ForestLayoutBlock, context))
            {
                Debug.LogError($"Forest layout: {problem}");
            }

            var rooms = new Dictionary<string, Transform>();
            foreach (var room in zone.Rooms)
            {
                rooms[room.Id] = root.Find(room.Id);
            }

            BuildTombDoor(level, rooms["room_forest_root_tunnel"], context.Progress);
            var encounter = BuildWardenArena(zone, rooms[ForestArenaRoom], context);
            BuildForestExits(zone, rooms, context.Progress);
            BuildRoadGate(zone, rooms["room_forest_gate_arch"], rooms["room_forest_broken_arch"], context.Progress);
            BuildForestGateHints(zone, rooms, context.Progress);
            var plant = AssetDatabase.LoadAssetAtPath<GameObject>(PoisonPlantPrefabPath);
            foreach (var marker in zone.Markers())
            {
                if (marker.Kind == "spot" && marker.Arg(0) == "plant")
                {
                    PlacePlant(rooms[marker.RoomId], plant, MarkerFeet(zone, marker) + new Vector2(0f, 0.3f));
                }
            }

            if (_forestPainted)
            {
                // The painter draws the ground, the rooms' backdrops and the sky behind them.
                PlacePaintedArt(ForestArtFolder, zone, root);
                return encounter;
            }

            foreach (var room in zone.Rooms)
            {
                DressForestRoom(zone, room, rooms[room.Id]);
            }

            BuildForestEarth(zone, root);
            var camera = Camera.main != null ? Camera.main.transform : null;
            BuildForestParallax(root, camera);
            return encounter;
        }

        /// <summary>How the Forest's ground looks: ledges as planks, the road as road, the rest by its shape.</summary>
        private static GameObject ForestLayoutBlock(Transform parent, string name, Rect world, LevelCell kind)
        {
            if (_forestPainted)
            {
                return PaintedSolid(parent, name, world, ForestArtFolder, ForestLayout);
            }

            ForestSurface surface;
            if (kind == LevelCell.Ledge)
            {
                // Every third plank rotten, picked by place so a rebuild draws the same ones.
                var seed = Mathf.Abs(Mathf.RoundToInt(world.x * 7f + world.y * 13f));
                surface = seed % 3 == 0 ? ForestSurface.BrokenPlank : ForestSurface.Plank;
            }
            else if (kind == LevelCell.Smooth)
            {
                surface = ForestSurface.RootWall;
            }
            else if (world.height > world.width)
            {
                surface = ForestSurface.Cliff;
            }
            else
            {
                surface = parent.name == "room_forest_royal_road" ? ForestSurface.Road : ForestSurface.Ground;
            }

            return ForestBlock(parent, name, world.center, world.size, surface);
        }

        // ---------- doors ----------

        /// <summary>
        /// The Tomb's far wall is the forest's first door: solid until the Guardian is recorded as
        /// beaten (a save that beat him before this zone existed included), then open for good.
        /// </summary>
        private static void BuildTombDoor(Transform level, Transform tunnel, GameProgress progress)
        {
            foreach (var stale in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (stale != null && (stale.name == "TombExitWall" || stale.name == "TombExitGate"))
                {
                    Object.DestroyImmediate(stale.gameObject);
                }
            }

            var wall = Block(tunnel, "TombExitWall", new Vector2(62.5f, 3f), new Vector2(1f, 8f), TombSurface.Wall);
            PlaceDoor(tunnel, "TombExitGate", new Vector2(62.5f, 3f), RequirementKind.BossDefeated,
                GuardianBossId, wall, progress);
        }

        /// <summary>
        /// The Warden in the middle of his clearing, the trigger a few steps past the door, and the
        /// roots that close the door behind the hero (shut until the fight starts).
        /// </summary>
        private static BossEncounter BuildWardenArena(LevelZone zone, Transform arena, LevelContext context)
        {
            var bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WardenPrefabPath);
            if (bossPrefab == null)
            {
                Debug.LogError("No Warden prefab; the Forest has no boss.");
                return null;
            }

            var boss = (GameObject)PrefabUtility.InstantiatePrefab(bossPrefab, arena);
            boss.transform.position = ForestSpot("warden") + new Vector2(0f, 1.2f);

            var door = ForestSpot("arena-door");
            var barrier = DoorBlock(zone, arena, "WardenBarrier", door, 10);
            barrier.SetActive(false);

            var encounterObject = new GameObject("WardenEncounter");
            encounterObject.transform.SetParent(arena);
            encounterObject.transform.position = door + new Vector2(4.5f, 2f);
            var trigger = encounterObject.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(3f, 6f);
            var encounter = encounterObject.AddComponent<BossEncounter>();
            SetReference(encounter, "_boss", boss.GetComponent<BossController>());
            SetReference(encounter, "_barrier", barrier);
            SetReference(encounter, "_progress", context.Progress);
            SetReference(encounter, "_respawner", context.Respawner);
            SetReference(encounter, "_progression", Object.FindAnyObjectByType<PlayerProgressionService>());
            SetString(encounter, "_rewardId", "warden_sigil");
            SetString(encounter, "_rewardName", "Печать Вартового");
            SetInt(encounter, "_rewardCurrency", 900);
            SetInt(encounter, "_rewardExperience", 1400);
            // The reward that matters: the second jump, inside the same once-only guard as the coins.
            SetString(encounter, "_rewardAbilityId", AbilityIds.DoubleJump);

            var inside = Inside(zone, zone.Room(ForestArenaRoom));
            var dressing = Section(arena, "ArenaDressing");
            var backdrop = ForestProp(dressing, SceneForestLook.ArenaBackdropName,
                new Vector2(inside.center.x, inside.yMin - 2f), "EnvironmentBack", 0);
            if (backdrop != null)
            {
                backdrop.transform.localScale = new Vector3(2f, 2f, 1f);
            }

            ForestProp(dressing, "env_forest_arena_crest", new Vector2(inside.center.x + 4f, inside.yMin + 0.1f),
                "GameplayEnvironment", 1, 1f, alpha: 0.8f);
            var lights = Section(dressing, "ArenaLights");
            foreach (var share in new[] { 0.3f, 0.72f })
            {
                var host = LightChild(lights, "ArenaLight",
                    new Vector2(Mathf.Lerp(inside.xMin, inside.xMax, share), inside.yMin + 9f));
                AddLight(host, Light2D.LightType.Point, new Color(0.72f, 0.88f, 0.92f), 1f, 11f, 1.5f, 0.45f);
            }

            return encounter;
        }

        /// <summary>
        /// The Warden's door out of his clearing, and the old road from the forest's edge into the
        /// capital: barred, with the winch on the capital's side (the way in is through the Village
        /// now, item 5 of the 15 zones), so it is the short way back, not the way in.
        /// </summary>
        private static void BuildForestExits(LevelZone zone, Dictionary<string, Transform> rooms, GameProgress progress)
        {
            var exit = DoorBlock(zone, rooms[ForestArenaRoom], "ForestExitWall", ForestSpot("exit-gate"), 10);
            PlaceDoor(rooms[ForestArenaRoom], "ForestExitGate", exit.transform.position, RequirementKind.BossDefeated,
                WardenBossId, exit, progress);

            // Bars and winch both just past the forest's last cell (318), on the capital's road, so
            // the split puts them in one scene and the winch keeps its hold on the bars.
            var edge = rooms["room_forest_far_gate"];
            var bars = DoorBlock(zone, edge, CapitalRoadGateName, ForestSpot("capital-seal") + new Vector2(2f, 0f), 10);
            var art = Object.Instantiate(bars.transform.Find("Visual").gameObject, edge);
            art.name = CapitalRoadGateName + "Bars";
            art.transform.position = bars.transform.position;
            bars.transform.Find("Visual").gameObject.SetActive(false);

            // On the capital's road, a step past the bars; the split puts it in the capital by place.
            var winchAt = new Vector2(ForestSpot("capital-seal").x + 3.5f, ForestSpot("capital-seal").y);
            var winch = new GameObject("CapitalRoadWinch");
            winch.transform.SetParent(edge);
            winch.transform.position = winchAt + new Vector2(0f, 0.9f);
            var trigger = winch.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 0.8f;
            var handle = ForestProp(winch.transform, "env_forest_stump_01", winchAt, "GameplayEnvironment", 3, 0.6f);
            var shortcut = winch.AddComponent<Shortcut>();
            SetString(shortcut, "_shortcutId", CapitalRoadShortcut);
            SetString(shortcut, "_displayName", "лебідка старої дороги");
            SetReference(shortcut, "_progress", progress);
            art.transform.position += new Vector3(0f, 5f, 0f);
            ConfigureShortcut(shortcut, System.Array.Empty<GameObject>(), new[] { bars },
                art.transform, new Vector3(0f, -5f, 0f), handle != null ? handle.transform : null);
        }

        /// <summary>Roots grown across a doorway: two cells wide from the spot, <paramref name="cells"/> high.</summary>
        private static GameObject DoorBlock(LevelZone zone, Transform parent, string name, Vector2 feet, int cells)
        {
            var cell = zone.CellSize;
            var block = ForestBlock(parent, name, feet + new Vector2(cell * 0.5f, cell * cells * 0.5f),
                new Vector2(cell * 2f, cell * cells), ForestSurface.RootWall);
            Ensure<NonClimbableSurface>(block);
            return block;
        }

        /// <summary>
        /// The Forest's shortcut (every zone has one, opened from its far side): bars across the
        /// old road's arch, lifted by a winch on the east. The first time through the hero goes
        /// over them by the road's gallery and drops into the arch; one pull on the way back and
        /// the road altar is a walk away.
        /// </summary>
        private static void BuildRoadGate(LevelZone zone, Transform arch, Transform eastRoom, GameProgress progress)
        {
            var cell = zone.CellSize;
            var bars = ForestBlock(arch, "RoadGate", ForestSpot("road-gate") + new Vector2(cell * 0.5f, cell * 3f),
                new Vector2(cell * 2f, cell * 6f), ForestSurface.RootWall);
            Ensure<NonClimbableSurface>(bars);
            var art = Object.Instantiate(bars.transform.Find("Visual").gameObject, arch);
            art.name = "RoadGateBars";
            art.transform.position = bars.transform.position;
            bars.transform.Find("Visual").gameObject.SetActive(false);

            var winchAt = ForestSpot("road-winch");
            var winch = new GameObject("RoadGateWinch");
            winch.transform.SetParent(eastRoom);
            winch.transform.position = winchAt + new Vector2(0f, 0.9f);
            var trigger = winch.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 0.8f;
            var handle = ForestProp(winch.transform, "env_forest_stump_01", winchAt, "GameplayEnvironment", 3, 0.6f);
            var shortcut = winch.AddComponent<Shortcut>();
            SetString(shortcut, "_shortcutId", ForestRoadGateShortcut);
            SetString(shortcut, "_displayName", "лебідка воріт");
            SetReference(shortcut, "_progress", progress);
            // Open, the bars sit up in the arch's vault; closed, three units lower, across the way.
            art.transform.position += new Vector3(0f, 3f, 0f);
            ConfigureShortcut(shortcut, System.Array.Empty<GameObject>(), new[] { bars },
                art.transform, new Vector3(0f, -3f, 0f), handle != null ? handle.transform : null);
        }

        /// <summary>Where each ability gate's mark stands (its spot), by the room the gate leads to.</summary>
        private static readonly Dictionary<string, string> ForestHintSpots = new Dictionary<string, string>
        {
            ["room_forest_hunter_hollow"] = "grapple-hint",
            ["room_forest_root_nest"] = "nest-hint",
            ["room_forest_deep_roots"] = "grapple-hint-2",
            ["room_forest_poison_grove"] = "double-jump-hint",
            ["room_forest_falconer_ladder"] = "falconer-hint",
            ["room_forest_thorn_course"] = "thorn-hint",
            ["room_forest_hunters_perch"] = "perch-hint"
        };

        /// <summary>The marks at the ability gates, and the glow that answers the ability.</summary>
        private static void BuildForestGateHints(LevelZone zone, Dictionary<string, Transform> rooms, GameProgress progress)
        {
            foreach (var link in zone.Links)
            {
                if (string.IsNullOrEmpty(link.Ability) || !ForestHintSpots.TryGetValue(link.To, out var spot))
                {
                    continue;
                }

                var glowAt = Inside(zone, zone.Room(link.To)).center;
                PlaceAbilityGate(rooms[link.From], $"{Short(link.To)}_Hint", ForestSpot(spot) + new Vector2(0f, 0.8f),
                    link.Ability, LinkId(link), glowAt, progress);
            }
        }

        /// <summary>A room less its walls.</summary>
        private static Rect Inside(LevelZone zone, LevelRoom room)
        {
            var bounds = zone.WorldBounds(room);
            var shell = zone.CellSize * 2f;
            return Rect.MinMaxRect(bounds.xMin + shell, bounds.yMin + shell, bounds.xMax - shell, bounds.yMax - shell);
        }

        // ---------- dressing ----------

        private static Dress FF(string sprite, float x, float scale = 1f, bool flip = false) =>
            new Dress("env_forest_" + sprite, x, -1f, scale, flip);

        private static Dress FH(string sprite, float x, float y, float scale = 1f, bool flip = false) =>
            new Dress("env_forest_" + sprite, x, y, scale, flip);

        /// <summary>
        /// Each hall its own set (the user's rule: nothing repeats from room to room in a way the eye
        /// catches): the same placeholder pieces, never the same arrangement, size or tone.
        /// </summary>
        private static readonly Dictionary<string, Dress[]> ForestDressing = new Dictionary<string, Dress[]>
        {
            ["room_forest_root_tunnel"] = new[] { FH("roots_01", 0.7f, 0.85f, 1.1f), FF("decal_moss_01", 0.4f) },
            ["room_forest_entrance"] = new[]
            {
                FF("arch_ruin_01", 0.08f, 1.5f), FF("trunk_dead_01", 0.24f, 1.6f), FF("stump_01", 0.36f),
                FF("trunk_01", 0.55f, 1.2f, true), FF("decal_moss_01", 0.45f), FF("decal_puddle_01", 0.62f),
                FF("roots_01", 0.78f), FH("canopy_01", 0.5f, 0.9f, 1.2f)
            },
            ["room_forest_royal_road"] = new[]
            {
                FF("statue_broken_01", 0.3f), FF("milestone_01", 0.85f, 0.9f), FF("trunk_dead_01", 0.6f, 1.2f, true)
            },
            ["room_forest_gate_arch"] = new[] { FH("roots_01", 0.5f, 0.9f, 0.5f) },
            ["room_forest_arch_ledge"] = new[] { FH("roots_01", 0.5f, 0.2f, 0.4f, true) },
            ["room_forest_old_graves"] = new[]
            {
                FF("statue_broken_01", 0.12f, 0.8f), FF("milestone_01", 0.3f, 0.8f), FF("milestone_01", 0.42f, 0.7f, true),
                FF("milestone_01", 0.66f, 0.85f), FF("stump_01", 0.78f, 0.8f), FH("roots_01", 0.5f, 0.85f, 0.9f)
            },
            ["room_forest_broken_arch"] = new[]
            {
                FF("arch_ruin_01", 0.32f, 1.7f), FF("trunk_01", 0.58f, 1.8f), FF("flora_01", 0.4f),
                FF("roots_01", 0.75f, 1.1f, true), FH("canopy_01", 0.6f, 0.92f, 1.3f)
            },
            ["room_forest_stone_bridge"] = new[]
            {
                FF("arch_ruin_01", 0.55f, 1.3f, true), FF("bridge_01", 0.3f, 1.1f), FH("roots_01", 0.2f, 0.8f)
            },
            ["room_forest_upper_path"] = new[] { FF("stump_01", 0.15f, 0.8f), FF("flora_01", 0.7f, 0.8f) },
            ["room_forest_grove_stash"] = new[] { FF("roots_01", 0.4f, 0.6f), FF("decal_spores_01", 0.7f, 0.7f) },
            ["room_forest_hunter_hollow"] = new[] { FF("stump_01", 0.35f, 0.7f, true), FH("roots_01", 0.6f, 0.75f, 0.6f) },
            ["room_forest_crown"] = new[]
            {
                FF("trunk_01", 0.12f, 1.9f), FF("trunk_dead_01", 0.42f, 1.4f, true), FF("flora_01", 0.62f, 1.1f),
                FH("canopy_01", 0.3f, 0.95f, 1.4f), FH("canopy_01", 0.78f, 0.88f, 1.1f, true)
            },
            ["room_forest_hollow_trunk"] = new[] { FH("roots_01", 0.5f, 0.5f, 0.5f) },
            ["room_forest_deep"] = new[]
            {
                FF("trunk_01", 0.18f, 2.3f), FF("roots_01", 0.3f, 1.4f), FF("trunk_dead_01", 0.5f, 1.5f, true),
                FF("statue_broken_01", 0.66f, 1.1f), FF("flora_01", 0.82f), FF("decal_spores_01", 0.4f),
                FH("canopy_01", 0.45f, 0.9f, 1.5f)
            },
            ["room_forest_watchtower"] = new[]
            {
                FF("watchtower_01", 0.45f, 2.2f), FF("bridge_01", 0.8f, 1.2f, true), FF("roots_01", 0.1f)
            },
            ["room_forest_hunters_perch"] = new[] { FF("stump_01", 0.25f, 0.6f) },
            ["room_forest_falconry"] = new[]
            {
                FF("trunk_dead_01", 0.08f, 0.9f), FF("bridge_01", 0.55f, 0.8f), FF("statue_broken_01", 0.85f, 0.8f, true)
            },
            ["room_forest_falconer_nest"] = new[] { FF("roots_01", 0.5f, 0.6f, true) },
            ["room_forest_falconer_ladder"] = new[] { FH("roots_01", 0.3f, 0.6f, 0.6f) },
            ["room_forest_walkway"] = new[]
            {
                FF("bridge_01", 0.7f, 1.4f), FF("trunk_01", 0.06f, 1.5f, true), FF("stump_01", 0.45f),
                FF("flora_01", 0.2f, 0.9f), FH("canopy_01", 0.35f, 0.9f, 1.1f)
            },
            ["room_forest_under_walkway"] = new[] { FH("roots_01", 0.6f, 0.9f, 1.2f), FF("decal_puddle_01", 0.4f, 0.8f) },
            ["room_forest_root_nest"] = new[] { FF("roots_01", 0.5f, 0.5f) },
            ["room_forest_warden_approach"] = new[]
            {
                FF("statue_broken_01", 0.2f), FF("statue_broken_01", 0.92f, 1f, true), FF("arch_ruin_01", 0.58f, 1.2f)
            },
            ["room_forest_warden_bridge"] = new[] { FF("trunk_dead_01", 0.15f, 1.3f), FF("roots_01", 0.85f, 1.1f, true) },
            ["room_forest_warden_arena"] = new[] { FF("roots_01", 0.1f, 1.2f), FF("roots_01", 0.9f, 1.2f, true) },
            ["room_forest_far_gate"] = new[] { FF("trunk_01", 0.75f, 1.1f) },
            ["room_forest_poison_grove"] = new[]
            {
                FF("trunk_dead_01", 0.3f, 1.5f), FF("flora_01", 0.45f, 1.2f), FF("flora_01", 0.6f, 0.9f, true),
                FF("decal_spores_01", 0.52f, 1.3f), FH("canopy_01", 0.7f, 0.92f, 1.2f)
            },
            ["room_forest_ravine"] = new[] { FH("roots_01", 0.5f, 0.75f, 0.8f), FH("roots_01", 0.4f, 0.3f, 0.7f, true) },
            ["room_forest_ravine_foot"] = new[] { FF("stump_01", 0.3f, 0.9f), FH("roots_01", 0.7f, 0.85f, 1f) },
            ["room_forest_deep_roots"] = new[]
            {
                FH("roots_01", 0.3f, 0.85f, 1.4f), FH("roots_01", 0.7f, 0.8f, 1.2f, true), FF("decal_spores_01", 0.6f)
            },
            ["room_forest_tree_grave"] = new[]
            {
                FF("trunk_01", 0.6f, 1.6f), FF("milestone_01", 0.5f, 1.1f), FF("roots_01", 0.75f, 1.2f)
            },
            ["room_forest_lower_gallery"] = new[]
            {
                FH("roots_01", 0.1f, 0.9f, 1.1f), FF("arch_ruin_01", 0.2f, 0.9f), FF("statue_broken_01", 0.38f, 0.9f),
                FH("roots_01", 0.55f, 0.95f, 1.6f, true), FF("flora_01", 0.7f), FH("roots_01", 0.88f, 0.9f, 1.2f)
            },
            ["room_forest_thorn_course"] = new[] { FH("roots_01", 0.4f, 0.85f, 1.3f), FH("roots_01", 0.8f, 0.9f, 1f, true) },
            ["room_forest_mushroom_pool"] = new[]
            {
                FF("flora_01", 0.2f, 1.3f), FF("decal_spores_01", 0.45f, 1.4f), FF("flora_01", 0.62f, 1.1f, true),
                FF("decal_puddle_01", 0.75f, 1.2f), FH("roots_01", 0.5f, 0.85f, 1.2f)
            },
            ["room_forest_mushroom_cache"] = new[] { FF("flora_01", 0.45f, 0.6f), FF("decal_spores_01", 0.7f, 0.6f) }
        };

        /// <summary>A tone per room, so the same wood never reads as the same room twice.</summary>
        private static Color ForestRoomTone(string roomId)
        {
            var hash = 0;
            foreach (var c in roomId)
            {
                hash = hash * 31 + c;
            }

            var r = (hash & 0xff) / 255f;
            var g = ((hash >> 8) & 0xff) / 255f;
            return new Color(0.5f + r * 0.12f, 0.6f + g * 0.14f, 0.56f + (1f - r) * 0.1f);
        }

        private static void DressForestRoom(LevelZone zone, LevelRoom room, Transform parent)
        {
            var inside = Inside(zone, room);
            var dressing = Section(parent, "Dressing");
            if (ForestEnclosedRooms.Contains(room.Id))
            {
                BuildForestRoomWall(room, inside, dressing);
            }
            else if (!room.IsSecret)
            {
                // A low bank of mist over the floor of the open rooms: drifts, never on the line of combat.
                var fog = Shape("shape_fog_band");
                if (fog != null)
                {
                    var tone = ForestRoomTone(room.Id);
                    FogLayer(dressing, fog, inside.center.x, inside.width, inside.yMin + 0.6f, 1.7f,
                        new Color(tone.r, tone.g, tone.b, 0.14f), "EnvironmentBack", 40, 0.4f);
                }
            }

            if (ForestDressing.TryGetValue(room.Id, out var pieces))
            {
                foreach (var piece in pieces)
                {
                    var x = Mathf.Lerp(inside.xMin + 0.6f, inside.xMax - 0.6f, piece.X);
                    var at = piece.Y < 0f
                        ? new Vector2(x, FloorUnder(zone, room, x))
                        : new Vector2(x, Mathf.Lerp(inside.yMin, inside.yMax, piece.Y));
                    var front = piece.Sprite.Contains("flora") || piece.Sprite.Contains("stump")
                        || piece.Sprite.Contains("decal") || piece.Sprite.Contains("milestone");
                    var canopy = piece.Sprite.Contains("canopy");
                    var prop = ForestProp(dressing, piece.Sprite, at,
                        canopy ? "Foreground" : front ? "GameplayEnvironment" : "EnvironmentBack",
                        front ? 1 : 2, piece.Scale, piece.Flip, canopy ? 0.85f : 1f);
                    if (prop != null && !front && !canopy)
                    {
                        prop.GetComponent<SpriteRenderer>().color = Color.Lerp(Color.white, ForestRoomTone(room.Id), 0.35f);
                    }
                }
            }

            foreach (var marker in room.Markers)
            {
                if (marker.Kind == "spot" && (marker.Arg(0) == "waterfall" || marker.Arg(0) == "pool"))
                {
                    var foot = MarkerFeet(zone, marker);
                    if (marker.Arg(0) == "waterfall")
                    {
                        BuildWaterfall(dressing, foot, inside.yMax, ForestWater);
                    }
                    else
                    {
                        BuildPool(dressing, foot, ForestWater);
                    }
                }
            }
        }

        /// <summary>The forest's water: green with what grows in it.</summary>
        private static readonly Color ForestWater = new Color(0.45f, 0.85f, 0.45f);

        /// <summary>The earth and roots behind an enclosed room, in its own tone; the open rooms show the sky.</summary>
        private static void BuildForestRoomWall(LevelRoom room, Rect inside, Transform parent)
        {
            var art = ForestArt(inside.width >= inside.height ? "env_forest_rootwall_01" : "env_forest_cliff_01");
            if (art == null)
            {
                return;
            }

            var back = new GameObject("Backdrop");
            back.transform.SetParent(parent, false);
            back.transform.position = inside.center;
            var renderer = back.AddComponent<SpriteRenderer>();
            renderer.sprite = art;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = inside.size;
            var tone = ForestRoomTone(room.Id) * 0.42f;
            tone.a = 1f;
            renderer.color = tone;
            renderer.sharedMaterial = _material;
            // Over the earth and the near trees: an enclosed room shows its own wall.
            SetSorting(renderer, "BackgroundNear", 11);
        }

        /// <summary>
        /// The earth under the forest: dark ground behind everything below the ground line, so the
        /// sky the parallax paints never shows between the underground rooms.
        /// </summary>
        private static void BuildForestEarth(LevelZone zone, Transform root)
        {
            var art = ForestArt("env_forest_cliff_01");
            if (art == null)
            {
                return;
            }

            var bottom = float.MaxValue;
            foreach (var room in zone.Rooms)
            {
                bottom = Mathf.Min(bottom, zone.WorldBounds(room).yMin);
            }

            var area = Rect.MinMaxRect(62.5f, bottom - 6f, 318f, ForestGroundLine);
            var earth = new GameObject("Earth");
            earth.transform.SetParent(root, false);
            earth.transform.position = area.center;
            var renderer = earth.AddComponent<SpriteRenderer>();
            renderer.sprite = art;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = area.size;
            renderer.color = new Color(0.13f, 0.15f, 0.15f);
            renderer.sharedMaterial = _material;
            SetSorting(renderer, "BackgroundNear", 10);
        }

        /// <summary>
        /// The Forest's own lights when it is rebuilt in a finished world (<see cref="SceneForest"/>):
        /// altars and the Warden, as BuildAtmosphere lights them in the generator.
        /// </summary>
        public static void LightForest(Transform forest)
        {
            BuildCheckpointLights(forest);
            BuildBossLights(forest);
        }
    }
}
