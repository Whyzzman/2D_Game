using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Moves a background layer a fraction of the camera's travel, which is what sells depth once
    /// the values have already separated the layers.
    ///
    /// Two rules keep it from becoming a problem. The factor is always well under one, so a wall
    /// never appears to slide against the level it belongs to. And the layer is scrolled by
    /// offsetting its texture rather than by teleporting the object, so a dash can never reveal the
    /// end of a background — there is no end.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ParallaxLayer : MonoBehaviour
    {
        [Tooltip("Share of camera movement this layer follows. 0 is painted on the screen, "
            + "1 is nailed to the world.")]
        [SerializeField, Range(0f, 1f)] private float _factor = 0.5f;
        [Tooltip("Vertical share, kept smaller: strong vertical parallax reads as sliding.")]
        [SerializeField, Range(0f, 1f)] private float _verticalFactor = 0.15f;
        [Tooltip("A tall zone: the layer keeps its height on the screen, offset from the view's "
            + "centre, and only scrolls sideways.")]
        [SerializeField] private bool _lockVertical;
        [SerializeField] private float _verticalOffset = -8f;
        [SerializeField] private Transform _camera;
        private SpriteRenderer _renderer;
        private MaterialPropertyBlock _block;
        private Vector3 _origin;
        private Vector2 _spriteWorldSize = Vector2.one;

        public float Factor => _factor;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _origin = transform.position;
            if (_camera == null && Camera.main != null)
            {
                _camera = Camera.main.transform;
            }

            if (_renderer.sprite != null)
            {
                _spriteWorldSize = _renderer.sprite.bounds.size;
            }
        }

        private void LateUpdate()
        {
            if (_camera == null || _renderer == null)
            {
                return;
            }

            // The layer follows a fraction of the camera's travel. Against the camera it therefore
            // drifts at (factor - 1), which is the parallax.
            var delta = _camera.position - _origin;
            var x = _origin.x + delta.x * _factor;
            var y = _lockVertical ? _camera.position.y + _verticalOffset : _origin.y + delta.y * _verticalFactor;

            // Then it is wrapped by whole tile widths so it always covers the view. The content
            // tiles seamlessly, so a jump of exactly one tile is invisible — and a dash can never
            // outrun the background, because there is no edge to reach.
            var tile = Mathf.Max(0.001f, _spriteWorldSize.x);
            var drift = _camera.position.x - x;
            if (Mathf.Abs(drift) >= tile)
            {
                x += Mathf.Round(drift / tile) * tile;
            }

            transform.position = new Vector3(x, y, _origin.z);
        }

        /// <summary>Lets the debug tools pin every layer flat to check the gameplay plane alone.</summary>
        public void SetEnabledForDebug(bool isEnabled)
        {
            enabled = isEnabled;
            if (!isEnabled)
            {
                transform.position = _origin;
            }
        }
    }
}
