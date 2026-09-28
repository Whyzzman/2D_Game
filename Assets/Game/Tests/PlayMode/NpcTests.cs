using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The first people of the world in the level (item 70, 2026-09-28): who stands where as the
    /// story moves, a conversation that pauses the world and is remembered, the merchant's cart,
    /// a locked chest, and the journal of people.
    /// </summary>
    public sealed class NpcTests
    {
        private const string Guardian = "guardian-of-the-tomb";
        private GameProgress _progress;
        private DialogueService _dialogue;
        private ShopService _shop;
        private HudController _hud;
        private PlayerCharacter _player;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            yield return TestWorld.Load();
            yield return null;
            _progress = Object.FindAnyObjectByType<GameProgress>();
            _dialogue = Object.FindAnyObjectByType<DialogueService>();
            _shop = Object.FindAnyObjectByType<ShopService>();
            _hud = Object.FindAnyObjectByType<HudController>();
            _player = Object.FindAnyObjectByType<PlayerCharacter>();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            _dialogue?.Close();
            _shop?.Close();
            GamePause.Clear();
            SaveSystem.Delete();
            yield return null;
        }

        private static Npc[] Placements(string npcId) =>
            Object.FindObjectsByType<Npc>(FindObjectsInactive.Include)
                .Where(n => n.Definition != null && n.Definition.NpcId == npcId).ToArray();

        private static NpcDefinition Definition(string npcId) => Placements(npcId)[0].Definition;

        [UnityTest]
        public IEnumerator EachCharacterStandsInOnePlaceAndMovesOnWithTheStory()
        {
            Assert.That(_dialogue, Is.Not.Null);
            Assert.That(_shop, Is.Not.Null);
            foreach (var id in new[] { "gnat", "myroslava", "severyn" })
            {
                Assert.That(Placements(id).Count(n => n.IsPresent), Is.EqualTo(1), $"{id} before the Guardian");
            }

            var merchantBefore = Placements("gnat").Single(n => n.IsPresent);
            _progress.MarkBossDefeated(Guardian);
            yield return null;
            var merchantAfter = Placements("gnat").Single(n => n.IsPresent);
            Assert.That(merchantAfter, Is.Not.SameAs(merchantBefore), "The merchant moved to the forest.");
            Assert.That(merchantAfter.transform.position.x, Is.GreaterThan(62f), "The forest starts at x 62.");
            Assert.That(Placements("myroslava").Count(n => n.IsPresent), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator EveryPersonAndChestStandsOnTheGround()
        {
            yield return null;
            var standing = Object.FindObjectsByType<Npc>(FindObjectsInactive.Include).Select(n => n.transform)
                .Concat(Object.FindObjectsByType<LockedChest>(FindObjectsInactive.Include).Select(c => c.transform));
            foreach (var thing in standing)
            {
                var hit = Physics2D.Raycast((Vector2)thing.position + Vector2.up * 0.3f, Vector2.down, 0.6f, 1 << 9);
                Assert.That((bool)hit, Is.True, $"{thing.name} at {thing.position} floats or sinks.");
            }
        }

        [UnityTest]
        public IEnumerator ATalkPausesTheWorldTypesItsPagesAndIsRemembered()
        {
            var pilgrim = Definition("myroslava");
            Assert.That(_dialogue.Begin(pilgrim), Is.True);
            yield return null;
            Assert.That(GamePause.IsPaused, Is.True, "Nothing walks up to a hero who is reading.");
            Assert.That(_hud.Dialogue.IsOpen, Is.True);
            Assert.That(_dialogue.IsTyping, Is.True, "The page is typed out, not shown at once.");

            var pages = _dialogue.PageCount;
            var topic = _dialogue.Topic;
            for (var i = 0; i < pages * 2 + 2 && _dialogue.IsOpen; i++)
            {
                _dialogue.Advance();
                yield return null;
            }

            Assert.That(_dialogue.IsOpen, Is.False);
            Assert.That(_hud.Dialogue.IsOpen, Is.False);
            Assert.That(GamePause.IsPaused, Is.False);
            Assert.That(_progress.HasReward(pilgrim.HeardId(topic)), Is.True);
            Assert.That(_progress.HasReward(pilgrim.MetId), Is.True);
            Assert.That(pilgrim.Pick(_progress), Is.Not.SameAs(topic), "Said once, then something else.");

            Assert.That(_hud.Inventory.Open(InventoryView.Page.People), Is.True);
            yield return null;
            Assert.That(_hud.Inventory.People.Shown, Does.Contain(pilgrim.DisplayName));
            Assert.That(_hud.Inventory.People.Shown, Has.No.Member(Definition("severyn").DisplayName),
                "Nobody unmet is listed.");
            _hud.Inventory.Close();
        }

        [UnityTest]
        public IEnumerator TheMerchantSellsOnceAndRefusesInWords()
        {
            var merchant = Definition("gnat");
            _progress.AddCurrency(420);
            Assert.That(_dialogue.Begin(merchant), Is.True);
            for (var i = 0; i < 20 && !_dialogue.IsChoosing; i++)
            {
                _dialogue.Advance();
                yield return null;
            }

            Assert.That(_dialogue.IsChoosing, Is.True, "The merchant ends with «Торгувати / Піти».");
            _dialogue.Trade();
            yield return null;
            Assert.That(_shop.IsOpen && _hud.Shop.IsOpen, Is.True);
            Assert.That(_shop.Offered.Any(g => !string.IsNullOrEmpty(g.requiresBoss)), Is.False,
                "The forest's goods wait for the Guardian.");

            var map = _shop.Offered.First(g => g.kind == GoodKind.ZoneMap);
            var purse = _progress.Currency;
            Assert.That(_shop.Buy(map), Is.EqualTo(PurchaseResult.Bought));
            Assert.That(_progress.Currency, Is.EqualTo(purse - map.price));
            Assert.That(_progress.HasProgression(MerchantStock.MapRewardId(map.zoneId)), Is.True);
            Assert.That(_shop.Buy(map), Is.EqualTo(PurchaseResult.AlreadySold));

            var flasks = _player.Flasks.MaxCharges;
            Assert.That(_shop.Buy(_shop.Offered.First(g => g.kind == GoodKind.FlaskShard)), Is.EqualTo(PurchaseResult.Bought));
            yield return null;
            Assert.That(_player.Flasks.MaxCharges, Is.EqualTo(flasks + 1), "A shard is one more flask.");

            var relic = _shop.Offered.First(g => g.kind == GoodKind.Relic);
            Assert.That(_shop.Buy(relic), Is.EqualTo(PurchaseResult.TooPoor));
            Assert.That(_shop.Describe(relic, PurchaseResult.TooPoor), Does.StartWith("Бракує"));

            var rumour = _shop.Offered.First(g => g.kind == GoodKind.Rumour);
            Assert.That(_shop.Buy(rumour), Is.EqualTo(PurchaseResult.Bought));
            yield return null;
            Assert.That(_hud.Shop.IsRumourShown, Is.True, "A rumour shows its words.");
            Assert.That(_progress.HasProgression(MerchantStock.RumourRewardId(rumour.target)), Is.True);

            _shop.Close();
            yield return null;
            Assert.That(GamePause.IsPaused, Is.False);
        }

        [UnityTest]
        public IEnumerator AChestTakesALockpickPaysOutAndStaysOpen()
        {
            var chest = Object.FindObjectsByType<LockedChest>(FindObjectsInactive.Include)
                .Single(c => c.ChestId == "chest-tomb-crypt");
            Assert.That(chest.CanInteract, Is.False, "Locked without a lockpick.");
            Assert.That(chest.Prompt, Does.Contain("відмичка"));

            _progress.AddItem(MerchantStock.LockpickItemId);
            var purse = _progress.Currency;
            Assert.That(chest.CanInteract, Is.True);
            chest.Interact(_player);
            yield return null;

            Assert.That(chest.IsOpen, Is.True);
            Assert.That(_progress.Currency, Is.GreaterThan(purse));
            Assert.That(_progress.HasItem(MerchantStock.LockpickItemId), Is.False, "The lockpick breaks.");
            Assert.That(_progress.IsPickupCollected(chest.ChestId), Is.True, "Remembered by the save.");
            Assert.That(chest.CanInteract, Is.False);
        }
    }
}
