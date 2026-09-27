using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The magic layer as data: resistance arithmetic, how intelligence scales a spell, the
    /// shipped spell list and the Mage branch, and what the save file makes of it all.
    /// </summary>
    public sealed class MagicDataTests
    {
        private SpellDatabase _spells;
        private SkillDatabase _skills;
        private ProgressionCurve _curve;

        [SetUp]
        public void SetUp()
        {
            _spells = Load<SpellDatabase>("Assets/Game/Data/SpellDatabase.asset");
            _skills = Load<SkillDatabase>("Assets/Game/Data/SkillDatabase.asset");
            _curve = Load<ProgressionCurve>("Assets/Game/Data/ProgressionCurve.asset");
        }

        [TearDown]
        public void TearDown()
        {
            SaveSystem.Delete();
        }

        // ---------- resistance ----------

        [Test]
        public void ResistanceLessensAHitAndWeaknessSharpensIt()
        {
            var profile = new ResistanceProfile();
            profile.Set(DamageType.Fire, 0.25f);
            profile.Set(DamageType.Frost, -0.25f);

            Assert.That(profile.Multiplier(DamageType.Fire), Is.EqualTo(0.75f).Within(0.001f));
            Assert.That(profile.Multiplier(DamageType.Frost), Is.EqualTo(1.25f).Within(0.001f));
            Assert.That(profile.Multiplier(DamageType.Magic), Is.EqualTo(1f).Within(0.001f),
                "An element nothing was said about is taken in full.");
        }

        [Test]
        public void NoResistanceCanMakeACreatureImmuneOrMakeAHitHeal()
        {
            var profile = new ResistanceProfile();
            profile.Set(DamageType.Fire, 5f);
            profile.Set(DamageType.Frost, -50f);

            Assert.That(profile.Get(DamageType.Fire),
                Is.EqualTo(ResistanceProfile.MaxResistance));
            Assert.That(profile.Multiplier(DamageType.Fire), Is.GreaterThan(0f),
                "Even the best ward leaves something through.");
            Assert.That(profile.Multiplier(DamageType.Frost), Is.LessThanOrEqualTo(2f),
                "And the worst weakness does not multiply a hit without limit.");
            Assert.That(profile.Multiplier(DamageType.Frost), Is.GreaterThan(1f));
        }

        [Test]
        public void EveryElementIsAnsweredSeparately()
        {
            var profile = new ResistanceProfile();
            foreach (var element in new[]
                     {
                         DamageType.Physical, DamageType.Fire, DamageType.Frost,
                         DamageType.Lightning, DamageType.Dark, DamageType.Magic
                     })
            {
                profile.Set(element, 0.5f);
                Assert.That(profile.Get(element), Is.EqualTo(0.5f).Within(0.001f));
                profile.Set(element, 0f);
            }
        }

        // ---------- spell scaling ----------

        [Test]
        public void IntelligenceRaisesASpellsDamage()
        {
            var bolt = _spells.GetSpell("fire_bolt");

            var novice = MagicDamageCalculator.Evaluate(bolt, _curve, 5);
            var adept = MagicDamageCalculator.Evaluate(bolt, _curve, 20);

            Assert.That(adept.Total, Is.GreaterThan(novice.Total));
            Assert.That(novice.Total, Is.GreaterThanOrEqualTo(bolt.BaseDamage),
                "Intelligence only ever adds.");
            Assert.That(adept.IntelligenceBonus, Is.GreaterThan(novice.IntelligenceBonus));
        }

        [Test]
        public void ASpellThatScalesBetterGainsMoreFromTheSamePoint()
        {
            var burst = _spells.GetSpell("arcane_burst");
            var shard = _spells.GetSpell("frost_shard");
            Assert.That(burst.IntelligenceScaling, Is.GreaterThan(shard.IntelligenceScaling));

            var burstGain = Gain(burst);
            var shardGain = Gain(shard);

            Assert.That(burstGain, Is.GreaterThan(shardGain));
        }

        [Test]
        public void FifteenPointsAreFeltWithoutBeingDecisive()
        {
            var bolt = _spells.GetSpell("fire_bolt");
            var before = MagicDamageCalculator.Evaluate(bolt, _curve, 5).Total;
            var after = MagicDamageCalculator.Evaluate(bolt, _curve, 20).Total;

            Assert.That(after / before, Is.InRange(1.2f, 2f));
        }

        [Test]
        public void SkillsMultiplyOnTopOfIntelligence()
        {
            var bolt = _spells.GetSpell("fire_bolt");
            var plain = MagicDamageCalculator.Evaluate(bolt, _curve, 10);
            var trained = MagicDamageCalculator.Evaluate(bolt, _curve, 10, 1.25f);

            Assert.That(trained.Total, Is.EqualTo(plain.Total * 1.25f).Within(0.01f));
            Assert.That(trained.SpellBase, Is.EqualTo(plain.SpellBase),
                "The multiplier is a multiplier, not a different spell.");
        }

        [Test]
        public void ASpellHitCarriesItsElementAndItsTags()
        {
            var bolt = _spells.GetSpell("fire_bolt");
            var burst = _spells.GetSpell("arcane_burst");

            var boltHit = MagicDamageCalculator.BuildDamage(bolt, 30f, null, null, Vector2.right);
            var burstHit = MagicDamageCalculator.BuildDamage(burst, 40f, null, null, Vector2.right);

            Assert.That(boltHit.Type, Is.EqualTo(DamageType.Fire));
            Assert.That(boltHit.HasTag(DamageTags.Spell), Is.True);
            Assert.That(boltHit.HasTag(DamageTags.Projectile), Is.True);
            Assert.That(boltHit.HasTag(DamageTags.Area), Is.False);
            Assert.That(burstHit.HasTag(DamageTags.Area), Is.True);
            Assert.That(boltHit.IsParryable, Is.False, "A bolt is not something to parry.");
        }

        [Test]
        public void TagsSurviveBeingAimedAndScaled()
        {
            var bolt = _spells.GetSpell("fire_bolt");
            var hit = MagicDamageCalculator.BuildDamage(bolt, 30f, null, null, Vector2.right);

            var aimed = hit.AimedAt(Vector2.one, Vector2.left).Scaled(50f, 5f, 5f, true);

            Assert.That(aimed.HasTag(DamageTags.Spell), Is.True);
            Assert.That(aimed.Type, Is.EqualTo(DamageType.Fire));
            Assert.That(aimed.Amount, Is.EqualTo(50f));
            Assert.That(aimed.IsCritical, Is.True);
        }

        // ---------- the shipped spells ----------

        [Test]
        public void TheShippedSpellsAreSound()
        {
            Assert.That(_spells.ValidateSpells(), Is.True);
            Assert.That(_spells.Spells.Count, Is.GreaterThanOrEqualTo(3));
        }

        [Test]
        public void EachSpellSolvesADifferentProblem()
        {
            var bolt = _spells.GetSpell("fire_bolt");
            var shard = _spells.GetSpell("frost_shard");
            var burst = _spells.GetSpell("arcane_burst");
            var shield = _spells.GetSpell("mana_shield");

            // The cheap reliable one against the one that trades damage for control.
            Assert.That(shard.BaseDamage, Is.LessThan(bolt.BaseDamage));
            Assert.That(shard.StaggerDamage, Is.GreaterThan(bolt.StaggerDamage));

            // The expensive one costs more in every currency it has.
            Assert.That(burst.ManaCost, Is.GreaterThan(bolt.ManaCost));
            Assert.That(burst.CastTime, Is.GreaterThan(bolt.CastTime));
            Assert.That(burst.Cooldown, Is.GreaterThan(bolt.Cooldown));
            Assert.That(burst.Type, Is.EqualTo(SpellType.Area));
            Assert.That(burst.CanCastInAir, Is.False, "Both feet on the ground for that one.");

            // And the one that deals nothing at all.
            Assert.That(shield.Type, Is.EqualTo(SpellType.SelfBuff));
            Assert.That(shield.BaseDamage, Is.Zero);
            Assert.That(shield.DamageReduction, Is.InRange(0.05f, 0.5f),
                "A ward, not invulnerability.");
            Assert.That(shield.BuffDuration, Is.GreaterThan(0f));
            Assert.That(shield.Cooldown, Is.GreaterThan(shield.BuffDuration),
                "It cannot be held up permanently.");
        }

        [Test]
        public void EveryElementUsedIsOneResistanceKnowsAbout()
        {
            var profile = new ResistanceProfile();
            foreach (var spell in _spells.Spells)
            {
                profile.Set(spell.Element, 0.5f);
                Assert.That(profile.Get(spell.Element), Is.EqualTo(0.5f).Within(0.001f),
                    $"'{spell.SpellId}' uses an element resistance cannot answer.");
                profile.Set(spell.Element, 0f);
            }
        }

        [Test]
        public void ABrokenSpellIsReported()
        {
            var spell = ScriptableObject.CreateInstance<SpellDefinition>();
            JsonUtility.FromJsonOverwrite(
                "{\"_spellId\":\"broken\",\"_type\":0,\"_projectileSpeed\":0}", spell);

            UnityEngine.TestTools.LogAssert.Expect(LogType.Error,
                new System.Text.RegularExpressions.Regex("would never move"));
            Assert.That(spell.Validate(), Is.False);
        }

        [Test]
        public void ASpellDescriptionShowsTheNumbersItReallyUses()
        {
            var bolt = _spells.GetSpell("fire_bolt");
            var lines = new List<string>();

            bolt.DescribeInto(lines, 42f, bolt.ManaCost, bolt.CastTime);

            Assert.That(lines.Exists(line => line.Contains("42")), Is.True,
                "The current damage is shown, not only the base.");
            Assert.That(lines.Exists(line => line.Contains("Вогонь")), Is.True);
        }

        // ---------- the Mage branch ----------

        [Test]
        public void TheMageBranchIsARealTreeThatTeachesTheSpells()
        {
            var mage = _skills.GetSkillsByBranch(SkillBranch.Mage);

            Assert.That(mage.Count, Is.InRange(7, 12));
            Assert.That(_skills.ValidateTree(), Is.True);

            var roots = mage.FindAll(skill => skill.Prerequisites.Count == 0);
            Assert.That(roots.Count, Is.EqualTo(1));
            Assert.That(roots[0].SkillId, Is.EqualTo("mage_training"));

            // Every damaging spell has a node that teaches it.
            foreach (var spellId in new[] { "fire_bolt", "frost_shard", "arcane_burst" })
            {
                Assert.That(mage.Exists(skill => skill.GrantedSpellId == spellId), Is.True,
                    $"Nothing in the tree teaches '{spellId}'.");
            }
        }

        [Test]
        public void EveryTaughtSpellExists()
        {
            foreach (var skill in _skills.Skills)
            {
                if (!string.IsNullOrEmpty(skill.GrantedSpellId))
                {
                    Assert.That(_spells.Contains(skill.GrantedSpellId), Is.True,
                        $"'{skill.SkillId}' teaches a spell that is not in the build.");
                }
            }
        }

        [Test]
        public void TheOtherTreesAreUntouchedByMagic()
        {
            foreach (var branch in new[] { SkillBranch.Warrior, SkillBranch.Hunter })
            {
                foreach (var skill in _skills.GetSkillsByBranch(branch))
                {
                    Assert.That(skill.GrantedSpellId, Is.Empty,
                        "Warrior and Hunter teach no spells; magic is its own tree.");
                }
            }
        }

        // ---------- mana from the blade ----------

        [Test]
        public void ASwingPaysInFullOnFleshHalfOnAShieldAndNothingWhenTurned()
        {
            Assert.That(WeaponManaGain.Evaluate(DamageOutcome.Hit, Faction.Enemy, 10f), Is.EqualTo(10f));
            Assert.That(WeaponManaGain.Evaluate(DamageOutcome.Hit, Faction.Neutral, 10f), Is.EqualTo(10f),
                "The training dummy pays too, so the rhythm can be practised.");
            Assert.That(WeaponManaGain.Evaluate(DamageOutcome.Blocked, Faction.Enemy, 10f), Is.EqualTo(5f));
            Assert.That(WeaponManaGain.Evaluate(DamageOutcome.Ignored, Faction.Enemy, 10f), Is.Zero,
                "A hit the target ignored earns nothing.");
            Assert.That(WeaponManaGain.Evaluate(DamageOutcome.Parried, Faction.Enemy, 10f), Is.Zero);
            Assert.That(WeaponManaGain.Evaluate(DamageOutcome.Hit, Faction.Player, 10f), Is.Zero);
        }

        [Test]
        public void TwoSwingsBuyTheCheapestSpell()
        {
            var cheapest = float.MaxValue;
            foreach (var spell in _spells.Spells)
            {
                cheapest = Mathf.Min(cheapest, spell.ManaCost);
            }

            var player = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Game/Prefabs/Player.prefab");
            var gain = player.GetComponent<WeaponManaGain>();
            Assert.That(gain, Is.Not.Null, "The content build puts mana from the blade on the player.");
            Assert.That(gain.PerHit * 2f, Is.GreaterThanOrEqualTo(cheapest),
                "The blade has to keep a caster casting, as the nail does in Hollow Knight.");
        }

        // ---------- mana from intelligence ----------

        [Test]
        public void IntelligenceBuysMana()
        {
            var start = _curve.StartingStats().intelligence;

            Assert.That(_curve.ManaFor(start), Is.EqualTo(_curve.BaseMana));
            Assert.That(_curve.ManaFor(start + 10), Is.GreaterThan(_curve.ManaFor(start)));
            Assert.That(_curve.ManaFor(0), Is.EqualTo(_curve.BaseMana),
                "Below the starting value there is nothing to take away.");
        }

        [Test]
        public void ManaAlsoSlowsDownAtTheBreakpoints()
        {
            var early = _curve.ManaFor(15) - _curve.ManaFor(10);
            var late = _curve.ManaFor(55) - _curve.ManaFor(50);

            Assert.That(late, Is.LessThan(early));
            Assert.That(late, Is.GreaterThan(0f));
        }

        // ---------- the save file ----------

        [Test]
        public void ANewCharacterHasIntelligenceAndNoSpells()
        {
            var data = GameProgressData.CreateNew();

            Assert.That(data.player.unlockedSpells, Is.Empty);
            Assert.That(data.player.spellSlot1, Is.Empty);
            Assert.That(data.version, Is.EqualTo(GameProgressData.CurrentVersion));
        }

        [Test]
        public void SpellsAndSlotsSurviveARoundTrip()
        {
            var data = GameProgressData.CreateNew();
            data.player.stats.intelligence = 17;
            data.player.unlockedSpells.Add("fire_bolt");
            data.player.unlockedSpells.Add("frost_shard");
            data.player.spellSlot1 = "fire_bolt";
            data.player.spellSlot3 = "frost_shard";

            Assert.That(SaveSystem.Write(data), Is.True);
            var loaded = SaveSystem.Read();

            Assert.That(loaded.player.stats.intelligence, Is.EqualTo(17));
            Assert.That(loaded.player.unlockedSpells,
                Is.EquivalentTo(new[] { "fire_bolt", "frost_shard" }));
            Assert.That(loaded.player.spellSlot1, Is.EqualTo("fire_bolt"));
            Assert.That(loaded.player.spellSlot2, Is.Empty);
            Assert.That(loaded.player.spellSlot3, Is.EqualTo("frost_shard"));
        }

        [Test]
        public void CurrentManaIsNeverWrittenToTheFile()
        {
            var data = GameProgressData.CreateNew();
            data.player.stats.intelligence = 12;
            SaveSystem.Write(data);

            var json = File.ReadAllText(SaveSystem.FilePath).ToLowerInvariant();

            Assert.That(json, Does.Not.Contain("currentmana"));
            Assert.That(json, Does.Not.Contain("\"mana\""),
                "Mana is a combat resource; a load always starts the player full.");
        }

        [Test]
        public void AFileFromBeforeMagicLoadsWithNoSpellsAndKeepsEverythingElse()
        {
            // A version six file: a real character with skills, but nothing about magic.
            File.WriteAllText(SaveSystem.FilePath,
                "{\"version\":6,\"checkpointId\":\"tomb-altar-01\","
                + "\"player\":{\"level\":9,\"statPoints\":2,"
                + "\"stats\":{\"strength\":12,\"dexterity\":6,\"vitality\":9,\"endurance\":7},"
                + "\"unlockedSkills\":[\"warrior_training\",\"warrior_iron_body:2\"]}}");

            var loaded = SaveSystem.Read();

            Assert.That(loaded.version, Is.EqualTo(GameProgressData.CurrentVersion));
            Assert.That(loaded.player.level, Is.EqualTo(9));
            Assert.That(loaded.player.statPoints, Is.EqualTo(2));
            Assert.That(loaded.player.stats.strength, Is.EqualTo(12));
            Assert.That(loaded.player.unlockedSkills.Count, Is.EqualTo(2),
                "The skills that were learned are untouched.");
            Assert.That(loaded.player.unlockedSpells, Is.Not.Null.And.Empty);
            Assert.That(loaded.player.spellSlot1, Is.Empty);
            Assert.That(loaded.player.stats.intelligence, Is.Zero,
                "Intelligence stays unset here; the curve fills it in once the game runs.");
        }

        [Test]
        public void DuplicateSpellsInAFileAreCleanedUp()
        {
            var data = GameProgressData.CreateNew();
            data.player.unlockedSpells.Add("fire_bolt");
            data.player.unlockedSpells.Add("fire_bolt");
            data.player.unlockedSpells.Add(string.Empty);
            File.WriteAllText(SaveSystem.FilePath, JsonUtility.ToJson(data, true));

            var loaded = SaveSystem.Read();

            Assert.That(loaded.player.unlockedSpells.Count, Is.EqualTo(1));
        }

        // ---------- helpers ----------

        private float Gain(SpellDefinition spell)
        {
            return MagicDamageCalculator.Evaluate(spell, _curve, 20).Total
                - MagicDamageCalculator.Evaluate(spell, _curve, 5).Total;
        }

        private static T Load<T>(string path) where T : ScriptableObject
        {
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(asset, Is.Not.Null, $"The content build writes {path}.");
            return asset;
        }
    }
}
