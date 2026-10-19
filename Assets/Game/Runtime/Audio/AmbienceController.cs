using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The sound of one kind of place: a bed, and the details that occasionally happen over it.
    /// </summary>
    [Serializable]
    public sealed class AmbiencePalette
    {
        [SerializeField] private string _zoneId = "zone_forgotten_tomb";
        [SerializeField] private AudioId _bed = AudioId.AmbienceTombBase;
        [Tooltip("Occasional one-shots: stone settling, a chain, a drip, a gust, a branch.")]
        [SerializeField] private AudioId[] _details = Array.Empty<AudioId>();
        [Tooltip("Range between details. Wide on purpose: a regular interval reads as a machine.")]
        [SerializeField, Min(1f)] private float _minimumGap = 7f;
        [SerializeField, Min(2f)] private float _maximumGap = 21f;

        public string ZoneId => _zoneId;
        public AudioId Bed => _bed;
        public AudioId[] Details => _details;
        public float MinimumGap => _minimumGap;
        public float MaximumGap => _maximumGap;
    }

    /// <summary>
    /// The sound of the room: one very quiet bed, plus occasional details at irregular intervals.
    ///
    /// The design is mostly about restraint. A tomb should feel empty, so the bed sits far below
    /// everything else and the details are rare and randomly spaced — a drip every ten seconds
    /// exactly stops being a room and becomes a metronome. During a boss fight the details stop
    /// entirely, because combat needs the space more than atmosphere does.
    ///
    /// A second region turned the fixed tomb palette into a set of them. The forest is a different
    /// room in every respect — wind and wet leaves instead of stone and chains, and a low corruption
    /// hum where the trees are worst — so the zone chooses the palette and the crossing swaps it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AmbienceController : MonoBehaviour
    {
        [SerializeField] private AmbiencePalette[] _palettes =
        {
            new AmbiencePalette()
        };
        [Tooltip("How far to either side a detail may be placed, so the room has width.")]
        [SerializeField, Min(0f)] private float _spread = 14f;
        [SerializeField] private Transform _listener;

        private AudioSource _bedSource;
        private AmbiencePalette _palette;
        private readonly HashSet<BossEncounter> _encounters = new HashSet<BossEncounter>();
        private float _nextDetailAt;
        private bool _isSuppressed;
        // The bus duck the bed's volume already carries: the manager applies a duck only when a
        // sound starts, so the long bed follows it here, by ratio, as Suppress does.
        private float _bedDuck = 1f;

        /// <summary>True while a boss fight has taken the room's space away.</summary>
        public bool IsSuppressed => _isSuppressed;

        /// <summary>The room-bus duck the playing bed carries right now; 1 is untouched.</summary>
        public float BedDuck => _bedDuck;

        /// <summary>Which palette is playing; empty before the first one is chosen.</summary>
        public string ZoneId => _palette != null ? _palette.ZoneId : string.Empty;

        private void Start()
        {
            if (_listener == null && Camera.main != null)
            {
                _listener = Camera.main.transform;
            }

            SetZone(_palettes != null && _palettes.Length > 0 ? _palettes[0].ZoneId : string.Empty);

            HookEncounters();
            ZoneStreamer.ZonesChanged += HookEncounters;
        }

        private void OnDestroy() => ZoneStreamer.ZonesChanged -= HookEncounters;

        /// <summary>Arenas of zones loaded since the last look; each is listened to once.</summary>
        private void HookEncounters()
        {
            foreach (var encounter in ZoneObjects.New(_encounters))
            {
                encounter.FightStarted += _ => Suppress(true);
                encounter.FightEnded += _ => Suppress(false);
            }
        }

        private void OnDisable() => StopBed();

        /// <summary>
        /// Moves to another region's sound. Asking for the palette already playing does nothing, so
        /// a trigger the player walks back and forth over cannot restart the bed on every pass.
        /// </summary>
        public void SetZone(string zoneId)
        {
            var next = Find(zoneId);
            if (next == null || next == _palette)
            {
                return;
            }

            _palette = next;
            StopBed();
            StartBed();
            ScheduleDetail();
        }

        private AmbiencePalette Find(string zoneId)
        {
            if (_palettes == null || _palettes.Length == 0)
            {
                return null;
            }

            foreach (var palette in _palettes)
            {
                if (palette != null && string.Equals(palette.ZoneId, zoneId, StringComparison.Ordinal))
                {
                    return palette;
                }
            }

            return _palettes[0];
        }

        private void StartBed()
        {
            var manager = AudioManager.Instance;
            if (manager == null || _palette == null || _palette.Bed == AudioId.None)
            {
                return;
            }

            // Flat and centred: the room is everywhere, not in one place.
            _bedSource = manager.PlayLoop(_palette.Bed, transform.position);
            if (_bedSource != null)
            {
                _bedSource.spatialBlend = 0f;
                _bedDuck = manager.BusDuck(AudioBus.Ambience);
                if (_isSuppressed)
                {
                    // A bed started while a fight is running must arrive already quiet, or
                    // crossing a zone line mid-fight would shout over the telegraphs.
                    _bedSource.volume *= 0.55f;
                }
            }
        }

        private void StopBed()
        {
            AudioManager.Stop(_bedSource);
            _bedSource = null;
        }

        /// <summary>
        /// Quiets the room for a boss fight. The bed stays — silence would be a bug that sounds
        /// like a crash — but the details stop competing with telegraphs.
        /// </summary>
        public void Suppress(bool suppressed)
        {
            if (suppressed == _isSuppressed)
            {
                return;
            }

            _isSuppressed = suppressed;
            if (_bedSource != null)
            {
                _bedSource.volume *= suppressed ? 0.55f : 1f / 0.55f;
            }
        }

        private void Update()
        {
            FollowDuck();
            if (_isSuppressed || _palette == null || _palette.Details == null
                || _palette.Details.Length == 0)
            {
                return;
            }

            if (Time.unscaledTime < _nextDetailAt)
            {
                return;
            }

            PlayDetail();
            ScheduleDetail();
        }

        /// <summary>
        /// Keeps the bed under the room bus's duck while it plays, so a blow to the hero or a
        /// boss's phase change muffles the room itself and not only the next detail.
        /// </summary>
        private void FollowDuck()
        {
            var manager = AudioManager.Instance;
            if (_bedSource == null || manager == null)
            {
                return;
            }

            var duck = manager.BusDuck(AudioBus.Ambience);
            if (Mathf.Approximately(duck, _bedDuck) || _bedDuck <= 0f)
            {
                return;
            }

            _bedSource.volume *= duck / _bedDuck;
            _bedDuck = duck;
        }

        private void PlayDetail()
        {
            var details = _palette.Details;
            var id = details[UnityEngine.Random.Range(0, details.Length)];
            var origin = _listener != null ? (Vector2)_listener.position : (Vector2)transform.position;
            // Placed off to one side so the room has width without anything being localisable.
            var at = origin + new Vector2(UnityEngine.Random.Range(-_spread, _spread),
                UnityEngine.Random.Range(-2f, 5f));
            AudioManager.Play(id, at, UnityEngine.Random.Range(0.5f, 0.9f));
        }

        private void ScheduleDetail()
        {
            var min = _palette != null ? _palette.MinimumGap : 7f;
            var max = _palette != null ? _palette.MaximumGap : 21f;
            _nextDetailAt = Time.unscaledTime
                + UnityEngine.Random.Range(min, Mathf.Max(min + 1f, max));
        }
    }
}
