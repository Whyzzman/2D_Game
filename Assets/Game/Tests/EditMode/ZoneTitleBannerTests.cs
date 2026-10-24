using NUnit.Framework;
using UnityEngine.UIElements;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The zone card: full on the first visit, short on a return, not again within the cooldown, and
    /// a shard or wave message replaces a card still waiting (GAME_FEEL_IDEAS.md, item 20). With no
    /// music director, as here, nothing waits.
    /// </summary>
    public sealed class ZoneTitleBannerTests
    {
        private static ZoneTitleBanner Banner(float cooldown)
        {
            var root = new VisualElement();
            var card = new VisualElement { name = "zone-title" };
            card.Add(new Label { name = "zone-title-text" });
            root.Add(card);
            return new ZoneTitleBanner(root, new ZoneTitleTiming { shortCooldown = cooldown });
        }

        [Test]
        public void TheFirstVisitIsTheFullCardAndAReturnTheShortOne()
        {
            var banner = Banner(0f);
            banner.ShowZone("zone_withered_forest", "Зів'ялий Ліс", isFirstVisit: true);
            Assert.That(banner.IsShowing, Is.True);
            Assert.That(banner.IsShort, Is.False);
            Assert.That(banner.Text, Is.EqualTo("ЗІВ'ЯЛИЙ ЛІС"));

            banner.ShowZone("zone_withered_forest", "Зів'ялий Ліс", isFirstVisit: false);
            Assert.That(banner.IsShort, Is.True);
        }

        [Test]
        public void ARecentCardIsNotRepeatedButAFirstVisitAlwaysIs()
        {
            var banner = Banner(45f);
            banner.ShowZone("zone_forgotten_tomb", "Забута Гробниця", isFirstVisit: false);
            Assert.That(banner.IsShort, Is.True);
            banner.Hide();

            banner.ShowZone("zone_forgotten_tomb", "Забута Гробниця", isFirstVisit: false);
            Assert.That(banner.IsShowing, Is.False, "Walking back over the border does not flash the name.");

            banner.ShowZone("zone_forgotten_tomb", "Забута Гробниця", isFirstVisit: true);
            Assert.That(banner.IsShowing, Is.True);
            Assert.That(banner.IsShort, Is.False);
        }

        [Test]
        public void AMessageThatIsNotAZoneShowsAtOnce()
        {
            var banner = Banner(0f);
            banner.Show("Хвиля 1/3");
            Assert.That(banner.IsShowing, Is.True);
            Assert.That(banner.IsWaitingForMusic, Is.False);
            Assert.That(banner.Text, Is.EqualTo("ХВИЛЯ 1/3"));
        }
    }
}
