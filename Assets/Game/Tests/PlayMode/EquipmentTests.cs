using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// Equipment in the running level: what may go where, what changes when it does, and what
    /// survives a restart. The stat tests all check the same thing twice — that a bonus arrives
    /// once and leaves cleanly.
    /// </summary>
    public sealed class EquipmentTests
    {
        private PlayerCharacter _player;
        private PlayerMotor2D _motor;
        private PlayerEquipment _equipment;
        private PlayerStats _stats;
        private PlayerCombat _combat;
        private GameProgress _progress;
        private InventoryScreen _screen;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            yield return LoadLevel();
        }

        [TearDown]
        public void TearDown()
        {
            if (_screen != null && _screen.IsOpen)
            {
                _screen.Close();
            }

            GamePause.Clear();
            SaveSystem.Delete();
            Time.timeScale = 1f;
        }

        // ---------- the starting state ----------

        [UnityTest]
        public IEnumerator ANewRunStartsWithTheOldSwordInHand()
        {
            Assert.That(_progress.HasItem(ItemIds.StarterWeapon), Is.True);
            Assert.That(_equipment.GetEquipped(EquipmentSlot.MainWeapon),
                Is.EqualTo(ItemIds.StarterWeapon));
            Assert.That(_equipment.ActiveWeaponDefinition, Is.Not.Null);
            Assert.That(_equipment.ActiveWeaponDefinition.WeaponType, Is.EqualTo(WeaponType.Sword));
            Assert.That(_combat.Weapon, Is.SameAs(_equipment.ActiveWeaponDefinition),
                "Combat swings whatever equipment says is in hand.");
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        // ---------- slot rules ----------

        [UnityTest]
        public IEnumerator EachKindOfItemOnlyGoesIntoItsOwnSlot()
        {
            GiveEverything();

            Assert.That(_equipment.Equip("iron_greatsword", EquipmentSlot.SecondaryWeapon),
                Is.EqualTo(EquipResult.Equipped));
            Assert.That(_equipment.Equip("guard_chestplate", EquipmentSlot.Chest),
                Is.EqualTo(EquipResult.Equipped));
            Assert.That(_equipment.Equip("ring_of_life", EquipmentSlot.Accessory1),
                Is.EqualTo(EquipResult.Equipped));

            Assert.That(_equipment.Equip("guard_chestplate", EquipmentSlot.Head),
                Is.EqualTo(EquipResult.WrongSlot));
            Assert.That(_equipment.Equip("iron_greatsword", EquipmentSlot.Chest),
                Is.EqualTo(EquipResult.WrongSlot));
            Assert.That(_equipment.Equip("ring_of_life", EquipmentSlot.MainWeapon),
                Is.EqualTo(EquipResult.WrongSlot));
            Assert.That(_equipment.Equip("iron_scrap", EquipmentSlot.Chest),
                Is.EqualTo(EquipResult.NotEquippable));
            yield return null;
        }

        [UnityTest]
        public IEnumerator NothingCanBeEquippedThatIsNotOwned()
        {
            Assert.That(_progress.HasItem("iron_greatsword"), Is.False);

            Assert.That(_equipment.Equip("iron_greatsword", EquipmentSlot.MainWeapon),
                Is.EqualTo(EquipResult.NotOwned));
            Assert.That(_equipment.Equip("no_such_item", EquipmentSlot.Chest),
                Is.EqualTo(EquipResult.UnknownItem));
            Assert.That(_equipment.GetEquipped(EquipmentSlot.MainWeapon),
                Is.EqualTo(ItemIds.StarterWeapon), "The refusal changed nothing.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheSameRingCannotBeWornOnBothHands()
        {
            GiveEverything();
            Assert.That(_equipment.Equip("ring_of_life", EquipmentSlot.Accessory1),
                Is.EqualTo(EquipResult.Equipped));

            Assert.That(_equipment.Equip("ring_of_life", EquipmentSlot.Accessory2),
                Is.EqualTo(EquipResult.AlreadyInOtherSlot),
                "Otherwise one ring would pay its bonus twice.");
            Assert.That(_equipment.GetEquipped(EquipmentSlot.Accessory2), Is.Empty);
            yield return null;
        }

        [UnityTest]
        public IEnumerator OneSwordCannotFillBothHands()
        {
            GiveEverything();

            Assert.That(_equipment.Equip("old_sword", EquipmentSlot.SecondaryWeapon),
                Is.EqualTo(EquipResult.AlreadyInOtherSlot));
            yield return null;
        }

        [UnityTest]
        public IEnumerator MovingAWeaponBetweenHandsLeavesNoCopyBehind()
        {
            GiveEverything();
            _equipment.Equip("iron_greatsword", EquipmentSlot.SecondaryWeapon);

            _equipment.Equip("iron_greatsword", EquipmentSlot.MainWeapon);

            Assert.That(_equipment.GetEquipped(EquipmentSlot.MainWeapon),
                Is.EqualTo("iron_greatsword"));
            Assert.That(_equipment.GetEquipped(EquipmentSlot.SecondaryWeapon), Is.Empty);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ArmourAndTrinketsComeOffButTheMainWeaponDoesNot()
        {
            GiveEverything();
            _equipment.Equip("guard_chestplate", EquipmentSlot.Chest);
            _equipment.Equip("ring_of_life", EquipmentSlot.Accessory1);

            Assert.That(_equipment.Unequip(EquipmentSlot.Chest), Is.True);
            Assert.That(_equipment.Unequip(EquipmentSlot.Accessory1), Is.True);
            Assert.That(_equipment.GetEquipped(EquipmentSlot.Chest), Is.Empty);

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(
                "main weapon cannot be unequipped"));
            Assert.That(_equipment.Unequip(EquipmentSlot.MainWeapon), Is.False,
                "There is no unarmed moveset to fall back on.");
            Assert.That(_equipment.ActiveWeaponDefinition, Is.Not.Null);
            yield return null;
        }

        // ---------- two real weapons ----------

        [UnityTest]
        public IEnumerator TheGreatswordIsSlowerHeavierAndHarderHittingThanTheSword()
        {
            GiveEverything();
            var sword = _progress.Items.GetItem("old_sword").Weapon;
            var greatsword = _progress.Items.GetItem("iron_greatsword").Weapon;

            Assert.That(greatsword.WeaponType, Is.EqualTo(WeaponType.Greatsword));
            Assert.That(greatsword.BaseDamage, Is.GreaterThan(sword.BaseDamage));
            Assert.That(greatsword.AttackSpeed, Is.LessThan(sword.AttackSpeed));
            Assert.That(greatsword.StaminaMultiplier, Is.GreaterThan(sword.StaminaMultiplier));
            Assert.That(greatsword.StaggerMultiplier, Is.GreaterThan(sword.StaggerMultiplier));
            Assert.That(greatsword.KnockbackMultiplier, Is.GreaterThan(sword.KnockbackMultiplier));

            // The movesets are genuinely different, not the same timings with bigger numbers.
            var swordFirst = sword.LightAttack(0);
            var greatFirst = greatsword.LightAttack(0);
            Assert.That(greatFirst.Windup, Is.GreaterThan(swordFirst.Windup));
            Assert.That(greatFirst.Recovery, Is.GreaterThan(swordFirst.Recovery));
            Assert.That(greatFirst.HitboxSize.x, Is.GreaterThan(swordFirst.HitboxSize.x),
                "A greatsword reaches further than a sword.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator EquippingTheGreatswordChangesWhatAHitIsWorth()
        {
            GiveEverything();
            _equipment.Equip("iron_greatsword", EquipmentSlot.MainWeapon);
            yield return new WaitForFixedUpdate();

            Assert.That(_combat.Weapon.WeaponType, Is.EqualTo(WeaponType.Greatsword));
            var sword = _progress.Items.GetItem("old_sword").Weapon;
            var great = _progress.Items.GetItem("iron_greatsword").Weapon;
            // Both weapons judged in the same hands, so the difference is the weapon itself.
            var curve = Object.FindAnyObjectByType<PlayerProgressionService>().Curve;
            var strength = _stats.GetFinalStat(CharacterStat.Strength);
            var dexterity = _stats.GetFinalStat(CharacterStat.Dexterity);
            var swordBase = WeaponDamageCalculator.Evaluate(sword, curve, strength, dexterity);
            var greatBase = WeaponDamageCalculator.Evaluate(great, curve, strength, dexterity);
            var swordHit = sword.LightAttack(0)
                .BuildWeaponDamage(sword, swordBase.ScaledBase, null, null, 1);
            var greatHit = great.LightAttack(0)
                .BuildWeaponDamage(great, greatBase.ScaledBase, null, null, 1);
            Assert.That(greatHit.Amount, Is.GreaterThan(swordHit.Amount));
            Assert.That(greatHit.EffectiveGuardDamage,
                Is.GreaterThan(swordHit.EffectiveGuardDamage),
                "Which is why a greatsword is the answer to a shield.");
        }

        // ---------- switching ----------

        [UnityTest]
        public IEnumerator SwitchingSwapsTheWeaponInHandAndBackAgain()
        {
            GiveEverything();
            _equipment.Equip("iron_greatsword", EquipmentSlot.SecondaryWeapon);
            var changes = 0;
            _equipment.ActiveWeaponChanged += _ => changes++;

            Assert.That(_player.TrySwitchWeapon(), Is.True);
            Assert.That(_equipment.IsSecondaryActive, Is.True);
            Assert.That(_combat.Weapon.WeaponType, Is.EqualTo(WeaponType.Greatsword));
            Assert.That(_player.Action, Is.EqualTo(PlayerAction.WeaponSwitch));

            yield return new WaitForSeconds(0.5f);
            Assert.That(_player.Action, Is.Not.EqualTo(PlayerAction.WeaponSwitch),
                "The swap is short and then over.");

            Assert.That(_player.TrySwitchWeapon(), Is.True);
            Assert.That(_equipment.IsSecondaryActive, Is.False);
            Assert.That(_combat.Weapon.WeaponType, Is.EqualTo(WeaponType.Sword));
            Assert.That(changes, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator SwitchingWithAnEmptySecondHandDoesNothingAtAll()
        {
            Assert.That(_equipment.HasSecondaryWeapon, Is.False);

            Assert.That(_player.TrySwitchWeapon(), Is.False);
            Assert.That(_equipment.SwitchWeapon(), Is.False);

            Assert.That(_player.Action, Is.EqualTo(PlayerAction.None));
            Assert.That(_combat.Weapon, Is.Not.Null, "And certainly does not empty the hand.");
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SwitchingDropsTheComboSoTheNewWeaponStartsFresh()
        {
            GiveEverything();
            _equipment.Equip("iron_greatsword", EquipmentSlot.SecondaryWeapon);
            _combat.TryStartLight();
            yield return new WaitForSeconds(0.2f);
            _combat.TryStartLight();
            Assert.That(_combat.ComboIndex, Is.GreaterThan(0));
            yield return new WaitForSeconds(0.6f);

            _player.TrySwitchWeapon();
            yield return new WaitForFixedUpdate();

            Assert.That(_combat.ComboIndex, Is.Zero, "A fresh weapon starts from its first swing.");
            Assert.That(_combat.IsAttacking, Is.False);
        }

        [UnityTest]
        public IEnumerator AWeaponCannotBeSwappedMidSwingOrMidDash()
        {
            GiveEverything();
            _equipment.Equip("iron_greatsword", EquipmentSlot.SecondaryWeapon);

            _combat.TryStartLight();
            yield return new WaitForFixedUpdate();
            Assert.That(_combat.IsAttacking, Is.True);
            Assert.That(_player.TrySwitchWeapon(), Is.False,
                "Swapping out of a live swing would let the hitbox belong to a weapon that is "
                + "no longer in hand.");
            Assert.That(_equipment.IsSecondaryActive, Is.False);
            yield return new WaitForSeconds(0.8f);

            _motor.TryBeginDash(1);
            yield return new WaitForFixedUpdate();
            Assert.That(_motor.IsDashing, Is.True);
            Assert.That(_player.TrySwitchWeapon(), Is.False);
        }

        [UnityTest]
        public IEnumerator ADeadOrStunnedPlayerCannotSwapWeapons()
        {
            GiveEverything();
            _equipment.Equip("iron_greatsword", EquipmentSlot.SecondaryWeapon);
            _player.Actor.ClearInvulnerability();
            _player.Actor.ApplyDamage(new DamageInfo(float.MaxValue, Vector2.right,
                isUnblockable: true));
            yield return new WaitForFixedUpdate();

            Assert.That(_player.IsAlive, Is.False);
            Assert.That(_player.TrySwitchWeapon(), Is.False);
        }

        // ---------- armour ----------

        [UnityTest]
        public IEnumerator ArmourReducesTheSameHitAndComesBackOffCleanly()
        {
            GiveEverything();
            var bare = TakeHit(20f);
            Assert.That(_stats.PhysicalDefense, Is.Zero);

            _equipment.Equip("guard_chestplate", EquipmentSlot.Chest);
            yield return new WaitForFixedUpdate();
            Assert.That(_stats.PhysicalDefense, Is.GreaterThan(0f));
            var armoured = TakeHit(20f);

            Assert.That(armoured, Is.LessThan(bare), "Armour has to be worth wearing.");
            Assert.That(armoured, Is.GreaterThan(0f), "And never makes a hit free.");

            _equipment.Unequip(EquipmentSlot.Chest);
            yield return new WaitForFixedUpdate();
            Assert.That(_stats.PhysicalDefense, Is.Zero);
            Assert.That(TakeHit(20f), Is.EqualTo(bare).Within(0.01f),
                "Taking it off restores exactly the old numbers.");
        }

        [UnityTest]
        public IEnumerator EvenTheHeaviestArmourLeavesEveryHitHurting()
        {
            GiveEverything();
            _equipment.Equip("traveler_hood", EquipmentSlot.Head);
            _equipment.Equip("guard_chestplate", EquipmentSlot.Chest);
            yield return new WaitForFixedUpdate();

            Assert.That(TakeHit(1f), Is.GreaterThan(0f), "No armour set makes a hit free.");
            Assert.That(TakeHit(60f), Is.GreaterThan(0f));
            Assert.That(_stats.EquipmentWeight, Is.GreaterThan(0f), "Weight is tracked.");
        }

        [UnityTest]
        public IEnumerator HeavyArmourAndTheTalismanBothMakeStaggeringHarder()
        {
            GiveEverything();
            var bare = _player.Stagger.Resistance;

            _equipment.Equip("iron_talisman", EquipmentSlot.Accessory1);
            yield return new WaitForFixedUpdate();
            var withTalisman = _player.Stagger.Resistance;

            _equipment.Equip("guard_chestplate", EquipmentSlot.Chest);
            yield return new WaitForFixedUpdate();

            Assert.That(withTalisman, Is.GreaterThan(bare));
            Assert.That(_player.Stagger.Resistance, Is.GreaterThan(withTalisman),
                "Heavy armour adds poise on top of the trinket.");

            _equipment.Unequip(EquipmentSlot.Accessory1);
            _equipment.Unequip(EquipmentSlot.Chest);
            yield return new WaitForFixedUpdate();
            Assert.That(_player.Stagger.Resistance, Is.EqualTo(bare).Within(0.0001f));
        }

        // ---------- accessories ----------

        [UnityTest]
        public IEnumerator TheRingOfLifeRaisesMaximumHealthExactlyOnce()
        {
            GiveEverything();
            var baseMax = _player.Health.Max;

            _equipment.Equip("ring_of_life", EquipmentSlot.Accessory1);
            yield return new WaitForFixedUpdate();
            var withRing = _player.Health.Max;
            Assert.That(withRing, Is.EqualTo(baseMax + 10f).Within(0.001f));

            // Re-equipping the same ring, and a full recalculation, must not add it again.
            _equipment.Equip("ring_of_life", EquipmentSlot.Accessory1);
            _stats.Recalculate();
            _stats.Recalculate();
            yield return new WaitForFixedUpdate();
            Assert.That(_player.Health.Max, Is.EqualTo(withRing).Within(0.001f));

            _equipment.Unequip(EquipmentSlot.Accessory1);
            yield return new WaitForFixedUpdate();
            Assert.That(_player.Health.Max, Is.EqualTo(baseMax).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator SwappingARingOnAndOffIsNeverAFreeHeal()
        {
            GiveEverything();
            _player.Health.SetCurrent(40f);
            var wounded = _player.Health.Current;

            for (var i = 0; i < 10; i++)
            {
                _equipment.Equip("ring_of_life", EquipmentSlot.Accessory1);
                _equipment.Unequip(EquipmentSlot.Accessory1);
            }

            yield return new WaitForFixedUpdate();

            Assert.That(_player.Health.Current, Is.EqualTo(wounded).Within(0.001f),
                "Current health is kept, not refilled, when the maximum moves.");
            Assert.That(_player.Health.Max, Is.EqualTo(100f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator ARingComingOffClampsHealthInsteadOfLeavingItAboveTheMaximum()
        {
            GiveEverything();
            _equipment.Equip("ring_of_life", EquipmentSlot.Accessory1);
            yield return new WaitForFixedUpdate();
            _player.Health.RestoreToFull();
            Assert.That(_player.Health.Current, Is.EqualTo(110f).Within(0.001f));

            _equipment.Unequip(EquipmentSlot.Accessory1);
            yield return new WaitForFixedUpdate();

            Assert.That(_player.Health.Max, Is.EqualTo(100f).Within(0.001f));
            Assert.That(_player.Health.Current, Is.EqualTo(100f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator TheRingOfEnduranceDoesTheSameForStamina()
        {
            GiveEverything();
            var baseMax = _player.Stamina.Max;
            _player.Stamina.Drain(30f);
            var spent = _player.Stamina.Current;

            _equipment.Equip("ring_of_endurance", EquipmentSlot.Accessory1);
            yield return new WaitForFixedUpdate();

            Assert.That(_player.Stamina.Max, Is.EqualTo(baseMax + 10f).Within(0.001f));
            Assert.That(_player.Stamina.Current, Is.EqualTo(spent).Within(0.5f),
                "A bigger bar is not a refilled one.");
        }

        [UnityTest]
        public IEnumerator TheSheathedWeaponGrantsNothing()
        {
            GiveEverything();
            _equipment.Equip("iron_greatsword", EquipmentSlot.SecondaryWeapon);
            yield return new WaitForFixedUpdate();

            var carried = _stats.ActiveModifiers.Count;
            _player.TrySwitchWeapon();
            yield return new WaitForFixedUpdate();

            Assert.That(_stats.ActiveModifiers.Count, Is.EqualTo(carried),
                "Neither weapon carries passive stats today, and the sheathed one never counts.");
            Assert.That(_combat.Weapon.WeaponType, Is.EqualTo(WeaponType.Greatsword));
        }

        // ---------- persistence ----------

        [UnityTest]
        public IEnumerator AWholeLoadoutComesBackAfterARestart()
        {
            GiveEverything();
            _equipment.Equip("iron_greatsword", EquipmentSlot.SecondaryWeapon);
            _equipment.Equip("guard_chestplate", EquipmentSlot.Chest);
            _equipment.Equip("traveler_hood", EquipmentSlot.Head);
            _equipment.Equip("ring_of_life", EquipmentSlot.Accessory1);
            _equipment.Equip("ring_of_endurance", EquipmentSlot.Accessory2);
            _player.TrySwitchWeapon();
            _progress.SaveNow();
            yield return new WaitForSecondsRealtime(0.4f);

            yield return LoadLevel();

            Assert.That(_equipment.GetEquipped(EquipmentSlot.SecondaryWeapon),
                Is.EqualTo("iron_greatsword"));
            Assert.That(_equipment.GetEquipped(EquipmentSlot.Chest), Is.EqualTo("guard_chestplate"));
            Assert.That(_equipment.GetEquipped(EquipmentSlot.Head), Is.EqualTo("traveler_hood"));
            Assert.That(_equipment.GetEquipped(EquipmentSlot.Accessory1), Is.EqualTo("ring_of_life"));
            Assert.That(_equipment.IsSecondaryActive, Is.True,
                "The weapon that was in hand is the one that comes back in hand.");
            Assert.That(_combat.Weapon.WeaponType, Is.EqualTo(WeaponType.Greatsword));
            Assert.That(_player.Health.Max, Is.EqualTo(110f).Within(0.001f),
                "And the bars come back the size the equipment makes them.");
            Assert.That(_stats.PhysicalDefense, Is.GreaterThan(0f));
        }

        [UnityTest]
        public IEnumerator AnEquippedItemFromAnotherBuildIsDroppedRatherThanCrashing()
        {
            var data = GameProgressData.CreateNew();
            data.equipment.Set(EquipmentSlot.Chest, "armour_from_a_mod");
            data.equipment.Set(EquipmentSlot.Accessory1, "ring_that_was_cut");
            SaveSystem.Write(data);

            LogAssert.ignoreFailingMessages = true;
            yield return LoadLevel();
            LogAssert.ignoreFailingMessages = false;

            Assert.That(_equipment.GetEquipped(EquipmentSlot.Chest), Is.Empty);
            Assert.That(_equipment.GetEquipped(EquipmentSlot.Accessory1), Is.Empty);
            Assert.That(_equipment.ActiveWeaponDefinition, Is.Not.Null,
                "And the player still has a weapon to fight with.");
        }

        [UnityTest]
        public IEnumerator AWeaponEquippedButNotOwnedIsReplacedByOneThatIs()
        {
            var data = GameProgressData.CreateNew();
            // The file claims a greatsword the inventory does not contain.
            data.equipment.Set(EquipmentSlot.MainWeapon, "iron_greatsword");
            SaveSystem.Write(data);

            LogAssert.ignoreFailingMessages = true;
            yield return LoadLevel();
            LogAssert.ignoreFailingMessages = false;

            Assert.That(_equipment.GetEquipped(EquipmentSlot.MainWeapon),
                Is.EqualTo(ItemIds.StarterWeapon), "It falls back to a weapon really owned.");
            Assert.That(_progress.GetItemAmount("iron_greatsword"), Is.Zero,
                "And no phantom copy is created to justify the file.");
        }

        [UnityTest]
        public IEnumerator DyingKeepsTheLoadoutAndOnlyResetsTheFight()
        {
            GiveEverything();
            _equipment.Equip("iron_greatsword", EquipmentSlot.MainWeapon);
            _equipment.Equip("guard_chestplate", EquipmentSlot.Chest);
            yield return new WaitForFixedUpdate();

            var respawner = Object.FindAnyObjectByType<PlayerRespawner>();
            respawner.Kill();
            yield return new WaitForSecondsRealtime(2.5f);

            Assert.That(_player.IsAlive, Is.True);
            Assert.That(_equipment.GetEquipped(EquipmentSlot.MainWeapon),
                Is.EqualTo("iron_greatsword"), "Death costs progress, not equipment.");
            Assert.That(_equipment.GetEquipped(EquipmentSlot.Chest), Is.EqualTo("guard_chestplate"));
            Assert.That(_combat.Weapon.WeaponType, Is.EqualTo(WeaponType.Greatsword));
        }

        // ---------- the screen ----------

        [UnityTest]
        public IEnumerator OpeningTheScreenFreezesTheWorldAndClosingItLetsGo()
        {
            Assert.That(_screen, Is.Not.Null);

            _screen.Open();
            Assert.That(_screen.IsOpen, Is.True);
            Assert.That(GamePause.IsPaused, Is.True);
            Assert.That(Time.timeScale, Is.Zero, "A paused enemy cannot swing at a reading player.");

            _screen.Close();
            Assert.That(_screen.IsOpen, Is.False);
            Assert.That(GamePause.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheScreenListsWhatIsOwnedUnderTheRightTab()
        {
            GiveEverything();
            _progress.AddItem("iron_scrap", 15);
            _screen.Open();

            _screen.SelectTab(ItemCategory.Weapon);
            var weapons = _screen.Entries.Select(entry => entry.ItemId).ToArray();
            Assert.That(weapons, Does.Contain("old_sword").And.Contain("iron_greatsword"));
            Assert.That(weapons, Does.Not.Contain("iron_scrap"));

            _screen.SelectTab(ItemCategory.Material);
            var scrap = _screen.Entries.First(entry => entry.ItemId == "iron_scrap");
            Assert.That(scrap.Amount, Is.EqualTo(15));
            Assert.That(scrap.AmountText, Is.EqualTo("x15"));
            Assert.That(scrap.CanEquip, Is.False, "A material has nothing to equip into.");

            _screen.SelectTab(ItemCategory.Accessory);
            Assert.That(_screen.Entries.Select(entry => entry.ItemId),
                Does.Contain("ring_of_life"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator AnEmptyTabIsEmptyRatherThanBroken()
        {
            _screen.Open();
            _screen.SelectTab(ItemCategory.KeyItem);

            Assert.That(_screen.IsEmptyTab, Is.True);
            Assert.That(_screen.SelectedItemId, Is.Empty);
            Assert.That(_screen.DescribeSelected(), Is.Empty);
            Assert.That(_screen.CompareSelected(), Is.Empty);
            _screen.MoveSelection(1);
            Assert.That(_screen.EquipSelected(), Is.EqualTo(EquipResult.UnknownItem));
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator EquippingFromTheScreenChangesTheWeaponAndMarksItEquipped()
        {
            GiveEverything();
            _screen.Open();
            _screen.SelectTab(ItemCategory.Weapon);
            _screen.Select("iron_greatsword");

            Assert.That(_screen.DescribeSelected(), Is.Not.Empty);
            Assert.That(_screen.CompareSelected(), Is.Not.Empty,
                "The player can see the trade before taking it.");
            Assert.That(_screen.Equip("iron_greatsword", EquipmentSlot.MainWeapon),
                Is.EqualTo(EquipResult.Equipped));

            var entry = _screen.Entries.First(item => item.ItemId == "iron_greatsword");
            Assert.That(entry.IsEquipped, Is.True);
            _screen.Close();
            yield return new WaitForFixedUpdate();

            Assert.That(_combat.Weapon.WeaponType, Is.EqualTo(WeaponType.Greatsword),
                "Closing the menu hands the new weapon to combat.");
        }

        [UnityTest]
        public IEnumerator ClosingTheScreenAfterAChangeWritesTheSave()
        {
            GiveEverything();
            SaveSystem.Delete();
            _screen.Open();
            _screen.Equip("guard_chestplate", EquipmentSlot.Chest);
            _screen.Close();

            yield return new WaitForSecondsRealtime(0.6f);

            Assert.That(SaveSystem.HasValidSave(), Is.True);
            Assert.That(SaveSystem.Read().equipment.Get(EquipmentSlot.Chest),
                Is.EqualTo("guard_chestplate"));
        }

        [UnityTest]
        public IEnumerator OpeningAndClosingWithoutTouchingAnythingWritesNothing()
        {
            SaveSystem.Delete();

            _screen.Open();
            _screen.SelectTab(ItemCategory.Material);
            _screen.Close();
            yield return new WaitForSecondsRealtime(0.5f);

            Assert.That(SaveSystem.HasValidSave(), Is.False,
                "Reading the menu is not progress and must not cost a disk write.");
        }

        [UnityTest]
        public IEnumerator TheScreenCannotBeOpenedByADeadPlayer()
        {
            _player.Actor.ClearInvulnerability();
            _player.Actor.ApplyDamage(new DamageInfo(float.MaxValue, Vector2.right,
                isUnblockable: true));
            yield return new WaitForFixedUpdate();

            _screen.Open();

            Assert.That(_screen.IsOpen, Is.False);
            Assert.That(GamePause.IsPaused, Is.False);
        }

        // ---------- helpers ----------

        private IEnumerator LoadLevel()
        {
            yield return TestWorld.Load();
            _player = Object.FindAnyObjectByType<PlayerCharacter>();
            _motor = _player.GetComponent<PlayerMotor2D>();
            _equipment = _player.GetComponent<PlayerEquipment>();
            _stats = _player.GetComponent<PlayerStats>();
            _combat = _player.GetComponent<PlayerCombat>();
            _progress = Object.FindAnyObjectByType<GameProgress>();
            _screen = Object.FindAnyObjectByType<InventoryScreen>();
            yield return new WaitForFixedUpdate();
        }

        private void GiveEverything()
        {
            foreach (var itemId in new[]
            {
                "iron_greatsword", "traveler_hood", "traveler_coat", "guard_chestplate",
                "ring_of_life", "ring_of_endurance", "iron_talisman"
            })
            {
                _progress.AddItem(itemId);
            }

            // These tests are about slots and stats, not about attribute gates, so the character
            // is made strong enough to lift everything it owns. Requirements have their own tests.
            var progression = Object.FindAnyObjectByType<PlayerProgressionService>();
            if (progression != null)
            {
                progression.SetStat(CharacterStat.Strength, 20);
                progression.SetStat(CharacterStat.Dexterity, 20);
            }
        }

        /// <summary>Applies a hit of a known size and reports what actually reached health.</summary>
        private float TakeHit(float amount)
        {
            _player.Health.RestoreToFull();
            _player.Actor.ClearInvulnerability();
            var result = _player.Actor.ApplyDamage(new DamageInfo(amount, Vector2.right,
                isUnblockable: true));
            _player.Health.RestoreToFull();
            return result.AppliedDamage;
        }
    }
}
