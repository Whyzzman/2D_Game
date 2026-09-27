using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// A dark shape in front of the play (the user's choice, 2026-09-27: sparse, at the edges of
    /// the frame): a broken column or a chain in the Tomb, roots or a branch in the forest. It is
    /// nearer than the level, so it slides past faster than the world does — the one parallax
    /// layer in front of the camera's subject — and it thins out while the player or an enemy is
    /// behind it, so it can never hide a fight.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ForegroundSilhouette : MonoBehaviour
    {
        /// <summary>How much faster than the world it slides past the camera.</summary>
        public const float Nearness = 0.3f;

        /// <summary>How far it thins while something stands behind it.</summary>
        public const float BehindAlpha = 0.35f;

        // None vertically: a shape standing on the floor would sink into it as the view rose.
        private const float VerticalNearness = 0f;
        private const float FadeSpeed = 5f;

        [SerializeField] private Transform _camera;

        /// <summary>The player and the enemies: nothing in a fight may be hidden behind it.</summary>
        private const int FighterLayers = (1 << 8) | (1 << 10);

        private static readonly System.Collections.Generic.List<Collider2D> Behind =
            new System.Collections.Generic.List<Collider2D>();

        private SpriteRenderer _renderer;
        private Vector3 _origin;
        private float _alpha = 1f;

        public float Alpha => _alpha;

        /// <summary>Where it stands in the level; the parallax draws it off that as the view moves.</summary>
        public Vector3 Origin => _origin;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _origin = transform.position;
            if (_camera == null && Camera.main != null) { _camera = Camera.main.transform; }
        }

        private void LateUpdate()
        {
            if (_camera != null)
            {
                // Nearer than the level: against the camera it drifts at -(1 + nearness).
                var delta = _camera.position - _origin;
                transform.position = new Vector3(_origin.x - delta.x * Nearness,
                    _origin.y - delta.y * VerticalNearness, _origin.z);
            }

            var bounds = _renderer.bounds;
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(FighterLayers);
            var behind = Physics2D.OverlapBox(bounds.center, bounds.size * 0.9f, 0f, filter, Behind) > 0;
            _alpha = Mathf.MoveTowards(_alpha, behind ? BehindAlpha : 1f, FadeSpeed * Time.deltaTime);
            var color = _renderer.color;
            if (!Mathf.Approximately(color.a, _alpha))
            {
                color.a = _alpha;
                _renderer.color = color;
            }
        }
    }
}
