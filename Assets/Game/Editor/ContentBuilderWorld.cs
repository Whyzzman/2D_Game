using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The kingdom as one world: the graph the map, discovery and fast travel read, the systems
    /// that run it, and the places added in PHASE 25 — secrets in the Tomb that only later movement
    /// reaches, the shortcuts, and the sealed road beyond the forest.
    ///
    /// Heights in this file are measured against the real jump, not the textbook one. The motor
    /// integrates at 50 Hz, so a full jump lifts the feet about 1.91 units (not v²/2g = 2.02) and a
    /// second jump adds about 1.41, for 3.32. Every gate below sits between what the player can
    /// do without its ability and what they can do with it, with room to spare on both sides; the
    /// tests hold the geometry to those numbers.
    /// </summary>
    public static partial class ContentBuilder
    {
        private const string WorldGraphPath = DataFolder + "/WorldGraph.asset";
        private const string FutureZoneId = "zone_unknown_east";
        private const string GuardianBossId = "guardian-of-the-tomb";

        // Area ids. Stable: the save records them, so renaming one loses the player's map.
        private const string TombEntrance = "room_tomb_entrance";
        private const string TombEastHall = "room_tomb_east_hall";
        private const string ForestTunnel = "room_forest_root_tunnel";
        private const string CapitalAshRoad = "room_capital_ash_road";
        private const string CapitalCityGate = "room_capital_city_gate";
        private const string CapitalMarket = "room_capital_market";
        private const string CapitalBellTower = "room_capital_bell_tower";
        private const string CapitalBellNest = "room_capital_bell_nest";
        private const string CapitalTemple = "room_capital_temple";
        private const string CapitalGardens = "room_capital_gardens";
        private const string CapitalPalaceApproach = "room_capital_palace_approach";
        private const string CapitalThroneHall = "room_capital_throne_hall";
        private const string CapitalGallery = "room_capital_royal_gallery";
        private const string BeyondCapital = "room_beyond_capital";
        // Rooms behind breakable walls (item 4, stage B).
        private const string CapitalRoofCache = "room_capital_roof_cache";
        private const string CapitalHiddenTreasury = "room_capital_hidden_treasury";

        public const string ForestRoadGateShortcut = "shortcut_forest_road_gate";

        /// <summary>World units to map units. The map is spatially honest, not 1:1.</summary>
        private const float MapScale = 0.1f;

        /// <summary>Area bounds waiting to be written once the rest of the graph is parsed.</summary>
        private static readonly Dictionary<string, Rect> _pendingBounds = new Dictionary<string, Rect>();

        // ---------- rewards ----------

        /// <summary>
        /// What the backtracking secrets hold. Each is a piece of equipment with a clear use, so
        /// a return trip pays for itself; none is the only way to make a build work.
        /// </summary>
        private static IEnumerable<ItemDefinition> BuildWorldItems()
        {
            var signet = BuildAccessory("ossuary_signet", "Кістки королів тримають удар.",
                new[]
                {
                    new StatModifier(StatType.Vitality, ModifierOperation.Add, 3f),
                    new StatModifier(StatType.StaggerResistance, ModifierOperation.Add, 0.08f)
                });
            var charm = BuildAccessory("reliquary_charm", "Тепла, наче в ній ще горить свічка.",
                new[]
                {
                    new StatModifier(StatType.Intelligence, ModifierOperation.Add, 3f),
                    new StatModifier(StatType.MaxMana, ModifierOperation.Add, 15f)
                });
            var heart = BuildAccessory("verdant_heart", "Ліс, який ще не згнив.",
                new[]
                {
                    new StatModifier(StatType.Endurance, ModifierOperation.Add, 3f),
                    new StatModifier(StatType.PoisonResistance, ModifierOperation.Add, 0.25f)
                });
            var hood = BuildArmor("bellringer_hood", EquipmentSlot.Head, 5f, 2f, 0.04f);

            return new[]
            {
                BuildItem("ossuary_signet", "Перстень Костниці",
                    "Печатка з королівської костниці. Холодна, скільки не грій.",
                    ItemCategory.Accessory, ItemRarity.Rare, 1, true, true, 260,
                    new Color(0.86f, 0.82f, 0.7f), signet),
                BuildItem("reliquary_charm", "Оберіг Реліквіарію",
                    "Уламок реліквіарію на ланцюжку. Хтось молився на нього до останнього.",
                    ItemCategory.Accessory, ItemRarity.Rare, 1, true, true, 240,
                    new Color(0.9f, 0.72f, 0.45f), charm),
                BuildItem("bellringer_hood", "Каптур Дзвонаря",
                    "Дзвонар бив на сполох, поки дзвін не впав. Каптур лишився на гачку.",
                    ItemCategory.Armor, ItemRarity.Rare, 1, true, true, 180,
                    new Color(0.5f, 0.46f, 0.52f), hood),
                BuildItem("verdant_heart", "Зелене Серце",
                    "Вузол живого дерева з-за Вартового. Воно ще б'ється.",
                    ItemCategory.Accessory, ItemRarity.Legendary, 1, true, true, 400,
                    new Color(0.45f, 0.82f, 0.45f), heart)
            };
        }

        // ---------- the graph ----------

        /// <summary>
        /// The world as data. Every number here was read off the level geometry below and in the
        /// forest file; the tests check that each area's bounds really contain its altars, and that
        /// the main path is walkable where the graph says it is.
        /// </summary>
        private static WorldGraph BuildWorldGraph()
        {
            var graph = CreateAsset<WorldGraph>(WorldGraphPath);
            var json = new StringBuilder("{");

            json.Append(WorldZonesJson()).Append(WorldRoadsJson());

            // The Tomb's and the Forest's rooms, links and altars come from their texts (Levels/*.txt).
            var tomb = LayoutGraphJson(LoadLayout(TombLayoutFile));
            var forest = LayoutGraphJson(LoadLayout(ForestLayoutFile));
            json.Append("\"_areas\":[").Append(string.Join(",", tomb.areas)).Append(",")
                .Append(string.Join(",", forest.areas)).Append(",").Append(string.Join(",", new[]
            {
                MainRoom(CapitalAshRoad, CapitalZoneId, "Попелястий тракт", 318f, 345f),
                MainRoom(CapitalCityGate, CapitalZoneId, "Міська брама", 345f, 372f),
                MainRoom(CapitalMarket, CapitalZoneId, "Ринкова площа", 372f, 404f),
                MainRoom(CapitalBellTower, CapitalZoneId, "Дзвінниця", 404f, 424f),
                Pocket(CapitalBellNest, CapitalZoneId, "Гніздо на дзвінниці", 421f, 424f, 8.2f,
                    secret: true, ability: AbilityIds.AirDash),
                MainRoom(CapitalTemple, CapitalZoneId, "Храмовий квартал", 424f, 458f),
                MainRoom(CapitalGardens, CapitalZoneId, "Спалені сади", 458f, 492f),
                MainRoom(CapitalPalaceApproach, CapitalZoneId, "Підступ до палацу", 492f, 512f),
                Pocket(CapitalGallery, CapitalZoneId, "Королівська галерея", 494f, 510f, 6.8f,
                    secret: true, ability: AbilityIds.Grapple),
                MainRoom(CapitalThroneHall, CapitalZoneId, "Тронна зала", 512f, 545f,
                    boss: CapitalKingBossId),
                Pocket(CapitalRoofCache, CapitalZoneId, "Сховок під дахом", 424.5f, 428.5f, 8.2f,
                    secret: true, ability: string.Empty, mapRow: 2.3f),
                Pocket(CapitalHiddenTreasury, CapitalZoneId, "Прихована скарбниця", 490.5f, 494.5f, 6.8f,
                    secret: true, ability: string.Empty),
                AreaJson(BeyondCapital, FutureZoneId, string.Empty,
                    new Vector2(551f * MapScale, 0f), new Vector2(0.8f, 1.2f),
                    Rect.MinMaxRect(545f, -8f, 557f, 16f), optional: true)
            })).Append("],");

            json.Append("\"_connections\":[").Append(string.Join(",", tomb.links)).Append(",")
                .Append(string.Join(",", forest.links)).Append(",").Append(string.Join(",", new[]
            {
                LinkJson("conn_tomb_to_forest", TombEastHall, ForestTunnel,
                    type: ConnectionType.ZoneTransition, boss: GuardianBossId),
                // The road the Warden kept: sealed until he falls, then the way to the capital.
                LinkJson("conn_forest_to_capital", "room_forest_far_gate", CapitalAshRoad,
                    type: ConnectionType.ZoneTransition, boss: WardenBossId),
                LinkJson("conn_capital_road_gate", CapitalAshRoad, CapitalCityGate),
                LinkJson("conn_capital_gate_market", CapitalCityGate, CapitalMarket),
                LinkJson("conn_capital_market_tower", CapitalMarket, CapitalBellTower),
                LinkJson("conn_capital_tower_nest", CapitalBellTower, CapitalBellNest,
                    MapDirection.North, ConnectionType.AbilityGate, ability: AbilityIds.AirDash),
                LinkJson("conn_capital_tower_temple", CapitalBellTower, CapitalTemple),
                LinkJson("conn_capital_temple_gardens", CapitalTemple, CapitalGardens),
                LinkJson("conn_capital_gardens_approach", CapitalGardens, CapitalPalaceApproach),
                LinkJson("conn_capital_approach_gallery", CapitalPalaceApproach, CapitalGallery,
                    MapDirection.North, ConnectionType.AbilityGate, ability: AbilityIds.Grapple),
                LinkJson("conn_capital_approach_throne", CapitalPalaceApproach, CapitalThroneHall,
                    type: ConnectionType.Door),
                // Behind breakable walls: three blows open each for good (item 4, stage B).
                LinkJson("conn_capital_nest_cache", CapitalBellNest, CapitalRoofCache),
                LinkJson("conn_capital_gallery_treasury", CapitalGallery, CapitalHiddenTreasury,
                    MapDirection.West),
                // East of the throne the road goes on, sealed: what lies there is the ending's.
                LinkJson("conn_capital_throne_beyond", CapitalThroneHall, BeyondCapital,
                    type: ConnectionType.Gate, boss: CapitalKingBossId, future: true)
            })).Append("],");

            json.Append("\"_checkpoints\":[").Append(string.Join(",", tomb.altars)).Append(",")
                .Append(string.Join(",", forest.altars)).Append(",").Append(string.Join(",", new[]
            {
                AltarJson("capital-gate", "Вівтар міської брами", CapitalZoneId, CapitalCityGate),
                AltarJson("capital-temple", "Храмовий вівтар", CapitalZoneId, CapitalTemple),
                AltarJson("capital-palace", "Вівтар біля палацу", CapitalZoneId,
                    CapitalPalaceApproach)
            })).Append("],");

            json.Append("\"_abilities\":[").Append(string.Join(",", new[]
            {
                AbilityJson(AbilityIds.Dash, "Ривок", AbilityKind.TraversalProgression, string.Empty),
                // The Warden hands it over inside the same transaction as the kill, and the way
                // past the Warden is shut until the kill: everyone beyond him has it.
                AbilityJson(AbilityIds.DoubleJump, "Подвійний стрибок",
                    AbilityKind.TraversalProgression, WardenBossId),
                // Offered at the Tomb's entrance but never forced on the player.
                AbilityJson(AbilityIds.AirDash, "Ривок у повітрі", AbilityKind.OptionalTraversal,
                    string.Empty),
                AbilityJson(AbilityIds.WallRun, "Біг по стіні", AbilityKind.OptionalTraversal,
                    string.Empty),
                // Bought on the skill tree. A build choice, so the world never asks for it.
                AbilityJson(AbilityIds.ShadowDash, "Тіньовий ривок", AbilityKind.CombatSkill,
                    string.Empty),
                // The Ashen King's: every ring in the kingdom asks for it, never the main path.
                AbilityJson(AbilityIds.Grapple, "Гак-кішка", AbilityKind.TraversalProgression,
                    CapitalKingBossId)
            })).Append("],");

            json.Append("\"_startAreaId\":\"").Append(TombEntrance).Append("\",");
            json.Append("\"_fastTravelRequirement\":{\"_kind\":")
                .Append((int)RequirementKind.BossDefeated)
                .Append(",\"_id\":\"").Append(GuardianBossId).Append("\"},");
            json.Append("\"_fastTravelOverrideId\":\"fast-travel\"}");

            JsonUtility.FromJsonOverwrite(json.ToString(), graph);

            // Rect does not read back from JSON (it lands as all zeros, silently), so the bounds
            // collected above are written through the serializer instead.
            var serialized = new SerializedObject(graph);
            var areas = serialized.FindProperty("_areas");
            for (var i = 0; i < areas.arraySize; i++)
            {
                var area = areas.GetArrayElementAtIndex(i);
                var id = area.FindPropertyRelative("_areaId").stringValue;
                if (_pendingBounds.TryGetValue(id, out var bounds))
                {
                    area.FindPropertyRelative("_worldBounds").rectValue = bounds;
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            _pendingBounds.Clear();
            BuildMapArt(graph);
            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();

            var problems = WorldGraphValidator.Validate(graph);
            foreach (var problem in problems)
            {
                Debug.LogError("World graph: " + problem, graph);
            }

            return graph;
        }

        /// <summary>A room on the main row of the map, spanning the level from one x to another.</summary>
        private static string MainRoom(string id, string zone, string name, float fromX, float toX,
            string boss = "", float top = 18f)
        {
            var width = (toX - fromX) * MapScale;
            return AreaJson(id, zone, name,
                new Vector2((fromX + toX) * 0.5f * MapScale, 0f),
                new Vector2(Mathf.Max(0.6f, width - 0.15f), 1.2f),
                Rect.MinMaxRect(fromX, -8f, toX, top), boss: boss,
                icon: string.IsNullOrEmpty(boss) ? MapIconType.None : MapIconType.Boss);
        }

        /// <summary>
        /// A small room above the path, reported by its own trigger rather than by bounds: brushing
        /// past its ledge must not count as having been inside.
        /// </summary>
        private static string Pocket(string id, string zone, string name, float fromX, float toX,
            float floorY, bool secret, string ability, float mapRow = 1.6f)
        {
            return AreaJson(id, zone, name,
                new Vector2((fromX + toX) * 0.5f * MapScale, mapRow), new Vector2(0.7f, 0.6f),
                Rect.MinMaxRect(fromX, floorY, toX, floorY + 2.2f), secret: secret, optional: true,
                trigger: true, ability: ability,
                icon: secret ? MapIconType.Secret : MapIconType.None);
        }

        private static string AreaJson(string id, string zone, string name, Vector2 mapPosition,
            Vector2 mapSize, Rect bounds, bool secret = false, bool optional = false,
            bool trigger = false, string ability = "", MapIconType icon = MapIconType.None,
            string boss = "")
        {
            _pendingBounds[id] = bounds;
            return "{" + Str("_areaId", id) + "," + Str("_zoneId", zone) + ","
                + Str("_displayName", name) + ","
                + "\"_mapPosition\":" + Vec(mapPosition) + ",\"_mapSize\":" + Vec(mapSize) + ","
                + "\"_isSecret\":" + Bool(secret) + ",\"_isOptional\":" + Bool(optional) + ","
                + "\"_discoveryRule\":" + (int)(trigger ? AreaDiscoveryRule.OnTrigger
                    : AreaDiscoveryRule.OnEnter) + ","
                + Str("_requiredAbilityId", ability) + ",\"_iconType\":" + (int)icon + ","
                + Str("_bossId", boss) + "}";
        }

        private static string LinkJson(string id, string from, string to,
            MapDirection direction = MapDirection.East, ConnectionType type = ConnectionType.Normal,
            bool bidirectional = true, string ability = "", string boss = "",
            string shortcut = "", string openedFrom = "", bool future = false)
        {
            return "{" + Str("_connectionId", id) + "," + Str("_fromAreaId", from) + ","
                + Str("_toAreaId", to) + ",\"_direction\":" + (int)direction + ","
                + "\"_type\":" + (int)type + ",\"_isBidirectional\":" + Bool(bidirectional) + ","
                + Str("_requiredAbilityId", ability) + "," + Str("_requiredProgressionId", "")
                + "," + Str("_requiredBossId", boss) + ","
                + "\"_isShortcut\":" + Bool(type == ConnectionType.Shortcut) + ","
                + Str("_persistentUnlockId", shortcut) + "," + Str("_openedFromAreaId", openedFrom)
                + ",\"_isFuture\":" + Bool(future) + "}";
        }

        private static string AltarJson(string id, string name, string zone, string area)
        {
            return "{" + Str("_checkpointId", id) + "," + Str("_displayName", name) + ","
                + Str("_zoneId", zone) + "," + Str("_areaId", area) + ",\"_canFastTravel\":true}";
        }

        private static string AbilityJson(string id, string name, AbilityKind kind, string boss)
        {
            return "{" + Str("_abilityId", id) + "," + Str("_displayName", name) + ","
                + "\"_kind\":" + (int)kind + "," + Str("_guaranteedByBossId", boss) + "}";
        }

        private static string Str(string key, string value) =>
            "\"" + key + "\":\"" + (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

        private static string Bool(bool value) => value ? "true" : "false";

        private static string F(float value) => value.ToString(CultureInfo.InvariantCulture);

        private static string Vec(Vector2 value) => "{\"x\":" + F(value.x) + ",\"y\":" + F(value.y) + "}";

        // ---------- systems ----------

        /// <summary>
        /// The world's services, on the same object as every other system so there is exactly one
        /// of each; the developer panel goes on the developer-only object.
        /// </summary>
        private static void BuildWorldSystems(GameObject systems, GameObject devTools,
            GameObject player, PlayerRespawner respawner, GameProgress progress)
        {
            var character = player.GetComponent<PlayerCharacter>();
            var world = systems.AddComponent<WorldState>();
            SetReference(world, "_progress", progress);
            SetReference(world, "_player", character);
            SetReference(world, "_respawner", respawner);

            var travel = systems.AddComponent<FastTravelService>();
            SetReference(travel, "_progress", progress);
            SetReference(travel, "_respawner", respawner);
            SetReference(travel, "_player", character);

            var shrine = systems.AddComponent<ShrineMenu>();
            SetReference(shrine, "_respawner", respawner);
            SetReference(shrine, "_travel", travel);
            SetReference(shrine, "_player", character);

            var map = systems.AddComponent<WorldMapScreen>();
            SetReference(map, "_world", world);
            SetReference(map, "_player", character);
            SetReference(map, "_respawner", respawner);
            SetReference(map, "_travel", travel);

            var debug = devTools.AddComponent<WorldDebugTools>();
            SetReference(debug, "_world", world);
            SetReference(debug, "_map", map);
            SetReference(debug, "_shrine", shrine);
            SetReference(debug, "_travel", travel);
            SetReference(debug, "_respawner", respawner);
        }

        // ---------- helpers ----------

        private static void AreaTrigger(Transform parent, string areaId, Vector2 centre, Vector2 size)
        {
            var trigger = new GameObject("Area_" + areaId);
            trigger.transform.SetParent(parent);
            trigger.transform.position = centre;
            var box = trigger.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = size;
            SetString(trigger.AddComponent<AreaDiscoveryTrigger>(), "_areaId", areaId);
        }

        /// <summary>
        /// The marker beside a movement obstacle, and the faint glow that appears at the unreachable
        /// spot once the ability is owned — the only invitation to come back.
        /// </summary>
        private static void PlaceAbilityGate(Transform parent, string name, Vector2 position,
            string abilityId, string connectionId, Vector2 glowAt, GameProgress progress)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.position = position;
            var trigger = root.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 1f;

            var glow = new GameObject("ReachableGlow");
            glow.transform.SetParent(root.transform);
            glow.transform.position = glowAt;
            glow.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
            var renderer = glow.AddComponent<SpriteRenderer>();
            renderer.sprite = Vfx("vfx_light_pulse");
            renderer.color = new Color(1f, 0.82f, 0.45f, 0.55f);
            SetSorting(renderer, "VFXBack", 2);
            glow.SetActive(false);

            var gate = root.AddComponent<AbilityGate>();
            SetString(gate, "_requiredAbilityId", abilityId);
            SetString(gate, "_connectionId", connectionId);
            SetReference(gate, "_progress", progress);
            SetReference(gate, "_unlockedVisual", glow);
        }

        private static void ConfigureShortcut(Shortcut shortcut, GameObject[] reveal,
            GameObject[] hide, Transform animated, Vector3 closedOffset, Transform handle)
        {
            var serialized = new SerializedObject(shortcut);
            var revealProperty = serialized.FindProperty("_revealWhenOpen");
            revealProperty.arraySize = reveal.Length;
            for (var i = 0; i < reveal.Length; i++)
            {
                revealProperty.GetArrayElementAtIndex(i).objectReferenceValue = reveal[i];
            }

            var hideProperty = serialized.FindProperty("_hideWhenOpen");
            hideProperty.arraySize = hide.Length;
            for (var i = 0; i < hide.Length; i++)
            {
                hideProperty.GetArrayElementAtIndex(i).objectReferenceValue = hide[i];
            }

            serialized.FindProperty("_animatedPart").objectReferenceValue = animated;
            serialized.FindProperty("_closedOffset").vector3Value = closedOffset;
            serialized.FindProperty("_leverHandle").objectReferenceValue = handle;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>A door that opens for good once its condition holds.</summary>
        private static PersistentDoor PlaceDoor(Transform parent, string name, Vector2 position,
            RequirementKind kind, string id, GameObject barrier, GameProgress progress)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.position = position;
            var door = root.AddComponent<PersistentDoor>();
            SetReference(door, "_progress", progress);
            SetReference(door, "_barrier", barrier);
            var serialized = new SerializedObject(door);
            serialized.FindProperty("_requirement._kind").enumValueIndex = (int)kind;
            serialized.FindProperty("_requirement._id").stringValue = id;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return door;
        }
    }
}
