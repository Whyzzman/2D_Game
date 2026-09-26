using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The item layer with no scene around it: the catalogue, what the player owns, and how a drop
    /// table rolls. All of this is plain data, so it is checked here rather than in play mode.
    /// </summary>
    public sealed class ItemSystemTests
    {
        [TearDown]
        public void TearDown()
        {
            SaveSystem.Delete();
        }

        // ---------- database ----------

        [Test]
        public void TheDatabaseFindsAnItemByIdAndRefusesAnUnknownOne()
        {
            var database = Database(Item("iron_scrap"), Item("healing_herb"));

            Assert.That(database.TryGetItem("iron_scrap", out var scrap), Is.True);
            Assert.That(scrap.ItemId, Is.EqualTo("iron_scrap"));
            Assert.That(database.Contains("healing_herb"), Is.True);
            Assert.That(database.Contains("no_such_item"), Is.False);
            Assert.That(database.GetItem("no_such_item"), Is.Null);
            Assert.That(database.Contains(string.Empty), Is.False);
        }

        [Test]
        public void TwoItemsSharingAnIdAreReportedAsAnError()
        {
            var database = Database(false, Item("iron_scrap"), Item("iron_scrap"));

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(
                "used by more than one item"));
            database.Rebuild();
            Assert.That(database.Contains("iron_scrap"), Is.True,
                "The first entry still answers, so one bad asset cannot break every lookup.");
        }

        [Test]
        public void TheShippedCatalogueHasUniqueLowercaseIds()
        {
            var database = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemDatabase>(
                "Assets/Game/Data/ItemDatabase.asset");
            Assert.That(database, Is.Not.Null, "The content build writes the item database.");

            var ids = database.Items.Select(item => item.ItemId).ToArray();
            Assert.That(ids, Is.Unique);
            Assert.That(ids, Has.All.Matches<string>(id => id == id.ToLowerInvariant()));
            Assert.That(ids, Does.Contain("iron_scrap"));
            Assert.That(ids, Does.Contain("guardian_core"));
        }

        // ---------- what the player owns ----------

        [Test]
        public void AddingTheSameMaterialTwiceStacksIt()
        {
            var items = new ItemCollection();

            Assert.That(items.Add("iron_scrap", 2, false), Is.EqualTo(AddItemResult.Added));
            Assert.That(items.Add("iron_scrap", 3, false), Is.EqualTo(AddItemResult.Added));

            Assert.That(items.GetAmount("iron_scrap"), Is.EqualTo(5));
            Assert.That(items.DistinctItemCount, Is.EqualTo(1), "One id is one entry.");
        }

        [Test]
        public void AUniqueItemIsNeverOwnedTwice()
        {
            var items = new ItemCollection();

            Assert.That(items.Add("old_sword", 1, true), Is.EqualTo(AddItemResult.Added));
            Assert.That(items.Add("old_sword", 5, true), Is.EqualTo(AddItemResult.AlreadyOwned));

            Assert.That(items.GetAmount("old_sword"), Is.EqualTo(1));
        }

        [Test]
        public void RemovingTakesExactlyWhatIsThereAndNeverGoesNegative()
        {
            var items = new ItemCollection();
            items.Add("iron_scrap", 3, false);

            Assert.That(items.TryRemove("iron_scrap", 4), Is.False, "More than is owned is refused.");
            Assert.That(items.GetAmount("iron_scrap"), Is.EqualTo(3), "A refused removal changes nothing.");
            Assert.That(items.TryRemove("iron_scrap", 2), Is.True);
            Assert.That(items.GetAmount("iron_scrap"), Is.EqualTo(1));
            Assert.That(items.TryRemove("iron_scrap", 1), Is.True);
            Assert.That(items.GetAmount("iron_scrap"), Is.Zero);
            Assert.That(items.DistinctItemCount, Is.Zero, "An empty stack leaves no entry behind.");
            Assert.That(items.TryRemove("iron_scrap", 1), Is.False);
            Assert.That(items.TryRemove("never_owned", 1), Is.False);
        }

        [Test]
        public void BrokenEntriesAndAmountsAreRejectedOrCleanedUp()
        {
            var items = new ItemCollection();

            Assert.That(items.Add(string.Empty, 1, false), Is.EqualTo(AddItemResult.InvalidItem));
            Assert.That(items.Add("iron_scrap", 0, false), Is.EqualTo(AddItemResult.InvalidItem));
            Assert.That(items.Add("iron_scrap", -3, false), Is.EqualTo(AddItemResult.InvalidItem));
            Assert.That(items.DistinctItemCount, Is.Zero);
        }

        [Test]
        public void AHandEditedInventoryIsMergedInsteadOfTrusted()
        {
            // Two entries for one id and one broken entry, as a patched file could contain.
            var items = JsonUtility.FromJson<ItemCollection>(
                "{\"_stacks\":[{\"itemId\":\"iron_scrap\",\"amount\":2},"
                + "{\"itemId\":\"iron_scrap\",\"amount\":3},"
                + "{\"itemId\":\"\",\"amount\":9},"
                + "{\"itemId\":\"healing_herb\",\"amount\":0}]}");

            items.Normalize();

            Assert.That(items.GetAmount("iron_scrap"), Is.EqualTo(5));
            Assert.That(items.GetAmount("healing_herb"), Is.Zero);
            Assert.That(items.DistinctItemCount, Is.EqualTo(1));
        }

        // ---------- drop tables ----------

        [Test]
        public void AGuaranteedEntryAlwaysDropsAndAZeroChanceEntryNeverDoes()
        {
            var table = Table(
                "{\"_isCurrency\":true,\"_chance\":1,\"_isGuaranteed\":true,"
                + "\"_minAmount\":5,\"_maxAmount\":5}",
                "{\"_itemId\":\"guard_emblem\",\"_chance\":0,\"_minAmount\":1,\"_maxAmount\":1}");

            var random = new System.Random(1234);
            for (var i = 0; i < 200; i++)
            {
                var results = table.Roll(random);
                Assert.That(results.Count, Is.EqualTo(1));
                Assert.That(results[0].IsCurrency, Is.True);
                Assert.That(results[0].Amount, Is.EqualTo(5));
            }
        }

        [Test]
        public void AmountsStayInsideTheAuthoredRangeAndCanHitBothEnds()
        {
            var table = Table(
                "{\"_itemId\":\"iron_scrap\",\"_chance\":1,\"_minAmount\":1,\"_maxAmount\":3}");

            var random = new System.Random(7);
            var seen = new System.Collections.Generic.HashSet<int>();
            for (var i = 0; i < 500; i++)
            {
                var amount = table.Roll(random)[0].Amount;
                Assert.That(amount, Is.InRange(1, 3));
                seen.Add(amount);
            }

            Assert.That(seen, Does.Contain(1), "The lower bound is reachable.");
            Assert.That(seen, Does.Contain(3), "The upper bound is inclusive.");
        }

        [Test]
        public void AChanceIsRoughlyRespectedOverManyRolls()
        {
            var table = Table(
                "{\"_itemId\":\"iron_scrap\",\"_chance\":0.25,\"_minAmount\":1,\"_maxAmount\":1}");

            var random = new System.Random(99);
            var drops = 0;
            for (var i = 0; i < 4000; i++)
            {
                drops += table.Roll(random).Count;
            }

            // Wide bounds on purpose: this catches an inverted or ignored chance, not noise.
            Assert.That(drops / 4000f, Is.EqualTo(0.25f).Within(0.05f));
        }

        [Test]
        public void EveryEntryIsRolledOnItsOwnSoOneDeathCanGiveSeveralThings()
        {
            var table = Table(
                "{\"_isCurrency\":true,\"_chance\":1,\"_minAmount\":5,\"_maxAmount\":5}",
                "{\"_itemId\":\"iron_scrap\",\"_chance\":1,\"_minAmount\":1,\"_maxAmount\":1}",
                "{\"_itemId\":\"healing_herb\",\"_chance\":1,\"_minAmount\":1,\"_maxAmount\":1}");

            var results = table.Roll(new System.Random(5));

            Assert.That(results.Count, Is.EqualTo(3));
            Assert.That(results.Count(result => result.IsCurrency), Is.EqualTo(1));
        }

        [Test]
        public void AnEmptyTableRollsNothingRatherThanFailing()
        {
            var table = ScriptableObject.CreateInstance<DropTable>();

            Assert.That(table.Roll(new System.Random(1)), Is.Empty);
            Assert.That(table.Roll(null), Is.Empty, "A missing generator is not a crash.");
        }

        // ---------- the save file ----------

        [Test]
        public void ItemsAndCollectedPickupsSurviveARoundTrip()
        {
            var data = GameProgressData.CreateNew();
            data.inventory.Add("iron_scrap", 4, false);
            data.inventory.Add("old_sword", 1, true);
            data.collectedPickups.Add("tomb-old-sword");
            data.player.currency = 150;

            Assert.That(SaveSystem.Write(data), Is.True);
            var loaded = SaveSystem.Read();

            Assert.That(loaded.inventory.GetAmount("iron_scrap"), Is.EqualTo(4));
            Assert.That(loaded.inventory.GetAmount("old_sword"), Is.EqualTo(1));
            Assert.That(loaded.collectedPickups, Does.Contain("tomb-old-sword"));
            Assert.That(loaded.player.currency, Is.EqualTo(150));
        }

        [Test]
        public void AFileFromBeforeLootLoadsWithAnEmptyInventory()
        {
            // A version two file: checkpoints and bosses, but no inventory and no pickup list.
            File.WriteAllText(SaveSystem.FilePath,
                "{\"version\":2,\"checkpointId\":\"tomb-altar-01\","
                + "\"defeatedBosses\":[\"guardian-of-the-tomb\"]}");

            var loaded = SaveSystem.Read();

            Assert.That(loaded.version, Is.EqualTo(GameProgressData.CurrentVersion));
            Assert.That(loaded.inventory, Is.Not.Null);
            Assert.That(loaded.inventory.GetAmount("iron_scrap"), Is.Zero,
                "Nothing is invented: only the starter weapon is granted, by the equipment "
                + "migration, so the player is not left unarmed.");
            Assert.That(loaded.inventory.HasItem(ItemIds.StarterWeapon), Is.True);
            Assert.That(loaded.collectedPickups, Is.Not.Null.And.Empty);
            Assert.That(loaded.IsBossDefeated("guardian-of-the-tomb"), Is.True,
                "Adding loot must not cost an older save its progress.");
        }

        [Test]
        public void ACollectedPickupIsNeverRecordedTwice()
        {
            var data = GameProgressData.CreateNew();
            data.collectedPickups.Add("tomb-old-sword");
            data.collectedPickups.Add("tomb-old-sword");
            File.WriteAllText(SaveSystem.FilePath, JsonUtility.ToJson(data, true));

            var loaded = SaveSystem.Read();

            Assert.That(loaded.collectedPickups.Count, Is.EqualTo(1));
        }

        // ---------- helpers ----------

        private static ItemDefinition Item(string id, bool isUnique = false)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            JsonUtility.FromJsonOverwrite(
                $"{{\"_itemId\":\"{id}\",\"_isUnique\":{(isUnique ? "true" : "false")}}}", item);
            return item;
        }

        private static ItemDatabase Database(params ItemDefinition[] items)
        {
            return Database(true, items);
        }

        private static ItemDatabase Database(bool build, params ItemDefinition[] items)
        {
            var database = ScriptableObject.CreateInstance<ItemDatabase>();
            typeof(ItemDatabase)
                .GetField("_items", System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.NonPublic)
                .SetValue(database, items);
            if (build)
            {
                database.Rebuild();
            }

            return database;
        }

        private static DropTable Table(params string[] entryJson)
        {
            var table = ScriptableObject.CreateInstance<DropTable>();
            JsonUtility.FromJsonOverwrite(
                "{\"_entries\":[" + string.Join(",", entryJson) + "]}", table);
            return table;
        }
    }
}
