using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The end of the game (item 5 of the big list, stage B): the King's fall marks the save
    /// completed and runs the lines, the credits and the summary; «Продовжити» gives the world
    /// back. Deaths are counted in the save for the summary.
    /// </summary>
    public sealed class EndingTests
    {
        private GameProgress _progress;
        private EndingSequence _ending;
        private HudController _hud;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            yield return TestWorld.Load();
            _progress = Object.FindAnyObjectByType<GameProgress>();
            _ending = Object.FindAnyObjectByType<EndingSequence>();
            _hud = Object.FindAnyObjectByType<HudController>();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // A failed test may leave the ending up: switching it off lets the world go.
            if (_ending != null) { _ending.enabled = false; }
            SaveSystem.Delete();
            yield return null;
        }

        private static IEnumerator Until(Func<bool> condition, float seconds, string what)
        {
            var deadline = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(condition(), Is.True, what);
        }

        [UnityTest]
        public IEnumerator TheKingsFallRunsTheEndingAndTheSummaryGivesTheWorldBack()
        {
            Assert.That(_ending, Is.Not.Null, "The world scene carries the ending.");
            _progress.Data.deaths = 2;
            _progress.MarkBossDefeated(EndingSequence.FinalBossId);
            yield return Until(() => _ending.Current == EndingSequence.Part.Lines, 8f, "The lines come after the fall.");
            yield return null;
            Assert.That(_progress.Data.completed, Is.True, "The save knows the game is done.");
            Assert.That(SaveSystem.Read().completed, Is.True, "Written at once.");
            Assert.That(GamePause.IsPaused, Is.True, "The world stops for the ending.");
            Assert.That(_hud.Ending.IsOpen, Is.True);
            Assert.That(_hud.Ending.LinesShown, Is.EqualTo(1), "One line at a time.");

            _ending.Skip();
            yield return Until(() => _ending.Current == EndingSequence.Part.Credits, 2f, "Skipping the lines rolls the credits.");
            _ending.Skip();
            yield return Until(() => _ending.Current == EndingSequence.Part.Summary, 2f, "Skipping the credits shows the summary.");
            yield return null;
            var values = _hud.Ending.SummaryValues;
            Assert.That(values[1], Is.EqualTo("2"), "Deaths.");
            StringAssert.EndsWith("%", values[2], "The map as a share.");
            StringAssert.Contains(" / ", values[4], "Secrets found of all.");
            Assert.That(values[5], Is.EqualTo(_progress.Data.player.level.ToString()));

            _ending.Skip();
            yield return null;
            Assert.That(_ending.Current, Is.EqualTo(EndingSequence.Part.Summary), "The summary waits for «Продовжити».");
            _ending.Continue();
            yield return null;
            Assert.That(_ending.Current, Is.EqualTo(EndingSequence.Part.None));
            Assert.That(_hud.Ending.IsOpen, Is.False);
            Assert.That(GamePause.IsPaused, Is.False, "The world goes on.");
        }

        [UnityTest]
        public IEnumerator DeathsAreCountedInTheSave()
        {
            var respawner = Object.FindAnyObjectByType<PlayerRespawner>();
            respawner.Kill();
            yield return null;
            Assert.That(_progress.Data.deaths, Is.EqualTo(1));
            Assert.That(GameSummary.FormatTime(4 * 3600 + 12 * 60 + 5), Is.EqualTo("4 год 12 хв"));
            Assert.That(GameSummary.FormatTime(12 * 60), Is.EqualTo("12 хв"));
        }
    }
}
