using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.Tests
{
    /// <summary>What makes the Bellringer, the Ash Commander and the Plague Stag fights of their own.</summary>
    public sealed class BossSignatureTests
    {
        private static SerializedProperty[] Attacks(string prefab)
        {
            var boss = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Game/Prefabs/{prefab}.prefab")
                .GetComponent<BossController>();
            var attacks = new SerializedObject(boss).FindProperty("_attacks");
            return Enumerable.Range(0, attacks.arraySize).Select(attacks.GetArrayElementAtIndex).ToArray();
        }

        private static SerializedProperty[] Strikes(string prefab) => Attacks(prefab)
            .Where(a => a.FindPropertyRelative("_kind").enumValueIndex == (int)BossAttackKind.GroundStrike)
            .ToArray();

        [Test]
        public void ASecondTollFallsIntoTheGapsOfTheFirst()
        {
            const float Width = 1.6f;
            var first = HazardPatterns.March(4, 2.4f, 3.2f, bothSides: true, shifted: false);
            var second = HazardPatterns.March(4, 2.4f, 3.2f, bothSides: true, shifted: true);
            Assert.That(first.Count, Is.EqualTo(8));
            Assert.That(second.Count(s => Mathf.Abs(s.Offset - 0.8f) < 0.01f), Is.EqualTo(1));
            foreach (var a in first)
            {
                foreach (var b in second)
                {
                    Assert.That(Mathf.Abs(a.Offset - b.Offset), Is.GreaterThanOrEqualTo(Width - 0.01f),
                        "A place safe from the first toll is struck by the second, never both at once.");
                }
            }
        }

        [Test]
        public void EachOfTheThreeHasAStrikeThatCallsSomethingOutOfTheFloor()
        {
            foreach (var prefab in new[] { "Bellringer", "AshCommander", "PlagueStag" })
            {
                var strikes = Strikes(prefab);
                Assert.That(strikes, Is.Not.Empty, prefab);
                foreach (var strike in strikes)
                {
                    var zone = strike.FindPropertyRelative("_spawn._prefab").objectReferenceValue as GameObject;
                    Assert.That(zone != null && zone.GetComponent<HazardZone>() != null, prefab);
                    Assert.That(strike.FindPropertyRelative("_spawn._pattern").enumValueIndex,
                        Is.Not.EqualTo((int)HazardPattern.Single), prefab);
                }
            }
        }

        [Test]
        public void TheThreeDoNotShareOnePattern()
        {
            Assert.That(Strikes("Bellringer").All(s => s.FindPropertyRelative("_spawn._bothSides").boolValue));
            Assert.That(Strikes("AshCommander").First().FindPropertyRelative("_spawn._bothSides").boolValue, Is.False);
            Assert.That(Strikes("PlagueStag").Single().FindPropertyRelative("_spawn._pattern").enumValueIndex,
                Is.EqualTo((int)HazardPattern.Rain));
            Assert.That(Attacks("PlagueStag").Count(a => a.FindPropertyRelative("_movesEverySwing").boolValue),
                Is.EqualTo(1), "The Stag's second phase has the charge that turns and comes back.");
            Assert.That(Attacks("PlagueStag").Count(a =>
                    a.FindPropertyRelative("_kind").enumValueIndex == (int)BossAttackKind.JumpSlam
                    && a.FindPropertyRelative("_spawn._prefab").objectReferenceValue != null),
                Is.EqualTo(2), "Wherever the Stag lands the ground stays poisoned.");
        }
    }
}
