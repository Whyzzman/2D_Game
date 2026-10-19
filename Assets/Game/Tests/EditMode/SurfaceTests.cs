using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The ground under the feet (GAME_FEEL_IDEAS item 4): each surface has its own step and its own
    /// dust, untagged blocks take their zone's ground, and a tagged block wins over the zone.
    /// </summary>
    public sealed class SurfaceTests
    {
        private readonly List<GameObject> _made = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _made) { Object.DestroyImmediate(go); }
            _made.Clear();
        }

        [Test]
        public void EverySurfaceHasItsOwnStepAndStoneKeepsTheOldOne()
        {
            var seen = new HashSet<AudioId>();
            foreach (SurfaceKind kind in System.Enum.GetValues(typeof(SurfaceKind)))
            {
                Assert.That(seen.Add(Surfaces.Footstep(kind)), Is.True, $"{kind} shares a step with another surface.");
            }

            Assert.That(Surfaces.Footstep(SurfaceKind.Stone), Is.EqualTo(AudioId.FootstepStone));
        }

        [Test]
        public void ZonesFallBackToTheirOwnGround()
        {
            Assert.That(Surfaces.DefaultFor("zone_forgotten_tomb"), Is.EqualTo(SurfaceKind.Stone));
            Assert.That(Surfaces.DefaultFor("zone_withered_forest"), Is.EqualTo(SurfaceKind.Earth));
            Assert.That(Surfaces.DefaultFor("zone_ashen_capital"), Is.EqualTo(SurfaceKind.Ash));
            Assert.That(Surfaces.DefaultFor(string.Empty), Is.EqualTo(SurfaceKind.Stone),
                "Nowhere in particular is stone.");
        }

        [Test]
        public void StoneDustIsUntouchedAndTheRestIsTinted()
        {
            Assert.That(Surfaces.DustTint(SurfaceKind.Stone), Is.EqualTo(Color.white));
            Assert.That(Surfaces.DustTint(SurfaceKind.Ash), Is.Not.EqualTo(Color.white));
            Assert.That(Surfaces.DustTint(SurfaceKind.Wood), Is.Not.EqualTo(Surfaces.DustTint(SurfaceKind.Earth)));
        }
    }
}
