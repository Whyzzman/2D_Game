using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The first full encounter: waking the Guardian, its two phases, what a parry and a poise
    /// break do to it, and the retry loop that has to leave the arena spotless every time.
    /// </summary>
    public sealed class GuardianOfTheTombTests
    {
        private PlayerCharacter _player;
        private PlayerMotor2D _motor;
        private PlayerRespawner _respawner;
        private GameProgress _progress;
        private BossEncounter _encounter;
        private BossController _boss;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            yield return TestWorld.Load();
            _player = Object.FindAnyObjectByType<PlayerCharacter>();
            _motor = _player.GetComponent<PlayerMotor2D>();
            _respawner = Object.FindAnyObjectByType<PlayerRespawner>();
            _progress = Object.FindAnyObjectByType<GameProgress>();
            _encounter = TombScene.GuardianEncounter();
            _boss = TombScene.Guardian();
            yield return new WaitForFixedUpdate();
        }

        [TearDown]
        public void TearDown()
        {
            SaveSystem.Delete();
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator TheGuardianSleepsUntilSomeoneWalksIn()
        {
            Assert.That(_boss.State, Is.EqualTo(BossState.Dormant));
            Assert.That(_boss.Phase, Is.EqualTo(1));
            Assert.That(_boss.TotalPhases, Is.EqualTo(2));
            Assert.That(_encounter.IsFightActive, Is.False);
            Assert.That(_encounter.IsBossDefeated, Is.False);
            Assert.That(Shield().activeSelf, Is.True, "Phase one carries the shield.");
            yield return new WaitForSeconds(0.5f);
            Assert.That(_boss.State, Is.EqualTo(BossState.Dormant),
                "It must not stir before the arena is entered.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator EnteringTheArenaSealsItAndPlaysAHarmlessIntro()
        {
            var started = false;
            _encounter.FightStarted += _ => started = true;
            var barrierClosed = false;

            yield return EnterArena();
            Assert.That(started, Is.True);
            Assert.That(_encounter.IsFightActive, Is.True);
            Assert.That(_boss.State, Is.EqualTo(BossState.Intro));

            // The intro must not be a free hit on the player.
            var health = _player.Health.Current;
            barrierClosed = Barrier().activeSelf;
            yield return new WaitForSeconds(0.6f);
            Assert.That(barrierClosed, Is.True, "The way out closes for the fight.");
            Assert.That(_player.Health.Current, Is.EqualTo(health).Within(0.01f));
            Assert.That(_boss.Actor.IsInvulnerable, Is.True, "The Guardian cannot be cheesed mid-intro.");

            yield return WaitUntil(() => _boss.State != BossState.Intro, 4f);
            Assert.That(_boss.State, Is.Not.EqualTo(BossState.Intro), "The fight has to begin.");
        }

        /// <summary>
        /// The pause between moves is spent on its feet (2026-11-06, <see cref="BossFootwork"/>): a far hero is
        /// walked in on, not waited for.
        /// </summary>
        [UnityTest]
        public IEnumerator BetweenMovesTheGuardianWalksInOnAFarHero()
        {
            yield return EnterArena();
            yield return WaitUntil(() => _boss.State != BossState.Intro, 4f);
            var hero = _player.transform.position.x;
            var before = Mathf.Abs(hero - _boss.transform.position.x);
            Assume.That(before, Is.GreaterThan(4f), "The arena's entry lies far from the Guardian.");
            var walked = false;
            var deadline = Time.realtimeSinceStartup + 0.8f;
            while (Time.realtimeSinceStartup < deadline && _boss.State != BossState.Attacking)
            {
                walked |= _boss.State == BossState.Reposition;
                yield return null;
            }

            Assert.That(walked, Is.True, "It walks in on a far hero instead of standing.");
            Assert.That(Mathf.Abs(hero - _boss.transform.position.x), Is.LessThan(before - 0.2f), "Closer than it was.");
        }

        [UnityTest]
        public IEnumerator PhaseTwoTriggersOnceAndThrowsTheShieldAway()
        {
            yield return EnterArena();
            var phaseReports = 0;
            var lastPhase = 1;
            var shieldDropped = false;
            _boss.PhaseChanged += phase => { phaseReports++; lastPhase = phase; };
            _boss.ShieldDropped += () => shieldDropped = true;

            _boss.Actor.Health.SetCurrent(_boss.Actor.Health.Max * 0.45f);
            yield return WaitUntil(() => _boss.Phase == 2, 8f);

            Assert.That(_boss.Phase, Is.EqualTo(2));
            Assert.That(lastPhase, Is.EqualTo(2));
            Assert.That(shieldDropped, Is.True);
            Assert.That(Shield().activeSelf, Is.False, "Phase two fights without the shield.");

            // Dropping further must not fire the transition again.
            var reportsAfter = phaseReports;
            _boss.Actor.Health.SetCurrent(_boss.Actor.Health.Max * 0.2f);
            yield return new WaitForSeconds(2f);
            Assert.That(phaseReports, Is.EqualTo(reportsAfter), "The transition happens once.");
        }

        [UnityTest]
        public IEnumerator TheTransitionIsInvulnerableAndDropsAnyLiveHitbox()
        {
            yield return EnterArena();
            yield return WaitUntil(() => _boss.State != BossState.Intro, 4f);

            _boss.Actor.Health.SetCurrent(_boss.Actor.Health.Max * 0.45f);
            yield return WaitUntil(() => _boss.State == BossState.PhaseTransition, 8f);

            Assert.That(_boss.Actor.IsInvulnerable, Is.True);
            var health = _boss.Actor.Health.Current;
            var result = _boss.Actor.ApplyDamage(new DamageInfo(50f, Vector2.left));
            Assert.That(result.Outcome, Is.EqualTo(DamageOutcome.Ignored));
            Assert.That(_boss.Actor.Health.Current, Is.EqualTo(health));
        }

        [UnityTest]
        public IEnumerator BreakingPoiseStunsTheGuardianAndThenItResistsForAWhile()
        {
            yield return EnterArena();
            yield return WaitUntil(() => _boss.State != BossState.Intro, 4f);

            var stagger = _boss.Actor.Stagger;
            var staggered = false;
            _boss.Staggered += _ => staggered = true;
            stagger.Add(stagger.Threshold * 3f);
            yield return new WaitForFixedUpdate();

            Assert.That(staggered, Is.True);
            Assert.That(stagger.IsStunned, Is.True);
            Assert.That(_boss.State, Is.EqualTo(BossState.Stunned));

            yield return WaitUntil(() => !stagger.IsStunned, 5f);
            yield return new WaitForSeconds(0.2f);
            Assert.That(stagger.IsImmune, Is.True,
                "A second break must not land the instant the first one ends.");
            Assert.That(stagger.Add(stagger.Threshold * 3f), Is.False);
        }

        [UnityTest]
        public IEnumerator AParryBreaksTheGuardiansSwing()
        {
            yield return EnterArena();
            yield return WaitUntil(() => _boss.State == BossState.Attacking, 10f);
            Assert.That(_boss.State, Is.EqualTo(BossState.Attacking), "The Guardian has to swing.");

            var parried = false;
            _boss.AttackParried += _ => parried = true;
            _boss.Actor.NotifyParried(new DamageInfo(10f, Vector2.left,
                attacker: _player.gameObject));
            yield return new WaitForFixedUpdate();

            Assert.That(parried, Is.True);
            Assert.That(_boss.State, Is.EqualTo(BossState.Recoil),
                "A parry always interrupts the swing.");
            yield return WaitUntil(() => _boss.State != BossState.Recoil, 3f);
            Assert.That(_boss.State, Is.Not.EqualTo(BossState.Recoil), "It recovers on its own.");
        }

        [UnityTest]
        public IEnumerator DyingBeforeTheKillResetsTheWholeEncounter()
        {
            yield return LightAltarBeforeArena();
            yield return EnterArena();
            _boss.Actor.Health.SetCurrent(_boss.Actor.Health.Max * 0.45f);
            yield return WaitUntil(() => _boss.Phase == 2, 8f);
            Assert.That(Shield().activeSelf, Is.False);
            var bossX = _boss.transform.position.x;
            _boss.transform.position = new Vector3(bossX + 5f, _boss.transform.position.y, 0f);

            _respawner.Kill();
            yield return WaitUntil(() => _player.IsAlive && !_respawner.IsSequenceRunning, 8f);

            Assert.That(_boss.State, Is.EqualTo(BossState.Dormant), "The Guardian goes back to sleep.");
            Assert.That(_boss.Phase, Is.EqualTo(1), "A retry always starts from phase one.");
            Assert.That(_boss.Actor.Health.Current, Is.EqualTo(_boss.Actor.Health.Max));
            Assert.That(Shield().activeSelf, Is.True, "The shield is back on its arm.");
            Assert.That(_boss.transform.position.x, Is.EqualTo(bossX).Within(0.5f));
            Assert.That(_encounter.IsFightActive, Is.False);
            Assert.That(Barrier().activeSelf, Is.False, "The arena opens again for the next try.");
            Assert.That(_progress.IsBossDefeated(_boss.BossId), Is.False);
        }

        [UnityTest]
        public IEnumerator TheEncounterCanBeRestartedCleanlyAfterARetry()
        {
            yield return LightAltarBeforeArena();
            yield return EnterArena();
            _respawner.Kill();
            yield return WaitUntil(() => _player.IsAlive && !_respawner.IsSequenceRunning, 8f);

            // Walk back in: the fight has to start from scratch, not resume.
            yield return EnterArena();
            Assert.That(_encounter.IsFightActive, Is.True);
            Assert.That(_boss.State, Is.EqualTo(BossState.Intro));
            Assert.That(_boss.Phase, Is.EqualTo(1));
            Assert.That(_boss.Actor.Health.Current, Is.EqualTo(_boss.Actor.Health.Max));
        }

        [UnityTest]
        public IEnumerator KillingTheGuardianOpensTheArenaAndGrantsTheRewardOnce()
        {
            yield return EnterArena();
            var defeated = 0;
            var rewards = 0;
            var rewardId = string.Empty;
            _encounter.BossDefeated += _ => defeated++;
            _encounter.RewardGranted += (id, _) => { rewards++; rewardId = id; };

            yield return KillBoss();

            Assert.That(_boss.State, Is.EqualTo(BossState.Dead));
            Assert.That(defeated, Is.EqualTo(1));
            Assert.That(rewards, Is.EqualTo(1), "The Guardian Core is handed over once.");
            Assert.That(rewardId, Is.EqualTo(_encounter.RewardId));
            Assert.That(_progress.HasReward(_encounter.RewardId), Is.True);
            Assert.That(_encounter.IsBossDefeated, Is.True);
            Assert.That(_encounter.IsFightActive, Is.False);

            yield return WaitUntil(() => !Barrier().activeSelf, 4f);
            Assert.That(Barrier().activeSelf, Is.False, "The way on opens after the kill.");
            Assert.That(SaveSystem.Read().HasReward(_encounter.RewardId), Is.True);
        }

        [UnityTest]
        public IEnumerator ADeadGuardianStopsFightingAndStaysDead()
        {
            yield return LightAltarBeforeArena();
            yield return EnterArena();
            yield return KillBoss();

            var health = _player.Health.Current;
            yield return new WaitForSeconds(1f);
            Assert.That(_player.Health.Current, Is.EqualTo(health).Within(0.01f),
                "A corpse must not land a delayed blow.");

            // Dying afterwards must not undo the victory or wake it up again.
            _respawner.Kill();
            yield return WaitUntil(() => _player.IsAlive && !_respawner.IsSequenceRunning, 8f);
            Assert.That(_progress.IsBossDefeated(_boss.BossId), Is.True);
            Assert.That(_encounter.IsBossDefeated, Is.True);

            yield return EnterArena();
            Assert.That(_encounter.IsFightActive, Is.False,
                "A beaten boss never starts another fight.");
        }

        [UnityTest]
        public IEnumerator ResetClearsCooldownsAndLastAttackMemory()
        {
            yield return EnterArena();
            yield return WaitUntil(() => _boss.LastAttack.HasValue, 12f);
            Assert.That(_boss.LastAttack.HasValue, Is.True);

            _boss.ResetBoss();
            Assert.That(_boss.LastAttack.HasValue, Is.False);
            Assert.That(_boss.CooldownRemaining(0), Is.Zero);
            Assert.That(_boss.State, Is.EqualTo(BossState.Dormant));
            Assert.That(_boss.Actor.Stagger.Current, Is.Zero);
            Assert.That(_boss.Actor.IsInvulnerable, Is.False);
        }

        private GameObject Shield()
        {
            var shield = _boss.transform.Find("Shield");
            Assert.That(shield, Is.Not.Null, "The Guardian prefab needs a shield object.");
            return shield.gameObject;
        }

        /// <summary>Taken from the encounter, because a closed barrier may be inactive.</summary>
        private GameObject Barrier()
        {
            Assert.That(_encounter.Barrier, Is.Not.Null, "The arena needs its barrier.");
            return _encounter.Barrier;
        }

        private IEnumerator LightAltarBeforeArena()
        {
            // The last altar before the Guardian: the east chapel's, off the chain shaft.
            var nearest = TombScene.TombAltars().Single(a => a.CheckpointId == "tomb-altar-02");
            _motor.ResetTo((Vector2)nearest.transform.position + new Vector2(0f, 0.1f));
            yield return new WaitForFixedUpdate();
            nearest.Interact(_player);
            yield return new WaitForFixedUpdate();
        }

        private IEnumerator EnterArena()
        {
            _motor.ResetTo(TombScene.ArenaEntry());
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
        }

        private IEnumerator KillBoss()
        {
            _boss.Actor.ClearInvulnerability();
            _boss.Actor.ApplyDamage(new DamageInfo(float.MaxValue, Vector2.left,
                isUnblockable: true, attacker: _player.gameObject));
            yield return new WaitForFixedUpdate();
        }

        private static IEnumerator WaitUntil(System.Func<bool> condition, float timeoutSeconds)
        {
            var deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!condition() && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
        }
    }
}
