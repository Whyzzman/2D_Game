using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The altar's three looks: cold, lit, and lit-and-current. It reads the checkpoint's own
    /// state and changes nothing about it — interaction, saving and respawning stay exactly where
    /// they were, and the collider is never touched by art.
    ///
    /// The tomb is cold and blue everywhere else, so this is the only warm light in the level.
    /// That contrast is the whole message: here you are safe.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Checkpoint))]
    public sealed class CheckpointVisual : MonoBehaviour
    {
        [SerializeField] private Checkpoint _checkpoint;
        [SerializeField] private SpriteRenderer _flame;
        [SerializeField] private SpriteRenderer _glow;
        [SerializeField] private Sprite _coldFlame;
        [SerializeField] private Sprite _litFlame;
        [Tooltip("Glow size when lit, and again when this is the altar you will return to.")]
        [SerializeField, Min(0f)] private float _litGlow = 2.4f;
        [SerializeField, Min(0f)] private float _currentGlow = 3.4f;
        [SerializeField, Min(0f)] private float _flickerSpeed = 3.5f;
        [SerializeField, Min(0f)] private float _flickerAmount = 0.12f;
        [Tooltip("The altar's real light: a faint cold glint while dead, warm gold once lit.")]
        [SerializeField] private UnityEngine.Rendering.Universal.Light2D _light;
        [SerializeField] private Color _deadLightColor = new Color(0.45f, 0.55f, 0.75f);
        [SerializeField] private Color _litLightColor = new Color(1f, 0.72f, 0.36f);
        [SerializeField, Min(0f)] private float _deadIntensity = 0.25f;
        [SerializeField, Min(0f)] private float _litIntensity = 1.35f;
        [SerializeField, Min(0f)] private float _currentIntensity = 1.75f;
        [Tooltip("Embers rising from the flame; only while it burns.")]
        [SerializeField] private ParticleSystem _embers;
        private bool _wasActivated;
        private bool _hasApplied;

        private void Awake()
        {
            if (_checkpoint == null) { _checkpoint = GetComponent<Checkpoint>(); }
        }

        // Applied before the first rendered frame, so a checkpoint restored from a save never
        // shows one frame of cold stone before catching up.
        private void Start() => Apply(true);

        private void LateUpdate() => Apply(false);

        private void Apply(bool immediate)
        {
            if (_checkpoint == null)
            {
                return;
            }

            var isActivated = _checkpoint.IsActivated;
            var isCurrent = _checkpoint.IsCurrent;
            if (_flame != null)
            {
                var wanted = isActivated ? _litFlame : _coldFlame;
                if (wanted != null && _flame.sprite != wanted)
                {
                    _flame.sprite = wanted;
                }
            }

            if (_glow != null)
            {
                var size = !isActivated ? 0f : isCurrent ? _currentGlow : _litGlow;
                if (size <= 0f)
                {
                    _glow.enabled = false;
                }
                else
                {
                    _glow.enabled = true;
                    // A slow flicker, not a pulse: a flame that throbs reads as a game object.
                    var flicker = immediate
                        ? 1f
                        : 1f + Mathf.Sin(Time.time * _flickerSpeed) * _flickerAmount;
                    _glow.transform.localScale = Vector3.one * size * flicker;
                }
            }

            if (_light != null)
            {
                var flicker = immediate ? 1f
                    : 1f + (Mathf.PerlinNoise(Time.time * 2.3f, 0.37f) - 0.5f) * 0.28f;
                _light.color = isActivated ? _litLightColor : _deadLightColor;
                _light.intensity = (!isActivated ? _deadIntensity
                    : isCurrent ? _currentIntensity : _litIntensity) * (isActivated ? flicker : 1f);
            }

            if (_embers != null && isActivated != _embers.isPlaying)
            {
                if (isActivated)
                {
                    _embers.Play();
                }
                else
                {
                    _embers.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }

            _wasActivated = isActivated;
            _hasApplied = true;
        }

        /// <summary>True once the visual has matched the checkpoint at least once.</summary>
        public bool HasApplied => _hasApplied;

        public bool ShowsLitFlame => _wasActivated;
    }
}
