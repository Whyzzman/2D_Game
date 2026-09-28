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
        public IEnumerator TheSkyStaysBehindTheViewAtTheTopOfTheClimb()
        {
            // The layers were thirteen units tall: at the top of the climb the camera saw its own
            // clear colour over the trees.
            _motor.ResetTo(new Vector2(198f, 14f));
            yield return Settle(2.5f);

            var camera = Camera.main;
            Assert.That(camera.transform.position.y, Is.GreaterThan(9f), "The view went up the climb.");
            var top = camera.transform.position.y + camera.orthographicSize;
            var bottom = camera.transform.position.y - camera.orthographicSize;
            var far = Strip("env_forest_bg_far_01");
            // Past the layer's top the camera's clear colour shows, and the sky is painted to end
            // in exactly that colour (ForestLookDataTests): either way the view sees sky.
            if (far.bounds.max.y < top)
            {
                var clear = camera.backgroundColor;
                Assert.That(clear.r * 255f, Is.EqualTo(10f).Within(2f), "The sky runs on into the clear colour.");
                Assert.That(clear.g * 255f, Is.EqualTo(11f).Within(2f));
                Assert.That(clear.b * 255f, Is.EqualTo(17f).Within(2f));
            }

            Assert.That(far.bounds.max.y, Is.GreaterThan(top - 3f), "Most of the view is painted sky.");
            Assert.That(far.bounds.min.y, Is.LessThan(bottom));
            var halfWidth = camera.orthographicSize * camera.aspect;
            Assert.That(far.bounds.min.x, Is.LessThan(camera.transform.position.x - halfWidth));
            Assert.That(far.bounds.max.x, Is.GreaterThan(camera.transform.position.x + halfWidth));
        }

        [UnityTest]
        public IEnumerator EveryWalkableForestLedgeHasALivingEdge()
        {
            yield return null;
            var blocks = Object.FindObjectsByType<BoxCollider2D>()
                .Where(b => b.gameObject.layer == 9 && !b.isTrigger && b.bounds.center.x > 62.5f
                    && b.bounds.size.x >= 0.8f && b.GetComponent<NonClimbableSurface>() == null)
                .Where(b => IsWalkable(b))
                .ToArray();

            Assert.That(blocks.Length, Is.GreaterThan(12), "The forest's ground was found.");
            foreach (var block in blocks)
            {
                Assert.That(block.transform.Find("ForestEdgeTop"), Is.Not.Null,
                    $"{block.name} is a bare slab with no moss on its edge.");
                if (block.bounds.min.y > 0.2f && block.bounds.size.x >= 1.2f)
                {
                    Assert.That(block.transform.Find("ForestEdgeRoots"), Is.Not.Null,
                        $"{block.name} floats with nothing growing under it.");
                }
            }
        }

        [UnityTest]
        public IEnumerator TheDressingCanNeverBeStoodOn()
        {
            yield return null;
            var dressing = Object.FindObjectsByType<Transform>()
                .Where(t => t.name.StartsWith("ForestEdge"))
                .Concat(GameObject.Find("ForestLook").GetComponentsInChildren<Transform>())
                .ToArray();

            Assert.That(dressing.Length, Is.GreaterThan(20));
            foreach (var piece in dressing)
            {
                Assert.That(piece.GetComponent<Collider2D>(), Is.Null, $"{piece.name} carries collision.");
            }
        }

        [UnityTest]
        public IEnumerator TheAbbeyStandsBehindTheWholeWardenFight()
        {
            yield return null;
            var abbey = GameObject.Find("env_forest_arena_backdrop").GetComponent<SpriteRenderer>();
            var floor = GameObject.Find("WardenArenaFloor").GetComponent<BoxCollider2D>().bounds;

            Assert.That(abbey.bounds.min.x, Is.LessThan(floor.min.x));
            Assert.That(abbey.bounds.max.x, Is.GreaterThan(floor.max.x));
            Assert.That(abbey.bounds.max.y, Is.GreaterThan(12f), "Its gable and sky fill the widened fight view.");
            var layers = SortingLayer.layers.Select(l => l.name).ToList();
            Assert.That(layers.IndexOf(abbey.sortingLayerName), Is.LessThan(layers.IndexOf("Characters")));
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

        private static SpriteRenderer Strip(string name) =>
            GameObject.Find("ForestParallax").GetComponentsInChildren<SpriteRenderer>()
                .First(r => r.name == name);

        private static bool IsWalkable(BoxCollider2D block)
        {
            var visual = block.transform.Find("Visual");
            var sprite = visual != null ? visual.GetComponent<SpriteRenderer>().sprite : null;
            return sprite != null && (sprite.name == "env_forest_ground_01" || sprite.name == "env_forest_road_01"
                || sprite.name == "env_forest_cliff_01");
        }

        private static IEnumerator Settle(float seconds)
        {
            var until = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < until)
            {
                yield return null;
            }
        }
    }
}
