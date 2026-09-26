using System;

namespace ForgottenKingdom
{
    public enum AttackPhase
    {
        None,
        Windup,
        Active,
        Recovery
    }

    /// <summary>
    /// Advances a single attack through its windup, active and recovery windows. Kept free of Unity
    /// types so attack timing can be tested without entering Play Mode.
    /// </summary>
    public sealed class AttackRunner
    {
        private float _speed = 1f;

        public AttackDefinition Current { get; private set; }
        public AttackPhase Phase { get; private set; } = AttackPhase.None;
        public float Elapsed { get; private set; }
        public bool IsBusy => Phase != AttackPhase.None;

        /// <summary>True once a buffered follow-up is allowed to replace this attack.</summary>
        public bool CanChain => Current != null && Elapsed >= Current.ComboLinkTime / _speed;

        /// <summary>True once a dash may cancel the remaining frames.</summary>
        public bool CanCancel => Current != null && Elapsed >= Current.CancelableAfter / _speed;

        public event Action<AttackDefinition> ActiveStarted;
        public event Action ActiveEnded;
        public event Action<AttackDefinition> Finished;

        public void Begin(AttackDefinition attack, float speed)
        {
            if (attack == null)
            {
                return;
            }

            EndActiveWindow();
            Current = attack;
            _speed = speed > 0.01f ? speed : 1f;
            Elapsed = 0f;
            Phase = attack.Windup > 0f ? AttackPhase.Windup : AttackPhase.Active;
            if (Phase == AttackPhase.Active)
            {
                ActiveStarted?.Invoke(attack);
            }
        }

        public void Tick(float deltaTime)
        {
            if (Current == null || deltaTime <= 0f)
            {
                return;
            }

            Elapsed += deltaTime;
            var windup = Current.Windup / _speed;
            var activeEnd = windup + Current.ActiveTime / _speed;
            var total = Current.Duration / _speed;
            var next = Elapsed < windup
                ? AttackPhase.Windup
                : (Elapsed < activeEnd ? AttackPhase.Active : AttackPhase.Recovery);

            if (Elapsed >= total)
            {
                var finished = Current;
                EndActiveWindow();
                Phase = AttackPhase.None;
                Current = null;
                Finished?.Invoke(finished);
                return;
            }

            if (next == Phase)
            {
                return;
            }

            if (next == AttackPhase.Active)
            {
                Phase = next;
                ActiveStarted?.Invoke(Current);
                return;
            }

            if (Phase == AttackPhase.Active)
            {
                ActiveEnded?.Invoke();
            }
            Phase = next;
        }

        public void Cancel()
        {
            EndActiveWindow();
            Phase = AttackPhase.None;
            Current = null;
            Elapsed = 0f;
        }

        private void EndActiveWindow()
        {
            if (Phase == AttackPhase.Active)
            {
                ActiveEnded?.Invoke();
            }
        }
    }
}
