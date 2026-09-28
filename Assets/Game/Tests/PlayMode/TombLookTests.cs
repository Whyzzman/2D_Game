using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The Tomb's readability pass (2026-09-28): the hanging walls reach the vault, floating slabs
    /// hang from chains, none of it can be touched, and a find on the floor has its halo.
    /// </summary>
    public sealed class TombLookTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            yield return TestWorld.Load();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SaveSystem.Delete();
            yield return null;
        }

        [UnityTest]
        public IEnumerator NothingInTheTombHangsFromNothing()
        {
            var dressing = Object.FindObjectsByType<SpriteRenderer>()
                .Where(r => r.transform.parent != null && r.transform.parent.name == "TombLook").ToArray();
            var carried = dressing.Where(r => r.name.StartsWith("BellShaft") && r.name.EndsWith("_Up")).ToArray();
            // The eastern wall goes up into the vault; the western one already carries the bell loft.
            Assert.That(carried.Select(r => r.name), Is.EqualTo(new[] { "BellShaftEast_Up" }));
            Assert.That(carried[0].bounds.max.y, Is.GreaterThanOrEqualTo(19.9f));
            var loft = Object.FindObjectsByType<BoxCollider2D>().Single(b => b.name == "BellLoft");
            var west = Object.FindObjectsByType<BoxCollider2D>().Single(b => b.name == "BellShaftWest");
            Assert.That(loft.bounds.min.y - west.bounds.max.y, Is.LessThan(0.05f), "The west wall holds the loft up.");
            Assert.That(dressing.Count(r => r.name.EndsWith("_Chain")), Is.GreaterThanOrEqualTo(6),
                "The floating slabs hang from chains.");
            Assert.That(dressing.Any(r => r.GetComponent<Collider2D>() != null), Is.False,
                "Dressing is never solid.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator AFindOnTheFloorGlows()
        {
            var pickup = Object.FindObjectsByType<WorldPickup>().First();
            var halo = pickup.GetComponentsInChildren<SpriteRenderer>().FirstOrDefault(r => r.name == "Halo");
            Assert.That(halo, Is.Not.Null);
            Assert.That(halo.sprite, Is.Not.Null);
            yield return null;
        }
    }
}
