using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The bestiary (item 4 of the big list, stage D): at the start only the Tomb and its boss,
    /// all «???»; a kill names its kind with a count and a dot, the dot goes once seen, and the
    /// count survives a load.
    /// </summary>
    public sealed class BestiaryTests
    {
        private GameProgress _progress;
        private HudController _hud;
        private PlayerCharacter _player;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            yield return TestWorld.Load();
            _progress = Object.FindAnyObjectByType<GameProgress>();
            _hud = Object.FindAnyObjectByType<HudController>();
            _player = Object.FindAnyObjectByType<PlayerCharacter>();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            _hud.Inventory.Close();
            SaveSystem.Delete();
            yield return null;
        }

        private BestiaryPage Open()
        {
            _hud.Inventory.Close();
            Assert.That(_hud.Inventory.Open(InventoryView.Page.Bestiary), Is.True);
            return _hud.Inventory.Bestiary;
        }

        [UnityTest]
        public IEnumerator ANewGameShowsOnlyTheTombAllUnknown()
        {
            var page = Open();
            yield return null;
            Assert.That(page.Zones, Is.EqualTo(new[] { "Забута Гробниця", "Боси" }), "Only where the hero has been.");
            Assert.That(page.Shown, Is.EqualTo(new[] { BestiaryPage.Unknown, BestiaryPage.Unknown, BestiaryPage.Unknown, BestiaryPage.Unknown }), "Three Tomb kinds and its boss.");
            Assert.That(page.KindsText, Is.EqualTo("0 з 3 видів"));
        }

        [UnityTest]
        public IEnumerator AKillNamesItsKindAndIsCountedAcrossALoad()
        {
            var zombie = Object.FindObjectsByType<EnemyController>()
                .First(e => e.Definition != null && e.Definition.name == "ZombieSoldier");
            zombie.Actor.ClearInvulnerability();
            zombie.Actor.ApplyDamage(new DamageInfo(float.MaxValue, Vector2.left, isUnblockable: true,
                attacker: _player.gameObject));
            yield return null;
            Assert.That(Bestiary.Kills(_progress, zombie.Definition), Is.EqualTo(1));

            var page = Open();
            yield return null;
            Assert.That(page.Shown, Does.Contain($"{zombie.Definition.DisplayName} · 1"));
            Assert.That(page.KindsText, Is.EqualTo("1 з 3 видів"));

            _progress.SaveNow();
            _progress.LoadGame();
            yield return null;
            Assert.That(Bestiary.Kills(_progress, zombie.Definition), Is.EqualTo(1), "The count is in the save.");
            Assert.That(Bestiary.IsSeen(_progress, zombie.Definition), Is.True, "Seen once shown: no dot next time.");
        }

        [UnityTest]
        public IEnumerator EveryKindOfTheWorldAndItsBossesAreInTheCatalog()
        {
            var catalog = _progress.BestiaryCatalog;
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.Enemies.Count, Is.EqualTo(14),
                "Three in the Tomb, four in the Forest, three in the Village, four in the Capital.");
            Assert.That(catalog.Enemies.All(e => e.Enemy != null), Is.True);
            Assert.That(catalog.Bosses.Select(b => b.BossId),
                Is.EquivalentTo(new[] { TombScene.GuardianId, TombScene.WardenId, "ashen-king" }));
            yield return null;
        }
    }
}
