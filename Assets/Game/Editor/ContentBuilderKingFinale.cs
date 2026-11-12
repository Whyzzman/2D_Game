using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The King's third phase, the eclipse (item 1 of the 2026-11-12 list; decided by Claude under the
    /// user's standing consent, gamedesign/KING_FINALE.md). At a quarter of his health and a little more the
    /// light goes out of the throne hall (<see cref="ScreenGrade"/>), his crown is the light left
    /// (<see cref="BossPhaseLight"/>), everything comes faster still — and he has one thing nobody else
    /// has: his oath, «Моє слово — стіна», made real. He drives the greatsword into the floor and fire
    /// rises at both walls of the hall and closes in on him, one step at a time.
    /// </summary>
    public static partial class ContentBuilder
    {
        private const string OathWallPrefabPath = PrefabFolder + "/OathWall.prefab";

        /// <summary>Phase three's numbers on the King's controller.</summary>
        private static void SetKingPhaseThree(BossController boss)
        {
            SetInt(boss, "_totalPhases", 3);
            SetFloat(boss, "_phaseThreeHealthFraction", 0.3f);
            SetFloat(boss, "_phaseThreeTransitionTime", 3f);
            SetFloat(boss, "_phaseThreeAttackSpeed", 1.4f);
            SetFloat(boss, "_phaseThreePauseScale", 0.5f);
            SetFloat(boss, "_phaseThreeMoveScale", 1.4f);
        }

        /// <summary>
        /// The wall of the oath. Low flame, so a jump clears it; both fronts start at the hall's walls and
        /// meet two steps short of him, where the greatsword already came down. The answers: jump the
        /// front as it passes, dash through it, or stand inside his reach before the blow and leave after.
        /// </summary>
        private static void AddKingOathWall(BossController boss)
        {
            var wall = BuildHazardPrefab("OathWall", OathWallPrefabPath, new Vector2(1.8f, 1.3f),
                new Color(1f, 0.62f, 0.3f, 0.32f), new Color(1f, 0.5f, 0.2f, 0.85f),
                telegraphTime: 0.5f, activeTime: 0.3f, dormantTime: 0f, StatusType.Burn,
                buildupPerSecond: 30f, damagePerTick: 22f, DamageType.Fire);

            var oath = AppendBossAttack(boss);
            SetBossAttack(oath.element, BossAttackKind.GroundStrike, 3, 0,
                0f, 16f, 6.5f, 0.3f, BossAttackMovement.None, 0f, false, new[]
                {
                    Swing(30f, 50f, 9f, 0.9f, 0.14f, 1.2f, 1.8f, 1.6f, 2.6f)
                }, parryable: false);
            oath.serialized.ApplyModifiedPropertiesWithoutUndo();
            SetBossSpawn(boss, oath.index, AttackSpawnKind.Hazard, wall, new Vector2(2.2f, 0f), atTarget: false);
            SetBossPattern(boss, oath.index, HazardPattern.Converge, count: 8, interval: 0.13f, spacing: 1.9f,
                bothSides: true, alternate: false);
            EditorUtility.SetDirty(boss);
        }
    }
}
