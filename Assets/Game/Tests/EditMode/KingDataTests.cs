using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// «Король-Заклятий» as data (item 2 of the big list, stage D): a heavy knight whose rage adds
    /// a shockwave, a burning scar and a second charge; and the two finds in the old zones that
    /// only his grapple reaches.
    /// </summary>
    public sealed class KingDataTests
    {
        private const string King = "ashen-king";

        private static BossController LoadKing()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/OathBoundKing.prefab");
            Assert.That(prefab, Is.Not.Null);
            return prefab.GetComponent<BossController>();
        }

        [Test]
        public void TheKingIsAHeavyKnightWithARagePhase()
        {
            var king = LoadKing();
            Assert.That(king.BossId, Is.EqualTo(King));
            Assert.That(king.DisplayName, Is.EqualTo("Король-Заклятий"));
            Assert.That(king.GetComponent<Health>().Max, Is.GreaterThan(900f), "Heavier than the Guardian.");

            var serialized = new SerializedObject(king);
            var attacks = serialized.FindProperty("_attacks");
            var phaseTwo = Enumerable.Range(0, attacks.arraySize)
                .Select(attacks.GetArrayElementAtIndex)
                .Where(a => a.FindPropertyRelative("_fromPhase").intValue == 2)
                .ToList();
            Assert.That(phaseTwo.Count, Is.EqualTo(3), "The rage has moves of its own.");

            var slam = phaseTwo.Single(a => a.FindPropertyRelative("_kind").enumValueIndex
                == (int)BossAttackKind.JumpSlam);
            Assert.That(slam.FindPropertyRelative("_endsWithShockwave").boolValue, Is.True);
            Assert.That(slam.FindPropertyRelative("_spawn._prefab").objectReferenceValue, Is.Not.Null,
                "The enraged slam leaves the floor burning.");
            Assert.That(serialized.FindProperty("_shockwave").objectReferenceValue, Is.Not.Null);
            Assert.That(serialized.FindProperty("_shieldVisual").objectReferenceValue, Is.Null, "No shield.");
        }

        [Test]
        public void EveryMoveHasItsFrames()
        {
            var set = AssetDatabase.LoadAssetAtPath<BossAnimationSet>("Assets/Game/Data/Animation/KingAnimations.asset");
            Assert.That(set, Is.Not.Null);
            foreach (var state in set.RequiredStates())
            {
                Assert.That(set.Clip(state), Is.Not.Null, state.ToString());
                Assert.That(set.Clip(state).FrameCount, Is.GreaterThan(0), state.ToString());
            }

            Assert.That(set.RequiredStates(), Has.No.Member(BossAnimationState.ShieldBash));
        }

        [Test]
        public void TheGrappleReachesOneNewPlaceInEachOldZone()
        {
            var graph = AssetDatabase.LoadAssetAtPath<WorldGraph>("Assets/Game/Data/WorldGraph.asset");
            var cache = graph.Area("room_tomb_crypt_nest");
            var hollow = graph.Area("room_forest_hunter_hollow");
            Assert.That(cache.ZoneId, Is.EqualTo("zone_forgotten_tomb"));
            Assert.That(hollow.ZoneId, Is.EqualTo("zone_withered_forest"));
            Assert.That(cache.IsSecret && hollow.IsSecret, Is.True);
            Assert.That(cache.RequiredAbilityId, Is.EqualTo(AbilityIds.Grapple));
            Assert.That(hollow.RequiredAbilityId, Is.EqualTo(AbilityIds.Grapple));
            Assert.That(graph.Connection("conn_tomb_collapsed_crypt_crypt_nest").RequiredAbilityId, Is.EqualTo(AbilityIds.Grapple));
            Assert.That(graph.Connection("conn_forest_grove_hollow").RequiredAbilityId, Is.EqualTo(AbilityIds.Grapple));
            Assert.That(WorldGraphValidator.Validate(graph), Is.Empty);

            var items = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/Game/Data/ItemDatabase.asset");
            Assert.That(items.GetItem("king_seal"), Is.Not.Null);
            Assert.That(items.GetItem("hunter_talon"), Is.Not.Null);
        }
    }
}
