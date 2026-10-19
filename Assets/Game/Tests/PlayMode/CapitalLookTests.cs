using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The painted Capital (item 6e, 2026-10-19, approved look design/levels/capital-look): its
    /// ground in pieces over the zone, the blocks only colliders, each room its own two-layer backdrop.
    /// </summary>
    public sealed class CapitalLookTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            yield return TestWorld.Load();
            yield return TestWorld.LoadEveryZone();
        }

        [TearDown]
        public void TearDown() => SaveSystem.Delete();

        [UnityTest]
        public IEnumerator TheCapitalIsPaintedRatherThanTiled()
        {
            yield return null;
            var painted = Painted();
            Assert.That(painted, Is.Not.Empty, "The Capital has its painted art.");
            var ground = painted.Select(p => p.Find("Terrain")).Where(t => t != null)
                .SelectMany(t => t.GetComponentsInChildren<SpriteRenderer>()).ToArray();
            Assert.That(ground.Length, Is.GreaterThanOrEqualTo(30), "The ground covers the zone.");
            var tiled = GameObject.Find("room_capital_angel_square").GetComponentsInChildren<BoxCollider2D>()
                .Where(b => b.name.StartsWith("Solid_")).SelectMany(b => b.GetComponentsInChildren<SpriteRenderer>());
            Assert.That(tiled, Is.Empty, "A block is a collider; the picture is the painter's.");
        }

        [UnityTest]
        public IEnumerator EveryCapitalRoomHasItsOwnPaintedBackdrop()
        {
            yield return null;
            var all = Painted().Select(p => p.Find("Rooms")).Where(r => r != null).ToArray();
            var sprites = new System.Collections.Generic.HashSet<Sprite>();
            foreach (var room in CapitalRooms())
            {
                var holder = all.Select(r => r.Find("Backdrop_" + room)).FirstOrDefault(h => h != null);
                Assert.That(holder, Is.Not.Null, $"{room} has no backdrop.");
                var far = holder.Find("Far").GetComponent<SpriteRenderer>();
                var wall = holder.Find("Wall").GetComponent<SpriteRenderer>();
                Assert.That(far.GetComponent<RoomParallax>(), Is.Not.Null);
                Assert.That(sprites.Add(far.sprite) && sprites.Add(wall.sprite), Is.True, $"{room} repeats a picture.");
            }
        }

        private static string[] CapitalRooms()
        {
            var text = System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath, "Game/Levels/capital.txt"));
            return LevelZone.Parse(text).Rooms.Select(r => r.Id).ToArray();
        }

        /// <summary>The world is cut by zone, so each zone's scene holds its own share of the Capital's root.</summary>
        private static Transform[] Painted() =>
            Object.FindObjectsByType<Transform>()
                .Where(t => t.name == "Painted" && t.parent != null && t.parent.name == "AshenCapital").ToArray();
    }
}
