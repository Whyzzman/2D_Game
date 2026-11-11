using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>The patterns the bosses' ground strikes lay out (BOSSES_SIGNATURES.md), on a real floor.</summary>
    public sealed class HazardVolleyTests
    {
        private GameObject _floor;
        private GameObject _wall;

        [SetUp]
        public void BuildFloor()
        {
            // far from any loaded level: a floor 24 wide with a wall 7 to the right of its middle
            _floor = Block(new Vector2(5000f, 99.5f), new Vector2(24f, 1f));
            _wall = Block(new Vector2(5007f, 102f), new Vector2(1f, 4f));
        }

        [TearDown]
        public void Sweep()
        {
            HazardZone.ClearSpawned(Faction.Enemy);
            Object.Destroy(_floor);
            Object.Destroy(_wall);
        }

        private static GameObject Block(Vector2 at, Vector2 size)
        {
            var block = new GameObject("TestGround") { layer = 9 };
            block.transform.position = at;
            block.AddComponent<BoxCollider2D>().size = size;
            return block;
        }

        private static GameObject Prefab(string name) =>
            AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Game/Prefabs/{name}.prefab");

        private static HazardZone[] Zones() => Object.FindObjectsByType<HazardZone>(FindObjectsInactive.Exclude)
            .Where(z => z.transform.position.x > 4900f).ToArray();

        private static IEnumerator Wait(float seconds)
        {
            var until = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < until)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator ARowRestsOnTheFloorAndStopsAtTheWall()
        {
            yield return new WaitForFixedUpdate();
            var steps = HazardPatterns.March(4, 2.4f, 3.2f, bothSides: true, shifted: false);
            HazardVolley.March(Prefab("BellToll"), Faction.Enemy, null, new Vector2(5000f, 101.3f), 1, steps, 0.05f);
            yield return Wait(0.45f);

            var zones = Zones();
            // left: 2.4, 5.6, 8.8, 12 (the last is past the floor's end at 12); right: 2.4, 5.6, then the wall
            var right = zones.Count(z => z.transform.position.x > 5000f);
            var left = zones.Count(z => z.transform.position.x < 5000f);
            Assert.That(right, Is.EqualTo(2), "Nothing falls behind the wall.");
            Assert.That(left, Is.InRange(3, 4));
            foreach (var zone in zones)
            {
                Assert.That(zone.transform.position.y - zone.Size.y * 0.5f, Is.EqualTo(100f).Within(0.15f),
                    "Each zone stands on the floor.");
                Assert.That(zone.Phase, Is.EqualTo(HazardPhase.Telegraph), "Seen before it bites.");
            }
        }

        [UnityTest]
        public IEnumerator RainFollowsTheTargetAndASweepStopsIt()
        {
            yield return new WaitForFixedUpdate();
            var target = new GameObject("Target");
            target.transform.position = new Vector2(4996f, 103f);
            HazardVolley.Rain(Prefab("SkullFall"), Faction.Enemy, null, target.transform, 3, 0.3f);
            yield return Wait(0.1f);
            Assert.That(Zones().Length, Is.EqualTo(1));
            Assert.That(Zones()[0].transform.position.y, Is.LessThan(102f), "Under a jump the zone is on the floor.");

            target.transform.position = new Vector2(4993f, 101f);
            yield return Wait(0.3f);
            Assert.That(Zones().Any(z => Mathf.Abs(z.transform.position.x - 4993f) < 0.1f), "The second finds the new place.");

            HazardZone.ClearSpawned(Faction.Enemy);
            yield return Wait(0.5f);
            Assert.That(Zones(), Is.Empty, "A reset stops the skulls still to fall.");
            Assert.That(HazardVolley.RunningCount, Is.EqualTo(0));
            Object.Destroy(target);
        }
    }
}
