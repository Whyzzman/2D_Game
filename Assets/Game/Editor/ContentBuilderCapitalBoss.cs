using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The Ashen Capital's lord, «Король-Заклятий» (item 2 of the big list, stage D; the user's
    /// choice of 2026-09-28: a heavy knight-ruler with a rage phase), his ember scars, his reward
    /// and the two finds elsewhere in the world that only his gift — the grapple — reaches.
    ///
    /// The third boss answers the first two: the Guardian taught the guard and the parry, the
    /// Warden taught spacing; the King is the test of both at once, heavier than either, and in
    /// his rage he takes the floor away where he lands, so the player has to keep moving.
    /// </summary>
    public static partial class ContentBuilder
    {
        private const string KingAnimationSetPath = AnimationFolder + "/KingAnimations.asset";
        private const string EmberScarPrefabPath = PrefabFolder + "/EmberScar.prefab";
        public const string KingPrefabName = "OathBoundKing";
        public const string KingRewardItemId = "king_seal";
        public const string CommanderRewardItemId = "commander_banner";
        public const string HunterRelicItemId = "hunter_talon";
        private const float KingStunHeight = 1.95f;

        // ---------- items ----------

        /// <summary>The King's seal and the relic in the forest's hollow.</summary>
        private static IEnumerable<ItemDefinition> BuildCapitalItems()
        {
            var talon = BuildAccessory(HunterRelicItemId, "Удар сильніший, рука спритніша.",
                new[]
                {
                    new StatModifier(StatType.AttackPower, ModifierOperation.Add, 4f),
                    new StatModifier(StatType.Dexterity, ModifierOperation.Add, 2f)
                });
            return new[]
            {
                BuildItem(KingRewardItemId, "Печать Короля",
                    "Королівська печать, досі тепла. Попіл на ній не холоне.",
                    ItemCategory.Progression, ItemRarity.Legendary, 1, true, true, 0,
                    new Color(0.95f, 0.55f, 0.28f)),
                BuildItem(CommanderRewardItemId, "Хоругва Командора",
                    "Обгоріле полотнище зі щита Командора. Під ним палацова печать відступила.",
                    ItemCategory.Progression, ItemRarity.Epic, 1, true, true, 0,
                    new Color(0.62f, 0.2f, 0.22f)),
                BuildItem(HunterRelicItemId, "Кіготь мисливця",
                    "Талісман із кігтя звіра, якого в Лісі вже ніхто не бачив живим.",
                    ItemCategory.Accessory, ItemRarity.Rare, 1, true, true, 450,
                    new Color(0.72f, 0.6f, 0.42f), talon)
            };
        }

        // ---------- the King ----------

        /// <summary>Frames, the ember scar and the prefab, in that order.</summary>
        private static GameObject BuildCapitalBoss()
        {
            BuildBossSet(KingAnimationSetPath, "boss_king", PlaceholderEnemyArt.KingStates,
                PlaceholderEnemyArt.GenerateKing());

            // Where an enraged slam lands the stone burns for a moment: a floor to leave, not a
            // floor to stand on. Fire, so it burns rather than poisons.
            var scar = BuildHazardPrefab("EmberScar", EmberScarPrefabPath, new Vector2(4.6f, 1.2f),
                new Color(1f, 0.5f, 0.22f, 0.32f), new Color(1f, 0.42f, 0.16f, 0.66f),
                telegraphTime: 0.45f, activeTime: 2.4f, dormantTime: 0f, StatusType.Burn,
                buildupPerSecond: 38f, damagePerTick: 8f, DamageType.Fire);
            return BuildKingPrefab(scar);
        }

        /// <summary>
        /// Bigger, slower and harder than the Warden, with more health than the Guardian: the last
        /// fight so far, met with the second jump and the dash already in hand.
        /// </summary>
        private static GameObject BuildKingPrefab(GameObject emberScar)
        {
            var root = CreateActorRoot(KingPrefabName, EnemyLayer,
                new Color(0.42f, 0.26f, 0.24f), new Vector2(1.5f, 2.7f));

            // Plate and a curse of fire: cuts glance off, fire is his own, frost bites.
            ApplyResistances(Ensure<Resistances>(root),
                Resist(physical: 0.15f, fire: 0.4f, frost: -0.2f, magic: 0.05f));

            var body = Ensure<Rigidbody2D>(root);
            body.gravityScale = 3.5f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.excludeLayers = (1 << PlayerLayer) | (1 << EnemyLayer);

            var capsule = Ensure<CapsuleCollider2D>(root);
            capsule.size = new Vector2(1.5f, 2.7f);
            capsule.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(FrictionlessPath);

            var motor = Ensure<EnemyMotor2D>(root);
            SetLayerMask(motor, "_groundMask", 1 << GroundLayer);
            SetReference(motor, "_visual", root.transform.Find("Visual"));
            SetVector(motor, "_groundProbeOffset", new Vector2(0f, -1.4f));
            SetVector(motor, "_ledgeProbeOffset", new Vector2(0.85f, -1.35f));
            SetVector(motor, "_wallProbeOffset", new Vector2(0f, -0.6f));

            SetFloat(Ensure<Health>(root), "_maxHealth", 1150f);
            var stagger = Ensure<StaggerMeter>(root);
            SetFloat(stagger, "_threshold", 210f);
            SetFloat(stagger, "_resistance", 0.45f);
            SetFloat(stagger, "_stunDuration", 1.8f);
            SetFloat(stagger, "_recoveryPerSecond", 26f);
            SetFloat(stagger, "_recoveryDelay", 2.2f);

            var statuses = Ensure<StatusEffects>(root);
            SetKingStatusRules(statuses);

            var actor = Ensure<CombatActor>(root);
            SetFloat(actor, "_knockbackResistance", 0.94f);
            SetFloat(actor, "_knockbackLift", 0f);
            SetFloat(actor, "_hitInvulnerability", 0.04f);
            Ensure<Hurtbox>(root);

            var hitbox = Ensure<Hitbox>(root);
            SetLayerMask(hitbox, "_targetMask", 1 << PlayerLayer);
            var shockwave = BuildBossShockwave(root, new Color(1f, 0.5f, 0.22f, 0.85f));

            var boss = Ensure<BossController>(root);
            SetString(boss, "_bossId", CapitalKingBossId);
            SetString(boss, "_displayName", "Король-Заклятий");
            SetString(boss, "_epithet", "Попіл, що присягнув");
            SetReference(boss, "_hitbox", hitbox);
            SetReference(boss, "_bodyCollider", capsule);
            SetReference(boss, "_shieldVisual", null);
            SetReference(boss, "_shockwave", shockwave);
            SetFloat(boss, "_introDuration", 1.8f);
            SetFloat(boss, "_idlePause", 1.0f);
            SetFloat(boss, "_repositionSpeed", 3.4f);
            SetFloat(boss, "_repositionTimeout", 1.6f);
            SetFloat(boss, "_preferredRange", 2.4f);
            // The rage: half his health gone, he burns, and everything comes sooner and faster.
            SetFloat(boss, "_phaseTwoHealthFraction", 0.5f);
            SetFloat(boss, "_phaseTransitionTime", 2.4f);
            SetFloat(boss, "_phaseTwoAttackSpeed", 1.3f);
            SetFloat(boss, "_phaseTwoPauseScale", 0.55f);
            SetFloat(boss, "_phaseTwoMoveScale", 1.35f);
            SetFloat(boss, "_staggerImmunityAfterStun", 1.5f);
            SetFloat(boss, "_parryRecoilTime", 0.8f);
            SetFloat(boss, "_tooCloseRange", 1.0f);
            ApplyKingAttacks(boss, emberScar);

            SetAudioProfile(root, AudioId.KingAwaken, AudioId.KingStunned, AudioId.KingDeath,
                AudioId.KingStep, AudioId.BossSlash, AudioId.BossJump, AudioId.KingRage);

            BuildStatusBars(root, actor, 1.9f, isBoss: true);
            BuildBossVisual(root, null, KingAnimationSetPath, 2.7f);
            BuildStatusVfx(root, 1.4f);
            BuildHitFlash(root);
            BuildStunIndicator(root, KingStunHeight);
            StripMissingScripts(root);
            return SavePrefab(root, $"{PrefabFolder}/{KingPrefabName}.prefab");
        }

        /// <summary>
        /// Burn does nothing to a body already burning; bleed finds the joints in the plate, and
        /// frost is what a curse of fire fears.
        /// </summary>
        private static void SetKingStatusRules(StatusEffects statuses)
        {
            var serialized = new SerializedObject(statuses);
            var rules = serialized.FindProperty("_rules");
            for (var i = 0; i < rules.arraySize; i++)
            {
                var rule = rules.GetArrayElementAtIndex(i);
                switch ((StatusType)rule.FindPropertyRelative("type").enumValueIndex)
                {
                    case StatusType.Burn:
                        rule.FindPropertyRelative("immune").boolValue = true;
                        break;
                    case StatusType.Bleed:
                        rule.FindPropertyRelative("resistance").floatValue = 0.2f;
                        break;
                    case StatusType.Frost:
                        rule.FindPropertyRelative("resistance").floatValue = 0f;
                        break;
                    case StatusType.Poison:
                        rule.FindPropertyRelative("resistance").floatValue = 0.5f;
                        break;
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(statuses);
        }

        /// <summary>
        /// Four moves at first, the Guardian's grammar in a heavier hand: a parryable greatsword
        /// blow, a shoulder charge, a three-count combo and a leap. In the rage the leap lands
        /// with a shockwave and a burning scar, a second, faster charge comes out of nowhere, and
        /// the greatsword turns into a two-blow overhead. Nothing homes; every blow shows itself.
        /// </summary>
        private static void ApplyKingAttacks(BossController boss, GameObject emberScar)
        {
            var serialized = new SerializedObject(boss);
            var attacks = serialized.FindProperty("_attacks");
            attacks.arraySize = 7;

            // Close: the greatsword from high. Slow and heavy, the rhythm to learn and to parry.
            SetBossAttack(attacks.GetArrayElementAtIndex(0), BossAttackKind.SwordSlash, 1, 1,
                0f, 3.0f, 2.4f, 0.24f, BossAttackMovement.None, 0f, false, new[]
                {
                    Swing(30f, 38f, 7f, 0.55f, 0.13f, 0.6f, 1.8f, 1.8f, 2.4f)
                }, parryable: true);

            // Middle to far: the shoulder charge. Unparryable; jump it or dash through.
            SetBossAttack(attacks.GetArrayElementAtIndex(1), BossAttackKind.Charge, 1, 0,
                3.5f, 13f, 4.0f, 0.32f, BossAttackMovement.Charge, 10.5f, false, new[]
                {
                    Swing(30f, 44f, 10f, 0.8f, 0.18f, 0.75f, 1.5f, 2.0f, 2.0f)
                }, parryable: false);

            // Close: three separate blows, each its own window, the last the heaviest.
            SetBossAttack(attacks.GetArrayElementAtIndex(2), BossAttackKind.TripleCombo, 1, 0,
                0f, 3.2f, 3.2f, 0.22f, BossAttackMovement.None, 0f, false, new[]
                {
                    Swing(18f, 24f, 4f, 0.42f, 0.11f, 0.28f, 1.7f, 1.7f, 2.2f),
                    Swing(20f, 26f, 4f, 0.30f, 0.11f, 0.28f, 1.7f, 1.7f, 2.2f),
                    Swing(30f, 50f, 10f, 0.46f, 0.13f, 0.85f, 1.9f, 2.0f, 2.4f)
                }, parryable: true);

            // Far: he closes the distance himself. No shockwave yet — the rage adds it.
            SetBossAttack(attacks.GetArrayElementAtIndex(3), BossAttackKind.JumpSlam, 1, 1,
                4f, 14f, 6.0f, 0.55f, BossAttackMovement.Leap, 11f, false, new[]
                {
                    Swing(32f, 56f, 8f, 0.65f, 0.14f, 0.95f, 1.6f, 2.2f, 2.4f)
                }, parryable: false);

            // Rage: the greatsword twice, the second a beat after the first — parry both or leave.
            SetBossAttack(attacks.GetArrayElementAtIndex(4), BossAttackKind.SwordSlash, 2, 0,
                0f, 3.2f, 2.6f, 0.2f, BossAttackMovement.None, 0f, false, new[]
                {
                    Swing(28f, 36f, 6f, 0.46f, 0.12f, 0.22f, 1.8f, 1.8f, 2.4f),
                    Swing(34f, 52f, 9f, 0.38f, 0.13f, 0.75f, 1.8f, 2.0f, 2.6f)
                }, parryable: true);

            // Rage signature: the leap lands with a shockwave both ways and leaves the floor on fire.
            SetBossAttack(attacks.GetArrayElementAtIndex(5), BossAttackKind.JumpSlam, 2, 0,
                3f, 14f, 5.5f, 0.6f, BossAttackMovement.Leap, 12.5f, true, new[]
                {
                    Swing(36f, 60f, 9f, 0.55f, 0.14f, 0.9f, 1.6f, 2.2f, 2.4f)
                }, parryable: false);

            // Rage: the charge again, sooner and faster, so distance alone is never safe.
            SetBossAttack(attacks.GetArrayElementAtIndex(6), BossAttackKind.Charge, 2, 0,
                5f, 16f, 5.0f, 0.3f, BossAttackMovement.Charge, 13f, false, new[]
                {
                    Swing(32f, 46f, 11f, 0.6f, 0.2f, 0.7f, 1.5f, 2.0f, 2.0f)
                }, parryable: false);

            serialized.ApplyModifiedPropertiesWithoutUndo();
            SetBossSpawn(boss, 5, AttackSpawnKind.Hazard, emberScar, new Vector2(0f, -1.1f),
                atTarget: false);
            EditorUtility.SetDirty(boss);
        }

        // ---------- the throne hall's fight ----------

        /// <summary>
        /// The King waits before his throne; stepping past the hall's threshold seals it behind the
        /// hero. His death opens it again and hands over the seal and the coin.
        /// </summary>
        /// <summary>
        /// A boss of the capital in its arena: the prefab at <paramref name="bossAt"/>, the trigger past
        /// the door at <paramref name="triggerAt"/> that seals <paramref name="barrier"/> behind the hero,
        /// and what the fall hands over.
        /// </summary>
        private static void BuildCapitalEncounter(Transform section, string prefabName, string name, GameObject barrier,
            GameProgress progress, PlayerRespawner respawner, Vector2 bossAt, Vector2 triggerAt, string rewardId,
            string rewardName, int currency, int experience)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/{prefabName}.prefab");
            if (prefab == null)
            {
                Debug.LogError($"{prefabName} is not built; run ContentBuilder.Build first.");
                return;
            }

            var boss = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            boss.transform.SetParent(section);
            boss.transform.position = bossAt;

            var encounterObject = new GameObject(name);
            encounterObject.transform.SetParent(section);
            encounterObject.transform.position = triggerAt;
            var trigger = encounterObject.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(3f, 6f);
            var encounter = encounterObject.AddComponent<BossEncounter>();
            SetReference(encounter, "_boss", boss.GetComponent<BossController>());
            SetReference(encounter, "_barrier", barrier);
            SetReference(encounter, "_progress", progress);
            SetReference(encounter, "_respawner", respawner);
            SetReference(encounter, "_progression", Object.FindAnyObjectByType<PlayerProgressionService>());
            SetString(encounter, "_rewardId", rewardId);
            SetString(encounter, "_rewardName", rewardName);
            SetInt(encounter, "_rewardCurrency", currency);
            SetInt(encounter, "_rewardExperience", experience);
        }

        // ---------- rings and what they reach ----------

        /// <summary>An iron ring the grapple catches, with the glow it shows while targeted.</summary>
        private static GameObject PlaceGrappleRing(Transform parent, string name, Vector2 position)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.position = position;
            var ring = root.AddComponent<GrappleRing>();

            var visual = FindOrCreateChild(root, "Visual");
            visual.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
            var renderer = Ensure<SpriteRenderer>(visual);
            renderer.sprite = Shape("shape_telegraph_sigil");
            renderer.sharedMaterial = _material;
            renderer.color = new Color(0.62f, 0.6f, 0.58f);
            SetSorting(renderer, "GameplayEnvironment", 6);

            var glowObject = FindOrCreateChild(root, "Glow");
            glowObject.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
            var glow = Ensure<SpriteRenderer>(glowObject);
            glow.sprite = Shape("shape_soft_glow");
            glow.sharedMaterial = _material;
            glow.color = new Color(1f, 0.78f, 0.45f, 0.6f);
            glow.enabled = false;
            SetSorting(glow, "GameplayEnvironment", 5);
            SetReference(ring, "_glow", glow);
            return root;
        }

        private const string GrappleReturnsName = "GrappleReturns";

        /// <summary>Clears the old root of the grapple's places in the old zones.</summary>
        private static void BuildGrappleReturns(Transform level, GameProgress progress, GameObject pickup)
        {
            // The rings are in the zones' texts now (the Tomb's crypt nest, the Forest's hollow,
            // root nest and deep roots); only a world built before that still has this root.
            var old = level.Find(GrappleReturnsName);
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }
        }

        /// <summary>The ring of force a heavy landing throws out, as the Guardian's does.</summary>
        private static Shockwave BuildBossShockwave(GameObject root, Color color)
        {
            var shockwaveObject = FindOrCreateChild(root, "Shockwave");
            shockwaveObject.transform.localPosition = Vector3.zero;
            var shockwaveVisual = FindOrCreateChild(shockwaveObject, "Visual");
            var shockwaveRenderer = Ensure<SpriteRenderer>(shockwaveVisual);
            shockwaveRenderer.sprite = Shape("shape_shockwave");
            shockwaveRenderer.sharedMaterial = _material;
            shockwaveRenderer.color = color;
            SetSorting(shockwaveRenderer, "VFXFront", 0);
            var shockwaveHitbox = Ensure<Hitbox>(shockwaveObject);
            SetLayerMask(shockwaveHitbox, "_targetMask", 1 << PlayerLayer);
            var shockwave = Ensure<Shockwave>(shockwaveObject);
            SetReference(shockwave, "_hitbox", shockwaveHitbox);
            SetReference(shockwave, "_visual", shockwaveRenderer);
            return shockwave;
        }

        /// <summary>A boss's clips from its frames, with its own moveset as the required list.</summary>
        private static void BuildBossSet(string path, string prefix,
            PlaceholderEnemyArt.Spec<BossAnimationState>[] specs,
            Dictionary<BossAnimationState, Sprite[]> frames)
        {
            var set = CreateAsset<BossAnimationSet>(path);
            var serialized = new SerializedObject(set);
            var clips = serialized.FindProperty("_clips");
            clips.arraySize = specs.Length;
            for (var i = 0; i < specs.Length; i++)
            {
                var spec = specs[i];
                var element = clips.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("state").enumValueIndex = (int)spec.State;
                WriteClip(element.FindPropertyRelative("clip"),
                    $"{prefix}_{spec.State.ToString().ToLowerInvariant()}", spec.Fps, spec.Loops,
                    frames[spec.State], PlaceholderEnemyArt.AnchorsFor(prefix, spec.State));
            }

            var required = serialized.FindProperty("_requiredStates");
            required.arraySize = specs.Length;
            for (var i = 0; i < specs.Length; i++)
            {
                required.GetArrayElementAtIndex(i).enumValueIndex = (int)specs[i].State;
            }

            var idle = System.Array.Find(specs, s => s.State == BossAnimationState.IdleP1);
            WriteClip(serialized.FindProperty("_fallback"), $"{prefix}_idlep1", idle.Fps, true,
                frames[BossAnimationState.IdleP1],
                PlaceholderEnemyArt.AnchorsFor(prefix, BossAnimationState.IdleP1));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(set);
        }
    }
}
