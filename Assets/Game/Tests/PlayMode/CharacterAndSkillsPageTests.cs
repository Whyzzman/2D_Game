using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The Character and Skills pages of the inventory window, in the real HUD: points are spent
    /// through them, kept, and a skill can be learned and put into an active slot.
    /// </summary>
    public sealed class CharacterAndSkillsPageTests
    {
        private HudController _hud;
        private InventoryScreen _screen;
        private PlayerStats _stats;
        private PlayerProgressionService _progression;
        private PlayerSkills _skills;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            yield return TestWorld.Load();
            yield return new WaitForSeconds(0.3f);
            _hud = Object.FindAnyObjectByType<HudController>();
            _screen = Object.FindAnyObjectByType<InventoryScreen>();
            _stats = Object.FindAnyObjectByType<PlayerCharacter>().Stats;
            _progression = Object.FindAnyObjectByType<PlayerProgressionService>();
            _skills = Object.FindAnyObjectByType<PlayerSkills>();
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
        public IEnumerator ThePauseOpensTheCharacterAndSkillsPages()
        {
            var pause = _hud.Pause;
            pause.Open();
            pause.OpenCharacter();
            Assert.That(_hud.Inventory.IsOpen, Is.True);
            Assert.That(_hud.Inventory.CurrentPage, Is.EqualTo(InventoryView.Page.Character));
            yield return null;
            pause.Toggle(); // Escape
            Assert.That(pause.IsOpen, Is.True);

            pause.OpenSkills();
            Assert.That(_hud.Inventory.CurrentPage, Is.EqualTo(InventoryView.Page.Skills));
            Assert.That(_hud.Inventory.Skills.NodeCount, Is.GreaterThan(0), "The Warrior tree is drawn.");
        }

        [UnityTest]
        public IEnumerator APointSpentOnThePageRaisesTheAttributeAndIsSavedOnClosing()
        {
            _progression.GrantStatPoints(1);
            var before = _stats.GetFinalStat(CharacterStat.Vitality);
            var health = _stats.MaxHealth;
            Assert.That(_hud.Inventory.Open(InventoryView.Page.Character), Is.True);
            yield return null;

            Assert.That(_hud.Inventory.Character.Allocate(CharacterStat.Vitality), Is.True);
            Assert.That(_stats.GetFinalStat(CharacterStat.Vitality), Is.EqualTo(before + 1));
            Assert.That(_stats.MaxHealth, Is.GreaterThan(health));
            Assert.That(_progression.StatPoints, Is.Zero);
            Assert.That(_hud.Inventory.Character.Allocate(CharacterStat.Vitality), Is.False,
                "No points left, nothing spent.");

            _screen.Close();
            yield return new WaitForSecondsRealtime(1.5f);
            var saved = SaveSystem.Read();
            Assert.That(saved, Is.Not.Null, "Closing after spending a point saves it.");
            Assert.That(saved.player.statPoints, Is.Zero);
        }

        [UnityTest]
        public IEnumerator WillPreviewsShadowPowerRatherThanAWeaponNumber()
        {
            yield return null;
            var lines = StatPreview.Lines(CharacterStat.Will, _stats,
                Object.FindAnyObjectByType<PlayerCharacter>().Equipment);
            Assert.That(lines, Has.Some.StartsWith("Сила Тіні"));
            Assert.That(lines, Has.None.StartsWith("Атака"));
            _progression.GrantStatPoints(1); // the one-line preview is shown only with a point to spend
            Assert.That(_screen.PreviewAllocation(CharacterStat.Will), Does.StartWith("Сила Тіні"));
        }

        [UnityTest]
        public IEnumerator ASkillIsLearnedFromTheTreeAndAnActiveOneGoesIntoASlot()
        {
            _skills.GrantBonusPoints(3);
            Assert.That(_hud.Inventory.Open(InventoryView.Page.Skills), Is.True);
            yield return null;

            var page = _hud.Inventory.Skills;
            page.SelectBranch(SkillBranch.Warrior);
            page.Select("warrior_training");
            Assert.That(page.Buy(), Is.EqualTo(SkillPurchaseResult.Purchased));
            Assert.That(_skills.HasSkill("warrior_training"), Is.True);

            page.SelectBranch(SkillBranch.Shadow);
            Assert.That(page.NodeCount, Is.GreaterThan(0), "Shadow is a branch of its own now.");
            _skills.DebugUnlock("shadow_dash");
            page.Select("shadow_dash");
            Assert.That(page.AssignNext(), Is.True);
            Assert.That(_screen.ActiveSlotSkillId(0) == "shadow_dash"
                || _screen.ActiveSlotSkillId(1) == "shadow_dash", Is.True);
            var first = _screen.ActiveSlotSkillId(0) == "shadow_dash" ? 0 : 1;
            Assert.That(page.AssignNext(), Is.True);
            Assert.That(_screen.ActiveSlotSkillId(1 - first), Is.EqualTo("shadow_dash"),
                "Pressing again moves it to the other slot.");
        }
    }
}
