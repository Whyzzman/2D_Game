using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The inventory window in the real HUD: it opens from the pause and from its key, draws what
    /// the player owns, equips from both pages and hands back to the pause it came from.
    /// </summary>
    public sealed class InventoryViewTests
    {
        private HudController _hud;
        private InventoryScreen _screen;
        private PlayerEquipment _equipment;
        private GameProgress _progress;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            yield return TestWorld.Load();
            yield return new WaitForSeconds(0.3f);
            _hud = Object.FindAnyObjectByType<HudController>();
            _screen = Object.FindAnyObjectByType<InventoryScreen>();
            _equipment = Object.FindAnyObjectByType<PlayerCharacter>().Equipment;
            _progress = Object.FindAnyObjectByType<GameProgress>();
        }

        [TearDown]
        public void TearDown()
        {
            _screen?.Close();
            _hud?.Pause?.Resume();
            GamePause.Clear();
            SaveSystem.Delete();
        }

        [UnityTest]
        public IEnumerator ThePauseOpensTheWindowAndBackReturnsToThePause()
        {
            var pause = _hud.Pause;
            pause.Open();
            Assert.That(pause.EnabledButtons, Does.Contain("pause-equipment"));

            pause.OpenEquipment();
            Assert.That(_hud.Inventory.IsOpen, Is.True);
            Assert.That(pause.IsOpen, Is.False, "The pause steps aside.");
            Assert.That(GamePause.IsPaused, Is.True, "The game stays still under the window.");
            yield return null;

            pause.Toggle(); // Escape
            Assert.That(_hud.Inventory.IsOpen, Is.False);
            Assert.That(pause.IsOpen, Is.True, "Opened from the pause, it goes back there.");
            pause.Resume();
            Assert.That(GamePause.IsPaused, Is.False);
        }

        [UnityTest]
        public IEnumerator TheKeyOpensTheGridWithWhatIsOwnedAndEscapeReturnsToTheGame()
        {
            Assert.That(_screen.HasPresenter, Is.True, "The HUD draws the screen, so its key works.");
            _screen.Toggle();
            yield return null;
            Assert.That(_hud.Inventory.IsOpen, Is.True);
            Assert.That(_screen.ActiveTab, Is.EqualTo(ItemCategory.Weapon));
            Assert.That(_hud.Inventory.ItemCellCount, Is.EqualTo(_screen.Entries.Count));
            Assert.That(_hud.Inventory.ItemCellCount, Is.GreaterThan(0), "The starting sword is there.");

            _hud.Pause.Toggle(); // Escape
            Assert.That(_hud.Inventory.IsOpen, Is.False);
            Assert.That(_hud.Pause.IsOpen, Is.False, "Opened by its key, it closes to the game.");
            Assert.That(GamePause.IsPaused, Is.False);
        }

        [UnityTest]
        public IEnumerator AHerbIsEatenInTheOpenNotOnThePausedScreen()
        {
            var player = Object.FindAnyObjectByType<PlayerCharacter>();
            _progress.AddItem("healing_herb", 2);
            var pause = _hud.Pause;
            pause.Open();
            pause.OpenEquipment();
            _screen.SelectTab(ItemCategory.Consumable);
            _screen.Select("healing_herb");
            yield return null;

            Assert.That(_screen.WhyCannotUseSelected(), Is.EqualTo("Здоров'я повне"));
            _hud.Inventory.ActOnSelectedItem();
            Assert.That(_hud.Inventory.IsOpen, Is.True, "Nothing to heal: the herb stays in the bag.");
            Assert.That(_progress.GetItemAmount("healing_herb"), Is.EqualTo(2));

            player.Health.SetCurrent(50f);
            _hud.Inventory.ActOnSelectedItem();
            Assert.That(_hud.Inventory.IsOpen, Is.False);
            Assert.That(pause.IsOpen, Is.False, "Straight to the game, not back to the pause.");
            Assert.That(GamePause.IsPaused, Is.False);
            Assert.That(player.Action, Is.EqualTo(PlayerAction.Heal), "Eaten standing, in the open.");
            Assert.That(player.Flasks.IsDrinkingFlask, Is.False);
            Assert.That(_progress.GetItemAmount("healing_herb"), Is.EqualTo(1));
            Assert.That(player.Flasks.Charges, Is.EqualTo(player.Flasks.MaxCharges));

            var end = Time.realtimeSinceStartup + 2f;
            while (Time.realtimeSinceStartup < end && player.Action == PlayerAction.Heal)
            {
                yield return null;
            }

            Assert.That(player.Health.Current, Is.EqualTo(70f).Within(0.01f), "Twenty back.");
        }

        [UnityTest]
        public IEnumerator TheWindowDoesNotOpenOverThePause()
        {
            _hud.Pause.Open();
            _screen.Toggle();
            yield return null;
            Assert.That(_screen.IsOpen, Is.False);
            Assert.That(_hud.Inventory.IsOpen, Is.False);
        }

        [UnityTest]
        public IEnumerator TheEquipmentPagePutsOnAndTakesOffWhatFitsTheChosenSlot()
        {
            _progress.AddItem("traveler_hood");
            Assume.That(_equipment.GetEquipped(EquipmentSlot.Head), Is.Not.EqualTo("traveler_hood"));
            Assert.That(_hud.Inventory.Open(InventoryView.Page.Equipment), Is.True);
            yield return null;

            _hud.Inventory.SelectSlot(EquipmentSlot.Head);
            Assert.That(_hud.Inventory.Candidates, Does.Contain("traveler_hood"));
            Assert.That(_hud.Inventory.Candidates, Has.None.EqualTo("old_sword"),
                "Only what fits the slot is listed.");

            _hud.Inventory.SelectCandidate("traveler_hood");
            _hud.Inventory.EquipCandidate();
            Assert.That(_equipment.GetEquipped(EquipmentSlot.Head), Is.EqualTo("traveler_hood"));

            _hud.Inventory.UnequipSlot();
            Assert.That(_equipment.GetEquipped(EquipmentSlot.Head), Is.Null.Or.Empty);

            _hud.Inventory.SelectSlot(EquipmentSlot.MainWeapon);
            var sword = _equipment.GetEquipped(EquipmentSlot.MainWeapon);
            _hud.Inventory.UnequipSlot();
            Assert.That(_equipment.GetEquipped(EquipmentSlot.MainWeapon), Is.EqualTo(sword),
                "There is no unarmed moveset: the main hand is never emptied.");
        }

        [UnityTest]
        public IEnumerator AWeaponTooHeavyToLiftIsRefusedWithAReason()
        {
            _progress.AddItem("warden_greatblade");
            _screen.Toggle();
            yield return null;
            _screen.Select("warden_greatblade");
            var entry = _screen.SelectedEntry;
            Assume.That(entry, Is.Not.Null);
            Assume.That(entry.Value.CanEquip, Is.False, "The start character is too weak for it.");

            _hud.Inventory.EquipSelectedItem();
            Assert.That(_equipment.IsEquipped("warden_greatblade"), Is.False);
            var problem = _hud.GetComponent<UIDocument>().rootVisualElement.Q<Label>("inv-problem");
            Assert.That(problem.text, Does.StartWith("Не вистачає"), "A refusal explains itself.");
        }
    }
}
