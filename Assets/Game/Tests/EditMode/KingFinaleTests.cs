using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The King's third phase and the farewell (item 1 of the 2026-11-12 list, gamedesign/KING_FINALE.md):
    /// when each phase begins, the eclipse, the oath's walls closing in, the King's data.
    /// </summary>
    public sealed class KingFinaleTests
    {
        [Test]
        public void APhaseFollowsTheHealthLeftAndAThirdOnlyForABossThatHasOne()
        {
            Assert.That(BossController.PhaseFor(0.8f, 3, 0.5f, 0.3f), Is.EqualTo(1));
            Assert.That(BossController.PhaseFor(0.5f, 3, 0.5f, 0.3f), Is.EqualTo(2));
            Assert.That(BossController.PhaseFor(0.3f, 3, 0.5f, 0.3f), Is.EqualTo(3));
            Assert.That(BossController.PhaseFor(0.1f, 2, 0.5f, 0.3f), Is.EqualTo(2), "Two phases stay two.");
            Assert.That(BossController.PhaseFor(0.1f, 1, 0.5f, 0.3f), Is.EqualTo(1));
        }

        [Test]
        public void OnlyAThirdPhaseIsUnderTheEclipse()
        {
            Assert.That(ScreenGrade.EclipseTarget(true, 3), Is.EqualTo(1f));
            Assert.That(ScreenGrade.EclipseTarget(true, 2), Is.EqualTo(0f));
            Assert.That(ScreenGrade.EclipseTarget(false, 3), Is.EqualTo(0f), "The fight over, the light comes back.");
        }

        [Test]
        public void TheOathsWallsStartAtTheEndsOfBothSidesAndCloseIn()
        {
            // a short side (a wall three steps out) and a long one (five): both fronts start at once
            var steps = new List<HazardPatterns.Step>
            {
                new HazardPatterns.Step(2f, 0), new HazardPatterns.Step(4f, 1), new HazardPatterns.Step(6f, 2),
                new HazardPatterns.Step(-2f, 0), new HazardPatterns.Step(-4f, 1), new HazardPatterns.Step(-6f, 2),
                new HazardPatterns.Step(-8f, 3), new HazardPatterns.Step(-10f, 4)
            };
            var times = HazardPatterns.ConvergeTimes(steps, 0.1f);
            Assert.That(times[2], Is.EqualTo(0f), "The far end of the short side drops first.");
            Assert.That(times[7], Is.EqualTo(0f), "And of the long side at the same moment.");
            Assert.That(times[0], Is.EqualTo(0.2f).Within(1e-5f), "The nearest last on its side.");
            Assert.That(times[3], Is.EqualTo(0.4f).Within(1e-5f));
            for (var i = 1; i < 3; i++)
            {
                Assert.That(times[i], Is.LessThan(times[i - 1]), "Farther drops sooner: the wall closes in.");
            }
        }

        [Test]
        public void TheKingHasAThirdPhaseWithHisOath()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/OathBoundKing.prefab");
            Assert.That(prefab, Is.Not.Null);
            var king = prefab.GetComponent<BossController>();
            Assert.That(king.TotalPhases, Is.EqualTo(3));
            Assert.That(king.PhaseThreeHealthFraction, Is.InRange(0.15f, king.PhaseTwoHealthFraction - 0.1f));

            var attacks = new SerializedObject(king).FindProperty("_attacks");
            var oath = Enumerable.Range(0, attacks.arraySize).Select(attacks.GetArrayElementAtIndex)
                .Where(a => a.FindPropertyRelative("_fromPhase").intValue == 3).ToList();
            Assert.That(oath.Count, Is.EqualTo(1), "The eclipse has its own move.");
            Assert.That(oath[0].FindPropertyRelative("_spawn._pattern").enumValueIndex,
                Is.EqualTo((int)HazardPattern.Converge));
            Assert.That(oath[0].FindPropertyRelative("_spawn._bothSides").boolValue, Is.True);
            var wall = (GameObject)oath[0].FindPropertyRelative("_spawn._prefab").objectReferenceValue;
            Assert.That(wall, Is.Not.Null);
            Assert.That(wall.GetComponent<HazardZone>().Size.y, Is.LessThan(1.9f), "Low enough to jump.");

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Game/Prefabs" }))
            {
                var boss = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid))
                    .GetComponent<BossController>();
                if (boss != null && boss != king)
                {
                    Assert.That(boss.TotalPhases, Is.LessThanOrEqualTo(2), $"{boss.BossId}: the eclipse is the King's alone.");
                }
            }
        }

        [Test]
        public void TheFarewellIsTwoLinesBeforeTheEnding()
        {
            Assert.That(EndingSequence.FarewellLines, Has.Length.EqualTo(2));
            Assert.That(EndingSequence.FarewellLines.All(l => !string.IsNullOrWhiteSpace(l)), Is.True);
            Assert.That(EndingSequence.FarewellZoom, Is.InRange(0.6f, 1f), "Drawn in, within what the camera allows.");
        }
    }
}
