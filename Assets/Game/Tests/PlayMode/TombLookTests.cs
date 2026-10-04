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
            // The painted Tomb (2026-10-04) draws each hanging slab's chains into its stone.
            Assert.That(GameObject.Find("Tomb").transform.Find("Painted/Terrain"), Is.Not.Null,
                "The slabs of the shafts hang from painted chains.");
            Assert.That(dressing.Count(r => r.name.EndsWith("_Chain")), Is.Zero,
                "No tiled chain is laid over the painted ones.");
            Assert.That(dressing.Where(r => r.name.EndsWith("_Up")), Is.Empty,
                "The Tomb from its text has walls up to its vaults; nothing is carried into the dark.");
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
