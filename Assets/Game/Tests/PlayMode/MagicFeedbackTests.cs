using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// Magic and altars as they are shown (item 6 of the 2026-09-27 list): the spell gathering in
    /// the hand, the spell coming apart when a blow breaks it, and the altar catching light. Each
    /// only reads the game; the checks that it changes nothing sit beside the ones that it shows.
    /// </summary>
    public sealed class MagicFeedbackTests
    {
        private PlayerCharacter _player;
        private PlayerMotor2D _motor;
        private SpellCaster _caster;
        private PlayerSpellbook _spellbook;
        private Mana _mana;
        private CastCharge _charge;
        private CombatFeedbackService _feedback;
        private readonly List<FeedbackKind> _played = new List<FeedbackKind>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            yield return TestWorld.Load();
            _player = Object.FindAnyObjectByType<PlayerCharacter>();
            _motor = _player.GetComponent<PlayerMotor2D>();
            _caster = _player.GetComponent<SpellCaster>();
            _mana = _player.GetComponent<Mana>();
            _spellbook = Object.FindAnyObjectByType<PlayerSpellbook>();
            _charge = _player.GetComponentInChildren<CastCharge>(true);
            _feedback = Object.FindAnyObjectByType<CombatFeedbackService>();
            _feedback.Played += OnPlayed;
            yield return new WaitForFixedUpdate();
            _played.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            if (_feedback != null) { _feedback.Played -= OnPlayed; }
            SpellProjectile.ClearAll(Faction.Player);
            GamePause.Clear();
            SaveSystem.Delete();
            GameTimeScale.Reset();
        }

        private void OnPlayed(FeedbackKind kind, Vector2 position) => _played.Add(kind);

        // ---------- the charge in the hand ----------

        [UnityTest]
        public IEnumerator TheSpellGathersInTheHandInItsColourAndGrowsTowardsRelease()
        {
            Assert.That(_charge, Is.Not.Null, "The player ships with the charge in the hand.");
            LearnAndEquip("fire_bolt", 0);
            var spell = _spellbook.SlotSpell(0);
            yield return Land();
            Assert.That(_charge.IsCharging, Is.False);

            Assert.That(_player.TryCastSpell(0), Is.EqualTo(CastResult.Casting));
            yield return null;
            yield return null;
            Assert.That(_charge.IsCharging, Is.True);
            var early = _charge.Size;
            var colour = _charge.Color;
            Assert.That(colour.r, Is.EqualTo(spell.Color.r).Within(0.01f));
            Assert.That(colour.g, Is.EqualTo(spell.Color.g).Within(0.01f),
                "The charge wears the spell's own colour.");

            yield return new WaitForSeconds(_caster.CastTimeOf(spell) * 0.8f);
            Assert.That(_charge.Size, Is.GreaterThan(early * 1.4f), "It grows towards release.");

            yield return new WaitForSeconds(_caster.CastTimeOf(spell) * 0.3f + 0.05f);
            Assert.That(_charge.IsCharging, Is.False, "Released: the spell takes over.");
            Assert.That(_played, Has.No.Member(FeedbackKind.CastFizzle),
                "A spell that leaves the hand did not fall apart.");
        }

        [UnityTest]
        public IEnumerator TheChargeSitsInFrontOfTheBodyOnTheFacingSide()
        {
            LearnAndEquip("fire_bolt", 0);
            yield return Land();
            _player.TryCastSpell(0);
            yield return null;

            var offset = _charge.transform.position.x - _player.transform.position.x;
            Assert.That(offset * _motor.FacingDirection, Is.GreaterThan(0.1f),
                "It gathers in the reaching hand, ahead of the body.");
        }

        // ---------- a broken cast ----------

        [UnityTest]
        public IEnumerator ABrokenCastFallsApartAndCostsNoMana()
        {
            LearnAndEquip("fire_bolt", 0);
            yield return Land();
            var mana = _mana.Current;
            _player.TryCastSpell(0);
            yield return new WaitForSeconds(0.1f);

            Assert.That(_caster.Interrupt(), Is.True);
            Assert.That(_played, Has.Member(FeedbackKind.CastFizzle), "The lost spell is shown.");
            Assert.That(_charge.IsCharging, Is.False);
            Assert.That(_mana.Current, Is.EqualTo(mana).Within(0.01f), "... and it cost nothing.");

            yield return new WaitForSeconds(0.3f);
            Assert.That(_charge.GetComponent<SpriteRenderer>().enabled, Is.False,
                "Nothing is left in the hand once the pop is over.");
        }

        [UnityTest]
        public IEnumerator TheFizzleIsHeardButNeverShakesOrFreezes()
        {
            var profile = _feedback.Library.Profile(FeedbackKind.CastFizzle);

            Assert.That(profile, Is.Not.Null);
            Assert.That(profile.HasBurst, Is.True);
            Assert.That(profile.HitStop, Is.Zero, "Losing a spell is a setback, not a blow.");
            Assert.That(profile.Shake, Is.EqualTo(ShakeProfile.None));
            Assert.That(CombatAudioBinder.ForFeedback(FeedbackKind.CastFizzle),
                Is.EqualTo(AudioId.CastFizzle));
            yield break;
        }

        // ---------- lighting an altar ----------

        [UnityTest]
        public IEnumerator LightingAnAltarFlashesAndSendsAWaveOfLightThatSettles()
        {
            var altar = TombScene.TombAltars()[0];
            var visual = altar.GetComponent<CheckpointVisual>();
            var light = altar.GetComponentInChildren<UnityEngine.Rendering.Universal.Light2D>();
            Assert.That(altar.IsActivated, Is.False);

            _player.GetComponent<PlayerCombatStatus>().Clear();
            _motor.ResetTo(new Vector2(altar.transform.position.x, altar.transform.position.y + 0.1f));
            yield return new WaitForFixedUpdate();
            altar.Interact(_player);
            yield return null;
            yield return null;

            Assert.That(_played, Has.Member(FeedbackKind.AltarKindle));
            Assert.That(visual.IsKindling, Is.True);
            var peak = light.intensity;

            yield return new WaitForSeconds(CheckpointVisual.KindleTime + 0.3f);
            Assert.That(visual.IsKindling, Is.False);
            Assert.That(peak, Is.GreaterThan(light.intensity * 1.5f),
                "The light surges when the altar catches, then sinks to its lit glow.");
        }

        [UnityTest]
        public IEnumerator AnAltarAlreadyLitFromTheSaveDoesNotFlashOnLoad()
        {
            foreach (var altar in TombScene.TombAltars())
            {
                Assert.That(altar.GetComponent<CheckpointVisual>().IsKindling, Is.False);
            }

            Assert.That(_played, Has.No.Member(FeedbackKind.AltarKindle));
            yield break;
        }

        // ---------- helpers ----------

        private void LearnAndEquip(string spellId, int slot)
        {
            _spellbook.Learn(spellId);
            _spellbook.AssignSlot(slot, spellId);
        }

        private IEnumerator Land()
        {
            var started = Time.time;
            while (!_motor.IsGrounded && Time.time - started < 3f)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(_motor.IsGrounded, Is.True, "The player never landed.");
        }
    }
}
