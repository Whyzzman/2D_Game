using System;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ForgottenKingdom
{
    /// <summary>
    /// Development-only tools for judging animation against gameplay. It exists because the two
    /// questions an animator actually has — "is this clip the length of the real action?" and "does
    /// the contact frame land inside the active window?" — cannot be answered by watching at full
    /// speed, and guessing at them is how art and combat drift apart.
    ///
    /// Off by default, reads state only, and never alters anything except the time scale it is
    /// explicitly asked to change.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AnimationDebugTools : MonoBehaviour
    {
        [SerializeField] private PlayerAnimationController _animation;
        [SerializeField] private PlayerCombat _combat;
        [SerializeField] private PlayerCharacter _character;
        [SerializeField] private VisualSockets _sockets;
        [Tooltip("F2 toggles the readout, F3 slow motion, F4 releases a forced state.")]
        [SerializeField] private bool _isVisible;
        [SerializeField, Range(0.05f, 1f)] private float _slowMotionScale = 0.2f;
        private readonly StringBuilder _text = new StringBuilder();
        private bool _isSlow;
        private GUIStyle _style;

        private void Awake()
        {
            if (_animation == null) { _animation = FindAnyObjectByType<PlayerAnimationController>(); }
            if (_combat == null && _animation != null)
            {
                _combat = _animation.GetComponent<PlayerCombat>();
            }
            if (_character == null && _animation != null)
            {
                _character = _animation.GetComponent<PlayerCharacter>();
            }
            if (_sockets == null && _animation != null)
            {
                _sockets = _animation.GetComponentInChildren<VisualSockets>();
            }
        }

        private void OnDisable()
        {
            RestoreTimeScale();
            DebugPanels.Report(this, false);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.f2Key.wasPressedThisFrame && DebugPanels.MayToggle(this))
            {
                _isVisible = !_isVisible;
                DebugPanels.Report(this, _isVisible);
            }

            if (!_isVisible)
            {
                return;
            }

            if (keyboard.f3Key.wasPressedThisFrame) { ToggleSlowMotion(); }
            if (keyboard.f4Key.wasPressedThisFrame && _animation != null)
            {
                _animation.ForcedState = null;
            }

            if (_animation == null)
            {
                return;
            }

            if (keyboard.leftBracketKey.wasPressedThisFrame) { StepForcedState(-1); }
            if (keyboard.rightBracketKey.wasPressedThisFrame) { StepForcedState(1); }
        }

        /// <summary>
        /// Slow motion is a real time-scale change, so it is restored rather than left behind: a
        /// debug key that quietly leaves the game at a fifth speed would look like a bug.
        /// </summary>
        public void ToggleSlowMotion()
        {
            _isSlow = !_isSlow;
            GameTimeScale.SetBaseScale(_isSlow ? _slowMotionScale : 1f);
        }

        private void RestoreTimeScale()
        {
            if (_isSlow)
            {
                _isSlow = false;
                GameTimeScale.SetBaseScale(1f);
            }
        }

        /// <summary>Walks the state list so every clip can be inspected without provoking it in play.</summary>
        public void StepForcedState(int direction)
        {
            if (_animation == null)
            {
                return;
            }

            var states = (PlayerAnimationState[])Enum.GetValues(typeof(PlayerAnimationState));
            var current = Array.IndexOf(states, _animation.ForcedState ?? _animation.State);
            var next = (current + direction + states.Length) % states.Length;
            _animation.ForcedState = states[next];
        }

        private void OnGUI()
        {
            if (!_isVisible || _animation == null)
            {
                return;
            }

            _style ??= new GUIStyle(GUI.skin.label) { fontSize = 13, richText = false };
            Build();
            GUI.Label(new Rect(12f, 12f, 560f, 320f), _text.ToString(), _style);
        }

        private void Build()
        {
            _text.Clear();
            _text.AppendLine("ANIMATION  F2 hide · F3 slow · F4 release · [ ] step state");
            _text.Append("state: ").Append(_animation.State);
            if (_animation.ForcedState.HasValue)
            {
                _text.Append("  (FORCED)");
            }

            _text.AppendLine();

            var clip = _animation.Clip;
            if (clip == null || !clip.HasFrames)
            {
                _text.AppendLine("clip: MISSING — showing the fallback");
            }
            else
            {
                _text.Append("clip: ").Append(string.IsNullOrEmpty(clip.ClipName) ? "(unnamed)" : clip.ClipName)
                    .Append("  frames: ").Append(clip.FrameCount)
                    .Append(" @ ").Append(clip.FramesPerSecond.ToString("0")).AppendLine(" fps");
                _text.Append("authored length: ").Append(clip.Duration.ToString("0.000")).AppendLine(" s");
            }

            _text.Append("normalized time: ").Append(_animation.NormalizedTime.ToString("0.00"))
                .Append("   playback: ").Append(_animation.PlaybackSpeed.ToString("0.00")).Append("x");
            if (Mathf.Abs(_animation.PlaybackSpeed - 1f) > 0.25f)
            {
                _text.Append("  <- clip length disagrees with the action");
            }

            _text.AppendLine();
            AppendAttackPhases();
            _text.Append("weapon set: ")
                .AppendLine(_animation.ActiveWeaponSet != null
                    ? _animation.ActiveWeaponSet.name
                    : "none (shared clips)");
            _text.Append("gameplay action: ")
                .AppendLine(_character != null ? _character.Action.ToString() : "n/a");
            if (_sockets != null)
            {
                _text.Append("sockets: ").AppendLine(_sockets.IsComplete ? "complete" : "INCOMPLETE");
            }

            if (_isSlow)
            {
                _text.Append("time scale: ").AppendLine(Time.timeScale.ToString("0.00"));
            }
        }

        /// <summary>
        /// The part that matters most: where the swing's real windup, active and recovery windows
        /// sit, and where playback currently is against them.
        /// </summary>
        private void AppendAttackPhases()
        {
            var attack = _combat != null ? _combat.CurrentAttack : null;
            if (attack == null)
            {
                return;
            }

            var total = Mathf.Max(0.0001f, attack.Duration);
            var windup = attack.Windup / total;
            var active = (attack.Windup + attack.ActiveTime) / total;
            _text.Append("attack: ").Append(attack.DisplayName)
                .Append("  total ").Append(total.ToString("0.000")).AppendLine(" s");
            _text.Append("  windup ").Append(attack.Windup.ToString("0.000"))
                .Append(" | active ").Append(attack.ActiveTime.ToString("0.000"))
                .Append(" | recovery ").Append(attack.Recovery.ToString("0.000")).AppendLine();
            _text.Append("  active window: ").Append(windup.ToString("0.00"))
                .Append(" .. ").Append(active.ToString("0.00"))
                .Append(" of the clip").AppendLine();
            _text.Append("  contact frame should land in ")
                .Append(Mathf.RoundToInt(windup * Mathf.Max(1, FrameCount())))
                .Append("..").Append(Mathf.RoundToInt(active * Mathf.Max(1, FrameCount())))
                .AppendLine();
        }

        private int FrameCount() => _animation.Clip != null ? _animation.Clip.FrameCount : 0;

#if UNITY_EDITOR
        /// <summary>
        /// Draws the attack's real hitbox, so the animator can see whether the drawn point of
        /// contact matches the box the game actually uses. The box comes from the attack data and
        /// is never derived from the art.
        /// </summary>
        private void OnDrawGizmos()
        {
            var attack = _combat != null ? _combat.CurrentAttack : null;
            if (attack == null || _character == null)
            {
                return;
            }

            var facing = _character.GetComponent<PlayerMotor2D>();
            var direction = facing != null ? facing.FacingDirection : 1;
            var centre = (Vector2)_character.transform.position
                + new Vector2(attack.HitboxOffset.x * direction, attack.HitboxOffset.y);
            Gizmos.color = new Color(1f, 0.35f, 0.2f, 0.9f);
            Gizmos.DrawWireCube(centre, attack.HitboxSize);
        }
#endif
    }
}
