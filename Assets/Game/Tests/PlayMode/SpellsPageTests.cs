using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>The spellbook page (spellbook.html): learned spells go into the combat slots, others cannot.</summary>
    public sealed class SpellsPageTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            yield return TestWorld.Load();
            yield return new WaitForSeconds(0.3f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.FindAnyObjectByType<InventoryScreen>()?.Close();
            GamePause.Clear();
            SaveSystem.Delete();
        }

        [UnityTest]
        public IEnumerator ALearnedSpellGoesIntoTheChosenSlotAndComesOut()
        {
            var hud = Object.FindAnyObjectByType<HudController>();
            var book = Object.FindAnyObjectByType<PlayerSpellbook>();
            book.Learn("fire_bolt");
            Assert.That(hud.Inventory.Open(InventoryView.Page.Spells), Is.True);
            yield return null;

            var page = hud.Inventory.Spells;
            Assert.That(page.SpellCount, Is.EqualTo(book.Database.Spells.Count), "The whole book is listed.");
            page.Select("fire_bolt");
            page.Target(2);
            Assert.That(page.Assign(), Is.True);
            Assert.That(book.SlotSpellId(2), Is.EqualTo("fire_bolt"));
            Assert.That(page.Remove(), Is.True);
            Assert.That(book.SlotSpellId(2), Is.Empty);
        }

        [UnityTest]
        public IEnumerator ASpellNotYetLearnedCannotBeSlotted()
        {
            var hud = Object.FindAnyObjectByType<HudController>();
            var book = Object.FindAnyObjectByType<PlayerSpellbook>();
            Assume.That(book.Knows("mana_shield"), Is.False);
            Assert.That(hud.Inventory.Open(InventoryView.Page.Spells), Is.True);
            yield return null;

            var page = hud.Inventory.Spells;
            page.Select("mana_shield");
            page.Target(0);
            Assert.That(page.Assign(), Is.False);
            Assert.That(book.SlotSpellId(0), Is.Not.EqualTo("mana_shield"));
        }
    }
}
