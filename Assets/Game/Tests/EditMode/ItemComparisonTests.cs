using System.Globalization;
using NUnit.Framework;
using UnityEngine;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The inventory's comparison panel: every row says whether its own number went the player's
    /// way, and weapon damage is the number the character would really deal.
    /// </summary>
    public sealed class ItemComparisonTests
    {
        [Test]
        public void AHeavierWeaponHitsHarderButCostsMoreStaminaAndIsSlower()
        {
            var sword = Weapon("sword", 20f, speed: 1f, stamina: 1f, stagger: 1f, strength: 0.5f);
            var greatsword = Weapon("greatsword", 34f, speed: 0.78f, stamina: 1.6f, stagger: 1.7f,
                strength: 0.9f);
            var rows = ItemComparison.Rows(greatsword, sword, new ItemComparison.Wielder(10, 10, null));

            Assert.That(Row(rows, "Шкода").Trend, Is.EqualTo(ComparisonTrend.Better));
            Assert.That(Row(rows, "Шкода").Delta, Is.EqualTo("+14"));
            Assert.That(Row(rows, "Шкода рівновазі").Trend, Is.EqualTo(ComparisonTrend.Better));
            Assert.That(Row(rows, "Витривалість за удар").Trend, Is.EqualTo(ComparisonTrend.Worse),
                "More stamina per swing is worse even though the number grew.");
            Assert.That(Row(rows, "Швидкість атаки").Trend, Is.EqualTo(ComparisonTrend.Worse));
            Assert.That(Row(rows, "Масштаб. Сила").From, Is.EqualTo("C"));
            Assert.That(Row(rows, "Масштаб. Сила").To, Is.EqualTo("A"));
            Assert.That(Row(rows, "Масштаб. Сила").Trend, Is.EqualTo(ComparisonTrend.Better));
        }

        [Test]
        public void UnmetRequirementsLowerTheDamageThePanelShows()
        {
            var curve = ScriptableObject.CreateInstance<ProgressionCurve>();
            var heavy = Weapon("heavy", 40f, strength: 0f, requirement: 16);
            var weak = new ItemComparison.Wielder(12, 10, curve);
            var strong = new ItemComparison.Wielder(16, 10, curve);

            Assert.That(ItemComparison.Damage(heavy, weak), Is.LessThan(ItemComparison.Damage(heavy, strong)),
                "The panel shows the penalised number, the one combat would use.");
        }

        [Test]
        public void TemperingAndTrinketAttackAreInTheDamageThePanelShows()
        {
            var sword = Weapon("sword", 20f, strength: 0f);
            var bare = new ItemComparison.Wielder(10, 10, null);
            var tempered = new ItemComparison.Wielder(10, 10, null,
                weapon => 4f + Tempering.Bonus(weapon.BaseDamage, 1));

            Assert.That(ItemComparison.Damage(sword, tempered) - ItemComparison.Damage(sword, bare),
                Is.EqualTo(6f).Within(0.001f), "Trinket +4 and one tempering (+10% of 20) both count.");
        }

        [Test]
        public void ArmourComparesDefenceUpAndWeightDown()
        {
            var hood = Armor("hood", EquipmentSlot.Head, defense: 3f, weight: 1f);
            var helm = Armor("helm", EquipmentSlot.Head, defense: 8f, weight: 4f);
            var rows = ItemComparison.Rows(helm, hood, new ItemComparison.Wielder(10, 10, null));

            Assert.That(Row(rows, "Захист").Trend, Is.EqualTo(ComparisonTrend.Better));
            Assert.That(Row(rows, "Вага").Trend, Is.EqualTo(ComparisonTrend.Worse));
            Assert.That(Row(rows, "Маг. захист").Trend, Is.EqualTo(ComparisonTrend.Same));
        }

        [Test]
        public void WithNothingWornTheRowsAreThePlainValues()
        {
            var hood = Armor("hood", EquipmentSlot.Head, defense: 3f, weight: 1f);
            var rows = ItemComparison.Rows(hood, null, new ItemComparison.Wielder(10, 10, null));
            Assert.That(rows, Is.Not.Empty);
            Assert.That(rows.TrueForAll(row => !row.IsComparison), Is.True);

            var sameItem = ItemComparison.Rows(hood, hood, new ItemComparison.Wielder(10, 10, null));
            Assert.That(sameItem.TrueForAll(row => !row.IsComparison), Is.True,
                "The worn item is not compared with itself.");
        }

        [Test]
        public void TheChangeIsBetweenTheNumbersAsShown()
        {
            var a = Weapon("a", 22.6f);
            var b = Weapon("b", 23.4f);
            var row = ItemComparison.Rows(b, a, new ItemComparison.Wielder(0, 0, null))[0];
            Assert.That(row.From, Is.EqualTo(row.To), "Both round to 23.");
            Assert.That(row.Trend, Is.EqualTo(ComparisonTrend.Same), "23 → 23 is no change, not +1.");
        }

        [Test]
        public void ScalingLettersFollowTheCoefficient()
        {
            Assert.That(ItemComparison.ScalingGrade(1f), Is.EqualTo("A"));
            Assert.That(ItemComparison.ScalingGrade(0.75f), Is.EqualTo("B"));
            Assert.That(ItemComparison.ScalingGrade(0.5f), Is.EqualTo("C"));
            Assert.That(ItemComparison.ScalingGrade(0.25f), Is.EqualTo("D"));
            Assert.That(ItemComparison.ScalingGrade(0.1f), Is.EqualTo("E"));
            Assert.That(ItemComparison.ScalingGrade(0f), Is.EqualTo("—"));
        }

        [Test]
        public void EveryAuthoredItemGetsASilhouetteThatFitsIt()
        {
            var items = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemDatabase>(
                "Assets/Game/Data/ItemDatabase.asset");
            Assume.That(items, Is.Not.Null);
            Assert.That(ItemIcon.KindFor(items.GetItem("old_sword")), Is.EqualTo(ItemIconKind.Sword));
            Assert.That(ItemIcon.KindFor(items.GetItem("iron_greatsword")), Is.EqualTo(ItemIconKind.Greatsword));
            Assert.That(ItemIcon.KindFor(items.GetItem("traveler_hood")), Is.EqualTo(ItemIconKind.Head));
            Assert.That(ItemIcon.KindFor(items.GetItem("ring_of_life")), Is.EqualTo(ItemIconKind.Ring));
            Assert.That(ItemIcon.KindFor(items.GetItem("venom_charm")), Is.EqualTo(ItemIconKind.Amulet));
            Assert.That(ItemIcon.KindFor(items.GetItem("healing_herb")), Is.EqualTo(ItemIconKind.Herb));
        }

        private static ComparisonRow Row(System.Collections.Generic.List<ComparisonRow> rows, string label)
        {
            var row = rows.Find(candidate => candidate.Label == label);
            Assert.That(row.Label, Is.EqualTo(label), $"No row '{label}'.");
            return row;
        }

        private static WeaponDefinition Weapon(string id, float damage, float speed = 1f,
            float stamina = 1f, float stagger = 1f, float strength = 0f, int requirement = 0)
        {
            var weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
            JsonUtility.FromJsonOverwrite(
                $"{{\"_itemId\":\"{id}\",\"_baseDamage\":{Num(damage)},\"_attackSpeed\":{Num(speed)},"
                + $"\"_staminaMultiplier\":{Num(stamina)},\"_staggerMultiplier\":{Num(stagger)},"
                + $"\"_strengthScaling\":{Num(strength)},\"_dexterityScaling\":0,"
                + $"\"_strengthRequirement\":{requirement},\"_dexterityRequirement\":0}}", weapon);
            return weapon;
        }

        private static ArmorDefinition Armor(string id, EquipmentSlot slot, float defense, float weight)
        {
            var armor = ScriptableObject.CreateInstance<ArmorDefinition>();
            JsonUtility.FromJsonOverwrite(
                $"{{\"_itemId\":\"{id}\",\"_slot\":{(int)slot},\"_defense\":{Num(defense)},"
                + $"\"_weight\":{Num(weight)}}}", armor);
            return armor;
        }

        private static string Num(float value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
