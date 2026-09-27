using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ForgottenKingdom
{
    /// <summary>
    /// The trace a death leaves (item 8 of the 2026-09-27 list, the user's choices of 2026-09-28):
    /// a dark wisp with gold motes on the last solid ground the hero stood on, holding every coin
    /// they carried. Walking up and pressing Interact takes it all back, as a bloodstain does in
    /// the Souls games; dying again first loses it for good.
    ///
    /// It only shows what <see cref="GameProgress"/> records, so it survives quitting and loading
    /// and never holds gold of its own.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LostCurrencyMark : MonoBehaviour, IInteractable
    {
        public static readonly Color Gold = new Color(1f, 0.82f, 0.4f);

        [SerializeField] private GameProgress _progress;
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private Collider2D _trigger;
        [SerializeField] private Light2D _light;
        [SerializeField] private Sprite[] _frames = System.Array.Empty<Sprite>();
        [SerializeField, Min(1f)] private float _framesPerSecond = 8f;
        [SerializeField, Min(0f)] private float _lightIntensity = 0.9f;

        private float _elapsed;
        private bool _shown;

        public bool IsShown => _shown;

        public bool CanInteract => _shown && isActiveAndEnabled;

        public string Prompt => _progress != null
            ? $"Забрати втрачене золото ({_progress.LostCurrency})"
            : string.Empty;

        private void Awake()
        {
            if (_renderer == null) { _renderer = GetComponentInChildren<SpriteRenderer>(); }
            if (_trigger == null) { _trigger = GetComponent<Collider2D>(); }
            if (_trigger != null) { _trigger.isTrigger = true; }
            Show(false);
        }

        private void OnEnable()
        {
            if (_progress == null) { _progress = FindAnyObjectByType<GameProgress>(); }
            if (_progress == null)
            {
                return;
            }

            _progress.LostCurrencyChanged += Refresh;
            _progress.Loaded += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (_progress == null)
            {
                return;
            }

            _progress.LostCurrencyChanged -= Refresh;
            _progress.Loaded -= Refresh;
        }

        private void Refresh()
        {
            var shown = _progress != null && _progress.HasLostCurrency;
            if (shown)
            {
                transform.position = _progress.LostCurrencyPosition;
            }

            Show(shown);
        }

        private void Show(bool shown)
        {
            _shown = shown;
            if (_renderer != null) { _renderer.enabled = shown; }
            if (_trigger != null) { _trigger.enabled = shown; }
            if (_light != null) { _light.enabled = shown; }
        }

        private void Update()
        {
            if (!_shown)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            if (_frames.Length > 0 && _renderer != null)
            {
                _renderer.sprite = _frames[(int)(_elapsed * _framesPerSecond) % _frames.Length];
            }

            if (_light != null)
            {
                // A slow breath, so it is found in the dark without calling like an altar.
                _light.intensity = _lightIntensity * (0.75f + 0.25f * Mathf.Sin(_elapsed * 2.2f));
            }
        }

        public void Interact(PlayerCharacter player)
        {
            if (!CanInteract || player == null || !player.IsAlive || _progress == null)
            {
                return;
            }

            var position = (Vector2)transform.position;
            if (_progress.RecoverLostCurrency() <= 0)
            {
                return;
            }

            _progress.Save();
            CombatFeedbackService.Play(FeedbackKind.LostGoldRecovered, position, Gold);
        }
    }
}
