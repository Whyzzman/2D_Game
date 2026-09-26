using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The feedback layer as data: that every combat moment has a profile, that the profiles are
    /// ordered so the right one owns the screen, and that presentation never smuggled gameplay
    /// numbers in with it.
    /// </summary>
    public sealed class CombatFeedbackDataTests
    {
        private FeedbackLibrary _library;

        [SetUp]
        public void SetUp()
        {
            _library = Load<FeedbackLibrary>(
                "Assets/Game/Data/Feedback/FeedbackLibrary.asset");
        }

        [TearDown]
        public void TearDown() => FeedbackSettings.ResetToDefaults();

        [Test]
        public void EveryCombatMomentHasAProfile()
        {
            Assert.That(_library.Validate(), Is.True);
            foreach (FeedbackKind kind in Enum.GetValues(typeof(FeedbackKind)))
            {
                Assert.That(_library.Has(kind), Is.True, $"{kind} has no profile.");
            }
        }

        [Test]
        public void EveryProfileHasSomethingToShow()
        {
            // A moment with no burst at all is silent, and silence is indistinguishable from a bug.
            foreach (FeedbackKind kind in Enum.GetValues(typeof(FeedbackKind)))
            {
                Assert.That(_library.Profile(kind).HasBurst, Is.True, $"{kind} draws nothing.");
            }
        }

        // ---------- the hierarchy ----------

        [Test]
        public void ThePriorityOrderRunsFromAnOrdinaryHitUpToTheBossDying()
        {
            // This order is what decides who owns the screen when several things land at once.
            var expected = new[]
            {
                FeedbackKind.LightHit, FeedbackKind.HeavyHit, FeedbackKind.CriticalHit,
                FeedbackKind.StaggerBreak, FeedbackKind.DashStrike, FeedbackKind.GroundBreaker,
                FeedbackKind.Parry, FeedbackKind.GuardBreak, FeedbackKind.BossSlam,
                FeedbackKind.BossPhase, FeedbackKind.BossDeath
            };

            for (var i = 1; i < expected.Length; i++)
            {
                Assert.That(_library.Profile(expected[i]).Priority,
                    Is.GreaterThan(_library.Profile(expected[i - 1]).Priority),
                    $"{expected[i]} must outrank {expected[i - 1]}.");
            }
        }

        [Test]
        public void AHeavyBlowFreezesLongerThanALightOne()
        {
            var light = _library.Profile(FeedbackKind.LightHit).HitStop;
            var heavy = _library.Profile(FeedbackKind.HeavyHit).HitStop;
            var parry = _library.Profile(FeedbackKind.Parry).HitStop;

            Assert.That(light, Is.InRange(0.02f, 0.04f), "A light hit is a tap, not a pause.");
            Assert.That(heavy, Is.InRange(0.05f, 0.08f));
            Assert.That(heavy, Is.GreaterThan(light));
            Assert.That(parry, Is.InRange(0.06f, 0.1f));
            Assert.That(parry, Is.GreaterThan(heavy));
        }

        [Test]
        public void NoFreezeIsLongEnoughToFeelLikeAStutter()
        {
            // The failure mode this guards against is a "weighty" hit that is really just the game
            // hanging. Nothing in the slice may freeze for more than an eighth of a second.
            foreach (FeedbackKind kind in Enum.GetValues(typeof(FeedbackKind)))
            {
                Assert.That(_library.Profile(kind).HitStop, Is.LessThanOrEqualTo(0.125f),
                    $"{kind} freezes for too long.");
            }
        }

        [Test]
        public void OnlyTheRarestMomentsAreAllowedToFlashTheScreen()
        {
            var allowed = new[]
            {
                FeedbackKind.Parry, FeedbackKind.GuardBreak, FeedbackKind.CriticalHit,
                FeedbackKind.BossSlam, FeedbackKind.BossPhase, FeedbackKind.BossDeath
            };

            foreach (FeedbackKind kind in Enum.GetValues(typeof(FeedbackKind)))
            {
                if (allowed.Contains(kind))
                {
                    continue;
                }

                Assert.That(_library.Profile(kind).ScreenFlash, Is.Zero,
                    $"{kind} flashes the screen; that is reserved for the moments that earn it.");
            }
        }

        [Test]
        public void TheFourElementsKeepDifferentLightColours()
        {
            // Light colour is most of how a player tells at a glance what just hit them, so fire,
            // frost, arcane and shadow must never drift towards each other.
            var fire = _library.Profile(FeedbackKind.FireImpact).LightColor;
            var frost = _library.Profile(FeedbackKind.FrostImpact).LightColor;
            var arcane = _library.Profile(FeedbackKind.ArcaneBurst).LightColor;
            var shadow = _library.Profile(FeedbackKind.ShadowImpact).LightColor;

            foreach (var pair in new[]
            {
                (fire, frost), (fire, arcane), (fire, shadow),
                (frost, arcane), (frost, shadow), (arcane, shadow)
            })
            {
                var difference = Mathf.Abs(pair.Item1.r - pair.Item2.r)
                    + Mathf.Abs(pair.Item1.g - pair.Item2.g)
                    + Mathf.Abs(pair.Item1.b - pair.Item2.b);
                Assert.That(difference, Is.GreaterThan(0.35f),
                    "Two elements light the room the same colour.");
            }
        }

        [Test]
        public void MovementAndStatusesNeverShakeTheCamera()
        {
            // These fire constantly. A camera that moved for every dash and every poison tick
            // would never be still.
            foreach (var kind in new[]
            {
                FeedbackKind.Dash, FeedbackKind.AirDash, FeedbackKind.WallContact,
                FeedbackKind.PoisonProc, FeedbackKind.BurnProc, FeedbackKind.FrostProc
            })
            {
                Assert.That(_library.Profile(kind).Shake, Is.EqualTo(ShakeProfile.None), $"{kind}");
            }
        }

        [Test]
        public void EveryMajorMomentCarriesASoundHook()
        {
            // Audio lands in a later phase; the hooks have to be here before then or every one of
            // them becomes a separate change to combat code later.
            foreach (var kind in new[]
            {
                FeedbackKind.Parry, FeedbackKind.GuardBreak, FeedbackKind.BossSlam,
                FeedbackKind.BossPhase, FeedbackKind.BossDeath, FeedbackKind.FireImpact,
                FeedbackKind.FrostImpact, FeedbackKind.ArcaneBurst, FeedbackKind.ShadowImpact
            })
            {
                Assert.That(_library.Profile(kind).SoundEvent, Is.Not.Empty, $"{kind}");
            }
        }

        // ---------- accessibility ----------

        [Test]
        public void EveryEmphasisCanBeTurnedOff()
        {
            FeedbackSettings.ScreenShakeMultiplier = 0f;
            FeedbackSettings.FlashIntensity = 0f;
            FeedbackSettings.HitStopMultiplier = 0f;

            Assert.That(_library.Profile(FeedbackKind.Parry).ScreenFlash, Is.Zero);
            Assert.That(_library.Profile(FeedbackKind.Parry).HitStop, Is.Zero);
            Assert.That(_library.Profile(FeedbackKind.Parry).HasBurst, Is.True,
                "With everything turned down, the particles still have to say what happened.");
        }

        [Test]
        public void TheAccessibilityScalarsClampInsteadOfAcceptingNonsense()
        {
            FeedbackSettings.FlashIntensity = 5f;
            Assert.That(FeedbackSettings.FlashIntensity, Is.EqualTo(1f));

            FeedbackSettings.ScreenShakeMultiplier = -3f;
            Assert.That(FeedbackSettings.ScreenShakeMultiplier, Is.Zero);

            FeedbackSettings.HitStopMultiplier = 99f;
            Assert.That(FeedbackSettings.HitStopMultiplier, Is.EqualTo(2f));
        }

        [Test]
        public void ReducedMotionTurnsDownTheThingsThatMoveTheWholeScreen()
        {
            FeedbackSettings.ReducedMotion = true;

            Assert.That(FeedbackSettings.ScreenShakeMultiplier, Is.Zero);
            Assert.That(FeedbackSettings.FlashIntensity, Is.Zero);
            Assert.That(FeedbackSettings.ReducedMotion, Is.True);
        }

        private static T Load<T>(string path) where T : ScriptableObject
        {
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(asset, Is.Not.Null, $"The content build writes {path}.");
            return asset;
        }
    }
}
