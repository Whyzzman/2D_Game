using UnityEngine;
using UnityEngine.InputSystem;

namespace ForgottenKingdom
{
    /// <summary>
    /// The title screen's camera breathing (design/ui/menus/menus.html): a slow drift of a few
    /// hundredths of a unit and a slight lean away from the mouse, smoothed, so the lit Tomb
    /// behind the menu feels alive without ever moving the menu itself. Unscaled time; still
    /// under reduced motion. <see cref="MainMenuController"/> puts it on the menu camera at run
    /// time, so the scene is not changed.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MenuCameraDrift : MonoBehaviour
    {
        private const float DriftX = 0.14f;
        private const float DriftY = 0.06f;
        private const float LeanX = 0.18f;
        private const float LeanY = 0.08f;
        private const float Smoothing = 2.2f;

        private Vector3 _home;
        private Vector2 _offset;

        private void OnEnable()
        {
            _home = transform.position;
            _offset = Vector2.zero;
        }

        private void OnDisable()
        {
            transform.position = _home;
        }

        private void LateUpdate()
        {
            var target = Vector2.zero;
            if (!FeedbackSettings.ReducedMotion)
            {
                var t = Time.unscaledTime;
                target = new Vector2(Mathf.Sin(t * 0.07f) * DriftX, Mathf.Sin(t * 0.05f + 1f) * DriftY);
                var mouse = Mouse.current;
                if (mouse != null && Screen.width > 0 && Screen.height > 0)
                {
                    var p = mouse.position.ReadValue();
                    var x = Mathf.Clamp(p.x / Screen.width * 2f - 1f, -1f, 1f);
                    var y = Mathf.Clamp(p.y / Screen.height * 2f - 1f, -1f, 1f);
                    target += new Vector2(-x * LeanX, -y * LeanY);
                }
            }

            _offset = Vector2.Lerp(_offset, target, Mathf.Min(1f, Time.unscaledDeltaTime * Smoothing));
            transform.position = _home + (Vector3)_offset;
        }
    }
}
