using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// One-shot authoring helper that writes the MVP's data assets, prefabs and the Tomb Trial test
    /// level as ordinary saved assets. After a run the assets are edited in the Editor as usual.
    /// </summary>
    public static partial class ContentBuilder
    {
        private const string DataFolder = "Assets/Game/Data";
        private const string ItemFolder = "Assets/Game/Data/Items";
        private const string PrefabFolder = "Assets/Game/Prefabs";
        private const string ScenePath = "Assets/Game/Scenes/TombTrial.unity";
        private const string PickupPrefabPath = PrefabFolder + "/WorldPickup.prefab";
        private const string ItemDatabasePath = DataFolder + "/ItemDatabase.asset";
        private const string ProgressionCurveName = "ProgressionCurve.asset";
        private const string ProgressionCurvePath = DataFolder + "/" + ProgressionCurveName;
        private const string SpellFolder = DataFolder + "/Spells";
        private const string SpellDatabaseName = "SpellDatabase.asset";
        private const string SpellDatabasePath = DataFolder + "/" + SpellDatabaseName;
        private const string ProjectilePrefabPath = PrefabFolder + "/SpellProjectile.prefab";
        private const string SkillFolder = DataFolder + "/Skills";
        private const string SkillDatabaseName = "SkillDatabase.asset";
        private const string SkillDatabasePath = DataFolder + "/" + SkillDatabaseName;
        private const string AnimationFolder = DataFolder + "/Animation";
        private const string PlayerAnimationSetPath = AnimationFolder + "/PlayerAnimations.asset";
        private const string GreatswordAnimationSetPath =
            AnimationFolder + "/GreatswordAnimations.asset";
        private const string ZombieAnimationSetPath = AnimationFolder + "/ZombieAnimations.asset";
        private const string GuardAnimationSetPath =
            AnimationFolder + "/ShieldGuardAnimations.asset";
        private const string BossAnimationSetPath = AnimationFolder + "/GuardianAnimations.asset";
        private const string FeedbackFolder = DataFolder + "/Feedback";
        private const string FeedbackLibraryPath = FeedbackFolder + "/FeedbackLibrary.asset";
        private const string AudioEventFolder = DataFolder + "/Audio";
        private const string AudioLibraryPath = AudioEventFolder + "/AudioLibrary.asset";
        private const string SpritePath = "Assets/Game/Art/PlaceholderSquare.png";
        private const string MaterialPath = "Assets/Game/Art/PlaceholderUnlit.mat";
        private const string FrictionlessPath = "Assets/Game/Art/PlayerFrictionless.physicsMaterial2D";
        private const string InputActionsPath = "Assets/Settings/InputSystem_Actions.inputactions";

        private const int PlayerLayer = 8;
        private const int GroundLayer = 9;
        private const int EnemyLayer = 10;

        private static Sprite _sprite;
        private static Material _material;
        private static System.Collections.Generic.Dictionary<string, Sprite> _tomb;
        private static System.Collections.Generic.Dictionary<string, Sprite[]> _vfx;

        /// <summary>
        /// Regenerates art, audio, data and prefabs. The scenes are the source of truth for the
        /// level since 2026-09-26 and are left as they are — hand edits in the Editor survive —
        /// unless a scene file is missing, in which case it is built from scratch.
        /// </summary>
        [MenuItem("Forgotten Kingdom/Rebuild MVP Content")]
        public static void Build() => BuildContent(rebuildScenes: false);

        /// <summary>
        /// Everything <see cref="Build"/> does, then TombTrial and MainMenu rebuilt from scratch
        /// with <c>NewScene</c>. This throws away every hand edit made to those scenes; it is for
        /// recovering a broken scene, not for everyday content work. Asks first in the Editor;
        /// in batch mode, calling it is the confirmation.
        /// </summary>
        [MenuItem("Forgotten Kingdom/Rebuild Scenes From Scratch (discards scene edits)")]
        public static void BuildWithScenes()
        {
            if (!Application.isBatchMode && !EditorUtility.DisplayDialog("Rebuild scenes",
                    "TombTrial and MainMenu will be rebuilt from the generator. Every hand edit "
                    + "made to them in the Editor will be lost.", "Rebuild", "Cancel"))
            {
                return;
            }

            BuildContent(rebuildScenes: true);
        }

        private static void BuildContent(bool rebuildScenes)
        {
            _sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            _material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (_sprite == null || _material == null)
            {
                Debug.LogError("Placeholder art is missing; cannot build content.");
                return;
            }

            PlaceholderImport.FinalArtUsed = 0;
            // The layer order has to exist before anything is assigned to it.
            SortingLayerSetup.Ensure();
            _tomb = PlaceholderTombArt.Generate();
            _shapes = ProductionSpriteArt.Generate();
            BuildHudAssets();

            EnsureFolder(DataFolder);
            BuildProgressionCurve();
            BuildSpells();
            BuildSkills();
            BuildItems();
            var zombieDrops = BuildDropTable("ZombieSoldierDrops", @"{
                ""_entries"": [
                    {""_isCurrency"":true,""_chance"":1,""_isGuaranteed"":true,
                     ""_minAmount"":5,""_maxAmount"":12},
                    {""_itemId"":""iron_scrap"",""_chance"":0.25,""_minAmount"":1,""_maxAmount"":2},
                    {""_itemId"":""healing_herb"",""_chance"":0.1,""_minAmount"":1,""_maxAmount"":1}
                ]
            }");
            var guardDrops = BuildDropTable("ShieldGuardDrops", @"{
                ""_entries"": [
                    {""_isCurrency"":true,""_chance"":1,""_isGuaranteed"":true,
                     ""_minAmount"":15,""_maxAmount"":30},
                    {""_itemId"":""iron_scrap"",""_chance"":0.5,""_minAmount"":1,""_maxAmount"":3},
                    {""_itemId"":""guard_emblem"",""_chance"":0.1,""_minAmount"":1,""_maxAmount"":1}
                ]
            }");
            var zombie = BuildZombieDefinition();
            var guard = BuildShieldGuardDefinition();
            AssetDatabase.SaveAssets();

            // These have to exist before the prefabs that point at them.
            BuildAnimationSet();
            BuildEnemyAnimationSets();
            BuildFeedback();
            BuildAudio();
            BuildSpellProjectilePrefab();
            BuildLostCurrencyMarkPrefab();
            var playerPrefab = BuildPlayerPrefab();
            var zombiePrefab = BuildEnemyPrefab("ZombieSoldier", zombie, false,
                // 70: about four sword swings, a Hollow Knight husk's worth (item 37).
                new Color(0.45f, 0.62f, 0.35f), 70f, 50f, zombieDrops,
                // Dry and long dead: fire finds it easily, cold barely touches it.
                Resist(physical: 0f, fire: -0.25f, frost: 0.15f, magic: 0f));
            var guardPrefab = BuildEnemyPrefab("ShieldGuard", guard, true,
                new Color(0.55f, 0.55f, 0.68f), 190f, 100f, guardDrops,
                // Plate turns a blade better than it turns a spell.
                Resist(physical: 0.15f, fire: 0f, frost: 0f, magic: 0f));
            var bossPrefab = BuildBossPrefab();
            // The Withered Forest's own assets. Built after the Tomb's, because it reuses every
            // one of its builders and only adds data on top of them.
            var forest = BuildForestContent();
            // The capital's four (item 2, stage C); the level places them by prefab name.
            BuildCapitalContent();
            // The Tomb's bone archer (item 3 of the 15 zones); its arrow is the forest's.
            BuildTombEnemies();
            // The Fallen Village's own three (item 5b); the scarecrow lets out the capital's raven.
            BuildVillageEnemies();
            // Their king (stage D); the throne hall places him by prefab name too.
            BuildCapitalBoss();
            // The Fallen Village's bell-keeper (item 5 of the 15 zones); the village places him by name.
            BuildVillageBoss();
            // The capital's Commander (item 6b), in the hall before the King's throne.
            BuildCapitalCommander();
            var checkpointPrefab = BuildCheckpointPrefab();
            var dummyPrefab = BuildDummyPrefab();
            var attackerPrefab = BuildAttackerPrefab();
            // Every character frame, the training targets included, gets its normal map.
            // The people of the world (item 70), before the normals so their figures get them too.
            BuildNpcContent();
            CharacterNormals.AttachAll();
            BuildPickupPrefab();
            AssetDatabase.SaveAssets();

            if (rebuildScenes || !File.Exists(ScenePath))
            {
                BuildScene(playerPrefab, zombiePrefab, guardPrefab, bossPrefab, checkpointPrefab,
                    dummyPrefab, attackerPrefab, forest);
                // The people stand by altars, so they are placed once the level is whole; then the
                // whole world is split into its world scene and one scene per zone.
                WorldScenes.Rebuild(() =>
                {
                    BuildCapital();
                    ConfigureCapitalAtmosphere();
                    SceneNpcs.Build();
                    BuildExploration();
                });
                AssetDatabase.SaveAssets();
            }
            else
            {
                Debug.Log($"Forgotten Kingdom: {ScenePath} kept as edited; "
                    + "use BuildWithScenes to rebuild it from scratch.");
            }

            // After the game scene, because the menu is placed in front of it in the build.
            if (rebuildScenes || !File.Exists(MainMenuScenePath))
            {
                BuildMainMenuScene();
                AssetDatabase.SaveAssets();
            }
            AssetDatabase.Refresh();
            Debug.Log("Forgotten Kingdom: MVP content rebuilt; "
                + $"{PlaceholderImport.FinalArtUsed} file(s) taken from final art.");
        }

        // ---------- data ----------

        /// <summary>
        /// The starter sword, unchanged in feel: base 20 damage with 0.75/0.9/1.25/1.75 comes to
        /// exactly the 15/18/25/35 it always dealt, now from the weapon rather than from code.
        /// </summary>
        private static WeaponDefinition BuildSword()
        {
            var weapon = CreateAsset<WeaponDefinition>($"{DataFolder}/Sword.asset");
            JsonUtility.FromJsonOverwrite(@"{
                ""_itemId"": ""old_sword"",
                ""_displayName"": ""Старий Меч"",
                ""_weaponType"": 0,
                ""_baseDamage"": 20,
                ""_strengthScaling"": 0.5,
                ""_dexterityScaling"": 0.4,
                ""_strengthRequirement"": 5,
                ""_dexterityRequirement"": 5,
                ""_attackSpeed"": 1.0,
                ""_staminaMultiplier"": 1.0,
                ""_staggerMultiplier"": 1.0,
                ""_knockbackMultiplier"": 1.0,
                ""_comboResetTime"": 0.55,
                ""_visualColor"": {""r"":0.82,""g"":0.84,""b"":0.9,""a"":1},
                ""_visualSize"": {""x"":0.12,""y"":0.9},
                ""_visualOffset"": {""x"":0.3,""y"":0.15},
                ""_lightCombo"": [
                    {""_displayName"":""Light 1"",""_animationState"":""LightAttack1"",""_damage"":0,
                     ""_damageMultiplier"":0.75,
                     ""_damageType"":0,""_staminaCost"":0,""_staggerDamage"":10,
                     ""_guardDamage"":5,""_knockback"":3,
                     ""_hitStop"":0.03,""_isUnblockable"":false,
                     ""_windup"":0.10,""_activeTime"":0.07,""_recovery"":0.18,
                     ""_comboLinkTime"":0.17,""_cancelableAfter"":0.16,
                     ""_hitboxOffset"":{""x"":0.85,""y"":0.05},""_hitboxSize"":{""x"":1.3,""y"":1.2},
                     ""_movementScale"":0.3},
                    {""_displayName"":""Light 2"",""_animationState"":""LightAttack2"",""_damage"":0,
                     ""_damageMultiplier"":0.9,
                     ""_damageType"":0,""_staminaCost"":0,""_staggerDamage"":12,
                     ""_guardDamage"":7,""_knockback"":4,
                     ""_hitStop"":0.04,""_isUnblockable"":false,
                     ""_windup"":0.09,""_activeTime"":0.07,""_recovery"":0.20,
                     ""_comboLinkTime"":0.16,""_cancelableAfter"":0.16,
                     ""_hitboxOffset"":{""x"":0.9,""y"":0.05},""_hitboxSize"":{""x"":1.45,""y"":1.25},
                     ""_movementScale"":0.2},
                    {""_displayName"":""Light 3"",""_animationState"":""LightAttack3"",""_damage"":0,
                     ""_damageMultiplier"":1.25,
                     ""_damageType"":0,""_staminaCost"":0,""_staggerDamage"":22,
                     ""_guardDamage"":14,""_knockback"":7,
                     ""_hitStop"":0.06,""_isUnblockable"":false,
                     ""_windup"":0.15,""_activeTime"":0.09,""_recovery"":0.32,
                     ""_comboLinkTime"":0.45,""_cancelableAfter"":0.26,
                     ""_hitboxOffset"":{""x"":1.0,""y"":0.05},""_hitboxSize"":{""x"":1.7,""y"":1.35},
                     ""_movementScale"":0.1}
                ],
                ""_heavyAttack"": {""_displayName"":""Heavy"",""_animationState"":""HeavyAttack"",
                     ""_damage"":0,""_damageMultiplier"":1.75,
                     ""_damageType"":0,""_staminaCost"":25,""_staggerDamage"":45,
                     ""_guardDamage"":45,""_knockback"":9,""_hitStop"":0.08,""_isUnblockable"":false,
                     ""_windup"":0.32,""_activeTime"":0.11,""_recovery"":0.44,
                     ""_comboLinkTime"":0.80,""_cancelableAfter"":0.45,
                     ""_hitboxOffset"":{""x"":1.05,""y"":0.05},""_hitboxSize"":{""x"":1.95,""y"":1.5},
                     ""_movementScale"":0.05}
            }", weapon);
            EditorUtility.SetDirty(weapon);
            return weapon;
        }

        /// <summary>
        /// The second real weapon type, and the proof the architecture holds: slower, heavier,
        /// costlier and far better at breaking a guard. Nothing in code knows it exists.
        /// </summary>
        private static WeaponDefinition BuildGreatsword()
        {
            var weapon = CreateAsset<WeaponDefinition>($"{DataFolder}/Greatsword.asset");
            JsonUtility.FromJsonOverwrite(@"{
                ""_itemId"": ""iron_greatsword"",
                ""_displayName"": ""Залізний Дворучник"",
                ""_weaponType"": 1,
                ""_baseDamage"": 34,
                ""_strengthScaling"": 0.9,
                ""_dexterityScaling"": 0.1,
                ""_strengthRequirement"": 10,
                ""_dexterityRequirement"": 3,
                ""_attackSpeed"": 0.78,
                ""_staminaMultiplier"": 1.6,
                ""_staggerMultiplier"": 1.7,
                ""_knockbackMultiplier"": 1.5,
                ""_comboResetTime"": 0.8,
                ""_visualColor"": {""r"":0.72,""g"":0.74,""b"":0.8,""a"":1},
                ""_visualSize"": {""x"":0.2,""y"":1.35},
                ""_visualOffset"": {""x"":0.36,""y"":0.2},
                ""_lightCombo"": [
                    {""_displayName"":""Wide Swing"",""_animationState"":""GreatLight1"",""_damage"":0,
                     ""_damageMultiplier"":0.85,
                     ""_damageType"":0,""_staminaCost"":0,""_staggerDamage"":20,
                     ""_guardDamage"":10,""_knockback"":5,
                     ""_hitStop"":0.05,""_isUnblockable"":false,
                     ""_windup"":0.24,""_activeTime"":0.10,""_recovery"":0.30,
                     ""_comboLinkTime"":0.34,""_cancelableAfter"":0.30,
                     ""_hitboxOffset"":{""x"":1.05,""y"":0.05},""_hitboxSize"":{""x"":1.9,""y"":1.5},
                     ""_movementScale"":0.2},
                    {""_displayName"":""Reverse Swing"",""_animationState"":""GreatLight2"",""_damage"":0,
                     ""_damageMultiplier"":0.95,
                     ""_damageType"":0,""_staminaCost"":0,""_staggerDamage"":24,
                     ""_guardDamage"":12,""_knockback"":6,
                     ""_hitStop"":0.05,""_isUnblockable"":false,
                     ""_windup"":0.22,""_activeTime"":0.10,""_recovery"":0.34,
                     ""_comboLinkTime"":0.33,""_cancelableAfter"":0.30,
                     ""_hitboxOffset"":{""x"":1.1,""y"":0.05},""_hitboxSize"":{""x"":2.0,""y"":1.55},
                     ""_movementScale"":0.15},
                    {""_displayName"":""Overhead Finisher"",""_animationState"":""GreatLight3"",
                     ""_damage"":0,""_damageMultiplier"":1.35,
                     ""_damageType"":0,""_staminaCost"":0,""_staggerDamage"":40,
                     ""_guardDamage"":26,""_knockback"":10,
                     ""_hitStop"":0.09,""_isUnblockable"":false,
                     ""_windup"":0.34,""_activeTime"":0.12,""_recovery"":0.52,
                     ""_comboLinkTime"":0.90,""_cancelableAfter"":0.50,
                     ""_hitboxOffset"":{""x"":1.15,""y"":0.05},""_hitboxSize"":{""x"":2.3,""y"":1.8},
                     ""_movementScale"":0.05}
                ],
                ""_heavyAttack"": {""_displayName"":""Great Overhead"",""_animationState"":""GreatHeavy"",
                     ""_damage"":0,""_damageMultiplier"":1.9,
                     ""_damageType"":0,""_staminaCost"":25,""_staggerDamage"":60,
                     ""_guardDamage"":60,""_knockback"":12,""_hitStop"":0.12,
                     ""_isUnblockable"":false,
                     ""_windup"":0.50,""_activeTime"":0.14,""_recovery"":0.62,
                     ""_comboLinkTime"":1.10,""_cancelableAfter"":0.66,
                     ""_hitboxOffset"":{""x"":1.2,""y"":0.05},""_hitboxSize"":{""x"":2.5,""y"":2.0},
                     ""_movementScale"":0.0}
            }", weapon);
            EditorUtility.SetDirty(weapon);
            return weapon;
        }

        private static EnemyDefinition BuildZombieDefinition()
        {
            var enemy = CreateAsset<EnemyDefinition>($"{DataFolder}/ZombieSoldier.asset");
            JsonUtility.FromJsonOverwrite(@"{
                ""_displayName"": ""Зомбі-солдат"",
                ""_experienceReward"": 25,
                ""_patrolSpeed"": 1.5, ""_chaseSpeed"": 3.2, ""_returnSpeed"": 2.2,
                ""_patrolRange"": 4.0, ""_idleTime"": 0.8, ""_idleTimeVariance"": 0.8,
                ""_detectionRange"": 8.0, ""_loseSightRange"": 11.0,
                ""_verticalDetectionRange"": 3.5, ""_alertTime"": 0.35,
                ""_searchTime"": 2.0, ""_behindDetectionRange"": 2.0,
                ""_facingDeadZone"": 0.35, ""_aiTickInterval"": 0.08,
                ""_attackRange"": 1.6, ""_verticalAttackRange"": 1.3, ""_attackCooldown"": 1.2,
                ""_facingLockTime"": 0.12,
                ""_hurtTime"": 0.3, ""_hitReactionCooldown"": 0.55, ""_deathTime"": 1.6,
                ""_maxChaseTime"": 12.0,
                ""_decisionCooldown"": 0.25, ""_guardMovementScale"": 0.3,
                ""_attacks"": [
                    {""_kind"":0,""_cooldown"":0,""_minRange"":0,""_maxRange"":1.7,""_weight"":1,
                     ""_facingLockTime"":0.12,
                     ""_attack"":{""_displayName"":""Sword Slash"",""_animationState"":""Attack"",
                       ""_damage"":16,""_damageType"":0,""_staminaCost"":0,
                       ""_staggerDamage"":15,""_guardDamage"":15,""_knockback"":5,""_hitStop"":0.04,
                       ""_isUnblockable"":false,""_isParryable"":true,
                       ""_windup"":0.42,""_activeTime"":0.10,""_recovery"":0.42,
                       ""_comboLinkTime"":0.5,""_cancelableAfter"":0.5,""_movementScale"":0,
                       ""_hitboxOffset"":{""x"":0.95,""y"":0.0},
                       ""_hitboxSize"":{""x"":1.6,""y"":1.4}}}
                ]
            }", enemy);
            EditorUtility.SetDirty(enemy);
            return enemy;
        }

        private static EnemyDefinition BuildShieldGuardDefinition()
        {
            var enemy = CreateAsset<EnemyDefinition>($"{DataFolder}/ShieldGuard.asset");
            JsonUtility.FromJsonOverwrite(@"{
                ""_displayName"": ""Щитоносець"",
                ""_experienceReward"": 60,
                ""_patrolSpeed"": 1.2, ""_chaseSpeed"": 2.6, ""_returnSpeed"": 1.8,
                ""_patrolRange"": 3.0, ""_idleTime"": 1.2, ""_idleTimeVariance"": 0.8,
                ""_detectionRange"": 8.5, ""_loseSightRange"": 12.0,
                ""_verticalDetectionRange"": 3.5, ""_alertTime"": 0.45,
                ""_searchTime"": 3.0, ""_behindDetectionRange"": 2.5,
                ""_facingDeadZone"": 0.4, ""_aiTickInterval"": 0.08,
                ""_attackRange"": 1.9, ""_verticalAttackRange"": 1.4, ""_attackCooldown"": 1.1,
                ""_facingLockTime"": 0.14, ""_decisionCooldown"": 0.3,
                ""_guardMovementScale"": 0.3,
                ""_hurtTime"": 0.3, ""_hitReactionCooldown"": 0.6, ""_deathTime"": 1.6,
                ""_maxChaseTime"": 14.0,
                ""_attacks"": [
                    {""_kind"":0,""_cooldown"":0,""_minRange"":0,""_maxRange"":1.9,""_weight"":3,
                     ""_facingLockTime"":0.14,
                     ""_attack"":{""_displayName"":""Sword Slash"",""_animationState"":""Attack"",
                       ""_damage"":22,""_damageType"":0,""_staggerDamage"":18,""_guardDamage"":20,
                       ""_knockback"":6,""_hitStop"":0.05,
                       ""_isUnblockable"":false,""_isParryable"":true,
                       ""_windup"":0.45,""_activeTime"":0.11,""_recovery"":0.45,
                       ""_comboLinkTime"":0.9,""_cancelableAfter"":0.9,""_movementScale"":0,
                       ""_hitboxOffset"":{""x"":1.0,""y"":0.0},
                       ""_hitboxSize"":{""x"":1.7,""y"":1.4}}},
                    {""_kind"":1,""_cooldown"":4.0,""_minRange"":0,""_maxRange"":2.0,""_weight"":1,
                     ""_weightAgainstGuard"":2.5,""_facingLockTime"":0.3,
                     ""_attack"":{""_displayName"":""Heavy Chop"",""_animationState"":""HeavyAttack"",
                       ""_damage"":35,""_damageType"":0,""_staggerDamage"":30,""_guardDamage"":45,
                       ""_knockback"":9,""_hitStop"":0.08,
                       ""_isUnblockable"":false,""_isParryable"":true,
                       ""_windup"":0.75,""_activeTime"":0.13,""_recovery"":0.80,
                       ""_comboLinkTime"":1.2,""_cancelableAfter"":1.2,""_movementScale"":0,
                       ""_hitboxOffset"":{""x"":1.15,""y"":0.0},
                       ""_hitboxSize"":{""x"":2.0,""y"":1.6}}},
                    {""_kind"":2,""_cooldown"":3.0,""_minRange"":0,""_maxRange"":1.5,""_weight"":6,
                     ""_requiredPressure"":3,""_facingLockTime"":0.12,
                     ""_attack"":{""_displayName"":""Shield Bash"",""_animationState"":""ShieldBash"",
                       ""_damage"":12,""_damageType"":0,""_staggerDamage"":20,""_guardDamage"":28,
                       ""_knockback"":8,""_hitStop"":0.05,
                       ""_isUnblockable"":false,""_isParryable"":true,
                       ""_windup"":0.30,""_activeTime"":0.10,""_recovery"":0.40,
                       ""_comboLinkTime"":0.7,""_cancelableAfter"":0.7,""_movementScale"":0,
                       ""_hitboxOffset"":{""x"":0.95,""y"":0.0},
                       ""_hitboxSize"":{""x"":1.4,""y"":1.3}}}
                ]
            }", enemy);
            EditorUtility.SetDirty(enemy);
            return enemy;
        }

        // ---------- prefabs ----------

        /// <summary>
        /// The socket the equipped weapon hangs from. It lives under the visual root so it flips
        /// with the player, and holds nothing until equipment says what to draw.
        /// </summary>
        private static WeaponVisual BuildWeaponMount(GameObject root)
        {
            var visualRoot = root.transform.Find("Visual");
            var parent = visualRoot != null ? visualRoot.gameObject : root;
            var mount = FindOrCreateChild(parent, "WeaponMount");
            mount.transform.localPosition = new Vector3(0.3f, 0.15f, 0f);
            mount.transform.localScale = Vector3.one;

            var bladeObject = FindOrCreateChild(mount, "Blade");
            bladeObject.transform.localScale = new Vector3(0.12f, 0.9f, 1f);
            var blade = Ensure<SpriteRenderer>(bladeObject);
            blade.sprite = _sprite;
            blade.sharedMaterial = _material;
            SetSorting(blade, "Weapons", 0);

            var weaponVisual = Ensure<WeaponVisual>(mount);
            SetReference(weaponVisual, "_mount", mount.transform);
            SetReference(weaponVisual, "_renderer", blade);
            return weaponVisual;
        }

        private static GameObject BuildPlayerPrefab()
        {
            var path = $"{PrefabFolder}/Player.prefab";
            var hasExisting = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
            // Editing the prefab contents keeps the existing GUIDs and scene references intact.
            var root = hasExisting
                ? PrefabUtility.LoadPrefabContents(path)
                : CreateActorRoot("Player", PlayerLayer, new Color(0.26f, 0.75f, 0.76f),
                    new Vector2(0.8f, 1.6f));
            root.layer = PlayerLayer;

            var body = Ensure<Rigidbody2D>(root);
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.excludeLayers = 1 << EnemyLayer;

            var capsule = Ensure<CapsuleCollider2D>(root);
            capsule.size = new Vector2(0.8f, 1.6f);
            capsule.sharedMaterial =
                AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(FrictionlessPath);

            var input = Ensure<PlayerInputReader>(root);
            SetReference(input, "_inputActions",
                AssetDatabase.LoadAssetAtPath<Object>(InputActionsPath));

            Ensure<WallMovement>(root);
            var motor = Ensure<PlayerMotor2D>(root);
            SetLayerMask(motor, "_groundMask", 1 << GroundLayer);
            var visual = root.transform.Find("Visual");
            SetReference(motor, "_visual", visual != null ? visual : root.transform);

            SetFloat(Ensure<Health>(root), "_maxHealth", 100f);
            var stamina = Ensure<Stamina>(root);
            SetFloat(stamina, "_maxStamina", 100f);
            SetFloat(stamina, "_regenerationPerSecond", 25f);
            SetFloat(stamina, "_regenerationDelay", 1f);
            SetFloat(stamina, "_exhaustedRegenerationDelay", 1.6f);
            var stagger = Ensure<StaggerMeter>(root);
            SetFloat(stagger, "_threshold", 55f);
            SetFloat(stagger, "_stunDuration", 0.8f);

            Ensure<StatusEffects>(root);
            var actor = Ensure<CombatActor>(root);
            SetEnum(actor, "_faction", (int)Faction.Player);
            SetFloat(actor, "_knockbackLift", 1.2f);
            // Hollow Knight's mercy window (item 37): about a second after a hit, so a group
            // cannot take the player apart in one volley.
            SetFloat(actor, "_hitInvulnerability", 1f);
            Ensure<Hurtbox>(root);

            var hitbox = Ensure<Hitbox>(root);
            SetLayerMask(hitbox, "_targetMask", 1 << EnemyLayer);

            // Equipment decides which weapon swings; the prefab holds no weapon of its own.
            var equipment = Ensure<PlayerEquipment>(root);
            SetString(equipment, "_starterWeaponId", ItemIds.StarterWeapon);
            var stats = Ensure<PlayerStats>(root);
            SetReference(stats, "_equipment", equipment);
            SetReference(equipment, "_stats", stats);
            SetReference(stats, "_health", root.GetComponent<Health>());
            SetReference(stats, "_stamina", stamina);
            SetReference(stats, "_stagger", stagger);

            var weaponVisual = BuildWeaponMount(root);

            var combat = Ensure<PlayerCombat>(root);
            SetReference(combat, "_equipment", equipment);
            SetReference(combat, "_stats", stats);
            SetReference(combat, "_weaponVisual", weaponVisual);
            // Only ever used by scenes that have no progression at all, such as the isolated
            // combat test scenes; in the game equipment always answers first.
            SetReference(combat, "_fallbackWeapon",
                AssetDatabase.LoadAssetAtPath<WeaponDefinition>($"{DataFolder}/Sword.asset"));
            SetReference(combat, "_hitbox", hitbox);
            SetReference(combat, "_stamina", stamina);

            var defense = Ensure<PlayerDefense>(root);
            SetReference(defense, "_stamina", stamina);
            SetFloat(defense, "_parryWindow", 0.14f);
            SetFloat(defense, "_parryCooldown", 0.45f);
            SetFloat(defense, "_parryStaminaCost", 0f);
            SetFloat(defense, "_parryStaggerDamage", 45f);
            SetFloat(defense, "_parryHitStop", 0.08f);
            SetFloat(defense, "_staminaPerGuardDamage", 1f);
            SetFloat(defense, "_guardBreakStun", 0.9f);

            var dash = new SerializedObject(motor);
            dash.FindProperty("_dashSettings._dashSpeed").floatValue = 16f;
            dash.FindProperty("_dashSettings._dashDuration").floatValue = 0.18f;
            dash.FindProperty("_dashSettings._cooldown").floatValue = 0.35f;
            dash.FindProperty("_dashSettings._staminaCost").floatValue = 0f;
            dash.FindProperty("_dashSettings._invincibilityDelay").floatValue = 0.02f;
            dash.FindProperty("_dashSettings._invincibilityDuration").floatValue = 0.14f;
            dash.FindProperty("_dashSettings._airDashes").intValue = 1;
            dash.ApplyModifiedPropertiesWithoutUndo();

            // The skill layer: what is learned, what a hit ends up being worth, what is in the
            // skill bar and what is temporarily true.
            var mana = Ensure<Mana>(root);
            SetFloat(mana, "_maxMana", 100f);
            SetReference(stats, "_mana", mana);
            // Mana does not come back on its own; the blade earns it (the user's choice, 2026-09-28).
            var manaGain = Ensure<WeaponManaGain>(root);
            SetReference(manaGain, "_combat", combat);
            SetReference(manaGain, "_mana", mana);
            SetFloat(manaGain, "_perHit", 10f);
            SetFloat(manaGain, "_perHeavyHit", 15f);

            var buffs = Ensure<TemporaryBuffs>(root);
            var offense = Ensure<PlayerOffense>(root);
            SetReference(offense, "_equipment", equipment);
            SetReference(offense, "_buffs", buffs);
            SetFloat(offense, "_baseCritChance", 0f);
            SetFloat(offense, "_baseCritMultiplier", 1.5f);

            Ensure<ShadowAbilities>(root);
            var activeSkills = Ensure<ActiveSkillController>(root);
            SetReference(activeSkills, "_equipment", equipment);
            SetReference(activeSkills, "_stats", stats);
            SetReference(activeSkills, "_offense", offense);
            SetReference(activeSkills, "_motor", motor);
            SetReference(activeSkills, "_stamina", stamina);
            SetReference(activeSkills, "_hitbox", hitbox);
            SetReference(activeSkills, "_actor", actor);
            SetReference(stats, "_motor", motor);
            SetReference(stats, "_buffs", buffs);

            // The spell layer: where a spell comes out, and the caster that shapes it.
            var castPoint = FindOrCreateChild(root, "CastPoint");
            castPoint.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            var caster = Ensure<SpellCaster>(root);
            SetReference(caster, "_stats", stats);
            SetReference(caster, "_offense", offense);
            SetReference(caster, "_buffs", buffs);
            SetReference(caster, "_mana", mana);
            SetReference(caster, "_actor", actor);
            SetReference(caster, "_motor", motor);
            SetReference(caster, "_castPoint", castPoint.transform);
            SetReference(caster, "_projectilePrefab",
                AssetDatabase.LoadAssetAtPath<SpellProjectile>(ProjectilePrefabPath));
            SetLayerMask(caster, "_areaTargetMask", 1 << EnemyLayer);

            var character = Ensure<PlayerCharacter>(root);
            SetReference(character, "_caster", caster);
            SetReference(character, "_mana", mana);
            SetReference(character, "_activeSkills", activeSkills);
            SetReference(character, "_buffs", buffs);
            SetReference(character, "_offense", offense);
            SetFloat(character, "_inputBufferTime", 0.15f);
            SetFloat(character, "_blockMovementScale", 0.28f);
            SetReference(character, "_equipment", equipment);
            SetReference(character, "_stats", stats);
            SetFloat(character, "_weaponSwitchTime", 0.28f);
            SetFloat(character, "_weaponSwitchMovementScale", 0.6f);

            var combatStatus = Ensure<PlayerCombatStatus>(root);
            SetReference(combatStatus, "_actor", actor);
            SetReference(combatStatus, "_combat", combat);
            SetFloat(combatStatus, "_combatTimeout", 4f);

            var interact = FindOrCreateChild(root, "InteractRange");
            interact.layer = PlayerLayer;
            var interactCollider = Ensure<CircleCollider2D>(interact);
            interactCollider.isTrigger = true;
            interactCollider.radius = 1.6f;
            Ensure<PlayerInteractor>(interact);
            BuildPlayerVisual(root);
            BuildStatusVfx(root, 0.85f);
            BuildInvulnerabilityBlink(root);
            Ensure<MovementFeedback>(root);
            Ensure<FlaskShards>(root);
            // The Ashen King's grapple (item 2): inert until the ability is owned.
            Ensure<PlayerGrapple>(root);
            BuildWeaponTrail(root, combat);
            BuildLostCurrencyDrop(root);
            StripMissingScripts(root);

            var saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            if (hasExisting)
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            else
            {
                Object.DestroyImmediate(root);
            }
            return saved;
        }

        /// <summary>
        /// One enemy prefab. Every part of it — body, poise, statuses, loot, experience, reset —
        /// is the same for every kind; what differs is the numbers and the artwork. The forest's
        /// three enemies go through this unchanged, which is the point: a new enemy should be a
        /// new definition and a new sprite set, not new code.
        /// </summary>
        private static GameObject BuildEnemyPrefab(string name, EnemyDefinition definition,
            bool hasShield, Color color, float health, float staggerThreshold, DropTable drops,
            ResistanceProfile resistances = null, string animationSetPath = null,
            Vector2? bodySize = null, float staggerResistance = -1f,
            System.Action<GameObject> configure = null)
        {
            var size = bodySize ?? new Vector2(0.85f, 1.7f);
            var root = CreateActorRoot(name, EnemyLayer, color, size);
            var body = Ensure<Rigidbody2D>(root);
            body.gravityScale = 3.5f;
            body.freezeRotation = true;
            // Physics runs at 50 Hz; without interpolation the body jumps between steps on a
            // 60 or 120 Hz screen while the smoothly interpolated player moves past it.
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.excludeLayers = (1 << PlayerLayer) | (1 << EnemyLayer);

            var capsule = Ensure<CapsuleCollider2D>(root);
            capsule.size = size;
            capsule.sharedMaterial =
                AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(FrictionlessPath);

            var motor = Ensure<EnemyMotor2D>(root);
            SetLayerMask(motor, "_groundMask", 1 << GroundLayer);
            SetReference(motor, "_visual", root.transform.Find("Visual"));
            // The probes follow the body: a low, wide hound needs them at its own height, not at
            // a soldier's, or it reads the floor as a cliff and refuses to walk anywhere.
            var half = size.y * 0.5f;
            SetVector(motor, "_groundProbeOffset", new Vector2(0f, -half - 0.05f));
            SetVector(motor, "_ledgeProbeOffset", new Vector2(size.x * 0.6f, -half));
            SetVector(motor, "_wallProbeOffset", new Vector2(0f, -half * 0.35f));

            SetFloat(Ensure<Health>(root), "_maxHealth", health);
            var stagger = Ensure<StaggerMeter>(root);
            SetFloat(stagger, "_threshold", staggerThreshold);
            SetFloat(stagger, "_stunDuration", 1.2f);
            SetFloat(stagger, "_recoveryPerSecond", 18f);
            SetFloat(stagger, "_recoveryDelay", 2f);
            // An armoured guard keeps its footing better than a shambling zombie.
            SetFloat(stagger, "_resistance",
                staggerResistance >= 0f ? staggerResistance : (hasShield ? 0.35f : 0f));

            Ensure<StatusEffects>(root);
            var actor = Ensure<CombatActor>(root);
            SetFloat(actor, "_hitInvulnerability", 0.05f);
            Ensure<Hurtbox>(root);

            var hitbox = Ensure<Hitbox>(root);
            SetLayerMask(hitbox, "_targetMask", 1 << PlayerLayer);

            EnemyGuard guard = null;
            if (hasShield)
            {
                guard = Ensure<EnemyGuard>(root);
                SetFloat(guard, "_maxGuard", 100f);
                SetFloat(guard, "_recoveryPerSecond", 18f);
                SetFloat(guard, "_recoveryDelay", 2f);
                SetFloat(guard, "_recoveryAfterBreak", 0.55f);
                SetFloat(guard, "_breakDuration", 1.6f);
                SetFloat(guard, "_blockedDamageLeak", 0.1f);
                SetFloat(guard, "_blockedStaggerShare", 0.3f);
                SetFloat(guard, "_parryGuardDamage", 25f);
                SetFloat(guard, "_reactionDelay", 0.22f);
                SetFloat(guard, "_minGuardDuration", 0.5f);
                SetFloat(guard, "_maxGuardDuration", 1.5f);
                SetFloat(guard, "_guardCooldown", 0.6f);
                SetFloat(guard, "_lightGuardDamage", 25f);
                SetFloat(guard, "_pressurePerLightHit", 1f);
                SetFloat(guard, "_pressureDecayPerSecond", 0.7f);
            }

            // The tell sits above the head and is switched on for the windup only.
            var telegraphObject = FindOrCreateChild(root, "Telegraph");
            telegraphObject.transform.localPosition = new Vector3(0f, half + 0.5f, 0f);
            telegraphObject.transform.localScale = new Vector3(0.45f, 0.45f, 1f);
            var telegraph = Ensure<SpriteRenderer>(telegraphObject);
            telegraph.sprite = Shape("shape_telegraph_sigil");
            telegraph.sharedMaterial = _material;
            SetSorting(telegraph, "VFXFront", 0);

            var enemyCombat = Ensure<EnemyCombat>(root);
            SetReference(enemyCombat, "_hitbox", hitbox);
            SetReference(enemyCombat, "_telegraph", telegraph);

            BuildStatusBars(root, actor, half + 0.3f);

            var controller = Ensure<EnemyController>(root);
            SetReference(controller, "_definition", definition);
            SetReference(controller, "_combat", enemyCombat);
            SetReference(controller, "_bodyCollider", capsule);
            SetReference(controller, "_visualRoot", root.transform.Find("Visual").gameObject);
            SetEnum(controller, "_resetPolicy", (int)EnemyResetPolicy.RespawnOnRestOrDeath);

            var dropper = Ensure<LootDropper>(root);
            SetReference(dropper, "_enemy", controller);
            SetReference(dropper, "_dropTable", drops);

            var experience = Ensure<ExperienceReward>(root);
            SetReference(experience, "_enemy", controller);
            ApplyResistances(Ensure<Resistances>(root), resistances);
            if (guard != null)
            {
                SetReference(controller, "_guard", guard);
            }

            BuildEnemyVisual(root,
                animationSetPath ?? (hasShield ? GuardAnimationSetPath : ZombieAnimationSetPath),
                size.y, hasShield);
            BuildStatusVfx(root, size.y * 0.53f);
            BuildHitFlash(root);
            BuildStunIndicator(root, size.y * 0.5f + 0.45f);
            // Anything one kind needs and the others do not — a voice, for now — goes on before
            // the prefab is written, so it is part of the asset rather than patched into it.
            configure?.Invoke(root);
            StripMissingScripts(root);
            return SavePrefab(root, $"{PrefabFolder}/{name}.prefab");
        }

        /// <summary>
        /// Health over poise, both anchored at their left edge so the fill scales from there.
        /// </summary>
        /// <summary>
        /// A slim health bar with poise under it, shown only for a few seconds after a hit.
        /// Bosses get none over their heads: their bar is the HUD's, with the name.
        /// </summary>
        private static ActorStatusBars BuildStatusBars(GameObject root, CombatActor actor,
            float height, bool isBoss = false)
        {
            var barsRoot = FindOrCreateChild(root, "StatusBars");
            barsRoot.transform.localPosition = new Vector3(0f, height, 0f);
            var health = BuildBar(barsRoot, "Health",
                new Color(0.62f, 0.13f, 0.17f), 0.055f, out var healthFill);
            var stagger = BuildBar(barsRoot, "Stagger",
                new Color(0.79f, 0.63f, 0.29f), 0.03f, out var staggerFill);
            health.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            stagger.transform.localPosition = new Vector3(0f, 0.01f, 0f);

            var bars = Ensure<ActorStatusBars>(barsRoot);
            SetReference(bars, "_actor", actor);
            SetReference(bars, "_root", barsRoot);
            SetReference(bars, "_healthFill", healthFill.transform);
            SetReference(bars, "_staggerFill", staggerFill.transform);
            SetBool(bars, "_isVisible", !isBoss);
            SetBool(bars, "_showOnlyWhenHurt", true);
            return bars;
        }

        private static GameObject BuildBar(GameObject parent, string name, Color color,
            float thickness, out GameObject fillPivot)
        {
            var bar = FindOrCreateChild(parent, name);
            var background = FindOrCreateChild(bar, "Background");
            // A dark iron sleeve with a thin warm edge, not a grey box.
            background.transform.localScale = new Vector3(1.0f, thickness + 0.035f, 1f);
            var backgroundRenderer = Ensure<SpriteRenderer>(background);
            backgroundRenderer.sprite = _sprite;
            backgroundRenderer.sharedMaterial = _material;
            backgroundRenderer.color = new Color(0.05f, 0.04f, 0.035f, 0.88f);
            SetSorting(backgroundRenderer, "WorldUI", 0);

            var edge = FindOrCreateChild(bar, "Edge");
            edge.transform.localScale = new Vector3(1.03f, thickness + 0.055f, 1f);
            var edgeRenderer = Ensure<SpriteRenderer>(edge);
            edgeRenderer.sprite = _sprite;
            edgeRenderer.sharedMaterial = _material;
            edgeRenderer.color = new Color(0.42f, 0.35f, 0.2f, 0.55f);
            SetSorting(edgeRenderer, "WorldUI", -1);

            fillPivot = FindOrCreateChild(bar, "FillPivot");
            fillPivot.transform.localPosition = new Vector3(-0.47f, 0f, 0f);
            var fill = FindOrCreateChild(fillPivot, "Fill");
            fill.transform.localPosition = new Vector3(0.47f, 0f, 0f);
            fill.transform.localScale = new Vector3(0.94f, thickness, 1f);
            var fillRenderer = Ensure<SpriteRenderer>(fill);
            fillRenderer.sprite = _sprite;
            fillRenderer.sharedMaterial = _material;
            fillRenderer.color = color;
            SetSorting(fillRenderer, "WorldUI", 1);
            return bar;
        }

        private static GameObject BuildBossPrefab()
        {
            var root = CreateActorRoot("GuardianOfTheTomb", EnemyLayer,
                new Color(0.62f, 0.3f, 0.35f), new Vector2(1.4f, 2.6f));
            // Old and warded, but nothing a caster cannot work with: a mage build must stay
            // viable against the boss, not merely possible.
            ApplyResistances(Ensure<Resistances>(root),
                Resist(physical: 0.1f, fire: 0.1f, frost: 0.1f, magic: 0.1f));
            var body = Ensure<Rigidbody2D>(root);
            body.gravityScale = 3.5f;
            body.freezeRotation = true;
            // Physics runs at 50 Hz; without interpolation the body jumps between steps on a
            // 60 or 120 Hz screen while the smoothly interpolated player moves past it.
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.excludeLayers = (1 << PlayerLayer) | (1 << EnemyLayer);

            var capsule = Ensure<CapsuleCollider2D>(root);
            capsule.size = new Vector2(1.4f, 2.6f);
            capsule.sharedMaterial =
                AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(FrictionlessPath);

            var motor = Ensure<EnemyMotor2D>(root);
            SetLayerMask(motor, "_groundMask", 1 << GroundLayer);
            SetReference(motor, "_visual", root.transform.Find("Visual"));
            SetVector(motor, "_groundProbeOffset", new Vector2(0f, -1.35f));
            SetVector(motor, "_ledgeProbeOffset", new Vector2(0.8f, -1.3f));
            SetVector(motor, "_wallProbeOffset", new Vector2(0f, -0.6f));

            SetFloat(Ensure<Health>(root), "_maxHealth", 900f);
            var stagger = Ensure<StaggerMeter>(root);
            SetFloat(stagger, "_threshold", 180f);
            SetFloat(stagger, "_resistance", 0.45f);
            SetFloat(stagger, "_stunDuration", 2f);
            SetFloat(stagger, "_recoveryPerSecond", 22f);
            SetFloat(stagger, "_recoveryDelay", 2.5f);

            Ensure<StatusEffects>(root);
            var actor = Ensure<CombatActor>(root);
            SetFloat(actor, "_knockbackResistance", 0.92f);
            SetFloat(actor, "_knockbackLift", 0f);
            SetFloat(actor, "_hitInvulnerability", 0.04f);
            Ensure<Hurtbox>(root);

            var hitbox = Ensure<Hitbox>(root);
            SetLayerMask(hitbox, "_targetMask", 1 << PlayerLayer);

            var shockwaveObject = FindOrCreateChild(root, "Shockwave");
            shockwaveObject.transform.localPosition = Vector3.zero;
            var shockwaveVisual = FindOrCreateChild(shockwaveObject, "Visual");
            var shockwaveRenderer = Ensure<SpriteRenderer>(shockwaveVisual);
            shockwaveRenderer.sprite = Shape("shape_shockwave");
            shockwaveRenderer.sharedMaterial = _material;
            shockwaveRenderer.color = new Color(1f, 0.7f, 0.3f, 0.8f);
            SetSorting(shockwaveRenderer, "VFXFront", 0);
            var shockwaveHitbox = Ensure<Hitbox>(shockwaveObject);
            SetLayerMask(shockwaveHitbox, "_targetMask", 1 << PlayerLayer);
            var shockwave = Ensure<Shockwave>(shockwaveObject);
            SetReference(shockwave, "_hitbox", shockwaveHitbox);
            SetReference(shockwave, "_visual", shockwaveRenderer);

            // The shield the Guardian throws away when phase two begins.
            var shieldObject = FindOrCreateChild(root, "Shield");
            shieldObject.transform.localPosition = new Vector3(-0.55f, 0f, 0f);
            shieldObject.transform.localScale = new Vector3(0.35f, 1.5f, 1f);
            var shieldRenderer = Ensure<SpriteRenderer>(shieldObject);
            shieldRenderer.sprite = _sprite;
            shieldRenderer.sharedMaterial = _material;
            shieldRenderer.color = new Color(0.72f, 0.68f, 0.5f);
            SetSorting(shieldRenderer, "Characters", 4);

            var boss = Ensure<BossController>(root);
            // The epithets on the title cards (approved with design/ui/boss-title, 2026-09-27).
            SetString(boss, "_epithet", "Той, хто не спить");
            SetReference(boss, "_hitbox", hitbox);
            SetReference(boss, "_shockwave", shockwave);
            SetReference(boss, "_bodyCollider", capsule);
            SetReference(boss, "_shieldVisual", shieldObject);
            SetFloat(boss, "_introDuration", 1.4f);
            SetFloat(boss, "_phaseTransitionTime", 2f);
            SetFloat(boss, "_phaseTwoAttackSpeed", 1.2f);
            SetFloat(boss, "_phaseTwoPauseScale", 0.7f);
            SetFloat(boss, "_phaseTwoMoveScale", 1.15f);
            SetFloat(boss, "_staggerImmunityAfterStun", 1.5f);
            SetFloat(boss, "_parryRecoilTime", 0.7f);
            SetFloat(boss, "_tooCloseRange", 0.9f);
            ApplyBossAttacks(boss);

            BuildStatusBars(root, actor, 1.8f, isBoss: true);
            BuildBossVisual(root, shieldObject);
            BuildStatusVfx(root, 1.5f);
            BuildHitFlash(root);
            BuildStunIndicator(root, GuardianStunHeight);
            StripMissingScripts(root);
            return SavePrefab(root, $"{PrefabFolder}/GuardianOfTheTomb.prefab");
        }

        private static void ApplyBossAttacks(BossController boss)
        {
            var serialized = new SerializedObject(boss);
            var attacks = serialized.FindProperty("_attacks");
            attacks.arraySize = 5;
            // Sword Slash: the bread-and-butter blow, parryable, the first pattern to learn.
            SetBossAttack(attacks.GetArrayElementAtIndex(0), BossAttackKind.SwordSlash, 1, 0,
                0f, 2.6f, 2.4f, 0.22f, BossAttackMovement.None, 0f, false, new[]
                {
                    Swing(22f, 34f, 6f, 0.45f, 0.12f, 0.55f, 1.7f, 1.6f, 1.9f)
                });
            // Shield Bash: phase one only, because phase two throws the shield away.
            SetBossAttack(attacks.GetArrayElementAtIndex(1), BossAttackKind.ShieldBash, 1, 1,
                0f, 2.2f, 3.5f, 0.2f, BossAttackMovement.None, 0f, false, new[]
                {
                    Swing(18f, 55f, 10f, 0.5f, 0.12f, 0.6f, 1.5f, 1.7f, 1.6f)
                });
            // Charge: long telegraph, then a committed straight run that a wall can stop.
            SetBossAttack(attacks.GetArrayElementAtIndex(2), BossAttackKind.Charge, 1, 0,
                3.5f, 11f, 3.5f, 0.3f, BossAttackMovement.Charge, 9.5f, false, new[]
                {
                    Swing(27f, 40f, 9f, 0.75f, 0.16f, 0.7f, 1.4f, 2.0f, 1.9f)
                });
            // Triple Combo: three separate windows, so each one can be read on its own.
            SetBossAttack(attacks.GetArrayElementAtIndex(3), BossAttackKind.TripleCombo, 1, 0,
                0f, 3.0f, 2.5f, 0.2f, BossAttackMovement.None, 0f, false, new[]
                {
                    Swing(15f, 22f, 4f, 0.38f, 0.10f, 0.26f, 1.6f, 1.5f, 1.8f),
                    Swing(18f, 24f, 4f, 0.28f, 0.10f, 0.26f, 1.6f, 1.5f, 1.8f),
                    Swing(25f, 46f, 9f, 0.4f, 0.12f, 0.75f, 1.8f, 1.9f, 2.0f)
                });
            // Jump Slam: phase two signature. Blockable but not parryable, so the answer is a dash.
            SetBossAttack(attacks.GetArrayElementAtIndex(4), BossAttackKind.JumpSlam, 2, 0,
                2.5f, 14f, 5.5f, 0.6f, BossAttackMovement.Leap, 11f, true, new[]
                {
                    Swing(34f, 60f, 8f, 0.6f, 0.14f, 0.9f, 1.6f, 2.2f, 2.2f)
                });
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static float[] Swing(float damage, float stagger, float knockback, float windup,
            float active, float recovery, float hitboxY, float hitboxOffsetX, float hitboxSizeX)
        {
            return new[]
            {
                damage, stagger, knockback, windup, active, recovery, hitboxY, hitboxOffsetX,
                hitboxSizeX
            };
        }

        private static void SetBossAttack(SerializedProperty element, BossAttackKind kind,
            int fromPhase, int toPhase, float minRange, float maxRange, float cooldown,
            float trackingTime, BossAttackMovement movement, float movementSpeed, bool shockwave,
            float[][] swings, bool? parryable = null, StatusType? status = null,
            float statusAmount = 0f, DamageType damageType = DamageType.Physical)
        {
            element.FindPropertyRelative("_kind").enumValueIndex = (int)kind;
            element.FindPropertyRelative("_fromPhase").intValue = fromPhase;
            element.FindPropertyRelative("_toPhase").intValue = toPhase;
            element.FindPropertyRelative("_trackingTime").floatValue = trackingTime;
            element.FindPropertyRelative("_minRange").floatValue = minRange;
            element.FindPropertyRelative("_maxRange").floatValue = maxRange;
            element.FindPropertyRelative("_cooldown").floatValue = cooldown;
            element.FindPropertyRelative("_movement").enumValueIndex = (int)movement;
            element.FindPropertyRelative("_movementSpeed").floatValue = movementSpeed;
            element.FindPropertyRelative("_swingInterval").floatValue = 0.1f;
            element.FindPropertyRelative("_endsWithShockwave").boolValue = shockwave;
            var list = element.FindPropertyRelative("_swings");
            list.arraySize = swings.Length;
            for (var i = 0; i < swings.Length; i++)
            {
                var swing = list.GetArrayElementAtIndex(i);
                var values = swings[i];
                swing.FindPropertyRelative("_displayName").stringValue = $"{kind} {i + 1}";
                swing.FindPropertyRelative("_damage").floatValue = values[0];
                swing.FindPropertyRelative("_staggerDamage").floatValue = values[1];
                // Blocking a boss swing is expensive: its guard damage matches its poise damage.
                swing.FindPropertyRelative("_guardDamage").floatValue = values[1];
                swing.FindPropertyRelative("_isParryable").boolValue = parryable
                    ?? (kind != BossAttackKind.Charge && kind != BossAttackKind.JumpSlam);
                swing.FindPropertyRelative("_damageType").enumValueIndex = (int)damageType;
                var buildup = swing.FindPropertyRelative("_statusBuildup");
                buildup.arraySize = status.HasValue ? 1 : 0;
                if (status.HasValue)
                {
                    var entry = buildup.GetArrayElementAtIndex(0);
                    entry.FindPropertyRelative("type").enumValueIndex = (int)status.Value;
                    entry.FindPropertyRelative("amount").floatValue = statusAmount;
                }
                swing.FindPropertyRelative("_knockback").floatValue = values[2];
                swing.FindPropertyRelative("_windup").floatValue = values[3];
                swing.FindPropertyRelative("_activeTime").floatValue = values[4];
                swing.FindPropertyRelative("_recovery").floatValue = values[5];
                swing.FindPropertyRelative("_hitStop").floatValue = 0.05f;
                swing.FindPropertyRelative("_comboLinkTime").floatValue = 1f;
                swing.FindPropertyRelative("_cancelableAfter").floatValue = 1f;
                swing.FindPropertyRelative("_hitboxOffset").vector2Value =
                    new Vector2(values[7], 0f);
                swing.FindPropertyRelative("_hitboxSize").vector2Value =
                    new Vector2(values[8], values[6]);
            }
        }

        private static GameObject BuildDummyPrefab()
        {
            var root = CreateActorRoot("TrainingDummy", EnemyLayer,
                new Color(0.55f, 0.48f, 0.38f), new Vector2(0.85f, 1.7f));
            var body = Ensure<Rigidbody2D>(root);
            body.gravityScale = 3.5f;
            body.freezeRotation = true;
            // Physics runs at 50 Hz; without interpolation the body jumps between steps on a
            // 60 or 120 Hz screen while the smoothly interpolated player moves past it.
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.excludeLayers = (1 << PlayerLayer) | (1 << EnemyLayer);

            var capsule = Ensure<CapsuleCollider2D>(root);
            capsule.size = new Vector2(0.85f, 1.7f);
            // Ordinary friction, unlike the player: a struck dummy slides a little and then stops
            // instead of sailing out of sword range.
            capsule.sharedMaterial = null;

            SetFloat(Ensure<Health>(root), "_maxHealth", 200f);
            var stagger = Ensure<StaggerMeter>(root);
            SetFloat(stagger, "_threshold", 60f);
            SetFloat(stagger, "_stunDuration", 1f);

            Ensure<StatusEffects>(root);
            var actor = Ensure<CombatActor>(root);
            SetEnum(actor, "_faction", (int)Faction.Neutral);
            SetFloat(actor, "_hitInvulnerability", 0.04f);
            SetFloat(actor, "_knockbackResistance", 0.35f);
            SetFloat(actor, "_knockbackLift", 0.6f);
            Ensure<Hurtbox>(root);

            // A left-anchored bar: scaling the pivot grows the fill from its left edge.
            var bar = FindOrCreateChild(root, "HealthBar");
            bar.transform.localPosition = new Vector3(0f, 1.3f, 0f);
            var barBackground = FindOrCreateChild(bar, "Background");
            barBackground.transform.localScale = new Vector3(1.5f, 0.18f, 1f);
            var backgroundRenderer = Ensure<SpriteRenderer>(barBackground);
            backgroundRenderer.sprite = _sprite;
            backgroundRenderer.sharedMaterial = _material;
            backgroundRenderer.color = new Color(0.08f, 0.07f, 0.07f);
            SetSorting(backgroundRenderer, "WorldUI", 0);

            var fillPivot = FindOrCreateChild(bar, "FillPivot");
            fillPivot.transform.localPosition = new Vector3(-0.7f, 0f, 0f);
            var fill = FindOrCreateChild(fillPivot, "Fill");
            fill.transform.localPosition = new Vector3(0.7f, 0f, 0f);
            fill.transform.localScale = new Vector3(1.4f, 0.12f, 1f);
            var fillRenderer = Ensure<SpriteRenderer>(fill);
            fillRenderer.sprite = _sprite;
            fillRenderer.sharedMaterial = _material;
            fillRenderer.color = new Color(0.72f, 0.2f, 0.22f);
            SetSorting(fillRenderer, "WorldUI", 1);

            DressTrainingTarget(root, PlaceholderDummyArt.Generate().dummy, 1.7f);
            var dummy = Ensure<TrainingDummy>(root);
            SetReference(dummy, "_actor", actor);
            SetReference(dummy, "_body",
                root.transform.Find("Visual/Body").GetComponent<SpriteRenderer>());
            SetReference(dummy, "_healthFill", fillPivot.transform);

            return SavePrefab(root, $"{PrefabFolder}/TrainingDummy.prefab");
        }

        private static GameObject BuildAttackerPrefab()
        {
            var root = CreateActorRoot("TrainingAttacker", EnemyLayer,
                new Color(0.62f, 0.4f, 0.45f), new Vector2(0.9f, 1.8f));
            var body = Ensure<Rigidbody2D>(root);
            body.gravityScale = 3.5f;
            body.freezeRotation = true;
            // Physics runs at 50 Hz; without interpolation the body jumps between steps on a
            // 60 or 120 Hz screen while the smoothly interpolated player moves past it.
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.constraints |= RigidbodyConstraints2D.FreezePositionX;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.excludeLayers = (1 << PlayerLayer) | (1 << EnemyLayer);

            var capsule = Ensure<CapsuleCollider2D>(root);
            capsule.size = new Vector2(0.9f, 1.8f);

            SetFloat(Ensure<Health>(root), "_maxHealth", 400f);
            var stagger = Ensure<StaggerMeter>(root);
            SetFloat(stagger, "_threshold", 70f);
            SetFloat(stagger, "_stunDuration", 1.6f);
            SetFloat(stagger, "_recoveryPerSecond", 20f);
            SetFloat(stagger, "_recoveryDelay", 2f);

            Ensure<StatusEffects>(root);
            var actor = Ensure<CombatActor>(root);
            SetFloat(actor, "_knockbackResistance", 1f);
            Ensure<Hurtbox>(root);

            var hitbox = Ensure<Hitbox>(root);
            SetLayerMask(hitbox, "_targetMask", 1 << PlayerLayer);

            var telegraphObject = FindOrCreateChild(root, "Telegraph");
            telegraphObject.transform.localPosition = new Vector3(0f, 1.45f, 0f);
            telegraphObject.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
            var telegraph = Ensure<SpriteRenderer>(telegraphObject);
            telegraph.sprite = Shape("shape_telegraph_sigil");
            telegraph.sharedMaterial = _material;
            SetSorting(telegraph, "VFXFront", 0);

            DressTrainingTarget(root, PlaceholderDummyArt.Generate().attacker, 1.8f);
            var attacker = Ensure<TrainingAttacker>(root);
            SetReference(attacker, "_actor", actor);
            SetReference(attacker, "_hitbox", hitbox);
            SetReference(attacker, "_visual", root.transform.Find("Visual"));
            SetReference(attacker, "_telegraph", telegraph);
            SetFloat(attacker, "_interval", 2.6f);
            SetFloat(attacker, "_range", 2.6f);
            ApplyTrainingAttacks(attacker);

            return SavePrefab(root, $"{PrefabFolder}/TrainingAttacker.prefab");
        }

        /// <summary>Three tells: yellow is parryable, orange must be blocked, red cannot be.</summary>
        private static void ApplyTrainingAttacks(TrainingAttacker attacker)
        {
            var serialized = new SerializedObject(attacker);
            var attacks = serialized.FindProperty("_attacks");
            attacks.arraySize = 3;
            SetTrainingAttack(attacks.GetArrayElementAtIndex(0), "Normal", 12f, 18f, 14f,
                0.55f, false, true);
            SetTrainingAttack(attacks.GetArrayElementAtIndex(1), "Heavy", 24f, 45f, 42f,
                0.85f, false, false);
            SetTrainingAttack(attacks.GetArrayElementAtIndex(2), "Unblockable", 20f, 30f, 0f,
                0.95f, true, false);

            var colors = serialized.FindProperty("_telegraphColors");
            colors.arraySize = 3;
            colors.GetArrayElementAtIndex(0).colorValue = new Color(1f, 0.85f, 0.3f);
            colors.GetArrayElementAtIndex(1).colorValue = new Color(1f, 0.5f, 0.15f);
            colors.GetArrayElementAtIndex(2).colorValue = new Color(1f, 0.15f, 0.15f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetTrainingAttack(SerializedProperty attack, string name, float damage,
            float stagger, float guardDamage, float windup, bool isUnblockable, bool isParryable)
        {
            attack.FindPropertyRelative("_displayName").stringValue = name;
            attack.FindPropertyRelative("_damage").floatValue = damage;
            attack.FindPropertyRelative("_staggerDamage").floatValue = stagger;
            attack.FindPropertyRelative("_guardDamage").floatValue = guardDamage;
            attack.FindPropertyRelative("_knockback").floatValue = 5f;
            attack.FindPropertyRelative("_hitStop").floatValue = 0.04f;
            attack.FindPropertyRelative("_isUnblockable").boolValue = isUnblockable;
            attack.FindPropertyRelative("_isParryable").boolValue = isParryable;
            attack.FindPropertyRelative("_windup").floatValue = windup;
            attack.FindPropertyRelative("_activeTime").floatValue = 0.12f;
            attack.FindPropertyRelative("_recovery").floatValue = 0.45f;
            attack.FindPropertyRelative("_comboLinkTime").floatValue = 1f;
            attack.FindPropertyRelative("_cancelableAfter").floatValue = 1f;
            attack.FindPropertyRelative("_movementScale").floatValue = 0f;
            attack.FindPropertyRelative("_hitboxOffset").vector2Value = new Vector2(1.1f, 0f);
            attack.FindPropertyRelative("_hitboxSize").vector2Value = new Vector2(1.8f, 1.6f);
        }

        private static GameObject BuildPickupPrefab()
        {
            var root = new GameObject("WorldPickup");
            var visualObject = FindOrCreateChild(root, "Visual");
            visualObject.transform.localScale = new Vector3(0.42f, 0.42f, 1f);
            var visual = Ensure<SpriteRenderer>(visualObject);
            visual.sprite = Shape("shape_pickup_relic");
            visual.sharedMaterial = _material;
            SetSorting(visual, "Characters", 1);
            // A soft halo behind it, so a find on a dark floor is seen from across the room. It
            // bobs with the item (a child of the visual) and costs nothing like a light would.
            var halo = FindOrCreateChild(visualObject, "Halo");
            halo.transform.localScale = new Vector3(1.25f, 1.25f, 1f);
            var haloRenderer = Ensure<SpriteRenderer>(halo);
            haloRenderer.sprite = Tomb("env_tomb_glow_warm");
            haloRenderer.sharedMaterial = _material;
            haloRenderer.color = new Color(1f, 0.92f, 0.75f, 0.38f);
            SetSorting(haloRenderer, "Characters", 0);

            var trigger = Ensure<CircleCollider2D>(root);
            trigger.isTrigger = true;
            trigger.radius = 0.55f;

            var pickup = Ensure<WorldPickup>(root);
            SetReference(pickup, "_visual", visual);

            return SavePrefab(root, PickupPrefabPath);
        }

        /// <summary>
        /// The thing a projectile spell becomes. It is deliberately plain: the spell decides
        /// everything about it at launch, so one prefab serves every projectile in the game.
        /// </summary>
        private static GameObject BuildSpellProjectilePrefab()
        {
            var root = new GameObject("SpellProjectile");
            var visualObject = FindOrCreateChild(root, "Visual");
            visualObject.transform.localScale = new Vector3(0.62f, 0.3f, 1f);
            var visual = Ensure<SpriteRenderer>(visualObject);
            visual.sprite = Shape("shape_projectile_bolt");
            visual.sharedMaterial = _material;
            SetSorting(visual, "Projectiles", 0);

            var trigger = Ensure<CircleCollider2D>(root);
            trigger.isTrigger = true;
            trigger.radius = 0.18f;

            var projectile = Ensure<SpellProjectile>(root);
            SetReference(projectile, "_visual", visual);
            // Its own light, coloured at launch: fire warms the walls it passes, frost chills them.
            var glow = FindOrCreateChild(root, "Glow");
            SetReference(projectile, "_light", AddLight(glow,
                UnityEngine.Rendering.Universal.Light2D.LightType.Point, Color.white, 1.1f, 2.6f, 0.1f, 0.6f));
            SetLayerMask(projectile, "_targetMask", 1 << EnemyLayer);
            SetLayerMask(projectile, "_obstacleMask", 1 << GroundLayer);
            return SavePrefab(root, ProjectilePrefabPath);
        }

        private static GameObject BuildCheckpointPrefab()
        {
            var root = new GameObject("Checkpoint");
            var visual = FindOrCreateChild(root, "Visual");
            visual.transform.localScale = new Vector3(0.5f, 1.6f, 1f);
            var renderer = Ensure<SpriteRenderer>(visual);
            var altar = Tomb("env_tomb_checkpoint_base");
            if (altar != null)
            {
                // The art carries its own size and sits on the floor; the stretched box is gone.
                visual.transform.localScale = Vector3.one;
                visual.transform.localPosition = new Vector3(0f, -0.8f, 0f);
                renderer.sprite = altar;
                renderer.color = Color.white;
            }
            else
            {
                renderer.sprite = _sprite;
                renderer.sharedMaterial = _material;
                renderer.color = new Color(0.35f, 0.35f, 0.4f);
            }

            SetSorting(renderer, "GameplayEnvironment", 2);

            var flameObject = FindOrCreateChild(root, "Flame");
            flameObject.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            flameObject.transform.localScale = Vector3.one;
            var flame = Ensure<SpriteRenderer>(flameObject);
            var coldFlame = Tomb("env_tomb_checkpoint_flame_cold");
            var litFlame = Tomb("env_tomb_checkpoint_flame_lit");
            if (coldFlame != null)
            {
                flame.sprite = coldFlame;
                flame.color = Color.white;
            }
            else
            {
                flameObject.transform.localScale = new Vector3(0.35f, 0.5f, 1f);
                flame.sprite = _sprite;
                flame.sharedMaterial = _material;
                flame.color = Color.gray;
            }

            SetSorting(flame, "VFXFront", 1);

            // The warm halo. It is the only warm light in a cold level, which is the entire
            // message the altar has to carry: this place is safe.
            var glowObject = FindOrCreateChild(root, "Glow");
            glowObject.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            var glow = Ensure<SpriteRenderer>(glowObject);
            glow.sprite = Tomb("env_tomb_glow_warm");
            glow.color = new Color(1f, 0.82f, 0.45f, 0.5f);
            glow.enabled = false;
            SetSorting(glow, "VFXBack", 0);

            // The trigger comes first: Checkpoint requires a Collider2D, which is abstract, so
            // Unity cannot add one for it and the component would silently fail to attach.
            var trigger = Ensure<BoxCollider2D>(root);
            trigger.isTrigger = true;
            trigger.size = new Vector2(2.4f, 2.4f);
            trigger.offset = new Vector2(0f, 0.6f);

            // The flame art now carries its own colour, so the checkpoint's tints step back to
            // near-white and only separate the three states slightly.
            var checkpoint = Ensure<Checkpoint>(root);
            SetColor(checkpoint, "_inactiveColor", new Color(0.72f, 0.74f, 0.8f));
            SetColor(checkpoint, "_activeColor", Color.white);
            SetColor(checkpoint, "_currentColor", new Color(1f, 0.97f, 0.88f));

            var checkpointVisual = Ensure<CheckpointVisual>(root);
            SetReference(checkpointVisual, "_flame", flame);
            SetReference(checkpointVisual, "_glow", glow);
            SetReference(checkpointVisual, "_coldFlame", coldFlame);
            SetReference(checkpointVisual, "_litFlame", litFlame);
            SetReference(checkpoint, "_flame", flame);
            SetReference(checkpointVisual, "_checkpoint", checkpoint);
            return SavePrefab(root, $"{PrefabFolder}/Checkpoint.prefab");
        }

        // ---------- scene ----------

        private static void BuildScene(GameObject playerPrefab, GameObject zombiePrefab,
            GameObject guardPrefab, GameObject bossPrefab, GameObject checkpointPrefab,
            GameObject dummyPrefab, GameObject attackerPrefab, ForestContent forest)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

            // Loaded after the new scene, not before: opening a scene drops the handles taken
            // earlier, and a dead handle is written into the scene as "nothing" without a word.
            var pickupComponent = AssetDatabase.LoadAssetAtPath<WorldPickup>(PickupPrefabPath);
            var items = AssetDatabase.LoadAssetAtPath<ItemDatabase>(ItemDatabasePath);
            var curve = AssetDatabase.LoadAssetAtPath<ProgressionCurve>(ProgressionCurvePath);
            var skillDatabase = AssetDatabase.LoadAssetAtPath<SkillDatabase>(SkillDatabasePath);
            if (skillDatabase == null || !skillDatabase.ValidateTree())
            {
                Debug.LogError("The skill tree is missing or invalid; the scene was not built.");
                return;
            }

            var spellDatabase = AssetDatabase.LoadAssetAtPath<SpellDatabase>(SpellDatabasePath);
            if (spellDatabase == null || !spellDatabase.ValidateSpells())
            {
                Debug.LogError("The spell list is missing or invalid; the scene was not built.");
                return;
            }

            if (curve == null)
            {
                Debug.LogError("The progression curve is missing; the scene would have no levels.");
                return;
            }

            if (pickupComponent == null || items == null)
            {
                Debug.LogError("The pickup prefab or the item database is missing; "
                    + "the scene would be built without loot.");
                return;
            }

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            // The scene had no listener at all, so nothing would have been audible. It belongs on
            // the camera: exactly one, and it moves with the view.
            Ensure<AudioListener>(cameraObject);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6f;
            camera.backgroundColor = new Color(0.07f, 0.07f, 0.1f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            cameraObject.transform.position = new Vector3(0f, 2f, -10f);

            var level = new GameObject("Level").transform;
            // The Tomb and the Forest are built from their texts further down (BuildTomb,
            // BuildForest); the Forest's builder puts the Tomb's far wall, which is its first door.

            var spawn = new GameObject("Spawn");
            spawn.transform.position = TombSpot("start") + new Vector2(0f, 0.85f);

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.position = spawn.transform.position;
            var playerCharacter = player.GetComponent<PlayerCharacter>();

            // The camera owns the shake and the flash, because both are properties of the view
            // rather than of any one attacker.
            var shaker = Ensure<CameraShaker>(cameraObject);
            var flashOverlay = FindOrCreateChild(cameraObject, "ScreenFlash");
            flashOverlay.transform.localPosition = new Vector3(0f, 0f, 1f);
            // Comfortably larger than the view at any supported aspect ratio, so a flash never
            // leaves an uncovered edge.
            flashOverlay.transform.localScale = new Vector3(40f, 24f, 1f);
            var flashRenderer = Ensure<SpriteRenderer>(flashOverlay);
            flashRenderer.sprite = Vfx("vfx_screen_flash");
            flashRenderer.enabled = false;
            SetSorting(flashRenderer, "ScreenUI", 100);
            var screenFlash = Ensure<ScreenFlash>(cameraObject);
            SetReference(screenFlash, "_overlay", flashRenderer);

            var follow = Ensure<CameraFollow2D>(cameraObject);
            SetReference(follow, "_target", player.GetComponent<PlayerMotor2D>());
            SetReference(follow, "_shaker", shaker);

            var systems = new GameObject("Systems");
            systems.AddComponent<GamePause>();
            var progress = systems.AddComponent<GameProgress>();
            SetReference(progress, "_items", items);
            // Loaded here, after the new scene, not taken from the forest bundle: that handle was
            // made before NewScene and would be written as nothing. PHASE 24's zone registry was
            // lost exactly that way.
            var world = AssetDatabase.LoadAssetAtPath<WorldGraph>(WorldGraphPath);
            if (world == null)
            {
                Debug.LogError("The world graph is missing; the scene would have no map.");
                return;
            }

            SetReference(progress, "_world", world);
            var progression = systems.AddComponent<PlayerProgressionService>();
            SetReference(progression, "_progress", progress);
            SetReference(progression, "_curve", curve);
            var skills = systems.AddComponent<PlayerSkills>();
            SetReference(skills, "_progress", progress);
            SetReference(skills, "_progression", progression);
            SetReference(skills, "_database", skillDatabase);
            SetReference(skills, "_combatStatus", player.GetComponent<PlayerCombatStatus>());
            var spellbook = systems.AddComponent<PlayerSpellbook>();
            SetReference(spellbook, "_progress", progress);
            SetReference(spellbook, "_skills", skills);
            SetReference(spellbook, "_database", spellDatabase);
            var enemyRegistry = systems.AddComponent<EnemyRegistry>();
            var loot = systems.AddComponent<LootService>();
            SetReference(loot, "_progress", progress);
            SetReference(loot, "_pickupPrefab", pickupComponent);
            var respawner = systems.AddComponent<PlayerRespawner>();
            SetReference(respawner, "_loot", loot);
            SetReference(respawner, "_player", playerCharacter);
            SetReference(respawner, "_motor", player.GetComponent<PlayerMotor2D>());
            SetReference(respawner, "_combatStatus", player.GetComponent<PlayerCombatStatus>());
            SetReference(respawner, "_defaultSpawn", spawn.transform);
            SetReference(respawner, "_progress", progress);
            SetReference(respawner, "_enemies", enemyRegistry);
            // Under the lowest room of any zone (the Forest's mushroom pool, -46) with room to spare.
            SetFloat(respawner, "_worldFloorY", WorldFloorY);

            // Every debug readout lives on its own object, switched off in a release build. They
            // have to be in the real scene to be useful, but a player must never meet them.
            var devTools = new GameObject("DeveloperTools");
            devTools.transform.SetParent(systems.transform, false);
            SetString(Ensure<DeveloperOnly>(devTools), "_purpose",
                "Debug readouts: F1 combat, F2 animation, F6 enemies, F9 environment, ctrl+F audio.");
            devTools.AddComponent<EnemyAnimationDebugTools>();
            devTools.AddComponent<EnvironmentDebugTools>();
            devTools.AddComponent<FeedbackDebugTools>();

            // One service turns every combat event into something you can see and feel.
            // Audio: the manager owns every voice, the binder turns existing gameplay events into
            // sounds, ambience fills the room and the director tracks music state silently.
            var audio = systems.AddComponent<AudioManager>();
            SetReference(audio, "_library",
                AssetDatabase.LoadAssetAtPath<AudioLibrary>(AudioLibraryPath));
            systems.AddComponent<CombatAudioBinder>();
            systems.AddComponent<AmbienceController>();
            systems.AddComponent<MusicDirector>();

            var feedback = systems.AddComponent<CombatFeedbackService>();
            SetReference(feedback, "_library",
                AssetDatabase.LoadAssetAtPath<FeedbackLibrary>(FeedbackLibraryPath));
            SetReference(feedback, "_shaker", shaker);
            SetReference(feedback, "_flash", screenFlash);
            SetReference(feedback, "_lightSprite", Vfx("vfx_light_pulse"));
            SetReference(feedback, "_effectMaterial", _material);
            var pulseTemplate = FindOrCreateChild(systems, "PulseLightTemplate");
            var pulseLight = AddLight(pulseTemplate, UnityEngine.Rendering.Universal.Light2D.LightType.Point,
                Color.white, 1f, 3f, 0f, 0.6f);
            pulseTemplate.SetActive(false);
            SetReference(feedback, "_lightTemplate", pulseLight);

            // The world's services: where the player is, the map, the altars' menu, fast travel.
            BuildWorldSystems(systems, devTools, player, respawner, progress);

            var debug = devTools.AddComponent<CombatDebugOverlay>();
            SetReference(debug, "_player", playerCharacter);
            SetReference(debug, "_motor", player.GetComponent<PlayerMotor2D>());
            SetReference(debug, "_combat", player.GetComponent<PlayerCombat>());
            SetReference(debug, "_defense", player.GetComponent<PlayerDefense>());
            SetReference(debug, "_respawner", respawner);
            SetReference(debug, "_interactor", player.GetComponentInChildren<PlayerInteractor>());
            SetReference(debug, "_combatStatus", player.GetComponent<PlayerCombatStatus>());
            SetReference(debug, "_progress", progress);
            SetReference(debug, "_loot", loot);
            SetReference(debug, "_equipment", player.GetComponent<PlayerEquipment>());
            SetReference(debug, "_stats", player.GetComponent<PlayerStats>());
            SetReference(debug, "_progression", progression);

            var screen = systems.AddComponent<InventoryScreen>();
            SetReference(screen, "_progress", progress);
            SetReference(screen, "_player", playerCharacter);
            SetReference(screen, "_equipment", player.GetComponent<PlayerEquipment>());
            SetReference(screen, "_input", player.GetComponent<PlayerInputReader>());
            SetReference(screen, "_progression", progression);
            SetReference(screen, "_stats", player.GetComponent<PlayerStats>());
            SetReference(screen, "_skills", skills);
            SetReference(screen, "_activeSkills", player.GetComponent<ActiveSkillController>());
            SetReference(debug, "_skills", skills);
            SetReference(debug, "_activeSkills", player.GetComponent<ActiveSkillController>());
            SetReference(debug, "_buffs", player.GetComponent<TemporaryBuffs>());

            // The player prefab cannot hold references to scene objects, so the parts of the
            // player that need the skill service are connected here.
            SetReference(player.GetComponent<PlayerCharacter>(), "_skills", skills);
            SetReference(player.GetComponent<PlayerStats>(), "_skills", skills);
            SetReference(player.GetComponent<PlayerDefense>(), "_skills", skills);
            SetReference(player.GetComponent<PlayerOffense>(), "_skills", skills);
            SetReference(player.GetComponent<ActiveSkillController>(), "_skills", skills);
            SetReference(player.GetComponent<SpellCaster>(), "_skills", skills);
            SetReference(player.GetComponent<SpellCaster>(), "_spellbook", spellbook);
            SetReference(screen, "_spellbook", spellbook);
            SetReference(screen, "_caster", player.GetComponent<SpellCaster>());
            SetReference(debug, "_spellbook", spellbook);
            SetReference(debug, "_caster", player.GetComponent<SpellCaster>());
            SetReference(debug, "_mana", player.GetComponent<Mana>());
            SetReference(debug, "_screen", screen);

            // The Tomb from its text: stone, altars, finds, enemies, the Guardian and his door.
            BuildTomb(level);

            // Everything past the Guardian's arena. It is built into the same scene and reached on
            // foot through a tunnel, so nothing is loaded and nothing teleports.
            BuildForest(level);
            // Over the capital, up the hill from the forest's edge (item 5 of the 15 zones).
            BuildVillage(level);
            ConfigureZoneAudio(systems);
            // Last, because it lights and grounds what is already placed.
            BuildAtmosphere(level, systems, cameraObject, player);
            ConfigureVillageAtmosphere();
            BuildHud(systems, player, respawner, progress, spellbook);

            EditorSceneManager.MarkSceneDirty(scene);
            EnsureFolder("Assets/Game/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
        }

        private static void PlaceCheckpoint(GameObject prefab, Vector2 position, string id,
            string displayName, PlayerRespawner respawner, GameProgress progress, GameObject player)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = position;
            var checkpoint = instance.GetComponent<Checkpoint>();
            SetReference(checkpoint, "_respawner", respawner);
            SetReference(checkpoint, "_progress", progress);
            SetReference(checkpoint, "_combatStatus", player.GetComponent<PlayerCombatStatus>());
            SetString(checkpoint, "_checkpointId", id);
            SetString(checkpoint, "_displayName", displayName);
        }

        /// <summary>
        /// Writes a resistance profile onto a creature. Values are shares: 0.2 shrugs a fifth
        /// off, −0.25 means it hurts a quarter more.
        /// </summary>
        private static void ApplyResistances(Resistances target, ResistanceProfile source)
        {
            if (target == null || source == null)
            {
                return;
            }

            var serialized = new SerializedObject(target);
            var profile = serialized.FindProperty("_profile");
            profile.FindPropertyRelative("_physical").floatValue = source.Get(DamageType.Physical);
            profile.FindPropertyRelative("_fire").floatValue = source.Get(DamageType.Fire);
            profile.FindPropertyRelative("_frost").floatValue = source.Get(DamageType.Frost);
            profile.FindPropertyRelative("_lightning").floatValue = source.Get(DamageType.Lightning);
            profile.FindPropertyRelative("_dark").floatValue = source.Get(DamageType.Dark);
            profile.FindPropertyRelative("_magic").floatValue = source.Get(DamageType.Magic);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ResistanceProfile Resist(float physical, float fire, float frost,
            float magic)
        {
            var profile = new ResistanceProfile();
            profile.Set(DamageType.Physical, physical);
            profile.Set(DamageType.Fire, fire);
            profile.Set(DamageType.Frost, frost);
            profile.Set(DamageType.Magic, magic);
            return profile;
        }

        private static void PlacePickup(GameObject prefab, Vector2 position, GameProgress progress,
            string itemId, bool isCurrency, int amount, string persistentId, bool autoCollect)
        {
            var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            root.transform.position = position;
            var instance = root.GetComponent<WorldPickup>();
            SetReference(instance, "_progress", progress);
            SetString(instance, "_itemId", itemId);
            SetBool(instance, "_isCurrency", isCurrency);
            SetInt(instance, "_amount", amount);
            SetString(instance, "_persistentId", persistentId);
            SetBool(instance, "_autoCollect", autoCollect);
        }

        private static GameObject SpawnEnemy(GameObject prefab, Vector2 position)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = position;
            return instance;
        }

        /// <summary>Marks something as a tool rather than content; hidden in a release build.</summary>
        private static void MarkDeveloperOnly(GameObject target, string purpose)
        {
            if (target == null)
            {
                return;
            }

            SetString(Ensure<DeveloperOnly>(target), "_purpose", purpose);
        }

        /// <summary>
        /// A solid piece of level: one collider and a tiled stone surface.
        ///
        /// The collider is created from the gameplay size first and the art is fitted to it, never
        /// the other way round. Tiling comes from the renderer rather than from a grid of objects,
        /// so a wall is still one draw call however long it is.
        /// </summary>
        private static GameObject Block(Transform parent, string name, Vector2 center,
            Vector2 size, TombSurface surface = TombSurface.Floor)
        {
            var block = new GameObject(name);
            block.transform.SetParent(parent);
            block.transform.position = center;
            block.layer = GroundLayer;
            var collider = block.AddComponent<BoxCollider2D>();
            collider.size = size;

            var visual = FindOrCreateChild(block, "Visual");
            visual.transform.localScale = Vector3.one;
            var renderer = visual.AddComponent<SpriteRenderer>();
            var tile = TileFor(surface);
            if (tile != null)
            {
                renderer.sprite = tile;
                renderer.drawMode = SpriteDrawMode.Tiled;
                renderer.tileMode = SpriteTileMode.Continuous;
                renderer.size = size;
                renderer.color = Color.white;
            }
            else
            {
                visual.transform.localScale = new Vector3(size.x, size.y, 1f);
                renderer.sprite = _sprite;
                renderer.sharedMaterial = _material;
                renderer.color = new Color(0.22f, 0.22f, 0.27f);
            }

            SetSorting(renderer, "GameplayEnvironment", 0);
            return block;
        }

        /// <summary>Which tile a piece of level is surfaced with; purely visual.</summary>
        private enum TombSurface
        {
            Floor,
            Wall,
            Ceiling,
            Platform,
            BrokenPlatform
        }

        private static System.Collections.Generic.Dictionary<string, Sprite> _shapes;

        /// <summary>
        /// One of the small production shapes (relic, bolt, sigil…), falling back to the old
        /// square only if generation failed — never silently to nothing.
        /// </summary>
        private static Sprite Shape(string name) =>
            _shapes != null && _shapes.TryGetValue(name, out var sprite) && sprite != null
                ? sprite
                : _sprite;

        /// <summary>One tomb texture by name, or null before the art has been generated.</summary>
        private static Sprite Tomb(string name) =>
            _tomb != null && _tomb.TryGetValue(name, out var sprite) ? sprite : null;

        /// <summary>The first frame of one effect, for the single-sprite uses.</summary>
        /// <summary>Under the lowest room of every zone: falling past it is falling out of the world.</summary>
        public const float WorldFloorY = -60f;

        private static Sprite Vfx(string name) =>
            _vfx != null && _vfx.TryGetValue(name, out var frames) && frames.Length > 0
                ? frames[0]
                : null;

        /// <summary>
        /// The streak the blade leaves while it is swinging. It hangs off the weapon mount, so it
        /// follows the weapon and mirrors with the player without knowing anything about either.
        ///
        /// Width comes from the equipped weapon at swing time, which is what makes a greatsword
        /// read heavier than a sword without a second trail existing anywhere.
        /// </summary>
        private static void BuildWeaponTrail(GameObject root, PlayerCombat combat)
        {
            var mount = root.transform.Find("Visual/WeaponMount");
            if (mount == null)
            {
                return;
            }

            var trailObject = FindOrCreateChild(mount.gameObject, "Trail");
            trailObject.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            var trail = Ensure<TrailRenderer>(trailObject);
            trail.time = 0.14f;
            trail.widthMultiplier = 0.16f;
            trail.minVertexDistance = 0.02f;
            trail.autodestruct = false;
            trail.emitting = false;
            trail.numCapVertices = 2;
            trail.alignment = LineAlignment.View;
            trail.sharedMaterial = TrailMaterial();
            trail.sortingLayerName = "VFXFront";
            trail.sortingOrder = 2;
            // Bright at the blade, gone at the tail: a swing that has already passed should not
            // still look dangerous.
            trail.startColor = new Color(1f, 0.97f, 0.9f, 0.75f);
            trail.endColor = new Color(0.8f, 0.85f, 0.95f, 0f);

            var weaponTrail = Ensure<WeaponTrail>(trailObject);
            SetReference(weaponTrail, "_trail", trail);
            SetReference(weaponTrail, "_combat", combat);
        }

        /// <summary>
        /// A shared material for every trail. One material rather than one per weapon, because a
        /// material per instance is the quiet way a 2D game loses its batching.
        /// </summary>
        private static Material TrailMaterial()
        {
            const string path = "Assets/Game/Art/VFX/WeaponTrail.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                return _material;
            }

            var material = new Material(shader) { name = "WeaponTrail" };
            EnsureFolder("Assets/Game/Art/VFX");
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        /// <summary>
        /// Puts the status overlays on a creature. They read <see cref="StatusEffects"/> and change
        /// nothing about it; a creature with no statuses shows nothing and costs nothing.
        /// </summary>
        private const string HitFlashMaterialPath = "Assets/Game/Art/VFX/HitFlash.mat";

        /// <summary>
        /// The flash of a landed hit on an enemy or boss (<see cref="HitFlash"/>): it copies the
        /// shape of the body the animation drives.
        /// </summary>
        // Just over each boss's head: half its body height and a little.
        private const float GuardianStunHeight = 1.75f;
        private const float WardenStunHeight = 1.55f;

        /// <summary>Stars over the head while stunned (<see cref="StunIndicator"/>).</summary>
        private static void BuildStunIndicator(GameObject root, float height)
        {
            var indicator = Ensure<StunIndicator>(root);
            var serialized = new SerializedObject(indicator);
            serialized.FindProperty("_actor").objectReferenceValue = root.GetComponent<CombatActor>();
            serialized.FindProperty("_anchor").objectReferenceValue = root.transform.Find("Visual");
            serialized.FindProperty("_height").floatValue = height;
            serialized.FindProperty("_material").objectReferenceValue = _material;
            var frames = _vfx != null && _vfx.TryGetValue(PlaceholderVfxArt.StunStarsName, out var sprites)
                ? sprites
                : System.Array.Empty<Sprite>();
            var list = serialized.FindProperty("_frames");
            list.arraySize = frames.Length;
            for (var i = 0; i < frames.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildHitFlash(GameObject root)
        {
            var flash = Ensure<HitFlash>(root);
            var serialized = new SerializedObject(flash);
            serialized.FindProperty("_actor").objectReferenceValue = root.GetComponent<CombatActor>();
            serialized.FindProperty("_material").objectReferenceValue = HitFlashMaterial();
            var parts = FlashParts(root);
            var list = serialized.FindProperty("_sources");
            list.arraySize = parts.Count;
            for (var i = 0; i < parts.Count; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = parts[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>The player's blink while untouchable after a hit (<see cref="InvulnerabilityBlink"/>).</summary>
        private static void BuildInvulnerabilityBlink(GameObject root)
        {
            var animation = root.GetComponentInChildren<PlayerAnimationController>(true);
            var renderers = new System.Collections.Generic.List<SpriteRenderer>();
            if (animation != null)
            {
                var data = new SerializedObject(animation);
                foreach (var field in new[] { "_body", "_face" })
                {
                    if (data.FindProperty(field)?.objectReferenceValue is SpriteRenderer renderer)
                    {
                        renderers.Add(renderer);
                    }
                }
            }

            var blink = Ensure<InvulnerabilityBlink>(root);
            var serialized = new SerializedObject(blink);
            serialized.FindProperty("_invulnerability").objectReferenceValue =
                root.GetComponent<Invulnerability>();
            var list = serialized.FindProperty("_renderers");
            list.arraySize = renderers.Count;
            for (var i = 0; i < renderers.Count; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>The sprites an enemy's or a boss's animation draws: its body and its face.</summary>
        private static System.Collections.Generic.List<SpriteRenderer> FlashParts(GameObject root)
        {
            MonoBehaviour animation = root.GetComponentInChildren<EnemyAnimationController>(true);
            if (animation == null)
            {
                animation = root.GetComponentInChildren<BossAnimationController>(true);
            }

            var parts = new System.Collections.Generic.List<SpriteRenderer>();
            if (animation != null)
            {
                var data = new SerializedObject(animation);
                foreach (var field in new[] { "_body", "_face" })
                {
                    if (data.FindProperty(field)?.objectReferenceValue is SpriteRenderer part)
                    {
                        parts.Add(part);
                    }
                }
            }

            if (parts.Count == 0 && root.transform.Find("Visual")?.GetComponentInChildren<SpriteRenderer>(true)
                is SpriteRenderer fallback)
            {
                parts.Add(fallback);
            }

            return parts;
        }

        private static Material HitFlashMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(HitFlashMaterialPath);
            if (existing != null)
            {
                return existing;
            }

            var shader = Shader.Find("ForgottenKingdom/SpriteSilhouette");
            if (shader == null)
            {
                Debug.LogError("The sprite silhouette shader is missing; enemies will not flash.");
                return null;
            }

            var material = new Material(shader) { name = "HitFlash" };
            EnsureFolder("Assets/Game/Art/VFX");
            AssetDatabase.CreateAsset(material, HitFlashMaterialPath);
            return material;
        }

        private static void BuildStatusVfx(GameObject root, float scale)
        {
            if (root.GetComponent<StatusEffects>() == null)
            {
                return;
            }

            var visual = root.transform.Find("Visual");
            var overlays = Ensure<StatusVfx>(root);
            SetReference(overlays, "_statuses", root.GetComponent<StatusEffects>());
            SetReference(overlays, "_anchor", visual != null ? visual : root.transform);
            SetReference(overlays, "_frostSprite", Vfx("vfx_frost_active"));
            SetReference(overlays, "_burnSprite", Vfx("vfx_burn_active"));
            SetReference(overlays, "_poisonSprite", Vfx("vfx_poison_active"));
            SetFloat(overlays, "_scale", scale);
        }

        private static Sprite TileFor(TombSurface surface)
        {
            if (_tomb == null)
            {
                return null;
            }

            var key = surface switch
            {
                TombSurface.Wall => "env_tomb_wall_01",
                TombSurface.Ceiling => "env_tomb_ceiling_01",
                TombSurface.Platform => "env_tomb_platform_01",
                TombSurface.BrokenPlatform => "env_tomb_platform_broken_01",
                _ => "env_tomb_floor_01"
            };
            return _tomb.TryGetValue(key, out var sprite) ? sprite : null;
        }

        private static void SetSorting(SpriteRenderer renderer, string layer, int order)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.sortingLayerName = layer;
            renderer.sortingOrder = order;
        }

        /// <summary>
        /// Places a piece of dressing. Decoration never carries a collider: the brief is explicit
        /// that a decorative ledge must not become something the player can stand on by accident.
        /// </summary>
        private static GameObject Prop(Transform parent, string spriteName, Vector2 position,
            string layer, int order, float scale = 1f, bool flip = false, float alpha = 1f)
        {
            if (_tomb == null || !_tomb.TryGetValue(spriteName, out var sprite) || sprite == null)
            {
                return null;
            }

            var prop = new GameObject(spriteName);
            prop.transform.SetParent(parent);
            prop.transform.position = position;
            prop.transform.localScale = new Vector3(flip ? -scale : scale, scale, 1f);
            var renderer = prop.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(1f, 1f, 1f, alpha);
            SetSorting(renderer, layer, order);
            return prop;
        }

        private static void PlaceTraversalReward(Transform parent, Vector2 position,
            string abilityId, string displayName)
        {
            var root = new GameObject(displayName + " Reward");
            root.transform.SetParent(parent);
            root.transform.position = position;
            var trigger = root.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 0.6f;
            var reward = root.AddComponent<TraversalReward>();
            var data = new SerializedObject(reward);
            data.FindProperty("_abilityId").stringValue = abilityId;
            data.FindProperty("_displayName").stringValue = displayName;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddSceneToBuildSettings(string path)
        {
            var scenes = EditorBuildSettings.scenes;
            foreach (var entry in scenes)
            {
                if (entry.path == path)
                {
                    return;
                }
            }

            var updated = new EditorBuildSettingsScene[scenes.Length + 1];
            scenes.CopyTo(updated, 0);
            updated[scenes.Length] = new EditorBuildSettingsScene(path, true);
            EditorBuildSettings.scenes = updated;
        }

        // ---------- combat feedback ----------

        /// <summary>
        /// Builds one profile per combat moment and the library that indexes them.
        ///
        /// The numbers here are the whole feel of the game in one table, which is exactly why they
        /// live in data: a light hit and a boss slam can be compared by reading two rows instead of
        /// by hunting through combat code. Priority is what decides who owns the screen when
        /// several land together, and it rises from an ordinary hit up to the Guardian dying.
        /// </summary>
        private static FeedbackLibrary BuildFeedback()
        {
            EnsureFolder(FeedbackFolder);
            _vfx = PlaceholderVfxArt.Generate();

            var entries =
                new System.Collections.Generic.List<(FeedbackKind kind, FeedbackProfile profile)>();

            // Physical combat. Light is a tap; heavy is a thud; a crit is sharper rather than
            // bigger, so it never gets mistaken for a boss-level event.
            entries.Add(Feedback(FeedbackKind.LightHit, "vfx_hit_light", hitStop: 0.03f,
                shake: ShakeProfile.Tiny, priority: 10));
            entries.Add(Feedback(FeedbackKind.HeavyHit, "vfx_hit_heavy", hitStop: 0.065f,
                shake: ShakeProfile.Small, shakeScale: 1.1f, verticalBias: 1.3f, priority: 20));
            entries.Add(Feedback(FeedbackKind.CriticalHit, "vfx_hit_critical", hitStop: 0.075f,
                shake: ShakeProfile.Small, shakeScale: 1.2f, flash: 0.12f,
                flashColor: new Color(1f, 0.93f, 0.76f), light: new Color(1f, 0.9f, 0.66f, 0.7f),
                lightRadius: 2.6f, priority: 30));
            entries.Add(Feedback(FeedbackKind.Block, "vfx_block", hitStop: 0.035f,
                shake: ShakeProfile.Tiny, priority: 25));

            // The parry is the single most important moment in the game's combat, and its profile
            // is louder than anything below boss level on purpose.
            entries.Add(Feedback(FeedbackKind.Parry, "vfx_parry", hitStop: 0.09f,
                shake: ShakeProfile.Medium, flash: 0.3f,
                flashColor: new Color(1f, 0.96f, 0.85f), light: new Color(1f, 0.92f, 0.7f, 0.85f),
                lightRadius: 3.4f, priority: 60, sound: "parry"));
            entries.Add(Feedback(FeedbackKind.GuardBreak, "vfx_guard_break", hitStop: 0.1f,
                shake: ShakeProfile.Medium, shakeScale: 1.2f, flash: 0.22f,
                flashColor: new Color(1f, 0.85f, 0.5f), light: new Color(0.94f, 0.71f, 0.26f, 0.8f),
                lightRadius: 3.2f, priority: 65, sound: "guard_break"));
            entries.Add(Feedback(FeedbackKind.StaggerBreak, "vfx_stagger", hitStop: 0.06f,
                shake: ShakeProfile.Small, shakeScale: 1.2f, priority: 40, sound: "stagger"));
            entries.Add(Feedback(FeedbackKind.PlayerHurt, "vfx_hurt_flash", hitStop: 0.02f,
                shake: ShakeProfile.Tiny, shakeScale: 1.4f, priority: 35, facesDirection: false));

            // Magic. Each element keeps its own light colour, which is most of how a player tells
            // at a glance what just hit them.
            entries.Add(Feedback(FeedbackKind.FireImpact, "vfx_fire_impact", hitStop: 0.04f,
                shake: ShakeProfile.Small, light: new Color(1f, 0.55f, 0.2f, 0.8f),
                lightRadius: 3f, priority: 22, sound: "fire"));
            entries.Add(Feedback(FeedbackKind.FrostImpact, "vfx_frost_impact", hitStop: 0.04f,
                shake: ShakeProfile.Small, light: new Color(0.56f, 0.84f, 1f, 0.75f),
                lightRadius: 2.8f, priority: 22, sound: "frost"));
            // Arcane leans blue and Shadow leans magenta. They started only 0.27 apart, which lit
            // the room almost identically — and the one thing these two must never do is look
            // like each other.
            entries.Add(Feedback(FeedbackKind.ArcaneBurst, "vfx_arcane_burst", hitStop: 0.05f,
                shake: ShakeProfile.Medium, light: new Color(0.42f, 0.56f, 1f, 0.85f),
                lightRadius: 4.2f, priority: 45, facesDirection: false, sound: "arcane"));

            // Shadow: violet rim rather than a darker screen, so it stays visible in the tomb.
            entries.Add(Feedback(FeedbackKind.ShadowImpact, "vfx_shadow_impact", hitStop: 0.05f,
                shake: ShakeProfile.Small, shakeScale: 1.1f,
                light: new Color(0.74f, 0.24f, 1f, 0.8f), lightRadius: 3f, priority: 45,
                sound: "shadow"));
            entries.Add(Feedback(FeedbackKind.ShadowDashStart, "vfx_shadow_dash_start",
                shake: ShakeProfile.None, priority: 5, facesDirection: false));
            entries.Add(Feedback(FeedbackKind.ShadowDashEnd, "vfx_shadow_dash_end",
                shake: ShakeProfile.None, priority: 5, facesDirection: false));

            // Statuses. Quiet by rule: they fire often, and a proc that shouted would bury the
            // wind-up the player actually needs to read.
            entries.Add(Feedback(FeedbackKind.BleedProc, "vfx_bleed_proc", hitStop: 0.02f,
                shake: ShakeProfile.Tiny, priority: 15, sound: "bleed"));
            entries.Add(Feedback(FeedbackKind.PoisonProc, "vfx_poison_proc",
                shake: ShakeProfile.None, priority: 12, facesDirection: false, sound: "poison"));
            entries.Add(Feedback(FeedbackKind.BurnProc, "vfx_burn_proc",
                shake: ShakeProfile.None, light: new Color(1f, 0.55f, 0.2f, 0.45f),
                lightRadius: 1.6f, priority: 12, facesDirection: false, sound: "burn"));
            entries.Add(Feedback(FeedbackKind.FrostProc, "vfx_frost_proc",
                shake: ShakeProfile.None, light: new Color(0.56f, 0.84f, 1f, 0.4f),
                lightRadius: 1.6f, priority: 12, facesDirection: false, sound: "frost_proc"));

            // Movement: read as dust and motion, never as magic.
            entries.Add(Feedback(FeedbackKind.Dash, "vfx_dash", shake: ShakeProfile.None,
                priority: 5, layer: "VFXBack", mirrors: true));
            entries.Add(Feedback(FeedbackKind.AirDash, "vfx_air_dash", shake: ShakeProfile.None,
                priority: 5, layer: "VFXBack", mirrors: true));
            // Footwork dust (MovementFeedback): the lowest priority there is, so it never claims
            // the screen from anything, and no sound — the footsteps already have theirs.
            entries.Add(Feedback(FeedbackKind.JumpDust, "vfx_dust_jump", shake: ShakeProfile.None,
                priority: 1, facesDirection: false, layer: "VFXBack"));
            entries.Add(Feedback(FeedbackKind.LandDust, "vfx_dust_land", shake: ShakeProfile.None,
                priority: 1, facesDirection: false, layer: "VFXBack"));
            entries.Add(Feedback(FeedbackKind.RunDust, "vfx_dust_kick", shake: ShakeProfile.None,
                priority: 1, layer: "VFXBack", mirrors: true));
            entries.Add(Feedback(FeedbackKind.WallSlideDust, "vfx_dust_wall", shake: ShakeProfile.None,
                priority: 1, layer: "VFXBack", mirrors: true));
            entries.Add(Feedback(FeedbackKind.WallJump, "vfx_wall_kick", shake: ShakeProfile.None,
                priority: 1, layer: "VFXBack", mirrors: true));
            entries.Add(Feedback(FeedbackKind.AirJump, "vfx_air_jump", shake: ShakeProfile.None,
                priority: 1, facesDirection: false, layer: "VFXBack"));
            // The enemy's wind-up glint: quiet (its sound is the telegraph's own), but lit, so it
            // is seen in the darkest room.
            entries.Add(Feedback(FeedbackKind.EnemyTelegraph, "vfx_enemy_telegraph",
                shake: ShakeProfile.None, light: new Color(1f, 0.82f, 0.45f, 0.45f),
                lightRadius: 1.2f, priority: 2, facesDirection: false));
            // Boss danger and the arena seal. The danger must be seen through anything but a
            // blow landing, and never shakes: the boss has not done anything yet.
            entries.Add(Feedback(FeedbackKind.BossDanger, "vfx_boss_danger", shake: ShakeProfile.None,
                light: new Color(1f, 0.35f, 0.2f, 0.6f), lightRadius: 3f, priority: 30,
                facesDirection: false));
            entries.Add(Feedback(FeedbackKind.ArenaSeal, "vfx_arena_seal", shake: ShakeProfile.Small,
                light: new Color(0.62f, 0.78f, 1f, 0.8f), lightRadius: 4f, priority: 40,
                facesDirection: false));
            entries.Add(Feedback(FeedbackKind.ArenaOpen, "vfx_arena_open", shake: ShakeProfile.Tiny,
                light: new Color(0.62f, 0.78f, 1f, 0.7f), lightRadius: 4f, priority: 40,
                facesDirection: false));
            // Only its frames and light colour are read, by ArenaSealGlow; it is never played.
            entries.Add(Feedback(FeedbackKind.ArenaRunes, "vfx_arena_runes", shake: ShakeProfile.None,
                light: new Color(0.62f, 0.78f, 1f, 0.8f), priority: 0, facesDirection: false));
            // Magic and altars. The fizzle is tinted by the lost spell's colour at play time; it
            // never shakes or freezes — losing a spell is a setback, not a blow.
            entries.Add(Feedback(FeedbackKind.CastFizzle, "vfx_cast_fizzle", shake: ShakeProfile.None,
                priority: 6, facesDirection: false, scale: 0.8f));
            entries.Add(Feedback(FeedbackKind.AltarKindle, "vfx_altar_kindle", shake: ShakeProfile.None,
                light: new Color(1f, 0.74f, 0.4f, 0.9f), lightRadius: 4.5f, priority: 20,
                facesDirection: false, layer: "VFXBack"));
            // The lost gold taken back: the pickup sparkle, larger and warm-lit; tinted gold at play.
            entries.Add(Feedback(FeedbackKind.LostGoldRecovered, "vfx_pickup", shake: ShakeProfile.None,
                light: new Color(1f, 0.82f, 0.4f, 0.9f), lightRadius: 3f, priority: 15,
                facesDirection: false, scale: 1.6f));
            entries.Add(Feedback(FeedbackKind.WallContact, "vfx_wall_contact",
                shake: ShakeProfile.None, priority: 5, layer: "VFXBack"));

            entries.Add(Feedback(FeedbackKind.GroundBreaker, "vfx_ground_breaker", hitStop: 0.08f,
                shake: ShakeProfile.Large, verticalBias: 1.6f,
                light: new Color(0.85f, 0.75f, 0.55f, 0.5f), lightRadius: 3.4f, priority: 55,
                facesDirection: false, sound: "ground_breaker"));
            entries.Add(Feedback(FeedbackKind.DashStrike, "vfx_dash_strike", hitStop: 0.05f,
                shake: ShakeProfile.Small, shakeScale: 1.1f, priority: 50, sound: "dash_strike"));

            // The boss tier. These are the only profiles allowed to take the whole screen.
            entries.Add(Feedback(FeedbackKind.BossCharge, "vfx_guardian_charge",
                shake: ShakeProfile.Small, priority: 50, layer: "VFXBack",
                sound: "boss_charge"));
            entries.Add(Feedback(FeedbackKind.BossSlam, "vfx_guardian_slam", hitStop: 0.09f,
                shake: ShakeProfile.BossLarge, verticalBias: 1.8f, flash: 0.16f,
                flashColor: new Color(1f, 0.9f, 0.72f), light: new Color(1f, 0.86f, 0.6f, 0.7f),
                lightRadius: 5f, priority: 80, facesDirection: false, sound: "boss_slam"));
            entries.Add(Feedback(FeedbackKind.BossPhase, "vfx_boss_phase", hitStop: 0.12f,
                shake: ShakeProfile.Large, flash: 0.32f,
                flashColor: new Color(0.62f, 0.36f, 0.95f), light: new Color(0.61f, 0.3f, 1f, 0.9f),
                lightRadius: 6f, priority: 95, facesDirection: false, sound: "boss_phase"));
            entries.Add(Feedback(FeedbackKind.BossDeath, "vfx_boss_death", hitStop: 0.1f,
                shake: ShakeProfile.BossLarge, flash: 0.28f,
                flashColor: new Color(0.7f, 0.5f, 1f), light: new Color(0.61f, 0.3f, 1f, 0.85f),
                lightRadius: 6f, priority: 100, facesDirection: false, sound: "boss_death"));

            entries.Add(Feedback(FeedbackKind.EnemyDeath, "vfx_enemy_death",
                shake: ShakeProfile.None, priority: 8, facesDirection: false));
            entries.Add(Feedback(FeedbackKind.Pickup, "vfx_pickup", shake: ShakeProfile.None,
                priority: 3, facesDirection: false));
            // The pogo, as Hollow Knight gives it: a spark under the feet, a hair of freeze and a
            // downward kick, whether the blade found an enemy or spikes — a bounce that makes no
            // mark reads as the player floating off by accident.
            entries.Add(Feedback(FeedbackKind.Pogo, "vfx_block", hitStop: 0.035f,
                shake: ShakeProfile.Tiny, verticalBias: 1.8f,
                light: new Color(0.85f, 0.9f, 1f, 0.45f), lightRadius: 1.6f, priority: 15));

            var library = CreateAsset<FeedbackLibrary>(FeedbackLibraryPath);
            var serialized = new SerializedObject(library);
            var list = serialized.FindProperty("_entries");
            list.arraySize = entries.Count;
            for (var i = 0; i < entries.Count; i++)
            {
                var element = list.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("kind").enumValueIndex = (int)entries[i].kind;
                element.FindPropertyRelative("profile").objectReferenceValue = entries[i].profile;
            }

            // The fallback is the ordinary light hit: a moment with no profile of its own still
            // shows something, and something small rather than something alarming.
            serialized.FindProperty("_fallback").objectReferenceValue = entries[0].profile;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(library);
            return library;
        }

        /// <summary>
        /// How hard each moment rumbles the pad, 0–1. Weight follows the screen's own ladder: a tap
        /// for a light hit, a jolt for a parry, the longest shudder for a boss falling. Movement,
        /// dust, statuses and pickups stay still — they fire too often to be felt.
        /// </summary>
        private static readonly System.Collections.Generic.Dictionary<FeedbackKind, float> Haptics =
            new System.Collections.Generic.Dictionary<FeedbackKind, float>
            {
                { FeedbackKind.LightHit, 0.15f }, { FeedbackKind.HeavyHit, 0.3f },
                { FeedbackKind.CriticalHit, 0.35f }, { FeedbackKind.Block, 0.25f },
                { FeedbackKind.Parry, 0.6f }, { FeedbackKind.GuardBreak, 0.6f },
                { FeedbackKind.StaggerBreak, 0.35f }, { FeedbackKind.PlayerHurt, 0.5f },
                { FeedbackKind.FireImpact, 0.25f }, { FeedbackKind.FrostImpact, 0.25f },
                { FeedbackKind.ArcaneBurst, 0.35f }, { FeedbackKind.ShadowImpact, 0.3f },
                { FeedbackKind.GroundBreaker, 0.6f }, { FeedbackKind.DashStrike, 0.3f },
                { FeedbackKind.BossSlam, 0.7f }, { FeedbackKind.BossPhase, 0.6f },
                { FeedbackKind.BossDeath, 0.8f }, { FeedbackKind.EnemyDeath, 0.2f },
                { FeedbackKind.Pogo, 0.15f }, { FeedbackKind.ArenaSeal, 0.2f }
            };

        private static (FeedbackKind, FeedbackProfile) Feedback(FeedbackKind kind, string vfx,
            float hitStop = 0f, ShakeProfile shake = ShakeProfile.Tiny, float shakeScale = 1f,
            float verticalBias = 1f, float flash = 0f, Color flashColor = default,
            Color light = default, float lightRadius = 2f, int priority = 10, float scale = 1f,
            bool facesDirection = true, string layer = "VFXFront", string sound = "",
            bool mirrors = false)
        {
            // Rumble (GAME_FEEL_IDEAS.md, item 6, 2026-10-19): by kind rather than by argument, so
            // the table of strengths reads in one place.
            Haptics.TryGetValue(kind, out var haptic);
            var name = "feedback_" + kind.ToString().ToLowerInvariant();
            var profile = CreateAsset<FeedbackProfile>($"{FeedbackFolder}/{name}.asset");
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("_hitStop").floatValue = hitStop;
            serialized.FindProperty("_shake").enumValueIndex = (int)shake;
            serialized.FindProperty("_shakeScale").floatValue = shakeScale;
            serialized.FindProperty("_shakeVerticalBias").floatValue = verticalBias;
            serialized.FindProperty("_screenFlash").floatValue = flash;
            serialized.FindProperty("_flashColor").colorValue =
                flashColor == default ? Color.white : flashColor;
            serialized.FindProperty("_lightColor").colorValue = light;
            serialized.FindProperty("_lightRadius").floatValue = lightRadius;
            serialized.FindProperty("_lightDuration").floatValue = 0.18f;
            serialized.FindProperty("_priority").intValue = priority;
            serialized.FindProperty("_scale").floatValue = scale;
            serialized.FindProperty("_facesHitDirection").boolValue = facesDirection;
            serialized.FindProperty("_mirrorsToDirection").boolValue = mirrors;
            serialized.FindProperty("_sortingLayer").stringValue = layer;
            serialized.FindProperty("_tint").colorValue = Color.white;
            serialized.FindProperty("_soundEvent").stringValue = sound;
            serialized.FindProperty("_haptic").floatValue = haptic;

            var frames = _vfx != null && _vfx.TryGetValue(vfx, out var sprites)
                ? sprites
                : System.Array.Empty<Sprite>();
            var spec = System.Array.Find(PlaceholderVfxArt.Effects, e => e.Name == vfx);
            WriteClip(serialized.FindProperty("_burst"), vfx,
                spec.Fps > 0f ? spec.Fps : 24f, false, frames);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            return (kind, profile);
        }

        // ---------- environment ----------

        /// <summary>
        /// Dresses the level: parallax behind it, architecture and story props around it, a
        /// foreground in front of it, and the arena at the end of it.
        ///
        /// Nothing here carries a collider or touches a position the gameplay depends on. The
        /// level's shape was decided long before this phase and the art fits itself to that shape,
        /// which is the rule the whole phase is built on.
        /// </summary>
        private static Transform Section(Transform parent, string name)
        {
            var section = new GameObject(name).transform;
            section.SetParent(parent);
            return section;
        }

        /// <summary>
        /// The arena seal. It is deliberately opaque and heavy: a translucent shimmer would invite
        /// the player to try a Shadow Dash through it, and the answer is no.
        /// </summary>
        private static void DressBarrier(GameObject barrier)
        {
            var renderer = barrier.GetComponentInChildren<SpriteRenderer>();
            if (renderer == null)
            {
                return;
            }

            var art = Tomb("env_tomb_boss_barrier");
            if (art == null)
            {
                renderer.color = new Color(0.7f, 0.25f, 0.25f, 0.8f);
                return;
            }

            renderer.sprite = art;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = new Vector2(1f, 7f);
            renderer.color = Color.white;
            SetSorting(renderer, "GameplayEnvironment", 5);
        }

        // ---------- animation ----------

        /// <summary>
        /// Puts the animation controller and the effect sockets on the player.
        ///
        /// The placeholder body was a unit square stretched to the capsule and centred on it. Real
        /// frames are pivoted at the feet, so the renderer moves down to the bottom of the capsule
        /// and drops back to unit scale; otherwise every frame would be both stretched and floating
        /// half a body above the floor.
        /// </summary>
        private static void BuildPlayerVisual(GameObject root)
        {
            var visual = root.transform.Find("Visual");
            if (visual == null)
            {
                Debug.LogError("Player prefab has no Visual root; animation cannot be wired.");
                return;
            }

            var bodyObject = FindOrCreateChild(visual.gameObject, "Body");
            bodyObject.transform.localPosition = new Vector3(0f, -0.8f, 0f);
            bodyObject.transform.localScale = Vector3.one;
            var body = Ensure<SpriteRenderer>(bodyObject);
            // The frames carry their own colour; tinting them would fight the palette.
            body.color = Color.white;
            SetSorting(body, "Characters", 3);
            // Lit, so a candle or a spell lights the side of the figure facing it (normal maps,
            // CharacterNormals); the Characters layer has a fill of its own (SceneCharacterLight).
            body.sharedMaterial = LitSpriteMaterial();

            var faceObject = visual.Find("Face");
            var face = faceObject != null ? faceObject.GetComponent<SpriteRenderer>() : null;
            if (face != null)
            {
                face.enabled = false;
                // Disabled, but still given its layer: a renderer left on Default is a loose end
                // that only shows up the day someone re-enables it.
                SetSorting(face, "Characters", 4);
            }

            var set = AssetDatabase.LoadAssetAtPath<PlayerAnimationSet>(PlayerAnimationSetPath);
            if (set == null)
            {
                Debug.LogError($"Animation set missing at {PlayerAnimationSetPath}.");
            }

            var controller = Ensure<PlayerAnimationController>(root);
            SetReference(controller, "_set", set);
            SetReference(controller, "_body", body);
            SetReference(controller, "_face", face);
            BuildCloakTail(visual.gameObject, controller, body);
            BuildCastCharge(root, bodyObject);
            BuildHeroMotion(root, controller, body);

            BuildVisualSockets(root, visual.gameObject, bodyObject);

            // PHASE 17 put this on the player root, where nothing can switch it off without
            // switching off the player. Moved to a child of its own; the old one has to go or the
            // prefab keeps both and the ungated one still answers the F-keys in a release build.
            var stale = root.GetComponent<AnimationDebugTools>();
            if (stale != null)
            {
                Object.DestroyImmediate(stale, true);
            }

            // On its own object under the player so it can be switched off in a release build
            // without taking any of the player's real components with it.
            var playerTools = FindOrCreateChild(root, "DeveloperTools");
            SetString(Ensure<DeveloperOnly>(playerTools), "_purpose",
                "Animation debug readout: F2 panel, F3 slow motion, [ ] step state; Ctrl+N noclip flight.");
            var animationTools = Ensure<AnimationDebugTools>(playerTools);
            SetReference(animationTools, "_animation", controller);
            Ensure<DeveloperNoclip>(playerTools);
        }

        /// <summary>
        /// The points effects attach to. They live under the visual so they inherit the facing
        /// flip, except the ground socket, which must stay on the floor line whichever way the
        /// character faces.
        /// </summary>
        private static void BuildVisualSockets(GameObject root, GameObject visual,
            GameObject bodyObject)
        {
            var sockets = Ensure<VisualSockets>(visual);

            var bodySocket = FindOrCreateChild(visual, "Socket_Body");
            bodySocket.transform.localPosition = Vector3.zero;

            var ground = FindOrCreateChild(root, "Socket_Ground");
            ground.transform.localPosition = new Vector3(0f, -0.8f, 0f);

            var overhead = FindOrCreateChild(visual, "Socket_Overhead");
            overhead.transform.localPosition = new Vector3(0f, 0.95f, 0f);

            var mount = visual.transform.Find("WeaponMount");
            var castPoint = root.transform.Find("CastPoint");
            SetReference(sockets, "_body", bodySocket.transform);
            SetReference(sockets, "_ground", ground.transform);
            SetReference(sockets, "_overhead", overhead.transform);
            SetReference(sockets, "_weapon", mount != null ? mount : bodyObject.transform);
            SetReference(sockets, "_cast", castPoint != null ? castPoint : bodyObject.transform);
        }

        /// <summary>
        /// The spell gathering in the casting hand (item 6 of the 2026-09-27 list). A child of the
        /// body frame, so it mirrors with the facing; lit by its own small light.
        /// </summary>
        private static void BuildCastCharge(GameObject root, GameObject bodyObject)
        {
            var orb = FindOrCreateChild(bodyObject, "CastOrb");
            orb.transform.localPosition = PlaceholderPlayerArt.CastHand();
            var renderer = Ensure<SpriteRenderer>(orb);
            renderer.sprite = _vfx != null && _vfx.TryGetValue(PlaceholderVfxArt.CastOrbName,
                out var frames) && frames.Length > 0 ? frames[0] : null;
            renderer.sharedMaterial = _material;
            renderer.sortingLayerName = "VFXFront";
            renderer.sortingOrder = 0;
            renderer.enabled = false;
            var light = orb.GetComponent<UnityEngine.Rendering.Universal.Light2D>();
            if (light == null)
            {
                light = AddLight(orb, UnityEngine.Rendering.Universal.Light2D.LightType.Point,
                    Color.white, 0f, 1.6f, 0.1f, 0.6f);
            }

            light.enabled = false;
            var charge = Ensure<CastCharge>(orb);
            SetReference(charge, "_caster", root.GetComponent<SpellCaster>());
            SetReference(charge, "_renderer", renderer);
            SetReference(charge, "_light", light);
        }

        /// <summary>
        /// The live back edge of the cloak (item 2 of the 2026-09-27 list): a short chain drawn
        /// with one strip texture, behind the body, hanging from each frame's anchor.
        /// </summary>
        private static void BuildCloakTail(GameObject visual, PlayerAnimationController controller,
            SpriteRenderer body)
        {
            var tailObject = FindOrCreateChild(visual, "CloakTail");
            tailObject.transform.localPosition = Vector3.zero;
            var line = Ensure<LineRenderer>(tailObject);
            line.useWorldSpace = true;
            line.positionCount = CloakTail.Links + 1;
            line.textureMode = LineTextureMode.Stretch;
            line.alignment = LineAlignment.View;
            line.numCornerVertices = 2;
            line.numCapVertices = 0;
            line.widthMultiplier = 0.19f;
            line.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.8f));
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            // The body's own sprite material, so the cloth is lit by the same lights as the frames.
            line.sharedMaterial = body.sharedMaterial != null ? body.sharedMaterial : _material;
            line.sortingLayerName = "Characters";
            line.sortingOrder = 2;

            var tail = Ensure<CloakTail>(tailObject);
            SetReference(tail, "_animation", controller);
            SetReference(tail, "_body", body);
            SetReference(tail, "_texture", AssetDatabase.LoadAssetAtPath<Texture2D>(
                PlaceholderPlayerArt.CloakTailPath));
        }

        /// <summary>
        /// Builds the player's animation set from the placeholder frames, plus a greatsword set
        /// that overrides the swings. Sprite references cannot go through JSON, so this is the one
        /// area authored field by field.
        /// </summary>
        private static PlayerAnimationSet BuildAnimationSet()
        {
            EnsureFolder(AnimationFolder);
            var frames = PlaceholderPlayerArt.Generate();
            PlaceholderPlayerArt.GenerateCloakTail();
            var set = CreateAsset<PlayerAnimationSet>(PlayerAnimationSetPath);
            var serialized = new SerializedObject(set);
            var clips = serialized.FindProperty("_clips");
            clips.arraySize = PlaceholderPlayerArt.States.Length;
            for (var i = 0; i < PlaceholderPlayerArt.States.Length; i++)
            {
                var spec = PlaceholderPlayerArt.States[i];
                var element = clips.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("state").enumValueIndex = (int)spec.State;
                WriteClip(element.FindPropertyRelative("clip"), spec, frames[spec.State], 0f);
            }

            // Idle doubles as the fallback: if a state ever loses its frames the character stands
            // there rather than disappearing, which is a visible bug instead of an invisible one.
            var idle = System.Array.Find(PlaceholderPlayerArt.States,
                s => s.State == PlayerAnimationState.Idle);
            WriteClip(serialized.FindProperty("_fallback"), idle,
                frames[PlayerAnimationState.Idle], 0f);

            var greatsword = BuildGreatswordAnimationSet();
            var weaponSets = serialized.FindProperty("_weaponSets");
            weaponSets.arraySize = 1;
            weaponSets.GetArrayElementAtIndex(0).objectReferenceValue = greatsword;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(set);
            return set;
        }

        private static WeaponAnimationSet BuildGreatswordAnimationSet()
        {
            var frames = PlaceholderPlayerArt.Generate(
                $"{PlaceholderPlayerArt.ArtFolder}/Greatsword",
                PlaceholderPlayerArt.GreatswordStates, 0.35f);
            var set = CreateAsset<WeaponAnimationSet>(GreatswordAnimationSetPath);
            var serialized = new SerializedObject(set);
            serialized.FindProperty("_weaponType").enumValueIndex = (int)WeaponType.Greatsword;
            foreach (var spec in PlaceholderPlayerArt.GreatswordStates)
            {
                var field = spec.State == PlayerAnimationState.Heavy ? "_heavy"
                    : spec.State == PlayerAnimationState.AttackUp ? "_attackUp"
                    : spec.State == PlayerAnimationState.AttackDown ? "_attackDown"
                    : "_attack" + ((int)spec.State - (int)PlayerAnimationState.Attack1 + 1);
                WriteClip(serialized.FindProperty(field), spec, frames[spec.State], 0.35f);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(set);
            return set;
        }

        private static void WriteClip(SerializedProperty clip, PlaceholderPlayerArt.StateSpec spec,
            Sprite[] sprites, float reachBonus)
        {
            WriteClip(clip, "player_" + spec.State.ToString().ToLowerInvariant(), spec.Fps,
                spec.Loops, sprites, PlaceholderPlayerArt.Anchors(spec, reachBonus));
        }

        private static void WriteClip(SerializedProperty clip, string clipName, float fps,
            bool loops, Sprite[] sprites, Vector3[] anchors = null)
        {
            // Where the live piece hangs on each frame: the hero's cloak edge, an enemy's tail or rags.
            var anchorList = clip.FindPropertyRelative("_anchors");
            anchorList.arraySize = anchors != null ? anchors.Length : 0;
            for (var i = 0; i < anchorList.arraySize; i++)
            {
                anchorList.GetArrayElementAtIndex(i).vector3Value = anchors[i];
            }

            clip.FindPropertyRelative("_clipName").stringValue = clipName;
            clip.FindPropertyRelative("_framesPerSecond").floatValue = fps;
            clip.FindPropertyRelative("_isLooping").boolValue = loops;
            var frames = clip.FindPropertyRelative("_frames");
            frames.arraySize = sprites.Length;
            for (var i = 0; i < sprites.Length; i++)
            {
                frames.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            }
        }

        /// <summary>
        /// Builds the animation sets for the two ordinary enemies and the Guardian. Sprite
        /// references cannot go through JSON, so these are authored field by field like the
        /// player's.
        /// </summary>
        private static void BuildEnemyAnimationSets()
        {
            EnsureFolder(AnimationFolder);
            BuildEnemySet(ZombieAnimationSetPath, "enemy_zombie", false,
                PlaceholderEnemyArt.ZombieStates, PlaceholderEnemyArt.GenerateZombie());
            BuildEnemySet(GuardAnimationSetPath, "enemy_shieldguard", true,
                PlaceholderEnemyArt.GuardStates, PlaceholderEnemyArt.GenerateGuard());
            BuildBossSet();
        }

        private static void BuildEnemySet(string path, string prefix, bool carriesShield,
            PlaceholderEnemyArt.Spec<EnemyAnimationState>[] specs,
            System.Collections.Generic.Dictionary<EnemyAnimationState, Sprite[]> frames)
        {
            var set = CreateAsset<EnemyAnimationSet>(path);
            var serialized = new SerializedObject(set);
            serialized.FindProperty("_carriesShield").boolValue = carriesShield;
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

            // Idle doubles as the fallback: a state that lost its frames leaves the enemy standing
            // rather than invisible, which is a visible bug instead of an invisible one.
            var idle = System.Array.Find(specs, s => s.State == EnemyAnimationState.Idle);
            WriteClip(serialized.FindProperty("_fallback"), $"{prefix}_idle", idle.Fps, true,
                frames[EnemyAnimationState.Idle],
                PlaceholderEnemyArt.AnchorsFor(prefix, EnemyAnimationState.Idle));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(set);
        }

        private static void BuildBossSet()
        {
            var frames = PlaceholderEnemyArt.GenerateBoss();
            var specs = PlaceholderEnemyArt.BossStates;
            var set = CreateAsset<BossAnimationSet>(BossAnimationSetPath);
            var serialized = new SerializedObject(set);
            var clips = serialized.FindProperty("_clips");
            clips.arraySize = specs.Length;
            for (var i = 0; i < specs.Length; i++)
            {
                var spec = specs[i];
                var element = clips.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("state").enumValueIndex = (int)spec.State;
                WriteClip(element.FindPropertyRelative("clip"),
                    $"boss_guardian_{spec.State.ToString().ToLowerInvariant()}", spec.Fps,
                    spec.Loops, frames[spec.State], PlaceholderEnemyArt.AnchorsFor("boss_guardian", spec.State));
            }

            var idle = System.Array.Find(specs, s => s.State == BossAnimationState.IdleP1);
            WriteClip(serialized.FindProperty("_fallback"), "boss_guardian_idlep1", idle.Fps, true,
                frames[BossAnimationState.IdleP1],
                PlaceholderEnemyArt.AnchorsFor("boss_guardian", BossAnimationState.IdleP1));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(set);
        }

        /// <summary>
        /// Puts the animation controller and the effect sockets on an enemy. The placeholder body
        /// was a unit square stretched to the capsule and centred on it; real frames are pivoted at
        /// the feet, so the renderer drops to the bottom of the capsule and back to unit scale.
        /// </summary>
        private static void BuildEnemyVisual(GameObject root, string animationSetPath,
            float capsuleHeight, bool carriesShield)
        {
            var visual = root.transform.Find("Visual");
            if (visual == null)
            {
                Debug.LogError($"{root.name} has no Visual root; animation cannot be wired.");
                return;
            }

            var bodyObject = FindOrCreateChild(visual.gameObject, "Body");
            bodyObject.transform.localPosition = new Vector3(0f, -capsuleHeight * 0.5f, 0f);
            bodyObject.transform.localScale = Vector3.one;
            var body = Ensure<SpriteRenderer>(bodyObject);
            // The frames carry their own colour; tinting them would fight the palette.
            body.color = Color.white;
            SetSorting(body, "Characters", 3);
            // Lit, so a candle or a spell lights the side of the figure facing it (normal maps,
            // CharacterNormals); the Characters layer has a fill of its own (SceneCharacterLight).
            body.sharedMaterial = LitSpriteMaterial();

            var faceObject = visual.Find("Face");
            var face = faceObject != null ? faceObject.GetComponent<SpriteRenderer>() : null;
            if (face != null)
            {
                face.enabled = false;
            }

            var set = AssetDatabase.LoadAssetAtPath<EnemyAnimationSet>(animationSetPath);
            if (set == null)
            {
                Debug.LogError($"Enemy animation set missing at {animationSetPath}.");
            }

            var controller = Ensure<EnemyAnimationController>(root);
            SetReference(controller, "_set", set);
            SetReference(controller, "_body", body);
            SetReference(controller, "_face", face);
            BuildActorSockets(root, visual.gameObject, bodyObject, capsuleHeight,
                carriesShield ? 0.45f : 0f);
            BuildEnemyLife(root, visual.gameObject, controller, body, isBoss: false);
        }

        /// <summary>
        /// The Guardian's visual. Its shield is drawn into the phase-one frames and simply absent
        /// from the phase-two ones, so dropping it is a change of pose rather than a hidden object.
        ///
        /// The old shield rectangle stays in the hierarchy with its renderer off: the boss already
        /// switches it with the phase and restores it on reset, which makes it exactly the right
        /// place to hang a blocked-hit effect — present while there is a shield, gone when there
        /// is not, without a second copy of that rule.
        /// </summary>
        private static void BuildBossVisual(GameObject root, GameObject shieldObject,
            string animationSetPath = null, float capsuleHeight = 2.6f)
        {
            var setPath = animationSetPath ?? BossAnimationSetPath;
            var visual = root.transform.Find("Visual");
            if (visual == null)
            {
                Debug.LogError("Boss prefab has no Visual root; animation cannot be wired.");
                return;
            }

            var bodyObject = FindOrCreateChild(visual.gameObject, "Body");
            bodyObject.transform.localPosition = new Vector3(0f, -capsuleHeight * 0.5f, 0f);
            bodyObject.transform.localScale = Vector3.one;
            var body = Ensure<SpriteRenderer>(bodyObject);
            body.color = Color.white;
            SetSorting(body, "Characters", 3);
            // Lit, so a candle or a spell lights the side of the figure facing it (normal maps,
            // CharacterNormals); the Characters layer has a fill of its own (SceneCharacterLight).
            body.sharedMaterial = LitSpriteMaterial();

            var faceObject = visual.Find("Face");
            var face = faceObject != null ? faceObject.GetComponent<SpriteRenderer>() : null;
            if (face != null)
            {
                face.enabled = false;
            }

            var shieldRenderer = shieldObject != null
                ? shieldObject.GetComponent<SpriteRenderer>()
                : null;
            if (shieldRenderer != null)
            {
                shieldRenderer.enabled = false;
            }

            var set = AssetDatabase.LoadAssetAtPath<BossAnimationSet>(setPath);
            if (set == null)
            {
                Debug.LogError($"Boss animation set missing at {setPath}.");
            }

            var controller = Ensure<BossAnimationController>(root);
            SetReference(controller, "_set", set);
            SetReference(controller, "_body", body);
            SetReference(controller, "_face", face);
            BuildActorSockets(root, visual.gameObject, bodyObject, capsuleHeight, 0f);
            BuildEnemyLife(root, visual.gameObject, controller, body, isBoss: true);
            if (shieldObject != null)
            {
                SetReference(visual.GetComponent<VisualSockets>(), "_shield",
                    shieldObject.transform);
            }
        }

        /// <summary>
        /// The points effects attach to. They live under the visual so they inherit the facing
        /// flip, except the ground socket, which must stay on the floor line whichever way the
        /// actor faces. The shield socket exists only on shield carriers.
        /// </summary>
        private static void BuildActorSockets(GameObject root, GameObject visual,
            GameObject bodyObject, float capsuleHeight, float shieldOffset)
        {
            var sockets = Ensure<VisualSockets>(visual);
            var half = capsuleHeight * 0.5f;

            var bodySocket = FindOrCreateChild(visual, "Socket_Body");
            bodySocket.transform.localPosition = Vector3.zero;

            var ground = FindOrCreateChild(root, "Socket_Ground");
            ground.transform.localPosition = new Vector3(0f, -half, 0f);

            var overhead = FindOrCreateChild(visual, "Socket_Overhead");
            overhead.transform.localPosition = new Vector3(0f, half * 0.6f, 0f);

            var weapon = FindOrCreateChild(visual, "Socket_Weapon");
            weapon.transform.localPosition = new Vector3(half * 0.35f, 0f, 0f);

            var cast = FindOrCreateChild(visual, "Socket_Cast");
            cast.transform.localPosition = new Vector3(half * 0.2f, half * 0.2f, 0f);

            SetReference(sockets, "_body", bodySocket.transform);
            SetReference(sockets, "_ground", ground.transform);
            SetReference(sockets, "_overhead", overhead.transform);
            SetReference(sockets, "_weapon", weapon.transform);
            SetReference(sockets, "_cast", cast.transform);
            if (shieldOffset > 0f)
            {
                var shield = FindOrCreateChild(visual, "Socket_Shield");
                shield.transform.localPosition = new Vector3(shieldOffset, 0f, 0f);
                SetReference(sockets, "_shield", shield.transform);
            }
        }

        // ---------- helpers ----------

        private const string LitSpritePath =
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat";

        /// <summary>The URP 2D sprite material that 2D lights, and their normal maps, reach.</summary>
        public static Material LitSpriteMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(LitSpritePath);
            if (material == null)
            {
                Debug.LogError($"Lit sprite material missing at {LitSpritePath}.");
            }

            return material;
        }

        /// <summary>
        /// A training target's drawn body in place of the coloured block: pivot at the foot of
        /// the post, on the floor under the capsule, rocking when struck.
        /// </summary>
        private static void DressTrainingTarget(GameObject root, Sprite art, float capsuleHeight)
        {
            var visual = root.transform.Find("Visual");
            var body = visual.Find("Body");
            body.localPosition = new Vector3(0f, -capsuleHeight * 0.5f, 0f);
            body.localScale = Vector3.one;
            var renderer = body.GetComponent<SpriteRenderer>();
            renderer.sprite = art;
            renderer.color = Color.white;
            renderer.sharedMaterial = LitSpriteMaterial();
            var face = visual.Find("Face");
            if (face != null)
            {
                Object.DestroyImmediate(face.gameObject);
            }

            var wobble = Ensure<HitWobble>(root);
            SetReference(wobble, "_actor", root.GetComponent<CombatActor>());
            SetReference(wobble, "_target", body);
        }

        private static GameObject CreateActorRoot(string name, int layer, Color color, Vector2 size)
        {
            var root = new GameObject(name) { layer = layer };
            var visual = FindOrCreateChild(root, "Visual");
            var bodyVisual = FindOrCreateChild(visual, "Body");
            bodyVisual.transform.localScale = new Vector3(size.x * 0.9f, size.y * 0.92f, 1f);
            var renderer = bodyVisual.AddComponent<SpriteRenderer>();
            renderer.sprite = _sprite;
            renderer.sharedMaterial = _material;
            renderer.color = color;
            SetSorting(renderer, "Characters", 3);

            var face = FindOrCreateChild(visual, "Face");
            face.transform.localPosition = new Vector3(size.x * 0.28f, size.y * 0.24f, 0f);
            face.transform.localScale = new Vector3(0.18f, 0.13f, 1f);
            var faceRenderer = face.AddComponent<SpriteRenderer>();
            faceRenderer.sprite = _sprite;
            faceRenderer.sharedMaterial = _material;
            faceRenderer.color = new Color(1f, 0.83f, 0.46f);
            SetSorting(faceRenderer, "Characters", 4);
            return root;
        }

        private static GameObject FindOrCreateChild(GameObject parent, string name)
        {
            var existing = parent.transform.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var child = new GameObject(name) { layer = parent.layer };
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        private static T Ensure<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        /// <summary>
        /// A prefab cannot be saved while it carries an unresolved script reference, which happens
        /// when a component's file was renamed between runs.
        /// </summary>
        private static void StripMissingScripts(GameObject root)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
            }
        }

        private static GameObject SavePrefab(GameObject instance, string path)
        {
            EnsureFolder(PrefabFolder);
            var saved = PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
            return saved;
        }

        private static T CreateAsset<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                return existing;
            }

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var leaf = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static void SetReference(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetColor(Object target, string field, Color value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).colorValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetLayerMask(Object target, string field, int mask)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).intValue = mask;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string field, float value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetEnum(Object target, string field, int value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).enumValueIndex = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(Object target, string field, int value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBool(Object target, string field, bool value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(Object target, string field, string value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetVector(Object target, string field, Vector2 value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).vector2Value = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

