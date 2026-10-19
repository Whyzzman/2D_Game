using NUnit.Framework;

namespace ForgottenKingdom.Tests
{
    /// <summary>A run of hits climbs a semitone a blow, tops out, and starts over after a pause (GAME_FEEL_IDEAS.md, item 8).</summary>
    public sealed class HitStreakTests
    {
        [Test]
        public void EachHitInARunSoundsASemitoneHigherUpToTheTop()
        {
            var streak = new HitStreak();
            Assert.That(streak.Register(0f), Is.EqualTo(1f).Within(0.0001f), "The first hit is as authored.");
            Assert.That(streak.Register(0.3f), Is.EqualTo(HitStreak.PitchFor(1)).Within(0.0001f));
            for (var i = 0; i < 10; i++)
            {
                streak.Register(0.6f + i * 0.3f);
            }

            Assert.That(streak.Step, Is.EqualTo(HitStreak.MaxSteps), "It tops out instead of squeaking.");
            Assert.That(HitStreak.PitchFor(12), Is.EqualTo(2f).Within(0.0001f));
        }

        [Test]
        public void APauseOrAHurtStartsTheRunOver()
        {
            var streak = new HitStreak();
            streak.Register(0f);
            streak.Register(0.5f);
            Assert.That(streak.Register(0.5f + HitStreak.Window + 0.1f), Is.EqualTo(1f).Within(0.0001f));

            streak.Register(2f);
            streak.Break();
            Assert.That(streak.Register(2.2f), Is.EqualTo(1f).Within(0.0001f));
        }
    }
}
