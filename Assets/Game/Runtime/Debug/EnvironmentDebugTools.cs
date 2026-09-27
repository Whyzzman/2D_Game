using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ForgottenKingdom
{
    /// <summary>
    /// Development-only toggles for looking at the level underneath its dressing: hide the
    /// foreground, flatten the parallax, and outline the things that actually decide where the
    /// player can go.
    ///
    /// It exists because once an environment is dressed it becomes hard to tell a decorative ledge
    /// from a real one by eye, and that confusion is exactly the bug this phase most needs to
    /// avoid shipping. Off by default; it changes rendering only, never collision or gameplay.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnvironmentDebugTools : MonoBehaviour
    {
        [Tooltip("F9 toggles the panel, F10 foreground, F11 parallax, F12 collision outlines.")]
        [SerializeField] private bool _isVisible;
        [SerializeField] private bool _showForeground = true;
        [SerializeField] private bool _parallaxEnabled = true;
        [SerializeField] private bool _showCollision;
        private readonly List<SpriteRenderer> _foreground = new List<SpriteRenderer>();
        private ParallaxLayer[] _parallax = System.Array.Empty<ParallaxLayer>();
        private readonly StringBuilder _text = new StringBuilder();
        private GUIStyle _style;

        private void Start() => Rescan();

        public void Rescan()
        {
            _parallax = FindObjectsByType<ParallaxLayer>();
            _foreground.Clear();
            foreach (var renderer in FindObjectsByType<SpriteRenderer>())
            {
                if (renderer.sortingLayerName == "Foreground")
                {
                    _foreground.Add(renderer);
                }
            }
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.f9Key.wasPressedThisFrame && DebugPanels.MayToggle(this))
            {
                _isVisible = !_isVisible;
                DebugPanels.Report(this, _isVisible);
                if (_isVisible) { Rescan(); }
            }

            if (!_isVisible)
            {
                return;
            }

            if (keyboard.f10Key.wasPressedThisFrame) { SetForeground(!_showForeground); }
            if (keyboard.f11Key.wasPressedThisFrame) { SetParallax(!_parallaxEnabled); }
            if (keyboard.f12Key.wasPressedThisFrame) { _showCollision = !_showCollision; }
        }

        public void SetForeground(bool isVisible)
        {
            _showForeground = isVisible;
            foreach (var renderer in _foreground)
            {
                if (renderer != null)
                {
                    renderer.enabled = isVisible;
                }
            }
        }

        /// <summary>
        /// Pins every layer flat. Useful for judging whether the gameplay plane reads on its own,
        /// which it has to: parallax is depth, not a substitute for contrast.
        /// </summary>
        public void SetParallax(bool isEnabled)
        {
            _parallaxEnabled = isEnabled;
            foreach (var layer in _parallax)
            {
                if (layer != null)
                {
                    layer.SetEnabledForDebug(isEnabled);
                }
            }
        }

        private void OnDisable()
        {
            DebugPanels.Report(this, false);
            SetForeground(true);
            SetParallax(true);
        }

        private void OnGUI()
        {
            if (!_isVisible)
            {
                return;
            }

            _style ??= new GUIStyle(GUI.skin.label) { fontSize = 13 };
            _text.Clear();
            _text.AppendLine("ENVIRONMENT  F9 hide · F10 foreground · F11 parallax · F12 collision");
            _text.Append("  foreground: ").Append(_showForeground ? "shown" : "HIDDEN")
                .Append("   parallax: ").Append(_parallaxEnabled ? "on" : "FLAT")
                .Append("   collision: ").AppendLine(_showCollision ? "outlined" : "off");
            _text.Append("  parallax layers: ").Append(_parallax.Length)
                .Append("   foreground renderers: ").Append(_foreground.Count).AppendLine();
            GUI.Label(new Rect(12f, 640f, 640f, 90f), _text.ToString(), _style);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Outlines what really blocks movement, over the final art. Ground colliders, the pit,
        /// the checkpoint trigger and the arena trigger each get their own colour, so a ledge that
        /// only looks solid is immediately obvious.
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!_showCollision || !Application.isPlaying)
            {
                return;
            }

            foreach (var collider in FindObjectsByType<Collider2D>())
            {
                if (collider == null || !collider.enabled)
                {
                    continue;
                }

                var bounds = collider.bounds;
                Gizmos.color = Colour(collider);
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }
        }

        private static Color Colour(Collider2D collider)
        {
            if (collider.GetComponent<KillZone>() != null)
            {
                return new Color(1f, 0.2f, 0.2f, 0.9f);
            }

            if (collider.GetComponent<Checkpoint>() != null)
            {
                return new Color(1f, 0.75f, 0.3f, 0.9f);
            }

            if (collider.GetComponent<BossEncounter>() != null)
            {
                return new Color(0.7f, 0.4f, 1f, 0.9f);
            }

            return collider.isTrigger
                ? new Color(0.35f, 0.8f, 1f, 0.55f)
                : new Color(0.4f, 1f, 0.5f, 0.75f);
        }
#endif
    }
}
