using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The enemy and boss animation sets as data: that every state a fight depends on has real
    /// frames, that the frame rates and loop rules follow the Visual Bible, and that a set which
    /// lost its art fails loudly instead of quietly showing the wrong thing.
    /// </summary>
    public sealed class EnemyAnimationDataTests
    {
        private EnemyAnimationSet _zombie;
        private EnemyAnimationSet _guard;
        private BossAnimationSet _boss;

        [SetUp]
        public void SetUp()
        {
            _zombie = Load<EnemyAnimationSet>("Assets/Game/Data/Animation/ZombieAnimations.asset");
            _guard = Load<EnemyAnimationSet>(
                "Assets/Game/Data/Animation/ShieldGuardAnimations.asset");
            _boss = Load<BossAnimationSet>("Assets/Game/Data/Animation/GuardianAnimations.asset");
        }

        // ---------- the shipped sets ----------

        [Test]
        public void EverySetValidates()
        {
            Assert.That(_zombie.Validate(), Is.True);
            Assert.That(_guard.Validate(), Is.True);
            Assert.That(_boss.Validate(), Is.True);
        }

        [Test]
        public void EveryRequiredZombieStateHasFrames()
        {
            foreach (var state in _zombie.RequiredStates())
            {
                Assert.That(_zombie.Has(state), Is.True, $"Zombie is missing {state}.");
            }
        }

        [Test]
        public void EveryRequiredShieldGuardStateHasFrames()
        {
            Assert.That(_guard.CarriesShield, Is.True);
            foreach (var state in _guard.RequiredStates())
            {
                Assert.That(_guard.Has(state), Is.True, $"Shield Guard is missing {state}.");
            }
        }

        [Test]
        public void TheShieldGuardOwesTheGuardStatesAndTheZombieDoesNot()
        {
            // The zombie never raises a shield, so it is not failed for having no guard art; the
            // guard is, because its whole fight is built on those four states reading instantly.
            Assert.That(_guard.RequiredStates(), Contains.Item(EnemyAnimationState.GuardBreak));
            Assert.That(_zombie.RequiredStates(),
                Has.No.Member(EnemyAnimationState.GuardBreak));
        }

        [Test]
        public void EveryBossStateHasFramesInBothPhases()
        {
            foreach (var state in BossAnimationSet.PhaseOneStates()
                .Concat(BossAnimationSet.PhaseTwoStates()))
            {
                Assert.That(_boss.Has(state), Is.True, $"Guardian is missing {state}.");
            }
        }

        [Test]
        public void NoBossStateFallsBackToAGenericClip()
        {
            // A boss whose charge or phase change is represented by a generic pose is a broken
            // fight, so every one of its states must be real rather than merely resolvable.
            foreach (var state in _boss.RequiredStates())
            {
                Assert.That(_boss.Clip(state), Is.Not.SameAs(_boss.Fallback),
                    $"{state} is showing the fallback.");
            }
        }

        // ---------- conventions ----------

        [Test]
        public void CombatRunsAtTwiceTheFrameRateOfStances()
        {
            Assert.That(_zombie.Clip(EnemyAnimationState.AttackLight).FramesPerSecond,
                Is.EqualTo(24f));
            Assert.That(_zombie.Clip(EnemyAnimationState.Patrol).FramesPerSecond,
                Is.EqualTo(12f));
            Assert.That(_boss.Clip(BossAnimationState.SlashP1).FramesPerSecond, Is.EqualTo(24f));
            Assert.That(_boss.Clip(BossAnimationState.WalkP1).FramesPerSecond, Is.EqualTo(12f));
        }

        [Test]
        public void MovementLoopsAndCommittedActionsDoNot()
        {
            Assert.That(_zombie.Clip(EnemyAnimationState.Idle).IsLooping, Is.True);
            Assert.That(_zombie.Clip(EnemyAnimationState.Patrol).IsLooping, Is.True);
            Assert.That(_guard.Clip(EnemyAnimationState.GuardIdle).IsLooping, Is.True);

            Assert.That(_zombie.Clip(EnemyAnimationState.AttackLight).IsLooping, Is.False);
            Assert.That(_zombie.Clip(EnemyAnimationState.Death).IsLooping, Is.False);
            Assert.That(_guard.Clip(EnemyAnimationState.GuardBreak).IsLooping, Is.False);
            Assert.That(_boss.Clip(BossAnimationState.PhaseTransition).IsLooping, Is.False,
                "A looping phase change would never end.");
            Assert.That(_boss.Clip(BossAnimationState.Death).IsLooping, Is.False);
        }

        [Test]
        public void TheZombieWindupIsTheLongestReadInTheGame()
        {
            // The concept brief is explicit that this telegraph carries the fight. It is the one
            // clip with a large frame budget on an ordinary enemy, and it must stay that way.
            var attack = _zombie.Clip(EnemyAnimationState.AttackLight);
            Assert.That(attack.FrameCount, Is.GreaterThanOrEqualTo(12),
                "A thin windup would make the zombie unreadable.");
        }

        [Test]
        public void TheBossSpendsItsFramesOnTheMomentsThatMatter()
        {
            // Charge telegraph, phase change and death are where the budget belongs; an idle does
            // not need to compete with them.
            var idle = _boss.Clip(BossAnimationState.IdleP1).FrameCount;
            Assert.That(_boss.Clip(BossAnimationState.ChargeStart).FrameCount,
                Is.GreaterThan(idle));
            Assert.That(_boss.Clip(BossAnimationState.PhaseTransition).FrameCount,
                Is.GreaterThan(idle));
            Assert.That(_boss.Clip(BossAnimationState.Death).FrameCount, Is.GreaterThan(idle));
        }

        [Test]
        public void TheComboSwingsAreThreeDifferentClips()
        {
            // The player is meant to be able to count the sequence, which only works if the three
            // swings are genuinely different animations.
            var one = _boss.Clip(BossAnimationState.Combo1);
            var two = _boss.Clip(BossAnimationState.Combo2);
            var three = _boss.Clip(BossAnimationState.Combo3);

            Assert.That(one, Is.Not.SameAs(two));
            Assert.That(two, Is.Not.SameAs(three));
            Assert.That(one, Is.Not.SameAs(three));
        }

        [Test]
        public void PhaseOneAndPhaseTwoDoNotShareTheirPoses()
        {
            // The brief asks the player to read the phase change with no interface at all, which
            // fails immediately if the two phases reuse the same clips.
            Assert.That(_boss.Clip(BossAnimationState.IdleP1),
                Is.Not.SameAs(_boss.Clip(BossAnimationState.IdleP2)));
            Assert.That(_boss.Clip(BossAnimationState.SlashP1),
                Is.Not.SameAs(_boss.Clip(BossAnimationState.SlashP2)));
        }

        [Test]
        public void AnUnsetStateIsAnAttackRatherThanAnIdle()
        {
            // Unity's serializer skips field initializers, so anything left unset lands on the
            // zero value. A move with no art chosen must look like a swing, never like standing.
            Assert.That(default(EnemyAnimationState),
                Is.EqualTo(EnemyAnimationState.AttackLight));
            Assert.That(default(BossAnimationState), Is.EqualTo(BossAnimationState.SlashP1));
        }

        private static T Load<T>(string path) where T : ScriptableObject
        {
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(asset, Is.Not.Null, $"The content build writes {path}.");
            return asset;
        }
    }
}
