using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// Skills in the running level: what may be bought, what a bought skill really changes, and
    /// what survives a restart. The passives are all checked the same way — measure, learn,
    /// measure again — because a skill that cannot be measured is not a skill.
    /// </summary>
    public sealed class SkillTreeTests
    {
        private PlayerCharacter _player;
        private PlayerMotor2D _motor;
        private PlayerStats _stats;
        private PlayerCombat _combat;
        private PlayerSkills _skills;
        private ActiveSkillController _activeSkills;
        private TemporaryBuffs _buffs;
        private PlayerProgressionService _progression;
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
            if (_screen != null && _screen.IsOpen) { _screen.Close(); }
            GamePause.Clear();
            SaveSystem.Delete();
            Time.timeScale = 1f;
        }

        // ---------- points ----------

        [UnityTest]
        public IEnumerator SkillPointsComeFromLevelsAndAreSeparateFromStatPoints()
        {
            Assert.That(_skills.AvailableSkillPoints, Is.Zero, "Level one has earned nothing yet.");

            _progression.AddExperience(5000);
            yield return null;

            var expected = _skills.TotalEarnedForLevel(_progression.Level);
            Assert.That(_skills.AvailableSkillPoints, Is.EqualTo(expected));
            Assert.That(expected, Is.GreaterThan(0));
            Assert.That(_progression.StatPoints, Is.EqualTo(_progression.Level - 1),
                "Attribute points and skill points are two different currencies.");
        }

        [UnityTest]
        public IEnumerator BuyingASkillCostsExactlyItsPriceAndOnlyWhenItIsAffordable()
        {
            Assert.That(_skills.Purchase("warrior_training"),
                Is.EqualTo(SkillPurchaseResult.NotEnoughPoints));
            Assert.That(_skills.HasSkill("warrior_training"), Is.False);

            _skills.GrantBonusPoints(1);
            Assert.That(_skills.Purchase("warrior_training"),
                Is.EqualTo(SkillPurchaseResult.Purchased));

            Assert.That(_skills.RankOf("warrior_training"), Is.EqualTo(1));
            Assert.That(_skills.AvailableSkillPoints, Is.Zero);
            Assert.That(_skills.Purchase("warrior_heavy_mastery"),
                Is.EqualTo(SkillPurchaseResult.NotEnoughPoints));
            Assert.That(_skills.SpentSkillPoints, Is.EqualTo(1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ASkillDeepInATreeNeedsTheOnesAboveItAndTheRightLevel()
        {
            _skills.GrantBonusPoints(20);

            Assert.That(_skills.Purchase("warrior_ground_breaker"),
                Is.EqualTo(SkillPurchaseResult.MissingPrerequisite));

            _skills.Purchase("warrior_training");
            _skills.Purchase("warrior_heavy_mastery");
            Assert.That(_skills.Purchase("warrior_ground_breaker"),
                Is.EqualTo(SkillPurchaseResult.LevelTooLow),
                "The prerequisites are met, but the character is still too green.");

            _progression.AddExperience(20000);
            yield return null;
            Assert.That(_skills.Purchase("warrior_ground_breaker"),
                Is.EqualTo(SkillPurchaseResult.Purchased));
        }

        [UnityTest]
        public IEnumerator ARankedSkillStopsAtItsLastRankWithoutEatingAPoint()
        {
            var ironBody = _skills.Database.GetSkill("warrior_iron_body");
            _skills.GrantBonusPoints(20);
            _skills.Purchase("warrior_training");
            _skills.Purchase("warrior_efficient_guard");
            for (var i = 0; i < ironBody.MaxRank; i++)
            {
                Assert.That(_skills.Purchase("warrior_iron_body"),
                    Is.EqualTo(SkillPurchaseResult.Purchased));
            }

            var points = _skills.AvailableSkillPoints;
            Assert.That(_skills.Purchase("warrior_iron_body"),
                Is.EqualTo(SkillPurchaseResult.AlreadyMaxRank));
            Assert.That(_skills.AvailableSkillPoints, Is.EqualTo(points));
            Assert.That(_skills.RankOf("warrior_iron_body"), Is.EqualTo(ironBody.MaxRank));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ABuildCanReachIntoBothTreesAtOnce()
        {
            _skills.GrantBonusPoints(20);
            _skills.Purchase("warrior_training");
            _skills.Purchase("warrior_heavy_mastery");

            Assert.That(_skills.Purchase("hunter_training"),
                Is.EqualTo(SkillPurchaseResult.Purchased),
                "Opening one tree must never lock the other: this is a build, not a class.");
            Assert.That(_skills.Purchase("hunter_quickstep"),
                Is.EqualTo(SkillPurchaseResult.Purchased));
            yield return null;
        }

        // ---------- what passives actually do ----------

        [UnityTest]
        public IEnumerator WarriorTrainingMakesEveryMeleeBlowHarder()
        {
            var zombie = Zombie();
            var plain = MeasureHit(zombie, SwingKind.Light);

            Learn("warrior_training");
            yield return null;

            Assert.That(MeasureHit(zombie, SwingKind.Light), Is.GreaterThan(plain));
        }

        [UnityTest]
        public IEnumerator HeavyMasteryChangesHeavyBlowsAndLeavesLightOnesAlone()
        {
            var zombie = Zombie();
            var light = MeasureHit(zombie, SwingKind.Light);
            var heavy = MeasureHit(zombie, SwingKind.Heavy);
            var heavyStagger = MeasureStagger(zombie, SwingKind.Heavy);

            Learn("warrior_training");
            var lightWithRoot = MeasureHit(zombie, SwingKind.Light);
            Learn("warrior_heavy_mastery");
            yield return null;

            Assert.That(MeasureHit(zombie, SwingKind.Heavy),
                Is.GreaterThan(heavy * 1.15f));
            Assert.That(MeasureStagger(zombie, SwingKind.Heavy),
                Is.GreaterThan(heavyStagger));
            Assert.That(MeasureHit(zombie, SwingKind.Light),
                Is.EqualTo(lightWithRoot).Within(0.01f),
                "Heavy mastery is about heavy attacks and nothing else.");
        }

        [UnityTest]
        public IEnumerator ExecutionerOnlyPaysAgainstAnEnemyThatCannotAnswer()
        {
            var zombie = Zombie();
            Learn("warrior_training");
            Learn("warrior_heavy_mastery");
            Learn("warrior_executioner");
            yield return null;

            var upright = MeasureHit(zombie, SwingKind.Light);

            zombie.Actor.Stagger.ForceStun(3f);
            yield return new WaitForFixedUpdate();
            Assert.That(zombie.Actor.Stagger.IsStunned, Is.True);

            Assert.That(MeasureHit(zombie, SwingKind.Light), Is.GreaterThan(upright),
                "The punish window is exactly what this skill rewards.");
        }

        [UnityTest]
        public IEnumerator PredatorOnlyPaysFromBehind()
        {
            var zombie = Zombie();
            Learn("hunter_training");
            Learn("hunter_precision");
            Learn("hunter_predator");
            yield return null;

            // The player stands still and the zombie's facing is what changes, both measurements
            // taken in the same frame. Teleporting around a live enemy and hoping it had not
            // turned yet was a race: it alerts, faces the player, and the reading flips.
            var motor = zombie.GetComponent<EnemyMotor2D>();
            _motor.ResetTo(new Vector2(zombie.transform.position.x + 1f,
                zombie.transform.position.y));
            yield return new WaitForFixedUpdate();

            motor.Face(1);
            var frontal = MeasureHit(zombie, SwingKind.Light);

            motor.Face(-1);
            var fromBehind = MeasureHit(zombie, SwingKind.Light);

            Assert.That(fromBehind, Is.GreaterThan(frontal),
                "The same reading the shield uses decides this, so the two can never disagree.");
        }

        [UnityTest]
        public IEnumerator IronBodyMakesThePlayerHarderToStagger()
        {
            var bare = _player.Stagger.Resistance;
            Learn("warrior_training");
            Learn("warrior_efficient_guard");
            Learn("warrior_iron_body");
            yield return new WaitForFixedUpdate();

            var oneRank = _player.Stagger.Resistance;
            Assert.That(oneRank, Is.GreaterThan(bare));

            Learn("warrior_iron_body");
            yield return new WaitForFixedUpdate();
            Assert.That(_player.Stagger.Resistance, Is.GreaterThan(oneRank),
                "A second rank is worth more than the first alone.");
        }

        [UnityTest]
        public IEnumerator EfficientGuardMakesBlockingCheaperWithoutMakingItFree()
        {
            var hit = new DamageInfo(20f, Vector2.left, guardDamage: 30f,
                attacker: Zombie().gameObject);
            var plain = MeasureBlockCost(hit);
            Assert.That(plain, Is.GreaterThan(0f));

            Learn("warrior_training");
            Learn("warrior_efficient_guard");
            yield return null;

            var trained = MeasureBlockCost(hit);
            Assert.That(trained, Is.LessThan(plain));
            Assert.That(trained, Is.GreaterThan(0f), "A free block would end every other option.");
        }

        [UnityTest]
        public IEnumerator CounterForceMakesAParryHurtTheAttackerMore()
        {
            var zombie = Zombie();
            var plain = MeasureParryStagger(zombie);
            Assert.That(plain.staggered, Is.False,
                "One untrained parry does not break a zombie's poise.");
            Assert.That(plain.applied, Is.GreaterThan(0f));
            yield return new WaitForSeconds(0.6f);

            Learn("warrior_training");
            Learn("warrior_efficient_guard");
            Learn("warrior_counter_force");
            yield return null;

            var trained = MeasureParryStagger(zombie);
            Assert.That(trained.staggered, Is.True,
                "A trained parry pushes the same enemy over its poise threshold.");
        }

        [UnityTest]
        public IEnumerator QuickstepShortensTheWaitBetweenDashes()
        {
            float plain = 0f;
            yield return MeasureDashCooldown(value => plain = value);
            Assert.That(plain, Is.GreaterThan(0f));

            Learn("hunter_training");
            Learn("hunter_quickstep");
            yield return new WaitForFixedUpdate();

            float trained = 0f;
            yield return MeasureDashCooldown(value => trained = value);

            Assert.That(trained, Is.LessThan(plain), "Trained footwork comes back sooner.");
            Assert.That(trained, Is.GreaterThan(0f), "But never instantly.");
        }

        [UnityTest]
        public IEnumerator EfficientMovementMakesTheWallRunCheaper()
        {
            // The dash became free (item 37), so "Ощадливий рух" now pays for the wall run.
            var wall = _player.GetComponent<WallMovement>();
            var plain = wall.RunStaminaPerSecond;
            Assert.That(plain, Is.GreaterThan(0f));

            Learn("hunter_training");
            Learn("hunter_quickstep");
            Learn("hunter_efficiency");
            yield return new WaitForFixedUpdate();

            Assert.That(wall.RunStaminaPerSecond, Is.EqualTo(plain * 0.85f).Within(0.01f));
            yield return Land();
            Assert.That(MeasureDashCost(), Is.Zero, "And the dash stays free.");
        }

        [UnityTest]
        public IEnumerator PrecisionIsTheOnlyThingThatMakesAHitCritical()
        {
            var offense = _player.GetComponent<PlayerOffense>();
            Assert.That(offense.CritChance, Is.Zero, "Nothing crits before it is learned.");

            Learn("hunter_training");
            Learn("hunter_precision");
            yield return null;

            Assert.That(offense.CritChance, Is.GreaterThan(0f));
            Learn("hunter_precision");
            yield return null;
            Assert.That(offense.CritChance, Is.GreaterThan(0.05f),
                "The second rank stacks on the first.");
            Assert.That(offense.CritMultiplier, Is.GreaterThan(1f));
        }

        [UnityTest]
        public IEnumerator MomentumNeedsARealAvoidedAttackAndNotMerelyADash()
        {
            Learn("hunter_training");
            Learn("hunter_quickstep");
            Learn("hunter_momentum");
            yield return null;

            yield return Land();
            Assert.That(_player.TryDash(), Is.True);
            yield return new WaitForSeconds(0.9f);
            Assert.That(_buffs.IsActive(BuffIds.Momentum), Is.False,
                "Dashing at nothing is not a dodge.");

            // Now a real attack arrives and the dash window eats it.
            _player.Stamina.RestoreToFull();
            Assert.That(_player.TryDash(), Is.True);
            _player.Actor.SetInvulnerable(ImmunitySource.Dodge, 0.3f);
            yield return new WaitForFixedUpdate();
            _player.Actor.ApplyDamage(new DamageInfo(15f, Vector2.left,
                attacker: Zombie().gameObject));
            yield return new WaitForFixedUpdate();

            Assert.That(_buffs.IsActive(BuffIds.Momentum), Is.True);
            Assert.That(_player.Health.Current, Is.EqualTo(_player.Health.Max),
                "And the hit really did nothing.");
        }

        [UnityTest]
        public IEnumerator AdrenalineStartsOnAKillAndExpiresOnItsOwn()
        {
            Learn("hunter_training");
            Learn("hunter_precision");
            Learn("hunter_adrenaline");
            yield return null;

            var zombie = Zombie();
            zombie.Actor.ClearInvulnerability();
            zombie.Actor.Health.SetCurrent(1f);
            _combat.TryStartLight();
            StandRelativeTo(zombie, inFront: false);
            yield return new WaitForSeconds(0.4f);

            if (!_buffs.IsActive(BuffIds.Adrenaline))
            {
                // The swing missed in this frame budget; the kill is what matters, so deal it.
                zombie.Actor.ClearInvulnerability();
                zombie.Actor.ApplyDamage(new DamageInfo(999f, Vector2.right,
                    isUnblockable: true, attacker: _player.gameObject));
                yield return new WaitForFixedUpdate();
            }

            Assert.That(zombie.IsAlive, Is.False);
            yield return null;
        }

        // ---------- active skills ----------

        [UnityTest]
        public IEnumerator AnActiveSkillHasToBeLearnedBeforeItCanGoOnTheBar()
        {
            Assert.That(_activeSkills.AssignSlot(0, "warrior_ground_breaker"), Is.False);
            Assert.That(_activeSkills.SlotSkillId(0), Is.Empty);
            Assert.That(_activeSkills.CanActivate(0), Is.EqualTo(SkillActivationResult.EmptySlot));

            LearnGroundBreaker();
            yield return null;

            Assert.That(_activeSkills.AssignSlot(0, "warrior_ground_breaker"), Is.True);
            Assert.That(_activeSkills.SlotSkillId(0), Is.EqualTo("warrior_ground_breaker"));
        }

        [UnityTest]
        public IEnumerator OneSkillCannotOccupyBothSlots()
        {
            LearnGroundBreaker();
            _activeSkills.AssignSlot(0, "warrior_ground_breaker");

            _activeSkills.AssignSlot(1, "warrior_ground_breaker");

            Assert.That(_activeSkills.SlotSkillId(1), Is.EqualTo("warrior_ground_breaker"));
            Assert.That(_activeSkills.SlotSkillId(0), Is.Empty,
                "Two slots for one ability would be two cooldowns for one ability.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator AnEmptySlotIsPressedSafely()
        {
            Assert.That(_player.TryActivateSkill(1), Is.EqualTo(SkillActivationResult.EmptySlot));
            Assert.That(_player.Action, Is.EqualTo(PlayerAction.None));
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator GroundBreakerSwingsOnceSpendsStaminaAndGoesOnCooldown()
        {
            LearnGroundBreaker();
            _activeSkills.AssignSlot(0, "warrior_ground_breaker");
            var zombie = Zombie();
            StandRelativeTo(zombie, inFront: false);
            zombie.Actor.ClearInvulnerability();
            var health = zombie.Actor.Health.Current;
            var stamina = _player.Stamina.Current;
            yield return new WaitForFixedUpdate();

            Assert.That(_player.TryActivateSkill(0),
                Is.EqualTo(SkillActivationResult.Activated));
            Assert.That(_player.Action, Is.EqualTo(PlayerAction.ActiveSkill));
            Assert.That(_player.Stamina.Current, Is.LessThan(stamina));
            Assert.That(_activeSkills.CooldownRemaining(0), Is.GreaterThan(0f));

            yield return new WaitForSeconds(1.6f);

            Assert.That(zombie.Actor.Health.Current, Is.LessThan(health), "It connected.");
            Assert.That(_player.Action, Is.Not.EqualTo(PlayerAction.ActiveSkill),
                "And it ended on its own.");
            Assert.That(_player.TryActivateSkill(0),
                Is.EqualTo(SkillActivationResult.OnCooldown), "It cannot be spammed.");
        }

        [UnityTest]
        public IEnumerator ASkillRefusedForStaminaCostsNothingAtAll()
        {
            LearnGroundBreaker();
            _activeSkills.AssignSlot(0, "warrior_ground_breaker");
            _player.Stamina.Drain(_player.Stamina.Current);
            yield return new WaitForFixedUpdate();

            Assert.That(_player.TryActivateSkill(0),
                Is.EqualTo(SkillActivationResult.NotEnoughStamina));
            Assert.That(_activeSkills.CooldownRemaining(0), Is.Zero,
                "A refused press must not start the cooldown.");
            Assert.That(_activeSkills.IsRunning, Is.False);
            Assert.That(_player.Action, Is.EqualTo(PlayerAction.None));
        }

        [UnityTest]
        public IEnumerator DashStrikeCarriesThePlayerForwardAndHitsOnce()
        {
            LearnDashStrike();
            _activeSkills.AssignSlot(1, "hunter_dash_strike");
            _motor.ResetTo(new Vector2(-9f, 0.85f));
            yield return new WaitForFixedUpdate();
            var start = _player.transform.position.x;

            Assert.That(_player.TryActivateSkill(1),
                Is.EqualTo(SkillActivationResult.Activated));
            yield return new WaitForSeconds(0.7f);

            Assert.That(Mathf.Abs(_player.transform.position.x - start), Is.GreaterThan(0.5f),
                "A mobility skill has to actually move the player.");
            Assert.That(_player.Actor.IsInvulnerable, Is.False,
                "Its invulnerability is a sliver, not the whole skill.");
        }

        [UnityTest]
        public IEnumerator ASkillCannotBeStartedWhileAnotherIsRunningOrWhileSwinging()
        {
            LearnGroundBreaker();
            LearnDashStrike();
            _activeSkills.AssignSlot(0, "warrior_ground_breaker");
            _activeSkills.AssignSlot(1, "hunter_dash_strike");

            _player.TryActivateSkill(0);
            yield return new WaitForFixedUpdate();

            Assert.That(_player.TryActivateSkill(1),
                Is.EqualTo(SkillActivationResult.BusyOrIncapacitated));
            yield return new WaitForSeconds(1.6f);

            _combat.TryStartLight();
            yield return new WaitForFixedUpdate();
            Assert.That(_player.TryActivateSkill(1),
                Is.EqualTo(SkillActivationResult.BusyOrIncapacitated),
                "Ordinary swings and skills do not interrupt each other.");
        }

        [UnityTest]
        public IEnumerator DyingClearsCooldownsAndBuffsButNotWhatWasLearned()
        {
            LearnGroundBreaker();
            Learn("hunter_training");
            _activeSkills.AssignSlot(0, "warrior_ground_breaker");
            _player.TryActivateSkill(0);
            _buffs.Apply(BuffIds.Momentum, SkillEffectType.MomentumDamage, 0.15f, 30f);
            yield return new WaitForSeconds(1.6f);
            Assert.That(_activeSkills.CooldownRemaining(0), Is.GreaterThan(0f));

            Object.FindAnyObjectByType<PlayerRespawner>().Kill();
            yield return new WaitForSecondsRealtime(2.5f);

            Assert.That(_player.IsAlive, Is.True);
            Assert.That(_buffs.IsActive(BuffIds.Momentum), Is.False, "Buffs belong to the fight.");
            Assert.That(_activeSkills.CooldownRemaining(0), Is.Zero);
            Assert.That(_skills.HasSkill("warrior_ground_breaker"), Is.True,
                "What was learned is never lost.");
            Assert.That(_activeSkills.SlotSkillId(0), Is.EqualTo("warrior_ground_breaker"));
        }

        // ---------- persistence ----------

        [UnityTest]
        public IEnumerator LearnedSkillsAndTheSkillBarComeBackAfterARestart()
        {
            LearnGroundBreaker();
            Learn("hunter_training");
            Learn("warrior_iron_body");
            _activeSkills.AssignSlot(0, "warrior_ground_breaker");
            var resistance = _player.Stagger.Resistance;
            var spent = _skills.SpentSkillPoints;
            _progress.SaveNow();
            yield return new WaitForSecondsRealtime(0.4f);

            yield return LoadLevel();

            Assert.That(_skills.HasSkill("warrior_ground_breaker"), Is.True);
            Assert.That(_skills.HasSkill("hunter_training"), Is.True);
            Assert.That(_skills.RankOf("warrior_iron_body"), Is.EqualTo(1));
            Assert.That(_skills.SpentSkillPoints, Is.EqualTo(spent));
            Assert.That(_activeSkills.SlotSkillId(0), Is.EqualTo("warrior_ground_breaker"));
            Assert.That(_player.Stagger.Resistance, Is.EqualTo(resistance).Within(0.0001f),
                "A passive is worth the same after a reload, not twice as much.");
        }

        [UnityTest]
        public IEnumerator ReloadingRepeatedlyNeverStacksAPassive()
        {
            Learn("warrior_training");
            Learn("warrior_efficient_guard");
            Learn("warrior_iron_body");
            yield return new WaitForFixedUpdate();
            var once = _player.Stagger.Resistance;

            for (var i = 0; i < 5; i++)
            {
                _skills.ReloadFromSave();
                _stats.Recalculate();
            }

            yield return new WaitForFixedUpdate();
            Assert.That(_player.Stagger.Resistance, Is.EqualTo(once).Within(0.0001f));
        }

        [UnityTest]
        public IEnumerator ASkillFromAnotherBuildIsIgnoredRatherThanCrashing()
        {
            var data = GameProgressData.CreateNew();
            data.player.unlockedSkills.Add("skill_from_a_mod");
            data.player.unlockedSkills.Add("warrior_training");
            data.player.activeSkillSlot1 = "active_that_was_cut";
            SaveSystem.Write(data);

            LogAssert.ignoreFailingMessages = true;
            yield return LoadLevel();
            LogAssert.ignoreFailingMessages = false;

            Assert.That(_skills.HasSkill("warrior_training"), Is.True, "The real one survives.");
            Assert.That(_skills.HasSkill("skill_from_a_mod"), Is.False);
            Assert.That(_activeSkills.SlotSkillId(0), Is.Empty);
        }

        [UnityTest]
        public IEnumerator ARankBeyondWhatExistsIsClampedOnLoad()
        {
            var data = GameProgressData.CreateNew();
            data.player.unlockedSkills.Add("warrior_iron_body:99");
            SaveSystem.Write(data);

            LogAssert.ignoreFailingMessages = true;
            yield return LoadLevel();
            LogAssert.ignoreFailingMessages = false;

            var maxRank = _skills.Database.GetSkill("warrior_iron_body").MaxRank;
            Assert.That(_skills.RankOf("warrior_iron_body"), Is.EqualTo(maxRank));
        }

        // ---------- the screen ----------

        [UnityTest]
        public IEnumerator TheScreenShowsBothTreesWithTheirNodeStates()
        {
            _skills.GrantBonusPoints(1);
            _screen.Open();

            var warrior = _screen.SkillNodes(SkillBranch.Warrior);
            var hunter = _screen.SkillNodes(SkillBranch.Hunter);
            Assert.That(warrior, Is.Not.Empty);
            Assert.That(hunter, Is.Not.Empty);

            var root = warrior.First(node => node.Skill.SkillId == "warrior_training");
            var deep = warrior.First(node => node.Skill.SkillId == "warrior_ground_breaker");
            Assert.That(root.CanBuy, Is.True, "The way in is open.");
            Assert.That(root.IsUnlocked, Is.False);
            Assert.That(deep.CanBuy, Is.False, "And the deep node is visibly not.");
            Assert.That(deep.State, Is.EqualTo(SkillPurchaseResult.MissingPrerequisite));

            Assert.That(_screen.BuySkill("warrior_training"),
                Is.EqualTo(SkillPurchaseResult.Purchased));
            var bought = _screen.SkillNodes(SkillBranch.Warrior)
                .First(node => node.Skill.SkillId == "warrior_training");
            Assert.That(bought.IsUnlocked, Is.True);
            Assert.That(bought.IsMaxRank, Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheDetailsPanelExplainsWhyASkillCannotBeBought()
        {
            _screen.Open();

            var lines = _screen.DescribeSkill("warrior_ground_breaker");

            Assert.That(lines, Is.Not.Empty);
            Assert.That(lines.Any(line => line.Contains("Активна")));
            Assert.That(lines.Any(line => line.Contains("Потрібно:")));
            Assert.That(lines.Any(line => line.Contains("рівень")));
            Assert.That(lines.Any(line =>
                line == InventoryScreen.PurchaseProblem(SkillPurchaseResult.MissingPrerequisite)));
            yield return null;
        }

        [UnityTest]
        public IEnumerator SkillsCannotBeBoughtInTheMiddleOfAFight()
        {
            _skills.GrantBonusPoints(5);
            var status = _player.GetComponent<PlayerCombatStatus>();
            _player.Actor.ClearInvulnerability();
            _player.Actor.ApplyDamage(new DamageInfo(5f, Vector2.left,
                attacker: Zombie().gameObject));
            yield return new WaitForFixedUpdate();

            Assert.That(status.IsInCombat, Is.True);
            Assert.That(_skills.CanSpendPoints, Is.False);
            Assert.That(_skills.Purchase("warrior_training"),
                Is.EqualTo(SkillPurchaseResult.NotAllowedNow));

            status.Clear();
            Assert.That(_skills.Purchase("warrior_training"),
                Is.EqualTo(SkillPurchaseResult.Purchased),
                "Out of combat it is an ordinary choice again.");
        }

        // ---------- helpers ----------

        private IEnumerator LoadLevel()
        {
            yield return TestWorld.Load();
            _player = Object.FindAnyObjectByType<PlayerCharacter>();
            _motor = _player.GetComponent<PlayerMotor2D>();
            _stats = _player.GetComponent<PlayerStats>();
            _combat = _player.GetComponent<PlayerCombat>();
            _activeSkills = _player.GetComponent<ActiveSkillController>();
            _buffs = _player.GetComponent<TemporaryBuffs>();
            _skills = Object.FindAnyObjectByType<PlayerSkills>();
            _progression = Object.FindAnyObjectByType<PlayerProgressionService>();
            _progress = Object.FindAnyObjectByType<GameProgress>();
            _screen = Object.FindAnyObjectByType<InventoryScreen>();
            yield return new WaitForFixedUpdate();
        }

        /// <summary>Learns a skill outright; the purchase rules have their own tests.</summary>
        private void Learn(string skillId)
        {
            _skills.DebugUnlock(skillId);
            _stats.Recalculate();
        }

        private void LearnGroundBreaker()
        {
            Learn("warrior_training");
            Learn("warrior_heavy_mastery");
            Learn("warrior_ground_breaker");
        }

        private void LearnDashStrike()
        {
            Learn("hunter_training");
            Learn("hunter_quickstep");
            Learn("hunter_dash_strike");
        }

        private EnemyController Zombie()
        {
            return Object.FindObjectsByType<EnemyController>()
                .First(enemy => enemy.name.StartsWith("ZombieSoldier"));
        }

        /// <summary>
        /// Sends one swing's worth of damage through the real pipeline and reports what landed.
        /// The target is restored afterwards, so a measurement never changes the fight.
        ///
        /// A critical is rolled on every swing, so once a skill grants crit chance one reading can
        /// come out 1.5 times the other by luck — which is exactly how PredatorOnlyPaysFromBehind
        /// failed now and then (a frontal 25.5 against 18.7 from behind: 17 × 1.5 and 17 × 1.1).
        /// The smallest of many swings is the hit without a critical, which is what the damage
        /// tests compare.
        /// </summary>
        private float MeasureHit(EnemyController target, SwingKind kind)
        {
            var offense = _player.GetComponent<PlayerOffense>();
            var weapon = _combat.Weapon;
            var attack = kind == SwingKind.Heavy ? weapon.HeavyAttack : weapon.LightAttack(0);
            var smallest = float.MaxValue;
            for (var i = 0; i < 30; i++)
            {
                offense.BeginSwing(kind);
                var built = attack.BuildWeaponDamage(weapon, _stats.ActiveWeaponAttack,
                    _player.gameObject, null, 1);
                smallest = Mathf.Min(smallest, offense.Augment(built, target.Actor).Amount);
            }

            return smallest;
        }

        private float MeasureStagger(EnemyController target, SwingKind kind)
        {
            var offense = _player.GetComponent<PlayerOffense>();
            var weapon = _combat.Weapon;
            var attack = kind == SwingKind.Heavy ? weapon.HeavyAttack : weapon.LightAttack(0);
            offense.BeginSwing(kind);
            var built = attack.BuildWeaponDamage(weapon, _stats.ActiveWeaponAttack,
                _player.gameObject, null, 1);
            return offense.Augment(built, target.Actor).StaggerDamage;
        }

        /// <summary>Raises the guard, takes one hit on it and reports the stamina it cost.</summary>
        private float MeasureBlockCost(DamageInfo hit)
        {
            var defense = _player.Defense;
            _player.Stamina.RestoreToFull();
            defense.EndBlock();
            defense.BeginBlock();
            // Raising the guard again inside the parry cooldown starts straight in the blocking
            // phase; a parry would cost nothing and measure nothing.
            defense.EndBlock();
            defense.BeginBlock();
            Assert.That(defense.Phase, Is.EqualTo(GuardPhase.Block));
            var before = _player.Stamina.Current;
            // Aimed at the front, where a block is possible at all.
            defense.Evaluate(hit.AimedAt(_player.transform.position,
                new Vector2(-_motor.FacingDirection, 0f)));
            var cost = before - _player.Stamina.Current;
            defense.EndBlock();
            _player.Stamina.RestoreToFull();
            return cost;
        }

        /// <summary>
        /// Parries one blow and reports what it did to the attacker's poise. The meter alone is
        /// not enough: crossing the threshold empties it, so a stronger parry can read as zero.
        /// </summary>
        private (float applied, bool staggered) MeasureParryStagger(EnemyController attacker)
        {
            var defense = _player.Defense;
            var meter = attacker.Actor.Stagger;
            meter.ResetStagger();
            var staggered = false;
            void OnStaggered(float duration) => staggered = true;
            meter.Staggered += OnStaggered;

            defense.EndBlock();
            defense.BeginBlock();
            defense.Evaluate(new DamageInfo(10f, new Vector2(-_motor.FacingDirection, 0f),
                attacker: attacker.gameObject));
            var applied = staggered ? meter.Threshold : meter.Current;

            meter.Staggered -= OnStaggered;
            defense.EndBlock();
            meter.ResetStagger();
            return (applied, staggered);
        }

        /// <summary>
        /// Dashes and waits, in real time, for the dash to become available again. A measurement
        /// rather than a recomputation of the formula, so it would notice the skill being wired
        /// to nothing.
        /// </summary>
        private IEnumerator MeasureDashCooldown(System.Action<float> report)
        {
            _player.Stamina.RestoreToFull();
            _motor.CancelDash();
            yield return Land();
            Assert.That(_player.TryDash(), Is.True);

            var started = Time.time;
            while (!_motor.IsDashReady && Time.time - started < 5f)
            {
                yield return null;
            }

            report(Time.time - started);
        }

        /// <summary>Dashes through the player's own rules and reports the stamina it cost.</summary>
        private float MeasureDashCost()
        {
            _player.Stamina.RestoreToFull();
            _motor.CancelDash();
            var before = _player.Stamina.Current;
            Assert.That(_player.TryDash(), Is.True);
            return before - _player.Stamina.Current;
        }

        /// <summary>
        /// Waits for the player to be standing on something. Dashing in the air is a traversal
        /// ability a new character does not have, and these tests are about stamina, not that.
        /// </summary>
        private IEnumerator Land()
        {
            var started = Time.time;
            while (!_motor.IsGrounded && Time.time - started < 3f)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(_motor.IsGrounded, Is.True, "The player never landed.");
        }

        /// <summary>Puts the player on one side of an enemy, facing it.</summary>
        private void StandRelativeTo(EnemyController enemy, bool inFront)
        {
            var side = inFront ? enemy.FacingDirection : -enemy.FacingDirection;
            _motor.ResetTo(new Vector2(enemy.transform.position.x + side * 1.0f,
                enemy.transform.position.y));
        }
    }
}
