using NUnit.Framework;

namespace ForgottenKingdom.Tests
{
    /// <summary>The boss title card's timing, as approved in design/ui/boss-title.</summary>
    public sealed class BossTitleCardTests
    {
        [Test]
        public void TheCardFadesInHoldsAndFadesOutInUnderFourSeconds()
        {
            Assert.That(BossTitleCard.Total, Is.EqualTo(3.6f).Within(0.001f));
            Assert.That(BossTitleCard.Opacity(0f), Is.Zero);
            Assert.That(BossTitleCard.Opacity(BossTitleCard.FadeIn * 0.5f), Is.InRange(0.4f, 0.6f));
            Assert.That(BossTitleCard.Opacity(BossTitleCard.FadeIn + 1f), Is.EqualTo(1f));
            Assert.That(BossTitleCard.Opacity(BossTitleCard.Total - 0.05f), Is.LessThan(0.1f));
            Assert.That(BossTitleCard.Opacity(BossTitleCard.Total), Is.Zero);
        }
    }
}
