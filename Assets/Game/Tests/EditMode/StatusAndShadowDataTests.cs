using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The Shadow and status layers as data: the shipped branch, what each status is shaped like,
    /// and what the save file makes of Will, shadow skills and traversal unlocks.
    /// </summary>
    public sealed class StatusAndShadowDataTests
    {
        private SkillDatabase _skills;
        private ProgressionCurve _curve;

        [SetUp]
        public void SetUp()
        {
            _skills = Load<SkillDatabase>("Assets/Game/Data/SkillDatabase.asset");
            _curve = Load<ProgressionCurve>("Assets/Game/Data/ProgressionCurve.asset");
        }

        [TearDown]
        public void TearDown()
        {
            SaveSystem.Delete();
        }

        // ---------- the Shadow branch ----------

        [Test]
        public void TheShadowBranchIsARealTree()
        {
            var shadow = _skills.GetSkillsByBranch(SkillBranch.Shadow);

            Assert.That(shadow.Count, Is.InRange(7, 12));
            Assert.That(_skills.ValidateTree(), Is.True);

            var roots = shadow.FindAll(skill => skill.Prerequisites.Count == 0);
            Assert.That(roots.Count, Is.EqualTo(1), "One way into the tree.");
            Assert.That(roots[0].SkillId, Is.EqualTo("shadow_initiate"));
        }

        [Test]
        public void TheShadowBranchCarriesItsOwnMobilityAndItsOwnBurst()
        {
            Assert.That(_skills.TryGetSkill("shadow_dash", out var dash), Is.True);
            Assert.That(_skills.TryGetSkill("shadow_strike", out var strike), Is.True);

            Assert.That(dash.IsActive, Is.True, "Shadow Dash is chosen, not a silent replacement "
                + "for the ordinary dash.");
            Assert.That(strike.IsActive, Is.True);
            Assert.That(dash.Active.Cooldown, Is.GreaterThan(0f));
            Assert.That(strike.Active.Cooldown, Is.GreaterThan(0f));
        }

        [Test]
        public void ShadowIsNotASecondHunterTree()
        {
            var shadow = _skills.GetSkillsByBranch(SkillBranch.Shadow);
            var hunter = _skills.GetSkillsByBranch(SkillBranch.Hunter);

            foreach (var skill in shadow)
            {
                Assert.That(hunter.Exists(other => other.SkillId == skill.SkillId), Is.False);
                foreach (var prerequisite in skill.Prerequisites)
                {
                    Assert.That(_skills.TryGetSkill(prerequisite, out var parent), Is.True);
                    Assert.That(parent.Branch, Is.EqualTo(SkillBranch.Shadow),
                        "A tree must not depend on another tree.");
                }
            }
        }

        [Test]
        public void EveryBranchNowHasARealTree()
        {
            foreach (SkillBranch branch in System.Enum.GetValues(typeof(SkillBranch)))
            {
                Assert.That(_skills.GetSkillsByBranch(branch).Count,
                    Is.GreaterThanOrEqualTo(6), $"The {branch} tree is too thin to be a build.");
            }
        }

        // ---------- status shapes ----------

        [Test]
        public void EachStatusIsShapedDifferentlyFromTheOthers()
        {
            var effects = new GameObject("statuses", typeof(Health), typeof(Invulnerability),
                typeof(CombatActor), typeof(StatusEffects)).GetComponent<StatusEffects>();
            try
            {
                var bleed = effects.Rule(StatusType.Bleed);
                var poison = effects.Rule(StatusType.Poison);
                var burn = effects.Rule(StatusType.Burn);
                var frost = effects.Rule(StatusType.Frost);

                // Bleed is a burst: it has no duration to tick over.
                Assert.That(bleed.duration, Is.Zero);
                Assert.That(bleed.damage, Is.GreaterThan(poison.damage),
                    "One burst is worth more than one tick of attrition.");

                // Poison is the long one, burn the short sharp one.
                Assert.That(poison.duration, Is.GreaterThan(burn.duration));
                Assert.That(burn.damage / burn.tickInterval,
                    Is.GreaterThan(poison.damage / poison.tickInterval),
                    "Burn hurts faster while it lasts.");

                // Frost controls rather than damages.
                Assert.That(frost.damage, Is.Zero);
                Assert.That(frost.slow, Is.InRange(0.1f, 0.5f));
                Assert.That(frost.reprocDelay, Is.GreaterThan(0f),
                    "Without a gap after it ends, a slow could be held forever.");
            }
            finally
            {
                Object.DestroyImmediate(effects.gameObject);
            }
        }

        [Test]
        public void EveryStatusHasAThresholdSoNoSingleHitAppliesIt()
        {
            var effects = new GameObject("statuses", typeof(Health), typeof(Invulnerability),
                typeof(CombatActor), typeof(StatusEffects)).GetComponent<StatusEffects>();
            try
            {
                foreach (StatusType type in System.Enum.GetValues(typeof(StatusType)))
                {
                    var rule = effects.Rule(type);
                    Assert.That(rule, Is.Not.Null, $"{type} has no rule.");
                    Assert.That(rule.threshold, Is.GreaterThan(1f),
                        $"{type} would apply from a single scratch.");
                    Assert.That(rule.decayRate, Is.GreaterThan(0f),
                        $"{type} buildup would never fade.");
                }
            }
            finally
            {
                Object.DestroyImmediate(effects.gameObject);
            }
        }

        // ---------- Will ----------

        [Test]
        public void TheWillGrantedByMigrationMatchesWhatANewCharacterStartsWith()
        {
            // These two live apart — plain save data cannot read the curve — so if the curve is
            // ever retuned, this is what catches the two drifting.
            File.WriteAllText(SaveSystem.FilePath,
                "{\"version\":7,\"player\":{\"level\":5,"
                + "\"stats\":{\"strength\":7,\"dexterity\":5,\"vitality\":6,"
                + "\"endurance\":5,\"intelligence\":5}}}");

            var loaded = SaveSystem.Read();

            Assert.That(loaded.player.stats.will,
                Is.EqualTo(_curve.StartingStat(CharacterStat.Will)));
        }

        [Test]
        public void WillIsAnOrdinaryAllocatableAttribute()
        {
            Assert.That(CharacterStats.Allocatable, Does.Contain(CharacterStat.Will));
            Assert.That(_curve.StartingStat(CharacterStat.Will), Is.GreaterThan(0));

            var stats = new CharacterStatBlock();
            stats.Set(CharacterStat.Will, 12);
            Assert.That(stats.Get(CharacterStat.Will), Is.EqualTo(12));
            Assert.That(stats.Clone().Get(CharacterStat.Will), Is.EqualTo(12));
        }

        [Test]
        public void EveryDeclaredAttributeCanActuallyBeRaised()
        {
            Assert.That(CharacterStats.Allocatable,
                Is.EquivalentTo(System.Enum.GetValues(typeof(CharacterStat))),
                "Nothing is declared and then quietly ignored.");
        }

        // ---------- elements and tags ----------

        [Test]
        public void DarkIsAnElementResistanceAlreadyAnswers()
        {
            var profile = new ResistanceProfile();
            profile.Set(DamageType.Dark, 0.3f);

            Assert.That(profile.Multiplier(DamageType.Dark), Is.EqualTo(0.7f).Within(0.001f));
            Assert.That(profile.Multiplier(DamageType.Physical), Is.EqualTo(1f).Within(0.001f),
                "Shadow damage is its own element, not a rename of physical.");
        }

        [Test]
        public void StatusDamageIsTaggedSoItCannotBeParriedOrBlocked()
        {
            var tick = new DamageInfo(4f, Vector2.zero, DamageType.Poison, isParryable: false,
                tags: DamageTags.Status);

            Assert.That(tick.IsStatusDamage, Is.True);
            Assert.That(tick.IsParryable, Is.False,
                "A tick of poison is not something to parry once a second.");
        }

        [Test]
        public void AHitCanCarryBuildupWithoutCarryingTheStatusItself()
        {
            var hit = new DamageInfo(30f, Vector2.right, DamageType.Fire)
                .WithStatuses(new[] { new StatusBuildup(StatusType.Burn, 25f) });

            Assert.That(hit.Statuses, Is.Not.Null.And.Length.EqualTo(1));
            Assert.That(hit.Statuses[0].type, Is.EqualTo(StatusType.Burn));
            Assert.That(hit.Statuses[0].amount, Is.EqualTo(25f));
            Assert.That(hit.IsStatusDamage, Is.False,
                "Applying buildup is not itself a tick of damage.");
        }

        // ---------- the save file ----------

        [Test]
        public void ShadowProgressAndTraversalUnlocksSurviveARoundTrip()
        {
            var data = GameProgressData.CreateNew();
            data.player.stats.will = 14;
            data.player.unlockedSkills.Add("shadow_initiate");
            data.player.unlockedSkills.Add("shadow_dash");
            data.player.unlockedAbilities.Add(AbilityIds.AirDash);
            data.player.unlockedAbilities.Add(AbilityIds.WallRun);

            Assert.That(SaveSystem.Write(data), Is.True);
            var loaded = SaveSystem.Read();

            Assert.That(loaded.player.stats.will, Is.EqualTo(14));
            Assert.That(loaded.player.unlockedSkills, Does.Contain("shadow_dash"));
            Assert.That(loaded.player.unlockedAbilities, Does.Contain(AbilityIds.AirDash));
            Assert.That(loaded.player.unlockedAbilities, Does.Contain(AbilityIds.WallRun));
        }

        [Test]
        public void NoActiveStatusEverReachesTheFile()
        {
            var data = GameProgressData.CreateNew();
            data.player.stats.will = 9;
            SaveSystem.Write(data);

            var json = File.ReadAllText(SaveSystem.FilePath).ToLowerInvariant();

            foreach (var forbidden in new[] { "bleed", "poison", "burn", "frost", "buildup" })
            {
                Assert.That(json, Does.Not.Contain(forbidden),
                    $"'{forbidden}' is combat state and must never be persisted.");
            }
        }

        [Test]
        public void AFileFromBeforeShadowLoadsWithWillDefaultedAndNothingLost()
        {
            // A version seven file: a real mage, but nothing about Will, Shadow or traversal.
            File.WriteAllText(SaveSystem.FilePath,
                "{\"version\":7,\"checkpointId\":\"tomb-altar-02\","
                + "\"player\":{\"level\":11,\"statPoints\":3,"
                + "\"stats\":{\"strength\":8,\"dexterity\":7,\"vitality\":9,"
                + "\"endurance\":6,\"intelligence\":15},"
                + "\"unlockedSkills\":[\"mage_training\",\"mage_fire_mastery\"],"
                + "\"unlockedSpells\":[\"fire_bolt\"],\"spellSlot1\":\"fire_bolt\"}}");

            var loaded = SaveSystem.Read();

            Assert.That(loaded.version, Is.EqualTo(GameProgressData.CurrentVersion));
            Assert.That(loaded.player.level, Is.EqualTo(11));
            Assert.That(loaded.player.stats.intelligence, Is.EqualTo(15),
                "The mage build is untouched.");
            Assert.That(loaded.player.unlockedSpells, Does.Contain("fire_bolt"));
            Assert.That(loaded.player.spellSlot1, Is.EqualTo("fire_bolt"));
            Assert.That(loaded.player.stats.will, Is.EqualTo(5),
                "An existing character is brought up to the starting Will rather than left at "
                + "zero, which would make them strictly worse than a fresh one.");
            Assert.That(loaded.player.unlockedAbilities, Does.Not.Contain(AbilityIds.AirDash),
                "Traversal abilities are earned, not granted by a migration.");
        }

        [Test]
        public void AnAbilityIsNeverUnlockedTwice()
        {
            var data = GameProgressData.CreateNew();

            data.player.unlockedAbilities.Add(AbilityIds.AirDash);
            data.player.unlockedAbilities.Add(AbilityIds.AirDash);
            data.Migrate();

            Assert.That(data.player.unlockedAbilities.FindAll(
                id => id == AbilityIds.AirDash).Count, Is.EqualTo(1));
        }

        private static T Load<T>(string path) where T : ScriptableObject
        {
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(asset, Is.Not.Null, $"The content build writes {path}.");
            return asset;
        }
    }
}
