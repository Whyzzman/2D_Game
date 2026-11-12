using System.Collections;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// A boss's phases: when each begins, what it changes in the pace of the fight, and the pause
    /// between them. Phase two is every boss's (the Guardian drops his shield, the King burns); a third
    /// is the King's alone (item 1 of the 2026-11-12 list, the eclipse — gamedesign/KING_FINALE.md).
    /// </summary>
    public sealed partial class BossController
    {
        [Header("Phases")]
        [SerializeField, Min(1)] private int _totalPhases = 2;
        [SerializeField, Range(0.05f, 0.95f)] private float _phaseTwoHealthFraction = 0.5f;
        [SerializeField, Min(0f)] private float _phaseTransitionTime = 2f;
        [Tooltip("Attack speed multiplier in phase two.")]
        [SerializeField, Min(0.1f)] private float _phaseTwoAttackSpeed = 1.2f;
        [Tooltip("Pause multiplier in phase two; below 1 means shorter pauses.")]
        [SerializeField, Range(0.1f, 1f)] private float _phaseTwoPauseScale = 0.7f;
        [Tooltip("Move speed multiplier in phase two.")]
        [SerializeField, Min(0.1f)] private float _phaseTwoMoveScale = 1.15f;

        [Header("Phase three (total phases 3)")]
        [SerializeField, Range(0.05f, 0.95f)] private float _phaseThreeHealthFraction = 0.25f;
        [Tooltip("The pause into phase three: longer than the first, it is the last.")]
        [SerializeField, Min(0f)] private float _phaseThreeTransitionTime = 3f;
        [SerializeField, Min(0.1f)] private float _phaseThreeAttackSpeed = 1.4f;
        [SerializeField, Range(0.1f, 1f)] private float _phaseThreePauseScale = 0.5f;
        [SerializeField, Min(0.1f)] private float _phaseThreeMoveScale = 1.4f;

        /// <summary>Health share at which phase two begins; the boss bar marks it.</summary>
        public float PhaseTwoHealthFraction => _phaseTwoHealthFraction;

        /// <summary>Health share at which phase three begins, or 0 for a boss with two phases.</summary>
        public float PhaseThreeHealthFraction => _totalPhases >= 3 ? _phaseThreeHealthFraction : 0f;

        public int TotalPhases => _totalPhases;

        /// <summary>The phase a boss with this much of its health left should be in.</summary>
        public static int PhaseFor(float healthShare, int totalPhases, float twoAt, float threeAt)
        {
            if (totalPhases >= 3 && healthShare <= threeAt)
            {
                return 3;
            }

            return totalPhases >= 2 && healthShare <= twoAt ? 2 : 1;
        }

        private bool NextPhaseDue() => Phase < PhaseFor(_actor.Health.Normalized, _totalPhases,
            _phaseTwoHealthFraction, _phaseThreeHealthFraction);

        /// <summary>
        /// One phase further, even when a heavy blow took it past two thresholds at once: each change
        /// is seen. Untouchable while it lasts, as every phase change always was.
        /// </summary>
        private IEnumerator EnterNextPhase()
        {
            SetState(BossState.PhaseTransition);
            StopHitboxes();
            _motor.Stop();
            Phase++;
            var pause = Phase >= 3 ? _phaseThreeTransitionTime : _phaseTransitionTime;
            _actor.SetInvulnerable(ImmunitySource.Script, pause);
            _actor.Stagger?.ResetStagger();
            if (Phase == 2)
            {
                SetShieldVisible(false);
                ShieldDropped?.Invoke();
            }

            PhaseChanged?.Invoke(Phase);
            yield return new WaitForSeconds(pause);
        }

        private float AttackSpeed => Phase >= 3 ? _phaseThreeAttackSpeed : Phase >= 2 ? _phaseTwoAttackSpeed : 1f;
        private float PauseScale => Phase >= 3 ? _phaseThreePauseScale : Phase >= 2 ? _phaseTwoPauseScale : 1f;
        private float MoveScale => Phase >= 3 ? _phaseThreeMoveScale : Phase >= 2 ? _phaseTwoMoveScale : 1f;
    }
}
