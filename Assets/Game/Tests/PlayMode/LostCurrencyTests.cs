using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The price of dying (item 8 of the 2026-09-27 list): all the gold stays where the hero last
    /// stood on solid ground, touching the trace takes it back, and a second death loses it.
    /// </summary>
    public sealed class LostCurrencyTests
    {
        private PlayerCharacter _player;
        private PlayerMotor2D _motor;
        private PlayerRespawner _respawner;
        private GameProgress _progress;
        private LostCurrencyMark _mark;
        private CombatFeedbackService _feedback;
        private readonly List<FeedbackKind> _played = new List<FeedbackKind>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            yield return TestWorld.Load();
            _player = Object.FindAnyObjectByType<PlayerCharacter>();
            _motor = _player.GetComponent<PlayerMotor2D>();
            _respawner = Object.FindAnyObjectByType<PlayerRespawner>();
            _progress = Object.FindAnyObjectByType<GameProgress>();
            _mark = _player.GetComponent<LostCurrencyDrop>().Mark;
            _feedback = Object.FindAnyObjectByType<CombatFeedbackService>();
            _feedback.Played += OnPlayed;
            yield return Land();
        }

        [TearDown]
        public void TearDown()
        {
            if (_feedback != null) { _feedback.Played -= OnPlayed; }
            GamePause.Clear();
            SaveSystem.Delete();
            GameTimeScale.Reset();
        }

        private void OnPlayed(FeedbackKind kind, Vector2 position) => _played.Add(kind);

        [UnityTest]
        public IEnumerator DyingLeavesAllTheGoldBehindAndTouchingTheTraceGivesItBack()
        {
            Assert.That(_mark, Is.Not.Null, "The player ships with the trace of the lost gold.");
            Assert.That(_mark.IsShown, Is.False, "Nothing is lost at the start.");
            _progress.AddCurrency(120);
            var ground = _motor.LastSafeGround;

            _respawner.Kill();
            yield return null;
            Assert.That(_progress.Currency, Is.Zero, "The purse is left behind, all of it.");
            Assert.That(_progress.LostCurrency, Is.EqualTo(120));
            Assert.That(_mark.IsShown, Is.True);
            Assert.That(Vector2.Distance(_mark.transform.position, ground), Is.LessThan(0.01f),
                "It waits where the hero last stood.");

            yield return WaitForRespawn();
            _motor.ResetTo(_mark.transform.position);
            yield return Land();
            yield return new WaitForFixedUpdate();
            var interactor = _player.GetComponentInChildren<PlayerInteractor>();
            Assert.That(interactor.CurrentPrompt, Does.Contain("120"),
                "Standing on it offers the gold back.");

            _mark.Interact(_player);
            Assert.That(_progress.Currency, Is.EqualTo(120));
            Assert.That(_progress.HasLostCurrency, Is.False);
            Assert.That(_mark.IsShown, Is.False);
            Assert.That(_played, Has.Member(FeedbackKind.LostGoldRecovered));
        }

        [UnityTest]
        public IEnumerator ASecondDeathLosesTheOldGoldForGood()
        {
            _progress.AddCurrency(100);
            _respawner.Kill();
            yield return WaitForRespawn();

            _progress.AddCurrency(30);
            _respawner.Kill();
            yield return null;

            Assert.That(_progress.LostCurrency, Is.EqualTo(30), "Only the latest death can be undone.");
            Assert.That(_progress.Currency, Is.Zero);
            yield return WaitForRespawn();
        }

        [UnityTest]
        public IEnumerator DyingWithAnEmptyPurseLeavesNoTraceAndStillLosesTheOld()
        {
            _progress.AddCurrency(50);
            _respawner.Kill();
            yield return WaitForRespawn();
            Assert.That(_mark.IsShown, Is.True);

            _respawner.Kill();
            yield return null;
            Assert.That(_progress.HasLostCurrency, Is.False);
            Assert.That(_mark.IsShown, Is.False, "Nothing to find.");
            yield return WaitForRespawn();
        }

        [UnityTest]
        public IEnumerator ADeathInTheAirLeavesTheGoldOnTheGroundLastStoodOn()
        {
            var ground = _motor.LastSafeGround;
            _progress.AddCurrency(40);
            _motor.ResetTo(ground + new Vector2(0f, 4f));
            yield return new WaitForFixedUpdate();
            Assert.That(_motor.IsGrounded, Is.False);

            _respawner.Kill();
            yield return null;
            Assert.That(Vector2.Distance(_progress.LostCurrencyPosition, ground), Is.LessThan(0.01f),
                "Never in the air or over a pit: somewhere the hero can walk back to.");
            yield return WaitForRespawn();
        }

        [UnityTest]
        public IEnumerator TheLostGoldOutlivesQuittingTheGame()
        {
            _progress.AddCurrency(75);
            _respawner.Kill();
            yield return null;
            _progress.SaveNow();

            _progress.LoadGame();
            yield return null;
            Assert.That(_progress.LostCurrency, Is.EqualTo(75));
            Assert.That(_progress.Currency, Is.Zero, "Quitting on the death screen gives nothing back.");
            Assert.That(_mark.IsShown, Is.True);
            yield return WaitForRespawn();
        }

        // ---------- helpers ----------

        private IEnumerator WaitForRespawn()
        {
            var started = Time.realtimeSinceStartup;
            while ((!_player.IsAlive || _respawner.IsSequenceRunning)
                && Time.realtimeSinceStartup - started < 6f)
            {
                yield return null;
            }

            Assert.That(_player.IsAlive, Is.True, "The player never came back.");
        }

        private IEnumerator Land()
        {
            var started = Time.realtimeSinceStartup;
            while (!_motor.IsGrounded && Time.realtimeSinceStartup - started < 3f)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(_motor.IsGrounded, Is.True, "The player never landed.");
        }
    }
}
