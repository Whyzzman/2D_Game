using NUnit.Framework;

namespace ForgottenKingdom.Tests
{
    public sealed class PlayerStateMachineTests
    {
        private PlayerStateMachine _state;

        [SetUp]
        public void SetUp()
        {
            _state = new PlayerStateMachine();
        }

        [Test]
        public void DeathBlocksEveryOtherAction()
        {
            _state.ExitTo(PlayerAction.Dead);
            Assert.That(_state.TryEnter(PlayerAction.LightAttack), Is.False);
            Assert.That(_state.TryEnter(PlayerAction.Dash), Is.False);
            Assert.That(_state.TryEnter(PlayerAction.Block), Is.False);
            Assert.That(_state.TryEnter(PlayerAction.Hurt), Is.False);
            Assert.That(_state.Current, Is.EqualTo(PlayerAction.Dead));
        }

        [Test]
        public void StunCannotBeEscapedWithADash()
        {
            _state.TryEnter(PlayerAction.Hurt);
            Assert.That(_state.TryEnter(PlayerAction.Dash), Is.False);
            Assert.That(_state.TryEnter(PlayerAction.LightAttack), Is.False);
            _state.ExitTo();
            Assert.That(_state.TryEnter(PlayerAction.Dash), Is.True);
        }

        [Test]
        public void AnAttackIsDashCancelableOnlyInsideItsCancelWindow()
        {
            _state.TryEnter(PlayerAction.LightAttack);
            Assert.That(_state.TryEnter(PlayerAction.Dash), Is.False);
            _state.IsAttackCancelable = true;
            Assert.That(_state.TryEnter(PlayerAction.Dash), Is.True);
        }

        [Test]
        public void LeavingAnAttackClearsTheCancelPermission()
        {
            _state.TryEnter(PlayerAction.LightAttack);
            _state.IsAttackCancelable = true;
            _state.ExitTo();
            Assert.That(_state.IsAttackCancelable, Is.False);
        }

        [Test]
        public void DamageAndDeathAlwaysInterrupt()
        {
            _state.TryEnter(PlayerAction.Dash);
            Assert.That(_state.TryEnter(PlayerAction.Hurt), Is.True);
            Assert.That(_state.TryEnter(PlayerAction.Dead), Is.True);
            Assert.That(_state.Current, Is.EqualTo(PlayerAction.Dead));
        }

        [Test]
        public void BlockingAllowsAttacksAndDashButNotHealing()
        {
            _state.TryEnter(PlayerAction.Block);
            Assert.That(_state.CanEnter(PlayerAction.Heal), Is.False);
            Assert.That(_state.CanEnter(PlayerAction.Dash), Is.True);
            Assert.That(_state.TryEnter(PlayerAction.HeavyAttack), Is.True);
        }

        [Test]
        public void HealingCannotBeInterruptedByAnotherAction()
        {
            _state.TryEnter(PlayerAction.Heal);
            Assert.That(_state.TryEnter(PlayerAction.LightAttack), Is.False);
            Assert.That(_state.TryEnter(PlayerAction.Dash), Is.False);
            Assert.That(_state.TryEnter(PlayerAction.Hurt), Is.True);
        }

        [Test]
        public void TheChangeEventReportsBothSides()
        {
            PlayerAction from = PlayerAction.Dead;
            PlayerAction to = PlayerAction.Dead;
            _state.ActionChanged += (previous, next) => { from = previous; to = next; };
            _state.TryEnter(PlayerAction.Block);
            Assert.That(from, Is.EqualTo(PlayerAction.None));
            Assert.That(to, Is.EqualTo(PlayerAction.Block));
        }
    }
}
