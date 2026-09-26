using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// A brief full-screen tint, reserved for the handful of moments that earn one: a parry, a
    /// guard break, the Guardian changing phase, the Guardian dying.
    ///
    /// Everything about it is deliberately restrained. It never reaches full white, it is measured
    /// in hundredths of a second, and it scales with an accessibility setting that can take it to
    /// zero. A flash on every hit stops being information and becomes an obstacle, which is the
    /// opposite of what feedback is for.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ScreenFlash : MonoBehaviour
    {
        [Tooltip("Hard ceiling on opacity, whatever a profile asks for.")]
        [SerializeField, Range(0f, 1f)] private float _maximumAlpha = 0.35f;
        [SerializeField, Min(0.01f)] private float _fadeDuration = 0.12f;
        [SerializeField] private SpriteRenderer _overlay;
        private float _alpha;
        private Color _color = Color.white;

        public float CurrentAlpha => _alpha;

        private void Awake()
        {
            if (_overlay != null)
            {
                _overlay.enabled = false;
            }
        }

        /// <summary>
        /// Requests a flash. A weaker one while a stronger is still fading is ignored, so several
        /// events landing together cannot add up into a white screen.
        /// </summary>
        public void Flash(Color color, float intensity)
        {
            var wanted = Mathf.Clamp01(intensity) * _maximumAlpha;
            if (wanted <= 0.001f || wanted <= _alpha)
            {
                return;
            }

            _color = color;
            _alpha = wanted;
        }

        private void LateUpdate()
        {
            if (_overlay == null)
            {
                return;
            }

            if (_alpha <= 0f)
            {
                if (_overlay.enabled)
                {
                    _overlay.enabled = false;
                }

                return;
            }

            // Unscaled: a flash that paused during its own hit-stop would linger far too long.
            _alpha = Mathf.Max(0f, _alpha - Time.unscaledDeltaTime * (_maximumAlpha / _fadeDuration));
            _overlay.enabled = true;
            _overlay.color = new Color(_color.r, _color.g, _color.b, _alpha);

            // The overlay rides with the camera rather than sitting in the world.
            _overlay.transform.position = new Vector3(transform.position.x, transform.position.y,
                transform.position.z + 1f);
        }

        public void Clear()
        {
            _alpha = 0f;
            if (_overlay != null)
            {
                _overlay.enabled = false;
            }
        }

        private void OnDisable() => Clear();
    }
}
