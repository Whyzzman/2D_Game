using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ForgottenKingdom.Tests
{
    public sealed class AttackRunnerTests
    {
        private const float Step = 0.02f;
        private AttackDefinition _attack;
        private AttackRunner _runner;
        private List<string> _events;

        [SetUp]
        public void SetUp()
        {
            // Default timing: 0.12 windup, 0.08 active, 0.22 recovery, 0.26 combo link.
            _attack = new AttackDefinition();
            _runner = new AttackRunner();
            _events = new List<string>();
            _runner.ActiveStarted += _ => _events.Add("active");
            _runner.ActiveEnded += () => _events.Add("inactive");
            _runner.Finished += _ => _events.Add("finished");
        }

        [Test]
        public void PhasesFollowWindupActiveRecoveryAndThenEnd()
        {
            _runner.Begin(_attack, 1f);
            Assert.That(_runner.Phase, Is.EqualTo(AttackPhase.Windup));
            Advance(_attack.Windup + Step);
            Assert.That(_runner.Phase, Is.EqualTo(AttackPhase.Active));
            Advance(_attack.ActiveTime);
            Assert.That(_runner.Phase, Is.EqualTo(AttackPhase.Recovery));
            Advance(_attack.Recovery);
            Assert.That(_runner.Phase, Is.EqualTo(AttackPhase.None));
            Assert.That(_runner.IsBusy, Is.False);
            Assert.That(_events, Is.EqualTo(new[] { "active", "inactive", "finished" }));
        }

        [Test]
        public void TheActiveWindowOpensAndClosesExactlyOnce()
        {
            _runner.Begin(_attack, 1f);
            Advance(_attack.Duration + Step);
            Assert.That(_events.FindAll(name => name == "active").Count, Is.EqualTo(1));
            Assert.That(_events.FindAll(name => name == "inactive").Count, Is.EqualTo(1));
        }

        [Test]
        public void ChainAndCancelWindowsOpenOnlyAfterTheirTimes()
        {
            _runner.Begin(_attack, 1f);
            Assert.That(_runner.CanChain, Is.False);
            Assert.That(_runner.CanCancel, Is.False);
            Advance(_attack.CancelableAfter + Step);
            Assert.That(_runner.CanCancel, Is.True);
            Advance(_attack.ComboLinkTime - _attack.CancelableAfter);
            Assert.That(_runner.CanChain, Is.True);
        }

        [Test]
        public void AttackSpeedShortensEveryWindow()
        {
            _runner.Begin(_attack, 2f);
            Advance(_attack.Duration / 2f + Step);
            Assert.That(_runner.IsBusy, Is.False);
        }

        [Test]
        public void CancellingDuringTheActiveWindowClosesTheHitbox()
        {
            _runner.Begin(_attack, 1f);
            Advance(_attack.Windup + Step);
            Assert.That(_runner.Phase, Is.EqualTo(AttackPhase.Active));
            _runner.Cancel();
            Assert.That(_runner.IsBusy, Is.False);
            Assert.That(_events, Is.EqualTo(new[] { "active", "inactive" }));
        }

        [Test]
        public void StartingANewAttackClosesThePreviousActiveWindow()
        {
            _runner.Begin(_attack, 1f);
            Advance(_attack.Windup + Step);
            _runner.Begin(_attack, 1f);
            Assert.That(_events, Is.EqualTo(new[] { "active", "inactive" }));
            Assert.That(_runner.Elapsed, Is.Zero);
            Assert.That(_runner.Phase, Is.EqualTo(AttackPhase.Windup));
        }

        private void Advance(float seconds)
        {
            var steps = Mathf.CeilToInt(seconds / Step);
            for (var i = 0; i < steps; i++)
            {
                _runner.Tick(Step);
            }
        }
    }
}
