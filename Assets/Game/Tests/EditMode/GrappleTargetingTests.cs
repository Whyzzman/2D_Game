using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// Which ring the grapple takes (item 2 of the big list, 2026-09-28): the nearest in reach that
    /// the hero faces, overhead counting either way, never one behind a wall or out of reach.
    /// </summary>
    public sealed class GrappleTargetingTests
    {
        private static int Choose(int facing, params Vector2[] rings) =>
            GrappleTargeting.Choose(Vector2.zero, facing, new List<Vector2>(rings), 7.5f, null);

        [Test]
        public void TheNearestRingInFrontIsTaken()
        {
            Assert.That(Choose(1, new Vector2(5f, 3f), new Vector2(2f, 2f)), Is.EqualTo(1));
        }

        [Test]
        public void ARingBehindIsNotTakenButOneOverheadIs()
        {
            Assert.That(Choose(1, new Vector2(-3f, 2f)), Is.EqualTo(-1), "Turn round to take it.");
            Assert.That(Choose(-1, new Vector2(-3f, 2f)), Is.EqualTo(0));
            Assert.That(Choose(1, new Vector2(-0.3f, 5f)), Is.EqualTo(0), "Straight up counts either way.");
        }

        [Test]
        public void OutOfReachOrBehindAWallIsNotOffered()
        {
            Assert.That(Choose(1, new Vector2(7f, 4f)), Is.EqualTo(-1), "8.1 away, reach is 7.5.");
            var blocked = GrappleTargeting.Choose(Vector2.zero, 1,
                new List<Vector2> { new Vector2(2f, 2f), new Vector2(4f, 4f) }, 7.5f,
                (from, to) => to.x < 3f);
            Assert.That(blocked, Is.EqualTo(1), "The near ring is walled off; the far one is taken.");
        }
    }
}
