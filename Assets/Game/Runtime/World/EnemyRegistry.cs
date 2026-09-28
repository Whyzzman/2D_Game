using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>What brings an enemy back after it has been beaten.</summary>
    public enum EnemyResetPolicy
    {
        /// <summary>Ordinary enemies: back on a rest and on the player's death.</summary>
        RespawnOnRestOrDeath,
        /// <summary>Elites: only a rest at an altar brings them back.</summary>
        RespawnOnRestOnly,
        /// <summary>Bosses and one-off encounters: once beaten, they stay beaten.</summary>
        NeverRespawn
    }

    /// <summary>Why the world is being put back the way it was.</summary>
    public enum WorldResetReason
    {
        PlayerDeath,
        CheckpointRest,
        Debug
    }

    /// <summary>
    /// Keeps the list of enemies that can be put back. Enemies add themselves when they wake up, so
    /// a reset never has to search the scene, and runtime-spawned enemies work the same way.
    ///
    /// It also hands out attack tokens: only so many ordinary enemies may be swinging or shooting
    /// at once, so a group takes turns instead of landing every telegraph in the same instant.
    /// A token is held for as long as its enemy is attacking and frees itself the moment it is
    /// not, whatever ended the attack — a finished swing, a parry, a stagger, a death, a reset.
    /// Bosses run their own fights and take no tokens.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyRegistry : MonoBehaviour
    {
        [Tooltip("How many ordinary enemies may attack at the same time, ranged ones included.")]
        [SerializeField, Min(1)] private int _maxSimultaneousAttackers = 2;
        private readonly List<EnemyController> _enemies = new List<EnemyController>();
        private readonly List<EnemyController> _attackers = new List<EnemyController>();
        // Who lies beaten since the last rest or death, by where they stand: a zone unloaded and
        // loaded again brings its enemies back from the scene, and this keeps them beaten, as
        // they would have stayed had the zone never gone.
        private readonly Dictionary<string, EnemyResetPolicy> _beaten =
            new Dictionary<string, EnemyResetPolicy>();

        public event Action<WorldResetReason> WorldReset;

        public int Count => _enemies.Count;

        /// <summary>Whether the enemy that stands at this spawn is beaten until the next reset.</summary>
        public bool IsBeaten(string spawnKey) => _beaten.ContainsKey(spawnKey);

        public int MaxSimultaneousAttackers => _maxSimultaneousAttackers;

        /// <summary>How many tokens are out right now, after freeing those no longer attacking.</summary>
        public int ActiveAttackers
        {
            get
            {
                ReleaseIdleTokens();
                return _attackers.Count;
            }
        }

        /// <summary>
        /// Asks to start an attack. True when the enemy already holds a token or one is free;
        /// the enemy must then begin the attack at once, or the token frees itself next time.
        /// </summary>
        public bool TryTakeAttackToken(EnemyController enemy)
        {
            if (enemy == null)
            {
                return false;
            }

            ReleaseIdleTokens();
            if (_attackers.Contains(enemy))
            {
                return true;
            }

            if (_attackers.Count >= _maxSimultaneousAttackers)
            {
                return false;
            }

            _attackers.Add(enemy);
            return true;
        }

        private void ReleaseIdleTokens()
        {
            for (var i = _attackers.Count - 1; i >= 0; i--)
            {
                var attacker = _attackers[i];
                if (attacker == null || !attacker.IsAlive || !attacker.isActiveAndEnabled
                    || attacker.Combat == null || !attacker.Combat.IsAttacking)
                {
                    _attackers.RemoveAt(i);
                }
            }
        }

        public void Register(EnemyController enemy)
        {
            if (enemy == null || _enemies.Contains(enemy))
            {
                return;
            }

            _enemies.Add(enemy);
            enemy.Died += OnEnemyDied;
            if (_beaten.ContainsKey(enemy.SpawnKey))
            {
                enemy.LieBeaten();
            }
        }

        public void Unregister(EnemyController enemy)
        {
            if (enemy != null)
            {
                enemy.Died -= OnEnemyDied;
            }

            _enemies.Remove(enemy);
            _attackers.Remove(enemy);
        }

        private void OnEnemyDied(EnemyController enemy) => _beaten[enemy.SpawnKey] = enemy.ResetPolicy;

        /// <summary>
        /// Puts every enemy whose policy allows it back at its spawn. Called under the fade, so the
        /// player never sees them jump.
        /// </summary>
        public void ResetAll(WorldResetReason reason)
        {
            for (var i = _enemies.Count - 1; i >= 0; i--)
            {
                var enemy = _enemies[i];
                if (enemy == null)
                {
                    _enemies.RemoveAt(i);
                    continue;
                }

                // Targets are dropped first so nothing re-acquires the player mid-reset.
                enemy.ClearTarget();
                if (ShouldRespawn(enemy.ResetPolicy, reason))
                {
                    enemy.ResetEnemy();
                }
            }

            // Those of zones not loaded now come back when their zone does.
            var revived = new List<string>();
            foreach (var beaten in _beaten)
            {
                if (ShouldRespawn(beaten.Value, reason))
                {
                    revived.Add(beaten.Key);
                }
            }

            foreach (var key in revived)
            {
                _beaten.Remove(key);
            }

            WorldReset?.Invoke(reason);
        }

        private static bool ShouldRespawn(EnemyResetPolicy policy, WorldResetReason reason)
        {
            switch (policy)
            {
                case EnemyResetPolicy.NeverRespawn:
                    return false;
                case EnemyResetPolicy.RespawnOnRestOnly:
                    return reason != WorldResetReason.PlayerDeath;
                default:
                    return true;
            }
        }
    }
}
