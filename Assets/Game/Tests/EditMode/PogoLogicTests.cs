using NUnit.Framework;
using UnityEngine;

namespace ForgottenKingdom.Tests
{
    /// <summary>The pogo's movement rules, as in Hollow Knight: a fixed rise and the air moves back.</summary>
    public sealed class PogoLogicTests
    {
        private const float StepTime = 0.02f;

        [Test]
        public void ABounceRisesAtTheFixedSpeedAndGivesTheAirMovesBack()
        {
            var settings = new MovementSettings();
            var logic = new PlayerMovementLogic(settings) { AirJumpsAllowed = 1 };
            var velocity = new Vector2(0f, -6f);
            velocity = logic.Step(velocity, false, new MovementInput(0f, isJumpPressed: true), StepTime);
            Assume.That(logic.AirJumpsUsed, Is.EqualTo(1), "The second jump is spent.");

            logic.Bounce(settings.PogoSpeed);
            Assert.That(logic.AirJumpsUsed, Is.Zero);
            Assert.That(logic.AirDashesUsed, Is.Zero);

            velocity = logic.Step(new Vector2(0f, -8f), false, default, StepTime);
            Assert.That(velocity.y, Is.GreaterThan(settings.PogoSpeed - 1f), "Up, whatever the fall was.");
            Assert.That(logic.IsBouncing, Is.True);
        }

        [Test]
        public void LettingGoOfJumpDoesNotCutABounceShort()
        {
            var settings = new MovementSettings();
            var bounced = new PlayerMovementLogic(settings);
            var jumped = new PlayerMovementLogic(settings);
            bounced.Bounce(settings.PogoSpeed);
            var a = bounced.Step(Vector2.zero, false, default, StepTime);
            var b = jumped.Step(new Vector2(0f, settings.PogoSpeed), false, default, StepTime);
            for (var i = 0; i < 5; i++)
            {
                a = bounced.Step(a, false, default, StepTime);
                b = jumped.Step(b, false, default, StepTime);
            }

            Assert.That(a.y, Is.GreaterThan(b.y + 1f),
                "An ordinary rise without the button held is cut; the bounce keeps its height.");
        }
    }
}
