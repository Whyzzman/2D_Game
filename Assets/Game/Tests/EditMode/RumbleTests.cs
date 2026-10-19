using NUnit.Framework;
using UnityEditor;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The pad's rumble (GAME_FEEL_IDEAS.md, item 6): the profiles' strengths follow the screen's
    /// own ladder, the frequent moments never rumble, and the setting at zero keeps the motors still.
    /// </summary>
    public sealed class RumbleTests
    {
        private FeedbackLibrary _library;

        [SetUp]
        public void SetUp()
        {
            _library = AssetDatabase.LoadAssetAtPath<FeedbackLibrary>("Assets/Game/Data/Feedback/FeedbackLibrary.asset");
            Assert.That(_library, Is.Not.Null);
        }

        [TearDown]
        public void TearDown() => FeedbackSettings.ResetToDefaults();

        [Test]
        public void TheRumbleClimbsWithTheWeightOfTheMoment()
        {
            float Haptic(FeedbackKind kind) => _library.Profile(kind).Haptic;
            Assert.That(Haptic(FeedbackKind.LightHit), Is.GreaterThan(0f));
            Assert.That(Haptic(FeedbackKind.HeavyHit), Is.GreaterThan(Haptic(FeedbackKind.LightHit)));
            Assert.That(Haptic(FeedbackKind.Parry), Is.GreaterThan(Haptic(FeedbackKind.HeavyHit)));
            Assert.That(Haptic(FeedbackKind.BossDeath), Is.GreaterThanOrEqualTo(Haptic(FeedbackKind.Parry)));
        }

        [Test]
        public void MovementDustAndStatusesNeverRumble()
        {
            foreach (var kind in new[]
            {
                FeedbackKind.Dash, FeedbackKind.AirDash, FeedbackKind.RunDust, FeedbackKind.LandDust,
                FeedbackKind.JumpDust, FeedbackKind.PoisonProc, FeedbackKind.BurnProc, FeedbackKind.Pickup
            })
            {
                Assert.That(_library.Profile(kind).Haptic, Is.Zero, $"{kind} rumbles.");
            }
        }

        [Test]
        public void VibrationAtZeroKeepsTheMotorsStill()
        {
            var rumble = new GamepadRumble();
            FeedbackSettings.HapticsMultiplier = 0f;
            rumble.Pulse(1f);
            Assert.That(rumble.Strength, Is.Zero);

            FeedbackSettings.HapticsMultiplier = 1f;
            rumble.Pulse(0.3f);
            rumble.Pulse(0.1f);
            Assert.That(rumble.Strength, Is.EqualTo(0.3f).Within(0.001f), "A weaker pulse does not replace a stronger one.");
            rumble.Stop();
            Assert.That(rumble.Strength, Is.Zero);
        }
    }
}
