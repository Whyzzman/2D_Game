using System;
using System.Collections.Generic;
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
        [Tooltip("Drums and bass on the exploration track's grid, faded in while the player fights.")]
        [SerializeField] private AudioClip _combatLayer;
        [Tooltip("The boss track's heavier drums, faded in for phase two.")]
        [SerializeField] private AudioClip _bossLayer;

        public string ZoneId => _zoneId;
        public AudioClip Exploration => _exploration;
        public AudioClip BossIntro => _bossIntro;
        public AudioClip BossPhaseOne => _bossPhaseOne;
        public AudioClip BossPhaseTwo => _bossPhaseTwo;
        public AudioClip Victory => _victory;
        public AudioClip CombatLayer => _combatLayer;
        public AudioClip BossLayer => _bossLayer;
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
    ///
    /// Each track may carry a layer of the same length and tempo (the user's choice, 2026-09-28):
    /// drums under exploration while the player is fighting, heavier drums for a boss's second
    /// phase. The layer always plays with its track, started on the same audio clock tick, and
    /// only its volume moves — so fading it in never restarts or shifts anything.
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
        [Tooltip("Where the director starts: exploration in the level, the menu theme in the menu.")]
        [SerializeField] private MusicState _startState = MusicState.Exploration;
        [SerializeField, Min(0.05f)] private float _layerFadeIn = 1.2f;
        [SerializeField, Min(0.05f)] private float _layerFadeOut = 3f;

        /// <summary>Lead time for a scheduled start, so a track and its layer begin on one tick.</summary>
        private const double ScheduleLead = 0.06;

        private AudioSource _current;
        private AudioSource _previous;
        private AudioSource _currentLayer;
        private AudioSource _previousLayer;
        private PlayerCombatStatus _combat;
        private float _layerMix;
        private float _previousLayerMix;
        private MusicPalette _palette;
        private MusicState _state = MusicState.Silent;
        private float _fade;
        private float _duck = 1f;
        private float _duckHeldUntil;

        public static MusicDirector Instance { get; private set; }

        public MusicState State => _state;

        /// <summary>How far the score is stepped back right now: 1 is untouched.</summary>
        public float DuckLevel => _duck;

        /// <summary>True when a track is actually assigned for the current state.</summary>
        public bool HasTrackForState => ClipFor(_state) != null;

        /// <summary>How far the current track's layer is faded in, 0 to 1.</summary>
        public float LayerMix => _layerMix;

        /// <summary>The layer playing under the current track, if it has one.</summary>
        public AudioClip CurrentLayer => _currentLayer != null ? _currentLayer.clip : null;

        public AudioClip CurrentTrack => _current != null ? _current.clip : null;

        /// <summary>True while a crossfade is still in progress.</summary>
        public bool IsCrossfading => _previous != null && _previous.isPlaying;

        /// <summary>Raised on every change, so a stinger or a later system can follow along.</summary>
        public event Action<MusicState> StateChanged;

        /// <summary>Raised when the score moves to another region's palette.</summary>
        public event Action<string> ZoneChanged;

        /// <summary>Which region's tracks are in use; empty before one is chosen.</summary>
        public string ZoneId => _palette != null ? _palette.ZoneId : string.Empty;

        private readonly HashSet<BossEncounter> _encounters = new HashSet<BossEncounter>();
        private readonly HashSet<BossController> _bosses = new HashSet<BossController>();

        private void Awake()
        {
            Instance = this;
            _current = CreateSource();
            _previous = CreateSource();
            _currentLayer = CreateSource();
            _previousLayer = CreateSource();
        }

        private void OnDestroy()
        {
            ZoneStreamer.ZonesChanged -= BindLevel;
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
            TransitionTo(_startState);
        }

        /// <summary>
        /// Listens to the encounter, the boss and the respawner. Both the arena trigger and the
        /// boss's own signal are used, because a fight can begin without the trigger being what
        /// started it — a test, or a later scripted entrance.
        /// </summary>
        private void Bind()
        {
            BindLevel();
            ZoneStreamer.ZonesChanged += BindLevel;
            _combat = FindAnyObjectByType<PlayerCombatStatus>();
            var respawner = FindAnyObjectByType<PlayerRespawner>();
            if (respawner != null)
            {
                respawner.PlayerDied += () => TransitionTo(MusicState.Death);
                // Boss music must never continue through a respawn as though nothing happened.
                respawner.PlayerRespawned += () => TransitionTo(MusicState.Exploration);
            }
        }

        /// <summary>The arenas and bosses of zones loaded since the last look, each hooked once.</summary>
        private void BindLevel()
        {
            // Every arena, not the first one found: with a boss per region, listening to one of
            // them left the other fight playing exploration music.
            foreach (var encounter in ZoneObjects.New(_encounters))
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

            foreach (var boss in ZoneObjects.New(_bosses, includeInactive: true))
            {
                boss.FightStarted += () => TransitionTo(MusicState.BossIntro);
                boss.PhaseChanged += OnBossPhase;
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
            else if (LayerFor(state) != CurrentLayer)
            {
                StartLayerInStep(LayerFor(state));
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
        public void Duck() => Duck(_duckAmount, 0f);

        /// <summary>
        /// Steps the score back by <paramref name="amount"/> and holds it there for
        /// <paramref name="hold"/> real seconds before it starts to return.
        /// </summary>
        public void Duck(float amount, float hold)
        {
            _duck = Mathf.Min(_duck, 1f - Mathf.Clamp01(amount));
            _duckHeldUntil = Mathf.Max(_duckHeldUntil, Time.unscaledTime + Mathf.Max(0f, hold));
        }

        private void Crossfade(AudioClip clip)
        {
            (_current, _previous) = (_previous, _current);
            (_currentLayer, _previousLayer) = (_previousLayer, _currentLayer);
            _previousLayerMix = _layerMix;
            _layerMix = 0f;
            _fade = 0f;

            _current.clip = clip;
            _current.volume = 0f;
            _currentLayer.clip = clip != null ? LayerFor(_state) : null;
            _currentLayer.volume = 0f;
            if (clip == null)
            {
                _current.Stop();
                _currentLayer.Stop();
                return;
            }

            // The track and its layer begin on the same tick of the audio clock, or they drift apart.
            var start = AudioSettings.dspTime + ScheduleLead;
            _current.PlayScheduled(start);
            if (_currentLayer.clip != null)
            {
                _currentLayer.PlayScheduled(start);
            }
            else
            {
                _currentLayer.Stop();
            }
        }

        /// <summary>A new layer under a track that keeps playing: joined at the track's own position.</summary>
        private void StartLayerInStep(AudioClip layer)
        {
            _currentLayer.Stop();
            _currentLayer.clip = layer;
            _layerMix = 0f;
            if (layer == null || !_current.isPlaying)
            {
                return;
            }

            _currentLayer.timeSamples = Mathf.Min(_current.timeSamples, layer.samples - 1);
            _currentLayer.Play();
        }

        private AudioClip LayerFor(MusicState state)
        {
            var palette = _palette ?? FindPalette(string.Empty);
            if (palette == null)
            {
                return null;
            }

            switch (state)
            {
                case MusicState.Exploration: return palette.CombatLayer;
                case MusicState.BossIntro:
                case MusicState.BossPhaseOne:
                case MusicState.BossPhaseTwo:
                    return palette.BossLayer;
                default: return null;
            }
        }

        /// <summary>Whether the layer should be heard now: a fight while exploring, or phase two.</summary>
        private bool WantsLayer()
        {
            switch (_state)
            {
                case MusicState.Exploration: return _combat != null && _combat.IsInCombat;
                case MusicState.BossPhaseTwo: return true;
                default: return false;
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
            else if (LayerFor(_state) != CurrentLayer)
            {
                StartLayerInStep(LayerFor(_state));
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
            if (Time.unscaledTime >= _duckHeldUntil)
            {
                _duck = Mathf.Min(1f, _duck + Time.unscaledDeltaTime / _duckRecovery);
            }
            _fade = Mathf.Min(1f, _fade + Time.unscaledDeltaTime / _crossfade);
            var layerTarget = WantsLayer() ? 1f : 0f;
            _layerMix = Mathf.MoveTowards(_layerMix, layerTarget,
                Time.unscaledDeltaTime / (layerTarget > _layerMix ? _layerFadeIn : _layerFadeOut));

            var target = _volume * BusVolume() * _duck;
            if (_current != null)
            {
                _current.volume = target * _fade;
                _currentLayer.volume = target * _fade * _layerMix;
            }

            if (_previous == null)
            {
                return;
            }

            _previous.volume = target * (1f - _fade);
            _previousLayer.volume = target * (1f - _fade) * _previousLayerMix;
            if (_fade >= 1f && _previous.isPlaying)
            {
                _previous.Stop();
                _previous.clip = null;
                _previousLayer.Stop();
                _previousLayer.clip = null;
            }
        }

        /// <summary>
        /// The music slider. The menu has no <see cref="AudioManager"/>, so there the settings are
        /// read directly — the slider must be obeyed on the title screen too.
        /// </summary>
        private static float BusVolume()
        {
            if (AudioManager.Instance != null)
            {
                return AudioManager.Instance.BusVolume(AudioBus.Music);
            }

            var settings = GameSettings.Current;
            return settings != null ? settings.musicVolume * settings.masterVolume : 1f;
        }
    }
}
