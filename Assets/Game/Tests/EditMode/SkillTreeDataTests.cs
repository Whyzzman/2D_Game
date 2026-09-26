using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The skill tree as data: whether the shipped tree is sound, whether the database catches a
    /// broken one, and what the save file makes of skills.
    /// </summary>
    public sealed class SkillTreeDataTests
    {
        private SkillDatabase _database;

        [SetUp]
        public void SetUp()
        {
            _database = UnityEditor.AssetDatabase.LoadAssetAtPath<SkillDatabase>(
                "Assets/Game/Data/SkillDatabase.asset");
            Assert.That(_database, Is.Not.Null, "The content build writes the skill database.");
        }

        [TearDown]
        public void TearDown()
        {
            SaveSystem.Delete();
        }

        // ---------- the shipped tree ----------

        [Test]
        public void TheShippedTreeIsSound()
        {
            Assert.That(_database.ValidateTree(), Is.True);
        }

        [Test]
        public void BothFirstBranchesHaveARealSetOfSkills()
        {
            var warrior = _database.GetSkillsByBranch(SkillBranch.Warrior);
            var hunter = _database.GetSkillsByBranch(SkillBranch.Hunter);

            Assert.That(warrior.Count, Is.InRange(6, 10));
            Assert.That(hunter.Count, Is.InRange(6, 10));
            // Every branch that exists now has a real tree behind it.
            foreach (SkillBranch branch in System.Enum.GetValues(typeof(SkillBranch)))
            {
                Assert.That(_database.GetSkillsByBranch(branch).Count,
                    Is.GreaterThanOrEqualTo(6), $"The {branch} tree is too thin to be a build.");
            }
        }

        [Test]
        public void EverySkillActuallyDoesSomething()
        {
            foreach (var skill in _database.Skills)
            {
                // A node earns its place by changing a stat, carrying a swing, or teaching a
                // spell. Anything else would be a point spent on nothing.
                var hasEffect = skill.Effects.Count > 0;
                var hasSwing = skill.IsActive && skill.Active != null;
                var teachesSpell = !string.IsNullOrEmpty(skill.GrantedSpellId);
                Assert.That(hasEffect || hasSwing || teachesSpell, Is.True,
                    $"'{skill.SkillId}' is an empty node; the tree has none of those.");
            }
        }

        [Test]
        public void EveryTreeHasExactlyOneRootAndEveryOtherNodeHangsOffIt()
        {
            foreach (var branch in new[] { SkillBranch.Warrior, SkillBranch.Hunter })
            {
                var skills = _database.GetSkillsByBranch(branch);
                var roots = skills.FindAll(skill => skill.Prerequisites.Count == 0);
                Assert.That(roots.Count, Is.EqualTo(1),
                    $"The {branch} tree needs exactly one way in.");

                foreach (var skill in skills)
                {
                    foreach (var prerequisite in skill.Prerequisites)
                    {
                        Assert.That(_database.TryGetSkill(prerequisite, out var parent), Is.True);
                        Assert.That(parent.Branch, Is.EqualTo(branch),
                            "A tree must not depend on another tree.");
                    }
                }
            }
        }

        [Test]
        public void BothBranchesOfferOneActiveSkillToStartWith()
        {
            foreach (var branch in new[] { SkillBranch.Warrior, SkillBranch.Hunter })
            {
                var actives = _database.GetSkillsByBranch(branch)
                    .FindAll(skill => skill.IsActive);
                Assert.That(actives.Count, Is.EqualTo(1));
                var active = actives[0];
                Assert.That(active.Active, Is.Not.Null);
                Assert.That(active.Active.Cooldown, Is.GreaterThan(0f),
                    "An active skill with no cooldown would replace ordinary combat.");
                Assert.That(active.Active.Attack.StaminaCost, Is.GreaterThan(0f));
                Assert.That(active.Cost, Is.GreaterThan(1), "The strong nodes cost more.");
                Assert.That(active.RequiredLevel, Is.GreaterThan(1));
            }
        }

        [Test]
        public void TheWarriorActiveBreaksGuardsAndTheHunterActiveClosesDistance()
        {
            var groundBreaker = _database.GetSkill("warrior_ground_breaker");
            var dashStrike = _database.GetSkill("hunter_dash_strike");

            Assert.That(groundBreaker.Active.Attack.StaggerDamage,
                Is.GreaterThan(dashStrike.Active.Attack.StaggerDamage * 2f),
                "Ground Breaker is the answer to a raised shield.");
            Assert.That(groundBreaker.Active.LungeSpeed, Is.Zero, "It is planted, not mobile.");
            Assert.That(dashStrike.Active.LungeSpeed, Is.GreaterThan(0f));
            Assert.That(dashStrike.Active.Attack.Windup,
                Is.LessThan(groundBreaker.Active.Attack.Windup));
            Assert.That(dashStrike.Active.Invulnerability,
                Is.LessThan(dashStrike.Active.Attack.Duration),
                "A mobility skill must not be invulnerable for its whole length.");
        }

        [Test]
        public void TheDescriptionsAreGeneratedFromTheValuesTheGameUses()
        {
            var heavy = _database.GetSkill("warrior_heavy_mastery");
            var lines = new List<string>();

            heavy.DescribeInto(lines, 1);

            // The panel cannot promise 15% while combat delivers something else, because the
            // number in the text is read from the same field combat reads.
            var expected = Mathf.RoundToInt(
                heavy.EffectValue(SkillEffectType.HeavyDamage, 1) * 100f);
            Assert.That(lines.Exists(line => line.Contains(expected.ToString())), Is.True);
        }

        [Test]
        public void ARankedSkillIsWorthMoreAtHigherRank()
        {
            var ironBody = _database.GetSkill("warrior_iron_body");

            Assert.That(ironBody.MaxRank, Is.GreaterThan(1));
            Assert.That(ironBody.EffectValue(SkillEffectType.StaggerResistance, 3),
                Is.EqualTo(ironBody.EffectValue(SkillEffectType.StaggerResistance, 1) * 3f)
                    .Within(0.0001f));
        }

        // ---------- a broken tree ----------

        [Test]
        public void ADuplicateIdIsReported()
        {
            var database = Database(Skill("a"), Skill("a"));

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(
                "used by more than one skill"));
            database.Rebuild();
        }

        [Test]
        public void APrerequisiteThatDoesNotExistIsReported()
        {
            var database = Database(Skill("a", "missing_parent"));

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(
                "which is not in the database"));
            Assert.That(database.ValidateTree(), Is.False);
        }

        [Test]
        public void ACycleIsCaughtRatherThanHanging()
        {
            // A needs B and B needs A: neither could ever be bought.
            var database = Database(Skill("a", "b"), Skill("b", "a"));

            LogAssert.ignoreFailingMessages = true;
            var isValid = database.ValidateTree();
            LogAssert.ignoreFailingMessages = false;

            Assert.That(isValid, Is.False);
        }

        [Test]
        public void AnActiveSkillWithNoUsableDataIsReported()
        {
            // Marked active but left with a zero cooldown: it would be spammable forever and
            // replace ordinary combat entirely.
            var skill = Skill("a");
            JsonUtility.FromJsonOverwrite("{\"_type\":1,\"_active\":{\"_cooldown\":0}}", skill);
            var database = Database(skill);

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(
                "has no usable skill data"));
            Assert.That(database.ValidateTree(), Is.False);
        }

        // ---------- the save file ----------

        [Test]
        public void SkillsAndSlotsSurviveARoundTrip()
        {
            var data = GameProgressData.CreateNew();
            data.player.unlockedSkills.Add("warrior_training");
            data.player.unlockedSkills.Add("warrior_iron_body:3");
            data.player.activeSkillSlot1 = "warrior_ground_breaker";
            data.player.activeSkillSlot2 = "hunter_dash_strike";
            data.player.bonusSkillPoints = 2;

            Assert.That(SaveSystem.Write(data), Is.True);
            var loaded = SaveSystem.Read();

            Assert.That(loaded.player.unlockedSkills,
                Is.EquivalentTo(new[] { "warrior_training", "warrior_iron_body:3" }));
            Assert.That(loaded.player.activeSkillSlot1, Is.EqualTo("warrior_ground_breaker"));
            Assert.That(loaded.player.activeSkillSlot2, Is.EqualTo("hunter_dash_strike"));
            Assert.That(loaded.player.bonusSkillPoints, Is.EqualTo(2));
        }

        [Test]
        public void AFileFromBeforeSkillsLoadsWithAnEmptyTree()
        {
            // A version five file: a real character, but nothing about skills.
            File.WriteAllText(SaveSystem.FilePath,
                "{\"version\":5,\"checkpointId\":\"tomb-altar-01\","
                + "\"player\":{\"level\":8,\"experience\":40,"
                + "\"stats\":{\"strength\":9,\"dexterity\":5,\"vitality\":7,\"endurance\":6}}}");

            var loaded = SaveSystem.Read();

            Assert.That(loaded.version, Is.EqualTo(GameProgressData.CurrentVersion));
            Assert.That(loaded.player.level, Is.EqualTo(8), "The character is untouched.");
            Assert.That(loaded.player.stats.strength, Is.EqualTo(9));
            Assert.That(loaded.player.unlockedSkills, Is.Not.Null.And.Empty);
            Assert.That(loaded.player.activeSkillSlot1, Is.Empty);
            Assert.That(loaded.player.bonusSkillPoints, Is.Zero,
                "The points a level earned are worked out from the level, not stored, so an "
                + "older save needs no grant at all.");
        }

        [Test]
        public void BrokenSkillNumbersAreClampedRatherThanTrusted()
        {
            var data = GameProgressData.CreateNew();
            data.player.bonusSkillPoints = -20;
            data.player.unlockedSkills.Add("warrior_training");
            data.player.unlockedSkills.Add("warrior_training");
            File.WriteAllText(SaveSystem.FilePath, JsonUtility.ToJson(data, true));

            var loaded = SaveSystem.Read();

            Assert.That(loaded.player.bonusSkillPoints, Is.Zero,
                "Skill points can never be negative in a file.");
            Assert.That(loaded.player.unlockedSkills.Count, Is.EqualTo(1));
        }

        // ---------- helpers ----------

        private static SkillDefinition Skill(string id, params string[] prerequisites)
        {
            var skill = ScriptableObject.CreateInstance<SkillDefinition>();
            var quoted = new List<string>();
            foreach (var prerequisite in prerequisites)
            {
                quoted.Add($"\"{prerequisite}\"");
            }

            JsonUtility.FromJsonOverwrite(
                $"{{\"_skillId\":\"{id}\",\"_cost\":1,\"_maxRank\":1,\"_requiredLevel\":1,"
                + $"\"_prerequisites\":[{string.Join(",", quoted)}]}}", skill);
            return skill;
        }

        private static SkillDatabase Database(params SkillDefinition[] skills)
        {
            var database = ScriptableObject.CreateInstance<SkillDatabase>();
            typeof(SkillDatabase)
                .GetField("_skills", System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.NonPublic)
                .SetValue(database, skills);
            return database;
        }
    }
}
