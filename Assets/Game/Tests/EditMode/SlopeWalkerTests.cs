using NUnit.Framework;
using UnityEngine;

namespace ForgottenKingdom.Tests
{
    /// <summary>How the flat-ground movement is laid onto a slope.</summary>
    public sealed class SlopeWalkerTests
    {
        private static readonly Vector2 ThirtyDegrees = new Vector2(-0.5f, 0.866f);

        [Test]
        public void RunningIsLaidAlongTheSurfaceAtTheSamePace()
        {
            var walker = new SlopeWalker();
            var along = walker.FromFlat(new Vector2(6.5f, 0f), true, ThirtyDegrees, 0.65f);
            Assert.That(walker.IsOnSlope, Is.True);
            Assert.That(along.magnitude, Is.EqualTo(6.5f).Within(0.01f), "Same speed along the slope.");
            Assert.That(Vector2.Dot(along, ThirtyDegrees), Is.EqualTo(0f).Within(0.001f), "Along, not into it.");
            Assert.That(along.y, Is.GreaterThan(0f), "Running right climbs this slope.");

            var down = walker.FromFlat(new Vector2(-6.5f, 0f), true, ThirtyDegrees, 0.65f);
            Assert.That(down.y, Is.LessThan(0f), "Running back goes down with the surface, no hop.");
            Assert.That(walker.ToFlat(down), Is.EqualTo(new Vector2(-6.5f, 0f)), "The logic keeps seeing flat ground.");
        }

        [Test]
        public void AJumpOrLevelGroundLeavesTheSlopeAlone()
        {
            var walker = new SlopeWalker();
            var jump = new Vector2(6.5f, 11f);
            Assert.That(walker.FromFlat(jump, true, ThirtyDegrees, 0.65f), Is.EqualTo(jump));
            Assert.That(walker.IsOnSlope, Is.False);
            var run = new Vector2(6.5f, 0f);
            Assert.That(walker.FromFlat(run, true, Vector2.up, 0.65f), Is.EqualTo(run));
            Assert.That(walker.FromFlat(run, false, ThirtyDegrees, 0.65f), Is.EqualTo(run), "In the air: no slope.");
            Assert.That(walker.FromFlat(run, true, new Vector2(-0.9f, 0.43f), 0.65f), Is.EqualTo(run),
                "Steeper than the ground limit is a wall, not a slope.");
        }
    }
}
