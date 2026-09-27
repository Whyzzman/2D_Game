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

        [Test]
        public void EachSwingDrawsItsOwnArc()
        {
            var upEnd = WeaponTrail.ArcPoint(AttackDirection.Up, 0.5f, 1, 1.35f);
            Assert.That(upEnd.y, Is.GreaterThan(1.2f), "Mid-swing, the swing up is overhead.");
            Assert.That(upEnd.x, Is.EqualTo(0f).Within(0.01f));
            var downMid = WeaponTrail.ArcPoint(AttackDirection.Down, 0.5f, 1, 1.35f);
            Assert.That(downMid.y, Is.LessThan(-0.8f), "Mid-swing, the swing down is under the feet.");
            Assert.That(downMid.x, Is.EqualTo(0f).Within(0.01f));
            var side = WeaponTrail.ArcPoint(AttackDirection.Side, 0.5f, 1, 1.35f);
            Assert.That(side.x, Is.GreaterThan(0.9f), "The side swing crosses in front.");
            var mirrored = WeaponTrail.ArcPoint(AttackDirection.Side, 0.5f, -1, 1.35f);
            Assert.That(mirrored.x, Is.EqualTo(-side.x).Within(0.0001f), "Facing left mirrors it.");
            Assert.That(WeaponTrail.ArcPoint(AttackDirection.Up, 0f, 1, 1.35f),
                Is.Not.EqualTo(WeaponTrail.ArcPoint(AttackDirection.Up, 1f, 1, 1.35f)),
                "The point travels.");
        }

        [Test]
        public void OnlyASideSwingPushesBackAndAShieldPushesHarder()
        {
            Assert.That(PlayerCombat.RecoilOf(AttackDirection.Side, DamageOutcome.Hit, true).push, Is.EqualTo(1f));
            Assert.That(PlayerCombat.RecoilOf(AttackDirection.Side, DamageOutcome.Blocked, true).push,
                Is.GreaterThan(1f), "A shield pushes back harder than flesh.");
            Assert.That(PlayerCombat.RecoilOf(AttackDirection.Side, DamageOutcome.Ignored, true),
                Is.EqualTo((0f, 0f)), "A hit that did nothing pushes nothing.");
            Assert.That(PlayerCombat.RecoilOf(AttackDirection.Up, DamageOutcome.Hit, false).fall,
                Is.GreaterThan(0f), "A swing up in the air presses the fall.");
            Assert.That(PlayerCombat.RecoilOf(AttackDirection.Up, DamageOutcome.Hit, true),
                Is.EqualTo((0f, 0f)), "On the ground it does nothing.");
            Assert.That(PlayerCombat.RecoilOf(AttackDirection.Down, DamageOutcome.Hit, false),
                Is.EqualTo((0f, 0f)), "The swing down has its pogo instead.");
        }
    }
}
