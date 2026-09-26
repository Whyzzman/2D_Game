using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The one place the game is frozen. Stopping time also stops enemy AI, attack windows,
    /// cooldowns and regeneration, because every one of them is driven by scaled time — so a menu
    /// cannot be used to dodge a swing or to wait out a cooldown. Anything that must keep running
    /// while paused uses unscaled time, as the respawn fade already does.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GamePause : MonoBehaviour
    {
        private static GamePause _instance;
        private int _holds;

        public static event Action<bool> PauseChanged;

        public static bool IsPaused => _instance != null && _instance._holds > 0;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("A second GamePause was found; only one may exist.", this);
                enabled = false;
                return;
            }

            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance != this)
            {
                return;
            }

            // Leaving the scene paused would freeze the next one before it started.
            if (_holds > 0)
            {
                GameTimeScale.SetPaused(false);
            }

            _instance = null;
        }

        /// <summary>
        /// Freezes the game. Counted, so two systems pausing at once do not fight over who resumes.
        /// </summary>
        public static void Push()
        {
            if (_instance == null)
            {
                return;
            }

            _instance._holds++;
            if (_instance._holds != 1)
            {
                return;
            }

            GameTimeScale.SetPaused(true);
            PauseChanged?.Invoke(true);
        }

        public static void Pop()
        {
            if (_instance == null || _instance._holds == 0)
            {
                return;
            }

            _instance._holds--;
            if (_instance._holds > 0)
            {
                return;
            }

            GameTimeScale.SetPaused(false);
            PauseChanged?.Invoke(false);
        }

        /// <summary>Drops every hold; used by respawns and scene changes.</summary>
        public static void Clear()
        {
            if (_instance == null || _instance._holds == 0)
            {
                return;
            }

            _instance._holds = 0;
            GameTimeScale.SetPaused(false);
            PauseChanged?.Invoke(false);
        }
    }
}
