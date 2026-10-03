using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The post-processing in play (visual item 2, 2026-09-28): each zone brings its grade, the
    /// settings switch turns the effects off and leaves the colour, a boss fight hardens the
    /// picture, and the title screen is graded too.
    /// </summary>
    public sealed class ScreenGradeTests
    {
        private ScreenGrade _grade;
        private PlayerMotor2D _motor;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            yield return TestWorld.Load();
            _grade = Camera.main.GetComponent<ScreenGrade>();
            _motor = Object.FindAnyObjectByType<PlayerMotor2D>();
            Object.FindAnyObjectByType<PlayerCharacter>().GetComponent<CombatActor>()
                .SetInvulnerable(ImmunitySource.Script, 99f);
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            GameSettings.ResetToDefaults();
            GamePause.Clear();
            SaveSystem.Delete();
            GameTimeScale.Reset();
        }

        [UnityTest]
        public IEnumerator TheLevelIsGradedAsTheTombFromTheStart()
        {
            yield return null;
            Assert.That(_grade, Is.Not.Null, "The level camera carries the grade.");
            Assert.That(Camera.main.GetUniversalAdditionalCameraData().renderPostProcessing, Is.True);
            Assert.That(_grade.ZoneWeight("zone_forgotten_tomb"), Is.EqualTo(1f));
            Assert.That(_grade.ZoneWeight("zone_withered_forest"), Is.Zero);
            Assert.That(_grade.Effects.weight, Is.EqualTo(1f));
            Assert.That(_grade.BossAmount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator CrossingIntoTheForestFadesItsGradeIn()
        {
            _motor.ResetTo(new Vector2(100f, 2f));
            yield return Realtime(0.3f);
            var midway = _grade.ZoneWeight("zone_withered_forest");
            yield return Realtime(2.5f);

            Assert.That(midway, Is.LessThan(1f), "The grade fades across, it does not cut.");
            Assert.That(_grade.ZoneWeight("zone_withered_forest"), Is.EqualTo(1f));
            Assert.That(_grade.ZoneWeight("zone_forgotten_tomb"), Is.Zero);
        }

        [UnityTest]
        public IEnumerator TheSwitchTurnsTheEffectsOffAndLeavesTheColour()
        {
            GameSettings.Set(d => d.postEffects = false);
            yield return null;
            yield return null;

            Assert.That(_grade.Effects.weight, Is.Zero, "No bloom, no vignette.");
            Assert.That(_grade.ZoneWeight("zone_forgotten_tomb"), Is.EqualTo(1f), "The zone keeps its colour.");

            GameSettings.Set(d => d.postEffects = true);
            yield return null;
            yield return null;
            Assert.That(_grade.Effects.weight, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator ABossFightHardensThePicture()
        {
            var boss = Object.FindObjectsByType<BossController>();
            _motor.ResetTo(TombScene.ArenaEntry());
            var until = Time.realtimeSinceStartup + 4f;
            while (Time.realtimeSinceStartup < until && !System.Array.Exists(boss, b => b.IsFighting))
            {
                yield return null;
            }

            Assert.That(System.Array.Exists(boss, b => b.IsFighting), Is.True, "The Guardian's fight began.");
            yield return Realtime(1.6f);
            Assert.That(_grade.BossAmount, Is.EqualTo(0.6f).Within(0.05f), "Phase one: some of the boss grade.");
            Assert.That(_grade.Boss.weight, Is.EqualTo(_grade.BossAmount));
        }

        [UnityTest]
        public IEnumerator TheTitleScreenIsGradedToo()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;
            var grade = Camera.main.GetComponent<ScreenGrade>();

            Assert.That(grade, Is.Not.Null);
            Assert.That(grade.ZoneWeight("zone_forgotten_tomb"), Is.EqualTo(1f));
            Assert.That(grade.Effects.weight, Is.EqualTo(1f));
        }

        private static IEnumerator Realtime(float seconds)
        {
            var until = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < until)
            {
                yield return null;
            }
        }
    }
}
