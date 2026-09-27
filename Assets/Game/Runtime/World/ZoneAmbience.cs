using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The air of each zone (the user's choice, 2026-09-27: restrained): slow dust and the odd
    /// crumb of grit in the Tomb, a few dry leaves and drifting spores in the forest. The emitters
    /// ride with the camera and simulate in the world, so the motes stay where they are while the
    /// view moves through them, and only the current zone's air is breathed out.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneAmbience : MonoBehaviour
    {
        [Serializable]
        public struct Entry
        {
            public string ZoneId;
            public ParticleSystem[] Systems;
        }

        [SerializeField] private Entry[] _zones = Array.Empty<Entry>();
        [SerializeField] private Transform _camera;

        private WorldState _world;
        private string _active;

        public string ActiveZone => _active;

        public bool IsBreathing(string zoneId)
        {
            foreach (var entry in _zones)
            {
                if (entry.ZoneId != zoneId) { continue; }
                foreach (var system in entry.Systems)
                {
                    if (system != null && system.isEmitting) { return true; }
                }
            }

            return false;
        }

        private void Awake()
        {
            if (_camera == null && Camera.main != null) { _camera = Camera.main.transform; }
        }

        private void OnEnable()
        {
            _world = WorldState.Current != null ? WorldState.Current : FindAnyObjectByType<WorldState>();
            if (_world != null) { _world.ZoneEntered += OnZoneEntered; }
        }

        private void OnDisable()
        {
            if (_world != null) { _world.ZoneEntered -= OnZoneEntered; }
        }

        private void Start()
        {
            Follow();
            var zone = _world != null ? _world.CurrentZone : null;
            Breathe(zone != null ? zone.ZoneId : (_zones.Length > 0 ? _zones[0].ZoneId : string.Empty), true);
        }

        private void LateUpdate() => Follow();

        private void Follow()
        {
            if (_camera != null)
            {
                transform.position = new Vector3(_camera.position.x, _camera.position.y, 0f);
            }
        }

        private void OnZoneEntered(ZoneDefinition zone)
        {
            if (zone != null) { Breathe(zone.ZoneId, false); }
        }

        /// <summary>
        /// Starts the zone's air and stops the rest; stopped motes finish their lives rather than
        /// vanishing, so walking from the Tomb into the forest is a change of air, not a cut.
        /// Only the first zone is prewarmed, on load: a prewarm is seconds of simulation in one
        /// frame, and on a zone change mid-play it showed as a hitch in the view.
        /// </summary>
        private void Breathe(string zoneId, bool onLoad)
        {
            if (zoneId == _active)
            {
                return;
            }

            _active = zoneId;
            foreach (var entry in _zones)
            {
                var on = entry.ZoneId == zoneId;
                foreach (var system in entry.Systems)
                {
                    if (system == null) { continue; }
                    if (on)
                    {
                        if (!onLoad)
                        {
                            var main = system.main;
                            main.prewarm = false;
                        }

                        system.Play(true);
                    }
                    else
                    {
                        system.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                    }
                }
            }
        }
    }
}
