using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The one place a sound is played.
    ///
    /// Gameplay names a moment — <c>AudioManager.Play(AudioId.Parry, position)</c> — and everything
    /// else happens here: which variant, how loud, where in the stereo field, whether there is room
    /// for it at all. No combat script ever touches an AudioSource, which is what stops mixing
    /// decisions from scattering into gameplay.
    ///
    /// Three rules keep a fight from turning into noise. Every event has a voice limit, so eight
    /// enemies cannot produce eight of the same sound. Every event has a priority, so when the
    /// voices run out it is the ambient creak that loses and never the parry. And loud moments
    /// duck the quiet buses rather than shouting over them.
    ///
    /// Sources are pooled: a fight makes hundreds of one-shots, and creating an object per sound
    /// is the cheapest way to turn a smooth fight into a stuttering one.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AudioManager : MonoBehaviour
    {
        private sealed class Voice
        {
            public AudioSource Source;
            public AudioId Id;
            public int Priority;
            public float StartedAt;
            public bool IsLoop;
        }

        [SerializeField] private AudioLibrary _library;
        [Tooltip("Hard ceiling on simultaneous sounds. Past this the mix stops meaning anything.")]
        [SerializeField, Min(8)] private int _maxVoices = 48;
        [Tooltip("How quickly a ducked bus comes back.")]
        [SerializeField, Min(0.05f)] private float _duckRecovery = 0.9f;

        private readonly List<Voice> _voices = new List<Voice>();
        private readonly Queue<Voice> _idle = new Queue<Voice>();
        private readonly Dictionary<AudioId, float> _lastPlayedAt = new Dictionary<AudioId, float>();
        private readonly Dictionary<AudioId, int> _lastClipIndex = new Dictionary<AudioId, int>();
        private readonly Dictionary<AudioId, int> _beforeClipIndex = new Dictionary<AudioId, int>();
        private readonly float[] _busVolume = new float[5];
        private readonly float[] _busDuck = new float[5];
        private readonly float[] _duckHeldUntil = new float[5];
        private Transform _root;

        public static AudioManager Instance { get; private set; }

        public AudioLibrary Library => _library;

        /// <summary>
        /// How many looping sounds are running. Loops are what survive a death or a reset when
        /// something goes wrong, so they are counted separately from everything else.
        /// </summary>
        public int LoopingVoices
        {
            get
            {
                var count = 0;
                foreach (var voice in _voices)
                {
                    if (voice.IsLoop && voice.Source != null && voice.Source.isPlaying)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>How many sounds are playing right now; used by tests and the debug readout.</summary>
        public int ActiveVoices
        {
            get
            {
                var count = 0;
                foreach (var voice in _voices)
                {
                    if (voice.Source != null && voice.Source.isPlaying)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>What is playing, by id; for test messages and the debug readout.</summary>
        public string DescribeActiveVoices()
        {
            var names = new System.Collections.Generic.List<string>();
            foreach (var voice in _voices)
            {
                if (voice.Source != null && voice.Source.isPlaying)
                {
                    names.Add(voice.Id.ToString());
                }
            }

            return string.Join(", ", names);
        }

        private void Awake()
        {
            Instance = this;
            _root = new GameObject("Voices").transform;
            _root.SetParent(transform, false);
            for (var i = 0; i < _busVolume.Length; i++)
            {
                _busVolume[i] = 1f;
                _busDuck[i] = 1f;
            }

            ApplySettings();
            GameSettings.Changed += ApplySettings;
        }

        private void OnDestroy()
        {
            GameSettings.Changed -= ApplySettings;
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Takes the player's settings and turns them into bus levels. Interface and room have no
        /// sliders of their own on purpose: the options screen stays short, and silencing combat
        /// must not also silence the room, which is what one shared slider would do.
        /// </summary>
        private void ApplySettings()
        {
            var data = GameSettings.Current;
            _busVolume[(int)AudioBus.Master] = data.masterVolume;
            _busVolume[(int)AudioBus.Music] = data.musicVolume;
            _busVolume[(int)AudioBus.Sfx] = data.effectsVolume;
            _busVolume[(int)AudioBus.Ui] = 0.9f;
            _busVolume[(int)AudioBus.Ambience] = 0.75f;
        }

        public float BusVolume(AudioBus bus) =>
            bus == AudioBus.Master
                ? _busVolume[(int)AudioBus.Master]
                : _busVolume[(int)bus] * _busVolume[(int)AudioBus.Master] * _busDuck[(int)bus];

        // ---------- playing ----------

        public static void Play(AudioId id, Vector2 position, float volumeScale = 1f) =>
            Instance?.PlayInternal(id, position, volumeScale, false);

        /// <summary>
        /// The same, with the pitch scaled on top of the event's own variation: a hit climbing in a
        /// streak (<see cref="HitStreak"/>). 1 is the sound as authored.
        /// </summary>
        public static void Play(AudioId id, Vector2 position, float volumeScale, float pitchScale) =>
            Instance?.PlayInternal(id, position, volumeScale, false, pitchScale);

        /// <summary>Plays without a position; for interface and anything that is not in the world.</summary>
        public static void Play2D(AudioId id, float volumeScale = 1f) =>
            Instance?.PlayInternal(id, Vector2.zero, volumeScale, true);

        private AudioSource PlayInternal(AudioId id, Vector2 position, float volumeScale,
            bool flat, float pitchScale = 1f)
        {
            if (_library == null)
            {
                return null;
            }

            var settings = _library.Event(id);
            if (settings == null || !settings.HasClips)
            {
                return null;
            }

            // A held key or a crowd of identical hits must not become a machine gun.
            if (settings.MinimumInterval > 0f
                && _lastPlayedAt.TryGetValue(id, out var last)
                && Time.unscaledTime - last < settings.MinimumInterval)
            {
                return null;
            }

            if (CountOf(id) >= settings.MaxVoices)
            {
                return null;
            }

            var voice = TakeVoice(settings.Priority);
            if (voice == null)
            {
                return null;
            }

            var index = _lastClipIndex.TryGetValue(id, out var previous) ? previous : -1;
            var before = _beforeClipIndex.TryGetValue(id, out var earlier) ? earlier : -1;
            var clip = settings.PickClip(ref index, ref before);
            _lastClipIndex[id] = index;
            _beforeClipIndex[id] = before;
            _lastPlayedAt[id] = Time.unscaledTime;

            voice.Id = id;
            voice.Priority = settings.Priority;
            voice.StartedAt = Time.unscaledTime;
            voice.IsLoop = settings.Loop;

            var source = voice.Source;
            source.transform.position = position;
            source.clip = clip;
            source.loop = settings.Loop;
            source.volume = settings.PickVolume() * Mathf.Max(0f, volumeScale)
                * BusVolume(settings.Bus);
            source.pitch = settings.PickPitch() * Mathf.Clamp(pitchScale, 0.5f, 2f);
            source.spatialBlend = flat ? 0f : settings.SpatialBlend;
            source.maxDistance = settings.MaxDistance;
            source.rolloffMode = AudioRolloffMode.Linear;
            // Unscaled, so a hit-stop does not stop the sound of the hit that caused it.
            source.ignoreListenerPause = settings.Bus == AudioBus.Ui;
            source.Play();
            return source;
        }

        private int CountOf(AudioId id)
        {
            var count = 0;
            foreach (var voice in _voices)
            {
                if (voice.Id == id && voice.Source != null && voice.Source.isPlaying)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Finds a free voice, or takes one from something less important. When the mix is full,
        /// the quiet thing loses — never the telegraph the player needs to hear.
        /// </summary>
        private Voice TakeVoice(int priority)
        {
            while (_idle.Count > 0)
            {
                var pooled = _idle.Dequeue();
                if (pooled?.Source != null && !pooled.Source.isPlaying)
                {
                    return pooled;
                }
            }

            foreach (var voice in _voices)
            {
                if (voice.Source != null && !voice.Source.isPlaying)
                {
                    return voice;
                }
            }

            if (_voices.Count < _maxVoices)
            {
                return Create();
            }

            Voice weakest = null;
            foreach (var voice in _voices)
            {
                if (voice.IsLoop || voice.Priority >= priority)
                {
                    continue;
                }

                if (weakest == null || voice.Priority < weakest.Priority
                    || (voice.Priority == weakest.Priority && voice.StartedAt < weakest.StartedAt))
                {
                    weakest = voice;
                }
            }

            if (weakest?.Source != null)
            {
                weakest.Source.Stop();
                return weakest;
            }

            // Everything playing matters more than this. Dropping it is the correct answer.
            return null;
        }

        private Voice Create()
        {
            var host = new GameObject("Voice");
            host.transform.SetParent(_root, false);
            var source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.dopplerLevel = 0f;
            source.minDistance = 4f;
            var voice = new Voice { Source = source };
            _voices.Add(voice);
            return voice;
        }

        // ---------- loops ----------

        /// <summary>
        /// Starts a looping sound and hands back its source so the caller can stop it. Loops are
        /// the thing most likely to be left running after a death or a reset, so ownership is
        /// explicit rather than fire-and-forget.
        /// </summary>
        public AudioSource PlayLoop(AudioId id, Vector2 position, float volumeScale = 1f) =>
            PlayInternal(id, position, volumeScale, false);

        public static void Stop(AudioSource source)
        {
            if (source != null)
            {
                source.Stop();
                source.clip = null;
            }
        }

        // ---------- ducking ----------

        /// <summary>
        /// Pushes a bus down for a moment so something more important can be heard. Used by the
        /// phase change, the boss dying and the player dying. It never mutes: a bus at zero is a
        /// bug that sounds like a crash.
        /// </summary>
        public void Duck(AudioBus bus, float amount) => Duck(bus, amount, 0f);

        /// <summary>
        /// The same, held down for <paramref name="hold"/> real seconds before it starts to come
        /// back — a blow to the hero leaves the room muffled for a moment, not for a frame.
        /// </summary>
        public void Duck(AudioBus bus, float amount, float hold)
        {
            var index = (int)bus;
            _busDuck[index] = Mathf.Clamp(Mathf.Min(_busDuck[index], 1f - amount), 0.15f, 1f);
            _duckHeldUntil[index] = Mathf.Max(_duckHeldUntil[index],
                Time.unscaledTime + Mathf.Max(0f, hold));
        }

        /// <summary>How far a bus is ducked right now: 1 is untouched.</summary>
        public float BusDuck(AudioBus bus) => _busDuck[(int)bus];

        private void Update()
        {
            for (var i = 0; i < _busDuck.Length; i++)
            {
                if (_busDuck[i] < 1f && Time.unscaledTime >= _duckHeldUntil[i])
                {
                    _busDuck[i] = Mathf.Min(1f,
                        _busDuck[i] + Time.unscaledDeltaTime / _duckRecovery);
                }
            }
        }

        // ---------- cleanup ----------

        /// <summary>
        /// Stops everything. Called on death, respawn and world reset, because a status loop or a
        /// boss layer surviving a respawn is the classic audio bug and the hardest to notice.
        /// </summary>
        public void StopAll()
        {
            foreach (var voice in _voices)
            {
                if (voice.Source == null)
                {
                    continue;
                }

                voice.Source.Stop();
                voice.Source.clip = null;
                voice.Id = AudioId.None;
                voice.IsLoop = false;
            }

            for (var i = 0; i < _busDuck.Length; i++)
            {
                _busDuck[i] = 1f;
                _duckHeldUntil[i] = 0f;
            }
        }

        /// <summary>Stops every loop but lets one-shots finish naturally.</summary>
        public void StopLoops()
        {
            foreach (var voice in _voices)
            {
                if (voice.IsLoop && voice.Source != null)
                {
                    voice.Source.Stop();
                    voice.IsLoop = false;
                }
            }
        }
    }
}
