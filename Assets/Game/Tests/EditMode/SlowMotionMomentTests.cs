using NUnit.Framework;
using UnityEngine;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The parry's slow world (GAME_FEEL_IDEAS item 9) as a rule: it borrows the base time scale,
    /// waits out the freeze, obeys the accessibility settings and never takes the scale back from
    /// someone who changed it meanwhile.
    /// </summary>
    public sealed class SlowMotionMomentTests
    {
        private SlowMotionMoment _moment;

        [SetUp]
        public void SetUp()
        {
            FeedbackSettings.ResetToDefaults();
            GameTimeScale.Reset();
            _moment = new SlowMotionMoment();
        }

        [TearDown]
        public void TearDown()
        {
            _moment.Stop();
            FeedbackSettings.ResetToDefaults();
            GameTimeScale.Reset();
        }

        [Test]
        public void AParrySlowsTheWorldAndGivesTimeBackAfterItsLength()
        {
            _moment.Begin(0.3f, 0.12f);
            Assert.That(Time.timeScale, Is.EqualTo(0.3f).Within(0.001f));
            Assert.That(_moment.IsRunning, Is.True);

            _moment.Tick(0.1f);
            Assert.That(Time.timeScale, Is.EqualTo(0.3f).Within(0.001f));

            _moment.Tick(0.03f);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(_moment.IsRunning, Is.False);
        }

        [Test]
        public void TheSlowWaitsOutTheFreezeInsteadOfHidingUnderIt()
        {
            _moment.Begin(0.3f, 0.12f);
            GameTimeScale.SetFrozen(true);
            _moment.Tick(0.5f);
            Assert.That(_moment.IsRunning, Is.True, "The freeze's seconds are not the slow's.");

            GameTimeScale.SetFrozen(false);
            Assert.That(Time.timeScale, Is.EqualTo(0.3f).Within(0.001f));
            _moment.Tick(0.13f);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [Test]
        public void ReducedMotionTurnsTheSlowOff()
        {
            FeedbackSettings.ReducedMotion = true;
            _moment.Begin(0.3f, 0.12f);
            Assert.That(_moment.IsRunning, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [Test]
        public void TheHitStopSettingScalesTheLengthToNothing()
        {
            FeedbackSettings.HitStopMultiplier = 0f;
            _moment.Begin(0.3f, 0.12f);
            Assert.That(_moment.IsRunning, Is.False);

            FeedbackSettings.HitStopMultiplier = 2f;
            _moment.Begin(0.3f, 0.12f);
            _moment.Tick(0.2f);
            Assert.That(_moment.IsRunning, Is.True, "Twice the setting, twice as long.");
            _moment.Tick(0.05f);
            Assert.That(_moment.IsRunning, Is.False);
        }

        [Test]
        public void SomeoneElsesSlowIsNeitherTakenNorUndone()
        {
            GameTimeScale.SetBaseScale(0.25f);
            _moment.Begin(0.3f, 0.12f);
            Assert.That(_moment.IsRunning, Is.False);
            Assert.That(GameTimeScale.BaseScale, Is.EqualTo(0.25f));

            GameTimeScale.SetBaseScale(1f);
            _moment.Begin(0.3f, 0.12f);
            GameTimeScale.SetBaseScale(0.5f);
            _moment.Tick(1f);
            Assert.That(GameTimeScale.BaseScale, Is.EqualTo(0.5f),
                "A scale changed meanwhile belongs to whoever changed it.");
        }
    }
}
