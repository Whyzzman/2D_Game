using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The forest's look in the scene (visual item 1, 2026-09-28): the sky stays behind the view
    /// even at the top of the climb, every walkable ledge has a living edge, the abbey stands
    /// behind the whole Warden fight, and none of the dressing can be stood on.
    /// </summary>
    public sealed class ForestLookTests
    {
        private PlayerCharacter _player;
        private PlayerMotor2D _motor;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            yield return TestWorld.Load();
            _player = Object.FindAnyObjectByType<PlayerCharacter>();
            _motor = _player.GetComponent<PlayerMotor2D>();
            foreach (var boss in Object.FindObjectsByType<BossController>())
            {
                boss.gameObject.SetActive(false);
            }

            _player.GetComponent<CombatActor>().SetInvulnerable(ImmunitySource.Script, 99f);
        }

        [TearDown]
        public void TearDown()
        {
            SaveSystem.Delete();
            GameTimeScale.Reset();
        }

        [UnityTest]
        public IEnumerator TheForestIsPaintedRatherThanTiled()
        {
            // Since the zone rework reached the Forest (2026-10-19, approved look design/levels/forest-look)
            // the painter draws it: ground in pieces over the whole zone, the blocks only colliders.
            yield return TestWorld.LoadEveryZone();
            var painted = Painted();
            Assert.That(painted, Is.Not.Empty, "The Forest has its painted art.");
            var ground = painted.Select(p => p.Find("Terrain")).Where(t => t != null)
                .SelectMany(t => t.GetComponentsInChildren<SpriteRenderer>()).ToArray();
            Assert.That(ground.Length, Is.GreaterThanOrEqualTo(40), "The ground covers the zone.");
            Assert.That(ground.All(r => r.sprite != null && r.sortingLayerName == "GameplayEnvironment"), Is.True);
            var tiled = GameObject.Find("room_forest_entrance").GetComponentsInChildren<BoxCollider2D>()
                .Where(b => b.name.StartsWith("Solid_")).SelectMany(b => b.GetComponentsInChildren<SpriteRenderer>());
            Assert.That(tiled, Is.Empty, "A block is a collider; the picture is the painter's.");
            Assert.That(GameObject.Find("ForestParallax"), Is.Null, "The rooms paint their own sky.");
        }

        [UnityTest]
        public IEnumerator EveryForestRoomHasItsOwnPaintedBackdrop()
        {
            // No picture repeats from room to room (the user's rule, 2026-10-03).
            yield return TestWorld.LoadEveryZone();
            var all = Painted().Select(p => p.Find("Rooms")).Where(r => r != null).ToArray();
            var sprites = new System.Collections.Generic.HashSet<Sprite>();
            foreach (var room in ForestScene.Layout.Rooms)
            {
                var holder = all.Select(r => r.Find("Backdrop_" + room.Id)).FirstOrDefault(h => h != null);
                Assert.That(holder, Is.Not.Null, $"{room.Id} has no backdrop.");
                var far = holder.Find("Far").GetComponent<SpriteRenderer>();
                var wall = holder.Find("Wall").GetComponent<SpriteRenderer>();
                Assert.That(far.maskInteraction, Is.EqualTo(SpriteMaskInteraction.VisibleInsideMask));
                Assert.That(far.GetComponent<RoomParallax>(), Is.Not.Null);
                Assert.That(wall.sortingLayerName, Is.EqualTo("BackgroundNear"));
                Assert.That(sprites.Add(far.sprite) && sprites.Add(wall.sprite), Is.True, $"{room.Id} repeats a picture.");
            }
        }

        [UnityTest]
        public IEnumerator TheMoonlightCanNeverBeStoodOn()
        {
            yield return null;
            var pieces = GameObject.Find("ForestLook").GetComponentsInChildren<Transform>();
            Assert.That(pieces.Length, Is.GreaterThan(8));
            foreach (var piece in pieces)
            {
                Assert.That(piece.GetComponent<Collider2D>(), Is.Null, $"{piece.name} carries collision.");
            }
        }

        [UnityTest]
        public IEnumerator MoonlightFallsOnlyInTheForest()
        {
            yield return null;
            var look = GameObject.Find("ForestLook");
            Assert.That(look, Is.Not.Null);
            Assert.That(look.GetComponent<ZoneBackdrop>().ZoneId, Is.EqualTo("zone_withered_forest"));
            Assert.That(look.GetComponentsInChildren<SpriteRenderer>().Length, Is.GreaterThanOrEqualTo(8));
        }

        // ---------- helpers ----------

        /// <summary>The world is cut by zone, so each zone's scene holds its own share of the Forest's root.</summary>
        private static Transform[] Painted() =>
            Object.FindObjectsByType<Transform>()
                .Where(t => t.name == "Painted" && t.parent != null && t.parent.name == "WitheredForest").ToArray();
    }
}
