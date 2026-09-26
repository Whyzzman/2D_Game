using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The progression rules that are pure arithmetic: what a level costs, what attributes are
    /// worth, how a weapon scales off them, and what an older save becomes.
    /// </summary>
    public sealed class ProgressionDataTests
    {
        private ProgressionCurve _curve;

        [SetUp]
        public void SetUp()
        {
            _curve = Curve();
        }

        [TearDown]
        public void TearDown()
        {
            SaveSystem.Delete();
        }

        // ---------- the curve ----------

        [Test]
        public void EveryLevelCostsSomethingAndCostsMoreThanTheLastOne()
        {
            var previous = 0;
            for (var level = 1; level < _curve.MaxLevel; level++)
            {
                var required = _curve.ExperienceForNextLevel(level);
                Assert.That(required, Is.GreaterThan(0),
                    $"Level {level} must cost something, or it would be granted forever.");
                Assert.That(required, Is.GreaterThanOrEqualTo(previous));
                previous = required;
            }
        }

        [Test]
        public void TheCapCostsNothingBecauseThereIsNothingToBuy()
        {
            Assert.That(_curve.ExperienceForNextLevel(_curve.MaxLevel), Is.Zero);
            Assert.That(_curve.ExperienceForNextLevel(_curve.MaxLevel + 50), Is.Zero);
        }

        [Test]
        public void AttributesBuyHealthStaminaAndCarryingCapacity()
        {
            var start = _curve.StartingStats();

            Assert.That(_curve.HealthFor(start.vitality), Is.EqualTo(_curve.BaseHealth));
            Assert.That(_curve.HealthFor(start.vitality + 5),
                Is.GreaterThan(_curve.HealthFor(start.vitality)));
            Assert.That(_curve.StaminaFor(start.endurance + 5),
                Is.GreaterThan(_curve.StaminaFor(start.endurance)));
            Assert.That(_curve.EquipLoadFor(start.endurance + 5),
                Is.GreaterThan(_curve.EquipLoadFor(start.endurance)));
        }

        [Test]
        public void FivePointsOfVitalityAreClearlyWorthHaving()
        {
            var start = _curve.StartingStats().vitality;
            var gain = _curve.HealthFor(start + 5) - _curve.HealthFor(start);

            // The target is "noticeably tougher", not "a different game": a quarter to a half
            // again on a hundred-point bar.
            Assert.That(gain, Is.InRange(25f, 60f));
        }

        [Test]
        public void TheRateDropsAtTheBreakpointsRatherThanRunningAwayForever()
        {
            // The bands exist so a late build cannot buy the same power as an early one.
            var early = _curve.HealthFor(15) - _curve.HealthFor(10);
            var late = _curve.HealthFor(55) - _curve.HealthFor(50);

            Assert.That(late, Is.LessThan(early),
                "Diminishing returns are in the curve, not in a future to-do.");
            Assert.That(late, Is.GreaterThan(0f), "But a point is never worth nothing.");
        }

        [Test]
        public void HealthNeverFallsBelowTheBaseHoweverLowTheAttribute()
        {
            Assert.That(_curve.HealthFor(0), Is.EqualTo(_curve.BaseHealth));
            Assert.That(_curve.StaminaFor(-5), Is.EqualTo(_curve.BaseStamina));
        }

        // ---------- weapon scaling ----------

        [Test]
        public void AttributesRaiseAWeaponsAttackPower()
        {
            var sword = Weapon(20f, strengthScaling: 0.5f, dexterityScaling: 0.4f);

            var weak = WeaponDamageCalculator.Evaluate(sword, _curve, 5, 5);
            var strong = WeaponDamageCalculator.Evaluate(sword, _curve, 15, 5);

            Assert.That(strong.ScaledBase, Is.GreaterThan(weak.ScaledBase));
            Assert.That(weak.ScaledBase, Is.GreaterThanOrEqualTo(sword.BaseDamage),
                "Attributes only ever add.");
        }

        [Test]
        public void AGreatswordRewardsStrengthFarMoreThanASwordDoes()
        {
            var sword = Weapon(20f, strengthScaling: 0.5f, dexterityScaling: 0.4f);
            var greatsword = Weapon(34f, strengthScaling: 0.9f, dexterityScaling: 0.1f);

            var swordGain = Gain(sword, CharacterStat.Strength);
            var greatGain = Gain(greatsword, CharacterStat.Strength);

            Assert.That(greatGain, Is.GreaterThan(swordGain * 1.5f),
                "Which is what makes a strength build a build rather than a preference.");
        }

        [Test]
        public void ASwordCaresAboutDexterityAndAGreatswordBarelyDoes()
        {
            var sword = Weapon(20f, strengthScaling: 0.5f, dexterityScaling: 0.4f);
            var greatsword = Weapon(34f, strengthScaling: 0.9f, dexterityScaling: 0.1f);

            Assert.That(Gain(sword, CharacterStat.Dexterity), Is.GreaterThan(0f));
            Assert.That(Gain(sword, CharacterStat.Dexterity),
                Is.GreaterThan(Gain(greatsword, CharacterStat.Dexterity)));
        }

        [Test]
        public void TenPointsAreFeltWithoutBeingDecisive()
        {
            var greatsword = Weapon(34f, strengthScaling: 0.9f, dexterityScaling: 0.1f);
            var before = WeaponDamageCalculator.Evaluate(greatsword, _curve, 5, 5).ScaledBase;
            var after = WeaponDamageCalculator.Evaluate(greatsword, _curve, 15, 5).ScaledBase;

            var growth = after / before;
            Assert.That(growth, Is.InRange(1.1f, 1.6f),
                "Ten points into the favoured attribute is worth a fifth or so, not a doubling.");
        }

        [Test]
        public void AWeaponTooHeavyToWieldHitsForLess()
        {
            var greatsword = Weapon(34f, strengthScaling: 0.9f, dexterityScaling: 0.1f,
                strengthRequirement: 10);

            Assert.That(greatsword.MeetsRequirements(8, 5), Is.False);
            Assert.That(greatsword.MeetsRequirements(10, 5), Is.True);

            var proper = WeaponDamageCalculator.Evaluate(greatsword, _curve, 10, 5, 0f, true);
            var struggling = WeaponDamageCalculator.Evaluate(greatsword, _curve, 10, 5, 0f, false);

            Assert.That(struggling.ScaledBase,
                Is.EqualTo(proper.ScaledBase * _curve.RequirementPenalty).Within(0.01f));
            Assert.That(struggling.MeetsRequirements, Is.False);
        }

        [Test]
        public void TheAttackShareIsAppliedOnTopOfTheScaledBase()
        {
            var sword = Weapon(20f, strengthScaling: 0f, dexterityScaling: 0f);
            var breakdown = WeaponDamageCalculator.Evaluate(sword, _curve, 5, 5);
            var attack = Attack(1.25f);

            Assert.That(WeaponDamageCalculator.AttackDamage(breakdown, attack),
                Is.EqualTo(25f).Within(0.001f));
            Assert.That(WeaponDamageCalculator.AttackDamage(breakdown, null), Is.Zero);
        }

        [Test]
        public void AMissingWeaponIsWorthNothingRatherThanCrashing()
        {
            var breakdown = WeaponDamageCalculator.Evaluate(null, _curve, 20, 20);

            Assert.That(breakdown.ScaledBase, Is.Zero);
            Assert.That(breakdown.MeetsRequirements, Is.True);
        }

        // ---------- the save file ----------

        [Test]
        public void ANewCharacterStartsAtLevelOneWithNothingSpent()
        {
            var data = GameProgressData.CreateNew();

            Assert.That(data.player.level, Is.EqualTo(1));
            Assert.That(data.player.experience, Is.Zero);
            Assert.That(data.player.statPoints, Is.Zero);
            Assert.That(data.version, Is.EqualTo(GameProgressData.CurrentVersion));
        }

        [Test]
        public void LevelExperiencePointsAndAttributesAllSurviveARoundTrip()
        {
            var data = GameProgressData.CreateNew();
            data.player.level = 7;
            data.player.experience = 143;
            data.player.statPoints = 3;
            data.player.stats.strength = 14;
            data.player.stats.dexterity = 6;
            data.player.stats.vitality = 11;
            data.player.stats.endurance = 9;

            Assert.That(SaveSystem.Write(data), Is.True);
            var loaded = SaveSystem.Read();

            Assert.That(loaded.player.level, Is.EqualTo(7));
            Assert.That(loaded.player.experience, Is.EqualTo(143));
            Assert.That(loaded.player.statPoints, Is.EqualTo(3));
            Assert.That(loaded.player.stats.strength, Is.EqualTo(14));
            Assert.That(loaded.player.stats.dexterity, Is.EqualTo(6));
            Assert.That(loaded.player.stats.vitality, Is.EqualTo(11));
            Assert.That(loaded.player.stats.endurance, Is.EqualTo(9));
        }

        [Test]
        public void AFileFromBeforeAttributesLoadsWithoutInventingProgress()
        {
            // A version four file: equipment and a level, but no attributes and no points.
            File.WriteAllText(SaveSystem.FilePath,
                "{\"version\":4,\"checkpointId\":\"tomb-altar-01\","
                + "\"player\":{\"level\":3,\"experience\":40,\"currency\":120,"
                + "\"maxHealth\":100,\"maxStamina\":100},"
                + "\"inventory\":{\"_stacks\":[{\"itemId\":\"old_sword\",\"amount\":1}]},"
                + "\"equipment\":{\"mainWeapon\":\"old_sword\"}}");

            var loaded = SaveSystem.Read();

            Assert.That(loaded.version, Is.EqualTo(GameProgressData.CurrentVersion));
            Assert.That(loaded.player.level, Is.EqualTo(3), "The level that was there is kept.");
            Assert.That(loaded.player.experience, Is.EqualTo(40));
            Assert.That(loaded.player.statPoints, Is.Zero,
                "No windfall of points for the levels earned before attributes existed.");
            Assert.That(loaded.player.stats.Total, Is.Zero,
                "Attributes stay unset here; the curve fills them in once the game runs.");
            Assert.That(loaded.equipment.Get(EquipmentSlot.MainWeapon), Is.EqualTo("old_sword"),
                "And nothing older is lost.");
        }

        [Test]
        public void BrokenProgressionNumbersAreClampedRatherThanTrusted()
        {
            var data = GameProgressData.CreateNew();
            data.player.level = -4;
            data.player.experience = -900;
            data.player.statPoints = -10;
            data.player.stats.strength = -7;
            File.WriteAllText(SaveSystem.FilePath, JsonUtility.ToJson(data, true));

            var loaded = SaveSystem.Read();

            Assert.That(loaded.player.level, Is.EqualTo(1));
            Assert.That(loaded.player.experience, Is.Zero);
            Assert.That(loaded.player.statPoints, Is.Zero);
            Assert.That(loaded.player.stats.strength, Is.Zero);
        }

        [Test]
        public void AnAttributeBlockAnswersEveryAttributeItOwns()
        {
            var stats = new CharacterStatBlock();

            foreach (var stat in CharacterStats.Allocatable)
            {
                stats.Set(stat, 12);
                Assert.That(stats.Get(stat), Is.EqualTo(12));
                stats.Add(stat, 3);
                Assert.That(stats.Get(stat), Is.EqualTo(15));
            }

            var expected = 15 * CharacterStats.Allocatable.Length;
            Assert.That(stats.Total, Is.EqualTo(expected));
            Assert.That(stats.Clone().Total, Is.EqualTo(expected),
                "A copy carries every attribute, so a build cannot be lost by cloning it.");
            Assert.That(CharacterStats.Allocatable,
                Is.EquivalentTo(System.Enum.GetValues(typeof(CharacterStat))),
                "Every attribute that exists is now one a point can go into; nothing is "
                + "declared and then quietly ignored.");
        }

        // ---------- helpers ----------

        private float Gain(WeaponDefinition weapon, CharacterStat stat)
        {
            var before = WeaponDamageCalculator.Evaluate(weapon, _curve, 5, 5).ScaledBase;
            var after = stat == CharacterStat.Strength
                ? WeaponDamageCalculator.Evaluate(weapon, _curve, 15, 5).ScaledBase
                : WeaponDamageCalculator.Evaluate(weapon, _curve, 5, 15).ScaledBase;
            return after - before;
        }

        private static ProgressionCurve Curve()
        {
            var curve = UnityEditor.AssetDatabase.LoadAssetAtPath<ProgressionCurve>(
                "Assets/Game/Data/ProgressionCurve.asset");
            Assert.That(curve, Is.Not.Null, "The content build writes the progression curve.");
            return curve;
        }

        private static WeaponDefinition Weapon(float baseDamage, float strengthScaling,
            float dexterityScaling, int strengthRequirement = 0)
        {
            var weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
            JsonUtility.FromJsonOverwrite(
                $"{{\"_itemId\":\"test\",\"_baseDamage\":{Num(baseDamage)},"
                + $"\"_strengthScaling\":{Num(strengthScaling)},"
                + $"\"_dexterityScaling\":{Num(dexterityScaling)},"
                + $"\"_strengthRequirement\":{strengthRequirement}}}", weapon);
            return weapon;
        }

        private static AttackDefinition Attack(float damageMultiplier)
        {
            var attack = new AttackDefinition();
            JsonUtility.FromJsonOverwrite(
                $"{{\"_damageMultiplier\":{Num(damageMultiplier)}}}", attack);
            return attack;
        }

        private static string Num(float value)
        {
            return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
