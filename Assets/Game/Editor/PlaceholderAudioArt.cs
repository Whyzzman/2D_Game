using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// Synthesises placeholder clips for every sound in the game.
    ///
    /// Not sound design. It exists because the questions this phase has to answer are structural —
    /// can you tell a block from a parry without looking, does the charge telegraph cut through a
    /// fight, does a respawn leave a loop running — and none of them can be answered against
    /// silence. Each family is built from a different synthesis recipe so that fire, frost, arcane
    /// and shadow are genuinely distinguishable rather than the same noise tinted differently.
    ///
    /// Everything is generated from a fixed seed, so a rebuild rewrites only what changed.
    /// </summary>
    public static class PlaceholderAudioArt
    {
        public const string Root = "Assets/Game/Audio";
        private const int Rate = 44100;

        /// <summary>One sound: which recipe, how many variants, and where it belongs.</summary>
        private readonly struct Spec
        {
            public readonly AudioId Id;
            public readonly string Folder;
            public readonly string Name;
            public readonly Recipe Kind;
            public readonly int Variants;
            public readonly float Seconds;
            public readonly float Tone;

            public Spec(AudioId id, string folder, string name, Recipe kind, int variants,
                float seconds, float tone)
            {
                Id = id;
                Folder = folder;
                Name = name;
                Kind = kind;
                Variants = variants;
                Seconds = seconds;
                Tone = tone;
            }
        }

        private enum Recipe
        {
            Whoosh,     // air moving — swings, dashes
            MetalHit,   // bright ring over a transient — blades on armour
            BodyHit,    // dull thud — flesh
            MetalRing,  // long clean ring — parry, block
            Crack,      // splintering — guard break, stone
            Boom,       // low falling sine — slams, phase changes
            Fire,       // crackle over a low rumble
            Frost,      // brittle high partials
            Arcane,     // clean harmonic chord
            Shadow,     // reverse swell into a low breath
            Step,       // short filtered click
            Rustle,     // cloth and dust
            Chime,      // progression cues
            Tick,       // interface
            Bed,        // long looping room tone
            Drone       // slow moving air
        }

        private static readonly Spec[] Catalogue =
        {
            // ---- player movement ----
            new Spec(AudioId.FootstepStone, "SFX/Player", "sfx_player_footstep_stone", Recipe.Step, 6, 0.16f, 1f),
            new Spec(AudioId.Land, "SFX/Player", "sfx_player_land", Recipe.BodyHit, 3, 0.3f, 0.8f),
            new Spec(AudioId.Jump, "SFX/Player", "sfx_player_jump", Recipe.Rustle, 3, 0.22f, 1.1f),
            new Spec(AudioId.DashGround, "SFX/Player", "sfx_player_dash_ground", Recipe.Whoosh, 3, 0.3f, 1.2f),
            new Spec(AudioId.DashAir, "SFX/Player", "sfx_player_dash_air", Recipe.Whoosh, 3, 0.3f, 1.5f),
            new Spec(AudioId.WallSlide, "SFX/Player", "sfx_player_wall_slide", Recipe.Rustle, 2, 0.4f, 0.7f),
            new Spec(AudioId.WallContact, "SFX/Player", "sfx_player_wall_contact", Recipe.Step, 3, 0.18f, 0.85f),

            // ---- weapons ----
            new Spec(AudioId.SwordSwingLight, "SFX/Weapons", "sfx_player_sword_swing_light", Recipe.Whoosh, 4, 0.26f, 1.6f),
            new Spec(AudioId.SwordSwingHeavy, "SFX/Weapons", "sfx_player_sword_swing_heavy", Recipe.Whoosh, 3, 0.38f, 1.1f),
            new Spec(AudioId.GreatswordSwingLight, "SFX/Weapons", "sfx_player_greatsword_swing_light", Recipe.Whoosh, 3, 0.44f, 0.72f),
            new Spec(AudioId.GreatswordSwingHeavy, "SFX/Weapons", "sfx_player_greatsword_swing_heavy", Recipe.Whoosh, 3, 0.6f, 0.52f),
            new Spec(AudioId.WeaponSwitch, "SFX/Weapons", "sfx_player_weapon_switch", Recipe.MetalHit, 3, 0.3f, 1.4f),

            // ---- impacts ----
            new Spec(AudioId.HitFlesh, "SFX/Weapons", "sfx_hit_flesh", Recipe.BodyHit, 4, 0.26f, 1f),
            new Spec(AudioId.HitArmor, "SFX/Weapons", "sfx_hit_armor", Recipe.MetalHit, 4, 0.34f, 1.15f),
            new Spec(AudioId.HitHeavy, "SFX/Weapons", "sfx_hit_heavy", Recipe.BodyHit, 3, 0.42f, 0.66f),
            new Spec(AudioId.HitCritical, "SFX/Weapons", "sfx_hit_critical", Recipe.MetalRing, 3, 0.5f, 1.9f),

            // ---- defence ----
            new Spec(AudioId.Block, "SFX/Weapons", "sfx_block", Recipe.MetalHit, 4, 0.3f, 0.95f),
            new Spec(AudioId.BlockShield, "SFX/Enemies", "sfx_guard_block_shield", Recipe.MetalRing, 3, 0.55f, 0.7f),
            new Spec(AudioId.Parry, "SFX/Weapons", "sfx_player_parry", Recipe.MetalRing, 3, 0.7f, 2.3f),
            new Spec(AudioId.GuardBreak, "SFX/Weapons", "sfx_guard_break", Recipe.Crack, 3, 0.8f, 0.8f),
            new Spec(AudioId.StaggerBreak, "SFX/Weapons", "sfx_stagger_break", Recipe.Crack, 3, 0.6f, 0.55f),

            // ---- the player being hurt ----
            new Spec(AudioId.PlayerHurt, "SFX/Player", "sfx_player_hurt", Recipe.BodyHit, 4, 0.3f, 1.2f),
            new Spec(AudioId.PlayerHurtHeavy, "SFX/Player", "sfx_player_hurt_heavy", Recipe.BodyHit, 3, 0.45f, 0.8f),
            new Spec(AudioId.PlayerDeath, "SFX/Player", "sfx_player_death", Recipe.Shadow, 1, 1.5f, 0.6f),

            // ---- skills ----
            new Spec(AudioId.GroundBreaker, "SFX/Player", "sfx_skill_ground_breaker", Recipe.Boom, 2, 1.0f, 0.75f),
            new Spec(AudioId.DashStrike, "SFX/Player", "sfx_skill_dash_strike", Recipe.Whoosh, 3, 0.3f, 1.8f),
            new Spec(AudioId.PerfectDodge, "SFX/Player", "sfx_perfect_dodge", Recipe.Whoosh, 2, 0.24f, 2.1f),

            // ---- magic ----
            new Spec(AudioId.FireCast, "SFX/Magic", "sfx_spell_fire_cast", Recipe.Fire, 3, 0.45f, 1.1f),
            new Spec(AudioId.FireImpact, "SFX/Magic", "sfx_spell_fire_impact", Recipe.Fire, 3, 0.6f, 0.85f),
            new Spec(AudioId.FrostCast, "SFX/Magic", "sfx_spell_frost_cast", Recipe.Frost, 3, 0.5f, 1.2f),
            new Spec(AudioId.FrostImpact, "SFX/Magic", "sfx_spell_frost_impact", Recipe.Frost, 3, 0.55f, 1f),
            new Spec(AudioId.ArcaneCast, "SFX/Magic", "sfx_spell_arcane_cast", Recipe.Arcane, 2, 0.7f, 1f),
            new Spec(AudioId.ArcaneBurst, "SFX/Magic", "sfx_spell_arcane_burst", Recipe.Arcane, 2, 1.0f, 0.7f),
            new Spec(AudioId.ManaShieldUp, "SFX/Magic", "sfx_spell_mana_shield_up", Recipe.Arcane, 1, 0.8f, 1.3f),
            new Spec(AudioId.ManaShieldHit, "SFX/Magic", "sfx_spell_mana_shield_hit", Recipe.Arcane, 3, 0.35f, 1.7f),
            new Spec(AudioId.CastFailed, "SFX/Magic", "sfx_spell_failed", Recipe.Tick, 2, 0.22f, 0.55f),

            // ---- shadow ----
            new Spec(AudioId.ShadowDashStart, "SFX/Shadow", "sfx_shadow_dash_start", Recipe.Shadow, 3, 0.4f, 1.2f),
            new Spec(AudioId.ShadowDashEnd, "SFX/Shadow", "sfx_shadow_dash_end", Recipe.Shadow, 3, 0.35f, 1.5f),
            new Spec(AudioId.ShadowStrike, "SFX/Shadow", "sfx_shadow_strike", Recipe.Shadow, 3, 0.5f, 0.9f),
            new Spec(AudioId.ShadowImpact, "SFX/Shadow", "sfx_shadow_impact", Recipe.Shadow, 3, 0.45f, 0.75f),

            // ---- statuses ----
            new Spec(AudioId.BleedProc, "SFX/Status", "sfx_status_bleed_proc", Recipe.BodyHit, 3, 0.3f, 1.4f),
            new Spec(AudioId.PoisonProc, "SFX/Status", "sfx_status_poison_proc", Recipe.Rustle, 3, 0.45f, 0.6f),
            new Spec(AudioId.BurnProc, "SFX/Status", "sfx_status_burn_proc", Recipe.Fire, 3, 0.5f, 1f),
            new Spec(AudioId.FrostProc, "SFX/Status", "sfx_status_frost_proc", Recipe.Frost, 3, 0.5f, 0.85f),

            // ---- zombie ----
            new Spec(AudioId.ZombieStep, "SFX/Enemies", "sfx_enemy_zombie_step", Recipe.Step, 4, 0.2f, 0.85f),
            new Spec(AudioId.ZombieAlert, "SFX/Enemies", "sfx_enemy_zombie_alert", Recipe.Rustle, 3, 0.5f, 0.55f),
            new Spec(AudioId.ZombieSwing, "SFX/Enemies", "sfx_enemy_zombie_swing", Recipe.Whoosh, 3, 0.4f, 0.95f),
            new Spec(AudioId.ZombieHurt, "SFX/Enemies", "sfx_enemy_zombie_hurt", Recipe.BodyHit, 4, 0.3f, 0.7f),
            new Spec(AudioId.ZombieDeath, "SFX/Enemies", "sfx_enemy_zombie_death", Recipe.Crack, 2, 0.9f, 0.6f),

            // ---- shield guard ----
            new Spec(AudioId.GuardStep, "SFX/Enemies", "sfx_enemy_guard_step", Recipe.Step, 4, 0.24f, 0.6f),
            new Spec(AudioId.GuardRaise, "SFX/Enemies", "sfx_enemy_guard_raise", Recipe.MetalHit, 3, 0.35f, 0.8f),
            new Spec(AudioId.GuardBash, "SFX/Enemies", "sfx_enemy_guard_bash", Recipe.MetalHit, 3, 0.45f, 0.62f),
            new Spec(AudioId.GuardHeavy, "SFX/Enemies", "sfx_enemy_guard_heavy", Recipe.Whoosh, 3, 0.55f, 0.6f),
            new Spec(AudioId.GuardHurt, "SFX/Enemies", "sfx_enemy_guard_hurt", Recipe.MetalHit, 3, 0.3f, 0.75f),
            new Spec(AudioId.GuardDeath, "SFX/Enemies", "sfx_enemy_guard_death", Recipe.Crack, 2, 1.1f, 0.45f),

            // ---- guardian ----
            new Spec(AudioId.BossAwaken, "SFX/Boss", "sfx_boss_guardian_awaken", Recipe.Arcane, 1, 2.0f, 0.5f),
            new Spec(AudioId.BossStep, "SFX/Boss", "sfx_boss_guardian_step", Recipe.Boom, 4, 0.5f, 1.3f),
            new Spec(AudioId.BossSlash, "SFX/Boss", "sfx_boss_guardian_slash", Recipe.Whoosh, 3, 0.6f, 0.55f),
            new Spec(AudioId.BossBash, "SFX/Boss", "sfx_boss_guardian_bash", Recipe.MetalHit, 3, 0.55f, 0.5f),
            new Spec(AudioId.BossChargeStart, "SFX/Boss", "sfx_boss_guardian_charge_start", Recipe.Drone, 1, 1.1f, 0.65f),
            new Spec(AudioId.BossCharge, "SFX/Boss", "sfx_boss_guardian_charge", Recipe.Whoosh, 2, 0.8f, 0.45f),
            new Spec(AudioId.BossCombo, "SFX/Boss", "sfx_boss_guardian_combo", Recipe.Whoosh, 3, 0.5f, 0.7f),
            new Spec(AudioId.BossPhase, "SFX/Boss", "sfx_boss_guardian_phase", Recipe.Shadow, 1, 2.2f, 0.5f),
            new Spec(AudioId.BossJump, "SFX/Boss", "sfx_boss_guardian_jump", Recipe.Whoosh, 2, 0.5f, 0.6f),
            new Spec(AudioId.BossSlam, "SFX/Boss", "sfx_boss_guardian_slam", Recipe.Boom, 2, 1.6f, 0.5f),
            new Spec(AudioId.BossStunned, "SFX/Boss", "sfx_boss_guardian_stunned", Recipe.Crack, 2, 0.9f, 0.4f),
            new Spec(AudioId.BossDeath, "SFX/Boss", "sfx_boss_guardian_death", Recipe.Shadow, 1, 2.6f, 0.42f),

            // ---- world ----
            new Spec(AudioId.CheckpointActivate, "SFX/World", "sfx_checkpoint_activate", Recipe.Chime, 1, 1.4f, 1f),
            new Spec(AudioId.CheckpointRest, "SFX/World", "sfx_checkpoint_rest", Recipe.Chime, 1, 1.2f, 0.72f),
            new Spec(AudioId.Respawn, "SFX/World", "sfx_respawn", Recipe.Chime, 1, 1.0f, 0.85f),
            new Spec(AudioId.PickupCommon, "SFX/World", "sfx_pickup_common", Recipe.Tick, 3, 0.25f, 1.6f),
            new Spec(AudioId.PickupRare, "SFX/World", "sfx_pickup_rare", Recipe.Chime, 2, 0.7f, 1.4f),
            new Spec(AudioId.PickupCurrency, "SFX/World", "sfx_pickup_currency", Recipe.Tick, 3, 0.22f, 2.2f),
            new Spec(AudioId.LevelUp, "SFX/World", "sfx_level_up", Recipe.Chime, 1, 1.5f, 1.2f),
            new Spec(AudioId.SkillUnlock, "SFX/World", "sfx_skill_unlock", Recipe.Chime, 1, 1.1f, 1.05f),

            // ---- interface ----
            new Spec(AudioId.UiNavigate, "UI", "ui_navigate", Recipe.Tick, 3, 0.1f, 1.8f),
            new Spec(AudioId.UiConfirm, "UI", "ui_confirm", Recipe.Tick, 1, 0.2f, 1.3f),
            new Spec(AudioId.UiCancel, "UI", "ui_cancel", Recipe.Tick, 1, 0.22f, 0.8f),
            new Spec(AudioId.UiOpen, "UI", "ui_open", Recipe.Rustle, 1, 0.3f, 1.2f),
            new Spec(AudioId.UiClose, "UI", "ui_close", Recipe.Rustle, 1, 0.28f, 0.9f),
            new Spec(AudioId.UiError, "UI", "ui_error", Recipe.Tick, 1, 0.26f, 0.5f),
            new Spec(AudioId.UiSaveFailed, "UI", "ui_save_failed", Recipe.Tick, 1, 0.5f, 0.45f),

            // ---- ambience ----
            new Spec(AudioId.AmbienceTombBase, "Ambience/Tomb", "amb_tomb_base", Recipe.Bed, 1, 8f, 1f),
            new Spec(AudioId.AmbienceArenaBase, "Ambience/Tomb", "amb_tomb_arena", Recipe.Bed, 1, 8f, 0.7f),
            new Spec(AudioId.AmbienceStoneSettle, "Ambience/Tomb", "amb_tomb_stone", Recipe.Crack, 3, 0.9f, 0.5f),
            new Spec(AudioId.AmbienceChain, "Ambience/Tomb", "amb_tomb_chain", Recipe.MetalHit, 3, 0.8f, 1.5f),
            new Spec(AudioId.AmbienceDrip, "Ambience/Tomb", "amb_tomb_drip", Recipe.Tick, 3, 0.4f, 2.4f),
            new Spec(AudioId.AmbienceWind, "Ambience/Tomb", "amb_tomb_wind", Recipe.Drone, 2, 3.2f, 1f),

            // ---- corrupted hound ----
            // Everything about it is lower and wetter than a soldier: a padded step instead of a
            // boot, a growl under the swing, and a bite that lands as flesh rather than as metal.
            new Spec(AudioId.HoundStep, "SFX/Enemies", "sfx_hound_step", Recipe.Step, 5, 0.12f, 0.7f),
            new Spec(AudioId.HoundGrowl, "SFX/Enemies", "sfx_hound_growl", Recipe.Shadow, 3, 0.55f, 0.45f),
            new Spec(AudioId.HoundBite, "SFX/Enemies", "sfx_hound_bite", Recipe.BodyHit, 4, 0.2f, 1.3f),
            new Spec(AudioId.HoundLeap, "SFX/Enemies", "sfx_hound_leap", Recipe.Whoosh, 3, 0.34f, 1.35f),
            new Spec(AudioId.HoundHurt, "SFX/Enemies", "sfx_hound_hurt", Recipe.BodyHit, 4, 0.26f, 1.5f),
            new Spec(AudioId.HoundDeath, "SFX/Enemies", "sfx_hound_death", Recipe.Shadow, 2, 0.9f, 0.55f),

            // ---- forest archer ----
            // The draw is the sound that matters: long, quiet and creaking, so it can be heard
            // through a fight and reacted to rather than only seen.
            new Spec(AudioId.ArcherStep, "SFX/Enemies", "sfx_archer_step", Recipe.Step, 4, 0.14f, 1.05f),
            new Spec(AudioId.ArcherDraw, "SFX/Enemies", "sfx_archer_draw", Recipe.Rustle, 3, 0.55f, 0.8f),
            new Spec(AudioId.ArcherRelease, "SFX/Enemies", "sfx_archer_release", Recipe.Whoosh, 4, 0.2f, 2.1f),
            new Spec(AudioId.ArrowImpact, "SFX/Enemies", "sfx_arrow_impact", Recipe.MetalHit, 4, 0.2f, 1.7f),
            new Spec(AudioId.ArcherHurt, "SFX/Enemies", "sfx_archer_hurt", Recipe.BodyHit, 3, 0.28f, 1.25f),
            new Spec(AudioId.ArcherDeath, "SFX/Enemies", "sfx_archer_death", Recipe.BodyHit, 2, 0.7f, 0.7f),

            // ---- root shaman ----
            new Spec(AudioId.ShamanStep, "SFX/Enemies", "sfx_shaman_step", Recipe.Rustle, 4, 0.2f, 0.75f),
            new Spec(AudioId.ShamanCast, "SFX/Enemies", "sfx_shaman_cast", Recipe.Arcane, 3, 0.75f, 0.65f),
            new Spec(AudioId.ShamanOrb, "SFX/Enemies", "sfx_shaman_orb", Recipe.Shadow, 3, 0.45f, 0.9f),
            new Spec(AudioId.ShamanZone, "SFX/Enemies", "sfx_shaman_zone", Recipe.Fire, 2, 0.9f, 0.6f),
            new Spec(AudioId.ShamanHurt, "SFX/Enemies", "sfx_shaman_hurt", Recipe.BodyHit, 3, 0.3f, 1.1f),
            new Spec(AudioId.ShamanDeath, "SFX/Enemies", "sfx_shaman_death", Recipe.Shadow, 2, 1.1f, 0.5f),

            // ---- hollow warden ----
            // Lighter and faster than the Guardian's set, and made of wood rather than of iron:
            // the two bosses must not be mistaken for each other with eyes closed.
            new Spec(AudioId.WardenAwaken, "SFX/Boss", "sfx_warden_awaken", Recipe.Shadow, 1, 2.2f, 0.42f),
            new Spec(AudioId.WardenStep, "SFX/Boss", "sfx_warden_step", Recipe.BodyHit, 4, 0.26f, 0.5f),
            new Spec(AudioId.WardenGlaive, "SFX/Boss", "sfx_warden_glaive", Recipe.Whoosh, 4, 0.42f, 0.85f),
            new Spec(AudioId.WardenThrust, "SFX/Boss", "sfx_warden_thrust", Recipe.Whoosh, 3, 0.38f, 1.5f),
            new Spec(AudioId.WardenSweep, "SFX/Boss", "sfx_warden_sweep", Recipe.Whoosh, 3, 0.5f, 0.62f),
            new Spec(AudioId.WardenRootShot, "SFX/Boss", "sfx_warden_root_shot", Recipe.Shadow, 3, 0.5f, 0.75f),
            new Spec(AudioId.WardenRootSlam, "SFX/Boss", "sfx_warden_root_slam", Recipe.Boom, 2, 1.1f, 0.6f),
            new Spec(AudioId.WardenPoisonField, "SFX/Boss", "sfx_warden_poison_field", Recipe.Fire, 2, 1.0f, 0.55f),
            new Spec(AudioId.WardenLeap, "SFX/Boss", "sfx_warden_leap", Recipe.Whoosh, 2, 0.45f, 0.9f),
            new Spec(AudioId.WardenPhase, "SFX/Boss", "sfx_warden_phase", Recipe.Boom, 1, 2.0f, 0.5f),
            new Spec(AudioId.WardenStunned, "SFX/Boss", "sfx_warden_stunned", Recipe.Crack, 2, 0.7f, 0.5f),
            new Spec(AudioId.WardenDeath, "SFX/Boss", "sfx_warden_death", Recipe.Shadow, 1, 2.6f, 0.4f),

            // ---- forest world ----
            new Spec(AudioId.HazardTelegraph, "SFX/World", "sfx_hazard_telegraph", Recipe.Rustle, 3, 0.4f, 1.3f),
            new Spec(AudioId.HazardBurst, "SFX/World", "sfx_hazard_burst", Recipe.Fire, 3, 0.5f, 1.05f),
            new Spec(AudioId.GateOpen, "SFX/World", "sfx_gate_open", Recipe.Crack, 2, 1.3f, 0.45f),
            new Spec(AudioId.DoubleJump, "SFX/Player", "sfx_player_double_jump", Recipe.Whoosh, 3, 0.26f, 1.9f),
            new Spec(AudioId.AbilityUnlocked, "SFX/World", "sfx_ability_unlocked", Recipe.Chime, 1, 1.6f, 1.1f),

            // ---- forest ambience ----
            // Wind and wet leaves where the Tomb had stone and dripping water. The deep bed is the
            // same room with a corruption hum under it, so going further in is audible.
            new Spec(AudioId.AmbienceForestBase, "Ambience/Forest", "amb_forest_base", Recipe.Bed, 1, 8f, 0.85f),
            new Spec(AudioId.AmbienceDeepForestBase, "Ambience/Forest", "amb_forest_deep_base", Recipe.Bed, 1, 8f, 0.55f),
            new Spec(AudioId.AmbienceLeaves, "Ambience/Forest", "amb_forest_leaves", Recipe.Rustle, 4, 0.9f, 1.2f),
            new Spec(AudioId.AmbienceBranch, "Ambience/Forest", "amb_forest_branch", Recipe.Crack, 3, 0.6f, 0.75f),
            new Spec(AudioId.AmbienceInsects, "Ambience/Forest", "amb_forest_insects", Recipe.Tick, 3, 0.8f, 2.2f),
            new Spec(AudioId.AmbienceDistantBeast, "Ambience/Forest", "amb_forest_beast", Recipe.Shadow, 2, 1.8f, 0.48f),
            new Spec(AudioId.AmbienceCorruptionHum, "Ambience/Forest", "amb_forest_corruption", Recipe.Drone, 1, 6f, 0.62f),

            // ---- healing ----
            new Spec(AudioId.FlaskDrink, "SFX/Player", "sfx_player_flask_drink", Recipe.Chime, 2, 0.35f, 1.5f),
            new Spec(AudioId.HerbEat, "SFX/Player", "sfx_player_herb_eat", Recipe.Rustle, 2, 0.4f, 0.9f)
        };

        /// <summary>Writes every missing or changed clip and returns them, by sound.</summary>
        public static Dictionary<AudioId, AudioClip[]> Generate()
        {
            var result = new Dictionary<AudioId, AudioClip[]>();
            foreach (var spec in Catalogue)
            {
                var folder = $"{Root}/{spec.Folder}";
                EnsureFolder(folder);
                var clips = new AudioClip[spec.Variants];
                for (var i = 0; i < spec.Variants; i++)
                {
                    var path = $"{folder}/{spec.Name}_{i + 1:00}.wav";
                    var seed = spec.Name.GetHashCode() ^ (i * 7919);
                    WriteIfChanged(path, Render(spec, seed));
                    clips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    if (clips[i] == null)
                    {
                        Debug.LogError($"Placeholder clip failed to import: {path}");
                    }
                }

                result[spec.Id] = clips;
            }

            return result;
        }

        /// <summary>The catalogue, for the content build and the asset list.</summary>
        public static IEnumerable<(AudioId id, string folder, string name, int variants)> Entries()
        {
            foreach (var spec in Catalogue)
            {
                yield return (spec.Id, spec.Folder, spec.Name, spec.Variants);
            }
        }

        // ---------- synthesis ----------

        private static byte[] Render(Spec spec, int seed)
        {
            var random = new System.Random(seed);
            var count = Mathf.Max(64, Mathf.RoundToInt(spec.Seconds * Rate));
            var samples = new float[count];

            switch (spec.Kind)
            {
                case Recipe.Whoosh: Whoosh(samples, random, spec.Tone); break;
                case Recipe.MetalHit: MetalHit(samples, random, spec.Tone); break;
                case Recipe.BodyHit: BodyHit(samples, random, spec.Tone); break;
                case Recipe.MetalRing: MetalRing(samples, random, spec.Tone); break;
                case Recipe.Crack: Crack(samples, random, spec.Tone); break;
                case Recipe.Boom: Boom(samples, random, spec.Tone); break;
                case Recipe.Fire: Fire(samples, random, spec.Tone); break;
                case Recipe.Frost: Frost(samples, random, spec.Tone); break;
                case Recipe.Arcane: Arcane(samples, random, spec.Tone); break;
                case Recipe.Shadow: Shadow(samples, random, spec.Tone); break;
                case Recipe.Step: Step(samples, random, spec.Tone); break;
                case Recipe.Rustle: Rustle(samples, random, spec.Tone); break;
                case Recipe.Chime: Chime(samples, random, spec.Tone); break;
                case Recipe.Tick: Tick(samples, random, spec.Tone); break;
                case Recipe.Bed: Bed(samples, random, spec.Tone); break;
                case Recipe.Drone: Drone(samples, random, spec.Tone); break;
            }

            Normalise(samples, spec.Kind == Recipe.Bed ? 0.16f : 0.82f);
            // Loops are cross-faded end to start so there is no click at the seam.
            if (spec.Kind == Recipe.Bed)
            {
                CrossfadeLoop(samples, Rate / 2);
            }

            return Wav(samples);
        }

        /// <summary>Air moving past a blade: band-passed noise that sweeps and falls away.</summary>
        private static void Whoosh(float[] samples, System.Random random, float tone)
        {
            var low = new OnePole();
            var high = new OnePole();
            for (var i = 0; i < samples.Length; i++)
            {
                var t = i / (float)samples.Length;
                var body = Mathf.Sin(t * Mathf.PI); // swells then goes
                var noise = (float)(random.NextDouble() * 2.0 - 1.0);
                // The sweep is what makes it a swing rather than a hiss.
                var cutoff = Mathf.Lerp(600f, 4200f, Mathf.Sin(t * Mathf.PI)) * tone;
                var filtered = low.Low(noise, cutoff) - high.Low(noise, 180f * tone);
                samples[i] = filtered * body * body;
            }
        }

        /// <summary>Blade on plate: a hard transient with a short metallic ring over it.</summary>
        private static void MetalHit(float[] samples, System.Random random, float tone)
        {
            var partials = new[] { 1f, 2.76f, 5.4f, 8.9f };
            var baseFrequency = 440f * tone;
            for (var i = 0; i < samples.Length; i++)
            {
                var t = i / (float)Rate;
                var decay = Mathf.Exp(-t * 16f);
                var value = 0f;
                for (var p = 0; p < partials.Length; p++)
                {
                    value += Mathf.Sin(2f * Mathf.PI * baseFrequency * partials[p] * t)
                        * Mathf.Exp(-t * (9f + p * 7f)) / (p + 1.6f);
                }

                var transient = (float)(random.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-t * 180f);
                samples[i] = value * 0.7f + transient * 0.9f;
                samples[i] *= decay + 0.08f;
            }
        }

        /// <summary>Flesh: a dull low thud with almost no ring.</summary>
        private static void BodyHit(float[] samples, System.Random random, float tone)
        {
            var low = new OnePole();
            for (var i = 0; i < samples.Length; i++)
            {
                var t = i / (float)Rate;
                var noise = (float)(random.NextDouble() * 2.0 - 1.0);
                var thump = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(150f, 60f, t * 6f) * tone * t);
                samples[i] = (low.Low(noise, 420f * tone) * 0.8f + thump * 0.6f)
                    * Mathf.Exp(-t * 22f);
            }
        }

        /// <summary>
        /// The parry: a long, bright, clean ring. Deliberately the most tonal sound in the game so
        /// it cannot be confused with the duller block.
        /// </summary>
        private static void MetalRing(float[] samples, System.Random random, float tone)
        {
            var partials = new[] { 1f, 2.4f, 3.9f, 6.1f, 9.3f };
            var baseFrequency = 620f * tone;
            for (var i = 0; i < samples.Length; i++)
            {
                var t = i / (float)Rate;
                var value = 0f;
                for (var p = 0; p < partials.Length; p++)
                {
                    value += Mathf.Sin(2f * Mathf.PI * baseFrequency * partials[p] * t)
                        * Mathf.Exp(-t * (3.5f + p * 2.2f)) / (p + 1.2f);
                }

                var transient = (float)(random.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-t * 220f);
                samples[i] = value * 0.85f + transient * 0.55f;
            }
        }

        /// <summary>Something giving way: a burst of short splinters rather than one hit.</summary>
        private static void Crack(float[] samples, System.Random random, float tone)
        {
            var low = new OnePole();
            var shards = 14;
            for (var s = 0; s < shards; s++)
            {
                var at = (int)(Mathf.Pow((float)random.NextDouble(), 1.7f) * samples.Length * 0.6f);
                var length = Rate / 40;
                var pitch = Mathf.Lerp(200f, 1400f, (float)random.NextDouble()) * tone;
                for (var i = 0; i < length && at + i < samples.Length; i++)
                {
                    var t = i / (float)Rate;
                    samples[at + i] += Mathf.Sin(2f * Mathf.PI * pitch * t)
                        * Mathf.Exp(-t * 90f) * 0.5f;
                }
            }

            for (var i = 0; i < samples.Length; i++)
            {
                var t = i / (float)Rate;
                var noise = (float)(random.NextDouble() * 2.0 - 1.0);
                samples[i] += low.Low(noise, 900f * tone) * Mathf.Exp(-t * 6f) * 0.45f;
            }
        }

        /// <summary>The slam: a falling sine with weight under it. The biggest sound in the game.</summary>
        private static void Boom(float[] samples, System.Random random, float tone)
        {
            var low = new OnePole();
            var phase = 0f;
            for (var i = 0; i < samples.Length; i++)
            {
                var t = i / (float)Rate;
                var progress = i / (float)samples.Length;
                // The drop in pitch is what reads as mass hitting the ground.
                var frequency = Mathf.Lerp(110f, 34f, Mathf.Pow(progress, 0.35f)) * tone;
                phase += 2f * Mathf.PI * frequency / Rate;
                var body = Mathf.Sin(phase) * Mathf.Exp(-t * 3.2f);
                var noise = (float)(random.NextDouble() * 2.0 - 1.0);
                var debris = low.Low(noise, 700f) * Mathf.Exp(-t * 9f) * 0.4f;
                samples[i] = body * 0.9f + debris;
            }
        }

        /// <summary>Fire: irregular crackle riding on a low combustion rumble.</summary>
        private static void Fire(float[] samples, System.Random random, float tone)
        {
            var low = new OnePole();
            var band = new OnePole();
            for (var i = 0; i < samples.Length; i++)
            {
                var t = i / (float)Rate;
                var progress = i / (float)samples.Length;
                var noise = (float)(random.NextDouble() * 2.0 - 1.0);
                var rumble = low.Low(noise, 220f) * Mathf.Exp(-t * 3f) * 0.8f;
                var air = band.Low(noise, 2600f * tone) * Mathf.Sin(progress * Mathf.PI) * 0.35f;
                // Crackle: sparse sharp spikes, which is what separates fire from wind.
                var crackle = random.NextDouble() < 0.004
                    ? (float)(random.NextDouble() * 2.0 - 1.0) * 0.7f
                    : 0f;
                samples[i] = rumble + air + crackle * Mathf.Exp(-t * 2f);
            }
        }

        /// <summary>Frost: brittle high partials and a cold hiss. The opposite of fire.</summary>
        private static void Frost(float[] samples, System.Random random, float tone)
        {
            var high = new OnePole();
            var partials = new[] { 1f, 1.9f, 3.1f, 4.7f, 7.2f };
            var baseFrequency = 1500f * tone;
            for (var i = 0; i < samples.Length; i++)
            {
                var t = i / (float)Rate;
                var value = 0f;
                for (var p = 0; p < partials.Length; p++)
                {
                    value += Mathf.Sin(2f * Mathf.PI * baseFrequency * partials[p] * t)
                        * Mathf.Exp(-t * (7f + p * 4f)) / (p + 2f);
                }

                var noise = (float)(random.NextDouble() * 2.0 - 1.0);
                var hiss = (noise - high.Low(noise, 1800f)) * Mathf.Exp(-t * 5f) * 0.4f;
                samples[i] = value * 0.8f + hiss;
            }
        }

        /// <summary>
        /// Arcane: a clean harmonic chord that swells. Tonal and ordered, where Shadow is airy and
        /// reversed — the two must never be mistaken for each other.
        /// </summary>
        private static void Arcane(float[] samples, System.Random random, float tone)
        {
            var intervals = new[] { 1f, 1.5f, 2f, 3f };
            var baseFrequency = 220f * tone;
            for (var i = 0; i < samples.Length; i++)
            {
                var t = i / (float)Rate;
                var progress = i / (float)samples.Length;
                var swell = Mathf.Sin(progress * Mathf.PI);
                var value = 0f;
                for (var n = 0; n < intervals.Length; n++)
                {
                    value += Mathf.Sin(2f * Mathf.PI * baseFrequency * intervals[n] * t)
                        / (n + 1.5f);
                }

                // A slow shimmer keeps it from sounding like a test tone.
                var shimmer = 1f + 0.12f * Mathf.Sin(2f * Mathf.PI * 5.5f * t);
                samples[i] = value * swell * shimmer * 0.55f;
            }
        }

        /// <summary>
        /// Shadow: a reversed swell into a low breath. Built backwards on purpose — the rising
        /// tail is what makes it feel wrong in a way no other family does.
        /// </summary>
        private static void Shadow(float[] samples, System.Random random, float tone)
        {
            var low = new OnePole();
            var temporary = new float[samples.Length];
            for (var i = 0; i < samples.Length; i++)
            {
                var t = i / (float)Rate;
                var noise = (float)(random.NextDouble() * 2.0 - 1.0);
                var breath = low.Low(noise, 700f * tone);
                var body = Mathf.Sin(2f * Mathf.PI * 70f * tone * t) * 0.5f;
                temporary[i] = (breath * 0.9f + body) * Mathf.Exp(-t * 2.4f);
            }

            for (var i = 0; i < samples.Length; i++)
            {
                samples[i] = temporary[samples.Length - 1 - i];
            }

            // A short forward thump at the end, so it resolves instead of just stopping.
            var tail = Mathf.Min(samples.Length, Rate / 6);
            for (var i = 0; i < tail; i++)
            {
                var t = i / (float)Rate;
                var at = samples.Length - tail + i;
                samples[at] += Mathf.Sin(2f * Mathf.PI * 55f * tone * t) * Mathf.Exp(-t * 18f) * 0.8f;
            }
        }

        private static void Step(float[] samples, System.Random random, float tone)
        {
            var low = new OnePole();
            for (var i = 0; i < samples.Length; i++)
            {
                var t = i / (float)Rate;
                var noise = (float)(random.NextDouble() * 2.0 - 1.0);
                samples[i] = low.Low(noise, 1200f * tone) * Mathf.Exp(-t * 45f);
                samples[i] += Mathf.Sin(2f * Mathf.PI * 90f * tone * t) * Mathf.Exp(-t * 60f) * 0.4f;
            }
        }

        private static void Rustle(float[] samples, System.Random random, float tone)
        {
            var low = new OnePole();
            for (var i = 0; i < samples.Length; i++)
            {
                var t = i / (float)Rate;
                var progress = i / (float)samples.Length;
                var noise = (float)(random.NextDouble() * 2.0 - 1.0);
                samples[i] = low.Low(noise, 2200f * tone) * Mathf.Sin(progress * Mathf.PI) * 0.7f;
            }
        }

        /// <summary>Progression cues: a warm rising interval. Never used for combat.</summary>
        private static void Chime(float[] samples, System.Random random, float tone)
        {
            var steps = new[] { 1f, 1.25f, 1.5f };
            var baseFrequency = 330f * tone;
            for (var i = 0; i < samples.Length; i++)
            {
                var t = i / (float)Rate;
                var value = 0f;
                for (var n = 0; n < steps.Length; n++)
                {
                    var start = n * 0.11f;
                    if (t < start)
                    {
                        continue;
                    }

                    var local = t - start;
                    value += Mathf.Sin(2f * Mathf.PI * baseFrequency * steps[n] * local)
                        * Mathf.Exp(-local * 3.2f) / (n + 1.4f);
                }

                samples[i] = value * 0.7f;
            }
        }

        private static void Tick(float[] samples, System.Random random, float tone)
        {
            for (var i = 0; i < samples.Length; i++)
            {
                var t = i / (float)Rate;
                samples[i] = Mathf.Sin(2f * Mathf.PI * 900f * tone * t) * Mathf.Exp(-t * 55f);
                samples[i] += (float)(random.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-t * 320f) * 0.4f;
            }
        }

        /// <summary>
        /// The room. Very low, very slow, and almost nothing happening — a tomb should feel empty,
        /// so this sits far under everything else and never carries information.
        /// </summary>
        private static void Bed(float[] samples, System.Random random, float tone)
        {
            var low = new OnePole();
            var slow = new OnePole();
            for (var i = 0; i < samples.Length; i++)
            {
                var t = i / (float)Rate;
                var noise = (float)(random.NextDouble() * 2.0 - 1.0);
                var air = low.Low(noise, 190f * tone);
                var breathing = 1f + 0.35f * Mathf.Sin(2f * Mathf.PI * 0.07f * t);
                var deep = Mathf.Sin(2f * Mathf.PI * 41f * tone * t) * 0.18f;
                samples[i] = slow.Low(air * breathing + deep, 260f);
            }
        }

        private static void Drone(float[] samples, System.Random random, float tone)
        {
            var low = new OnePole();
            for (var i = 0; i < samples.Length; i++)
            {
                var t = i / (float)Rate;
                var progress = i / (float)samples.Length;
                var noise = (float)(random.NextDouble() * 2.0 - 1.0);
                var sweep = Mathf.Lerp(300f, 1100f, Mathf.Sin(progress * Mathf.PI)) * tone;
                // Rising, so it reads as something building rather than something ending.
                var rise = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(50f, 130f, progress) * tone * t);
                samples[i] = (low.Low(noise, sweep) * 0.75f + rise * 0.35f)
                    * Mathf.Sin(progress * Mathf.PI);
            }
        }

        /// <summary>A one-pole low-pass. Crude, stable, and enough to shape noise into a material.</summary>
        private sealed class OnePole
        {
            private float _z;

            public float Low(float input, float cutoff)
            {
                var a = Mathf.Clamp01(1f - Mathf.Exp(-2f * Mathf.PI * cutoff / Rate));
                _z += a * (input - _z);
                return _z;
            }
        }

        private static void Normalise(float[] samples, float peak)
        {
            var maximum = 0.0001f;
            foreach (var sample in samples)
            {
                maximum = Mathf.Max(maximum, Mathf.Abs(sample));
            }

            var scale = peak / maximum;
            for (var i = 0; i < samples.Length; i++)
            {
                samples[i] *= scale;
            }
        }

        /// <summary>Blends the tail into the head so a looping bed has no click at the seam.</summary>
        private static void CrossfadeLoop(float[] samples, int fade)
        {
            fade = Mathf.Min(fade, samples.Length / 3);
            for (var i = 0; i < fade; i++)
            {
                var t = i / (float)fade;
                var tail = samples[samples.Length - fade + i];
                samples[i] = Mathf.Lerp(tail, samples[i], t);
            }

            Array.Resize(ref samples, samples.Length);
        }

        // ---------- files ----------

        /// <summary>16-bit mono PCM. Small, universally supported, and enough for a placeholder.</summary>
        private static byte[] Wav(float[] samples)
        {
            var dataBytes = samples.Length * 2;
            using var stream = new MemoryStream(44 + dataBytes);
            using var writer = new BinaryWriter(stream);

            writer.Write(new[] { 'R', 'I', 'F', 'F' });
            writer.Write(36 + dataBytes);
            writer.Write(new[] { 'W', 'A', 'V', 'E' });
            writer.Write(new[] { 'f', 'm', 't', ' ' });
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(Rate);
            writer.Write(Rate * 2);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write(new[] { 'd', 'a', 't', 'a' });
            writer.Write(dataBytes);

            foreach (var sample in samples)
            {
                writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(sample, -1f, 1f) * 32000f));
            }

            writer.Flush();
            return stream.ToArray();
        }

        private static void WriteIfChanged(string path, byte[] wav)
        {
            var full = Path.GetFullPath(path);
            if (File.Exists(full) && BytesEqual(File.ReadAllBytes(full), wav))
            {
                return;
            }

            File.WriteAllBytes(full, wav);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            ConfigureImporter(path);
        }

        private static bool BytesEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }

            for (var i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Short sounds load into memory; long beds stream. Getting this backwards is how a game
        /// ends up holding minutes of ambience in RAM or hitching on every footstep.
        /// </summary>
        private static void ConfigureImporter(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
            {
                return;
            }

            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            var isLong = clip != null && clip.length > 3f;
            var settings = importer.defaultSampleSettings;
            settings.loadType = isLong
                ? AudioClipLoadType.Streaming
                : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = isLong
                ? AudioCompressionFormat.Vorbis
                : AudioCompressionFormat.PCM;
            settings.quality = 0.7f;
            // Preloading moved onto the per-platform sample settings; the importer-level flag is
            // obsolete and ignored.
            settings.preloadAudioData = !isLong;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;
            importer.SaveAndReimport();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
