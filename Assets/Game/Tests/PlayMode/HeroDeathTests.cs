using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The hero's death as a moment (GAME_FEEL_IDEAS item 22): the world slows and gives the time
    /// back, the body darkens and comes apart into shadows, and the hero returns whole and opaque.
    /// </summary>
    public sealed class HeroDeathTests
    {
        private PlayerCharacter _player;
        private PlayerRespawner _respawner;
        private HeroDeath _death;
        private SpriteRenderer _body;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            yield return TestWorld.Load();
            _player = Object.FindAnyObjectByType<PlayerCharacter>();
            _respawner = Object.FindAnyObjectByType<PlayerRespawner>();
            _death = _player.GetComponent<HeroDeath>();
            _body = _player.GetComponentInChildren<PlayerAnimationController>().Body;
        }

        [TearDown]
        public void TearDown()
        {
            FeedbackSettings.HitStopMultiplier = 1f;
            GamePause.Clear();
            SaveSystem.Delete();
            GameTimeScale.Reset();
        }

        [UnityTest]
        public IEnumerator DeathSlowsTheWorldDissolvesTheBodyAndTheHeroReturnsWhole()
        {
            Assert.That(_death, Is.Not.Null, "The player ships with its death presentation.");
            var colour = _body.color;

            _respawner.Kill();
            yield return null;
            Assert.That(_death.IsDissolving, Is.True);
            Assert.That(Time.timeScale, Is.LessThan(1f), "The world slows as the hero falls.");

            var started = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - started < 1f) { yield return null; }
            Assert.That(Time.timeScale, Is.EqualTo(1f).Within(0.001f), "The slow is handed back.");
            Assert.That(_death.ActiveWisps, Is.GreaterThan(0), "The body leaves as shadows.");
            Assert.That(_body.color.a, Is.LessThan(colour.a), "The body thins away.");

            yield return WaitForRespawn();
            Assert.That(_death.IsDissolving, Is.False);
            started = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - started < 2f) { yield return null; }
            Assert.That(_body.color.a, Is.EqualTo(colour.a).Within(0.01f),
                "Back at the altar the hero is opaque again, after the spawn blink.");
            Assert.That(_body.color.r, Is.EqualTo(colour.r).Within(0.01f), "And no longer dark.");
            Assert.That(_death.ActiveWisps, Is.Zero);
        }

        [UnityTest]
        public IEnumerator WithTheFreezeSettingAtZeroTheWorldDoesNotSlow()
        {
            FeedbackSettings.HitStopMultiplier = 0f;
            _respawner.Kill();
            yield return null;
            Assert.That(Time.timeScale, Is.EqualTo(1f).Within(0.001f),
                "The slow belongs to the hit-stop accessibility setting.");
            yield return WaitForRespawn();
        }

        private IEnumerator WaitForRespawn()
        {
            var started = Time.realtimeSinceStartup;
            while ((!_player.IsAlive || _respawner.IsSequenceRunning)
                && Time.realtimeSinceStartup - started < 6f)
            {
                yield return null;
            }

            Assert.That(_player.IsAlive, Is.True, "The player never came back.");
        }
    }
}
