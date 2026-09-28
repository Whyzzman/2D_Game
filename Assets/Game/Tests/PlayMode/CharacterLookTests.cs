using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The characters in the level's light (visual item 3, 2026-09-28): lit sprites with a fill of
    /// their own so they are never darker than before, point lights that read their normal maps,
    /// and training targets that rock when struck.
    /// </summary>
    public sealed class CharacterLookTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            yield return SceneManager.LoadSceneAsync("TombTrial");
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            SaveSystem.Delete();
        }

        [UnityTest]
        public IEnumerator EveryFigureIsALitSprite()
        {
            yield return null;
            var bodies = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include)
                .Where(r => r.name == "Body" && r.sortingLayerName == "Characters")
                .ToArray();

            Assert.That(bodies.Length, Is.GreaterThan(10));
            foreach (var body in bodies)
            {
                Assert.That(body.sharedMaterial != null && body.sharedMaterial.name.Contains("Sprite-Lit"), Is.True,
                    $"{body.transform.parent.parent.name} is unlit: no light can shape it.");
            }
        }

        [UnityTest]
        public IEnumerator TheCharactersHaveTheirOwnFillAndTheZoneLightLeavesThemAlone()
        {
            yield return null;
            var characters = SortingLayer.NameToID("Characters");
            var globals = Object.FindObjectsByType<Light2D>().Where(l => l.lightType == Light2D.LightType.Global).ToArray();
            var fill = globals.SingleOrDefault(l => l.name == "CharacterFill");

            Assert.That(fill, Is.Not.Null);
            Assert.That(fill.intensity, Is.GreaterThanOrEqualTo(0.95f), "As bright as they were unlit.");
            Assert.That(fill.targetSortingLayers, Is.EquivalentTo(new[] { characters }));
            foreach (var ambient in globals.Where(l => l != fill))
            {
                Assert.That(ambient.targetSortingLayers.Contains(characters), Is.False,
                    "Two global lights on one layer: only one of them would count.");
            }
        }

        [UnityTest]
        public IEnumerator EveryPointLightShapesByNormalMaps()
        {
            yield return null;
            var lights = Object.FindObjectsByType<Light2D>()
                .Where(l => l.lightType == Light2D.LightType.Point)
                .ToArray();

            Assert.That(lights.Length, Is.GreaterThan(5));
            foreach (var light in lights)
            {
                Assert.That(light.normalMapQuality, Is.Not.EqualTo(Light2D.NormalMapQuality.Disabled), light.name);
            }
        }

        [UnityTest]
        public IEnumerator AStruckDummyRocksAndSettles()
        {
            var dummy = Object.FindAnyObjectByType<TrainingDummy>();
            var wobble = dummy.GetComponent<HitWobble>();
            Assert.That(wobble, Is.Not.Null);

            dummy.Actor.ApplyDamage(new DamageInfo(5f, Vector2.right));
            var peak = 0f;
            var until = Time.realtimeSinceStartup + 0.4f;
            while (Time.realtimeSinceStartup < until)
            {
                yield return null;
                peak = Mathf.Max(peak, Mathf.Abs(wobble.Angle));
            }

            Assert.That(peak, Is.GreaterThan(3f), "The blow rocks it.");
            until = Time.realtimeSinceStartup + 3f;
            while (Time.realtimeSinceStartup < until)
            {
                yield return null;
            }

            Assert.That(Mathf.Abs(wobble.Angle), Is.LessThan(0.5f), "And it settles back upright.");
        }
    }
}
