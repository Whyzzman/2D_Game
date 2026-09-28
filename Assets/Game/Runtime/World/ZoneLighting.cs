using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ForgottenKingdom
{
    /// <summary>One region's ambient light.</summary>
    [Serializable]
    public sealed class ZoneLight
    {
        [SerializeField] private string _zoneId = string.Empty;
        [SerializeField] private Color _color = new Color(0.6f, 0.66f, 0.8f);
        [SerializeField, Min(0f)] private float _intensity = 0.8f;

        public string ZoneId => _zoneId;
        public Color Color => _color;
        public float Intensity => _intensity;
    }

    /// <summary>
    /// The global light: cold blue-grey in the Tomb, moonlit silver-green in the forest. It eases
    /// between them as the player crosses a border rather than switching, and during a boss fight
    /// the room darkens a little so the arena's own light and the telegraphs carry the scene.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light2D))]
    public sealed class ZoneLighting : MonoBehaviour
    {
        [SerializeField] private ZoneLight[] _zones = Array.Empty<ZoneLight>();
        [SerializeField, Min(0.05f)] private float _blendTime = 1.6f;
        [SerializeField, Range(0.5f, 1f)] private float _bossFightDimming = 0.82f;

        private Light2D _light;
        private Color _fromColor;
        private Color _toColor;
        private float _fromIntensity;
        private float _toIntensity;
        private float _blend = 1f;
        private float _dimming = 1f;
        private BossEncounter[] _encounters = Array.Empty<BossEncounter>();
        private WorldState _world;

        private void Awake()
        {
            _light = GetComponent<Light2D>();
            _toColor = _fromColor = _light.color;
            _toIntensity = _fromIntensity = _light.intensity;
            if (_zones.Length > 0)
            {
                Apply(_zones[0], immediate: true);
            }
        }

        private void OnEnable()
        {
            _world = WorldState.Current != null ? WorldState.Current : FindAnyObjectByType<WorldState>();
            if (_world != null)
            {
                _world.ZoneEntered += OnZoneEntered;
            }

            ZoneStreamer.ZonesChanged += OnZonesChanged;
            OnZonesChanged();
        }

        private void OnDisable()
        {
            if (_world != null)
            {
                _world.ZoneEntered -= OnZoneEntered;
            }

            ZoneStreamer.ZonesChanged -= OnZonesChanged;
            HookEncounters(false);
        }

        /// <summary>The arenas come and go with their zones; listen to the ones there now.</summary>
        private void OnZonesChanged()
        {
            HookEncounters(false);
            _encounters = ZoneObjects.All<BossEncounter>(includeInactive: true);
            HookEncounters(true);
        }

        private void HookEncounters(bool on)
        {
            foreach (var encounter in _encounters)
            {
                if (encounter == null)
                {
                    continue;
                }

                encounter.FightStarted -= OnFightStarted;
                encounter.FightEnded -= OnFightEnded;
                if (on)
                {
                    encounter.FightStarted += OnFightStarted;
                    encounter.FightEnded += OnFightEnded;
                }
            }
        }

        private void Start()
        {
            if (_world != null && _world.CurrentZone != null)
            {
                OnZoneEntered(_world.CurrentZone);
                _blend = 1f;
            }
        }

        private void Update()
        {
            if (_blend < 1f)
            {
                _blend = Mathf.Min(1f, _blend + Time.unscaledDeltaTime / _blendTime);
            }

            var t = Mathf.SmoothStep(0f, 1f, _blend);
            _light.color = Color.Lerp(_fromColor, _toColor, t);
            _light.intensity = Mathf.Lerp(_fromIntensity, _toIntensity, t) * _dimming;
        }

        private void OnZoneEntered(ZoneDefinition zone)
        {
            foreach (var entry in _zones)
            {
                if (entry != null && zone != null && entry.ZoneId == zone.ZoneId)
                {
                    Apply(entry, immediate: false);
                    return;
                }
            }
        }

        private void Apply(ZoneLight entry, bool immediate)
        {
            _fromColor = immediate ? entry.Color : _light.color;
            _fromIntensity = immediate ? entry.Intensity : _light.intensity / Mathf.Max(0.01f, _dimming);
            _toColor = entry.Color;
            _toIntensity = entry.Intensity;
            _blend = immediate ? 1f : 0f;
        }

        private void OnFightStarted(BossController boss) => _dimming = _bossFightDimming;

        private void OnFightEnded(BossController boss) => _dimming = 1f;
    }
}
