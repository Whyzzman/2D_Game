using NUnit.Framework;

namespace ForgottenKingdom.Tests
{
    /// <summary>Automated runs of a build make no sound (user's request, 2026-11-12).</summary>
    public sealed class SilentRunTests
    {
        [Test]
        public void ProbeTourAndMuteAreSilentAnOrdinaryRunIsNot()
        {
            Assert.That(SilentRun.Requested(new[] { "game", "-perfProbe", "r.tsv" }), Is.True);
            Assert.That(SilentRun.Requested(new[] { "game", "-screenshotTour", "shots" }), Is.True);
            Assert.That(SilentRun.Requested(new[] { "game", "-mute" }), Is.True);
            Assert.That(SilentRun.Requested(new[] { "game", "-screen-width", "1280" }), Is.False);
        }
    }
}
