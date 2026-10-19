using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The smith Ostap in the capital's workshop (item 6c): three temperings bought in turn, each for
    /// gold and iron scrap, each adding a tenth of the weapon's own base to the weapon in hand.
    /// </summary>
    public sealed class OstapTests
    {
        private GameProgress _progress;
        private ShopService _shop;
        private PlayerStats _stats;
        private NpcDefinition _ostap;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            yield return TestWorld.Load();
            yield return TestWorld.LoadEveryZone();
            _progress = Object.FindAnyObjectByType<GameProgress>();
            _shop = Object.FindAnyObjectByType<ShopService>();
            _stats = Object.FindAnyObjectByType<PlayerStats>();
            _ostap = Object.FindObjectsByType<Npc>(FindObjectsInactive.Include)
                .Select(n => n.Definition).First(d => d != null && d.NpcId == "ostap");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            _shop.Close();
            SaveSystem.Delete();
            yield return null;
        }

        [UnityTest]
        public IEnumerator HeStandsInTheWorkshopAndTemperingIsBoughtInTurnForGoldAndIron()
        {
            var smith = Object.FindObjectsByType<Npc>(FindObjectsInactive.Include).First(n => n.Definition == _ostap);
            Assert.That(CapitalScene.InRoom(smith, "room_capital_workshop"), Is.True, "At his forge in the workshop.");

            _progress.AddCurrency(5000);
            _shop.Open(_ostap);
            yield return null;
            var first = _shop.Offered.Single(g => g.id == "temper-1");
            var second = _shop.Offered.Single(g => g.id == "temper-2");
            Assert.That(_shop.Check(first), Is.EqualTo(PurchaseResult.NoIron), "Gold alone is not enough.");
            _progress.AddItem(Tempering.IronItemId, 12);
            Assert.That(_shop.Check(second), Is.EqualTo(PurchaseResult.NotYet), "The second after the first.");

            var before = _stats.WeaponDamage;
            Assert.That(_shop.Buy(first), Is.EqualTo(PurchaseResult.Bought));
            yield return null;
            Assert.That(_progress.GetItemAmount(Tempering.IronItemId), Is.EqualTo(10), "The iron goes into the forge.");
            Assert.That(Tempering.Level(_progress), Is.EqualTo(1));
            Assert.That(_stats.WeaponDamage.ScaledBase, Is.GreaterThan(before.ScaledBase + before.WeaponBase * 0.09f),
                "A tenth of the weapon's base more.");
            Assert.That(_shop.Check(second), Is.EqualTo(PurchaseResult.Bought), "Now the second.");
        }
    }
}
