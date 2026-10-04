using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The Bellringer, the Fallen Village's boss (item 5 of the 15-zone list, WORLD_DESIGN.md:
    /// «бой на площадке у колокола; удары колокола — волны по полу», reward the grapple).
    ///
    /// The same kind of fight as the Guardian and the King — a big body and one heavy weapon — at
    /// the village's threat level: lighter and slower than the King, with the bell's waves from the
    /// first phase: every hammer blow on the floor sends a shockwave both ways, and the second phase
    /// rings twice. The waves are the lesson; the grapple is the reward.
    /// </summary>
    public static partial class ContentBuilder
    {
        public const string BellringerBossId = "bellringer";
        public const string BellringerPrefabName = "Bellringer";
        private const string BellringerAnimationSetPath = AnimationFolder + "/BellringerAnimations.asset";
        private const float BellringerStunHeight = 1.8f;

        /// <summary>Frames, then the prefab.</summary>
        private static GameObject BuildVillageBoss()
        {
            BuildBossSet(BellringerAnimationSetPath, "boss_bellringer", PlaceholderEnemyArt.KingStates,
                PlaceholderEnemyArt.GenerateBellringer());
            return BuildBellringerPrefab();
        }

        private static GameObject BuildBellringerPrefab()
        {
            var root = CreateActorRoot(BellringerPrefabName, EnemyLayer,
                new Color(0.38f, 0.3f, 0.22f), new Vector2(1.4f, 2.5f));

            // A smock and a bell's bronze: cuts bite, fire catches the cloth, the hammer shrugs frost.
            ApplyResistances(Ensure<Resistances>(root),
                Resist(physical: 0.05f, fire: -0.15f, frost: 0.1f, magic: 0f));

            var body = Ensure<Rigidbody2D>(root);
            body.gravityScale = 3.5f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.excludeLayers = (1 << PlayerLayer) | (1 << EnemyLayer);

            var capsule = Ensure<CapsuleCollider2D>(root);
            capsule.size = new Vector2(1.4f, 2.5f);
            capsule.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(FrictionlessPath);

            var motor = Ensure<EnemyMotor2D>(root);
            SetLayerMask(motor, "_groundMask", 1 << GroundLayer);
            SetReference(motor, "_visual", root.transform.Find("Visual"));
            SetVector(motor, "_groundProbeOffset", new Vector2(0f, -1.3f));
            SetVector(motor, "_ledgeProbeOffset", new Vector2(0.8f, -1.25f));
            SetVector(motor, "_wallProbeOffset", new Vector2(0f, -0.55f));

            SetFloat(Ensure<Health>(root), "_maxHealth", 820f);
            var stagger = Ensure<StaggerMeter>(root);
            SetFloat(stagger, "_threshold", 170f);
            SetFloat(stagger, "_resistance", 0.35f);
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
            var shockwave = BuildBossShockwave(root, new Color(0.95f, 0.72f, 0.38f, 0.85f));

            var boss = Ensure<BossController>(root);
            SetString(boss, "_bossId", BellringerBossId);
            SetString(boss, "_displayName", "Дзвонар");
            SetString(boss, "_epithet", "Той, хто б'є на сполох");
            SetReference(boss, "_hitbox", hitbox);
            SetReference(boss, "_bodyCollider", capsule);
            SetReference(boss, "_shieldVisual", null);
            SetReference(boss, "_shockwave", shockwave);
            SetFloat(boss, "_introDuration", 1.6f);
            SetFloat(boss, "_idlePause", 1.1f);
            SetFloat(boss, "_repositionSpeed", 3.0f);
            SetFloat(boss, "_repositionTimeout", 1.6f);
            SetFloat(boss, "_preferredRange", 2.6f);
            // The second phase: half his health gone, the bell rings faster and twice.
            SetFloat(boss, "_phaseTwoHealthFraction", 0.5f);
            SetFloat(boss, "_phaseTransitionTime", 2.2f);
            SetFloat(boss, "_phaseTwoAttackSpeed", 1.22f);
            SetFloat(boss, "_phaseTwoPauseScale", 0.65f);
            SetFloat(boss, "_phaseTwoMoveScale", 1.25f);
            SetFloat(boss, "_staggerImmunityAfterStun", 1.5f);
            SetFloat(boss, "_parryRecoilTime", 0.8f);
            SetFloat(boss, "_tooCloseRange", 1.0f);
            ApplyBellringerAttacks(boss);

            SetAudioProfile(root, AudioId.BossAwaken, AudioId.BossStunned, AudioId.BossDeath,
                AudioId.BossStep, AudioId.BossSlash, AudioId.BossSlam, AudioId.BossPhase);

            BuildStatusBars(root, actor, 1.8f, isBoss: true);
            BuildBossVisual(root, null, BellringerAnimationSetPath, 2.5f);
            BuildStatusVfx(root, 1.3f);
            BuildHitFlash(root);
            BuildStunIndicator(root, BellringerStunHeight);
            StripMissingScripts(root);
            return SavePrefab(root, $"{PrefabFolder}/{BellringerPrefabName}.prefab");
        }

        /// <summary>
        /// The hammer from high (parry it), the hammer brought down on the floor with a wave both
        /// ways (jump it), and three blows; the second phase strikes the floor twice in a row.
        /// </summary>
        private static void ApplyBellringerAttacks(BossController boss)
        {
            var serialized = new SerializedObject(boss);
            var attacks = serialized.FindProperty("_attacks");
            attacks.arraySize = 6;

            // Close: the hammer from high. Slow, readable, the one to learn to parry.
            SetBossAttack(attacks.GetArrayElementAtIndex(0), BossAttackKind.SwordSlash, 1, 1,
                0f, 3.0f, 2.6f, 0.26f, BossAttackMovement.None, 0f, false, new[]
                {
                    Swing(26f, 34f, 7f, 0.62f, 0.13f, 0.65f, 1.9f, 1.8f, 2.4f)
                }, parryable: true);

            // Any range: he leaps in and strikes the floor; the bell's wave runs out both ways.
            SetBossAttack(attacks.GetArrayElementAtIndex(1), BossAttackKind.JumpSlam, 1, 1,
                2f, 14f, 4.6f, 0.55f, BossAttackMovement.Leap, 10f, true, new[]
                {
                    Swing(28f, 48f, 8f, 0.7f, 0.14f, 0.95f, 1.6f, 2.2f, 2.4f)
                }, parryable: false);

            // Close: three blows, each its own window, the last the heaviest.
            SetBossAttack(attacks.GetArrayElementAtIndex(2), BossAttackKind.TripleCombo, 1, 0,
                0f, 3.2f, 3.4f, 0.24f, BossAttackMovement.None, 0f, false, new[]
                {
                    Swing(16f, 22f, 4f, 0.46f, 0.11f, 0.3f, 1.7f, 1.7f, 2.2f),
                    Swing(18f, 24f, 4f, 0.34f, 0.11f, 0.3f, 1.7f, 1.7f, 2.2f),
                    Swing(26f, 44f, 9f, 0.5f, 0.13f, 0.85f, 1.9f, 2.0f, 2.4f)
                }, parryable: true);

            // Middle to far: he charges the hero down, the hammer dragging. Jump it or dash through.
            SetBossAttack(attacks.GetArrayElementAtIndex(3), BossAttackKind.Charge, 1, 0,
                3.5f, 12f, 4.2f, 0.34f, BossAttackMovement.Charge, 9.5f, false, new[]
                {
                    Swing(26f, 40f, 9f, 0.8f, 0.18f, 0.75f, 1.5f, 2.0f, 2.0f)
                }, parryable: false);

            // Phase two: the bell twice — a leap with its wave, sooner and harder.
            SetBossAttack(attacks.GetArrayElementAtIndex(4), BossAttackKind.JumpSlam, 2, 1,
                2f, 14f, 3.6f, 0.5f, BossAttackMovement.Leap, 11.5f, true, new[]
                {
                    Swing(30f, 52f, 9f, 0.55f, 0.14f, 0.85f, 1.6f, 2.2f, 2.4f)
                }, parryable: false);

            // Phase two: the hammer from high twice, a beat apart.
            SetBossAttack(attacks.GetArrayElementAtIndex(5), BossAttackKind.SwordSlash, 2, 0,
                0f, 3.2f, 2.8f, 0.22f, BossAttackMovement.None, 0f, false, new[]
                {
                    Swing(24f, 32f, 6f, 0.5f, 0.12f, 0.24f, 1.8f, 1.8f, 2.4f),
                    Swing(28f, 44f, 8f, 0.42f, 0.13f, 0.75f, 1.8f, 2.0f, 2.6f)
                }, parryable: true);

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(boss);
        }
    }
}
