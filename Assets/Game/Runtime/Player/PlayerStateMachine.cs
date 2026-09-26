using System;

namespace ForgottenKingdom
{
    /// <summary>
    /// What the player is doing besides moving. Locomotion stays in <see cref="MovementState"/>;
    /// this is the layer that decides which actions may interrupt which.
    /// </summary>
    public enum PlayerAction
    {
        None,
        LightAttack,
        HeavyAttack,
        Dash,
        Block,
        Parry,
        /// <summary>A brief flinch from an ordinary hit.</summary>
        Hurt,
        /// <summary>Poise broke: a long opening the attacker can punish.</summary>
        Stunned,
        /// <summary>The guard was shattered by guard damage rather than by poise.</summary>
        GuardBroken,
        Heal,
        /// <summary>Putting one weapon away and drawing the other; short and attack-proof.</summary>
        WeaponSwitch,
        /// <summary>A skill from the bar is running; committed until it finishes.</summary>
        ActiveSkill,
        /// <summary>A spell is being shaped; a real hit takes it away.</summary>
        Casting,
        Dead
    }

    /// <summary>
    /// A small explicit rule table instead of scattered boolean checks, so a new action only needs
    /// a line here to be safe against the existing ones.
    /// </summary>
    public sealed class PlayerStateMachine
    {
        public PlayerAction Current { get; private set; } = PlayerAction.None;
        public float TimeInAction { get; private set; }

        /// <summary>Set while the running attack has reached its dash-cancel window.</summary>
        public bool IsAttackCancelable { get; set; }

        public event Action<PlayerAction, PlayerAction> ActionChanged;

        public bool IsBusy => Current != PlayerAction.None;
        public bool IsDead => Current == PlayerAction.Dead;
        public bool IsAttacking => Current == PlayerAction.LightAttack
            || Current == PlayerAction.HeavyAttack;
        public bool IsBlocking => Current == PlayerAction.Block || Current == PlayerAction.Parry;

        /// <summary>A helpless state: no attacking, blocking, dashing or steering.</summary>
        public bool IsIncapacitated => Current == PlayerAction.Stunned
            || Current == PlayerAction.GuardBroken;

        public bool CanEnter(PlayerAction next)
        {
            if (Current == PlayerAction.Dead)
            {
                return false;
            }

            if (next == PlayerAction.Dead)
            {
                return true;
            }

            // A broken guard or a poise break has to be ridden out; further hits land but do not
            // shorten it, so only death interrupts.
            if (IsIncapacitated)
            {
                return false;
            }

            if (next == PlayerAction.Hurt || next == PlayerAction.None
                || next == PlayerAction.Stunned || next == PlayerAction.GuardBroken)
            {
                return true;
            }

            switch (Current)
            {
                case PlayerAction.None:
                    return true;
                case PlayerAction.Hurt:
                case PlayerAction.Heal:
                case PlayerAction.Dash:
                    return false;
                case PlayerAction.WeaponSwitch:
                    // The draw may be abandoned to dodge, but not turned into a swing.
                    return next == PlayerAction.Dash;
                case PlayerAction.ActiveSkill:
                    // Skills are committed: their power is paid for with the inability to change
                    // your mind halfway through.
                    return false;
                case PlayerAction.Casting:
                    // A spell may be abandoned to dodge — that is the caster's way out — but it
                    // cannot turn into a swing or a block.
                    return next == PlayerAction.Dash;
                case PlayerAction.LightAttack:
                case PlayerAction.HeavyAttack:
                    // A follow-up swing is gated by the combo window in PlayerCombat; a dash may
                    // cancel only once the attack has reached its cancel frames.
                    return next == PlayerAction.LightAttack
                        || next == PlayerAction.HeavyAttack
                        || (next == PlayerAction.Dash && IsAttackCancelable);
                case PlayerAction.Block:
                    return next != PlayerAction.Heal;
                case PlayerAction.Parry:
                    return next != PlayerAction.Heal;
                default:
                    return false;
            }
        }

        public bool TryEnter(PlayerAction next)
        {
            if (next == Current && next != PlayerAction.Hurt)
            {
                return false;
            }

            if (!CanEnter(next))
            {
                return false;
            }

            Enter(next);
            return true;
        }

        public void ExitTo(PlayerAction next = PlayerAction.None)
        {
            if (Current == PlayerAction.Dead && next != PlayerAction.None)
            {
                return;
            }

            Enter(next);
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime > 0f)
            {
                TimeInAction += deltaTime;
            }
        }

        public void Reset()
        {
            IsAttackCancelable = false;
            Enter(PlayerAction.None);
        }

        private void Enter(PlayerAction next)
        {
            var previous = Current;
            Current = next;
            TimeInAction = 0f;
            if (next != PlayerAction.LightAttack && next != PlayerAction.HeavyAttack)
            {
                IsAttackCancelable = false;
            }

            if (previous != next)
            {
                ActionChanged?.Invoke(previous, next);
            }
        }
    }
}
