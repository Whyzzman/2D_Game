using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ForgottenKingdom
{
    /// <summary>
    /// The spell gathering in the casting hand (the user's choice, 2026-09-27: a charge in the
    /// hand, no runes on the floor). It takes the spell's colour, grows and brightens towards the
    /// moment of release and beats faster as that moment nears, so a player learns the cast time
    /// by eye. The mana shield gathers the same way, softer.
    ///
    /// If a blow breaks the shaping the charge comes apart — a pop and dim sparks of its colour
    /// falling away, with a hiss (<see cref="FeedbackKind.CastFizzle"/>) — which is how the
    /// player learns the spell is lost and the mana is not.
    ///
    /// Presentation only: it reads <see cref="SpellCaster"/> and never touches the cast.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CastCharge : MonoBehaviour
    {
        /// <summary>Size at the very start of the shaping, as a share of its size at release.</summary>
        public const float StartSize = 0.35f;
        private const float PopTime = 0.14f;
        private const float SoftShare = 0.75f;

        [SerializeField] private SpellCaster _caster;
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private Light2D _light;
        [Tooltip("Scale of the charge at the moment of release.")]
        [SerializeField, Min(0.05f)] private float _size = 0.55f;
        [SerializeField, Min(0f)] private float _lightIntensity = 1.2f;
        [SerializeField, Min(0f)] private float _lightRadius = 1.6f;

        private SpellDefinition _spell;
        private bool _charging;
        private float _popAt = float.NegativeInfinity;
        private float _phase;

        /// <summary>True while a spell is gathering in the hand.</summary>
        public bool IsCharging => _charging;

        /// <summary>The charge's scale now; grows with the shaping.</summary>
        public float Size => _renderer != null ? _renderer.transform.localScale.x : 0f;

        public Color Color => _renderer != null ? _renderer.color : Color.clear;

        private void Awake()
        {
            if (_caster == null) { _caster = GetComponentInParent<SpellCaster>(); }
            if (_renderer == null) { _renderer = GetComponent<SpriteRenderer>(); }
            Hide();
        }

        private void OnEnable()
        {
            if (_caster == null)
            {
                return;
            }

            _caster.CastStarted += OnStarted;
            _caster.SpellReleased += OnReleased;
            _caster.CastInterrupted += OnInterrupted;
        }

        private void OnDisable()
        {
            if (_caster != null)
            {
                _caster.CastStarted -= OnStarted;
                _caster.SpellReleased -= OnReleased;
                _caster.CastInterrupted -= OnInterrupted;
            }

            Hide();
        }

        private void OnStarted(SpellDefinition spell)
        {
            _spell = spell;
            _charging = true;
            _phase = 0f;
            _popAt = float.NegativeInfinity;
        }

        // The spell itself takes over the moment it leaves: the charge is simply gone.
        private void OnReleased(SpellDefinition spell)
        {
            _charging = false;
            Hide();
        }

        private void OnInterrupted(SpellDefinition spell)
        {
            if (!_charging)
            {
                return;
            }

            _charging = false;
            _popAt = Time.time;
            var color = spell != null ? spell.Color : Color.white;
            CombatFeedbackService.Play(FeedbackKind.CastFizzle, transform.position, color);
        }

        private void LateUpdate()
        {
            if (_renderer == null)
            {
                return;
            }

            // Death and rest clear the cast without an interruption: nothing is left in the hand.
            if (_charging && (_caster == null || !_caster.IsCasting))
            {
                _charging = false;
                Hide();
            }

            if (_charging)
            {
                ShowCharge(_caster.CastProgress);
            }
            else if (Time.time - _popAt < PopTime)
            {
                ShowPop((Time.time - _popAt) / PopTime);
            }
            else if (_renderer.enabled)
            {
                Hide();
            }
        }

        private void ShowCharge(float progress)
        {
            var soft = _spell != null && _spell.Type == SpellType.SelfBuff ? SoftShare : 1f;
            // Beats faster and harder as release nears: the cast time, readable by eye.
            _phase += Time.deltaTime * Mathf.PI * 2f * Mathf.Lerp(3f, 11f, progress);
            var beat = Mathf.Sin(_phase) * Mathf.Lerp(0.04f, 0.12f, progress);
            var grown = Mathf.Lerp(StartSize, 1f, Mathf.Sqrt(progress));
            _renderer.enabled = true;
            _renderer.transform.localScale = Vector3.one * (_size * soft * grown * (1f + beat));
            var color = _spell != null ? _spell.Color : Color.white;
            color.a = Mathf.Lerp(0.55f, 1f, progress) * soft;
            _renderer.color = color;
            if (_light != null)
            {
                _light.enabled = true;
                _light.color = color;
                _light.pointLightOuterRadius = _lightRadius * grown;
                _light.intensity = _lightIntensity * soft * Mathf.Lerp(0.25f, 1f, progress) * (1f + beat);
            }
        }

        /// <summary>The broken charge: a quick swell as it bursts, then nothing.</summary>
        private void ShowPop(float t)
        {
            _renderer.enabled = true;
            _renderer.transform.localScale = Vector3.one * (_size * Mathf.Lerp(0.8f, 1.4f, t));
            var color = _renderer.color;
            color.a = Mathf.Lerp(0.8f, 0f, t);
            _renderer.color = color;
            if (_light != null)
            {
                _light.intensity = Mathf.Lerp(_lightIntensity * 0.8f, 0f, t);
            }
        }

        private void Hide()
        {
            if (_renderer != null)
            {
                _renderer.enabled = false;
            }

            if (_light != null)
            {
                _light.enabled = false;
            }
        }
    }
}
