using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// Audio in the running game. The questions are the ones that decide whether a soundscape
    /// helps or turns into noise: is there exactly one listener, does a crowd stay capped, does a
    /// respawn leave loops running, and can the player actually turn it off.
    /// </summary>
    public sealed class AudioTests
    {
        private PlayerCharacter _player;
        private AudioManager _audio;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            GameSettings.ResetToDefaults();
            yield return SceneManager.LoadSceneAsync("TombTrial");
            _player = Object.FindAnyObjectByType<PlayerCharacter>();
            _audio = Object.FindAnyObjectByType<AudioManager>();
            yield return new WaitForFixedUpdate();

            // The player spawns a few centimetres above the floor and lands with a sound. Tests
            // that count voices start after that landing, not racing it: with the lit scene and
            // the HUD the first frames are longer, and the landing used to fall inside a test.
            var motor = _player.GetComponent<PlayerMotor2D>();
            var deadline = Time.realtimeSinceStartup + 3f;
            while (!motor.IsGrounded && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.2f);
        }

        [TearDown]
        public void TearDown()
        {
            _audio?.StopAll();
            GameSettings.ResetToDefaults();
            GamePause.Clear();
            SaveSystem.Delete();
            Time.timeScale = 1f;
        }

        // ---------- wiring ----------

        [UnityTest]
        public IEnumerator TheSceneHasExactlyOneListener()
        {
            yield return null;
            var listeners = Object.FindObjectsByType<AudioListener>();

            Assert.That(listeners.Length, Is.EqualTo(1),
                "Zero listeners is a silent game; two is undefined behaviour.");
            Assert.That(listeners[0].GetComponent<Camera>(), Is.Not.Null,
                "The listener rides the camera, so it hears from where the player is looking.");
        }

        [UnityTest]
        public IEnumerator TheSceneShipsWithTheAudioSystems()
        {
            yield return null;

            Assert.That(_audio, Is.Not.Null);
            Assert.That(_audio.Library, Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<CombatAudioBinder>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<AmbienceController>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<MusicDirector>(), Is.Not.Null);
        }

        // ---------- playing ----------

        [UnityTest]
        public IEnumerator AskingForASoundProducesAVoice()
        {
            _audio.StopAll();
            yield return null;

            AudioManager.Play(AudioId.Parry, _player.transform.position);
            yield return null;

            Assert.That(_audio.ActiveVoices, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator AskingForNothingIsHarmless()
        {
            _audio.StopAll();
            yield return null;

            AudioManager.Play(AudioId.None, Vector2.zero);
            yield return null;

            Assert.That(_audio.ActiveVoices, Is.Zero,
                $"A forgotten reference plays silence; playing: {_audio.DescribeActiveVoices()}.");
        }

        [UnityTest]
        public IEnumerator ACrowdOfTheSameSoundIsCapped()
        {
            // Eight enemies hitting at once must not produce eight of the same impact.
            _audio.StopAll();
            yield return null;

            var limit = _audio.Library.Event(AudioId.HitFlesh).MaxVoices;
            for (var i = 0; i < 20; i++)
            {
                AudioManager.Play(AudioId.HitFlesh,
                    (Vector2)_player.transform.position + Vector2.right * i);
            }

            yield return null;
            Assert.That(_audio.ActiveVoices, Is.LessThanOrEqualTo(limit + 1),
                "The voice cap is the main defence against a wall of noise.");
        }

        [UnityTest]
        public IEnumerator ASoundWithAMinimumGapRefusesToMachineGun()
        {
            _audio.StopAll();
            yield return null;
            Assume.That(_audio.Library.Event(AudioId.CastFailed).MinimumInterval,
                Is.GreaterThan(0.1f));

            for (var i = 0; i < 10; i++)
            {
                AudioManager.Play2D(AudioId.CastFailed);
            }

            yield return null;
            Assert.That(_audio.ActiveVoices, Is.LessThanOrEqualTo(1), $"playing: {_audio.DescribeActiveVoices()}");
        }

        // ---------- the player can turn it down ----------

        [UnityTest]
        public IEnumerator TheMasterVolumeScalesEveryBus()
        {
            GameSettings.Set(data => data.masterVolume = 0f);
            yield return null;

            foreach (var bus in new[]
            {
                AudioBus.Music, AudioBus.Sfx, AudioBus.Ui, AudioBus.Ambience
            })
            {
                Assert.That(_audio.BusVolume(bus), Is.Zero, $"{bus} survived master at zero.");
            }
        }

        [UnityTest]
        public IEnumerator SilencingCombatLeavesTheRoomAudible()
        {
            // The sliders have to be separable, or turning down combat also empties the world.
            GameSettings.Set(data =>
            {
                data.effectsVolume = 0f;
                data.masterVolume = 1f;
            });
            yield return null;

            Assert.That(_audio.BusVolume(AudioBus.Sfx), Is.Zero);
            Assert.That(_audio.BusVolume(AudioBus.Ambience), Is.GreaterThan(0f));
            Assert.That(_audio.BusVolume(AudioBus.Ui), Is.GreaterThan(0f));
        }

        [UnityTest]
        public IEnumerator SilencingMusicLeavesCombatAudible()
        {
            GameSettings.Set(data =>
            {
                data.musicVolume = 0f;
                data.masterVolume = 1f;
                data.effectsVolume = 1f;
            });
            yield return null;

            Assert.That(_audio.BusVolume(AudioBus.Music), Is.Zero);
            Assert.That(_audio.BusVolume(AudioBus.Sfx), Is.GreaterThan(0f));
        }

        [UnityTest]
        public IEnumerator VolumeSettingsSurviveARestart()
        {
            GameSettings.Set(data => data.effectsVolume = 0.3f);
            GameSettings.Unload();
            GameSettings.Load();
            yield return null;

            Assert.That(GameSettings.Current.effectsVolume, Is.EqualTo(0.3f).Within(0.001f));
        }

        // ---------- ducking ----------

        [UnityTest]
        public IEnumerator ALoudMomentQuietensTheRoomWithoutMutingIt()
        {
            var before = _audio.BusVolume(AudioBus.Ambience);
            _audio.Duck(AudioBus.Ambience, 0.7f);
            yield return null;

            var ducked = _audio.BusVolume(AudioBus.Ambience);
            Assert.That(ducked, Is.LessThan(before));
            Assert.That(ducked, Is.GreaterThan(0f),
                "A bus at zero is a bug that sounds like a crash.");

            yield return new WaitForSeconds(1.4f);
            Assert.That(_audio.BusVolume(AudioBus.Ambience), Is.EqualTo(before).Within(0.02f),
                "The room has to come back on its own.");
        }

        // ---------- nothing is left running ----------

        [UnityTest]
        public IEnumerator StoppingEverythingLeavesNoVoices()
        {
            AudioManager.Play(AudioId.BossSlam, _player.transform.position);
            AudioManager.Play(AudioId.Parry, _player.transform.position);
            yield return null;

            _audio.StopAll();
            yield return null;

            Assert.That(_audio.ActiveVoices, Is.Zero);
        }

        [UnityTest]
        public IEnumerator RespawningLeavesNoLoopStillRunning()
        {
            // The classic audio bug: a status or boss layer surviving a death. Counted rather than
            // held by reference, because the pool may hand the same source to something else.
            _audio.StopAll();
            _audio.PlayLoop(AudioId.AmbienceTombBase, _player.transform.position);
            yield return null;
            Assume.That(_audio.LoopingVoices, Is.GreaterThan(0));

            _player.Actor.ApplyDamage(new DamageInfo(99999f, Vector2.right,
                hitPoint: _player.transform.position));
            yield return new WaitForSeconds(0.2f);

            var respawner = Object.FindAnyObjectByType<PlayerRespawner>();
            respawner.RespawnPlayer();

            var deadline = Time.realtimeSinceStartup + 6f;
            while (!_player.IsAlive && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assume.That(_player.IsAlive, Is.True);
            yield return null;

            Assert.That(_audio.LoopingVoices, Is.Zero, "A respawn starts from silence.");
        }

        [UnityTest]
        public IEnumerator StoppingLoopsLeavesOneShotsAlone()
        {
            _audio.StopAll();
            _audio.PlayLoop(AudioId.AmbienceTombBase, Vector2.zero);
            // Long enough that it is certainly still sounding after the loops are cut.
            AudioManager.Play(AudioId.BossDeath, Vector2.zero);
            yield return null;
            Assume.That(_audio.LoopingVoices, Is.GreaterThan(0));

            _audio.StopLoops();
            yield return null;

            Assert.That(_audio.LoopingVoices, Is.Zero);
            Assert.That(_audio.ActiveVoices, Is.GreaterThan(0),
                "A one-shot that was already sounding should finish naturally.");
        }

        // ---------- music state ----------

        [UnityTest]
        public IEnumerator EachRegionHasItsTrackAndTheBossesShareOne()
        {
            var director = Object.FindAnyObjectByType<MusicDirector>();
            yield return null;
            Assume.That(director.State, Is.EqualTo(MusicState.Exploration));
            Assert.That(director.HasTrackForState, Is.True, "The Tomb has music now.");
            var tomb = director.CurrentTrack;
            Assert.That(director.CurrentLayer, Is.Not.Null, "... and a fight layer under it.");

            director.SetZone("zone_withered_forest");
            Assert.That(director.CurrentTrack, Is.Not.Null.And.Not.EqualTo(tomb),
                "The forest sounds like itself.");
            director.SetZone("zone_forgotten_tomb");

            var boss = TombScene.Guardian();
            boss.BeginFight(_player);
            yield return null;
            Assert.That(director.State, Is.Not.EqualTo(MusicState.Exploration));
            var fight = director.CurrentTrack;
            Assert.That(fight, Is.Not.Null.And.Not.EqualTo(tomb));

            director.TransitionTo(MusicState.BossPhaseTwo);
            Assert.That(director.CurrentTrack, Is.SameAs(fight),
                "Phase two adds drums to the same track rather than restarting it.");
            var deadline = Time.realtimeSinceStartup + 3f;
            while (director.LayerMix < 0.99f && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(director.LayerMix, Is.EqualTo(1f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator TheFightLayerRisesWhileFightingAndSettlesAfter()
        {
            var director = Object.FindAnyObjectByType<MusicDirector>();
            var combat = _player.GetComponent<PlayerCombatStatus>();
            yield return null;
            Assume.That(director.State, Is.EqualTo(MusicState.Exploration));
            Assert.That(director.LayerMix, Is.Zero, "Quiet while nothing is fighting.");

            combat.Report();
            var deadline = Time.realtimeSinceStartup + 3f;
            while (director.LayerMix < 0.99f && Time.realtimeSinceStartup < deadline)
            {
                combat.Report();
                yield return null;
            }

            Assert.That(director.LayerMix, Is.EqualTo(1f).Within(0.01f), "The pulse comes in.");

            combat.Clear();
            deadline = Time.realtimeSinceStartup + 5f;
            while (director.LayerMix > 0f && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(director.LayerMix, Is.Zero, "... and ebbs away once the fight is over.");
        }

        [UnityTest]
        public IEnumerator TheTitleScreenPlaysItsOwnTheme()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;
            var director = Object.FindAnyObjectByType<MusicDirector>();

            Assert.That(director, Is.Not.Null);
            Assert.That(director.State, Is.EqualTo(MusicState.MainMenu));
            Assert.That(director.HasTrackForState, Is.True);
        }

        [UnityTest]
        public IEnumerator DyingDoesNotLeaveBossMusicPlayingThroughTheRespawn()
        {
            var director = Object.FindAnyObjectByType<MusicDirector>();
            var boss = TombScene.Guardian();
            boss.BeginFight(_player);
            yield return null;

            var respawner = Object.FindAnyObjectByType<PlayerRespawner>();
            respawner.RespawnPlayer();
            var deadline = Time.realtimeSinceStartup + 6f;
            while (director.State != MusicState.Exploration
                && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(director.State, Is.EqualTo(MusicState.Exploration));
        }

        // ---------- the room ----------

        [UnityTest]
        public IEnumerator TheRoomQuietensDownForABossFight()
        {
            var ambience = Object.FindAnyObjectByType<AmbienceController>();
            yield return null;
            Assume.That(ambience.IsSuppressed, Is.False);

            ambience.Suppress(true);
            yield return null;

            Assert.That(ambience.IsSuppressed, Is.True,
                "Combat needs the space more than atmosphere does.");
            ambience.Suppress(false);
        }

        [UnityTest]
        public IEnumerator EveryEnemyInTheSceneCanBeHeard()
        {
            yield return null;
            var enemies = Object.FindObjectsByType<EnemyController>();
            Assume.That(enemies.Length, Is.GreaterThan(0));

            // Every enemy kind present has to have a full set, or a fight goes half-silent.
            foreach (var id in new[]
            {
                AudioId.ZombieSwing, AudioId.ZombieHurt, AudioId.ZombieDeath,
                AudioId.GuardBash, AudioId.GuardHeavy, AudioId.GuardDeath
            })
            {
                Assert.That(_audio.Library.Has(id), Is.True, $"{id}");
            }
        }
    }
}
