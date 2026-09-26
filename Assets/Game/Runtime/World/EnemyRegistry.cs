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
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyRegistry : MonoBehaviour
    {
        private readonly List<EnemyController> _enemies = new List<EnemyController>();

        public event Action<WorldResetReason> WorldReset;

        public int Count => _enemies.Count;

        public void Register(EnemyController enemy)
        {
            if (enemy != null && !_enemies.Contains(enemy))
            {
                _enemies.Add(enemy);
            }
        }

        public void Unregister(EnemyController enemy)
        {
            _enemies.Remove(enemy);
        }

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
