using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The loop itself: die, come back at the last altar, find the world put back, rest to reset
    /// it again. Everything here runs against the saved level, not a fixture.
    /// </summary>
    public sealed class GameplayLoopTests
    {
        private PlayerCharacter _player;
        private PlayerMotor2D _motor;
        private PlayerRespawner _respawner;
        private PlayerCombatStatus _combatStatus;
        private EnemyRegistry _registry;
        private Checkpoint[] _checkpoints;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            yield return SceneManager.LoadSceneAsync("TombTrial");
            _player = Object.FindAnyObjectByType<PlayerCharacter>();
            _motor = _player.GetComponent<PlayerMotor2D>();
            _combatStatus = _player.GetComponent<PlayerCombatStatus>();
            _respawner = Object.FindAnyObjectByType<PlayerRespawner>();
            _registry = Object.FindAnyObjectByType<EnemyRegistry>();
            // The Tomb's two altars; the forest's shrines further east are not this loop.
            _checkpoints = TombScene.TombAltars();
            yield return new WaitForFixedUpdate();
        }

        [TearDown]
        public void TearDown()
        {
            SaveSystem.Delete();
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator TheLevelHasTwoAltarsAndARegisteredEnemyRoster()
        {
            Assert.That(_checkpoints.Length, Is.EqualTo(2));
            Assert.That(_checkpoints[0].CheckpointId, Is.Not.EqualTo(_checkpoints[1].CheckpointId),
                "A save has to be able to tell the altars apart.");
            Assert.That(_respawner, Is.Not.Null);
            Assert.That(_registry, Is.Not.Null);
            Assert.That(TombScene.TombEnemies().Length, Is.EqualTo(5),
                "Three zombies and two shield guards in the Tomb.");
            Assert.That(_registry.Count,
                Is.EqualTo(Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Include)
                    .Length), "Every enemy in the world is registered for resets, forest included.");
            Assert.That(_respawner.ActiveCheckpointId, Is.Empty,
                "Nothing is lit at the start; the default spawn is used.");
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator DyingWithoutAnAltarReturnsThePlayerToTheDefaultSpawn()
        {
            var spawn = _respawner.RespawnPosition;
            _motor.ResetTo(new Vector2(10f, 1f));
            yield return new WaitForFixedUpdate();

            yield return KillPlayerAndWait();

            Assert.That(_player.IsAlive, Is.True);
            Assert.That(_player.transform.position.x, Is.EqualTo(spawn.x).Within(0.6f));
            Assert.That(_player.Health.Current, Is.EqualTo(_player.Health.Max));
        }

        [UnityTest]
        public IEnumerator LightingAnAltarMakesItTheRespawnPointAndHealsThePlayer()
        {
            var altar = _checkpoints[0];
            _player.Health.Remove(60f);
            _player.Stamina.Drain(50f);
            yield return StandAt(altar);

            altar.Interact(_player);
            yield return new WaitForFixedUpdate();

            Assert.That(altar.IsActivated, Is.True);
            Assert.That(altar.IsCurrent, Is.True);
            Assert.That(_respawner.ActiveCheckpointId, Is.EqualTo(altar.CheckpointId));
            Assert.That(_player.Health.Current, Is.EqualTo(_player.Health.Max));
            Assert.That(_player.Stamina.Current, Is.EqualTo(_player.Stamina.Max));
        }

        [UnityTest]
        public IEnumerator TheSecondAltarTakesOverAsTheRespawnPoint()
        {
            yield return StandAt(_checkpoints[0]);
            _checkpoints[0].Interact(_player);
            yield return new WaitForFixedUpdate();
            Assert.That(_checkpoints[0].IsCurrent, Is.True);

            yield return StandAt(_checkpoints[1]);
            _checkpoints[1].Interact(_player);
            yield return new WaitForFixedUpdate();

            Assert.That(_checkpoints[1].IsCurrent, Is.True);
            Assert.That(_checkpoints[0].IsCurrent, Is.False,
                "Only one altar is current at a time.");
            Assert.That(_checkpoints[0].IsActivated, Is.True,
                "The first altar stays lit for future fast travel.");

            yield return KillPlayerAndWait();
            Assert.That(_player.transform.position.x,
                Is.EqualTo(_checkpoints[1].RespawnPosition.x).Within(0.6f));
        }

        [UnityTest]
        public IEnumerator TheCurrentAltarSurvivesRepeatedDeaths()
        {
            yield return StandAt(_checkpoints[1]);
            _checkpoints[1].Interact(_player);
            yield return new WaitForFixedUpdate();

            for (var i = 0; i < 2; i++)
            {
                _motor.ResetTo(new Vector2(10f, 1f));
                yield return new WaitForFixedUpdate();
                yield return KillPlayerAndWait();
                Assert.That(_respawner.ActiveCheckpointId, Is.EqualTo(_checkpoints[1].CheckpointId));
                Assert.That(_player.transform.position.x,
                    Is.EqualTo(_checkpoints[1].RespawnPosition.x).Within(0.6f));
            }

            Assert.That(_respawner.DeathCount, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator DeathPutsEveryOrdinaryEnemyBackWhereItStarted()
        {
            var zombie = FindEnemy("ZombieSoldier");
            var spawnX = zombie.transform.position.x;

            // Drag it away from its post and hurt it.
            zombie.transform.position = new Vector3(spawnX + 6f, zombie.transform.position.y, 0f);
            zombie.Actor.Health.Remove(50f);
            zombie.Actor.Stagger.Add(20f);

            yield return KillPlayerAndWait();

            Assert.That(zombie.transform.position.x, Is.EqualTo(spawnX).Within(0.5f),
                "No enemy may be left standing next to the altar.");
            Assert.That(zombie.Actor.Health.Current, Is.EqualTo(zombie.Actor.Health.Max));
            Assert.That(zombie.Actor.Stagger.Current, Is.Zero);
            Assert.That(zombie.State, Is.EqualTo(EnemyState.Idle).Or.EqualTo(EnemyState.Patrol));
            Assert.That(zombie.Combat.IsAttacking, Is.False, "No hitbox may survive the reset.");
        }

        [UnityTest]
        public IEnumerator AKilledEnemyComesBackAliveWithoutADuplicate()
        {
            var zombie = FindEnemy("ZombieSoldier");
            var before = _registry.Count;
            zombie.Actor.ClearInvulnerability();
            zombie.Actor.ApplyDamage(new DamageInfo(9999f, Vector2.right));
            yield return new WaitForFixedUpdate();
            Assert.That(zombie.State, Is.EqualTo(EnemyState.Dead));

            _respawner.ResetWorld(WorldResetReason.CheckpointRest);
            yield return new WaitForFixedUpdate();

            Assert.That(zombie.IsAlive, Is.True, "A rest brings ordinary enemies back.");
            Assert.That(zombie.Actor.Health.Current, Is.EqualTo(zombie.Actor.Health.Max));
            Assert.That(_registry.Count, Is.EqualTo(before),
                "Reviving must reuse the instance, never spawn a copy.");
        }

        [UnityTest]
        public IEnumerator AShieldGuardComesBackWithItsShieldWhole()
        {
            var shieldGuard = FindEnemy("ShieldGuard");
            shieldGuard.Guard.SpendGuard(shieldGuard.Guard.MaxGuard);
            shieldGuard.Actor.Health.Remove(80f);
            yield return new WaitForFixedUpdate();
            Assert.That(shieldGuard.Guard.IsBroken, Is.True);
            Assert.That(shieldGuard.State, Is.EqualTo(EnemyState.GuardBroken));

            _respawner.ResetWorld(WorldResetReason.CheckpointRest);
            yield return new WaitForFixedUpdate();

            Assert.That(shieldGuard.Guard.IsBroken, Is.False);
            Assert.That(shieldGuard.Guard.CurrentGuard, Is.EqualTo(shieldGuard.Guard.MaxGuard));
            Assert.That(shieldGuard.Guard.Pressure, Is.Zero);
            Assert.That(shieldGuard.Guard.IsRaised, Is.False);
            Assert.That(shieldGuard.Actor.Health.Current, Is.EqualTo(shieldGuard.Actor.Health.Max));
            Assert.That(shieldGuard.State, Is.EqualTo(EnemyState.Idle)
                .Or.EqualTo(EnemyState.Patrol));
        }

        [UnityTest]
        public IEnumerator RestingRestoresThePlayerAndTheWorldWithoutMovingHim()
        {
            var altar = _checkpoints[0];
            yield return StandAt(altar);
            altar.Interact(_player);
            yield return new WaitForFixedUpdate();

            var zombie = FindEnemy("ZombieSoldier");
            zombie.Actor.ClearInvulnerability();
            zombie.Actor.ApplyDamage(new DamageInfo(9999f, Vector2.right));
            _player.Health.Remove(70f);
            _player.Stamina.Drain(60f);
            _combatStatus.Clear();
            yield return new WaitForFixedUpdate();

            // A lit altar opens its menu; resting is its first entry.
            altar.Interact(_player);
            var menu = Object.FindAnyObjectByType<ShrineMenu>();
            Assert.That(menu.IsOpen, Is.True, "A lit altar offers its menu.");
            menu.Rest();
            yield return WaitUntil(() => !_respawner.IsSequenceRunning, 6f);

            Assert.That(_player.Health.Current, Is.EqualTo(_player.Health.Max));
            Assert.That(_player.Stamina.Current, Is.EqualTo(_player.Stamina.Max));
            Assert.That(zombie.IsAlive, Is.True, "Resting brings the enemies back.");
            Assert.That(_player.transform.position.x,
                Is.EqualTo(altar.RespawnPosition.x).Within(0.6f),
                "A rest leaves the player at the altar.");
        }

        [UnityTest]
        public IEnumerator RestingIsRefusedWhileTheFightIsStillOn()
        {
            var altar = _checkpoints[0];
            yield return StandAt(altar);
            altar.Interact(_player);
            yield return new WaitForFixedUpdate();

            _combatStatus.Report();
            Assert.That(_combatStatus.IsInCombat, Is.True);
            Assert.That(altar.CanInteract, Is.False, "No resting mid-fight.");
            Assert.That(altar.Prompt, Does.Contain("бою"));

            _combatStatus.Clear();
            Assert.That(altar.CanInteract, Is.True);
        }

        [UnityTest]
        public IEnumerator FallingIntoThePitKillsThroughTheOrdinaryDeathFlow()
        {
            yield return StandAt(_checkpoints[0]);
            _checkpoints[0].Interact(_player);
            yield return new WaitForFixedUpdate();

            var deaths = _respawner.DeathCount;
            // Straight into the gap between the floor and the corridor.
            _motor.ResetTo(new Vector2(21.75f, -4f));
            yield return WaitUntil(() => _respawner.DeathCount > deaths, 4f);

            Assert.That(_respawner.DeathCount, Is.EqualTo(deaths + 1),
                "The pit kills; it does not quietly teleport.");
            yield return WaitUntil(() => _player.IsAlive && !_respawner.IsSequenceRunning, 8f);
            Assert.That(_player.IsAlive, Is.True, "The death flow has to finish on its own.");
            Assert.That(_player.transform.position.x,
                Is.EqualTo(_checkpoints[0].RespawnPosition.x).Within(0.6f));
        }

        [UnityTest]
        public IEnumerator DyingMidActionLeavesNoStaleCombatStateBehind()
        {
            yield return StandAt(_checkpoints[0]);
            _checkpoints[0].Interact(_player);
            yield return new WaitForFixedUpdate();

            // Break the guard, then die while helpless and mid-dash.
            _player.Defense.BeginBlock();
            _player.Stamina.Drain(_player.Stamina.Current);
            _player.Actor.ApplyDamage(new DamageInfo(1f, Vector2.left, guardDamage: 50f));
            _motor.TryBeginDash(1);
            yield return new WaitForFixedUpdate();

            yield return KillPlayerAndWait();

            Assert.That(_player.Action, Is.EqualTo(PlayerAction.None));
            Assert.That(_player.Defense.IsBlocking, Is.False);
            Assert.That(_motor.IsDashing, Is.False);
            Assert.That(_player.Stagger.IsStunned, Is.False);
            Assert.That(_player.Stamina.Current, Is.EqualTo(_player.Stamina.Max));
            Assert.That(Mathf.Abs(_motor.Velocity.y), Is.LessThan(1f),
                "No old fall speed survives the respawn.");
        }

        [UnityTest]
        public IEnumerator RespawnGrantsAShortGraceWindow()
        {
            yield return StandAt(_checkpoints[0]);
            _checkpoints[0].Interact(_player);
            yield return new WaitForFixedUpdate();
            yield return KillPlayerAndWait();

            Assert.That(_player.Actor.Invulnerability.IsActive(ImmunitySource.Respawn), Is.True,
                "A stray blow must not kill on arrival.");
            var result = _player.Actor.ApplyDamage(new DamageInfo(30f, Vector2.left));
            Assert.That(result.Outcome, Is.EqualTo(DamageOutcome.Ignored));
        }

        [UnityTest]
        public IEnumerator TheFadeHooksFireAroundEveryRespawn()
        {
            var fadeOut = 0;
            var fadeIn = 0;
            _respawner.FadeOutRequested += _ => fadeOut++;
            _respawner.FadeInRequested += _ => fadeIn++;

            yield return KillPlayerAndWait();

            Assert.That(fadeOut, Is.EqualTo(1), "The HUD needs a hook to fade out on.");
            Assert.That(fadeIn, Is.EqualTo(1));
        }

        private EnemyController FindEnemy(string prefabName)
        {
            var enemy = Object.FindObjectsByType<EnemyController>()
                .FirstOrDefault(candidate => candidate.name.StartsWith(prefabName));
            Assert.That(enemy, Is.Not.Null, $"The level needs a {prefabName}.");
            return enemy;
        }

        private IEnumerator StandAt(Checkpoint altar)
        {
            _combatStatus.Clear();
            _motor.ResetTo(new Vector2(altar.transform.position.x,
                altar.transform.position.y + 0.1f));
            yield return new WaitForFixedUpdate();
        }

        private IEnumerator KillPlayerAndWait()
        {
            _respawner.Kill();
            Assert.That(_player.IsAlive, Is.False, "The killing blow has to land.");
            yield return WaitUntil(() => _player.IsAlive && !_respawner.IsSequenceRunning, 8f);
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
