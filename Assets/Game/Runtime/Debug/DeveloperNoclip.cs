using UnityEngine;
using UnityEngine.InputSystem;

namespace ForgottenKingdom
{
    /// <summary>
    /// Development-only flight through the level, to look the built zones over.
    ///
    /// Ctrl+N toggles it. While flying: WASD or the arrows move, Shift is faster, the mouse wheel
    /// or - and = zoom the view out and in. The hero's body leaves the physics world, so walls,
    /// spikes and enemies pass through and nothing can hit it; the camera drops its rooms and its
    /// dead zone and follows freely. Ctrl+N again lands the hero where it is.
    ///
    /// Not a panel: it never takes the panels' keys, and it works with one of them open. It lives
    /// on the player's developer-only object, so a release build never has it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DeveloperNoclip : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float _speed = 12f;
        [SerializeField, Min(1f)] private float _fastMultiplier = 3f;
        [SerializeField, Min(1f)] private float _maxZoomOut = 5f;

        private PlayerMotor2D _motor;
        private Rigidbody2D _body;
        private CameraFollow2D _camera;
        private float _zoom = 1f;
        private GUIStyle _style;

        /// <summary>True while the hero is flying; the respawner does not kill it below the world.</summary>
        public static bool IsFlying { get; private set; }

        public bool IsActive { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad() => IsFlying = false;

        private void Awake()
        {
            _motor = GetComponentInParent<PlayerMotor2D>();
            _body = _motor != null ? _motor.GetComponent<Rigidbody2D>() : null;
        }

        private void OnDisable()
        {
            SetActive(false);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || _motor == null)
            {
                return;
            }

            var control = keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
            if (control && keyboard.nKey.wasPressedThisFrame)
            {
                SetActive(!IsActive);
            }

            if (IsActive)
            {
                Fly(keyboard);
            }
        }

        public void SetActive(bool active)
        {
            if (active == IsActive || _motor == null)
            {
                return;
            }

            IsActive = active;
            IsFlying = active;
            _camera ??= FindAnyObjectByType<CameraFollow2D>();
            if (active)
            {
                _motor.enabled = false;
                if (_body != null) { _body.simulated = false; }
                _zoom = 1f;
                if (_camera != null) { _camera.IsFree = true; }
                return;
            }

            if (_body != null) { _body.simulated = true; }
            _motor.enabled = true;
            _motor.ResetTo(_motor.transform.position);
            if (_camera != null)
            {
                _camera.IsFree = false;
                _camera.SetZoom(1f, 0.3f);
            }
        }

        private void Fly(Keyboard keyboard)
        {
            var direction = Vector2.zero;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) { direction.x -= 1f; }
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) { direction.x += 1f; }
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) { direction.y -= 1f; }
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) { direction.y += 1f; }

            var speed = _speed * (keyboard.shiftKey.isPressed ? _fastMultiplier : 1f);
            var step = direction.normalized * (speed * Time.unscaledDeltaTime);
            if (step != Vector2.zero)
            {
                var position = _motor.transform.position + (Vector3)step;
                _motor.transform.position = position;
                if (_body != null) { _body.position = position; }
            }

            var zoom = _zoom;
            var scroll = Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
            if (scroll != 0f) { zoom *= scroll > 0f ? 0.9f : 1.1f; }
            if (keyboard.minusKey.isPressed) { zoom *= 1f + Time.unscaledDeltaTime; }
            if (keyboard.equalsKey.isPressed) { zoom /= 1f + Time.unscaledDeltaTime; }
            _zoom = Mathf.Clamp(zoom, 0.6f, _maxZoomOut);
            if (_camera != null) { _camera.SetFreeZoom(_zoom); }
        }

        private void OnGUI()
        {
            if (!IsActive || _motor == null)
            {
                return;
            }

            _style ??= new GUIStyle(GUI.skin.label) { fontSize = 13 };
            var position = _motor.transform.position;
            GUI.Label(new Rect(12f, 12f, 900f, 40f),
                "NOCLIP  Ctrl+N land · WASD/arrows fly · Shift fast · wheel or -/= zoom\n"
                + $"  x {position.x:0.0}  y {position.y:0.0}  zoom {_zoom:0.0}", _style);
        }
    }
}
