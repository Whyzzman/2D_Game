using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    public static partial class ContentBuilder
    {
        // ---------- items ----------

        /// <summary>
        /// Every balancing number for progression, in one asset. The defaults here are the
        /// starting point for tuning, not a final balance.
        /// </summary>
        private static ProgressionCurve BuildProgressionCurve()
        {
            var curve = CreateAsset<ProgressionCurve>($"{DataFolder}/{ProgressionCurveName}");
            JsonUtility.FromJsonOverwrite(@"{
                ""_baseExperience"": 100, ""_experienceExponent"": 0.85,
                ""_experienceRounding"": 10, ""_maxLevel"": 100, ""_statPointsPerLevel"": 1,
                ""_startingStrength"": 5, ""_startingDexterity"": 5,
                ""_startingVitality"": 5, ""_startingEndurance"": 5, ""_statCap"": 99,
                ""_baseHealth"": 100, ""_baseStamina"": 100,
                ""_healthPerVitality"": 10, ""_healthPerVitalityMid"": 7,
                ""_healthPerVitalityHigh"": 4,
                ""_staminaPerEndurance"": 6, ""_staminaPerEnduranceMid"": 4,
                ""_staminaPerEnduranceHigh"": 2,
                ""_softCap"": 20, ""_hardCap"": 40,
                ""_baseEquipLoad"": 30, ""_equipLoadPerEndurance"": 2,
                ""_scaleFactor"": 0.03, ""_requirementPenalty"": 0.6
            }", curve);
            EditorUtility.SetDirty(curve);
            return curve;
        }

        /// <summary>
        /// The first spells. Each answers a different problem: Fire Bolt is the reliable one,
        /// Frost Shard trades damage for staggering power, Arcane Burst clears a crowd at a
        /// price, and Mana Shield buys time instead of dealing damage at all.
        /// </summary>
        private static SpellDatabase BuildSpells()
        {
            EnsureFolder(SpellFolder);
            var spells = new[]
            {
                BuildSpell("fire_bolt", @"{
                    ""_displayName"": ""Вогняний Спалах"",
                    ""_description"": ""Кидає згусток вогню перед собою. Дешеве й надійне."",
                    ""_type"": 0, ""_element"": 1,
                    ""_statusBuildup"": [{""type"":2,""amount"":25}],
                    ""_manaCost"": 15, ""_castTime"": 0.3, ""_recoveryTime"": 0.18,
                    ""_cooldown"": 0.4, ""_movementScale"": 0.35, ""_canCastInAir"": true,
                    ""_baseDamage"": 30, ""_intelligenceScaling"": 0.8,
                    ""_staggerDamage"": 10, ""_guardDamage"": 12, ""_knockback"": 3,
                    ""_hitStop"": 0.05, ""_canCrit"": true, ""_isBlockable"": true,
                    ""_projectileSpeed"": 14, ""_range"": 13, ""_pierceCount"": 1,
                    ""_color"": {""r"":1,""g"":0.55,""b"":0.2,""a"":1}
                }"),
                BuildSpell("frost_shard", @"{
                    ""_displayName"": ""Крижаний Осколок"",
                    ""_description"": ""Б'є слабше за вогонь, але значно сильніше розхитує ворога."",
                    ""_type"": 0, ""_element"": 2,
                    ""_statusBuildup"": [{""type"":3,""amount"":30}],
                    ""_manaCost"": 18, ""_castTime"": 0.35, ""_recoveryTime"": 0.2,
                    ""_cooldown"": 0.6, ""_movementScale"": 0.35, ""_canCastInAir"": true,
                    ""_baseDamage"": 22, ""_intelligenceScaling"": 0.7,
                    ""_staggerDamage"": 26, ""_guardDamage"": 18, ""_knockback"": 4,
                    ""_hitStop"": 0.06, ""_canCrit"": true, ""_isBlockable"": true,
                    ""_projectileSpeed"": 12, ""_range"": 12, ""_pierceCount"": 1,
                    ""_color"": {""r"":0.5,""g"":0.8,""b"":1,""a"":1}
                }"),
                BuildSpell("arcane_burst", @"{
                    ""_displayName"": ""Вибух Сили"",
                    ""_description"": ""Розриває повітря навколо. Дорого, довго — і скидає всіх поряд."",
                    ""_type"": 1, ""_element"": 7,
                    ""_manaCost"": 30, ""_castTime"": 0.75, ""_recoveryTime"": 0.4,
                    ""_cooldown"": 3, ""_movementScale"": 0.1, ""_canCastInAir"": false,
                    ""_baseDamage"": 45, ""_intelligenceScaling"": 1.0,
                    ""_staggerDamage"": 45, ""_guardDamage"": 55, ""_knockback"": 7,
                    ""_hitStop"": 0.1, ""_canCrit"": true, ""_isBlockable"": true,
                    ""_areaRadius"": 3.2,
                    ""_color"": {""r"":0.75,""g"":0.5,""b"":1,""a"":1}
                }"),
                BuildSpell("mana_shield", @"{
                    ""_displayName"": ""Мановий Щит"",
                    ""_description"": ""Кілька секунд шкода по вас слабша. Не робить невразливим."",
                    ""_type"": 2, ""_element"": 7,
                    ""_manaCost"": 35, ""_castTime"": 0.4, ""_recoveryTime"": 0.25,
                    ""_cooldown"": 12, ""_movementScale"": 0.5, ""_canCastInAir"": true,
                    ""_baseDamage"": 0, ""_intelligenceScaling"": 0,
                    ""_staggerDamage"": 0, ""_canCrit"": false,
                    ""_damageReduction"": 0.2, ""_buffDuration"": 5,
                    ""_color"": {""r"":0.6,""g"":0.85,""b"":0.9,""a"":1}
                }")
            };

            var database = CreateAsset<SpellDatabase>($"{DataFolder}/{SpellDatabaseName}");
            var serialized = new SerializedObject(database);
            var list = serialized.FindProperty("_spells");
            list.arraySize = spells.Length;
            for (var i = 0; i < spells.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = spells[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
            return database;
        }

        private static SpellDefinition BuildSpell(string id, string json)
        {
            var spell = CreateAsset<SpellDefinition>($"{SpellFolder}/{id}.asset");
            JsonUtility.FromJsonOverwrite(json, spell);
            var serialized = new SerializedObject(spell);
            serialized.FindProperty("_spellId").stringValue = id;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(spell);
            return spell;
        }

        /// <summary>
        /// The two first trees. Warrior rewards commitment — heavy blows, a guard that holds and
        /// punishing what it breaks. Hunter rewards position — footwork, backs and openings.
        /// Nothing here is referenced by name in code: combat asks the tree by effect.
        /// </summary>
        private static SkillDatabase BuildSkills()
        {
            EnsureFolder(SkillFolder);
            var skills = new[]
            {
                // ---- Warrior ----
                BuildSkill("warrior_training", "Вишкіл воїна", SkillBranch.Warrior,
                    "Основа гілки воїна: кожен удар у ближньому бою важчий.",
                    1, 1, 1, new Vector2Int(0, 0), null,
                    new[] { new SkillEffect(SkillEffectType.MeleeDamage, 0.05f) }),
                BuildSkill("warrior_heavy_mastery", "Майстер важкої зброї", SkillBranch.Warrior,
                    "Важкі атаки б'ють сильніше і значно краще ламають рівновагу.",
                    1, 1, 1, new Vector2Int(1, 0), new[] { "warrior_training" },
                    new[]
                    {
                        new SkillEffect(SkillEffectType.HeavyDamage, 0.15f),
                        new SkillEffect(SkillEffectType.HeavyStagger, 0.2f)
                    }),
                BuildSkill("warrior_executioner", "Кат", SkillBranch.Warrior,
                    "Удари по оглушеному чи зі зламаним блоком ворогу завдають більше шкоди.",
                    1, 1, 1, new Vector2Int(2, 1), new[] { "warrior_heavy_mastery" },
                    new[] { new SkillEffect(SkillEffectType.StunnedDamage, 0.15f) }),
                BuildSkill("warrior_efficient_guard", "Ощадливий блок", SkillBranch.Warrior,
                    "Тримати блок коштує менше витривалості.",
                    1, 1, 1, new Vector2Int(1, 2), new[] { "warrior_training" },
                    new[] { new SkillEffect(SkillEffectType.GuardCost, -0.15f) }),
                BuildSkill("warrior_counter_force", "Сила відповіді", SkillBranch.Warrior,
                    "Ідеальне парирування сильніше розхитує рівновагу нападника.",
                    1, 1, 1, new Vector2Int(2, 2), new[] { "warrior_efficient_guard" },
                    new[] { new SkillEffect(SkillEffectType.ParryStagger, 0.25f) }),
                BuildSkill("warrior_guard_recovery", "Швидке дихання", SkillBranch.Warrior,
                    "Після заблокованого удару витривалість починає відновлюватися раніше.",
                    1, 1, 1, new Vector2Int(2, 3), new[] { "warrior_efficient_guard" },
                    new[] { new SkillEffect(SkillEffectType.GuardRecovery, 0.25f) }),
                BuildSkill("warrior_iron_body", "Залізне тіло", SkillBranch.Warrior,
                    "Вас важче збити з ніг. Кожен ранг додає опір оглушенню.",
                    1, 3, 1, new Vector2Int(3, 2), new[] { "warrior_efficient_guard" },
                    new[] { new SkillEffect(SkillEffectType.StaggerResistance, 0.05f) }),
                BuildSkill("warrior_second_wind", "Друге дихання", SkillBranch.Warrior,
                    "На малому здоров'ї ви ненадовго стаєте стійкішим. Не лікує.",
                    1, 1, 4, new Vector2Int(4, 2), new[] { "warrior_iron_body" },
                    new[] { new SkillEffect(SkillEffectType.SecondWindResistance, 0.2f) }),

                // ---- Hunter ----
                BuildSkill("hunter_training", "Вишкіл мисливця", SkillBranch.Hunter,
                    "Основа гілки мисливця: легка зброя у ваших руках небезпечніша.",
                    1, 1, 1, new Vector2Int(0, 0), null,
                    new[] { new SkillEffect(SkillEffectType.DexterityWeaponDamage, 0.05f) }),
                BuildSkill("hunter_quickstep", "Швидкий крок", SkillBranch.Hunter,
                    "Ривок відновлюється швидше.",
                    1, 1, 1, new Vector2Int(1, 0), new[] { "hunter_training" },
                    new[] { new SkillEffect(SkillEffectType.DashCooldown, -0.15f) }),
                BuildSkill("hunter_efficiency", "Ощадливий рух", SkillBranch.Hunter,
                    "Біг по стіні коштує менше витривалості.",
                    1, 1, 1, new Vector2Int(2, 0), new[] { "hunter_quickstep" },
                    new[] { new SkillEffect(SkillEffectType.WallRunStaminaCost, -0.15f) }),
                BuildSkill("hunter_momentum", "Інерція", SkillBranch.Hunter,
                    "Ухилившись від справжнього удару, ви ненадовго б'єте сильніше.",
                    1, 1, 1, new Vector2Int(2, 1), new[] { "hunter_quickstep" },
                    new[] { new SkillEffect(SkillEffectType.MomentumDamage, 0.15f) }),
                BuildSkill("hunter_precision", "Точність", SkillBranch.Hunter,
                    "Шанс критичного удару. Кожен ранг додає ще.",
                    1, 2, 1, new Vector2Int(1, 2), new[] { "hunter_training" },
                    new[] { new SkillEffect(SkillEffectType.CritChance, 0.05f) }),
                BuildSkill("hunter_predator", "Хижак", SkillBranch.Hunter,
                    "Удари у спину завдають більше шкоди.",
                    1, 1, 1, new Vector2Int(2, 2), new[] { "hunter_precision" },
                    new[] { new SkillEffect(SkillEffectType.BackAttackDamage, 0.1f) }),
                BuildSkill("hunter_adrenaline", "Адреналін", SkillBranch.Hunter,
                    "Після вбивства витривалість деякий час відновлюється швидше.",
                    1, 1, 1, new Vector2Int(3, 2), new[] { "hunter_precision" },
                    new[] { new SkillEffect(SkillEffectType.AdrenalineRegen, 0.5f) }),

                // ---- Mage ----
                BuildSkill("mage_training", "Вишкіл мага", SkillBranch.Mage,
                    "Основа гілки мага: закляття сильніші, і ви вчите Вогняний Спалах.",
                    1, 1, 1, new Vector2Int(0, 0), null,
                    new[] { new SkillEffect(SkillEffectType.SpellDamage, 0.05f) },
                    SkillType.Passive, "fire_bolt"),
                BuildSkill("mage_mana_well", "Джерело мани", SkillBranch.Mage,
                    "Запас мани зростає.",
                    1, 2, 1, new Vector2Int(1, 0), new[] { "mage_training" },
                    new[] { new SkillEffect(SkillEffectType.MaxMana, 15f) }),
                BuildSkill("mage_efficiency", "Ощадливе читання", SkillBranch.Mage,
                    "Закляття коштують менше мани.",
                    1, 1, 1, new Vector2Int(2, 0), new[] { "mage_mana_well" },
                    new[] { new SkillEffect(SkillEffectType.SpellManaCost, -0.1f) }),
                BuildSkill("mage_mana_shield", "Мановий щит", SkillBranch.Mage,
                    "Відкриває захисне закляття, що ненадовго послаблює вхідну шкоду.",
                    2, 1, 5, new Vector2Int(3, 0), new[] { "mage_efficiency" },
                    System.Array.Empty<SkillEffect>(), SkillType.Passive, "mana_shield"),
                BuildSkill("mage_fire_mastery", "Влада вогню", SkillBranch.Mage,
                    "Вогняні закляття завдають більше шкоди. Інші стихії не змінюються.",
                    1, 1, 1, new Vector2Int(1, 2), new[] { "mage_training" },
                    new[] { new SkillEffect(SkillEffectType.FireDamage, 0.15f) }),
                BuildSkill("mage_arcane_focus", "Зосередження", SkillBranch.Mage,
                    "Усі закляття завдають більше шкоди.",
                    1, 1, 1, new Vector2Int(2, 2), new[] { "mage_fire_mastery" },
                    new[] { new SkillEffect(SkillEffectType.SpellDamage, 0.1f) }),
                BuildSkill("mage_quick_cast", "Швидке читання", SkillBranch.Mage,
                    "Закляття читаються швидше. Миттєвими вони не стають.",
                    1, 1, 1, new Vector2Int(3, 2), new[] { "mage_arcane_focus" },
                    new[] { new SkillEffect(SkillEffectType.SpellCastTime, -0.12f) }),
                BuildSkill("mage_frost_studies", "Студії льоду", SkillBranch.Mage,
                    "Відкриває Крижаний Осколок і підсилює крижані закляття.",
                    1, 1, 1, new Vector2Int(1, 4), new[] { "mage_training" },
                    new[] { new SkillEffect(SkillEffectType.FrostDamage, 0.1f) },
                    SkillType.Passive, "frost_shard"),
                BuildSkill("mage_arcane_burst", "Вибух сили", SkillBranch.Mage,
                    "Відкриває Вибух Сили — дороге закляття проти кількох ворогів одразу.",
                    2, 1, 5, new Vector2Int(2, 4), new[] { "mage_frost_studies" },
                    System.Array.Empty<SkillEffect>(), SkillType.Passive, "arcane_burst"),
                BuildSkill("mage_spell_precision", "Магічна точність", SkillBranch.Mage,
                    "Шанс критичного удару закляттями.",
                    1, 2, 1, new Vector2Int(3, 4), new[] { "mage_frost_studies" },
                    new[] { new SkillEffect(SkillEffectType.SpellCritChance, 0.05f) }),
                BuildSkill("mage_arcane_recovery", "Живлення", SkillBranch.Mage,
                    "Убивши ворога закляттям, ви повертаєте трохи мани.",
                    1, 1, 4, new Vector2Int(4, 4), new[] { "mage_spell_precision" },
                    new[] { new SkillEffect(SkillEffectType.SpellKillMana, 8f) }),

                BuildGroundBreaker(),
                BuildDashStrike(),
                BuildShadowSkill("shadow_initiate", "Посвята Тіні", SkillEffectType.DarkDamage, 0.05f, null, 0, 0),
                BuildShadowSkill("shadow_blood_edge", "Криваве лезо", SkillEffectType.BloodBuildup, 10f, "shadow_initiate", 1, 0),
                BuildShadowSkill("shadow_dark_affinity", "Спорідненість із темрявою", SkillEffectType.DarkDamage, 0.12f, "shadow_initiate", 1, 2),
                BuildShadowSkill("shadow_ruthless", "Безжальна Тінь", SkillEffectType.StatusTargetDamage, 0.12f, "shadow_blood_edge", 2, 0),
                BuildShadowSkill("shadow_veiled_step", "Прихований крок", SkillEffectType.ShadowCost, -0.1f, "shadow_initiate", 1, 1),
                BuildShadowSkill("shadow_cutting_dash", "Ріжуча Тінь", SkillEffectType.CuttingShadow, 1f, "shadow_dash", 3, 1),
                BuildShadowActive(false),
                BuildShadowActive(true)
            };

            var database = CreateAsset<SkillDatabase>($"{DataFolder}/{SkillDatabaseName}");
            var serialized = new SerializedObject(database);
            var list = serialized.FindProperty("_skills");
            list.arraySize = skills.Length;
            for (var i = 0; i < skills.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = skills[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
            return database;
        }

        /// <summary>
        /// The Warrior answer to a raised shield: slow, committed and enormously heavy on the
        /// guard. Its reach is a wide patch of ground in front, not the screen.
        /// </summary>
        private static SkillDefinition BuildShadowSkill(string id, string name, SkillEffectType effect,
            float value, string prerequisite, int column, int row)
        {
            return BuildSkill(id, name, SkillBranch.Shadow, name, 1, 1, 1,
                new Vector2Int(column, row), prerequisite == null ? System.Array.Empty<string>()
                    : new[] { prerequisite }, new[] { new SkillEffect(effect, value) });
        }

        private static SkillDefinition BuildShadowActive(bool strike)
        {
            var skill = BuildSkill(strike ? "shadow_strike" : "shadow_dash",
                strike ? "Удар Тіні" : "Ривок Тіні", SkillBranch.Shadow,
                strike ? "Короткий удар із безпечним переміщенням." : "Ривок крізь ворога, але не стіни.",
                1, 1, 1, new Vector2Int(2, strike ? 2 : 1),
                new[] { strike ? "shadow_blood_edge" : "shadow_veiled_step" },
                System.Array.Empty<SkillEffect>(), SkillType.Active);
            JsonUtility.FromJsonOverwrite(strike ? @"{
                ""_active"": { ""_shadowAction"":2, ""_manaCost"":25, ""_cooldown"":5,
                ""_requiresMeleeWeapon"":true, ""_attack"":{
                ""_animationState"":""ShadowStrike"", ""_staminaCost"":15, ""_windup"":0.18,
                ""_activeTime"":0.08, ""_recovery"":0.28, ""_staggerDamage"":25,
                ""_hitboxOffset"":{""x"":0.8,""y"":0}, ""_hitboxSize"":{""x"":2,""y"":1.5}}}
            }" : @"{
                ""_active"": { ""_shadowAction"":1, ""_manaCost"":15, ""_cooldown"":3,
                ""_requiresMeleeWeapon"":false, ""_attack"":{
                ""_animationState"":""ShadowDash"", ""_staminaCost"":20, ""_windup"":0.01,
                ""_activeTime"":0.24, ""_recovery"":0.03, ""_staggerDamage"":5,
                ""_hitboxOffset"":{""x"":0,""y"":0}, ""_hitboxSize"":{""x"":1.2,""y"":1.5}}}
            }", skill);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillDefinition BuildGroundBreaker()
        {
            var skill = BuildSkill("warrior_ground_breaker", "Розлом", SkillBranch.Warrior,
                "Обвалює зброю в землю перед собою. Величезна шкода рівновазі й блоку.",
                2, 1, 5, new Vector2Int(2, 0), new[] { "warrior_heavy_mastery" },
                System.Array.Empty<SkillEffect>(), SkillType.Active);
            JsonUtility.FromJsonOverwrite(@"{
                ""_active"": {
                    ""_cooldown"": 9.0, ""_requiresMeleeWeapon"": true,
                    ""_lungeSpeed"": 0.0, ""_invulnerability"": 0.0,
                    ""_animationState"": 25,
                    ""_tags"": [""Melee"",""Warrior"",""Heavy"",""Physical""],
                    ""_attack"": {
                        ""_displayName"":""Ground Breaker"",""_animationState"":""GroundBreaker"",
                        ""_damage"":0,""_damageMultiplier"":2.2,""_damageType"":0,
                        ""_staminaCost"":40,""_staggerDamage"":90,""_guardDamage"":110,
                        ""_knockback"":9,""_hitStop"":0.14,""_isUnblockable"":false,
                        ""_windup"":0.55,""_activeTime"":0.14,""_recovery"":0.70,
                        ""_comboLinkTime"":1.5,""_cancelableAfter"":1.5,
                        ""_hitboxOffset"":{""x"":1.35,""y"":-0.25},
                        ""_hitboxSize"":{""x"":3.4,""y"":1.5},
                        ""_movementScale"":0.0
                    }
                }
            }", skill);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        /// <summary>
        /// The Hunter answer to distance: a short committed lunge that closes a gap and lands one
        /// hard hit. Its invulnerability is a sliver, so it is an opening taken, not a free one.
        /// </summary>
        private static SkillDefinition BuildDashStrike()
        {
            var skill = BuildSkill("hunter_dash_strike", "Ривковий удар", SkillBranch.Hunter,
                "Кидок уперед із ударом. Скорочує дистанцію й карає відкат ворога.",
                2, 1, 5, new Vector2Int(3, 0), new[] { "hunter_quickstep" },
                System.Array.Empty<SkillEffect>(), SkillType.Active);
            JsonUtility.FromJsonOverwrite(@"{
                ""_active"": {
                    ""_cooldown"": 6.0, ""_requiresMeleeWeapon"": true,
                    ""_lungeSpeed"": 13.0, ""_invulnerability"": 0.08,
                    ""_animationState"": 26,
                    ""_tags"": [""Melee"",""Hunter"",""Mobility"",""Dash"",""Physical""],
                    ""_attack"": {
                        ""_displayName"":""Dash Strike"",""_animationState"":""DashStrike"",
                        ""_damage"":0,""_damageMultiplier"":1.6,""_damageType"":0,
                        ""_staminaCost"":25,""_staggerDamage"":30,""_guardDamage"":20,
                        ""_knockback"":6,""_hitStop"":0.07,""_isUnblockable"":false,
                        ""_windup"":0.10,""_activeTime"":0.12,""_recovery"":0.34,
                        ""_comboLinkTime"":1.0,""_cancelableAfter"":1.0,
                        ""_hitboxOffset"":{""x"":1.0,""y"":0.05},
                        ""_hitboxSize"":{""x"":1.8,""y"":1.3},
                        ""_movementScale"":0.0
                    }
                }
            }", skill);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillDefinition BuildSkill(string id, string displayName,
            SkillBranch branch, string description, int cost, int maxRank, int requiredLevel,
            Vector2Int position, string[] prerequisites, SkillEffect[] effects,
            SkillType type = SkillType.Passive, string grantedSpellId = null)
        {
            var skill = CreateAsset<SkillDefinition>($"{SkillFolder}/{id}.asset");
            var serialized = new SerializedObject(skill);
            serialized.FindProperty("_skillId").stringValue = id;
            serialized.FindProperty("_displayName").stringValue = displayName;
            serialized.FindProperty("_description").stringValue = description;
            serialized.FindProperty("_branch").enumValueIndex = (int)branch;
            serialized.FindProperty("_type").enumValueIndex = (int)type;
            serialized.FindProperty("_cost").intValue = cost;
            serialized.FindProperty("_maxRank").intValue = maxRank;
            serialized.FindProperty("_requiredLevel").intValue = requiredLevel;
            serialized.FindProperty("_position").vector2IntValue = position;
            serialized.FindProperty("_grantedSpellId").stringValue = grantedSpellId ?? string.Empty;

            var required = serialized.FindProperty("_prerequisites");
            required.arraySize = prerequisites?.Length ?? 0;
            for (var i = 0; i < required.arraySize; i++)
            {
                required.GetArrayElementAtIndex(i).stringValue = prerequisites[i];
            }

            var list = serialized.FindProperty("_effects");
            list.arraySize = effects?.Length ?? 0;
            for (var i = 0; i < list.arraySize; i++)
            {
                var element = list.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("_type").enumValueIndex = (int)effects[i].Type;
                element.FindPropertyRelative("_valuePerRank").floatValue = effects[i].ValuePerRank;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(skill);
            return skill;
        }

        /// <summary>The starting catalogue. Ids are lowercase and never change once shipped.</summary>
        private static ItemDatabase BuildItems()
        {
            EnsureFolder(ItemFolder);
            // Equipment first: every equippable item points at the asset that holds its numbers.
            var sword = BuildSword();
            var greatsword = BuildGreatsword();
            var hood = BuildArmor("traveler_hood", EquipmentSlot.Head, 2f, 1.5f, 0f);
            var coat = BuildArmor("traveler_coat", EquipmentSlot.Chest, 5f, 4f, 0f);
            var chestplate = BuildArmor("guard_chestplate", EquipmentSlot.Chest, 10f, 11f, 0.08f);
            var ringOfLife = BuildAccessory("ring_of_life", "Максимум здоров'я зростає.",
                new[] { new StatModifier(StatType.MaxHealth, ModifierOperation.Add, 10f) });
            var ringOfEndurance = BuildAccessory("ring_of_endurance",
                "Максимум витривалості зростає.",
                new[] { new StatModifier(StatType.MaxStamina, ModifierOperation.Add, 10f) });
            var talisman = BuildAccessory("iron_talisman", "Оглушити вас стає важче.",
                new[] { new StatModifier(StatType.StaggerResistance, ModifierOperation.Add, 0.1f) });
            var ringOfMight = BuildAccessory("ring_of_might", "Дає силу, наче чужу.",
                new[] { new StatModifier(StatType.Strength, ModifierOperation.Add, 3f) });

            var definitions = new System.Collections.Generic.List<ItemDefinition>
            {
                BuildItem("iron_scrap", "Уламок заліза",
                    "Уламок старого металу. Використовується для покращення спорядження.",
                    ItemCategory.Material, ItemRarity.Common, 99, false, false, 4,
                    new Color(0.62f, 0.64f, 0.68f)),
                // Half a flask (user's choice, 2026-09-27): eaten from the bag like a flask is drunk.
                BuildItem("healing_herb", "Лікувальна трава",
                    "Гірке листя, що затягує рани. Відновлює 20 здоров'я. Їсти треба стоячи, "
                    + "і удар до кінця ковтка його зводить нанівець.",
                    ItemCategory.Consumable, ItemRarity.Common, 20, false, false, 8,
                    new Color(0.45f, 0.75f, 0.42f), healAmount: 20f),
                BuildItem("guard_emblem", "Емблема варти",
                    "Знак старої королівської варти.",
                    ItemCategory.Material, ItemRarity.Uncommon, 99, false, false, 25,
                    new Color(0.72f, 0.7f, 0.85f)),
                BuildItem("guardian_core", "Ядро Стража",
                    "Ядро, залишене Стражем Гробниці.",
                    ItemCategory.Progression, ItemRarity.Legendary, 1, true, true, 0,
                    new Color(1f, 0.78f, 0.35f)),
                BuildItem("old_sword", "Старий Меч",
                    "Потертий меч вартового. Досі гострий.",
                    ItemCategory.Weapon, ItemRarity.Common, 1, true, true, 50,
                    new Color(0.8f, 0.8f, 0.86f), sword),
                BuildItem("iron_greatsword", "Залізний Дворучник",
                    "Важкий і повільний. Ламає щити, а не парирування.",
                    ItemCategory.Weapon, ItemRarity.Uncommon, 1, true, true, 180,
                    new Color(0.7f, 0.72f, 0.8f), greatsword),
                BuildItem("traveler_hood", "Каптур мандрівника",
                    "Просте сукно. Краще, ніж нічого.",
                    ItemCategory.Armor, ItemRarity.Common, 1, true, false, 30,
                    new Color(0.55f, 0.48f, 0.4f), hood),
                BuildItem("traveler_coat", "Плащ мандрівника",
                    "Потертий дорожній одяг із нашитими латками.",
                    ItemCategory.Armor, ItemRarity.Common, 1, true, false, 60,
                    new Color(0.5f, 0.44f, 0.38f), coat),
                BuildItem("guard_chestplate", "Кіраса варти",
                    "Важка сталь королівської варти. Захищає добре, важить відповідно.",
                    ItemCategory.Armor, ItemRarity.Uncommon, 1, true, false, 150,
                    new Color(0.6f, 0.62f, 0.7f), chestplate),
                BuildItem("ring_of_life", "Перстень Життя",
                    "Тепло, що тримає серце битися трохи довше.",
                    ItemCategory.Accessory, ItemRarity.Uncommon, 1, true, false, 120,
                    new Color(0.85f, 0.35f, 0.4f), ringOfLife),
                BuildItem("ring_of_endurance", "Перстень Витривалості",
                    "Дихається легше, ніж мало б.",
                    ItemCategory.Accessory, ItemRarity.Uncommon, 1, true, false, 120,
                    new Color(0.4f, 0.75f, 0.55f), ringOfEndurance),
                BuildItem("iron_talisman", "Залізний Талісман",
                    "Груба залізна бляха. Удари відчуваються глухіше.",
                    ItemCategory.Accessory, ItemRarity.Uncommon, 1, true, false, 140,
                    new Color(0.65f, 0.6f, 0.5f), talisman),
                BuildItem("ring_of_might", "Перстень Сили",
                    "Рука тримає важче, ніж має право. Знявши його, це відчуваєш одразу.",
                    ItemCategory.Accessory, ItemRarity.Rare, 1, true, false, 200,
                    new Color(0.78f, 0.45f, 0.3f), ringOfMight)
            };

            // The Withered Forest's own catalogue. Kept in its own file rather than grown here,
            // because a zone's loot belongs with the zone, not in an ever-longer shared list.
            definitions.AddRange(BuildForestItems());
            definitions.AddRange(BuildWorldItems());
            definitions.AddRange(BuildNpcItems());
            definitions.AddRange(BuildCapitalItems());
            definitions.AddRange(BuildMarshItems());

            var database = CreateAsset<ItemDatabase>($"{DataFolder}/ItemDatabase.asset");
            var serialized = new SerializedObject(database);
            var items = serialized.FindProperty("_items");
            items.arraySize = definitions.Count;
            for (var i = 0; i < definitions.Count; i++)
            {
                items.GetArrayElementAtIndex(i).objectReferenceValue = definitions[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
            return database;
        }

        private static ArmorDefinition BuildArmor(string id, EquipmentSlot slot, float defense,
            float weight, float staggerBonus)
        {
            var armor = CreateAsset<ArmorDefinition>($"{ItemFolder}/{id}_armor.asset");
            var serialized = new SerializedObject(armor);
            serialized.FindProperty("_itemId").stringValue = id;
            serialized.FindProperty("_slot").enumValueIndex = (int)slot;
            serialized.FindProperty("_defense").floatValue = defense;
            serialized.FindProperty("_weight").floatValue = weight;
            serialized.FindProperty("_staggerResistanceBonus").floatValue = staggerBonus;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(armor);
            return armor;
        }

        private static AccessoryDefinition BuildAccessory(string id, string effectText,
            StatModifier[] modifiers)
        {
            var accessory = CreateAsset<AccessoryDefinition>($"{ItemFolder}/{id}_accessory.asset");
            var serialized = new SerializedObject(accessory);
            serialized.FindProperty("_itemId").stringValue = id;
            serialized.FindProperty("_effectText").stringValue = effectText;
            var list = serialized.FindProperty("_modifiers");
            list.arraySize = modifiers.Length;
            for (var i = 0; i < modifiers.Length; i++)
            {
                var element = list.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("_stat").enumValueIndex = (int)modifiers[i].Stat;
                element.FindPropertyRelative("_operation").enumValueIndex =
                    (int)modifiers[i].Operation;
                element.FindPropertyRelative("_value").floatValue = modifiers[i].Value;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(accessory);
            return accessory;
        }

        private static ItemDefinition BuildItem(string id, string displayName, string description,
            ItemCategory category, ItemRarity rarity, int maxStack, bool isUnique,
            bool autosave, int value, Color color, EquipmentDefinition equipment = null,
            float healAmount = 0f)
        {
            var item = CreateAsset<ItemDefinition>($"{ItemFolder}/{id}.asset");
            var serialized = new SerializedObject(item);
            serialized.FindProperty("_itemId").stringValue = id;
            serialized.FindProperty("_displayName").stringValue = displayName;
            serialized.FindProperty("_description").stringValue = description;
            serialized.FindProperty("_category").enumValueIndex = (int)category;
            serialized.FindProperty("_rarity").enumValueIndex = (int)rarity;
            serialized.FindProperty("_maxStack").intValue = maxStack;
            serialized.FindProperty("_isUnique").boolValue = isUnique;
            serialized.FindProperty("_autosaveOnAcquire").boolValue = autosave;
            serialized.FindProperty("_baseValue").intValue = value;
            serialized.FindProperty("_pickupColor").colorValue = color;
            serialized.FindProperty("_equipment").objectReferenceValue = equipment;
            serialized.FindProperty("_healAmount").floatValue = healAmount;
            // A painted icon (Art/Items/Icons/<id>.png) wins over the vector silhouette by kind.
            var icon = ItemIcons.For(id);
            if (icon != null)
            {
                serialized.FindProperty("_icon").objectReferenceValue = icon;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
            return item;
        }

        private static DropTable BuildDropTable(string name, string json)
        {
            var table = CreateAsset<DropTable>($"{DataFolder}/{name}.asset");
            JsonUtility.FromJsonOverwrite(json, table);
            EditorUtility.SetDirty(table);
            return table;
        }
    }
}
