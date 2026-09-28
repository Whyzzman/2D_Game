using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// Enemy and boss animation in the running level. The questions are the ones that produce
    /// visible bugs: can a more important state be hidden by a less important one, does the visual
    /// come back after a reset, and does the phase change happen exactly once.
    /// </summary>
    public sealed class EnemyAnimationTests
    {
        private PlayerCharacter _player;
        private BossController _boss;
        private BossAnimationController _bossAnimation;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            yield return TestWorld.Load();
            _player = UnityEngine.Object.FindAnyObjectByType<PlayerCharacter>();
            _boss = TombScene.Guardian();
            _bossAnimation = _boss != null
                ? _boss.GetComponent<BossAnimationController>()
                : null;
            yield return new WaitForFixedUpdate();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var animation in UnityEngine.Object
                .FindObjectsByType<EnemyAnimationController>())
            {
                animation.ForcedState = null;
            }

            if (_bossAnimation != null) { _bossAnimation.ForcedState = null; }
            GamePause.Clear();
            SaveSystem.Delete();
            Time.timeScale = 1f;
        }

        // ---------- wiring ----------

        [UnityTest]
        public IEnumerator EveryEnemyShipsWithAnAnimationControllerAndSockets()
        {
            yield return null;
            var enemies = UnityEngine.Object
                .FindObjectsByType<EnemyController>();
            Assume.That(enemies.Length, Is.GreaterThan(0));

            foreach (var enemy in enemies)
            {
                var animation = enemy.GetComponent<EnemyAnimationController>();
                var sockets = enemy.GetComponentInChildren<VisualSockets>();
                Assert.That(animation, Is.Not.Null, $"{enemy.name} has no animation controller.");
                Assert.That(animation.Clip, Is.Not.Null, $"{enemy.name} has no clip.");
                Assert.That(sockets, Is.Not.Null, $"{enemy.name} has no sockets.");
                Assert.That(sockets.IsComplete, Is.True, $"{enemy.name} has an unwired socket.");
            }
        }

        [UnityTest]
        public IEnumerator EnemiesShowAFrameRatherThanAPlaceholderBox()
        {
            yield return null;
            foreach (var animation in UnityEngine.Object
                .FindObjectsByType<EnemyAnimationController>())
            {
                var body = Body(animation.gameObject);
                Assert.That(body.sprite, Is.Not.Null, $"{animation.name} has no sprite.");
                Assert.That(body.transform.localScale, Is.EqualTo(Vector3.one),
                    "Frames carry their own size through pixels-per-unit.");
            }
        }

        [UnityTest]
        public IEnumerator OnlyTheShieldCarrierHasAShieldSocket()
        {
            yield return null;
            var guard = Enemy("ShieldGuard");
            var zombie = Enemy("ZombieSoldier");

            Assert.That(guard.GetComponentInChildren<VisualSockets>()
                .Has(VisualSocket.Shield), Is.True);
            Assert.That(zombie.GetComponentInChildren<VisualSockets>()
                .Has(VisualSocket.Shield), Is.False,
                "A zombie has no shield, so a blocked-hit effect falls back to its body.");
        }

        [UnityTest]
        public IEnumerator TheBossShipsWithItsOwnControllerAndStartsDormant()
        {
            yield return null;

            Assert.That(_bossAnimation, Is.Not.Null);
            Assert.That(_boss.State, Is.EqualTo(BossState.Dormant));
            Assert.That(_bossAnimation.State, Is.EqualTo(BossAnimationState.Dormant));
            Assert.That(Body(_boss.gameObject).sprite, Is.Not.Null);
        }

        // ---------- priority ----------

        [UnityTest]
        public IEnumerator DeathOutranksEverythingElse()
        {
            var zombie = Enemy("ZombieSoldier");
            var animation = zombie.GetComponent<EnemyAnimationController>();
            zombie.Actor.ApplyDamage(new DamageInfo(9999f, Vector2.right));
            yield return new WaitForFixedUpdate();
            yield return null;

            Assume.That(zombie.State, Is.EqualTo(EnemyState.Dead));
            Assert.That(animation.State, Is.EqualTo(EnemyAnimationState.Death));
        }

        [UnityTest]
        public IEnumerator AGuardBreakOutranksTheGuardPose()
        {
            // The worst version of this bug: the shield is shattered and the enemy still looks
            // like it is blocking, so the punish window is invisible.
            var guard = Enemy("ShieldGuard");
            var animation = guard.GetComponent<EnemyAnimationController>();
            Assume.That(guard.Guard, Is.Not.Null);

            // Spend the whole meter behind a raised shield: the guard's own way of breaking,
            // driven deterministically rather than by hoping a fight produces it.
            guard.Guard.RaiseGuard();
            guard.Guard.SpendGuard(guard.Guard.MaxGuard * 2f);
            yield return new WaitForFixedUpdate();
            yield return null;

            Assume.That(guard.Guard.IsBroken, Is.True);
            Assume.That(guard.State, Is.EqualTo(EnemyState.GuardBroken));
            Assert.That(animation.State, Is.EqualTo(EnemyAnimationState.GuardBreak),
                "A shattered shield that still looked like a block would hide the punish window.");
        }

        [UnityTest]
        public IEnumerator EveryEnemyStateCanBePlayedWithoutBreakingAnything()
        {
            var zombie = Enemy("ZombieSoldier");
            var animation = zombie.GetComponent<EnemyAnimationController>();
            var body = Body(zombie.gameObject);
            var before = zombie.State;

            foreach (EnemyAnimationState state in Enum.GetValues(typeof(EnemyAnimationState)))
            {
                animation.ForcedState = state;
                yield return null;

                Assert.That(animation.State, Is.EqualTo(state));
                Assert.That(body.sprite, Is.Not.Null, $"{state} left the enemy invisible.");
            }

            animation.ForcedState = null;
            Assert.That(zombie.State, Is.EqualTo(before), "Animation never drives the AI.");
        }

        [UnityTest]
        public IEnumerator EveryBossStateCanBePlayedWithoutBreakingAnything()
        {
            var body = Body(_boss.gameObject);
            var before = _boss.State;

            foreach (BossAnimationState state in Enum.GetValues(typeof(BossAnimationState)))
            {
                _bossAnimation.ForcedState = state;
                yield return null;

                Assert.That(_bossAnimation.State, Is.EqualTo(state));
                Assert.That(body.sprite, Is.Not.Null, $"{state} left the boss invisible.");
            }

            _bossAnimation.ForcedState = null;
            Assert.That(_boss.State, Is.EqualTo(before), "Animation never drives the fight.");
        }

        // ---------- the phase change ----------

        [UnityTest]
        public IEnumerator PhaseTwoLooksDifferentFromPhaseOne()
        {
            // This is the acceptance criterion the brief cares about most: the change has to be
            // readable with no interface at all, which means different frames, not a tint.
            _bossAnimation.ForcedState = BossAnimationState.IdleP1;
            yield return null;
            var phaseOne = Body(_boss.gameObject).sprite;

            _bossAnimation.ForcedState = BossAnimationState.IdleP2;
            yield return null;
            var phaseTwo = Body(_boss.gameObject).sprite;

            Assert.That(phaseTwo, Is.Not.SameAs(phaseOne));
        }

        [UnityTest]
        public IEnumerator ResettingTheBossRestoresPhaseOneAndItsShield()
        {
            _boss.BeginFight(_player);
            yield return new WaitForFixedUpdate();
            _boss.Actor.ApplyDamage(new DamageInfo(100000f, Vector2.right));
            yield return new WaitForSeconds(0.2f);

            _boss.ResetBoss();
            yield return null;

            Assert.That(_boss.Phase, Is.EqualTo(1));
            Assert.That(_bossAnimation.State, Is.EqualTo(BossAnimationState.Dormant));
            Assert.That(_bossAnimation.TransitionsPlayed, Is.Zero,
                "A reset puts the fight back as it was found, transition included.");
            var shield = _boss.transform.Find("Shield");
            Assert.That(shield, Is.Not.Null);
            Assert.That(shield.gameObject.activeSelf, Is.True,
                "The shield comes back with phase one.");
        }

        [UnityTest]
        public IEnumerator TheDormantPoseDoesNotShowTheBossMidFight()
        {
            // Dormant has to look asleep. If it shared a pose with the fighting idle, the arena
            // would give away that the statue is a boss before the player ever triggers it.
            _bossAnimation.ForcedState = BossAnimationState.Dormant;
            yield return null;
            var dormant = Body(_boss.gameObject).sprite;

            _bossAnimation.ForcedState = BossAnimationState.IdleP1;
            yield return null;

            Assert.That(Body(_boss.gameObject).sprite, Is.Not.SameAs(dormant));
        }

        // ---------- helpers ----------

        private static EnemyController Enemy(string namePrefix)
        {
            var enemy = UnityEngine.Object
                .FindObjectsByType<EnemyController>()
                .FirstOrDefault(e => e.name.StartsWith(namePrefix));
            Assume.That(enemy, Is.Not.Null, $"The level contains a {namePrefix}.");
            return enemy;
        }

        private static SpriteRenderer Body(GameObject root) =>
            root.transform.Find("Visual/Body").GetComponent<SpriteRenderer>();
    }
}
