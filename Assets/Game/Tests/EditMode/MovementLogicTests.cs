using NUnit.Framework;
using UnityEngine;

namespace ForgottenKingdom.Tests
{
    public sealed class MovementLogicTests
    {
        private const float StepTime = 0.02f;
        private MovementSettings _settings;
        private PlayerMovementLogic _logic;

        [SetUp]
        public void SetUp()
        {
            _settings = new MovementSettings();
            _logic = new PlayerMovementLogic(_settings);
        }

        [Test]
        public void WalkAcceleratesToLimitAndBrakesToZero()
        {
            var velocity = _logic.Step(Vector2.zero, true, new MovementInput(1f), StepTime);
            Assert.That(velocity.x, Is.EqualTo(1.2f).Within(0.001f));
            for (var i = 0; i < 20; i++)
            {
                velocity = _logic.Step(velocity, true, new MovementInput(1f), StepTime);
            }
            Assert.That(velocity.x, Is.EqualTo(3f).Within(0.001f));
            for (var i = 0; i < 3; i++)
            {
                velocity = _logic.Step(velocity, true, default, StepTime);
            }
            Assert.That(velocity.x, Is.Zero);
            Assert.That(_logic.State, Is.EqualTo(MovementState.Idle));
        }

        [Test]
        public void SprintAndAnalogInputUseDifferentTargetSpeeds()
        {
            var velocity = Vector2.zero;
            for (var i = 0; i < 20; i++)
            {
                velocity = _logic.Step(velocity, true, new MovementInput(1f, true), StepTime);
            }
            Assert.That(velocity.x, Is.EqualTo(6.5f).Within(0.001f));
            Assert.That(_logic.State, Is.EqualTo(MovementState.Run));
            for (var i = 0; i < 20; i++)
            {
                velocity = _logic.Step(velocity, true, new MovementInput(0.5f), StepTime);
            }
            Assert.That(velocity.x, Is.EqualTo(1.5f).Within(0.001f));
        }

        [Test]
        public void AirControlAndDirectionChangeAreResponsive()
        {
            var velocity = _logic.Step(new Vector2(3f, 0f), false, new MovementInput(-1f), StepTime);
            Assert.That(velocity.x, Is.EqualTo(2.4f).Within(0.001f));
            Assert.That(_logic.FacingDirection, Is.EqualTo(-1));
            for (var i = 0; i < 12; i++)
            {
                velocity = _logic.Step(velocity, false, new MovementInput(-1f), StepTime);
            }
            Assert.That(velocity.x, Is.EqualTo(-3f).Within(0.001f));
        }

        [Test]
        public void JumpWorksInsideCoyoteWindowButNotAfterExpiry()
        {
            _logic.Step(Vector2.zero, true, default, StepTime);
            _logic.Step(Vector2.zero, false, default, 0.06f);
            var jumped = _logic.Step(Vector2.zero, false,
                new MovementInput(0f, false, true, true), StepTime);
            Assert.That(jumped.y, Is.GreaterThan(9f));

            _logic.Reset();
            _logic.Step(Vector2.zero, true, default, StepTime);
            _logic.Step(Vector2.zero, false, default, 0.12f);
            var missed = _logic.Step(Vector2.zero, false,
                new MovementInput(0f, false, true, true), StepTime);
            Assert.That(missed.y, Is.LessThan(0f));
        }

        [Test]
        public void BufferedPressFiresOnLandingAndOnlyOnce()
        {
            _logic.Step(new Vector2(0f, -2f), false,
                new MovementInput(0f, false, true, true), StepTime);
            var velocity = _logic.Step(Vector2.zero, true,
                new MovementInput(0f, false, false, true), 0.06f);
            Assert.That(velocity.y, Is.GreaterThan(9f));
            _logic.Step(velocity, false, new MovementInput(0f, false, false, true), StepTime);
            velocity = _logic.Step(Vector2.zero, true,
                new MovementInput(0f, false, false, true), StepTime);
            Assert.That(velocity.y, Is.Zero);
        }

        [Test]
        public void ExpiredBufferDoesNotJumpOnLanding()
        {
            _logic.Step(Vector2.down, false, new MovementInput(0f, false, true, true), StepTime);
            var velocity = _logic.Step(Vector2.zero, true,
                new MovementInput(0f, false, false, true), 0.15f);
            Assert.That(velocity.y, Is.Zero);
        }

        [Test]
        public void OldInputBetweenPhysicsTicksCannotRenewBuffer()
        {
            var velocity = _logic.Step(Vector2.zero, true,
                new MovementInput(0f, false, true, true, 0.2f), StepTime);
            Assert.That(velocity.y, Is.Zero);
        }

        [Test]
        public void JumpConsumesCoyoteAndIgnoresLingeringGroundContact()
        {
            var velocity = _logic.Step(Vector2.zero, true,
                new MovementInput(0f, false, true, true), StepTime);
            var next = _logic.Step(velocity, true,
                new MovementInput(0f, false, true, true), StepTime);
            Assert.That(next.y, Is.LessThan(velocity.y));
            var airborne = _logic.Step(next, false,
                new MovementInput(0f, false, true, true), StepTime);
            Assert.That(airborne.y, Is.LessThan(next.y));
        }

        [Test]
        public void ReleaseMakesJumpLowerAndFallSpeedIsLimited()
        {
            var fullHeight = SimulateJump(true);
            _logic.Reset();
            var shortHeight = SimulateJump(false);
            Assert.That(fullHeight, Is.GreaterThan(1.7f));
            Assert.That(shortHeight, Is.LessThan(fullHeight * 0.65f));
            var velocity = new Vector2(0f, -19.5f);
            for (var i = 0; i < 200; i++)
            {
                velocity = _logic.Step(velocity, false, default, StepTime);
            }
            Assert.That(velocity.y, Is.EqualTo(-20f).Within(0.001f));
        }

        [Test]
        public void ResetRemovesBufferedPressAndCoyotePermission()
        {
            _logic.Step(Vector2.zero, true, default, StepTime);
            _logic.Step(Vector2.down, false, new MovementInput(-1f, false, true, true, 0.2f), StepTime);
            _logic.Reset();
            Assert.That(_logic.State, Is.EqualTo(MovementState.Idle));
            Assert.That(_logic.FacingDirection, Is.EqualTo(1));
            Assert.That(_logic.Step(Vector2.zero, false,
                new MovementInput(0f, false, true, true), StepTime).y, Is.LessThan(0f));
            _logic.Reset();
            Assert.That(_logic.Step(Vector2.zero, true, default, StepTime).y, Is.Zero);
        }

        private float SimulateJump(bool hold)
        {
            var velocity = _logic.Step(Vector2.zero, true,
                new MovementInput(0f, false, true, true), StepTime);
            var height = velocity.y * StepTime;
            var peak = height;
            for (var i = 0; i < 100; i++)
            {
                velocity = _logic.Step(velocity, false,
                    new MovementInput(0f, false, false, hold), StepTime);
                height += velocity.y * StepTime;
                peak = Mathf.Max(peak, height);
            }
            return peak;
        }
    }
}
