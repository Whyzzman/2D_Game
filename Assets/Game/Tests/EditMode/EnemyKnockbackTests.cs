using NUnit.Framework;

namespace ForgottenKingdom.Tests
{
    /// <summary>The slide a blow gives an enemy: felt on every hit, never out of reach of the next.</summary>
    public sealed class EnemyKnockbackTests
    {
        [Test]
        public void EveryBlowSlidesBetweenHalfAUnitAndJustUnderOne()
        {
            // The sword's lightest swing (3) and the greatsword's heaviest (12 x 1.5).
            Assert.That(EnemyKnockback.SlideDistance(3f), Is.EqualTo(EnemyKnockback.MinimumSlide).Within(0.001f));
            Assert.That(EnemyKnockback.SlideDistance(18f), Is.EqualTo(EnemyKnockback.MaximumSlide).Within(0.001f));
            Assert.That(EnemyKnockback.SlideDistance(7f), Is.GreaterThan(EnemyKnockback.SlideDistance(4f)),
                "A heavier blow should shove further.");
            Assert.That(EnemyKnockback.MaximumSlide, Is.LessThan(1f), "A combo must still reach.");
        }

        [Test]
        public void ABlowWithNoKnockbackDoesNotMoveTheEnemy()
        {
            Assert.That(EnemyKnockback.SlideDistance(0f), Is.Zero);
        }
    }
}
