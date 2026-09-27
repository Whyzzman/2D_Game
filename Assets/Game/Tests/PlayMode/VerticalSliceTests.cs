using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The slice as a whole, rather than any one system: that nothing meant for making the game
    /// reaches a player, that the score follows the fight, that a reload does not quietly double
    /// the services, and that traversal cannot walk around the encounter it is supposed to face.
    /// </summary>
    public sealed class VerticalSliceTests
    {
        private PlayerCharacter _player;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            GameSettings.ResetToDefaults();
            yield return SceneManager.LoadSceneAsync("TombTrial");
            _player = Object.FindAnyObjectByType<PlayerCharacter>();
            yield return new WaitForFixedUpdate();
        }

        [TearDown]
        public void TearDown()
        {
            GameSettings.ResetToDefaults();
            GamePause.Clear();
            SaveSystem.Delete();
            Time.timeScale = 1f;
        }

        // ---------- nothing for developers reaches a player ----------

        [UnityTest]
        public IEnumerator EveryDebugToolIsMarkedDeveloperOnly()
        {
            yield return null;

            // A debug readout left ungated ships with the game and answers the F-keys of whoever
            // is playing it.
            foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>())
            {
                var name = behaviour.GetType().Name;
                if (!name.Contains("Debug") && !name.Contains("Overlay"))
                {
                    continue;
                }

                Assert.That(behaviour.GetComponentInParent<DeveloperOnly>(true), Is.Not.Null,
                    $"{name} on '{behaviour.gameObject.name}' would ship enabled.");
            }
        }

        [UnityTest]
        public IEnumerator TrainingTargetsAreMarkedDeveloperOnly()
        {
            yield return null;

            // They stand where a player starts, so they must not be there in a release build.
            foreach (var dummy in Object.FindObjectsByType<TrainingDummy>())
            {
                Assert.That(dummy.GetComponentInParent<DeveloperOnly>(true), Is.Not.Null,
                    $"'{dummy.name}' would greet the player.");
            }

            foreach (var attacker in Object.FindObjectsByType<TrainingAttacker>())
            {
                Assert.That(attacker.GetComponentInParent<DeveloperOnly>(true), Is.Not.Null,
                    $"'{attacker.name}' would greet the player.");
            }
        }

        [UnityTest]
        public IEnumerator DeveloperContentIsStillPresentWhileTesting()
        {
            // The gate must not be so eager that it removes what the tests fight.
            yield return null;

            Assert.That(DeveloperOnly.IsDeveloperBuild, Is.True);
            Assert.That(Object.FindObjectsByType<TrainingDummy>(),
                Is.Not.Empty);
        }

        // ---------- one of each service ----------

        [UnityTest]
        public IEnumerator ReloadingTheSceneDoesNotDoubleTheServices()
        {
            yield return SceneManager.LoadSceneAsync("TombTrial");
            yield return new WaitForFixedUpdate();

            // A service that survives a reload and is created again is the quiet way a game ends
            // up playing every sound twice.
            AssertSingle<AudioManager>();
            AssertSingle<MusicDirector>();
            AssertSingle<CombatFeedbackService>();
            AssertSingle<GameProgress>();
            AssertSingle<PlayerRespawner>();
            AssertSingle<AudioListener>();
            AssertSingle<PlayerCharacter>();
        }

        private static void AssertSingle<T>() where T : Object
        {
            var found = Object.FindObjectsByType<T>();
            Assert.That(found.Length, Is.EqualTo(1),
                $"{typeof(T).Name}: expected exactly one, found {found.Length}.");
        }

        /// <summary>
        /// Wears the Guardian down until it dies. One enormous hit does not work: the intro makes
        /// it untouchable, and the phase change makes it untouchable again halfway through. Any
        /// test that kills it has to respect both, which is also the honest way to reach the
        /// victory state.
        /// </summary>
        private static IEnumerator Wear(BossController boss)
        {
            var deadline = Time.realtimeSinceStartup + 20f;
            while (boss.Actor.IsAlive && Time.realtimeSinceStartup < deadline)
            {
                if (!boss.Actor.IsInvulnerable)
                {
                    boss.Actor.ClearInvulnerability();
                    boss.Actor.ApplyDamage(new DamageInfo(400f, Vector2.right,
                        hitPoint: boss.transform.position));
                }

                yield return new WaitForSeconds(0.08f);
            }
        }

        // ---------- the score follows the fight ----------

        [UnityTest]
        public IEnumerator TheScoreFollowsTheFightFromIntroToPhaseTwo()
        {
            var director = Object.FindAnyObjectByType<MusicDirector>();
            var boss = TombScene.Guardian();
            yield return null;
            Assume.That(director.State, Is.EqualTo(MusicState.Exploration));

            boss.BeginFight(_player);
            yield return null;
            Assert.That(director.State, Is.EqualTo(MusicState.BossIntro));

            yield return Wear(boss);
            var deadline = Time.realtimeSinceStartup + 6f;
            while (director.State != MusicState.BossPhaseTwo
                && director.State != MusicState.BossVictory
                && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(director.State,
                Is.EqualTo(MusicState.BossPhaseTwo).Or.EqualTo(MusicState.BossVictory),
                "The phase turn has to move the music with it.");
        }

        [UnityTest]
        public IEnumerator DyingDoesNotCarryBossMusicIntoTheNextAttempt()
        {
            var director = Object.FindAnyObjectByType<MusicDirector>();
            var boss = TombScene.Guardian();
            boss.BeginFight(_player);
            yield return null;
            Assume.That(director.State, Is.Not.EqualTo(MusicState.Exploration));

            _player.Actor.ApplyDamage(new DamageInfo(99999f, Vector2.right,
                hitPoint: _player.transform.position));
            yield return new WaitForSeconds(0.2f);
            Object.FindAnyObjectByType<PlayerRespawner>().RespawnPlayer();

            var deadline = Time.realtimeSinceStartup + 6f;
            while (director.State != MusicState.Exploration
                && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(director.State, Is.EqualTo(MusicState.Exploration));
        }

        [UnityTest]
        public IEnumerator TheDirectorWorksWithNoTracksAssigned()
        {
            // There is no music yet, and that has to be an ordinary state rather than an error.
            var director = Object.FindAnyObjectByType<MusicDirector>();
            yield return null;

            Assert.That(director.HasTrackForState, Is.False);
            director.TransitionTo(MusicState.BossPhaseTwo);
            yield return null;
            Assert.That(director.State, Is.EqualTo(MusicState.BossPhaseTwo));

            director.StopMusic();
            yield return null;
            Assert.That(director.State, Is.EqualTo(MusicState.Silent));
        }

        // ---------- traversal cannot walk around the fight ----------

        [UnityTest]
        public IEnumerator TheSealedArenaCannotBeCrossedOnFoot()
        {
            var encounter = TombScene.GuardianEncounter();
            var motor = _player.GetComponent<PlayerMotor2D>();
            encounter.Barrier.SetActive(true);
            yield return new WaitForFixedUpdate();

            // Standing just outside the seal and walking into it must not pass through.
            var barrierX = encounter.Barrier.transform.position.x;
            motor.ResetTo(new Vector2(barrierX - 2f, 0.85f));
            yield return new WaitForFixedUpdate();

            var deadline = Time.realtimeSinceStartup + 1.5f;
            while (Time.realtimeSinceStartup < deadline)
            {
                motor.transform.position += new Vector3(0.05f, 0f, 0f);
                yield return new WaitForFixedUpdate();
                if (_player.transform.position.x > barrierX + 0.6f)
                {
                    break;
                }
            }

            Assert.That(_player.transform.position.x, Is.LessThan(barrierX + 0.6f),
                "The seal has to be solid; walking through it would skip the encounter.");
        }

        [UnityTest]
        public IEnumerator TheBarrierIsSolidRatherThanATrigger()
        {
            var encounter = TombScene.GuardianEncounter();
            encounter.Barrier.SetActive(true);
            yield return null;

            var collider = encounter.Barrier.GetComponent<Collider2D>();
            Assert.That(collider, Is.Not.Null);
            Assert.That(collider.isTrigger, Is.False,
                "A trigger would let a dash carry the player straight through.");
        }

        // ---------- the player always recovers ----------

        [UnityTest]
        public IEnumerator InterruptingEveryCommittedActionLeavesThePlayerFree()
        {
            // The softlock this guards against: an action interrupted at the wrong moment leaving
            // the player permanently unable to act.
            var combat = _player.GetComponent<PlayerCombat>();
            var motor = _player.GetComponent<PlayerMotor2D>();

            foreach (var interrupt in new[] { "hurt", "teleport", "kill" })
            {
                _player.Health.RestoreToFull();
                _player.Actor.ClearInvulnerability();
                _player.Stamina.RestoreToFull();
                combat.TryStartLight();
                yield return null;

                switch (interrupt)
                {
                    case "hurt":
                        _player.Actor.ApplyDamage(new DamageInfo(8f, Vector2.left,
                            knockback: 3f, hitPoint: _player.transform.position));
                        break;
                    case "teleport":
                        motor.ResetTo((Vector2)_player.transform.position + Vector2.up * 2f);
                        break;
                    case "kill":
                        _player.Actor.ApplyDamage(new DamageInfo(99999f, Vector2.left,
                            hitPoint: _player.transform.position));
                        break;
                }

                yield return new WaitForSeconds(0.1f);

                if (interrupt == "kill")
                {
                    Object.FindAnyObjectByType<PlayerRespawner>().RespawnPlayer();
                    var deadline = Time.realtimeSinceStartup + 6f;
                    while (!_player.IsAlive && Time.realtimeSinceStartup < deadline)
                    {
                        yield return null;
                    }
                }

                yield return new WaitForSeconds(1.2f);
                Assert.That(_player.Action, Is.EqualTo(PlayerAction.None),
                    $"The player never came back from being interrupted by '{interrupt}'.");
            }
        }

        // ---------- the reward is given once ----------

        [UnityTest]
        public IEnumerator DefeatingTheGuardianGrantsItsRewardExactlyOnce()
        {
            var encounter = TombScene.GuardianEncounter();
            var progress = Object.FindAnyObjectByType<GameProgress>();
            var boss = TombScene.Guardian();
            var granted = 0;
            encounter.RewardGranted += (id, displayName) => granted++;

            boss.BeginFight(_player);
            yield return null;
            yield return Wear(boss);

            var deadline = Time.realtimeSinceStartup + 10f;
            while (!encounter.IsBossDefeated && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assume.That(encounter.IsBossDefeated, Is.True, "The Guardian has to be beatable.");
            yield return new WaitForSeconds(0.6f);

            Assert.That(granted, Is.LessThanOrEqualTo(1), "The reward was granted twice.");
            Assert.That(progress.HasItem("guardian_core"), Is.True);
            Assert.That(encounter.IsArenaSealed, Is.False, "Victory has to open the arena.");
        }

        [UnityTest]
        public IEnumerator ADefeatedGuardianStaysDefeated()
        {
            var encounter = TombScene.GuardianEncounter();
            var boss = TombScene.Guardian();
            boss.BeginFight(_player);
            yield return null;
            yield return Wear(boss);

            var deadline = Time.realtimeSinceStartup + 10f;
            while (!encounter.IsBossDefeated && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assume.That(encounter.IsBossDefeated, Is.True);
            Object.FindAnyObjectByType<GameProgress>().Save();
            yield return SceneManager.LoadSceneAsync("TombTrial");
            yield return new WaitForSeconds(0.4f);

            var reloaded = TombScene.GuardianEncounter();
            Assert.That(reloaded.IsBossDefeated, Is.True, "A beaten boss must stay beaten.");
            Assert.That(reloaded.IsArenaSealed, Is.False);
        }
    }
}
