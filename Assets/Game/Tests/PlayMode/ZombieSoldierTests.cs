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
            yield return TestWorld.Load();
            _player = Object.FindAnyObjectByType<PlayerCharacter>();
            _motor = _player.GetComponent<PlayerMotor2D>();
            _zombies = Object.FindObjectsByType<EnemyController>()
                // The guards' passage: one on the open floor, one on a dais with two drops, one behind a pillar.
                .Where(enemy => enemy.name.StartsWith("ZombieSoldier") && TombScene.InRoom(enemy, "room_tomb_guard_passage"))
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
            _motor.ResetTo(TombScene.Ground(0f));
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
            _motor.ResetTo(new Vector2(hidden.transform.position.x - 6f, hidden.transform.position.y - 0.05f));
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
            var lastHit = string.Empty;
            _player.Blocked += _ => blocked = true;
            _player.Actor.DamageTaken += (info, result) =>
            {
                lastResult = result;
                lastHit = $"{(info.Attacker != null ? info.Attacker.name : "?")} dir {info.HitDirection.x:0.0}";
            };

            // Knockback slides the player out of reach, so close the gap again between swings and
            // keep him alive; the check is about what a block does, not about surviving a fight.
            var attempts = new System.Text.StringBuilder();
            for (var attempt = 0; attempt < 8 && !blocked; attempt++)
            {
                yield return StandNextTo(zombie, 1.3f);
                _player.Health.RestoreToFull();
                _player.Stamina.RestoreToFull();
                attempts.Append($" [{attempt}: zombie {zombie.transform.position.x:0.00} facing {zombie.FacingDirection} {zombie.State},"
                    + $" hero {_player.transform.position.x:0.00} facing {_motor.FacingDirection} {_player.Action}");
                InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Q));
                yield return Realtime(0.35f);
                var guardedHealth = _player.Health.Current;
                var stamina = _player.Stamina.Current;

                yield return WaitUntil(() => blocked || lastResult.Outcome == DamageOutcome.Hit,
                    3.5f);
                attempts.Append($" -> {(blocked ? "blocked" : lastResult.Outcome.ToString())} by {lastHit}, hero {_player.transform.position.x:0.00}"
                    + $" facing {_motor.FacingDirection} guard {_player.Defense.Phase}]");
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

            Assert.That(blocked, Is.True, "A raised guard has to stop a zombie slash." + attempts);
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

                // A slow frame can spoil an attempt; only the one that parries is judged on cost.
                health = _player.Health.Current;
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
            _motor.ResetTo(TombScene.Ground(0f));
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

        [UnityTest]
        public IEnumerator AGroupTakesTurnsRatherThanAllSwingingAtOnce()
        {
            // Three zombies around one player: without tokens every telegraph lands together.
            var registry = Object.FindAnyObjectByType<EnemyRegistry>();
            Assume.That(registry, Is.Not.Null);
            Assume.That(_zombies.Length, Is.GreaterThanOrEqualTo(3));
            _player.Actor.SetInvulnerable(ImmunitySource.Script, 1000f);
            var at = (Vector2)_player.transform.position;
            var offsets = new[] { 1.1f, -1.1f, 1.4f };
            var attacks = 0;
            for (var i = 0; i < 3; i++)
            {
                var motor = _zombies[i].GetComponent<EnemyMotor2D>();
                motor.Teleport(at + new Vector2(offsets[i], 0.1f));
                motor.Face(offsets[i] > 0f ? -1 : 1);
                _zombies[i].AttackTelegraphed += _ => attacks++;
            }

            GameTimeScale.SetBaseScale(2f);
            var most = 0;
            var end = Time.realtimeSinceStartup + 6f;
            while (Time.realtimeSinceStartup < end)
            {
                var swinging = _zombies.Take(3).Count(zombie => zombie.Combat.IsAttacking);
                most = Mathf.Max(most, swinging);
                yield return null;
            }

            GameTimeScale.SetBaseScale(1f);
            _player.Actor.ClearInvulnerability();
            Assume.That(attacks, Is.GreaterThanOrEqualTo(3), "The group has to actually fight.");
            Assert.That(most, Is.LessThanOrEqualTo(registry.MaxSimultaneousAttackers),
                "More zombies swung at once than the tokens allow.");
        }

        [UnityTest]
        public IEnumerator AZombieStruckFromOutOfSightGoesAfterTheAttacker()
        {
            // Spells reach 12 to 13; a zombie sees 8. Hit from past that, it used to flinch and
            // walk home, so it could be shot down for free.
            var zombie = _zombies[0];
            var origin = (Vector2)zombie.transform.position;
            var spot = Vector2.zero;
            foreach (var side in new[] { -1f, 1f })
            {
                var candidate = origin + new Vector2(side * 10.5f, 0.5f);
                if (Physics2D.Raycast(candidate, Vector2.down, 3f, 1 << 9))
                {
                    spot = candidate;
                    break;
                }
            }

            Assume.That(spot, Is.Not.EqualTo(Vector2.zero), "Needs floor 10.5 away from the zombie.");
            _player.Actor.SetInvulnerable(ImmunitySource.Script, 1000f);
            _motor.ResetTo(spot);
            yield return Realtime(0.3f);
            Assume.That(zombie.State, OneOf(EnemyState.Idle, EnemyState.Patrol));
            var towardPlayer = spot.x > zombie.transform.position.x ? 1 : -1;

            zombie.Actor.ApplyDamage(new DamageInfo(5f, new Vector2(-towardPlayer, 0f),
                attacker: _player.gameObject, hitPoint: zombie.transform.position));
            yield return Realtime(1f);

            Assert.That(zombie.State, OneOf(EnemyState.Search, EnemyState.Alert,
                EnemyState.Chase, EnemyState.Attack), "It went home instead of after the attacker.");
            Assert.That(zombie.FacingDirection, Is.EqualTo(towardPlayer), "It turned toward the blow.");
            _player.Actor.ClearInvulnerability();
        }

        [UnityTest]
        public IEnumerator AZombieGivesGroundToAHeroInItsFace()
        {
            // Between swings it used to stand still, and a hero pressed against it got walked through and struck in
            // the back. Now it backs off a hero inside its near line (EnemySpacing), still facing him.
            var zombie = _zombies[0];
            var zombieMotor = zombie.GetComponent<EnemyMotor2D>();
            _player.Actor.SetInvulnerable(ImmunitySource.Script, 1000f);
            yield return StandNextTo(zombie, 0.35f);

            var backedOff = false;
            var deadline = Time.realtimeSinceStartup + 6f;
            while (!backedOff && Time.realtimeSinceStartup < deadline)
            {
                var toHero = Mathf.Sign(_player.transform.position.x - zombie.transform.position.x);
                backedOff = zombie.State == EnemyState.Chase && zombieMotor.Velocity.x * toHero < -0.1f
                    && zombie.FacingDirection == (int)toHero;
                yield return null;
            }

            _player.Actor.ClearInvulnerability();
            Assert.That(backedOff, Is.True, $"It never stepped back. state={zombie.State} "
                + $"gap={Mathf.Abs(_player.transform.position.x - zombie.transform.position.x):0.00}");
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

            // ResetTo faces the player right; turn him to the zombie on the spot. A tap of the key used to do it,
            // and on slow frames it carried him half a metre on, into the zombie: walking through him, it turned
            // and struck his back (2026-11-06, the block test failed every attempt).
            _motor.Face(side > 0f ? -1 : 1);
            yield return Neutral(2);
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
