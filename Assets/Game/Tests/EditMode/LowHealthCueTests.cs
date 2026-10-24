using NUnit.Framework;
using UnityEngine.UIElements;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// Low health felt in the world (GAME_FEEL_IDEAS item 17) as a rule: under a quarter it eases
    /// in and the heart beats, above it or on death it eases out and the heart stops, and reduced
    /// motion holds the edge still.
    /// </summary>
    public sealed class LowHealthCueTests
    {
        private const float Step = 0.05f;

        [SetUp]
        public void SetUp()
        {
            FeedbackSettings.ResetToDefaults();
            GameTimeScale.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            FeedbackSettings.ResetToDefaults();
            GameTimeScale.Reset();
        }

        [Test]
        public void UnderAQuarterTheEdgePalesAndTheHeartBeats()
        {
            var cue = new LowHealthCue(null, new LowHealthSettings());
            var beats = 0;
            cue.Beat += _ => beats++;

            Run(cue, 3f, 0.2f, true);

            Assert.That(cue.Amount, Is.EqualTo(1f));
            Assert.That(cue.Opacity, Is.GreaterThan(0f));
            Assert.That(beats, Is.InRange(2, 4), "About one beat a second.");
        }

        [Test]
        public void HealthyOrDeadThereIsNothing()
        {
            var cue = new LowHealthCue(null, new LowHealthSettings());
            var beats = 0;
            cue.Beat += _ => beats++;

            Run(cue, 2f, 0.6f, true);
            Assert.That(cue.Amount, Is.Zero);

            Run(cue, 2f, 0.1f, false);
            Assert.That(cue.Amount, Is.Zero);
            Assert.That(beats, Is.Zero);
        }

        [Test]
        public void ItFadesOutWhenHealthComesBack()
        {
            var cue = new LowHealthCue(null, new LowHealthSettings());
            Run(cue, 2f, 0.2f, true);
            Assume.That(cue.Amount, Is.EqualTo(1f));

            var beats = 0;
            cue.Beat += _ => beats++;
            Run(cue, 0.3f, 0.8f, true);
            Assert.That(cue.Amount, Is.InRange(0.01f, 0.99f), "It eases out, not cuts.");

            Run(cue, 2f, 0.8f, true);
            Assert.That(cue.Amount, Is.Zero);
            Assert.That(beats, Is.Zero, "The heart stops as soon as the danger passes.");
        }

        [Test]
        public void ReducedMotionHoldsTheEdgeStill()
        {
            FeedbackSettings.ReducedMotion = true;
            var cue = new LowHealthCue(null, new LowHealthSettings());
            Run(cue, 2f, 0.2f, true);
            var first = cue.Opacity;
            for (var i = 0; i < 40; i++)
            {
                cue.Tick(Step, 0.2f, true);
                Assert.That(cue.Opacity, Is.EqualTo(first).Within(0.0001f));
            }
        }

        [Test]
        public void TheVeilSitsJustOverTheHudVignette()
        {
            var root = new VisualElement();
            var hud = new VisualElement { name = "hud" };
            root.Add(hud);
            hud.Add(new VisualElement { name = "vignette" });
            hud.Add(new VisualElement { name = "conditions" });

            var cue = new LowHealthCue(root, new LowHealthSettings());

            Assert.That(cue.Veil, Is.Not.Null);
            Assert.That(hud.IndexOf(cue.Veil), Is.EqualTo(1));
            Assert.That(cue.Veil.pickingMode, Is.EqualTo(PickingMode.Ignore));
        }

        private static void Run(LowHealthCue cue, float seconds, float share, bool alive)
        {
            for (var t = 0f; t < seconds; t += Step)
            {
                cue.Tick(Step, share, alive);
            }
        }
    }
}
