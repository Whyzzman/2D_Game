using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// Levelling in the running level: earning experience, spending points, and what that does to
    /// the character. The recurring question is the same one equipment asks — does a bonus arrive
    /// exactly once, and does it survive a restart.
    /// </summary>
    public sealed class ProgressionTests
    {
        private PlayerCharacter _player;
        private PlayerEquipment _equipment;
        private PlayerStats _stats;
        private PlayerCombat _combat;
        private GameProgress _progress;
        private PlayerProgressionService _progression;
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

        // ---------- starting out ----------

        [UnityTest]
        public IEnumerator ANewCharacterStartsAtLevelOneWithTheCurvesAttributes()
        {
            Assert.That(_progression, Is.Not.Null);
            Assert.That(_progression.Level, Is.EqualTo(1));
            Assert.That(_progression.Experience, Is.Zero);
            Assert.That(_progression.StatPoints, Is.Zero);

            var curve = _progression.Curve;
            foreach (var stat in CharacterStats.Allocatable)
            {
                Assert.That(_stats.GetFinalStat(stat), Is.EqualTo(curve.StartingStat(stat)));
                Assert.That(_progression.GetAllocated(stat), Is.Zero);
            }

            Assert.That(_player.Health.Max, Is.EqualTo(curve.BaseHealth).Within(0.01f),
                "The bars are the curve's answer for the starting attributes.");
            Assert.That(_player.Stamina.Max, Is.EqualTo(curve.BaseStamina).Within(0.01f));
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        // ---------- experience ----------

        [UnityTest]
        public IEnumerator ExperienceAccumulatesAndBuysALevelWithAPointToSpend()
        {
            var required = _progression.ExperienceForNextLevel;
            var gained = 0;
            _progression.ExperienceGained += (amount, _) => gained += amount;

            Assert.That(_progression.AddExperience(required - 1), Is.Zero);
            Assert.That(_progression.Level, Is.EqualTo(1));
            Assert.That(_progression.Experience, Is.EqualTo(required - 1));

            Assert.That(_progression.AddExperience(1), Is.EqualTo(1));
            Assert.That(_progression.Level, Is.EqualTo(2));
            Assert.That(_progression.Experience, Is.Zero);
            Assert.That(_progression.StatPoints, Is.EqualTo(1));
            Assert.That(gained, Is.EqualTo(required));
            yield return null;
        }

        [UnityTest]
        public IEnumerator OneLargeRewardCanBuySeveralLevelsAndKeepsTheRemainder()
        {
            var levelChanges = 0;
            var from = 0;
            var to = 0;
            _progression.LevelChanged += (previous, current) =>
            {
                levelChanges++;
                from = previous;
                to = current;
            };

            var levels = _progression.AddExperience(5000);

            Assert.That(levels, Is.GreaterThan(2), "Five thousand is worth several levels.");
            Assert.That(_progression.Level, Is.EqualTo(1 + levels));
            Assert.That(_progression.StatPoints, Is.EqualTo(levels),
                "One point per level, however many arrived at once.");
            Assert.That(_progression.Experience, Is.GreaterThanOrEqualTo(0));
            Assert.That(_progression.Experience,
                Is.LessThan(_progression.ExperienceForNextLevel),
                "The overflow carries into the next level rather than vanishing.");
            Assert.That(levelChanges, Is.EqualTo(1),
                "One event for the whole gain, not one popup per level.");
            Assert.That(from, Is.EqualTo(1));
            Assert.That(to, Is.EqualTo(_progression.Level));
            yield return null;
        }

        [UnityTest]
        public IEnumerator NegativeOrEmptyExperienceIsRefusedOutright()
        {
            Assert.That(_progression.AddExperience(-500), Is.Zero);
            Assert.That(_progression.AddExperience(0), Is.Zero);

            Assert.That(_progression.Experience, Is.Zero);
            Assert.That(_progression.Level, Is.EqualTo(1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheLevelCapHoldsAndNothingOverflows()
        {
            var curve = _progression.Curve;
            _progression.AddExperience(int.MaxValue / 4);
            for (var i = 0; i < 60 && !_progression.IsMaxLevel; i++)
            {
                _progression.AddExperience(int.MaxValue / 4);
            }

            Assert.That(_progression.Level, Is.EqualTo(curve.MaxLevel));
            Assert.That(_progression.IsMaxLevel, Is.True);

            var pointsAtCap = _progression.StatPoints;
            Assert.That(_progression.AddExperience(int.MaxValue / 4), Is.Zero);
            Assert.That(_progression.Level, Is.EqualTo(curve.MaxLevel));
            Assert.That(_progression.StatPoints, Is.EqualTo(pointsAtCap));
            Assert.That(_progression.Experience, Is.GreaterThanOrEqualTo(0),
                "No wrap-around, whatever is thrown at it.");
            yield return null;
        }

        // ---------- spending points ----------

        [UnityTest]
        public IEnumerator SpendingAPointCostsExactlyOneAndOnlyWhenThereIsOne()
        {
            Assert.That(_progression.CanAllocate(CharacterStat.Strength), Is.False);
            Assert.That(_progression.AllocatePoint(CharacterStat.Strength), Is.False,
                "No point, no attribute.");
            var before = _stats.GetFinalStat(CharacterStat.Strength);

            _progression.GrantStatPoints(2);
            Assert.That(_progression.AllocatePoint(CharacterStat.Strength), Is.True);

            Assert.That(_progression.StatPoints, Is.EqualTo(1));
            Assert.That(_stats.GetFinalStat(CharacterStat.Strength), Is.EqualTo(before + 1));
            Assert.That(_progression.GetAllocated(CharacterStat.Strength), Is.EqualTo(1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator AnAttributeStopsAtItsCapWithoutEatingThePoint()
        {
            var curve = _progression.Curve;
            _progression.GrantStatPoints(5);
            _progression.SetStat(CharacterStat.Strength, curve.StatCap);
            yield return null;

            Assert.That(_progression.CanAllocate(CharacterStat.Strength), Is.False);
            Assert.That(_progression.AllocatePoint(CharacterStat.Strength), Is.False);
            Assert.That(_progression.StatPoints, Is.EqualTo(5),
                "A refused allocation never costs a point.");
        }

        [UnityTest]
        public IEnumerator VitalityRaisesMaximumHealthWithoutHealingTheWound()
        {
            _progression.GrantStatPoints(5);
            _player.Health.SetCurrent(40f);
            var wounded = _player.Health.Current;
            var before = _player.Health.Max;

            _progression.AllocatePoint(CharacterStat.Vitality);
            yield return new WaitForFixedUpdate();

            Assert.That(_player.Health.Max, Is.GreaterThan(before));
            Assert.That(_player.Health.Current, Is.EqualTo(wounded).Within(0.001f),
                "Opening the menu is not a heal.");
        }

        [UnityTest]
        public IEnumerator EnduranceRaisesStaminaAndCarryingCapacity()
        {
            _progression.GrantStatPoints(5);
            var stamina = _player.Stamina.Max;
            var load = _stats.MaxEquipLoad;

            _progression.AllocatePoint(CharacterStat.Endurance);
            yield return new WaitForFixedUpdate();

            Assert.That(_player.Stamina.Max, Is.GreaterThan(stamina));
            Assert.That(_stats.MaxEquipLoad, Is.GreaterThan(load));
        }

        [UnityTest]
        public IEnumerator SpendingPointsOverAndOverNeverStacksTheSameBonusTwice()
        {
            _progression.GrantStatPoints(3);
            _progression.AllocatePoint(CharacterStat.Vitality);
            yield return new WaitForFixedUpdate();
            var expected = _player.Health.Max;

            for (var i = 0; i < 20; i++)
            {
                _stats.Recalculate();
            }

            yield return new WaitForFixedUpdate();
            Assert.That(_player.Health.Max, Is.EqualTo(expected).Within(0.001f),
                "Derived values are worked out again, never added to.");
        }

        // ---------- scaling in the running game ----------

        [UnityTest]
        public IEnumerator StrengthMakesTheWeaponInHandHitHarder()
        {
            var before = _stats.ActiveWeaponAttack;
            _progression.GrantStatPoints(10);
            for (var i = 0; i < 10; i++)
            {
                _progression.AllocatePoint(CharacterStat.Strength);
            }

            yield return new WaitForFixedUpdate();

            Assert.That(_stats.ActiveWeaponAttack, Is.GreaterThan(before));
            Assert.That(_stats.WeaponDamage.StrengthBonus, Is.GreaterThan(0f));
        }

        [UnityTest]
        public IEnumerator TheDamageAZombieTakesFollowsTheAttributes()
        {
            var zombie = Zombie();
            var weapon = _combat.Weapon;
            var light = weapon.LightAttack(0);

            var weak = WeaponDamageCalculator.AttackDamage(_stats.WeaponDamage, light);
            _progression.GrantStatPoints(10);
            for (var i = 0; i < 10; i++)
            {
                _progression.AllocatePoint(CharacterStat.Strength);
            }

            yield return new WaitForFixedUpdate();
            var strong = WeaponDamageCalculator.AttackDamage(_stats.WeaponDamage, light);
            Assert.That(strong, Is.GreaterThan(weak));

            // And that is really what the enemy feels, not just what the sheet claims.
            zombie.Actor.ClearInvulnerability();
            var health = zombie.Actor.Health.Current;
            zombie.Actor.ApplyDamage(light.BuildWeaponDamage(weapon, _stats.ActiveWeaponAttack,
                _player.gameObject, null, 1));
            yield return new WaitForFixedUpdate();
            Assert.That(health - zombie.Actor.Health.Current, Is.EqualTo(strong).Within(0.01f));
        }

        // ---------- requirements ----------

        [UnityTest]
        public IEnumerator AGreatswordCannotBePickedUpUntilTheArmCanLiftIt()
        {
            _progress.AddItem("iron_greatsword");
            var greatsword = _progress.Items.GetItem("iron_greatsword").Weapon;
            Assert.That(greatsword.StrengthRequirement, Is.GreaterThan(
                _stats.GetFinalStat(CharacterStat.Strength)));

            Assert.That(_equipment.Equip("iron_greatsword", EquipmentSlot.MainWeapon),
                Is.EqualTo(EquipResult.RequirementsNotMet));
            Assert.That(_equipment.GetEquipped(EquipmentSlot.MainWeapon),
                Is.EqualTo(ItemIds.StarterWeapon));

            _progression.SetStat(CharacterStat.Strength, greatsword.StrengthRequirement);
            yield return new WaitForFixedUpdate();

            Assert.That(_equipment.Equip("iron_greatsword", EquipmentSlot.MainWeapon),
                Is.EqualTo(EquipResult.Equipped));
            Assert.That(_stats.MeetsWeaponRequirements, Is.True);
        }

        [UnityTest]
        public IEnumerator ARingCanMakeUpTheDifferenceAndTakingItOffOnlyCostsDamage()
        {
            var greatsword = _progress.Items.GetItem("iron_greatsword").Weapon;
            _progress.AddItem("iron_greatsword");
            _progress.AddItem("ring_of_might");
            // Two points short on their own; the ring is what closes the gap.
            _progression.SetStat(CharacterStat.Strength, greatsword.StrengthRequirement - 2);
            yield return new WaitForFixedUpdate();
            Assert.That(_progress.HasItem("ring_of_might"), Is.True);

            Assert.That(_equipment.Equip("iron_greatsword", EquipmentSlot.MainWeapon),
                Is.EqualTo(EquipResult.RequirementsNotMet), "Two points short on their own.");

            Assert.That(_equipment.Equip("ring_of_might", EquipmentSlot.Accessory1),
                Is.EqualTo(EquipResult.Equipped));
            yield return new WaitForFixedUpdate();
            Assert.That(_stats.GetEquipmentBonus(CharacterStat.Strength), Is.GreaterThan(0),
                "The ring counts towards the requirement like any other strength.");
            Assert.That(_equipment.Equip("iron_greatsword", EquipmentSlot.MainWeapon),
                Is.EqualTo(EquipResult.Equipped));
            var armed = _stats.ActiveWeaponAttack;

            _equipment.Unequip(EquipmentSlot.Accessory1);
            yield return new WaitForFixedUpdate();

            Assert.That(_equipment.GetEquipped(EquipmentSlot.MainWeapon),
                Is.EqualTo("iron_greatsword"),
                "A weapon is never torn out of the player's hands mid-fight.");
            Assert.That(_stats.MeetsWeaponRequirements, Is.False);
            Assert.That(_stats.ActiveWeaponAttack, Is.LessThan(armed),
                "It just hits for less until the strength comes back.");
            Assert.That(_stats.WeaponDamage.RequirementFactor,
                Is.EqualTo(_progression.Curve.RequirementPenalty).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator AWeaponFromAnOlderSaveIsHonouredEvenIfItIsNowTooHeavy()
        {
            var data = GameProgressData.CreateNew();
            data.inventory.Add("iron_greatsword", 1, true);
            data.equipment.Set(EquipmentSlot.MainWeapon, "iron_greatsword");
            SaveSystem.Write(data);

            yield return LoadLevel();

            Assert.That(_equipment.GetEquipped(EquipmentSlot.MainWeapon),
                Is.EqualTo("iron_greatsword"),
                "Adding a requirement must never make an existing save unplayable.");
            Assert.That(_stats.MeetsWeaponRequirements, Is.False);
            Assert.That(_stats.ActiveWeaponAttack, Is.GreaterThan(0f));
        }

        // ---------- enemies and the boss ----------

        [UnityTest]
        public IEnumerator KillingAZombiePaysOnceAndPaysAgainAfterItComesBack()
        {
            var zombie = Zombie();
            var reward = zombie.GetComponent<ExperienceReward>();
            Assert.That(reward, Is.Not.Null);
            Assert.That(reward.Reward, Is.GreaterThan(0));
            var before = TotalExperience();

            yield return Kill(zombie);
            var afterFirst = TotalExperience();
            Assert.That(afterFirst - before, Is.EqualTo(reward.Reward));

            // A second damage event on a corpse must not pay again.
            zombie.Actor.ClearInvulnerability();
            zombie.Actor.ApplyDamage(new DamageInfo(9999f, Vector2.right, isUnblockable: true));
            yield return new WaitForFixedUpdate();
            Assert.That(TotalExperience(), Is.EqualTo(afterFirst));

            zombie.ResetEnemy();
            yield return new WaitForFixedUpdate();
            Assert.That(reward.HasGranted, Is.False);
            yield return Kill(zombie);
            Assert.That(TotalExperience() - afterFirst, Is.EqualTo(reward.Reward),
                "Resting and killing again is allowed to pay.");
        }

        [UnityTest]
        public IEnumerator PuttingAnEnemyBackWithoutKillingItPaysNothing()
        {
            var zombie = Zombie();
            var before = TotalExperience();

            zombie.ResetEnemy();
            yield return new WaitForFixedUpdate();

            Assert.That(TotalExperience(), Is.EqualTo(before));
        }

        [UnityTest]
        public IEnumerator AShieldGuardIsWorthMoreThanAZombie()
        {
            var zombie = Zombie().GetComponent<ExperienceReward>();
            var guard = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None)
                .First(enemy => enemy.name.StartsWith("ShieldGuard"))
                .GetComponent<ExperienceReward>();

            Assert.That(guard.Reward, Is.GreaterThan(zombie.Reward));
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheGuardianPaysAFortuneOnceAndNeverAgain()
        {
            var encounter = TombScene.GuardianEncounter();
            _player.GetComponent<PlayerMotor2D>().ResetTo(new Vector2(42f, 1f));
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            var before = TotalExperience();
            var boss = encounter.Boss;
            boss.Actor.ClearInvulnerability();
            boss.Actor.ApplyDamage(new DamageInfo(float.MaxValue, Vector2.left,
                isUnblockable: true, attacker: _player.gameObject));
            yield return new WaitForFixedUpdate();

            var earned = TotalExperience() - before;
            Assert.That(earned, Is.GreaterThan(300),
                "A boss should be worth several ordinary enemies put together.");
            Assert.That(_progression.Level, Is.GreaterThan(1), "And it is worth a level or more.");
            yield return new WaitForSecondsRealtime(0.4f);

            var banked = TotalExperience();
            yield return LoadLevel();
            Assert.That(TotalExperience(), Is.EqualTo(banked),
                "A beaten Guardian never pays a second time.");
        }

        // ---------- persistence ----------

        [UnityTest]
        public IEnumerator LevelExperienceAndABuildAllComeBackAfterARestart()
        {
            _progression.AddExperience(700);
            _progression.GrantStatPoints(6);
            for (var i = 0; i < 4; i++)
            {
                _progression.AllocatePoint(CharacterStat.Strength);
            }

            _progression.AllocatePoint(CharacterStat.Vitality);
            var level = _progression.Level;
            var experience = _progression.Experience;
            var points = _progression.StatPoints;
            var strength = _stats.GetFinalStat(CharacterStat.Strength);
            var health = _player.Health.Max;
            var attack = _stats.ActiveWeaponAttack;
            _progress.SaveNow();
            yield return new WaitForSecondsRealtime(0.4f);

            yield return LoadLevel();

            Assert.That(_progression.Level, Is.EqualTo(level));
            Assert.That(_progression.Experience, Is.EqualTo(experience));
            Assert.That(_progression.StatPoints, Is.EqualTo(points),
                "Unspent points are progress too.");
            Assert.That(_stats.GetFinalStat(CharacterStat.Strength), Is.EqualTo(strength));
            Assert.That(_player.Health.Max, Is.EqualTo(health).Within(0.01f));
            Assert.That(_stats.ActiveWeaponAttack, Is.EqualTo(attack).Within(0.01f),
                "The same build hits for the same amount.");
        }

        [UnityTest]
        public IEnumerator DyingCostsNothingButTime()
        {
            _progression.AddExperience(400);
            _progression.GrantStatPoints(2);
            _progression.AllocatePoint(CharacterStat.Strength);
            var level = _progression.Level;
            var experience = _progression.Experience;
            var points = _progression.StatPoints;
            var strength = _stats.GetFinalStat(CharacterStat.Strength);

            Object.FindAnyObjectByType<PlayerRespawner>().Kill();
            yield return new WaitForSecondsRealtime(2.5f);

            Assert.That(_player.IsAlive, Is.True);
            Assert.That(_progression.Level, Is.EqualTo(level));
            Assert.That(_progression.Experience, Is.EqualTo(experience));
            Assert.That(_progression.StatPoints, Is.EqualTo(points));
            Assert.That(_stats.GetFinalStat(CharacterStat.Strength), Is.EqualTo(strength));
        }

        [UnityTest]
        public IEnumerator AFileFromBeforeAttributesGetsThemOnLoadWithoutAWindfall()
        {
            var data = GameProgressData.CreateNew();
            data.player.level = 4;
            data.player.stats = new CharacterStatBlock();
            SaveSystem.Write(data);

            yield return LoadLevel();

            Assert.That(_progression.Level, Is.EqualTo(4));
            Assert.That(_progression.StatPoints, Is.Zero, "No free points for old levels.");
            foreach (var stat in CharacterStats.Allocatable)
            {
                Assert.That(_stats.GetFinalStat(stat),
                    Is.EqualTo(_progression.Curve.StartingStat(stat)),
                    "Attributes arrive at the curve's starting values, not at zero.");
            }

            Assert.That(_player.Health.Max, Is.GreaterThan(0f));
            Assert.That(_combat.Weapon, Is.Not.Null);
        }

        // ---------- the character sheet ----------

        [UnityTest]
        public IEnumerator TheCharacterSheetReportsWhatTheGameActuallyUses()
        {
            _progression.GrantStatPoints(1);
            _screen.Open();

            var lines = _screen.DescribeCharacter();

            Assert.That(lines, Is.Not.Empty);
            Assert.That(lines.Any(line => line.Contains(_progression.Level.ToString())));
            Assert.That(lines.Any(line => line.Contains("Вільні очки")));
            foreach (var stat in CharacterStats.Allocatable)
            {
                Assert.That(lines.Any(line => line.StartsWith(CharacterStats.Display(stat))),
                    $"{stat} is missing from the sheet.");
            }

            Assert.That(_screen.PreviewAllocation(CharacterStat.Vitality), Is.Not.Empty,
                "The player can see what a point would buy before spending it.");
            Assert.That(_screen.CanAllocate(CharacterStat.Strength), Is.True);
            Assert.That(_screen.AllocatePoint(CharacterStat.Strength), Is.True);
            Assert.That(_screen.CanAllocate(CharacterStat.Strength), Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AWeaponPanelShowsTheDamageItWouldReallyDeal()
        {
            _progress.AddItem("iron_greatsword");
            _screen.Open();
            _screen.SelectTab(ItemCategory.Weapon);
            _screen.Select(ItemIds.StarterWeapon);

            var lines = _screen.DescribeSelected();

            Assert.That(lines.Any(line => line.Contains("Сила атаки")));
            var shown = lines.First(line => line.Contains("Сила атаки"));
            Assert.That(shown, Does.Contain($"{_stats.ActiveWeaponAttack:0.#}"),
                "The panel and the swing read from the same calculator.");

            _screen.Select("iron_greatsword");
            Assert.That(_screen.DescribeSelected().Any(line => line.Contains("Не вистачає")),
                "And it says plainly when the character cannot lift it.");
            yield return null;
        }

        // ---------- helpers ----------

        private IEnumerator LoadLevel()
        {
            yield return SceneManager.LoadSceneAsync("TombTrial");
            _player = Object.FindAnyObjectByType<PlayerCharacter>();
            _equipment = _player.GetComponent<PlayerEquipment>();
            _stats = _player.GetComponent<PlayerStats>();
            _combat = _player.GetComponent<PlayerCombat>();
            _progress = Object.FindAnyObjectByType<GameProgress>();
            _progression = Object.FindAnyObjectByType<PlayerProgressionService>();
            _screen = Object.FindAnyObjectByType<InventoryScreen>();
            yield return new WaitForFixedUpdate();
        }

        /// <summary>
        /// Experience earned altogether, levels included: comparing raw experience alone would
        /// miss everything that was spent on a level.
        /// </summary>
        private int TotalExperience()
        {
            var total = _progression.Experience;
            for (var level = 1; level < _progression.Level; level++)
            {
                total += _progression.Curve.ExperienceForNextLevel(level);
            }

            return total;
        }

        private EnemyController Zombie()
        {
            return Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None)
                .First(enemy => enemy.name.StartsWith("ZombieSoldier"));
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
