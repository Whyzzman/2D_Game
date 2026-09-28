using System;
using System.Collections;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The end of the game (item 5 of the big list, gamedesign/STORY.md): a few seconds after the
    /// Oath-Bound King falls the world stops, three lines come one by one, the credits roll, and
    /// the summary waits for «Продовжити», which takes the hero back to the last altar with the
    /// grapple in hand to play on. Holding interact skips a part. The save marks the game
    /// completed before the first line, so quitting during the ending loses nothing.
    /// <see cref="EndingView"/> draws it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EndingSequence : MonoBehaviour
    {
        public const string FinalBossId = "ashen-king";
        public const float LineSeconds = 4.5f;
        public const float CreditsSeconds = 20f;

        public static readonly string[] Lines =
        {
            "Король упав, і слово його впало разом із ним.",
            "Попіл нарешті охолов. Ліс, що тримав присягу, відпустив коріння.",
            "Королівство забули. Уперше за сто років воно спочиває.\nА тінь без імені лишилася — пам'ятати за всіх."
        };

        public enum Part
        {
            None,
            Lines,
            Credits,
            Summary
        }

        [SerializeField] private GameProgress _progress;
        [SerializeField] private PlayerRespawner _respawner;
        [SerializeField] private PlayerCharacter _player;
        [Tooltip("From the King's fall to the first line: his death is seen through.")]
        [SerializeField, Min(0f)] private float _delay = 3f;

        private PlayerInputReader _input;
        private Coroutine _running;
        private bool _skip;
        private float _clock;

        public Part Current { get; private set; }

        /// <summary>The line on screen now, 0–2, while the lines run.</summary>
        public int Line { get; private set; }

        /// <summary>Seconds into the credits, for the view to roll them.</summary>
        public float CreditsProgress => Current == Part.Credits ? Mathf.Clamp01(_clock / CreditsSeconds) : 0f;

        public GameSummary Summary { get; private set; }
        public bool IsRunning => Current != Part.None || _running != null;

        public event Action Changed;

        private void Awake()
        {
            if (_progress == null) { _progress = FindAnyObjectByType<GameProgress>(); }
            if (_respawner == null) { _respawner = FindAnyObjectByType<PlayerRespawner>(); }
            if (_player == null) { _player = FindAnyObjectByType<PlayerCharacter>(); }
            _input = _player != null ? _player.GetComponent<PlayerInputReader>() : null;
        }

        private void OnEnable()
        {
            if (_progress != null) { _progress.WorldChanged += OnWorldChanged; }
        }

        private void OnDisable()
        {
            if (_progress != null) { _progress.WorldChanged -= OnWorldChanged; }
            if (Current != Part.None) { Finish(false); }
            _running = null;
        }

        private void OnWorldChanged(WorldFlag flag, string id)
        {
            if (flag == WorldFlag.BossDefeated && id == FinalBossId)
            {
                Begin();
            }
        }

        /// <summary>Starts the ending; also for tests and the debug menu.</summary>
        public void Begin()
        {
            if (IsRunning || !isActiveAndEnabled)
            {
                return;
            }

            _running = StartCoroutine(Run());
        }

        /// <summary>The rest of the part on screen is skipped; the summary is never skipped.</summary>
        public void Skip() => _skip = Current == Part.Lines || Current == Part.Credits;

        /// <summary>«Продовжити» on the summary: back to the world at the last altar.</summary>
        public void Continue()
        {
            if (Current == Part.Summary)
            {
                Finish(true);
            }
        }

        private IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(_delay);
            if (_progress != null)
            {
                _progress.Data.completed = true;
                _progress.SaveNow();
            }

            Inscription.Close();
            _input?.SetGameplayEnabled(false);
            GamePause.Push();
            Current = Part.Lines;
            for (Line = 0; Line < Lines.Length && !_skip; Line++)
            {
                Changed?.Invoke();
                yield return Wait(LineSeconds);
            }

            _skip = false;
            Line = Lines.Length - 1;
            Current = Part.Credits;
            Changed?.Invoke();
            yield return Wait(CreditsSeconds);

            _skip = false;
            Summary = GameSummary.Of(_progress);
            Current = Part.Summary;
            Changed?.Invoke();
            _running = null;
        }

        private IEnumerator Wait(float seconds)
        {
            for (_clock = 0f; _clock < seconds && !_skip; _clock += Time.unscaledDeltaTime)
            {
                yield return null;
            }
        }

        private void Finish(bool travel)
        {
            if (_running != null)
            {
                StopCoroutine(_running);
                _running = null;
            }

            _skip = false;
            Current = Part.None;
            GamePause.Pop();
            _input?.SetGameplayEnabled(true);
            Changed?.Invoke();
            if (travel && _respawner != null)
            {
                _respawner.BeginTravel(_respawner.ActiveCheckpointId);
            }
        }
    }
}
