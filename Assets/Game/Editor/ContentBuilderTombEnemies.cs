using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The Tomb's own archer (item 3 of the 15-zone list): a bone bowman beside the zombies and the
    /// shield guards, the first enemy that makes the hero come to it. The forest archer's behaviour
    /// a step weaker, as the Tomb is the zone that teaches: a longer draw, a lighter arrow and no
    /// poison on it. Its arrow is the forest's; built after <see cref="BuildForestContent"/>.
    /// </summary>
    public static partial class ContentBuilder
    {
        private const string TombEnemyFolder = DataFolder + "/Tomb";
        private const string BoneArcherAnimationSetPath = AnimationFolder + "/BoneArcherAnimations.asset";
        public const string BoneArcherPrefabName = "BoneArcher";

        private static void BuildTombEnemies()
        {
            EnsureFolder(TombEnemyFolder);
            BuildEnemySet(BoneArcherAnimationSetPath, "enemy_bone_archer", false,
                PlaceholderEnemyArt.BoneArcherStates, PlaceholderEnemyArt.GenerateBoneArcher());

            var archer = CreateAsset<EnemyDefinition>($"{TombEnemyFolder}/BoneArcher.asset");
            JsonUtility.FromJsonOverwrite(@"{
                ""_displayName"": ""Костяний лучник"", ""_experienceReward"": 30,
                ""_patrolSpeed"": 1.3, ""_chaseSpeed"": 2.4, ""_returnSpeed"": 2.0, ""_patrolRange"": 2.5,
                ""_idleTime"": 1.2, ""_idleTimeVariance"": 0.6,
                ""_detectionRange"": 11.0, ""_loseSightRange"": 14.0,
                ""_verticalDetectionRange"": 5.0, ""_alertTime"": 0.5, ""_searchTime"": 3.0,
                ""_behindDetectionRange"": 2.0, ""_facingDeadZone"": 0.35, ""_aiTickInterval"": 0.08,
                ""_attackRange"": 10.5, ""_verticalAttackRange"": 4.5, ""_attackCooldown"": 1.7,
                ""_facingLockTime"": 0.55, ""_decisionCooldown"": 0.3, ""_guardMovementScale"": 1.0,
                ""_preferredRange"": 7.0, ""_retreatRange"": 4.0, ""_retreatSpeed"": 2.8,
                ""_hurtTime"": 0.34, ""_hitReactionCooldown"": 0.5, ""_deathTime"": 1.5,
                ""_maxChaseTime"": 14.0,
                ""_attacks"": [
                    {""_kind"":0,""_cooldown"":2.6,""_minRange"":3.2,""_maxRange"":10.5,""_weight"":5,
                     ""_facingLockTime"":0.55,
                     ""_attack"":{""_displayName"":""Shoot"",""_animationState"":""Attack"",
                       ""_damage"":13,""_damageType"":0,""_staggerDamage"":10,""_guardDamage"":12,
                       ""_knockback"":3,""_hitStop"":0.04,""_isUnblockable"":false,""_isParryable"":false,
                       ""_windup"":0.78,""_activeTime"":0.06,""_recovery"":0.6,
                       ""_comboLinkTime"":1.0,""_cancelableAfter"":1.0,""_movementScale"":0,
                       ""_hitboxOffset"":{""x"":0.45,""y"":0.0},""_hitboxSize"":{""x"":0.35,""y"":0.35}}},
                    {""_kind"":2,""_cooldown"":3.8,""_minRange"":0,""_maxRange"":1.9,""_weight"":6,
                     ""_facingLockTime"":0.12,
                     ""_attack"":{""_displayName"":""Bow Swipe"",""_animationState"":""ShieldBash"",
                       ""_damage"":7,""_damageType"":0,""_staggerDamage"":12,""_guardDamage"":14,
                       ""_knockback"":10,""_hitStop"":0.05,""_isUnblockable"":false,""_isParryable"":true,
                       ""_windup"":0.3,""_activeTime"":0.09,""_recovery"":0.45,
                       ""_comboLinkTime"":0.6,""_cancelableAfter"":0.6,""_movementScale"":0,
                       ""_hitboxOffset"":{""x"":0.8,""y"":0.0},""_hitboxSize"":{""x"":1.3,""y"":1.2}}}
                ]}", archer);
            EditorUtility.SetDirty(archer);
            AssetDatabase.SaveAssets();

            var arrow = AssetDatabase.LoadAssetAtPath<GameObject>(ArrowPrefabPath);
            SetAttackSpawn(archer, 0, AttackSpawnKind.Projectile, arrow, new Vector2(0.55f, 0.35f),
                speed: 12f, range: 14f, color: new Color(0.86f, 0.84f, 0.76f), atTarget: false);

            BuildEnemyPrefab(BoneArcherPrefabName, archer, false, new Color(0.6f, 0.58f, 0.52f), 60f, 30f,
                BuildDropTable("BoneArcherDrops", @"{""_entries"":[
                    {""_isCurrency"":true,""_chance"":1,""_isGuaranteed"":true,""_minAmount"":8,""_maxAmount"":16},
                    {""_itemId"":""royal_arrowhead"",""_chance"":0.3,""_minAmount"":1,""_maxAmount"":2}]}"),
                // Dry bone: a blade splinters it, fire takes it, frost hardly matters.
                Resist(physical: -0.1f, fire: -0.15f, frost: 0.1f, magic: 0f),
                BoneArcherAnimationSetPath, new Vector2(0.8f, 1.7f), staggerResistance: 0f,
                configure: go => SetAudioProfile(go, AudioId.ArcherDraw, AudioId.ArcherHurt,
                    AudioId.ArcherDeath, AudioId.ArcherStep, AudioId.ArcherDraw, AudioId.None,
                    AudioId.ArcherRelease));
            AssetDatabase.SaveAssets();
        }
    }
}
