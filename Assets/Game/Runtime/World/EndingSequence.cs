using System;
using System.Collections;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The end of the game (item 5 of the big list, gamedesign/STORY.md): a few seconds after the
    /// Oath-Bound King falls, his farewell — the world still going, the eclipse lifting, the camera
    /// drawn in, his last two lines under him (gamedesign/KING_FINALE.md, 2026-11-12); then the world
    /// stops, three lines come one by one, the credits roll, and
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
        public const float FarewellLineSeconds = 4f;

        /// <summary>The camera's size through the farewell, against its usual one: drawn in to him.</summary>
        public const float FarewellZoom = 0.8f;

        /// <summary>The King's last words, under him while the world still runs (decided by Claude, 2026-11-12).</summary>
        public static readonly string[] FarewellLines =
        {
            "Я дав слово, що вас не забудуть…",
            "…і тримав його, доки від вас не лишився попіл. Пам'ятай їх ти."
        };
        public const float CreditsSeconds = 20f;

        public static readonly string[] Lines =
        {
            "Король упав, і слово його впало разом із ним.",
            "Попіл нарешті охолов. Ліс, що тримав присягу, відпустив коріння.",
            "Королівство забули. Уперше за триста років воно спочиває.\nА тінь без імені лишилася — пам'ятати за всіх."
        };

        public enum Part
        {
            None,
            Lines,
            Credits,
            Summary,
            /// <summary>The King's last words, before the dark: the world is not paused yet.</summary>
            Farewell
        }

        [SerializeField] private GameProgress _progress;
        [SerializeField] private PlayerRespawner _respawner;
        [SerializeField] private PlayerCharacter _player;
        [Tooltip("From the King's fall to the first line: his death is seen through.")]
        [SerializeField, Min(0f)] private float _delay = 3f;

        private PlayerInputReader _input;
        private CameraFollow2D _camera;
        private bool _paused;
        private Coroutine _running;
        private bool _skip;
        private float _clock;

        public Part Current { get; private set; }

        /// <summary>The line on screen now: 0–2 while the lines run, 0–1 through the farewell.</summary>
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
            _camera = FindAnyObjectByType<CameraFollow2D>();
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
        public void Skip() => _skip = Current == Part.Farewell || Current == Part.Lines || Current == Part.Credits;

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
            Current = Part.Farewell;
            _camera?.SetZoom(FarewellZoom, 2.5f);
            for (Line = 0; Line < FarewellLines.Length && !_skip; Line++)
            {
                Changed?.Invoke();
                yield return Wait(FarewellLineSeconds);
            }

            _skip = false;
            _camera?.SetZoom(1f, 0.5f);
            GamePause.Push();
            _paused = true;
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
            if (_paused)
            {
                GamePause.Pop();
                _paused = false;
            }

            _camera?.SetZoom(1f, 0.5f);
            _input?.SetGameplayEnabled(true);
            Changed?.Invoke();
            if (travel && _respawner != null)
            {
                _respawner.BeginTravel(_respawner.ActiveCheckpointId);
            }
        }
    }
}
