using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// Loot in the running level: what a dead enemy leaves, what picking it up does to the save,
    /// and what a reset sweeps away. The world reset rules are the point of most of these.
    /// </summary>
    public sealed class LootAndPickupTests
    {
        private PlayerCharacter _player;
        private PlayerMotor2D _motor;
        private PlayerRespawner _respawner;
        private GameProgress _progress;
        private LootService _loot;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            yield return LoadLevel();
        }

        [TearDown]
        public void TearDown()
        {
            SaveSystem.Delete();
            Time.timeScale = 1f;
        }

        // ---------- the level is wired ----------

        [UnityTest]
        public IEnumerator TheLevelHasALootServiceAndEveryEnemyCanDrop()
        {
            Assert.That(_loot, Is.Not.Null);
            Assert.That(_progress.Items, Is.Not.Null, "Progress knows the catalogue.");

            var droppers = Object.FindObjectsByType<LootDropper>()
                .Where(dropper => dropper.GetComponent<EnemyController>() != null
                    && !dropper.name.StartsWith("Training"))
                .ToArray();
            Assert.That(droppers, Is.Not.Empty);
            Assert.That(droppers.Where(dropper => dropper.name.StartsWith("ZombieSoldier")
                    || dropper.name.StartsWith("ShieldGuard")),
                Has.All.Matches<LootDropper>(dropper => dropper.DropTable != null),
                "Both real enemy kinds carry a drop table.");
            yield return null;
        }

        // ---------- enemy drops ----------

        [UnityTest]
        public IEnumerator KillingAnEnemyLeavesLootOnTheGround()
        {
            var zombie = Zombie();
            var before = _loot.TransientPickupCount;

            yield return Kill(zombie);

            Assert.That(_loot.TransientPickupCount, Is.GreaterThan(before),
                "The guaranteed coin entry always lands.");
        }

        [UnityTest]
        public IEnumerator OneDeathRollsTheTableExactlyOnce()
        {
            var zombie = Zombie();
            yield return Kill(zombie);
            var afterFirstDeath = _loot.TransientPickupCount;

            // A second damage event on a corpse must not pay again.
            zombie.Actor.ClearInvulnerability();
            zombie.Actor.ApplyDamage(new DamageInfo(9999f, Vector2.right, isUnblockable: true));
            yield return new WaitForFixedUpdate();

            Assert.That(_loot.TransientPickupCount, Is.EqualTo(afterFirstDeath));
            Assert.That(zombie.GetComponent<LootDropper>().HasDropped, Is.True);
        }

        [UnityTest]
        public IEnumerator RestingClearsLootNobodyPickedUpAndArmsTheEnemyAgain()
        {
            var zombie = Zombie();
            var dropper = zombie.GetComponent<LootDropper>();
            yield return Kill(zombie);
            Assert.That(_loot.TransientPickupCount, Is.GreaterThan(0));

            _respawner.ResetWorld(WorldResetReason.CheckpointRest);
            yield return null;
            yield return null;

            Assert.That(_loot.TransientPickupCount, Is.Zero,
                "Uncollected drops go back with the enemies that left them.");
            Assert.That(dropper.HasDropped, Is.False, "The next real death pays again.");
            Assert.That(zombie.IsAlive, Is.True);
        }

        [UnityTest]
        public IEnumerator PuttingAnEnemyBackWithoutKillingItDropsNothing()
        {
            var zombie = Zombie();
            var before = _loot.TransientPickupCount;

            zombie.ResetEnemy();
            yield return new WaitForFixedUpdate();

            Assert.That(_loot.TransientPickupCount, Is.EqualTo(before));
            Assert.That(zombie.GetComponent<LootDropper>().HasDropped, Is.False);
        }

        [UnityTest]
        public IEnumerator DyingDoesNotBankLootThePlayerNeverTouched()
        {
            var zombie = Zombie();
            yield return Kill(zombie);
            var coins = _progress.Currency;

            _respawner.ResetWorld(WorldResetReason.PlayerDeath);
            yield return null;

            Assert.That(_progress.Currency, Is.EqualTo(coins), "Only picking loot up grants it.");
            Assert.That(_loot.TransientPickupCount, Is.Zero);
        }

        // ---------- picking things up ----------

        [UnityTest]
        public IEnumerator WalkingOverAPurseAddsTheCoins()
        {
            var before = _progress.Currency;
            var purse = _loot.Spawn(new DropResult(string.Empty, true, 30),
                (Vector2)_player.transform.position + new Vector2(0f, 0.2f));
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.That(purse.IsCollected, Is.True, "Coins are walked into, not pressed.");
            Assert.That(_progress.Currency, Is.EqualTo(before + 30));
        }

        [UnityTest]
        public IEnumerator AMaterialPickupLandsInTheInventory()
        {
            var pickup = _loot.Spawn(new DropResult("iron_scrap", false, 2),
                (Vector2)_player.transform.position + new Vector2(3f, 0.2f));
            yield return new WaitForFixedUpdate();

            Assert.That(pickup.TryCollect(_player), Is.True);
            Assert.That(_progress.GetItemAmount("iron_scrap"), Is.EqualTo(2));
            Assert.That(pickup.gameObject.activeSelf, Is.False, "A taken pickup leaves the world.");
            Assert.That(pickup.TryCollect(_player), Is.False, "It cannot be taken twice.");
            Assert.That(_progress.GetItemAmount("iron_scrap"), Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator AnUnknownItemIdSpawnsNothingAndGrantsNothing()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(
                "unknown item"));
            var pickup = _loot.Spawn(new DropResult("not_a_real_item", false, 1),
                (Vector2)_player.transform.position + new Vector2(3f, 0.2f));
            yield return null;

            Assert.That(pickup, Is.Null);
            Assert.That(_progress.HasItem("not_a_real_item"), Is.False,
                "Only the starter weapon a new run comes with is owned.");
        }

        [UnityTest]
        public IEnumerator AHandPlacedUniquePickupStaysTakenAfterAReload()
        {
            var coat = HandPlaced("tomb-coat");
            Assert.That(coat, Is.Not.Null, "The level holds the unique test pickup.");
            Assert.That(coat.IsPersistent, Is.True);

            Assert.That(coat.TryCollect(_player), Is.True);
            Assert.That(_progress.HasItem("traveler_coat"), Is.True);
            Assert.That(_progress.IsPickupCollected("tomb-coat"), Is.True);
            yield return new WaitForSecondsRealtime(0.4f);

            yield return LoadLevel();

            Assert.That(_progress.HasItem("traveler_coat"), Is.True,
                "It is still owned after a reload.");
            var restored = HandPlaced("tomb-coat");
            Assert.That(restored == null || !restored.gameObject.activeSelf, Is.True,
                "A unique item is never lying there a second time.");
        }

        [UnityTest]
        public IEnumerator AHandPlacedPickupIsStillThereIfItWasNeverTaken()
        {
            Assert.That(HandPlaced("tomb-coat").gameObject.activeSelf, Is.True);

            _progress.SaveNow();
            yield return new WaitForSecondsRealtime(0.4f);
            yield return LoadLevel();

            Assert.That(HandPlaced("tomb-coat").gameObject.activeSelf, Is.True);
        }

        [UnityTest]
        public IEnumerator HandPlacedLootIsNotSweptUpByAReset()
        {
            var scrap = HandPlaced("tomb-scrap-01");
            Assert.That(scrap, Is.Not.Null);

            _respawner.ResetWorld(WorldResetReason.CheckpointRest);
            yield return null;

            Assert.That(scrap != null && scrap.gameObject.activeSelf, Is.True,
                "Only enemy drops are temporary.");
        }

        // ---------- currency ----------

        [UnityTest]
        public IEnumerator CoinsAddUpAndAreSpentOnlyWhenThereAreEnough()
        {
            _progress.AddCurrency(100);
            Assert.That(_progress.Currency, Is.EqualTo(100));

            Assert.That(_progress.TrySpendCurrency(150), Is.False);
            Assert.That(_progress.Currency, Is.EqualTo(100), "A refused purchase costs nothing.");
            Assert.That(_progress.TrySpendCurrency(40), Is.True);
            Assert.That(_progress.Currency, Is.EqualTo(60));
            Assert.That(_progress.TrySpendCurrency(60), Is.True);
            Assert.That(_progress.Currency, Is.Zero, "Currency never goes below zero.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheCurrencyEventReportsTheTotalAndTheChange()
        {
            var total = -1;
            var delta = 0;
            _progress.CurrencyChanged += (current, change) => { total = current; delta = change; };

            _progress.AddCurrency(25);

            Assert.That(total, Is.EqualTo(_progress.Currency));
            Assert.That(delta, Is.EqualTo(25));
            yield return null;
        }

        [UnityTest]
        public IEnumerator CoinsAndItemsSurviveARestart()
        {
            _progress.AddCurrency(77);
            _progress.AddItem("iron_scrap", 3);
            _progress.SaveNow();
            yield return new WaitForSecondsRealtime(0.4f);

            yield return LoadLevel();

            Assert.That(_progress.Currency, Is.EqualTo(77));
            Assert.That(_progress.GetItemAmount("iron_scrap"), Is.EqualTo(3));
        }

        // ---------- the boss reward ----------

        [UnityTest]
        public IEnumerator TheGuardianHandsOverItsCoreAndItsCoinsOnce()
        {
            var encounter = TombScene.GuardianEncounter();
            _motor.ResetTo(new Vector2(42f, 1f));
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            var boss = encounter.Boss;
            boss.Actor.ClearInvulnerability();
            boss.Actor.ApplyDamage(new DamageInfo(float.MaxValue, Vector2.left,
                isUnblockable: true, attacker: _player.gameObject));
            yield return new WaitForFixedUpdate();

            Assert.That(_progress.HasItem("guardian_core"), Is.True,
                "The story reward is granted directly, never dropped where it could be missed.");
            Assert.That(_progress.Currency, Is.EqualTo(500));
            yield return new WaitForSecondsRealtime(0.4f);

            yield return LoadLevel();
            Assert.That(_progress.GetItemAmount("guardian_core"), Is.EqualTo(1),
                "A beaten Guardian never pays a second time.");
            Assert.That(_progress.Currency, Is.EqualTo(500));
        }

        // ---------- helpers ----------

        private IEnumerator LoadLevel()
        {
            yield return SceneManager.LoadSceneAsync("TombTrial");
            _player = Object.FindAnyObjectByType<PlayerCharacter>();
            _motor = _player.GetComponent<PlayerMotor2D>();
            _respawner = Object.FindAnyObjectByType<PlayerRespawner>();
            _progress = Object.FindAnyObjectByType<GameProgress>();
            _loot = Object.FindAnyObjectByType<LootService>();
            _loot.SetRandom(new System.Random(20260920));
            yield return new WaitForFixedUpdate();
        }

        private EnemyController Zombie()
        {
            return Object.FindObjectsByType<EnemyController>()
                .First(enemy => enemy.name.StartsWith("ZombieSoldier"));
        }

        private WorldPickup HandPlaced(string persistentId)
        {
            return Resources.FindObjectsOfTypeAll<WorldPickup>()
                .FirstOrDefault(pickup => pickup.PersistentId == persistentId
                    && pickup.gameObject.scene.IsValid());
        }

        private IEnumerator Kill(EnemyController enemy)
        {
            enemy.Actor.ClearInvulnerability();
            enemy.Actor.ApplyDamage(new DamageInfo(9999f, Vector2.right, isUnblockable: true,
                attacker: _player.gameObject));
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
        }
    }
}
