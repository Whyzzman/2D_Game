using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The Ashen Capital's enemies (item 2 of the big list, stage C, the user's choices of
    /// 2026-09-28): the ash raven that dives out of the air, the stone gargoyle that sleeps on its
    /// plinth until someone comes close, the palace priest that mends its allies and steps away,
    /// and the royal crossbowman with its heavy bolts. Each is a definition, a sprite set and a
    /// prefab through the same builder as the forest's; the three behaviours of their own are
    /// components (<see cref="EnemyFlight"/>, <see cref="EnemyDormancy"/>, <see cref="EnemySupport"/>).
    ///
    /// Numbers sit a step above the forest's, as its sit above the Tomb's: the third zone, met with
    /// the second jump and a better blade in hand.
    /// </summary>
    public static partial class ContentBuilder
    {
        private const string CapitalFolder = DataFolder + "/Capital";
        private const string RavenAnimationSetPath = AnimationFolder + "/RavenAnimations.asset";
        private const string GargoyleAnimationSetPath = AnimationFolder + "/GargoyleAnimations.asset";
        private const string PriestAnimationSetPath = AnimationFolder + "/PriestAnimations.asset";
        private const string CrossbowmanAnimationSetPath = AnimationFolder + "/CrossbowmanAnimations.asset";
        private const string BoltPrefabPath = PrefabFolder + "/CrossbowBolt.prefab";
        private const string CinderPrefabPath = PrefabFolder + "/PriestCinder.prefab";
        public const string RavenPrefabName = "AshRaven";
        public const string GargoylePrefabName = "StoneGargoyle";
        public const string PriestPrefabName = "PalacePriest";
        public const string CrossbowmanPrefabName = "RoyalCrossbowman";

        private static void BuildCapitalContent()
        {
            EnsureFolder(CapitalFolder);
            BuildEnemySet(RavenAnimationSetPath, "enemy_raven", false,
                PlaceholderEnemyArt.RavenStates, PlaceholderEnemyArt.GenerateRaven());
            BuildEnemySet(GargoyleAnimationSetPath, "enemy_gargoyle", false,
                PlaceholderEnemyArt.GargoyleStates, PlaceholderEnemyArt.GenerateGargoyle());
            BuildEnemySet(PriestAnimationSetPath, "enemy_priest", false,
                PlaceholderEnemyArt.PriestStates, PlaceholderEnemyArt.GeneratePriest());
            BuildEnemySet(CrossbowmanAnimationSetPath, "enemy_crossbowman", false,
                PlaceholderEnemyArt.CrossbowmanStates, PlaceholderEnemyArt.GenerateCrossbowman());

            var raven = Definition("AshRaven", @"{
                ""_displayName"": ""Попелястий ворон"", ""_experienceReward"": 60,
                ""_patrolSpeed"": 0, ""_chaseSpeed"": 0, ""_returnSpeed"": 0, ""_patrolRange"": 0,
                ""_idleTime"": 1.0, ""_detectionRange"": 9.0, ""_loseSightRange"": 13.0,
                ""_verticalDetectionRange"": 9.0, ""_alertTime"": 0.3, ""_hurtTime"": 0.25,
                ""_hitReactionCooldown"": 0.4, ""_deathTime"": 1.4, ""_attackRange"": 6.0,
                ""_attacks"": [
                    {""_kind"":0,""_cooldown"":1.6,""_minRange"":0,""_maxRange"":8,""_weight"":1,
                     ""_facingLockTime"":0.4,
                     ""_attack"":{""_displayName"":""Dive"",""_animationState"":""Attack"",
                       ""_damage"":16,""_damageType"":0,""_staggerDamage"":8,""_guardDamage"":10,
                       ""_knockback"":5,""_hitStop"":0.04,""_isUnblockable"":false,""_isParryable"":true,
                       ""_windup"":0.55,""_activeTime"":0.5,""_recovery"":0.35,
                       ""_comboLinkTime"":1.0,""_cancelableAfter"":1.0,""_movementScale"":0,
                       ""_hitboxOffset"":{""x"":0.2,""y"":0.35},""_hitboxSize"":{""x"":1.0,""y"":0.8}}}
                ]}");
            var gargoyle = Definition("StoneGargoyle", @"{
                ""_displayName"": ""Кам'яна горгуля"", ""_experienceReward"": 110,
                ""_patrolSpeed"": 0, ""_chaseSpeed"": 2.6, ""_returnSpeed"": 2.0, ""_patrolRange"": 0,
                ""_idleTime"": 1.0, ""_detectionRange"": 7.0, ""_loseSightRange"": 11.0,
                ""_verticalDetectionRange"": 3.5, ""_alertTime"": 0.5, ""_searchTime"": 2.0,
                ""_attackRange"": 1.8, ""_verticalAttackRange"": 1.6, ""_attackCooldown"": 1.4,
                ""_hurtTime"": 0.22, ""_hitReactionCooldown"": 0.9, ""_deathTime"": 1.6,
                ""_maxChaseTime"": 14.0,
                ""_attacks"": [
                    {""_kind"":0,""_cooldown"":1.5,""_minRange"":0,""_maxRange"":1.8,""_weight"":5,
                     ""_facingLockTime"":0.2,
                     ""_attack"":{""_displayName"":""Claw"",""_animationState"":""Attack"",
                       ""_damage"":18,""_damageType"":0,""_staggerDamage"":14,""_guardDamage"":16,
                       ""_knockback"":5,""_hitStop"":0.05,""_isUnblockable"":false,""_isParryable"":true,
                       ""_windup"":0.45,""_activeTime"":0.1,""_recovery"":0.45,
                       ""_comboLinkTime"":0.6,""_cancelableAfter"":0.6,""_movementScale"":0,
                       ""_hitboxOffset"":{""x"":1.0,""y"":0.1},""_hitboxSize"":{""x"":1.5,""y"":1.2}}},
                    {""_kind"":1,""_cooldown"":4.0,""_minRange"":0,""_maxRange"":2.2,""_weight"":3,
                     ""_facingLockTime"":0.3,
                     ""_attack"":{""_displayName"":""Slam"",""_animationState"":""AttackHeavy"",
                       ""_damage"":30,""_damageType"":0,""_staggerDamage"":30,""_guardDamage"":40,
                       ""_knockback"":12,""_hitStop"":0.08,""_isUnblockable"":false,""_isParryable"":false,
                       ""_windup"":0.8,""_activeTime"":0.12,""_recovery"":0.7,
                       ""_comboLinkTime"":1.0,""_cancelableAfter"":1.0,""_movementScale"":0,
                       ""_hitboxOffset"":{""x"":1.1,""y"":-0.2},""_hitboxSize"":{""x"":2.2,""y"":1.0}}}
                ]}");
            var priest = Definition("PalacePriest", @"{
                ""_displayName"": ""Палацовий жрець"", ""_experienceReward"": 90,
                ""_patrolSpeed"": 1.2, ""_chaseSpeed"": 2.4, ""_returnSpeed"": 2.0, ""_patrolRange"": 2.5,
                ""_idleTime"": 1.4, ""_detectionRange"": 10.0, ""_loseSightRange"": 14.0,
                ""_verticalDetectionRange"": 4.5, ""_alertTime"": 0.45, ""_searchTime"": 3.0,
                ""_attackRange"": 9.0, ""_verticalAttackRange"": 4.0, ""_attackCooldown"": 1.6,
                ""_facingLockTime"": 0.5, ""_preferredRange"": 6.5, ""_retreatRange"": 3.5,
                ""_retreatSpeed"": 2.8, ""_hurtTime"": 0.32, ""_hitReactionCooldown"": 0.5,
                ""_deathTime"": 1.5, ""_maxChaseTime"": 16.0,
                ""_attacks"": [
                    {""_kind"":0,""_cooldown"":2.6,""_minRange"":2.5,""_maxRange"":9.0,""_weight"":5,
                     ""_facingLockTime"":0.5,
                     ""_attack"":{""_displayName"":""Cinder"",""_animationState"":""Attack"",
                       ""_damage"":16,""_damageType"":1,""_staggerDamage"":8,""_guardDamage"":12,
                       ""_knockback"":2,""_hitStop"":0.03,""_isUnblockable"":false,""_isParryable"":false,
                       ""_windup"":0.7,""_activeTime"":0.06,""_recovery"":0.55,
                       ""_comboLinkTime"":1.0,""_cancelableAfter"":1.0,""_movementScale"":0,
                       ""_hitboxOffset"":{""x"":0.5,""y"":0.3},""_hitboxSize"":{""x"":0.35,""y"":0.35}}}
                ]}");
            var crossbowman = Definition("RoyalCrossbowman", @"{
                ""_displayName"": ""Королівський арбалетник"", ""_experienceReward"": 85,
                ""_patrolSpeed"": 1.4, ""_chaseSpeed"": 2.8, ""_returnSpeed"": 2.2, ""_patrolRange"": 2.5,
                ""_idleTime"": 1.2, ""_detectionRange"": 13.0, ""_loseSightRange"": 16.0,
                ""_verticalDetectionRange"": 5.0, ""_alertTime"": 0.45, ""_searchTime"": 3.0,
                ""_attackRange"": 12.5, ""_verticalAttackRange"": 4.5, ""_attackCooldown"": 1.8,
                ""_facingLockTime"": 0.6, ""_preferredRange"": 8.0, ""_retreatRange"": 4.0,
                ""_retreatSpeed"": 3.0, ""_hurtTime"": 0.3, ""_hitReactionCooldown"": 0.55,
                ""_deathTime"": 1.5, ""_maxChaseTime"": 16.0,
                ""_attacks"": [
                    {""_kind"":0,""_cooldown"":3.0,""_minRange"":3.5,""_maxRange"":12.5,""_weight"":5,
                     ""_facingLockTime"":0.6,
                     ""_attack"":{""_displayName"":""Bolt"",""_animationState"":""Attack"",
                       ""_damage"":26,""_damageType"":0,""_staggerDamage"":18,""_guardDamage"":22,
                       ""_knockback"":5,""_hitStop"":0.05,""_isUnblockable"":false,""_isParryable"":false,
                       ""_windup"":0.95,""_activeTime"":0.06,""_recovery"":0.8,
                       ""_comboLinkTime"":1.0,""_cancelableAfter"":1.0,""_movementScale"":0,
                       ""_hitboxOffset"":{""x"":0.45,""y"":0.0},""_hitboxSize"":{""x"":0.35,""y"":0.35}}},
                    {""_kind"":2,""_cooldown"":3.5,""_minRange"":0,""_maxRange"":1.9,""_weight"":6,
                     ""_facingLockTime"":0.12,
                     ""_attack"":{""_displayName"":""Stock Blow"",""_animationState"":""ShieldBash"",
                       ""_damage"":12,""_damageType"":0,""_staggerDamage"":16,""_guardDamage"":18,
                       ""_knockback"":11,""_hitStop"":0.05,""_isUnblockable"":false,""_isParryable"":true,
                       ""_windup"":0.26,""_activeTime"":0.09,""_recovery"":0.42,
                       ""_comboLinkTime"":0.6,""_cancelableAfter"":0.6,""_movementScale"":0,
                       ""_hitboxOffset"":{""x"":0.8,""y"":0.0},""_hitboxSize"":{""x"":1.3,""y"":1.2}}}
                ]}");
            AssetDatabase.SaveAssets();

            var bolt = BuildProjectilePrefab("CrossbowBolt", BoltPrefabPath,
                new Vector2(0.6f, 0.12f), new Color(0.6f, 0.58f, 0.55f), 0.14f);
            var cinder = BuildProjectilePrefab("PriestCinder", CinderPrefabPath,
                new Vector2(0.32f, 0.32f), new Color(1f, 0.62f, 0.3f), 0.18f);
            SetAttackSpawn(crossbowman, 0, AttackSpawnKind.Projectile, bolt, new Vector2(0.6f, 0.35f),
                speed: 17f, range: 17f, color: new Color(0.6f, 0.58f, 0.55f), atTarget: false);
            SetAttackSpawn(priest, 0, AttackSpawnKind.Projectile, cinder, new Vector2(0.6f, 0.5f),
                speed: 8f, range: 14f, color: new Color(1f, 0.62f, 0.3f), atTarget: false);

            BuildEnemyPrefab(RavenPrefabName, raven, false, new Color(0.12f, 0.11f, 0.13f), 55f, 24f,
                Drops("AshRavenDrops", 10, 20, "hunter_cloth", 0.2f),
                Resist(physical: -0.1f, fire: -0.2f, frost: 0f, magic: 0f),
                RavenAnimationSetPath, new Vector2(0.9f, 0.6f), staggerResistance: 0f,
                configure: go =>
                {
                    go.GetComponent<Rigidbody2D>().gravityScale = 0f;
                    Ensure<EnemyFlight>(go);
                    SetAudioProfile(go, AudioId.RavenCaw, AudioId.HoundHurt, AudioId.HoundDeath,
                        AudioId.None, AudioId.RavenCaw, AudioId.None, AudioId.None);
                });
            BuildEnemyPrefab(GargoylePrefabName, gargoyle, false, new Color(0.34f, 0.33f, 0.33f), 180f, 90f,
                Drops("StoneGargoyleDrops", 20, 34, "iron_scrap", 0.4f),
                // Stone: a blade glances off, magic and frost find the cracks.
                Resist(physical: 0.35f, fire: 0f, frost: -0.15f, magic: -0.1f),
                GargoyleAnimationSetPath, new Vector2(1.1f, 1.8f), staggerResistance: 0.3f,
                configure: go =>
                {
                    Ensure<EnemyDormancy>(go);
                    SetAudioProfile(go, AudioId.None, AudioId.GuardHurt, AudioId.GuardDeath,
                        AudioId.GuardStep, AudioId.GuardHeavy, AudioId.GuardHeavy, AudioId.None);
                });
            BuildEnemyPrefab(PriestPrefabName, priest, false, new Color(0.56f, 0.53f, 0.49f), 90f, 40f,
                Drops("PalacePriestDrops", 24, 40, "ancient_bark", 0.15f),
                Resist(physical: -0.05f, fire: 0.2f, frost: 0f, magic: 0.25f),
                PriestAnimationSetPath, new Vector2(0.85f, 1.75f), staggerResistance: 0f,
                configure: go =>
                {
                    Ensure<EnemySupport>(go);
                    SetAudioProfile(go, AudioId.ShamanCast, AudioId.ShamanHurt, AudioId.ShamanDeath,
                        AudioId.ShamanStep, AudioId.ShamanOrb, AudioId.None, AudioId.None);
                });
            BuildEnemyPrefab(CrossbowmanPrefabName, crossbowman, false, new Color(0.3f, 0.26f, 0.27f), 110f, 55f,
                Drops("RoyalCrossbowmanDrops", 20, 34, "royal_arrowhead", 0.4f),
                Resist(physical: 0.1f, fire: 0f, frost: 0f, magic: 0f),
                CrossbowmanAnimationSetPath, new Vector2(0.85f, 1.75f), staggerResistance: 0.1f,
                configure: go => SetAudioProfile(go, AudioId.ArcherDraw, AudioId.ArcherHurt,
                    AudioId.ArcherDeath, AudioId.GuardStep, AudioId.ArcherDraw, AudioId.None,
                    AudioId.ArcherRelease));
            AssetDatabase.SaveAssets();
        }

        private static EnemyDefinition Definition(string name, string json)
        {
            var definition = CreateAsset<EnemyDefinition>($"{CapitalFolder}/{name}.asset");
            JsonUtility.FromJsonOverwrite(json, definition);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static DropTable Drops(string name, int minGold, int maxGold, string itemId, float chance)
        {
            return BuildDropTable(name, "{\"_entries\":[{\"_isCurrency\":true,\"_chance\":1,\"_isGuaranteed\":true,"
                + $"\"_minAmount\":{minGold},\"_maxAmount\":{maxGold}}},"
                + $"{{\"_itemId\":\"{itemId}\",\"_chance\":{chance.ToString(System.Globalization.CultureInfo.InvariantCulture)},"
                + "\"_minAmount\":1,\"_maxAmount\":1}]}");
        }

        /// <summary>A capital enemy's prefab by name, for the level; built by <see cref="BuildCapitalContent"/>.</summary>
        private static GameObject CapitalEnemy(string name) =>
            AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/{name}.prefab");
    }
}
