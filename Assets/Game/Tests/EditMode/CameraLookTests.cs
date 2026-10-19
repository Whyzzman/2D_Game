using NUnit.Framework;

namespace ForgottenKingdom.Tests
{
    /// <summary>Looking up and down (GAME_FEEL_IDEAS.md, item 1): only a deliberate hold moves the view.</summary>
    public sealed class CameraLookTests
    {
        [Test]
        public void AHoldShorterThanTheDelayDoesNotMoveTheView()
        {
            // A swing up or a pogo is a tap, not a look.
            Assert.That(CameraFollow2D.LookOffset(1, 0.2f, 0.4f, 3f), Is.Zero);
            Assert.That(CameraFollow2D.LookOffset(-1, 0.39f, 0.4f, 3f), Is.Zero);
        }

        [Test]
        public void AHeldLookMovesTheWholeDistanceThatWay()
        {
            Assert.That(CameraFollow2D.LookOffset(1, 0.5f, 0.4f, 3f), Is.EqualTo(3f));
            Assert.That(CameraFollow2D.LookOffset(-1, 2f, 0.4f, 3f), Is.EqualTo(-3f));
            Assert.That(CameraFollow2D.LookOffset(0, 2f, 0.4f, 3f), Is.Zero);
        }
    }
}
