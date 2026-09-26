using System;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ForgottenKingdom
{
    /// <summary>
    /// Development-only controls for combat feedback: fire any effect on demand, kick the camera
    /// at any tier, and watch what is currently live.
    ///
    /// It exists because the only way to judge whether a parry reads louder than a block, or
    /// whether a slam reads louder than both, is to see them one after another without having to
    /// produce each one in a real fight.
    ///
    /// Off by default, and it only calls the same public entry point the game uses.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FeedbackDebugTools : MonoBehaviour
    {
        [Tooltip("Ctrl+F toggles the panel; , and . step the effect; Enter fires it.")]
        [SerializeField] private bool _isVisible;
        [SerializeField] private CameraShaker _shaker;
        [SerializeField] private PlayerCharacter _player;
        private FeedbackKind _selected = FeedbackKind.LightHit;
        private ShakeProfile _shakeTier = ShakeProfile.Small;
        private readonly StringBuilder _text = new StringBuilder();
        private GUIStyle _style;

        private void Start()
        {
            if (_shaker == null && Camera.main != null)
            {
                _shaker = Camera.main.GetComponent<CameraShaker>();
            }

            if (_player == null) { _player = FindAnyObjectByType<PlayerCharacter>(); }
        }

        private void OnDisable() => DebugPanels.Report(this, false);

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            var control = keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
            if (control && keyboard.fKey.wasPressedThisFrame && DebugPanels.MayToggle(this))
            {
                _isVisible = !_isVisible;
                DebugPanels.Report(this, _isVisible);
            }

            if (!_isVisible)
            {
                return;
            }

            if (keyboard.commaKey.wasPressedThisFrame) { StepEffect(-1); }
            if (keyboard.periodKey.wasPressedThisFrame) { StepEffect(1); }
            if (keyboard.enterKey.wasPressedThisFrame) { FireSelected(); }
            if (keyboard.backslashKey.wasPressedThisFrame) { StepShake(1); }
            if (keyboard.slashKey.wasPressedThisFrame) { FireShake(); }
            if (keyboard.pageUpKey.wasPressedThisFrame) { StepAccessibility(1); }
            if (keyboard.pageDownKey.wasPressedThisFrame) { StepAccessibility(-1); }
        }

        public void StepEffect(int direction)
        {
            var kinds = (FeedbackKind[])Enum.GetValues(typeof(FeedbackKind));
            var index = Array.IndexOf(kinds, _selected);
            _selected = kinds[(index + direction + kinds.Length) % kinds.Length];
        }

        /// <summary>Spawns the selected effect in front of the player, where it can be seen.</summary>
        public void FireSelected()
        {
            var at = _player != null
                ? (Vector2)_player.transform.position + new Vector2(1.2f, 0.2f)
                : Vector2.zero;
            CombatFeedbackService.Play(_selected, at, Vector2.right);
        }

        public void StepShake(int direction)
        {
            var tiers = (ShakeProfile[])Enum.GetValues(typeof(ShakeProfile));
            var index = Array.IndexOf(tiers, _shakeTier);
            _shakeTier = tiers[(index + direction + tiers.Length) % tiers.Length];
        }

        public void FireShake() => _shaker?.Shake(_shakeTier);

        /// <summary>
        /// Walks the accessibility scalars between full, half and off together, so the "can this
        /// still be played with all of it turned down" question is one keypress away.
        /// </summary>
        public void StepAccessibility(int direction)
        {
            var next = Mathf.Clamp(FeedbackSettings.ScreenShakeMultiplier + direction * 0.5f,
                0f, 1f);
            FeedbackSettings.ScreenShakeMultiplier = next;
            FeedbackSettings.FlashIntensity = next;
        }

        private void OnGUI()
        {
            if (!_isVisible)
            {
                return;
            }

            _style ??= new GUIStyle(GUI.skin.label) { fontSize = 13 };
            var service = CombatFeedbackService.Instance;
            _text.Clear();
            _text.AppendLine("FEEDBACK  ctrl+F hide · , . effect · Enter fire · \\ shake tier "
                + "· / shake · PgUp/PgDn accessibility");
            _text.Append("  effect: ").Append(_selected);
            if (service != null && service.Library != null)
            {
                _text.Append(service.Library.Has(_selected) ? "" : "   (NO PROFILE)");
            }

            _text.AppendLine();
            _text.Append("  shake tier: ").Append(_shakeTier)
                .Append("   amplitude: ")
                .Append((_shaker != null ? _shaker.CurrentAmplitude : 0f).ToString("0.000"))
                .AppendLine();
            _text.Append("  live bursts: ").Append(service != null ? service.LiveBursts : 0)
                .Append("   live lights: ").Append(service != null ? service.LiveLights : 0)
                .Append("   hit-stop: ").Append(HitStop.IsActive ? "ON" : "off")
                .AppendLine();
            _text.Append("  shake x").Append(FeedbackSettings.ScreenShakeMultiplier.ToString("0.0"))
                .Append("   flash x").Append(FeedbackSettings.FlashIntensity.ToString("0.0"))
                .Append("   hit-stop x")
                .Append(FeedbackSettings.HitStopMultiplier.ToString("0.0"))
                .AppendLine();
            GUI.Label(new Rect(12f, 740f, 760f, 110f), _text.ToString(), _style);
        }
    }
}
