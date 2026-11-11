using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The Plague Marsh's enemies and its boss (the fifth level, MARSH.md §4–5). Decided by Claude under the
    /// user's standing approval (2026-11-05, «five levels, the game finished»): numbers a step over the
    /// capital's; the corpse-eater bites and lunges out of the water like a hound; the plague doctor keeps his
    /// distance and throws a poison flask (the forest shaman's poison field where it breaks); the leech flies
    /// and dives like the ash raven. The Plague Stag fights like the Commander — long reach, a leap that sends
    /// the bog's wave both ways — but faster and lighter on his feet; his fall gives the plague mask.
    /// </summary>
    public static partial class ContentBuilder
    {
        private const string MarshEnemyFolder = DataFolder + "/Marsh";
        private const string CorpseEaterAnimationSetPath = AnimationFolder + "/CorpseEaterAnimations.asset";
        private const string PlagueDoctorAnimationSetPath = AnimationFolder + "/PlagueDoctorAnimations.asset";
        private const string LeechAnimationSetPath = AnimationFolder + "/LeechAnimations.asset";
        private const string StagAnimationSetPath = AnimationFolder + "/PlagueStagAnimations.asset";
        private const string FlaskPrefabPath = PrefabFolder + "/PlagueFlask.prefab";
        public const string CorpseEaterPrefabName = "MarshCorpseEater";
        public const string PlagueDoctorPrefabName = "PlagueDoctor";
        public const string LeechPrefabName = "MarshLeech";
        public const string StagPrefabName = "PlagueStag";
        public const string StagBossId = "plague-stag";
        public const string StagRewardItemId = "plague_mask";

        private static IEnumerable<ItemDefinition> BuildMarshItems()
        {
            return new[]
            {
                BuildItem(StagRewardItemId, "Чумна маска",
                    "Дзьобата маска з кістки Чумного Оленя. Крізь неї болото пахне лише травами.",
                    ItemCategory.Progression, ItemRarity.Legendary, 1, true, true, 0,
                    new Color(0.62f, 0.86f, 0.36f))
            };
        }

        private static void BuildMarshEnemies()
        {
            EnsureFolder(MarshEnemyFolder);
            BuildEnemySet(CorpseEaterAnimationSetPath, "enemy_corpse_eater", false,
                PlaceholderEnemyArt.CorpseEaterStates, PlaceholderEnemyArt.GenerateCorpseEater());
            BuildEnemySet(PlagueDoctorAnimationSetPath, "enemy_plague_doctor", false,
                PlaceholderEnemyArt.PlagueDoctorStates, PlaceholderEnemyArt.GeneratePlagueDoctor());
            BuildEnemySet(LeechAnimationSetPath, "enemy_leech", false,
                PlaceholderEnemyArt.LeechStates, PlaceholderEnemyArt.GenerateLeech());

            var eater = MarshDefinition(CorpseEaterPrefabName, @"{
                ""_displayName"": ""Болотний трупоїд"", ""_experienceReward"": 95,
                ""_patrolSpeed"": 1.4, ""_chaseSpeed"": 5.4, ""_returnSpeed"": 2.6, ""_patrolRange"": 3.0,
                ""_idleTime"": 1.0, ""_idleTimeVariance"": 0.6,
                ""_detectionRange"": 8.0, ""_loseSightRange"": 12.0,
                ""_verticalDetectionRange"": 3.0, ""_alertTime"": 0.3, ""_searchTime"": 2.5,
                ""_behindDetectionRange"": 3.0, ""_facingDeadZone"": 0.3, ""_aiTickInterval"": 0.06,
                ""_attackRange"": 5.0, ""_verticalAttackRange"": 1.8, ""_attackCooldown"": 0.9,
                ""_facingLockTime"": 0.15, ""_decisionCooldown"": 0.22, ""_guardMovementScale"": 1.0,
                ""_preferredRange"": 1.2, ""_hurtTime"": 0.25, ""_hitReactionCooldown"": 0.5,
                ""_deathTime"": 1.3, ""_maxChaseTime"": 20.0,
                ""_attacks"": [
                    {""_kind"":0,""_cooldown"":0.8,""_minRange"":0,""_maxRange"":1.5,""_weight"":4,
                     ""_facingLockTime"":0.12,
                     ""_attack"":{""_displayName"":""Bite"",""_animationState"":""Attack"",
                       ""_damage"":24,""_damageType"":0,""_staggerDamage"":14,""_guardDamage"":14,
                       ""_knockback"":4,""_hitStop"":0.04,""_isUnblockable"":false,""_isParryable"":true,
                       ""_windup"":0.3,""_activeTime"":0.09,""_recovery"":0.3,
                       ""_comboLinkTime"":0.4,""_cancelableAfter"":0.4,""_movementScale"":0,
                       ""_hitboxOffset"":{""x"":0.7,""y"":0.0},""_hitboxSize"":{""x"":1.1,""y"":0.8}}},
                    {""_kind"":1,""_cooldown"":3.6,""_minRange"":2.2,""_maxRange"":5.0,""_weight"":3,
                     ""_facingLockTime"":0.4,""_lunge"":{""x"":9.5,""y"":5.0},
                     ""_attack"":{""_displayName"":""Leap"",""_animationState"":""AttackHeavy"",
                       ""_damage"":28,""_damageType"":0,""_staggerDamage"":20,""_guardDamage"":24,
                       ""_knockback"":9,""_hitStop"":0.06,""_isUnblockable"":false,""_isParryable"":true,
                       ""_windup"":0.55,""_activeTime"":0.3,""_recovery"":0.6,
                       ""_comboLinkTime"":1.2,""_cancelableAfter"":1.2,""_movementScale"":0,
                       ""_hitboxOffset"":{""x"":0.6,""y"":0.0},""_hitboxSize"":{""x"":1.4,""y"":1.1}}}
                ]}");
            var doctor = MarshDefinition(PlagueDoctorPrefabName, @"{
                ""_displayName"": ""Чумний лікар"", ""_experienceReward"": 110,
                ""_patrolSpeed"": 1.2, ""_chaseSpeed"": 2.4, ""_returnSpeed"": 2.0, ""_patrolRange"": 2.5,
                ""_idleTime"": 1.4, ""_detectionRange"": 10.0, ""_loseSightRange"": 14.0,
                ""_verticalDetectionRange"": 4.5, ""_alertTime"": 0.45, ""_searchTime"": 3.0,
                ""_attackRange"": 9.0, ""_verticalAttackRange"": 4.0, ""_attackCooldown"": 1.6,
                ""_facingLockTime"": 0.5, ""_preferredRange"": 6.0, ""_retreatRange"": 3.5,
                ""_retreatSpeed"": 2.8, ""_hurtTime"": 0.32, ""_hitReactionCooldown"": 0.5,
                ""_deathTime"": 1.5, ""_maxChaseTime"": 16.0,
                ""_attacks"": [
                    {""_kind"":0,""_cooldown"":2.8,""_minRange"":2.5,""_maxRange"":9.0,""_weight"":5,
                     ""_facingLockTime"":0.5,
                     ""_attack"":{""_displayName"":""Flask"",""_animationState"":""Attack"",
                       ""_damage"":18,""_damageType"":0,""_staggerDamage"":8,""_guardDamage"":12,
                       ""_knockback"":2,""_hitStop"":0.03,""_isUnblockable"":false,""_isParryable"":false,
                       ""_windup"":0.7,""_activeTime"":0.06,""_recovery"":0.55,
                       ""_comboLinkTime"":1.0,""_cancelableAfter"":1.0,""_movementScale"":0,
                       ""_hitboxOffset"":{""x"":0.5,""y"":0.3},""_hitboxSize"":{""x"":0.35,""y"":0.35}}}
                ]}");
            var leech = MarshDefinition(LeechPrefabName, @"{
                ""_displayName"": ""Болотна п'явка"", ""_experienceReward"": 50,
                ""_patrolSpeed"": 0, ""_chaseSpeed"": 0, ""_returnSpeed"": 0, ""_patrolRange"": 0,
                ""_idleTime"": 1.0, ""_detectionRange"": 9.0, ""_loseSightRange"": 13.0,
                ""_verticalDetectionRange"": 9.0, ""_alertTime"": 0.3, ""_hurtTime"": 0.25,
                ""_hitReactionCooldown"": 0.4, ""_deathTime"": 1.4, ""_attackRange"": 6.0,
                ""_attacks"": [
                    {""_kind"":0,""_cooldown"":1.5,""_minRange"":0,""_maxRange"":8,""_weight"":1,
                     ""_facingLockTime"":0.4,
                     ""_attack"":{""_displayName"":""Dive"",""_animationState"":""Attack"",
                       ""_damage"":18,""_damageType"":0,""_staggerDamage"":8,""_guardDamage"":10,
                       ""_knockback"":5,""_hitStop"":0.04,""_isUnblockable"":false,""_isParryable"":true,
                       ""_windup"":0.5,""_activeTime"":0.5,""_recovery"":0.35,
                       ""_comboLinkTime"":1.0,""_cancelableAfter"":1.0,""_movementScale"":0,
                       ""_hitboxOffset"":{""x"":0.2,""y"":0.35},""_hitboxSize"":{""x"":1.0,""y"":0.8}}}
                ]}");
            AssetDatabase.SaveAssets();

            var flask = BuildProjectilePrefab("PlagueFlask", FlaskPrefabPath,
                new Vector2(0.3f, 0.36f), new Color(0.6f, 0.9f, 0.3f), 0.18f);
            SetAttackSpawn(doctor, 0, AttackSpawnKind.Projectile, flask, new Vector2(0.6f, 0.5f),
                speed: 8.5f, range: 13f, color: new Color(0.6f, 0.9f, 0.3f), atTarget: false);

            BuildEnemyPrefab(CorpseEaterPrefabName, eater, false, new Color(0.25f, 0.3f, 0.22f), 140f, 60f,
                Drops("MarshCorpseEaterDrops", 18, 30, "beast_fang", 0.35f),
                // Waterlogged: fire barely catches, cold stiffens it.
                Resist(physical: 0f, fire: 0.15f, frost: -0.2f, magic: 0f),
                CorpseEaterAnimationSetPath, new Vector2(1.45f, 0.9f), staggerResistance: 0.1f,
                configure: go => SetAudioProfile(go, AudioId.HoundGrowl, AudioId.HoundHurt, AudioId.HoundDeath,
                    AudioId.HoundStep, AudioId.HoundBite, AudioId.HoundLeap, AudioId.None));
            BuildEnemyPrefab(PlagueDoctorPrefabName, doctor, false, new Color(0.15f, 0.14f, 0.15f), 110f, 45f,
                Drops("PlagueDoctorDrops", 26, 44, "healing_herb", 0.4f),
                Resist(physical: -0.05f, fire: -0.15f, frost: 0f, magic: 0.15f),
                PlagueDoctorAnimationSetPath, new Vector2(0.85f, 1.75f), staggerResistance: 0f,
                configure: go =>
                {
                    Ensure<EnemySupport>(go);
                    SetAudioProfile(go, AudioId.ShamanCast, AudioId.ShamanHurt, AudioId.ShamanDeath,
                        AudioId.ShamanStep, AudioId.ShamanOrb, AudioId.None, AudioId.None);
                });
            BuildEnemyPrefab(LeechPrefabName, leech, false, new Color(0.2f, 0.16f, 0.14f), 45f, 20f,
                Drops("MarshLeechDrops", 8, 16, "hunter_cloth", 0.15f),
                Resist(physical: -0.1f, fire: -0.25f, frost: 0f, magic: 0f),
                LeechAnimationSetPath, new Vector2(0.9f, 0.6f), staggerResistance: 0f,
                configure: go =>
                {
                    go.GetComponent<Rigidbody2D>().gravityScale = 0f;
                    Ensure<EnemyFlight>(go);
                    SetAudioProfile(go, AudioId.RavenCaw, AudioId.HoundHurt, AudioId.HoundDeath,
                        AudioId.None, AudioId.RavenCaw, AudioId.None, AudioId.None);
                });
            AssetDatabase.SaveAssets();
        }

        private static EnemyDefinition MarshDefinition(string name, string json)
        {
            var definition = CreateAsset<EnemyDefinition>($"{MarshEnemyFolder}/{name}.asset");
            JsonUtility.FromJsonOverwrite(json, definition);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        // ---------- the Plague Stag ----------

        private static GameObject BuildMarshBoss()
        {
            BuildBossSet(StagAnimationSetPath, "boss_stag", PlaceholderEnemyArt.KingStates,
                PlaceholderEnemyArt.GeneratePlagueStag());
            var root = CreateActorRoot(StagPrefabName, EnemyLayer, new Color(0.3f, 0.32f, 0.24f), new Vector2(1.4f, 2.7f));
            ApplyResistances(Ensure<Resistances>(root), Resist(physical: 0.05f, fire: -0.15f, frost: 0f, magic: 0.1f));

            var body = Ensure<Rigidbody2D>(root);
            body.gravityScale = 3.5f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.excludeLayers = (1 << PlayerLayer) | (1 << EnemyLayer);

            var capsule = Ensure<CapsuleCollider2D>(root);
            capsule.size = new Vector2(1.4f, 2.7f);
            capsule.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(FrictionlessPath);

            var motor = Ensure<EnemyMotor2D>(root);
            SetLayerMask(motor, "_groundMask", 1 << GroundLayer);
            SetReference(motor, "_visual", root.transform.Find("Visual"));
            SetVector(motor, "_groundProbeOffset", new Vector2(0f, -1.35f));
            SetVector(motor, "_ledgeProbeOffset", new Vector2(0.8f, -1.3f));
            SetVector(motor, "_wallProbeOffset", new Vector2(0f, -0.55f));

            SetFloat(Ensure<Health>(root), "_maxHealth", 1100f);
            var stagger = Ensure<StaggerMeter>(root);
            SetFloat(stagger, "_threshold", 200f);
            SetFloat(stagger, "_resistance", 0.35f);
            SetFloat(stagger, "_stunDuration", 2f);
            SetFloat(stagger, "_recoveryPerSecond", 26f);
            SetFloat(stagger, "_recoveryDelay", 2.2f);

            Ensure<StatusEffects>(root);
            var actor = Ensure<CombatActor>(root);
            SetFloat(actor, "_knockbackResistance", 0.9f);
            SetFloat(actor, "_knockbackLift", 0f);
            SetFloat(actor, "_hitInvulnerability", 0.04f);
            Ensure<Hurtbox>(root);

            var hitbox = Ensure<Hitbox>(root);
            SetLayerMask(hitbox, "_targetMask", 1 << PlayerLayer);
            var shockwave = BuildBossShockwave(root, new Color(0.55f, 0.9f, 0.3f, 0.85f));

            var boss = Ensure<BossController>(root);
            SetString(boss, "_bossId", StagBossId);
            SetString(boss, "_displayName", "Чумний Олень");
            SetString(boss, "_epithet", "Господар гнилої води");
            SetReference(boss, "_hitbox", hitbox);
            SetReference(boss, "_bodyCollider", capsule);
            SetReference(boss, "_shieldVisual", null);
            SetReference(boss, "_shockwave", shockwave);
            SetFloat(boss, "_introDuration", 1.6f);
            SetFloat(boss, "_idlePause", 0.95f);
            SetFloat(boss, "_repositionSpeed", 3.6f);
            SetFloat(boss, "_repositionTimeout", 1.6f);
            SetFloat(boss, "_preferredRange", 2.8f);
            SetFloat(boss, "_phaseTwoHealthFraction", 0.5f);
            SetFloat(boss, "_phaseTransitionTime", 2.2f);
            SetFloat(boss, "_phaseTwoAttackSpeed", 1.25f);
            SetFloat(boss, "_phaseTwoPauseScale", 0.6f);
            SetFloat(boss, "_phaseTwoMoveScale", 1.3f);
            SetFloat(boss, "_staggerImmunityAfterStun", 1.5f);
            SetFloat(boss, "_parryRecoilTime", 0.8f);
            SetFloat(boss, "_tooCloseRange", 1.0f);
            ApplyStagAttacks(boss);
            AddStagSignature(boss, leap: 1, leapPhaseTwo: 4, charge: 3);

            SetAudioProfile(root, AudioId.BossAwaken, AudioId.BossStunned, AudioId.BossDeath,
                AudioId.BossStep, AudioId.BossSlash, AudioId.BossSlam, AudioId.BossPhase);

            BuildStatusBars(root, actor, 1.95f, isBoss: true);
            BuildBossVisual(root, null, StagAnimationSetPath, 2.7f);
            BuildStatusVfx(root, 1.35f);
            BuildHitFlash(root);
            BuildStunIndicator(root, 2.0f);
            StripMissingScripts(root);
            return SavePrefab(root, $"{PrefabFolder}/{StagPrefabName}.prefab");
        }

        /// <summary>
        /// The antlers' sweep (parry it); the leap into the bog, its wave both ways (jump it); three
        /// sweeps; the antler charge (jump or dash through). Phase two: the leap twice, the sweep doubled.
        /// </summary>
        private static void ApplyStagAttacks(BossController boss)
        {
            var serialized = new SerializedObject(boss);
            var attacks = serialized.FindProperty("_attacks");
            attacks.arraySize = 6;

            SetBossAttack(attacks.GetArrayElementAtIndex(0), BossAttackKind.SwordSlash, 1, 1,
                0f, 3.6f, 2.4f, 0.26f, BossAttackMovement.None, 0f, false, new[]
                {
                    Swing(30f, 36f, 7f, 0.55f, 0.13f, 0.55f, 2.3f, 1.8f, 2.6f)
                }, parryable: true);
            SetBossAttack(attacks.GetArrayElementAtIndex(1), BossAttackKind.JumpSlam, 1, 1,
                2.5f, 14f, 4.0f, 0.55f, BossAttackMovement.Leap, 11f, true, new[]
                {
                    Swing(32f, 50f, 8f, 0.68f, 0.14f, 0.9f, 1.8f, 2.2f, 2.4f)
                }, parryable: false);
            SetBossAttack(attacks.GetArrayElementAtIndex(2), BossAttackKind.TripleCombo, 1, 0,
                0f, 3.6f, 3.2f, 0.24f, BossAttackMovement.None, 0f, false, new[]
                {
                    Swing(20f, 24f, 4f, 0.4f, 0.11f, 0.28f, 2.0f, 1.7f, 2.4f),
                    Swing(22f, 26f, 4f, 0.3f, 0.11f, 0.28f, 2.0f, 1.7f, 2.4f),
                    Swing(30f, 46f, 9f, 0.45f, 0.13f, 0.8f, 2.2f, 2.0f, 2.6f)
                }, parryable: true);
            SetBossAttack(attacks.GetArrayElementAtIndex(3), BossAttackKind.Charge, 1, 0,
                3.5f, 12f, 3.6f, 0.34f, BossAttackMovement.Charge, 11.5f, false, new[]
                {
                    Swing(30f, 42f, 9f, 0.7f, 0.18f, 0.7f, 1.6f, 2.1f, 2.0f)
                }, parryable: false);
            SetBossAttack(attacks.GetArrayElementAtIndex(4), BossAttackKind.JumpSlam, 2, 1,
                2.5f, 14f, 3.2f, 0.5f, BossAttackMovement.Leap, 12.5f, true, new[]
                {
                    Swing(34f, 54f, 9f, 0.5f, 0.14f, 0.8f, 1.8f, 2.2f, 2.4f)
                }, parryable: false);
            SetBossAttack(attacks.GetArrayElementAtIndex(5), BossAttackKind.SwordSlash, 2, 0,
                0f, 3.6f, 2.6f, 0.22f, BossAttackMovement.None, 0f, false, new[]
                {
                    Swing(28f, 34f, 6f, 0.45f, 0.12f, 0.22f, 2.2f, 1.8f, 2.6f),
                    Swing(32f, 46f, 8f, 0.38f, 0.13f, 0.7f, 2.2f, 2.0f, 2.8f)
                }, parryable: true);

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(boss);
        }
    }
}
