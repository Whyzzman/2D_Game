using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    public static partial class ContentBuilder
    {
        // ---------- audio ----------

        /// <summary>
        /// Builds one event per sound and the library that indexes them.
        ///
        /// The numbers here are the mix. Priority decides who survives when the voices run out —
        /// the Guardian's charge telegraph sits at the very top because a player who misses it
        /// eats the charge, while an ambient creak is the first thing dropped. Voice limits are
        /// what stop a crowd of enemies producing a crowd of identical sounds.
        /// </summary>
        private static AudioLibrary BuildAudio()
        {
            EnsureFolder(AudioEventFolder);
            var clips = PlaceholderAudioArt.Generate();
            // The score is written here too, so a content build alone keeps the tracks current;
            // SceneMusic puts them into the scenes.
            PlaceholderMusicArt.Generate();
            var events = new System.Collections.Generic.List<AudioEvent>();

            // Movement. Frequent and quiet: high voice counts, low priority, nothing spatial.
            events.Add(Sound(AudioId.FootstepStone, clips, 0.34f, voices: 4, priority: 15,
                interval: 0.1f, spatial: 0.4f));
            // The same step on the other surfaces (item 4): the same weight, its own material.
            events.Add(Sound(AudioId.FootstepWood, clips, 0.36f, voices: 4, priority: 15,
                interval: 0.1f, spatial: 0.4f));
            events.Add(Sound(AudioId.FootstepEarth, clips, 0.3f, voices: 4, priority: 15,
                interval: 0.1f, spatial: 0.4f));
            events.Add(Sound(AudioId.FootstepWater, clips, 0.34f, voices: 4, priority: 15,
                interval: 0.1f, spatial: 0.4f));
            events.Add(Sound(AudioId.FootstepAsh, clips, 0.3f, voices: 4, priority: 15,
                interval: 0.1f, spatial: 0.4f));
            events.Add(Sound(AudioId.Land, clips, 0.55f, voices: 2, priority: 30));
            events.Add(Sound(AudioId.Jump, clips, 0.3f, voices: 2, priority: 15));
            events.Add(Sound(AudioId.DashGround, clips, 0.5f, voices: 2, priority: 30));
            events.Add(Sound(AudioId.DashAir, clips, 0.5f, voices: 2, priority: 30));
            events.Add(Sound(AudioId.WallSlide, clips, 0.35f, voices: 1, priority: 15,
                interval: 0.35f));
            events.Add(Sound(AudioId.WallContact, clips, 0.45f, voices: 2, priority: 20));
            // The skid of a start or a turn and the kick off a wall (2026-10-19): footwork, so as
            // quiet and low in priority as the steps themselves.
            events.Add(Sound(AudioId.FootstepScuff, clips, 0.3f, voices: 2, priority: 16,
                interval: 0.12f, spatial: 0.4f));
            events.Add(Sound(AudioId.WallJumpKick, clips, 0.42f, voices: 1, priority: 28,
                spatial: 0.4f));

            // Weapons. The greatsword is louder and lower before a single clip is chosen.
            events.Add(Sound(AudioId.SwordSwingLight, clips, 0.5f, voices: 3, priority: 45));
            events.Add(Sound(AudioId.SwordSwingHeavy, clips, 0.6f, voices: 2, priority: 50));
            events.Add(Sound(AudioId.GreatswordSwingLight, clips, 0.62f, voices: 2, priority: 50));
            events.Add(Sound(AudioId.GreatswordSwingHeavy, clips, 0.72f, voices: 2, priority: 55));
            events.Add(Sound(AudioId.WeaponSwitch, clips, 0.45f, voices: 1, priority: 30,
                interval: 0.25f));

            events.Add(Sound(AudioId.HitFlesh, clips, 0.62f, voices: 4, priority: 60));
            events.Add(Sound(AudioId.HitArmor, clips, 0.64f, voices: 4, priority: 60));
            events.Add(Sound(AudioId.HitHeavy, clips, 0.74f, voices: 3, priority: 65));
            events.Add(Sound(AudioId.HitCritical, clips, 0.78f, voices: 2, priority: 75));

            // Defence. The parry is the loudest non-boss sound and the hardest to lose.
            events.Add(Sound(AudioId.Block, clips, 0.66f, voices: 3, priority: 70));
            events.Add(Sound(AudioId.BlockShield, clips, 0.7f, voices: 3, priority: 70));
            events.Add(Sound(AudioId.Parry, clips, 0.9f, voices: 2, priority: 95));
            events.Add(Sound(AudioId.GuardBreak, clips, 0.88f, voices: 2, priority: 95));
            events.Add(Sound(AudioId.StaggerBreak, clips, 0.76f, voices: 2, priority: 80));

            events.Add(Sound(AudioId.PlayerHurt, clips, 0.8f, voices: 2, priority: 90,
                spatial: 0.1f));
            events.Add(Sound(AudioId.PlayerHurtHeavy, clips, 0.88f, voices: 1, priority: 92,
                spatial: 0.1f));
            events.Add(Sound(AudioId.PlayerDeath, clips, 0.85f, voices: 1, priority: 100,
                spatial: 0f));

            events.Add(Sound(AudioId.GroundBreaker, clips, 0.85f, voices: 1, priority: 85));
            events.Add(Sound(AudioId.DashStrike, clips, 0.68f, voices: 2, priority: 70));
            events.Add(Sound(AudioId.PerfectDodge, clips, 0.5f, voices: 1, priority: 60));

            // Magic. Four families, four recipes, so they cannot be confused by ear.
            events.Add(Sound(AudioId.FireCast, clips, 0.6f, voices: 2, priority: 60));
            events.Add(Sound(AudioId.FireImpact, clips, 0.68f, voices: 3, priority: 65));
            events.Add(Sound(AudioId.FrostCast, clips, 0.6f, voices: 2, priority: 60));
            events.Add(Sound(AudioId.FrostImpact, clips, 0.66f, voices: 3, priority: 65));
            events.Add(Sound(AudioId.ArcaneCast, clips, 0.6f, voices: 2, priority: 60));
            events.Add(Sound(AudioId.ArcaneBurst, clips, 0.8f, voices: 1, priority: 78));
            events.Add(Sound(AudioId.ManaShieldUp, clips, 0.6f, voices: 1, priority: 60));
            events.Add(Sound(AudioId.ManaShieldHit, clips, 0.55f, voices: 2, priority: 60));
            // Held cast key must not machine-gun the failure tone.
            events.Add(Sound(AudioId.CastFailed, clips, 0.4f, voices: 1, priority: 40,
                interval: 0.55f));

            events.Add(Sound(AudioId.ShadowDashStart, clips, 0.62f, voices: 2, priority: 65));
            events.Add(Sound(AudioId.ShadowDashEnd, clips, 0.58f, voices: 2, priority: 65));
            events.Add(Sound(AudioId.ShadowStrike, clips, 0.72f, voices: 2, priority: 72));
            events.Add(Sound(AudioId.ShadowImpact, clips, 0.68f, voices: 3, priority: 68));

            // Statuses fire often, so they are quiet, capped and spaced.
            events.Add(Sound(AudioId.BleedProc, clips, 0.45f, voices: 2, priority: 45,
                interval: 0.3f));
            events.Add(Sound(AudioId.PoisonProc, clips, 0.4f, voices: 2, priority: 40,
                interval: 0.4f));
            events.Add(Sound(AudioId.BurnProc, clips, 0.45f, voices: 2, priority: 45,
                interval: 0.35f));
            events.Add(Sound(AudioId.FrostProc, clips, 0.45f, voices: 2, priority: 45,
                interval: 0.35f));

            // Enemies. Voice caps matter most here: three zombies must not be three times as loud.
            events.Add(Sound(AudioId.ZombieStep, clips, 0.3f, voices: 3, priority: 12,
                interval: 0.12f));
            events.Add(Sound(AudioId.ZombieAlert, clips, 0.55f, voices: 2, priority: 55,
                interval: 0.8f));
            events.Add(Sound(AudioId.ZombieSwing, clips, 0.6f, voices: 3, priority: 72));
            events.Add(Sound(AudioId.ZombieHurt, clips, 0.5f, voices: 3, priority: 50));
            events.Add(Sound(AudioId.ZombieDeath, clips, 0.6f, voices: 2, priority: 60));

            events.Add(Sound(AudioId.GuardStep, clips, 0.36f, voices: 2, priority: 14,
                interval: 0.16f));
            events.Add(Sound(AudioId.GuardRaise, clips, 0.55f, voices: 2, priority: 58));
            events.Add(Sound(AudioId.GuardBash, clips, 0.68f, voices: 2, priority: 76));
            events.Add(Sound(AudioId.GuardHeavy, clips, 0.72f, voices: 2, priority: 80));
            events.Add(Sound(AudioId.GuardHurt, clips, 0.52f, voices: 3, priority: 50));
            events.Add(Sound(AudioId.GuardDeath, clips, 0.66f, voices: 2, priority: 62));

            // The Guardian. Everything here outranks ordinary combat.
            events.Add(Sound(AudioId.BossAwaken, clips, 0.85f, voices: 1, priority: 98));
            events.Add(Sound(AudioId.BossStep, clips, 0.55f, voices: 2, priority: 40,
                interval: 0.2f));
            events.Add(Sound(AudioId.BossSlash, clips, 0.78f, voices: 2, priority: 85));
            events.Add(Sound(AudioId.BossBash, clips, 0.8f, voices: 2, priority: 85));
            // The single most important sound in the game: it must never be dropped or masked.
            events.Add(Sound(AudioId.BossChargeStart, clips, 0.95f, voices: 1, priority: 120,
                spatial: 0.25f));
            events.Add(Sound(AudioId.BossCharge, clips, 0.8f, voices: 1, priority: 90));
            events.Add(Sound(AudioId.BossCombo, clips, 0.78f, voices: 2, priority: 85));
            events.Add(Sound(AudioId.BossPhase, clips, 0.95f, voices: 1, priority: 115,
                spatial: 0f));
            events.Add(Sound(AudioId.BossJump, clips, 0.78f, voices: 1, priority: 95));
            events.Add(Sound(AudioId.BossSlam, clips, 1f, voices: 1, priority: 110));
            events.Add(Sound(AudioId.BossStunned, clips, 0.7f, voices: 1, priority: 80));
            events.Add(Sound(AudioId.BossDeath, clips, 0.95f, voices: 1, priority: 115,
                spatial: 0f));

            // The world. Warm, calm, and flat — safety is not a thing you localise.
            events.Add(Sound(AudioId.CheckpointActivate, clips, 0.7f, voices: 1, priority: 85,
                spatial: 0f));
            events.Add(Sound(AudioId.CheckpointRest, clips, 0.6f, voices: 1, priority: 80,
                spatial: 0f));
            events.Add(Sound(AudioId.Respawn, clips, 0.6f, voices: 1, priority: 85, spatial: 0f));
            events.Add(Sound(AudioId.PickupCommon, clips, 0.45f, voices: 2, priority: 35,
                interval: 0.08f));
            events.Add(Sound(AudioId.PickupRare, clips, 0.6f, voices: 1, priority: 60));
            // Bundled coins collapse into one tone rather than a rattle of fifty, but the gap is
            // short enough for coins flying in one after another to ring their rising run
            // (CoinChime, GAME_FEEL_IDEAS.md item 15; 0.18 → 0.07, decided by Claude).
            events.Add(Sound(AudioId.PickupCurrency, clips, 0.4f, voices: 3, priority: 30,
                interval: 0.07f));
            events.Add(Sound(AudioId.LevelUp, clips, 0.7f, voices: 1, priority: 90, spatial: 0f));
            events.Add(Sound(AudioId.SkillUnlock, clips, 0.6f, voices: 1, priority: 70,
                spatial: 0f));
            // Healing is heard by the player only as a cue that the commitment started.
            events.Add(Sound(AudioId.FlaskDrink, clips, 0.55f, voices: 1, priority: 60));
            events.Add(Sound(AudioId.HerbEat, clips, 0.5f, voices: 1, priority: 55));
            // The pogo's ring is how a bounce is felt; chains of them must each be heard.
            events.Add(Sound(AudioId.Pogo, clips, 0.7f, voices: 2, priority: 75, interval: 0.05f));
            events.Add(Sound(AudioId.CastFizzle, clips, 0.55f, voices: 1, priority: 60));
            // Several bodies can go at once after a fight; they are a quiet afterthought.
            events.Add(Sound(AudioId.CorpseCrumble, clips, 0.4f, voices: 3, priority: 30));
            events.Add(Sound(AudioId.ShopBuy, clips, 0.45f, voices: 1, priority: 50));
            events.Add(Sound(AudioId.ChestOpen, clips, 0.55f, voices: 1, priority: 55));
            // The capital's own air, at the bottom of the table with the other rooms.
            events.Add(Sound(AudioId.AmbienceCapitalBase, clips, 0.5f, AudioBus.Ambience, voices: 1,
                priority: 5, spatial: 0f, loop: true));
            events.Add(Sound(AudioId.AmbienceBellToll, clips, 0.3f, AudioBus.Ambience, voices: 1,
                priority: 8, spatial: 0.8f));
            events.Add(Sound(AudioId.AmbienceEmbers, clips, 0.3f, AudioBus.Ambience, voices: 2,
                priority: 7, spatial: 0.6f));
            events.Add(Sound(AudioId.RavenCaw, clips, 0.5f, voices: 2, priority: 60));
            events.Add(Sound(AudioId.GargoyleWake, clips, 0.7f, voices: 2, priority: 80));
            events.Add(Sound(AudioId.PriestHeal, clips, 0.55f, voices: 2, priority: 70));
            events.Add(Sound(AudioId.PriestBlink, clips, 0.5f, voices: 2, priority: 60));
            // The King. His rage is the one that must never be dropped, as the Warden's phase.
            events.Add(Sound(AudioId.KingAwaken, clips, 0.9f, voices: 1, priority: 110, spatial: 0.2f));
            events.Add(Sound(AudioId.KingStep, clips, 0.55f, voices: 2, priority: 35, interval: 0.14f));
            events.Add(Sound(AudioId.KingRage, clips, 0.95f, voices: 1, priority: 120, spatial: 0.1f));
            events.Add(Sound(AudioId.KingStunned, clips, 0.75f, voices: 1, priority: 90));
            events.Add(Sound(AudioId.KingDeath, clips, 0.95f, voices: 1, priority: 120, spatial: 0.1f));
            // A wall giving way to the hero's blows (item 4).
            events.Add(Sound(AudioId.WallCrack, clips, 0.6f, voices: 2, priority: 50));
            events.Add(Sound(AudioId.WallCrumble, clips, 0.8f, voices: 1, priority: 80));
            events.Add(Sound(AudioId.TrialGong, clips, 0.85f, voices: 1, priority: 100, spatial: 0.3f));
            events.Add(Sound(AudioId.PropBreak, clips, 0.6f, voices: 3, priority: 45, interval: 0.04f));
            // The hero's death and return: never dropped, nearly flat — it is about the player.
            events.Add(Sound(AudioId.HeroFall, clips, 0.85f, voices: 1, priority: 120, spatial: 0.15f));
            events.Add(Sound(AudioId.AltarCall, clips, 0.7f, voices: 1, priority: 110, spatial: 0.15f));
            // Ringing ears after a blow to the hero: quiet, flat (it is in the head), never stacked.
            events.Add(Sound(AudioId.HurtRing, clips, 0.22f, voices: 1, priority: 85, interval: 0.3f,
            // The heart at low health: quiet, flat (it is inside the hero), one at a time.
            events.Add(Sound(AudioId.Heartbeat, clips, 0.45f, voices: 1, priority: 60, interval: 0.3f,
                spatial: 0f));

            // Interface. Quiet, flat, and spaced so fast navigation does not chatter.
            events.Add(Sound(AudioId.UiNavigate, clips, 0.3f, AudioBus.Ui, voices: 1, priority: 25,
                interval: 0.05f, spatial: 0f));
            // Confirm and cancel are heard from MenuInput, which several screens may ask in one
            // frame; the interval keeps one press one sound.
            events.Add(Sound(AudioId.UiConfirm, clips, 0.45f, AudioBus.Ui, voices: 1, priority: 40,
                interval: 0.06f, spatial: 0f));
            events.Add(Sound(AudioId.UiCancel, clips, 0.4f, AudioBus.Ui, voices: 1, priority: 40,
                interval: 0.06f, spatial: 0f));
            events.Add(Sound(AudioId.UiOpen, clips, 0.45f, AudioBus.Ui, voices: 1, priority: 40,
                spatial: 0f));
            events.Add(Sound(AudioId.UiClose, clips, 0.42f, AudioBus.Ui, voices: 1, priority: 40,
                spatial: 0f));
            events.Add(Sound(AudioId.UiError, clips, 0.45f, AudioBus.Ui, voices: 1, priority: 55,
                interval: 0.4f, spatial: 0f));
            events.Add(Sound(AudioId.UiSaveFailed, clips, 0.6f, AudioBus.Ui, voices: 1,
                priority: 85, spatial: 0f));

            // The room. Lowest priority in the game: it is the first thing to lose a voice.
            events.Add(Sound(AudioId.AmbienceTombBase, clips, 0.5f, AudioBus.Ambience, voices: 1,
                priority: 5, spatial: 0f, loop: true));
            events.Add(Sound(AudioId.AmbienceArenaBase, clips, 0.5f, AudioBus.Ambience, voices: 1,
                priority: 5, spatial: 0f, loop: true));
            events.Add(Sound(AudioId.AmbienceStoneSettle, clips, 0.4f, AudioBus.Ambience,
                voices: 1, priority: 8, spatial: 0.7f));
            events.Add(Sound(AudioId.AmbienceChain, clips, 0.35f, AudioBus.Ambience, voices: 1,
                priority: 8, spatial: 0.7f));
            events.Add(Sound(AudioId.AmbienceDrip, clips, 0.35f, AudioBus.Ambience, voices: 1,
                priority: 8, spatial: 0.7f));
            events.Add(Sound(AudioId.AmbienceWind, clips, 0.4f, AudioBus.Ambience, voices: 1,
                priority: 6, spatial: 0.5f));

            // The Withered Forest. Same rules, same table; a second zone adds rows here and
            // nothing else, which was the point of building the mixer this way.
            AddForestAudio(events, clips);

            var library = CreateAsset<AudioLibrary>(AudioLibraryPath);
            var serialized = new SerializedObject(library);
            var list = serialized.FindProperty("_events");
            list.arraySize = events.Count;
            for (var i = 0; i < events.Count; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = events[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(library);
            return library;
        }

        private static AudioEvent Sound(AudioId id,
            System.Collections.Generic.Dictionary<AudioId, AudioClip[]> clips, float volume,
            AudioBus bus = AudioBus.Sfx, int voices = 3, int priority = 50, float interval = 0f,
            float spatial = 0.55f, bool loop = false)
        {
            var name = "audio_" + id.ToString().ToLowerInvariant();
            var asset = CreateAsset<AudioEvent>($"{AudioEventFolder}/{name}.asset");
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("_id").enumValueIndex = (int)id;
            serialized.FindProperty("_bus").enumValueIndex = (int)bus;
            serialized.FindProperty("_volume").floatValue = volume;
            serialized.FindProperty("_volumeJitter").floatValue = loop ? 0f : 0.06f;
            serialized.FindProperty("_pitchJitter").floatValue = loop ? 0f : 0.045f;
            serialized.FindProperty("_spatialBlend").floatValue = spatial;
            serialized.FindProperty("_maxDistance").floatValue = 26f;
            serialized.FindProperty("_maxVoices").intValue = voices;
            serialized.FindProperty("_minimumInterval").floatValue = interval;
            serialized.FindProperty("_priority").intValue = priority;
            serialized.FindProperty("_loop").boolValue = loop;

            var variants = clips != null && clips.TryGetValue(id, out var found)
                ? found
                : System.Array.Empty<AudioClip>();
            var array = serialized.FindProperty("_clips");
            array.arraySize = variants.Length;
            for (var i = 0; i < variants.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = variants[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }
    }
}
