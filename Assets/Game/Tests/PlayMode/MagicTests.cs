using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// Magic in the running level: mana, casting, what a spell does when it lands, and what
    /// survives a restart. Spells go through the same hitboxes and the same damage pipeline as
    /// everything else, so most of these check that nothing about magic is special-cased.
    /// </summary>
    public sealed class MagicTests
    {
        private PlayerCharacter _player;
        private PlayerMotor2D _motor;
        private PlayerStats _stats;
        private PlayerSkills _skills;
        private PlayerSpellbook _spellbook;
        private SpellCaster _caster;
        private Mana _mana;
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
            SpellProjectile.ClearAll(Faction.Player);
            GamePause.Clear();
            SaveSystem.Delete();
            Time.timeScale = 1f;
        }

        // ---------- mana ----------

        [UnityTest]
        public IEnumerator ManaStartsFullAndIsSeparateFromStamina()
        {
            Assert.That(_mana, Is.Not.Null);
            Assert.That(_mana.Current, Is.EqualTo(_mana.Max));
            Assert.That(_mana.Max, Is.EqualTo(_stats.MaxMana).Within(0.01f));

            _player.Stamina.Drain(50f);
            yield return null;

            Assert.That(_mana.Current, Is.EqualTo(_mana.Max),
                "Spending the body does not spend the mind.");
        }

        [UnityTest]
        public IEnumerator ManaIsSpentWholeOrNotAtAllAndNeverGoesBelowZero()
        {
            Assert.That(_mana.TrySpend(30f), Is.True);
            Assert.That(_mana.Current, Is.EqualTo(_mana.Max - 30f).Within(0.01f));

            var left = _mana.Current;
            Assert.That(_mana.TrySpend(left + 10f), Is.False);
            Assert.That(_mana.Current, Is.EqualTo(left).Within(0.01f),
                "A refused spend costs nothing.");

            Assert.That(_mana.TrySpend(left), Is.True);
            Assert.That(_mana.Current, Is.Zero);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ManaWaitsOutItsPauseAndThenComesBack()
        {
            _mana.TrySpend(40f);
            var afterSpend = _mana.Current;
            yield return new WaitForSeconds(0.5f);

            Assert.That(_mana.Current, Is.EqualTo(afterSpend).Within(0.5f),
                "Nothing comes back during the pause.");

            yield return new WaitForSeconds(3f);
            Assert.That(_mana.Current, Is.GreaterThan(afterSpend + 5f));
        }

        [UnityTest]
        public IEnumerator IntelligenceBuysABiggerPoolWithoutRefillingIt()
        {
            var before = _mana.Max;
            _mana.TrySpend(30f);
            var spent = _mana.Current;

            _progression.GrantStatPoints(5);
            for (var i = 0; i < 5; i++)
            {
                _progression.AllocatePoint(CharacterStat.Intelligence);
            }

            yield return new WaitForFixedUpdate();

            Assert.That(_mana.Max, Is.GreaterThan(before));
            Assert.That(_mana.Current, Is.EqualTo(spent).Within(1f),
                "A bigger pool is not a full one.");
        }

        // ---------- casting ----------

        [UnityTest]
        public IEnumerator CastingTakesTimeSpendsManaAtTheEndAndThenRecovers()
        {
            LearnAndEquip("fire_bolt", 0);
            var spell = _spellbook.SlotSpell(0);
            var mana = _mana.Current;

            Assert.That(_player.TryCastSpell(0), Is.EqualTo(CastResult.Casting));
            Assert.That(_player.Action, Is.EqualTo(PlayerAction.Casting));
            Assert.That(_mana.Current, Is.EqualTo(mana).Within(0.01f),
                "Nothing is paid until the spell is actually released.");

            yield return new WaitForSeconds(_caster.CastTimeOf(spell) + 0.1f);
            Assert.That(_mana.Current, Is.LessThan(mana), "Released and paid for.");
            Assert.That(_caster.CooldownRemaining(0), Is.GreaterThan(0f));

            yield return new WaitForSeconds(spell.RecoveryTime + 0.2f);
            Assert.That(_player.Action, Is.Not.EqualTo(PlayerAction.Casting));
        }

        [UnityTest]
        public IEnumerator ACastRefusedForManaCostsNothingAndStartsNoCooldown()
        {
            LearnAndEquip("arcane_burst", 0);
            yield return Land();
            _mana.TrySpend(_mana.Current);
            yield return new WaitForFixedUpdate();

            Assert.That(_player.TryCastSpell(0), Is.EqualTo(CastResult.NotEnoughMana));
            Assert.That(_caster.IsCasting, Is.False);
            Assert.That(_caster.CooldownRemaining(0), Is.Zero);
            Assert.That(_player.Action, Is.EqualTo(PlayerAction.None));
            Assert.That(SpellProjectile.LiveCount(Faction.Player), Is.Zero);
        }

        [UnityTest]
        public IEnumerator AHitDuringTheCastTakesTheSpellAwayButNotTheMana()
        {
            LearnAndEquip("arcane_burst", 0);
            yield return Land();
            var mana = _mana.Current;
            Assert.That(_player.TryCastSpell(0), Is.EqualTo(CastResult.Casting));
            yield return new WaitForFixedUpdate();
            Assert.That(_caster.IsCasting, Is.True);

            _player.Actor.ClearInvulnerability();
            _player.Actor.ApplyDamage(new DamageInfo(12f, Vector2.left,
                attacker: Zombie().gameObject));
            yield return new WaitForFixedUpdate();

            Assert.That(_caster.IsCasting, Is.False, "The spell is lost.");
            Assert.That(_mana.Current, Is.EqualTo(mana).Within(0.01f),
                "And it cost nothing but the time.");
            Assert.That(_caster.CooldownRemaining(0), Is.Zero);
        }

        [UnityTest]
        public IEnumerator AnEmptySlotAndAnUnknownSpellAreBothSafe()
        {
            Assert.That(_player.TryCastSpell(2), Is.EqualTo(CastResult.EmptySlot));
            Assert.That(_player.Action, Is.EqualTo(PlayerAction.None));
            Assert.That(_spellbook.AssignSlot(0, "fire_bolt"), Is.False,
                "A spell not yet learned cannot go on the bar.");
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ASpellCannotBeCastWhileSwingingDashingOrCasting()
        {
            LearnAndEquip("fire_bolt", 0);
            LearnAndEquip("frost_shard", 1);

            _player.Combat.TryStartLight();
            yield return new WaitForFixedUpdate();
            Assert.That(_player.TryCastSpell(0),
                Is.EqualTo(CastResult.BusyOrIncapacitated));
            yield return new WaitForSeconds(0.6f);

            _player.TryCastSpell(0);
            yield return new WaitForFixedUpdate();
            Assert.That(_caster.IsCasting, Is.True);
            Assert.That(_player.TryCastSpell(1), Is.EqualTo(CastResult.BusyOrIncapacitated),
                "One spell at a time.");
        }

        [UnityTest]
        public IEnumerator AGroundOnlySpellIsRefusedInTheAir()
        {
            LearnAndEquip("arcane_burst", 0);
            LearnAndEquip("fire_bolt", 1);
            _motor.ResetTo(new Vector2(-12f, 6f));
            yield return new WaitForFixedUpdate();
            Assert.That(_motor.IsGrounded, Is.False);

            Assert.That(_player.TryCastSpell(0), Is.EqualTo(CastResult.GroundOnly));
            Assert.That(_player.TryCastSpell(1), Is.EqualTo(CastResult.Casting),
                "A bolt can be thrown from anywhere.");
        }

        // ---------- projectiles ----------

        [UnityTest]
        public IEnumerator AFireBoltFliesHitsOnceAndIsSpent()
        {
            LearnAndEquip("fire_bolt", 0);
            // A training dummy rather than a zombie: it stands still, so the test measures the
            // projectile and not the enemy's patrol.
            // The leftmost dummy, with clear air between it and the player: anything standing in
            // between would stop the bolt first, which is itself correct but not what this checks.
            var dummy = Object.FindObjectsByType<TrainingDummy>()
                .OrderBy(target => target.transform.position.x).First();
            var target = dummy.GetComponent<CombatActor>();
            target.ClearInvulnerability();
            _motor.ResetTo(new Vector2(dummy.transform.position.x - 4f, 0.85f));
            yield return Land();
            Assert.That(_motor.FacingDirection, Is.EqualTo(1), "Facing the dummy.");
            var health = target.Health.Current;

            Assert.That(_player.TryCastSpell(0), Is.EqualTo(CastResult.Casting));

            var sawItFly = false;
            var started = Time.time;
            while (Time.time - started < 2f)
            {
                sawItFly |= SpellProjectile.LiveCount(Faction.Player) > 0;
                yield return new WaitForFixedUpdate();
            }

            Assert.That(sawItFly, Is.True, "It was in the air.");
            Assert.That(target.Health.Current, Is.LessThan(health), "And it connected.");
            Assert.That(SpellProjectile.LiveCount(Faction.Player), Is.Zero,
                "One target and the bolt is spent.");
            Assert.That(dummy.HitCount, Is.EqualTo(1), "Exactly once.");
        }

        [UnityTest]
        public IEnumerator AProjectileDiesAgainstAWallRatherThanFlyingOn()
        {
            LearnAndEquip("fire_bolt", 0);
            // Facing the level's left wall, well away from any enemy.
            _motor.ResetTo(new Vector2(-13.5f, 0.85f));
            yield return new WaitForFixedUpdate();
            _motor.ResetTo(new Vector2(-14f, 0.85f));
            yield return new WaitForFixedUpdate();

            _player.TryCastSpell(0);
            yield return new WaitForSeconds(2f);

            Assert.That(SpellProjectile.LiveCount(Faction.Player), Is.Zero,
                "Nothing is left flying: a wall or its own range ends it.");
        }

        [UnityTest]
        public IEnumerator AProjectileNeverOutlivesItsRange()
        {
            LearnAndEquip("fire_bolt", 0);
            // Pointed down an empty corridor.
            _motor.ResetTo(new Vector2(26f, 0.85f));
            yield return new WaitForFixedUpdate();

            _player.TryCastSpell(0);
            yield return new WaitForSeconds(0.5f);
            yield return new WaitForSeconds(3f);

            Assert.That(SpellProjectile.LiveCount(Faction.Player), Is.Zero);
        }

        [UnityTest]
        public IEnumerator APlayerSpellNeverHitsThePlayer()
        {
            LearnAndEquip("fire_bolt", 0);
            var health = _player.Health.Current;
            _player.Actor.ClearInvulnerability();

            _player.TryCastSpell(0);
            yield return new WaitForSeconds(0.8f);

            Assert.That(_player.Health.Current, Is.EqualTo(health).Within(0.01f));
        }

        // ---------- area and buff ----------

        [UnityTest]
        public IEnumerator ArcaneBurstStrikesEveryoneNearbyExactlyOnce()
        {
            LearnAndEquip("arcane_burst", 0);
            var dummies = Object.FindObjectsByType<TrainingDummy>();
            Assert.That(dummies.Length, Is.GreaterThan(1));
            var first = dummies[0].GetComponent<Health>();
            var second = dummies[1].GetComponent<Health>();
            _motor.ResetTo(new Vector2(
                (dummies[0].transform.position.x + dummies[1].transform.position.x) * 0.5f, 0.85f));
            foreach (var dummy in dummies)
            {
                dummy.GetComponent<CombatActor>().ClearInvulnerability();
            }

            var firstHealth = first.Current;
            var secondHealth = second.Current;
            yield return Land();

            Assert.That(_player.TryCastSpell(0), Is.EqualTo(CastResult.Casting));
            yield return new WaitForSeconds(1.4f);

            Assert.That(first.Current, Is.LessThan(firstHealth));
            Assert.That(second.Current, Is.LessThan(secondHealth),
                "A burst catches everyone in reach.");
            Assert.That(dummies[0].HitCount, Is.EqualTo(1),
                "And each of them exactly once, not once per frame.");
        }

        [UnityTest]
        public IEnumerator ManaShieldSoftensHitsForAWhileAndThenStops()
        {
            LearnAndEquip("mana_shield", 0);
            yield return Land();
            var bare = TakeHit(30f);
            // Taking a hit puts the player in a flinch; casting waits for it, as it would in play.
            yield return Recover();

            Assert.That(_player.TryCastSpell(0), Is.EqualTo(CastResult.Casting));
            yield return new WaitForSeconds(0.6f);
            Assert.That(_buffs.IsActive("mana_shield"), Is.True);

            var warded = TakeHit(30f);
            Assert.That(warded, Is.LessThan(bare));
            Assert.That(warded, Is.GreaterThan(0f), "A ward is not invulnerability.");

            // Casting it again refreshes rather than stacking, so it cannot be layered.
            var spell = _spellbook.SlotSpell(0);
            yield return new WaitForSeconds(spell.BuffDuration + 0.5f);
            Assert.That(_buffs.IsActive("mana_shield"), Is.False);
            Assert.That(TakeHit(30f), Is.EqualTo(bare).Within(0.01f),
                "And when it ends it leaves nothing behind.");
        }

        // ---------- elements ----------

        [UnityTest]
        public IEnumerator AZombieBurnsAndAGuardShrugsOffSteel()
        {
            var zombie = Zombie().Actor.Resistances;
            var guard = Object.FindObjectsByType<EnemyController>()
                .First(enemy => enemy.name.StartsWith("ShieldGuard")).Actor.Resistances;

            Assert.That(zombie, Is.Not.Null);
            Assert.That(zombie.Multiplier(DamageType.Fire), Is.GreaterThan(1f),
                "Dry and long dead: fire finds it easily.");
            Assert.That(zombie.Multiplier(DamageType.Frost), Is.LessThan(1f));
            Assert.That(guard.Multiplier(DamageType.Physical), Is.LessThan(1f),
                "Plate turns a blade.");
            Assert.That(guard.Multiplier(DamageType.Magic), Is.EqualTo(1f).Within(0.001f),
                "But not a spell.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator ResistanceReallyChangesWhatReachesHealth()
        {
            var zombie = Zombie();
            zombie.Actor.ClearInvulnerability();

            var fire = ApplyElement(zombie, DamageType.Fire, 40f);
            zombie.Actor.ClearInvulnerability();
            var frost = ApplyElement(zombie, DamageType.Frost, 40f);

            Assert.That(fire, Is.GreaterThan(40f), "A weakness makes the same hit worse.");
            Assert.That(frost, Is.LessThan(40f), "And a resistance makes it milder.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator FireMasteryLiftsFireAndLeavesFrostAlone()
        {
            Learn("mage_training");
            Learn("mage_frost_studies");
            var bolt = _spellbook.Database.GetSpell("fire_bolt");
            var shard = _spellbook.Database.GetSpell("frost_shard");
            var fireBefore = _caster.Evaluate(bolt).Total;
            var frostBefore = _caster.Evaluate(shard).Total;

            Learn("mage_fire_mastery");
            yield return null;

            Assert.That(_caster.Evaluate(bolt).Total, Is.GreaterThan(fireBefore));
            Assert.That(_caster.Evaluate(shard).Total, Is.EqualTo(frostBefore).Within(0.01f),
                "Mastery of one element says nothing about another.");
        }

        // ---------- the Mage branch ----------

        [UnityTest]
        public IEnumerator LearningAMageSkillTeachesItsSpell()
        {
            Assert.That(_spellbook.Knows("fire_bolt"), Is.False);

            Learn("mage_training");
            yield return null;

            Assert.That(_spellbook.Knows("fire_bolt"), Is.True);
            Assert.That(_spellbook.AssignSlot(0, "fire_bolt"), Is.True);

            Learn("mage_frost_studies");
            Assert.That(_spellbook.Knows("frost_shard"), Is.True);
        }

        [UnityTest]
        public IEnumerator MageSkillsChangeWhatSpellsCostAndDo()
        {
            Learn("mage_training");
            var bolt = _spellbook.Database.GetSpell("fire_bolt");
            var damage = _caster.Evaluate(bolt).Total;
            var cost = _caster.ManaCostOf(bolt);
            var castTime = _caster.CastTimeOf(bolt);
            var mana = _stats.MaxMana;

            Learn("mage_mana_well");
            Learn("mage_efficiency");
            Learn("mage_fire_mastery");
            Learn("mage_arcane_focus");
            Learn("mage_quick_cast");
            yield return new WaitForFixedUpdate();

            Assert.That(_stats.MaxMana, Is.GreaterThan(mana));
            Assert.That(_caster.ManaCostOf(bolt), Is.LessThan(cost));
            Assert.That(_caster.ManaCostOf(bolt), Is.GreaterThan(0f), "Never free.");
            Assert.That(_caster.CastTimeOf(bolt), Is.LessThan(castTime));
            Assert.That(_caster.CastTimeOf(bolt), Is.GreaterThan(0f), "Never instant.");
            Assert.That(_caster.Evaluate(bolt).Total, Is.GreaterThan(damage));
        }

        [UnityTest]
        public IEnumerator SpellPrecisionIsTheOnlyThingThatMakesASpellCrit()
        {
            Learn("mage_training");
            Assert.That(_caster.SpellCritChance, Is.Zero);

            Learn("mage_frost_studies");
            Learn("mage_spell_precision");
            yield return null;

            Assert.That(_caster.SpellCritChance, Is.GreaterThan(0f));
        }

        [UnityTest]
        public IEnumerator AHybridCanCarryASwordAndASpellAtOnce()
        {
            Learn("warrior_training");
            Learn("warrior_heavy_mastery");
            Learn("mage_training");
            LearnAndEquip("fire_bolt", 0);
            yield return null;

            Assert.That(_player.Combat.Weapon, Is.Not.Null, "The sword is still in hand.");
            Assert.That(_skills.HasSkill("warrior_heavy_mastery"), Is.True);
            Assert.That(_spellbook.Knows("fire_bolt"), Is.True);

            _player.Combat.TryStartLight();
            yield return new WaitForSeconds(0.7f);
            Assert.That(_player.TryCastSpell(0), Is.EqualTo(CastResult.Casting),
                "One encounter, both systems, no conflict.");
        }

        // ---------- cleanup and persistence ----------

        [UnityTest]
        public IEnumerator DyingRestoresManaAndLeavesNothingInTheAir()
        {
            LearnAndEquip("fire_bolt", 0);
            _player.TryCastSpell(0);
            yield return new WaitForSeconds(0.45f);
            Assert.That(SpellProjectile.LiveCount(Faction.Player), Is.EqualTo(1));
            _buffs.Apply("mana_shield", SkillEffectType.SpellDamageReduction, 0.2f, 60f);

            Object.FindAnyObjectByType<PlayerRespawner>().Kill();
            yield return new WaitForSecondsRealtime(2.5f);

            Assert.That(_player.IsAlive, Is.True);
            Assert.That(_mana.Current, Is.EqualTo(_mana.Max).Within(0.01f));
            Assert.That(SpellProjectile.LiveCount(Faction.Player), Is.Zero,
                "A bolt fired a moment before dying must not arrive after the world was reset.");
            Assert.That(_buffs.IsActive("mana_shield"), Is.False);
            Assert.That(_caster.IsCasting, Is.False);
            Assert.That(_spellbook.Knows("fire_bolt"), Is.True, "What was learned stays learned.");
        }

        [UnityTest]
        public IEnumerator SpellsIntelligenceAndTheSpellBarComeBackAfterARestart()
        {
            Learn("mage_training");
            Learn("mage_frost_studies");
            _spellbook.AssignSlot(0, "fire_bolt");
            _spellbook.AssignSlot(2, "frost_shard");
            _progression.GrantStatPoints(4);
            for (var i = 0; i < 4; i++)
            {
                _progression.AllocatePoint(CharacterStat.Intelligence);
            }

            var intelligence = _stats.GetFinalStat(CharacterStat.Intelligence);
            var maxMana = _stats.MaxMana;
            var damage = _caster.Evaluate(_spellbook.SlotSpell(0)).Total;
            _progress.SaveNow();
            yield return new WaitForSecondsRealtime(0.4f);

            yield return LoadLevel();

            Assert.That(_spellbook.Knows("fire_bolt"), Is.True);
            Assert.That(_spellbook.Knows("frost_shard"), Is.True);
            Assert.That(_spellbook.SlotSpellId(0), Is.EqualTo("fire_bolt"));
            Assert.That(_spellbook.SlotSpellId(2), Is.EqualTo("frost_shard"));
            Assert.That(_stats.GetFinalStat(CharacterStat.Intelligence), Is.EqualTo(intelligence));
            Assert.That(_stats.MaxMana, Is.EqualTo(maxMana).Within(0.01f),
                "A passive is worth the same after a reload, not twice as much.");
            Assert.That(_mana.Current, Is.EqualTo(_mana.Max).Within(0.01f),
                "And mana is never saved: a load starts full.");
            Assert.That(_caster.Evaluate(_spellbook.SlotSpell(0)).Total,
                Is.EqualTo(damage).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator ReloadingRepeatedlyNeverDoublesAMagePassive()
        {
            Learn("mage_training");
            Learn("mage_mana_well");
            yield return new WaitForFixedUpdate();
            var once = _stats.MaxMana;

            for (var i = 0; i < 5; i++)
            {
                _skills.ReloadFromSave();
                _spellbook.ReloadFromSave();
                _stats.Recalculate();
            }

            yield return new WaitForFixedUpdate();
            Assert.That(_stats.MaxMana, Is.EqualTo(once).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator AFileWithASkillButNoSpellGetsTheSpellBack()
        {
            // A hand-edited or older file: the teaching skill is there, the spell is not.
            var data = GameProgressData.CreateNew();
            data.player.unlockedSkills.Add("mage_training");
            data.player.bonusSkillPoints = 1;
            SaveSystem.Write(data);

            yield return LoadLevel();

            Assert.That(_skills.HasSkill("mage_training"), Is.True);
            Assert.That(_spellbook.Knows("fire_bolt"), Is.True,
                "A skill that teaches a spell must never leave the player without it.");
        }

        [UnityTest]
        public IEnumerator ASpellFromAnotherBuildIsIgnoredRatherThanCrashing()
        {
            var data = GameProgressData.CreateNew();
            data.player.unlockedSpells.Add("spell_from_a_mod");
            data.player.spellSlot1 = "spell_that_was_cut";
            SaveSystem.Write(data);

            LogAssert.ignoreFailingMessages = true;
            yield return LoadLevel();
            LogAssert.ignoreFailingMessages = false;

            Assert.That(_spellbook.Knows("spell_from_a_mod"), Is.False);
            Assert.That(_spellbook.SlotSpellId(0), Is.Empty);
            Assert.That(_player.IsAlive, Is.True);
        }

        // ---------- the screen ----------

        [UnityTest]
        public IEnumerator TheSpellbookShowsWhatASpellWouldReallyDo()
        {
            Learn("mage_training");
            _screen.Open();

            var lines = _screen.DescribeSpell("fire_bolt");

            Assert.That(lines, Is.Not.Empty);
            Assert.That(lines.Any(line => line.Contains("Вогонь")));
            Assert.That(lines.Any(line => line.Contains("Мана")));
            var damage = _caster.Evaluate(_spellbook.Database.GetSpell("fire_bolt")).Total;
            Assert.That(lines.Any(line => line.Contains($"{damage:0.#}")), Is.True,
                "The panel and the cast read from the same calculator.");
            Assert.That(_screen.AssignSpell(0, "fire_bolt"), Is.True);
            Assert.That(_screen.SpellSlotId(0), Is.EqualTo("fire_bolt"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheTreeScreenNowOffersThreeBranches()
        {
            _screen.Open();

            Assert.That(InventoryScreen.Branches, Does.Contain(SkillBranch.Mage));
            Assert.That(_screen.SkillNodes(SkillBranch.Mage), Is.Not.Empty);
            Assert.That(_screen.SkillNodes(SkillBranch.Warrior), Is.Not.Empty,
                "And the older two are untouched.");
            Assert.That(_screen.SkillNodes(SkillBranch.Hunter), Is.Not.Empty);
            Assert.That(_screen.DescribeCharacter()
                .Any(line => line.StartsWith(CharacterStats.Display(CharacterStat.Intelligence))),
                Is.True, "Intelligence is on the character sheet.");
            yield return null;
        }

        // ---------- helpers ----------

        private IEnumerator LoadLevel()
        {
            yield return SceneManager.LoadSceneAsync("TombTrial");
            _player = Object.FindAnyObjectByType<PlayerCharacter>();
            _motor = _player.GetComponent<PlayerMotor2D>();
            _stats = _player.GetComponent<PlayerStats>();
            _caster = _player.GetComponent<SpellCaster>();
            _mana = _player.GetComponent<Mana>();
            _buffs = _player.GetComponent<TemporaryBuffs>();
            _skills = Object.FindAnyObjectByType<PlayerSkills>();
            _spellbook = Object.FindAnyObjectByType<PlayerSpellbook>();
            _progression = Object.FindAnyObjectByType<PlayerProgressionService>();
            _progress = Object.FindAnyObjectByType<GameProgress>();
            _screen = Object.FindAnyObjectByType<InventoryScreen>();
            yield return new WaitForFixedUpdate();
        }

        private void Learn(string skillId)
        {
            _skills.DebugUnlock(skillId);
            _stats.Recalculate();
        }

        /// <summary>
        /// Waits for the player to be standing on something. A character that has just been
        /// placed is still falling, and a ground-only spell would rightly refuse.
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

        /// <summary>Waits out a flinch, so the next action is not refused for being mid-hurt.</summary>
        private IEnumerator Recover()
        {
            var started = Time.time;
            while (_player.Action != PlayerAction.None && Time.time - started < 2f)
            {
                yield return new WaitForFixedUpdate();
            }
        }

        /// <summary>Teaches a spell outright and puts it on the bar; the tree has its own tests.</summary>
        private void LearnAndEquip(string spellId, int slot)
        {
            _spellbook.Learn(spellId);
            _spellbook.AssignSlot(slot, spellId);
        }

        private EnemyController Zombie()
        {
            return Object.FindObjectsByType<EnemyController>()
                .First(enemy => enemy.name.StartsWith("ZombieSoldier"));
        }

        private float ApplyElement(EnemyController enemy, DamageType element, float amount)
        {
            var result = enemy.Actor.ApplyDamage(new DamageInfo(amount, Vector2.right, element,
                isUnblockable: true));
            enemy.Actor.Health.RestoreToFull();
            return result.AppliedDamage;
        }

        /// <summary>Applies a known hit and reports what reached health.</summary>
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
