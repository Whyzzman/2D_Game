using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// Enemy animation 2.0 in play (visual item 4, 2026-09-28): a landed blow squashes the body
    /// and it springs back; loose pieces hang off the body they belong to.
    /// </summary>
    public sealed class EnemyLifeTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            yield return TestWorld.Load();
            yield return null;
        }

        [TearDown]
        public void TearDown() => SaveSystem.Delete();

        [UnityTest]
        public IEnumerator ALandedBlowSquashesTheBodyAndItSpringsBack()
        {
            var zombie = Object.FindObjectsByType<EnemyController>().First(e => e.name.Contains("Zombie"));
            var recoil = zombie.GetComponent<HitRecoil>();
            var body = zombie.transform.Find("Visual/Body");
            Assert.That(recoil, Is.Not.Null);

            zombie.Actor.ApplyDamage(new DamageInfo(1f, Vector2.right));
            yield return null;
            Assert.That(body.localScale.y, Is.LessThan(0.95f), "The blow flattens it for a moment.");
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(0f, body.localEulerAngles.z)), Is.GreaterThan(2f),
                "And rocks it away from the blow.");

            var until = Time.realtimeSinceStartup + 0.8f;
            while (Time.realtimeSinceStartup < until)
            {
                yield return null;
            }

            Assert.That(body.localScale.y, Is.EqualTo(1f).Within(0.01f), "It springs back upright.");
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(0f, body.localEulerAngles.z)), Is.LessThan(0.5f));
        }

        [UnityTest]
        public IEnumerator LoosePiecesHangOffTheirOwnBodies()
        {
            var until = Time.realtimeSinceStartup + 0.5f;
            while (Time.realtimeSinceStartup < until)
            {
                yield return null;
            }

            var trails = Object.FindObjectsByType<CloakTail>()
                .Where(t => t.GetComponentInParent<EnemyController>() != null
                    || t.GetComponentInParent<BossController>() != null)
                .ToArray();
            Assert.That(trails.Length, Is.GreaterThanOrEqualTo(5), "Rags, tails, hoods, robes, scarves.");
            foreach (var trail in trails)
            {
                var body = trail.transform.parent.Find("Body").GetComponent<SpriteRenderer>();
                Assert.That(body.bounds.SqrDistance(trail.Root), Is.LessThan(0.3f * 0.3f),
                    $"{trail.transform.parent.parent.name}'s piece floats off its body.");
            }
        }
    }
}
