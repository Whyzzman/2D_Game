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

        public const string CapitalLayoutFile = "capital.txt";

        /// <summary>The Capital's painted art (item 6e; approved look — design/levels/capital-look).</summary>
        public const string CapitalArtFolder = "Capital";

        private static bool _capitalPainted;
        private static LevelZone _capitalLayout;

        /// <summary>The Capital's text (Levels/capital.txt), read once per build.</summary>
        public static LevelZone CapitalLayout => _capitalLayout ?? (_capitalLayout = LoadLayout(CapitalLayoutFile));

        /// <summary>A named spot of the Capital's text: its floor, mid-cell.</summary>
        public static Vector2 CapitalSpot(string name) => SpotFeet(CapitalLayout, name);

        /// <summary>
        /// The whole zone from its text (item 6a of the 15 zones, the user's map
        /// reference/maps/04_ruined_capital.webp): three tiers round the bell tower, the only way
        /// between them; run inside a world opened whole. The trial of arms keeps the Ash Road's old
        /// numbers, which the text keeps true; the King waits in the throne hall until the
        /// Commander takes it (6b) and he moves to the Throne (6d).
        /// </summary>
        public static void BuildCapital()
        {
            if (_sprite == null) { _sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath); }
            if (_material == null) { _material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath); }
            if (_shapes == null) { _shapes = ProductionSpriteArt.Generate(); }
            if (_vfx == null) { _vfx = PlaceholderVfxArt.Generate(); }
            _capital = PlaceholderCapitalArt.Generate();
            _capitalLayout = null;

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

            var zone = CapitalLayout;
            var root = Section(level.transform, CapitalRootName);
            var context = LevelContext.FromScene();
            _capitalPainted = HasPaintedArt(CapitalArtFolder);
            foreach (var problem in BuildLayout(zone, root, CapitalLayoutBlock, context))
            {
                Debug.LogError($"Capital layout: {problem}");
            }

            var rooms = new Dictionary<string, Transform>();
            foreach (var room in zone.Rooms)
            {
                rooms[room.Id] = root.Find(room.Id);
            }

            BuildCapitalDressing(rooms);
            var respawner = Object.FindAnyObjectByType<PlayerRespawner>();
            BuildCommanderHall(rooms[CapitalThroneRoom], context.Progress, respawner);
            BuildKingsThrone(rooms[CapitalKingsRoom], context.Progress, respawner);
            foreach (var link in zone.Links)
            {
                if (link.Ability == AbilityIds.Grapple && link.To == CapitalGalleryArea)
                {
                    PlaceAbilityGate(rooms[link.From], "GalleryHint", CapitalSpot("gallery-hint") + new Vector2(0f, 0.8f),
                        link.Ability, LinkId(link), Inside(zone, zone.Room(link.To)).center, context.Progress);
                }
            }

            var camera = Camera.main != null ? Camera.main.transform : null;
            BuildGrappleReturns(level.transform, context.Progress, null);
            if (_capitalPainted)
            {
                // The painter draws the stone and each room's sky and halls; the props and their fire stay.
                PlacePaintedArt(CapitalArtFolder, zone, root);
            }
            else
            {
                BuildCapitalParallax(root, camera);
            }

            BuildCapitalAsh(root);
        }

        private const string CapitalThroneRoom = "room_capital_throne_hall";

        private static GameProgress CapitalLayoutProgress() => Object.FindAnyObjectByType<GameProgress>();
        private const string CapitalKingsRoom = "room_capital_kings_throne";

        /// <summary>How the Capital's stone looks: ledges as slabs, walls as masonry, floors as flagstones.</summary>
        private static GameObject CapitalLayoutBlock(Transform parent, string name, Rect world, LevelCell kind)
        {
            if (_capitalPainted)
            {
                return PaintedSolid(parent, name, world, CapitalArtFolder, CapitalLayout);
            }

            var surface = kind == LevelCell.Ledge ? CapitalSurface.Ledge
                : world.height > world.width ? CapitalSurface.Masonry : CapitalSurface.Flagstone;
            return CapitalBlock(parent, name, world.center, world.size, surface);
        }

        /// <summary>Each room's own props, placed by the text's spots; none of them carries collision.</summary>
        private static void BuildCapitalDressing(Dictionary<string, Transform> rooms)
        {
            Transform Dressing(string room) => Section(rooms[room], "Dressing");

            var road = Dressing("room_capital_ash_road");
            CapitalProp(road, "env_capital_cart_01", new Vector2(321f, 0f), "EnvironmentBack", 2);
            CapitalProp(road, "env_capital_column_01", new Vector2(327f, 0f), "EnvironmentBack", 1, 1.1f);
            CapitalProp(road, "env_capital_tree_burnt_01", new Vector2(343f, 0f), "EnvironmentBack", 1);
            Brazier(road, new Vector2(335f, 0f));

            var gate = Dressing("room_capital_city_gate");
            CapitalProp(gate, "env_capital_gate_01", new Vector2(351f, 0f), "EnvironmentBack", 0, 1f);
            Brazier(gate, new Vector2(364f, 0f));

            var square = Dressing("room_capital_angel_square");
            CapitalProp(square, "env_capital_statue_01", CapitalSpot("angel"), "EnvironmentBack", 2, 1.6f);
            CapitalProp(square, "env_capital_house_01", new Vector2(384f, 0f), "EnvironmentBack", 0);
            CapitalProp(square, "env_capital_house_02", new Vector2(415f, 0f), "EnvironmentBack", 0, 1f, flip: true);
            Brazier(square, CapitalSpot("angel") + new Vector2(-3f, 0f));
            Brazier(square, CapitalSpot("angel") + new Vector2(3f, 0f));

            CapitalProp(Dressing("room_capital_bell_tower"), "env_capital_bell_01", CapitalSpot("bell"),
                "GameplayEnvironment", 2, 0.8f);

            var temple = Dressing("room_capital_temple");
            CapitalProp(temple, "env_capital_temple_01", CapitalSpot("temple-dais"), "EnvironmentBack", 0);
            Brazier(temple, CapitalSpot("temple-dais") + new Vector2(-5f, 0f));

            var market = Dressing("room_capital_market");
            CapitalProp(market, "env_capital_house_01", new Vector2(386f, -21f), "EnvironmentBack", 0);
            CapitalProp(market, "env_capital_cart_01", new Vector2(401f, -21f), "EnvironmentBack", 3, 0.8f);

            var gardens = Dressing("room_capital_gardens");
            foreach (var x in new[] { 432f, 445f, 456f, 462f })
            {
                CapitalProp(gardens, "env_capital_tree_burnt_01", new Vector2(x, -19.5f),
                    "EnvironmentBack", 1, 0.8f + (x % 3f) * 0.1f, flip: x == 456f);
            }

            Brazier(Dressing("room_capital_workshop"), CapitalSpot("smith") + new Vector2(2f, 0f));
            CapitalProp(Dressing("room_capital_crypts"), "env_capital_column_01", new Vector2(396f, -42f),
                "EnvironmentBack", 1, 1.2f);
            CapitalProp(Dressing("room_capital_cistern"), "env_capital_column_01", new Vector2(444f, -40.5f),
                "EnvironmentBack", 1, 1.3f);

            // The three blue seals of the user's map: the palace's door, the ways to the Cathedral
            // and the Citadel — shut until their zones exist.
            foreach (var (spot, room) in new[] { ("seal-palace", CapitalKingsRoom), ("seal-cathedral", "room_capital_crypts"),
                ("seal-citadel", "room_capital_cistern") })
            {
                var seal = CapitalProp(Dressing(room), "env_capital_gate_01", CapitalSpot(spot), "EnvironmentBack", 1, 0.9f);
                if (seal != null)
                {
                    seal.name = "Seal_" + spot;
                    // A door that will not open until the zone behind it exists (item 6d).
                    PlaceDoor(Dressing(room), "SealDoor_" + spot, CapitalSpot(spot) + new Vector2(0f, 1.2f),
                        RequirementKind.Unavailable, string.Empty, seal, CapitalLayoutProgress());
                    seal.GetComponent<SpriteRenderer>().color = new Color(0.62f, 0.78f, 1f);
                    var host = LightChild(seal.transform, "SealLight", CapitalSpot(spot) + new Vector2(0f, 2.2f));
                    AddLight(host, Light2D.LightType.Point, new Color(0.5f, 0.7f, 1f), 0.8f, 3.6f, 0.3f, 0.6f);
                }
            }
        }

        /// <summary>
        /// The Commander's hall (item 6b; the old throne hall, its id kept for saves): sealed behind the
        /// hero by his fight, and the door east to the King's throne shut until he falls.
        /// </summary>
        private static void BuildCommanderHall(Transform room, GameProgress progress, PlayerRespawner respawner)
        {
            var section = Section(room, "Commander");
            var hall = Inside(CapitalLayout, CapitalLayout.Room(CapitalThroneRoom));
            var barrier = CapitalBlock(section, "CommanderBarrier", new Vector2(hall.xMin - 0.5f, hall.yMin + 4.5f),
                new Vector2(1f, 9f), CapitalSurface.Rubble);
            barrier.SetActive(false);
            BuildCapitalEncounter(section, CommanderPrefabName, "CommanderEncounter", barrier, progress, respawner,
                CapitalSpot("commander") + new Vector2(0f, 1.4f), new Vector2(hall.xMin + 4f, hall.yMin + 2f),
                CommanderRewardItemId, "Хоругва Командора", 1100, 2000);

            var door = CapitalBlock(section, "KingsDoor", new Vector2(hall.xMax + 0.5f, hall.yMin + 2.5f), new Vector2(1f, 5f),
                CapitalSurface.Masonry);
            Ensure<NonClimbableSurface>(door);
            PlaceDoor(section, "KingsDoorGate", door.transform.position, RequirementKind.BossDefeated, CommanderBossId,
                door, progress);
            CapitalProp(section, "env_capital_banner_01", new Vector2(hall.xMin + 8f, hall.yMin + 4f), "EnvironmentBack", 3);
            CapitalProp(section, "env_capital_banner_01", new Vector2(hall.xMax - 8f, hall.yMin + 4f), "EnvironmentBack", 3, 1f, flip: true);
            Brazier(section, new Vector2(hall.xMin + 4f, hall.yMin));
            Brazier(section, new Vector2(hall.xMax - 3f, hall.yMin));
        }

        /// <summary>
        /// The King's throne (item 6b): his arena now, past the Commander, until he moves to the
        /// Throne of Oblivion (item 17); the palace's blue seal stands behind him.
        /// </summary>
        private static void BuildKingsThrone(Transform room, GameProgress progress, PlayerRespawner respawner)
        {
            var section = Section(room, "Throne");
            var hall = Inside(CapitalLayout, CapitalLayout.Room(CapitalKingsRoom));
            var barrier = CapitalBlock(section, "ThroneBarrier", new Vector2(hall.xMin - 0.5f, hall.yMin + 4.5f),
                new Vector2(1f, 9f), CapitalSurface.Rubble);
            barrier.SetActive(false);
            BuildCapitalEncounter(section, KingPrefabName, "KingEncounter", barrier, progress, respawner,
                CapitalSpot("king") + new Vector2(0f, 1.4f), new Vector2(hall.xMin + 4f, hall.yMin + 2f),
                KingRewardItemId, "Печать Короля", 1200, 2000);
            CapitalProp(section, "env_capital_throne_01", CapitalSpot("throne"), "EnvironmentBack", 1);
            CapitalProp(section, "env_capital_banner_01", new Vector2(hall.xMin + 6f, hall.yMin + 4f), "EnvironmentBack", 3);
            Brazier(section, new Vector2(hall.xMin + 3f, hall.yMin));
            Brazier(section, new Vector2(hall.xMax - 2f, hall.yMin));
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
