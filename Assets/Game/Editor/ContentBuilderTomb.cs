using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The Forgotten Tomb built from its text (item 3 of the 15-zone list, the user's map
    /// `reference/maps/01_forgotten_tomb.webp`): the layout gives the stone, the finds, the
    /// enemies, the altars and the graph; this file adds what only the Tomb has — the Guardian and
    /// his sealed door, the training figures, each hall's own dressing and backdrop, its lights, and
    /// the hints at the ability gates. One root, <c>Level/Tomb</c>, replaced whole on every build.
    /// </summary>
    public static partial class ContentBuilder
    {
        public const string TombLayoutFile = "tomb.txt";
        public const string TombRootName = "Tomb";
        private const string TrainingDummyPrefabPath = PrefabFolder + "/TrainingDummy.prefab";
        private const string TrainingAttackerPrefabPath = PrefabFolder + "/TrainingAttacker.prefab";
        private const string GuardianPrefabPath = PrefabFolder + "/GuardianOfTheTomb.prefab";

        private static LevelZone _tombLayout;

        /// <summary>The Tomb's layout, read once per build.</summary>
        public static LevelZone TombLayout => _tombLayout ?? (_tombLayout = LoadLayout(TombLayoutFile));

        /// <summary>Where a named spot of the Tomb's layout stands, on its floor.</summary>
        public static Vector2 TombSpot(string name) => SpotFeet(TombLayout, name);

        /// <summary>
        /// The whole Tomb under <paramref name="level"/>. Runs in the generator's new scene and in a
        /// world opened whole (<see cref="SceneTomb"/>); everything it hooks up to is found there.
        /// </summary>
        public static BossEncounter BuildTomb(Transform level)
        {
            _tombLayout = null;
            if (_tomb == null) { _tomb = PlaceholderTombArt.Generate(); }
            if (_shapes == null) { _shapes = ProductionSpriteArt.Generate(); }
            if (_sprite == null) { _sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath); }
            if (_material == null) { _material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath); }

            var old = level.Find(TombRootName);
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            var zone = TombLayout;
            var root = Section(level, TombRootName);
            var context = LevelContext.FromScene();
            var problems = BuildLayout(zone, root, TombBlock, context);
            foreach (var problem in problems)
            {
                Debug.LogError($"Tomb layout: {problem}");
            }

            var rooms = new Dictionary<string, Transform>();
            foreach (var room in zone.Rooms)
            {
                rooms[room.Id] = root.Find(room.Id);
            }

            var encounter = BuildGuardianArena(zone, rooms["room_tomb_guardian_arena"], context);
            BuildTrainingFigures(rooms["room_tomb_entrance"]);
            BuildTombGateHints(zone, rooms, context.Progress);
            BuildChainGate(zone, rooms["room_tomb_chain_shaft"], context.Progress);
            foreach (var room in zone.Rooms)
            {
                DressTombRoom(zone, room, rooms[room.Id]);
            }

            return encounter;
        }

        /// <summary>How the Tomb's stone looks: ledges as slabs, the rest by its shape.</summary>
        private static GameObject TombBlock(Transform parent, string name, Rect world, LevelCell kind)
        {
            TombSurface surface;
            if (kind == LevelCell.Ledge)
            {
                // Every third slab cracked, picked by place so a rebuild draws the same ones.
                var seed = Mathf.Abs(Mathf.RoundToInt(world.x * 7f + world.y * 13f));
                surface = seed % 3 == 0 ? TombSurface.BrokenPlatform : TombSurface.Platform;
            }
            else
            {
                surface = world.height > world.width ? TombSurface.Wall : TombSurface.Floor;
            }

            return Block(parent, name, world.center, world.size, surface);
        }

        // ---------- the Guardian ----------

        /// <summary>
        /// The Guardian in the middle of his hall, the trigger a few steps past the door, and the
        /// seal that drops into the door behind the hero (shut until the fight starts).
        /// </summary>
        private static BossEncounter BuildGuardianArena(LevelZone zone, Transform arena, LevelContext context)
        {
            var bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GuardianPrefabPath);
            if (bossPrefab == null)
            {
                Debug.LogError("No Guardian prefab; the Tomb has no boss.");
                return null;
            }

            var boss = (GameObject)PrefabUtility.InstantiatePrefab(bossPrefab, arena);
            boss.transform.position = TombSpot("guardian") + new Vector2(0f, 1.4f);

            var door = TombSpot("arena-door");
            var cell = zone.CellSize;
            var barrier = Block(arena, "ArenaBarrier", door + new Vector2(cell * 0.5f, cell * 4f),
                new Vector2(cell * 2f, cell * 8f), TombSurface.Wall);
            DressBarrier(barrier);
            barrier.SetActive(false);

            var encounterObject = new GameObject("BossEncounter");
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
            SetString(encounter, "_rewardId", "guardian_core");
            SetString(encounter, "_rewardName", "Ядро Стража");
            var debug = Object.FindAnyObjectByType<CombatDebugOverlay>(FindObjectsInactive.Include);
            if (debug != null)
            {
                SetReference(debug, "_bossEncounter", encounter);
            }

            // The gate itself, standing open in the door until the seal drops.
            var gate = Prop(arena, "env_tomb_boss_gate", door + new Vector2(cell, 0f), "EnvironmentBack", 8, 1.05f);
            if (gate != null)
            {
                gate.name = "BossGate";
            }

            return encounter;
        }

        /// <summary>
        /// Two straw men and a sparring partner on the entrance's floor. They are for testing the
        /// fight in the Editor, so a release build hides them.
        /// </summary>
        private static void BuildTrainingFigures(Transform entrance)
        {
            var dummy = AssetDatabase.LoadAssetAtPath<GameObject>(TrainingDummyPrefabPath);
            var attacker = AssetDatabase.LoadAssetAtPath<GameObject>(TrainingAttackerPrefabPath);
            foreach (var (spot, prefab, purpose) in new[]
            {
                ("dummy-1", dummy, "Training dummy"),
                ("dummy-2", dummy, "Training dummy"),
                ("sparring", attacker, "Sparring partner for block and parry")
            })
            {
                if (prefab == null)
                {
                    continue;
                }

                var figure = SpawnEnemy(prefab, TombSpot(spot) + new Vector2(0f, 1f));
                figure.transform.SetParent(entrance, true);
                MarkDeveloperOnly(figure, purpose);
            }
        }

        /// <summary>The marks at the two ability gates, and the glow that answers the ability.</summary>
        private static void BuildTombGateHints(LevelZone zone, Dictionary<string, Transform> rooms, GameProgress progress)
        {
            foreach (var link in zone.Links)
            {
                if (string.IsNullOrEmpty(link.Ability))
                {
                    continue;
                }

                var target = zone.Room(link.To);
                var glowAt = link.To == "room_tomb_bell_shaft" ? TombSpot("bell-glow") + new Vector2(0f, 0.5f)
                    : zone.WorldBounds(target).center;
                var hint = TombSpot(link.Ability + "-hint");
                PlaceAbilityGate(rooms[link.From], $"{Short(link.To)}_Hint", hint + new Vector2(0f, 0.8f), link.Ability,
                    LinkId(link), glowAt, progress);
            }
        }

        public const string TombChainGateShortcut = "shortcut_tomb_chain_gate";

        /// <summary>
        /// The Tomb's shortcut (the world's rule: every zone has one, opened from its far side):
        /// iron bars across the door from the chain shaft into the entrance, wound up by a winch on
        /// the shaft's side. Coming back from the Guardian, one pull and the first altar is a walk away.
        /// </summary>
        private static void BuildChainGate(LevelZone zone, Transform shaft, GameProgress progress)
        {
            var door = TombSpot("chain-gate");
            var cell = zone.CellSize;
            var bars = Block(shaft, "ChainGate", door + new Vector2(cell * 0.5f, cell * 4f), new Vector2(cell * 2f, cell * 8f),
                TombSurface.Wall);
            Ensure<NonClimbableSurface>(bars);
            var art = Object.Instantiate(bars.transform.Find("Visual").gameObject, shaft);
            art.name = "ChainGateBars";
            art.transform.position = bars.transform.position;
            art.GetComponent<SpriteRenderer>().color = new Color(0.55f, 0.55f, 0.62f);
            bars.transform.Find("Visual").gameObject.SetActive(false);

            var winchAt = TombSpot("chain-winch");
            var winch = new GameObject("ChainGateWinch");
            winch.transform.SetParent(shaft);
            winch.transform.position = winchAt + new Vector2(0f, 0.9f);
            var trigger = winch.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 0.8f;
            var handle = Prop(winch.transform, "env_tomb_chains_01", winchAt + new Vector2(0f, 0.2f), "GameplayEnvironment", 3, 0.5f);
            var shortcut = winch.AddComponent<Shortcut>();
            SetString(shortcut, "_shortcutId", TombChainGateShortcut);
            SetString(shortcut, "_displayName", "лебідка ґрат");
            SetReference(shortcut, "_progress", progress);
            // Open, the bars sit up in the vault; closed, four units lower, across the door.
            art.transform.position += new Vector3(0f, 4f, 0f);
            ConfigureShortcut(shortcut, System.Array.Empty<GameObject>(), new[] { bars },
                art.transform, new Vector3(0f, -4f, 0f), handle != null ? handle.transform : null);
        }

        // ---------- dressing ----------

        /// <summary>One piece of a room's dressing: where along the room and how it stands.</summary>
        private readonly struct Dress
        {
            public readonly string Sprite;
            /// <summary>0 at the room's west wall, 1 at its east one.</summary>
            public readonly float X;
            /// <summary>Below 0: stands on the floor. Otherwise hung at this share of the height.</summary>
            public readonly float Y;
            public readonly float Scale;
            public readonly bool Flip;

            public Dress(string sprite, float x, float y = -1f, float scale = 1f, bool flip = false)
            {
                Sprite = sprite;
                X = x;
                Y = y;
                Scale = scale;
                Flip = flip;
            }
        }

        private static Dress F(string sprite, float x, float scale = 1f, bool flip = false) =>
            new Dress("env_tomb_" + sprite, x, -1f, scale, flip);

        private static Dress H(string sprite, float x, float y, float scale = 1f, bool flip = false) =>
            new Dress("env_tomb_" + sprite, x, y, scale, flip);

        /// <summary>
        /// Each hall its own set (the user's rule: nothing repeats from room to room in a way the eye
        /// catches): the same placeholder pieces, but never the same arrangement, size or tone.
        /// </summary>
        private static readonly Dictionary<string, Dress[]> TombDressing = new Dictionary<string, Dress[]>
        {
            ["room_tomb_entrance"] = new[]
            {
                F("arch_01", 0.1f, 1.4f), F("statue_broken_01", 0.28f), F("sarcophagus_broken_01", 0.52f),
                F("bones_01", 0.6f), F("grave_marker_01", 0.82f), F("candles_01", 0.06f), H("chains_01", 0.7f, 0.75f)
            },
            ["room_tomb_burial_hall"] = new[]
            {
                F("statue_knight_01", 0.05f, 1.2f), H("banner_01", 0.1f, 0.55f), F("pillar_01", 0.18f),
                F("sarcophagus_01", 0.27f), F("candles_01", 0.33f), F("urn_01", 0.48f, 0.9f),
                F("sarcophagus_01", 0.58f, 0.85f, true), F("arch_01", 0.7f, 1.5f), F("memorial_01", 0.86f),
                F("pillar_broken_01", 0.95f)
            },
            ["room_tomb_west_shaft"] = new[] { H("chains_01", 0.5f, 0.8f, 0.8f), H("roots_01", 0.15f, 0.3f, 0.7f) },
            ["room_tomb_guard_passage"] = new[]
            {
                H("roots_01", 0.05f, 0.8f), F("pillar_01", 0.36f), F("arch_01", 0.46f, 1.5f), H("banner_01", 0.42f, 0.5f),
                H("banner_01", 0.62f, 0.5f, 1f, true), F("memorial_01", 0.53f), F("candles_01", 0.57f),
                F("rubble_01", 0.7f), F("statue_knight_01", 0.93f, 1f, true)
            },
            ["room_tomb_chain_shaft"] = new[]
            {
                H("chains_01", 0.5f, 0.45f), H("chains_01", 0.45f, 0.82f, 0.75f), H("banner_01", 0.25f, 0.25f, 0.9f)
            },
            ["room_tomb_guardian_approach"] = new[]
            {
                F("pillar_01", 0.62f), F("statue_knight_01", 0.5f), F("statue_knight_01", 0.9f, 1f, true),
                F("candles_01", 0.56f), F("candles_01", 0.95f), H("decal_rune_01", 0.7f, 0.55f), H("banner_01", 0.8f, 0.7f)
            },
            ["room_tomb_guardian_arena"] = new[]
            {
                F("pillar_01", 0.04f, 1.2f), F("pillar_01", 0.96f, 1.2f), F("statue_knight_01", 0.14f, 1.15f),
                F("statue_knight_01", 0.86f, 1.15f, true), F("candles_01", 0.2f), F("candles_01", 0.8f)
            },
            ["room_tomb_royal_ossuary"] = new[] { F("urn_01", 0.35f, 0.7f), F("bones_01", 0.6f), F("sarcophagus_01", 0.8f, 0.55f) },
            ["room_tomb_upper_passage"] = new[] { F("rubble_01", 0.22f, 0.8f), F("bones_01", 0.55f, 0.9f), F("decal_crack_01", 0.8f) },
            ["room_tomb_statue_hall"] = new[] { F("statue_knight_01", 0.5f, 1.3f), H("roots_01", 0.2f, 0.85f) },
            ["room_tomb_east_chapel"] = new[]
            {
                F("arch_01", 0.15f, 1.3f), H("banner_01", 0.08f, 0.6f), F("candles_01", 0.6f), F("candles_01", 0.7f, 0.8f),
                F("memorial_01", 0.84f, 0.9f), F("grave_marker_01", 0.42f, 0.8f)
            },
            ["room_tomb_flooded_crypt"] = new[]
            {
                F("pillar_broken_01", 0.18f), H("roots_01", 0.85f, 0.7f, 1f, true), F("decal_moss_01", 0.5f)
            },
            ["room_tomb_drowned_cache"] = new[] { F("urn_01", 0.25f, 0.6f), F("rubble_01", 0.7f, 0.6f) },
            ["room_tomb_east_hall"] = new[]
            {
                F("pillar_01", 0.18f), F("arch_01", 0.34f, 1.6f), F("pillar_01", 0.5f, 1.1f), H("banner_01", 0.42f, 0.65f),
                F("grave_marker_01", 0.6f, 1.1f), F("statue_knight_01", 0.78f, 1.2f), F("candles_01", 0.9f)
            },
            ["room_tomb_falls"] = new[] { F("rubble_01", 0.3f, 0.9f, true), H("roots_01", 0.7f, 0.75f, 0.8f) },
            ["room_tomb_lower_crypt"] = new[]
            {
                F("sarcophagus_broken_01", 0.18f, 0.9f), F("statue_broken_01", 0.42f, 0.9f, true), H("decal_rune_01", 0.55f, 0.4f),
                F("bones_01", 0.72f)
            },
            ["room_tomb_lower_chain"] = new[] { H("chains_01", 0.6f, 0.6f, 0.7f) },
            ["room_tomb_reliquary"] = new[] { F("urn_01", 0.15f, 0.55f) },
            ["room_tomb_rubble_slope"] = new[] { F("pillar_broken_01", 0.12f, 0.9f), F("rubble_01", 0.62f, 1.1f), F("rubble_01", 0.85f, 0.7f, true) },
            ["room_tomb_buried_cache"] = new[] { F("bones_01", 0.3f, 0.8f), F("urn_01", 0.75f, 0.5f) },
            ["room_tomb_collapsed_crypt"] = new[]
            {
                F("statue_broken_01", 0.08f, 1.1f), F("pillar_broken_01", 0.24f, 1.2f), F("rubble_01", 0.42f, 1.3f),
                F("sarcophagus_broken_01", 0.6f), H("roots_01", 0.5f, 0.9f, 1.2f), F("roots_01", 0.75f, 0.9f)
            },
            ["room_tomb_column"] = new[] { H("chains_01", 0.6f, 0.5f, 0.9f) },
            ["room_tomb_walled_cell"] = new[] { F("bones_01", 0.55f, 0.7f, true), F("grave_marker_01", 0.75f, 0.7f) },
            ["room_tomb_mason_cache"] = new[] { F("rubble_01", 0.35f, 0.7f), F("memorial_01", 0.55f, 0.8f), F("candles_01", 0.8f, 0.9f) },
            ["room_tomb_bell_shaft"] = new[] { H("chains_01", 0.5f, 0.88f, 1.1f), H("banner_01", 0.3f, 0.45f, 0.8f, true) },
            ["room_tomb_arcade"] = new[]
            {
                F("arch_01", 0.22f, 1.3f), F("pillar_01", 0.37f, 0.9f), F("statue_knight_01", 0.45f, 0.95f), F("arch_01", 0.6f, 1.25f, true),
                F("pillar_01", 0.75f, 0.9f), F("candles_01", 0.9f, 0.8f)
            }
        };

        /// <summary>A tone per room, so the same stone never reads as the same room twice.</summary>
        private static Color RoomTone(string roomId)
        {
            var hash = 0;
            foreach (var c in roomId)
            {
                hash = hash * 31 + c;
            }

            var r = (hash & 0xff) / 255f;
            var g = ((hash >> 8) & 0xff) / 255f;
            return new Color(0.5f + r * 0.14f, 0.54f + g * 0.12f, 0.66f + (1f - r) * 0.12f);
        }

        private static void DressTombRoom(LevelZone zone, LevelRoom room, Transform parent)
        {
            var bounds = zone.WorldBounds(room);
            var shell = zone.CellSize * 2f;
            var inside = Rect.MinMaxRect(bounds.xMin + shell, bounds.yMin + shell, bounds.xMax - shell, bounds.yMax - shell);
            var dressing = Section(parent, "Dressing");
            BuildRoomBackdrop(room, inside, dressing);
            if (TombDressing.TryGetValue(room.Id, out var pieces))
            {
                foreach (var piece in pieces)
                {
                    var x = Mathf.Lerp(inside.xMin + 0.6f, inside.xMax - 0.6f, piece.X);
                    var at = piece.Y < 0f
                        ? new Vector2(x, FloorUnder(zone, room, x))
                        : new Vector2(x, Mathf.Lerp(inside.yMin, inside.yMax, piece.Y));
                    var front = piece.Sprite.Contains("candles") || piece.Sprite.Contains("bones")
                        || piece.Sprite.Contains("rubble") || piece.Sprite.Contains("decal");
                    var prop = Prop(dressing, piece.Sprite, at, front ? "GameplayEnvironment" : "EnvironmentBack",
                        front ? 1 : 2, piece.Scale, piece.Flip);
                    if (prop != null && !front)
                    {
                        prop.GetComponent<SpriteRenderer>().color = Color.Lerp(Color.white, RoomTone(room.Id), 0.35f);
                    }
                }
            }

            if (room.Height * zone.CellSize >= 10f && !room.IsSecret)
            {
                BuildRoomShaft(room, inside, dressing);
            }

            if (room.Id == "room_tomb_guardian_arena")
            {
                BuildTombArenaDressing(inside, dressing);
            }

            foreach (var spot in new[] { "waterfall", "pool" })
            {
                foreach (var marker in room.Markers)
                {
                    if (marker.Kind == "spot" && marker.Arg(0) == spot)
                    {
                        BuildWaterfall(dressing, MarkerFeet(zone, marker), inside.yMax);
                    }
                }
            }
        }

        /// <summary>The top of the floor under x in this room, read from the grid (lowest floor).</summary>
        private static float FloorUnder(LevelZone zone, LevelRoom room, float x)
        {
            var column = zone.CellOf(new Vector2(x, 0f)).x;
            for (var y = room.Origin.y + 1; y < room.Origin.y + room.Height; y++)
            {
                var here = new Vector2Int(column, y);
                if (!zone.IsSolid(here) && zone.At(here) != LevelCell.Spikes && zone.IsSolid(here + Vector2Int.down))
                {
                    return zone.CellCorner(here).y;
                }
            }

            return zone.WorldBounds(room).yMin + zone.CellSize * 2f;
        }

        /// <summary>
        /// The wall behind the room: halls show the vaulted nave, shafts the bare masonry, each
        /// in its own tone and offset; the rock between rooms stays the dark of the camera.
        /// </summary>
        private static void BuildRoomBackdrop(LevelRoom room, Rect inside, Transform parent)
        {
            var hall = inside.width >= inside.height;
            var art = Tomb(hall ? (room.IsSecret ? "env_tomb_bg_near_01" : "env_tomb_bg_mid_01") : "env_tomb_wall_01");
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
            var tone = RoomTone(room.Id) * (hall ? 0.75f : 0.45f);
            tone.a = 1f;
            renderer.color = tone;
            renderer.sharedMaterial = _material;
            SetSorting(renderer, "BackgroundMid", 0);
        }

        /// <summary>Cold light falling through the vault of a tall room: guides the eye, lights nothing.</summary>
        private static void BuildRoomShaft(LevelRoom room, Rect inside, Transform parent)
        {
            var sprite = Shape("shape_light_shaft");
            if (sprite == null)
            {
                return;
            }

            var hash = Mathf.Abs(room.Id.GetHashCode());
            var beam = new GameObject("LightShaft");
            beam.transform.SetParent(parent, false);
            beam.transform.position = new Vector2(Mathf.Lerp(inside.xMin, inside.xMax, 0.3f + (hash % 40) / 100f), inside.center.y);
            beam.transform.rotation = Quaternion.Euler(0f, 0f, (hash % 2 == 0 ? 1f : -1f) * (4f + hash % 5));
            beam.transform.localScale = new Vector3(Mathf.Min(3.4f, inside.width * 0.25f), inside.height * 1.05f, 1f);
            var renderer = beam.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(0.7f, 0.8f, 1f, 0.08f);
            renderer.sharedMaterial = _material;
            SetSorting(renderer, "BackgroundNear", 8);
        }

        /// <summary>Water falling from the vault to a floor, as the map draws it.</summary>
        private static void BuildWaterfall(Transform parent, Vector2 foot, float top, Color? tint = null)
        {
            var water = tint ?? new Color(0.55f, 0.85f, 0.95f);
            var sprite = Shape("shape_light_shaft");
            if (sprite == null)
            {
                return;
            }

            var fall = new GameObject("Waterfall");
            fall.transform.SetParent(parent, false);
            fall.transform.position = new Vector2(foot.x, (foot.y + top) * 0.5f);
            fall.transform.localScale = new Vector3(1.6f, top - foot.y, 1f);
            var renderer = fall.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(water.r, water.g, water.b, 0.28f);
            renderer.sharedMaterial = _material;
            SetSorting(renderer, "EnvironmentBack", 6);
            BuildPool(parent, foot, tint);
        }

        /// <summary>Still water lying on a floor.</summary>
        private static void BuildPool(Transform parent, Vector2 foot, Color? tint = null)
        {
            var water = tint.HasValue
                ? new Color(tint.Value.r * 0.65f, tint.Value.g * 0.82f, tint.Value.b * 0.84f)
                : new Color(0.35f, 0.7f, 0.8f);
            var pool = new GameObject("Pool");
            pool.transform.SetParent(parent, false);
            pool.transform.position = foot + new Vector2(0f, 0.12f);
            pool.transform.localScale = new Vector3(5f, 0.24f, 1f);
            var poolRenderer = pool.AddComponent<SpriteRenderer>();
            poolRenderer.sprite = _sprite;
            poolRenderer.sharedMaterial = _material;
            poolRenderer.color = new Color(water.r, water.g, water.b, 0.35f);
            SetSorting(poolRenderer, "GameplayEnvironment", 2);
        }

        /// <summary>The Guardian's tomb behind him and the arena's own light.</summary>
        private static void BuildTombArenaDressing(Rect inside, Transform parent)
        {
            var backdrop = Prop(parent, "env_tomb_arena_backdrop", new Vector2(inside.center.x, inside.yMin - 1f),
                "BackgroundNear", 5, 1.15f);
            if (backdrop != null)
            {
                backdrop.name = "ArenaBackdrop";
            }

            var lights = Section(parent, "ArenaLights");
            foreach (var (fx, fy, color, intensity, radius) in new[]
            {
                (0.3f, 0.25f, new Color(0.7f, 0.8f, 1f), 1.0f, 9f),
                (0.7f, 0.25f, new Color(0.7f, 0.8f, 1f), 1.0f, 9f),
                (0.5f, 0.08f, new Color(0.9f, 0.72f, 0.45f), 0.55f, 7f)
            })
            {
                var host = LightChild(lights, "ArenaLight",
                    new Vector2(Mathf.Lerp(inside.xMin, inside.xMax, fx), Mathf.Lerp(inside.yMin, inside.yMax, fy)));
                AddLight(host, Light2D.LightType.Point, color, intensity, radius, 1.5f, 0.45f);
            }
        }
    }
}
