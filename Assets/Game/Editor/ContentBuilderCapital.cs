using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The Ashen Capital as a place (item 2 of the big list, the user's choices of 2026-09-28):
    /// the burnt seat of the kingdom past the road the Warden kept. Eight rooms from x = 313.5 to
    /// 545 — the Ash Road, the City Gate, the Market, the Bell Tower, the Temple Quarter, the Burnt
    /// Gardens, the Palace Approach and the Throne Hall — two secrets and three altars.
    ///
    /// Built into the whole world (inside <see cref="WorldScenes.Edit"/>) under <c>Level/AshenCapital</c>,
    /// replaced whole on every run; the split puts it in <c>Zone_Capital</c> by the graph's areas.
    /// Floors are at y = 0 like the other zones; every step of the main path is 1.6, under a
    /// jump, so the road is walkable with nothing but running and jumping, both ways.
    /// Its enemies and its king come in later stages; the Throne Hall stands empty till then.
    /// </summary>
    public static partial class ContentBuilder
    {
        public const string CapitalRootName = "AshenCapital";
        public const string CapitalRoadGateName = "CapitalRoadGate";
        private static Dictionary<string, Sprite> _capital;

        private enum CapitalSurface { Flagstone, Masonry, Ledge, Rubble }

        private static Sprite CapitalArt(string name) =>
            _capital != null && _capital.TryGetValue(name, out var sprite) ? sprite : null;

        /// <summary>The whole zone, from nothing; run inside a world opened whole.</summary>
        public static void BuildCapital()
        {
            if (_sprite == null) { _sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath); }
            if (_material == null) { _material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath); }
            if (_shapes == null) { _shapes = ProductionSpriteArt.Generate(); }
            if (_vfx == null) { _vfx = PlaceholderVfxArt.Generate(); }
            _capital = PlaceholderCapitalArt.Generate();

            var level = GameObject.Find("Level");
            if (level == null)
            {
                Debug.LogError("The world has no Level to build the capital in.");
                return;
            }

            var old = level.transform.Find(CapitalRootName);
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            // Altars and finds are placed at the scene's root by their helpers; the capital's own
            // are found by id and gathered under its root, so a rebuild replaces them too.
            foreach (var stray in CapitalPlacements())
            {
                Object.DestroyImmediate(stray);
            }

            var progress = Object.FindAnyObjectByType<GameProgress>();
            var respawner = Object.FindAnyObjectByType<PlayerRespawner>();
            var player = Object.FindAnyObjectByType<PlayerCharacter>();
            var checkpoint = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/Checkpoint.prefab");
            var pickup = AssetDatabase.LoadAssetAtPath<GameObject>(PickupPrefabPath);
            var camera = Camera.main != null ? Camera.main.transform : null;

            var root = Section(level.transform, CapitalRootName);
            OpenCapitalRoad(progress);
            BuildAshRoad(root);
            BuildCityGate(root, checkpoint, respawner, progress, player != null ? player.gameObject : null);
            BuildMarket(root, pickup, progress);
            BuildBellTower(root, pickup, progress);
            BuildTempleQuarter(root, checkpoint, respawner, progress, player != null ? player.gameObject : null);
            BuildBurntGardens(root, pickup, progress);
            BuildPalaceApproach(root, checkpoint, pickup, respawner, progress, player != null ? player.gameObject : null);
            BuildThroneHall(root, progress, respawner);
            BuildGrappleReturns(level.transform, progress, pickup);
            BuildCapitalParallax(root, camera);
            BuildCapitalAsh(root);
            PlaceCapitalEnemies();
            var placed = Section(root, "Placed");
            foreach (var placement in CapitalPlacements())
            {
                placement.transform.SetParent(placed, true);
            }
        }

        private static List<GameObject> CapitalPlacements()
        {
            var found = new List<GameObject>();
            foreach (var altar in Object.FindObjectsByType<Checkpoint>(FindObjectsInactive.Include))
            {
                if (altar.CheckpointId.StartsWith("capital-")) { found.Add(altar.gameObject); }
            }

            foreach (var find in Object.FindObjectsByType<WorldPickup>(FindObjectsInactive.Include))
            {
                if (find.PersistentId != null && find.PersistentId.StartsWith("capital-")) { found.Add(find.gameObject); }
            }

            foreach (var enemy in Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Include))
            {
                var x = enemy.transform.position.x;
                if (x > 313.5f && x < 545f) { found.Add(enemy.gameObject); }
            }

            return found;
        }

        /// <summary>
        /// Who stands where (stage C): ravens over the market and the gardens, crossbowmen on the
        /// balconies, gargoyles on the belfry's rubble, flanking the temple's dais and before the
        /// palace, priests on the dais and the pergola. The gardens have one of each. Heights are
        /// body centres: feet on the floor at 0 are 0.9 up, as the forest's are.
        /// </summary>
        private static void PlaceCapitalEnemies()
        {
            var raven = CapitalEnemy(RavenPrefabName);
            var gargoyle = CapitalEnemy(GargoylePrefabName);
            var priest = CapitalEnemy(PriestPrefabName);
            var crossbowman = CapitalEnemy(CrossbowmanPrefabName);
            if (raven == null || gargoyle == null || priest == null || crossbowman == null)
            {
                Debug.LogError("The capital's enemies are not built; run ContentBuilder.Build first.");
                return;
            }

            // The market.
            SpawnEnemy(raven, new Vector2(383f, 6f));
            SpawnEnemy(raven, new Vector2(395f, 6.5f));
            SpawnEnemy(crossbowman, new Vector2(379.5f, 3.5f));
            SpawnEnemy(crossbowman, new Vector2(397.5f, 3.5f));
            // The bell tower: asleep on the fallen belfry.
            SpawnEnemy(gargoyle, new Vector2(414f, 7.3f));
            // The temple: two stone guardians either side of the dais, a priest upon it.
            SpawnEnemy(gargoyle, new Vector2(437.5f, 0.9f));
            SpawnEnemy(gargoyle, new Vector2(454f, 0.9f));
            SpawnEnemy(priest, new Vector2(446f, 2.5f));
            // The gardens: all four together.
            SpawnEnemy(raven, new Vector2(466f, 6f));
            SpawnEnemy(gargoyle, new Vector2(476f, 0.9f));
            SpawnEnemy(priest, new Vector2(481f, 4.1f));
            SpawnEnemy(crossbowman, new Vector2(488f, 0.9f));
            // Before the palace, one more stone guard.
            SpawnEnemy(gargoyle, new Vector2(507f, 0.9f));
        }

        /// <summary>
        /// The Forest's sealed road becomes the way here: the same seal at x = 313.5, now opened by
        /// the Warden's death instead of never. Found by either of its names, so running this on a
        /// world built before the capital and on one built after gives the same door.
        /// </summary>
        private static void OpenCapitalRoad(GameProgress progress)
        {
            foreach (var door in Object.FindObjectsByType<PersistentDoor>(FindObjectsInactive.Include))
            {
                if (door.name != "FutureRoadGate" && door.name != CapitalRoadGateName)
                {
                    continue;
                }

                door.name = CapitalRoadGateName;
                var serialized = new SerializedObject(door);
                serialized.FindProperty("_requirement._kind").enumValueIndex = (int)RequirementKind.BossDefeated;
                serialized.FindProperty("_requirement._id").stringValue = WardenBossId;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // ---------- rooms ----------

        private static void BuildAshRoad(Transform parent)
        {
            var section = Section(parent, "AshRoad");
            CapitalBlock(section, "AshRoadFloor", new Vector2(329.25f, -0.5f), new Vector2(31.5f, 1f));
            CapitalProp(section, "env_capital_cart_01", new Vector2(321f, 0f), "EnvironmentBack", 2);
            CapitalProp(section, "env_capital_column_01", new Vector2(327f, 0f), "EnvironmentBack", 1, 1.1f);
            CapitalProp(section, "env_capital_banner_01", new Vector2(331.5f, 1.2f), "EnvironmentBack", 3, 0.9f);
            CapitalProp(section, "env_capital_cart_01", new Vector2(339f, 0f), "EnvironmentBack", 2, 0.9f, flip: true);
            CapitalProp(section, "env_capital_tree_burnt_01", new Vector2(343f, 0f), "EnvironmentBack", 1);
            Brazier(section, new Vector2(335f, 0f));
        }

        private static void BuildCityGate(Transform parent, GameObject checkpoint,
            PlayerRespawner respawner, GameProgress progress, GameObject player)
        {
            var section = Section(parent, "CityGate");
            CapitalBlock(section, "CityGateFloor", new Vector2(358.5f, -0.5f), new Vector2(27f, 1f));
            CapitalProp(section, "env_capital_gate_01", new Vector2(351f, 0f), "EnvironmentBack", 0, 1f);
            CapitalProp(section, "env_capital_statue_01", new Vector2(365f, 0f), "EnvironmentBack", 2);
            Brazier(section, new Vector2(362f, 0f));
            if (checkpoint != null)
            {
                PlaceCheckpoint(checkpoint, new Vector2(358f, 0.8f), "capital-gate",
                    "Вівтар міської брами", respawner, progress, player);
            }
        }

        /// <summary>
        /// The market: the widest floor in the zone, with stone balconies either side where the
        /// crossbowmen will stand. The high one holds a purse; nothing on the way needs them.
        /// </summary>
        private static void BuildMarket(Transform parent, GameObject pickup, GameProgress progress)
        {
            var section = Section(parent, "Market");
            CapitalBlock(section, "MarketFloor", new Vector2(388f, -0.5f), new Vector2(32f, 1f));
            // Balconies high enough to walk under (their undersides at 2.1 and 3.7, the hero is
            // 1.6), each reached from a heap of stalls at 1.2. The stalls stand 1.5 clear of the
            // balconies: a jump onto them from under a balcony would strike its underside.
            CapitalBlock(section, "StallsWest", new Vector2(375f, 0.6f), new Vector2(2f, 1.2f), CapitalSurface.Rubble);
            CapitalBlock(section, "BalconyWest", new Vector2(379.75f, 2.35f), new Vector2(4.5f, 0.5f), CapitalSurface.Ledge);
            CapitalBlock(section, "BalconyHigh", new Vector2(387.5f, 3.95f), new Vector2(5f, 0.5f), CapitalSurface.Ledge);
            CapitalBlock(section, "BalconyEast", new Vector2(397.5f, 2.35f), new Vector2(4f, 0.5f), CapitalSurface.Ledge);
            CapitalBlock(section, "StallsEast", new Vector2(402f, 0.6f), new Vector2(2f, 1.2f), CapitalSurface.Rubble);
            CapitalProp(section, "env_capital_house_01", new Vector2(377f, 0f), "EnvironmentBack", 0);
            CapitalProp(section, "env_capital_house_02", new Vector2(389f, 0f), "EnvironmentBack", 0);
            CapitalProp(section, "env_capital_house_01", new Vector2(401f, 0f), "EnvironmentBack", 0, 1f, flip: true);
            CapitalProp(section, "env_capital_cart_01", new Vector2(393f, 0f), "EnvironmentBack", 3, 0.8f);
            if (pickup != null)
            {
                PlacePickup(pickup, new Vector2(387.5f, 4.9f), progress, string.Empty, true, 120,
                    "capital-market-purse", false);
            }
        }

        /// <summary>
        /// The bell tower: the street is buried under the fallen belfry, so the way on is up the
        /// stairs, over the rubble at 6.4 and down the other side — four steps of 1.6 each way.
        /// The nest sits east of the rubble at 8.2, over the last steps: a jump from the top (1.97)
        /// rises high enough but not far enough (the gap is 4, a jump is at its peak about 2.5
        /// out and falling after); a dash at the top of that jump, or the second jump, makes it.
        /// From the steps below it is out of reach of every jump.
        /// </summary>
        private static void BuildBellTower(Transform parent, GameObject pickup, GameProgress progress)
        {
            var section = Section(parent, "BellTower");
            CapitalBlock(section, "TowerFloor", new Vector2(414f, -0.5f), new Vector2(20f, 1f));
            var steps = new[] { (406f, 1.6f), (408f, 3.2f), (410f, 4.8f), (418f, 4.8f), (420f, 3.2f), (422f, 1.6f) };
            foreach (var (x, top) in steps)
            {
                CapitalBlock(section, $"Stair_{x:0}", new Vector2(x, top * 0.5f), new Vector2(2f, top),
                    CapitalSurface.Masonry);
            }

            var rubble = CapitalBlock(section, "FallenBelfry", new Vector2(414f, 3.2f), new Vector2(6f, 6.4f),
                CapitalSurface.Rubble);
            Ensure<NonClimbableSurface>(rubble);
            CapitalProp(section, "env_capital_bell_01", new Vector2(414f, 6.4f), "GameplayEnvironment", 2, 0.8f);

            var nest = CapitalBlock(section, "BellNest", new Vector2(422.5f, 7.95f), new Vector2(3f, 0.5f),
                CapitalSurface.Ledge);
            Ensure<NonClimbableSurface>(nest);
            AreaTrigger(section, CapitalBellNestArea, new Vector2(422.5f, 9.1f), new Vector2(2.6f, 1.4f));
            PlaceAbilityGate(section, "BellNestHint", new Vector2(416f, 7.2f), AbilityIds.AirDash,
                "conn_capital_tower_nest", new Vector2(422.5f, 8.8f), progress);
            if (pickup != null)
            {
                PlacePickup(pickup, new Vector2(422.5f, 8.8f), progress, string.Empty, true, 250,
                    "capital-bell-nest", false);
            }
        }

        private static void BuildTempleQuarter(Transform parent, GameObject checkpoint,
            PlayerRespawner respawner, GameProgress progress, GameObject player)
        {
            var section = Section(parent, "TempleQuarter");
            CapitalBlock(section, "TempleFloor", new Vector2(441f, -0.5f), new Vector2(34f, 1f));
            CapitalBlock(section, "TempleDais", new Vector2(446f, 0.8f), new Vector2(12f, 1.6f), CapitalSurface.Masonry);
            CapitalProp(section, "env_capital_temple_01", new Vector2(446f, 1.6f), "EnvironmentBack", 0);
            Brazier(section, new Vector2(441f, 1.6f));
            Brazier(section, new Vector2(451f, 1.6f));
            if (checkpoint != null)
            {
                PlaceCheckpoint(checkpoint, new Vector2(430f, 0.8f), "capital-temple",
                    "Храмовий вівтар", respawner, progress, player);
            }
        }

        private static void BuildBurntGardens(Transform parent, GameObject pickup, GameProgress progress)
        {
            var section = Section(parent, "BurntGardens");
            CapitalBlock(section, "GardensFloor", new Vector2(475f, -0.5f), new Vector2(34f, 1f));
            CapitalBlock(section, "Terrace", new Vector2(470f, 0.8f), new Vector2(8f, 1.6f), CapitalSurface.Masonry);
            CapitalBlock(section, "Pergola", new Vector2(481f, 2.95f), new Vector2(6f, 0.5f), CapitalSurface.Ledge);
            foreach (var x in new[] { 462f, 468f, 476f, 486f })
            {
                CapitalProp(section, "env_capital_tree_burnt_01", new Vector2(x, x == 468f ? 1.6f : 0f),
                    "EnvironmentBack", 1, 0.8f + (x % 3f) * 0.1f, flip: x == 476f);
            }

            CapitalProp(section, "env_capital_statue_01", new Vector2(489f, 0f), "EnvironmentBack", 2, 0.9f, flip: true);
            if (pickup != null)
            {
                PlacePickup(pickup, new Vector2(481f, 3.6f), progress, "healing_herb", false, 1,
                    "capital-gardens-herb", false);
            }
        }

        /// <summary>
        /// Before the throne: the last altar, and above it the royal gallery at 6.8 — twice what a
        /// second jump reaches from this bare floor, with no wall or ledge near. Rings to it come with the King's own gift.
        /// </summary>
        private static void BuildPalaceApproach(Transform parent, GameObject checkpoint, GameObject pickup,
            PlayerRespawner respawner, GameProgress progress, GameObject player)
        {
            var section = Section(parent, "PalaceApproach");
            CapitalBlock(section, "ApproachFloor", new Vector2(502f, -0.5f), new Vector2(20f, 1f));
            CapitalProp(section, "env_capital_column_01", new Vector2(494.5f, 0f), "EnvironmentBack", 1, 1.4f);
            CapitalProp(section, "env_capital_column_01", new Vector2(509.5f, 0f), "EnvironmentBack", 1, 1.4f, flip: true);
            CapitalProp(section, "env_capital_banner_01", new Vector2(502f, 4.5f), "EnvironmentBack", 3, 1.1f);
            var gallery = CapitalBlock(section, "RoyalGallery", new Vector2(502f, 6.55f), new Vector2(14f, 0.5f),
                CapitalSurface.Ledge);
            Ensure<NonClimbableSurface>(gallery);
            AreaTrigger(section, CapitalGalleryArea, new Vector2(502f, 7.7f), new Vector2(12f, 1.6f));
            // Past the gallery's east end, above it: the line lets go 1.9 under the ring and the pop
            // tops out at 1.1 under it, so the second jump (+1.5) is what clears the shelf, and a
            // step west once over it puts the hero on top. Clear of its face, so it is no wall jump. Its underside would stop a line thrown through it.
            PlaceGrappleRing(section, "GalleryRing", new Vector2(511f, 8f));
            if (pickup != null)
            {
                PlacePickup(pickup, new Vector2(498f, 7.3f), progress, string.Empty, true, 300,
                    "capital-gallery-purse", true);
            }
            PlaceAbilityGate(section, "GalleryHint", new Vector2(500f, 0.8f), AbilityIds.Grapple,
                "conn_capital_approach_gallery", new Vector2(502f, 7.3f), progress);
            if (checkpoint != null)
            {
                PlaceCheckpoint(checkpoint, new Vector2(496f, 0.8f), "capital-palace",
                    "Вівтар біля палацу", respawner, progress, player);
            }
        }

        /// <summary>
        /// The Throne Hall: a flat arena 33 wide before the throne, sealed behind the hero by the
        /// King's fight (stage D), and the road east sealed for the ending's sake.
        /// </summary>
        private static void BuildThroneHall(Transform parent, GameProgress progress, PlayerRespawner respawner)
        {
            var section = Section(parent, "ThroneHall");
            CapitalBlock(section, "ThroneFloor", new Vector2(528.5f, -0.5f), new Vector2(33f, 1f));
            var barrier = CapitalBlock(section, "ThroneBarrier", new Vector2(512f, 3.5f), new Vector2(1f, 9f),
                CapitalSurface.Rubble);
            barrier.SetActive(false);
            BuildKingEncounter(section, barrier, progress, respawner);
            CapitalProp(section, "env_capital_throne_01", new Vector2(539f, 0f), "EnvironmentBack", 1);
            CapitalProp(section, "env_capital_banner_01", new Vector2(520f, 4f), "EnvironmentBack", 3);
            CapitalProp(section, "env_capital_banner_01", new Vector2(532f, 4f), "EnvironmentBack", 3, 1f, flip: true);
            Brazier(section, new Vector2(516f, 0f));
            Brazier(section, new Vector2(541f, 0f));

            var seal = CapitalBlock(section, "EastRoadSeal", new Vector2(545f, 4f), new Vector2(1f, 10f),
                CapitalSurface.Rubble);
            Ensure<NonClimbableSurface>(seal);
            PlaceDoor(section, "EastRoadGate", new Vector2(545f, 4f), RequirementKind.Unavailable,
                "zone_unknown_east", seal, progress);
        }

        /// <summary>
        /// The capital's light and sound in the world scene's services: its ambient light for
        /// ZoneLighting and its room tone for AmbienceController, each added or replaced by zone
        /// id so running it again changes nothing.
        /// </summary>
        public static void ConfigureCapitalAtmosphere()
        {
            var lighting = Object.FindAnyObjectByType<ZoneLighting>(FindObjectsInactive.Include);
            if (lighting != null)
            {
                var serialized = new SerializedObject(lighting);
                var entry = Upsert(serialized.FindProperty("_zones"), "_zoneId");
                WriteZoneLight(entry, CapitalZoneId, new Color(0.8f, 0.64f, 0.58f), 0.8f);
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            var ambience = Object.FindAnyObjectByType<AmbienceController>(FindObjectsInactive.Include);
            if (ambience != null)
            {
                var serialized = new SerializedObject(ambience);
                var palette = Upsert(serialized.FindProperty("_palettes"), "_zoneId");
                palette.FindPropertyRelative("_zoneId").stringValue = CapitalZoneId;
                palette.FindPropertyRelative("_bed").enumValueIndex = (int)AudioId.AmbienceCapitalBase;
                var details = palette.FindPropertyRelative("_details");
                var sounds = new[] { AudioId.AmbienceBellToll, AudioId.AmbienceEmbers,
                    AudioId.AmbienceStoneSettle, AudioId.AmbienceWind };
                details.arraySize = sounds.Length;
                for (var i = 0; i < sounds.Length; i++)
                {
                    details.GetArrayElementAtIndex(i).enumValueIndex = (int)sounds[i];
                }

                // A dead city is quieter than the forest and a little busier than the crypt.
                palette.FindPropertyRelative("_minimumGap").floatValue = 6f;
                palette.FindPropertyRelative("_maximumGap").floatValue = 18f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>The capital's entry of a zone list, made if it is not there yet.</summary>
        private static SerializedProperty Upsert(SerializedProperty list, string idField)
        {
            for (var i = 0; i < list.arraySize; i++)
            {
                var entry = list.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative(idField).stringValue == CapitalZoneId)
                {
                    return entry;
                }
            }

            list.arraySize++;
            return list.GetArrayElementAtIndex(list.arraySize - 1);
        }

        private const string CapitalBellNestArea = "room_capital_bell_nest";
        private const string CapitalGalleryArea = "room_capital_royal_gallery";

        // ---------- backdrop and air ----------

        private static void BuildCapitalParallax(Transform parent, Transform camera)
        {
            var layers = new[]
            {
                ("env_capital_bg_far_01", "BackgroundFar", 0.12f, 0.04f),
                ("env_capital_bg_mid_01", "BackgroundMid", 0.32f, 0.09f),
                ("env_capital_bg_near_01", "BackgroundNear", 0.58f, 0.16f)
            };

            var root = Section(parent, "CapitalParallax");
            SetString(root.gameObject.AddComponent<ZoneBackdrop>(), "_zoneId", CapitalZoneId);
            foreach (var (spriteName, layer, factor, vertical) in layers)
            {
                var art = CapitalArt(spriteName);
                if (art == null)
                {
                    continue;
                }

                var strip = new GameObject(spriteName);
                strip.transform.SetParent(root);
                strip.transform.position = new Vector3(430f, -4f, 0f);
                var renderer = strip.AddComponent<SpriteRenderer>();
                if (layer != "BackgroundNear")
                {
                    renderer.sharedMaterial = _material;
                }

                renderer.sprite = art;
                renderer.drawMode = SpriteDrawMode.Tiled;
                renderer.tileMode = SpriteTileMode.Continuous;
                renderer.size = new Vector2(art.bounds.size.x * 3f, art.bounds.size.y);
                SetSorting(renderer, layer, 1);
                var parallax = strip.AddComponent<ParallaxLayer>();
                SetFloat(parallax, "_factor", factor);
                SetFloat(parallax, "_verticalFactor", vertical);
                SetReference(parallax, "_camera", camera);
            }
        }

        /// <summary>Ash lying along the streets in two banks, as fog lies in the Tomb and the Forest.</summary>
        private static void BuildCapitalAsh(Transform parent)
        {
            var ash = CapitalArt("env_capital_ash_bank_01");
            if (ash == null)
            {
                return;
            }

            var bank = Section(parent, "Ash_" + CapitalZoneId);
            SetString(bank.gameObject.AddComponent<ZoneBackdrop>(), "_zoneId", CapitalZoneId);
            FogLayer(bank, ash, 430f, 232f, 2.6f, 5f, new Color(0.62f, 0.52f, 0.48f, 0.22f),
                "BackgroundMid", 20, 0.3f);
            FogLayer(bank, ash, 430f, 232f, 0.55f, 1.7f, new Color(0.7f, 0.6f, 0.55f, 0.16f),
                "EnvironmentBack", 40, 0.5f);
        }

        // ---------- pieces ----------

        private static GameObject CapitalBlock(Transform parent, string name, Vector2 centre, Vector2 size,
            CapitalSurface surface = CapitalSurface.Flagstone)
        {
            var block = new GameObject(name);
            block.transform.SetParent(parent);
            block.transform.position = centre;
            block.layer = GroundLayer;
            block.AddComponent<BoxCollider2D>().size = size;

            var visual = FindOrCreateChild(block, "Visual");
            var renderer = visual.AddComponent<SpriteRenderer>();
            var tile = CapitalArt(surface == CapitalSurface.Masonry ? "env_capital_masonry_01"
                : surface == CapitalSurface.Ledge ? "env_capital_ledge_01"
                : surface == CapitalSurface.Rubble ? "env_capital_rubble_01"
                : "env_capital_flagstone_01");
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
                renderer.color = new Color(0.25f, 0.23f, 0.22f);
            }

            SetSorting(renderer, "GameplayEnvironment", 0);
            return block;
        }

        /// <summary>Dressing; never a collider.</summary>
        private static GameObject CapitalProp(Transform parent, string spriteName, Vector2 position,
            string layer, int order, float scale = 1f, bool flip = false)
        {
            var sprite = CapitalArt(spriteName);
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
            SetSorting(renderer, layer, order);
            return prop;
        }

        /// <summary>A brazier still burning, with the warm light it throws on the street.</summary>
        private static void Brazier(Transform parent, Vector2 foot)
        {
            var prop = CapitalProp(parent, "env_capital_brazier_01", foot, "EnvironmentBack", 4, 0.8f);
            if (prop == null)
            {
                return;
            }

            var glow = LightChild(prop.transform, "BrazierLight", foot + new Vector2(0f, 1.3f));
            AddLight(glow, Light2D.LightType.Point, new Color(1f, 0.55f, 0.28f), 0.9f, 4.5f, 0.4f);
        }
    }
}
