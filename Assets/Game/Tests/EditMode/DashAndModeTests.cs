using NUnit.Framework;
using UnityEngine;

namespace ForgottenKingdom.Tests
{
    public sealed class DashAndModeTests
    {
        private const float Step = 0.02f;
        private PlayerMovementLogic _logic;
        private DashSettings _dash;

        [SetUp]
        public void SetUp()
        {
            _dash = new DashSettings();
            _logic = new PlayerMovementLogic(new MovementSettings(), _dash);
        }

        [Test]
        public void DashDrivesAFlatBurstAndIgnoresGravity()
        {
            Assert.That(_logic.TryBeginDash(1), Is.True);
            var velocity = _logic.Step(Vector2.zero, false, default, Step);
            Assert.That(velocity.x, Is.EqualTo(_dash.DashSpeed).Within(0.001f));
            Assert.That(velocity.y, Is.Zero);
            Assert.That(_logic.State, Is.EqualTo(MovementState.Dash));
            Assert.That(_logic.IsDashing, Is.True);
        }

        [Test]
        public void DashEndsAfterItsDurationAndLeavesReducedSpeed()
        {
            _logic.TryBeginDash(-1);
            var velocity = Vector2.zero;
            var steps = 0;
            do
            {
                velocity = _logic.Step(velocity, false, default, Step);
                steps++;
            }
            while (_logic.IsDashing && steps < 100);
            Assert.That(_logic.IsDashing, Is.False);
            Assert.That(steps * Step, Is.EqualTo(_dash.DashDuration).Within(Step));
            Assert.That(velocity.x,
                Is.EqualTo(-_dash.DashSpeed * _dash.ExitSpeedFraction).Within(0.001f));
            Assert.That(_logic.FacingDirection, Is.EqualTo(-1));
        }

        [Test]
        public void DashRespectsItsCooldown()
        {
            _logic.TryBeginDash(1);
            RunSteps(_dash.DashDuration + 0.05f);
            Assert.That(_logic.TryBeginDash(1), Is.False, "Dash must wait out its cooldown.");
            RunSteps(_dash.Cooldown);
            Assert.That(_logic.IsDashReady, Is.True);
            Assert.That(_logic.TryBeginDash(1), Is.True);
        }

        [Test]
        public void AJumpPressedBeforeADashDoesNotFireAfterIt()
        {
            _logic.Step(Vector2.zero, true, default, Step);
            _logic.TryBeginDash(1);
            var velocity = _logic.Step(Vector2.zero, true,
                new MovementInput(0f, true, true), Step);
            Assert.That(velocity.y, Is.Zero, "The dash suspends vertical motion.");
            RunSteps(_dash.DashDuration);
            var afterDash = _logic.Step(Vector2.zero, true, new MovementInput(0f, false, true), Step);
            Assert.That(afterDash.y, Is.Zero, "The stale buffered press must have expired.");
        }

        [Test]
        public void NoInputModeBrakesTheCharacterAndRefusesJumps()
        {
            var velocity = new Vector2(6f, 0f);
            velocity = _logic.Step(velocity, true, new MovementInput(1f, true, true), Step,
                LocomotionMode.NoInput);
            Assert.That(velocity.x, Is.LessThan(6f));
            Assert.That(velocity.y, Is.Zero);
        }

        [Test]
        public void AirDashesAreLimitedAndComeBackOnLanding()
        {
            // Leave the ground first: air dashes are only counted while airborne.
            _logic.Step(Vector2.zero, false, default, Step);
            Assert.That(_logic.TryBeginDash(1), Is.True);
            RunAirborne(_dash.DashDuration + _dash.Cooldown + 0.05f);

            Assert.That(_logic.AirDashesUsed, Is.EqualTo(1));
            Assert.That(_logic.IsDashReady, Is.False,
                "The air dash is spent even though the cooldown has elapsed.");
            Assert.That(_logic.TryBeginDash(1), Is.False);

            _logic.Step(Vector2.zero, true, default, Step);
            Assert.That(_logic.AirDashesUsed, Is.Zero);
            Assert.That(_logic.TryBeginDash(1), Is.True, "Landing gives the air dash back.");
        }

        [Test]
        public void TheDashTracksItsOwnElapsedTimeForTheInvincibilityWindow()
        {
            _logic.TryBeginDash(1);
            var velocity = _logic.Step(Vector2.zero, true, default, Step);
            Assert.That(_logic.DashElapsed, Is.EqualTo(Step).Within(0.0001f));
            velocity = _logic.Step(velocity, true, default, Step);
            Assert.That(_logic.DashElapsed, Is.EqualTo(Step * 2f).Within(0.0001f));
        }

        [Test]
        public void RestrictedModeScalesSpeedAndRefusesJumpsAndTurns()
        {
            var settings = new MovementSettings();
            var velocity = Vector2.zero;
            for (var i = 0; i < 40; i++)
            {
                velocity = _logic.Step(velocity, true, new MovementInput(1f, true, true),
                    Step, LocomotionMode.Restricted, 0.3f);
            }

            Assert.That(velocity.x, Is.EqualTo(settings.WalkSpeed * 0.3f).Within(0.001f));
            Assert.That(velocity.y, Is.Zero, "An attack must not be turned into a jump.");

            _logic.Step(velocity, true, new MovementInput(-1f), Step, LocomotionMode.Restricted, 0.3f);
            Assert.That(_logic.FacingDirection, Is.EqualTo(1),
                "Facing stays where the swing started.");
        }

        [Test]
        public void KnockbackModeKeepsHorizontalSpeedWhileGravityApplies()
        {
            var velocity = new Vector2(-7f, 0f);
            velocity = _logic.Step(velocity, false, new MovementInput(1f), Step,
                LocomotionMode.Knockback);
            Assert.That(velocity.x, Is.EqualTo(-7f).Within(0.001f));
            Assert.That(velocity.y, Is.LessThan(0f));
            Assert.That(_logic.FacingDirection, Is.EqualTo(1),
                "Input is ignored, so facing does not flip during knockback.");
        }

        private void RunAirborne(float seconds)
        {
            var steps = Mathf.CeilToInt(seconds / Step);
            var velocity = Vector2.zero;
            for (var i = 0; i < steps; i++)
            {
                velocity = _logic.Step(velocity, false, default, Step);
            }
        }

        private void RunSteps(float seconds)
        {
            var steps = Mathf.CeilToInt(seconds / Step);
            var velocity = Vector2.zero;
            for (var i = 0; i < steps; i++)
            {
                velocity = _logic.Step(velocity, true, default, Step);
            }
        }
    }
}
