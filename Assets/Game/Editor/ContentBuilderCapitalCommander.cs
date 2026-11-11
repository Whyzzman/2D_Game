using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The Ash Commander, the capital's own boss (item 6b of the 15-zone list, WORLD_DESIGN.md:
    /// «рыцарь с алебардой и щитом-хоругвью», «обвальный удар»), in the hall before the King's throne.
    /// Decided by Claude under the user's standing approval: the King's kind of fight — a big armoured
    /// body and one long weapon — a step under the King, with the collapse from the first phase: he
    /// leaps and drives the halberd into the floor, and the stone's wave runs out both ways; the second
    /// phase brings it down twice. His fall opens the palace's seal to the King.
    /// </summary>
    public static partial class ContentBuilder
    {
        public const string CommanderBossId = "ash-commander";
        public const string CommanderPrefabName = "AshCommander";
        private const string CommanderAnimationSetPath = AnimationFolder + "/CommanderAnimations.asset";
        private const float CommanderStunHeight = 1.95f;

        /// <summary>Frames, then the prefab.</summary>
        private static GameObject BuildCapitalCommander()
        {
            BuildBossSet(CommanderAnimationSetPath, "boss_commander", PlaceholderEnemyArt.KingStates,
                PlaceholderEnemyArt.GenerateCommander());
            return BuildCommanderPrefab();
        }

        private static GameObject BuildCommanderPrefab()
        {
            var root = CreateActorRoot(CommanderPrefabName, EnemyLayer,
                new Color(0.32f, 0.3f, 0.3f), new Vector2(1.4f, 2.6f));

            // Plate burnt black: a blade turns on it, fire is home to him, frost finds the joints.
            ApplyResistances(Ensure<Resistances>(root),
                Resist(physical: 0.15f, fire: 0.2f, frost: -0.1f, magic: 0f));

            var body = Ensure<Rigidbody2D>(root);
            body.gravityScale = 3.5f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.excludeLayers = (1 << PlayerLayer) | (1 << EnemyLayer);

            var capsule = Ensure<CapsuleCollider2D>(root);
            capsule.size = new Vector2(1.4f, 2.6f);
            capsule.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(FrictionlessPath);

            var motor = Ensure<EnemyMotor2D>(root);
            SetLayerMask(motor, "_groundMask", 1 << GroundLayer);
            SetReference(motor, "_visual", root.transform.Find("Visual"));
            SetVector(motor, "_groundProbeOffset", new Vector2(0f, -1.3f));
            SetVector(motor, "_ledgeProbeOffset", new Vector2(0.8f, -1.25f));
            SetVector(motor, "_wallProbeOffset", new Vector2(0f, -0.55f));

            SetFloat(Ensure<Health>(root), "_maxHealth", 960f);
            var stagger = Ensure<StaggerMeter>(root);
            SetFloat(stagger, "_threshold", 190f);
            SetFloat(stagger, "_resistance", 0.4f);
            SetFloat(stagger, "_stunDuration", 2f);
            SetFloat(stagger, "_recoveryPerSecond", 24f);
            SetFloat(stagger, "_recoveryDelay", 2.2f);

            Ensure<StatusEffects>(root);
            var actor = Ensure<CombatActor>(root);
            SetFloat(actor, "_knockbackResistance", 0.9f);
            SetFloat(actor, "_knockbackLift", 0f);
            SetFloat(actor, "_hitInvulnerability", 0.04f);
            Ensure<Hurtbox>(root);

            var hitbox = Ensure<Hitbox>(root);
            SetLayerMask(hitbox, "_targetMask", 1 << PlayerLayer);
            var shockwave = BuildBossShockwave(root, new Color(0.9f, 0.5f, 0.3f, 0.85f));

            var boss = Ensure<BossController>(root);
            SetString(boss, "_bossId", CommanderBossId);
            SetString(boss, "_displayName", "Командор Попелу");
            SetString(boss, "_epithet", "Останній, хто тримає браму");
            SetReference(boss, "_hitbox", hitbox);
            SetReference(boss, "_bodyCollider", capsule);
            SetReference(boss, "_shieldVisual", null);
            SetReference(boss, "_shockwave", shockwave);
            SetFloat(boss, "_introDuration", 1.6f);
            SetFloat(boss, "_idlePause", 1.1f);
            SetFloat(boss, "_repositionSpeed", 3.0f);
            SetFloat(boss, "_repositionTimeout", 1.6f);
            SetFloat(boss, "_preferredRange", 2.6f);
            // The second phase: half his health gone, the collapse comes faster and twice.
            SetFloat(boss, "_phaseTwoHealthFraction", 0.5f);
            SetFloat(boss, "_phaseTransitionTime", 2.2f);
            SetFloat(boss, "_phaseTwoAttackSpeed", 1.22f);
            SetFloat(boss, "_phaseTwoPauseScale", 0.65f);
            SetFloat(boss, "_phaseTwoMoveScale", 1.25f);
            SetFloat(boss, "_staggerImmunityAfterStun", 1.5f);
            SetFloat(boss, "_parryRecoilTime", 0.8f);
            SetFloat(boss, "_tooCloseRange", 1.0f);
            ApplyCommanderAttacks(boss);
            AddCommanderSignature(boss);
            ScaleBossBlows(boss, 1.2f);

            SetAudioProfile(root, AudioId.BossAwaken, AudioId.BossStunned, AudioId.BossDeath,
                AudioId.BossStep, AudioId.BossSlash, AudioId.BossSlam, AudioId.BossPhase);

            BuildStatusBars(root, actor, 1.9f, isBoss: true);
            BuildBossVisual(root, null, CommanderAnimationSetPath, 2.6f);
            BuildStatusVfx(root, 1.3f);
            BuildHitFlash(root);
            BuildStunIndicator(root, CommanderStunHeight);
            StripMissingScripts(root);
            return SavePrefab(root, $"{PrefabFolder}/{CommanderPrefabName}.prefab");
        }

        /// <summary>
        /// The halberd's cut (parry it), the collapse — a leap and the halberd driven into the floor, a
        /// wave both ways (jump it) — three blows, and a charge behind the banner-shield; the second
        /// phase brings the collapse down twice and cuts twice.
        /// </summary>
        private static void ApplyCommanderAttacks(BossController boss)
        {
            var serialized = new SerializedObject(boss);
            var attacks = serialized.FindProperty("_attacks");
            attacks.arraySize = 6;

            // Close: the halberd's cut, long and readable — the one to parry.
            SetBossAttack(attacks.GetArrayElementAtIndex(0), BossAttackKind.SwordSlash, 1, 1,
                0f, 3.6f, 2.6f, 0.26f, BossAttackMovement.None, 0f, false, new[]
                {
                    Swing(28f, 36f, 7f, 0.6f, 0.13f, 0.6f, 2.3f, 1.8f, 2.6f)
                }, parryable: true);

            // Any range: the collapse. He leaps, the halberd goes into the floor, the stone runs both ways.
            SetBossAttack(attacks.GetArrayElementAtIndex(1), BossAttackKind.JumpSlam, 1, 1,
                2.5f, 14f, 4.4f, 0.55f, BossAttackMovement.Leap, 10.5f, true, new[]
                {
                    Swing(30f, 50f, 8f, 0.72f, 0.14f, 0.95f, 1.8f, 2.2f, 2.4f)
                }, parryable: false);

            // Close: three cuts, the last the heaviest.
            SetBossAttack(attacks.GetArrayElementAtIndex(2), BossAttackKind.TripleCombo, 1, 0,
                0f, 3.6f, 3.4f, 0.24f, BossAttackMovement.None, 0f, false, new[]
                {
                    Swing(18f, 24f, 4f, 0.44f, 0.11f, 0.3f, 2.0f, 1.7f, 2.4f),
                    Swing(20f, 26f, 4f, 0.32f, 0.11f, 0.3f, 2.0f, 1.7f, 2.4f),
                    Swing(28f, 46f, 9f, 0.48f, 0.13f, 0.85f, 2.2f, 2.0f, 2.6f)
                }, parryable: true);

            // Middle to far: behind the banner-shield he runs the hero down. Jump it or dash through.
            SetBossAttack(attacks.GetArrayElementAtIndex(3), BossAttackKind.Charge, 1, 0,
                3.5f, 12f, 4.0f, 0.34f, BossAttackMovement.Charge, 10f, false, new[]
                {
                    Swing(28f, 42f, 9f, 0.75f, 0.18f, 0.75f, 1.6f, 2.1f, 2.0f)
                }, parryable: false);

            // Phase two: the collapse twice — sooner and harder.
            SetBossAttack(attacks.GetArrayElementAtIndex(4), BossAttackKind.JumpSlam, 2, 1,
                2.5f, 14f, 3.4f, 0.5f, BossAttackMovement.Leap, 12f, true, new[]
                {
                    Swing(32f, 54f, 9f, 0.52f, 0.14f, 0.85f, 1.8f, 2.2f, 2.4f)
                }, parryable: false);

            // Phase two: the cut twice, a beat apart.
            SetBossAttack(attacks.GetArrayElementAtIndex(5), BossAttackKind.SwordSlash, 2, 0,
                0f, 3.6f, 2.8f, 0.22f, BossAttackMovement.None, 0f, false, new[]
                {
                    Swing(26f, 34f, 6f, 0.48f, 0.12f, 0.24f, 2.2f, 1.8f, 2.6f),
                    Swing(30f, 46f, 8f, 0.4f, 0.13f, 0.75f, 2.2f, 2.0f, 2.8f)
                }, parryable: true);

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(boss);
        }
    }
}
