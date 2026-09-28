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
        private const string TombBurialHall = "room_tomb_burial_hall";
        private const string TombGuardPassage = "room_tomb_guard_passage";
        private const string TombCrypt = "room_tomb_collapsed_crypt";
        private const string TombApproach = "room_tomb_guardian_approach";
        private const string TombArena = "room_tomb_guardian_arena";
        private const string TombOssuary = "room_tomb_royal_ossuary";
        private const string TombBellShaft = "room_tomb_bell_shaft";
        private const string TombReliquary = "room_tomb_reliquary";
        private const string ForestTunnel = "room_forest_root_tunnel";
        private const string ForestEntrance = "room_forest_entrance";
        private const string ForestRoyalRoad = "room_forest_royal_road";
        private const string ForestBrokenArch = "room_forest_broken_arch";
        private const string ForestGrove = "room_forest_poison_grove";
        private const string ForestWatchtower = "room_forest_watchtower";
        private const string ForestWatchtowerTop = "room_forest_watchtower_top";
        private const string ForestHuntersPerch = "room_forest_hunters_perch";
        private const string ForestDeep = "room_forest_deep";
        private const string ForestWardenApproach = "room_forest_warden_approach";
        private const string ForestWardenArena = "room_forest_warden_arena";
        private const string ForestFarGate = "room_forest_far_gate";
        private const string ForestOverlook = "room_forest_warden_overlook";
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

        public const string TombBridgeShortcut = "shortcut_tomb_crypt_bridge";
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

            json.Append("\"_zones\":[")
                .Append(ZoneJson(TombZoneId, "Забута Гробниця", "Забута Гробниця",
                    new Vector2(-12f, 0.85f), true, new Color(0.72f, 0.66f, 0.55f),
                    WorldScenes.TombSceneName)).Append(',')
                .Append(ZoneJson(ForestZoneId, "Зів'ялий Ліс", "Зів'ялий Ліс",
                    new Vector2(80f, 0.85f), true, new Color(0.5f, 0.66f, 0.46f),
                    WorldScenes.ForestSceneName)).Append(',')
                .Append(ZoneJson(CapitalZoneId, "Попеляста Столиця", "Попеляста Столиця",
                    new Vector2(330f, 0.85f), true, new Color(0.7f, 0.56f, 0.5f),
                    WorldScenes.CapitalSceneName)).Append(',')
                // Unnamed on purpose: the map shows a sealed road east, not what lies there.
                .Append(ZoneJson(FutureZoneId, string.Empty, string.Empty, Vector2.zero, false,
                    new Color(0.4f, 0.4f, 0.42f)))
                .Append("],");

            json.Append("\"_areas\":[").Append(string.Join(",", new[]
            {
                MainRoom(TombEntrance, TombZoneId, "Вхід до Гробниці", -22f, -7f),
                MainRoom(TombBurialHall, TombZoneId, "Поховальна зала", -7f, 6f),
                MainRoom(TombGuardPassage, TombZoneId, "Прохід варти", 6f, 20.5f),
                MainRoom(TombCrypt, TombZoneId, "Обвалений склеп", 20.5f, 27f),
                MainRoom(TombApproach, TombZoneId, "Підступ до Стража", 27f, 38.5f),
                MainRoom(TombArena, TombZoneId, "Арена Стража", 38.5f, 62.5f,
                    boss: GuardianBossId),
                Pocket(TombOssuary, TombZoneId, "Королівська костниця", 12f, 15f, 7.2f,
                    secret: true, ability: AbilityIds.DoubleJump),
                Pocket(TombBellShaft, TombZoneId, "Дзвонова шахта", -6.8f, -2.8f, 10.4f,
                    secret: true, ability: AbilityIds.WallRun),
                Pocket(TombReliquary, TombZoneId, "Реліквіарій", 3f, 5f, 4f,
                    secret: false, ability: AbilityIds.AirDash),
                MainRoom(ForestTunnel, ForestZoneId, "Кореневий тунель", 62.5f, 81f),
                MainRoom(ForestEntrance, ForestZoneId, "Узлісся", 81f, 110f),
                MainRoom(ForestRoyalRoad, ForestZoneId, "Зруйнований королівський шлях", 110f, 134f),
                MainRoom(ForestBrokenArch, ForestZoneId, "Зламана арка", 134f, 147f),
                MainRoom(ForestGrove, ForestZoneId, "Отруйний гай", 147f, 181f),
                MainRoom(ForestWatchtower, ForestZoneId, "Руїни сторожової вежі", 181f, 212f,
                    top: 11.9f),
                AreaJson(ForestWatchtowerTop, ForestZoneId, "Вершина вежі",
                    new Vector2(197f * MapScale, 1.9f), new Vector2(1.6f, 0.9f),
                    Rect.MinMaxRect(186f, 11.9f, 206f, 18f), optional: true,
                    ability: AbilityIds.WallRun),
                Pocket(ForestHuntersPerch, ForestZoneId, "Мисливський поміст", 201f, 205f, 14.9f,
                    secret: true, ability: AbilityIds.DoubleJump, mapRow: 3.1f),
                MainRoom(ForestDeep, ForestZoneId, "Глибокий ліс", 212f, 248f),
                MainRoom(ForestWardenApproach, ForestZoneId, "Підступ до Вартового", 248f, 272.5f),
                MainRoom(ForestWardenArena, ForestZoneId, "Галявина Вартового", 272.5f, 302f,
                    boss: WardenBossId),
                MainRoom(ForestFarGate, ForestZoneId, "Край лісу", 302f, 318f),
                Pocket(ForestOverlook, ForestZoneId, "Оглядовий виступ", 304.5f, 309.5f, 2.8f,
                    secret: true, ability: AbilityIds.DoubleJump),
                MainRoom(CapitalAshRoad, CapitalZoneId, "Попелястий тракт", 318f, 345f),
                MainRoom(CapitalCityGate, CapitalZoneId, "Міська брама", 345f, 372f),
                MainRoom(CapitalMarket, CapitalZoneId, "Ринкова площа", 372f, 404f),
                MainRoom(CapitalBellTower, CapitalZoneId, "Дзвінниця", 404f, 424f),
                Pocket(CapitalBellNest, CapitalZoneId, "Гніздо на дзвінниці", 421f, 424f, 8.2f,
                    secret: true, ability: AbilityIds.AirDash),
                MainRoom(CapitalTemple, CapitalZoneId, "Храмовий квартал", 424f, 458f),
                MainRoom(CapitalGardens, CapitalZoneId, "Спалені сади", 458f, 492f),
                MainRoom(CapitalPalaceApproach, CapitalZoneId, "Підступ до палацу", 492f, 512f),
                Pocket(CapitalGallery, CapitalZoneId, "Королівська галерея", 494f, 510f, 9.4f,
                    secret: true, ability: AbilityIds.Grapple),
                MainRoom(CapitalThroneHall, CapitalZoneId, "Тронна зала", 512f, 545f,
                    boss: CapitalKingBossId),
                AreaJson(BeyondCapital, FutureZoneId, string.Empty,
                    new Vector2(551f * MapScale, 0f), new Vector2(0.8f, 1.2f),
                    Rect.MinMaxRect(545f, -8f, 557f, 16f), optional: true)
            })).Append("],");

            json.Append("\"_connections\":[").Append(string.Join(",", new[]
            {
                LinkJson("conn_tomb_entrance_hall", TombEntrance, TombBurialHall),
                LinkJson("conn_tomb_hall_passage", TombBurialHall, TombGuardPassage),
                // The pit: a jump every player can make. The bridge below is the easy way.
                LinkJson("conn_tomb_passage_crypt", TombGuardPassage, TombCrypt),
                LinkJson("conn_tomb_crypt_bridge", TombGuardPassage, TombCrypt,
                    type: ConnectionType.Shortcut, shortcut: TombBridgeShortcut,
                    openedFrom: TombCrypt),
                LinkJson("conn_tomb_crypt_approach", TombCrypt, TombApproach),
                LinkJson("conn_tomb_approach_arena", TombApproach, TombArena,
                    type: ConnectionType.Door),
                LinkJson("conn_tomb_to_forest", TombArena, ForestTunnel,
                    type: ConnectionType.ZoneTransition, boss: GuardianBossId),
                LinkJson("conn_tomb_passage_ossuary", TombGuardPassage, TombOssuary,
                    MapDirection.North, ConnectionType.AbilityGate, ability: AbilityIds.DoubleJump),
                LinkJson("conn_tomb_hall_bellshaft", TombBurialHall, TombBellShaft,
                    MapDirection.North, ConnectionType.AbilityGate, ability: AbilityIds.WallRun),
                LinkJson("conn_tomb_passage_reliquary", TombGuardPassage, TombReliquary,
                    MapDirection.West, ConnectionType.AbilityGate, ability: AbilityIds.AirDash),
                LinkJson("conn_forest_tunnel_entrance", ForestTunnel, ForestEntrance),
                LinkJson("conn_forest_entrance_road", ForestEntrance, ForestRoyalRoad),
                // Over the archer's ledge and down past the barred arch: one way only, east.
                LinkJson("conn_forest_road_ledge", ForestRoyalRoad, ForestBrokenArch,
                    type: ConnectionType.Drop, bidirectional: false),
                LinkJson("conn_forest_road_gate", ForestRoyalRoad, ForestBrokenArch,
                    type: ConnectionType.Shortcut, shortcut: ForestRoadGateShortcut,
                    openedFrom: ForestBrokenArch),
                LinkJson("conn_forest_arch_grove", ForestBrokenArch, ForestGrove),
                LinkJson("conn_forest_grove_watchtower", ForestGrove, ForestWatchtower),
                LinkJson("conn_forest_watchtower_deep", ForestWatchtower, ForestDeep),
                LinkJson("conn_forest_watchtower_climb", ForestWatchtower, ForestWatchtowerTop,
                    MapDirection.North, ConnectionType.AbilityGate, ability: AbilityIds.WallRun),
                // The rotten lip at the top: a way down that is always open.
                LinkJson("conn_forest_watchtower_drop", ForestWatchtowerTop, ForestWatchtower,
                    MapDirection.South, ConnectionType.Drop, bidirectional: false),
                LinkJson("conn_forest_top_perch", ForestWatchtowerTop, ForestHuntersPerch,
                    MapDirection.North, ConnectionType.AbilityGate, ability: AbilityIds.DoubleJump),
                LinkJson("conn_forest_deep_approach", ForestDeep, ForestWardenApproach),
                LinkJson("conn_forest_approach_arena", ForestWardenApproach, ForestWardenArena,
                    type: ConnectionType.Door),
                LinkJson("conn_forest_arena_far", ForestWardenArena, ForestFarGate,
                    type: ConnectionType.BossGate, boss: WardenBossId),
                LinkJson("conn_forest_far_overlook", ForestFarGate, ForestOverlook,
                    MapDirection.North, ConnectionType.AbilityGate, ability: AbilityIds.DoubleJump),
                // The road the Warden kept: sealed until he falls, then the way to the capital.
                LinkJson("conn_forest_to_capital", ForestFarGate, CapitalAshRoad,
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
                // East of the throne the road goes on, sealed: what lies there is the ending's.
                LinkJson("conn_capital_throne_beyond", CapitalThroneHall, BeyondCapital,
                    type: ConnectionType.Gate, boss: CapitalKingBossId, future: true)
            })).Append("],");

            json.Append("\"_checkpoints\":[").Append(string.Join(",", new[]
            {
                AltarJson("tomb-altar-01", "Вівтар входу", TombZoneId, TombEntrance),
                AltarJson("tomb-altar-02", "Вівтар переходу", TombZoneId, TombCrypt),
                AltarJson("forest-entrance", "Вівтар узлісся", ForestZoneId, ForestEntrance),
                AltarJson("royal-road-shrine", "Придорожня святиня", ForestZoneId, ForestRoyalRoad),
                AltarJson("deep-forest-shrine", "Святиня глибокого лісу", ForestZoneId, ForestDeep),
                AltarJson("warden-shrine", "Святиня Вартового", ForestZoneId,
                    ForestWardenApproach),
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

        private static string ZoneJson(string id, string name, string title, Vector2 spawn,
            bool implemented, Color tint, string contentScene = "")
        {
            return "{" + Str("_zoneId", id) + "," + Str("_displayName", name) + ","
                + Str("_titleCard", title) + "," + Str("_sceneName", WorldScenes.WorldSceneName) + ","
                + Str("_contentScene", contentScene) + ","
                + "\"_defaultSpawn\":" + Vec(spawn) + ","
                + "\"_isImplemented\":" + Bool(implemented) + ","
                + "\"_mapTint\":{\"r\":" + F(tint.r) + ",\"g\":" + F(tint.g) + ",\"b\":" + F(tint.b)
                + ",\"a\":1}}";
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

        // ---------- the Tomb, revisited ----------

        /// <summary>
        /// Three places in the Tomb that a first visit can see and cannot reach, and the bridge
        /// that makes the pit safe to cross on the way back.
        /// </summary>
        private static void BuildTombWorld(Transform level, GameProgress progress,
            GameObject pickupPrefab)
        {
            var root = new GameObject("TombWorld").transform;
            root.SetParent(level);

            // The Royal Ossuary: above the guards' ledge. From that ledge a jump reaches 6.4, a
            // second jump 7.8; the shelf is at 7.2. Its faces are polished marble, so a wall run
            // cannot climb what the second jump is for.
            var ossuary = Block(root, "OssuaryShelf", new Vector2(13.5f, 6.95f),
                new Vector2(3f, 0.5f), TombSurface.Platform);
            Ensure<NonClimbableSurface>(ossuary);
            Prop(root, "env_tomb_sarcophagus_01", new Vector2(14.2f, 7.2f), "EnvironmentBack", 4,
                0.6f);
            PlacePickup(pickupPrefab, new Vector2(13.1f, 7.7f), progress, "ossuary_signet", false,
                1, "tomb-ossuary-signet", false);
            // Inset from the shelf's edges: a failed jump pressed against its side is not "inside".
            AreaTrigger(root, TombOssuary, new Vector2(13.4f, 8.2f), new Vector2(2.6f, 1.6f));
            PlaceAbilityGate(root, "OssuaryHint", new Vector2(16.4f, 5.3f), AbilityIds.DoubleJump,
                "conn_tomb_passage_ossuary", new Vector2(13.5f, 7.6f), progress);

            // The Bell Shaft: two walls hanging over the burial hall, two units apart. Only a wall
            // jump climbs it; the bell loft sits on the western wall, ten units up and far from
            // anything a second jump could start from.
            Block(root, "BellShaftWest", new Vector2(-3.2f, 6.3f), new Vector2(0.8f, 7.4f),
                TombSurface.Wall);
            Block(root, "BellShaftEast", new Vector2(-0.4f, 6.3f), new Vector2(0.8f, 7.4f),
                TombSurface.Wall);
            Block(root, "BellLoft", new Vector2(-4.8f, 10.2f), new Vector2(4f, 0.4f),
                TombSurface.BrokenPlatform);
            Prop(root, "env_tomb_chains_01", new Vector2(-1.8f, 10.5f), "EnvironmentBack", 3, 0.8f);
            PlacePickup(pickupPrefab, new Vector2(-5.4f, 10.9f), progress, "bellringer_hood",
                false, 1, "tomb-bell-hood", false);
            PlacePickup(pickupPrefab, new Vector2(-4.2f, 10.8f), progress, string.Empty, true,
                180, "tomb-bell-purse", true);
            AreaTrigger(root, TombBellShaft, new Vector2(-4.8f, 11.3f), new Vector2(3.6f, 1.8f));
            PlaceAbilityGate(root, "BellShaftHint", new Vector2(-1.8f, 0.8f), AbilityIds.WallRun,
                "conn_tomb_hall_bellshaft", new Vector2(-4.8f, 10.9f), progress);

            // The Reliquary: a shelf level with nothing, six units west of the second step. A
            // running jump from the step falls a metre short; a jump and a dash in the air does
            // not. From the floor below it is 0.7 higher than even a second jump.
            var reliquary = Block(root, "ReliquaryShelf", new Vector2(4f, 3.75f),
                new Vector2(2f, 0.5f), TombSurface.Platform);
            Ensure<NonClimbableSurface>(reliquary);
            Prop(root, "env_tomb_urn_01", new Vector2(4.5f, 4f), "EnvironmentBack", 4, 0.6f);
            PlacePickup(pickupPrefab, new Vector2(3.8f, 4.5f), progress, "reliquary_charm", false,
                1, "tomb-reliquary-charm", false);
            AreaTrigger(root, TombReliquary, new Vector2(4f, 4.9f), new Vector2(1.6f, 1.2f));
            PlaceAbilityGate(root, "ReliquaryHint", new Vector2(11.2f, 4.1f), AbilityIds.AirDash,
                "conn_tomb_passage_reliquary", new Vector2(4f, 4.5f), progress);

            BuildCryptBridge(root);
        }

        /// <summary>
        /// The Tomb's shortcut: the old bridge over the collapsed crypt, hauled up from the rubble
        /// by a winch on the altar side. The first crossing is a jump over a real pit; every one
        /// after it, in either direction, is a walk.
        /// </summary>
        private static void BuildCryptBridge(Transform parent)
        {
            var bridge = Block(parent, "CryptBridge", new Vector2(21.75f, -0.25f),
                new Vector2(2.5f, 0.5f), TombSurface.Platform);
            var art = Object.Instantiate(bridge.transform.Find("Visual").gameObject, parent);
            art.name = "CryptBridgeArt";
            art.transform.position = bridge.transform.position;
            // The swinging copy is the only one drawn; the collider's own art would double it.
            bridge.transform.Find("Visual").gameObject.SetActive(false);
            // The collider arrives only once the bridge is visibly in place.
            bridge.SetActive(false);

            var lever = new GameObject("CryptBridgeWinch");
            lever.transform.SetParent(parent);
            lever.transform.position = new Vector2(23.5f, 0.9f);
            var trigger = lever.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 0.8f;
            var handle = Prop(lever.transform, "env_tomb_chains_01", new Vector2(23.5f, 0.2f),
                "GameplayEnvironment", 3, 0.5f);
            var shortcut = lever.AddComponent<Shortcut>();
            SetString(shortcut, "_shortcutId", TombBridgeShortcut);
            SetString(shortcut, "_displayName", "лебідка мосту");
            ConfigureShortcut(shortcut, new[] { bridge }, System.Array.Empty<GameObject>(),
                art.transform, new Vector3(0f, -2.6f, 0f), handle != null ? handle.transform : null);
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
