using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The first real encounter, driven in the saved level: patrol and its edges, sight and the
    /// wall that breaks it, the telegraphed swing, and how the enemy answers the player's defence.
    /// </summary>
    public sealed class ZombieSoldierTests
    {
        private Keyboard _keyboard;
        private PlayerCharacter _player;
        private PlayerMotor2D _motor;
        private EnemyController[] _zombies;
        private InputSettings _previousSettings;
        private InputSettings _testSettings;
        private bool _wasRunningInBackground;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _previousSettings = InputSystem.settings;
            if (_previousSettings.hideFlags == HideFlags.HideAndDontSave)
            {
                _previousSettings = Object.Instantiate(_previousSettings);
            }
            _testSettings = Object.Instantiate(_previousSettings);
            _testSettings.hideFlags = HideFlags.HideAndDontSave;
            _testSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            _testSettings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings = _testSettings;
            _wasRunningInBackground = Application.runInBackground;
            Application.runInBackground = true;
            _keyboard = InputSystem.AddDevice<Keyboard>("ZombieTestKeyboard");

            SaveSystem.Delete();
            yield return SceneManager.LoadSceneAsync("TombTrial");
            _player = Object.FindAnyObjectByType<PlayerCharacter>();
            _motor = _player.GetComponent<PlayerMotor2D>();
            _zombies = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None)
                .Where(enemy => enemy.name.StartsWith("ZombieSoldier") && TombScene.InTomb(enemy))
                .OrderBy(enemy => enemy.transform.position.x)
                .ToArray();
            yield return Neutral(4);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_keyboard != null) { InputSystem.RemoveDevice(_keyboard); }
            InputSystem.settings = _previousSettings;
            Object.Destroy(_testSettings);
            Application.runInBackground = _wasRunningInBackground;
            GameTimeScale.Reset();
            SaveSystem.Delete();
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheLevelHasThreeZombiesWithIndependentStates()
        {
            Assert.That(_zombies.Length, Is.EqualTo(3));
            foreach (var zombie in _zombies)
            {
                Assert.That(zombie.Actor.Health.Current, Is.EqualTo(zombie.Actor.Health.Max));
                Assert.That(zombie.IsAlive, Is.True);
            }

            // Aggro only the first one; the others must carry on by themselves.
            var first = _zombies[0];
            yield return StandNextTo(first, 3f);
            yield return WaitUntil(() => first.State != EnemyState.Idle
                && first.State != EnemyState.Patrol, 4f);

            Assert.That(first.State, OneOf(EnemyState.Alert, EnemyState.Chase,
                EnemyState.Attack));
            Assert.That(_zombies[2].State, OneOf(EnemyState.Idle, EnemyState.Patrol),
                "A distant zombie must not share the first one's target.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator PatrolTurnsAroundInsteadOfWalkingOffTheLedge()
        {
            var ledgeZombie = _zombies[1];
            var startY = ledgeZombie.transform.position.y;
            // Keep the player far away so the zombie keeps patrolling.
            _motor.ResetTo(new Vector2(-12f, 0.85f));
            yield return Realtime(6f);

            Assert.That(ledgeZombie.transform.position.y, Is.GreaterThan(startY - 1f),
                "The zombie must turn at the edge instead of dropping off.");
            Assert.That(ledgeZombie.State, OneOf(EnemyState.Idle, EnemyState.Patrol));
        }

        [UnityTest]
        public IEnumerator AWallBlocksSightUntilThePlayerStepsAround()
        {
            var hidden = _zombies[2];
            // Within detection range, but the pillar stands between the two.
            _motor.ResetTo(new Vector2(hidden.transform.position.x - 6f, 0.85f));
            yield return Realtime(1.5f);

            Assert.That(hidden.State, OneOf(EnemyState.Idle, EnemyState.Patrol),
                "Sight must not pass through solid level geometry.");

            yield return StandNextTo(hidden, 2f);
            yield return WaitUntil(() => hidden.State != EnemyState.Idle
                && hidden.State != EnemyState.Patrol, 4f);
            Assert.That(hidden.State, OneOf(EnemyState.Alert, EnemyState.Chase,
                EnemyState.Attack), "With a clear line the zombie has to notice the player.");
        }

        [UnityTest]
        public IEnumerator TheZombieTelegraphsBeforeItsSwingConnects()
        {
            var zombie = _zombies[0];
            yield return StandNextTo(zombie, 1.3f);

            var telegraphedAt = -1f;
            zombie.Combat.AttackTelegraphed += _ => telegraphedAt = Time.realtimeSinceStartup;
            var landedAt = -1f;
            zombie.Combat.AttackLanded += _ => landedAt = Time.realtimeSinceStartup;

            yield return WaitUntil(() => landedAt > 0f, 8f);
            Assert.That(telegraphedAt, Is.GreaterThan(0f), "The windup must announce the swing.");
            Assert.That(landedAt - telegraphedAt, Is.GreaterThan(0.25f),
                "The player needs time to read the tell and react.");
        }

        [UnityTest]
        public IEnumerator AnUnguardedSwingHurtsThePlayer()
        {
            var zombie = _zombies[0];
            yield return StandNextTo(zombie, 1.3f);

            var health = _player.Health.Current;
            yield return WaitUntil(() => _player.Health.Current < health, 10f);
            Assert.That(_player.Health.Current, Is.LessThan(health),
                $"The swing has to connect. state={zombie.State} "
                + $"zombieX={zombie.transform.position.x:0.00} "
                + $"playerX={_player.transform.position.x:0.00}");
        }

        [UnityTest]
        public IEnumerator BlockingAZombieSwingCostsStaminaInsteadOfHealth()
        {
            var zombie = _zombies[0];
            var blocked = false;
            DamageResult lastResult = default;
            _player.Blocked += _ => blocked = true;
            _player.Actor.DamageTaken += (_, result) => lastResult = result;

            // Knockback slides the player out of reach, so close the gap again between swings and
            // keep him alive; the check is about what a block does, not about surviving a fight.
            for (var attempt = 0; attempt < 8 && !blocked; attempt++)
            {
                yield return StandNextTo(zombie, 1.3f);
                _player.Health.RestoreToFull();
                _player.Stamina.RestoreToFull();
                InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Q));
                yield return Realtime(0.35f);
                var guardedHealth = _player.Health.Current;
                var stamina = _player.Stamina.Current;

                yield return WaitUntil(() => blocked || lastResult.Outcome == DamageOutcome.Hit,
                    3.5f);
                if (!blocked)
                {
                    lastResult = default;
                    yield return Neutral(2);
                    continue;
                }

                Assert.That(_player.Stamina.Current, Is.LessThan(stamina),
                    "Blocking pays with stamina.");
                Assert.That(_player.Health.Current, Is.EqualTo(guardedHealth).Within(0.01f),
                    "A blocked swing must not reach health.");
            }

            Assert.That(blocked, Is.True, "A raised guard has to stop a zombie slash.");
            yield return Neutral(4);
        }

        [UnityTest]
        public IEnumerator APerfectParryBreaksTheSwingAndStaggersTheZombie()
        {
            var zombie = _zombies[0];
            yield return StandNextTo(zombie, 1.3f);

            var parried = false;
            zombie.Parried += _ => parried = true;
            var poiseBefore = zombie.Actor.Stagger.Current;
            var health = _player.Health.Current;

            for (var attempt = 0; attempt < 6 && !parried; attempt++)
            {
                // Wait for the swing to be almost out of its windup, then raise the guard: that is
                // exactly the read a player makes off the tell.
                var windup = -1f;
                yield return WaitUntil(() =>
                {
                    var attack = zombie.Combat.CurrentAttack;
                    if (attack == null || zombie.Combat.Phase != AttackPhase.Windup)
                    {
                        return false;
                    }

                    windup = attack.Windup;
                    return zombie.Combat.Elapsed >= windup - 0.1f;
                }, 10f);

                if (windup < 0f)
                {
                    break;
                }

                InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Q));
                yield return Realtime(0.3f);
                yield return Neutral(2);
            }

            Assert.That(parried, Is.True,
                $"A well timed guard has to parry the slash. state={zombie.State} "
                + $"guard={_player.Defense.Phase} action={_player.Action} "
                + $"zombieX={zombie.transform.position.x:0.00} "
                + $"playerX={_player.transform.position.x:0.00} "
                + $"playerHp={_player.Health.Current:0}");
            Assert.That(_player.Health.Current, Is.EqualTo(health).Within(0.01f),
                "A parry costs the player nothing.");
            Assert.That(zombie.Actor.Stagger.Current, Is.GreaterThan(poiseBefore),
                "The parry has to load the zombie's poise.");
            Assert.That(zombie.Combat.IsAttacking, Is.False, "The swing is cancelled outright.");
            yield return Neutral(4);
        }

        [UnityTest]
        public IEnumerator BreakingPoiseStunsTheZombieAndStopsItsAi()
        {
            var zombie = _zombies[0];
            yield return StandNextTo(zombie, 2.5f);
            yield return Realtime(0.6f);

            zombie.Actor.ClearInvulnerability();
            zombie.Actor.ApplyDamage(new DamageInfo(1f, Vector2.right,
                staggerDamage: zombie.Actor.Stagger.Threshold + 5f, attacker: _player.gameObject));
            yield return new WaitForFixedUpdate();

            Assert.That(zombie.State, Is.EqualTo(EnemyState.Stunned));
            Assert.That(zombie.Actor.Stagger.IsStunned, Is.True);
            Assert.That(zombie.Combat.IsAttacking, Is.False, "A stun cancels the swing.");

            var position = zombie.transform.position.x;
            yield return Realtime(0.5f);
            Assert.That(zombie.transform.position.x, Is.EqualTo(position).Within(0.2f),
                "A stunned zombie does not chase.");
            Assert.That(zombie.State, Is.EqualTo(EnemyState.Stunned));
        }

        [UnityTest]
        public IEnumerator ADeadZombieStopsFightingAtOnce()
        {
            var zombie = _zombies[0];
            yield return StandNextTo(zombie, 1.3f);
            yield return Realtime(0.8f);

            zombie.Actor.ClearInvulnerability();
            zombie.Actor.ApplyDamage(new DamageInfo(9999f, Vector2.right,
                attacker: _player.gameObject));
            yield return new WaitForFixedUpdate();

            Assert.That(zombie.State, Is.EqualTo(EnemyState.Dead));
            Assert.That(zombie.Combat.IsAttacking, Is.False);
            Assert.That(zombie.IsAlive, Is.False);

            var health = _player.Health.Current;
            yield return Realtime(0.8f);
            Assert.That(_player.Health.Current, Is.EqualTo(health).Within(0.01f),
                "A corpse must not land a delayed hit.");
        }

        [UnityTest]
        public IEnumerator LosingThePlayerSendsTheZombieHome()
        {
            var zombie = _zombies[0];
            var spawnX = zombie.transform.position.x;
            yield return StandNextTo(zombie, 2.5f);
            yield return WaitUntil(() => zombie.State == EnemyState.Chase
                || zombie.State == EnemyState.Attack, 6f);

            // Leave the area entirely.
            _motor.ResetTo(new Vector2(-12f, 0.85f));
            yield return WaitUntil(() => zombie.State == EnemyState.ReturnToPatrol
                || zombie.State == EnemyState.Idle || zombie.State == EnemyState.Patrol, 12f);

            Assert.That(zombie.State, OneOf(EnemyState.Search, EnemyState.ReturnToPatrol,
                EnemyState.Idle, EnemyState.Patrol));
            yield return Realtime(6f);
            Assert.That(Mathf.Abs(zombie.transform.position.x - spawnX), Is.LessThan(5f),
                "The zombie walks back to its patrol area instead of wandering off.");
        }

        [UnityTest]
        public IEnumerator ResetEnemyPutsTheZombieBackTheWayItSpawned()
        {
            var zombie = _zombies[0];
            var spawnX = zombie.transform.position.x;
            yield return StandNextTo(zombie, 2f);
            yield return Realtime(1.5f);

            zombie.Actor.Health.Remove(40f);
            zombie.Actor.Stagger.Add(20f);
            Assert.That(zombie.Actor.Health.Current, Is.LessThan(zombie.Actor.Health.Max));
            Assert.That(zombie.Actor.Stagger.Current, Is.GreaterThan(0f));

            zombie.ResetEnemy();
            yield return new WaitForFixedUpdate();

            Assert.That(zombie.Actor.Health.Current, Is.EqualTo(zombie.Actor.Health.Max));
            Assert.That(zombie.Actor.Stagger.Current, Is.Zero);
            Assert.That(zombie.Actor.Stagger.IsStunned, Is.False);
            Assert.That(zombie.State, Is.EqualTo(EnemyState.Idle));
            Assert.That(zombie.transform.position.x, Is.EqualTo(spawnX).Within(0.35f));
            Assert.That(zombie.Combat.IsAttacking, Is.False);
        }

        [UnityTest]
        public IEnumerator TheZombieGivesUpOnADeadPlayer()
        {
            var zombie = _zombies[0];
            yield return StandNextTo(zombie, 2.5f);
            yield return WaitUntil(() => zombie.State == EnemyState.Chase
                || zombie.State == EnemyState.Attack, 6f);

            _player.Actor.ClearInvulnerability();
            _player.Actor.ApplyDamage(new DamageInfo(9999f, Vector2.right));
            Assert.That(_player.IsAlive, Is.False);

            yield return WaitUntil(() => zombie.State != EnemyState.Chase
                && zombie.State != EnemyState.Attack, 6f);
            Assert.That(zombie.State, OneOf(EnemyState.ReturnToPatrol, EnemyState.Idle,
                EnemyState.Patrol, EnemyState.Search),
                "The zombie must not keep swinging at a corpse.");
        }

        [UnityTest]
        public IEnumerator ALongFightInReachNeverRunsOutOfChaseTime()
        {
            // The chase limit used to keep counting through the whole fight: after 12–16 s the
            // zombie gave up mid-swing, searched, re-acquired the player and stood for its alert
            // time — a free opening every few exchanges. Standing in reach, that must not happen.
            var zombie = _zombies[0];
            _player.Actor.SetInvulnerable(ImmunitySource.Script, 1000f);
            yield return StandNextTo(zombie, 1.2f);
            yield return WaitUntil(() => zombie.State == EnemyState.Chase
                || zombie.State == EnemyState.Attack, 6f);
            Assume.That(zombie.State, OneOf(EnemyState.Chase, EnemyState.Attack));

            var alerts = 0;
            zombie.Alerted += () => alerts++;
            GameTimeScale.SetBaseScale(4f);
            var end = Time.time + zombie.Definition.MaxChaseTime * 3f;
            while (Time.time < end)
            {
                Assert.That(zombie.State, Is.Not.EqualTo(EnemyState.Search),
                    "The zombie lost interest in a player standing in reach.");
                yield return null;
            }

            GameTimeScale.SetBaseScale(1f);
            Assert.That(alerts, Is.Zero, "The zombie had to find the player again mid-fight.");
            _player.Actor.ClearInvulnerability();
        }

        /// <summary>Accepts any of several states, for checks where the exact tick does not matter.</summary>
        private static NUnit.Framework.Constraints.EqualConstraint OneOf(params EnemyState[] states)
        {
            var constraint = Is.EqualTo(states[0]);
            for (var i = 1; i < states.Length; i++)
            {
                constraint = constraint.Or.EqualTo(states[i]);
            }

            return constraint;
        }

        /// <summary>
        /// Puts the player in front of the zombie and turned toward it, which is the only position
        /// where detection, blocking and parrying are all meaningful.
        /// </summary>
        private IEnumerator StandNextTo(EnemyController zombie, float distance)
        {
            var side = zombie.FacingDirection;
            _motor.ResetTo(new Vector2(zombie.transform.position.x + side * distance,
                zombie.transform.position.y + 0.1f));
            yield return Neutral(2);

            // ResetTo faces the player right; tap the other way when the zombie is on the left.
            if (side > 0f)
            {
                InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.A));
                yield return Realtime(0.12f);
                yield return Neutral(2);
            }
        }

        private IEnumerator Neutral(int frames)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return null;
            for (var i = 0; i < frames; i++)
            {
                yield return new WaitForFixedUpdate();
            }
            yield return null;
        }

        private static IEnumerator WaitUntil(System.Func<bool> condition, float timeoutSeconds)
        {
            var deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!condition() && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
        }

        /// <summary>Hit stop freezes the scaled clock, so waits run on real time.</summary>
        private static IEnumerator Realtime(float seconds)
        {
            var end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end)
            {
                yield return null;
            }
            yield return new WaitForFixedUpdate();
        }
    }
}
