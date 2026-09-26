using NUnit.Framework;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// Settings persistence. The question that matters most is the separation: a player who starts
    /// a new game must not lose the comfort options they needed in order to play.
    /// </summary>
    public sealed class GameSettingsTests
    {
        [SetUp]
        public void SetUp()
        {
            GameSettings.Delete();
            FeedbackSettings.ResetToDefaults();
        }

        [TearDown]
        public void TearDown()
        {
            GameSettings.Delete();
            FeedbackSettings.ResetToDefaults();
        }

        [Test]
        public void WithNoFileTheDefaultsAreSensible()
        {
            var data = GameSettings.Current;

            Assert.That(data.screenShake, Is.EqualTo(1f));
            Assert.That(data.flashIntensity, Is.EqualTo(1f));
            Assert.That(data.showDamageNumbers, Is.True);
        }

        [Test]
        public void ChangingASettingAppliesItImmediately()
        {
            GameSettings.Set(data => data.screenShake = 0f);

            Assert.That(FeedbackSettings.ScreenShakeMultiplier, Is.Zero,
                "A slider has to take effect without waiting for a restart.");
        }

        [Test]
        public void SettingsSurviveARestart()
        {
            GameSettings.Set(data =>
            {
                data.screenShake = 0f;
                data.flashIntensity = 0.25f;
                data.masterVolume = 0.4f;
            });

            // Restart: drop what is in memory and read the file again.
            GameSettings.Unload();
            FeedbackSettings.ResetToDefaults();
            GameSettings.Load();

            Assert.That(GameSettings.Current.screenShake, Is.Zero);
            Assert.That(GameSettings.Current.flashIntensity, Is.EqualTo(0.25f).Within(0.001f));
            Assert.That(GameSettings.Current.masterVolume, Is.EqualTo(0.4f).Within(0.001f));
            Assert.That(FeedbackSettings.ScreenShakeMultiplier, Is.Zero,
                "Loading has to push the values back into the systems that use them.");
        }

        [Test]
        public void DeletingTheSaveDoesNotTouchTheSettings()
        {
            // The line this whole class exists to hold: progress and preferences are separate.
            GameSettings.Set(data => data.screenShake = 0f);
            SaveSystem.Delete();
            GameSettings.Unload();

            Assert.That(GameSettings.Current.screenShake, Is.Zero,
                "Starting a new game must not cost someone their accessibility options.");
        }

        [Test]
        public void SettingsLiveInTheirOwnFile()
        {
            Assert.That(GameSettings.FilePath, Is.Not.EqualTo(SaveSystem.FilePath));
            Assert.That(GameSettings.FilePath, Is.Not.EqualTo(SaveSystem.BackupPath));
        }

        [Test]
        public void NonsenseValuesAreClampedRatherThanStored()
        {
            GameSettings.Set(data =>
            {
                data.screenShake = 9f;
                data.musicVolume = -4f;
            });

            Assert.That(GameSettings.Current.screenShake, Is.EqualTo(1f));
            Assert.That(GameSettings.Current.musicVolume, Is.Zero);
        }

        [Test]
        public void ResettingReturnsEverythingToDefaults()
        {
            GameSettings.Set(data => data.flashIntensity = 0f);
            GameSettings.ResetToDefaults();

            Assert.That(GameSettings.Current.flashIntensity, Is.EqualTo(1f));
            Assert.That(FeedbackSettings.FlashIntensity, Is.EqualTo(1f));
        }
    }
}
