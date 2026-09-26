using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The equipment rules that are pure data: how modifiers add up, what a loadout is, what the
    /// numbers behind a swing are, and what an older save turns into.
    /// </summary>
    public sealed class EquipmentDataTests
    {
        [TearDown]
        public void TearDown()
        {
            SaveSystem.Delete();
        }

        // ---------- modifiers ----------

        [Test]
        public void AddingAndMultiplyingLandOnTheBaseValue()
        {
            var stats = new StatBlock();
            stats.SetBase(StatType.MaxHealth, 100f);

            stats.Apply(new StatModifier(StatType.MaxHealth, ModifierOperation.Add, 10f));
            Assert.That(stats.Get(StatType.MaxHealth), Is.EqualTo(110f));

            stats.Apply(new StatModifier(StatType.MaxHealth, ModifierOperation.Multiply, 1.1f));
            Assert.That(stats.Get(StatType.MaxHealth), Is.EqualTo(121f).Within(0.001f),
                "Additions land first, then the multiplier scales the result.");
        }

        [Test]
        public void RecalculatingTheSameModifiersNeverStacksThem()
        {
            var stats = new StatBlock();
            stats.SetBase(StatType.MaxHealth, 100f);
            var ring = new StatModifier(StatType.MaxHealth, ModifierOperation.Add, 10f);

            for (var i = 0; i < 50; i++)
            {
                stats.ClearModifiers();
                stats.Apply(ring);
            }

            Assert.That(stats.Get(StatType.MaxHealth), Is.EqualTo(110f),
                "Fifty recalculations are worth exactly one ring.");
        }

        [Test]
        public void ClearingModifiersLeavesTheBaseAlone()
        {
            var stats = new StatBlock();
            stats.SetBase(StatType.MaxStamina, 100f);
            stats.Apply(new StatModifier(StatType.MaxStamina, ModifierOperation.Add, 25f));

            stats.ClearModifiers();

            Assert.That(stats.Get(StatType.MaxStamina), Is.EqualTo(100f));
        }

        [Test]
        public void TwoDifferentRingsBothCount()
        {
            var stats = new StatBlock();
            stats.SetBase(StatType.PhysicalDefense, 0f);
            stats.Apply(new List<StatModifier>
            {
                new StatModifier(StatType.PhysicalDefense, ModifierOperation.Add, 5f),
                new StatModifier(StatType.PhysicalDefense, ModifierOperation.Add, 3f)
            });

            Assert.That(stats.Get(StatType.PhysicalDefense), Is.EqualTo(8f));
        }

        // ---------- slots ----------

        [Test]
        public void EachKindOfEquipmentOnlyFitsItsOwnSlots()
        {
            var sword = Weapon("old_sword", 20f);
            var chest = Armor("guard_chestplate", EquipmentSlot.Chest, 10f);
            var ring = Accessory("ring_of_life");

            Assert.That(sword.FitsSlot(EquipmentSlot.MainWeapon), Is.True);
            Assert.That(sword.FitsSlot(EquipmentSlot.SecondaryWeapon), Is.True);
            Assert.That(sword.FitsSlot(EquipmentSlot.Chest), Is.False);

            Assert.That(chest.FitsSlot(EquipmentSlot.Chest), Is.True);
            Assert.That(chest.FitsSlot(EquipmentSlot.Head), Is.False);
            Assert.That(chest.FitsSlot(EquipmentSlot.MainWeapon), Is.False);

            Assert.That(ring.FitsSlot(EquipmentSlot.Accessory1), Is.True);
            Assert.That(ring.FitsSlot(EquipmentSlot.Accessory2), Is.True);
            Assert.That(ring.FitsSlot(EquipmentSlot.Head), Is.False);
        }

        [Test]
        public void ALoadoutRemembersEverySlotSeparately()
        {
            var loadout = new EquipmentLoadout();
            foreach (var slot in EquipmentSlots.All)
            {
                loadout.Set(slot, $"item_{slot}");
            }

            foreach (var slot in EquipmentSlots.All)
            {
                Assert.That(loadout.Get(slot), Is.EqualTo($"item_{slot}"));
                Assert.That(loadout.IsEmpty(slot), Is.False);
            }

            Assert.That(loadout.Contains("item_Chest"), Is.True);
            Assert.That(loadout.Contains("nothing"), Is.False);
        }

        [Test]
        public void ACopiedLoadoutIsIndependentOfTheOriginal()
        {
            var loadout = new EquipmentLoadout();
            loadout.Set(EquipmentSlot.MainWeapon, "old_sword");

            var copy = loadout.Clone();
            Assert.That(copy.SameAs(loadout), Is.True);

            copy.Set(EquipmentSlot.MainWeapon, "iron_greatsword");
            Assert.That(loadout.Get(EquipmentSlot.MainWeapon), Is.EqualTo("old_sword"),
                "The screen compares against the loadout it opened with; a shared object would "
                + "never look changed.");
            Assert.That(copy.SameAs(loadout), Is.False);
        }

        [Test]
        public void AnEmptySecondHandCannotBeTheActiveOne()
        {
            var loadout = new EquipmentLoadout { isSecondaryActive = true };
            loadout.Set(EquipmentSlot.MainWeapon, "old_sword");

            loadout.Normalize();

            Assert.That(loadout.isSecondaryActive, Is.False,
                "Otherwise a load would arrive holding nothing.");
        }

        // ---------- what a swing is worth ----------

        [Test]
        public void WeaponBaseDamageTimesTheAttackShareIsTheDamage()
        {
            var sword = Weapon("old_sword", 20f);
            var attack = Attack(damageMultiplier: 1.25f, staminaCost: 15f, stagger: 22f);

            var hit = attack.BuildWeaponDamage(sword, sword.BaseDamage, null, null, 1);

            Assert.That(hit.Amount, Is.EqualTo(25f).Within(0.001f),
                "The third sword swing has always dealt 25.");
            Assert.That(attack.StaminaCostFor(sword), Is.EqualTo(15f));
            Assert.That(hit.StaggerDamage, Is.EqualTo(22f).Within(0.001f));
        }

        [Test]
        public void TheSameAttackOnAHeavierWeaponHitsHarderAndCostsMore()
        {
            var attack = Attack(damageMultiplier: 1.25f, staminaCost: 15f, stagger: 22f,
                knockback: 7f);
            var sword = Weapon("old_sword", 20f);
            var greatsword = Weapon("iron_greatsword", 34f, staminaMultiplier: 1.6f,
                staggerMultiplier: 1.7f, knockbackMultiplier: 1.5f);

            var light = attack.BuildWeaponDamage(sword, sword.BaseDamage, null, null, 1);
            var heavy = attack.BuildWeaponDamage(greatsword, greatsword.BaseDamage, null, null, 1);

            Assert.That(heavy.Amount, Is.GreaterThan(light.Amount));
            Assert.That(heavy.StaggerDamage, Is.GreaterThan(light.StaggerDamage));
            Assert.That(heavy.Knockback, Is.GreaterThan(light.Knockback));
            Assert.That(attack.StaminaCostFor(greatsword),
                Is.GreaterThan(attack.StaminaCostFor(sword)));
        }

        [Test]
        public void TheFlatDamageFieldIsIgnoredWhenAWeaponIsSwinging()
        {
            // The flat value stays for enemies; a weapon attack must not read it as well, or the
            // same swing would have two sources of truth.
            var attack = Attack(damageMultiplier: 1f, flatDamage: 999f);
            var sword = Weapon("old_sword", 20f);

            Assert.That(attack.BuildWeaponDamage(sword, sword.BaseDamage, null, null, 1).Amount,
                Is.EqualTo(20f).Within(0.001f));
            Assert.That(attack.BuildDamage(null, null, 1).Amount, Is.EqualTo(999f),
                "An enemy attack still uses its own flat number.");
        }

        [Test]
        public void AttackPowerFromEquipmentAddsToTheWeaponsBase()
        {
            var sword = Weapon("old_sword", 20f);
            var attack = Attack(damageMultiplier: 1f);

            // The flat bonus is folded into the scaled base by the calculator, which is the only
            // thing that decides what a weapon is worth in a given pair of hands.
            var breakdown = WeaponDamageCalculator.Evaluate(sword, null, 0, 0, 5f);

            Assert.That(breakdown.ScaledBase, Is.EqualTo(25f).Within(0.001f));
            Assert.That(attack.BuildWeaponDamage(sword, breakdown.ScaledBase, null, null, 1).Amount,
                Is.EqualTo(25f).Within(0.001f));
        }

        // ---------- the save file ----------

        [Test]
        public void ANewGameStartsOwningAndHoldingTheStarterSword()
        {
            var data = GameProgressData.CreateNew();

            Assert.That(data.inventory.HasItem(ItemIds.StarterWeapon), Is.True);
            Assert.That(data.equipment.Get(EquipmentSlot.MainWeapon),
                Is.EqualTo(ItemIds.StarterWeapon));
            Assert.That(data.equipment.IsEmpty(EquipmentSlot.SecondaryWeapon), Is.True);
            Assert.That(data.version, Is.EqualTo(GameProgressData.CurrentVersion));
        }

        [Test]
        public void AWholeLoadoutSurvivesARoundTrip()
        {
            var data = GameProgressData.CreateNew();
            data.inventory.Add("iron_greatsword", 1, true);
            data.inventory.Add("guard_chestplate", 1, true);
            data.inventory.Add("ring_of_life", 1, true);
            data.equipment.Set(EquipmentSlot.SecondaryWeapon, "iron_greatsword");
            data.equipment.Set(EquipmentSlot.Chest, "guard_chestplate");
            data.equipment.Set(EquipmentSlot.Accessory1, "ring_of_life");
            data.equipment.isSecondaryActive = true;

            Assert.That(SaveSystem.Write(data), Is.True);
            var loaded = SaveSystem.Read();

            Assert.That(loaded.equipment.Get(EquipmentSlot.MainWeapon),
                Is.EqualTo(ItemIds.StarterWeapon));
            Assert.That(loaded.equipment.Get(EquipmentSlot.SecondaryWeapon),
                Is.EqualTo("iron_greatsword"));
            Assert.That(loaded.equipment.Get(EquipmentSlot.Chest), Is.EqualTo("guard_chestplate"));
            Assert.That(loaded.equipment.Get(EquipmentSlot.Accessory1), Is.EqualTo("ring_of_life"));
            Assert.That(loaded.equipment.isSecondaryActive, Is.True);
        }

        [Test]
        public void AFileFromBeforeEquipmentComesBackArmed()
        {
            // A version three file: an inventory with the sword in it, but no loadout at all.
            File.WriteAllText(SaveSystem.FilePath,
                "{\"version\":3,\"checkpointId\":\"tomb-altar-01\","
                + "\"inventory\":{\"_stacks\":[{\"itemId\":\"old_sword\",\"amount\":1},"
                + "{\"itemId\":\"iron_scrap\",\"amount\":4}]}}");

            var loaded = SaveSystem.Read();

            Assert.That(loaded.version, Is.EqualTo(GameProgressData.CurrentVersion));
            Assert.That(loaded.equipment.Get(EquipmentSlot.MainWeapon), Is.EqualTo("old_sword"),
                "The sword the player already owned goes into the hand that was empty.");
            Assert.That(loaded.inventory.GetAmount("old_sword"), Is.EqualTo(1),
                "It is equipped, not duplicated.");
            Assert.That(loaded.inventory.GetAmount("iron_scrap"), Is.EqualTo(4));
            Assert.That(loaded.checkpointId, Is.EqualTo("tomb-altar-01"));
        }

        [Test]
        public void AFileFromBeforeItemsExistedGetsAStarterWeapon()
        {
            // A version two file: no inventory, no loadout, but real progress worth keeping.
            File.WriteAllText(SaveSystem.FilePath,
                "{\"version\":2,\"checkpointId\":\"tomb-altar-02\","
                + "\"defeatedBosses\":[\"guardian-of-the-tomb\"]}");

            var loaded = SaveSystem.Read();

            Assert.That(loaded.inventory.HasItem(ItemIds.StarterWeapon), Is.True);
            Assert.That(loaded.equipment.Get(EquipmentSlot.MainWeapon),
                Is.EqualTo(ItemIds.StarterWeapon));
            Assert.That(loaded.IsBossDefeated("guardian-of-the-tomb"), Is.True,
                "Adding equipment must not cost an older save its progress.");
        }

        [Test]
        public void LoadingTwiceNeverProducesTwoStarterSwords()
        {
            File.WriteAllText(SaveSystem.FilePath, "{\"version\":2}");

            var first = SaveSystem.Read();
            SaveSystem.Write(first);
            var second = SaveSystem.Read();
            second.Migrate();

            Assert.That(second.inventory.GetAmount(ItemIds.StarterWeapon), Is.EqualTo(1));
        }

        [Test]
        public void AnAlreadyArmedFileIsLeftAlone()
        {
            var data = GameProgressData.CreateNew();
            data.inventory.Add("iron_greatsword", 1, true);
            data.equipment.Set(EquipmentSlot.MainWeapon, "iron_greatsword");

            data.Migrate();

            Assert.That(data.equipment.Get(EquipmentSlot.MainWeapon),
                Is.EqualTo("iron_greatsword"), "Migration never overrides a real choice.");
        }

        // ---------- helpers ----------

        private static WeaponDefinition Weapon(string id, float baseDamage,
            float staminaMultiplier = 1f, float staggerMultiplier = 1f,
            float knockbackMultiplier = 1f)
        {
            var weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
            JsonUtility.FromJsonOverwrite(
                $"{{\"_itemId\":\"{id}\",\"_baseDamage\":{Num(baseDamage)},"
                + $"\"_staminaMultiplier\":{Num(staminaMultiplier)},"
                + $"\"_staggerMultiplier\":{Num(staggerMultiplier)},"
                + $"\"_knockbackMultiplier\":{Num(knockbackMultiplier)}}}", weapon);
            return weapon;
        }

        private static ArmorDefinition Armor(string id, EquipmentSlot slot, float defense)
        {
            var armor = ScriptableObject.CreateInstance<ArmorDefinition>();
            JsonUtility.FromJsonOverwrite(
                $"{{\"_itemId\":\"{id}\",\"_slot\":{(int)slot},\"_defense\":{Num(defense)}}}",
                armor);
            return armor;
        }

        private static AccessoryDefinition Accessory(string id)
        {
            var accessory = ScriptableObject.CreateInstance<AccessoryDefinition>();
            JsonUtility.FromJsonOverwrite($"{{\"_itemId\":\"{id}\"}}", accessory);
            return accessory;
        }

        private static AttackDefinition Attack(float damageMultiplier, float staminaCost = 10f,
            float stagger = 10f, float knockback = 3f, float flatDamage = 0f)
        {
            var attack = new AttackDefinition();
            JsonUtility.FromJsonOverwrite(
                $"{{\"_damage\":{Num(flatDamage)},\"_damageMultiplier\":{Num(damageMultiplier)},"
                + $"\"_staminaCost\":{Num(staminaCost)},\"_staggerDamage\":{Num(stagger)},"
                + $"\"_knockback\":{Num(knockback)}}}", attack);
            return attack;
        }

        /// <summary>Floats for JSON always use a dot, whatever the machine's locale says.</summary>
        private static string Num(float value)
        {
            return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
