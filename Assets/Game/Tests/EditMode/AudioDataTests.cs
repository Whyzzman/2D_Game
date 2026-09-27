using System;
using NUnit.Framework;
using UnityEngine;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The audio layer as data: that every sound exists, that the mix is ordered so the important
    /// things survive a busy fight, and that the sounds which fire constantly are held back.
    /// </summary>
    public sealed class AudioDataTests
    {
        private AudioLibrary _library;

        [SetUp]
        public void SetUp()
        {
            _library = Load<AudioLibrary>("Assets/Game/Data/Audio/AudioLibrary.asset");
        }

        [Test]
        public void EverySoundExistsAndHasClips()
        {
            Assert.That(_library.Validate(), Is.True);
            foreach (AudioId id in Enum.GetValues(typeof(AudioId)))
            {
                if (id == AudioId.None)
                {
                    continue;
                }

                var entry = _library.Event(id);
                Assert.That(entry, Is.Not.Null, $"{id} has no event.");
                Assert.That(entry.HasClips, Is.True, $"{id} has no clips.");
            }
        }

        [Test]
        public void AnUnsetReferenceAsksForSilenceRatherThanARandomSound()
        {
            // Unity's serializer skips field initializers, so anything forgotten lands on zero.
            Assert.That(default(AudioId), Is.EqualTo(AudioId.None));
            Assert.That(_library.Event(AudioId.None), Is.Null);
        }

        // ---------- the mix is ordered ----------

        [Test]
        public void TheChargeTelegraphOutranksEverythingElse()
        {
            // A player who misses this eats the charge. It must never be the sound that gets
            // dropped when the voices run out.
            var charge = _library.Event(AudioId.BossChargeStart).Priority;
            foreach (AudioId id in Enum.GetValues(typeof(AudioId)))
            {
                if (id == AudioId.None || id == AudioId.BossChargeStart)
                {
                    continue;
                }

                Assert.That(_library.Event(id).Priority, Is.LessThanOrEqualTo(charge),
                    $"{id} outranks the charge telegraph.");
            }
        }

        [Test]
        public void ThePriorityOrderRunsFromTheRoomUpToTheBoss()
        {
            var order = new[]
            {
                AudioId.AmbienceTombBase, AudioId.FootstepStone, AudioId.PickupCommon,
                AudioId.SwordSwingLight, AudioId.HitFlesh, AudioId.Block, AudioId.Parry,
                AudioId.BossSlam, AudioId.BossChargeStart
            };

            for (var i = 1; i < order.Length; i++)
            {
                Assert.That(_library.Event(order[i]).Priority,
                    Is.GreaterThan(_library.Event(order[i - 1]).Priority),
                    $"{order[i]} must outrank {order[i - 1]}.");
            }
        }

        [Test]
        public void TheRoomIsTheFirstThingDropped()
        {
            foreach (var id in new[]
            {
                AudioId.AmbienceTombBase, AudioId.AmbienceChain, AudioId.AmbienceDrip,
                AudioId.AmbienceWind, AudioId.AmbienceStoneSettle
            })
            {
                Assert.That(_library.Event(id).Priority, Is.LessThan(20), $"{id}");
                Assert.That(_library.Event(id).Bus, Is.EqualTo(AudioBus.Ambience), $"{id}");
            }
        }

        [Test]
        public void TheParryIsLouderAndRarerThanAnOrdinaryBlock()
        {
            var block = _library.Event(AudioId.Block);
            var parry = _library.Event(AudioId.Parry);

            Assert.That(parry.Volume, Is.GreaterThan(block.Volume));
            Assert.That(parry.Priority, Is.GreaterThan(block.Priority),
                "A parry must never lose a voice to a block.");
        }

        [Test]
        public void TheGreatswordIsHeavierBeforeAClipIsEvenChosen()
        {
            Assert.That(_library.Event(AudioId.GreatswordSwingHeavy).Volume,
                Is.GreaterThan(_library.Event(AudioId.SwordSwingHeavy).Volume));
            Assert.That(_library.Event(AudioId.GreatswordSwingLight).Volume,
                Is.GreaterThan(_library.Event(AudioId.SwordSwingLight).Volume));
        }

        [Test]
        public void TheSlamIsTheLoudestImpactInTheGame()
        {
            var slam = _library.Event(AudioId.BossSlam).Volume;

            Assert.That(slam, Is.GreaterThan(_library.Event(AudioId.GroundBreaker).Volume),
                "The player's heaviest skill sits below the boss's heaviest attack.");
            Assert.That(slam, Is.GreaterThan(_library.Event(AudioId.HitHeavy).Volume));
        }

        // ---------- the things that fire constantly are held back ----------

        [Test]
        public void RepeatedActionsHaveEnoughVariantsToNotWearAGroove()
        {
            Assert.That(_library.Event(AudioId.FootstepStone).ClipCount,
                Is.GreaterThanOrEqualTo(4), "Footsteps are the most repeated sound in the game.");
            Assert.That(_library.Event(AudioId.SwordSwingLight).ClipCount,
                Is.GreaterThanOrEqualTo(3));
            Assert.That(_library.Event(AudioId.HitFlesh).ClipCount, Is.GreaterThanOrEqualTo(3));
        }

        [Test]
        public void CrowdedSoundsAreCappedSoThreeEnemiesAreNotThreeTimesAsLoud()
        {
            foreach (var id in new[]
            {
                AudioId.ZombieStep, AudioId.GuardStep, AudioId.FootstepStone,
                AudioId.BleedProc, AudioId.PoisonProc, AudioId.BurnProc, AudioId.FrostProc
            })
            {
                Assert.That(_library.Event(id).MaxVoices, Is.LessThanOrEqualTo(4), $"{id}");
            }
        }

        [Test]
        public void SoundsAHeldKeyCouldSpamHaveAMinimumGap()
        {
            foreach (var id in new[]
            {
                AudioId.CastFailed, AudioId.UiError, AudioId.FootstepStone,
                AudioId.ZombieStep, AudioId.PickupCurrency
            })
            {
                Assert.That(_library.Event(id).MinimumInterval, Is.GreaterThan(0f), $"{id}");
            }
        }

        [Test]
        public void OnlyTheRoomLoops()
        {
            foreach (AudioId id in Enum.GetValues(typeof(AudioId)))
            {
                if (id == AudioId.None)
                {
                    continue;
                }

                var entry = _library.Event(id);
                if (!entry.Loop)
                {
                    continue;
                }

                Assert.That(entry.Bus, Is.EqualTo(AudioBus.Ambience),
                    $"{id} loops but is not ambience; loops are the easiest thing to leave running.");
            }
        }

        // ---------- buses ----------

        [Test]
        public void InterfaceSoundsAreOnTheInterfaceBus()
        {
            foreach (var id in new[]
            {
                AudioId.UiNavigate, AudioId.UiConfirm, AudioId.UiCancel, AudioId.UiOpen,
                AudioId.UiClose, AudioId.UiError, AudioId.UiSaveFailed
            })
            {
                Assert.That(_library.Event(id).Bus, Is.EqualTo(AudioBus.Ui), $"{id}");
            }
        }

        [Test]
        public void SoundsThatBelongToThePlayerRatherThanToAPlaceAreNotPositioned()
        {
            // Dying, levelling up and reaching a checkpoint happen to you, not somewhere near you.
            foreach (var id in new[]
            {
                AudioId.PlayerDeath, AudioId.LevelUp, AudioId.CheckpointActivate,
                AudioId.BossPhase, AudioId.BossDeath
            })
            {
                Assert.That(_library.Event(id).SpatialBlend, Is.Zero, $"{id}");
            }
        }

        [Test]
        public void VariationPicksDifferentClipsRatherThanTheSameOneRepeatedly()
        {
            var footsteps = _library.Event(AudioId.FootstepStone);
            Assume.That(footsteps.ClipCount, Is.GreaterThan(1));

            var lastIndex = -1;
            var seen = new System.Collections.Generic.HashSet<AudioClip>();
            for (var i = 0; i < 40; i++)
            {
                var previous = lastIndex;
                seen.Add(footsteps.PickClip(ref lastIndex));
                Assert.That(lastIndex, Is.Not.EqualTo(previous),
                    "The same variant twice in a row is what makes a library sound like one.");
            }

            Assert.That(seen.Count, Is.GreaterThan(1));
        }

        // ---------- the score ----------

        [TestCase("music_tomb", "music_tomb_pulse")]
        [TestCase("music_forest", "music_forest_pulse")]
        [TestCase("music_boss", "music_boss_drums")]
        public void AFightLayerIsExactlyAsLongAsItsTrack(string track, string layer)
        {
            // Started on one tick and looping together, they stay in step only if they match.
            Assert.That(Samples(layer).Length, Is.EqualTo(Samples(track).Length));
        }

        [TestCase("music_menu")]
        [TestCase("music_tomb")]
        [TestCase("music_tomb_pulse")]
        [TestCase("music_forest")]
        [TestCase("music_forest_pulse")]
        [TestCase("music_boss")]
        [TestCase("music_boss_drums")]
        [TestCase("music_victory")]
        public void EveryLoopJoinsWithoutAClick(string name)
        {
            var samples = Samples(name);
            Assert.That(samples.Length, Is.GreaterThan(22050 * 10), "Long enough not to wear thin.");
            Assert.That(Mathf.Abs(samples[samples.Length - 1] - samples[0]), Is.LessThan(0.05f),
                "The end runs into the start.");
        }

        /// <summary>The raw 16-bit samples of a generated track, read from the file itself.</summary>
        private static float[] Samples(string name)
        {
            var path = System.IO.Path.GetFullPath($"Assets/Game/Audio/Music/{name}.wav");
            Assert.That(System.IO.File.Exists(path), Is.True, $"The content build writes {name}.");
            var bytes = System.IO.File.ReadAllBytes(path);
            var samples = new float[(bytes.Length - 44) / 2];
            for (var i = 0; i < samples.Length; i++)
            {
                samples[i] = BitConverter.ToInt16(bytes, 44 + i * 2) / 32000f;
            }

            return samples;
        }

        private static T Load<T>(string path) where T : ScriptableObject
        {
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(asset, Is.Not.Null, $"The content build writes {path}.");
            return asset;
        }
    }
}
