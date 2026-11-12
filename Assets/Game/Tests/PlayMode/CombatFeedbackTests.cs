using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// Combat feedback in the running game. The questions are the ones that decide whether a
    /// feedback layer helps or lies: does a blocked hit read as a block rather than as damage,
    /// does a hit absorbed by invincibility stay silent, does the screen recover afterwards, and
    /// can several things landing at once still only take the screen once.
    /// </summary>
    public sealed class CombatFeedbackTests
    {
        private PlayerCharacter _player;
        private CombatFeedbackService _feedback;
        private CameraShaker _shaker;
        private readonly List<FeedbackKind> _played = new List<FeedbackKind>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            FeedbackSettings.ResetToDefaults();
            yield return TestWorld.Load();
            _player = Object.FindAnyObjectByType<PlayerCharacter>();
            _feedback = Object.FindAnyObjectByType<CombatFeedbackService>();
            _shaker = Camera.main != null ? Camera.main.GetComponent<CameraShaker>() : null;
            _played.Clear();
            if (_feedback != null)
            {
                _feedback.Played += OnPlayed;
            }

            yield return new WaitForFixedUpdate();
        }

        [TearDown]
        public void TearDown()
        {
            if (_feedback != null)
            {
                _feedback.Played -= OnPlayed;
            }

            FeedbackSettings.ResetToDefaults();
            GamePause.Clear();
            SaveSystem.Delete();
            GameTimeScale.Reset();
        }

        // Dust of the hero's own movement (a landing that falls in the same frame) is not the hit's feedback:
        // counted, it made the one-impact check fail now and then in the full run (2026-11-12).
        private void OnPlayed(FeedbackKind kind, Vector2 position)
        {
            if (kind == FeedbackKind.JumpDust || kind == FeedbackKind.LandDust || kind == FeedbackKind.RunDust
                || kind == FeedbackKind.WallSlideDust || kind == FeedbackKind.WallJump || kind == FeedbackKind.AirJump)
            {
                return;
            }

            _played.Add(kind);
        }

        // ---------- wiring ----------

        [UnityTest]
        public IEnumerator TheSceneShipsWithTheFeedbackServiceAndCamera()
        {
            yield return null;

            Assert.That(_feedback, Is.Not.Null);
            Assert.That(_feedback.Library, Is.Not.Null);
            Assert.That(_shaker, Is.Not.Null);
            Assert.That(Camera.main.GetComponent<ScreenFlash>(), Is.Not.Null);
        }

        // ---------- one event per hit ----------

        [UnityTest]
        public IEnumerator AHitThatLandsPlaysExactlyOneImpact()
        {
            var zombie = Enemy();
            _played.Clear();
            zombie.Actor.ApplyDamage(new DamageInfo(12f, Vector2.right, staggerDamage: 10f,
                hitPoint: zombie.transform.position));
            yield return null;

            Assert.That(_played.Count, Is.EqualTo(1), "Impacts: " + Join(_played));
            Assert.That(_played[0], Is.EqualTo(FeedbackKind.LightHit));
        }

        [UnityTest]
        public IEnumerator ALandedHitFlashesTheEnemyAndEveryBossCanFlash()
        {
            var zombie = Enemy();
            var flash = zombie.GetComponent<HitFlash>();
            Assert.That(flash, Is.Not.Null, "Every enemy is built with a hit flash.");
            foreach (var boss in Object.FindObjectsByType<BossController>(FindObjectsInactive.Include))
            {
                Assert.That(boss.GetComponent<HitFlash>(), Is.Not.Null, $"{boss.name} can flash too.");
            }

            zombie.Actor.ApplyDamage(new DamageInfo(12f, Vector2.right, staggerDamage: 10f,
                hitPoint: zombie.transform.position));
            yield return null;
            Assert.That(flash.IsFlashing, Is.True);
            var overlay = zombie.GetComponentsInChildren<SpriteRenderer>()
                .FirstOrDefault(r => r.name == "HitFlash");
            Assert.That(overlay != null && overlay.enabled && overlay.sprite != null, Is.True,
                "A silhouette of the body is drawn over it.");

            var end = Time.realtimeSinceStartup + 0.4f;
            while (Time.realtimeSinceStartup < end) { yield return null; }
            Assert.That(flash.IsFlashing, Is.False);
            Assert.That(overlay.enabled, Is.False, "And gone a moment later.");
        }

        [UnityTest]
        public IEnumerator ThePlayerBlinksWhileUntouchableAfterAHitAndNotAfter()
        {
            var player = Object.FindAnyObjectByType<PlayerCharacter>();
            var blink = player.GetComponent<InvulnerabilityBlink>();
            Assert.That(blink, Is.Not.Null);
            Assert.That(blink.Renderers.Count, Is.GreaterThan(0), "It knows what to blink.");
            var body = blink.Renderers[0];
            var alpha = body.color.a;

            player.Actor.ApplyDamage(new DamageInfo(10f, Vector2.right, staggerDamage: 5f));
            var dimmed = false;
            var end = Time.realtimeSinceStartup + 0.5f;
            while (Time.realtimeSinceStartup < end)
            {
                yield return null;
                dimmed |= blink.IsBlinking && body.color.a < alpha - 0.1f;
            }

            Assert.That(dimmed, Is.True, "Blinking while untouchable.");
            end = Time.realtimeSinceStartup + 3f;
            while (Time.realtimeSinceStartup < end && blink.IsBlinking) { yield return null; }
            yield return null;
            Assert.That(blink.IsBlinking, Is.False);
            Assert.That(body.color.a, Is.EqualTo(alpha).Within(0.001f), "Solid again once it ends.");
        }

        [UnityTest]
        public IEnumerator AHeavyBlowReadsAsHeavyRatherThanLight()
        {
            var zombie = Enemy();
            _played.Clear();
            zombie.Actor.ApplyDamage(new DamageInfo(40f, Vector2.right, staggerDamage: 45f,
                hitPoint: zombie.transform.position));
            yield return null;

            Assert.That(_played, Contains.Item(FeedbackKind.HeavyHit));
        }

        [UnityTest]
        public IEnumerator ACriticalHitGetsItsOwnImpact()
        {
            var zombie = Enemy();
            _played.Clear();
            zombie.Actor.ApplyDamage(new DamageInfo(40f, Vector2.right, staggerDamage: 12f,
                isCritical: true, hitPoint: zombie.transform.position));
            yield return null;

            Assert.That(_played, Contains.Item(FeedbackKind.CriticalHit));
        }

        [UnityTest]
        public IEnumerator AHitAbsorbedByInvincibilityStaysSilent()
        {
            // Showing an impact here would tell the player they were hit when they were not.
            var zombie = Enemy();
            zombie.Actor.SetInvulnerable(ImmunitySource.Script, 1f);
            // Everyone settled first: a landing's dust is not an impact, but it would be heard here.
            var motor = Object.FindAnyObjectByType<PlayerMotor2D>();
            var settle = Time.realtimeSinceStartup + 3f;
            while ((!motor.IsGrounded || !zombie.GetComponent<EnemyMotor2D>().IsGrounded) && Time.realtimeSinceStartup < settle)
            {
                yield return null;
            }

            yield return new WaitForFixedUpdate();

            _played.Clear();
            var result = zombie.Actor.ApplyDamage(new DamageInfo(30f, Vector2.right,
                hitPoint: zombie.transform.position));
            yield return null;

            Assume.That(result.Outcome, Is.EqualTo(DamageOutcome.Ignored));
            Assert.That(_played, Is.Empty, "Impacts: " + Join(_played));
        }

        [UnityTest]
        public IEnumerator EachElementPlaysItsOwnImpact()
        {
            var zombie = Enemy();
            foreach (var pair in new[]
            {
                (DamageType.Fire, FeedbackKind.FireImpact),
                (DamageType.Frost, FeedbackKind.FrostImpact),
                (DamageType.Dark, FeedbackKind.ShadowImpact),
                (DamageType.Magic, FeedbackKind.ArcaneBurst)
            })
            {
                zombie.Actor.ClearInvulnerability();
                zombie.Actor.Health.RestoreToFull();
                _played.Clear();
                zombie.Actor.ApplyDamage(new DamageInfo(8f, Vector2.right, pair.Item1,
                    hitPoint: zombie.transform.position));
                yield return null;

                Assert.That(_played, Contains.Item(pair.Item2),
                    $"{pair.Item1} did not read as {pair.Item2}.");
            }
        }

        // ---------- the screen is taken once ----------

        [UnityTest]
        public IEnumerator AMenuOpenedDuringAHitStopKeepsTheWorldStill()
        {
            // The freeze used to put back the scale it found when it started, so a menu opened
            // inside those few hundredths of a second was undone and the world ran behind it.
            HitStop.Request(0.1f);
            Assert.That(Time.timeScale, Is.Zero);
            GamePause.Push();
            yield return WaitForHitStopToEnd();

            Assert.That(GamePause.IsPaused, Is.True);
            Assert.That(Time.timeScale, Is.Zero, "The open menu must keep the world still.");
            GamePause.Pop();
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator AHitStopEndsBackAtTheSlowMotionScale()
        {
            GameTimeScale.SetBaseScale(0.5f);
            HitStop.Request(0.05f);
            Assert.That(Time.timeScale, Is.Zero);
            yield return WaitForHitStopToEnd();

            Assert.That(Time.timeScale, Is.EqualTo(0.5f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator ClosingAMenuDuringAHitStopLeavesTheFreezeRunning()
        {
            GamePause.Push();
            HitStop.Request(0.2f);
            GamePause.Pop();
            Assert.That(Time.timeScale, Is.Zero, "The freeze is still running.");
            yield return WaitForHitStopToEnd();

            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        private static IEnumerator WaitForHitStopToEnd()
        {
            var deadline = Time.realtimeSinceStartup + 1.5f;
            while (HitStop.IsActive && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(HitStop.IsActive, Is.False, "The freeze never ended.");
        }

        [UnityTest]
        public IEnumerator ManyHitsInOneFrameOnlyFreezeTheGameOnce()
        {
            // An area spell hitting a crowd must not multiply into a noticeable pause.
            var enemies = Object.FindObjectsByType<EnemyController>()
                .Take(4).ToArray();
            Assume.That(enemies.Length, Is.GreaterThan(1));

            foreach (var enemy in enemies)
            {
                enemy.Actor.ClearInvulnerability();
                enemy.Actor.ApplyDamage(new DamageInfo(6f, Vector2.right, staggerDamage: 8f,
                    hitStop: 0.03f, hitPoint: enemy.transform.position));
            }

            yield return null;
            var deadline = Time.realtimeSinceStartup + 1.5f;
            while (HitStop.IsActive && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(HitStop.IsActive, Is.False, "The freeze never ended.");
            Assert.That(Time.timeScale, Is.EqualTo(1f).Within(0.001f),
                "Time has to come back exactly where it was.");
        }

        [UnityTest]
        public IEnumerator TheStrongestMomentOwnsTheScreen()
        {
            // A boss slam landing in the same frame as an ordinary hit must shake like a slam.
            _shaker.Clear();
            CombatFeedbackService.Play(FeedbackKind.LightHit, _player.transform.position);
            CombatFeedbackService.Play(FeedbackKind.BossSlam, _player.transform.position);
            yield return null;
            yield return null;

            Assert.That(_shaker.CurrentAmplitude, Is.GreaterThan(0.2f),
                "The slam was drowned out by the light hit.");
        }

        // ---------- the camera recovers ----------

        [UnityTest]
        public IEnumerator TheCameraStopsShakingAndLeavesNoOffset()
        {
            _shaker.Shake(ShakeProfile.BossLarge);
            yield return null;
            Assume.That(_shaker.CurrentAmplitude, Is.GreaterThan(0f));

            var deadline = Time.realtimeSinceStartup + 2f;
            while (_shaker.CurrentAmplitude > 0f && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(_shaker.CurrentAmplitude, Is.Zero);
            Assert.That(_shaker.CurrentOffset, Is.EqualTo(Vector2.zero));
        }

        [UnityTest]
        public IEnumerator TheCameraStaysWithThePlayerWhileShaking()
        {
            // A shake that loses the player is worse than no shake at all.
            _shaker.Shake(ShakeProfile.BossLarge);
            yield return new WaitForSeconds(0.1f);

            // Measured against where the camera means to be: it frames the player a little low
            // and ahead on purpose, so the raw distance to the player includes that offset.
            var follow = Camera.main.GetComponent<CameraFollow2D>();
            var distance = Vector2.Distance(Camera.main.transform.position,
                follow != null ? follow.TargetPosition : (Vector2)_player.transform.position);
            Assert.That(distance, Is.LessThan(2f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator TurningShakeOffLeavesTheCameraCompletelyStill()
        {
            FeedbackSettings.ScreenShakeMultiplier = 0f;
            _shaker.Clear();
            _shaker.Shake(ShakeProfile.BossLarge);
            yield return null;

            Assert.That(_shaker.CurrentAmplitude, Is.Zero);
            Assert.That(_shaker.CurrentOffset, Is.EqualTo(Vector2.zero));
        }

        [UnityTest]
        public IEnumerator EffectsStillPlayWithEveryEmphasisTurnedOff()
        {
            FeedbackSettings.ReducedMotion = true;
            FeedbackSettings.HitStopMultiplier = 0f;
            _played.Clear();

            CombatFeedbackService.Play(FeedbackKind.Parry, _player.transform.position);
            yield return null;

            Assert.That(_played, Contains.Item(FeedbackKind.Parry),
                "With the camera and flashes off, the particles still have to say what happened.");
            Assert.That(HitStop.IsActive, Is.False);
        }

        // ---------- nothing is left behind ----------

        [UnityTest]
        public IEnumerator EffectsAreRecycledRatherThanAccumulating()
        {
            for (var i = 0; i < 80; i++)
            {
                CombatFeedbackService.Play(FeedbackKind.LightHit,
                    (Vector2)_player.transform.position + Vector2.right * (i % 5));
            }

            yield return null;
            var peak = _feedback.LiveBursts;
            Assert.That(peak, Is.GreaterThan(0));

            yield return new WaitForSeconds(1f);
            Assert.That(_feedback.LiveBursts, Is.Zero, "Bursts have to clean themselves up.");
        }

        [UnityTest]
        public IEnumerator ClearingDropsEverythingAtOnce()
        {
            CombatFeedbackService.Play(FeedbackKind.BossDeath, _player.transform.position);
            _shaker.Shake(ShakeProfile.BossLarge);
            yield return null;

            _feedback.ClearAll();
            yield return null;

            Assert.That(_feedback.LiveBursts, Is.Zero);
            Assert.That(_shaker.CurrentAmplitude, Is.Zero);
            Assert.That(Camera.main.GetComponent<ScreenFlash>().CurrentAlpha, Is.Zero);
        }

        [UnityTest]
        public IEnumerator DyingAndRespawningLeavesNothingRunning()
        {
            // Respawning runs a fade, so this is about the state the player lands in, not about
            // the same frame: once they are back on their feet, nothing from the death may still
            // be shaking the camera, freezing time or drawing itself.
            _player.Actor.ApplyDamage(new DamageInfo(99999f, Vector2.right,
                hitPoint: _player.transform.position));
            _shaker.Shake(ShakeProfile.BossLarge);
            CombatFeedbackService.Play(FeedbackKind.BossDeath, _player.transform.position);
            yield return new WaitForSeconds(0.2f);

            var respawner = Object.FindAnyObjectByType<PlayerRespawner>();
            respawner.RespawnPlayer();

            var deadline = Time.realtimeSinceStartup + 6f;
            while (!_player.IsAlive && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assume.That(_player.IsAlive, Is.True, "The player has to come back.");
            yield return new WaitForSeconds(0.5f);

            Assert.That(_shaker.CurrentAmplitude, Is.Zero, "A respawn starts clean.");
            Assert.That(_feedback.LiveBursts, Is.Zero);
            Assert.That(HitStop.IsActive, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f).Within(0.001f));
        }

        // ---------- status overlays ----------

        [UnityTest]
        public IEnumerator AnEnemyShowsAnOverlayForAnActiveStatusAndDropsItAfterwards()
        {
            var zombie = Enemy();
            var overlays = zombie.GetComponent<StatusVfx>();
            Assume.That(overlays, Is.Not.Null);
            Assume.That(overlays.ActiveOverlays, Is.Zero);

            var statuses = zombie.GetComponent<StatusEffects>();
            statuses.Add(StatusType.Burn, statuses.Rule(StatusType.Burn).threshold * 1.2f);
            yield return new WaitForFixedUpdate();
            yield return null;

            Assume.That(statuses.Remaining(StatusType.Burn), Is.GreaterThan(0f));
            Assert.That(overlays.ActiveOverlays, Is.EqualTo(1));

            statuses.Clear();
            yield return null;

            Assert.That(overlays.ActiveOverlays, Is.Zero, "A cleared status leaves no overlay.");
        }

        [UnityTest]
        public IEnumerator ThreeStatusesAtOnceDoNotBuryTheCreature()
        {
            var zombie = Enemy();
            var overlays = zombie.GetComponent<StatusVfx>();
            var statuses = zombie.GetComponent<StatusEffects>();

            foreach (var type in new[] { StatusType.Burn, StatusType.Frost, StatusType.Poison })
            {
                statuses.Add(type, statuses.Rule(type).threshold * 1.2f);
                yield return new WaitForFixedUpdate();
            }

            yield return null;

            Assert.That(overlays.ActiveOverlays, Is.LessThanOrEqualTo(2),
                "Three overlays at once would hide the wind-up the player needs to read.");
        }

        // ---------- helpers ----------

        private static EnemyController Enemy() =>
            Object.FindObjectsByType<EnemyController>()
                .First(e => e.name.StartsWith("ZombieSoldier"));

        private static string Join(IEnumerable<FeedbackKind> kinds) =>
            string.Join(", ", kinds);
    }
}
