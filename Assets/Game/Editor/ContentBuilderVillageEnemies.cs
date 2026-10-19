using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The Fallen Village's own enemies (item 5b of the 15 zones, the user's choices of 2026-10-19),
    /// in place of the stand-ins it was built with: the possessed villager with a pitchfork — a long
    /// poke and a rare lunge with a readable windup, a zombie's strength; the scarecrow asleep on its
    /// pole like the gargoyle on its plinth, a scythe when it wakes and ash ravens out of its straw
    /// (<see cref="EnemySummoner"/>); and the watchdog on a chain (<see cref="EnemyLeash"/>), faster
    /// and harder-leaping than the forest hound but only within its chain.
    ///
    /// Numbers sit between the forest's and the capital's, as the village does on the road. Built
    /// after <see cref="BuildCapitalContent"/>, whose raven the scarecrow lets out.
    /// </summary>
    public static partial class ContentBuilder
    {
        private const string VillageEnemyFolder = DataFolder + "/Village";
        private const string VillagerAnimationSetPath = AnimationFolder + "/VillagerAnimations.asset";
        private const string ScarecrowAnimationSetPath = AnimationFolder + "/ScarecrowAnimations.asset";
        private const string WatchdogAnimationSetPath = AnimationFolder + "/WatchdogAnimations.asset";
        public const string VillagerPrefabName = "PossessedVillager";
        public const string ScarecrowPrefabName = "Scarecrow";
        public const string WatchdogPrefabName = "Watchdog";

        private static void BuildVillageEnemies()
        {
            EnsureFolder(VillageEnemyFolder);
            BuildEnemySet(VillagerAnimationSetPath, "enemy_villager", false,
                PlaceholderEnemyArt.VillagerStates, PlaceholderEnemyArt.GenerateVillager());
            BuildEnemySet(ScarecrowAnimationSetPath, "enemy_scarecrow", false,
                PlaceholderEnemyArt.ScarecrowStates, PlaceholderEnemyArt.GenerateScarecrow());
            BuildEnemySet(WatchdogAnimationSetPath, "enemy_watchdog", false,
                PlaceholderEnemyArt.WatchdogStates, PlaceholderEnemyArt.GenerateWatchdog());

            var villager = VillageDefinition(VillagerPrefabName, @"{
                ""_displayName"": ""Одержимий селянин"", ""_experienceReward"": 45,
                ""_patrolSpeed"": 1.2, ""_chaseSpeed"": 2.6, ""_returnSpeed"": 2.0, ""_patrolRange"": 3.0,
                ""_idleTime"": 1.4, ""_idleTimeVariance"": 0.8,
                ""_detectionRange"": 8.0, ""_loseSightRange"": 12.0,
                ""_verticalDetectionRange"": 3.0, ""_alertTime"": 0.5, ""_searchTime"": 3.0,
                ""_behindDetectionRange"": 1.5, ""_facingDeadZone"": 0.35, ""_aiTickInterval"": 0.08,
                ""_attackRange"": 5.0, ""_verticalAttackRange"": 1.6, ""_attackCooldown"": 1.3,
                ""_facingLockTime"": 0.3, ""_decisionCooldown"": 0.3, ""_guardMovementScale"": 1.0,
                ""_preferredRange"": 2.0, ""_hurtTime"": 0.3, ""_hitReactionCooldown"": 0.5,
                ""_deathTime"": 1.4, ""_maxChaseTime"": 14.0,
                ""_attacks"": [
                    {""_kind"":0,""_cooldown"":1.5,""_minRange"":0,""_maxRange"":2.4,""_weight"":6,
                     ""_facingLockTime"":0.3,
                     ""_attack"":{""_displayName"":""Poke"",""_animationState"":""Attack"",
                       ""_damage"":15,""_damageType"":0,""_staggerDamage"":12,""_guardDamage"":14,
                       ""_knockback"":5,""_hitStop"":0.04,""_isUnblockable"":false,""_isParryable"":true,
                       ""_windup"":0.45,""_activeTime"":0.1,""_recovery"":0.45,
                       ""_comboLinkTime"":0.6,""_cancelableAfter"":0.6,""_movementScale"":0,
                       ""_hitboxOffset"":{""x"":1.35,""y"":0.1},""_hitboxSize"":{""x"":2.0,""y"":0.5}}},
                    {""_kind"":1,""_cooldown"":4.5,""_minRange"":2.6,""_maxRange"":5.0,""_weight"":3,
                     ""_facingLockTime"":0.5,""_lunge"":{""x"":8.0,""y"":1.5},
                     ""_attack"":{""_displayName"":""Lunge"",""_animationState"":""AttackHeavy"",
                       ""_damage"":21,""_damageType"":0,""_staggerDamage"":18,""_guardDamage"":22,
                       ""_knockback"":8,""_hitStop"":0.06,""_isUnblockable"":false,""_isParryable"":true,
                       ""_windup"":0.65,""_activeTime"":0.3,""_recovery"":0.6,
                       ""_comboLinkTime"":1.0,""_cancelableAfter"":1.0,""_movementScale"":0,
                       ""_hitboxOffset"":{""x"":1.3,""y"":0.1},""_hitboxSize"":{""x"":1.9,""y"":0.6}}}
                ]}");
            var scarecrow = VillageDefinition(ScarecrowPrefabName, @"{
                ""_displayName"": ""Опудало"", ""_experienceReward"": 80,
                ""_patrolSpeed"": 0, ""_chaseSpeed"": 2.1, ""_returnSpeed"": 1.8, ""_patrolRange"": 0,
                ""_idleTime"": 1.0, ""_detectionRange"": 7.0, ""_loseSightRange"": 11.0,
                ""_verticalDetectionRange"": 3.5, ""_alertTime"": 0.5, ""_searchTime"": 2.0,
                ""_attackRange"": 2.4, ""_verticalAttackRange"": 1.6, ""_attackCooldown"": 1.5,
                ""_hurtTime"": 0.25, ""_hitReactionCooldown"": 0.7, ""_deathTime"": 1.6,
                ""_maxChaseTime"": 12.0,
                ""_attacks"": [
                    {""_kind"":0,""_cooldown"":1.6,""_minRange"":0,""_maxRange"":2.4,""_weight"":5,
                     ""_facingLockTime"":0.25,
                     ""_attack"":{""_displayName"":""Scythe"",""_animationState"":""Attack"",
                       ""_damage"":17,""_damageType"":0,""_staggerDamage"":12,""_guardDamage"":15,
                       ""_knockback"":5,""_hitStop"":0.05,""_isUnblockable"":false,""_isParryable"":true,
                       ""_windup"":0.5,""_activeTime"":0.12,""_recovery"":0.5,
                       ""_comboLinkTime"":0.6,""_cancelableAfter"":0.6,""_movementScale"":0,
                       ""_hitboxOffset"":{""x"":1.1,""y"":0.2},""_hitboxSize"":{""x"":2.3,""y"":1.2}}},
                    {""_kind"":1,""_cooldown"":4.5,""_minRange"":0,""_maxRange"":2.2,""_weight"":2,
                     ""_facingLockTime"":0.35,
                     ""_attack"":{""_displayName"":""Reap"",""_animationState"":""AttackHeavy"",
                       ""_damage"":25,""_damageType"":0,""_staggerDamage"":26,""_guardDamage"":34,
                       ""_knockback"":10,""_hitStop"":0.07,""_isUnblockable"":false,""_isParryable"":false,
                       ""_windup"":0.8,""_activeTime"":0.14,""_recovery"":0.7,
                       ""_comboLinkTime"":1.0,""_cancelableAfter"":1.0,""_movementScale"":0,
                       ""_hitboxOffset"":{""x"":1.0,""y"":-0.1},""_hitboxSize"":{""x"":2.4,""y"":1.0}}}
                ]}");
            var watchdog = VillageDefinition(WatchdogPrefabName, @"{
                ""_displayName"": ""Сторожовий пес"", ""_experienceReward"": 50,
                ""_patrolSpeed"": 1.8, ""_chaseSpeed"": 6.2, ""_returnSpeed"": 3.0, ""_patrolRange"": 2.0,
                ""_idleTime"": 0.8, ""_idleTimeVariance"": 0.6,
                ""_detectionRange"": 8.5, ""_loseSightRange"": 11.0,
                ""_verticalDetectionRange"": 3.0, ""_alertTime"": 0.2, ""_searchTime"": 2.0,
                ""_behindDetectionRange"": 4.0, ""_facingDeadZone"": 0.3, ""_aiTickInterval"": 0.06,
                ""_attackRange"": 5.0, ""_verticalAttackRange"": 1.8, ""_attackCooldown"": 0.8,
                ""_facingLockTime"": 0.1, ""_decisionCooldown"": 0.2, ""_guardMovementScale"": 1.0,
                ""_preferredRange"": 1.2, ""_hurtTime"": 0.25, ""_hitReactionCooldown"": 0.45,
                ""_deathTime"": 1.3, ""_maxChaseTime"": 30.0,
                ""_attacks"": [
                    {""_kind"":0,""_cooldown"":0.65,""_minRange"":0,""_maxRange"":1.5,""_weight"":3,
                     ""_facingLockTime"":0.1,
                     ""_attack"":{""_displayName"":""Bite"",""_animationState"":""Attack"",
                       ""_damage"":16,""_damageType"":0,""_staggerDamage"":11,""_guardDamage"":11,
                       ""_knockback"":4,""_hitStop"":0.03,""_isUnblockable"":false,""_isParryable"":true,
                       ""_windup"":0.24,""_activeTime"":0.09,""_recovery"":0.26,
                       ""_comboLinkTime"":0.4,""_cancelableAfter"":0.4,""_movementScale"":0,
                       ""_hitboxOffset"":{""x"":0.7,""y"":0.0},""_hitboxSize"":{""x"":1.1,""y"":0.8}}},
                    {""_kind"":1,""_cooldown"":3.2,""_minRange"":2.2,""_maxRange"":5.0,""_weight"":4,
                     ""_facingLockTime"":0.4,""_lunge"":{""x"":10.5,""y"":5.5},
                     ""_attack"":{""_displayName"":""Leap"",""_animationState"":""AttackHeavy"",
                       ""_damage"":23,""_damageType"":0,""_staggerDamage"":20,""_guardDamage"":24,
                       ""_knockback"":9,""_hitStop"":0.06,""_isUnblockable"":false,""_isParryable"":true,
                       ""_windup"":0.48,""_activeTime"":0.3,""_recovery"":0.55,
                       ""_comboLinkTime"":1.2,""_cancelableAfter"":1.2,""_movementScale"":0,
                       ""_hitboxOffset"":{""x"":0.6,""y"":0.0},""_hitboxSize"":{""x"":1.4,""y"":1.1}}}
                ]}");
            AssetDatabase.SaveAssets();

            BuildEnemyPrefab(VillagerPrefabName, villager, false, new Color(0.42f, 0.36f, 0.28f), 85f, 50f,
                Drops("PossessedVillagerDrops", 10, 20, "healing_herb", 0.15f),
                Resist(physical: 0f, fire: -0.1f, frost: 0f, magic: -0.1f),
                VillagerAnimationSetPath, new Vector2(0.85f, 1.7f), staggerResistance: 0f,
                configure: go => SetAudioProfile(go, AudioId.ZombieAlert, AudioId.ZombieHurt,
                    AudioId.ZombieDeath, AudioId.ZombieStep, AudioId.ZombieSwing, AudioId.ZombieSwing,
                    AudioId.None));
            var raven = CapitalEnemy(RavenPrefabName);
            BuildEnemyPrefab(ScarecrowPrefabName, scarecrow, false, new Color(0.48f, 0.4f, 0.24f), 130f, 70f,
                Drops("ScarecrowDrops", 18, 30, "hunter_cloth", 0.35f),
                // Straw: it burns like nothing else in the village, and a blade only cuts stuffing.
                Resist(physical: 0.1f, fire: -0.35f, frost: 0f, magic: 0f),
                ScarecrowAnimationSetPath, new Vector2(0.95f, 1.8f), staggerResistance: 0.15f,
                configure: go =>
                {
                    Ensure<EnemyDormancy>(go);
                    var summoner = Ensure<EnemySummoner>(go);
                    SetReference(summoner, "_summon", raven);
                    SetAudioProfile(go, AudioId.RavenCaw, AudioId.GuardHurt, AudioId.ZombieDeath,
                        AudioId.ZombieStep, AudioId.ZombieSwing, AudioId.GuardHeavy, AudioId.None);
                });
            BuildEnemyPrefab(WatchdogPrefabName, watchdog, false, new Color(0.3f, 0.26f, 0.22f), 80f, 34f,
                Drops("WatchdogDrops", 10, 18, "beast_fang", 0.3f),
                Resist(physical: 0f, fire: -0.1f, frost: -0.1f, magic: 0f),
                WatchdogAnimationSetPath, new Vector2(1.45f, 0.9f), staggerResistance: 0f,
                configure: go =>
                {
                    Ensure<EnemyLeash>(go);
                    SetAudioProfile(go, AudioId.HoundGrowl, AudioId.HoundHurt, AudioId.HoundDeath,
                        AudioId.HoundStep, AudioId.HoundBite, AudioId.HoundLeap, AudioId.None);
                });
            AssetDatabase.SaveAssets();
        }

        private static EnemyDefinition VillageDefinition(string name, string json)
        {
            var definition = CreateAsset<EnemyDefinition>($"{VillageEnemyFolder}/{name}.asset");
            JsonUtility.FromJsonOverwrite(json, definition);
            EditorUtility.SetDirty(definition);
            return definition;
        }
    }
}
