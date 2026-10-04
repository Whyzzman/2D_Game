using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The kingdom of fifteen regions as the world map draws it (item 2 of the 15-zone list,
    /// gamedesign/WORLD_DESIGN.md, mockup design/ui/world-map/world-map.html): where each island
    /// stands, how it looks, its colours and card, and the roads between. Regions not built yet are
    /// entries too — unimplemented, so nothing can be travelled to — because the map shows them as
    /// silhouettes and fog before they exist.
    /// </summary>
    public static partial class ContentBuilder
    {
        // Region ids of the plan. The three built ones keep their old ids: saves record them.
        public const string VillageZoneId = "zone_fallen_village";
        public const string MarshZoneId = "zone_plague_marsh";
        public const string CathedralZoneId = "zone_sunken_cathedral";
        public const string AqueductsZoneId = "zone_royal_aqueducts";
        public const string DepthsZoneId = "zone_ashen_depths";
        public const string CavernsZoneId = "zone_crystal_caverns";
        public const string CitadelZoneId = "zone_iron_citadel";
        public const string BastionZoneId = "zone_frozen_bastion";
        public const string ObservatoryZoneId = "zone_arcane_observatory";
        public const string PalaceZoneId = "zone_royal_palace";
        public const string ShadowZoneId = "zone_shadow_realm";
        public const string ThroneZoneId = "zone_throne_of_oblivion";

        private readonly struct Region
        {
            public readonly string Id, Name, Art, Scene, Description;
            public readonly Vector2 Position, Spawn;
            public readonly Color Rooms, Glow;

            public Region(string id, string name, string art, Vector2 position, string rooms, string glow,
                string description, string scene = "", Vector2 spawn = default)
            {
                Id = id; Name = name; Art = art; Position = position; Description = description;
                Scene = scene; Spawn = spawn;
                ColorUtility.TryParseHtmlString(rooms, out Rooms);
                ColorUtility.TryParseHtmlString(glow, out Glow);
            }

            public bool IsBuilt => !string.IsNullOrEmpty(Scene);
        }

        /// <summary>
        /// Positions are the mockup's: a canvas of 1470 × 900, y down. «Rooms» is the colour the
        /// zone's rooms and corridors take on its map, «glow» its light on the world map.
        /// </summary>
        private static readonly Region[] Regions =
        {
            new Region(TombZoneId, "Забута Гробниця", "tomb", new Vector2(200f, 690f), "#2b3a4a", "#f0d9a0",
                "Тут лежать ті, хто не присягнув. Склепи й шахти під землею, де прокинулася тінь без імені.",
                WorldScenes.TombSceneName, new Vector2(-12f, 0.85f)),
            new Region(ForestZoneId, "Зів'ялий Ліс", "forest", new Vector2(545f, 715f), "#2a5a4e", "#ffb35a",
                "Цвинтар прадавніх дерев, де життя чіпляється за гниль. Коріння оплітає забуті руїни, а повітря густе від пам'яті про те, що було.",
                WorldScenes.ForestSceneName, new Vector2(80f, 0.85f)),
            new Region(VillageZoneId, "Занепале Село", "village", new Vector2(830f, 745f), "#4a3424", "#ff9c4a",
                "Хати на палях, колодязь на площі й дзвіниця, що б'є сама по собі. Люди пішли, а вогні досі горять.",
                WorldScenes.VillageSceneName, new Vector2(320f, 20.85f)),
            new Region(MarshZoneId, "Чумні Болота", "marsh", new Vector2(1075f, 790f), "#2f4a1e", "#b6ff5a",
                "Отруйна вода, гнилі містки й хижі на палях. Тут лікували чуму, доки вона не стала господарем."),
            new Region(DepthsZoneId, "Попелясті Глибини", "depths", new Vector2(1300f, 735f), "#4a1e12", "#ff6a2a",
                "Кузні під землею, де лава тече замість води. Перший із Присяжних береже тут печатку вогню."),
            new Region(CavernsZoneId, "Кришталеві Печери", "crystal", new Vector2(190f, 470f), "#1e2c5a", "#7fb6ff",
                "Шахти, що проросли кристалом. Світло тут холодне й ріже, як лезо."),
            new Region(CitadelZoneId, "Залізна Цитадель", "citadel", new Vector2(500f, 455f), "#4a1a18", "#ff5a3a",
                "Тюрма королівства: камери, ланцюги й арена, де в'язні билися за волю, якої не було."),
            new Region(CapitalZoneId, "Зруйнована Столиця", "capital", new Vector2(830f, 450f), "#3a362e", "#ffd27a",
                "Попелясті вулиці під собором і вежею. Місто горіло роками, бо Король заборонив йому згасати.",
                WorldScenes.CapitalSceneName, new Vector2(330f, 0.85f)),
            new Region(CathedralZoneId, "Затоплений Собор", "cathedral", new Vector2(1060f, 555f), "#1e3a44", "#9ee0ff",
                "Неф, де вода піднялася до вівтаря. Свічки досі горять над нею, і хтось досі співає."),
            new Region(AqueductsZoneId, "Королівські Акведуки", "aqueducts", new Vector2(1300f, 505f), "#14404a", "#6ff0ff",
                "Канали, шлюзи й водоспади під містом. Владар води тримає тут другу печатку."),
            new Region(BastionZoneId, "Крижаний Бастіон", "ice", new Vector2(250f, 255f), "#2a3a52", "#e8f6ff",
                "Фортеця на перевалі, вкрита кригою. Третя Присяжна чекає у тронній залі, де ніхто не мерзне, крім живих."),
            new Region(ObservatoryZoneId, "Таємна Обсерваторія", "observatory", new Vector2(545f, 185f), "#2a1e5a", "#c9a6ff",
                "Куполи над хмарами й острови, що висять у повітрі. Звідси видно навіть те, чого немає."),
            new Region(PalaceZoneId, "Королівський Палац", "palace", new Vector2(830f, 225f), "#4a4230", "#ffe6a0",
                "Мармур, золото й місячний сад. Двір досі чекає на короля, що не повернеться."),
            new Region(ShadowZoneId, "Царство Тіні", "shadow", new Vector2(1155f, 300f), "#2c1a4a", "#b47aff",
                "Руїни, що висять у порожнечі. Тут живе тінь, яка пам'ятає героя краще, ніж він сам."),
            new Region(ThroneZoneId, "Трон Забуття", "throne", new Vector2(1290f, 200f), "#4a121a", "#ff4a6a",
                "Чорні шпилі під затемненням. Тут Король тримає свою клятву, і тут вона скінчиться.")
        };

        /// <summary>[from, to, gated]: the map of WORLD_DESIGN.md, «Порядок и связи».</summary>
        private static readonly (string from, string to, bool gated)[] Roads =
        {
            (TombZoneId, ForestZoneId, false), (ForestZoneId, VillageZoneId, false), (ForestZoneId, CavernsZoneId, true),
            (VillageZoneId, MarshZoneId, false), (MarshZoneId, DepthsZoneId, true), (VillageZoneId, CapitalZoneId, true),
            (CapitalZoneId, CitadelZoneId, false), (CavernsZoneId, CitadelZoneId, true), (CitadelZoneId, BastionZoneId, true),
            (CapitalZoneId, CathedralZoneId, false), (CathedralZoneId, AqueductsZoneId, true), (AqueductsZoneId, DepthsZoneId, true),
            (CapitalZoneId, PalaceZoneId, true), (BastionZoneId, ObservatoryZoneId, true), (ObservatoryZoneId, PalaceZoneId, true),
            (ObservatoryZoneId, ShadowZoneId, true), (AqueductsZoneId, ShadowZoneId, true), (PalaceZoneId, ThroneZoneId, true),
            (ShadowZoneId, ThroneZoneId, true)
        };

        /// <summary>The zones array of the graph: the fifteen regions and the unnamed road east.</summary>
        private static string WorldZonesJson()
        {
            var zones = Regions.Select(r => ZoneJson(r.Id, r.Name, r.Name, r.Spawn, r.IsBuilt, r.Rooms, r.Scene,
                r.Position, r.Art, r.Glow, r.Description)).ToList();
            // Unnamed on purpose: the map shows a sealed road east of the throne hall, not what lies
            // there. It has no island; the rebuilt capital (item 6) takes it away.
            zones.Add(ZoneJson(FutureZoneId, string.Empty, string.Empty, Vector2.zero, false,
                new Color(0.4f, 0.4f, 0.42f)));
            return "\"_zones\":[" + string.Join(",", zones) + "],";
        }

        private static string WorldRoadsJson()
        {
            return "\"_roads\":[" + string.Join(",", Roads.Select(r => "{" + Str("_fromZoneId", r.from) + ","
                + Str("_toZoneId", r.to) + ",\"_isGated\":" + Bool(r.gated) + "}")) + "],";
        }

        private static string ZoneJson(string id, string name, string title, Vector2 spawn,
            bool implemented, Color tint, string contentScene = "", Vector2 worldPosition = default,
            string art = "", Color glow = default, string description = "")
        {
            return "{" + Str("_zoneId", id) + "," + Str("_displayName", name) + ","
                + Str("_titleCard", title) + "," + Str("_sceneName", WorldScenes.WorldSceneName) + ","
                + Str("_contentScene", contentScene) + ","
                + "\"_defaultSpawn\":" + Vec(spawn) + ","
                + "\"_isImplemented\":" + Bool(implemented) + ","
                + "\"_mapTint\":" + Col(tint) + ","
                + "\"_worldPosition\":" + Vec(worldPosition) + "," + Str("_art", art) + ","
                + "\"_glow\":" + Col(glow == default ? Color.white : glow) + "," + Str("_description", description) + "}";
        }

        private static string Col(Color c) =>
            "{\"r\":" + F(c.r) + ",\"g\":" + F(c.g) + ",\"b\":" + F(c.b) + ",\"a\":1}";

        private const string MapArtFolder = "Assets/Game/Art/Map";
        private const string MapArtPath = DataFolder + "/MapArt.asset";
        /// <summary>Pixels per map unit of a room's picture (tools/map-art/render.sh).</summary>
        public const float RoomArtPixelsPerUnit = 150f;
        public const string MapRoomsJson = "design/ui/world-map/map-rooms.json";

        /// <summary>
        /// Gathers the rendered pictures into <see cref="MapArt"/> and points the graph at it. The
        /// pictures are not generated here: they come from the mockup's code through
        /// tools/map-art/render.sh, so the game shows what was approved. A missing one is a warning,
        /// and the map draws that place without it.
        /// </summary>
        private static void BuildMapArt(WorldGraph graph)
        {
            var art = CreateAsset<MapArt>(MapArtPath);
            var serialized = new SerializedObject(art);
            FillMapEntries(serialized.FindProperty("_islands"), Regions.Select(r => (r.Art, $"{MapArtFolder}/Islands/{r.Art}.png")));
            FillMapEntries(serialized.FindProperty("_cards"), Regions.Select(r => (r.Art, $"{MapArtFolder}/Cards/{r.Art}.png")));
            FillMapEntries(serialized.FindProperty("_rooms"), graph.Areas
                .Select(a => (a.AreaId, $"{MapArtFolder}/Rooms/{a.AreaId}.png")).Where(e => File.Exists(e.Item2)));
            serialized.FindProperty("_fog").objectReferenceValue = MapTexture($"{MapArtFolder}/fog.png");
            serialized.FindProperty("_glow").objectReferenceValue = MapTexture($"{MapArtFolder}/glow.png");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(art);
            var graphObject = new SerializedObject(graph);
            graphObject.FindProperty("_mapArt").objectReferenceValue = art;
            graphObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void FillMapEntries(SerializedProperty list, IEnumerable<(string id, string path)> entries)
        {
            var all = entries.ToList();
            list.arraySize = all.Count;
            for (var i = 0; i < all.Count; i++)
            {
                var entry = list.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("Id").stringValue = all[i].id;
                entry.FindPropertyRelative("Texture").objectReferenceValue = MapTexture(all[i].path);
            }
        }

        /// <summary>The picture, imported for the interface: full size, no mipmaps, clean alpha.</summary>
        private static Texture2D MapTexture(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning($"Map art missing: {path} (run tools/map-art/render.sh).");
                return null;
            }

            if (AssetImporter.GetAtPath(path) is TextureImporter importer
                && (importer.mipmapEnabled || !importer.alphaIsTransparency || importer.maxTextureSize < 2048
                    || importer.textureCompression != TextureImporterCompression.CompressedHQ))
            {
                importer.textureType = TextureImporterType.Default;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>
        /// The rooms the map paints, for tools/map-art/render.sh: every room of a built region but the
        /// secret ones (those are drawn as dashed hiding places), with its picture's size in pixels.
        /// Run: -executeMethod ForgottenKingdom.EditorTools.ContentBuilder.ExportMapRooms
        /// </summary>
        [MenuItem("Forgotten Kingdom/Levels/Export Map Rooms")]
        public static void ExportMapRooms()
        {
            var graph = AssetDatabase.LoadAssetAtPath<WorldGraph>(WorldGraphPath);
            var lines = new List<string>();
            foreach (var area in graph.Areas)
            {
                var zone = graph.Zone(area.ZoneId);
                if (area.IsSecret || zone == null || !zone.IsImplemented)
                {
                    continue;
                }

                var size = area.MapSize * RoomArtPixelsPerUnit;
                lines.Add("    {" + Str("id", area.AreaId) + "," + Str("art", zone.Art) + ",\"w\":" + F(Mathf.Round(size.x))
                    + ",\"h\":" + F(Mathf.Round(size.y)) + ",\"boss\":" + Bool(area.IsBossArena) + "}");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(MapRoomsJson));
            File.WriteAllText(MapRoomsJson, "{\n  \"rooms\": [\n" + string.Join(",\n", lines) + "\n  ]\n}\n");
            Debug.Log($"Map rooms exported: {lines.Count} → {MapRoomsJson}");
        }

        /// <summary>
        /// Only the map's pictures, after tools/map-art/render.sh drew new ones; the rest of the build
        /// stays as it was.
        /// Run: -executeMethod ForgottenKingdom.EditorTools.ContentBuilder.RefreshMapArt
        /// </summary>
        [MenuItem("Forgotten Kingdom/Levels/Refresh Map Art")]
        public static void RefreshMapArt()
        {
            AssetDatabase.Refresh();
            var graph = AssetDatabase.LoadAssetAtPath<WorldGraph>(WorldGraphPath);
            BuildMapArt(graph);
            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();
        }

        /// <summary>The regions in order, for the map art export and tests.</summary>
        public static IEnumerable<(string id, string art)> WorldRegions() => Regions.Select(r => (r.Id, r.Art));
    }
}
