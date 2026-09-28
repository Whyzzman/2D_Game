using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// Statuses, Shadow and advanced traversal in the running level. The recurring questions are
    /// the same three: does buildup become an effect exactly once, does the effect end cleanly,
    /// and can any of this get the player somewhere the level did not intend.
    /// </summary>
    public sealed class StatusAndShadowTests
    {
        private PlayerCharacter _player;
        private PlayerMotor2D _motor;
        private PlayerStats _stats;
        private PlayerSkills _skills;
        private PlayerProgressionService _progression;
        private GameProgress _progress;
        private TemporaryBuffs _buffs;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            yield return LoadLevel();
        }

        [TearDown]
        public void TearDown()
        {
            SpellProjectile.ClearAll(Faction.Player);
            GamePause.Clear();
            SaveSystem.Delete();
            Time.timeScale = 1f;
        }

        // ---------- buildup ----------

        [UnityTest]
        public IEnumerator BuildupHasToReachTheThresholdBeforeAnythingHappens()
        {
            var target = Zombie();
            var statuses = Statuses(target);
            var rule = statuses.Rule(StatusType.Poison);
            var health = target.Actor.Health.Current;

            statuses.Add(StatusType.Poison, rule.threshold * 0.5f);
            yield return new WaitForFixedUpdate();

            Assert.That(statuses.Buildup(StatusType.Poison), Is.GreaterThan(0f));
            Assert.That(statuses.Remaining(StatusType.Poison), Is.Zero, "Not yet poisoned.");
            Assert.That(target.Actor.Health.Current, Is.EqualTo(health).Within(0.01f));

            statuses.Add(StatusType.Poison, rule.threshold * 0.6f);
            yield return new WaitForFixedUpdate();

            Assert.That(statuses.Remaining(StatusType.Poison), Is.GreaterThan(0f), "Now it is.");
            Assert.That(statuses.Buildup(StatusType.Poison), Is.Zero,
                "And the buildup starts again from nothing.");
        }

        [UnityTest]
        public IEnumerator BuildupThatFallsShortFadesAwayOnItsOwn()
        {
            var statuses = Statuses(Zombie());
            var rule = statuses.Rule(StatusType.Bleed);
            statuses.Add(StatusType.Bleed, rule.threshold * 0.6f);
            var peak = statuses.Buildup(StatusType.Bleed);

            yield return new WaitForSeconds(rule.decayDelay + 2f);

            Assert.That(statuses.Buildup(StatusType.Bleed), Is.LessThan(peak),
                "Half-finished work does not wait for you forever.");
        }

        [UnityTest]
        public IEnumerator ResistanceSlowsBuildupAndImmunityStopsItDead()
        {
            var statuses = Statuses(Zombie());
            var rule = statuses.Rule(StatusType.Burn);
            var plainResistance = rule.resistance;

            statuses.Add(StatusType.Burn, 30f);
            var unresisted = statuses.Buildup(StatusType.Burn);
            statuses.Clear();

            rule.resistance = 0.5f;
            statuses.Add(StatusType.Burn, 30f);
            Assert.That(statuses.Buildup(StatusType.Burn), Is.LessThan(unresisted));
            statuses.Clear();

            rule.immune = true;
            statuses.Add(StatusType.Burn, 500f);
            Assert.That(statuses.Buildup(StatusType.Burn), Is.Zero, "Immunity means nothing lands.");
            Assert.That(statuses.Remaining(StatusType.Burn), Is.Zero);

            rule.immune = false;
            rule.resistance = plainResistance;
            yield return null;
        }

        // ---------- each status ----------

        [UnityTest]
        public IEnumerator BleedBurstsOnceAndThenHasToBeBuiltUpAgain()
        {
            var target = Zombie();
            target.Actor.ClearInvulnerability();
            var statuses = Statuses(target);
            var rule = statuses.Rule(StatusType.Bleed);
            var procs = 0;
            statuses.Applied += type => { if (type == StatusType.Bleed) { procs++; } };
            var health = target.Actor.Health.Current;

            statuses.Add(StatusType.Bleed, rule.threshold);
            yield return new WaitForSeconds(1.5f);

            Assert.That(procs, Is.EqualTo(1), "One burst, not one per frame.");
            Assert.That(target.Actor.Health.Current, Is.LessThan(health));
            Assert.That(statuses.Remaining(StatusType.Bleed), Is.Zero,
                "Bleed is a burst, not something that lingers.");
            Assert.That(statuses.Buildup(StatusType.Bleed), Is.Zero);
        }

        [UnityTest]
        public IEnumerator PoisonTicksAwayForItsDurationAndThenStops()
        {
            var target = Zombie();
            target.Actor.ClearInvulnerability();
            var statuses = Statuses(target);
            var rule = statuses.Rule(StatusType.Poison);
            var ticks = 0;
            statuses.Ticked += type => { if (type == StatusType.Poison) { ticks++; } };
            var health = target.Actor.Health.Current;

            statuses.Add(StatusType.Poison, rule.threshold);
            yield return new WaitForSeconds(3f);

            Assert.That(ticks, Is.GreaterThan(1), "Attrition means more than one tick.");
            Assert.That(target.Actor.Health.Current, Is.LessThan(health));
            var afterThree = ticks;

            yield return new WaitForSeconds(rule.duration + 1f);
            var whenDone = ticks;
            yield return new WaitForSeconds(1.5f);

            Assert.That(ticks, Is.GreaterThan(afterThree));
            Assert.That(ticks, Is.EqualTo(whenDone), "And it really does end.");
            Assert.That(statuses.Remaining(StatusType.Poison), Is.Zero);
        }

        [UnityTest]
        public IEnumerator PoisonedAgainRefreshesRatherThanRunningTwice()
        {
            var target = Zombie();
            target.Actor.ClearInvulnerability();
            var statuses = Statuses(target);
            var rule = statuses.Rule(StatusType.Poison);

            statuses.Add(StatusType.Poison, rule.threshold);
            yield return new WaitForSeconds(1.2f);
            var ticksBefore = 0;
            statuses.Ticked += type => { if (type == StatusType.Poison) { ticksBefore++; } };

            statuses.Add(StatusType.Poison, rule.threshold);
            Assert.That(statuses.Remaining(StatusType.Poison),
                Is.EqualTo(rule.duration).Within(0.2f), "The clock is reset, not doubled.");

            yield return new WaitForSeconds(1.1f);
            Assert.That(ticksBefore, Is.LessThanOrEqualTo(2),
                "One poisoning ticks at one rate, however many times it was reapplied.");
        }

        [UnityTest]
        public IEnumerator BurnIsShorterAndFiercerThanPoison()
        {
            var target = Zombie();
            target.Actor.ClearInvulnerability();
            var statuses = Statuses(target);
            var burn = statuses.Rule(StatusType.Burn);
            var poison = statuses.Rule(StatusType.Poison);

            Assert.That(burn.duration, Is.LessThan(poison.duration));

            var health = target.Actor.Health.Current;
            statuses.Add(StatusType.Burn, burn.threshold);
            yield return new WaitForSeconds(burn.duration + 1f);

            Assert.That(target.Actor.Health.Current, Is.LessThan(health));
            Assert.That(statuses.Remaining(StatusType.Burn), Is.Zero);
        }

        [UnityTest]
        public IEnumerator FrostSlowsAnEnemyAndGivesTheSpeedBackExactly()
        {
            var target = Zombie();
            var statuses = Statuses(target);
            var rule = statuses.Rule(StatusType.Frost);

            Assert.That(statuses.MovementMultiplier, Is.EqualTo(1f).Within(0.001f));

            statuses.Add(StatusType.Frost, rule.threshold);
            yield return new WaitForFixedUpdate();
            Assert.That(statuses.MovementMultiplier, Is.LessThan(1f), "Chilled and slower.");

            yield return new WaitForSeconds(rule.duration + 0.5f);

            Assert.That(statuses.MovementMultiplier, Is.EqualTo(1f).Within(0.001f),
                "The speed comes back exactly, not approximately.");
        }

        [UnityTest]
        public IEnumerator FrostCannotBeReappliedTheInstantItEnds()
        {
            var target = Zombie();
            var statuses = Statuses(target);
            var rule = statuses.Rule(StatusType.Frost);

            statuses.Add(StatusType.Frost, rule.threshold);
            yield return new WaitForSeconds(rule.duration + 0.2f);

            statuses.Add(StatusType.Frost, rule.threshold * 3f);
            Assert.That(statuses.Remaining(StatusType.Frost), Is.Zero,
                "A gap after it ends is what stops a permanent slow.");

            yield return new WaitForSeconds(rule.reprocDelay + 0.3f);
            statuses.Add(StatusType.Frost, rule.threshold);
            Assert.That(statuses.Remaining(StatusType.Frost), Is.GreaterThan(0f),
                "But only a gap, not an immunity.");
        }

        [UnityTest]
        public IEnumerator TwoDifferentStatusesRunSideBySide()
        {
            var target = Zombie();
            target.Actor.ClearInvulnerability();
            var statuses = Statuses(target);

            statuses.Add(StatusType.Poison, statuses.Rule(StatusType.Poison).threshold);
            statuses.Add(StatusType.Burn, statuses.Rule(StatusType.Burn).threshold);
            yield return new WaitForFixedUpdate();

            Assert.That(statuses.Remaining(StatusType.Poison), Is.GreaterThan(0f));
            Assert.That(statuses.Remaining(StatusType.Burn), Is.GreaterThan(0f),
                "One status does not push another out.");
        }

        // ---------- cleanup ----------

        [UnityTest]
        public IEnumerator PuttingAnEnemyBackLeavesNoStatusOnIt()
        {
            var target = Zombie();
            var statuses = Statuses(target);
            statuses.Add(StatusType.Poison, statuses.Rule(StatusType.Poison).threshold);
            statuses.Add(StatusType.Bleed, 40f);
            yield return new WaitForFixedUpdate();
            Assert.That(statuses.Remaining(StatusType.Poison), Is.GreaterThan(0f));

            target.ResetEnemy();
            yield return new WaitForFixedUpdate();

            Assert.That(statuses.Remaining(StatusType.Poison), Is.Zero);
            Assert.That(statuses.Buildup(StatusType.Bleed), Is.Zero,
                "A restored enemy is restored, half-built poison included.");
        }

        [UnityTest]
        public IEnumerator DyingClearsThePlayersStatusesAndTheyDoNotComeBack()
        {
            var statuses = _player.GetComponent<StatusEffects>();
            Assert.That(statuses, Is.Not.Null, "The player can be poisoned too.");
            // Generously over the threshold: the character's own Will resists some of it, so
            // exactly the threshold would fall short — which is the point of that resistance.
            statuses.Add(StatusType.Poison, statuses.Rule(StatusType.Poison).threshold * 5f);
            yield return new WaitForFixedUpdate();
            Assert.That(statuses.Remaining(StatusType.Poison), Is.GreaterThan(0f));

            Object.FindAnyObjectByType<PlayerRespawner>().Kill();
            yield return new WaitForSecondsRealtime(2.5f);

            Assert.That(_player.IsAlive, Is.True);
            Assert.That(statuses.Remaining(StatusType.Poison), Is.Zero);
            Assert.That(statuses.Buildup(StatusType.Poison), Is.Zero);
        }

        [UnityTest]
        public IEnumerator AnEnemyKilledByPoisonStillPaysItsExperienceAndLootOnce()
        {
            var target = Zombie();
            target.Actor.ClearInvulnerability();
            var reward = target.GetComponent<ExperienceReward>();
            var loot = Object.FindAnyObjectByType<LootService>();
            var experienceBefore = _progression.Experience;
            var lootBefore = loot.TransientPickupCount;
            var statuses = Statuses(target);

            target.Actor.Health.SetCurrent(3f);
            statuses.Add(StatusType.Poison, statuses.Rule(StatusType.Poison).threshold,
                _player.gameObject);

            var started = Time.time;
            while (target.IsAlive && Time.time - started < 8f)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(target.IsAlive, Is.False, "Poison can finish an enemy off.");
            yield return new WaitForSeconds(0.5f);

            Assert.That(_progression.Experience, Is.Not.EqualTo(experienceBefore),
                "A kill is a kill, whoever struck the last blow.");
            Assert.That(loot.TransientPickupCount, Is.GreaterThan(lootBefore));
            Assert.That(reward.HasGranted, Is.True);
        }

        // ---------- Shadow ----------

        [UnityTest]
        public IEnumerator ShadowDashIsItsOwnAbilityAndHasToBeLearned()
        {
            Assert.That(_skills.HasSkill("shadow_dash"), Is.False);

            Learn("shadow_initiate");
            Learn("shadow_veiled_step");
            Learn("shadow_dash");
            yield return null;

            Assert.That(_skills.HasSkill("shadow_dash"), Is.True);
            Assert.That(_progress.HasAbility(AbilityIds.Dash), Is.True,
                "And the ordinary dash is untouched by it.");
        }

        [UnityTest]
        public IEnumerator WillMakesShadowHitHarder()
        {
            var before = _stats.ShadowPower;

            _progression.GrantStatPoints(10);
            for (var i = 0; i < 10; i++)
            {
                _progression.AllocatePoint(CharacterStat.Will);
            }

            yield return new WaitForFixedUpdate();

            Assert.That(_stats.ShadowPower, Is.GreaterThan(before));
        }

        [UnityTest]
        public IEnumerator WillMakesTheCharacterHarderToAfflict()
        {
            var before = _stats.StatusResistance(StatusType.Poison);

            _progression.GrantStatPoints(15);
            for (var i = 0; i < 15; i++)
            {
                _progression.AllocatePoint(CharacterStat.Will);
            }

            yield return new WaitForFixedUpdate();

            var after = _stats.StatusResistance(StatusType.Poison);
            Assert.That(after, Is.GreaterThan(before));
            Assert.That(after, Is.LessThan(1f),
                "Resolve makes poison harder to land, never impossible.");
        }

        // ---------- traversal ----------

        [UnityTest]
        public IEnumerator DashingInTheAirHasToBeEarned()
        {
            Assert.That(_progress.HasAbility(AbilityIds.AirDash), Is.False);
            _motor.ResetTo(new Vector2(-12f, 6f));
            yield return new WaitForFixedUpdate();
            Assert.That(_motor.IsGrounded, Is.False);

            Assert.That(_motor.IsDashReady, Is.False, "Not without the ability.");

            _progress.UnlockAbility(AbilityIds.AirDash);
            yield return new WaitForFixedUpdate();
            Assert.That(_motor.IsDashReady, Is.True);
        }

        [UnityTest]
        public IEnumerator AnEarnedAirDashIsStillOnlyOnePerJump()
        {
            _progress.UnlockAbility(AbilityIds.AirDash);
            _motor.ResetTo(new Vector2(-12f, 8f));
            yield return new WaitForFixedUpdate();

            Assert.That(_player.TryDash(), Is.True);
            Assert.That(_motor.AirDashesUsed, Is.EqualTo(1));
            yield return new WaitForSeconds(0.6f);

            if (!_motor.IsGrounded)
            {
                Assert.That(_player.TryDash(), Is.False, "Only one before landing.");
            }

            var started = Time.time;
            while (!_motor.IsGrounded && Time.time - started < 5f)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(_motor.AirDashesUsed, Is.Zero, "Landing gives it back.");
        }

        [UnityTest]
        public IEnumerator TraversalAbilitiesAreEarnedOnceAndSurviveARestart()
        {
            _progress.UnlockAbility(AbilityIds.AirDash);
            _progress.UnlockAbility(AbilityIds.WallRun);
            _progress.SaveNow();
            yield return new WaitForSecondsRealtime(0.4f);

            yield return LoadLevel();

            Assert.That(_progress.HasAbility(AbilityIds.AirDash), Is.True);
            Assert.That(_progress.HasAbility(AbilityIds.WallRun), Is.True);
            Assert.That(_progress.Data.player.unlockedAbilities
                .FindAll(id => id == AbilityIds.AirDash).Count, Is.EqualTo(1),
                "Earned once, recorded once.");
        }

        [UnityTest]
        public IEnumerator NoAmountOfMobilityGetsThePlayerOutOfASealedArena()
        {
            var encounter = TombScene.GuardianEncounter();
            _progress.UnlockAbility(AbilityIds.AirDash);
            Learn("shadow_initiate");
            Learn("shadow_veiled_step");
            Learn("shadow_dash");

            _motor.ResetTo(new Vector2(42f, 1f));
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(encounter.IsArenaSealed, Is.True, "The way out is shut.");

            // Throw everything at the barrier from the inside.
            var barrier = encounter.Barrier.transform.position.x;
            for (var attempt = 0; attempt < 6; attempt++)
            {
                _player.TryDash();
                yield return new WaitForSeconds(0.35f);
            }

            Assert.That(_player.transform.position.x, Is.GreaterThan(barrier),
                "The player is still on the arena side of the barrier.");
        }

        // ---------- persistence ----------

        [UnityTest]
        public IEnumerator AShadowBuildComesBackWholeAfterARestart()
        {
            Learn("shadow_initiate");
            Learn("shadow_blood_edge");
            _progression.GrantStatPoints(5);
            for (var i = 0; i < 5; i++)
            {
                _progression.AllocatePoint(CharacterStat.Will);
            }

            var will = _stats.GetFinalStat(CharacterStat.Will);
            var power = _stats.ShadowPower;
            _progress.SaveNow();
            yield return new WaitForSecondsRealtime(0.4f);

            yield return LoadLevel();

            Assert.That(_skills.HasSkill("shadow_initiate"), Is.True);
            Assert.That(_skills.HasSkill("shadow_blood_edge"), Is.True);
            Assert.That(_stats.GetFinalStat(CharacterStat.Will), Is.EqualTo(will));
            Assert.That(_stats.ShadowPower, Is.EqualTo(power).Within(0.001f),
                "A passive is worth the same after a reload, not twice as much.");
        }

        [UnityTest]
        public IEnumerator AFileFromBeforeShadowStillLoadsAndPlays()
        {
            var data = GameProgressData.CreateNew();
            data.player.level = 9;
            data.player.unlockedSkills.Add("warrior_training");
            data.player.stats.will = 0;
            SaveSystem.Write(data);

            yield return LoadLevel();

            Assert.That(_skills.HasSkill("warrior_training"), Is.True, "Old progress survives.");
            Assert.That(_stats.GetFinalStat(CharacterStat.Will),
                Is.EqualTo(_progression.Curve.StartingStat(CharacterStat.Will)),
                "Will arrives at the curve's starting value, not at zero.");
            Assert.That(_progress.HasAbility(AbilityIds.AirDash), Is.False,
                "And nothing is handed out that was never earned.");
            Assert.That(_player.IsAlive, Is.True);
        }

        // ---------- helpers ----------

        private IEnumerator LoadLevel()
        {
            yield return TestWorld.Load();
            _player = Object.FindAnyObjectByType<PlayerCharacter>();
            _motor = _player.GetComponent<PlayerMotor2D>();
            _stats = _player.GetComponent<PlayerStats>();
            _buffs = _player.GetComponent<TemporaryBuffs>();
            _skills = Object.FindAnyObjectByType<PlayerSkills>();
            _progression = Object.FindAnyObjectByType<PlayerProgressionService>();
            _progress = Object.FindAnyObjectByType<GameProgress>();
            yield return new WaitForFixedUpdate();
        }

        private void Learn(string skillId)
        {
            _skills.DebugUnlock(skillId);
            _stats.Recalculate();
        }

        private EnemyController Zombie()
        {
            return Object.FindObjectsByType<EnemyController>()
                .First(enemy => enemy.name.StartsWith("ZombieSoldier"));
        }

        private static StatusEffects Statuses(EnemyController enemy)
        {
            var statuses = enemy.GetComponent<StatusEffects>();
            Assert.That(statuses, Is.Not.Null, $"{enemy.name} can be afflicted.");
            statuses.Clear();
            return statuses;
        }
    }
}
