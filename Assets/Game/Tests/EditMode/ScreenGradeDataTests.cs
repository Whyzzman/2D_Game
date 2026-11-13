using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The post-processing as data (visual item 2, 2026-09-28): restrained, no film grain, the
    /// switch holding bloom and vignette only, the zones holding colour only, a boss a little harsher.
    /// </summary>
    public sealed class ScreenGradeDataTests
    {
        private const string Folder = "Assets/Game/Data/Grading";

        [Test]
        public void ThePictureEffectsAreOnByDefaultEvenForAnOldSettingsFile()
        {
            Assert.That(new SettingsData().postEffects, Is.True);
            var old = JsonUtility.FromJson<SettingsData>("{\"version\":1,\"masterVolume\":0.5}");
            Assert.That(old.postEffects, Is.True, "A file written before the switch existed keeps the effects.");
        }

        [Test]
        public void TheSwitchHoldsBloomAndVignetteAndNothingElse()
        {
            var effects = Load("grade_effects");
            Assert.That(effects.TryGet(out Bloom bloom) && bloom.active, Is.True);
            Assert.That(effects.TryGet(out Vignette vignette) && vignette.active, Is.True);
            Assert.That(effects.Has<ColorAdjustments>(), Is.False,
                "Colour belongs to the zones and stays when the effects are off.");
            AssertNoFilmLook(effects);
        }

        [TestCase("grade_tomb")]
        [TestCase("grade_forest")]
        [TestCase("grade_boss")]
        public void TheGradesCarryColourOnly(string name)
        {
            var grade = Load(name);
            Assert.That(grade.Has<ColorAdjustments>(), Is.True);
            Assert.That(grade.Has<Bloom>(), Is.False, "Glow is the switchable part.");
            Assert.That(grade.Has<Vignette>(), Is.False, "So is the vignette.");
            AssertNoFilmLook(grade);
        }

        [Test]
        public void TheLookStaysRestrained()
        {
            Load("grade_effects").TryGet(out Bloom bloom);
            Load("grade_effects").TryGet(out Vignette vignette);
            Assert.That(bloom.intensity.value, Is.LessThanOrEqualTo(0.8f));
            Assert.That(bloom.threshold.value, Is.GreaterThanOrEqualTo(0.7f),
                "Only the bright things glow: flames, runes, the moon, magic.");
            Assert.That(vignette.intensity.value, Is.LessThanOrEqualTo(0.3f));

            foreach (var zone in new[] { "grade_tomb", "grade_forest" })
            {
                Load(zone).TryGet(out ColorAdjustments zoneColour);
                Load("grade_boss").TryGet(out ColorAdjustments bossColour);
                Assert.That(Mathf.Abs(zoneColour.contrast.value), Is.LessThanOrEqualTo(12f), zone);
                Assert.That(Mathf.Abs(zoneColour.saturation.value), Is.LessThanOrEqualTo(12f), zone);
                Assert.That(bossColour.contrast.value - zoneColour.contrast.value, Is.InRange(4f, 14f),
                    $"A boss fight is a little harsher than {zone}, not a different picture.");
            }
        }

        [Test]
        public void ABossWantsSomeOfTheGradeInPhaseOneAndAllOfItInPhaseTwo()
        {
            Assert.That(ScreenGrade.BossTarget(false, 2, 0.6f), Is.Zero);
            Assert.That(ScreenGrade.BossTarget(true, 1, 0.6f), Is.EqualTo(0.6f));
            Assert.That(ScreenGrade.BossTarget(true, 2, 0.6f), Is.EqualTo(1f));
        }

        private static void AssertNoFilmLook(VolumeProfile profile)
        {
            // The user's choice: no film grain, and none of its relatives either.
            Assert.That(profile.Has<FilmGrain>(), Is.False, profile.name);
            Assert.That(profile.Has<ChromaticAberration>(), Is.False, profile.name);
            Assert.That(profile.Has<LensDistortion>(), Is.False, profile.name);
        }

        private static VolumeProfile Load(string name)
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>($"{Folder}/{name}.asset");
            Assert.That(profile, Is.Not.Null, $"{name} is written by SceneScreenGrade.");
            return profile;
        }
    }
}
