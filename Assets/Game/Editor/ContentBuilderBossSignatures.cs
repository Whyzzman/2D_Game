using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// What makes three bosses their own fights (2026-11-11, decided by Claude under the user's standing
    /// approval; gamedesign/BOSSES_SIGNATURES.md). The Bellringer, the Ash Commander and the Plague Stag
    /// shared one move list with different numbers; each now has a thing the others do not, all of it out
    /// of the same parts — a weapon driven into the floor and <see cref="HazardZone"/>s in a pattern.
    /// </summary>
    public static partial class ContentBuilder
    {
        private const string BellTollPrefabPath = PrefabFolder + "/BellToll.prefab";
        private const string RubbleBurstPrefabPath = PrefabFolder + "/RubbleBurst.prefab";
        private const string PlaguePoolPrefabPath = PrefabFolder + "/PlaguePool.prefab";
        private const string PlaguePoolLongPrefabPath = PrefabFolder + "/PlaguePoolLong.prefab";
        private const string SkullFallPrefabPath = PrefabFolder + "/SkullFall.prefab";

        /// <summary>
        /// The toll. He strikes the floor where he stands and the belfry answers: stone falls in a row both
        /// ways with gaps a body fits in — and the hammer itself covers the ground in front of him, so the
        /// places to be are a gap or his back. The second phase tolls twice, and the second row falls into
        /// the gaps of the first.
        /// </summary>
        private static void AddBellringerSignature(BossController boss)
        {
            var toll = BuildHazardPrefab("BellToll", BellTollPrefabPath, new Vector2(1.6f, 2.6f),
                new Color(1f, 0.8f, 0.42f, 0.3f), new Color(1f, 0.86f, 0.55f, 0.78f),
                telegraphTime: 0.6f, activeTime: 0.35f, dormantTime: 0f, StatusType.Bleed,
                buildupPerSecond: 0f, damagePerTick: 20f, DamageType.Physical,
                drop: new Color(0.62f, 0.56f, 0.48f));

            var first = AppendBossAttack(boss);
            SetBossAttack(first.element, BossAttackKind.GroundStrike, 1, 1,
                0f, 12f, 7.5f, 0.3f, BossAttackMovement.None, 0f, false, new[]
                {
                    Swing(24f, 40f, 8f, 0.8f, 0.14f, 1.0f, 1.8f, 1.6f, 2.4f)
                }, parryable: false);
            first.serialized.ApplyModifiedPropertiesWithoutUndo();
            SetBossSpawn(boss, first.index, AttackSpawnKind.Hazard, toll, new Vector2(2.4f, 0f), atTarget: false);
            SetBossPattern(boss, first.index, HazardPattern.March, count: 4, interval: 0.07f, spacing: 3.2f,
                bothSides: true, alternate: false);

            var second = AppendBossAttack(boss);
            SetBossAttack(second.element, BossAttackKind.GroundStrike, 2, 0,
                0f, 12f, 6.5f, 0.3f, BossAttackMovement.None, 0f, false, new[]
                {
                    Swing(24f, 40f, 8f, 0.7f, 0.14f, 0.35f, 1.8f, 1.6f, 2.4f),
                    Swing(28f, 46f, 9f, 0.6f, 0.14f, 1.1f, 1.8f, 1.6f, 2.4f)
                }, parryable: false);
            second.serialized.ApplyModifiedPropertiesWithoutUndo();
            SetBossSpawn(boss, second.index, AttackSpawnKind.Hazard, toll, new Vector2(2.4f, 0f), atTarget: false);
            SetBossPattern(boss, second.index, HazardPattern.March, count: 4, interval: 0.07f, spacing: 3.2f,
                bothSides: true, alternate: true, everySwing: true);
            EditorUtility.SetDirty(boss);
        }

        /// <summary>
        /// The collapse along the floor. The halberd goes into the stone and the floor breaks away from him
        /// in a line, one burst after another: jump the front as it passes, dash through it, or be behind
        /// him. The second phase breaks the floor both ways, and behind him is no longer an answer.
        /// </summary>
        private static void AddCommanderSignature(BossController boss)
        {
            var rubble = BuildHazardPrefab("RubbleBurst", RubbleBurstPrefabPath, new Vector2(2.0f, 2.2f),
                new Color(1f, 0.55f, 0.3f, 0.3f), new Color(1f, 0.62f, 0.34f, 0.8f),
                telegraphTime: 0.5f, activeTime: 0.35f, dormantTime: 0f, StatusType.Burn,
                buildupPerSecond: 26f, damagePerTick: 24f, DamageType.Physical);

            var first = AppendBossAttack(boss);
            SetBossAttack(first.element, BossAttackKind.GroundStrike, 1, 1,
                2.5f, 11f, 7f, 0.3f, BossAttackMovement.None, 0f, false, new[]
                {
                    Swing(26f, 44f, 8f, 0.75f, 0.14f, 1.0f, 2.0f, 1.6f, 2.4f)
                }, parryable: false);
            first.serialized.ApplyModifiedPropertiesWithoutUndo();
            SetBossSpawn(boss, first.index, AttackSpawnKind.Hazard, rubble, new Vector2(2.6f, 0f), atTarget: false);
            SetBossPattern(boss, first.index, HazardPattern.March, count: 5, interval: 0.14f, spacing: 2.2f,
                bothSides: false, alternate: false);

            var second = AppendBossAttack(boss);
            SetBossAttack(second.element, BossAttackKind.GroundStrike, 2, 0,
                0f, 12f, 6f, 0.3f, BossAttackMovement.None, 0f, false, new[]
                {
                    Swing(28f, 48f, 9f, 0.65f, 0.14f, 1.0f, 2.0f, 1.6f, 2.4f)
                }, parryable: false);
            second.serialized.ApplyModifiedPropertiesWithoutUndo();
            SetBossSpawn(boss, second.index, AttackSpawnKind.Hazard, rubble, new Vector2(2.6f, 0f), atTarget: false);
            SetBossPattern(boss, second.index, HazardPattern.March, count: 6, interval: 0.12f, spacing: 2.2f,
                bothSides: true, alternate: false);
            EditorUtility.SetDirty(boss);
        }

        /// <summary>
        /// Poisoned ground (BOSSES_7_10.md §1). Wherever the Stag lands, the bog stays poisoned for a while,
        /// so the arena fills and the fight has to be led to clean floor. In the second phase the pools last
        /// longer, the charge turns and comes back, and the bells of the dead bring three skulls down, one
        /// after another, on wherever the hero is standing.
        /// </summary>
        private static void AddStagSignature(BossController boss, int leap, int leapPhaseTwo, int charge)
        {
            var pool = BuildHazardPrefab("PlaguePool", PlaguePoolPrefabPath, new Vector2(3.6f, 1.2f),
                new Color(0.5f, 0.85f, 0.3f, 0.3f), new Color(0.44f, 0.95f, 0.3f, 0.62f),
                telegraphTime: 0.45f, activeTime: 4f, dormantTime: 0f, StatusType.Poison,
                buildupPerSecond: 40f, damagePerTick: 4f);
            var poolLong = BuildHazardPrefab("PlaguePoolLong", PlaguePoolLongPrefabPath, new Vector2(3.6f, 1.2f),
                new Color(0.5f, 0.85f, 0.3f, 0.3f), new Color(0.44f, 0.95f, 0.3f, 0.62f),
                telegraphTime: 0.45f, activeTime: 6f, dormantTime: 0f, StatusType.Poison,
                buildupPerSecond: 40f, damagePerTick: 4f);
            var skull = BuildHazardPrefab("SkullFall", SkullFallPrefabPath, new Vector2(1.5f, 2.4f),
                new Color(0.75f, 0.95f, 0.5f, 0.3f), new Color(0.85f, 1f, 0.6f, 0.8f),
                telegraphTime: 0.6f, activeTime: 0.3f, dormantTime: 0f, StatusType.Poison,
                buildupPerSecond: 30f, damagePerTick: 18f, DamageType.Physical,
                drop: new Color(0.86f, 0.84f, 0.7f));

            SetBossSpawn(boss, leap, AttackSpawnKind.Hazard, pool, new Vector2(0f, -1.2f), atTarget: false);
            SetBossSpawn(boss, leapPhaseTwo, AttackSpawnKind.Hazard, poolLong, new Vector2(0f, -1.2f), atTarget: false);

            // The single charge belongs to the first phase now; the second has the one that comes back.
            var serialized = new SerializedObject(boss);
            serialized.FindProperty("_attacks").GetArrayElementAtIndex(charge)
                .FindPropertyRelative("_toPhase").intValue = 1;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var twice = AppendBossAttack(boss);
            SetBossAttack(twice.element, BossAttackKind.Charge, 2, 0,
                3.5f, 14f, 4.2f, 0.3f, BossAttackMovement.Charge, 12f, false, new[]
                {
                    Swing(30f, 42f, 9f, 0.7f, 0.18f, 0.3f, 1.6f, 2.1f, 2.0f),
                    Swing(32f, 44f, 10f, 0.6f, 0.18f, 0.85f, 1.6f, 2.1f, 2.0f)
                }, parryable: false);
            twice.element.FindPropertyRelative("_movesEverySwing").boolValue = true;
            twice.serialized.ApplyModifiedPropertiesWithoutUndo();

            var bells = AppendBossAttack(boss);
            SetBossAttack(bells.element, BossAttackKind.GroundStrike, 2, 0,
                2.5f, 16f, 8f, 0.3f, BossAttackMovement.None, 0f, false, new[]
                {
                    Swing(0f, 0f, 0f, 0.6f, 0.1f, 0.9f, 0.2f, 0.5f, 0.2f)
                }, parryable: false);
            bells.serialized.ApplyModifiedPropertiesWithoutUndo();
            SetBossSpawn(boss, bells.index, AttackSpawnKind.Hazard, skull, Vector2.zero, atTarget: true);
            SetBossPattern(boss, bells.index, HazardPattern.Rain, count: 3, interval: 0.55f, spacing: 1f,
                bothSides: false, alternate: false);
            EditorUtility.SetDirty(boss);
        }

        private static (SerializedObject serialized, SerializedProperty element, int index) AppendBossAttack(
            BossController boss)
        {
            var serialized = new SerializedObject(boss);
            var attacks = serialized.FindProperty("_attacks");
            var index = attacks.arraySize;
            attacks.arraySize = index + 1;
            var element = attacks.GetArrayElementAtIndex(index);
            // a new element is a copy of the one before it: nothing of that one's may come along
            element.FindPropertyRelative("_movesEverySwing").boolValue = false;
            element.FindPropertyRelative("_spawnsEverySwing").boolValue = false;
            var spawn = element.FindPropertyRelative("_spawn");
            spawn.FindPropertyRelative("_kind").enumValueIndex = (int)AttackSpawnKind.None;
            spawn.FindPropertyRelative("_prefab").objectReferenceValue = null;
            spawn.FindPropertyRelative("_pattern").enumValueIndex = (int)HazardPattern.Single;
            return (serialized, element, index);
        }

        private static void SetBossPattern(BossController boss, int attackIndex, HazardPattern pattern, int count,
            float interval, float spacing, bool bothSides, bool alternate, bool everySwing = false)
        {
            var serialized = new SerializedObject(boss);
            var attack = serialized.FindProperty("_attacks").GetArrayElementAtIndex(attackIndex);
            attack.FindPropertyRelative("_spawnsEverySwing").boolValue = everySwing;
            var spawn = attack.FindPropertyRelative("_spawn");
            spawn.FindPropertyRelative("_pattern").enumValueIndex = (int)pattern;
            spawn.FindPropertyRelative("_count").intValue = count;
            spawn.FindPropertyRelative("_interval").floatValue = interval;
            spawn.FindPropertyRelative("_spacing").floatValue = spacing;
            spawn.FindPropertyRelative("_bothSides").boolValue = bothSides;
            spawn.FindPropertyRelative("_alternate").boolValue = alternate;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
