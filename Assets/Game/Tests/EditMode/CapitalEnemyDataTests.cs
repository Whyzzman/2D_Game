using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The capital's four enemies as built (item 2, stage C, 2026-09-28): each prefab carries the
    /// behaviour that makes it what the user chose — the raven flies, the gargoyle sleeps, the
    /// priest mends and steps away, the crossbowman shoots bolts from range.
    /// </summary>
    public sealed class CapitalEnemyDataTests
    {
        private static GameObject Prefab(string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Game/Prefabs/{name}.prefab");
            Assert.That(prefab, Is.Not.Null, name);
            return prefab;
        }

        [Test]
        public void TheRavenFliesTheGargoyleSleepsThePriestMends()
        {
            var raven = Prefab("AshRaven");
            Assert.That(raven.GetComponent<EnemyFlight>(), Is.Not.Null);
            Assert.That(raven.GetComponent<Rigidbody2D>().gravityScale, Is.EqualTo(0f), "It stays up.");
            Assert.That(Prefab("StoneGargoyle").GetComponent<EnemyDormancy>(), Is.Not.Null);
            Assert.That(Prefab("PalacePriest").GetComponent<EnemySupport>(), Is.Not.Null);
        }

        [Test]
        public void TheCrossbowmanAndThePriestShootAndKeepTheirDistance()
        {
            foreach (var name in new[] { "RoyalCrossbowman", "PalacePriest" })
            {
                var definition = Prefab(name).GetComponent<EnemyController>().Definition;
                Assert.That(definition.KeepsDistance, Is.True, name);
                Assert.That(definition.Attack(0).Spawn.Kind, Is.EqualTo(AttackSpawnKind.Projectile), name);
            }

            var bolt = Prefab("RoyalCrossbowman").GetComponent<EnemyController>().Definition.Attack(0).Attack;
            var arrow = Prefab("ForestArcher").GetComponent<EnemyController>().Definition.Attack(0).Attack;
            Assert.That(bolt.Damage, Is.GreaterThan(arrow.Damage), "Heavier than the forest's arrow…");
            Assert.That(bolt.Windup, Is.GreaterThan(arrow.Windup), "…and slower to loose, so it can be read.");
        }
    }
}
