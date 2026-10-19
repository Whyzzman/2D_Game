using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The first people of the world (item 70, the user's choices of 2026-09-28): Hnat the
    /// merchant, Myroslava the pilgrim and the ghost of Severyn — what they say, what Hnat sells
    /// and for how much, the lockpick and the two relics, and their prefabs. Where they stand is
    /// the scene's business (<see cref="SceneNpcs"/>).
    /// </summary>
    public static partial class ContentBuilder
    {
        public const string NpcDataFolder = DataFolder + "/Npc";
        public const string MerchantStockPath = NpcDataFolder + "/MerchantStock.asset";
        public const string GnatPath = NpcDataFolder + "/Npc_Gnat.asset";
        public const string OstapPath = NpcDataFolder + "/Npc_Ostap.asset";
        public const string OstapStockPath = NpcDataFolder + "/OstapStock.asset";
        public const string OstapPrefabPath = PrefabFolder + "/Npc_Ostap.prefab";
        public const string MyroslavaPath = NpcDataFolder + "/Npc_Myroslava.asset";
        public const string SeverynPath = NpcDataFolder + "/Npc_Severyn.asset";
        public const string GnatPrefabPath = PrefabFolder + "/Npc_Gnat.prefab";
        public const string MyroslavaPrefabPath = PrefabFolder + "/Npc_Myroslava.prefab";
        public const string SeverynPrefabPath = PrefabFolder + "/Npc_Severyn.prefab";
        public const string LockedChestPrefabPath = PrefabFolder + "/LockedChest.prefab";

        // ---------- items ----------

        /// <summary>The lockpick and the two relics the merchant sells.</summary>
        private static IEnumerable<ItemDefinition> BuildNpcItems()
        {
            var candle = BuildAccessory("pilgrim_candle", "Мана більша, заклинання тримаються краще.",
                new[]
                {
                    new StatModifier(StatType.MaxMana, ModifierOperation.Add, 20f),
                    new StatModifier(StatType.Intelligence, ModifierOperation.Add, 2f)
                });
            var knot = BuildAccessory("whisper_knot", "Здоров'я більше, отрута чіпляється гірше.",
                new[]
                {
                    new StatModifier(StatType.MaxHealth, ModifierOperation.Add, 15f),
                    new StatModifier(StatType.PoisonResistance, ModifierOperation.Add, 0.3f)
                });
            return new[]
            {
                BuildItem(MerchantStock.LockpickItemId, "Відмичка",
                    "Тонкий гачок із загартованої сталі. Відкриває одну замкнену скриню — і ламається.",
                    ItemCategory.KeyItem, ItemRarity.Common, 9, false, true, 0,
                    new Color(0.62f, 0.64f, 0.68f)),
                BuildItem("pilgrim_candle", "Свічка Паломника",
                    "Недогарок, що горів біля кожного вівтаря на шляху паломників.",
                    ItemCategory.Accessory, ItemRarity.Rare, 1, true, true, 350,
                    new Color(0.95f, 0.78f, 0.45f), candle),
                BuildItem("whisper_knot", "Вузол Шепоту",
                    "Сплетене коріння, що шепоче, коли поруч отрута.",
                    ItemCategory.Accessory, ItemRarity.Rare, 1, true, true, 600,
                    new Color(0.5f, 0.72f, 0.45f), knot)
            };
        }

        // ---------- people ----------

        private static void BuildNpcContent()
        {
            EnsureFolder(NpcDataFolder);
            var art = PlaceholderNpcArt.Generate();
            var stock = BuildMerchantStock();
            var gnat = BuildNpc(GnatPath, "gnat", "Скупник Гнат", "торговець", GnatTopics(), stock);
            var myroslava = BuildNpc(MyroslavaPath, "myroslava", "Паломниця Мирослава", "шукачка вогню",
                MyroslavaTopics(), null);
            var severyn = BuildNpc(SeverynPath, "severyn", "Вартовий Северин", "привид", SeverynTopics(), null);
            var ostap = BuildNpc(OstapPath, "ostap", "Коваль Остап", "коваль", OstapTopics(), BuildOstapStock());
            AssetDatabase.SaveAssets();

            BuildNpcPrefab(GnatPrefabPath, gnat, art.gnat, new Vector2(2.8f, 2.2f),
                new Vector3(-1f, 1.57f, 0f), new Color(1f, 0.72f, 0.38f));
            BuildNpcPrefab(MyroslavaPrefabPath, myroslava, art.myroslava, new Vector2(1.6f, 2.2f),
                new Vector3(0.36f, 1.53f, 0f), new Color(1f, 0.72f, 0.38f));
            BuildNpcPrefab(SeverynPrefabPath, severyn, art.severyn, new Vector2(1.6f, 2.4f),
                new Vector3(0f, 1.2f, 0f), new Color(0.6f, 0.75f, 1f));
            BuildNpcPrefab(OstapPrefabPath, ostap, art.ostap, new Vector2(2.4f, 2.4f),
                new Vector3(0.9f, 0.9f, 0f), new Color(1f, 0.55f, 0.25f));
            BuildLockedChestPrefab(art.chestClosed, art.chestOpen);
        }

        private static NpcDefinition BuildNpc(string path, string id, string name, string epithet,
            DialogueTopic[] topics, MerchantStock stock)
        {
            var npc = CreateAsset<NpcDefinition>(path);
            npc.Configure(id, name, epithet, topics, stock);
            EditorUtility.SetDirty(npc);
            return npc;
        }

        private static DialogueTopic Topic(string id, string note, bool once, string requires, string until,
            params string[] pages) => new DialogueTopic
        {
            id = id, journalNote = note, once = once, requiresBoss = requires ?? string.Empty,
            untilBoss = until ?? string.Empty, pages = pages
        };

        private static DialogueTopic[] GnatTopics() => new[]
        {
            Topic("meet", "Скупник біля першого вівтаря. Торгує картами, уламками фляг і чутками.", true,
                null, GuardianBossId,
                "Живий? Рідкість у цих стінах.",
                "Я Гнат. Скуповую, що лишилося від королівства, і продаю тим, хто ще дихає.",
                "Золото маєш — товар знайдеться. Чутки теж, та вони дорожчі."),
            Topic("moved", "Переїхав за вами в Ліс. Привіз те, що не продав у Гробниці.", true,
                GuardianBossId, null,
                "Страж упав? Отже, дорога в Ліс відчинена.",
                "Я перебрався сюди. Біля мертвих дерев торгувати краще, ніж біля мертвих королів.",
                "На возі є й нове — для Лісу."),
            Topic("village", "Переїхав у Село до колодязя. Радить не стояти під дзвіницею.", true,
                WardenBossId, BellringerBossId,
                "Село. Тут колись пекли хліб для двору.",
                "Тепер дзвін б'є сам. Люди виходять на нього — і вже не повертаються людьми.",
                "Не стій під дзвіницею, коли він гуде."),
            Topic("night-bell", "Розповів про нічний дзвін на площі: ним скликали варту.", true,
                BellringerBossId, null,
                "Дзвін замовк. Уперше за триста років я чую колодязь.",
                "На площі висить малий дзвін — нічний. Ним скликали варту. Вдариш — прийдуть усі, хто ще блукає. "
                    + "Сміливим — нагорода."),
            Topic("again", string.Empty, false, null, null, "Ну, подивимось, що в тебе в гаманці.")
        };

        /// <summary>
        /// The smith of the capital's workshop (item 6c; WORLD_DESIGN: «коваль Остап — улучшение оружия за
        /// железо и золото»). Words decided by Claude under the user's standing approval.
        /// </summary>
        private static DialogueTopic[] OstapTopics() => new[]
        {
            Topic("meet", "Коваль у майстерні Столиці. Гартує зброю за залізо й золото.", true, null, null,
                "Живий? Тоді давай сюди руки, а не очі.",
                "Я Остап. Кував для варти, поки місто горіло. Горн досі гарячий — Король не дав йому згаснути.",
                "Принось залізо, що валяється по руїнах, і золото. Загартую — рубатимеш глибше."),
            Topic("commander", "Знав Командора. Каже, той тримає браму й досі.", true, null, CommanderBossId,
                "Командор? Я кував йому алебарду. Добра робота. Шкода, що тепер вона проти тебе.",
                "Коли він б'є в підлогу — стрибай. Камінь біжить в обидва боки."),
            Topic("after-commander", "Командор упав; Остап мовчки поклав молот.", true, CommanderBossId, null,
                "Упав, кажеш… Ну, хоч його алебарда більше нікого не різатиме.",
                "За ним — Король. Мого заліза на нього замало. Але що є — твоє."),
            Topic("again", string.Empty, false, null, null, "Залізо принесла? Тоді до горна.")
        };

        /// <summary>
        /// Three temperings, each after the one before (item 6c): about one zone's worth of gold each
        /// in turn, and iron the capital's chests and soldiers drop.
        /// </summary>
        private static MerchantStock BuildOstapStock()
        {
            const string text = "Остап розпікає клинок у горні, що не гасне, і гартує в попелі.";
            var goods = new[]
            {
                Good("temper-1", GoodKind.Temper, "Загартування I", "2 заліза", 300, text,
                    "+10 % до базової шкоди будь-якої зброї в руках.", target: "1"),
                Good("temper-2", GoodKind.Temper, "Загартування II", "4 заліза", 700, text,
                    "Ще +10 % — разом +20 %.", target: "2"),
                Good("temper-3", GoodKind.Temper, "Загартування III", "6 заліза", 1400, text,
                    "Ще +10 % — разом +30 %. Більше горн не дасть.", target: "3")
            };
            goods[0].iron = 2;
            goods[1].iron = 4;
            goods[2].iron = 6;
            var stock = CreateAsset<MerchantStock>(OstapStockPath);
            stock.Configure(goods);
            EditorUtility.SetDirty(stock);
            return stock;
        }

        private static DialogueTopic[] MyroslavaTopics() => new[]
        {
            Topic("altar", "Шукає згаслі вівтарі. Радить запалювати кожен.", true, null, GuardianBossId,
                "Цей вівтар ще теплий. Хтось запалив його до тебе — або він так і не згас.",
                "Триста років… а вогонь пам'ятає королівську руку.",
                "Я шукаю згаслі вівтарі. Побачиш холодний — запали. Він віддячить."),
            Topic("guardian", string.Empty, false, null, GuardianBossId,
                "Далі — Страж. Він не пропускає нікого. Відпочинь, перш ніж іти."),
            Topic("forest", "Каже, що вогонь у Лісі горить інакше.", true, GuardianBossId, WardenBossId,
                "Ти пройшов Стража? Тоді й мене пропусти до вогню.",
                "У Лісі вогонь горить інакше. Зеленіше. Наче щось п'є з нього.",
                "Вартовий — глибше в лісі. Корені слухають його, не короля."),
            Topic("roots", string.Empty, false, GuardianBossId, WardenBossId,
                "Корені пам'ятають кожен крок. Не зупиняйся надовго."),
            Topic("church", "Перейшла до сільської церкви. Каже, під нею крипта.", true, BellringerBossId, null,
                "Церква стояла порожня, доки дзвін бив сам. Тепер тут знову можна молитися.",
                "Під церквою — крипта. Селяни ховали там те, що не віддали королю.",
                "Моя дорога тут кінчається. Твоя — вниз, до Столиці."),
            Topic("end", "Лишилася біля вівтаря глибокого лісу. Її дорога скінчилась.", true, WardenBossId,
                BellringerBossId,
                "Ліс затих. Я чую вогонь — він знову чистий.",
                "Моя дорога тут кінчається. Твоя — ні. Схід чекає."),
            Topic("farewell", string.Empty, false, WardenBossId, null, "Хай вогонь тримає тебе.")
        };

        private static DialogueTopic[] SeverynTopics() => new[]
        {
            Topic("silent", string.Empty, false, null, GuardianBossId,
                "…", "(Привид дивиться на двері арени й мовчить.)"),
            Topic("night", "Привид варти. Пам'ятає ніч, коли впали ворота.", true, GuardianBossId, null,
                "Страж упав… Отже, я вже не вартую.",
                "Ворота впали вночі. Ми тримали залу, доки свічки не догоріли.",
                "Король пішов у вогонь, а ми лишились. Триста років — одна ніч, що не скінчилась."),
            Topic("ossuary", "Розповів про Королівську костницю над проходом варти.", true, GuardianBossId, null,
                "Над проходом варти є костниця. Туди клали найкращих із нас.",
                "Дістанеться лише той, хто стрибне вище, ніж уміє жива людина."),
            Topic("go", string.Empty, false, null, null, "Іди. Хтось має закінчити ту ніч.")
        };

        // ---------- the cart ----------

        private static MerchantGood Good(string id, GoodKind kind, string name, string subtitle, int price,
            string description, string effect, string zone = "", string target = "", string quote = "",
            string requires = "") => new MerchantGood
        {
            id = id, kind = kind, displayName = name, subtitle = subtitle, price = price,
            description = description, effect = effect, zoneId = zone, target = target, quote = quote,
            requiresBoss = requires
        };

        /// <summary>
        /// Prices against one pass of a zone (the Tomb pays ≈630, the Forest ≈1350): the map and
        /// the flask shard about half, the whole cart about 1.4 times. The Forest's goods join the
        /// cart once the Guardian falls; what was not bought in the Tomb comes along.
        /// </summary>
        private static MerchantStock BuildMerchantStock()
        {
            const string rumourText = "Гнат багато чує і дешево не віддає.";
            const string rumourEffect = "Позначка схованки на карті.";
            const string pickText = "Тонкий гачок із загартованої сталі. Ламається після одного замка.";
            const string pickEffect = "Відкриває одну замкнену скриню.";
            const string shardText = "Шматок старої королівської фляги. Приклади до своєї — і вогонь вівтаря "
                + "наповнить на ковток більше.";
            const string shardEffect = "Ще одна фляга, повна з першого ж відпочинку.";
            var goods = new[]
            {
                Good("map-tomb", GoodKind.ZoneMap, "Карта Гробниці", "відкриває всю зону", 50,
                    "Гнат перемалював її з планів варти. Де стіни обвалились — там і на папері діра.",
                    "Усі кімнати Гробниці з'являються на карті блідим обрисом, разом із вівтарями.",
                    TombZoneId),
                Good("quill", GoodKind.Quill, "Перо картографа", "власні позначки", 30,
                    "Гусяче перо й пляшечка чорнила, що не висихає.",
                    "П'ять позначок на карті: R або Y на обраній кімнаті."),
                Good("rumour-ossuary", GoodKind.Rumour, "Чутка", "про Гробницю", 40, rumourText, rumourEffect,
                    TombZoneId, "room_tomb_royal_ossuary",
                    "Над проходом варти є костниця. Туди клали найкращих. Та стеля висока — простим стрибком не дістанеш."),
                Good("rumour-bellshaft", GoodKind.Rumour, "Чутка", "про Гробницю", 40, rumourText, rumourEffect,
                    TombZoneId, "room_tomb_bell_shaft",
                    "Над Поховальною залою — стара дзвонова шахта. Хто вміє бігти по стіні, той туди вибереться."),
                Good("flask-tomb", GoodKind.FlaskShard, "Уламок фляги", "+1 фляга", 300, shardText, shardEffect),
                Good("relic-candle", GoodKind.Relic, "Свічка Паломника", "реліквія", 350,
                    "Недогарок, що горів біля кожного вівтаря на шляху паломників.",
                    "+20 до мани, +2 до інтелекту. Одягається як прикраса.", target: "pilgrim_candle"),
                Good("lockpick-tomb", GoodKind.Lockpick, "Відмичка", "1 замок", 100, pickText, pickEffect,
                    target: MerchantStock.LockpickItemId),

                Good("map-forest", GoodKind.ZoneMap, "Карта Лісу", "відкриває всю зону", 100,
                    "Мисливська карта Лісу. Стежки на ній заросли, але вежі стоять де стояли.",
                    "Усі кімнати Лісу з'являються на карті блідим обрисом, разом із вівтарями.",
                    ForestZoneId, requires: GuardianBossId),
                Good("rumour-perch", GoodKind.Rumour, "Чутка", "про Ліс", 70, rumourText, rumourEffect,
                    ForestZoneId, "room_forest_hunters_perch",
                    "Над руїнами вежі мисливці тримали поміст. Там досі лежить їхнє. З вершини вежі треба ще стрибнути.",
                    GuardianBossId),
                Good("rumour-tower", GoodKind.Rumour, "Чутка", "про Ліс", 70, rumourText, rumourEffect,
                    ForestZoneId, "room_forest_watchtower",
                    "На стару вежу над кроною ще можна вилізти. Згори видно пів лісу — і дещо ще.",
                    GuardianBossId),
                Good("rumour-overlook", GoodKind.Rumour, "Чутка", "про Ліс", 70, rumourText, rumourEffect,
                    ForestZoneId, "room_forest_poison_grove",
                    "За кладкою над яром є гай, куди звичайним стрибком не дістатися. Туди ніхто не ходить. Тим і цікаво.",
                    GuardianBossId),
                Good("flask-forest", GoodKind.FlaskShard, "Уламок фляги", "+1 фляга", 550, shardText, shardEffect,
                    requires: GuardianBossId),
                Good("relic-knot", GoodKind.Relic, "Вузол Шепоту", "реліквія", 600,
                    "Сплетене коріння, що шепоче, коли поруч отрута.",
                    "+15 до здоров'я, отрута чіпляється на 30 % гірше.", target: "whisper_knot",
                    requires: GuardianBossId),
                Good("lockpick-forest-1", GoodKind.Lockpick, "Відмичка", "1 замок", 150, pickText, pickEffect,
                    target: MerchantStock.LockpickItemId, requires: GuardianBossId),
                Good("lockpick-forest-2", GoodKind.Lockpick, "Відмичка", "1 замок", 150, pickText, pickEffect,
                    target: MerchantStock.LockpickItemId, requires: GuardianBossId),

                Good("map-village", GoodKind.ZoneMap, "Карта Села", "відкриває всю зону", 150,
                    "Межова карта села з панських книг. Хати на ній ще цілі.",
                    "Усі кімнати Села з'являються на карті блідим обрисом, разом із вівтарями.",
                    VillageZoneId, requires: WardenBossId),
                Good("rumour-haybarn", GoodKind.Rumour, "Чутка", "про Село", 90, rumourText, rumourEffect,
                    VillageZoneId, "room_village_hay_barn",
                    "У сіннику одна стіна новіша за інші. Селяни щось за нею сховали від збирачів податку.",
                    WardenBossId),
                Good("rumour-attic", GoodKind.Rumour, "Чутка", "про Село", 90, rumourText, rumourEffect,
                    VillageZoneId, "room_village_attic",
                    "Над верхніми хатами є горище, куди не ведуть сходи. Хто туди лазив — не жалівся.",
                    WardenBossId),
                Good("flask-village", GoodKind.FlaskShard, "Уламок фляги", "+1 фляга", 450, shardText, shardEffect,
                    requires: WardenBossId)
            };

            var stock = CreateAsset<MerchantStock>(MerchantStockPath);
            stock.Configure(goods);
            EditorUtility.SetDirty(stock);
            return stock;
        }

        // ---------- prefabs ----------

        /// <summary>
        /// A character: the placement root (<see cref="Npc"/>) and its presence — the lit figure,
        /// the trigger the hero's interaction range meets, and a small light of their own.
        /// </summary>
        private static void BuildNpcPrefab(string path, NpcDefinition definition, Sprite art, Vector2 reach,
            Vector3 lightAt, Color lightColor)
        {
            var root = new GameObject(System.IO.Path.GetFileNameWithoutExtension(path));
            var presence = FindOrCreateChild(root, "Presence");
            var body = FindOrCreateChild(presence, "Body");
            var renderer = Ensure<SpriteRenderer>(body);
            renderer.sprite = art;
            renderer.color = Color.white;
            renderer.sharedMaterial = LitSpriteMaterial();
            SetSorting(renderer, "Characters", 1);
            var trigger = Ensure<BoxCollider2D>(presence);
            trigger.isTrigger = true;
            trigger.size = reach;
            trigger.offset = new Vector2(0f, reach.y * 0.5f);

            var lamp = FindOrCreateChild(presence, "Lamp");
            lamp.transform.localPosition = lightAt;
            var light = AddLight(lamp, Light2D.LightType.Point, lightColor, 0.9f, 2.4f, 0.2f, 0.7f);
            SceneCharacterLight.UseNormalMaps(light, SceneCharacterLight.NormalDistance);

            var npc = Ensure<Npc>(root);
            SetReference(npc, "_definition", definition);
            SetReference(npc, "_presence", presence);
            SavePrefab(root, path);
        }

        private static void BuildLockedChestPrefab(Sprite closed, Sprite open)
        {
            var root = new GameObject("LockedChest");
            var body = FindOrCreateChild(root, "Body");
            var renderer = Ensure<SpriteRenderer>(body);
            renderer.sprite = closed;
            renderer.sharedMaterial = LitSpriteMaterial();
            SetSorting(renderer, "GameplayEnvironment", 4);
            var trigger = Ensure<BoxCollider2D>(root);
            trigger.isTrigger = true;
            trigger.size = new Vector2(1.8f, 1.4f);
            trigger.offset = new Vector2(0f, 0.6f);
            var chest = Ensure<LockedChest>(root);
            SetReference(chest, "_renderer", renderer);
            SetReference(chest, "_closedSprite", closed);
            SetReference(chest, "_openSprite", open);
            SavePrefab(root, LockedChestPrefabPath);
        }
    }
}
