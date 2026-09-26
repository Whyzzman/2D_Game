using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>Where the game currently is, musically.</summary>
    public enum MusicState
    {
        Silent,
        MainMenu,
        Exploration,
        BossIntro,
        BossPhaseOne,
        BossPhaseTwo,
        BossVictory,
        Death
    }

    /// <summary>
    /// One region's score. The states are the same everywhere — exploring, a boss waking, a phase
    /// turning, a victory — so a second region is another set of tracks rather than another set of
    /// states, and the fight code never learns that the forest exists.
    /// </summary>
    [Serializable]
    public sealed class MusicPalette
    {
        [SerializeField] private string _zoneId = "zone_forgotten_tomb";
        [SerializeField] private AudioClip _exploration;
        [SerializeField] private AudioClip _bossIntro;
        [SerializeField] private AudioClip _bossPhaseOne;
        [SerializeField] private AudioClip _bossPhaseTwo;
        [SerializeField] private AudioClip _victory;

        public string ZoneId => _zoneId;
        public AudioClip Exploration => _exploration;
        public AudioClip BossIntro => _bossIntro;
        public AudioClip BossPhaseOne => _bossPhaseOne;
        public AudioClip BossPhaseTwo => _bossPhaseTwo;
        public AudioClip Victory => _victory;
    }

    /// <summary>
    /// Decides what the music should be doing and crossfades between tracks.
    ///
    /// No boss script starts a track by name. The fight raises the events it always raised, this
    /// listens, and the music follows — which is what lets a composer change the whole score later
    /// without touching a line of combat code.
    ///
    /// There is no music yet, and this deliberately does not pretend otherwise: with no clip
    /// assigned it still changes state and raises the event, and plays silence. The value now is
    /// that every transition in the slice is already wired and tested — the encounter beginning,
    /// the phase turning, the boss dying, the player dying and coming back — so writing the score
    /// later is adding clips rather than threading a new system through the fight.
    ///
    /// Two sources, not one: a hard cut from one track to another is a jump-scare, so the outgoing
    /// track fades out while the incoming one fades in.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MusicDirector : MonoBehaviour
    {
        [Header("Tracks (optional — the director works silently without them)")]
        [Tooltip("One set of tracks per region. The first is used until a zone is chosen.")]
        [SerializeField] private MusicPalette[] _palettes = { new MusicPalette() };
        [SerializeField] private AudioClip _mainMenu;

        [Header("Behaviour")]
        [Tooltip("Crossfade length. Long enough not to be a cut, short enough not to be a smear.")]
        [SerializeField, Min(0.05f)] private float _crossfade = 1.6f;
        [Tooltip("How far the score steps back when something louder needs the room.")]
        [SerializeField, Range(0f, 1f)] private float _duckAmount = 0.45f;
        [SerializeField, Min(0.05f)] private float _duckRecovery = 1.5f;
        [SerializeField, Range(0f, 1f)] private float _volume = 0.55f;

        private AudioSource _current;
        private AudioSource _previous;
        private MusicPalette _palette;
        private MusicState _state = MusicState.Silent;
        private float _fade;
        private float _duck = 1f;

        public static MusicDirector Instance { get; private set; }

        public MusicState State => _state;

        /// <summary>True when a track is actually assigned for the current state.</summary>
        public bool HasTrackForState => ClipFor(_state) != null;

        /// <summary>True while a crossfade is still in progress.</summary>
        public bool IsCrossfading => _previous != null && _previous.isPlaying;

        /// <summary>Raised on every change, so a stinger or a later system can follow along.</summary>
        public event Action<MusicState> StateChanged;

        /// <summary>Raised when the score moves to another region's palette.</summary>
        public event Action<string> ZoneChanged;

        /// <summary>Which region's tracks are in use; empty before one is chosen.</summary>
        public string ZoneId => _palette != null ? _palette.ZoneId : string.Empty;

        private void Awake()
        {
            Instance = this;
            _current = CreateSource();
            _previous = CreateSource();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private AudioSource CreateSource()
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = 0f;
            return source;
        }

        private void Start()
        {
            _palette = FindPalette(string.Empty);
            Bind();
            TransitionTo(MusicState.Exploration);
        }

        /// <summary>
        /// Listens to the encounter, the boss and the respawner. Both the arena trigger and the
        /// boss's own signal are used, because a fight can begin without the trigger being what
        /// started it — a test, or a later scripted entrance.
        /// </summary>
        private void Bind()
        {
            // Every arena, not the first one found: with a boss per region, listening to one of
            // them left the other fight playing exploration music.
            foreach (var encounter in FindObjectsByType<BossEncounter>())
            {
                encounter.FightStarted += _ => TransitionTo(MusicState.BossIntro);
                encounter.BossDefeated += _ => TransitionTo(MusicState.BossVictory);
                encounter.FightEnded += _ =>
                {
                    // Leaving the arena alive is not victory; it is back to exploring.
                    if (_state != MusicState.BossVictory)
                    {
                        TransitionTo(MusicState.Exploration);
                    }
                };
            }

            foreach (var boss in FindObjectsByType<BossController>(FindObjectsInactive.Include))
            {
                boss.FightStarted += () => TransitionTo(MusicState.BossIntro);
                boss.PhaseChanged += OnBossPhase;
            }

            var respawner = FindAnyObjectByType<PlayerRespawner>();
            if (respawner != null)
            {
                respawner.PlayerDied += () => TransitionTo(MusicState.Death);
                // Boss music must never continue through a respawn as though nothing happened.
                respawner.PlayerRespawned += () => TransitionTo(MusicState.Exploration);
            }
        }

        /// <summary>
        /// The phase turn. Only moves the music while a fight is actually running, because the
        /// boss also reports phase one when it is reset, and that must not start boss music in an
        /// empty arena.
        /// </summary>
        private void OnBossPhase(int phase)
        {
            if (_state != MusicState.BossIntro && _state != MusicState.BossPhaseOne
                && _state != MusicState.BossPhaseTwo)
            {
                return;
            }

            TransitionTo(phase >= 2 ? MusicState.BossPhaseTwo : MusicState.BossPhaseOne);
        }

        // ---------- api ----------

        /// <summary>Moves to a state, crossfading if the track actually changes.</summary>
        public void TransitionTo(MusicState state)
        {
            if (_state == state)
            {
                return;
            }

            var wanted = ClipFor(state);
            var playing = _current != null ? _current.clip : null;
            _state = state;

            // The phase turn keeps playing rather than restarting when the track is shared, so a
            // score that layers instead of switching is not chopped in half.
            if (wanted != playing)
            {
                Crossfade(wanted);
            }

            StateChanged?.Invoke(state);
        }

        public void StopMusic()
        {
            _state = MusicState.Silent;
            Crossfade(null);
            StateChanged?.Invoke(_state);
        }

        /// <summary>Steps the score back so a louder moment can be heard over it.</summary>
        public void Duck() => _duck = Mathf.Min(_duck, 1f - _duckAmount);

        private void Crossfade(AudioClip clip)
        {
            (_current, _previous) = (_previous, _current);
            _fade = 0f;

            _current.clip = clip;
            _current.volume = 0f;
            if (clip != null)
            {
                _current.Play();
            }
            else
            {
                _current.Stop();
            }
        }

        /// <summary>
        /// Moves to another region's score. The state is untouched: exploring the forest is still
        /// exploring, it just sounds like a forest. If the new palette's track for the current
        /// state differs, it crossfades; if the two regions share a track, nothing restarts.
        /// </summary>
        public void SetZone(string zoneId)
        {
            var next = FindPalette(zoneId);
            if (next == null || next == _palette)
            {
                return;
            }

            _palette = next;
            ZoneChanged?.Invoke(zoneId);
            // A victory is over once the player has walked (or travelled) into another region;
            // carrying the fanfare across the border would be the old zone lingering.
            if (_state == MusicState.BossVictory)
            {
                _state = MusicState.Exploration;
                StateChanged?.Invoke(_state);
            }

            var wanted = ClipFor(_state);
            if (wanted != (_current != null ? _current.clip : null))
            {
                Crossfade(wanted);
            }
        }

        private MusicPalette FindPalette(string zoneId)
        {
            if (_palettes == null || _palettes.Length == 0)
            {
                return null;
            }

            foreach (var palette in _palettes)
            {
                if (palette != null && string.Equals(palette.ZoneId, zoneId,
                        StringComparison.Ordinal))
                {
                    return palette;
                }
            }

            return _palettes[0];
        }

        private AudioClip ClipFor(MusicState state)
        {
            // The main menu belongs to no region, so it is the one track kept outside the palettes.
            if (state == MusicState.MainMenu)
            {
                return _mainMenu;
            }

            var palette = _palette ?? FindPalette(string.Empty);
            if (palette == null)
            {
                return null;
            }

            switch (state)
            {
                case MusicState.Exploration: return palette.Exploration;
                case MusicState.BossIntro:
                    return palette.BossIntro != null ? palette.BossIntro : palette.BossPhaseOne;
                case MusicState.BossPhaseOne: return palette.BossPhaseOne;
                case MusicState.BossPhaseTwo: return palette.BossPhaseTwo;
                case MusicState.BossVictory: return palette.Victory;
                // Death is deliberately silent: the moment lands better without a score.
                default: return null;
            }
        }

        private void Update()
        {
            _duck = Mathf.Min(1f, _duck + Time.unscaledDeltaTime / _duckRecovery);
            _fade = Mathf.Min(1f, _fade + Time.unscaledDeltaTime / _crossfade);

            var bus = AudioManager.Instance != null
                ? AudioManager.Instance.BusVolume(AudioBus.Music)
                : 1f;
            var target = _volume * bus * _duck;

            if (_current != null)
            {
                _current.volume = target * _fade;
            }

            if (_previous == null)
            {
                return;
            }

            _previous.volume = target * (1f - _fade);
            if (_fade >= 1f && _previous.isPlaying)
            {
                _previous.Stop();
                _previous.clip = null;
            }
        }
    }
}
