using NUnit.Framework;
using UnityEngine;

namespace ForgottenKingdom.Tests
{
    /// <summary>The flask rules on their own, stepped by hand.</summary>
    public sealed class HealingFlaskTests
    {
        private GameObject _owner;
        private Health _health;
        private HealingFlasks _flasks;

        [SetUp]
        public void SetUp()
        {
            _owner = new GameObject("FlaskOwner");
            _health = _owner.AddComponent<Health>();
            _health.RestoreToFull();
            _flasks = new HealingFlasks();
            _flasks.Bind(_health);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_owner);

        [Test]
        public void ADrinkSpendsAChargeAtOnceAndHealsAtTheHealPoint()
        {
            _health.SetCurrent(_health.Max * 0.5f);
            var before = _health.Current;
            Assert.That(_flasks.TryBegin(), Is.True);
            Assert.That(_flasks.Charges, Is.EqualTo(_flasks.MaxCharges - 1), "Paid when it starts.");

            _flasks.Tick(0.3f);
            Assert.That(_health.Current, Is.EqualTo(before), "Not yet swallowed.");
            _flasks.Tick(0.3f);
            Assert.That(_health.Current, Is.EqualTo(before + _flasks.HealAmount).Within(0.01f));
            Assert.That(_flasks.IsDrinking, Is.True, "The drink still holds the player.");
            _flasks.Tick(0.3f);
            Assert.That(_flasks.IsDrinking, Is.False);
        }

        [Test]
        public void AHealingItemIsTakenLikeAFlaskButSpendsNoCharge()
        {
            _health.SetCurrent(_health.Max * 0.3f);
            var before = _health.Current;
            Assert.That(_flasks.TryBeginItem(20f), Is.True);
            Assert.That(_flasks.IsDrinkingFlask, Is.False);
            Assert.That(_flasks.Charges, Is.EqualTo(_flasks.MaxCharges), "The flasks are untouched.");
            _flasks.Tick(0.6f);
            Assert.That(_health.Current, Is.EqualTo(before + 20f).Within(0.01f), "Its own amount.");

            _flasks.Tick(1f);
            Assert.That(_flasks.TryBegin(), Is.True);
            Assert.That(_flasks.IsDrinkingFlask, Is.True);
            _flasks.Tick(0.6f);
            Assert.That(_health.Current, Is.EqualTo(before + 20f + _flasks.HealAmount).Within(0.01f),
                "A flask after it still heals a flask's worth.");
        }

        [Test]
        public void NothingIsEatenAtFullHealth()
        {
            Assert.That(_flasks.CanHeal, Is.False);
            Assert.That(_flasks.TryBeginItem(20f), Is.False);
        }

        [Test]
        public void AHitBeforeTheHealPointSpillsTheFlask()
        {
            _health.SetCurrent(_health.Max * 0.5f);
            var before = _health.Current;
            var spilled = false;
            _flasks.Spilled += () => spilled = true;
            _flasks.TryBegin();
            _flasks.Tick(0.2f);
            _flasks.Interrupt();
            _flasks.Tick(1f);

            Assert.That(spilled, Is.True);
            Assert.That(_health.Current, Is.EqualTo(before), "Nothing healed.");
            Assert.That(_flasks.Charges, Is.EqualTo(_flasks.MaxCharges - 1), "And the charge is gone.");
        }

        [Test]
        public void NoDrinkAtFullHealthOrWithEmptyFlasks()
        {
            Assert.That(_flasks.TryBegin(), Is.False, "A full-health press must not waste a flask.");

            _health.SetCurrent(1f);
            for (var i = 0; i < _flasks.MaxCharges; i++)
            {
                Assert.That(_flasks.TryBegin(), Is.True);
                _flasks.Tick(1f);
                _health.SetCurrent(1f);
            }

            Assert.That(_flasks.Charges, Is.Zero);
            Assert.That(_flasks.TryBegin(), Is.False, "Empty.");
        }

        [Test]
        public void RefillingFillsEveryFlask()
        {
            _health.SetCurrent(1f);
            _flasks.TryBegin();
            _flasks.Tick(1f);
            _flasks.Refill();
            Assert.That(_flasks.Charges, Is.EqualTo(_flasks.MaxCharges));
        }
    }
}
