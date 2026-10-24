using NUnit.Framework;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The attack buffer around a dash (GAME_FEEL_IDEAS.md, item 3): a swing pressed in the dash's
    /// last stretch waits for it to end, one pressed earlier lapses as any buffered press does.
    /// </summary>
    public sealed class AttackBufferTests
    {
        private const float Buffer = 0.15f;
        private const float Window = 0.15f;
        private const float Grace = 0.05f;
        private const float Step = 0.02f;

        [Test]
        public void WithoutADashAPressWaitsTheOrdinaryBuffer()
        {
            Assert.That(AttackBuffer.WaitsUntil(10f, Buffer, 0f, Window, Grace),
                Is.EqualTo(10f + Buffer).Within(0.0001f));
        }

        [Test]
        public void APressAtTheVeryStartOfTheDashTailOutlastsTheDash()
        {
            var dash = new DashSettings();
            // The earliest press the window takes: the plain buffer would end exactly with the
            // dash, a step too early when the character sees the dash end a step late.
            var remaining = Window;
            var until = AttackBuffer.WaitsUntil(10f, Buffer, remaining, Window, Grace);
            Assert.That(until, Is.GreaterThanOrEqualTo(10f + remaining + Step),
                "The press survives the step it takes to see the dash end.");
            Assert.That(dash.DashDuration, Is.GreaterThan(Buffer),
                "The dash is longer than the buffer, which is why the tail needs its own rule.");
        }

        [Test]
        public void APressLateInTheDashKeepsAtLeastTheOrdinaryBuffer()
        {
            Assert.That(AttackBuffer.WaitsUntil(10f, Buffer, 0.02f, Window, Grace),
                Is.EqualTo(10f + Buffer).Within(0.0001f));
        }

        [Test]
        public void APressBeforeTheDashTailLapsesAsUsual()
        {
            // Pressed with 0.17 of the dash left, before its last 0.15: too soon, not buffered on.
            Assert.That(AttackBuffer.WaitsUntil(10f, Buffer, 0.17f, Window, Grace),
                Is.EqualTo(10f + Buffer).Within(0.0001f));
        }

        [Test]
        public void ADashStepByStepNeverLetsATailPressLapseBeforeItEnds()
        {
            var dash = new DashSettings();
            for (var elapsed = 0f; elapsed < dash.DashDuration; elapsed += Step)
            {
                var remaining = dash.DashDuration - elapsed;
                if (remaining > Window)
                {
                    continue;
                }

                var until = AttackBuffer.WaitsUntil(elapsed, Buffer, remaining, Window, Grace);
                Assert.That(until, Is.GreaterThan(dash.DashDuration + Step - 0.0001f),
                    $"A press {remaining:0.00} before the end must still wait when it ends.");
            }
        }
    }
}
