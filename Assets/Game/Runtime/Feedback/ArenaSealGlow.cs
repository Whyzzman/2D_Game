using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ForgottenKingdom
{
    /// <summary>
    /// A boss arena coming alive for its fight (the user's choice, 2026-09-27): the barrier seals
    /// with a flare and carries pulsing runes and their light while it stands, the arena's own
    /// lights burn brighter and colder for the fight, and the seal breaks with a flare of its own
    /// once the boss is beaten.
    ///
    /// It watches the barrier the encounter already switches, so it can never disagree with it,
    /// and it is made at runtime by <see cref="CombatFeedbackService"/> from the feedback library,
    /// so the scenes need no new objects.
    /// </summary>
    public sealed class ArenaSealGlow : MonoBehaviour
    {
        /// <summary>How much brighter the arena lights burn while the fight lasts.</summary>
        public const float FightBrightness = 1.4f;

        /// <summary>Arena lights this close to the boss belong to its arena.</summary>
        private const float ArenaReach = 20f;

        private const float RuneFps = 10f;
        private const float BlendTime = 0.8f;

        private BossEncounter _encounter;
        private AnimationClipDefinition _runes;
        private SpriteRenderer _renderer;
        private Light2D _light;
        private readonly List<(Light2D light, float intensity)> _arenaLights = new List<(Light2D, float)>();
        private bool _wasSealed;
        private float _elapsed;
        private float _fight;

        public bool IsGlowing => _renderer != null && _renderer.enabled;

        /// <summary>0 at rest, 1 at full fight brightness.</summary>
        public float FightBlend => _fight;

        public static ArenaSealGlow Attach(BossEncounter encounter, FeedbackLibrary library,
            Material material, Light2D lightTemplate)
        {
            var barrier = encounter != null ? encounter.Barrier : null;
            if (barrier == null)
            {
                return null;
            }

            var host = new GameObject("ArenaSealGlow");
            host.transform.SetParent(encounter.transform, false);
            var glow = host.AddComponent<ArenaSealGlow>();
            glow._encounter = encounter;
            var profile = library != null ? library.Profile(FeedbackKind.ArenaRunes) : null;
            glow._runes = profile != null ? profile.Burst : null;
            glow._renderer = host.AddComponent<SpriteRenderer>();
            if (material != null) { glow._renderer.sharedMaterial = material; }
            glow._renderer.sortingLayerName = "VFXFront";
            glow._renderer.enabled = false;
            if (lightTemplate != null)
            {
                glow._light = Instantiate(lightTemplate, host.transform);
                glow._light.enabled = false;
                glow._light.color = profile != null ? profile.LightColor : Color.white;
            }

            glow.FitTo(barrier);
            glow.FindArenaLights();
            glow._wasSealed = barrier.activeInHierarchy;
            return glow;
        }

        /// <summary>Lays the rune column over the barrier, stretched to its height.</summary>
        private void FitTo(GameObject barrier)
        {
            var bounds = new Bounds(barrier.transform.position, Vector3.one);
            var found = false;
            foreach (var part in barrier.GetComponentsInChildren<Renderer>(true))
            {
                if (!found) { bounds = part.bounds; found = true; }
                else { bounds.Encapsulate(part.bounds); }
            }

            if (!found)
            {
                var collider = barrier.GetComponent<Collider2D>();
                if (collider != null) { bounds = collider.bounds; }
            }

            transform.position = bounds.center;
            // The rune art is one unit wide and seven tall, the Tomb barrier's own size.
            var height = Mathf.Max(1f, bounds.size.y);
            transform.localScale = new Vector3(height / 7f, height / 7f, 1f);
            if (_light != null)
            {
                _light.transform.localScale = Vector3.one;
                _light.pointLightOuterRadius = height * 0.6f;
                _light.pointLightInnerRadius = 0f;
            }
        }

        private void FindArenaLights()
        {
            var boss = _encounter.Boss;
            if (boss == null)
            {
                return;
            }

            foreach (var light in FindObjectsByType<Light2D>(FindObjectsInactive.Include))
            {
                if (light.name == "ArenaLight"
                    && Mathf.Abs(light.transform.position.x - boss.transform.position.x) < ArenaReach)
                {
                    _arenaLights.Add((light, light.intensity));
                }
            }
        }

        private void LateUpdate()
        {
            var barrier = _encounter != null ? _encounter.Barrier : null;
            var isSealed = barrier != null && barrier.activeInHierarchy;
            if (isSealed != _wasSealed)
            {
                OnSealChanged(isSealed, barrier);
            }

            _wasSealed = isSealed;
            TickRunes(isSealed);
            TickArenaLights(_encounter != null && _encounter.IsFightActive);
        }

        private void OnSealChanged(bool isSealed, GameObject barrier)
        {
            if (isSealed)
            {
                _elapsed = 0f;
                CombatFeedbackService.Play(FeedbackKind.ArenaSeal, transform.position);
            }
            else if (_encounter.IsBossDefeated)
            {
                // Only a won fight breaks the seal; after a death it simply goes, under the fade.
                CombatFeedbackService.Play(FeedbackKind.ArenaOpen, transform.position);
            }
        }

        private void TickRunes(bool isSealed)
        {
            var show = isSealed && _runes != null && _runes.HasFrames;
            _renderer.enabled = show;
            if (_light != null) { _light.enabled = show; }
            if (!show)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            var count = _runes.Frames.Count;
            _renderer.sprite = _runes.Frames[(int)(_elapsed * RuneFps) % count];
            if (_light != null)
            {
                // A slow breath, so the seal reads as alive rather than as a lamp.
                _light.intensity = 0.9f + 0.35f * Mathf.Sin(_elapsed * 2.4f);
            }
        }

        private void TickArenaLights(bool fighting)
        {
            _fight = Mathf.MoveTowards(_fight, fighting ? 1f : 0f, Time.deltaTime / BlendTime);
            var pulse = 1f + 0.08f * Mathf.Sin(Time.time * 1.7f) * _fight;
            var gain = Mathf.Lerp(1f, FightBrightness, _fight) * pulse;
            foreach (var (light, intensity) in _arenaLights)
            {
                if (light != null) { light.intensity = intensity * gain; }
            }
        }
    }
}
