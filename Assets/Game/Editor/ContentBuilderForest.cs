using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The Withered Forest: its catalogue, its enemies, its boss and its level.
    ///
    /// It is deliberately a second file rather than a second builder. Everything here goes through
    /// the same helpers the Tomb uses — <c>BuildEnemyPrefab</c>, <c>BuildItem</c>,
    /// <c>BuildDropTable</c>, <c>PlaceCheckpoint</c>, <c>Block</c> — because the whole question this
    /// zone exists to answer is whether the systems built for one level survive a second one. Where
    /// something had to change to make that work, it changed in the shared code and the Tomb got it
    /// too; nothing here is a private copy.
    /// </summary>
    public static partial class ContentBuilder
    {
        private const string ForestFolder = DataFolder + "/Forest";
        private const string HoundAnimationSetPath = AnimationFolder + "/HoundAnimations.asset";
        private const string ArcherAnimationSetPath = AnimationFolder + "/ArcherAnimations.asset";
        private const string ShamanAnimationSetPath = AnimationFolder + "/ShamanAnimations.asset";
        private const string SpearmanAnimationSetPath =
            AnimationFolder + "/SpearmanAnimations.asset";
        private const string WardenAnimationSetPath = AnimationFolder + "/WardenAnimations.asset";
        private const string ArrowPrefabPath = PrefabFolder + "/Arrow.prefab";
        private const string PoisonOrbPrefabPath = PrefabFolder + "/PoisonOrb.prefab";
        private const string RootShotPrefabPath = PrefabFolder + "/RootShot.prefab";
        private const string PoisonPlantPrefabPath = PrefabFolder + "/PoisonPlant.prefab";
        private const string PoisonFieldPrefabPath = PrefabFolder + "/PoisonField.prefab";
        private const string RootEruptionPrefabPath = PrefabFolder + "/RootEruption.prefab";

        public const string TombZoneId = "zone_forgotten_tomb";
        public const string ForestZoneId = "zone_withered_forest";
        public const string WardenBossId = "hollow-warden";

        // ---------- catalogue ----------

        /// <summary>
        /// Materials, weapons, armour and charms found in the forest.
        ///
        /// The two weapons are written as alternatives to the Tomb's pair rather than replacements
        /// for them: the Thornblade hits softer than the Old Sword and bleeds, the Greatblade hits
        /// harder than the Iron Greatsword and asks for more strength and more stamina. Walking out
        /// of this zone should widen the choice, not end it.
        /// </summary>
        private static IEnumerable<ItemDefinition> BuildForestItems()
        {
            var thornblade = BuildThornblade();
            var greatblade = BuildWardenGreatblade();
            var pauldron = BuildArmor("warden_pauldron", EquipmentSlot.Chest, 8f, 7f, 0.05f);
            var venomCharm = BuildAccessory("venom_charm", "Отрута береться повільніше.",
                new[] { new StatModifier(StatType.PoisonResistance, ModifierOperation.Add, 0.35f) });
            var hunterSigil = BuildAccessory("hunter_sigil", "Рука твердіша, ніж була.",
                new[] { new StatModifier(StatType.Dexterity, ModifierOperation.Add, 3f) });

            return new[]
            {
                BuildItem("beast_fang", "Ікло звіра",
                    "Ікло зіпсутого гончака. Надто довге для собаки.",
                    ItemCategory.Material, ItemRarity.Common, 99, false, false, 6,
                    new Color(0.82f, 0.78f, 0.7f)),
                BuildItem("corrupted_hide", "Зіпсута шкура",
                    "Шкіра, крізь яку проросло щось фіолетове.",
                    ItemCategory.Material, ItemRarity.Common, 99, false, false, 10,
                    new Color(0.36f, 0.28f, 0.4f)),
                BuildItem("toxic_root", "Отруйний корінь",
                    "Корінь, що досі сочиться. Пахне мокрою міддю.",
                    ItemCategory.Material, ItemRarity.Uncommon, 99, false, false, 18,
                    new Color(0.45f, 0.72f, 0.35f)),
                BuildItem("royal_arrowhead", "Королівський наконечник",
                    "Наконечник стріли з тавром королівських розвідників.",
                    ItemCategory.Material, ItemRarity.Uncommon, 99, false, false, 14,
                    new Color(0.7f, 0.66f, 0.5f)),
                BuildItem("ancient_bark", "Прадавня кора",
                    "Кора, тверда як камінь. Дерево померло стоячи.",
                    ItemCategory.Material, ItemRarity.Rare, 99, false, false, 32,
                    new Color(0.4f, 0.34f, 0.28f)),
                BuildItem("hunter_cloth", "Сукно мисливця",
                    "Вицвіле королівське сукно. Колись було синім.",
                    ItemCategory.Material, ItemRarity.Common, 99, false, false, 8,
                    new Color(0.42f, 0.46f, 0.52f)),
                BuildItem("warden_sigil", "Печать Вартового",
                    "Печать, вирізана з того, що лишилося від Порожнього Вартового. "
                    + "Тримати її — наче стояти на землі, якої немає.",
                    ItemCategory.Progression, ItemRarity.Legendary, 1, true, true, 0,
                    new Color(0.55f, 0.85f, 0.6f)),
                BuildItem("royal_road_marker", "Дорожній камінь",
                    "Уламок верстового стовпа. «До столиці — вісім ліг». Дороги більше немає.",
                    ItemCategory.Material, ItemRarity.Uncommon, 1, true, true, 0,
                    new Color(0.62f, 0.6f, 0.55f)),
                BuildItem("thornblade", "Терновий Клинок",
                    "Легший за старий меч і злобніший. Рани від нього не закриваються.",
                    ItemCategory.Weapon, ItemRarity.Rare, 1, true, true, 240,
                    new Color(0.62f, 0.78f, 0.6f), thornblade),
                BuildItem("warden_greatblade", "Великий Клинок Вартового",
                    "Завеликий для людини. Вартовий і не був нею.",
                    ItemCategory.Weapon, ItemRarity.Rare, 1, true, true, 320,
                    new Color(0.55f, 0.62f, 0.55f), greatblade),
                BuildItem("warden_pauldron", "Наплічник Вартового",
                    "Наплічник лісової варти, прошитий корінням.",
                    ItemCategory.Armor, ItemRarity.Rare, 1, true, false, 190,
                    new Color(0.42f, 0.5f, 0.42f), pauldron),
                BuildItem("venom_charm", "Оберіг від Отрути",
                    "Висушений корінь на шнурку. Гіркий запах відганяє гірше.",
                    ItemCategory.Accessory, ItemRarity.Uncommon, 1, true, false, 150,
                    new Color(0.5f, 0.78f, 0.4f), venomCharm),
                BuildItem("hunter_sigil", "Знак Мисливця",
                    "Знак королівського розвідника. Носили ті, хто не повернувся.",
                    ItemCategory.Accessory, ItemRarity.Rare, 1, true, false, 210,
                    new Color(0.58f, 0.66f, 0.74f), hunterSigil)
            };
        }

        /// <summary>
        /// A dexterity sword that trades raw damage for bleed. Base 17 against the Old Sword's 20,
        /// so its first hit is worse and its fifth is better; picking it is a decision about how a
        /// fight is going to be won rather than an upgrade.
        /// </summary>
        private static WeaponDefinition BuildThornblade()
        {
            var weapon = CreateAsset<WeaponDefinition>($"{DataFolder}/Thornblade.asset");
            JsonUtility.FromJsonOverwrite(@"{
                ""_itemId"": ""thornblade"",
                ""_displayName"": ""Терновий Клинок"",
                ""_weaponType"": 0,
                ""_baseDamage"": 17,
                ""_strengthScaling"": 0.25,
                ""_dexterityScaling"": 0.75,
                ""_strengthRequirement"": 5,
                ""_dexterityRequirement"": 12,
                ""_attackSpeed"": 1.12,
                ""_staminaMultiplier"": 0.9,
                ""_staggerMultiplier"": 0.8,
                ""_knockbackMultiplier"": 0.85,
                ""_critChance"": 0.05,
                ""_comboResetTime"": 0.5,
                ""_visualColor"": {""r"":0.62,""g"":0.78,""b"":0.6,""a"":1},
                ""_visualSize"": {""x"":0.11,""y"":0.88},
                ""_visualOffset"": {""x"":0.3,""y"":0.15},
                ""_lightCombo"": [
                    {""_displayName"":""Light 1"",""_animationState"":""LightAttack1"",""_damage"":0,
                     ""_damageMultiplier"":0.78,
                     ""_damageType"":0,""_staminaCost"":0,""_staggerDamage"":8,
                     ""_guardDamage"":4,""_knockback"":3,
                     ""_hitStop"":0.03,""_isUnblockable"":false,
                     ""_windup"":0.09,""_activeTime"":0.07,""_recovery"":0.17,
                     ""_comboLinkTime"":0.16,""_cancelableAfter"":0.15,
                     ""_hitboxOffset"":{""x"":0.85,""y"":0.05},""_hitboxSize"":{""x"":1.3,""y"":1.2},
                     ""_movementScale"":0.35,
                     ""_statusBuildup"":[{""type"":0,""amount"":14}]},
                    {""_displayName"":""Light 2"",""_animationState"":""LightAttack2"",""_damage"":0,
                     ""_damageMultiplier"":0.92,
                     ""_damageType"":0,""_staminaCost"":0,""_staggerDamage"":10,
                     ""_guardDamage"":6,""_knockback"":4,
                     ""_hitStop"":0.04,""_isUnblockable"":false,
                     ""_windup"":0.08,""_activeTime"":0.07,""_recovery"":0.19,
                     ""_comboLinkTime"":0.15,""_cancelableAfter"":0.15,
                     ""_hitboxOffset"":{""x"":0.9,""y"":0.05},""_hitboxSize"":{""x"":1.45,""y"":1.25},
                     ""_movementScale"":0.25,
                     ""_statusBuildup"":[{""type"":0,""amount"":14}]},
                    {""_displayName"":""Light 3"",""_animationState"":""LightAttack3"",""_damage"":0,
                     ""_damageMultiplier"":1.2,
                     ""_damageType"":0,""_staminaCost"":0,""_staggerDamage"":18,
                     ""_guardDamage"":12,""_knockback"":6,
                     ""_hitStop"":0.06,""_isUnblockable"":false,
                     ""_windup"":0.13,""_activeTime"":0.09,""_recovery"":0.30,
                     ""_comboLinkTime"":0.42,""_cancelableAfter"":0.24,
                     ""_hitboxOffset"":{""x"":1.0,""y"":0.05},""_hitboxSize"":{""x"":1.7,""y"":1.35},
                     ""_movementScale"":0.15,
                     ""_statusBuildup"":[{""type"":0,""amount"":20}]}
                ],
                ""_heavyAttack"": {""_displayName"":""Heavy"",""_animationState"":""HeavyAttack"",
                     ""_damage"":0,""_damageMultiplier"":1.6,
                     ""_damageType"":0,""_staminaCost"":22,""_staggerDamage"":34,
                     ""_guardDamage"":34,""_knockback"":8,""_hitStop"":0.08,""_isUnblockable"":false,
                     ""_windup"":0.29,""_activeTime"":0.11,""_recovery"":0.41,
                     ""_comboLinkTime"":0.76,""_cancelableAfter"":0.42,
                     ""_hitboxOffset"":{""x"":1.05,""y"":0.05},""_hitboxSize"":{""x"":1.95,""y"":1.5},
                     ""_movementScale"":0.05,
                     ""_statusBuildup"":[{""type"":0,""amount"":30}]}
            }", weapon);
            EditorUtility.SetDirty(weapon);
            return weapon;
        }

        /// <summary>
        /// The heavy option. Above the Iron Greatsword on every axis that matters and above it on
        /// the requirements too: 14 strength is a real investment, not a rounding error.
        /// </summary>
        private static WeaponDefinition BuildWardenGreatblade()
        {
            var weapon = CreateAsset<WeaponDefinition>($"{DataFolder}/WardenGreatblade.asset");
            JsonUtility.FromJsonOverwrite(@"{
                ""_itemId"": ""warden_greatblade"",
                ""_displayName"": ""Великий Клинок Вартового"",
                ""_weaponType"": 1,
                ""_baseDamage"": 42,
                ""_strengthScaling"": 1.0,
                ""_dexterityScaling"": 0.1,
                ""_strengthRequirement"": 14,
                ""_dexterityRequirement"": 4,
                ""_attackSpeed"": 0.72,
                ""_staminaMultiplier"": 1.8,
                ""_staggerMultiplier"": 2.0,
                ""_knockbackMultiplier"": 1.6,
                ""_comboResetTime"": 0.9,
                ""_visualColor"": {""r"":0.55,""g"":0.62,""b"":0.55,""a"":1},
                ""_visualSize"": {""x"":0.23,""y"":1.45},
                ""_visualOffset"": {""x"":0.38,""y"":0.2},
                ""_lightCombo"": [
                    {""_displayName"":""Light 1"",""_animationState"":""LightAttack1"",""_damage"":0,
                     ""_damageMultiplier"":0.8,
                     ""_damageType"":0,""_staminaCost"":0,""_staggerDamage"":26,
                     ""_guardDamage"":26,""_knockback"":7,
                     ""_hitStop"":0.06,""_isUnblockable"":false,
                     ""_windup"":0.24,""_activeTime"":0.10,""_recovery"":0.34,
                     ""_comboLinkTime"":0.40,""_cancelableAfter"":0.36,
                     ""_hitboxOffset"":{""x"":1.05,""y"":0.05},""_hitboxSize"":{""x"":2.0,""y"":1.5},
                     ""_movementScale"":0.12},
                    {""_displayName"":""Light 2"",""_animationState"":""LightAttack2"",""_damage"":0,
                     ""_damageMultiplier"":1.05,
                     ""_damageType"":0,""_staminaCost"":0,""_staggerDamage"":32,
                     ""_guardDamage"":32,""_knockback"":9,
                     ""_hitStop"":0.07,""_isUnblockable"":false,
                     ""_windup"":0.28,""_activeTime"":0.11,""_recovery"":0.40,
                     ""_comboLinkTime"":0.62,""_cancelableAfter"":0.44,
                     ""_hitboxOffset"":{""x"":1.15,""y"":0.05},""_hitboxSize"":{""x"":2.2,""y"":1.6},
                     ""_movementScale"":0.08}
                ],
                ""_heavyAttack"": {""_displayName"":""Heavy"",""_animationState"":""HeavyAttack"",
                     ""_damage"":0,""_damageMultiplier"":1.95,
                     ""_damageType"":0,""_staminaCost"":34,""_staggerDamage"":68,
                     ""_guardDamage"":68,""_knockback"":13,""_hitStop"":0.10,
                     ""_isUnblockable"":false,
                     ""_windup"":0.46,""_activeTime"":0.13,""_recovery"":0.58,
                     ""_comboLinkTime"":1.1,""_cancelableAfter"":0.62,
                     ""_hitboxOffset"":{""x"":1.25,""y"":0.05},""_hitboxSize"":{""x"":2.5,""y"":1.8},
                     ""_movementScale"":0.03}
            }", weapon);
            EditorUtility.SetDirty(weapon);
            return weapon;
        }

        // ---------- enemies ----------

        /// <summary>
        /// Fast, fragile, and a punishment for standing still. The hound has no guard and very
        /// little poise, so a heavy swing folds it — which is the trade for how quickly it closes.
        /// Its leap is the move worth learning: a long crouch, then a committed jump that a dash
        /// beats and a block survives.
        /// </summary>
        private static EnemyDefinition BuildHoundDefinition()
        {
            var enemy = CreateAsset<EnemyDefinition>($"{ForestFolder}/CorruptedHound.asset");
            JsonUtility.FromJsonOverwrite(@"{
                ""_displayName"": ""Зіпсутий Гончак"",
                ""_experienceReward"": 40,
                ""_patrolSpeed"": 2.2, ""_chaseSpeed"": 5.4, ""_returnSpeed"": 3.0,
                ""_patrolRange"": 5.5, ""_idleTime"": 0.6, ""_idleTimeVariance"": 0.7,
                ""_detectionRange"": 9.5, ""_loseSightRange"": 13.0,
                ""_verticalDetectionRange"": 3.0, ""_alertTime"": 0.25,
                ""_searchTime"": 2.5, ""_behindDetectionRange"": 3.5,
                ""_facingDeadZone"": 0.3, ""_aiTickInterval"": 0.07,
                ""_attackRange"": 4.8, ""_verticalAttackRange"": 1.8, ""_attackCooldown"": 0.9,
                ""_facingLockTime"": 0.1, ""_decisionCooldown"": 0.2,
                ""_guardMovementScale"": 1.0,
                ""_preferredRange"": 1.2, ""_retreatRange"": 0.0, ""_retreatSpeed"": 0.0,
                ""_hurtTime"": 0.25, ""_hitReactionCooldown"": 0.45, ""_deathTime"": 1.3,
                ""_maxChaseTime"": 14.0,
                ""_attacks"": [
                    {""_kind"":0,""_cooldown"":0.7,""_minRange"":0,""_maxRange"":1.5,""_weight"":3,
                     ""_facingLockTime"":0.1,
                     ""_attack"":{""_displayName"":""Bite"",""_animationState"":""Attack"",
                       ""_damage"":14,""_damageType"":0,
                       ""_staggerDamage"":10,""_guardDamage"":10,""_knockback"":4,""_hitStop"":0.03,
                       ""_isUnblockable"":false,""_isParryable"":true,
                       ""_windup"":0.26,""_activeTime"":0.09,""_recovery"":0.28,
                       ""_comboLinkTime"":0.4,""_cancelableAfter"":0.4,""_movementScale"":0,
                       ""_hitboxOffset"":{""x"":0.7,""y"":0.0},
                       ""_hitboxSize"":{""x"":1.1,""y"":0.8},
                       ""_statusBuildup"":[{""type"":0,""amount"":12}]}},
                    {""_kind"":1,""_cooldown"":4.0,""_minRange"":2.2,""_maxRange"":4.8,""_weight"":4,
                     ""_facingLockTime"":0.42,""_lunge"":{""x"":9.5,""y"":6.5},
                     ""_attack"":{""_displayName"":""Leap"",""_animationState"":""HeavyAttack"",
                       ""_damage"":20,""_damageType"":0,
                       ""_staggerDamage"":18,""_guardDamage"":22,""_knockback"":8,""_hitStop"":0.06,
                       ""_isUnblockable"":false,""_isParryable"":true,
                       ""_windup"":0.52,""_activeTime"":0.30,""_recovery"":0.55,
                       ""_comboLinkTime"":1.2,""_cancelableAfter"":1.2,""_movementScale"":0,
                       ""_hitboxOffset"":{""x"":0.6,""y"":0.0},
                       ""_hitboxSize"":{""x"":1.4,""y"":1.1},
                       ""_statusBuildup"":[{""type"":0,""amount"":16}]}}
                ]
            }", enemy);
            EditorUtility.SetDirty(enemy);
            return enemy;
        }

        /// <summary>
        /// Keeps its distance and makes the player come to it. The aim is long and obvious, the
        /// arrow is a real object that a wall stops and a dash outruns, and walking into its face
        /// gets a kick rather than a free kill — an archer that stood still while being hit would
        /// make the bow pointless.
        /// </summary>
        private static EnemyDefinition BuildArcherDefinition()
        {
            var enemy = CreateAsset<EnemyDefinition>($"{ForestFolder}/ForestArcher.asset");
            JsonUtility.FromJsonOverwrite(@"{
                ""_displayName"": ""Лісовий Лучник"",
                ""_experienceReward"": 55,
                ""_patrolSpeed"": 1.6, ""_chaseSpeed"": 3.0, ""_returnSpeed"": 2.4,
                ""_patrolRange"": 3.5, ""_idleTime"": 1.0, ""_idleTimeVariance"": 0.6,
                ""_detectionRange"": 12.0, ""_loseSightRange"": 15.0,
                ""_verticalDetectionRange"": 5.0, ""_alertTime"": 0.4,
                ""_searchTime"": 3.0, ""_behindDetectionRange"": 2.5,
                ""_facingDeadZone"": 0.35, ""_aiTickInterval"": 0.08,
                ""_attackRange"": 11.5, ""_verticalAttackRange"": 4.5, ""_attackCooldown"": 1.4,
                ""_facingLockTime"": 0.5, ""_decisionCooldown"": 0.3,
                ""_guardMovementScale"": 1.0,
                ""_preferredRange"": 7.5, ""_retreatRange"": 4.5, ""_retreatSpeed"": 3.4,
                ""_hurtTime"": 0.32, ""_hitReactionCooldown"": 0.5, ""_deathTime"": 1.5,
                ""_maxChaseTime"": 16.0,
                ""_attacks"": [
                    {""_kind"":0,""_cooldown"":2.3,""_minRange"":3.2,""_maxRange"":11.5,
                     ""_weight"":5,""_facingLockTime"":0.5,
                     ""_attack"":{""_displayName"":""Shoot"",""_animationState"":""Attack"",
                       ""_damage"":18,""_damageType"":0,
                       ""_staggerDamage"":12,""_guardDamage"":14,""_knockback"":3,""_hitStop"":0.04,
                       ""_isUnblockable"":false,""_isParryable"":false,
                       ""_windup"":0.62,""_activeTime"":0.06,""_recovery"":0.52,
                       ""_comboLinkTime"":1.0,""_cancelableAfter"":1.0,""_movementScale"":0,
                       ""_hitboxOffset"":{""x"":0.45,""y"":0.0},
                       ""_hitboxSize"":{""x"":0.35,""y"":0.35},
                       ""_statusBuildup"":[{""type"":0,""amount"":10}]}},
                    {""_kind"":2,""_cooldown"":3.5,""_minRange"":0,""_maxRange"":1.9,""_weight"":6,
                     ""_facingLockTime"":0.12,
                     ""_attack"":{""_displayName"":""Kick Away"",""_animationState"":""ShieldBash"",
                       ""_damage"":9,""_damageType"":0,
                       ""_staggerDamage"":14,""_guardDamage"":16,""_knockback"":11,
                       ""_hitStop"":0.05,
                       ""_isUnblockable"":false,""_isParryable"":true,
                       ""_windup"":0.24,""_activeTime"":0.09,""_recovery"":0.40,
                       ""_comboLinkTime"":0.6,""_cancelableAfter"":0.6,""_movementScale"":0,
                       ""_hitboxOffset"":{""x"":0.8,""y"":0.0},
                       ""_hitboxSize"":{""x"":1.3,""y"":1.2}}}
                ]
            }", enemy);
            EditorUtility.SetDirty(enemy);
            return enemy;
        }

        /// <summary>
        /// The reason to close distance. Fragile and slow to act, but it turns the ground itself
        /// against the player, and two of them left alone will fill a clearing with pools. Its
        /// cooldowns are long on purpose: the counter is reaching it, and that has to be possible.
        /// </summary>
        private static EnemyDefinition BuildShamanDefinition()
        {
            var enemy = CreateAsset<EnemyDefinition>($"{ForestFolder}/RootShaman.asset");
            JsonUtility.FromJsonOverwrite(@"{
                ""_displayName"": ""Кореневий Шаман"",
                ""_experienceReward"": 80,
                ""_patrolSpeed"": 1.2, ""_chaseSpeed"": 2.4, ""_returnSpeed"": 1.8,
                ""_patrolRange"": 2.5, ""_idleTime"": 1.4, ""_idleTimeVariance"": 0.6,
                ""_detectionRange"": 11.0, ""_loseSightRange"": 14.0,
                ""_verticalDetectionRange"": 4.5, ""_alertTime"": 0.5,
                ""_searchTime"": 3.5, ""_behindDetectionRange"": 2.5,
                ""_facingDeadZone"": 0.35, ""_aiTickInterval"": 0.09,
                ""_attackRange"": 10.0, ""_verticalAttackRange"": 4.0, ""_attackCooldown"": 2.0,
                ""_facingLockTime"": 0.6, ""_decisionCooldown"": 0.35,
                ""_guardMovementScale"": 1.0,
                ""_preferredRange"": 6.5, ""_retreatRange"": 4.0, ""_retreatSpeed"": 2.6,
                ""_hurtTime"": 0.38, ""_hitReactionCooldown"": 0.5, ""_deathTime"": 1.6,
                ""_maxChaseTime"": 16.0,
                ""_attacks"": [
                    {""_kind"":0,""_cooldown"":3.4,""_minRange"":2.8,""_maxRange"":10.0,
                     ""_weight"":4,""_facingLockTime"":0.6,
                     ""_attack"":{""_displayName"":""Poison Orb"",""_animationState"":""Attack"",
                       ""_damage"":14,""_damageType"":3,
                       ""_staggerDamage"":8,""_guardDamage"":18,""_knockback"":2,""_hitStop"":0.04,
                       ""_isUnblockable"":false,""_isParryable"":false,
                       ""_windup"":0.78,""_activeTime"":0.06,""_recovery"":0.62,
                       ""_comboLinkTime"":1.2,""_cancelableAfter"":1.2,""_movementScale"":0,
                       ""_hitboxOffset"":{""x"":0.5,""y"":0.1},
                       ""_hitboxSize"":{""x"":0.4,""y"":0.4},
                       ""_statusBuildup"":[{""type"":1,""amount"":34}]}},
                    {""_kind"":1,""_cooldown"":8.0,""_minRange"":2.0,""_maxRange"":8.0,
                     ""_weight"":3,""_facingLockTime"":0.9,
                     ""_attack"":{""_displayName"":""Toxic Ground"",""_animationState"":""HeavyAttack"",
                       ""_damage"":0,""_damageType"":3,
                       ""_staggerDamage"":0,""_guardDamage"":0,""_knockback"":0,""_hitStop"":0,
                       ""_isUnblockable"":true,""_isParryable"":false,
                       ""_windup"":0.92,""_activeTime"":0.05,""_recovery"":0.75,
                       ""_comboLinkTime"":1.4,""_cancelableAfter"":1.4,""_movementScale"":0,
                       ""_hitboxOffset"":{""x"":0.0,""y"":0.0},
                       ""_hitboxSize"":{""x"":0.01,""y"":0.01}}}
                ]
            }", enemy);
            EditorUtility.SetDirty(enemy);
            return enemy;
        }

        /// <summary>
        /// The spacing enemy. It owns the middle distance with a long thrust and punishes anyone
        /// who sits there, and it has nothing at all for a player who gets past the point of the
        /// spear — which is the whole lesson it teaches.
        /// </summary>
        private static EnemyDefinition BuildSpearmanDefinition()
        {
            var enemy = CreateAsset<EnemyDefinition>($"{ForestFolder}/RoyalSpearman.asset");
            JsonUtility.FromJsonOverwrite(@"{
                ""_displayName"": ""Королівський Списник"",
                ""_experienceReward"": 65,
                ""_patrolSpeed"": 1.4, ""_chaseSpeed"": 2.9, ""_returnSpeed"": 2.0,
                ""_patrolRange"": 3.5, ""_idleTime"": 1.1, ""_idleTimeVariance"": 0.7,
                ""_detectionRange"": 9.0, ""_loseSightRange"": 12.5,
                ""_verticalDetectionRange"": 3.5, ""_alertTime"": 0.4,
                ""_searchTime"": 3.0, ""_behindDetectionRange"": 2.0,
                ""_facingDeadZone"": 0.4, ""_aiTickInterval"": 0.08,
                ""_attackRange"": 3.6, ""_verticalAttackRange"": 1.5, ""_attackCooldown"": 1.3,
                ""_facingLockTime"": 0.2, ""_decisionCooldown"": 0.28,
                ""_guardMovementScale"": 1.0,
                ""_preferredRange"": 2.7, ""_retreatRange"": 1.1, ""_retreatSpeed"": 2.2,
                ""_hurtTime"": 0.3, ""_hitReactionCooldown"": 0.55, ""_deathTime"": 1.6,
                ""_maxChaseTime"": 14.0,
                ""_attacks"": [
                    {""_kind"":0,""_cooldown"":1.6,""_minRange"":1.6,""_maxRange"":3.6,
                     ""_weight"":4,""_facingLockTime"":0.22,
                     ""_attack"":{""_displayName"":""Thrust"",""_animationState"":""Attack"",
                       ""_damage"":24,""_damageType"":0,
                       ""_staggerDamage"":16,""_guardDamage"":20,""_knockback"":7,""_hitStop"":0.05,
                       ""_isUnblockable"":false,""_isParryable"":true,
                       ""_windup"":0.48,""_activeTime"":0.10,""_recovery"":0.46,
                       ""_comboLinkTime"":0.9,""_cancelableAfter"":0.9,""_movementScale"":0,
                       ""_hitboxOffset"":{""x"":1.85,""y"":0.0},
                       ""_hitboxSize"":{""x"":2.6,""y"":0.7}}},
                    {""_kind"":1,""_cooldown"":4.5,""_minRange"":0,""_maxRange"":2.2,""_weight"":4,
                     ""_facingLockTime"":0.26,
                     ""_attack"":{""_displayName"":""Sweep"",""_animationState"":""HeavyAttack"",
                       ""_damage"":20,""_damageType"":0,
                       ""_staggerDamage"":26,""_guardDamage"":32,""_knockback"":10,
                       ""_hitStop"":0.07,
                       ""_isUnblockable"":false,""_isParryable"":true,
                       ""_windup"":0.62,""_activeTime"":0.12,""_recovery"":0.62,
                       ""_comboLinkTime"":1.1,""_cancelableAfter"":1.1,""_movementScale"":0,
                       ""_hitboxOffset"":{""x"":1.1,""y"":-0.2},
                       ""_hitboxSize"":{""x"":2.4,""y"":1.0}}},
                    {""_kind"":2,""_cooldown"":6.0,""_minRange"":2.6,""_maxRange"":5.0,
                     ""_weight"":3,""_facingLockTime"":0.4,""_lunge"":{""x"":7.5,""y"":0.0},
                     ""_attack"":{""_displayName"":""Advancing Stab"",
                       ""_animationState"":""ShieldBash"",
                       ""_damage"":26,""_damageType"":0,
                       ""_staggerDamage"":22,""_guardDamage"":26,""_knockback"":9,
                       ""_hitStop"":0.06,
                       ""_isUnblockable"":false,""_isParryable"":true,
                       ""_windup"":0.55,""_activeTime"":0.22,""_recovery"":0.60,
                       ""_comboLinkTime"":1.2,""_cancelableAfter"":1.2,""_movementScale"":0,
                       ""_hitboxOffset"":{""x"":1.7,""y"":0.0},
                       ""_hitboxSize"":{""x"":2.4,""y"":0.8}}}
                ]
            }", enemy);
            EditorUtility.SetDirty(enemy);
            return enemy;
        }

        /// <summary>
        /// Points one of a definition's attacks at the thing it throws. Prefab references cannot
        /// survive <c>JsonUtility</c>, so the numbers arrive as JSON and the objects are wired here.
        /// </summary>
        private static void SetAttackSpawn(EnemyDefinition definition, int attackIndex,
            AttackSpawnKind kind, GameObject prefab, Vector2 offset, float speed = 9f,
            float range = 14f, Color color = default, bool atTarget = true)
        {
            var serialized = new SerializedObject(definition);
            var attacks = serialized.FindProperty("_attacks");
            if (attackIndex < 0 || attackIndex >= attacks.arraySize)
            {
                Debug.LogError($"{definition.name} has no attack {attackIndex} to arm.");
                return;
            }

            var spawn = attacks.GetArrayElementAtIndex(attackIndex).FindPropertyRelative("_spawn");
            spawn.FindPropertyRelative("_kind").enumValueIndex = (int)kind;
            spawn.FindPropertyRelative("_prefab").objectReferenceValue = prefab;
            spawn.FindPropertyRelative("_offset").vector2Value = offset;
            spawn.FindPropertyRelative("_speed").floatValue = speed;
            spawn.FindPropertyRelative("_range").floatValue = range;
            spawn.FindPropertyRelative("_pierce").intValue = 1;
            spawn.FindPropertyRelative("_color").colorValue =
                color == default ? Color.white : color;
            spawn.FindPropertyRelative("_atTargetPosition").boolValue = atTarget;
            spawn.FindPropertyRelative("_maxPlacementDistance").floatValue = 8f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
        }

        // ---------- projectiles and hazards ----------

        /// <summary>
        /// One projectile prefab per flavour, all of them the same component. The shooter decides
        /// everything at launch, so these differ only in how big and what colour they are.
        /// </summary>
        private static GameObject BuildProjectilePrefab(string name, string path, Vector2 size,
            Color color, float radius)
        {
            var root = new GameObject(name);
            var visualObject = FindOrCreateChild(root, "Visual");
            // A real shape rather than a tinted square. The drawing has transparent margins, so
            // it is scaled up a little to keep the same apparent size; the trigger below is what
            // decides hits and is untouched.
            var isArrow = name == "Arrow";
            var isOrb = name == "PoisonOrb";
            var drawn = isArrow ? new Vector2(size.x * 1.15f, size.x * 0.6f)
                : (isOrb ? size * 1.5f : new Vector2(size.x * 1.45f, size.y * 1.45f));
            visualObject.transform.localScale = new Vector3(drawn.x, drawn.y, 1f);
            var visual = Ensure<SpriteRenderer>(visualObject);
            visual.sprite = Shape(isArrow ? "shape_projectile_arrow"
                : (isOrb ? "shape_projectile_orb" : "shape_projectile_bolt"));
            visual.sharedMaterial = _material;
            visual.color = isArrow ? Color.white : color;
            SetSorting(visual, "Projectiles", 0);

            var trigger = Ensure<CircleCollider2D>(root);
            trigger.isTrigger = true;
            trigger.radius = radius;

            var projectile = Ensure<SpellProjectile>(root);
            SetReference(projectile, "_visual", visual);
            if (!isArrow)
            {
                // A shaman's orb glows sickly green on the stone as it comes; an arrow does not.
                var glow = FindOrCreateChild(root, "Glow");
                SetReference(projectile, "_light", AddLight(glow,
                    UnityEngine.Rendering.Universal.Light2D.LightType.Point, color, 0.9f, 2.2f, 0.1f, 0.6f));
            }

            // Fired by enemies, so it looks for the player and is stopped by the level.
            SetLayerMask(projectile, "_targetMask", 1 << PlayerLayer);
            SetLayerMask(projectile, "_obstacleMask", 1 << GroundLayer);
            return SavePrefab(root, path);
        }

        /// <summary>
        /// A patch of dangerous ground. The repeating version is a plant rooted in the level; the
        /// one-shot version is what a shaman or the Warden drops and then leaves behind briefly.
        /// </summary>
        private static GameObject BuildHazardPrefab(string name, string path, Vector2 size,
            Color telegraph, Color active, float telegraphTime, float activeTime, float dormantTime,
            StatusType status, float buildupPerSecond, float damagePerTick,
            DamageType damageType = DamageType.Poison)
        {
            var root = new GameObject(name);
            var visualObject = FindOrCreateChild(root, "Visual");
            // A low cloud that fills the zone, not a translucent rectangle over it.
            visualObject.transform.localScale = new Vector3(size.x * 1.15f, size.y * 1.3f, 1f);
            var visual = Ensure<SpriteRenderer>(visualObject);
            visual.sprite = Shape("shape_hazard_mist");
            visual.sharedMaterial = _material;
            visual.color = telegraph;
            SetSorting(visual, "VFXBack", 0);

            var zone = Ensure<HazardZone>(root);
            SetVector(zone, "_size", size);
            SetVector(zone, "_offset", Vector2.zero);
            SetLayerMask(zone, "_targetMask", 1 << PlayerLayer);
            SetFloat(zone, "_telegraphTime", telegraphTime);
            SetFloat(zone, "_activeTime", activeTime);
            SetFloat(zone, "_dormantTime", dormantTime);
            SetEnum(zone, "_status", (int)status);
            SetFloat(zone, "_buildupPerSecond", buildupPerSecond);
            SetFloat(zone, "_damagePerTick", damagePerTick);
            SetFloat(zone, "_tickInterval", 0.5f);
            SetEnum(zone, "_damageType", (int)damageType);
            SetFloat(zone, "_knockback", damagePerTick > 0f ? 4f : 0f);
            SetReference(zone, "_visual", visual);
            SetColor(zone, "_telegraphColor", telegraph);
            SetColor(zone, "_activeColor", active);
            return SavePrefab(root, path);
        }

        // ---------- the Hollow Warden ----------

        /// <summary>
        /// The second boss. Where the Guardian was a wall that taught blocking and parrying, the
        /// Warden is a spacing problem: it owns close range with a glaive, punishes the middle with
        /// a thrust, and refuses to let the player camp at distance by shooting roots. Smaller and
        /// faster than the Guardian, with less health and far less patience.
        /// </summary>
        private static GameObject BuildWardenPrefab(GameObject rootShot, GameObject poisonField,
            GameObject rootEruption)
        {
            var root = CreateActorRoot("HollowWarden", EnemyLayer,
                new Color(0.34f, 0.44f, 0.36f), new Vector2(1.1f, 2.2f));

            // Poison is its own element and does nothing to it; fire is what the forest fears.
            // Frost only slows what is already barely alive, and bleed still works on a body.
            ApplyResistances(Ensure<Resistances>(root),
                Resist(physical: 0.05f, fire: -0.25f, frost: 0.2f, magic: 0.1f));

            var body = Ensure<Rigidbody2D>(root);
            body.gravityScale = 3.5f;
            body.freezeRotation = true;
            // Physics runs at 50 Hz; without interpolation the body jumps between steps on a
            // 60 or 120 Hz screen while the smoothly interpolated player moves past it.
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.excludeLayers = (1 << PlayerLayer) | (1 << EnemyLayer);

            var capsule = Ensure<CapsuleCollider2D>(root);
            capsule.size = new Vector2(1.1f, 2.2f);
            capsule.sharedMaterial =
                AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(FrictionlessPath);

            var motor = Ensure<EnemyMotor2D>(root);
            SetLayerMask(motor, "_groundMask", 1 << GroundLayer);
            SetReference(motor, "_visual", root.transform.Find("Visual"));
            SetVector(motor, "_groundProbeOffset", new Vector2(0f, -1.15f));
            SetVector(motor, "_ledgeProbeOffset", new Vector2(0.7f, -1.1f));
            SetVector(motor, "_wallProbeOffset", new Vector2(0f, -0.5f));

            // Less health than the Guardian's 900 and a lower poise threshold, because it moves
            // far more: the punish windows are shorter, so they have to come round sooner.
            SetFloat(Ensure<Health>(root), "_maxHealth", 780f);
            var stagger = Ensure<StaggerMeter>(root);
            SetFloat(stagger, "_threshold", 150f);
            SetFloat(stagger, "_resistance", 0.35f);
            SetFloat(stagger, "_stunDuration", 1.6f);
            SetFloat(stagger, "_recoveryPerSecond", 24f);
            SetFloat(stagger, "_recoveryDelay", 2.2f);

            var statuses = Ensure<StatusEffects>(root);
            SetWardenStatusRules(statuses);

            var actor = Ensure<CombatActor>(root);
            SetFloat(actor, "_knockbackResistance", 0.88f);
            SetFloat(actor, "_knockbackLift", 0f);
            SetFloat(actor, "_hitInvulnerability", 0.04f);
            Ensure<Hurtbox>(root);

            var hitbox = Ensure<Hitbox>(root);
            SetLayerMask(hitbox, "_targetMask", 1 << PlayerLayer);

            var boss = Ensure<BossController>(root);
            SetString(boss, "_bossId", WardenBossId);
            SetString(boss, "_displayName", "Порожній Вартовий");
            SetReference(boss, "_hitbox", hitbox);
            SetReference(boss, "_bodyCollider", capsule);
            // No shield and no shockwave: this one is not the Guardian wearing green.
            SetReference(boss, "_shieldVisual", null);
            SetReference(boss, "_shockwave", null);
            SetFloat(boss, "_introDuration", 1.6f);
            SetFloat(boss, "_idlePause", 0.85f);
            SetFloat(boss, "_repositionSpeed", 4.2f);
            SetFloat(boss, "_repositionTimeout", 1.5f);
            SetFloat(boss, "_preferredRange", 2.6f);
            SetFloat(boss, "_phaseTransitionTime", 2.2f);
            SetFloat(boss, "_phaseTwoAttackSpeed", 1.15f);
            SetFloat(boss, "_phaseTwoPauseScale", 0.65f);
            SetFloat(boss, "_phaseTwoMoveScale", 1.2f);
            SetFloat(boss, "_staggerImmunityAfterStun", 1.4f);
            SetFloat(boss, "_parryRecoilTime", 0.75f);
            SetFloat(boss, "_tooCloseRange", 1.0f);
            ApplyWardenAttacks(boss, rootShot, poisonField, rootEruption);

            SetAudioProfile(root, AudioId.WardenAwaken, AudioId.WardenStunned, AudioId.WardenDeath,
                AudioId.WardenStep, AudioId.WardenGlaive, AudioId.WardenRootSlam,
                AudioId.WardenPhase);

            BuildStatusBars(root, actor, 1.5f, isBoss: true);
            BuildBossVisual(root, null, WardenAnimationSetPath, 2.2f);
            BuildStatusVfx(root, 1.2f);
            StripMissingScripts(root);
            return SavePrefab(root, $"{PrefabFolder}/HollowWarden.prefab");
        }

        /// <summary>
        /// The Warden's own resistances to being afflicted, as opposed to being hit. It is made of
        /// poison, so poison is nothing to it; bleed still finds something to open, and fire is
        /// what dry dead wood is for.
        /// </summary>
        private static void SetWardenStatusRules(StatusEffects statuses)
        {
            var serialized = new SerializedObject(statuses);
            var rules = serialized.FindProperty("_rules");
            for (var i = 0; i < rules.arraySize; i++)
            {
                var rule = rules.GetArrayElementAtIndex(i);
                switch ((StatusType)rule.FindPropertyRelative("type").enumValueIndex)
                {
                    case StatusType.Poison:
                        rule.FindPropertyRelative("immune").boolValue = true;
                        break;
                    case StatusType.Bleed:
                        rule.FindPropertyRelative("resistance").floatValue = 0.25f;
                        break;
                    case StatusType.Burn:
                        rule.FindPropertyRelative("resistance").floatValue = 0f;
                        rule.FindPropertyRelative("damage").floatValue = 8f;
                        break;
                    case StatusType.Frost:
                        rule.FindPropertyRelative("resistance").floatValue = 0.4f;
                        break;
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(statuses);
        }

        /// <summary>
        /// Seven moves across two phases, chosen by distance so every range has an answer and a
        /// threat. Nothing here homes, nothing lands without a windup the player can see, and the
        /// two ground attacks both telegraph through <see cref="HazardZone"/> before they bite.
        /// </summary>
        private static void ApplyWardenAttacks(BossController boss, GameObject rootShot,
            GameObject poisonField, GameObject rootEruption)
        {
            var serialized = new SerializedObject(boss);
            var attacks = serialized.FindProperty("_attacks");
            attacks.arraySize = 7;

            // Close: the ordinary blow, parryable, the rhythm to learn first.
            SetBossAttack(attacks.GetArrayElementAtIndex(0), BossAttackKind.GlaiveSlash, 1, 0,
                0f, 2.9f, 2.2f, 0.22f, BossAttackMovement.None, 0f, false, new[]
                {
                    Swing(24f, 30f, 6f, 0.40f, 0.12f, 0.50f, 1.6f, 1.7f, 2.2f)
                }, parryable: true);

            // Medium: punishes standing at the exact distance that feels safe against a glaive.
            SetBossAttack(attacks.GetArrayElementAtIndex(1), BossAttackKind.LongThrust, 1, 0,
                2.4f, 5.4f, 3.4f, 0.28f, BossAttackMovement.Charge, 6.5f, false, new[]
                {
                    Swing(28f, 34f, 9f, 0.62f, 0.13f, 0.62f, 0.9f, 2.6f, 3.6f)
                }, parryable: true);

            // Close and low. Parryable, but a jump clears it — two answers, not one.
            SetBossAttack(attacks.GetArrayElementAtIndex(2), BossAttackKind.Sweep, 1, 0,
                0f, 3.2f, 4.0f, 0.25f, BossAttackMovement.None, 0f, false, new[]
                {
                    Swing(22f, 42f, 11f, 0.55f, 0.14f, 0.70f, 0.8f, 1.5f, 3.0f)
                }, parryable: true);

            // Far: stops the fight becoming a staring contest. Magic, so it cannot be parried.
            SetBossAttack(attacks.GetArrayElementAtIndex(3), BossAttackKind.RootShot, 1, 0,
                4.5f, 16f, 4.5f, 0.35f, BossAttackMovement.None, 0f, false, new[]
                {
                    Swing(20f, 16f, 4f, 0.70f, 0.08f, 0.60f, 0.5f, 1.0f, 0.5f)
                }, parryable: false, status: StatusType.Poison, statusAmount: 30f,
                damageType: DamageType.Poison);

            // Phase two. The ground erupts along a lane in front of it, after the roots gather.
            SetBossAttack(attacks.GetArrayElementAtIndex(4), BossAttackKind.RootSlam, 2, 0,
                0f, 7f, 6.5f, 0.3f, BossAttackMovement.None, 0f, false, new[]
                {
                    Swing(26f, 50f, 8f, 0.80f, 0.12f, 0.85f, 1.2f, 1.6f, 2.4f)
                }, parryable: false);

            // Phase two: takes a piece of the arena away for a while rather than dealing damage.
            SetBossAttack(attacks.GetArrayElementAtIndex(5), BossAttackKind.PoisonField, 2, 0,
                2f, 10f, 11f, 0.4f, BossAttackMovement.None, 0f, false, new[]
                {
                    Swing(0f, 0f, 0f, 0.85f, 0.06f, 0.70f, 0.2f, 0.5f, 0.2f)
                }, parryable: false);

            // Phase two signature: committed at take-off, so a dash beats it and standing does not.
            SetBossAttack(attacks.GetArrayElementAtIndex(6), BossAttackKind.LeapStrike, 2, 0,
                3f, 13f, 7f, 0.55f, BossAttackMovement.Leap, 12f, false, new[]
                {
                    Swing(32f, 56f, 10f, 0.60f, 0.15f, 0.90f, 1.6f, 1.4f, 2.4f)
                }, parryable: false);

            serialized.ApplyModifiedPropertiesWithoutUndo();

            // The two ground attacks and the shot are wired to what they emit.
            SetBossSpawn(boss, 3, AttackSpawnKind.Projectile, rootShot, new Vector2(0.9f, 0.45f),
                speed: 8.5f, range: 18f, color: new Color(0.55f, 0.85f, 0.45f), atTarget: false);
            SetBossSpawn(boss, 4, AttackSpawnKind.Hazard, rootEruption, new Vector2(2.2f, -0.9f),
                atTarget: false);
            SetBossSpawn(boss, 5, AttackSpawnKind.Hazard, poisonField, new Vector2(0f, -0.9f),
                atTarget: true);

            // Phase two's combo is a second entry so the first stays a single readable blow.
            AddWardenCombo(boss);
        }

        /// <summary>
        /// Phase two's three-hit glaive combo. Added after the rest so the indices the spawns were
        /// wired to do not move under them.
        /// </summary>
        private static void AddWardenCombo(BossController boss)
        {
            var serialized = new SerializedObject(boss);
            var attacks = serialized.FindProperty("_attacks");
            attacks.arraySize = 8;
            SetBossAttack(attacks.GetArrayElementAtIndex(7), BossAttackKind.TripleCombo, 2, 0,
                0f, 3.4f, 5.0f, 0.22f, BossAttackMovement.None, 0f, false, new[]
                {
                    Swing(16f, 20f, 4f, 0.34f, 0.10f, 0.24f, 1.5f, 1.6f, 2.1f),
                    Swing(18f, 22f, 4f, 0.26f, 0.10f, 0.24f, 1.5f, 1.6f, 2.1f),
                    Swing(27f, 46f, 9f, 0.42f, 0.13f, 0.80f, 1.7f, 1.9f, 2.5f)
                }, parryable: true);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(boss);
        }

        private static void SetBossSpawn(BossController boss, int attackIndex,
            AttackSpawnKind kind, GameObject prefab, Vector2 offset, float speed = 9f,
            float range = 14f, Color color = default, bool atTarget = true)
        {
            var serialized = new SerializedObject(boss);
            var spawn = serialized.FindProperty("_attacks")
                .GetArrayElementAtIndex(attackIndex).FindPropertyRelative("_spawn");
            spawn.FindPropertyRelative("_kind").enumValueIndex = (int)kind;
            spawn.FindPropertyRelative("_prefab").objectReferenceValue = prefab;
            spawn.FindPropertyRelative("_offset").vector2Value = offset;
            spawn.FindPropertyRelative("_speed").floatValue = speed;
            spawn.FindPropertyRelative("_range").floatValue = range;
            spawn.FindPropertyRelative("_pierce").intValue = 1;
            spawn.FindPropertyRelative("_color").colorValue =
                color == default ? Color.white : color;
            spawn.FindPropertyRelative("_atTargetPosition").boolValue = atTarget;
            // Clamped well inside the clearing, so a field can never be dropped outside the arena.
            spawn.FindPropertyRelative("_maxPlacementDistance").floatValue = 9f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(boss);
        }

        /// <summary>Gives a prefab its voice. Every field is optional; silence is a valid answer.</summary>
        private static void SetAudioProfile(GameObject root, AudioId alert, AudioId hurt,
            AudioId death, AudioId step, AudioId light, AudioId heavy, AudioId special)
        {
            var profile = Ensure<ActorAudioProfile>(root);
            SetEnum(profile, "_alert", (int)alert);
            SetEnum(profile, "_hurt", (int)hurt);
            SetEnum(profile, "_death", (int)death);
            SetEnum(profile, "_step", (int)step);
            SetEnum(profile, "_lightAttack", (int)light);
            SetEnum(profile, "_heavyAttack", (int)heavy);
            SetEnum(profile, "_specialAttack", (int)special);
        }

        // ---------- audio ----------

        /// <summary>
        /// The forest's rows in the mixing table. Priorities are set against the Tomb's, not
        /// against each other: the Warden's dash telegraph sits at 120 like the Guardian's, because
        /// the sound that says "move now" is the one that must never be dropped, and forest
        /// ambience sits at the bottom with the crypt's.
        /// </summary>
        private static void AddForestAudio(List<AudioEvent> events,
            Dictionary<AudioId, AudioClip[]> clips)
        {
            // Hound. Frequent and quiet, with a hard cap: three of them must not be three times
            // as loud as one.
            events.Add(Sound(AudioId.HoundStep, clips, 0.3f, voices: 4, priority: 14,
                interval: 0.08f, spatial: 0.5f));
            events.Add(Sound(AudioId.HoundGrowl, clips, 0.6f, voices: 2, priority: 68));
            events.Add(Sound(AudioId.HoundBite, clips, 0.6f, voices: 3, priority: 60));
            // The leap's tell outranks almost everything a hound does, because it is the one the
            // player has to answer.
            events.Add(Sound(AudioId.HoundLeap, clips, 0.72f, voices: 2, priority: 88));
            events.Add(Sound(AudioId.HoundHurt, clips, 0.55f, voices: 3, priority: 55));
            events.Add(Sound(AudioId.HoundDeath, clips, 0.62f, voices: 2, priority: 70));

            // Archer. The draw is loud and long for the same reason: it is information.
            events.Add(Sound(AudioId.ArcherStep, clips, 0.3f, voices: 3, priority: 14,
                interval: 0.1f, spatial: 0.5f));
            events.Add(Sound(AudioId.ArcherDraw, clips, 0.6f, voices: 3, priority: 86));
            events.Add(Sound(AudioId.ArcherRelease, clips, 0.58f, voices: 3, priority: 72));
            events.Add(Sound(AudioId.ArrowImpact, clips, 0.6f, voices: 4, priority: 62));
            events.Add(Sound(AudioId.ArcherHurt, clips, 0.55f, voices: 3, priority: 55));
            events.Add(Sound(AudioId.ArcherDeath, clips, 0.6f, voices: 2, priority: 70));

            // Shaman. The cast is the interrupt window, so it is heard over the fight.
            events.Add(Sound(AudioId.ShamanStep, clips, 0.28f, voices: 2, priority: 12,
                interval: 0.14f, spatial: 0.5f));
            events.Add(Sound(AudioId.ShamanCast, clips, 0.64f, voices: 2, priority: 88));
            events.Add(Sound(AudioId.ShamanOrb, clips, 0.55f, voices: 3, priority: 64));
            events.Add(Sound(AudioId.ShamanZone, clips, 0.68f, voices: 2, priority: 84));
            events.Add(Sound(AudioId.ShamanHurt, clips, 0.55f, voices: 3, priority: 55));
            events.Add(Sound(AudioId.ShamanDeath, clips, 0.62f, voices: 2, priority: 70));

            // The Warden. Flat-panned where the moment is the player's rather than the room's.
            events.Add(Sound(AudioId.WardenAwaken, clips, 0.9f, voices: 1, priority: 110,
                spatial: 0.2f));
            events.Add(Sound(AudioId.WardenStep, clips, 0.5f, voices: 2, priority: 35,
                interval: 0.12f));
            events.Add(Sound(AudioId.WardenGlaive, clips, 0.74f, voices: 2, priority: 92));
            // The thrust is the move that punishes standing still, so its tell is the loudest
            // thing in the fight after the leap.
            events.Add(Sound(AudioId.WardenThrust, clips, 0.8f, voices: 2, priority: 118));
            events.Add(Sound(AudioId.WardenSweep, clips, 0.78f, voices: 2, priority: 112));
            events.Add(Sound(AudioId.WardenRootShot, clips, 0.7f, voices: 2, priority: 95));
            events.Add(Sound(AudioId.WardenRootSlam, clips, 0.88f, voices: 1, priority: 115));
            events.Add(Sound(AudioId.WardenPoisonField, clips, 0.72f, voices: 2, priority: 100));
            events.Add(Sound(AudioId.WardenLeap, clips, 0.82f, voices: 1, priority: 120));
            events.Add(Sound(AudioId.WardenPhase, clips, 0.95f, voices: 1, priority: 120,
                spatial: 0.1f));
            events.Add(Sound(AudioId.WardenStunned, clips, 0.75f, voices: 1, priority: 90));
            events.Add(Sound(AudioId.WardenDeath, clips, 0.95f, voices: 1, priority: 120,
                spatial: 0.1f));

            // Hazards and the world. The telegraph is spaced so a grove of plants is a forest and
            // not an alarm clock.
            events.Add(Sound(AudioId.HazardTelegraph, clips, 0.45f, voices: 3, priority: 58,
                interval: 0.3f));
            events.Add(Sound(AudioId.HazardBurst, clips, 0.52f, voices: 3, priority: 52,
                interval: 0.3f));
            events.Add(Sound(AudioId.GateOpen, clips, 0.75f, voices: 1, priority: 90,
                spatial: 0.3f));
            events.Add(Sound(AudioId.DoubleJump, clips, 0.45f, voices: 2, priority: 40));
            events.Add(Sound(AudioId.AbilityUnlocked, clips, 0.8f, voices: 1, priority: 100,
                spatial: 0f));

            // The forest itself. Bottom of the table with the crypt, as the room always is.
            events.Add(Sound(AudioId.AmbienceForestBase, clips, 0.5f, AudioBus.Ambience, voices: 1,
                priority: 5, spatial: 0f, loop: true));
            events.Add(Sound(AudioId.AmbienceDeepForestBase, clips, 0.5f, AudioBus.Ambience,
                voices: 1, priority: 5, spatial: 0f, loop: true));
            events.Add(Sound(AudioId.AmbienceLeaves, clips, 0.38f, AudioBus.Ambience, voices: 2,
                priority: 8, spatial: 0.6f));
            events.Add(Sound(AudioId.AmbienceBranch, clips, 0.34f, AudioBus.Ambience, voices: 1,
                priority: 8, spatial: 0.7f));
            events.Add(Sound(AudioId.AmbienceInsects, clips, 0.28f, AudioBus.Ambience, voices: 1,
                priority: 6, spatial: 0.6f));
            events.Add(Sound(AudioId.AmbienceDistantBeast, clips, 0.4f, AudioBus.Ambience,
                voices: 1, priority: 9, spatial: 0.8f));
            events.Add(Sound(AudioId.AmbienceCorruptionHum, clips, 0.35f, AudioBus.Ambience,
                voices: 1, priority: 6, spatial: 0.3f, loop: true));
        }

        // ---------- animation sets ----------

        private static void BuildForestAnimationSets()
        {
            BuildEnemySet(HoundAnimationSetPath, "enemy_hound", false,
                PlaceholderEnemyArt.HoundStates, PlaceholderEnemyArt.GenerateHound());
            BuildEnemySet(ArcherAnimationSetPath, "enemy_archer", false,
                PlaceholderEnemyArt.ArcherStates, PlaceholderEnemyArt.GenerateArcher());
            BuildEnemySet(ShamanAnimationSetPath, "enemy_shaman", false,
                PlaceholderEnemyArt.ShamanStates, PlaceholderEnemyArt.GenerateShaman());
            BuildEnemySet(SpearmanAnimationSetPath, "enemy_spearman", false,
                PlaceholderEnemyArt.SpearmanStates, PlaceholderEnemyArt.GenerateSpearman());
            BuildWardenSet();
        }

        private static void BuildWardenSet()
        {
            var frames = PlaceholderEnemyArt.GenerateWarden();
            var specs = PlaceholderEnemyArt.WardenStates;
            var set = CreateAsset<BossAnimationSet>(WardenAnimationSetPath);
            var serialized = new SerializedObject(set);
            var clips = serialized.FindProperty("_clips");
            clips.arraySize = specs.Length;
            for (var i = 0; i < specs.Length; i++)
            {
                var spec = specs[i];
                var element = clips.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("state").enumValueIndex = (int)spec.State;
                WriteClip(element.FindPropertyRelative("clip"),
                    $"boss_warden_{spec.State.ToString().ToLowerInvariant()}", spec.Fps,
                    spec.Loops, frames[spec.State]);
            }

            // The Warden's own moveset, stated outright: it has no shield to bash with, and the
            // Guardian's validation must not be the yardstick for a different fight.
            var required = serialized.FindProperty("_requiredStates");
            required.arraySize = specs.Length;
            for (var i = 0; i < specs.Length; i++)
            {
                required.GetArrayElementAtIndex(i).enumValueIndex = (int)specs[i].State;
            }

            var idle = System.Array.Find(specs, s => s.State == BossAnimationState.IdleP1);
            WriteClip(serialized.FindProperty("_fallback"), "boss_warden_idlep1", idle.Fps, true,
                frames[BossAnimationState.IdleP1]);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(set);
        }

        // ---------- everything the forest needs, built in order ----------

        /// <summary>
        /// Bundles the zone's prefabs so the scene builder can take them in one argument instead
        /// of growing another eight parameters.
        /// </summary>
        private sealed class ForestContent
        {
            public GameObject Hound;
            public GameObject Archer;
            public GameObject Shaman;
            public GameObject Spearman;
            public GameObject Warden;
            public GameObject PoisonPlant;
            /// <summary>The world graph; it replaced the zone registry of PHASE 24.</summary>
            public WorldGraph World;
        }

        private static ForestContent BuildForestContent()
        {
            EnsureFolder(ForestFolder);
            BuildForestAnimationSets();

            var houndDrops = BuildDropTable("CorruptedHoundDrops", @"{
                ""_entries"": [
                    {""_isCurrency"":true,""_chance"":1,""_isGuaranteed"":true,
                     ""_minAmount"":8,""_maxAmount"":18},
                    {""_itemId"":""beast_fang"",""_chance"":0.4,""_minAmount"":1,""_maxAmount"":2},
                    {""_itemId"":""corrupted_hide"",""_chance"":0.22,
                     ""_minAmount"":1,""_maxAmount"":1}
                ]
            }");
            var archerDrops = BuildDropTable("ForestArcherDrops", @"{
                ""_entries"": [
                    {""_isCurrency"":true,""_chance"":1,""_isGuaranteed"":true,
                     ""_minAmount"":14,""_maxAmount"":26},
                    {""_itemId"":""royal_arrowhead"",""_chance"":0.45,
                     ""_minAmount"":1,""_maxAmount"":3},
                    {""_itemId"":""hunter_cloth"",""_chance"":0.25,
                     ""_minAmount"":1,""_maxAmount"":1}
                ]
            }");
            var shamanDrops = BuildDropTable("RootShamanDrops", @"{
                ""_entries"": [
                    {""_isCurrency"":true,""_chance"":1,""_isGuaranteed"":true,
                     ""_minAmount"":22,""_maxAmount"":40},
                    {""_itemId"":""toxic_root"",""_chance"":0.5,""_minAmount"":1,""_maxAmount"":2},
                    {""_itemId"":""ancient_bark"",""_chance"":0.12,
                     ""_minAmount"":1,""_maxAmount"":1}
                ]
            }");
            var spearmanDrops = BuildDropTable("RoyalSpearmanDrops", @"{
                ""_entries"": [
                    {""_isCurrency"":true,""_chance"":1,""_isGuaranteed"":true,
                     ""_minAmount"":18,""_maxAmount"":32},
                    {""_itemId"":""iron_scrap"",""_chance"":0.45,""_minAmount"":1,""_maxAmount"":3},
                    {""_itemId"":""guard_emblem"",""_chance"":0.12,
                     ""_minAmount"":1,""_maxAmount"":1}
                ]
            }");

            var hound = BuildHoundDefinition();
            var archer = BuildArcherDefinition();
            var shaman = BuildShamanDefinition();
            var spearman = BuildSpearmanDefinition();
            AssetDatabase.SaveAssets();

            // The things the forest throws. Built before the enemies that reference them.
            var arrow = BuildProjectilePrefab("Arrow", ArrowPrefabPath,
                new Vector2(0.55f, 0.1f), new Color(0.82f, 0.78f, 0.62f), 0.13f);
            var orb = BuildProjectilePrefab("PoisonOrb", PoisonOrbPrefabPath,
                new Vector2(0.34f, 0.34f), new Color(0.55f, 0.9f, 0.4f), 0.19f);
            var rootShot = BuildProjectilePrefab("RootShot", RootShotPrefabPath,
                new Vector2(0.48f, 0.26f), new Color(0.5f, 0.8f, 0.42f), 0.2f);

            // Level hazard: cycles forever, so its rhythm can be learned and walked through.
            var plant = BuildHazardPrefab("PoisonPlant", PoisonPlantPrefabPath,
                new Vector2(2.6f, 1.8f), new Color(0.55f, 0.85f, 0.35f, 0.3f),
                new Color(0.45f, 0.95f, 0.3f, 0.62f), telegraphTime: 0.85f, activeTime: 1.7f,
                dormantTime: 2.8f, StatusType.Poison, buildupPerSecond: 40f, damagePerTick: 0f);
            // Dropped by a caster: one burst, then gone.
            var field = BuildHazardPrefab("PoisonField", PoisonFieldPrefabPath,
                new Vector2(3.4f, 1.6f), new Color(0.5f, 0.85f, 0.35f, 0.32f),
                new Color(0.42f, 0.95f, 0.32f, 0.6f), telegraphTime: 0.7f, activeTime: 5.0f,
                dormantTime: 0f, StatusType.Poison, buildupPerSecond: 46f, damagePerTick: 3f);
            // The Warden's lane of roots: short, hard, and it does real damage.
            var eruption = BuildHazardPrefab("RootEruption", RootEruptionPrefabPath,
                new Vector2(5.2f, 1.4f), new Color(0.45f, 0.7f, 0.35f, 0.4f),
                new Color(0.62f, 0.45f, 0.85f, 0.7f), telegraphTime: 0.55f, activeTime: 0.9f,
                dormantTime: 0f, StatusType.Poison, buildupPerSecond: 24f, damagePerTick: 14f);

            // Arrow on the archer's shot, orb and pool on the shaman's two spells.
            SetAttackSpawn(archer, 0, AttackSpawnKind.Projectile, arrow,
                new Vector2(0.55f, 0.35f), speed: 13f, range: 16f,
                color: new Color(0.82f, 0.78f, 0.62f), atTarget: false);
            SetAttackSpawn(shaman, 0, AttackSpawnKind.Projectile, orb,
                new Vector2(0.6f, 0.45f), speed: 6.5f, range: 14f,
                color: new Color(0.55f, 0.9f, 0.4f), atTarget: false);
            SetAttackSpawn(shaman, 1, AttackSpawnKind.Hazard, field,
                new Vector2(0f, -0.85f), atTarget: true);

            var content = new ForestContent
            {
                PoisonPlant = plant,
                World = BuildWorldGraph(),
                Hound = BuildEnemyPrefab("CorruptedHound", hound, false,
                    new Color(0.3f, 0.26f, 0.3f), 70f, 32f, houndDrops,
                    // Thin, dry and already rotting: fire and bleed both find it easily.
                    Resist(physical: 0f, fire: -0.2f, frost: -0.15f, magic: 0f),
                    HoundAnimationSetPath, new Vector2(1.45f, 0.9f), staggerResistance: 0f,
                    configure: go => SetAudioProfile(go, AudioId.HoundGrowl, AudioId.HoundHurt,
                        AudioId.HoundDeath, AudioId.HoundStep, AudioId.HoundBite,
                        AudioId.HoundLeap, AudioId.None)),
                Archer = BuildEnemyPrefab("ForestArcher", archer, false,
                    new Color(0.34f, 0.42f, 0.5f), 85f, 45f, archerDrops,
                    Resist(physical: 0f, fire: -0.1f, frost: 0f, magic: 0f),
                    ArcherAnimationSetPath, new Vector2(0.8f, 1.7f), staggerResistance: 0f,
                    configure: go => SetAudioProfile(go, AudioId.ArcherDraw, AudioId.ArcherHurt,
                        AudioId.ArcherDeath, AudioId.ArcherStep, AudioId.ArcherDraw,
                        AudioId.None, AudioId.ArcherRelease)),
                Shaman = BuildEnemyPrefab("RootShaman", shaman, false,
                    new Color(0.36f, 0.5f, 0.34f), 75f, 38f, shamanDrops,
                    // It is made of the poison it throws; fire is the answer to it.
                    Resist(physical: -0.1f, fire: -0.3f, frost: 0.1f, magic: 0.25f),
                    ShamanAnimationSetPath, new Vector2(0.85f, 1.75f), staggerResistance: 0f,
                    configure: go => SetAudioProfile(go, AudioId.ShamanCast, AudioId.ShamanHurt,
                        AudioId.ShamanDeath, AudioId.ShamanStep, AudioId.ShamanOrb,
                        AudioId.ShamanZone, AudioId.None)),
                Spearman = BuildEnemyPrefab("RoyalSpearman", spearman, false,
                    new Color(0.42f, 0.5f, 0.52f), 120f, 70f, spearmanDrops,
                    Resist(physical: 0.12f, fire: 0f, frost: 0f, magic: 0f),
                    SpearmanAnimationSetPath, new Vector2(0.9f, 1.75f), staggerResistance: 0.2f,
                    // Still a royal soldier, so it keeps the guard's voice; only its spear is new.
                    configure: go => SetAudioProfile(go, AudioId.GuardRaise, AudioId.GuardHurt,
                        AudioId.GuardDeath, AudioId.GuardStep, AudioId.WardenThrust,
                        AudioId.GuardHeavy, AudioId.WardenThrust)),
                Warden = BuildWardenPrefab(rootShot, field, eruption)
            };

            AssetDatabase.SaveAssets();
            return content;
        }
    }
}
