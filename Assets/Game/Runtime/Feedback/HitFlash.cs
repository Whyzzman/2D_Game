using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// A landed hit flashes the creature for a moment, as Hollow Knight's enemies do: the one
    /// sign on a boss, which never flinches, that the blow went in. The flash is a flat-coloured
    /// copy of the body sprite laid on top and fading out, because tinting a dark silhouette can
    /// only make it darker. Warm and pale rather than white (the user's choice, 2026-09-27), in
    /// the candlelit palette of the game.
    ///
    /// It times itself in real seconds: the hit-stop freezes the game on the same blow, and the
    /// flash has to be seen through that freeze, not after it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HitFlash : MonoBehaviour
    {
        [SerializeField] private CombatActor _actor;
        [Tooltip("The sprites the flash takes its shape from: the body, and the face drawn over it.")]
        [SerializeField] private SpriteRenderer[] _sources = System.Array.Empty<SpriteRenderer>();
        [Tooltip("ForgottenKingdom/SpriteSilhouette. Without it there is no flash and nothing breaks.")]
        [SerializeField] private Material _material;
        [SerializeField] private Color _color = new Color(1f, 0.93f, 0.78f, 0.85f);
        [SerializeField, Min(0.01f)] private float _duration = 0.1f;

        private SpriteRenderer[] _overlays = System.Array.Empty<SpriteRenderer>();
        private float _startedAt = float.NegativeInfinity;

        public bool IsFlashing => Time.unscaledTime < _startedAt + _duration;

        private void Awake()
        {
            if (_actor == null) { _actor = GetComponentInParent<CombatActor>(); }
            if (_material == null)
            {
                return;
            }

            _overlays = new SpriteRenderer[_sources.Length];
            for (var i = 0; i < _sources.Length; i++)
            {
                if (_sources[i] == null)
                {
                    continue;
                }

                var overlay = new GameObject("HitFlash");
                overlay.transform.SetParent(_sources[i].transform, false);
                _overlays[i] = overlay.AddComponent<SpriteRenderer>();
                _overlays[i].sharedMaterial = _material;
                _overlays[i].enabled = false;
            }
        }

        private void OnEnable()
        {
            if (_actor != null) { _actor.DamageTaken += OnDamageTaken; }
        }

        private void OnDisable()
        {
            if (_actor != null) { _actor.DamageTaken -= OnDamageTaken; }
            foreach (var overlay in _overlays)
            {
                if (overlay != null) { overlay.enabled = false; }
            }

            _startedAt = float.NegativeInfinity;
        }

        private void OnDamageTaken(DamageInfo info, DamageResult result)
        {
            // A blow that went in. A blocked or ignored one is the shield's or the dodge's moment,
            // and a burn ticking every second would strobe.
            if (result.Outcome != DamageOutcome.Hit || info.HasTag(DamageTags.Status))
            {
                return;
            }

            _startedAt = Time.unscaledTime;
        }

        private void LateUpdate()
        {
            var fade = Mathf.Clamp01(1f - (Time.unscaledTime - _startedAt) / _duration);
            var top = int.MinValue;
            foreach (var source in _sources)
            {
                if (source != null) { top = Mathf.Max(top, source.sortingOrder); }
            }

            for (var i = 0; i < _overlays.Length; i++)
            {
                var overlay = _overlays[i];
                var source = _sources[i];
                if (overlay == null)
                {
                    continue;
                }

                var flashing = IsFlashing && source.enabled && source.gameObject.activeInHierarchy
                    && source.sprite != null;
                overlay.enabled = flashing;
                if (!flashing)
                {
                    continue;
                }

                overlay.sprite = source.sprite;
                overlay.flipX = source.flipX;
                overlay.flipY = source.flipY;
                overlay.sortingLayerID = source.sortingLayerID;
                // Over every part, so the whole creature reads as one flash.
                overlay.sortingOrder = top + 1;
                overlay.color = new Color(_color.r, _color.g, _color.b, _color.a * fade);
            }
        }
    }
}
