using NUnit.Framework;

namespace ForgottenKingdom.Tests
{
    /// <summary>Coins taken one after another ring higher, top out and start over after a pause (GAME_FEEL_IDEAS.md, item 15).</summary>
    public sealed class CoinChimeTests
    {
        [Test]
        public void EachCoinInARunRingsAStepHigherUpToTheTop()
        {
            var chime = new CoinChime(1f, 1, 7);
            Assert.That(chime.Register(0f), Is.EqualTo(1f).Within(0.0001f), "The first coin is as authored.");
            Assert.That(chime.Register(0.2f), Is.EqualTo(UnityEngine.Mathf.Pow(2f, 1f / 12f)).Within(0.0001f));
            for (var i = 0; i < 20; i++)
            {
                chime.Register(0.4f + i * 0.2f);
            }

            Assert.That(chime.Step, Is.EqualTo(7), "It holds its top note instead of squeaking.");
        }

        [Test]
        public void APauseStartsTheRunOver()
        {
            var chime = new CoinChime(1f, 2, 7);
            chime.Register(0f);
            Assert.That(chime.Register(0.5f), Is.EqualTo(UnityEngine.Mathf.Pow(2f, 2f / 12f)).Within(0.0001f),
                "The step is configurable.");
            Assert.That(chime.Register(1.6f), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(chime.Step, Is.Zero);
        }
    }
}
